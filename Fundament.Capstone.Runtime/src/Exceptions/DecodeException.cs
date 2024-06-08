namespace Fundament.Capstone.Runtime.Exceptions;

/// <summary>
/// Base class for exceptions thrown during decoding.
/// </summary>
public abstract class DecodeException(Word word, Index? index = null) : Exception()
{
    /// <summary>
    /// The word that caused the exception.
    /// </summary>
    public Word Word => word;

    /// <summary>
    /// The location of the word in it's segment.
    /// </summary>
    public Index? Index => index;
}