using System.Collections;
using UnityEngine;

/**
 * Class that generates a falloff map which can be used to make a map surrounded by water or some other flat area
 */
public static class FalloffGenerator {
    /**
     * Generates the falloff map
     * @param size The dimensions of the falloff map (it will be a square)
     * @return The falloff map
     */
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                float i = x / (float)size * 2 - 1;
                float j = y / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));

                map[x, y] = Curve(value);
            }
        }

        return map;
    }

    static float Curve(float value)
    {
        // a and b can be changed to modify how much of the map is water and how much is land
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
