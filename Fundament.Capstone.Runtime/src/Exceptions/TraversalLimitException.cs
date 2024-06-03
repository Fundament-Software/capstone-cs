namespace Fundament.Capstone.Runtime.Exceptions;

public class TraversalLimitException(int traversalCounter, int traversalLimit) :
    Exception($"Traversed {traversalCounter} words, which exceeds the traversal limit of {traversalLimit} words.")
{
    public int TraversalCounter { get; } = traversalCounter;
    public int TraversalLimit { get; } = traversalLimit;

    public static int ThrowIfExceededLimit(int traversalCounter, int traversalLimit) =>
        traversalCounter > traversalLimit ? throw new TraversalLimitException(traversalCounter, traversalLimit) : traversalCounter;
}