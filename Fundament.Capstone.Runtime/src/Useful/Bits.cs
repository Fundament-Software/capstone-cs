namespace Fundament.Capstone.Runtime;

internal static class Bits
{
    public static ulong BitMaskOf(int bits) => 
        bits == (sizeof(ulong) * 8) 
            ? ulong.MaxValue
            : (1UL << bits) - 1;
}