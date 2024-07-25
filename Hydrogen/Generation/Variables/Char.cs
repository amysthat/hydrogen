namespace Hydrogen.Generation.Variables;

public class Char : IntegerType // TODO: Probably shouldn't be an integer type, but a lot of the generation code only works for this
{
    public override string Keyword => "char";
    public override long Size => 1;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "al";
    public override string AsmBRegister => "bl";
    public override string AsmPointerSize => "byte";

    public override void IntegerCast(Generator generator, IntegerType integerType)
    {
        if (integerType is Byte)
        {
            return; // Successful cast
        }

        throw new CompilationException($"Can not cast char to {integerType}.");
    }
}