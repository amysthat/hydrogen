namespace Hydrogen.Generation;

public enum VariableType
{
    UnsignedInteger64,
    SignedInteger64,
    SignedInteger16,
    UnsignedInteger16,
    SignedInteger32,
    UnsignedInteger32,
    Byte,
}

public struct Variable
{
    public ulong BaseStackDifference;
    public ulong Size;

    public VariableType Type;

    public required Scope Owner;
}