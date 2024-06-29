using Hydrogen.Generation;
using Hydrogen.Parsing;
using Hydrogen.Tokenization;

namespace Hydrogen;

public enum VariableType
{
    UnsignedInteger64,
    SignedInteger64,
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
            case VariableType.SignedInteger64:
                switch (targetType)
                {
                    case VariableType.UnsignedInteger64:
                        return;
                    case VariableType.Byte:
                        generator.Pop("rax ; Cast i64 to byte");
                        generator.output += $"    movzx rax, al\n";
                        generator.Push("rax");
                        return;
                }
                break;
            case VariableType.UnsignedInteger64:
                switch (targetType)
                {
                    case VariableType.SignedInteger64:
                        return;
                    case VariableType.Byte:
                        generator.Pop("rax ; Cast u64 to byte");
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
        generator.Push("rax");
    }
}