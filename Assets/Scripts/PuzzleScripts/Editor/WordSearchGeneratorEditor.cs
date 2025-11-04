using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates a custom inspector for the WordSearchGenerator class.
/// </summary>
[CustomEditor(typeof(WordSearchGenerator))]
public class WordSearchGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WordSearchGenerator generator = (WordSearchGenerator)target;

        GUILayout.Space(15);

        if (GUILayout.Button("Generate Word Search", GUILayout.Height(30)))
        {
            generator.GenerateGrid();
        }

        if (GUILayout.Button("Clear Grid"))
        {
            generator.ClearGrid();
        }
    }
}