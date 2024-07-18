﻿#pragma warning disable CS0618 // Tür veya üye artık kullanılmıyor
using Hydrogen.Generation.Variables;

namespace Hydrogen.Generation;

public class Scope
{
    [Obsolete("Don't use this, use 128 bytes.")]
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
#pragma warning restore CS0618 // Tür veya üye artık kullanılmıyor