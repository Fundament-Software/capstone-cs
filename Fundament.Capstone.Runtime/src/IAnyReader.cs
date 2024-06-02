namespace Fundament.Capstone.Runtime;

/// <summary>
/// Interface for a discriminated union of *Reader types.
/// </summary>
public interface IAnyReader<TCap>
{
    public IStructReader<TCap> AsStructReader();
}