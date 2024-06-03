namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics;

using Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// A reader for a struct in a cap'n proto message.
/// </summary>
/// <typeparam name="TCap">The type of the capability.</typeparam>
/// <remarks>
/// In the case ofthe MessageStream encoding, we consider instantiating a new reader to be a traversal of the pointer.
/// Therefore, instantiating a new reader increments the traversal counter.
/// </remarks>
public sealed class StructReader<TCap> : IStructReader<TCap>
{
    // This class has an invariant we need to maintain:
    // The sharedReaderState.WireMessage is the same instance as dataSection.WireMessage and pointerSection.WireMessage
    // This isn't too hard because SharedReaderState is how we pass around the WireMessage instance between readers.
    private readonly SharedReaderState sharedReaderState;

    private readonly int segmentId;

    private readonly WireSegmentSlice dataSection;

    private readonly WireSegmentSlice pointerSection;

    private readonly ILogger<StructReader<TCap>> logger;

    internal StructReader(
        SharedReaderState sharedReaderState,
        int segmentId,
        StructPointer structPointer,
        ILogger<StructReader<TCap>> logger)
    {
        this.sharedReaderState = sharedReaderState;
        this.segmentId = segmentId;
        this.dataSection = sharedReaderState.WireMessage.Slice(segmentId, structPointer.DataSectionRange);
        this.pointerSection = sharedReaderState.WireMessage.Slice(segmentId, structPointer.PointerSectionRange);
        this.logger = logger;

        this.sharedReaderState.TraversalCounter += this.Size;

        this.logger.LogStructPointerTraversal(structPointer, segmentId, this.sharedReaderState.TraversalCounter);
    }

    public int Size => this.dataSection.Count + this.pointerSection.Count;

    public void ReadVoid(int offset) => this.sharedReaderState.TraversalCounter += 1;

    public T ReadData<T>(int offset, T defaultValue)
    where T : unmanaged, IBinaryNumber<T> =>
        this.dataSection.GetBySizeAlignedOffset<T>(offset) ^ defaultValue;

    public AnyReader<TCap> ReadPointer(int offset) =>
        WirePointer
            .Decode(this.pointerSection.AsSpan(), offset)
            .Match(
                (StructPointer structPointer) => new StructReader<TCap>(this.sharedReaderState, this.segmentId, structPointer, this.logger),
                (ListPointer listPointer) => throw new NotImplementedException(),
                (FarPointer farPointer) => throw new NotImplementedException(),
                (CapabilityPointer capabilityPointer) => throw new NotImplementedException());

    IAnyReader<TCap> IStructReader<TCap>.ReadPointer(int offset) => this.ReadPointer(offset);

    public bool ReadBool(int offset, bool defaultValue) => this.dataSection.GetBitByOffset(offset) ^ defaultValue;
}