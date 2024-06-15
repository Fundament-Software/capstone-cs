namespace Fundament.Capstone.Runtime;

/// <summary>
/// A utility "unit" type that represents a valueless type.
/// </summary>
/// <remarks>
/// This type is useful for representing a valueless type in a type-safe manner.
/// C# has the `void` keyword, but it's not a type and can't be used as a type argument.
/// </remarks>
public class Unit : IComparable
{
    private Unit()
    {
    }

    public static Unit Value { get; } = new Unit();

    public override int GetHashCode() => 0;

    public override bool Equals(object? obj) => obj switch {
        Unit _ => true,
        null => true,
        _ => false,
    };

    public override string ToString() => "Unit";

    public int CompareTo(object? obj) => 0;
}