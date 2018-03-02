using System.Collections;
using UnityEngine;

/**
 * Class that creates a mesh made out of triangles for use in terrain
 */
public static class MeshGenerator
{
    /**
     * Method that generates the mesh values, including the vertices and triangles
     * @param heightMap The height map which supplies height values to the mesh
     * @param heightMultiplier Value that is multiplied with the height; allows the elevation of the terrain to be more visible
     * @param heightCurve The curve that determines the level of effect the heightMultiplier has on a given elevation
     * @param levelOfDetail A value that determines the number of vertices and triangles in the mesh
     * @return meshData The data of the mesh
     */
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        // Created a new animation curve to avoid a weird issue from occurring
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        // Values used in calculating the mesh vertices
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        // Implement level of detail; avoid the zero case
        int meshIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = ((width - 1) / meshIncrement) + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        // Where the mesh values are all assigned
        for (int y = 0; y < height; y+= meshIncrement) {
            for (int x = 0; x < width; x+= meshIncrement) {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uv[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < (width - 1) && y < (height - 1)) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

/**
 * Class that stores all the data for the mesh we want to create
 */
public class MeshData
{
    // These arrays are all for use with the Mesh class Unity uses (see CreateMesh method)
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uv;

    int triangleIndex;

    /**
     * Constructor for the class
     * @param meshWidth The width of the mesh
     * @param meshHeight The height of the mesh
     */
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uv = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

    }

    /**
     * Method that enables the adding of triangles to the mesh
     * @param vertex[1,2,3] The vertices of the triangle
     */
    public void AddTriangle(int vertex1, int vertex2, int vertex3)
    {
        triangles[triangleIndex] = vertex1;
        triangles[triangleIndex + 1] = vertex2;
        triangles[triangleIndex + 2] = vertex3;

        triangleIndex += 3;
    }

    /**
     * Method that actually creates a Mesh object
     * @return the newly-created mesh
     */
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        // Makes the lighting and shadows fit with the new heights, etc.
        mesh.RecalculateNormals();

        return mesh;
    }
}
