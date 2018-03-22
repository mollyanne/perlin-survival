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
     * @param _heightCurve The curve that determines the level of effect the heightMultiplier has on a given elevation
     * @param levelOfDetail A value that determines the number of vertices and triangles in the mesh
     * @param useFlatShading Bool that determines if flat shading is enables or not
     * @return meshData The data of the mesh
     */
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading)
    {
        // Created a new animation curve to avoid a weird issue from occurring
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // Implement level of detail; avoid the zero case
        int meshIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - (2 * meshIncrement);
        int meshSizeUnsimplified = borderedSize - 2;

        // Values used in calculating the mesh vertices
        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = ((meshSize - 1) / meshIncrement) + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderedVertexIndex = -1;

        // Implement border indices and mesh indices
        for (int y = 0; y < borderedSize; y += meshIncrement) {
            for (int x = 0; x < borderedSize; x += meshIncrement) {
                bool isBorderVertex = (y == 0) || (y == borderedSize - 1) || (x == 0) || (x == borderedSize - 1);

                if (isBorderVertex) {
                    vertexIndicesMap[x, y] = borderedVertexIndex;
                    borderedVertexIndex--;
                } else {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // Where the mesh values are all assigned
        for (int y = 0; y < borderedSize; y += meshIncrement) {
            for (int x = 0; x < borderedSize; x += meshIncrement) {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector2 percent = new Vector2((x - meshIncrement) / (float)meshSize, (y - meshIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + (percent.x * meshSizeUnsimplified), height, topLeftZ - (percent.y * meshSizeUnsimplified));

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < (borderedSize - 1) && y < (borderedSize - 1)) {
                    // Vertices for our two triangles
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshIncrement, y];
                    int c = vertexIndicesMap[x, y + meshIncrement];
                    int d = vertexIndicesMap[x + meshIncrement, y + meshIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }
}

/**
 * Class that stores all the data for the mesh we want to create
 */
public class MeshData
{
    // These arrays are all for use with the Mesh class Unity uses (see CreateMesh method)
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uv;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    /**
     * Constructor for the class
     * @param verticesPerLine The dimensions of the mesh
     * @param useFlatShading Bool that determines whether or not flat shading is used
     */
    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uv = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[verticesPerLine * 24];
    }

    /**
     * Adds the border and mesh vertices to their respective arrays
     * @param vertexPosition The position of the vertex
     * @param uv The uv coordinate of the vertex
     * @param vertexIndex The index of vertex in the array
     */
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0) {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        } else {
            vertices[vertexIndex] = vertexPosition;
            this.uv[vertexIndex] = uv;
        }
    }

    /**
     * Method that enables the adding of triangles to the mesh
     * @param vertex[1,2,3] The vertices of the triangle
     */
    public void AddTriangle(int vertex1, int vertex2, int vertex3)
    {
        if (vertex1 < 0 || vertex2 < 0 || vertex3 < 0) {
            borderTriangles[borderTriangleIndex] = vertex1;
            borderTriangles[borderTriangleIndex + 1] = vertex2;
            borderTriangles[borderTriangleIndex + 2] = vertex3;

            borderTriangleIndex += 3;
        } else {
            triangles[triangleIndex] = vertex1;
            triangles[triangleIndex + 1] = vertex2;
            triangles[triangleIndex + 2] = vertex3;

            triangleIndex += 3;
        }
    }

    /**
     * Calculates the normals of the mesh
     * @return Array of normals at each vertex
     */
    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        // Calculate mesh triangle normals
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;

        // Calculate border triangle normals
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    /**
     * Method that calculates the surface normal of a triangle given indices of the vertices array
     * @param indexA The first index of the triangle
     * @param indexB The second index of the triangle
     * @param indexC The third index of the triangle
     * @return Surface normal vector
     */
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        // Check each vertex to see if its a border vertex
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    /**
     * Finalizes the mesh based on whether or not flat shading is enabled
     */
    public void ProcessMesh()
    {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
        }
    }

    /**
     * Method that calls the CalculateNormals method; called within a separate thread for increased performance
     */
    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    /**
     * Method that enables flat shading by increasing the number of vertices per two triangles from 4 to 6
     */
    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUV = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUV[i] = uv[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uv = flatShadedUV;
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
        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
