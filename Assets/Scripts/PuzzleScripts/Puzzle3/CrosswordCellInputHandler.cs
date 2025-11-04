using UnityEngine;
using TMPro;
[RequireComponent(typeof(TMP_InputField))]
public class CrosswordCellInputHandler : MonoBehaviour
{
    public ProceduralCrosswordCell myCell;
    private TMP_InputField inputField;
private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    private void OnInputValueChanged(string newText)
    {
        // Limit to a single character
        if (newText.Length > 1)
        {
            inputField.text = newText.Substring(0, 1);
            return;
        }

        if (newText.Length > 0)
        {
            // Character was entered: move to the next cell
            myCell.generator.FocusNextAvailableCell(myCell.gridPosition);
            myCell.generator.CheckActiveWordCompletion();
        }
        else
        {
            // Character was deleted from THIS cell: move to the previous cell
            myCell.generator.FocusPreviousAvailableCell(myCell.gridPosition);
        }
    }

    private void Update()
    {
        // This is now for a very specific action:
        // When the current cell is EMPTY and the user presses Backspace,
        // we want to clear the character in the PREVIOUS cell.
        if (inputField.isFocused && Input.GetKeyDown(KeyCode.Backspace) && string.IsNullOrEmpty(inputField.text))
        {
            myCell.generator.FocusAndClearPreviousCell(myCell.gridPosition);
        }
    }
}