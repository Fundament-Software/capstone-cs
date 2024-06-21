namespace Fundament.Capstone.Runtime;

using System.Numerics;

/// <summary>
/// Interface for defining an object that can read values from a struct in a cap'n proto message.
/// </summary>
/// <remarks>
/// In Cap'n Proto, struct values are aligned on to a multiple of their size, so the index is always in multiples of the size of the value being read.
/// </remarks>
public interface IStructReader<TCap> : IReader<TCap>
{
    /// <summary>
    /// The total size of the struct in words.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Read a void value from the struct at the given index from the start of the struct's data section.
    /// </summary>
    /// <remarks>
    /// This method doesn't return anything, but it is important to call it so that the traversal counter is incremented,
    /// and we prevent list amplification attacks.
    /// </remarks>
    /// <param name="index">
    /// The index of the data being read, from the start of the struct's data section.
    /// </param>
    public void ReadVoid(int index);

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the struct at the given type-aligned index from the start of the struct's data section.
    /// </summary>
    /// <typeparam name="T">The type of the data being read.</typeparam>
    /// <param name="index">The index of the data being read, in number of <typeparamref name="T"/>s, from the beginning of the data section.</param>
    /// <param name="defaultValue">The default value of the data.</param>
    /// <returns>The value of type <typeparamref name="T"/> at the provided index.</returns>
    /// <remarks>
    /// Old versions of a schema may specify structs with fields that are not present in the current version of the schema.
    /// For this reason, Read never throw an exception if the index is out of range. Instead, the default value should be returned.
    /// </remarks>
    public T ReadData<T>(int index, T defaultValue)
    where T : unmanaged, IBinaryNumber<T>;

    /// <summary>
    /// Decodes and follows a pointer from the struct at the given index from the start of the struct's pointer section.
    /// </summary>
    /// <param name="index">The index of the pointer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range for the pointer section.</exception>
    /// <returns>The reader for the object that the pointer points to.</returns>
    public IReader<TCap> ReadPointer(int index);

    /// <summary>
    /// Reads a boolean value from the struct at the given index from the start of the struct's data section.
    /// </summary>
    /// <param name="index">The index in bits.</param>
    /// <param name="defaultValue">The default value of the bool field.</param>
    /// <remarks>
    /// Unlike other Read* definitions in the interface, this method cannot have a default implementation because in Cap'n Proto, 
    /// boolean values are stored as bits, whereas C# stores them as bytes.
    /// </remarks>
    /// <returns></returns>
    public bool ReadBool(int index, bool defaultValue);

    /// <summary>
    /// Reads an 8-bit signed integer from the struct at the given index from the start of the struct's data section.
    /// </summary>
    /// <param name="index">The index in bytes.</param>
    /// <param name="defaultValue">The default value of the Int8 field.</param>
    /// <returns></returns>
    public sbyte ReadInt8(int index, sbyte defaultValue) => this.ReadData(index, defaultValue);

    /// <summary>
    /// Reads a 16-bit signed integer from the struct at the given index from the start of the struct's data section.
    /// </summary>
    /// <param name="index">The index in 4-bytes</param>
    /// <param name="defaultValue">The default value of the Int16 field.</param>
    /// <returns></returns>
    public short ReadInt16(int index, short defaultValue) => this.ReadData(index, defaultValue);

    public int ReadInt32(int index, int defaultValue) => this.ReadData(index, defaultValue);

    public long ReadInt64(int index, long defaultValue) => this.ReadData(index, defaultValue);

    public byte ReadUInt8(int index, byte defaultValue) => this.ReadData(index, defaultValue);

    public ushort ReadUInt16(int index, ushort defaultValue) => this.ReadData(index, defaultValue);

    public uint ReadUInt32(int index, uint defaultValue) => this.ReadData(index, defaultValue);

    public ulong ReadUInt64(int index, ulong defaultValue) => this.ReadData(index, defaultValue);

    public float ReadFloat32(int index, float defaultValue) => this.ReadData(index, defaultValue);

    public double ReadFloat64(int index, double defaultValue) => this.ReadData(index, defaultValue);
}