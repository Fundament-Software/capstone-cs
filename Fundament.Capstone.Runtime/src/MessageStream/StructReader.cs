namespace Fundament.Capstone.Runtime.MessageStream;

using Microsoft.Extensions.Logging;

public sealed record class StructReader : IStructReader
{
    private readonly ArraySegment<Word> dataSection;
    
    private readonly ArraySegment<Word> pointerSection;

    private readonly ILogger<StructReader> logger;

    public StructReader(WireMessageSegment segment, StructPointer pointer, ILogger<StructReader> logger)
    {
        this.logger = logger;

        this.dataSection = segment.Contents.Slice(pointer.DataSection);
        this.pointerSection = segment.Contents.Slice(pointer.PointerSection);
    }
}