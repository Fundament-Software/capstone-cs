namespace Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Fast logging for the <see cref="StreamMessageReader"/> class.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(
        EventName = "ReadSegmentTable",
        Level = LogLevel.Debug,
        Message = "Read segment table header: {SegmentCount} segments, {MessageSize} words.")]
    internal static partial void LogSegmentTable(this ILogger<StreamMessageReader> logger, uint segmentCount, uint messageSize);

    [LoggerMessage(
        EventName = "SegmentExceedesThreshold",
        Level = LogLevel.Warning,
        Message = "Segment count {SegmentCount} exceeds threshold {Threshold}, skipping message.")]
    internal static partial void LogSegmentExceedesThreshold(this ILogger<StreamMessageReader> logger, uint segmentCount, uint threshold);

    [LoggerMessage(
        EventName = "SkipMessage",
        Level = LogLevel.Debug,
        Message = "Skipped message of {MessageSize} words and {SegmentCount} segments.")]
    internal static partial void LogSkipMessage(this ILogger<StreamMessageReader> logger, uint messageSize, uint segmentCount);
}