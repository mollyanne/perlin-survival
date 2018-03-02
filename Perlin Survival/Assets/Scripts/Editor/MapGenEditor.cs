using System.Collections;
using UnityEngine;
using UnityEditor;

/**
 * Simple editor script that adds Generate button and allows the map to be updated in real time; very uninteresting
 */
[CustomEditor (typeof (MapGenerator))]
public class NewBehaviourScript : Editor {
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector()) {
            if (mapGen.autoUpdate) {
                mapGen.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate")) {
            mapGen.DrawMapInEditor();
        }
    }
}
