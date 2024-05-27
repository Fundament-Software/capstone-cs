namespace Fundament.Capstone.Runtime.MessageStream;

public sealed record class SharedReaderState(WireMessage MessageFrame, int MessageTraversalCount);