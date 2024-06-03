namespace Fundament.Capstone.Runtime.Logging;

using Fundament.Capstone.Runtime.MessageStream;
using Microsoft.Extensions.Logging;

internal static partial class LogMessages
{
    // Workaround for LoggerMessageAttribute not supporting generic types
    internal static void LogStructPointerTraversal<TCap>(this ILogger<StructReader<TCap>> logger, StructPointer structPointer, int segmentId, int traversalCounter) =>
        LogStructPointerTraversal(logger as ILogger, structPointer, segmentId, traversalCounter);

    [LoggerMessage(
        EventName = "StructPointerTraversal",
        Level = LogLevel.Trace,
        Message = "Traversed {structPointer} at segment {segmentId}. Traversal counter: {traversalCounter}.")]
    private static partial void LogStructPointerTraversal(this ILogger logger, StructPointer structPointer, int segmentId, int traversalCounter);
}