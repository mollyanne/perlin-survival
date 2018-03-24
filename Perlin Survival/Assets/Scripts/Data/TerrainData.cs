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

    // Minimum height of the mesh
    public float MinHeight {
        get {
            return uniformScale * meshHeightMultipiler * meshHeightCurve.Evaluate(0);
        }
    }

    // Maximum height of the mesh
    public float MaxHeight {
        get {
            return uniformScale * meshHeightMultipiler * meshHeightCurve.Evaluate(1);
        }
    }
}
