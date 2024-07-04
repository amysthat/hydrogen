namespace Hydrogen.Generation;

public class Scope
{
    public long CurrentStackSize;
    public readonly Map<string, Variable> variables = new();

    public required Scope Parent;

    public long DefineVariable(string variableName, VariableType type)
    {
        var variablePosition = CurrentStackSize;

        var variable = new Variable
        {
            Type = type,
            BaseStackDifference = variablePosition,
            Size = Variable.GetSize(type),
            Owner = this,
        };

        variables.Add(variableName, variable);

        CurrentStackSize += variable.Size;

        return variablePosition;
    }
}