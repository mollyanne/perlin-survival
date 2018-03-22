using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {
    // Scale the terrain according to player size or whatever else
    public float uniformScale = 7f;

    public bool useFlatShading;
    public bool useFalloff;

    public float meshHeightMultipiler;
    public AnimationCurve meshHeightCurve;
}
