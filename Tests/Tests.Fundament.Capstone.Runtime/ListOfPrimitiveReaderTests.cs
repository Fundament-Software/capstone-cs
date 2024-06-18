namespace Tests.Fundament.Capstone.Runtime;

using FluentAssertions.Execution;

using Fundament.Capstone.Runtime;

using global::Fundament.Capstone.Runtime;
using global::Fundament.Capstone.Runtime.MessageStream;

using Xunit.Abstractions;

public class ListOfPrimitiveReaderTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ListOfPrimitiveReader_BehavesLikeCorrectList()
    {
        var listPointer = new ListPointer(0, ListElementType.FourBytes, 5);
        var sharedReaderState = new SharedReaderState
        {
            WireMessage = new WireMessage([
                [
                    listPointer.AsWord,
                    0x0000_0002_0000_0001,
                    0x0000_0004_0000_0003,
                    0x0000_0000_0000_0005,
                ]
            ]),
            LoggerFactory = outputHelper.ToLoggerFactory(),
        };
        var reader = new ListOfPrimitiveReader<int, Unit>(sharedReaderState, 0, 0, listPointer);

        reader.Should().Equal(1, 2, 3, 4, 5);
    }
}
