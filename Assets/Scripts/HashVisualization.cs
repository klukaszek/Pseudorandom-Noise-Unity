using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[ExecuteInEditMode]
public class HashVisualization : Visualization
{
    // Parallelize the hash calculation for each point in the visualization
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor {

        [WriteOnly]
        public NativeArray<uint4> hashes;

        [ReadOnly]
        public NativeArray<float3x4> positions;

        public float3x4 domainTRS;
        public SmallXXHash4 hash;

        // Execute the hash function in parallel
        public void Execute(int i)
        {
            float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));

            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    static int hashesId = Shader.PropertyToID("_Hashes");

    [SerializeField]
    int seed;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS {
        scale = 8f
    };

    NativeArray<uint4> hashes;

    ComputeBuffer hashesBuffer;

    // Set variables for the object to be instanced
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        // Allocate memory for the hashes
        hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);

        // Allocate memory for the GPU compute buffers
        hashesBuffer = new ComputeBuffer(dataLength * 4, 4);

        // Pass the compute buffers and other data to the material
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
    }

    // Free the memory allocated by the NativeArray and ComputeBuffer when the component is disabled
    protected override void DisableVisualization()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
    {
        // Parallelize the hash calculation and pass the necessary data to the HashJob, set dependency to the positions job
        new HashJob
        {
            positions = positions,
            hashes = hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(hashes.Length, resolution, handle).Complete();

        // Assign data to GPU compute buffers
        hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
    }
}
