namespace Tests.Fundament.Capstone.Runtime;

using System.CodeDom;
using System.Diagnostics;

using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;

using global::Fundament.Capstone.Runtime.MessageStream;

using Microsoft.VisualBasic;

using Xunit.Abstractions;

public class CompilerSchemaDecodeTest(ITestOutputHelper outputHelper)
{
    private const string SchemaFilePath = "./resources/schema.capnp";

    [Fact]
    public async Task DecodeCompilerOutput()
    {
        // Start the compiler and get it's output into a message.
        var message = await this.RunCompiler();
        var rootReader = message.NewRootReader(outputHelper.ToLoggerFactory());


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

    
}