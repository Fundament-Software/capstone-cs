namespace Fundament.Capstone.Runtime;

public interface IStructReader
{
    public void ReadVoid(int offset);

    public bool ReadBool(int offset, bool defaultValue);

    public sbyte ReadInt8(int offset, sbyte defaultValue);

    public short ReadInt16(int offset, short defaultValue);

    public int ReadInt32(int offset, int defaultValue);

    public long ReadInt64(int offset, long defaultValue);

    public byte ReadUInt8(int offset, byte defaultValue);

    public ushort ReadUInt16(int offset, ushort defaultValue);

    public uint ReadUInt32(int offset, uint defaultValue);

    public ulong ReadUInt64(int offset, ulong defaultValue);

    public float ReadFloat32(int offset, float defaultValue);

    public double ReadFloat64(int offset, double defaultValue);

    // public object ReadPointer(int offset);
}