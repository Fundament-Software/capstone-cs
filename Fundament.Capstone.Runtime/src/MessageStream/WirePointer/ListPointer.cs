namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

/// <summary>
/// Decoded value of a list pointer in a cap'n proto message.
/// </summary>
/// <param name="Offset">The offset, in words from the end of the pointer to the start of the struct's data section. Signed.</param>
/// <param name="ElementSize">The size of each element in the list.</param>
/// <param name="Size">
///     The size of the list.
///     For all values where ElementSize is not 7, the size is the number of elements in the list.
///     For ElementSize 7, the size is the number of words in the list, not including the tag word that prefixes the list content.
/// </param>
internal readonly record struct ListPointer(int Offset, ListElementType ElementSize, uint Size)
{
    public bool IsPointer => this.ElementSize == ListElementType.EightBytesPointer;

    public bool IsComposite => this.ElementSize == ListElementType.Composite;

    public uint SizeInWords => this.ElementSize switch
    {
        ListElementType.Void => 0,
        // this.Size / 64
        ListElementType.Bit => this.Size / sizeof(Word) * 8,
        // https://stackoverflow.com/a/4846569
        // this.Size * 8 / sizeof(Word) * 8
        ListElementType.Byte => (this.Size + sizeof(Word) - 1) / sizeof(Word),
        // this.Size * 16 / sizeof(Word) * 8
        ListElementType.TwoBytes => ((this.Size * 2) + sizeof(Word) - 1) / sizeof(Word),
        // this.Size * 32 / sizeof(Word) * 8
        ListElementType.FourBytes => ((this.Size * 4) + sizeof(Word) - 1) / sizeof(Word),
        _ => this.Size,
    };

    public Word AsWord =>
        ((Word)this.Offset & Bits.BitMaskOf(30)) << 2 |
        ((Word)this.ElementSize & Bits.BitMaskOf(3)) << 32 |
        (this.Size & Bits.BitMaskOf(29)) << 35 |
        ((Word)PointerType.List);

    public static ListPointer Decode(Word word)
    {
        PointerDecodingUtils.AssertWordTag(word, PointerType.List);

        // First 30 bits after the tag are the offset, as a signed integer
        var offset = int.CreateChecked(word >> 2 & Bits.BitMaskOf(30));
        // Next 3 bits are the element size
        var elementSize = CreateCheckedListElementType(word >> 32 & Bits.BitMaskOf(3));
        // Last 29 bits represent the size of the list
        var size = uint.CreateChecked(word >> 35 & Bits.BitMaskOf(29));

        return new ListPointer(offset, elementSize, size);
    }

    public IReader<TCap> Traverse<TCap>(SharedReaderState state, int segmentId, Index pointerIndex) =>
        this.ElementSize switch
        {
            ListElementType.Void => new ListOfVoidReader<TCap>(state, segmentId, pointerIndex, this),
            ListElementType.Bit => new ListOfBooleanReader<TCap>(state, segmentId, pointerIndex, this),
            ListElementType.Byte => new ListOfPrimitiveReader<byte, TCap>(state, segmentId, pointerIndex, this),
            ListElementType.TwoBytes => new ListOfPrimitiveReader<short, TCap>(state, segmentId, pointerIndex, this),
            ListElementType.FourBytes => new ListOfPrimitiveReader<int, TCap>(state, segmentId, pointerIndex, this),
            ListElementType.EightBytes => new ListOfPrimitiveReader<long, TCap>(state, segmentId, pointerIndex, this),
            ListElementType.EightBytesPointer => new ListOfPointerReader<TCap>(state, segmentId, pointerIndex, this),
            ListElementType.Composite => new ListOfCompositeReader<TCap>(state, segmentId, pointerIndex, this),
            var unknown => throw new InvalidOperationException($"Unknown list element type {unknown}."),
        };

    /// <summary>
    /// Helper method that validates the provided value is a valid ListElementType and converts it to the enum.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If the value is out of range for ListElementType.</exception>
    private static ListElementType CreateCheckedListElementType(Word value)
    {
        Guard.IsBetweenOrEqualTo(value, (byte)ListElementType.Void, (byte)ListElementType.Composite);
        return (ListElementType)value;
    }
}