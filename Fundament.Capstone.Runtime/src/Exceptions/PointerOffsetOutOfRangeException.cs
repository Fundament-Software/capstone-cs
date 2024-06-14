namespace Fundament.Capstone.Runtime.Exceptions;

using System.Globalization;
using System.Text;

/// <summary>
/// Exception thrown when a pointer has an offset that is out of bounds of the segment.
/// </summary>
/// <param name="word">The word that caused the exception.</param>
/// <param name="index">The location of the word in it's segment.</param>
/// <param name="targetOffset">The erroronuous target of the pointer, as an offset from start of the segment.</param>
public class PointerOffsetOutOfRangeException(Word word, int targetOffset, Index? index = null, Exception? innerException = null) : DecodeException(word, index, innerException)
{
    public int TargetOffset => targetOffset;

    public override string Message
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(CultureInfo.CurrentCulture, $"Offset of pointer {this.Word:X}");

            if (this.Index.HasValue)
            {
                sb.Append(CultureInfo.CurrentCulture, $"at index {this.Index}");
            }

            sb.Append(CultureInfo.CurrentCulture, $" is out of bounds, would be {this.TargetOffset} words from the beginning of the segment, which is");

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
}