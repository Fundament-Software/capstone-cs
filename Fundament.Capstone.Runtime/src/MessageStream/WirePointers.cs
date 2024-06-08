namespace Fundament.Capstone.Runtime.MessageStream;

using System.Runtime.InteropServices;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// Enum of tag values for pointer types in cap'n proto messages.
/// These are the least significant 2 bits of a pointer word.
/// Also used as the tag for the MessagePointer sum type.
/// </summary>
internal enum PointerType : byte
{
    Struct = 0,
    List = 1,
    Far = 2,
    Capability = 3
}

internal enum ListElementType : byte
{
    Void = 0,
    Bit = 1,
    Byte = 2,
    TwoBytes = 3,
    FourBytes = 4,
    EightBytes = 5,
    EightBytesPointer = 6,
    Composite = 7
}

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
}

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
    public bool IsComposite => this.ElementSize == ListElementType.Composite;

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
}

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

/// <summary>
/// Sum type for the different pointer types in cap'n proto message.
/// This is implemented as a struct with a union-like layout to avoid boxing.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal readonly struct WirePointer
{
    [FieldOffset(0)]
    private readonly PointerType tag;

    [FieldOffset(1)]
    private readonly StructPointer structPointer;

    [FieldOffset(1)]
    private readonly ListPointer listPointer;

    [FieldOffset(1)]
    private readonly FarPointer farPointer;

    [FieldOffset(1)]
    private readonly CapabilityPointer capabilityPointer;

    public WirePointer(StructPointer structPointer)
    {
        this.tag = PointerType.Struct;
        this.structPointer = structPointer;
    }

    public WirePointer(ListPointer listPointer)
    {
        this.tag = PointerType.List;
        this.listPointer = listPointer;
    }

    public WirePointer(FarPointer farPointer)
    {
        this.tag = PointerType.Far;
        this.farPointer = farPointer;
    }

    public WirePointer(CapabilityPointer capabilityPointer)
    {
        this.tag = PointerType.Capability;
        this.capabilityPointer = capabilityPointer;
    }

    public bool IsStruct => this.tag == PointerType.Struct;
    public bool IsList => this.tag == PointerType.List;
    public bool IsFar => this.tag == PointerType.Far;
    public bool IsCapability => this.tag == PointerType.Capability;

    public StructPointer UnwrapStruct =>
        !this.IsStruct
            ? throw new InvalidOperationException("MessagePointer is not a struct pointer.")
            : this.structPointer;

    public ListPointer UnwrapList =>
        !this.IsList
            ? throw new InvalidOperationException("MessagePointer is not a list pointer.")
            : this.listPointer;

    public FarPointer UnwrapFar =>
        !this.IsFar
            ? throw new InvalidOperationException("MessagePointer is not a far pointer.")
            : this.farPointer;

    public CapabilityPointer UnwrapCapability =>
        !this.IsCapability
            ? throw new InvalidOperationException("MessagePointer is not a capability pointer.")
            : this.capabilityPointer;

    public static implicit operator WirePointer(StructPointer v) => new(v);
    public static implicit operator WirePointer(ListPointer v) => new(v);
    public static implicit operator WirePointer(FarPointer v) => new(v);
    public static implicit operator WirePointer(CapabilityPointer v) => new(v);

    public static implicit operator StructPointer?(WirePointer self) => self.IsStruct ? self.structPointer : null;
    public static implicit operator ListPointer?(WirePointer self) => self.IsList ? self.listPointer : null;
    public static implicit operator FarPointer?(WirePointer self) => self.IsFar ? self.farPointer : null;
    public static implicit operator CapabilityPointer?(WirePointer self) => self.IsCapability ? self.capabilityPointer : null;

#pragma warning disable SA1413 // UseTrailingCommasInMultiLineInitializers - For some reason the switch expression is a "multi-line initializer"
    public static WirePointer Decode(Word word) =>
        (PointerType)(word & 3) switch
        {
            PointerType.Struct => StructPointer.Decode(word),
            PointerType.List => ListPointer.Decode(word),
            PointerType.Far => FarPointer.Decode(word),
            PointerType.Capability => CapabilityPointer.Decode(word),
            _ => throw new InvalidOperationException("Unknown pointer type.")
        };

    public T Match<T>(Func<StructPointer, T> onStruct, Func<ListPointer, T> onList, Func<FarPointer, T> onFar, Func<CapabilityPointer, T> onCapability) => this.tag switch
    {
        PointerType.Struct => onStruct(this.structPointer),
        PointerType.List => onList(this.listPointer),
        PointerType.Far => onFar(this.farPointer),
        PointerType.Capability => onCapability(this.capabilityPointer),
        _ => throw new InvalidOperationException("Unknown pointer type.")
    };

    public TReturn Match<TState, TReturn>(
        TState state,
        Func<TState, StructPointer, TReturn> onStruct,
        Func<TState, ListPointer, TReturn> onList,
        Func<TState, FarPointer, TReturn> onFar,
        Func<TState, CapabilityPointer, TReturn> onCapability) => this.tag switch
    {
        PointerType.Struct => onStruct(state, this.structPointer),
        PointerType.List => onList(state, this.listPointer),
        PointerType.Far => onFar(state, this.farPointer),
        PointerType.Capability => onCapability(state, this.capabilityPointer),
        _ => throw new InvalidOperationException("Unknown pointer type.")
    };

    public override string ToString() => this.tag switch
    {
        PointerType.Struct => this.structPointer.ToString(),
        PointerType.List => this.listPointer.ToString(),
        PointerType.Far => this.farPointer.ToString(),
        PointerType.Capability => this.capabilityPointer.ToString(),
        _ => throw new InvalidOperationException("Unknown pointer type.")
    };
#pragma warning restore SA1413
}

internal static class PointerDecodingUtils
{
    /// <summary>
    /// Checks if the offset of a pointer is within the bounds of the segment.
    /// </summary>
    /// <param name="segment">The segment the pointer resides in.</param>
    /// <param name="pointerIndex">The index of the pointer word in the segment.</param>
    /// <param name="offset">The decoded offset of the pointer word.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the word index is out of bounds for segment.</exception>
    /// <exception cref="PointerOffsetOutOfRangeException">If the offset points outside of the bounds of the segment.</exception>
    public static void CheckPointerOffset(ReadOnlySpan<Word> segment, Index pointerIndex, int offset)
    {
        var targetOffset = pointerIndex.AddOffset(offset + 1).GetOffset(segment.Length);

        if (targetOffset < 0 || targetOffset >= segment.Length)
        {
            throw new PointerOffsetOutOfRangeException(segment[pointerIndex], targetOffset, pointerIndex);
        }
    }

    public static void AssertWordTag(Word word, PointerType expectedTag)
    {
        var tag = word & 3;
        var expectedTagValue = (byte)expectedTag;

        if (tag != expectedTagValue)
        {
            throw new TypeTagMismatchException(word, expectedTagValue);
        }
    }
}