namespace Fundament.Capstone.Runtime.MessageStream;

using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance.Buffers;
using Fundament.Capstone.Runtime.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads cap'n proto messages from a stream, using the recommended bytestream framing (https://capnproto.org/encoding.html#serialization-over-a-stream)
/// </summary>
public class StreamMessageReader(Stream byteStream, ILogger<StreamMessageReader> logger)
{
    private const uint SegmentLimit = 512;

    private const ulong TraversalLimitInWords = 8 * 1024 * 1024;

    private const int SkipBufferSizeInWords = 512;

    private readonly Stream byteStream = byteStream;

    private readonly ILogger<StreamMessageReader> logger = logger;

    public async ValueTask<MessageFrameOwner> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        // According to the cap'n proto spec, the first word of the message is the number of segments minus one (since there is always one segment for the root object)
        var segmentCount = await this.byteStream.ReadUInt32Async(cancellationToken) + 1;

        if (SegmentCountOverLimit(segmentCount)) {
            this.logger.LogSegmentExceedesThreshold(segmentCount, SegmentLimit);
            throw new NotImplementedException("Segment count exceeds threshold, logic to handle this case is not implemented yet.");
        }

        using var segmentSizes = await this.ReadSegmentSizesAsync(segmentCount, cancellationToken);
        var messageSize = SumUIntSpan(segmentSizes.Span);

        if (MessageSizeOverLimit(messageSize)) {
            this.logger.LogMessageTooBig(messageSize, segmentCount, TraversalLimitInWords);
            throw new NotImplementedException("Message size exceeds traversal limit, logic to handle this case is not implemented yet.");
        }

        this.logger.LogSegmentTable(segmentCount, messageSize);

        await this.SkipPaddingAsync(segmentSizes.Length, cancellationToken);

        var segments = new MemoryOwner<Word>[segmentCount];
        for (var i = 0; i < segmentCount; i++) {
            var segmentSize = segmentSizes.Span[i];
            segments[i] = await this.byteStream.ReadWordsAsync(segmentSize, cancellationToken);
            this.logger.LogReadSegment(i, segmentSize);
        }

        return new(segments);
    }

    private async ValueTask<MemoryOwner<uint>> ReadSegmentSizesAsync(uint segmentCount, CancellationToken cancellationToken) => 
        segmentCount == 0
            ? MemoryOwner<uint>.Empty 
            : await this.byteStream.ReadUInt32ArrayAsync(segmentCount, cancellationToken);

    /// <summary>
    /// Calculates if there is any padding in the segment table and skips it. 
    /// </summary>
    /// <param name="segmentSizesLength"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>1 or true if there was 4 bytes of padding to skip, 0 or false if there was no padding.</returns>
    /// <exception cref="NotImplementedException"></exception>
    private async ValueTask<bool> SkipPaddingAsync(int segmentSizesLength, CancellationToken cancellationToken) {
        // The segment table is padded to a ulong word boundary, determine if there is any padding and skip it
        // There should only 0 or 4 bytes of padding
        var segmentTableSizeInBytes = 4 + segmentSizesLength * sizeof(uint);
        var paddingSizeInBytes = segmentTableSizeInBytes % sizeof(Word);
        this.logger.LogSkipPadding(paddingSizeInBytes);

        if (paddingSizeInBytes == 0) {
            return false;
        }

        if (paddingSizeInBytes != 4) {
            throw new NotImplementedException("Unexpected padding size, logic to handle this case is not implemented yet.");
        }

        var padding = await this.byteStream.ReadUInt32Async(cancellationToken);
        if (padding != 0) {
            this.logger.LogUnexpectedPaddingValues(padding);
        }
        return true;
    }

    private static uint SumUIntSpan(ReadOnlySpan<uint> span) {
        uint totalWords = 0;
        for (var i = 0; i < span.Length; i++) {
            totalWords += span[i];
        }
        return totalWords;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SegmentCountOverLimit(uint segmentCount) => segmentCount > SegmentLimit;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MessageSizeOverLimit(uint messageSize) => messageSize > TraversalLimitInWords;

}