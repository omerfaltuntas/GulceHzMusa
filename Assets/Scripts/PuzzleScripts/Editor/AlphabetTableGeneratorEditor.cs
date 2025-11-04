using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AlphabetTableGenerator))]
public class AlphabetTableGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AlphabetTableGenerator generator = (AlphabetTableGenerator)target;
        
        GUILayout.Space(15);

        // Updated button text and method call
        if (GUILayout.Button("Generate Alphabet Tables", GUILayout.Height(30)))
        {
            generator.GenerateTables();
        }
    }
}