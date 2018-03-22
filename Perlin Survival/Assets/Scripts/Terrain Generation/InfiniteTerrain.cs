using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Provides the functionality for creating infinitely generating terrain using chunking around a viewer
 */
public class InfiniteTerrain : MonoBehaviour
{
    // Distance the viewer must move to initiate a chunk update
    const float distanceThresholdForChunkUpdate = 25f;
    const float sqrDistanceThresholdForChunkUpdate = distanceThresholdForChunkUpdate * distanceThresholdForChunkUpdate;

    // Array of detail levels set in the editor
    public LODInfo[] detailLevels;
    // View distance affects the number of chunks that will be loaded around the viewer
    public static float maxViewDistance;
    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewerPosition;
    Vector2 oldViewerPosition;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInView;

    // Lists to keep track of currently visible chunks and previously visible chunks
    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksPreviouslyVisible = new List<TerrainChunk>();

    /**
     * Start method, which initializes the chunk size and the amount of visible chunks
     */
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        // Set the max distance according to the furthest detail level away
        maxViewDistance = detailLevels[detailLevels.Length - 1].distanceThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    /**
     * Update method, which updates the viewer position and updates the chunks visible by the viewer
     */
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        if ((oldViewerPosition - viewerPosition).sqrMagnitude > distanceThresholdForChunkUpdate) {
            oldViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    /**
     * Method that adds chunks to the lists if they are within a certain distance of the viewer,
     * clears and resets the previously visible chunks,
     * and updates already visible chunks
     */
    void UpdateVisibleChunks()
    {
        // Disable chunks that are no longer visible
        for (int i = 0; i < terrainChunksPreviouslyVisible.Count; i++) {
            terrainChunksPreviouslyVisible[i].SetVisible(false);
        }

        // Clear previously visible chunks
        terrainChunksPreviouslyVisible.Clear();

        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Calculate surrounding chunks and update them
        for (int yOffset = -chunksVisibleInView; yOffset <= chunksVisibleInView; yOffset++) {
            for (int xOffset = -chunksVisibleInView; xOffset <= chunksVisibleInView; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (terrainChunks.ContainsKey(viewedChunkCoord)) {
                    terrainChunks[viewedChunkCoord].UpdateTerrainChunk();
                } else {
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
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
        bool mapDataRecieved;
        int previousLODIndex = -1;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        /**
         * Constructor; initializes all default values
         */
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = position3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            // Default setting is invisible
            SetVisible(false);

            // Setup variable LOD meshes for chunk
            lodMeshes = new LODMesh[detailLevels.Length];

            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);

                if (detailLevels[i].useForCollider) {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        /**
         * Method for keeping track of whether or not map data has been recieved for convenience
         */
        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;

            UpdateTerrainChunk();
        }

        /**
         * Calculates the visibility of the chunk and sets it accordingly
         */
        public void UpdateTerrainChunk()
        {
            if (mapDataRecieved) {
                float viewerDistFromNearestChunk = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistFromNearestChunk <= maxViewDistance;

                if (visible) {
                    int lodIndex = 0;

                    // Calculates the index of the array of detail levels that each chunk should display
                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDistFromNearestChunk > detailLevels[i].distanceThreshold) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    // Set mesh according to the correct index calculated above
                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];

                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        } else if (!lodMesh.hasRequestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    if (lodIndex == 0) {
                        if (collisionLODMesh.hasMesh) {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        } else if (!collisionLODMesh.hasRequestedMesh) {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }

                    // Add currently visible chunk to previous list for next update
                    terrainChunksPreviouslyVisible.Add(this);
                }

                SetVisible(visible);
            }
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

    /**
     * Class that controls the level of detail for a mesh
     */
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        System.Action updateCallback;

        int lod;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.updateCallback = updateCallback;
            this.lod = lod;
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;

            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }

    /**
     * Struct containing the info that specifies the LOD of an area and how far away it has to be to qualify; modifiable in the editor
     */
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float distanceThreshold;
        public bool useForCollider;
    }
}
