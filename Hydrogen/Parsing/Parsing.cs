using System.Reflection;
using System.Runtime.CompilerServices;
using Hydrogen.Generation.Variables;
using Hydrogen.Tokenization;

namespace Hydrogen.Parsing;

public struct NodeScope : NodeStatement
{
    public int LineNumber { get; set; }

    public List<NodeStatement> Statements;
}

public struct NodeProgram
{
    public List<NodeStatement> Statements;

    public NodeProgram() => Statements = [];
}

public interface Node
{
    public int LineNumber { get; set; }
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
                LineNumber = intLitToken!.Value.LineNumber,
                IntegerLiteral = intLitToken!.Value,
            };
        }
        else if (TryConsume(TokenType.Identifier, out var identToken))
            return new NodeTermIdentifier { LineNumber = identToken!.Value.LineNumber, Identifier = identToken!.Value };
        else if (TryConsume(TokenType.VarToPtr, out _))
        {
            TryPeek(TokenType.Identifier, identToken => throw new ParsingException(identToken.LineNumber, "Identifier after pointer expected."));
            identToken = Consume();

            return new NodeTermPointerAddress { LineNumber = identToken!.Value.LineNumber, Identifier = new NodeTermIdentifier { LineNumber = identToken!.Value.LineNumber, Identifier = identToken!.Value } };
        }
        else if (TryPeek(TokenType.Star) && TryPeek(TokenType.Identifier, 1))
        {
            Consume(); // *
            var identifier = Consume();

            return new NodeTermPointerValue { LineNumber = identifier!.Value.LineNumber, Identifier = new NodeTermIdentifier { LineNumber = identToken!.Value.LineNumber, Identifier = identifier!.Value } };
        }
        // else if (TryConsume(TokenType.OpenParenthesis, out var openParen))
        // {
        //     var expression = ParseExpression();

        //     if (expression == null)
        //     {
        //         throw new ParsingException(openParen!.Value.LineNumber, "Expected expression after parenthesis '('.");
        //     }

        //     if (TryPeek(TokenType.CloseParenthesis, errToken => throw new ParsingException(errToken.LineNumber, "Expected ')' after parenthesis for expression.")))
        //         Consume();

        //     return new NodeTermParen { LineNumber = openParen!.Value.LineNumber, Expression = expression };
        // }
        else if (TryConsume(TokenType.Char, out var charToken))
        {
            return new NodeTermChar { LineNumber = charToken!.Value.LineNumber, Char = charToken!.Value };
        }
        else if (TryConsume(TokenType.String, out var stringToken))
        {
            return new NodeTermString { LineNumber = stringToken!.Value.LineNumber, String = stringToken!.Value };
        }
        else if (TryConsume(TokenType.Bool, out var boolToken))
        {
            bool value = boolToken!.Value.Value == true.ToString();

            return new NodeTermBool { LineNumber = boolToken!.Value.LineNumber, Value = value };
        }

        return null;
    }

    public NodeExpression? ParseExpression()
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

            var castTerm = ParseTerm() ?? throw new ParsingException(castToken.LineNumber, "Invalid term after 'cast'.");

            return new NodeExprCast { LineNumber = castToken.LineNumber, CastType = varType!, Term = castTerm };
        }

        var logicalNotExpression = ParseLogicalNotExpression();

        if (logicalNotExpression is not null)
            return logicalNotExpression;

        var sharedTerm = ParseTerm();

        if (sharedTerm is BinaryExprSupporter)
        {
            var binExpr = ParseBinaryExpression(preparsedTerm: sharedTerm);

            if (binExpr is not null)
                return binExpr;
        } // even if the term is a supports binary expressions, there was just no binary expressions, only the term itself

        return sharedTerm;
    }

    public NodeBinExpr? ParseBinaryExpression(NodeTerm preparsedTerm = null!, int minimumPrecedence = 0)
    {
        var term = preparsedTerm ?? ParseTerm();

        if (term is not BinaryExprSupporter lhsExpr)
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

            var rhsExpr = ParseBinaryExpression(minimumPrecedence: nextMinimumPrecedence) ?? throw new ParsingException(
                currentToken!.Value.LineNumber,
                "Unable to parse right hand side expression.");

            var binExpr = new NodeBinExpr
            {
                LineNumber = currentToken!.Value.LineNumber,
                Left = lhsExpr,
                Right = rhsExpr,
                Type = opr.Type switch
                {
                    TokenType.Plus => NodeBinExprType.Add,
                    TokenType.Minus => NodeBinExprType.Subtract,
                    TokenType.Star => NodeBinExprType.Multiply,
                    TokenType.Slash => NodeBinExprType.Divide,
                    _ => throw new InvalidProgramException($"Unreachable state reached in {nameof(ParseBinaryExpression)}()."),
                }
            };

            lhsExpr = binExpr;
        }

        if (lhsExpr is not NodeBinExpr)
            return null; // turns out this is not a full-on bin expr, but just a term that supports binary expressions

        return (NodeBinExpr)lhsExpr;
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

    public NodeLogicNotExpr? ParseLogicalNotExpression()
    {
        if (Peek()!.Value.Type != TokenType.Not) // Peek() to avoid consuming unhandled token
            return null;

        var notToken = Consume();

        var innerExpression = ParseExpression();

        if (innerExpression is not LogicalExprSupporter logicalInnerExpression)
            throw new ParsingException(notToken!.Value.LineNumber, "Encountered a non-logical expression after 'not' token.");

        return new NodeLogicNotExpr
        {
            LineNumber = notToken!.Value.LineNumber,
            InnerExpression = logicalInnerExpression,
        };
    }

    public NodeLogicalExpr? ParseLogicalExpression(NodeTerm preparsedTerm = null!)
    {
        throw new NotImplementedException();
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

        var isPointerValueAssignment = TryPeek(TokenType.Star) && TryPeek(TokenType.Identifier, 1) && TryPeek(TokenType.Equals, 2);
        var isStandartAssignment = TryPeek(TokenType.Identifier) && TryPeek(TokenType.Equals, 1);
        if (isStandartAssignment || isPointerValueAssignment) // variable assignment
        {
            return NodeStatements.ParseVariableAssignment(this, isPointerValueAssignment);
        }

        if (TryPeek(TokenType.OpenCurlyBraces))
        {
            var scope = ParseScope()!;

            if (!scope.HasValue)
                throw new InvalidProgramException(); // should have thrown an exception already

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
        if (!TryConsume(TokenType.OpenCurlyBraces, out var openCurlyBraces))
            return null;

        var scope = new NodeScope
        {
            LineNumber = openCurlyBraces!.Value.LineNumber,
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
