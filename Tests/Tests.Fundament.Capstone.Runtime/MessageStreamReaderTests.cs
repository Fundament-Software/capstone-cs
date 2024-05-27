namespace Tests.Fundament.Capstone.Runtime;

using System.Collections.Immutable;

using FluentAssertions.Execution;

using global::Fundament.Capstone.Runtime.MessageStream;

using Xunit.Abstractions;

public class MessageStreamReaderTests(ITestOutputHelper outputHelper)
{
    /// <summary>
    /// A simple message with a single segment containing a single 64-bit word, which is a pointer to a zero-sized struct for the root object.
    /// </summary>
    public static readonly ImmutableArray<byte> SingleEmptyMessage = [
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x3F, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

    public const ulong EmptyStructPointer = 0x000000000000C03F;

    [Fact]
    public async Task ReadSimpleMessage()
    {
        using var stream = new MemoryStream([.. SingleEmptyMessage]);
        var sut = new StreamMessageReader(stream, outputHelper.ToLogger<StreamMessageReader>());

        using var message = await sut.ReadMessageAsync();

        using (new AssertionScope()) {
            message.SegmentCount.Should().Be(1);
            message[0].Length.Should().Be(1);
            message[0].Span[0].Should().Be(EmptyStructPointer);
        }
    }

    [Fact]
    public async Task ReadHandlesPadding()
    {
        byte[] bytes = [
            0x01, 0x00, 0x00, 0x00, // 2 segments
            0x01, 0x00, 0x00, 0x00, // 1 word in first segment
            0x01, 0x00, 0x00, 0x00, // 1 word in second segment
            0x00, 0x00, 0x00, 0x00, // padding
            0x3F, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // pointer to empty struct
            0x3F, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // pointer to empty struct
        ];
        using var stream = new MemoryStream(bytes);
        var sut = new StreamMessageReader(stream, outputHelper.ToLogger<StreamMessageReader>());

        using var message = await sut.ReadMessageAsync(); 

        using (new AssertionScope()) {
            message.SegmentCount.Should().Be(2);
            message[0].Length.Should().Be(1);
            message[1].Length.Should().Be(1);
            message[0].Span[0].Should().Be(EmptyStructPointer);
            message[1].Span[0].Should().Be(EmptyStructPointer);
        }
    }
}