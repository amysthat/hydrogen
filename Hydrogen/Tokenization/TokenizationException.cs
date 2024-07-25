namespace Hydrogen;

public class TokenizationException(int line, string message) : CompilerDerivedException($"Tokenization Error: Line {line}: {message}")
{
}