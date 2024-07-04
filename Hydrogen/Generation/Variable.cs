using Hydrogen.Generation.Variables.Integers;

namespace Hydrogen.Generation;

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

    public abstract bool Cast(VariableType targetType);
}

public abstract class IntegerType : VariableType
{
    public abstract IntegerSignedness Signedness { get; }
    public abstract string AsmARegister { get; }
    public abstract string AsmBRegister { get; }
    public abstract string AsmPointerSize { get; }

    public override bool Cast(VariableType targetType)
    {
        if (targetType is not IntegerType integerType)
            return false;

        IntegerCast(integerType);
        return true;
    }

    public abstract void IntegerCast(IntegerType integerType);
}

public enum IntegerSignedness
{
    SignedInteger,
    UnsignedInteger,
}

public struct Variable
{
    public ulong BaseStackDifference;
    public ulong Size;

    public VariableType Type;

    public required Scope Owner;

    public static bool IsSignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.SignedInteger;
    public static bool IsUnsignedInteger(IntegerType integerType) => integerType.Signedness == IntegerSignedness.UnsignedInteger;
    
    public static long GetSize(VariableType variableType) => variableType.Size;
}