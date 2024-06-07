namespace Tests.Fundament.Capstone.Runtime;

using CommunityToolkit.Diagnostics;

using global::Fundament.Capstone.Runtime;
using global::Fundament.Capstone.Runtime.MessageStream;

using Xunit.Abstractions;

public class StructReaderTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ReadsShort()
    {
        // 15 encoded as the first 16-bit integer in the data section.
        var (pointer, message) = CreateStructMessage([0x0000_0000_0000_000F]);
        var (structReader, _) = this.CreateStructReader(pointer, message);

        (structReader as IStructReader<object>).ReadInt16(0, 0).Should().Be(15);
    }

    [Fact]
    public void ReadsFloat()
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

    [Fact]
    public void ReadsStructPointer()
    {
        var expectedRootData = 927.32;
        ulong expectedSecondaryData = 45;

        var segment = new ulong[4];

        // Create the root struct pointer and set the root data.
        var rootStructPointer = new StructPointer(0, 0, 1, 1);
        segment[0] = rootStructPointer.AsWord;
        segment[1] = BitConverter.DoubleToUInt64Bits(expectedRootData);

        // Create the secondary struct pointer and set the secondary data.
        var secondaryStructPointer = new StructPointer(2, 0, 1, 0);
        segment[2] = secondaryStructPointer.AsWord;
        segment[3] = expectedSecondaryData;

        var message = new WireMessage([segment]);
        var (rootStructReader, _) = this.CreateStructReader(rootStructPointer, message);

        // Assertions
        (rootStructReader as IStructReader<object>).ReadFloat64(0, 0).Should().Be(expectedRootData);
        var secondaryStructReader = rootStructReader.ReadPointer(0);
        secondaryStructReader.Should().NotBeNull();
        secondaryStructReader.Value.Should().BeOfType<StructReader<object>>();
        (secondaryStructReader.AsT0 as IStructReader<object>).ReadUInt64(0, 0).Should().Be(expectedSecondaryData);
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