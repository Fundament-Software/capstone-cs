namespace Fundament.Capstone.Runtime;

using System.Buffers.Binary;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;


/// <summary>
/// Extensions for <see cref="Stream"/> that work with binary data.
/// Integers are read in little-endian format.
/// </summary>
internal static class BinaryStreamExtensions {

    // My hypothesis is that copying ints around is fast enough that it doesn't matter.
    public static async ValueTask<uint> ReadUInt32Async(this Stream stream, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        // I wonder if I can do some bullshit with stackalloc to avoid a heap allocation here, but I'll save that for when this works.
        using var buffer = MemoryOwner<byte>.Allocate(sizeof(uint));
        await stream.ReadExactlyAsync(buffer.Memory.AsBytes(), cancellationToken);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Span);
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> uints from the stream.
    /// This methods works by allocating a buffer from a pool, reading the data into the buffer, and then returning the buffer.
    /// The callee assumes ownership of the buffer and is responsible for disposing it.
    /// </summary>
    public static async ValueTask<MemoryOwner<uint>> ReadUInt32ArrayAsync(this Stream stream, uint count, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = MemoryOwner<uint>.Allocate((int)count);
        await stream.ReadExactlyAsync(buffer.Memory.AsBytes(), cancellationToken);
        return buffer;
    }
}