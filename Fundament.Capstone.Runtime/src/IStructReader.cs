namespace Fundament.Capstone.Runtime;

using System.Numerics;

/// <summary>
/// Interface for defining an object that can read values from a struct in a cap'n proto message.
/// </summary>
/// <remarks>
/// In Cap'n Proto, struct values are aligned on to a multiple of their size, so the offset is always in multiples of the size of the value being read.
/// <remarks>
public interface IStructReader<TCap>
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
    /// Reads a value of type <typeparamref name="T"/> from the struct at the given type-aligned offset from the start of the struct's data section.
    /// </summary>
    /// <typeparam name="T">The type of the data being read.</typeparam>
    /// <param name="offset">The offset of the data being read, in number of <typeparamref name="T"/>s, from the beginning of the data section.</param>
    /// <param name="defaultValue">The default value of the data.</param>
    /// <returns>The value of type <typeparamref name="T"/> at the provided offset.</returns>   
    /// <remarks>
    /// Old versions of a schema may specify structs with fields that are not present in the current version of the schema.
    /// For this reason, Read never throw an exception if the offset is out of range. Instead, the default value should be returned.
    /// <remarks>
    public T ReadData<T>(int offset, T defaultValue) where T : unmanaged, IBinaryNumber<T>;

    // public object ReadPointer(int offset);

    /// <summary>
    /// Reads a boolean value from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in bits.</param>
    /// <param name="defaultValue">The default value of the bool field.</param>
    /// <remarks>
    /// Unlike other Read* definitions in the interface, this method cannot have a default implementation because in Cap'n Proto, 
    /// boolean values are stored as bits, whereas C# stores them as bytes.
    /// <remarks>
    /// <returns></returns>
    public bool ReadBool(int offset, bool defaultValue);

    /// <summary>
    /// Reads an 8-bit signed integer from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in bytes.</param>
    /// <param name="defaultValue">The default value of the Int8 field.</param>
    /// <returns></returns>
    public sbyte ReadInt8(int offset, sbyte defaultValue) => this.ReadData(offset, defaultValue);

    /// <summary>
    /// Reads a 16-bit signed integer from the struct at the given offset from the start of the struct's data section.
    /// </summary>
    /// <param name="offset">The offset in 4-bytes</param>
    /// <param name="defaultValue">The default value of the Int16 field.</param>
    /// <returns></returns>
    public short ReadInt16(int offset, short defaultValue) => this.ReadData(offset, defaultValue);

    public int ReadInt32(int offset, int defaultValue) => this.ReadData(offset, defaultValue);

    public long ReadInt64(int offset, long defaultValue) => this.ReadData(offset, defaultValue);

    public byte ReadUInt8(int offset, byte defaultValue) => this.ReadData(offset, defaultValue);

    public ushort ReadUInt16(int offset, ushort defaultValue) => this.ReadData(offset, defaultValue);

    public uint ReadUInt32(int offset, uint defaultValue) => this.ReadData(offset, defaultValue);

    public ulong ReadUInt64(int offset, ulong defaultValue) => this.ReadData(offset, defaultValue);

    public float ReadFloat32(int offset, float defaultValue) => this.ReadData(offset, defaultValue);

    public double ReadFloat64(int offset, double defaultValue) => this.ReadData(offset, defaultValue);
}