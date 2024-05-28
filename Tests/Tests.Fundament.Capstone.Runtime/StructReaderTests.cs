namespace Tests.Fundament.Capstone.Runtime;

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

        structReader.ReadInt16(0, 0).Should().Be(15);
    }
}