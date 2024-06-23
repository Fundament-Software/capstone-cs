namespace Fundament.Capstone.Runtime;

using Fundament.Capstone.Runtime.Exceptions;

public sealed class InvalidPointerTypeException(
    Word word,
    Index? index = null,
    string? message = null) : DecodeException(word, index, message)
{
}