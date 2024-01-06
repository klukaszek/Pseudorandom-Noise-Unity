using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise {

    // Lattice Noise along the X axis
    public struct Lattice1D : INoise 
    {
        public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash)
        {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            return lerp(hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t) * 2f - 1f;
        }
    }

    // Lattice Noise along the XZ axes
    public struct Lattice2D : INoise 
    {
        public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash)
        {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            LatticeSpan4 z = GetLatticeSpan4(positions.c2);

            SmallXXHash4 h0 = hash.Eat(x.p0);
            SmallXXHash4 h1 = hash.Eat(x.p1);

            // Lerp between the lattice points in the X direction, then the Z direction
            // This is known as a "Bilinear Interpolation"
            return lerp(
                lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
                lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
                x.t
            ) * 2f - 1f;
        }
    }

    // Contains the lattice points and interpolation values for a 1D set of coordinates
    struct LatticeSpan4
    {
        public int4 p0, p1;
        public float4 t;
    }

    // Get the lattice points and interpolation values for a 1D set of coordinates
    static LatticeSpan4 GetLatticeSpan4 (float4 coordinates)
    {
        float4 points = floor(coordinates);
        LatticeSpan4 span;

        span.p0 = (int4)points;
        span.p1 = span.p0 + 1;
        span.t = coordinates - points;

        return span;
    }

}