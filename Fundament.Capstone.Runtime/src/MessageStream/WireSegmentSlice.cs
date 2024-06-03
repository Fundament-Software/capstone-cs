namespace Fundament.Capstone.Runtime.MessageStream;

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

using CommunityToolkit.Diagnostics;

/// <summary>
/// Delimits a section of a wire message.
/// </summary>
public readonly struct WireSegmentSlice : IReadOnlyList<Word>
{
    private readonly WireMessage message;
    private readonly int segmentId;
    private readonly int offset;
    private readonly int length;

    public WireSegmentSlice(WireMessage message, int segmentId)
    : this(message, segmentId, Range.All)
    {
    }

    public WireSegmentSlice(WireMessage message, int segmentId, Range range)
    {
        Guard.IsInRangeFor(segmentId, message.Segments);

        this.message = message;
        this.segmentId = segmentId;
        (this.offset, this.length) = range.GetOffsetAndLength(message.Segments[segmentId].Length);
    }

    public WireMessage Message => this.message;

    public int SegmentId => this.segmentId;

    public Word[] Segment => this.message.Segments[this.segmentId];

    public int Offset => this.offset;

    /// <summary>Gets the number of elements in the range delimited by the array segment.</summary>
    public int Count => this.length;

    public Word this[int index]
    {
        get
        {
            Guard.IsInRange(index, 0, this.Count);
            this.GuardNotEmpty();

            return this.Segment[this.offset + index];
        }
    }

    public static WireSegmentSlice Empty(WireMessage message, int segmentId) => new(message, segmentId, 0..0);

    /// <summary>
    /// Static helper method to calculate the array index and bit offset for a sized-aligned offset.
    /// </summary>
    /// <param name="offset">The offset in number of types of size <paramref name="typeSize"/>.</param>
    /// <param name="typeSize">The size of the type in bytes.</param>
    /// <example>
    /// Given an offset of 6 and a type size of 16 (representing a 2-byte integer),
    /// this method would return (1, 32) as 4 2-byte integers fit in a 64-bit word,
    /// and the 6th 2-byte integer would begin at the 32nd bit of the second word.
    /// </example>
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

    public Span<Word> AsSpan() => this.Segment.AsSpan(this.Offset, this.Count);

    public Enumerator GetEnumerator()
    {
        this.GuardNotEmpty();
        return new(this);
    }

    public override int GetHashCode() => HashCode.Combine(this.message.GetHashCode(), this.segmentId, this.offset, this.length);

    public void CopyTo(Word[] destination, int destinationIndex = 0)
    {
        this.GuardNotEmpty();

        Array.Copy(this.Segment, this.offset, destination, destinationIndex, this.Count);
    }

    /// <summary>
    /// Creates a new array and copies the contents of the slice into it.
    /// </summary>
    /// <returns>A new array containing a copy of the elements of the slice.</returns>
    public Word[] ToArray()
    {
        this.GuardNotEmpty();
        return this.Segment[this.offset..(this.offset + this.Count)];
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
    public T GetBySizeAlignedOffset<T>(int offset)
    where T : unmanaged, IBinaryNumber<T>
    {
        var wordValue = this.GetBySizeAlignedOffset(offset, Unsafe.SizeOf<T>() * 8);
        return Unsafe.As<Word, T>(ref wordValue);
    }

    IEnumerator<Word> IEnumerable<Word>.GetEnumerator() => this.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private void GuardNotEmpty()
    {
        if (this.Count == 0)
        {
            throw new InvalidOperationException("The slice is empty.");
        }
    }

    public struct Enumerator : IEnumerator<Word>
    {
        private readonly Word[] segment;
        private readonly int start;
        private readonly int end;
        private int current;

        internal Enumerator(WireSegmentSlice slice)
        {
            this.segment = slice.Segment;
            this.start = slice.Offset;
            this.end = slice.Offset + slice.Count;
            this.current = slice.Offset - 1;
        }

        public readonly Word Current
        {
            get
            {
                Guard.IsInRange(this.current, this.start, this.end);
                return this.segment[this.current];
            }
        }

        readonly object? IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.current < this.end)
            {
                this.current++;
                return this.current < this.end;
            }

            return false;
        }

        void IEnumerator.Reset() => this.current = this.start - 1;

        public void Dispose()
        {
        }
    }
}