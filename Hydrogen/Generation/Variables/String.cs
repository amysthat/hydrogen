using Hydrogen.Generation.Variables.Integers;

namespace Hydrogen.Generation.Variables;

public class String : IntegerType
{
    public override string Keyword => "string";
    public override long Size => 8;

    public override IntegerSignedness Signedness => IntegerSignedness.UnsignedInteger;
    public override string AsmARegister => "rax";
    public override string AsmBRegister => "rbx";
    public override string AsmPointerSize => "qword";

    public override void IntegerCast(Generator generator, IntegerType integerType)
    {
        if (integerType is Pointer pointer)
        {
            if (pointer.RepresentingType is Char)
                return; // Proper cast
        }

        if (integerType is UnsignedInteger64)
        {
            return; // Proper cast
        }

        throw new CompilationException($"Can not cast string to anything other than char* or u64. Tried to cast to {integerType.Keyword}.");
    }
}