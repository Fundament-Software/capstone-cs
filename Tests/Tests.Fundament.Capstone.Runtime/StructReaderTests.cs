namespace Tests.Fundament.Capstone.Runtime;

using System.Net.NetworkInformation;

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
        var (pointer, message) = CreateRootStructMessage([0x0000_0000_0000_000F]);
        var (structReader, _) = this.NewStructReader(message, pointer);

        (structReader as IStructReader<object>).ReadInt16(0, 0).Should().Be(15);
    }

    [Fact]
    public void ReadsFloat()
    {
        var expected = 8.62f;
        var (pointer, message ) = CreateRootStructMessage([BitConverter.SingleToUInt32Bits(expected)]);
        var (structReader, _) = this.NewStructReader(message, pointer);

        (structReader as IStructReader<object>).ReadFloat32(0, 0).Should().Be(expected);
    }

    [Fact]
    public void StructReaderReadsDouble()
    {
        var expected = 3.83;
        var (pointer, message) = CreateRootStructMessage([BitConverter.DoubleToUInt64Bits(expected)]);
        var (structReader, _) = this.NewStructReader(message, pointer);

        (structReader as IStructReader<object>).ReadFloat64(0, 0).Should().Be(expected);
    }

    [Fact]
    public void ReadsStructPointer()
    {
        var expectedRootData = 927.32;
        ulong expectedSecondaryData = 45;

        // Create the root struct pointer and set the root data.
        var (rootPointer, message) = CreateRootStructMessage([BitConverter.DoubleToUInt64Bits(expectedRootData)], pointerSize: 1, padding: 1);

        // Create the secondary struct pointer and set the secondary data.
        var secondaryStructPointer = WriteStruct(message[0], 2, 3, [expectedSecondaryData], 0);

        var (rootStructReader, sharedReaderState) = this.NewStructReader(message, rootPointer);

        // Assertions
        (rootStructReader as IStructReader<object>).ReadFloat64(0, 0).Should().Be(expectedRootData);
        var secondaryStructReader = rootStructReader.ReadPointer(0);
        secondaryStructReader.Should().NotBeNull();
        secondaryStructReader.Value.Should().BeOfType<StructReader<object>>();
        (secondaryStructReader.AsT0 as IStructReader<object>).ReadUInt64(0, 0).Should().Be(expectedSecondaryData);
    }

    private static StructPointer WriteStruct(Span<ulong> segment, Index pointerIndex, Index structIndex, ReadOnlySpan<ulong> data, ushort pointerSize)
    {
        var structOffset = structIndex.GetOffset(segment.Length) - pointerIndex.GetOffset(segment.Length) - 1;
        var structPointer = new StructPointer(structOffset, ushort.CreateChecked(data.Length), pointerSize);

        segment[pointerIndex] = structPointer.AsWord;
        data.CopyTo(segment[structIndex.StartRange(data.Length)]);

        return structPointer;
    }

    private static (StructPointer Pointer, WireMessage Message) CreateRootStructMessage(ReadOnlySpan<ulong> data, int structOffset = 0, ushort pointerSize = 0, int padding = 0)
    {
        var segment = new ulong[data.Length + structOffset + pointerSize + padding + 1];
        var structPointer = WriteStruct(segment, 0, structOffset + 1, data, pointerSize);
        return (structPointer, new WireMessage([segment]));
    }

    private (StructReader<object> RootStructReader, SharedReaderState State) NewStructReader(
        WireMessage message,
        StructPointer pointer,
        int segmentId = 0,
        Index index = default)
    {
        var state = new SharedReaderState { WireMessage = message, LoggerFactory = outputHelper.ToLoggerFactory() };
        var structReader = new StructReader<object>(state, segmentId, index, pointer);
        return (structReader, state);
    }
}