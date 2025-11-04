using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralCrosswordGenerator))]
public class ProceduralCrosswordGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ProceduralCrosswordGenerator generator = (ProceduralCrosswordGenerator)target;

        GUILayout.Space(15);
        if (GUILayout.Button("Generate Random Crossword", GUILayout.Height(30)))
        {
            generator.GenerateCrossword();
        }
        if (GUILayout.Button("Clear Grid"))
        {
            generator.ClearGrid();
        }
    }
}