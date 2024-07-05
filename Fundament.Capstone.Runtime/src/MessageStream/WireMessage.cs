namespace Fundament.Capstone.Runtime.MessageStream;

using Microsoft.Extensions.Logging;

/// <summary>
/// Light wrapper around an array of Word arrays representing a wire message.
/// </summary>
/// <param name="Segments">The segments that make up the message.</param>
public readonly record struct WireMessage(Word[][] Segments)
{
    internal WirePointer RootPointer => WirePointer.Decode(this.Segments[0][0]);

    public Word[] this[int index] => this.Segments[index];

    /// <summary>
    /// Gets the contents of a segments. This is the same as accessing the index of the WireMessage.
    /// </summary>
    /// <param name="index">The index of the segment.</param>
    /// <returns>The segment at the specified index.</returns>
    public Word[] GetSegmentContents(int index) => this.Segments[index];

    public WireSegmentSlice Slice(int segmentId, Range range) => new(this, segmentId, range);

    /// <summary>
    /// Creates a new reader for the root object of the message.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    /// <typeparam name="TCap">The type of the capability table.</typeparam>
    /// <returns>A reader for the root object of the message. This is almost always an instance of <see cref="StructReader{TCap}"/>. </returns>
    /// <remarks>
    /// This method is the "entry point" into the reading API.
    /// Every invocation of this method creates a new context for the reader which is shared among all readers created from it.
    /// </remarks>
    public IReader<TCap> NewRootReader<TCap>(ILoggerFactory loggerFactory) =>
        this.RootPointer.Traverse<TCap>(
            new SharedReaderState
            {
                WireMessage = this,
                LoggerFactory = loggerFactory,
            },
            0,
            0
        );

    public IReader<Unit> NewRootReader(ILoggerFactory loggerFactory) =>
        this.NewRootReader<Unit>(loggerFactory);
}