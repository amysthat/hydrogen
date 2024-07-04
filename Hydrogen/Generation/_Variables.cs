using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public class VariableNotFoundException(string variable) : Exception(variable + " was not found");

public static class _Variables
{
    [Obsolete]
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

    /// <summary>
    /// Moves an Integer to the appropriate register.
    /// </summary>
    /// <returns>Used register</returns>
    [Obsolete]
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
}