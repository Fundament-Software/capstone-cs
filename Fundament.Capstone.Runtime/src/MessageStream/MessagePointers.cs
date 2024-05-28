namespace Fundament.Capstone.Runtime.MessageStream;

using System.Runtime.InteropServices;

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
/// <param name="PointerIndex">Index of the pointer in the segment.</param>
/// <param name="Offset">The offset, in words from the end of the pointer to the start of the struct's data section. Signed.</param>
/// <param name="DataSize">Size of the struct's data section, in words. </param>
/// <param name="PointerSize">Size of the struct's pointer section, in words.</param>
public readonly record struct StructPointer(Index PointerIndex, int Offset, ushort DataSize, ushort PointerSize)
{
    public bool IsNull => this.Offset == 0 && this.DataSize == 0 && this.PointerSize == 0;

    public bool IsEmpty => this.Offset == -1 && this.DataSize == 0 && this.PointerSize == 0;
    
    /// <summary>Index to the first word of the struct in the segment.</summary>
    public Index StructIndex => this.PointerIndex.AddOffset(this.Offset + 1);

    private Index PointerSectionIndex => this.StructIndex.AddOffset(this.DataSize);

    /// <summary>Range representing the data section of the struct in the segment.</summary>
    public Range DataSectionRange => this.StructIndex.StartRange(this.DataSize);

    /// <summary>Range representing the pointer section of the struct in the segment.</summary>
    public Range PointerSectionRange => this.PointerSectionIndex.StartRange(this.PointerSize);
}

public enum ListElementType : byte
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
/// Decoded value of a list pointer in a cap'n proto message.
/// </summary>
/// <param name="Offset">The offset, in words from the end of the pointer to the start of the struct's data section. Signed.</param>
/// <param name="ElementSize">The size of each element in the list.</param>
/// <param name="Size">
///     The size of the list.
///     For all values where ElementSize is not 7, the size is the number of elements in the list.
///     For ElementSize 7, the size is the number of words in the list, not including the tag word that prefixes the list content.
/// </param>
public readonly record struct ListPointer(int Offset, ListElementType ElementSize, uint Size)
{
    public bool IsComposite => this.ElementSize == ListElementType.Composite;
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
public readonly record struct FarPointer(bool IsDoubleFar, uint Offset, uint SegmentId);

public readonly record struct CapabilityPointer(int CapabilityTableOffset);

/// <summary>
/// Sum type for the different pointer types in cap'n proto message.
/// This is implemented as a struct with a union-like layout to avoid boxing.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct MessagePointer
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

    public MessagePointer(StructPointer structPointer)
    {
        this.tag = PointerType.Struct;
        this.structPointer = structPointer;
    }

    public MessagePointer(ListPointer listPointer)
    {
        this.tag = PointerType.List;
        this.listPointer = listPointer;
    }

    public MessagePointer(FarPointer farPointer)
    {
        this.tag = PointerType.Far;
        this.farPointer = farPointer;
    }

    public MessagePointer(CapabilityPointer capabilityPointer)
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

    public static implicit operator MessagePointer(StructPointer v) => new(v);
    public static implicit operator MessagePointer(ListPointer v) => new(v);
    public static implicit operator MessagePointer(FarPointer v) => new(v);
    public static implicit operator MessagePointer(CapabilityPointer v) => new(v);

    public static implicit operator StructPointer?(MessagePointer self) => self.IsStruct ? self.structPointer : null;
    public static implicit operator ListPointer?(MessagePointer self) => self.IsList ? self.listPointer : null;
    public static implicit operator FarPointer?(MessagePointer self) => self.IsFar ? self.farPointer : null;
    public static implicit operator CapabilityPointer?(MessagePointer self) => self.IsCapability ? self.capabilityPointer : null;
}