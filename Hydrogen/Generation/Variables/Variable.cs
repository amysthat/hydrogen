using Hydrogen.Generation.Variables.Integers;
using Hydrogen.Parsing;

namespace Hydrogen.Generation.Variables;

public struct Variable
{
    public required long RelativePosition;
    public required long Size;

    public required string Name;
    public required VariableType Type;

    public required Scope Owner;

    public readonly override string ToString() => Type.ToString() + " " + Name;

    public static bool IsSignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.SignedInteger;
    public static bool IsUnsignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.UnsignedInteger;

    public static long GetSize(VariableType variableType) => variableType.Size;

    public static void Cast(Generator generator, VariableType variableType, VariableType targetType, int lineNumber)
    {
        bool castSuccessful = variableType.Cast(generator, targetType, lineNumber);

        if (!castSuccessful)
        {
            throw new CompilationException(lineNumber, $"Can not cast {variableType.Keyword} to {targetType.Keyword}.");
        }
    }

    /// <summary>
    /// Moves an Integer to the appropriate register.
    /// </summary>
    /// <returns>Used register</returns>
    public static string MoveIntegerToRegister(Generator generator, NodeTermInteger integer, IntegerType type)
    {
        var register = type.AsmARegister;

        if (type is Pointer)
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for {type.Keyword}\n";
        }
        else if (type.Size == 8)
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for 64 bits\n";
        }
        else if (type.Size == 4) // I hate you assembly
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for 32 bits\n";
        }
        else
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for {type.Keyword}\n";
            generator.output += $"    movzx rax, {register}\n";
        }

        return register;
    }
}