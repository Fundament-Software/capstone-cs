namespace Fundament.Capstone.Runtime.MessageStream;

using System.Diagnostics.CodeAnalysis;

using OneOf;

/// <summary>
/// A discriminated union of *Reader types.
/// </summary>
/// <typeparam name="TCap">The type of the capability table imbued in the reader.</typeparam>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:PartialElementsMustBeDocumented", Justification = "Partial class due to code generation")]
[GenerateOneOf]
public partial class AnyReader<TCap> : OneOfBase<StructReader<TCap>>, IAnyReader<TCap>
{
    public StructReader<TCap> AsStructReader() => this.AsT0;

    IStructReader<TCap> IAnyReader<TCap>.AsStructReader() => this.AsStructReader();

    internal static AnyReader<TCap> TraversePointer(WirePointer pointer, SharedReaderState sharedReaderState, int segmentId, Index pointerIndex) =>
        pointer.Match(
            (StructPointer structPointer) => new StructReader<TCap>(sharedReaderState, segmentId, pointerIndex, structPointer),
            (ListPointer listPointer) => throw new NotImplementedException(),
            (FarPointer farPointer) => throw new NotImplementedException(),
            (CapabilityPointer capabilityPointer) => throw new NotImplementedException()
        );
}