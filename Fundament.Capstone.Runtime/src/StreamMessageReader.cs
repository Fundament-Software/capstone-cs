namespace Fundament.Capstone.Runtime;

using System.Buffers;
using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.HighPerformance.Helpers;

using Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

using Word = UInt64;

/// <summary>
/// Reads cap'n proto messages from a stream, using the recommended bytestream framing (https://capnproto.org/encoding.html#serialization-over-a-stream)
/// </summary>
public class StreamMessageReader(Stream byteStream, ILogger<StreamMessageReader> logger)
{
    private const uint SegmentLimit = 512;

    private const ulong traversalLimitInWords = 8 * 1024 * 1024;

    private readonly Stream byteStream = byteStream;

    private readonly ILogger<StreamMessageReader> logger = logger;


    public async ValueTask ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        var (segmentCount, firstSegmentSize) = await this.ReadSegmentTableHeaderAsync(cancellationToken);

        this.logger.LogSegmentTableHeader(segmentCount, firstSegmentSize);

        // First line of security: chech the segment count is within reasonable limits
        // A malicious sender could send a message with a UINT_MAX segment count,
        // which would mean we would have to allocate and read UINT_MAX uints (about 16 GB of memory) for the segment size table.
        // When we hit the limit, we set segmentCount and segmentSize to default values
        // Which will probably cause the message and subsequent messages to fail parsing.
        if (segmentCount < SegmentLimit) {
            this.logger.LogSegmentExceedesThreshold(segmentCount, SegmentLimit);
            segmentCount = 1;
            firstSegmentSize = 1;
        }

        
    }

    private async ValueTask<(uint SegmentCount, uint FirstSegmentSize)> ReadSegmentTableHeaderAsync(CancellationToken cancellationToken)
    {
        // I'm pretty sure that copying two ints for return is cheap, so allocate memory here and dispose it after the return.
        using var buffer = MemoryOwner<uint>.Allocate(2);
        await this.byteStream.ReadExactlyAsync(buffer.Memory.AsBytes(), cancellationToken);

        // Number of segments + 1
        var segmentCount = buffer.Span[0] + 1;
        return (segmentCount, buffer.Span[1]);
    }

    private async ValueTask<MemoryOwner<uint>> ReadSegmentSizesAsync(uint segmentCount, CancellationToken cancellationToken)
    {
        var remainingSegmentCount = segmentCount - 1;

        if (remainingSegmentCount == 0) {
            return MemoryOwner<uint>.Empty;
        }

        // This cast should be compeletely safe, as we have already checked that segmentCount is less than SegmentLimit
        var segmentSizes = MemoryOwner<uint>.Allocate((int)remainingSegmentCount);
        
        await this.byteStream.ReadExactlyAsync(segmentSizes.Memory.AsBytes(), cancellationToken);

        return segmentSizes;
    }

    private static uint ComputeTotalWords(uint firstSegmentSize, ReadOnlySpan<uint> segmentSizes)
    {
        var totalWords = firstSegmentSize;

        for (var i = 0; i < segmentSizes.Length; i++) {
            totalWords += segmentSizes[i];
        }

        return totalWords;
    }
}