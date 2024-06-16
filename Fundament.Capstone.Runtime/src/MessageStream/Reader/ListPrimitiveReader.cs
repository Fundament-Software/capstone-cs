namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics;

using CommunityToolkit.Diagnostics;

/// <summary>
/// A reader for a list of primitive values. This shoud NOT be used for bools. Use <see cref="ListBooleanReader"/> instead.
/// </summary>
/// <typeparam name="T">The type of the primitive values in the list. This should not be `bool`.</typeparam>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public class ListPrimitiveReader<T, TCap> : AbstractBaseListReader<T, TCap, ListPrimitiveReader<T, TCap>>
where T : unmanaged, IBinaryNumber<T>
{
    internal ListPrimitiveReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
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

public class ListPrimitiveReader<T> : ListPrimitiveReader<T, Unit>
where T : unmanaged, IBinaryNumber<T>
{
    internal ListPrimitiveReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
    }
}