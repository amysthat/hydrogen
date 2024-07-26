using Hydrogen.Generation.Variables;
using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeStmtExit : NodeStatement
{
    public int LineNumber { get; set; }

    public NodeExpression ReturnCodeExpression;
}
public struct NodeStmtWrite : NodeStatement
{
    public int LineNumber { get; set; }

    public NodeExpression String;
}
public struct NodeStmtVariable : NodeStatement
{
    public int LineNumber { get; set; }

    public Token Identifier;
    public VariableType Type;
    public NodeExpression ValueExpression;
}
public struct NodeStmtAssign : NodeStatement
{
    public int LineNumber { get; set; }

    public bool IsPointerValue;
    public Token Identifier;
    public NodeExpression ValueExpression;
}
public interface NodeStatement : Node
{
}
public struct NodeStmtIf : NodeStatement
{
    public int LineNumber { get; set; }

    public NodeStmtIfSingle This;
    public List<NodeStmtIfSingle> Elifs;
    public NodeScope? Else;
}
public struct NodeStmtIfSingle
{
    public int LineNumber { get; set; }

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
                LineNumber = exitToken.LineNumber,
                ReturnCodeExpression =
                new NodeTermInteger
                {
                    LineNumber = exitToken.LineNumber,
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
            throw new ParsingException(exitToken.LineNumber, "Invalid expression after 'exit'.");
        }

        var exitStatement = new NodeStmtExit { LineNumber = exitToken.LineNumber, ReturnCodeExpression = nodeExpr };

        if (parser.TryPeek(TokenType.Semicolon, token => throw new ParsingException(exitToken.LineNumber, "';' expected after 'exit'.")))
        {
            parser.Consume(); // ";"
        }

        return exitStatement;
    }

    public static NodeStmtWrite ParseWrite(Parser parser)
    {
        var writeToken = parser.Consume()!.Value;

        var nodeExpr = parser.ParseExpression(); // return code

        if (nodeExpr == null)
        {
            throw new ParsingException(writeToken.LineNumber, "Invalid expression after 'write'.");
        }

        var writeStatement = new NodeStmtWrite { LineNumber = writeToken.LineNumber, String = nodeExpr };

        if (parser.TryPeek(TokenType.Semicolon, token => throw new ParsingException(writeToken.LineNumber, "Expected ';' after 'write'.")))
        {
            parser.Consume(); // ";"
        }

        return writeStatement;
    }

    public static NodeStmtVariable ParseVariableStatement(Parser parser)
    {
        var identifierToken = parser.Consume()!.Value;
        parser.Consume(); // ":"

        var variableType = parser.ParseVariableType();

        if (variableType == null)
        {
            throw new ParsingException(identifierToken.LineNumber, "Invalid variable type after variable hint.");
        }

        parser.TryPeek(TokenType.Equals, token => throw new ParsingException(token.LineNumber, "Expected '=' after variable type."));

        parser.Consume(); // "="

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            throw new ParsingException(identifierToken.LineNumber, "Expected expression after '=' of variable statement.");
        }

        var statement = new NodeStmtVariable { LineNumber = identifierToken.LineNumber, Identifier = identifierToken, Type = variableType!, ValueExpression = expression! };

        if (parser.TryPeek(TokenType.Semicolon, _ => throw new ParsingException(identifierToken.LineNumber, "Expected ';' after variable statement.")))
        {
            parser.Consume(); // ";"
        }

        return statement;
    }

    public static NodeStmtAssign ParseVariableAssignment(Parser parser, bool isPointerValue)
    {
        Token identifierToken;

        if (isPointerValue)
        {
            parser.TryPeek(TokenType.Star, errToken => throw new InvalidProgramException());

            parser.Consume(); // *
            identifierToken = parser.Consume()!.Value; // Pointer
        }
        else
        {
            identifierToken = parser.Consume()!.Value; // Identifier
        }

        if (parser.Consume()!.Value.Type != TokenType.Equals)
            throw new InvalidProgramException();

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            throw new ParsingException(identifierToken.LineNumber, "Expected expression after '=' of variable assignment.");
        }

        var statement = new NodeStmtAssign { LineNumber = identifierToken.LineNumber, IsPointerValue = isPointerValue, Identifier = identifierToken, ValueExpression = expression! };

        parser.TryPeek(TokenType.Semicolon, errToken => throw new ParsingException(errToken.LineNumber, "Expected ';' after variable statement."));
        parser.Consume(); // ";"

        return statement;
    }

    public static NodeStmtIf ParseIfStatement(Parser parser)
    {
        var ifToken = parser.Consume()!.Value;

        var nodeStmtIf = new NodeStmtIf
        {
            LineNumber = ifToken.LineNumber,
            Elifs = [],
        };

        var expression = parser.ParseExpression();

        if (expression == null)
        {
            throw new ParsingException(ifToken.LineNumber, "Invalid expression after 'if'.");
        }

        var scope = parser.ParseScope();

        if (!scope.HasValue)
        {
            throw new ParsingException(ifToken.LineNumber, "Invalid scope encountered after 'if'.");
        }

        nodeStmtIf.This.Expression = expression!;
        nodeStmtIf.This.Scope = scope!.Value;

        while (parser.TryConsume(TokenType.Elif, out var token))
        {
            var elifStmt = new NodeStmtIfSingle
            {
                LineNumber = token!.Value.LineNumber,
            };

            var elifExpression = parser.ParseExpression();

            if (elifExpression == null)
            {
                throw new ParsingException(token!.Value.LineNumber, "Invalid expression encountered after 'elif'.");
            }

            var elifScope = parser.ParseScope();

            if (!elifScope.HasValue)
            {
                throw new ParsingException(token!.Value.LineNumber, "Invalid scope encountered after 'elif'");
            }

            elifStmt.Expression = elifExpression!;
            elifStmt.Scope = elifScope!.Value;

            nodeStmtIf.Elifs.Add(elifStmt);
        }

        if (parser.TryConsume(TokenType.Else, out _))
        {
            parser.TryPeek(TokenType.OpenCurlyBraces, token => throw new ParsingException(token.LineNumber, "'{' missing after 'else'."));

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