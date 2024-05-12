namespace Fundament.Capstone.Runtime.Logging;

using Fundament.Capstone.Runtime.BinaryStream;
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
        EventName = "MessageTooBig",
        Level = LogLevel.Warning,
        Message = "Message of {MessageSize} words and {SegmentCount} segments exceeds traversal limit of {TraversalLimitInWords} words.")]
    internal static partial void LogMessageTooBig(this ILogger<StreamMessageReader> logger, uint messageSize, uint segmentCount, ulong traversalLimitInWords);

    [LoggerMessage(
        EventName = "SkipPadding",
        Level = LogLevel.Debug,
        Message = "Skipping {PaddingSize} bytes of padding.")]
    internal static partial void LogSkipPadding(this ILogger<StreamMessageReader> logger, int paddingSize);

    [LoggerMessage(
        EventName = "UnexpectedPaddingValues",
        Level = LogLevel.Warning,
        Message = "Unexpected padding values {PaddingValue}, should be 0 or 4.")]
    internal static partial void LogUnexpectedPaddingValues(this ILogger<StreamMessageReader> logger, uint paddingValue);

    [LoggerMessage(
        EventName = "ReadSegment",
        Level = LogLevel.Trace,
        Message = "Read {SegmentSize} words for segment {SegmentIndex}.")]
    internal static partial void LogReadSegment(this ILogger<StreamMessageReader> logger, int segmentIndex, uint segmentSize);
}