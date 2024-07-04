namespace Hydrogen.Generation.Variables.Integers;

public class SignedInteger64 : IntegerType
{
    public override string Keyword => "i64";
    public override long Size => 8;

    public override IntegerSignedness Signedness => IntegerSignedness.SignedInteger;
    public override string AsmARegister => "rax";
    public override string AsmBRegister => "rbx";
    public override string AsmPointerSize => "qword";

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
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

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
    }
}