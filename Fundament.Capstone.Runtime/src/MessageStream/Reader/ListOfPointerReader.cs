namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// A ListReader specialized for reading List(T) where T is not known.
/// </summary>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public sealed class ListOfPointerReader<TCap> : AbstractBaseListReader<AnyReader<TCap>, TCap, ListOfPointerReader<TCap>>
{
    internal ListOfPointerReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
        InvalidListKindException.ThrowIfListKindIsNot(pointer, ListElementType.EightBytesPointer, pointerIndex);
    }

    public override AnyReader<TCap> this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);
            var pointer = WirePointer.Decode(this.ListSlice[index]);
            return AnyReader<TCap>.TraversePointer(pointer, this.SharedReaderState, this.SegmentId, this.ListSlice.Offset + index);
        }
    }
}