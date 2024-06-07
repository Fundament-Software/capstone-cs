namespace Fundament.Capstone.Runtime.Exceptions;

using System.Globalization;
using System.Text;

/// <summary>
/// Exception thrown when a pointer has an offset that is out of bounds of the segment.
/// </summary>
/// <param name="word">The word that caused the exception.</param>
/// <param name="index">The location of the word in it's segment.</param>
/// <param name="targetOffset">The erroronuous target of the pointer, as an offset from start of the segment.</param>
public class PointerOffsetOutOfRangeException(Word word, int targetOffset, Index? index = null) : DecodeException(word, ConstructMessage(word, targetOffset, index), index)
{
    public int TargetOffset => targetOffset;

    private static string ConstructMessage(Word word, int targetOffset, Index? index)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.CurrentCulture, $"Offset of pointer {word:X} at index {index} is out of bounds, ");

        if (index.HasValue)
        {
            sb.Append(CultureInfo.CurrentCulture, $"at index {index}");
        }

        sb.Append(" would be {targetOffset} words from the beginning of the segment, which is");

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