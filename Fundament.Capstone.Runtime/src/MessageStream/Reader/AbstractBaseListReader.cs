namespace Fundament.Capstone.Runtime;

using System.Collections;
using System.Collections.Generic;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;
using Fundament.Capstone.Runtime.Logging;
using Fundament.Capstone.Runtime.MessageStream;

public abstract class AbstractBaseListReader<T, TCap, TSelf> : BaseReader<TCap, TSelf>, IListReader<T, TCap>
where TSelf : AbstractBaseListReader<T, TCap, TSelf>
{
    private protected AbstractBaseListReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state)
    {
        this.SegmentId = segmentId;
        this.TargetIndex = pointerIndex.AddOffset(pointer.Offset + 1);
        this.ListSlice = this.EvaluatePointerTarget(segmentId, pointerIndex, pointer);
        this.Count = (int)pointer.Size;

        this.SharedReaderState.TraversalCounter += GetTraveralCounterIncrement(pointer);

        this.Logger.LogPointerTraversal(pointer, segmentId, this.SharedReaderState.TraversalCounter);
    }

    public int Count { get; protected init; }

    protected int SegmentId { get; init; }

    /// <summary>The index of the start of the list in the segment.</summary>
    protected Index TargetIndex { get; init; }

    private protected WireSegmentSlice ListSlice { get; init; }

    public abstract T this[int index] { get; }

    public IEnumerator<T> GetEnumerator() => new ListReaderEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new ListReaderEnumerator(this);

    private static int GetTraveralCounterIncrement(ListPointer pointer) =>
        pointer.ElementSize switch
        {
            ListElementType.Void => (int)pointer.Size,
            _ => (int)pointer.SizeInWords,
        };

    /// <summary>
    /// Evaluates the target of a pointer and returns the slice of the wire message that the pointer points to.
    /// </summary>
    /// <param name="segmentId">The segment id of the pointer.</param>
    /// <param name="pointerIndex">The index of the pointer in the segment.</param>
    /// <param name="pointer">The pointer to evaluate.</param>
    /// <returns>The slice of the wire message that the pointer points to.</returns>
    /// <exception cref="PointerOffsetOutOfRangeException">Thrown when the pointer points outside the bounds of the segment.</exception>
    private WireSegmentSlice EvaluatePointerTarget(int segmentId, Index pointerIndex, ListPointer pointer)
    {
        try
        {
            var targetRange = this.TargetIndex..this.TargetIndex.AddOffset((int)pointer.SizeInWords);
            return this.WireMessage.Slice(segmentId, targetRange);
        }
        catch (IndexOutOfRangeException e)
        {
            throw new PointerOffsetOutOfRangeException(
                this.WireMessage[segmentId][pointerIndex],
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