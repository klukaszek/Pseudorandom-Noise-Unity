using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System;


[ExecuteInEditMode]
public abstract class Visualization : MonoBehaviour
{
    static int configId = Shader.PropertyToID("_Config");
    static int positionsId = Shader.PropertyToID("_Positions");
    static int normalsId = Shader.PropertyToID("_Normals");

    [SerializeField]
    Mesh instanceMesh;

    [SerializeField]
    Material material;

    [SerializeField, Range(1, 512)]
    int resolution = 16;

    [SerializeField, Range(-5f, 5f)]
    float displacement = 0.1f;

    NativeArray<float3x4> positions, normals;

    ComputeBuffer positionsBuffer, normalsBuffer;

    MaterialPropertyBlock propertyBlock;

    bool isDirty;

    Bounds bounds;

    public enum Shape { Plane, Sphere, Torus }

    static Shapes.ScheduleDelegate[] shapeJobs = {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField]
    Shape shape;

    [SerializeField, Range(0.1f, 10f)]
    float instanceScale = 2f;

    // Set variables for the object to be instanced
    void OnEnable()
    {
        isDirty = true;

        int arr_len = resolution * resolution;

        //Support odd resolutions by adding the least significant bit of the length to the vectorized length (0 for even, 1 for odd)
        arr_len = arr_len / 4 + (arr_len & 1);

        // Allocate memory for the positions, and normals
        positions = new NativeArray<float3x4>(arr_len, Allocator.Persistent);
        normals = new NativeArray<float3x4>(arr_len, Allocator.Persistent);

        // Allocate memory for the GPU compute buffers
        positionsBuffer = new ComputeBuffer(arr_len * 4, 3 * 4);
        normalsBuffer = new ComputeBuffer(arr_len * 4, 3 * 4);

        // Pass the compute buffers and other data to the material
        propertyBlock ??= new MaterialPropertyBlock();
        EnableVisualization(arr_len, propertyBlock);
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);
        propertyBlock.SetVector(configId, new Vector4(resolution, instanceScale / resolution, displacement));
    }

    // Free the memory allocated by the NativeArray and ComputeBuffer when the component is disabled
    void OnDisable()
    {
        positions.Dispose();
        normals.Dispose();
        positionsBuffer.Release();
        normalsBuffer.Release();
        positionsBuffer = null;
        normalsBuffer = null;
        DisableVisualization();
    }

    // Reset everything in OnValidate so any changes in play mode are reflected
    void OnValidate() 
    {
        if (positionsBuffer != null && enabled) {
            OnDisable();
            OnEnable();
        }
    }

    void Update()
    {
        // Only update the positions if dirty or the transform has changed
        if (isDirty || transform.hasChanged)
        {
            // Reset the dirty flag and the transform changed flag
            isDirty = false;
            transform.hasChanged = false;

            // Update visualization in parallel and pass the necessary data to the shape job based on the shape type
            UpdateVisualization(positions, resolution, shapeJobs[(int)shape](positions, normals, resolution, transform.localToWorldMatrix, default));

            // Assign data to GPU compute buffers
            positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
            normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

            // Only update the bounds if the transform has changed
            bounds = new Bounds(
                transform.position,
                float3(2f * cmax(abs(transform.lossyScale)) + displacement)
            );
        }

        Graphics.DrawMeshInstancedProcedural(
            instanceMesh,
            0,
            material,
            bounds,
            resolution * resolution,
            propertyBlock
        );
    }

    protected abstract void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock);

    protected abstract void DisableVisualization();

    protected abstract void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle);

}
