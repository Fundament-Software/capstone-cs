namespace Fundament.Capstone.Runtime.MessageStream;

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

using CommunityToolkit.Diagnostics;

/// <summary>
/// Decorator around an ArraySegment<Word> to represent a slice of a wire message.
/// </summary>
/// <param name="Slice"></param>
public readonly record struct WireSegmentSlice(ArraySegment<Word> Slice) : IReadOnlyList<Word>
{
    public WireSegmentSlice(Word[] array, Range range) : this(array.Slice(range)) {}

    public static implicit operator WireSegmentSlice(ArraySegment<Word> slice) => new(slice);

    public IEnumerator<Word> GetEnumerator() => this.Slice.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.Slice.GetEnumerator();

    public Word this[int index] => this.Slice[index];

    /// <summary>
    /// The main underlying array of the slice.
    /// Forwards to the underlying ArraySegment's Array property.
    /// </summary>
    public Word[]? Array => this.Slice.Array;

    /// <summary>
    /// The starting offset of the slice.
    /// Forwards to the underlying ArraySegment's Offset property.
    /// </summary>
    public int Offset => this.Slice.Offset;

    /// <summary>Gets the number of elements in the range delimited by the array segment.</summary>
    public int Count => this.Slice.Count;

    /// <summary>
    /// Static helper method to calculate the array index and bit offset for a sized-aligned offset.
    /// </summary>
    /// <param name="offset">The offset in number of types of size <paramref name="typeSize"/>.</param>
    /// <param name="typeSize">The size of the type in bytes.</param>
    /// <example>
    /// Given an offset of 6 and a type size of 16 (representing a 2-byte integer),
    /// this method would return (1, 32) as 4 2-byte integers fit in a 64-bit word,
    /// and the 6th 2-byte integer would begin at the 32nd bit of the second word.
    /// <example>
    public static (int ArrayIndex, int WordIndex) CalculateSizeAlignedIndiciesFromOffset(int offset, int typeSize)
    {
        // Figure out how many of the type fit in a word
        var typePerWord = 64 / typeSize;
        // Calculate which word contains the type we want
        var arrayIndex = offset / typePerWord;
        // Calculate the bit offset of the type within the word
        var wordIndex = offset % typePerWord * typeSize;

        return (arrayIndex, wordIndex);
    }

    public bool IsIndexInRange(int index) => index >= 0 && index < this.Count;

    /// <summary>
    /// Gets the bit at the specified bit index in the slice.
    /// </summary>
    /// <param name="index">Index of the bit to get the value, in bits from the beginning of the slice.</param>
    public bool GetBitByOffset(int index)
    {
        var (arrayIndex, wordIndex) = CalculateSizeAlignedIndiciesFromOffset(index, 1);

        if (!this.IsIndexInRange(arrayIndex))
        {
            return false;
        }

        // Get the word
        var word = this[arrayIndex];
        return (word >> wordIndex & 1) == 1;
    }

    /// <summary>
    /// Gets a value of the specified size at the specified type-size-aligned offset.
    /// If the offset is out of range, zero is returned.
    /// </summary>
    /// <param name="offset">The offset, in number of <paramref name="typeSize"/> groups of bits from the beginning of the slice.</param>
    /// <param name="typeSize">The size of the type in bits.</param>
    /// <returns>A word holding the value at the offset. Only the first <paramref name="typeSize"/> bits of the words are significant.</returns>
    public Word GetBySizeAlignedOffset(int offset, int typeSize)
    {
        Guard.IsInRange(typeSize, 1, sizeof(Word) * 8 + 1);
        var (arrayIndex, wordIndex) = CalculateSizeAlignedIndiciesFromOffset(offset, typeSize);

        if (!this.IsIndexInRange(arrayIndex))
        {
            return default;
        }

        // Get the word
        var word = this[arrayIndex];
        return word >> wordIndex & Bits.BitMaskOf(typeSize);
    }

    /// <summary>
    /// Gets a value of type T from the slice at the specified type-aligned offset.
    /// If the offset is out of range, the default value of T is returned.
    /// </summary>
    /// <typeparam name="T">A value type implementing IBinaryNumber.</typeparam>
    /// <param name="offset">The offset, in number of <typeparamref name="T"/>s, from the beginning of the slice.</param>
    /// <returns>The value as <typeparamref name="T"/> at the specified offset or the default value if the offset is out of range.</returns>
    public T GetBySizeAlignedOffset<T>(int offset) where T : unmanaged, IBinaryNumber<T>
    {
        var wordValue = this.GetBySizeAlignedOffset(offset, Unsafe.SizeOf<T>() * 8);
        return Unsafe.As<Word, T>(ref wordValue);
    }
}

/// <summary>
/// Light wrapper around a Word array representing a segment of a wire message.
/// </summary>
/// <param name="Contents"></param>
public readonly record struct WireMessageSegment(Word[] Contents)
{
    public static implicit operator WireMessageSegment(Word[] contents) => new(contents);

    public WireSegmentSlice this[Range range] => this.Slice(range);

    public WireSegmentSlice Slice(Range range) => this.Contents.Slice(range);
}

/// <summary>
/// Light wrapper around an array of WireMessageSegments representing a wire message.
/// </summary>
/// <param name="Segments"></param>
public readonly record struct WireMessage(WireMessageSegment[] Segments)
{
    public WireMessageSegment this[int index] => this.Segments[index];

    public Word[] GetSegmentContents(int index) => this.Segments[index].Contents;
}