namespace Tests.Fundament.Capstone.Runtime;

using global::Fundament.Capstone.Runtime;
using global::Fundament.Capstone.Runtime.MessageStream;

public class ListOfReaderTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ListOfPrimitiveReader_BehavesLikeCorrectList()
    {
        var listPointer = new ListPointer(0, ListElementType.FourBytes, 6);
        var sharedReaderState = new SharedReaderState
        {
            WireMessage = new WireMessage([
                [
                    listPointer.AsWord,
                    0x0000_0002_0000_0001,
                    0x0000_0004_0000_0003,
                    0x0000_0006_0000_0005,
                ]
            ]),
            LoggerFactory = outputHelper.ToLoggerFactory(),
        };
        var reader = new ListOfPrimitiveReader<int, Unit>(sharedReaderState, 0, 0, listPointer);

        reader.ShouldBe([1, 2, 3, 4, 5, 6]);
    }
}