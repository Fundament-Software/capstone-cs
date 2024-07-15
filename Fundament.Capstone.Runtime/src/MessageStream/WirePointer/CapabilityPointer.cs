namespace Fundament.Capstone.Runtime.MessageStream;

using Fundament.Capstone.Runtime.Exceptions;

internal readonly record struct CapabilityPointer(int CapabilityTableOffset)
{
    /// <summary>
    /// Decodes a capability pointer from a segment.
    /// </summary>
    /// <returns>The data encoded in the word as a <see cref="CapabilityPointer"/>.</returns>
    /// <exception cref="TypeTagMismatchException">If the tag of the word does not match the expected tag.</exception>
    public static CapabilityPointer Decode(Word word)
    {
        PointerDecodingUtils.AssertWordTag(word, PointerType.Capability);

        // We only care about the last 32 bits of the word, which is the index to the capability table.
        var capabilityOffset = int.CreateChecked(word >> 32 & Bits.BitMaskOf(32));

        return new CapabilityPointer(capabilityOffset);
    }
}