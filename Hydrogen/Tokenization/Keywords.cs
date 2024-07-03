namespace Hydrogen.Tokenization;

internal static class Keywords
{
    public static Token Handle(string keyword, int lineCount)
    {
        if (keyword == "exit")      return Token(TokenType.Exit);
        else if (keyword == "if")   return Token(TokenType.If);
        else if (keyword == "elif") return Token(TokenType.Elif);
        else if (keyword == "else") return Token(TokenType.Else);
        else if (keyword == "cast") return Token(TokenType.Cast);
        else if (keyword == "i64")  return Token(TokenType.SignedInteger64);
        else if (keyword == "u64")  return Token(TokenType.UnsignedInteger64);
        else if (keyword == "i16")  return Token(TokenType.SignedInteger16);
        else if (keyword == "u16")  return Token(TokenType.UnsignedInteger16);
        else if (keyword == "i32")  return Token(TokenType.SignedInteger32);
        else if (keyword == "u32")  return Token(TokenType.UnsignedInteger32);
        else if (keyword == "byte") return Token(TokenType.Byte);
        else                        return Token(TokenType.Identifier, keyword);

        Token Token(TokenType type, string value = "") => new() { Type = type, Value = value, LineNumber = lineCount };
    }
}