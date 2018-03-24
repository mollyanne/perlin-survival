using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    public Color[] baseColors;
    [Range(0, 1)]
    public float[] baseStartHeights;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        material.SetInt("baseColorCount", baseColors.Length);
        material.SetColorArray("baseColors", baseColors);
        material.SetFloatArray("baseStartHeights", baseStartHeights);

        // Save previous height values so the texture can be updated properly
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }
}
