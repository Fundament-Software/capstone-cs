namespace Fundament.Capstone.Runtime;

using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

using Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

using Word = UInt64;

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


    public async ValueTask<object?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        // According to the cap'n proto spec, the first word of the message is the number of segments minus one (since there is always one segment for the root object)
        var segmentCount = await this.byteStream.ReadUInt32Async(cancellationToken) + 1;

        if (SegmentCountOverLimit(segmentCount)) {
            this.logger.LogSegmentExceedesThreshold(segmentCount, SegmentLimit);
            var skippedMessageSize = await this.SkipMessageOverSegmentCountLimit(segmentCount, cancellationToken);
            this.logger.LogSkipMessage(skippedMessageSize, segmentCount);
            return null;
        }

        using var segmentSizes = await this.ReadSegmentSizesAsync(segmentCount, cancellationToken);
        var messageSize = SumUIntSpan(segmentSizes.Span);

        if (MessageCountOverLimit(messageSize)) {
            await this.SkipMessage(messageSize, cancellationToken);
            this.logger.LogSkipMessage(messageSize, segmentCount);
            return null;
        }
    }

    private async ValueTask<MemoryOwner<uint>> ReadSegmentSizesAsync(uint segmentCount, CancellationToken cancellationToken) => 
        segmentCount == 0
            ? MemoryOwner<uint>.Empty 
            : await this.byteStream.ReadUInt32ArrayAsync(segmentCount, cancellationToken);

    /// <summary>
    /// Skips a message that has more segments than the limit.
    /// </summary>
    /// <param name="segmentCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The size of the message skiped in words.</returns>
    private async ValueTask<uint> SkipMessageOverSegmentCountLimit(uint segmentCount, CancellationToken cancellationToken) {
        uint messageSize = 0;
        var segmentSizesRead = 0;
        using var sizesBuffer = MemoryOwner<uint>.Allocate(512);
        while (segmentSizesRead < segmentCount) {
            cancellationToken.ThrowIfCancellationRequested();

            var segmentsToRead = Math.Min((int)segmentCount - segmentSizesRead, sizesBuffer.Length);
            await this.byteStream.ReadExactlyAsync(sizesBuffer.Memory[..segmentsToRead].AsBytes(), cancellationToken);

            segmentSizesRead += segmentsToRead;
            messageSize += SumUIntSpan(sizesBuffer.Span[..segmentsToRead]);
        }

        await this.SkipMessage(messageSize, cancellationToken);

        return messageSize;
    }

    private async ValueTask SkipMessage(uint messageSize, CancellationToken cancellationToken) {
        using var buffer = MemoryOwner<Word>.Allocate(512);

        while (messageSize > 0) {
            cancellationToken.ThrowIfCancellationRequested();

            var wordsToRead = Math.Min((int)messageSize, buffer.Length);
            await this.byteStream.ReadExactlyAsync(buffer.Memory[..wordsToRead].AsBytes(), cancellationToken);

            messageSize -= (uint)wordsToRead;
        }
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
    private static bool MessageCountOverLimit(uint messageSize) => messageSize > TraversalLimitInWords;

}