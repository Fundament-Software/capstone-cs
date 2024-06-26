namespace Fundament.Capstone.Runtime;

using System.Diagnostics.Contracts;

using CommunityToolkit.Diagnostics;

internal static class IndexExtensions
{
    /// <summary>
    /// Adds an offset to an index.
    /// This respects the direction of the index. If the index is from the end, then the offset is substracted from the index.
    /// It is the caller's responsibility to ensure that the resulting index is within bounds of the collection.
    /// </summary>
    [Pure]
    public static Index AddOffset(this Index self, int offset) =>
        self.IsFromEnd 
            ? ^(self.Value - offset)
            : self.Value + offset;

    /// <summary>
    /// Constructs a Range with size <paramref name="length"/> and starting index of itself.
    /// </summary>
    public static Range StartRange(this Index self, int length)
    {
        Guard.IsGreaterThanOrEqualTo(length, 0);
        return self.IsFromEnd
            ? self..^(self.Value - length)
            : self..(self.Value + length);
    }
}