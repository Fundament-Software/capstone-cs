namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

public class ListBooleanReader<TCap> : AbstractBaseListReader<bool, TCap, ListBooleanReader<TCap>>
{
    internal ListBooleanReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
    }

    public override bool this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);
            return this.ListSlice.GetBitByOffset(index);
        }
    }

    private protected override int CalculateCount(ListPointer pointer) => (int)pointer.Size;
}

public class ListBooleanReader : ListBooleanReader<Unit>
{
    internal ListBooleanReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
    }
}
