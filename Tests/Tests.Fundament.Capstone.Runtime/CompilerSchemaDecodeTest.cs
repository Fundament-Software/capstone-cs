namespace Tests.Fundament.Capstone.Runtime;

using System.Runtime.CompilerServices;

using CliWrap;

using global::Fundament.Capstone.Runtime;
using global::Fundament.Capstone.Runtime.MessageStream;

using Xunit.Abstractions;

public class CompilerSchemaDecodeTest(ITestOutputHelper outputHelper)
{
    private const string SchemaFilePath = "./resources/schema.capnp";

    private static readonly CapnpVersion ExpectedCapnpVersion = new(1, 0, 2);

    [Fact]
    public async Task DecodeCompilerOutput()
    {
        // Start the compiler and get it's output into a message.
        var message = await this.RunCompiler();

        // Can use `capnp compiler ./resources/schema.capnp -ocapnp` to generate the offsets of the schema.
        // The root struct is a CodeGeneratorRequest, which is a struct of 4 pointers.
        var rootReader = message.NewRootReader(outputHelper.ToLoggerFactory()) as StructReader<Unit>;
        rootReader.ShouldNotBeNull();

        // The 2nd pointer is the CapnpVersion struct.
        // This lets us test struct pointer parsing.
        var capnpVersionReader = rootReader.ReadPointer(2) as StructReader<Unit>;
        capnpVersionReader.ShouldNotBeNull();
        CapnpVersion.Deserialize(capnpVersionReader).ShouldBe(ExpectedCapnpVersion);

        // The 0th pointer is a List[Node], which is encoded as a composite list.
        // This lets us test list parsing.
        var nodeListReader = rootReader.ReadPointer(0) as ListOfCompositeReader<Unit>;
        nodeListReader.ShouldNotBeNull();
        nodeListReader.
    }

    private async Task<WireMessage> RunCompiler()
    {
        var ouputStream = new MemoryStream();
        var messageReader = new StreamMessageReader(ouputStream, outputHelper.ToLogger<StreamMessageReader>());

        var result = await Cli.Wrap("capnpc")
            .WithArguments(["-o-", "--verbose", SchemaFilePath])
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => outputHelper.WriteLine($"[capnpc STDERR] {line}\n")))
            .WithStandardOutputPipe(PipeTarget.ToStream(ouputStream))
            .ExecuteAsync();

        ouputStream.Seek(0, SeekOrigin.Begin);
        return await messageReader.ReadMessageAsync();
    }

    private record CapnpVersion(ushort Major, byte Minor, byte Micro)
    {
        public static CapnpVersion Deserialize(IStructReader<Unit> reader) => new(
            reader.ReadUInt16(0, 0),
            reader.ReadUInt8(2, 0),
            reader.ReadUInt8(3, 0)
        );
    }

    private class Node(IStructReader<Unit> reader)
    {
        private ulong? _id;
        private string? _displayName;
        private string? _scopeDisplayName;

        public ulong Id => this._id ??= reader.ReadUInt64(0, 0);
    }
}