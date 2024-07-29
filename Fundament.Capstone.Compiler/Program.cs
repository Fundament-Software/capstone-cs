using System.Reflection;

using Capnp;
using Capnp.Schema;

var versionLine = $"Capstone C# Compiler Plugin Version {Assembly.GetExecutingAssembly().GetName().Version}";

// Parse command-line arguments
Stream input;

if (args is [ "--version" ])
{
    Console.WriteLine(versionLine);
    return 0;
}
else if (args is [ "--help"])
{
    Console.WriteLine(versionLine);
    Console.WriteLine("Normally invoked by the Capstone compiler, this program reads a binary-encoded code generation request from a file or stdin if no file is provided.");
    Console.WriteLine("Usage: capstone-csharp-compiler [path]");
    Console.WriteLine("  --version  Print version information");
    Console.WriteLine("  --help     Print this help");
    Console.WriteLine("  [path]     Read binary-encoded code generation from file at [path]");
    return 0;
}
else if (args is [ var pathArg ])
{
    input = File.OpenRead(pathArg);
}
else
{
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine(versionLine);
        Console.WriteLine("Expecting binary-ecoded code generation from stdin...");
    }

    input = Console.OpenStandardInput();
}


if (input == null)
{
    Console.Error.WriteLine("Failed to open stdin.");
    return 1;
}

var segments = Framing.ReadSegments(input);
var root = DeserializerState.CreateRoot(segments);
var reader = CodeGeneratorRequest.READER.create(root);

foreach (var node in reader.Nodes) {
    Console.WriteLine($"Node: {node.DisplayName}");
}

return 0;