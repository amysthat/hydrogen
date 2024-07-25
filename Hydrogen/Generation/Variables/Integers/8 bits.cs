namespace Hydrogen.Generation.Variables.Integers;

public class Byte : IntegerType
{
    public override string Keyword => "byte";
    public override long Size => 1;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "al";
    public override string AsmBRegister => "bl";
    public override string AsmPointerSize => "byte";

    public override void IntegerCast(Generator generator, IntegerType integerType, int lineNumber) { } // Nothing
}