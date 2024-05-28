namespace Fundament.Capstone.Runtime.MessageStream;

using System.Numerics;

using Microsoft.Extensions.Logging;

public sealed class StructReader(WireSegmentSlice dataSection, WireSegmentSlice pointerSection, ILogger<StructReader> logger) : IStructReader
{
    private readonly WireSegmentSlice dataSection = dataSection;

    private readonly WireSegmentSlice pointerSection = pointerSection;

    private readonly ILogger<StructReader> logger = logger;

    public StructReader(WireMessageSegment segment, StructPointer structPointer, ILogger<StructReader> logger) 
        : this(segment[structPointer.DataSectionRange], segment[structPointer.PointerSectionRange], logger) {}

    public void ReadVoid(int offset)
    {
        // TODO: Write this implementation
        // All ReadVoid should do is increment the traversal limit counter by one word.
    }

    public bool ReadBool(int offset, bool defaultValue) => this.dataSection.GetBitByOffset(offset) ^ defaultValue;

    public sbyte ReadInt8(int offset, sbyte defaultValue) => this.ReadT(offset, defaultValue);

    public short ReadInt16(int offset, short defaultValue) => this.ReadT(offset, defaultValue);

    public int ReadInt32(int offset, int defaultValue) => this.ReadT(offset, defaultValue);

    // Micro-optimization: GetBySizeAlignedOffset will work with words, but may have more branches than just checking the offset.
    public long ReadInt64(int offset, long defaultValue) => 
        this.dataSection.IsIndexInRange(offset)
            ? (long) this.dataSection[offset]
            : defaultValue;
     
    public byte ReadUInt8(int offset, byte defaultValue) => this.ReadT(offset, defaultValue);

    public ushort ReadUInt16(int offset, ushort defaultValue) => this.ReadT(offset, defaultValue);

    public uint ReadUInt32(int offset, uint defaultValue) => this.ReadT(offset, defaultValue);

    // Same micro-optimization as ReadInt64
    public ulong ReadUInt64(int offset, ulong defaultValue) => 
        this.dataSection.IsIndexInRange(offset)
            ? this.dataSection[offset]
            : defaultValue;

    public float ReadFloat32(int offset, float defaultValue) => this.ReadT(offset, defaultValue);

    public double ReadFloat64(int offset, double defaultValue) => 
        this.dataSection.IsIndexInRange(offset)
            ? BitConverter.UInt64BitsToDouble(this.dataSection[offset] ^ BitConverter.DoubleToUInt64Bits(defaultValue))
            : defaultValue;

    // Generic implementation of the Read* methods.
    // May want to promote this to the interface.
    // Most of the reason for this method's existence is to be able to use .NET 7's generic math,
    // which defines the bitwise xor operators for all numeric types, including those which normally 
    // don't have bitwise operators, like float and double.
    private T ReadT<T>(int offset, T defaultValue) where T : struct, IBinaryNumber<T> => 
        T.CreateChecked(this.dataSection.GetBySizeAlignedOffset<T>(offset) ^ defaultValue);
}