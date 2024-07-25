namespace Hydrogen.Generation;

public class CompilationException : CompilerException
{
    public CompilationException(int line, string message) : base($"Compilation Error: Line {line}: {message}")
    {
        if (line == 0)
        {
            throw new InvalidDataException($"Line number for {nameof(CompilationException)} is zero, which is not possible. This is a bug. Message given: {message}");
        }
    }
}