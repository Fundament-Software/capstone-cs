namespace Fundament.Capstone.Runtime.MessageStream;

using System.Diagnostics;
using System.Numerics;

using Fundament.Capstone.Runtime.Exceptions;
using Fundament.Capstone.Runtime.Logging;

/// <summary>
/// A reader for a struct in a cap'n proto message.
/// </summary>
/// <typeparam name="TCap">The type of the capability.</typeparam>
/// <remarks>
/// In the case ofthe MessageStream encoding, we consider instantiating a new reader to be a traversal of the pointer.
/// Therefore, instantiating a new reader increments the traversal counter.
/// </remarks>
public sealed class StructReader<TCap> : BaseReader<TCap, StructReader<TCap>>, IStructReader<TCap>
{
    private readonly int segmentId;

    // This class has an invariant we need to maintain:
    // The sharedReaderState.WireMessage is the same instance as dataSection.WireMessage and pointerSection.WireMessage
    // This isn't too hard because SharedReaderState is how we pass around the WireMessage instance between readers.
    private readonly WireSegmentSlice dataSection;

    private readonly WireSegmentSlice pointerSection;

    internal StructReader(SharedReaderState sharedReaderState, int segmentId, Index pointerIndex, StructPointer structPointer)
    : base(sharedReaderState)
    {
        this.segmentId = segmentId;

        // The index of the struct in the segment. Called "targetIndex" because it's the target of the struct pointer.
        var targetIndex = EvaluatePointerTarget(sharedReaderState.WireMessage[segmentId], pointerIndex, structPointer);

        this.dataSection = this.SliceDataSection(targetIndex, structPointer.DataSize);
        this.pointerSection = this.SlicePointerSection(targetIndex, structPointer.DataSize, structPointer.PointerSize);

        this.SharedReaderState.TraversalCounter += this.Size;

        this.Logger.LogPointerTraversal(structPointer, segmentId, this.SharedReaderState.TraversalCounter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructReader{TCap}"/> class.
    /// This constructor is used to create readers elements in composite lists.
    /// As a result, the traversal counter is not incremented in this constructor, as the pointer has already been traversed.
    /// </summary>
    /// <param name="sharedReaderState">The shared reader state.</param>
    /// <param name="segmentId">The segment id the struct resides in.</param>
    /// <param name="startIndex">The index of the struct in the segment.</param>
    /// <param name="dataSize">The size of the data section of the struct.</param>
    /// <param name="pointerSize">The size of the pointer section of the struct.</param>
    internal StructReader(SharedReaderState sharedReaderState, int segmentId, Index startIndex, ushort dataSize, ushort pointerSize)
    : base(sharedReaderState)
    {
        this.segmentId = segmentId;

        this.dataSection = this.SliceDataSection(startIndex, dataSize);
        this.pointerSection = this.SlicePointerSection(startIndex, dataSize, pointerSize);
    }

    public int Size => this.dataSection.Count + this.pointerSection.Count;

    public void ReadVoid(int index) => this.SharedReaderState.TraversalCounter += 1;

    public T ReadData<T>(int index, T defaultValue)
    where T : unmanaged, IBinaryNumber<T> =>
        this.dataSection.GetBySizeAlignedOffset<T>(index) ^ defaultValue;

    /// <summary>
    /// Reads at the specified index in the pointer section. This traverses the pointer and creates a new reader.
    /// </summary>
    /// <param name="index">The index of the pointer in the pointer section to read.</param>
    /// <returns>A new reader for the object pointed to by the pointer.</returns>
    public IReader<TCap> ReadPointer(int index)
    {
        var pointer = WirePointer.Decode(this.pointerSection[index]);
        var pointerIndex = this.pointerSection.Offset + index;

        return pointer.Traverse<TCap>(this.SharedReaderState, this.segmentId, pointerIndex);
    }

    IReader<TCap> IStructReader<TCap>.ReadPointer(int offset) => this.ReadPointer(offset);

    public bool ReadBool(int index, bool defaultValue) => this.dataSection.GetBitByOffset(index) ^ defaultValue;

    private static Index EvaluatePointerTarget(ReadOnlySpan<Word> segment, Index pointerIndex, StructPointer structPointer)
    {
        // StructReader constructer and StructPointer is internal, so only run this check when developing the library.
        Debug.Assert(segment[pointerIndex] == structPointer.AsWord, $"Expected word {segment[pointerIndex]:X} at index {pointerIndex} to equal {structPointer.AsWord:X}.");

        var pointerTargetIndex = pointerIndex.AddOffset(structPointer.Offset + 1);
        var normalizedPointerTargetIndex = pointerTargetIndex.GetOffset(segment.Length);

        PointerOffsetOutOfRangeException.ThrowIfOutOfRange(segment[pointerIndex], normalizedPointerTargetIndex, segment.Length, pointerIndex);
        return pointerTargetIndex;
    }

    private WireSegmentSlice SliceDataSection(Index targetIndex, ushort dataSize)
    {
        var dataSectionRange = targetIndex..targetIndex.AddOffset(dataSize);
        return this.WireMessage.Slice(this.segmentId, dataSectionRange);
    }

    private WireSegmentSlice SlicePointerSection(Index targetIndex, ushort dataSize, ushort pointerSize)
    {
        var pointerSectionIndex = targetIndex.AddOffset(dataSize);
        var pointerSectionRange = pointerSectionIndex..pointerSectionIndex.AddOffset(pointerSize);
        return this.WireMessage.Slice(this.segmentId, pointerSectionRange);
    }
}