namespace Hydrogen.Tokenization;

public static class GlobalTokenization // TODO: Move this to parsing
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