using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(MapPreview))]
public class MapPreviewEditor : Editor {
    public override void OnInspectorGUI() {
        MapPreview mapGen = (MapPreview)target;

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