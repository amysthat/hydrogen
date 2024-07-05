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
}

public abstract class VariableType
{
    public abstract string Keyword { get; }
    public abstract long Size { get; }

    public abstract bool Cast(Generator generator, VariableType targetType);

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
}

public abstract class IntegerType : VariableType
{
    public abstract IntegerSignedness Signedness { get; }
    public abstract string AsmARegister { get; }
    public abstract string AsmBRegister { get; }
    public abstract string AsmPointerSize { get; }

    public override bool Cast(Generator generator, VariableType targetType)
    {
        if (targetType is not IntegerType integerType)
            return false;

        IntegerCast(generator, integerType);
        return true;
    }

    public abstract void IntegerCast(Generator generator, IntegerType integerType);
}

public enum IntegerSignedness
{
    SignedInteger,
    UnsignedInteger,
}

public struct Variable
{
    public long BaseStackDifference;
    public long Size;

    public VariableType Type;

    public required Scope Owner;

    public static bool IsSignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.SignedInteger;
    public static bool IsUnsignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.UnsignedInteger;

    public static long GetSize(VariableType variableType) => variableType.Size;

    public static void Cast(Generator generator, VariableType variableType, VariableType targetType)
    {
        bool castSuccessful = variableType.Cast(generator, targetType);

        if (!castSuccessful)
        {
            Console.Error.WriteLine($"Can not cast {variableType.Keyword} to {targetType.Keyword}.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Moves an Integer to the appropriate register.
    /// </summary>
    /// <returns>Used register</returns>
    public static string MoveIntegerToRegister(Generator generator, NodeTermInteger integer, IntegerType type)
    {
        var register = type.AsmARegister;

        if (type is UnsignedInteger64 || type is SignedInteger64)
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for 64 bits\n";
        }
        else if (type is UnsignedInteger32 || type is SignedInteger32) // I hate you assembly
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