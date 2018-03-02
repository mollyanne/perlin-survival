using System.Collections;
using UnityEngine;

/**
 * Class that creates textures from various inputs
 */
public static class TextureGenerator {
    /**
     * Draws the noise map using a map of color values given by the regions specified in MapGenerator.cs
     */
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);

        // Texture settings
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // Texture setup
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    /**
     * Draws the noise map using a map of color values between 0 (black) and 1 (white)
     */
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        // Set the width and height of the texture
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // Color map initialized
        Color[] colorMap = new Color[width * height];

        // Map the values of the noise map to a greyscale color value
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }
}
