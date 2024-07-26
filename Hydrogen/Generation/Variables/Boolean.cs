using Hydrogen.Generation.Variables.Integers;

namespace Hydrogen.Generation.Variables;

public class Boolean : IntegerType
{
    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "al";
    public override string AsmBRegister => "bl";
    public override string AsmPointerSize => "byte";

    public override string Keyword => "bool";
    public override long Size => 1;

    public override void IntegerCast(Generator generator, IntegerType integerType, int lineNumber) { } // Nothing
}