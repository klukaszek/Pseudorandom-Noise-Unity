using Unity.Mathematics;

// XXHash is a non-cryptographic hash function that is fast and has good avalanche characteristics
public readonly struct SmallXXHash
{
    // Binary prime numbers selected empirically selected by Yann Collet as the best values used to manipulate bits for the hash function
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    readonly uint accumulator;

    // Initialize the hash with an accumulator value
    public SmallXXHash (uint accumulator)
    {
        this.accumulator = accumulator;
    }

    // Implicitly convert a uint accumulator to a SmallXXHash
    public static implicit operator SmallXXHash (uint accumulator) => new SmallXXHash(accumulator);

    // Initialize the hash with a seed
    public static SmallXXHash Seed (int seed) => (uint)seed + primeE;

    // Finalize the hash by applying avalanche and returning the result
    public static implicit operator uint (SmallXXHash hash) 
    {
        uint avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    // Apply leftward rotation to the data by shifting the bits to the left and then ORing the bits that were shifted off the end back onto the beginning
    static uint RotL (uint data, int steps) => (data << steps) | (data >> (32 - steps));

    // Consume 32 bits of data (only eat a single portion of data at a time for SmallXXHash)
    // All operations are effectively modulo 2^32
    public SmallXXHash Eat (int data) => RotL(accumulator + (uint)data * primeC, 17) * primeD;

    public SmallXXHash Eat (byte data) => RotL(accumulator + data * primeE, 11) * primeA;

    // Convert single value version to vectorized version
    public static implicit operator SmallXXHash4 (SmallXXHash hash) => new SmallXXHash4(hash.accumulator);
}

public readonly struct SmallXXHash4
{
    // Binary prime numbers selected empirically selected by Yann Collet as the best values used to manipulate bits for the hash function
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    readonly uint4 accumulator;

    public uint4 BytesA => (uint4)this & 255;
    public uint4 BytesB => ((uint4)this >> 8) & 255;
	public uint4 BytesC => ((uint4)this >> 16) & 255;
	public uint4 BytesD => (uint4)this >> 24;

    // Return the floats in the range [0, 1] by dividing the bytes by 255
    public float4 Floats01A => (float4)BytesA * (1f / 255f);
    public float4 Floats01B => (float4)BytesB * (1f / 255f);
	public float4 Floats01C => (float4)BytesC * (1f / 255f);
	public float4 Floats01D => (float4)BytesD * (1f / 255f);

    // Initialize the hash with an accumulator value
    public SmallXXHash4 (uint4 accumulator)
    {
        this.accumulator = accumulator;
    }

    // Implicitly convert a uint4 accumulator to a SmallXXHash
    public static implicit operator SmallXXHash4 (uint4 accumulator) => new SmallXXHash4(accumulator);

    // Initialize the hash with a seed
    public static SmallXXHash4 Seed (int4 seed) => (uint4)seed + primeE;

    // Finalize the hash by applying avalanche and returning the result
    public static implicit operator uint4 (SmallXXHash4 hash) 
    {
        uint4 avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    // Apply leftward rotation to the data by shifting the bits to the left and then ORing the bits that were shifted off the end back onto the beginning
    static uint4 RotL (uint4 data, int steps) => (data << steps) | (data >> (32 - steps));

    // Consume 32 bits of data (only eat a single portion of data at a time for SmallXXHash)
    // All operations are effectively modulo 2^32
    public SmallXXHash4 Eat (int4 data) => RotL(accumulator + (uint4)data * primeC, 17) * primeD;
}