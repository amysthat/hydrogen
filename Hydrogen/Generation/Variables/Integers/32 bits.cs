namespace Hydrogen.Generation.Variables.Integers;

public class SignedInteger32 : IntegerType
{
    public override string Keyword => "i32";
    public override long Size => 4;

    public override IntegerSignedness Signedness => IntegerSignedness.SignedInteger;
    public override string AsmARegister => "eax";
    public override string AsmBRegister => "ebx";
    public override string AsmPointerSize => "dword";

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
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

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
    }
}