namespace UseBackingField.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit.Abstractions;

public class UseBackingFieldSnapshotTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task TestSimpleClass()
    {
        var source = """
        namespace Tests;

        using UseBackingField;

        [GenerateBackingFields]
        public class Test
        {
            public string Property
            {
                get => this.__property;
                set => this.__property = value;
            }
        }
        """;

        var driver = GeneratorDriver(source, outputHelper);
        await Verify(driver);
    }

    public static GeneratorDriver GeneratorDriver(string source, ITestOutputHelper outputHelper)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(FindAssemblyLocationByName("netstandard")),
                MetadataReference.CreateFromFile(FindAssemblyLocationByName("System.Runtime")),
                MetadataReference.CreateFromFile(typeof(GenerateBackingFieldsAttribute).Assembly.Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new BackingFieldsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generatorCompilation, out var diagnostics);

        var result = driver.GetRunResult();
        if (result.Results[0].GeneratedSources.Length <= 0)
        {
            var compilationDiagnostics = generatorCompilation.GetDiagnostics();
            foreach (var diagnostic in compilationDiagnostics)
            {
                outputHelper.WriteLine(diagnostic.ToString());
            }

            throw new InvalidOperationException("No generated sources");
        }

        return driver;
    }

    public static string FindAssemblyLocationByName(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == name).Location;
}