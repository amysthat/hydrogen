namespace Hydrogen.Generation;

public class CompilationException(string message) : CompilerException($"Compilation Error: {message}") { }