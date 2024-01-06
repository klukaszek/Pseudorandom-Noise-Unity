using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Noise;

[ExecuteInEditMode]
public class NoiseVisualization : Visualization
{
    static int noiseId = Shader.PropertyToID("_Noise");

    [SerializeField]
    int seed;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS {
        scale = 8f
    };

    NativeArray<float4> noise;

    ComputeBuffer noiseBuffer;

    // Set variables for the object to be instanced
    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
    {
        // Allocate memory for the noise, positions, and normals
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent);

        // Allocate memory for the GPU compute buffers
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4);

        // Pass the compute buffers and other data to the material
        propertyBlock.SetBuffer(noiseId, noiseBuffer);
    }

    // Free the memory allocated by the NativeArray and ComputeBuffer when the component is disabled
    protected override void DisableVisualization()
    {
        noise.Dispose();
        noiseBuffer.Release();
        noiseBuffer = null;
    }

    // Update the visualization
    protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
    {
        // Schedule the job for the selected noise type
        Job<Lattice2D>.ScheduleParallel(positions, noise, seed, domain, resolution, handle).Complete();

        // Assign data to GPU compute buffers
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
