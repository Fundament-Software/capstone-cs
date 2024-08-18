namespace Fundament.Capstone.Compiler.Model;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;

using Capnp.Schema;

public interface INode
{
    public Word Id { get; }

    public string DisplayName { get; }

    uint DisplayNamePrefixLength { get; }

    public Word ParentId { get; }
}

public record File(
    Word Id,
    string DisplayName,
    uint DisplayNamePrefixLength,
    Word ParentId,
    string FileName,
    IImmutableList<Import> Imports,
    IImmutableList<INode> NestedNodes) : INode
{
    public static File Build(ModelBuilder builder, Node.READER nodeReader, IReadOnlyDictionary<Word, INode> nodeInstances)
    {
        Debug.Assert(nodeReader.which == Node.WHICH.File);

        var id = nodeReader.Id;
        var requestedFile = builder.RequestedFiles[id];

        return new(
            id,
            nodeReader.DisplayName,
            nodeReader.DisplayNamePrefixLength,
            nodeReader.ScopeId,
            requestedFile.Filename,
            requestedFile.Imports.Select(imports => new Import(imports.Id, imports.Name)).ToImmutableList(),
            nodeReader.NestedNodes.Select(nested => nodeInstances[nested.Id]).ToImmutableList()
        );
    }
}

public readonly record struct Import(Word Id, string Name);

public readonly record struct Parameter(string Name);