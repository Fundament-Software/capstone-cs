namespace UseBackingField.Tests;

using Microsoft.CodeAnalysis.CSharp;

public class UseBackingFieldSnapshotTests
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

        var driver = GeneratorDriver(source);
        await Verify(driver);
    }

    public static CSharpGeneratorDriver GeneratorDriver(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree]
        );

        var generator = new BackingFieldsGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGenerators(compilation);

        return driver;
    }
}
