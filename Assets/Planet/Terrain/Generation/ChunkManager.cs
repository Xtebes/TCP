using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
public class ChunkManager : MonoBehaviour
{
    #region variables
    private Queue<IEnumerator> deformQueue;
    public Transform disabledChunkPool, enabledChunkPool, disabledCrystals, enabledCrystals;
    public GameObject[] crystalBases, crystalTops;
    public Material redMat, yellowMat, orangeMat;
    public float[] crystalDistances;
    public FastNoiseSIMDUnity fastNoiseSurface, fastNoiseCavern;
    public int chunkGenerationRadius, chunkDeletionRadius;
    public int dimSize = 32;
    public int minCrystalsPerChunk, maxCrystalsPerChunk;
    public bool isFlat;
    private int dimSizeMinusOne { get { return dimSize - 1; } }
    private int volumeSize { get { return dimSize * dimSize * dimSize; } }
    public float minPlanetRadius, maxPlanetRadius, minCaveRadius, maxCaveRadius, cavernDensity, surfaceDensity;
    [Range(0,1)]
    public float isoLevel;
    public int3 corePosition;
    public Transform centerTransform;
    private int3 chunkedPosition;
    private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
    private int3[] chunkLocalPositions;
    public static readonly int3 one = new int3(1, 1, 1);
    [SerializeField]
    private Material terrainMat;
    #endregion
    public void SphereDeform(Vector3 pos, int radius, float strength, Color color)
    {   
        deformQueue.Enqueue(SphereDeformChunks(new int3((int)pos.x, (int)pos.y, (int)pos.z), radius, strength, color));
    }
    public void DestroyCrystal(Crystal crystal)
    {
        crystal.belongingChunk.data.crystals.Remove(crystal);
        List<CrystalData> crystalData = new List<CrystalData>();
        foreach (var chunkCrystal in crystal.belongingChunk.data.crystals)
        {
            crystalData.Add(chunkCrystal.data);
        }
        ChunkOperations.SerializeCrystals(crystal.belongingChunk.data.position, crystalData);
        PoolCrystal(crystal);
    }
    private IEnumerator DeformCoordinator()
    {
        while (true)
        {
            while (deformQueue.Count > 0)
            {
                yield return StartCoroutine(deformQueue.Dequeue());
            }
            yield return null;
        }
    }
    private void PoolChunkSource(Chunk chunkObject)
    {
        
        chunkObject.gameObject.SetActive(false);
        chunkObject.meshFilter.mesh.Clear();
        chunkObject.data = null;
        chunkObject.transform.parent = disabledChunkPool;
    }
    private void PoolCrystal(Crystal crystal)
    {
        crystal.gameObject.SetActive(false);
        crystal.belongingChunk = null;
        crystal.data = null;
        crystal.transform.parent = disabledCrystals;
    }
    private Crystal GetNewCrystalObject(CrystalData crystalData, Chunk chunk)
    {
        GameObject crystalObject;
        Crystal crystal;
        if (disabledCrystals.childCount > 0)
        {
            crystalObject = disabledCrystals.GetChild(0).gameObject;
            crystal = crystalObject.GetComponent<Crystal>();
        }
        else
        {
            crystalObject = Instantiate(crystalBases[UnityEngine.Random.Range(0, crystalBases.Length)]);
            Instantiate(crystalTops[UnityEngine.Random.Range(0, crystalTops.Length)], crystalObject.transform);
            crystal = crystalObject.AddComponent<Crystal>();
        }
        MeshRenderer crystalRenderer = crystalObject.transform.GetChild(0).GetComponent<MeshRenderer>();
        if (crystalData.crystalType == "red")
        {
            crystalRenderer.material = redMat;
            crystalRenderer.GetComponent<Light>().color = new Color(1, 0, 0);
        }
        else if (crystalData.crystalType == "yellow")
        {
            crystalRenderer.material = yellowMat;
            crystalRenderer.GetComponent<Light>().color = new Color(1, 1, 0);
        }
        else
        {
            crystalRenderer.material = orangeMat;
            crystalRenderer.GetComponent<Light>().color = new Color(1, 0.4f, 0);
        }
        crystal.belongingChunk = chunk;
        crystalObject.transform.position = new Vector3(chunk.transform.position.x + crystalData.position[0], chunk.transform.position.y + crystalData.position[1], chunk.transform.position.z + crystalData.position[2]);
        crystalObject.transform.up = new Vector3(crystalData.up[0], crystalData.up[1], crystalData.up[2]);
        crystalObject.SetActive(true);
        crystal.data = crystalData;
        crystalObject.transform.parent = enabledCrystals;
        return crystal;
    }
    private Chunk GetNewChunkObject(int3 pos)
    {
        Chunk chunkObject;
        if (disabledChunkPool.childCount > 0 && disabledChunkPool.GetChild(0).gameObject.activeSelf == false)
        {
            chunkObject = disabledChunkPool.GetChild(0).GetComponent<Chunk>();
        }
        else
        {
            chunkObject = new GameObject("Chunk").AddComponent<Chunk>();
            chunkObject.CreateChunkComponents();
            chunkObject.gameObject.layer = LayerMask.NameToLayer("Terrain");
            chunkObject.tag = "Terrain";
            chunkObject.gameObject.SetActive(false);
            chunkObject.meshRenderer.material = terrainMat;
        }
        chunkObject.data = new ChunkData();
        chunkObject.data.crystals = new List<Crystal>();
        chunkObject.data.position = pos;
        ChunkOperations.TryGetSerializedChunk(ref chunkObject.data);
        chunkObject.transform.position = (float3)pos;
        chunkObject.transform.parent = enabledChunkPool;
        return chunkObject;
    }
    private float[] GenNoiseForChunk(FastNoiseSIMDUnity noise, int3 pos)
    {
        return noise.fastNoiseSIMD.GetNoiseSet(pos.z, pos.y, pos.x, dimSize, dimSize, dimSize);
    }
    private IEnumerator SphereDeformChunks(int3 sphereCenter, int radius, float strength, Color color)
    {
        int3 centerChunkPos = SpatialHelpers.GetChunkPositionFromPosition(sphereCenter, dimSizeMinusOne);
        int3 chunksAffectedRadius = one * (int)math.ceil((float)radius / (float)dimSizeMinusOne) * dimSizeMinusOne;
        int3 minChunkPos = centerChunkPos - chunksAffectedRadius;
        int3 maxChunkPos = centerChunkPos + chunksAffectedRadius;
        List<Chunk> chunksToEdit = new List<Chunk>();
        for (int z = minChunkPos.z; z <= maxChunkPos.z; z += dimSizeMinusOne)
        {
            for (int y = minChunkPos.y; y <= maxChunkPos.y; y += dimSizeMinusOne)
            {
                for (int x = minChunkPos.x; x <= maxChunkPos.x; x += dimSizeMinusOne)
                {
                    int3 pos = new int3(x, y, z);
                    int3 localPosition = sphereCenter - pos;
                    int3 deformMinPosition = localPosition - one * radius;
                    int3 deformMaxPosition = localPosition + one * radius;
                    deformMinPosition = math.clamp(deformMinPosition, int3.zero, one * dimSize);
                    deformMaxPosition = math.clamp(deformMaxPosition, int3.zero, one * dimSize);
                    int3 deformDistance = deformMaxPosition - deformMinPosition;
                    if (deformDistance.x == 0 || deformDistance.y == 0 || deformDistance.z == 0) continue;
                    if (chunks.ContainsKey(pos))
                    {
                        while (chunks[pos].isGenerating)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        chunks.Add(pos, GetNewChunkObject(pos));
                        ChunkOperations.TryGetSerializedChunk(ref chunks[pos].data);
                    }
                    chunks[pos].isGenerating = true;
                    chunksToEdit.Add(chunks[pos]);
                }
            }
        }
        foreach (var chunk in chunksToEdit)
        {
            int3 pos = chunk.data.position;
            int3 localPosition = sphereCenter - pos;
            int3 deformMinPosition = localPosition - one * radius;
            int3 deformMaxPosition = localPosition + one * radius;
            deformMinPosition = math.clamp(deformMinPosition, int3.zero, one * dimSize);
            deformMaxPosition = math.clamp(deformMaxPosition, int3.zero, one * dimSize);
            if (chunk.data.noise == null)
            {
                float[] noise1 = GenNoiseForChunk(fastNoiseSurface, pos);
                float[] noise2 = GenNoiseForChunk(fastNoiseCavern, pos);
                NativeArray<bool> bools;
                NativeArray<float> postProcessedNoise;
                NoisePostProcessJob noiseJob = GetNoiseJobAtPos(pos, noise1, noise2, out bools, out postProcessedNoise);
                JobHandle noiseJobHandle = noiseJob.Schedule(noise1.Length, 64);
                noiseJobHandle.Complete();
                chunk.data.noise = postProcessedNoise.ToArray().Select( r => (byte)(r * 255)).ToArray();
                postProcessedNoise.Dispose();
                bools.Dispose();
            }
            if (chunk.data.colors == null)
            {
                chunk.data.colors = new Color32[volumeSize];
            }
            NativeArray<byte> noise = new NativeArray<byte>(chunk.data.noise, Allocator.TempJob);
            NativeArray<Color32> colors = new NativeArray<Color32>(chunk.data.colors, Allocator.TempJob);
            NativeArray<bool> bools1 = new NativeArray<bool>(2, Allocator.TempJob);
            SphereDeformJob sphereDeformJob = new SphereDeformJob();
            sphereDeformJob.colors = colors;
            sphereDeformJob.radius = radius;
            sphereDeformJob.strength = strength;
            sphereDeformJob.dimSize = dimSize;
            sphereDeformJob.deformMaxPosition = deformMaxPosition;
            sphereDeformJob.deformMinPosition = deformMinPosition;
            sphereDeformJob.localPosition = localPosition;
            sphereDeformJob.bools = bools1;
            sphereDeformJob.color = color;
            sphereDeformJob.isoLevel = isoLevel;
            sphereDeformJob.noise = noise;
            JobHandle sphereDeformHandle = sphereDeformJob.Schedule(chunk.data.noise.Length, 24);
            while (!sphereDeformHandle.IsCompleted) yield return null;
            sphereDeformHandle.Complete();
            chunk.data.noise = noise.ToArray();
            chunk.data.colors = colors.ToArray();
            colors.Dispose();
            noise.Dispose();
            bools1.Dispose();
            ChunkOperations.SerializeChunk(chunk);
        }      
        for (int i = 0; i < chunksToEdit.Count; i++)
        {
            int3 position = chunksToEdit[i].data.position;
            yield return StartCoroutine(GenChunk(chunks[position], 1, 1, 0));
        }
    }
    private NoisePostProcessJob GetNoiseJobAtPos(int3 pos, float[] noise1, float[] noise2, out NativeArray<bool> bools, out NativeArray<float> postProcessedNoise)
    {
        NoisePostProcessJob noisePostProcessJob = new NoisePostProcessJob();
        noisePostProcessJob.planetNoise = new NativeArray<float>(noise1, Allocator.TempJob);
        noisePostProcessJob.dimSize = dimSize;
        noisePostProcessJob.corePosition = corePosition;
        noisePostProcessJob.chunkPosition = pos;
        noisePostProcessJob.maxCaveRadius = maxCaveRadius;
        noisePostProcessJob.minCaveRadius = minCaveRadius;
        noisePostProcessJob.minPlanetRadius = minPlanetRadius;
        noisePostProcessJob.maxPlanetRadius = maxPlanetRadius;
        noisePostProcessJob.cavernDensity = cavernDensity;
        noisePostProcessJob.surfaceDensity = surfaceDensity;
        noisePostProcessJob.isoLevel = isoLevel;
        bools = new NativeArray<bool>(new bool[] { false, false }, Allocator.TempJob);
        noisePostProcessJob.bools = bools;
        noisePostProcessJob.caveNoise = new NativeArray<float>(noise2, Allocator.TempJob);
        postProcessedNoise = new NativeArray<float>(dimSize * dimSize * dimSize, Allocator.TempJob);
        noisePostProcessJob.postProcessedNoise = postProcessedNoise;
        return noisePostProcessJob;
    }
    private int3[] GetChunkPositionsToLoad()
    {
        List<int3> chunksToLoad = new List<int3>();
        int3 minChunk = chunkedPosition - one * dimSizeMinusOne * chunkGenerationRadius;
        int3 maxChunk = chunkedPosition + one * dimSizeMinusOne * chunkGenerationRadius;
        int maxVec = dimSizeMinusOne * chunkDeletionRadius;
        if (chunkLocalPositions == null)
        {
            for (int z = minChunk.z; z < maxChunk.z; z += dimSizeMinusOne)
            {
                for (int y = minChunk.y; y < maxChunk.y; y += dimSizeMinusOne)
                {
                    for (int x = minChunk.x; x < maxChunk.x; x += dimSizeMinusOne)
                    {
                        int3 chunkPosition = new int3(x, y, z);
                        if (math.distance(chunkPosition, chunkedPosition) < maxVec)
                            chunksToLoad.Add(chunkPosition - chunkedPosition);
                    }
                }
            }
            chunksToLoad.Sort((int3 posA, int3 posB) => SpatialHelpers.SortBySqrtMagnitude(int3.zero, posA, posB));
            chunkLocalPositions = chunksToLoad.ToArray();
            chunksToLoad.Clear();
        }
        for (int i = 0; i < chunkLocalPositions.Length; i++)
        {
            int3 chunkPosition = chunkedPosition + chunkLocalPositions[i];
            if (!chunks.ContainsKey(chunkPosition))
            {
                chunksToLoad.Add(chunkPosition);
            }
        }
        return chunksToLoad.ToArray();
    }
    private void GenCrystals(Chunk chunk)
    {
        MeshFilter meshFilter = chunk.GetComponent<MeshFilter>();
        int3 pos = chunk.data.position;
        List<CrystalData> crystalData;
        if (!ChunkOperations.TryGetSerializedCrystals(pos, out crystalData))
        {
            crystalData = new List<CrystalData>();
            int crystalCount = UnityEngine.Random.Range(minCrystalsPerChunk, maxCrystalsPerChunk);
            if (crystalCount * 5 < meshFilter.mesh.vertexCount)
            {
                for (int i = 0; i < crystalCount; i++)
                {
                    var crystal = new CrystalData();
                    int crystalIndex = UnityEngine.Random.Range(0, meshFilter.mesh.vertexCount);
                    Vector3 vertex = meshFilter.mesh.vertices[crystalIndex];
                    Vector3 up = meshFilter.mesh.normals[crystalIndex];
                    crystal.position = new float[] { vertex.x, vertex.y, vertex.z };
                    crystal.up = new float[] { up.x, up.y, up.z };
                    crystal.crystalType = "orange";
                    Vector3 crystalWorldPosition = vertex + (Vector3)(float3)pos;
                    if (Vector3.Distance(crystalWorldPosition, Vector3.zero) > crystalDistances[0])
                        crystal.crystalType = "yellow";
                    if (Vector3.Distance(crystalWorldPosition, Vector3.zero) > crystalDistances[1])
                        crystal.crystalType = "red";
                    crystal.crystalAmount = UnityEngine.Random.Range(1, 3);
                    crystalData.Add(crystal);
                }
            }
        }
        foreach (var crystal in crystalData)
        {
            chunk.data.crystals.Add(GetNewCrystalObject(crystal, chunk));
        }
        ChunkOperations.SerializeCrystals(pos, crystalData);
    }
    private IEnumerator GenChunk(Chunk chunkObject, float startAlpha, float endAlpha, float duration)
    {
        int3 pos = chunkObject.data.position;
        if (!chunks.ContainsKey(pos)) chunks.Add(pos, chunkObject);
        chunkObject.isGenerating = true;
        if (chunkObject.data.noise == null && !ChunkOperations.TryGetSerializedNoise(pos, out chunkObject.data.noise))
        {
            float[] noise1 = GenNoiseForChunk(fastNoiseSurface, pos);
            float[] noise2 = GenNoiseForChunk(fastNoiseCavern, pos);
            NativeArray<bool> bools;
            NativeArray<float> postProcessedNoise;
            NoisePostProcessJob noiseJob = GetNoiseJobAtPos(pos, noise1, noise2, out bools, out postProcessedNoise);
            JobHandle noiseJobHandle = noiseJob.Schedule(noise1.Length, 16);
            while (!noiseJobHandle.IsCompleted) yield return null;
            noiseJobHandle.Complete();
            chunkObject.data.noise = postProcessedNoise.ToArray().Select(r => (byte)(r * 255)).ToArray();
            postProcessedNoise.Dispose();
            if (!bools[0] || !bools[1])
            {
                //--The Chunk is either full of air or full of terrain, which means it doesn't need to calculate a mesh--//
                bools.Dispose();
                chunkObject.isGenerating = false;
                chunkObject.gameObject.SetActive(false);
                yield break;
            }
            bools.Dispose();
        }
        if (chunkObject.data.colors == null && !ChunkOperations.TryGetSerializedColors(pos, out chunkObject.data.colors))
        {
            chunkObject.data.colors = new Color32[volumeSize];
        }
        NativeQueue<triangle> triangles;
        MarchCubesJob marchCubesJob = MarchCubesJob.GetMarchCubesJob(chunkObject.data.noise, chunkObject.data.colors, out triangles, dimSize, isoLevel);
        JobHandle marchJobHandle = marchCubesJob.Schedule(chunkObject.data.noise.Length, 24);
        while (!marchJobHandle.IsCompleted) yield return null;
        marchJobHandle.Complete();
        if (isFlat)
        {
            NativeArray<int> meshTriangles;
            NativeArray<Vector3> meshVertices;
            NativeArray<Color32> meshColors;
            BuildMeshJob buildMeshJob = BuildMeshJob.GetBuildMeshJob(triangles, out meshTriangles, out meshVertices, out meshColors);
            JobHandle buildMeshHandle = buildMeshJob.Schedule(meshVertices.Length / 3, 24);
            while (!buildMeshHandle.IsCompleted) yield return null;
            buildMeshHandle.Complete();
            chunkObject.OverrideChunkMesh(meshTriangles.ToArray(), meshVertices.ToArray(), meshColors.ToArray());
            meshVertices.Dispose();
            meshColors.Dispose();
            meshTriangles.Dispose();
        }
        else
        {
            Dictionary<int2, int> vertexIds = new Dictionary<int2, int>();
            List<Vector3> verticeList = new List<Vector3>();
            List<Color32> colors = new List<Color32>();
            List<int> triangleList = new List<int>();
            NativeArray<triangle> triangleArray = triangles.ToArray(Allocator.Temp);
            for (int i = 0; i < triangleArray.Length; i++)
            {
                if (!vertexIds.ContainsKey(triangleArray[i].aID))
                {
                    verticeList.Add(triangleArray[i].aPos);
                    vertexIds.Add(triangleArray[i].aID, verticeList.Count - 1);
                    colors.Add(triangleArray[i].aCol);
                }
                triangleList.Add(vertexIds[triangleArray[i].aID]);

                if (!vertexIds.ContainsKey(triangleArray[i].bID))
                {
                    verticeList.Add(triangleArray[i].bPos);
                    vertexIds.Add(triangleArray[i].bID, verticeList.Count - 1);
                    colors.Add(triangleArray[i].bCol);
                }
                triangleList.Add(vertexIds[triangleArray[i].bID]);

                if (!vertexIds.ContainsKey(triangleArray[i].cID))
                {
                    verticeList.Add(triangleArray[i].cPos);
                    vertexIds.Add(triangleArray[i].cID, verticeList.Count - 1);
                    colors.Add(triangleArray[i].cCol);
                }
                triangleList.Add(vertexIds[triangleArray[i].cID]);
            }
            triangles.Dispose();
            triangleArray.Dispose();
            chunkObject.OverrideChunkMesh(triangleList.ToArray(), verticeList.ToArray(), colors.ToArray());
        }
        chunkObject.gameObject.SetActive(true);
        chunkObject.isGenerating = false;
        chunkObject.DoFade(startAlpha, endAlpha, duration);
    }
    private void Start()
    {
        string[] folders = Directory.GetDirectories(Application.persistentDataPath + Path.DirectorySeparatorChar + "Chunks");
        for (int i = 0; i < folders.Length; i++)
        {
            string[] files = Directory.GetFiles(folders[i]);
            for (int x = 0; x < files.Length; x++)
            {
                File.Delete(files[x]);
            }
            Directory.Delete(folders[i]);
        }
        deformQueue = new Queue<IEnumerator>();
        StartCoroutine(DeformCoordinator());
    }
    private IEnumerator ChunkGenerationCycle()
    {
        var positions = GetChunkPositionsToLoad();
        foreach (var pos in positions)
        {
            Chunk chunk = GetNewChunkObject(pos);
            yield return StartCoroutine(GenChunk(chunk, 0, 1, 0.3f));
            GenCrystals(chunk);
        }
    }
    private Coroutine chunkGenerationCycle;
    private void Update()
    {
        if (centerTransform != null)
        {
            int3 playerChunkedPosition = SpatialHelpers.GetChunkPositionFromPosition(centerTransform.position, dimSizeMinusOne);
            bool3 isPosDif = playerChunkedPosition != chunkedPosition;
            if (isPosDif.x || isPosDif.y || isPosDif.z)
            {
                chunkedPosition = playerChunkedPosition;
                DeleteDistantChunks();
                if (chunkGenerationCycle != null) StopCoroutine(chunkGenerationCycle);
                chunkGenerationCycle = StartCoroutine(ChunkGenerationCycle());
            }
        }
    }
    private void DeleteDistantChunks()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            KeyValuePair<int3, Chunk> chunk = chunks.ElementAt(i);
            if (math.distance(chunkedPosition, chunk.Key) >= dimSizeMinusOne * chunkDeletionRadius && !chunk.Value.isGenerating)
            {
                chunks.Remove(chunk.Key);
                chunk.Value.DoFade(1, 0, 0.3f).onComplete = () =>
                { 
                    if (chunk.Value.data.crystals != null)
                    {
                        for (int y = 0; y < chunk.Value.data.crystals.Count; y++)
                        {
                            PoolCrystal(chunk.Value.data.crystals[y]);
                        }
                    }
                    PoolChunkSource(chunk.Value);
                };
            }
        }
    }
}