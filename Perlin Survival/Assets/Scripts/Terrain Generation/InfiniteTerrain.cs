using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Provides the functionality for creating infinitely generating terrain using chunking around a viewer
 */
public class InfiniteTerrain : MonoBehaviour
{
    // View distance affects the number of chunks that will be loaded around the viewer
    public const float maxViewDistance = 450;

    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInView;

    // Lists to keep track of currently visible chunks and previously visible chunks
    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksPreviouslyVisible = new List<TerrainChunk>();

    /**
     * Start method, which initializes the chunk size and the amount of visible chunks
     */
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    /**
     * Update method, which updates the viewer position and updates the chunks visible by the viewer
     */
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    /**
     * Method that adds chunks to the lists if they are within a certain distance of the viewer,
     * clears and resets the previously visible chunks,
     * and updates already visible chunks
     */
    void UpdateVisibleChunks()
    {
        // Disable chunks no longer visible
        for (int i = 0; i < terrainChunksPreviouslyVisible.Count; i++) {
            terrainChunksPreviouslyVisible[i].SetVisible(false);
        }

        // Clear previously visible chunks
        terrainChunksPreviouslyVisible.Clear();

        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Calculate surrounding chunks and either add them to the list or update them
        for (int yOffset = -chunksVisibleInView; yOffset <= chunksVisibleInView; yOffset++) {
            for (int xOffset = -chunksVisibleInView; xOffset <= chunksVisibleInView; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (terrainChunks.ContainsKey(viewedChunkCoord)) {
                    terrainChunks[viewedChunkCoord].UpdateTerrainChunk();

                    if (terrainChunks[viewedChunkCoord].IsVisible()) {
                        terrainChunksPreviouslyVisible.Add(terrainChunks[viewedChunkCoord]);
                    }
                } else {
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }

    /**
     * Class that enables the terrain chunking functionality
     */
    public class TerrainChunk
    {
        // MeshObject that will become the chunk itself in the form of a plane
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        /**
         * Constructor; initializes all default values
         */
        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = position3;
            meshObject.transform.parent = parent;

            // Default setting is invisible
            SetVisible(false);

            mapGenerator.RequestMapData(OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataRecieved);
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        /**
         * Calculates the visibility of the chunk and sets it accordingly
         */
        public void UpdateTerrainChunk()
        {
            float viewerDistFromNearestChunk = bounds.SqrDistance(viewerPosition);
            bool visible = viewerDistFromNearestChunk <= (maxViewDistance * maxViewDistance);

            SetVisible(visible);
        }

        /**
         * Toggles the chunk's visibility
         */ 
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        /**
         * Checks whether the chunk is visible
         */
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
}
