namespace Fundament.Capstone.Runtime;

/// <summary>
/// Interface for defining an object that can read values from a struct in a cap'n proto message.
/// </summary>
/// <remarks>
/// In Cap'n Proto, struct values are aligned on to a multiple of their size, so the offset is always in multiples of the size of the value being read.
/// <remarks>
public interface IStructReader
{
    /// <summary>
    /// Read a void value from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <remarks>
    /// This method doesn't return anything, but it is important to call it so that the traversal counter is incremented,
    /// and we prevent list amplification attacks.
    /// </remarks>
    /// <param name="offset"></param>
    public void ReadVoid(int offset);

    /// <summary>
    /// Reads a boolean value from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in bits.</param>
    /// <param name="defaultValue">The default value of the bool field.</param>
    /// <returns></returns>
    public bool ReadBool(int offset, bool defaultValue);

    /// <summary>
    /// Reads an 8-bit signed integer from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in bytes.</param>
    /// <param name="defaultValue">The default value of the Int8 field.</param>
    /// <returns></returns>
    public sbyte ReadInt8(int offset, sbyte defaultValue);

    /// <summary>
    /// Reads a 16-bit signed integer from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in 4-bytes</param>
    /// <param name="defaultValue">The default value of the Int16 field.</param>
    /// <returns></returns>
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