namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics;

using Microsoft.Extensions.Logging;

public sealed class StructReader(WireSegmentSlice dataSection, WireSegmentSlice pointerSection, ILogger<StructReader> logger) : IStructReader
{
    private readonly WireSegmentSlice dataSection = dataSection;

    private readonly WireSegmentSlice pointerSection = pointerSection;

    private readonly ILogger<StructReader> logger = logger;

    public StructReader(WireMessageSegment segment, StructPointer structPointer, ILogger<StructReader> logger) 
        : this(segment[structPointer.DataSectionRange], segment[structPointer.PointerSectionRange], logger) {}

    public void ReadVoid(int offset)
    {
        // TODO: Write this implementation
        // All ReadVoid should do is increment the traversal limit counter by one word.
    }

    public T ReadData<T>(int offset, T defaultValue) where T : unmanaged, IBinaryNumber<T> => 
        this.dataSection.GetBySizeAlignedOffset<T>(offset) ^ defaultValue;

    public bool ReadBool(int offset, bool defaultValue) => this.dataSection.GetBitByOffset(offset) ^ defaultValue;
}