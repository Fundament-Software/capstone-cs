using System.Reflection;
using System.Xml.Serialization;

using Capnp;

if (!Console.IsInputRedirected)
{
    Console.WriteLine($"Capstone C# Compiler Plugin Version {Assembly.GetExecutingAssembly().GetName().Version}");
    Console.WriteLine("Expecting binary-ecoded code generation from stdin...");
}

var input = Console.OpenStandardInput();
if (input == null)
{
    Console.Error.WriteLine("Failed to open stdin.");
    return 1;
}

var segments = Framing.ReadSegments(input);
var root = DeserializerState.CreateRoot(segments);
var reader = Capnp.Schema.CodeGeneratorRequest.READER.create(root);

return 0;