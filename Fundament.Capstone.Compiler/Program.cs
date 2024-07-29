namespace Fundament.Capstone.Compiler;

using System.Reflection;

using Capnp;
using Capnp.Schema;

public static class Program 
{
    public static readonly string VersionLine = $"Capstone C# Compiler Plugin Version {Assembly.GetExecutingAssembly().GetName().Version}";

    public static int Main(string[] args)
    {
        var inputStream = ParseArgs(args);
        if (inputStream is null) {
            return 0;
        }

        var segments = Framing.ReadSegments(inputStream);
        var root = DeserializerState.CreateRoot(segments);
        var reader = CodeGeneratorRequest.READER.create(root);

        foreach (var node in reader.Nodes) {
            Console.WriteLine($"Node: {node.DisplayName}");
        }

        return 0;
    }

    private static Stream? ParseArgs(string[] args) => args switch
    {
        { Length: >= 2 } => throw new ArgumentException("Too many arguments"),
        ["--version"] => PrintVersion(),
        ["--help"] => PrintHelp(),
        [ var filePath ] => File.OpenRead(filePath),
        _ => OpenStdin(),
    };

    private static Stream? PrintVersion()
    {
        Console.WriteLine(VersionLine);
        return null;
    }

    private static Stream? PrintHelp()
    {
        Console.WriteLine($"""  
            {VersionLine}
            Normally invoked by the Capstone compiler, this program reads a binary-encoded code generation request from a file or stdin if no file is provided.
            Usage: capstone-csharp-compiler [path]
            --version  Print version information
            --help     Print this help");
            [path]     Read binary-encoded code generation from file at [path]
        """);
        return null;
    }

    private static Stream OpenStdin()
    {
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine(VersionLine);
            Console.WriteLine("Expecting binary-encoded code generation from stdin...");
        }

        return Console.OpenStandardInput();
    }
}