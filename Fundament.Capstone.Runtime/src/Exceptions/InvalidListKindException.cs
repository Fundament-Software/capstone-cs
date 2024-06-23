namespace Fundament.Capstone.Runtime.Exceptions;

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Fundament.Capstone.Runtime.MessageStream;

public sealed class InvalidListKindException(
    Word word,
    ListElementType invalidListKind,
    IReadOnlySet<ListElementType> expectedKinds,
    Index? index = null,
    Exception? innerException = null) : DecodeException(word, index, null, innerException)
{
    public InvalidListKindException(Word word, ListElementType invalidListKind, ListElementType expectedKind, Index? index = null, Exception? innerException = null)
        : this(word, invalidListKind, new HashSet<ListElementType> { expectedKind }, index, innerException)
    {
    }

    public ListElementType InvalidListKind { get; } = invalidListKind;

    public IReadOnlySet<ListElementType> ExpectedKinds { get; } = expectedKinds;

    public override string Message
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"In word {this.Word:X}");

            if (this.Index.HasValue)
            {
                sb.Append(CultureInfo.InvariantCulture, $" at index {this.Index}");
            }

            sb.Append(CultureInfo.InvariantCulture, $" the list kind is {this.InvalidListKind}");

            if (this.ExpectedKinds.Count == 1)
            {
                sb.Append(CultureInfo.InvariantCulture, $", expected {this.ExpectedKinds.First()}");
            }
            else
            {
                sb.Append(", expected one of: ");
                sb.AppendJoin(", ", this.ExpectedKinds);
            }

            return sb.ToString();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfListKindIsNot(ListPointer pointer, ListElementType expectedKind, Index? index = null)
    {
        if (pointer.ElementSize != expectedKind)
        {
            throw new InvalidListKindException(pointer.AsWord, pointer.ElementSize, expectedKind, index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfListKindIsNot(ListPointer pointer, IReadOnlySet<ListElementType> expectedKinds, Index? index = null)
    {
        if (!expectedKinds.Contains(pointer.ElementSize))
        {
            throw new InvalidListKindException(pointer.AsWord, pointer.ElementSize, expectedKinds, index);
        }
    }
}