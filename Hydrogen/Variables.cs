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
    public ulong StackLocation;
    public VariableType Type;
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

    public static void MoveIntegerToRegister(Generator generator, string register, NodeTermInteger integer, VariableType type)
    {
        if (!IsInteger(type))
            throw new InvalidOperationException();

        if (type == VariableType.Byte)
        {
            generator.output += $"    mov al, {integer.Int_Lit.Value} ; IntLit expression for type Byte\n";
            generator.output += $"    movzx {register}, al\n";
        }
        else if (type == VariableType.SignedInteger16 || type == VariableType.UnsignedInteger16)
        {
            generator.output += $"    mov ax, {integer.Int_Lit.Value} ; IntLit expression for type 16 bits\n";
            generator.output += $"    movzx {register}, ax\n";
        }
        else if (type == VariableType.SignedInteger32 || type == VariableType.UnsignedInteger32)
        {
            generator.output += $"    xor rax, rax ; IntLit expression for type 32 bits\n";
            generator.output += $"    mov eax, {integer.Int_Lit.Value}\n";
        }
        else
        {
            generator.output += $"    mov {register}, {integer.Int_Lit.Value} ; IntLit expression for type {type}\n";
        }
    }

    public static void CapInteger(Generator generator, VariableType type)
    {
        if (!IsInteger(type))
            throw new InvalidOperationException();

        if (type == VariableType.UnsignedInteger64 || type == VariableType.SignedInteger64)
            return; // Nothing to cap

        generator.Pop("rax");
        if (type == VariableType.Byte)
        {
            generator.output += $"    movzx rax, al ; Cap integer to byte limits\n";
        }
        else if (type == VariableType.SignedInteger16 || type == VariableType.UnsignedInteger16)
        {
            generator.output += $"    movzx rax, ax ; Cap integer to 16 bit limits\n";
        }
        generator.Push("rax");
    }
}