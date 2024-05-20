namespace Fundament.Capstone.Runtime.BinaryStream;

using System.Collections;

using CommunityToolkit.HighPerformance.Buffers;

using Word = UInt64;

/// <summary>
/// Represents a message cap'n proto message frame, as a sequence of segments.
/// This is a light wrapper around an array of MemoryOwners.
/// Owners of this object assume all rights and responsibilities of the MemoryOwners.
/// </summary>
/// <remarks>
/// Normally Cap'n Proto messages are prefixed by a segment table which contain the count and size of each segment.
/// This object does not contain the segment table, only the segments themselves, as the count and size are properties of the segment array and the segments themselves.
/// </remarks>
/// <param name="Segments"></param>
public readonly record struct MessageFrame(MemoryOwner<Word>[] Segments) : IDisposable, IReadOnlyCollection<Memory<Word>>
{
    public readonly int Count => this.Segments.Length;

    public readonly Memory<Word> this[int index] => this.Segments[index].Memory;

    public readonly int GetSegmentSize(int index) => this.Segments[index].Length;

    public readonly void Dispose() {
        foreach (var segment in this.Segments) {
            segment.Dispose();
        }
    }

    public readonly IEnumerator<Memory<ulong>> GetEnumerator() => this.Segments.Select(segment => segment.Memory).GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}