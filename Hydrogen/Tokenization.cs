using System.Runtime.CompilerServices;

namespace Hydrogen.Tokenization;

public enum TokenType
{
    Exit,
    If,
    Elif,
    Else,

    Int_Lit,

    Semicolon,

    OpenParenthesis,
    CloseParenthesis,
    OpenCurlyBraces,
    CloseCurlyBraces,

    Identifier,

    VariableHint,

    Equals,
    Plus,
    Star,
    Minus,
    Slash,
}

public struct Token
{
    public TokenType Type;
    public string? Value;

    public int LineNumber;
}

public class Tokenizer
{
    private string source;
    private int position;

    public Tokenizer(string source)
    {
        this.source = source;
        position = 0;
    }

    public List<Token> Tokenize()
    {
        List<Token> tokens = new();
        string buf = string.Empty;
        int lineCount = 1;

        while (Peek().HasValue)
        {
            char peekedChar = Peek()!.Value;

            if (peekedChar == '\n')
                lineCount++;

            if (char.IsLetter(peekedChar))
            {
                buf += Consume();

                while (Peek().HasValue && char.IsLetterOrDigit(Peek()!.Value))
                {
                    buf += Consume();
                }

                if (buf == "exit")
                    tokens.Add(PrepareToken(TokenType.Exit, lineCount));
                else if (buf == "if")
                    tokens.Add(PrepareToken(TokenType.If, lineCount));
                else if (buf == "elif")
                    tokens.Add(PrepareToken(TokenType.Elif, lineCount));
                else if (buf == "else")
                    tokens.Add(PrepareToken(TokenType.Else, lineCount));
                else // Identifier
                    tokens.Add(PrepareToken(TokenType.Identifier, lineCount, buf));

                buf = string.Empty;
                continue;
            }
            else if (char.IsDigit(peekedChar))
            {
                buf += Consume();

                while (Peek().HasValue && char.IsDigit(Peek()!.Value))
                {
                    buf += Consume();
                }

                tokens.Add(PrepareToken(TokenType.Int_Lit, lineCount, buf));
                buf = string.Empty;

                continue;
            }
            else if (char.IsWhiteSpace(peekedChar))
            {
                Consume();
                continue;
            }
            else if (Peek()!.Value == '/' && Peek(1).HasValue && Peek(1)!.Value == '/')
            {
                while (Peek().HasValue && Consume() != '\n') { }
                lineCount++;
                continue;
            }
            else if (Peek()!.Value == '/' && Peek(1).HasValue && Peek(1)!.Value == '*')
            {
                while (true)
                {
                    if (Peek().HasValue && Peek()!.Value == '\n')
                        lineCount++;

                    if (!Peek().HasValue || !Peek(1).HasValue)
                        break;

                    if (Consume()!.Value == '*')
                    {
                        if (Peek()!.Value == '/')
                        {
                            Consume();
                            break;
                        }
                    }
                }
                continue;
            }
            else
            {
                Consume();

                switch (peekedChar)
                {
                    case '=':
                        tokens.Add(PrepareToken(TokenType.Equals, lineCount));
                        continue;
                    case ':':
                        tokens.Add(PrepareToken(TokenType.VariableHint, lineCount));
                        continue;
                    case '(':
                        tokens.Add(PrepareToken(TokenType.OpenParenthesis, lineCount));
                        continue;
                    case ')':
                        tokens.Add(PrepareToken(TokenType.CloseParenthesis, lineCount));
                        continue;
                    case '{':
                        tokens.Add(PrepareToken(TokenType.OpenCurlyBraces, lineCount));
                        continue;
                    case '}':
                        tokens.Add(PrepareToken(TokenType.CloseCurlyBraces, lineCount));
                        continue;
                    case '+':
                        tokens.Add(PrepareToken(TokenType.Plus, lineCount));
                        continue;
                    case '-':
                        tokens.Add(PrepareToken(TokenType.Minus, lineCount));
                        continue;
                    case '*':
                        tokens.Add(PrepareToken(TokenType.Star, lineCount));
                        continue;
                    case '/':
                        tokens.Add(PrepareToken(TokenType.Slash, lineCount));
                        continue;
                    case ';':
                        tokens.Add(PrepareToken(TokenType.Semicolon, lineCount));
                        continue;
                }
            }

            Console.Error.WriteLine($"Invalid keyword or token '{peekedChar}' on line {lineCount}.");
            Environment.Exit(1);
        }

        position = 0;

        return tokens;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token PrepareToken(TokenType type, int lineNumber, string value = "") => new Token { Type = type, Value = value, LineNumber = lineNumber };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char? Consume()
    {
        return source[position++];
    }

    private char? Peek(int offset = 0)
    {
        if (position + offset >= source.Length)
        {
            return null;
        }

        return source[position + offset];
    }
}

public static class GTokenization
{
    public static int? GetBinaryPrecedence(Token token)
    {
        switch (token.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
                return 0;
            case TokenType.Star:
            case TokenType.Slash:
                return 1;
        }

        return null;
    }
}