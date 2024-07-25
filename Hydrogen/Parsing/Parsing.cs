using System.Reflection;
using System.Runtime.CompilerServices;
using Hydrogen.Generation;
using Hydrogen.Generation.Variables;
using Hydrogen.Tokenization;
using Pointer = Hydrogen.Generation.Variables.Pointer;

namespace Hydrogen.Parsing;

public struct NodeScope : NodeStatement
{
    public List<NodeStatement> Statements;
}

public struct NodeProgram
{
    public List<NodeStatement> Statements;

    public NodeProgram() => Statements = [];
}

public partial class Parser(List<Token> tokens)
{
    private readonly List<Token> tokens = tokens;
    private int position = 0;

    public NodeTerm? ParseTerm()
    {
        if (!Peek().HasValue)
            return null;

        if (TryConsume(TokenType.Int_Lit, out var intLitToken))
        {
            return new NodeTermInteger
            {
                Int_Lit = intLitToken!.Value,
            };
        }
        else if (TryConsume(TokenType.Identifier, out var identToken))
            return new NodeTermIdentifier { Identifier = identToken!.Value };
        else if (TryConsume(TokenType.VarToPtr, out _))
        {
            TryPeek(TokenType.Identifier, identToken => throw new ParsingException(identToken.LineNumber, "Identifier after pointer expected."));
            identToken = Consume();

            return new NodeTermPointerAddress { Identifier = new NodeTermIdentifier { Identifier = identToken!.Value } };
        }
        else if (TryPeek(TokenType.Star) && TryPeek(TokenType.Identifier, 1))
        {
            Consume(); // *
            var identifier = Consume();

            return new NodeTermPointerValue { Identifier = new NodeTermIdentifier { Identifier = identifier!.Value } };
        }
        else if (TryConsume(TokenType.OpenParenthesis, out _))
        {
            var expression = ParseExpression();

            if (expression == null)
            {
                Console.Error.WriteLine("Expected expression after parenthesis '('.");
                Environment.Exit(1);
            }

            if (TryPeek(TokenType.CloseParenthesis, errToken => throw new ParsingException(errToken.LineNumber, "Expected ')' after parenthesis for expression.")))
                Consume();

            return new NodeTermParen { Expression = expression };
        }
        else if (TryConsume(TokenType.Char, out var charToken))
        {
            return new NodeTermChar { Char = charToken!.Value };
        }
        else if (TryConsume(TokenType.String, out var stringToken))
        {
            return new NodeTermString { String = stringToken!.Value };
        }

        return null;
    }

    public NodeExpression? ParseExpression(int minimumPrecedence = 0)
    {
        if (!Peek().HasValue)
            return null;

        if (TryPeek(TokenType.Cast, 1) || TryPeek(TokenType.Cast, 2))
        {
            var varType = ParseVariableType();

            var castToken = Consume()!.Value;

            if (castToken.Type != TokenType.Cast)
                throw new InvalidOperationException();

            if (varType == null)
            {
                throw new ParsingException(castToken.LineNumber, "Invalid variable type before 'cast'.");
            }

            var expression = ParseExpression();

            if (expression == null)
            {
                throw new ParsingException(castToken.LineNumber, "Invalid expression after 'cast'.");
            }

            return new NodeExprCast { CastType = varType!, Expression = expression! };
        }

        if (ParseTerm() is not NodeExpression lhsExpr)
            return null;

        while (true)
        {
            Token? currentToken = Peek();

            if (!currentToken.HasValue)
                break;

            int? precedence = GetBinaryPrecedence(currentToken.Value);

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

            var binExpr = new NodeBinExpr
            {
                Left = lhsExpr,
                Right = rhsExpr,
                Type = opr.Type switch
                {
                    TokenType.Plus => NodeBinExprType.Add,
                    TokenType.Minus => NodeBinExprType.Subtract,
                    TokenType.Star => NodeBinExprType.Multiply,
                    TokenType.Slash => NodeBinExprType.Divide,
                    _ => throw new InvalidProgramException("Unreachable state reached in ParseExpression()."),
                }
            };

            lhsExpr = binExpr;
        }

        return lhsExpr;
    }

    private static int? GetBinaryPrecedence(Token token)
    {
        return token.Type switch
        {
            TokenType.Plus or TokenType.Minus => 0,
            TokenType.Star or TokenType.Slash => 1,
            _ => null,
        };
    }

    public NodeStatement? ParseStatement()
    {
        if (TryPeek(TokenType.Exit))
        {
            return NodeStatements.ParseExit(this);
        }

        if (TryPeek(TokenType.Write))
        {
            return NodeStatements.ParseWrite(this);
        }

        if (TryPeek(TokenType.Identifier) && TryPeek(TokenType.VariableHint, 1)) // variable statement
        {
            return NodeStatements.ParseVariableStatement(this);
        }

        if (TryPeek(TokenType.Identifier) && TryPeek(TokenType.Equals, 1)) // variable assignment
        {
            return NodeStatements.ParseVariableAssignment(this);
        }

        if (TryPeek(TokenType.OpenCurlyBraces))
        {
            var scope = ParseScope()!;

            if (!scope.HasValue)
                throw new InvalidProgramException("ParseScope() returned null instead of Environment.Exit.");

            return scope.Value;
        }

        if (TryPeek(TokenType.If))
        {
            var nodeStmtIf = NodeStatements.ParseIfStatement(this);

            return nodeStmtIf;
        }

        return null;
    }

    public NodeScope? ParseScope()
    {
        if (!TryConsume(TokenType.OpenCurlyBraces, out _))
            return null;

        var scope = new NodeScope
        {
            Statements = [],
        };

        while (true)
        {
            if (TryConsume(TokenType.CloseCurlyBraces, out _))
                break;

            var statement = ParseStatement()!;

            if (statement is null)
            {
                throw new ParsingException(Peek()!.Value.LineNumber, "Failure parsing statement in scope.");
            }

            scope.Statements.Add(statement);
        }

        return scope;
    }

    public NodeProgram ParseProgram()
    {
        NodeProgram nodeProgram = new();

        while (Peek().HasValue)
        {
            NodeStatement? statement = ParseStatement();
            if (statement != null)
            {
                nodeProgram.Statements.Add(statement);
            }
            else
            {
                throw new ParsingException(Peek()!.Value.LineNumber, "Unknown/invalid statement.");
            }
        }

        return nodeProgram;
    }

    public VariableType? ParseVariableType()
    {
        if (!Peek().HasValue)
            return null;

        var token = Consume()!.Value;

        if (token.Type != TokenType.VariableType)
        {
            throw new ParsingException(token.LineNumber, "Invalid token type for variable type: " + token.Type);
        }

        string variableTypeStr = token.Value!;
        var variableType = (VariableType)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType(variableTypeStr)!)!;

        bool isPointer = Peek()?.Type == TokenType.Star;

        if (isPointer)
        {
            Consume();

            variableType = new Pointer { RepresentingType = variableType };
        }

        return variableType;
    }

    #region Consume & Peek
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Token? Consume()
    {
        return tokens[position++];
    }

    public Token? Peek(int offset = 0)
    {
        if (position + offset >= tokens.Count)
        {
            return null;
        }

        return tokens[position + offset];
    }

    public bool TryPeek(TokenType type, Action<Token> error, int offset = 0)
    {
        var token = Peek(offset);

        if (token == null)
        {
            throw new ParsingException(-1, "Reached end of file prematurely.");
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
