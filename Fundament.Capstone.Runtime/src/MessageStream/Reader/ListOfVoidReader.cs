namespace Fundament.Capstone.Runtime.MessageStream;

using System;

public sealed class ListOfVoidReader<TCap> : AbstractBaseListReader<Unit, TCap, ListOfVoidReader<TCap>>
{
    internal ListOfVoidReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
    : base(state, segmentId, pointerIndex, pointer)
    {
    }

    public override Unit this[int index] => Unit.Value;
}