namespace Fundament.Capstone.Runtime.MessageStream;

/// <summary>
/// Decoded value of a struct pointer in a cap'n proto message.
/// </summary>
/// <param name="Offset">The offset, in words from the end of the pointer to the start of the struct's data section. Signed.</param>
/// <param name="DataSize">Size of the struct's data section, in words. </param>
/// <param name="PointerSize">Size of the struct's pointer section, in words.</param>
internal readonly record struct StructPointer(int Offset, ushort DataSize, ushort PointerSize)
{
    public bool IsNull => this.Offset == 0 && this.DataSize == 0 && this.PointerSize == 0;

    public bool IsEmpty => this.Offset == -1 && this.DataSize == 0 && this.PointerSize == 0;

    public Word AsWord =>
        ((Word)this.Offset & Bits.BitMaskOf(30)) << 2 |
        (this.DataSize & Bits.BitMaskOf(16)) << 32 |
        (this.PointerSize & Bits.BitMaskOf(16)) << 48 |
        ((Word)PointerType.Struct);

    public static StructPointer Decode(Word word)
    {
        PointerDecodingUtils.AssertWordTag(word, PointerType.Struct);

        // First 30 bits after the tag are the offset, as a signed integer
        var offset = int.CreateChecked(word >> 2 & Bits.BitMaskOf(30));
        // Next 16 bits are the size of the data section
        var dataSize = ushort.CreateChecked(word >> 32 & Bits.BitMaskOf(16));
        // Last 16 bits are the size of the pointer section
        var pointerSize = ushort.CreateChecked(word >> 48 & Bits.BitMaskOf(16));

        return new StructPointer(offset, dataSize, pointerSize);
    }

    public StructReader<TCap> Traverse<TCap>(SharedReaderState state, int segmentId, Index pointerIndex) =>
        new(state, segmentId, pointerIndex, this);
}