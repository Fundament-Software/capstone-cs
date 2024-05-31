namespace Tests.Fundament.Capstone.Runtime;

using FluentAssertions.Execution;

using global::Fundament.Capstone.Runtime.MessageStream;

public class PointerDecodingTests
{
    [Fact]
    public void TestDecodeStructPointer()
    {
        // =-=-=-=( Arrange )=-=-=-=
        const int expectedOffset = 37;
        const ushort expectedDataSize = 42;
        const ushort expectedPointerSize = 6;
        ulong[] segment = [ ConstructStructPointerWord(expectedOffset, expectedDataSize, expectedPointerSize) ];
        
        // =-=-=-=( Act )=-=-=-=
        var result = PointerDecodingUtils.DecodeStructPointer(segment, 0);

        // =-=-=-=( Assert )=-=-=-=
        using (new AssertionScope()) {
            result.Offset.Should().Be(expectedOffset);
            result.DataSize.Should().Be(expectedDataSize);
            result.PointerSize.Should().Be(expectedPointerSize);
        }
    }

    // More or less smoke tests for the pointer sum type.
    // Wasn't sure if I could make a struct union like I did.
    [Fact]
    public void TestPointerSumType()
    {
        // =-=-=-=( Arrange )=-=-=-=
        const int expectedOffset = 8;
        const ushort expectedDataSize = 31;
        const ushort expectedPointerSize = 18;
        var originalStructPointer = PointerDecodingUtils.DecodeStructPointer(
            [ ConstructStructPointerWord(expectedOffset, expectedDataSize, expectedPointerSize) ],
            0);

        // =-=-=-=( Act )=-=-=-=
        WirePointer convert = originalStructPointer;

        // =-=-=-=( Assert )=-=-=-=
        convert.IsStruct.Should().BeTrue();
        convert.IsList.Should().BeFalse();
        convert.IsFar.Should().BeFalse();
        convert.IsCapability.Should().BeFalse();

        convert.UnwrapStruct.Should().BeEquivalentTo(originalStructPointer);
        convert.Invoking(x => x.UnwrapList).Should().Throw<InvalidOperationException>();
        convert.Invoking(x => x.UnwrapFar).Should().Throw<InvalidOperationException>();
        convert.Invoking(x => x.UnwrapCapability).Should().Throw<InvalidOperationException>();

        convert.Match(
            structPointer => structPointer.Should().BeEquivalentTo(originalStructPointer),
            listPointer => throw new InvalidOperationException("Unexpected list pointer."),
            farPointer => throw new InvalidOperationException("Unexpected far pointer."),
            capabilityPointer => throw new InvalidOperationException("Unexpected capability pointer."));
    }

    private static ulong ConstructStructPointerWord(int offset, ushort dataSize, ushort pointerSize)
    {
        ulong word = 0;
        word |= (ulong)offset << 2;
        word |= (ulong)dataSize << 32;
        word |= (ulong)pointerSize << 48;
        return word;
    }
}