namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics; 

using Microsoft.Extensions.Logging;

public sealed class StructReader<TCap>(WireSegmentSlice dataSection, WireSegmentSlice pointerSection, ILogger<StructReader<TCap>> logger) : IStructReader<TCap>
{
    private readonly WireSegmentSlice dataSection = dataSection;

    private readonly WireSegmentSlice pointerSection = pointerSection;

    private readonly ILogger<StructReader<TCap>> logger = logger;

    internal StructReader(WireMessage segment, int segmentId, StructPointer structPointer, ILogger<StructReader<TCap>> logger) 
        : this(segment.Slice(segmentId, structPointer.DataSectionRange), segment.Slice(segmentId, structPointer.PointerSectionRange), logger) {}

    public void ReadVoid(int offset)
    {
        // TODO: Write this implementation
        // All ReadVoid should do is increment the traversal limit counter by one word.
    }

    public T ReadData<T>(int offset, T defaultValue) where T : unmanaged, IBinaryNumber<T> => 
        this.dataSection.GetBySizeAlignedOffset<T>(offset) ^ defaultValue;

    public bool ReadBool(int offset, bool defaultValue) => this.dataSection.GetBitByOffset(offset) ^ defaultValue;
}