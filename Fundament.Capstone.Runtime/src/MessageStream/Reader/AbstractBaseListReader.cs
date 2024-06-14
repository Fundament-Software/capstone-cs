namespace Fundament.Capstone.Runtime;

using System.Collections;
using System.Collections.Generic;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;
using Fundament.Capstone.Runtime.MessageStream;

public abstract class AbstractBaseListReader<T, TCap, TSelf> : BaseReader<TCap, TSelf>, IListReader<T, TCap>
where TSelf : AbstractBaseListReader<T, TCap, TSelf>
{
    private protected AbstractBaseListReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state)
    {
        this.SegmentId = segmentId;
        this.ListSlice = this.EvaluatePointerTarget(segmentId, pointerIndex, pointer);

        if (pointer.ElementSize == ListElementType.Void)
        {
            this.SharedReaderState.TraversalCounter += (int)pointer.Size;
        }
        else
        {
            this.SharedReaderState.TraversalCounter += (int)pointer.SizeInWords;
        }
    }

    public int Count { get; protected init; }

    protected int SegmentId { get; init; }

    private protected WireSegmentSlice ListSlice { get; init; }

    public abstract T this[int index] { get; }

    public IEnumerator<T> GetEnumerator() => new ListReaderEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new ListReaderEnumerator(this);

    /// <summary>
    /// Evaluates the target of a pointer and returns the slice of the wire message that the pointer points to.
    /// </summary>
    /// <param name="segmentId">The segment id of the pointer.</param>
    /// <param name="pointerIndex">The index of the pointer in the segment.</param>
    /// <param name="pointer">The pointer to evaluate.</param>
    /// <returns>The slice of the wire message that the pointer points to.</returns>
    /// <exception cref="PointerOffsetOutOfRangeException">Thrown when the pointer points outside the bounds of the segment.</exception>
    private protected virtual WireSegmentSlice EvaluatePointerTarget(int segmentId, Index pointerIndex, ListPointer pointer)
    {
        var startIndex = pointerIndex.AddOffset(pointer.Offset);
        var endIndex = startIndex.AddOffset((int)pointer.SizeInWords);

        try
        {
            return this.SharedReaderState.WireMessage.Slice(segmentId, startIndex..endIndex);
        }
        catch (IndexOutOfRangeException e)
        {
            throw new PointerOffsetOutOfRangeException(
                this.SharedReaderState.WireMessage[segmentId][pointerIndex],
                pointer.Offset,
                pointerIndex,
                e
            );
        }
    }

    private class ListReaderEnumerator(AbstractBaseListReader<T, TCap, TSelf> reader) : IEnumerator<T>, IEnumerator
    {
        private readonly AbstractBaseListReader<T, TCap, TSelf> reader = reader;
        private int index = -1;

        public T Current
        {
            get
            {
                Guard.IsInRangeFor(this.index, this.reader);
                return this.reader[this.index];
            }
        }

        object? IEnumerator.Current => this.Current;

        public bool MoveNext() => ++this.index < this.reader.Count;

        public void Reset() => this.index = -1;

        public void Dispose()
        {
        }
    }
}