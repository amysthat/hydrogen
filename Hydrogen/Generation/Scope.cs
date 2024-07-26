using Hydrogen.Generation.Variables;

namespace Hydrogen.Generation;

public class Scope
{
    public long CurrentStackSize;
    public readonly Map<string, Variable> variables = new();

    public required Scope Parent;

    public Variable DefineVariable(string variableName, VariableType type)
    {
        var variablePosition = CurrentStackSize;

        var variable = new Variable
        {
            Type = type,
            RelativePosition = variablePosition,
            Size = Variable.GetSize(type),
            Owner = this,
            Name = variableName,
        };

        variables.Add(variableName, variable);

        CurrentStackSize += variable.Size;

        return variable;
    }
}