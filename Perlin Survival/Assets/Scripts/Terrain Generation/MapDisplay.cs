using System.Collections;
using UnityEngine;

/**
 * Class that displays a map texture in the scene
 */
public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    /**
     * Displays a texture
     * @param texture The texture to be displayed
     */
    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        // Makes sure the size of the plane matches with the size of the texture
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    /**
     * Draws the mesh to the MeshFilter and MeshRenderer
     * @param meshData The data of the mesh
     */
    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        // Scale mesh according to our uniform scale
        meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().terrainData.uniformScale;
    }
}
