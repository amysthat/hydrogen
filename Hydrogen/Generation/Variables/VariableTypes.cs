using Hydrogen.Generation.Variables.Integers;

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
    public static Bool Bool => new();
}
