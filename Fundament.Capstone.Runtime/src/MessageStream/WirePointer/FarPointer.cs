namespace Fundament.Capstone.Runtime.MessageStream;

using Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// Decoded value of a far pointer in a cap'n proto message.
/// </summary>
/// <param name="IsDoubleFar">
///     Identifies the type of the pointer target.
///     If false, the target is a regular intra-segment pointer.
///     If true, the target is another far pointer followed by a tag word.
/// </param>
/// <param name="Offset">The offset, in words, from the start of the target segment to the location of the far-pointer landing-pad.</param>
/// <param name="SegmentId">The id of the target segment.</param>
internal readonly record struct FarPointer(bool IsDoubleFar, uint Offset, uint SegmentId)
{
    public Word AsWord =>
        ((Word)PointerType.Far) |
        (this.IsDoubleFar ? 1UL << 2 : 0) |
        (this.Offset & Bits.BitMaskOf(29)) << 3 |
        (this.SegmentId & Bits.BitMaskOf(32)) << 32;

    /// <summary>
    /// Decodes a far pointer from a segment.
    /// The caller must validate the segment id and offset, as this method is unable to check bounds outside of the provided segment.
    /// </summary>
    /// <param name="word">The word to decode.</param>
    /// <returns>The data encoded in the word as a <c>FarPointer</c>.</returns>
    /// <exception cref="TypeTagMismatchException">If the tag of the word does not match the expected tag.</exception>
    public static FarPointer Decode(Word word)
    {
        PointerDecodingUtils.AssertWordTag(word, PointerType.Far);

        // First bit is the double-far flag
        var doubleFarFlag = (word >> 2 & 1) == 1;
        // Next 29 bits are the offset in words from the beginning of the target segment.
        var offset = uint.CreateChecked(word >> 3 & Bits.BitMaskOf(29));
        // Last 32 bits are the segment id.
        var segmentId = uint.CreateChecked(word >> 32 & Bits.BitMaskOf(32));

        return new FarPointer(doubleFarFlag, offset, segmentId);
    }

    /*
    Honestly, I kinda don't like the Pointer types having methods defined on them to define traversal
    Because I think it violates Single-Responsibility, and I think that's especially apparent here in FarPointer.
    These types are intended to be simple data types which store data extracted from Cap'n'Proto words, with minimal
    dependencies.

    But, we have to have a single place to define how to traverse pointers, otherwise every Reader would define it's
    own traversal method. And the simplest, idiomatic, and most efficient way to do so, is to just define plain, normal
    instance methods on a type.
    */

    public IReader<TCap> Traverse<TCap>(SharedReaderState state)
    {
        if (this.IsDoubleFar)
        {
            var (tag, tagWord, targetOffset, targetSegmentId) = this.DecodeDoubleFar(state.WireMessage);
            return tag.Match(
                structPointer => (structPointer with { Offset = targetOffset }).Traverse<TCap>(state, targetSegmentId, 0),
                listPointer => (listPointer with { Offset = targetOffset }).Traverse<TCap>(state, targetSegmentId, 0),
                farPointer => throw new InvalidPointerTypeException(tagWord, message: "Expected tag word to be a struct, list, or capability pointer."),
                capPointer => throw new NotImplementedException("Capability pointers are not yet supported.")
            );
        }
        else
        {
            return this
                .DecodeSingleFar(state.WireMessage)
                .Traverse<TCap>(state, (int)this.SegmentId, (int)this.Offset);
        }
    }

    private (WirePointer Tag, Word TagWord, int TargetOffset, int TargetSegmentId) DecodeDoubleFar(WireMessage wireMessage)
    {
        var landingPadWords = this.GetTarget(wireMessage, 2);

        // The first word should be another single-far pointer which points to the start of the object's content.
        var (_, targetOffset, targetSegmentId) = this.DecodeDoubleFarLandingPad(landingPadWords[0]);

        // The second word is a tag word, which is an object pointer with an offset of zero that describes the object.
        var tagWord = landingPadWords[1];
        var tag = WirePointer.Decode(tagWord);

        return (tag, tagWord, (int)targetOffset, (int)targetSegmentId);
    }

    private WirePointer DecodeSingleFar(WireMessage wireMessage) => WirePointer.Decode(this.GetTarget(wireMessage));

    private FarPointer DecodeDoubleFarLandingPad(Word pointerWord)
    {
        var offset = (int)this.Offset;
        var pointer = Decode(pointerWord);

        return pointer.IsDoubleFar
            ? throw new InvalidPointerTypeException(
                pointerWord,
                offset,
                $"Expected first word of double-far landing pad (0x{pointerWord}:X at index {offset}) to be a single-far pointer.")
            : pointer;
    }

    private Word[] GetTargetSegment(WireMessage wireMessage) => wireMessage[(int)this.SegmentId];

    private Span<Word> GetTarget(WireMessage wireMessage, int length) =>
        this.GetTargetSegment(wireMessage).AsSpan((int)this.Offset, length);

    private Word GetTarget(WireMessage wireMessage) => this.GetTargetSegment(wireMessage)[(int)this.Offset];
}