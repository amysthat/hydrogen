namespace Hydrogen.Tokenization;

public struct Token
{
    public TokenType Type;
    public string? Value;

    public int LineNumber;
}
