namespace Fundament.Capstone.Runtime.MessageStream;

using System.Runtime.InteropServices;

using Fundament.Capstone.Runtime.Exceptions;

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