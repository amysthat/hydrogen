using System.Runtime.CompilerServices;

namespace Hydrogen.Tokenization;

public class Tokenizer(string source)
{
    private readonly List<Token> tokens = [];
    private int position = 0;
    private string buf = string.Empty;
    private int lineCount = 1;

    public List<Token> Tokenize()
    {
        while (Peek().HasValue)
        {
            char peekedChar = Peek()!.Value;

            if (peekedChar == '\n')
                lineCount++;

            if (char.IsWhiteSpace(peekedChar))
            {
                Consume();
                continue;
            }

            bool isKeyword = char.IsLetter(peekedChar) || peekedChar == '_';
            if (isKeyword)
            {
                buf += Consume();

                while (Peek().HasValue && (char.IsLetterOrDigit(Peek()!.Value) || Peek().HasValue && Peek()!.Value == '_'))
                {
                    buf += Consume();
                }

                tokens.Add(Keywords.Handle(buf, lineCount));

                buf = string.Empty;
                continue;
            }

            var isNegativeInteger = peekedChar == '-' && tokens[^1].Type != TokenType.Int_Lit && Peek(1)!.HasValue && char.IsDigit(Peek(1)!.Value);
            if (char.IsDigit(peekedChar) || isNegativeInteger)
            {
                buf += Consume();

                while (Peek().HasValue && char.IsDigit(Peek()!.Value))
                {
                    buf += Consume();
                }

                tokens.Add(new Token { Type = TokenType.Int_Lit, LineNumber = lineCount, Value = buf });
                buf = string.Empty;
                continue;
            }

            if (peekedChar == '\'')
            {
                Consume(); // '
                buf = Consume()!.Value.ToString(); // char
                var endChar = Consume();

                if (!endChar.HasValue || endChar.Value != '\'')
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Tokenization Error: Only 1 character allowed for char on line {lineCount}.");
                    Environment.Exit(1);
                }

                tokens.Add(new Token { Type = TokenType.Char, Value = buf, LineNumber = lineCount });
                buf = string.Empty;
                continue;
            }

            if (peekedChar == '"')
            {
                Consume(); // "

                while (true)
                {
                    if (Peek().HasValue && Peek()!.Value == '\n')
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Tokenization Error: Multiple lined string found on line {lineCount}.\nPlease keep the string in a single line.\nUse '\\n' if you want to store multiple lines");
                        Environment.Exit(1);
                    }

                    if (!Peek().HasValue || !Peek(1).HasValue)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Tokenization Error: Reached end of string prematurely on line {lineCount}.");
                        Environment.Exit(1);
                    }

                    var value = Consume()!.Value;

                    if (value == '"')
                    {
                        break;
                    }

                    if (value == '\\')
                    {
                        if (Peek().HasValue && Peek()!.Value == 'n') // Newline \n
                        {
                            Consume();
                            value = '\n';
                        }
                    }

                    buf += value;
                }

                tokens.Add(new Token { Type = TokenType.String, Value = buf, LineNumber = lineCount });
                buf = string.Empty;
                continue;
            }

            if (HandleComments())
                continue;

            // Handle single character token
            Consume();
            tokens.Add(SingleCharTokenizer.Handle(peekedChar, lineCount));
            continue;
        }

        return tokens;
    }

    /// <returns>If comment handling was successful, and the while loop should move on with "continue".</returns>
    private bool HandleComments()
    {
        if (Peek()!.Value == '/' && Peek(1).HasValue && Peek(1)!.Value == '/')
        {
            while (Peek().HasValue && Consume() != '\n') { }
            lineCount++;
            return true;
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
            return true;
        }

        return false;
    }

    #region Consume & Peek
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
    #endregion
}