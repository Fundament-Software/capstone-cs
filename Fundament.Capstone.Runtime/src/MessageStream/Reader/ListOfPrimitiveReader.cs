namespace Fundament.Capstone.Runtime.MessageStream;

using System.Collections.Frozen;
using System.Numerics;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// A reader for a list of primitive values. This shoud NOT be used for bools. Use <see cref="ListBooleanReader"/> instead.
/// </summary>
/// <typeparam name="T">The type of the primitive values in the list. This should not be `bool`.</typeparam>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public sealed class ListOfPrimitiveReader<T, TCap> : AbstractBaseListReader<T, TCap, ListOfPrimitiveReader<T, TCap>>
where T : unmanaged, IBinaryNumber<T>
{
    private static readonly FrozenSet<ListElementType> AllowedListKinds = new HashSet<ListElementType>
    {
        ListElementType.Void,
        ListElementType.Byte,
        ListElementType.TwoBytes,
        ListElementType.FourBytes,
        ListElementType.EightBytes,
    }.ToFrozenSet();

    internal ListOfPrimitiveReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
        InvalidListKindException.ThrowIfListKindIsNot(pointer, AllowedListKinds, pointerIndex);
    }

    public override T this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);
            return this.ListSlice.GetBySizeAlignedOffset<T>(index);
        }
    }

    private protected override int CalculateCount(ListPointer pointer) => (int)pointer.Size;
}