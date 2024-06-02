namespace Fundament.Capstone.Runtime.MessageStream;

using System.Diagnostics.CodeAnalysis;

using OneOf;

/// <summary>
/// A discriminated union of *Reader types.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:PartialElementsMustBeDocumented", Justification = "Partial class due to code generation")]
[GenerateOneOf]
public partial class AnyReader<TCap> : OneOfBase<StructReader<TCap>>, IAnyReader<TCap>
{

    public StructReader<TCap> AsStructReader() => this.AsT0;

    IStructReader<TCap> IAnyReader<TCap>.AsStructReader() => this.AsStructReader();
}