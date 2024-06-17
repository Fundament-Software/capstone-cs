namespace Fundament.Capstone.Runtime.Logging;

using Fundament.Capstone.Runtime.MessageStream;

using Microsoft.Extensions.Logging;

internal partial class LogMessages
{
    internal static void LogPointerTraversal<TCap, TSelf>(
        this ILogger<BaseReader<TCap, TSelf>> logger,
        WirePointer listPointer,
        int segmentId,
        int traversalCounter)
        where TSelf : BaseReader<TCap, TSelf> =>
        LogListPointerTraversal(logger, listPointer, segmentId, traversalCounter);

    [LoggerMessage(
        EventName = "PointerTraversal",
        Level = LogLevel.Trace,
        Message = "Traversed {listPointer} at segment {segmentId}. Traversal counter: {traversalCounter}.")]
    private static partial void LogListPointerTraversal(this ILogger logger, WirePointer listPointer, int segmentId, int traversalCounter);
}