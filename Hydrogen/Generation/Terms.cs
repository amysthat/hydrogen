using Hydrogen.Generation.Variables;
using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public static class Terms
{
    public static VariableType Integer(Generator generator, NodeTermInteger termInteger, VariableType suggestionType)
    {
        IntegerType integerType = suggestionType is IntegerType _integerType ? _integerType : VariableTypes.SignedInteger64;

        if (Variable.IsUnsignedInteger(integerType) && termInteger.Int_Lit.Value!.StartsWith('-'))
        {
            Console.Error.WriteLine("Negative value given for unsigned integer.");
            Environment.Exit(1);
        }

        generator.Push(Variable.MoveIntegerToRegister(generator, termInteger, integerType));
        return integerType;
    }

    public static VariableType Identifier(Generator generator, NodeTermIdentifier termIdentifier)
    {
        string identifier = termIdentifier.Identifier.Value!;

        var variable = generator.GetVariable(identifier);

        if (!variable.HasValue)
        {
            Console.Error.WriteLine($"Variable '{identifier}' has not been declared.");
            Environment.Exit(1);
        }

        if (variable.Value.Type is not IntegerType integerType)
        {
            throw new InvalidOperationException();
        }

        var variablePosition = generator.GetRelativeVariablePosition(identifier);
        var assemblyString = Generator.CastRelativeVariablePositionToAssembly(variablePosition);

        var aRegister = integerType.AsmARegister;
        var asmPointerSize = integerType.AsmPointerSize;

        generator.output += $"    mov {aRegister}, {asmPointerSize} [{assemblyString}] ; {variable!.Value.Type.Keyword} {identifier} variable\n";
        generator.Push("rax");

        return variable!.Value.Type;
    }
}