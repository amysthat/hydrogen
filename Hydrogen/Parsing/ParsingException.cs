namespace Hydrogen.Parsing;

public class ParsingException(int line, string message) : CompilerDerivedException($"Parse Error: Line {line}: {message}")
{
}