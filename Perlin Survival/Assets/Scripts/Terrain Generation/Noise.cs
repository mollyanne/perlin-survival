using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class that handles the noise in the creation of terrain
 */ 
public static class Noise
{
    public enum NormalizeMode {Local, Global}

    /**
     * Method that generates the noise map (a 2D representation of what the 3D terrarin will look like)
     * @param mapChunkSize Width of the generated map
     * @param mapHeight Height of the generated map
     * @param seed The unique seed which determines the appearance of the map
     * @param scale The scale (or zoom) of the map
     * @param octaves Variable that controls the amount of "fine details" in the map
     * @param persistence Controls the amount that the amplitude of the octaves increases
     * @param lacunarity Controls the amount that the frequency of the octaves increases
     * @param offset Allows the generated terrain to be "scrolled"
     * @param normalizeMode The mode in which the terrain heights are normalized based on a curve
     */
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        // Noise map initialized
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Random number generator for the offset
        System.Random rng = new System.Random(seed);
        // Allows each octave to sample from a different point in the noise
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        // Set the octave offsets
        for (int i = 0; i < octaves; i++) {
            float offsetX = rng.Next(-100000, 100000) + offset.x;
            float offsetY = rng.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        // Divide-by-zero preventificator
        if (scale <= 0) {
            scale = 0.0001f;
        }

        // Variables for keeping track of local max and min noise (terrain) heights
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // For use in aligning the noise scale to the center rather than top right
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Noise loop
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                // Default values
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // Applying the octaves
                for (int i = 0; i < octaves; i++) {
                    // Applying all functionality to each coordinate in the noise map
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // Perlin noise calculation, allowed to use negative values for increased visual appeal
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // Noise height set accordingly
                    noiseHeight += perlinValue * amplitude;

                    // More functionality of the various variables
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Set max and min noise heights if need be
                if (noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                } else if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }

                // Actual assigning of the value to the noise map
                noiseMap[x, y] = noiseHeight;
            }
        }

        // Second pass on the noise map values to apply a reverse linear interpolation (a method of fitting the values to the curve we want)
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                if (normalizeMode == NormalizeMode.Local) {
                    // Map is not infinite
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                } else {
                    // Map is infinite
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
