namespace Fundament.Capstone.Compiler.Model;

using System.Collections.Immutable;
using System.Security.Cryptography;

using Capnp.Schema;

using Microsoft.Extensions.Logging;

public class ModelBuilder(CodeGeneratorRequest.READER reader, ILogger<ModelBuilder>? logger = null)
{
    private readonly CodeGeneratorRequest.READER reader = reader;

    private readonly ILogger<ModelBuilder>? logger = logger;

    public ImmutableDictionary<Word, Node.READER> NodeReaderById { get; } = reader.Nodes.ToImmutableDictionary(node => node.Id);

    public ImmutableDictionary<Word, Word> NodeParents { get; } = GatherNodeParents(reader);

    public ImmutableDictionary<Word, string> NodeNames { get; } = reader.Nodes.SelectMany(node => node.NestedNodes).ToImmutableDictionary(node => node.Id, node => node.Name);

    public ImmutableDictionary<Word, CodeGeneratorRequest.RequestedFile.READER> RequestedFiles { get; } = reader.RequestedFiles.ToImmutableDictionary(file => file.Id);

    public void BuildNodeTree()
    {
        var internalNodes = this.NodeParents.Values.ToHashSet();
        var leafNodes = this.NodeReaderById.Where(kvp => !internalNodes.Contains(kvp.Key)).Select(kvp => kvp.Value);

        // Construct the nodes from the bottom up using a queue, instead of using recursion
        // because C# doesn't have tail-call optimization (the CLR does! but C# doesn't expose it)
        var constructedNodes = new Dictionary<Word, INode>();
        var nodeQueue = new Queue<Node.READER>(leafNodes);
        while (nodeQueue.TryDequeue(out var reader))
        {
            var node = this.ConstructNode(reader, constructedNodes);
            constructedNodes[node.Id] = node;

            if (node.ParentId != 0 && !constructedNodes.ContainsKey(node.ParentId))
            {
                nodeQueue.Enqueue(this.NodeReaderById[node.ParentId]);
            }
        }
    }

    private static ImmutableDictionary<Word, Word> GatherNodeParents(CodeGeneratorRequest.READER reader, ILogger<ModelBuilder>? logger = null)
    {
        var parents = reader.Nodes.ToDictionary(node => node.Id, node => node.ScopeId);

        var nestedNodes = reader.Nodes.SelectMany(node => node.NestedNodes.Select(nested => (node, nested)));
        foreach (var (parentNode, nestedNode) in nestedNodes) {
            if (parents.TryGetValue(nestedNode.Id, out var existingParentId) && existingParentId != parentNode.Id) {
                logger?.LogCriticalParentMismatch(parentNode.Id, nestedNode.Id, existingParentId);
                throw new InvalidOperationException($"Parent mismatch: Node {parentNode.Id} claims to be the parent of {nestedNode.Id}, but {nestedNode.Id} claims {existingParentId} as its parent.");
            }

            parents[nestedNode.Id] = parentNode.Id;
        }

        return parents.ToImmutableDictionary();
    }

    private INode ConstructNode(Node.READER nodeReader, IReadOnlyDictionary<Word, INode> constructedNodes) =>
        nodeReader.which switch
        {
            Node.WHICH.File => File.Build(this, nodeReader, constructedNodes),
        };

}