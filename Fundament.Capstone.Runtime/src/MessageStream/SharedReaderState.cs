namespace Fundament.Capstone.Runtime.MessageStream;

internal sealed class SharedReaderState
{
    public required WireMessage WireMessage { get; init; }

    public int TraversalCounter { get; private set; }

    public void IncrementTraversalCounter(int increment) => this.TraversalCounter += increment;
}