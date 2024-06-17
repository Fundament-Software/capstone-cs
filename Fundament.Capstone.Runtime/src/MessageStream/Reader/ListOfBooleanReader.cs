namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

public sealed class ListOfBooleanReader<TCap> : AbstractBaseListReader<bool, TCap, ListOfBooleanReader<TCap>>
{
    internal ListOfBooleanReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
        InvalidListKindException.ThrowIfListKindIsNot(pointer, ListElementType.Bit, pointerIndex);
    }

    public override bool this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);
            return this.ListSlice.GetBitByOffset(index);
        }
    }
}