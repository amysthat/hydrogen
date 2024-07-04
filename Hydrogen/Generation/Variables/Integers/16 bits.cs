namespace Hydrogen.Generation.Variables.Integers;

public class SignedInteger16 : IntegerType
{
    public override string Keyword => "i16";
    public override long Size => 2;

    public override IntegerSignedness Signedness => IntegerSignedness.SignedInteger;
    public override string AsmARegister => "ax";
    public override string AsmBRegister => "bx";
    public override string AsmPointerSize => "word";

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
    }
}

public class UnsignedInteger16 : IntegerType
{
    public override string Keyword => "u16";
    public override long Size => 2;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "ax";
    public override string AsmBRegister => "bx";
    public override string AsmPointerSize => "word";

    public override void IntegerCast(IntegerType integerType)
    {
        // TODO: Implement
    }
}