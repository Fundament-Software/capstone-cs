namespace Fundament.Capstone.Runtime.MessageStream;

/// <summary>
/// Enum of tag values for pointer types in cap'n proto messages.
/// These are the least significant 2 bits of a pointer word.
/// Also used as the tag for the MessagePointer sum type.
/// </summary>
internal enum PointerType : byte
{
    /// <summary> Pointer to a struct.</summary>
    Struct = 0,

    /// <summary>Pointer to a list.</summary>
    List = 1,

    /// <summary>Inter-segment pointer.</summary>
    Far = 2,

    /// <summary> Pointer into the capability table.</summary>
    Capability = 3,
}