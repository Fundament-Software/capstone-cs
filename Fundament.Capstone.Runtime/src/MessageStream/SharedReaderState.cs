namespace Fundament.Capstone.Runtime.MessageStream;

using CommunityToolkit.Diagnostics;

using Fundament.Capstone.Runtime.Exceptions;

internal sealed class SharedReaderState
{
    private int traversalCounter;

    public required WireMessage WireMessage { get; init; }

    public required int TraversalLimitInWords { get; init; } = 8 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the traversal counter.
    /// This can only be set to a value greater than the current value, otherwise an exception is thrown.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the new value is less than the current value.</exception>
    /// <exception cref="TraversalLimitException">Thrown when the traversal limit is exceeded.</exception>
    public int TraversalCounter
    {
        get => this.traversalCounter;
        set
        {
            Guard.IsGreaterThan(value, this.traversalCounter);
            TraversalLimitException.ThrowIfExceededLimit(value, this.TraversalLimitInWords);
            this.traversalCounter = value;
        }
    }
}