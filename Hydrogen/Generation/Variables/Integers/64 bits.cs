namespace Hydrogen.Generation.Variables.Integers;

public class SignedInteger64 : IntegerType
{
    public override string Keyword => "i64";
    public override long Size => 8;

    public override IntegerSignedness Signedness => IntegerSignedness.SignedInteger;
    public override string AsmARegister => "rax";
    public override string AsmBRegister => "rbx";
    public override string AsmPointerSize => "qword";

    public override void IntegerCast(Generator generator, IntegerType integerType)
    {
        if (integerType is Byte)
        {
            generator.Pop("rax ; Cast 64 bits to byte");
            generator.output += $"    movzx rax, al\n";
            generator.Push("rax");
        }
        else if (integerType.Size == 2)
        {
            generator.Pop("rax ; Cast 64 bits to 16 bits");
            generator.output += $"    movzx rax, ax\n";
            generator.Push("rax");
        }
        else if (integerType.Size == 4)
        {
            generator.Pop("rax ; Cast 64 bits to 32 bits");
            generator.output += $"    mov eax, eax ; Truncate rax to 32 bits\n";
            generator.Push("rax");
        }
    }
}

public class UnsignedInteger64 : IntegerType
{
    public override string Keyword => "u64";
    public override long Size => 8;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "rax";
    public override string AsmBRegister => "rbx";
    public override string AsmPointerSize => "qword";

    public override void IntegerCast(Generator generator, IntegerType integerType)
    {
        if (integerType is Byte)
        {
            generator.Pop("rax ; Cast 64 bits to byte");
            generator.output += $"    movzx rax, al\n";
            generator.Push("rax");
        }
        else if (integerType.Size == 2)
        {
            generator.Pop("rax ; Cast 64 bits to 16 bits");
            generator.output += $"    movzx rax, ax\n";
            generator.Push("rax");
        }
        else if (integerType.Size == 4)
        {
            generator.Pop("rax ; Cast 64 bits to 32 bits");
            generator.output += $"    mov eax, eax ; Truncate rax to 32 bits\n";
            generator.Push("rax");
        }
    }
}