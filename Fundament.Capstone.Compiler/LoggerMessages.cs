namespace Fundament.Capstone.Compiler;

using Fundament.Capstone.Compiler.Model;

using Microsoft.Extensions.Logging;

internal static partial class LoggerMessages
{
    [LoggerMessage(
        LogLevel.Critical,
        "Parent mismatch: Node {ParentId} claims to be the parent of {NestedNodeId}, but {NestedNodeId} claims {SecondParentId} as its parent. This is an error with the data recieved by this program."
    )]
    public static partial void LogCriticalParentMismatch(this ILogger<ModelBuilder> logger, Word parentId, Word nestedNodeId, Word secondParentId);
}