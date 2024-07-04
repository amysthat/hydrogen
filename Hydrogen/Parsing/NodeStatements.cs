using Hydrogen.Generation;
using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeStmtExit : NodeStatement
{
    public NodeExpression ReturnCodeExpression;
}
public struct NodeStmtVariable : NodeStatement
{
    public Token Identifier;
    public VariableType Type;
    public NodeExpression ValueExpression;
}
public struct NodeStmtAssign : NodeStatement
{
    public Token Identifier;
    public NodeExpression ValueExpression;
}
public interface NodeStatement
{
}
public struct NodeStmtIf : NodeStatement
{
    public NodeStmtIfSingle This;
    public List<NodeStmtIfSingle> Elifs;
    public NodeScope? Else;
}
public struct NodeStmtIfSingle
{
    public NodeExpression Expression;
    public NodeScope Scope;
}

internal static class NodeStatements
{
    public static NodeStmtExit ParseExit(Parser parser)
    {
        var exitToken = parser.Consume()!.Value;

        if (parser.TryConsume(TokenType.Semicolon, out _))
        {
            return new NodeStmtExit
            {
                ReturnCodeExpression =
                new NodeTermInteger
                {
                    Int_Lit =
                    new Token
                    {
                        Type = TokenType.Int_Lit,
                        Value = "0",
                        LineNumber = exitToken.LineNumber
                    }
                }
            };
        }

        var nodeExpr = parser.ParseExpression(); // return code

        if (nodeExpr == null)
        {
            parser.ErrorInvalid("expression after 'exit'", exitToken.LineNumber);
            return new NodeStmtExit(); // Unreachable
        }

        var exitStatement = new NodeStmtExit { ReturnCodeExpression = nodeExpr };

        if (parser.TryPeek(TokenType.Semicolon, token => parser.ErrorExpected("';' after 'exit'", token.LineNumber)))
        {
            parser.Consume(); // ";"
        }

        return exitStatement;
    }

    public static NodeStmtVariable ParseVariableStatement(Parser parser)
    {
        var identifierToken = parser.Consume()!.Value;
        parser.Consume(); // ":"

        var variableType = parser.ParseVariableType();

        if (variableType == null)
        {
            parser.ErrorInvalid("variable type after variable hint", identifierToken.LineNumber);
        }

        parser.TryPeek(TokenType.Equals, token => parser.ErrorExpected("'=' expected after variable type", token.LineNumber));

        parser.Consume(); // "="

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            parser.ErrorExpected("expression after '=' of variable statement", identifierToken.LineNumber);
        }

        var statement = new NodeStmtVariable { Identifier = identifierToken, Type = variableType!, ValueExpression = expression! };

        if (parser.TryPeek(TokenType.Semicolon, _ => parser.ErrorExpected("';' after variable statement", identifierToken.LineNumber)))
        {
            parser.Consume(); // ";"
        }

        return statement;
    }

    public static NodeStmtAssign ParseVariableAssignment(Parser parser)
    {
        var identifierToken = parser.Consume()!.Value; // Identifier

        if (parser.Consume()!.Value.Type != TokenType.Equals)
            throw new InvalidProgramException();

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            parser.ErrorExpected("expression after '=' of variable assignment", identifierToken.LineNumber);
        }

        var statement = new NodeStmtAssign { Identifier = identifierToken, ValueExpression = expression! };

        if (parser.TryPeek(TokenType.Semicolon, token => parser.ErrorExpected("';' after variable statement", token.LineNumber)))
        {
            parser.Consume(); // ";"
        }

        return statement;
    }

    public static NodeStmtIf ParseIfStatement(Parser parser)
    {
        var nodeStmtIf = new NodeStmtIf
        {
            Elifs = [],
        };

        var ifToken = parser.Consume()!.Value;

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            parser.ErrorInvalid("expression after 'if'", ifToken.LineNumber);
        }

        var scope = parser.ParseScope();

        if (!scope.HasValue)
        {
            parser.ErrorInvalid("scope after 'if'", ifToken.LineNumber);
        }

        nodeStmtIf.This.Expression = expression!;
        nodeStmtIf.This.Scope = scope!.Value;

        while (parser.TryConsume(TokenType.Elif, out var token))
        {
            var elifStmt = new NodeStmtIfSingle();

            var elifExpression = parser.ParseExpression();

            if (elifExpression == null)
            {
                parser.ErrorInvalid("expression after 'elif'", token!.Value.LineNumber);
            }

            var elifScope = parser.ParseScope();

            if (!elifScope.HasValue)
            {
                parser.ErrorInvalid("scope after 'elif'", token!.Value.LineNumber);
            }

            elifStmt.Expression = elifExpression!;
            elifStmt.Scope = elifScope!.Value;

            nodeStmtIf.Elifs.Add(elifStmt);
        }

        if (parser.TryConsume(TokenType.Else, out _))
        {
            parser.TryPeek(TokenType.OpenCurlyBraces, token => parser.ErrorExpected("'{' after 'else'", token.LineNumber));

            var elseScope = parser.ParseScope();

            if (!elseScope.HasValue)
            {
                throw new InvalidProgramException("ParseScope() for else returned null when it should've Environment.Exit.");
            }

            nodeStmtIf.Else = elseScope!.Value;
        }

        return nodeStmtIf;
    }
}