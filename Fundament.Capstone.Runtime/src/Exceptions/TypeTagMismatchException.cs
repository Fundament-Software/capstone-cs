namespace Fundament.Capstone.Runtime.Exceptions;

using System.Data.Common;
using System.Globalization;
using System.Text;

/// <summary>
/// Exception thrown when a word decoded as a pointer has a type tag that does not match the expected type.
/// </summary>
public class TypeTagMismatchException(Word word, byte expectedType, Index? index = null) : DecodeException(word, index)
{
    public byte AcutalTypeTag => (byte)(this.Word & 3);

    public byte ExpectedTypeTag => expectedType;

    public override string Message
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(CultureInfo.CurrentCulture, $"Expected word 0x{this.Word:X}");

            if (this.Index.HasValue)
            {
                sb.Append(CultureInfo.CurrentCulture, $" at index {this.Index}");
            }

            sb.Append(CultureInfo.CurrentCulture, $" to have type tag {this.ExpectedTypeTag}, but it has {this.Word & 3}");

            return sb.ToString();
        }
    }
}
