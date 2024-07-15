namespace Fundament.Capstone.Runtime.MessageStream;

public sealed class FarPointerReader<TCap> : BaseReader<TCap, FarPointerReader<TCap>>, IFarPointerReader<TCap>
{
    // The pointer that this far pointer points to. If this is a double-far pointer, then this is the tag word.
    private readonly WirePointer landingPad;

    private readonly bool isDoubleFar;

    // The offset of the target pointer in the target segment. If this is a double-far pointer, then this is the offset of the target object's content.
    private readonly uint targetOffset;

    // The segment ID of the target object. If this is a double-far pointer, then this is the segment ID of the target object's content.
    private readonly uint targetSegmentId;

    internal FarPointerReader(SharedReaderState state, FarPointer farPointer)
    : base(state)
    {
        this.isDoubleFar = farPointer.IsDoubleFar;

        if (farPointer.IsDoubleFar)
        {
            (this.landingPad, this.targetOffset, this.targetSegmentId) = this.DecodeDoubleFar(farPointer);

            this.SharedReaderState.TraversalCounter += 2;
        }
        else
        {
            this.landingPad = this.DecodeSingleFar(farPointer);
            (_, this.targetOffset, this.targetSegmentId) = farPointer;

            this.SharedReaderState.TraversalCounter += 1;
        }
    }

    public IReader<TCap> Reader =>
        this.isDoubleFar
            ? this.landingPad.MatchTraverse<int, TCap>(
                this.SharedReaderState,
                (int)this.targetSegmentId,
                0,
                (int)this.targetOffset,
                (targetOffset, structPointer) => structPointer with { Offset = targetOffset },
                (targetOffset, listPointer) => listPointer with { Offset = targetOffset })
            : this.landingPad.Traverse<TCap>(this.SharedReaderState, (int)this.targetSegmentId, (int)this.targetOffset);

    private static FarPointer DecodeDoubleFarLandingPad(Word pointerWord, int offset)
    {
        var pointer = FarPointer.Decode(pointerWord);

        return pointer.IsDoubleFar
            ? throw new InvalidPointerTypeException(
                pointerWord,
                offset,
                $"Expected first word of double-far landing pad (0x{pointerWord}:X at index {offset}) to be a single-far pointer.")
            : pointer;
    }

    private WirePointer DecodeSingleFar(FarPointer farPointer)
    {
        var landingPad = this.WireMessage[(int)farPointer.SegmentId][(int)farPointer.Offset];
        return WirePointer.Decode(landingPad);
    }

    private (WirePointer Tag, uint TargetOffset, uint SegmentId) DecodeDoubleFar(FarPointer farPointer)
    {
        var landingPadOffset = (int)farPointer.Offset;
        var landingPadWords = this.WireMessage[(int)farPointer.SegmentId].AsSpan(landingPadOffset, 2);
        // The first word should be another single-far pointer which points to the start of the object's content.
        var landingPointer = DecodeDoubleFarLandingPad(landingPadWords[0], landingPadOffset);

        // The second word is a tag word, which is an object pointer with an offset of zero that describes the object.
        var tagWord = landingPadWords[1];
        var tag = WirePointer.Decode(tagWord);

        return (tag, landingPointer.Offset, landingPointer.SegmentId);
    }
}
