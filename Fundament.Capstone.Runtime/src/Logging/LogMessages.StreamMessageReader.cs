namespace Fundament.Capstone.Runtime.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Fast logging for the <see cref="StreamMessageReader"/> class.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(
        EventName = "ReadSegmentTableHeader",
        Level = LogLevel.Debug,
        Message = "Read segment table header: {SegmentCount} segments, first segment size: {FirstSegmentSize}")]
    internal static partial void LogSegmentTableHeader(this ILogger<StreamMessageReader> logger, uint segmentCount, uint firstSegmentSize);

    [LoggerMessage(
        EventName = "SegmentExceedesThreshold",
        Level = LogLevel.Warning,
        Message = "Segment count {SegmentCount} exceeds threshold {Threshold}, skipping message.")]
    internal static partial void LogSegmentExceedesThreshold(this ILogger<StreamMessageReader> logger, uint segmentCount, uint threshold);

    internal static partial void LogSocketEcxeption(this ILogger<StreamMessageReader> logger, );
}