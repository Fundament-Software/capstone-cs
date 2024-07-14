namespace UseBackingField;

using System.Collections;
using System.Diagnostics.CodeAnalysis;

internal static class EquatableArray
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> enumerable)
    where T : IEquatable<T> => new(enumerable.ToArray());
}

/// <summary>
/// A wrapper around array that uses value equality for its elements.
/// </summary>
internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
where T : IEquatable<T>
{
    private readonly T[] array = array;

    public ref readonly T this[int index] => ref this.array[index];

    public bool IsEmpty => this.array.Length == 0;

    public bool Equals(EquatableArray<T> other) => this.AsSpan().SequenceEqual(other.AsSpan());

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquatableArray<T> other && Equals(this, other);

    public override int GetHashCode()
    {
        if (this.IsEmpty)
        {
            return 0;
        }

        HashCode hashCode = default;
        foreach (var item in this.array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    public ReadOnlySpan<T> AsSpan() => this.array.AsSpan();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)this.array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.array.GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}