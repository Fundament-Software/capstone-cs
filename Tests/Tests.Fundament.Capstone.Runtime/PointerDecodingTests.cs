namespace Tests.Fundament.Capstone.Runtime;

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
        var word = ConstructStructPointerWord(expectedOffset, expectedDataSize, expectedPointerSize);

        // =-=-=-=( Act )=-=-=-=
        var result = StructPointer.Decode(word);

        // =-=-=-=( Assert )=-=-=-=
        result.ShouldSatisfyAllConditions(
            r => r.Offset.ShouldBe(expectedOffset),
            r => r.DataSize.ShouldBe(expectedDataSize),
            r => r.PointerSize.ShouldBe(expectedPointerSize)
        );
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
        var word = ConstructStructPointerWord(expectedOffset, expectedDataSize, expectedPointerSize);
        var originalStructPointer = StructPointer.Decode(word);

        // =-=-=-=( Act )=-=-=-=
        WirePointer convert = originalStructPointer;

        // =-=-=-=( Assert )=-=-=-=
        convert.IsStruct.ShouldBeTrue();
        convert.IsList.ShouldBeFalse();
        convert.IsFar.ShouldBeFalse();
        convert.IsCapability.ShouldBeFalse();

        convert.UnwrapStruct.ShouldBeEquivalentTo(originalStructPointer);
        Should.Throw<InvalidOperationException>(() => convert.UnwrapList);
        Should.Throw<InvalidOperationException>(() => convert.UnwrapFar);
        Should.Throw<InvalidOperationException>(() => convert.UnwrapCapability);
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