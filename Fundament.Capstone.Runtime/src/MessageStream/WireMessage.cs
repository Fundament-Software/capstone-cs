namespace Fundament.Capstone.Runtime.MessageStream;
/// <summary>
/// Light wrapper around a Word array representing a segment of a wire message.
/// </summary>
/// <param name="Contents"></param>
public readonly record struct WireMessageSegment(Word[] Contents)
{
    public static implicit operator WireMessageSegment(Word[] contents) => new(contents);

    public WireSegmentSlice this[Range range] => this.Slice(range);

    public WireSegmentSlice Slice(Range range) => this.Contents.Slice(range);
}

/// <summary>
/// Light wrapper around an array of WireMessageSegments representing a wire message.
/// </summary>
/// <param name="Segments"></param>
public readonly record struct WireMessage(WireMessageSegment[] Segments)
{
    public WireMessageSegment this[int index] => this.Segments[index];

    public Word[] GetSegmentContents(int index) => this.Segments[index].Contents;
}