namespace Fundament.Capstone.Runtime.MessageStream;

/// <summary>
/// Represents the encoding value of the list type in a list pointer.
/// </summary>
public enum ListElementType : byte
{
    /// <summary>
    /// A list of voids, i.e a list of a type with only value.
    /// </summary>
    Void = 0,

    /// <summary>A list of bits.</summary>
    Bit = 1,

    /// <summary>A list of bytes.</summary>
    Byte = 2,

    /// <summary>A list of two-byte values, corresponding to <see cref="short"/>.</summary>
    TwoBytes = 3,

    /// <summary>A list of four-byte values, corresponding to <see cref="int"/>.</summary>
    FourBytes = 4,

    /// <summary>A list of eight-byte values, corresponding to <see cref="long"/>.</summary>
    EightBytes = 5,

    /// <summary>A list of pointers.</summary>
    EightBytesPointer = 6,

    /// <summary>A list of composite values, which are usually structs.</summary>
    Composite = 7,
}