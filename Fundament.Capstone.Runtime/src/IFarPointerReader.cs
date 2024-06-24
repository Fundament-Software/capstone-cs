namespace Fundament.Capstone.Runtime;

/// <summary>
/// Defines an object that can read values from a far pointer in a Cap'n Proto message.
/// </summary>
/// <typeparam name="TCap">The type of the cap table imbued in the reader.</typeparam>
public interface IFarPointerReader<TCap> : IReader<TCap>
{
    IReader<TCap> Reader { get; }
}