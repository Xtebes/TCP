using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public struct triangle
{
    public float3 aPos, bPos, cPos;
    public int aEdgeI, bEdgeI, cEdgeI;
    public int2 aID, bID, cID;
    public Color32 aCol, bCol, cCol;
}
[BurstCompile]
public struct MarchCubesJob : IJobParallelFor
{
    public static MarchCubesJob GetMarchCubesJob(byte[] noise, Color32[] colors, out NativeQueue<triangle> triangles, int dimSize, float isoLevel)
    {
        MarchCubesJob marchCubesJob = new MarchCubesJob();
        marchCubesJob.currentNoise = new NativeArray<byte>(noise, Allocator.TempJob);
        marchCubesJob.edgeIndexA = new NativeArray<int>(SpatialHelpers.edgeIndexA, Allocator.TempJob);
        marchCubesJob.edgeIndexB = new NativeArray<int>(SpatialHelpers.edgeIndexB, Allocator.TempJob);
        marchCubesJob.triangulationFlat = new NativeArray<int>(SpatialHelpers.triangulationFlat, Allocator.TempJob);
        marchCubesJob.colors = new NativeArray<Color32>(colors, Allocator.TempJob);
        triangles = new NativeQueue<triangle>(Allocator.TempJob);
        marchCubesJob.queue = triangles.AsParallelWriter();
        marchCubesJob.dimSize = dimSize;
        marchCubesJob.isoLevel = isoLevel;
        return marchCubesJob;
    }
    [WriteOnly]
    public NativeQueue<triangle>.ParallelWriter queue;
    [ReadOnly][DeallocateOnJobCompletion]
    public NativeArray<int> edgeIndexA, edgeIndexB, triangulationFlat;
    [ReadOnly][DeallocateOnJobCompletion]
    public NativeArray<byte> currentNoise;
    [ReadOnly][DeallocateOnJobCompletion]
    public NativeArray<Color32> colors;
    public int dimSize;
    public float isoLevel;
    public void Execute(int index)
    {
        int3 pos = SpatialHelpers.PositionFromIndex(index, dimSize);
        if (pos.x >= dimSize - 1 || pos.y >= dimSize - 1 || pos.z >= dimSize - 1) return;
        NativeArray<int3> corners = new NativeArray<int3>(8, Allocator.Temp);
        corners[0] = new int3(pos.x    , pos.y    , pos.z    );
        corners[1] = new int3(pos.x + 1, pos.y    , pos.z    );
        corners[2] = new int3(pos.x + 1, pos.y    , pos.z + 1);
        corners[3] = new int3(pos.x    , pos.y    , pos.z + 1);
        corners[4] = new int3(pos.x    , pos.y + 1, pos.z    );
        corners[5] = new int3(pos.x + 1, pos.y + 1, pos.z    );
        corners[6] = new int3(pos.x + 1, pos.y + 1, pos.z + 1);
        corners[7] = new int3(pos.x    , pos.y + 1, pos.z + 1);
        NativeArray<float> cornerNoises = new NativeArray<float>(8, Allocator.Temp);
        NativeArray<int> cornerIndexes = new NativeArray<int>(8, Allocator.Temp);
        float3 CreateVertex(int indexA, int indexB, float isoLevel)
        {
            return SpatialHelpers.InterpolatePositions(corners[indexA], corners[indexB], cornerNoises[indexA], cornerNoises[indexB], isoLevel);
        }
        int config = 0;
        for (int l = 0; l < 8; l++)
        {
            cornerIndexes[l] = SpatialHelpers.IndexFromPosition(corners[l], dimSize);
            cornerNoises[l] = (float)currentNoise[cornerIndexes[l]] / 255;
            config |= (int)math.step(isoLevel, cornerNoises[l]) << l;
        }
        for (int i = config * 16; triangulationFlat[i] != -1; i += 3)
        {
            triangle tri = new triangle();

            int aIndex1 = edgeIndexA[triangulationFlat[i]];
            int aIndex2 = edgeIndexB[triangulationFlat[i]];

            int bIndex1 = edgeIndexA[triangulationFlat[i + 1]];
            int bIndex2 = edgeIndexB[triangulationFlat[i + 1]];

            int cIndex1 = edgeIndexA[triangulationFlat[i + 2]];
            int cIndex2 = edgeIndexB[triangulationFlat[i + 2]];

            tri.aPos = CreateVertex(aIndex1, aIndex2, isoLevel);
            tri.bPos = CreateVertex(bIndex1, bIndex2, isoLevel);
            tri.cPos = CreateVertex(cIndex1, cIndex2, isoLevel);

            tri.aID = new int2(math.min(cornerIndexes[aIndex1], cornerIndexes[aIndex2]), math.max(cornerIndexes[aIndex1], cornerIndexes[aIndex2]));
            tri.bID = new int2(math.min(cornerIndexes[bIndex1], cornerIndexes[bIndex2]), math.max(cornerIndexes[bIndex1], cornerIndexes[bIndex2]));
            tri.cID = new int2(math.min(cornerIndexes[cIndex1], cornerIndexes[cIndex2]), math.max(cornerIndexes[cIndex1], cornerIndexes[cIndex2]));

            int3 clampedAPos = math.clamp((int3)tri.aPos, int3.zero, new int3(1, 1, 1) * dimSize);
            int3 clampedBPos = math.clamp((int3)tri.bPos, int3.zero, new int3(1, 1, 1) * dimSize);
            int3 clampedCPos = math.clamp((int3)tri.cPos, int3.zero, new int3(1, 1, 1) * dimSize);

            int indexA = SpatialHelpers.IndexFromPosition(clampedAPos.x, clampedAPos.y, clampedAPos.z, dimSize);
            int indexB = SpatialHelpers.IndexFromPosition(clampedBPos.x, clampedBPos.y, clampedBPos.z, dimSize);
            int indexC = SpatialHelpers.IndexFromPosition(clampedCPos.x, clampedCPos.y, clampedCPos.z, dimSize);

            tri.aCol = colors[indexA];
            tri.bCol = colors[indexB];
            tri.cCol = colors[indexC];

            queue.Enqueue(tri);
        }
    }
}
public struct SphereDeformJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<byte> noise;
    [WriteOnly][NativeDisableParallelForRestriction]
    public NativeArray<Color32> colors;
    [WriteOnly][NativeDisableParallelForRestriction]
    public NativeArray<bool> bools;
    public int3 deformMinPosition;
    public int3 deformMaxPosition;
    public int3 localPosition;
    public int radius;
    public int dimSize;
    public float isoLevel;
    public float strength;
    public Color32 color;
    public void Execute(int index)
    {
        int3 indexPosition = SpatialHelpers.PositionFromIndex(index, dimSize);
        //Transform the mesh
        int3 localPosition = deformMinPosition + indexPosition;
        int localIndex = SpatialHelpers.IndexFromPosition(localPosition.x, localPosition.y, localPosition.z, dimSize);
        float localDistance = math.distance(localPosition, this.localPosition);    
        if (!(localPosition.x > deformMaxPosition.x - 1 || localPosition.y > deformMaxPosition.y - 1 || localPosition.z > deformMaxPosition.z - 1))
        {
            localDistance = math.clamp(localDistance, 1, radius);
            if (localDistance < radius)
            {
                colors[localIndex] = color;
            }
            float appliedStrength = strength - (localDistance / radius * strength);
            noise[localIndex] = (byte)math.clamp(noise[localIndex] + appliedStrength * 255, 0, 255);
        }
        bools[(int)math.step(isoLevel, noise[index])] = true;
    }
}
[BurstCompile]
public struct NoisePostProcessJob : IJobParallelFor
{
    [ReadOnly][DeallocateOnJobCompletion]
    public NativeArray<float> caveNoise;
    [ReadOnly][DeallocateOnJobCompletion]
    public NativeArray<float> planetNoise;
    [WriteOnly]
    public NativeArray<float> postProcessedNoise;
    [WriteOnly][NativeDisableParallelForRestriction]
    public NativeArray<bool> bools;
    public int dimSize;
    public float isoLevel;
    public float minPlanetRadius;
    public float maxPlanetRadius;
    public float minCaveRadius;
    public float surfaceDensity;
    public float cavernDensity;
    public float maxCaveRadius;
    public float caveNoiseMultiplier;
    public float surfaceNoiseMultiplier;
    public int3 corePosition;
    public int3 chunkPosition;
    public void Execute(int index)
    {
        int3 posInNoise = SpatialHelpers.PositionFromIndex(index, dimSize);
        posInNoise = new int3(posInNoise.z, posInNoise.y, posInNoise.x);
        float midRadius = (minPlanetRadius + maxPlanetRadius) / 2;
        float3 posInWorld = chunkPosition + posInNoise + corePosition;
        float distToMidRadius = math.distance(posInWorld, math.normalize(posInWorld) * midRadius);
        float planetThickness = maxPlanetRadius - minPlanetRadius;
        float caveThickness = maxCaveRadius - minCaveRadius;
        float scaleShiftedCaveNoise = caveNoise[index] / 2 + 0.5f;
        float scaleShiftedSurfaceNoise = planetNoise[index] / 2 + 0.5f;
        float outterWeight = math.max(1 - (distToMidRadius / planetThickness), 0);
        float innerWeight = math.max(1 - (distToMidRadius / caveThickness), 0);
        float surfaceNoise = math.clamp((scaleShiftedSurfaceNoise + outterWeight * surfaceDensity) * outterWeight, 0, 1);
        float cavernNoise = math.clamp((scaleShiftedCaveNoise + innerWeight * cavernDensity) * outterWeight, 0, 1);
        float finalNoise = math.max(surfaceNoise - cavernNoise, 0);
        postProcessedNoise[index] = finalNoise;
        bools[(int)math.step(isoLevel, finalNoise)] = true;
    }
}
[BurstCompile]
public struct BuildMeshJob : IJobParallelFor
{
    public static BuildMeshJob GetBuildMeshJob(NativeQueue<triangle> packedTriangles, out NativeArray<int> triangles, out NativeArray<Vector3> vertices, out NativeArray<Color32> colors)
    {
        BuildMeshJob buildMeshJob = new BuildMeshJob();
        buildMeshJob.packedTriangles = packedTriangles.ToArray(Allocator.TempJob);
        triangles = new NativeArray<int>(packedTriangles.Count * 3, Allocator.TempJob);
        colors = new NativeArray<Color32>(packedTriangles.Count * 3, Allocator.TempJob);
        vertices = new NativeArray<Vector3>(packedTriangles.Count * 3, Allocator.TempJob);
        buildMeshJob.triangles = triangles;
        buildMeshJob.vertices = vertices;
        buildMeshJob.colors = colors;
        packedTriangles.Dispose();
        return buildMeshJob;
        
    }
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<triangle> packedTriangles;
    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<int> triangles;
    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> vertices;
    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<Color32> colors;

    public void Execute(int index)
    {
        vertices[index * 3] = packedTriangles[index].aPos;
        vertices[index * 3 + 1] = packedTriangles[index].bPos;
        vertices[index * 3 + 2] = packedTriangles[index].cPos;
        colors[index * 3] = packedTriangles[index].aCol;
        colors[index * 3 + 1] = packedTriangles[index].bCol;
        colors[index * 3 + 2] = packedTriangles[index].cCol;
        triangles[index * 3] = index * 3;
        triangles[index * 3 + 1] = index * 3 + 1;
        triangles[index * 3 + 2] = index * 3 + 2;
    }
}
public static class SpatialHelpers
{
    public static float3 InterpolatePositions(int3 v1, int3 v2, float d1, float d2, float isoLevel) => v1.xyz + (isoLevel - d1) / (d2 - d1) * (float3)(v2.xyz - v1.xyz);
    public static int3 GetChunkPositionFromPosition(float3 position, int dimSizeMinusOne) { return (int3)math.floor(position / dimSizeMinusOne) * dimSizeMinusOne; }
    public static int3 PositionFromIndex(int index, int dimSize) { return new int3(index / (dimSize * dimSize), (index / dimSize) % dimSize, index % dimSize); }
    public static int IndexFromPosition(int x, int y, int z, int dimSize) { return z * dimSize * dimSize + y * dimSize + x; }
    public static int IndexFromPosition(int3 pos, int dimSize) { return pos.z * dimSize * dimSize + pos.y * dimSize + pos.x; }
    public static int SortBySqrtMagnitude(int3 refPoint, int3 positionA, int3 positionB)
    {
        int3 vecA = refPoint - positionA;
        int3 vecB = refPoint - positionB;
        float distA = vecA.x * vecA.x + vecA.y * vecA.y + vecA.z * vecA.z;
        float distB = vecB.x * vecB.x + vecB.y * vecB.y + vecB.z * vecB.z;
        if (distA < distB)
            return -1;
        return 1;
    }
    public static readonly int[] triangulationFlat = new int[]
    {
       -1 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 1 , 9 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 8 , 3 , 9 , 8 , 1 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , 1 , 2 , 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 2 , 10, 0 , 2 , 9 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 8 , 3 , 2 , 10, 8 , 10, 9 , 8 , -1, -1, -1, -1, -1, -1, -1 ,
        3 , 11, 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 11, 2 , 8 , 11, 0 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 9 , 0 , 2 , 3 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 11, 2 , 1 , 9 , 11, 9 , 8 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 10, 1 , 11, 10, 3 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 10, 1 , 0 , 8 , 10, 8 , 11, 10, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 9 , 0 , 3 , 11, 9 , 11, 10, 9 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 8 , 10, 10, 8 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 7 , 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 3 , 0 , 7 , 3 , 4 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 1 , 9 , 8 , 4 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 1 , 9 , 4 , 7 , 1 , 7 , 3 , 1 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, 8 , 4 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 4 , 7 , 3 , 0 , 4 , 1 , 2 , 10, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 2 , 10, 9 , 0 , 2 , 8 , 4 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        2 , 10, 9 , 2 , 9 , 7 , 2 , 7 , 3 , 7 , 9 , 4 , -1, -1, -1, -1 ,
        8 , 4 , 7 , 3 , 11, 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        11, 4 , 7 , 11, 2 , 4 , 2 , 0 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 0 , 1 , 8 , 4 , 7 , 2 , 3 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 7 , 11, 9 , 4 , 11, 9 , 11, 2 , 9 , 2 , 1 , -1, -1, -1, -1 ,
        3 , 10, 1 , 3 , 11, 10, 7 , 8 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 11, 10, 1 , 4 , 11, 1 , 0 , 4 , 7 , 11, 4 , -1, -1, -1, -1 ,
        4 , 7 , 8 , 9 , 0 , 11, 9 , 11, 10, 11, 0 , 3 , -1, -1, -1, -1 ,
        4 , 7 , 11, 4 , 11, 9 , 9 , 11, 10, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 5 , 4 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 5 , 4 , 0 , 8 , 3 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 5 , 4 , 1 , 5 , 0 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        8 , 5 , 4 , 8 , 3 , 5 , 3 , 1 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, 9 , 5 , 4 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 0 , 8 , 1 , 2 , 10, 4 , 9 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 2 , 10, 5 , 4 , 2 , 4 , 0 , 2 , -1, -1, -1, -1, -1, -1, -1 ,
        2 , 10, 5 , 3 , 2 , 5 , 3 , 5 , 4 , 3 , 4 , 8 , -1, -1, -1, -1 ,
        9 , 5 , 4 , 2 , 3 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 11, 2 , 0 , 8 , 11, 4 , 9 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 5 , 4 , 0 , 1 , 5 , 2 , 3 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 1 , 5 , 2 , 5 , 8 , 2 , 8 , 11, 4 , 8 , 5 , -1, -1, -1, -1 ,
        10, 3 , 11, 10, 1 , 3 , 9 , 5 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        4 , 9 , 5 , 0 , 8 , 1 , 8 , 10, 1 , 8 , 11, 10, -1, -1, -1, -1 ,
        5 , 4 , 0 , 5 , 0 , 11, 5 , 11, 10, 11, 0 , 3 , -1, -1, -1, -1 ,
        5 , 4 , 8 , 5 , 8 , 10, 10, 8 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 7 , 8 , 5 , 7 , 9 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 3 , 0 , 9 , 5 , 3 , 5 , 7 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 7 , 8 , 0 , 1 , 7 , 1 , 5 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 5 , 3 , 3 , 5 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 7 , 8 , 9 , 5 , 7 , 10, 1 , 2 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 1 , 2 , 9 , 5 , 0 , 5 , 3 , 0 , 5 , 7 , 3 , -1, -1, -1, -1 ,
        8 , 0 , 2 , 8 , 2 , 5 , 8 , 5 , 7 , 10, 5 , 2 , -1, -1, -1, -1 ,
        2 , 10, 5 , 2 , 5 , 3 , 3 , 5 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        7 , 9 , 5 , 7 , 8 , 9 , 3 , 11, 2 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 5 , 7 , 9 , 7 , 2 , 9 , 2 , 0 , 2 , 7 , 11, -1, -1, -1, -1 ,
        2 , 3 , 11, 0 , 1 , 8 , 1 , 7 , 8 , 1 , 5 , 7 , -1, -1, -1, -1 ,
        11, 2 , 1 , 11, 1 , 7 , 7 , 1 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 5 , 8 , 8 , 5 , 7 , 10, 1 , 3 , 10, 3 , 11, -1, -1, -1, -1 ,
        5 , 7 , 0 , 5 , 0 , 9 , 7 , 11, 0 , 1 , 0 , 10, 11, 10, 0 , -1 ,
        11, 10, 0 , 11, 0 , 3 , 10, 5 , 0 , 8 , 0 , 7 , 5 , 7 , 0 , -1 ,
        11, 10, 5 , 7 , 11, 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        10, 6 , 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , 5 , 10, 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 0 , 1 , 5 , 10, 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 8 , 3 , 1 , 9 , 8 , 5 , 10, 6 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 6 , 5 , 2 , 6 , 1 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 6 , 5 , 1 , 2 , 6 , 3 , 0 , 8 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 6 , 5 , 9 , 0 , 6 , 0 , 2 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 9 , 8 , 5 , 8 , 2 , 5 , 2 , 6 , 3 , 2 , 8 , -1, -1, -1, -1 ,
        2 , 3 , 11, 10, 6 , 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        11, 0 , 8 , 11, 2 , 0 , 10, 6 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 1 , 9 , 2 , 3 , 11, 5 , 10, 6 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 10, 6 , 1 , 9 , 2 , 9 , 11, 2 , 9 , 8 , 11, -1, -1, -1, -1 ,
        6 , 3 , 11, 6 , 5 , 3 , 5 , 1 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 11, 0 , 11, 5 , 0 , 5 , 1 , 5 , 11, 6 , -1, -1, -1, -1 ,
        3 , 11, 6 , 0 , 3 , 6 , 0 , 6 , 5 , 0 , 5 , 9 , -1, -1, -1, -1 ,
        6 , 5 , 9 , 6 , 9 , 11, 11, 9 , 8 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 10, 6 , 4 , 7 , 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 3 , 0 , 4 , 7 , 3 , 6 , 5 , 10, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 9 , 0 , 5 , 10, 6 , 8 , 4 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 6 , 5 , 1 , 9 , 7 , 1 , 7 , 3 , 7 , 9 , 4 , -1, -1, -1, -1 ,
        6 , 1 , 2 , 6 , 5 , 1 , 4 , 7 , 8 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 5 , 5 , 2 , 6 , 3 , 0 , 4 , 3 , 4 , 7 , -1, -1, -1, -1 ,
        8 , 4 , 7 , 9 , 0 , 5 , 0 , 6 , 5 , 0 , 2 , 6 , -1, -1, -1, -1 ,
        7 , 3 , 9 , 7 , 9 , 4 , 3 , 2 , 9 , 5 , 9 , 6 , 2 , 6 , 9 , -1 ,
        3 , 11, 2 , 7 , 8 , 4 , 10, 6 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 10, 6 , 4 , 7 , 2 , 4 , 2 , 0 , 2 , 7 , 11, -1, -1, -1, -1 ,
        0 , 1 , 9 , 4 , 7 , 8 , 2 , 3 , 11, 5 , 10, 6 , -1, -1, -1, -1 ,
        9 , 2 , 1 , 9 , 11, 2 , 9 , 4 , 11, 7 , 11, 4 , 5 , 10, 6 , -1 ,
        8 , 4 , 7 , 3 , 11, 5 , 3 , 5 , 1 , 5 , 11, 6 , -1, -1, -1, -1 ,
        5 , 1 , 11, 5 , 11, 6 , 1 , 0 , 11, 7 , 11, 4 , 0 , 4 , 11, -1 ,
        0 , 5 , 9 , 0 , 6 , 5 , 0 , 3 , 6 , 11, 6 , 3 , 8 , 4 , 7 , -1 ,
        6 , 5 , 9 , 6 , 9 , 11, 4 , 7 , 9 , 7 , 11, 9 , -1, -1, -1, -1 ,
        10, 4 , 9 , 6 , 4 , 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 10, 6 , 4 , 9 , 10, 0 , 8 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 0 , 1 , 10, 6 , 0 , 6 , 4 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        8 , 3 , 1 , 8 , 1 , 6 , 8 , 6 , 4 , 6 , 1 , 10, -1, -1, -1, -1 ,
        1 , 4 , 9 , 1 , 2 , 4 , 2 , 6 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        3 , 0 , 8 , 1 , 2 , 9 , 2 , 4 , 9 , 2 , 6 , 4 , -1, -1, -1, -1 ,
        0 , 2 , 4 , 4 , 2 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        8 , 3 , 2 , 8 , 2 , 4 , 4 , 2 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 4 , 9 , 10, 6 , 4 , 11, 2 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 2 , 2 , 8 , 11, 4 , 9 , 10, 4 , 10, 6 , -1, -1, -1, -1 ,
        3 , 11, 2 , 0 , 1 , 6 , 0 , 6 , 4 , 6 , 1 , 10, -1, -1, -1, -1 ,
        6 , 4 , 1 , 6 , 1 , 10, 4 , 8 , 1 , 2 , 1 , 11, 8 , 11, 1 , -1 ,
        9 , 6 , 4 , 9 , 3 , 6 , 9 , 1 , 3 , 11, 6 , 3 , -1, -1, -1, -1 ,
        8 , 11, 1 , 8 , 1 , 0 , 11, 6 , 1 , 9 , 1 , 4 , 6 , 4 , 1 , -1 ,
        3 , 11, 6 , 3 , 6 , 0 , 0 , 6 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        6 , 4 , 8 , 11, 6 , 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        7 , 10, 6 , 7 , 8 , 10, 8 , 9 , 10, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 7 , 3 , 0 , 10, 7 , 0 , 9 , 10, 6 , 7 , 10, -1, -1, -1, -1 ,
        10, 6 , 7 , 1 , 10, 7 , 1 , 7 , 8 , 1 , 8 , 0 , -1, -1, -1, -1 ,
        10, 6 , 7 , 10, 7 , 1 , 1 , 7 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 6 , 1 , 6 , 8 , 1 , 8 , 9 , 8 , 6 , 7 , -1, -1, -1, -1 ,
        2 , 6 , 9 , 2 , 9 , 1 , 6 , 7 , 9 , 0 , 9 , 3 , 7 , 3 , 9 , -1 ,
        7 , 8 , 0 , 7 , 0 , 6 , 6 , 0 , 2 , -1, -1, -1, -1, -1, -1, -1 ,
        7 , 3 , 2 , 6 , 7 , 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 3 , 11, 10, 6 , 8 , 10, 8 , 9 , 8 , 6 , 7 , -1, -1, -1, -1 ,
        2 , 0 , 7 , 2 , 7 , 11, 0 , 9 , 7 , 6 , 7 , 10, 9 , 10, 7 , -1 ,
        1 , 8 , 0 , 1 , 7 , 8 , 1 , 10, 7 , 6 , 7 , 10, 2 , 3 , 11, -1 ,
        11, 2 , 1 , 11, 1 , 7 , 10, 6 , 1 , 6 , 7 , 1 , -1, -1, -1, -1 ,
        8 , 9 , 6 , 8 , 6 , 7 , 9 , 1 , 6 , 11, 6 , 3 , 1 , 3 , 6 , -1 ,
        0 , 9 , 1 , 11, 6 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        7 , 8 , 0 , 7 , 0 , 6 , 3 , 11, 0 , 11, 6 , 0 , -1, -1, -1, -1 ,
        7 , 11, 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        7 , 6 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 0 , 8 , 11, 7 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 1 , 9 , 11, 7 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        8 , 1 , 9 , 8 , 3 , 1 , 11, 7 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 1 , 2 , 6 , 11, 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, 3 , 0 , 8 , 6 , 11, 7 , -1, -1, -1, -1, -1, -1, -1 ,
        2 , 9 , 0 , 2 , 10, 9 , 6 , 11, 7 , -1, -1, -1, -1, -1, -1, -1 ,
        6 , 11, 7 , 2 , 10, 3 , 10, 8 , 3 , 10, 9 , 8 , -1, -1, -1, -1 ,
        7 , 2 , 3 , 6 , 2 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        7 , 0 , 8 , 7 , 6 , 0 , 6 , 2 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        2 , 7 , 6 , 2 , 3 , 7 , 0 , 1 , 9 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 6 , 2 , 1 , 8 , 6 , 1 , 9 , 8 , 8 , 7 , 6 , -1, -1, -1, -1 ,
        10, 7 , 6 , 10, 1 , 7 , 1 , 3 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 7 , 6 , 1 , 7 , 10, 1 , 8 , 7 , 1 , 0 , 8 , -1, -1, -1, -1 ,
        0 , 3 , 7 , 0 , 7 , 10, 0 , 10, 9 , 6 , 10, 7 , -1, -1, -1, -1 ,
        7 , 6 , 10, 7 , 10, 8 , 8 , 10, 9 , -1, -1, -1, -1, -1, -1, -1 ,
        6 , 8 , 4 , 11, 8 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 6 , 11, 3 , 0 , 6 , 0 , 4 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        8 , 6 , 11, 8 , 4 , 6 , 9 , 0 , 1 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 4 , 6 , 9 , 6 , 3 , 9 , 3 , 1 , 11, 3 , 6 , -1, -1, -1, -1 ,
        6 , 8 , 4 , 6 , 11, 8 , 2 , 10, 1 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, 3 , 0 , 11, 0 , 6 , 11, 0 , 4 , 6 , -1, -1, -1, -1 ,
        4 , 11, 8 , 4 , 6 , 11, 0 , 2 , 9 , 2 , 10, 9 , -1, -1, -1, -1 ,
        10, 9 , 3 , 10, 3 , 2 , 9 , 4 , 3 , 11, 3 , 6 , 4 , 6 , 3 , -1 ,
        8 , 2 , 3 , 8 , 4 , 2 , 4 , 6 , 2 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 4 , 2 , 4 , 6 , 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 9 , 0 , 2 , 3 , 4 , 2 , 4 , 6 , 4 , 3 , 8 , -1, -1, -1, -1 ,
        1 , 9 , 4 , 1 , 4 , 2 , 2 , 4 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        8 , 1 , 3 , 8 , 6 , 1 , 8 , 4 , 6 , 6 , 10, 1 , -1, -1, -1, -1 ,
        10, 1 , 0 , 10, 0 , 6 , 6 , 0 , 4 , -1, -1, -1, -1, -1, -1, -1 ,
        4 , 6 , 3 , 4 , 3 , 8 , 6 , 10, 3 , 0 , 3 , 9 , 10, 9 , 3 , -1 ,
        10, 9 , 4 , 6 , 10, 4 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 9 , 5 , 7 , 6 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , 4 , 9 , 5 , 11, 7 , 6 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 0 , 1 , 5 , 4 , 0 , 7 , 6 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        11, 7 , 6 , 8 , 3 , 4 , 3 , 5 , 4 , 3 , 1 , 5 , -1, -1, -1, -1 ,
        9 , 5 , 4 , 10, 1 , 2 , 7 , 6 , 11, -1, -1, -1, -1, -1, -1, -1 ,
        6 , 11, 7 , 1 , 2 , 10, 0 , 8 , 3 , 4 , 9 , 5 , -1, -1, -1, -1 ,
        7 , 6 , 11, 5 , 4 , 10, 4 , 2 , 10, 4 , 0 , 2 , -1, -1, -1, -1 ,
        3 , 4 , 8 , 3 , 5 , 4 , 3 , 2 , 5 , 10, 5 , 2 , 11, 7 , 6 , -1 ,
        7 , 2 , 3 , 7 , 6 , 2 , 5 , 4 , 9 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 5 , 4 , 0 , 8 , 6 , 0 , 6 , 2 , 6 , 8 , 7 , -1, -1, -1, -1 ,
        3 , 6 , 2 , 3 , 7 , 6 , 1 , 5 , 0 , 5 , 4 , 0 , -1, -1, -1, -1 ,
        6 , 2 , 8 , 6 , 8 , 7 , 2 , 1 , 8 , 4 , 8 , 5 , 1 , 5 , 8 , -1 ,
        9 , 5 , 4 , 10, 1 , 6 , 1 , 7 , 6 , 1 , 3 , 7 , -1, -1, -1, -1 ,
        1 , 6 , 10, 1 , 7 , 6 , 1 , 0 , 7 , 8 , 7 , 0 , 9 , 5 , 4 , -1 ,
        4 , 0 , 10, 4 , 10, 5 , 0 , 3 , 10, 6 , 10, 7 , 3 , 7 , 10, -1 ,
        7 , 6 , 10, 7 , 10, 8 , 5 , 4 , 10, 4 , 8 , 10, -1, -1, -1, -1 ,
        6 , 9 , 5 , 6 , 11, 9 , 11, 8 , 9 , -1, -1, -1, -1, -1, -1, -1 ,
        3 , 6 , 11, 0 , 6 , 3 , 0 , 5 , 6 , 0 , 9 , 5 , -1, -1, -1, -1 ,
        0 , 11, 8 , 0 , 5 , 11, 0 , 1 , 5 , 5 , 6 , 11, -1, -1, -1, -1 ,
        6 , 11, 3 , 6 , 3 , 5 , 5 , 3 , 1 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 10, 9 , 5 , 11, 9 , 11, 8 , 11, 5 , 6 , -1, -1, -1, -1 ,
        0 , 11, 3 , 0 , 6 , 11, 0 , 9 , 6 , 5 , 6 , 9 , 1 , 2 , 10, -1 ,
        11, 8 , 5 , 11, 5 , 6 , 8 , 0 , 5 , 10, 5 , 2 , 0 , 2 , 5 , -1 ,
        6 , 11, 3 , 6 , 3 , 5 , 2 , 10, 3 , 10, 5 , 3 , -1, -1, -1, -1 ,
        5 , 8 , 9 , 5 , 2 , 8 , 5 , 6 , 2 , 3 , 8 , 2 , -1, -1, -1, -1 ,
        9 , 5 , 6 , 9 , 6 , 0 , 0 , 6 , 2 , -1, -1, -1, -1, -1, -1, -1 ,
        1 , 5 , 8 , 1 , 8 , 0 , 5 , 6 , 8 , 3 , 8 , 2 , 6 , 2 , 8 , -1 ,
        1 , 5 , 6 , 2 , 1 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 3 , 6 , 1 , 6 , 10, 3 , 8 , 6 , 5 , 6 , 9 , 8 , 9 , 6 , -1 ,
        10, 1 , 0 , 10, 0 , 6 , 9 , 5 , 0 , 5 , 6 , 0 , -1, -1, -1, -1 ,
        0 , 3 , 8 , 5 , 6 , 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        10, 5 , 6 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        11, 5 , 10, 7 , 5 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        11, 5 , 10, 11, 7 , 5 , 8 , 3 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 11, 7 , 5 , 10, 11, 1 , 9 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        10, 7 , 5 , 10, 11, 7 , 9 , 8 , 1 , 8 , 3 , 1 , -1, -1, -1, -1 ,
        11, 1 , 2 , 11, 7 , 1 , 7 , 5 , 1 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , 1 , 2 , 7 , 1 , 7 , 5 , 7 , 2 , 11, -1, -1, -1, -1 ,
        9 , 7 , 5 , 9 , 2 , 7 , 9 , 0 , 2 , 2 , 11, 7 , -1, -1, -1, -1 ,
        7 , 5 , 2 , 7 , 2 , 11, 5 , 9 , 2 , 3 , 2 , 8 , 9 , 8 , 2 , -1 ,
        2 , 5 , 10, 2 , 3 , 5 , 3 , 7 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        8 , 2 , 0 , 8 , 5 , 2 , 8 , 7 , 5 , 10, 2 , 5 , -1, -1, -1, -1 ,
        9 , 0 , 1 , 5 , 10, 3 , 5 , 3 , 7 , 3 , 10, 2 , -1, -1, -1, -1 ,
        9 , 8 , 2 , 9 , 2 , 1 , 8 , 7 , 2 , 10, 2 , 5 , 7 , 5 , 2 , -1 ,
        1 , 3 , 5 , 3 , 7 , 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 7 , 0 , 7 , 1 , 1 , 7 , 5 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 0 , 3 , 9 , 3 , 5 , 5 , 3 , 7 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 8 , 7 , 5 , 9 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        5 , 8 , 4 , 5 , 10, 8 , 10, 11, 8 , -1, -1, -1, -1, -1, -1, -1 ,
        5 , 0 , 4 , 5 , 11, 0 , 5 , 10, 11, 11, 3 , 0 , -1, -1, -1, -1 ,
        0 , 1 , 9 , 8 , 4 , 10, 8 , 10, 11, 10, 4 , 5 , -1, -1, -1, -1 ,
        10, 11, 4 , 10, 4 , 5 , 11, 3 , 4 , 9 , 4 , 1 , 3 , 1 , 4 , -1 ,
        2 , 5 , 1 , 2 , 8 , 5 , 2 , 11, 8 , 4 , 5 , 8 , -1, -1, -1, -1 ,
        0 , 4 , 11, 0 , 11, 3 , 4 , 5 , 11, 2 , 11, 1 , 5 , 1 , 11, -1 ,
        0 , 2 , 5 , 0 , 5 , 9 , 2 , 11, 5 , 4 , 5 , 8 , 11, 8 , 5 , -1 ,
        9 , 4 , 5 , 2 , 11, 3 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 5 , 10, 3 , 5 , 2 , 3 , 4 , 5 , 3 , 8 , 4 , -1, -1, -1, -1 ,
        5 , 10, 2 , 5 , 2 , 4 , 4 , 2 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        3 , 10, 2 , 3 , 5 , 10, 3 , 8 , 5 , 4 , 5 , 8 , 0 , 1 , 9 , -1 ,
        5 , 10, 2 , 5 , 2 , 4 , 1 , 9 , 2 , 9 , 4 , 2 , -1, -1, -1, -1 ,
        8 , 4 , 5 , 8 , 5 , 3 , 3 , 5 , 1 , -1, -1, -1, -1, -1, -1, -1 ,
        0 , 4 , 5 , 1 , 0 , 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        8 , 4 , 5 , 8 , 5 , 3 , 9 , 0 , 5 , 0 , 3 , 5 , -1, -1, -1, -1 ,
        9 , 4 , 5 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 11, 7 , 4 , 9 , 11, 9 , 10, 11, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 8 , 3 , 4 , 9 , 7 , 9 , 11, 7 , 9 , 10, 11, -1, -1, -1, -1 ,
        1 , 10, 11, 1 , 11, 4 , 1 , 4 , 0 , 7 , 4 , 11, -1, -1, -1, -1 ,
        3 , 1 , 4 , 3 , 4 , 8 , 1 , 10, 4 , 7 , 4 , 11, 10, 11, 4 , -1 ,
        4 , 11, 7 , 9 , 11, 4 , 9 , 2 , 11, 9 , 1 , 2 , -1, -1, -1, -1 ,
        9 , 7 , 4 , 9 , 11, 7 , 9 , 1 , 11, 2 , 11, 1 , 0 , 8 , 3 , -1 ,
        11, 7 , 4 , 11, 4 , 2 , 2 , 4 , 0 , -1, -1, -1, -1, -1, -1, -1 ,
        11, 7 , 4 , 11, 4 , 2 , 8 , 3 , 4 , 3 , 2 , 4 , -1, -1, -1, -1 ,
        2 , 9 , 10, 2 , 7 , 9 , 2 , 3 , 7 , 7 , 4 , 9 , -1, -1, -1, -1 ,
        9 , 10, 7 , 9 , 7 , 4 , 10, 2 , 7 , 8 , 7 , 0 , 2 , 0 , 7 , -1 ,
        3 , 7 , 10, 3 , 10, 2 , 7 , 4 , 10, 1 , 10, 0 , 4 , 0 , 10, -1 ,
        1 , 10, 2 , 8 , 7 , 4 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 9 , 1 , 4 , 1 , 7 , 7 , 1 , 3 , -1, -1, -1, -1, -1, -1, -1 ,
        4 , 9 , 1 , 4 , 1 , 7 , 0 , 8 , 1 , 8 , 7 , 1 , -1, -1, -1, -1 ,
        4 , 0 , 3 , 7 , 4 , 3 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        4 , 8 , 7 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        9 , 10, 8 , 10, 11, 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 0 , 9 , 3 , 9 , 11, 11, 9 , 10, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 1 , 10, 0 , 10, 8 , 8 , 10, 11, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 1 , 10, 11, 3 , 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 2 , 11, 1 , 11, 9 , 9 , 11, 8 , -1, -1, -1, -1, -1, -1, -1 ,
        3 , 0 , 9 , 3 , 9 , 11, 1 , 2 , 9 , 2 , 11, 9 , -1, -1, -1, -1 ,
        0 , 2 , 11, 8 , 0 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        3 , 2 , 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 3 , 8 , 2 , 8 , 10, 10, 8 , 9 , -1, -1, -1, -1, -1, -1, -1 ,
        9 , 10, 2 , 0 , 9 , 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        2 , 3 , 8 , 2 , 8 , 10, 0 , 1 , 8 , 1 , 10, 8 , -1, -1, -1, -1 ,
        1 , 10, 2 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        1 , 3 , 8 , 9 , 1 , 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 9 , 1 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
        0 , 3 , 8 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
       -1 , -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
    };
    public static readonly int[] edgeIndexA = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3 };
    public static readonly int[] edgeIndexB = new int[] { 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };
}