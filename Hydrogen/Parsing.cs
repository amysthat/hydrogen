using System.Runtime.CompilerServices;
using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public enum NodeExpressionType
{
    Term,
    BinaryExpression,
    Cast,
}
public struct NodeExprCast
{
    public VariableType CastType;
    public NodeExpression Expression;
}
public class NodeExpression
{
    public NodeExpressionType Type;

    public NodeTerm Term;
    public NodeBinaryExpression BinaryExpression;
    public NodeExprCast Cast;
}

public struct NodeTermInteger
{
    public Token Int_Lit;
}
public struct NodeTermIdentifier
{
    public Token Identifier;
}
public struct NodeTermParen
{
    public NodeExpression Expression;
}
public enum NodeTermType
{
    Integer,
    Identifier,
    Parenthesis
}
public struct NodeTerm
{
    public NodeTermType Type;

    public NodeTermInteger Integer;
    public NodeTermIdentifier Identifier;
    public NodeTermParen Parenthesis;
}

public enum NodeBinaryExpressionType
{
    Add,
    Subtract,
    Multiply,
    Divide,
}
public struct NodeBinaryExpression
{
    public NodeBinaryExpressionType Type;

    public NodeExpression Left;
    public NodeExpression Right;
}

public struct NodeStmtExit
{
    public NodeExpression ReturnCodeExpression;
}
public struct NodeStmtVar
{
    public Token Identifier;
    public VariableType Type;
    public NodeExpression ValueExpression;
}
public struct NodeStmtAssign
{
    public Token Identifier;
    public NodeExpression ValueExpression;
}
public enum NodeStatementType
{
    Exit,
    Variable,
    Assign,
    Scope,
    If,
}
public struct NodeStatement
{
    public NodeStatementType Type;

    public NodeStmtExit Exit;
    public NodeStmtVar Variable;
    public NodeStmtAssign Assign;
    public NodeScope Scope;
    public NodeStmtIf If;
}

public struct NodeStmtIf
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

public struct NodeScope
{
    public List<NodeStatement> Statements;
}

public struct NodeProgram
{
    public NodeProgram()
    {
        Statements = new();
    }

    public List<NodeStatement> Statements;
}

public class Parser
{
    #region Header
    private List<Token> tokens;
    private int position;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
        position = 0;
    }
    #endregion

    public NodeTerm? ParseTerm()
    {
        if (!Peek().HasValue)
            return null;

        if (TryConsume(TokenType.Int_Lit, out var intLitToken))
        {
            var nodeTermInteger = new NodeTermInteger
            {
                Int_Lit = intLitToken!.Value,
            };
            return new NodeTerm { Type = NodeTermType.Integer, Integer = nodeTermInteger };
        }
        else if (TryConsume(TokenType.Identifier, out var identToken)) // Variable type handled by generator
            return new NodeTerm { Type = NodeTermType.Identifier, Identifier = new NodeTermIdentifier { Identifier = identToken!.Value } };
        else if (TryConsume(TokenType.OpenParenthesis, out _))
        {
            var expression = ParseExpression();

            if (expression == null)
            {
                Console.Error.WriteLine("Expected expression after parenthesis '('.");
                Environment.Exit(1);
            }

            if (TryPeek(TokenType.CloseParenthesis, "')' expected after parenthesis for expression."))
                Consume(); // ')'

            return new NodeTerm { Type = NodeTermType.Parenthesis, Parenthesis = new NodeTermParen { Expression = expression } };
        }

        return null;
    }

    public NodeExpression? ParseExpression(int minimumPrecedence = 0)
    {
        if (!Peek().HasValue)
            return null;

        if (TryPeek(TokenType.Cast, 1))
        {
            var varType = ParseVariableType();

            var castToken = Consume()!.Value;

            if (castToken.Type != TokenType.Cast)
                throw new InvalidOperationException();

            if (!varType.HasValue)
            {
                ErrorInvalid("variable type before 'cast'", castToken.LineNumber);
            }

            var expression = ParseExpression();

            if (expression == null)
            {
                ErrorInvalid("expression after 'cast'", castToken.LineNumber);
            }

            return new NodeExpression { Type = NodeExpressionType.Cast, Cast = new NodeExprCast { CastType = varType!.Value, Expression = expression! } };
        }

        var lhs_optional = ParseTerm()!;

        if (!lhs_optional.HasValue)
            return null;

        var lhsExpr = new NodeExpression { Type = NodeExpressionType.Term, Term = lhs_optional.Value };

        while (true)
        {
            Token? currentToken = Peek();

            if (!currentToken.HasValue)
                break;

            int? precedence = GTokenization.GetBinaryPrecedence(currentToken.Value);

            if (!precedence.HasValue || precedence.Value < minimumPrecedence)
                break;

            var opr = Consume()!.Value;

            int nextMinimumPrecedence = precedence.Value + 1;

            var rhsExpr = ParseExpression(nextMinimumPrecedence);

            if (rhsExpr is null)
            {
                Console.Error.WriteLine("Unable to parse right hand side expression.");
                Environment.Exit(1);
            }

            var binExpr = new NodeBinaryExpression
            {
                Left = lhsExpr,
                Right = rhsExpr
            };

            switch (opr.Type)
            {
                case TokenType.Plus:
                    binExpr.Type = NodeBinaryExpressionType.Add;
                    break;
                case TokenType.Minus:
                    binExpr.Type = NodeBinaryExpressionType.Subtract;
                    break;
                case TokenType.Star:
                    binExpr.Type = NodeBinaryExpressionType.Multiply;
                    break;
                case TokenType.Slash:
                    binExpr.Type = NodeBinaryExpressionType.Divide;
                    break;
                default:
                    throw new InvalidProgramException("Unreachable state reached in ParseExpression().");
            }

            lhsExpr = new NodeExpression { Type = NodeExpressionType.BinaryExpression, BinaryExpression = binExpr };
        }

        return lhsExpr;
    }

    public NodeStatement? ParseStatement()
    {
        if (TryConsume(TokenType.Exit, out var exitToken)) // exit
        {
            NodeStatement? exitStatement = null;

            if (TryConsume(TokenType.Semicolon, out _))
            {
                exitStatement = new NodeStatement
                {
                    Type = NodeStatementType.Exit,
                    Exit =
                    new NodeStmtExit
                    {
                        ReturnCodeExpression =
                        new NodeExpression
                        {
                            Type = NodeExpressionType.Term,
                            Term =
                            new NodeTerm
                            {
                                Type = NodeTermType.Integer,
                                Integer =
                                new NodeTermInteger
                                {
                                    Int_Lit =
                                    new Token
                                    {
                                        Type = TokenType.Int_Lit,
                                        Value = "0",
                                        LineNumber = exitToken!.Value.LineNumber
                                    }
                                }
                            }
                        }
                    }
                };
                return exitStatement;
            }

            var nodeExpr = ParseExpression(); // return code

            if (nodeExpr != null)
            {
                exitStatement = new NodeStatement { Type = NodeStatementType.Exit, Exit = new NodeStmtExit { ReturnCodeExpression = nodeExpr } };
            }
            else
            {
                ErrorInvalid("expression after 'exit'", exitToken!.Value.LineNumber);
            }

            if (TryPeek(TokenType.Semicolon, token => ErrorExpected("';' after 'exit'", token.LineNumber)))
            {
                Consume(); // ";"
            }

            return exitStatement;
        }
        else if (TryPeek(TokenType.Identifier) && TryPeek(TokenType.VariableHint, 1)) // variable
        {
            var identifierToken = Consume()!.Value; // Identifier
            Consume(); // ":"

            var variableType = ParseVariableType();

            if (!variableType.HasValue)
            {
                ErrorInvalid("variable type after variable hint", identifierToken.LineNumber);
            }

            TryPeek(TokenType.Equals, token => ErrorExpected("'=' expected after variable type", token.LineNumber));

            Consume(); // Equals

            var expression = ParseExpression();

            if (expression == null)
            {
                ErrorExpected("expression after '=' of variable statement", identifierToken.LineNumber);
            }

            var statement = new NodeStatement { Type = NodeStatementType.Variable, Variable = new NodeStmtVar { Identifier = identifierToken, Type = variableType!.Value, ValueExpression = expression! } };

            if (TryPeek(TokenType.Semicolon, _ => ErrorExpected("';' after variable statement", identifierToken.LineNumber)))
            {
                Consume(); // ";"
            }

            return statement;
        }
        else if (TryPeek(TokenType.Identifier) && TryPeek(TokenType.Equals, 1))
        {
            var identifierToken = Consume()!.Value; // Identifier

            if (Consume()!.Value.Type != TokenType.Equals)
                throw new InvalidProgramException();

            var expression = ParseExpression();

            if (expression == null)
            {
                ErrorExpected("expression after '=' of variable assignment", identifierToken.LineNumber);
            }

            var statement = new NodeStatement { Type = NodeStatementType.Assign, Assign = new NodeStmtAssign { Identifier = identifierToken, ValueExpression = expression! } };

            if (TryPeek(TokenType.Semicolon, token => ErrorExpected("';' after variable statement", token.LineNumber)))
            {
                Consume(); // ";"
            }

            return statement;
        }
        else if (TryPeek(TokenType.OpenCurlyBraces))
        {
            var scope = ParseScope()!;

            if (!scope.HasValue)
                throw new InvalidProgramException("ParseScope() returned null instead of Environment.Exit.");

            return new NodeStatement { Type = NodeStatementType.Scope, Scope = scope.Value };
        }
        else if (TryConsume(TokenType.If, out var token))
        {
            var nodeStmtIf = ParseIfStatement(token!.Value.LineNumber);

            if (!nodeStmtIf.HasValue)
            {
                ErrorInvalid("if statement", token!.Value.LineNumber);
            }

            return new NodeStatement { Type = NodeStatementType.If, If = nodeStmtIf!.Value };
        }

        return null;
    }

    private NodeStmtIf? ParseIfStatement(int lineNumber)
    {
        var nodeStmtIf = new NodeStmtIf
        {
            Elifs = [],
        };

        var expression = ParseExpression();

        if (expression == null)
        {
            ErrorInvalid("expression after 'if'", lineNumber);
        }

        var scope = ParseScope();

        if (!scope.HasValue)
        {
            ErrorInvalid("scope after 'if'", lineNumber);
        }

        nodeStmtIf.This.Expression = expression!;
        nodeStmtIf.This.Scope = scope!.Value;

        while (TryConsume(TokenType.Elif, out var token))
        {
            var elifStmt = new NodeStmtIfSingle();

            var elifExpression = ParseExpression();

            if (elifExpression == null)
            {
                ErrorInvalid("expression after 'elif'", token!.Value.LineNumber);
            }

            var elifScope = ParseScope();

            if (!elifScope.HasValue)
            {
                ErrorInvalid("scope after 'elif'", token!.Value.LineNumber);
            }

            elifStmt.Expression = elifExpression!;
            elifStmt.Scope = elifScope!.Value;

            nodeStmtIf.Elifs.Add(elifStmt);
        }

        if (TryConsume(TokenType.Else, out _))
        {
            TryPeek(TokenType.OpenCurlyBraces, token => ErrorExpected("'{' after 'else'", token.LineNumber));

            var elseScope = ParseScope();

            if (!elseScope.HasValue)
            {
                throw new InvalidProgramException("ParseScope() for else returned null when it should've Environment.Exit.");
            }

            nodeStmtIf.Else = elseScope!.Value;
        }

        return nodeStmtIf;
    }

    private NodeScope? ParseScope()
    {
        if (!TryConsume(TokenType.OpenCurlyBraces, out _))
            return null;

        var scope = new NodeScope
        {
            Statements = [],
        };

        while (true)
        {
            var statement = ParseStatement()!;

            if (!statement.HasValue)
            {
                Console.Error.WriteLine("Parse Error: Failure parsing statement in scope.");
                Environment.Exit(1);
            }

            scope.Statements.Add(statement.Value);

            if (TryConsume(TokenType.CloseCurlyBraces, out _))
                break;
        }

        return scope;
    }

    public NodeProgram ParseProgram()
    {
        NodeProgram nodeProgram = new NodeProgram();

        while (Peek().HasValue)
        {
            NodeStatement? statement = ParseStatement();
            if (statement != null)
            {
                nodeProgram.Statements.Add(statement.Value);
            }
            else
            {
                ErrorInvalid("statement", Peek()!.Value.LineNumber);
            }
        }

        return nodeProgram;
    }

    private VariableType? ParseVariableType()
    {
        if (!Peek().HasValue)
            return null;

        var token = Consume()!.Value;

        switch (token.Type)
        {
            case TokenType.UnsignedInteger64:
                return VariableType.UnsignedInteger64;
            case TokenType.SignedInteger64:
                return VariableType.SignedInteger64;
            case TokenType.UnsignedInteger16:
                return VariableType.UnsignedInteger16;
            case TokenType.SignedInteger16:
                return VariableType.SignedInteger16;
            case TokenType.SignedInteger32:
                return VariableType.SignedInteger32;
            case TokenType.UnsignedInteger32:
                return VariableType.UnsignedInteger32;
            case TokenType.Byte:
                return VariableType.Byte;
        }

        return null;
    }

    private void ErrorExpected(string message, int line)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Parse Error: Expected {message} on line {line}.");
        Environment.Exit(1);
    }

    private void ErrorInvalid(string message, int line)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Parse Error: Invalid {message} on line {line}.");
        Environment.Exit(1);
    }

    #region Consume & Peek
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token? Consume()
    {
        return tokens[position++];
    }

    private Token? Peek(int offset = 0)
    {
        if (position + offset >= tokens.Count)
        {
            return null;
        }

        return tokens[position + offset];
    }

    public bool TryPeek(TokenType type, string errorMessage, int offset = 0)
    {
        var token = Peek(offset);

        if (token == null)
            return false;

        if (token.Value.Type != type)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
            return false;
        }

        return true;
    }

    public bool TryPeek(TokenType type, Action<Token> error, int offset = 0)
    {
        var token = Peek(offset);

        if (token == null)
        {
            Console.Error.WriteLine("Parse Error: Unknown error at the end of the file.");
            Environment.Exit(1);
        }

        if (token.Value.Type != type)
        {
            error?.Invoke(token.Value);
            return false;
        }

        return true;
    }

    public bool TryPeek(TokenType type, int offset = 0)
    {
        var token = Peek(offset);

        if (token == null)
            return false;

        return token.Value.Type == type;
    }

    public bool TryConsume(TokenType type, out Token? token)
    {
        token = Peek()!;

        if (!token.HasValue)
            return false;

        if (token.Value.Type != type)
            return false;

        Consume();
        return true;
    }
    #endregion
}
