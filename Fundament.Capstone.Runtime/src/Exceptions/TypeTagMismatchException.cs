namespace Fundament.Capstone.Runtime.Exceptions;

using System.Data.Common;
using System.Globalization;
using System.Text;

/// <summary>
/// Exception thrown when a word decoded as a pointer has a type tag that does not match the expected type.
/// </summary>
public class TypeTagMismatchException(Word word, byte expectedType, Index? index = null) : DecodeException(word, BuildMessage(word, expectedType, index), index)
{
    public byte AcutalTypeTag => (byte)(this.Word & 3);

    public byte ExpectedTypeTag => expectedType;

    private static string BuildMessage(Word word, byte expectedType, Index? index)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.CurrentCulture, $"Expected word 0x{word:X}");

        if (index.HasValue)
        {
            sb.Append(CultureInfo.CurrentCulture, $" at index {index}");
        }

        sb.Append(CultureInfo.CurrentCulture, $" to have type tag {expectedType}, but it has {word & 3}");

        return sb.ToString();
    }
}
