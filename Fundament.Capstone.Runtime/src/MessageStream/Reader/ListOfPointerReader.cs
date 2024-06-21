namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// A ListReader specialized for reading List(T) where T is not known.
/// </summary>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public sealed class ListOfPointerReader<TCap> : AbstractBaseListReader<IReader<TCap>, TCap, ListOfPointerReader<TCap>>
{
    internal ListOfPointerReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
        InvalidListKindException.ThrowIfListKindIsNot(pointer, ListElementType.EightBytesPointer, pointerIndex);
    }

    public override IReader<TCap> this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);
            var pointer = WirePointer.Decode(this.ListSlice[index]);
            return pointer.Traverse<TCap>(this.SharedReaderState, this.SegmentId, this.ListSlice.Offset + index);
        }
    }
}