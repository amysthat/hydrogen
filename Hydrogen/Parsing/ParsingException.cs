namespace Hydrogen.Parsing;

public class ParsingException(int line, string message) : CompilerException($"Parse Error: Line {line}: {message}")
{
}