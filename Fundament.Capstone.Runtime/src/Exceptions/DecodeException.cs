namespace Fundament.Capstone.Runtime.Exceptions;

using System.Globalization;
using System.Text;

/// <summary>
/// Base class for exceptions thrown during decoding.
/// </summary>
public abstract class DecodeException(Word word, Index index, string message) : Exception(message)
{
    /// <summary>
    /// The word that caused the exception.
    /// </summary>
    public Word Word => word;

    /// <summary>
    /// The location of the word in it's segment.
    /// </summary>
    public Index Index => index;
}

/// <summary>
/// Exception thrown when a word decoded as a pointer has a type tag that does not match the expected type.
/// </summary>
public class TypeTagMismatchException(Word word, Index index, byte expectedType) :
    DecodeException(word, index, $"Expected word 0x{word:X} at index {index} to have type tag {expectedType}, but it had {word & 3}")
{
    public byte AcutalTypeTag => (byte)(this.Word & 3);

    public byte ExpectedTypeTag => expectedType;
}

/// <summary>
/// Exception thrown when a pointer has an offset that is out of bounds of the segment.
/// </summary>
/// <param name="word">The word that caused the exception.</param>
/// <param name="index">The location of the word in it's segment.</param>
/// <param name="targetOffset">The erroronuous target of the pointer, as an offset from start of the segment.</param>
public class PointerOffsetOutOfRangeException(Word word, Index index, int targetOffset): DecodeException(word, index, ConstructMessage(word, index, targetOffset))
{
    public int TargetOffset => targetOffset;

    private static string ConstructMessage(Word word, Index index, int targetOffset)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.CurrentCulture, $"Offset of pointer {word:X} at index {index} is out of bounds, would be {targetOffset} words from the beginning of the segment, which is");
        if (targetOffset < 0)
        {
            sb.Append(" before the start of the segment.");
        }
        else
        {
            sb.Append(" after the end of the segment.");
        }
        return sb.ToString();
    }
}