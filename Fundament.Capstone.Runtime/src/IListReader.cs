namespace Fundament.Capstone.Runtime;

/// <summary>
/// Defines an object that can read values from a list in a Cap'n Proto message.
/// For primitive types, this will be a list of values. For composite types or pointers, this will be a list of readers.
/// </summary>
/// <typeparam name="T">The type of the values in the list.</typeparam>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
public interface IListReader<T, TCap> : IReadOnlyList<T>, IReader<TCap>
where T : notnull
{
}