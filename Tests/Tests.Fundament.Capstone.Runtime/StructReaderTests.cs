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
        var message = new WireMessage([[0x0000_0000_0000_000F]]);
        var dataSlice = new WireSegmentSlice(message, 0);
        var pointerSlice = WireSegmentSlice.Empty(message, 0);
        var structReader = new StructReader<object>(dataSlice, pointerSlice, outputHelper.ToLogger<StructReader<object>>());

        (structReader as IStructReader<object>).ReadInt16(0, 0).Should().Be(15);
    }

    [Fact]
    public void StructReaderReadsFloat()
    {
        var expected = 8.62f;
        var message = new WireMessage([[BitConverter.SingleToUInt32Bits(expected)]]);
        var dataSlice = new WireSegmentSlice(message, 0);
        var pointerSlice = WireSegmentSlice.Empty(message, 0);
        var structReader = new StructReader<object>(dataSlice, pointerSlice, outputHelper.ToLogger<StructReader<object>>());

        (structReader as IStructReader<object>).ReadFloat32(0, 0).Should().Be(expected);
    }

    [Fact]
    public void StructReaderReadsDouble()
    {
        var expected = 3.83;
        var message = new WireMessage([[BitConverter.DoubleToUInt64Bits(expected)]]);
        var dataSlice = new WireSegmentSlice(message, 0);
        var pointerSlice = WireSegmentSlice.Empty(message, 0);
        var structReader = new StructReader<object>(dataSlice, pointerSlice, outputHelper.ToLogger<StructReader<object>>());

        (structReader as IStructReader<object>).ReadFloat64(0, 0).Should().Be(expected);
    }
}