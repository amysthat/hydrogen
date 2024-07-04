using Hydrogen.Generation.Variables.Integers;
using Hydrogen.Parsing;

namespace Hydrogen.Generation;

public class VariableNotFoundException(string variable) : Exception(variable + " was not found");

[Obsolete("Currently under removal, with functionallity being transfered.")]
public static class _Variables
{
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