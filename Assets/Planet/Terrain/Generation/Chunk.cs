using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
public class Chunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    public ChunkData data;
    public Tween fadeTween;
    public bool isGenerating = false;
    public Tween DoFade(float startAlpha, float endAlpha, float duration)
    {
        meshRenderer.material.SetFloat("AlphaMultiplier", startAlpha);
        if (fadeTween != null) fadeTween.Kill();
        fadeTween = meshRenderer.material.DOFloat(endAlpha, "AlphaMultiplier", duration);
        fadeTween.Play();
        fadeTween.onComplete += ()=> meshRenderer.material.SetFloat("AlphaMultiplier", endAlpha);
        return fadeTween;
    }
    public void OverrideChunkMesh(int[] meshTriangles, Vector3[] meshVertices, Color32[] meshColors)
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh.SetVertices(meshVertices);
        meshFilter.mesh.SetTriangles(meshTriangles, 0);
        meshFilter.mesh.SetColors(meshColors);
        meshFilter.mesh.RecalculateNormals(UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers);
        meshFilter.mesh.UploadMeshData(false);
        meshCollider.sharedMesh = meshFilter.mesh;
    }
    public void CreateChunkComponents()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }
}
public static class ChunkOperations
{
    private static string GetChunkPath(int3 pos)
    {
        return Application.persistentDataPath + Path.DirectorySeparatorChar + "Chunks" + Path.DirectorySeparatorChar + pos;
    }
    private static string GetNoisePath(string folderPath)
    {
        return folderPath + Path.DirectorySeparatorChar + "noise";
    }
    private static string GetCrystalPath(string folderPath)
    {
        return folderPath + Path.DirectorySeparatorChar + "crystals";
    }
    private static string GetColorPath(string folderPath)
    {
        return folderPath + Path.DirectorySeparatorChar + "vertexColors";
    }
    public static void SerializeCrystals(int3 pos, List<CrystalData> crystals)
    {
        string chunkPath = GetChunkPath(pos);
        if (!Directory.Exists(chunkPath)) Directory.CreateDirectory(chunkPath);
        string crystalPath = GetCrystalPath(chunkPath);
        using (FileStream fl = new FileStream(crystalPath, FileMode.OpenOrCreate))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fl, crystals);
        }
    }
    public static void SerializeColors(int3 pos, Color32[] colors)
    {
        string chunkPath = GetChunkPath(pos);
        float[] serializingColors = new float[colors.Length * 4];
        for (int i = 0; i < colors.Length; i += 4)
        {
            serializingColors[i    ] = colors[i].r;
            serializingColors[i + 1] = colors[i].b;
            serializingColors[i + 2] = colors[i].r;
            serializingColors[i + 3] = colors[i].a;
        }
        if (!Directory.Exists(chunkPath)) Directory.CreateDirectory(chunkPath);
        string colorPath = GetColorPath(chunkPath);
        using (FileStream fl = new FileStream(colorPath, FileMode.OpenOrCreate))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fl, serializingColors);
        }
    }
    public static bool TryGetSerializedCrystals(int3 pos, out List<CrystalData> crystals)
    {
        crystals = null;
        string path = GetCrystalPath(GetChunkPath(pos));
        if (!File.Exists(path)) return false;
        using (FileStream fl = new FileStream(path, FileMode.Open))
        {
            BinaryFormatter bf = new BinaryFormatter();
            crystals = (List<CrystalData>)bf.Deserialize(fl);
        }
        return true;
    }
    public static bool TryGetSerializedNoise(int3 pos, out byte[] noise)
    {
        noise = null;
        string path = GetNoisePath(GetChunkPath(pos));
        if (!File.Exists(path)) return false;
        using (FileStream fl = new FileStream(path, FileMode.Open))
        {
            BinaryFormatter bf = new BinaryFormatter();
            noise = (byte[])bf.Deserialize(fl);
        }
        return true;
    }
    public static bool TryGetSerializedColors(int3 pos, out Color32[] colors)
    {
        colors = null;
        string colorPath = GetColorPath(GetChunkPath(pos));
        if (!File.Exists(colorPath)) return false;
        try
        {
            byte[] serializedColors;
            using (FileStream fl = new FileStream(colorPath, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                serializedColors = (byte[])bf.Deserialize(fl);
            }
            colors = new Color32[serializedColors.Length / 4];
            for (int i = 0; i < serializedColors.Length; i+= 4)
            {
                colors[i / 4] = new Color32(serializedColors[i], serializedColors[i + 1], serializedColors[i + 2], serializedColors[i + 3]); 
            }
            return true;
        }
        catch
        {
            colors = null;
            return false;
        }
    }
    public static void SerializeNoise(int3 pos, byte[] noise)
    {
        string chunkPath = GetChunkPath(pos);
        if (!Directory.Exists(chunkPath)) Directory.CreateDirectory(chunkPath);
        string noisePath = GetNoisePath(chunkPath);
        using (FileStream fl = new FileStream(noisePath, FileMode.OpenOrCreate))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fl, noise);
        }
    }
    public static void SerializeChunk(this Chunk chunk)
    {
        var chunkData = chunk.data;
        int3 position = new int3(chunkData.position[0], chunkData.position[1], chunkData.position[2]);
        SerializeNoise(position, chunkData.noise);
        SerializeColors(position, chunkData.colors);
    }
    public static void TryGetSerializedChunk(ref ChunkData chunkData)
    {
        TryGetSerializedNoise(chunkData.position, out chunkData.noise);
        TryGetSerializedColors(chunkData.position, out chunkData.colors);
    }
}
public class ChunkData
{
    public int3 position;
    public byte[] noise;
    public Color32[] colors;
    public List<Crystal> crystals;
}
public class Crystal : MonoBehaviour
{
    public Chunk belongingChunk;
    public CrystalData data;
}
[System.Serializable]
public class CrystalData
{
    public float[] position = new float[3], up = new float[3];
    public int crystalAmount;
    public string crystalType;
}