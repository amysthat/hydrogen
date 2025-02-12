﻿using Hydrogen.Generation.Variables;

namespace Hydrogen.Tokenization;

internal static class Keywords
{
    public static Token Handle(string keyword, int lineCount)
    {
        if (keyword == "exit") return Token(TokenType.Exit);
        else if (keyword == "if") return Token(TokenType.If);
        else if (keyword == "elif") return Token(TokenType.Elif);
        else if (keyword == "else") return Token(TokenType.Else);
        else if (keyword == "cast") return Token(TokenType.Cast);
        else if (keyword == "write") return Token(TokenType.Write);
        else if (keyword == "true") return Token(TokenType.Bool, true.ToString());
        else if (keyword == "false") return Token(TokenType.Bool, false.ToString());
        else if (keyword == "not") return Token(TokenType.Not);
        else
        {
            foreach (var property in typeof(VariableTypes).GetProperties())
            {
                VariableType varType = (VariableType)property.GetValue(null)!;

                if (varType.Keyword == keyword)
                {
                    return Token(TokenType.VariableType, varType.GetType().FullName!);
                }
            }

            return Token(TokenType.Identifier, keyword);
        }

        Token Token(TokenType type, string value = "") => new() { Type = type, Value = value, LineNumber = lineCount };
    }
}
