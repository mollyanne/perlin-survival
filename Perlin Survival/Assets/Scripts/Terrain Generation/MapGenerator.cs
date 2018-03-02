using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

/**
 * Map generator class that is used as an object in the scene to set the values of each variable, etc.
 */
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh };
    public DrawMode drawMode;

    // Basically all of these are explained in Noise.cs
    public const int mapChunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;
    public float noiseScale;

    // Range part is to transform it into a slider with a range [0, 1]
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultipler;
    public AnimationCurve meshHeightCurve;

    // Creates a tick box that can be checked to update the map in real time
    public bool autoUpdate;

    // List of regions specified
    public TerrarinType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfo = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfo = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // Draw mode selection
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultipler, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();

        // Makes it so that only one thread at a time may access the queue
        lock (mapDataThreadInfo) {
            mapDataThreadInfo.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultipler, meshHeightCurve, levelOfDetail);

        lock (meshDataThreadInfo) {
            meshDataThreadInfo.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfo.Count > 0) {
            for (int i = 0; i < mapDataThreadInfo.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfo.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfo.Count > 0) {
            for (int i = 0; i < meshDataThreadInfo.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfo.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    /**
     * Method that generates the noise map and color map
     */
    MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        // Color map generation based on the height map
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight <= regions[i].height) {
                        // Set color map values based on region heights and colors
                        colorMap[y * mapChunkSize + x] = regions[i].color;

                        // Short circuit loop if the region is found
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    /**
     * Places constraints on some of the values to avoid weird behavior
     */
    void OnValidate()
    {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 1) {
            octaves = 1;
        }
    }

    struct MapThreadInfo<T>
    {
        // Struct data should be immutable, hence readonly
        // Generic type so it can hold MapData or MeshData
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

/**
 * Adds customizable region areas that are mapped to certain colors
 */
[System.Serializable]
public struct TerrarinType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}