using Hydrogen.Generation;
using Hydrogen.Parsing;

namespace Hydrogen;

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

    public required Generator.Scope Owner;
}

public class VariableNotFoundException : Exception
{
    public VariableNotFoundException(string variable) : base(variable + " was not found") { }
}

public static class Variables
{
    public static void Cast(Generator generator, VariableType castType, VariableType targetType)
    {
        if (castType == targetType)
            return;

        switch (castType)
        {
            case VariableType.UnsignedInteger64:
            case VariableType.SignedInteger64:
                switch (targetType)
                {
                    case VariableType.UnsignedInteger64:
                    case VariableType.SignedInteger64:
                        return;
                    case VariableType.Byte:
                        generator.Pop("rax ; Cast 64 bits to byte");
                        generator.output += $"    movzx rax, al\n";
                        generator.Push("rax");
                        return;
                    case VariableType.SignedInteger16:
                    case VariableType.UnsignedInteger16:
                        generator.Pop("rax ; Cast 64 bits to 16 bits");
                        generator.output += $"    movzx rax, ax\n";
                        generator.Push("rax");
                        return;
                    case VariableType.SignedInteger32:
                    case VariableType.UnsignedInteger32:
                        generator.Pop("rax ; Cast 64 bits to 32 bits");
                        generator.output += $"    mov eax, eax ; Truncate rax to 32 bits\n";
                        generator.Push("rax");
                        return;
                }
                break;
            case VariableType.Byte:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                    case VariableType.UnsignedInteger64:
                    case VariableType.SignedInteger16:
                    case VariableType.UnsignedInteger16:
                    case VariableType.SignedInteger32:
                    case VariableType.UnsignedInteger32:
                        return;
                }
                break;
            case VariableType.SignedInteger16:
            case VariableType.UnsignedInteger16:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                    case VariableType.UnsignedInteger64:
                    case VariableType.SignedInteger32:
                    case VariableType.UnsignedInteger32:
                    case VariableType.UnsignedInteger16:
                    case VariableType.SignedInteger16:
                        return;
                    case VariableType.Byte:
                        generator.Pop("rax ; Cast 16 bits to byte");
                        generator.output += $"    movzx rax, al\n";
                        generator.Push("rax");
                        return;
                }
                break;
            case VariableType.SignedInteger32:
            case VariableType.UnsignedInteger32:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                    case VariableType.UnsignedInteger64:
                    case VariableType.SignedInteger32:
                    case VariableType.UnsignedInteger32:
                        return;
                    case VariableType.SignedInteger16:
                    case VariableType.UnsignedInteger16:
                        generator.Pop("rax ; Cast 32 bits to 16 bits");
                        generator.output += $"    movzx rax, ax\n";
                        generator.Push("rax");
                        return;
                    case VariableType.Byte:
                        generator.Pop("rax ; Cast 32 bits to byte");
                        generator.output += $"    movzx rax, al\n";
                        generator.Push("rax");
                        return;
                }
                break;
        }

        throw new NotImplementedException($"Casting {castType} to {targetType} is not supported yet.");
    }

    public static bool IsInteger(VariableType type)
    {
        switch (type)
        {
            case VariableType.SignedInteger64:
            case VariableType.UnsignedInteger64:
            case VariableType.SignedInteger16:
            case VariableType.UnsignedInteger16:
            case VariableType.SignedInteger32:
            case VariableType.UnsignedInteger32:
            case VariableType.Byte:
                return true;

            default:
                return false;
        }
    }

    public static bool IsSignedInteger(VariableType type)
    {
        switch (type)
        {
            case VariableType.SignedInteger64:
                return true;
            case VariableType.SignedInteger16:
                return true;
            case VariableType.SignedInteger32:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Moves an Integer to the appropriate register.
    /// </summary>
    /// <returns>Used register</returns>
    public static string MoveIntegerToRegister(Generator generator, NodeTermInteger integer, VariableType type)
    {
        if (!IsInteger(type))
            throw new InvalidOperationException();

        var register = GetARegisterForIntegerType(type);

        if (type == VariableType.UnsignedInteger64 || type == VariableType.SignedInteger64)
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for 64 bits\n";
        }
        else if (type == VariableType.UnsignedInteger32 || type == VariableType.SignedInteger32) // I hate you assembly
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value}\n";
        }
        else
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; Integer for {type}\n";
            generator.output += $"    movzx rax, {register}\n";
        }

        return register;
    }

    public static ulong GetSize(VariableType type) => type switch
    {
        VariableType.UnsignedInteger64 => 8,
        VariableType.SignedInteger64 => 8,
        VariableType.UnsignedInteger32 => 4,
        VariableType.SignedInteger32 => 4,
        VariableType.UnsignedInteger16 => 2,
        VariableType.SignedInteger16 => 2,
        VariableType.Byte => 1,
        _ => throw new Exception($"Unknown variable type size: {type}"),
    };

    public static string GetARegisterForIntegerType(VariableType type) => type switch
    {
        VariableType.UnsignedInteger64 => "rax",
        VariableType.SignedInteger64 => "rax",
        VariableType.UnsignedInteger32 => "eax",
        VariableType.SignedInteger32 => "eax",
        VariableType.UnsignedInteger16 => "ax",
        VariableType.SignedInteger16 => "ax",
        VariableType.Byte => "al",
        _ => throw new Exception($"Unknown integer variable type: {type}"),
    };

    public static string GetBRegisterForIntegerType(VariableType type) => type switch
    {
        VariableType.UnsignedInteger64 => "rbx",
        VariableType.SignedInteger64 => "rbx",
        VariableType.UnsignedInteger32 => "ebx",
        VariableType.SignedInteger32 => "ebx",
        VariableType.UnsignedInteger16 => "bx",
        VariableType.SignedInteger16 => "bx",
        VariableType.Byte => "bl",
        _ => throw new Exception($"Unknown integer variable type: {type}"),
    };

    public static string GetAsmPointerSizeForIntegerType(VariableType type) => type switch
    {
        VariableType.UnsignedInteger64 => "qword",
        VariableType.SignedInteger64 => "qword",
        VariableType.UnsignedInteger32 => "dword",
        VariableType.SignedInteger32 => "dword",
        VariableType.UnsignedInteger16 => "word",
        VariableType.SignedInteger16 => "word",
        VariableType.Byte => "byte",
        _ => throw new Exception($"Unknown integer variable type: {type}"),
    };
}