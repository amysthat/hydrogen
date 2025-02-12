﻿namespace Hydrogen.Tokenization;

internal static class SingleCharTokenizer
{
    public static Token Handle(char c, int lineCount)
    {
        if (c == '=') return Token(TokenType.Equals);
        else if (c == ':') return Token(TokenType.VariableHint);
        else if (c == '(') return Token(TokenType.OpenParenthesis);
        else if (c == ')') return Token(TokenType.CloseParenthesis);
        else if (c == '{') return Token(TokenType.OpenCurlyBraces);
        else if (c == '}') return Token(TokenType.CloseCurlyBraces);
        else if (c == '+') return Token(TokenType.Plus);
        else if (c == '-') return Token(TokenType.Minus);
        else if (c == '*') return Token(TokenType.Star);
        else if (c == '/') return Token(TokenType.Slash);
        else if (c == ';') return Token(TokenType.Semicolon);
        else if (c == '&') return Token(TokenType.VarToPtr);
        else throw new TokenizationException(lineCount, $"Unknown standalone character: '{c}'");

        Token Token(TokenType type) => new() { Type = type, LineNumber = lineCount };
    }
}
