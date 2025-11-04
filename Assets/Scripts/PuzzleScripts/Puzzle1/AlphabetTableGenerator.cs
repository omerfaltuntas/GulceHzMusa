using UnityEngine;
using UnityEngine.UI;
using AgeOfKids.Localization;

public class AlphabetTableGenerator : MonoBehaviour
{

    [Header("Input")]
    [Tooltip("Enter all the alphabet letters in order, without spaces or numbers.")]
    string alphabetString;
    CustomLocalizationString AlphabetStringLocalizationKey;
    [Header("Layout Control")]
    [Tooltip("How many letters should go into the first table? The rest will go into the second.")]
    [SerializeField] private int lettersInFirstTable = 14;
    [Tooltip("The vertical (Y) position offset for the second table, creating space between them.")]
    [SerializeField] private float secondTableYOffset = -150f;

    [Header("Required Objects")]
    [Tooltip("The parent object for the tables. You position this manually.")]
    [SerializeField] private Transform tableParent;
    [Tooltip("Prefab for one table. MUST have a GridLayoutGroup.")]
    [SerializeField] private GameObject tableContainerPrefab;
    [Tooltip("Prefab for a single cell. MUST have a SimpleTableCell script.")]
    [SerializeField] private GameObject simpleCellPrefab;

    /// <summary>
    /// This method is called automatically when the game starts.
    /// </summary>
    private void Awake()
    {
         AlphabetStringLocalizationKey = GetComponent<CustomLocalizationString>();
        alphabetString = AlphabetStringLocalizationKey.GetString();
    }
    private void Start()
    {
        GenerateTables();
    }

    /// <summary>
    /// Generates the alphabet tables inside the assigned 'tableParent' using the layout settings.
    /// </summary>
    public void GenerateTables()
    {
        // 1. Validate that the user has assigned the placeholder parent.
        if (tableParent == null || tableContainerPrefab == null || simpleCellPrefab == null)
        {
            Debug.LogError("One or more required objects (Parent or Prefabs) are not assigned!", this);
            return;
        }

        // 2. Clear any previously generated tables from the placeholder.
        for (int i = tableParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(tableParent.GetChild(i).gameObject);
            else DestroyImmediate(tableParent.GetChild(i).gameObject);
        }

        // 3. Split the alphabet based on the user-defined count.
        int splitIndex = Mathf.Min(alphabetString.Length, lettersInFirstTable);
        string firstHalf = alphabetString.Substring(0, splitIndex);
        string secondHalf = (alphabetString.Length > splitIndex) ? alphabetString.Substring(splitIndex) : "";

        // 4. Generate the tables.
        // The first table is created at the parent's default position (0,0,0).
        CreateTableFor(firstHalf, 0);

        // If a second half exists, create the second table and apply the manual Y offset.
        if (!string.IsNullOrEmpty(secondHalf))
        {
            GameObject secondTableObject = CreateTableFor(secondHalf, splitIndex);
            if (secondTableObject != null)
            {
                // Apply the vertical offset to the second table's local position.
                secondTableObject.transform.localPosition = new Vector3(0, secondTableYOffset, 0);
            }
        }
    }

    /// <summary>
    /// Creates a single table, automatically configures its grid to be 2 rows, and populates it with cells.
    /// </summary>
    /// <returns>The GameObject of the created table.</returns>
    private GameObject CreateTableFor(string alphabetPart, int numberOffset)
    {
        // Instantiate the table container directly inside the pre-positioned parent.
        GameObject tableContainer = Instantiate(tableContainerPrefab, tableParent);

        // --- Automatic Grid Calculation ---
        // To get exactly two rows (one for letters, one for numbers), the number of columns
        // must be equal to the number of letters in this segment.
        int columnCount = alphabetPart.Length;

        // Get the grid layout component from the new table instance.
        GridLayoutGroup gridLayout = tableContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            // Apply the calculated column count to ensure a 2-row layout.
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columnCount;
        }
        else
        {
            Debug.LogWarning("The 'Table Container Prefab' is missing a GridLayoutGroup component. Grid constraints will not be applied.", this);
        }

        // Populate the container with cells. The GridLayoutGroup will handle positioning.
        // First, add all the letter cells. These will form the first row.
        foreach (char letter in alphabetPart)
        {
            InstantiateCell(letter.ToString(), tableContainer.transform);
        }
        // Next, add all the number cells. These will wrap to form the second row.
        for (int i = 0; i < alphabetPart.Length; i++)
        {
            int number = i + 1 + numberOffset;
            InstantiateCell(number.ToString(), tableContainer.transform);
        }

        // Return the newly created table object so it can be positioned.
        return tableContainer;
    }

    private void InstantiateCell(string text, Transform parent)
    {
        GameObject cellObject = Instantiate(simpleCellPrefab, parent);
        SimpleTableCell cell = cellObject.GetComponent<SimpleTableCell>();
        if (cell != null && cell.CellText != null)
        {
            cell.CellText.text = text;
        }
    }
}