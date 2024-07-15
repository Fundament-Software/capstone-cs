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
    /// <summary>
    /// Decodes a far pointer from a segment.
    /// The caller must validate the segment id and offset, as this method is unable to check bounds outside of the provided segment.
    /// </summary>
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

    public FarPointerReader<TCap> Traverse<TCap>(SharedReaderState state) =>
        new(state, this);
}