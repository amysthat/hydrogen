namespace Hydrogen.Generation;

public class VariableNotFoundException(string variable) : Exception(variable + " was not found");
