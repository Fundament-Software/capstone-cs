namespace Tests.Fundament.Capstone.Runtime;

using global::Fundament.Capstone.Runtime;
using global::Fundament.Capstone.Runtime.MessageStream;

using Xunit.Abstractions;

public class StructReaderTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void StructReaderReadsSimpleValue()
    {
        // 15 encoded as the first 16-bit integer in the data section.
        var dataSection = new WireSegmentSlice(new ulong[] { 0x0000_0000_0000_000F });
        var structReader = new StructReader(dataSection, new WireSegmentSlice(Array.Empty<ulong>()), outputHelper.ToLogger<StructReader>());

        (structReader as IStructReader).ReadInt16(0, 0).Should().Be(15);
    }

    [Fact]
    public void StructReaderReadsFloat()
    {
        var expected = 8.62f;
        var dataSection = new WireSegmentSlice(new ulong[] { BitConverter.SingleToUInt32Bits(expected) });
        var structReader = new StructReader(dataSection, new WireSegmentSlice(Array.Empty<ulong>()), outputHelper.ToLogger<StructReader>());

        (structReader as IStructReader).ReadFloat32(0, 0).Should().Be(expected);
    }

    [Fact]
    public void StructReaderReadsDouble()
    {
        var expected = 3.83;
        var dataSection = new WireSegmentSlice(new ulong[] { BitConverter.DoubleToUInt64Bits(expected) });
        var structReader = new StructReader(dataSection, new WireSegmentSlice(Array.Empty<ulong>()), outputHelper.ToLogger<StructReader>());

        (structReader as IStructReader).ReadFloat64(0, 0).Should().Be(expected);
    }
}