namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics; 

using Microsoft.Extensions.Logging;

public sealed class StructReader<TCap>: IStructReader<TCap>
{
    // This class has an invariant we need to maintain:
    // The sharedReaderState.WireMessage is the same instance as dataSection.WireMessage and pointerSection.WireMessage
    // This isn't too hard because SharedReaderState is how we pass around the WireMessage instance between readers.
    private readonly SharedReaderState sharedReaderState;

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
        this.dataSection = sharedReaderState.WireMessage.Slice(segmentId, structPointer.DataSectionRange);
        this.pointerSection = sharedReaderState.WireMessage.Slice(segmentId, structPointer.PointerSectionRange);
        this.logger = logger;
    }

    public void ReadVoid(int offset) => this.sharedReaderState.IncrementTraversalCounter(1);

    public T ReadData<T>(int offset, T defaultValue) where T : unmanaged, IBinaryNumber<T> => 
        this.dataSection.GetBySizeAlignedOffset<T>(offset) ^ defaultValue;

    public bool ReadBool(int offset, bool defaultValue) => this.dataSection.GetBitByOffset(offset) ^ defaultValue;

}