namespace Hydrogen.Generation.Variables;

public class VariableNotFoundException(string variable) : Exception(variable + " was not found");
