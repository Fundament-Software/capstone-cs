namespace Fundament.Capstone.Runtime.MessageStream;

/// <summary>
/// A ListReader specialized for reading List(T) where T is not known.
/// </summary>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public sealed class ListOfPointerReader<TCap> : AbstractBaseListReader<AnyReader<TCap>, TCap, ListOfPointerReader<TCap>>
{
    internal ListOfPointerReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
    }

    
}