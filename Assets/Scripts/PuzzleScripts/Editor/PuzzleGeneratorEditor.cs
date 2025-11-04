using UnityEngine;
using UnityEditor; // Required for editor scripts

/// <summary>
/// Creates a custom inspector for the PuzzleGenerator class,
/// adding buttons to trigger grid generation and clearing.
/// </summary>
[CustomEditor(typeof(PuzzleGenerator))]
public class PuzzleGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (columns, rows, prefabs, etc.)
        DrawDefaultInspector();

        // Get a reference to the script we are creating an editor for.
        PuzzleGenerator generator = (PuzzleGenerator)target;

        // Add some space for better layout.
        GUILayout.Space(15);

        // Add a button. The code inside the if-statement runs when the button is clicked.
        if (GUILayout.Button("Generate Grid", GUILayout.Height(30)))
        {
            // Call the public method from our main script.
            generator.GenerateGrid();
        }

        // Add a button for clearing the grid.
        if (GUILayout.Button("Clear Grid"))
        {
            generator.ClearGrid();
        }
    }
}
