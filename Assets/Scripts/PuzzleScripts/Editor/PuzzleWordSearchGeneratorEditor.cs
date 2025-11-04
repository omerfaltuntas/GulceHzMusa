using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PuzzleWordSearchGenerator))]
public class PuzzleWordSearchGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PuzzleWordSearchGenerator generator = (PuzzleWordSearchGenerator)target;

        GUILayout.Space(15);

        if (GUILayout.Button("Generate Grid", GUILayout.Height(30)))
        {
            generator.GenerateGrid();
        }

        if (GUILayout.Button("Clear Grid"))
        {
            generator.ClearGrid();
        }
    }
}