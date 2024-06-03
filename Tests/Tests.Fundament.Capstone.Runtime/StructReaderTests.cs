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
        var (pointer, message) = CreateStructMessage([0x0000_0000_0000_000F]);
        var (structReader, _) = this.CreateStructReader(pointer, message);

        (structReader as IStructReader<object>).ReadInt16(0, 0).Should().Be(15);
    }

    [Fact]
    public void StructReaderReadsFloat()
    {
        var expected = 8.62f;
        var (pointer, message) = CreateStructMessage([BitConverter.SingleToUInt32Bits(expected)]);
        var (structReader, _) = this.CreateStructReader(pointer, message);

        (structReader as IStructReader<object>).ReadFloat32(0, 0).Should().Be(expected);
    }

    [Fact]
    public void StructReaderReadsDouble()
    {
        var expected = 3.83;
        var (pointer, message) = CreateStructMessage([BitConverter.DoubleToUInt64Bits(expected)]);
        var (structReader, _) = this.CreateStructReader(pointer, message);

        (structReader as IStructReader<object>).ReadFloat64(0, 0).Should().Be(expected);
    }

    private static (StructPointer Pointer, WireMessage Message) CreateStructMessage(ulong[]? data = null, ulong[]? pointers = null)
    {
        data ??= [];
        pointers ??= [];

        var pointer = new StructPointer(0, 0, ushort.CreateChecked(data.Length), ushort.CreateChecked(pointers.Length));
        ulong[] segment = [
            pointer.AsWord,
            ..data,
            ..pointers
        ];
        return (pointer, new WireMessage([segment]));
    }

    private (StructReader<object> StructReader, SharedReaderState State) CreateStructReader(StructPointer structPointer, WireMessage message)
    {
        var state = new SharedReaderState { WireMessage = message };
        var reader = new StructReader<object>(state, 0, structPointer, outputHelper.ToLogger<StructReader<object>>());
        return (reader, state);
    }
}