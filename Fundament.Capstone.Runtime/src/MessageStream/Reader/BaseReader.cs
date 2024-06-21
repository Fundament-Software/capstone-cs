namespace Fundament.Capstone.Runtime.MessageStream;

using Microsoft.Extensions.Logging;

// Note:
// This class is public only because it's derived types are public and derived types cannot have more permissive access than their base type.
// We could get around this by making the derived types internal, and then defining public wrapper classes around the internal types.
// But that's a lot more work for no real benefit -- the internal constructor means that the class cannot be derived from outside the assembly anyways.

/// <summary>
/// The base class for all reader types.
/// The constructor for this class is internal and cannot be derived from outside the assembly.
/// </summary>
/// <typeparam name="TCap">The type of the capability table imbued in the reader. Unused for now.</typeparam>
/// <typeparam name="TSelf">The type of the dervied class. Used for logging.</typeparam>
public abstract class BaseReader<TCap, TSelf> : IReader<TCap>
where TSelf : BaseReader<TCap, TSelf>
{
    private protected BaseReader(SharedReaderState state)
    {
        this.Logger = state.LoggerFactory.CreateLogger<TSelf>();
        this.SharedReaderState = state;
    }

    protected ILogger<TSelf> Logger { get; init; }

    private protected SharedReaderState SharedReaderState { get; init; }
}