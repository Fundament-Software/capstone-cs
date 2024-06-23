namespace Fundament.Capstone.Runtime.MessageStream;

using System.Diagnostics;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

public sealed class ListOfCompositeReader<TCap> : AbstractBaseListReader<StructReader<TCap>, TCap, ListOfCompositeReader<TCap>>
{
    private readonly ushort dataSize;

    private readonly ushort pointerSize;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Debug assertion message")]
    internal ListOfCompositeReader(SharedReaderState state, int segmentId, Index pointerIndex, ListPointer pointer)
        : base(state, segmentId, pointerIndex, pointer)
    {
        InvalidListKindException.ThrowIfListKindIsNot(pointer, ListElementType.Composite, pointerIndex);

        (this.Count, this.dataSize, this.pointerSize) = this.DecodeTag(this.TargetIndex);
        this.TargetIndex = this.TargetIndex.AddOffset(1);

        Debug.Assert(
            this.ListSlice.Count == pointer.SizeInWords,
            $"The size of the list specified in the pointer ({pointer.SizeInWords}) does not match the size of the slice ({this.ListSlice.Count})."
        );
        Debug.Assert(
            pointer.SizeInWords == this.Count * this.StructSize,
            $"""
            This size of the list specified in the pointer ({pointer.SizeInWords}) 
            does not much the size of the list calculated from the tag 
            ({this.Count} elements * {this.StructSize} words = {this.Count * this.StructSize}).
            """
        );
    }

    private int StructSize => this.dataSize + this.pointerSize;

    public override StructReader<TCap> this[int index]
    {
        get
        {
            Guard.IsInRangeFor(index, this);

            var structOffset = this.TargetIndex.AddOffset(index * this.StructSize);
            return new StructReader<TCap>(this.SharedReaderState, this.SegmentId, structOffset, this.dataSize, this.pointerSize);
        }
    }

    private (int Count, ushort DataSize, ushort PointerSize) DecodeTag(Index tagIndex)
    {
        var tagWord = this.WireMessage[this.SegmentId][tagIndex];

        var tag = StructPointer.Decode(tagWord);
        return (tag.Offset, tag.DataSize, tag.PointerSize);
    }
}