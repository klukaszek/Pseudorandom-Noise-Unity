using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class Shapes
{
    // A Burst compiled job that only accepts IShape types
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S>: IJobFor where S : struct, IShape{

        [WriteOnly]
        NativeArray<float3x4> positions, normals;

        public float resolution, invResolution;

        // Transform, Rotation, Scale
        public float3x4 positionTRS, normalTRS;

        // Execute position calculation for each element in parallel
        public void Execute (int i)
        {
            // Get the uv values for the current index
            Point4 p = default(S).GetPoint4(i, resolution, invResolution);

            // Calculate position of  using the position transformation matrix
            positions[i] = transpose(positionTRS.TransformVectors(p.positions));

            // Calculate normals using the position transformation matrix
            float3x4 n = transpose(normalTRS.TransformVectors(p.normals, 0f));
            normals[i] = float3x4(normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3));
        }

        // Custom ScheduleParallel method that takes all necessary parameters for the job
        public static JobHandle ScheduleParallel (NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency)
        {
            return new Job<S> {
                positions = positions,
                normals = normals,
                resolution = resolution,
                invResolution = 1f / resolution,
                positionTRS = trs.Get3x4(),
                normalTRS = transpose(inverse(trs)).Get3x4()
            }.ScheduleParallel(positions.Length, resolution, dependency);
        }
    }

    public delegate JobHandle ScheduleDelegate (NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency);

    // Interface for generic shapes
    public interface IShape
    {
        Point4 GetPoint4 (int i, float resolution, float invResolution);
    }

    // Convert an index value to a uv coordinate
    public static float4x2 IndexTo4UV(int i, float resolution, float invResolution)
    {
        float4x2 uv;

        // Calculate u and v as floats
        float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
        uv.c1 = floor(invResolution * i4 + 0.00001f);
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f);
        uv.c1 = invResolution * (uv.c1 + 0.5f);

        return uv;
    }

    // A point in 3D space
    public struct Point4 
    {
        public float4x3 positions, normals;
    }

    // A plane in 3D space
    public struct Plane : IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            // Subtract 0.5f from the u and v coordinates to center the plane at the origin
            return new Point4 {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
                normals = float4x3(0f, 1f, 0f)
            };
        }
    }

    public struct Sphere : IShape 
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            Point4 p;

            // Create octahedron
            p.positions.c0 = uv.c0 - 0.5f;
            p.positions.c1 = uv.c1 - 0.5f;
            p.positions.c2 = 0f;
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);

            float4 offset = max(-p.positions.c2, 0f);

            // If X or Y is negative, add the offset, otherwise subtract the offset
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
            p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);

            // Turn octahedron into sphere
            float4 scale = 0.5f * rsqrt(
                p.positions.c0 * p.positions.c0 +
                p.positions.c1 * p.positions.c1 +
                p.positions.c2 * p.positions.c2
            );
            
            // Apply scale to the positions
            p.positions.c0 *= scale;
            p.positions.c1 *= scale;
            p.positions.c2 *= scale;

            p.normals = p.positions;

            // The length of the vectors is 0.5 but the job will normalize them
            return p;
        }
    }

    public struct Torus : IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            float r1 = 0.375f;
            float r2 = 0.125f;

            // Double the u coordinate (c0) to make a full circle
            float4 s = r1 + r2 * cos(2f * PI * uv.c1);

            Point4 p;

            // Double the u coordinate (c0) to make a full circle
            p.positions.c0 = s * sin(2f * PI * uv.c0);
			p.positions.c1 = r2 * sin(2f * PI * uv.c1);
			p.positions.c2 = s * cos(2f * PI * uv.c0);
			
            // We have to adjust the normals so that they are not pointing away from the center, and instead they have to point away from the ring of the torus.
            p.normals = p.positions;
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0);
            p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
			
            return p;
        }
    }
}
