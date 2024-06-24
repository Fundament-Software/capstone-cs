namespace Fundament.Capstone.Runtime.MessageStream;

using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Traverses the pointer and returns the appropriate reader for the target.
    /// </summary>
    /// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
    /// <param name="state">The shared reader state.</param>
    /// <param name="segmentId">The segment id of the pointer.</param>
    /// <param name="pointerIndex">The index of the pointer in the segment.</param>
    /// <returns>A reader for the target of the pointer.</returns>
    public IReader<TCap> Traverse<TCap>(SharedReaderState state, int segmentId, Index pointerIndex) =>
        this.tag switch
        {
            PointerType.Struct => this.structPointer.Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.List => this.listPointer.Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.Far => this.farPointer.Traverse<TCap>(state),
            PointerType.Capability => throw new NotImplementedException("Capability pointer traversal is not implemented."),
            var unknown => throw new InvalidOperationException($"Unknown pointer type {unknown}. This error should be impossible.")
        };

    /// <summary>
    /// Composition of <see cref="Match"/> and <see cref="Traverse"/>.
    /// </summary>
    /// <typeparam name="TCap">The type of the capibility table imbued in the reader.</typeparam>
    /// <param name="state">The shared reader state.</param>
    /// <param name="segmentId">The segment id of the pointer.</param>
    /// <param name="pointerIndex">The index of the pointer in the segment.</param>
    /// <param name="onStruct">Function to execute on the struct pointer before traverse.</param>
    /// <param name="onList">Function to execute on the list pointer before traverse.</param>
    /// <param name="onFar">Function to execute on the far pointer before traverse.</param>
    /// <param name="onCapability">Function to call the far pointer before traverse.</param>
    /// <returns>A reader for the pointed object.</returns>
    public IReader<TCap> MatchTraverse<TCap>(
        SharedReaderState state,
        int segmentId,
        Index pointerIndex,
        Func<StructPointer, StructPointer>? onStruct = null,
        Func<ListPointer, ListPointer>? onList = null,
        Func<FarPointer, FarPointer>? onFar = null,
        Func<CapabilityPointer, CapabilityPointer>? onCapability = null) =>
        this.tag switch
        {
            PointerType.Struct => (onStruct?.Invoke(this.structPointer) ?? this.structPointer).Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.List => (onList?.Invoke(this.listPointer) ?? this.listPointer).Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.Far => (onFar?.Invoke(this.farPointer) ?? this.farPointer).Traverse<TCap>(state),
            PointerType.Capability => throw new NotImplementedException("Capability pointer traversal is not implemented."),
            var unknown => throw new InvalidOperationException($"Unknown pointer type {unknown}. This error should be impossible.")
        };

    public IReader<TCap> MatchTraverse<TState, TCap>(
        SharedReaderState state,
        int segmentId,
        Index pointerIndex,
        TState functionState,
        Func<TState, StructPointer, StructPointer>? onStruct = null,
        Func<TState, ListPointer, ListPointer>? onList = null,
        Func<TState, FarPointer, FarPointer>? onFar = null,
        Func<TState, CapabilityPointer, CapabilityPointer>? onCapability = null) =>
        this.tag switch
        {
            PointerType.Struct => (onStruct?.Invoke(functionState, this.structPointer) ?? this.structPointer).Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.List => (onList?.Invoke(functionState, this.listPointer) ?? this.listPointer).Traverse<TCap>(state, segmentId, pointerIndex),
            PointerType.Far => (onFar?.Invoke(functionState, this.farPointer) ?? this.farPointer).Traverse<TCap>(state),
            PointerType.Capability => throw new NotImplementedException("Capability pointer traversal is not implemented."),
            var unknown => throw new InvalidOperationException($"Unknown pointer type {unknown}. This error should be impossible.")
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