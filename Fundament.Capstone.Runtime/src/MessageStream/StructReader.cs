namespace Fundament.Capstone.Runtime.MessageStream;

using System.Diagnostics;
using System.Numerics;

using Fundament.Capstone.Runtime.Exceptions;
using Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// A reader for a struct in a cap'n proto message.
/// </summary>
/// <typeparam name="TCap">The type of the capability.</typeparam>
/// <remarks>
/// In the case ofthe MessageStream encoding, we consider instantiating a new reader to be a traversal of the pointer.
/// Therefore, instantiating a new reader increments the traversal counter.
/// </remarks>
public sealed class StructReader<TCap> : IStructReader<TCap>
{
    // This class has an invariant we need to maintain:
    // The sharedReaderState.WireMessage is the same instance as dataSection.WireMessage and pointerSection.WireMessage
    // This isn't too hard because SharedReaderState is how we pass around the WireMessage instance between readers.
    private readonly SharedReaderState sharedReaderState;

    private readonly int segmentId;

    private readonly WireSegmentSlice dataSection;

    private readonly WireSegmentSlice pointerSection;

    private readonly ILogger<StructReader<TCap>> logger;

    internal StructReader(
        SharedReaderState sharedReaderState,
        int segmentId,
        Index pointerIndex,
        StructPointer structPointer,
        ILogger<StructReader<TCap>> logger)
    {
        this.sharedReaderState = sharedReaderState;
        this.segmentId = segmentId;

        // The index of the struct in the segment. Called "targetIndex" because it's the target of the struct pointer.
        var targetIndex = EvaluatePointerTarget(sharedReaderState.WireMessage[segmentId], pointerIndex, structPointer);

        var dataSectionRange = targetIndex..targetIndex.AddOffset(structPointer.DataSize);
        this.dataSection = sharedReaderState.WireMessage.Slice(segmentId, dataSectionRange);

        var pointerSectionIndex = targetIndex.AddOffset(structPointer.DataSize);
        var pointerSectionRange = pointerSectionIndex..pointerSectionIndex.AddOffset(structPointer.PointerSize);
        this.pointerSection = sharedReaderState.WireMessage.Slice(segmentId, pointerSectionRange);

        this.logger = logger;

        this.sharedReaderState.TraversalCounter += this.Size;

        this.logger.LogStructPointerTraversal(structPointer, segmentId, this.sharedReaderState.TraversalCounter);
    }

    public int Size => this.dataSection.Count + this.pointerSection.Count;

    public void ReadVoid(int index) => this.sharedReaderState.TraversalCounter += 1;

    public T ReadData<T>(int index, T defaultValue)
    where T : unmanaged, IBinaryNumber<T> =>
        this.dataSection.GetBySizeAlignedOffset<T>(index) ^ defaultValue;

    public AnyReader<TCap> ReadPointer(int index) =>
        WirePointer
            .Decode(this.pointerSection[index])
            .Match(
                (StructPointer structPointer) => new StructReader<TCap>(this.sharedReaderState, this.segmentId, this.pointerSection.Offset + index, structPointer, this.logger),
                (ListPointer listPointer) => throw new NotImplementedException(),
                (FarPointer farPointer) => throw new NotImplementedException(),
                (CapabilityPointer capabilityPointer) => throw new NotImplementedException());

    IAnyReader<TCap> IStructReader<TCap>.ReadPointer(int offset) => this.ReadPointer(offset);

    public bool ReadBool(int index, bool defaultValue) => this.dataSection.GetBitByOffset(index) ^ defaultValue;

    private static Index EvaluatePointerTarget(ReadOnlySpan<Word> segment, Index pointerIndex, StructPointer structPointer)
    {
        // StructReader constructer and StructPointer is internal, so only run this check when developing the library.
        Debug.Assert(segment[pointerIndex] == structPointer.AsWord, $"Expected word {segment[pointerIndex]:X} at index {pointerIndex} to equal {structPointer.AsWord:X}.");

        var pointerTargetIndex = pointerIndex.AddOffset(structPointer.Offset + 1);
        var normalizedPointerTargetIndex = pointerTargetIndex.GetOffset(segment.Length);

        if (normalizedPointerTargetIndex < 0 || normalizedPointerTargetIndex >= segment.Length)
        {
            throw new PointerOffsetOutOfRangeException(segment[pointerIndex], normalizedPointerTargetIndex, pointerIndex);
        }

        return pointerTargetIndex;
    }
}