using Hydrogen.Generation.Variables.Integers;

namespace Hydrogen.Generation.Variables;

public class Pointer : IntegerType
{
    public override string Keyword => RepresentingType?.Keyword + "*";
    public override long Size => 8;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "rax";
    public override string AsmBRegister => "rbx";
    public override string AsmPointerSize => "qword";

    public required VariableType RepresentingType;

    public override void IntegerCast(Generator generator, IntegerType integerType)
    {
        if (integerType is UnsignedInteger64)
        {
            return; // Proper cast
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Can not cast pointer to anything other than u64. Tried to cast to {integerType.Keyword}.");
        Environment.Exit(1);
    }
}