using Hydrogen.Generation.Variables.Integers;
using Hydrogen.Parsing;

namespace Hydrogen.Generation.Variables;

public static class VariableTypes
{
    // Integers
    public static SignedInteger64 SignedInteger64 => new();
    public static UnsignedInteger64 UnsignedInteger64 => new();
    public static SignedInteger32 SignedInteger32 => new();
    public static UnsignedInteger32 UnsignedInteger32 => new();
    public static SignedInteger16 SignedInteger16 => new();
    public static UnsignedInteger16 UnsignedInteger16 => new();
    public static Byte Byte => new();

    // Other
    public static Char Char => new();
    public static String String => new();
}

public abstract class VariableType
{
    public abstract string Keyword { get; }
    public abstract long Size { get; }

    public abstract bool Cast(Generator generator, VariableType targetType, int lineNumber);

    public static bool operator ==(VariableType? x, VariableType? y) => x is not null && y is not null && x.Keyword == y.Keyword;
    public static bool operator !=(VariableType? x, VariableType? y) => x is not null && y is not null && x.Keyword != y.Keyword;

    public override bool Equals(object? obj)
    {
        if (obj is not VariableType variableType)
        {
            return false;
        }

        return variableType.Keyword == Keyword;
    }

    public override int GetHashCode() => Keyword.GetHashCode();

    public override string ToString() => Keyword;
}

public abstract class IntegerType : VariableType
{
    public abstract IntegerSignedness Signedness { get; }
    public abstract string AsmARegister { get; }
    public abstract string AsmBRegister { get; }
    public abstract string AsmPointerSize { get; }

    public override bool Cast(Generator generator, VariableType targetType, int lineNumber)
    {
        if (targetType is not IntegerType integerType)
            return false;

        IntegerCast(generator, integerType, lineNumber);
        return true;
    }

    public abstract void IntegerCast(Generator generator, IntegerType integerType, int lineNumber);
}

public enum IntegerSignedness
{
    SignedInteger,
    UnsignedInteger,
}

public struct Variable
{
    public required long RelativePosition;
    public required long Size;

    public required string Name;
    public required VariableType Type;

    public required Scope Owner;

    public override string ToString() => Type.ToString() + " " + Name;

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