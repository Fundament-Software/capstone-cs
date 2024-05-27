namespace Fundament.Capstone.Runtime;

internal static class Bits
{
    public static ulong BitMaskOf(int bits) => (1UL << bits) - 1;
}