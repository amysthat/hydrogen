namespace Hydrogen.Tokenization;

public class TokenizationException(int line, string message) : CompilerException($"Tokenization Error: Line {line}: {message}")
{
}