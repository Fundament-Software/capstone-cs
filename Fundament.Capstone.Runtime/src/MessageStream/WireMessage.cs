namespace Fundament.Capstone.Runtime.MessageStream;

/// <summary>
/// Light wrapper around an array of WireMessageSegments representing a wire message.
/// </summary>
/// <param name="Segments"></param>
public readonly record struct WireMessage(Word[][] Segments)
{
    public Word[] this[int index] => this.Segments[index];

    public Word[] GetSegmentContents(int index) => this.Segments[index];

    public WireSegmentSlice Slice(int segmentId, Range range) => new(this, segmentId, range);
}