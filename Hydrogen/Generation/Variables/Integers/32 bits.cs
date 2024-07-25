namespace Hydrogen.Generation.Variables.Integers;

public class SignedInteger32 : IntegerType
{
    public override string Keyword => "i32";
    public override long Size => 4;

    public override IntegerSignedness Signedness => IntegerSignedness.SignedInteger;
    public override string AsmARegister => "eax";
    public override string AsmBRegister => "ebx";
    public override string AsmPointerSize => "dword";

    public override void IntegerCast(Generator generator, IntegerType integerType, int lineNumber)
    {
        if (integerType.Size == 2)
        {
            generator.Pop("rax ; Cast 32 bits to 16 bits");
            generator.output += $"    movzx rax, ax\n";
            generator.Push("rax");
        }
        else if (integerType is Byte)
        {
            generator.Pop("rax ; Cast 32 bits to byte");
            generator.output += $"    movzx rax, al\n";
            generator.Push("rax");
        }
    }
}

public class UnsignedInteger32 : IntegerType
{
    public override string Keyword => "u32";
    public override long Size => 4;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "eax";
    public override string AsmBRegister => "ebx";
    public override string AsmPointerSize => "dword";

    public override void IntegerCast(Generator generator, IntegerType integerType, int lineNumber)
    {
        if (integerType.Size == 2)
        {
            generator.Pop("rax ; Cast 32 bits to 16 bits");
            generator.output += $"    movzx rax, ax\n";
            generator.Push("rax");
        }
        else if (integerType is Byte)
        {
            generator.Pop("rax ; Cast 32 bits to byte");
            generator.output += $"    movzx rax, al\n";
            generator.Push("rax");
        }
    }
}