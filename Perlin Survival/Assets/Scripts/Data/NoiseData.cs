using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {
    public Noise.NormalizeMode normalizeMode;

    public float noiseScale;

    public int octaves;
    // Range part is to transform it into a slider with a range [0, 1]
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    protected override void OnValidate()
    {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 1) {
            octaves = 1;
        }

        base.OnValidate();
    }
}
