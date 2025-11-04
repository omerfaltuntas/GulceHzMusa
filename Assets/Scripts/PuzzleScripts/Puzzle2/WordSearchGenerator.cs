using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using AgeOfKids.Localization;

public class WordSearchGenerator : MonoBehaviour
{
    public BoxCollider2D pageCollider;
    public static WordSearchGenerator Instance;

    [Header("Word List")]
    [Tooltip("Lokalizasyon kullanlmyorsa veya anahtar kelimeler olarak kullanlacak liste.")]
    public List<string> wordsToPlace;

    // YEN: Lokalizasyon iin CustomLocalizationString referans
    [Header("Localization")]
    [Tooltip("Kelimeleri lokalize etmek iin tire (-) ile ayrlm string'i ieren CustomLocalizationString bileenini buraya srkleyin.")]
    CustomLocalizationString wordListLocalization;

    [Header("Pre-solved Word")]
    [Tooltip("Bu kelime bulmacada zaten bulunmu gibi gsterilecek.")]
    public string wordToPreSolve;

    [Header("UI")]
    [Tooltip("Bulunacak kelimelerin listelendii TextMeshPro bileeni (UI olmayan).")]
    public TextMeshPro wordListText;

    [Header("Grid Settings")]
    public bool autoSizeGrid = true;
    public int customWidth = 15;
    public int customHeight = 15;

    [Header("Prefabs")]
    public GameObject gridPrefab;
    public GameObject cellPrefab;

    [SerializeField] private List<string> originalWords;
    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;

    private char[,] gridData;
    private WordSearchCell[,] cellGrid;
    private List<WordSearchCell> currentSelection = new List<WordSearchCell>();
    private bool isSelecting = false;

    private Dictionary<string, List<Vector2Int>> placedWordLocations = new Dictionary<string, List<Vector2Int>>();

    private string initialWordListTextFormat; // GNCELLEND: Artk orijinal format tutuyoruz.

    private enum Direction { Horizontal, Vertical, Diagonal }
    private const string AllowedRandomLetters = "ABCDEFGHIJKLMNPRSTUVYZ";

    private void Awake()
    {

        wordListLocalization = GetComponent<CustomLocalizationString>();
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        if (wordListText != null)
        {
            // YEN: Balangtaki metni format olarak sakla, kelimeleri deil.
            initialWordListTextFormat = wordListText.text;
        }
    }

    private void Start()
    {
        if (transform.childCount > 0 && originalWords != null && originalWords.Count > 0)
        {
            RebuildGridReferences();
        }
        else
        {
            GenerateGrid();
        }
    }

    private void Update()
    {
        if (isSelecting && Input.GetMouseButtonUp(0))
        {
            EndSelection();
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            ClearGrid();
        }
    }
    private void OnEnable()
    {
        GenerateGrid();
    }

    private void RebuildGridReferences()
    {
        cellGrid = new WordSearchCell[gridWidth, gridHeight];
        WordSearchCell[] cells = GetComponentsInChildren<WordSearchCell>();
        foreach (var cell in cells)
        {
            cellGrid[cell.gridPosition.x, cell.gridPosition.y] = cell;
        }
        Debug.Log("Successfully rebuilt references to editor-generated grid.");
    }

    public void GenerateGrid()
    {
        ClearGrid();

        // GNCELLEND: Lokalizasyon mant eklendi
        List<string> wordsForPuzzle;

        if (wordListLocalization != null)
        {
            // Lokalize edilmi, tire ile birletirilmi string'i al
            string localizedCombinedString = wordListLocalization.GetString();

            // String'i tireye gre blerek kelime listesine dntr
            wordsForPuzzle = new List<string>(localizedCombinedString.Split('-'));

            // Eer string sonunda veya banda fazladan tire varsa oluabilecek bo elemanlar temizle
            wordsForPuzzle.RemoveAll(string.IsNullOrEmpty);
        }
        else
        {
            // Lokalizasyon scripti atanmamsa, dorudan Inspector'daki listeyi kullan
            wordsForPuzzle = new List<string>(wordsToPlace);
        }


        if (wordsForPuzzle == null || wordsForPuzzle.Count == 0) { return; }

        // GNCELLEND: Oyunun geri kalan bu lokalize edilmi listeyi kullanacak
        originalWords = new List<string>(wordsForPuzzle);

        // YEN: UI' lokalize edilmi kelimelerle doldur
        PopulateWordListUI(originalWords);

        var sortedWords = originalWords.Where(w => !string.IsNullOrEmpty(w)).Select(w => w.ToUpper()).OrderByDescending(w => w.Length).ToList();

        if (autoSizeGrid)
        {
            int longestWordLength = sortedWords.Any() ? sortedWords[0].Length : 10;
            int wordCount = sortedWords.Count;
            gridHeight = longestWordLength + 2;
            gridWidth = Mathf.Max(wordCount + 2, gridHeight + 1);
        }
        else
        {
            gridWidth = customWidth;
            gridHeight = customHeight;
        }

        gridData = new char[gridWidth, gridHeight];
        cellGrid = new WordSearchCell[gridWidth, gridHeight];

        placedWordLocations.Clear();

        foreach (string word in sortedWords)
        {
            bool placed = TryPlaceWord(word, gridWidth, gridHeight);
            if (!placed)
            {
                Debug.LogWarning($"Failed to place word: '{word}'. Grid might be too crowded. Try making it larger or removing a word.");
            }
        }

        FillEmptyCells(gridWidth, gridHeight);
        InstantiateGrid(gridWidth, gridHeight);

        MarkPreSolvedWord();
    }

    // YEN: Kelime listesi UI'n balangta doldurmak iin yeni bir metot
    private void PopulateWordListUI(List<string> words)
    {
        if (wordListText != null)
        {
            // Kelimeleri aralarna iki boluk koyarak birletir ve UI text'ine ata.
            wordListText.text = string.Join("  ", words);
        }
    }

    private void EndSelection()
    {
        isSelecting = false;

        if (originalWords == null || originalWords.Count == 0)
        {
            foreach (var cell in currentSelection) { cell.SetSelected(false); }
            currentSelection.Clear();
            return;
        }

        string selectedWord = "";
        foreach (var cell in currentSelection) { selectedWord += cell.Letter; }

        string reversedWord = new string(selectedWord.Reverse().ToArray());

        // GNCELLEND: originalWords listesi artk lokalize kelimeleri ierdii iin bu kontrol doru alacaktr.
        string foundWord = originalWords.FirstOrDefault(w => w.ToUpper() == selectedWord || w.ToUpper() == reversedWord);
        bool isCorrect = (foundWord != null);

        foreach (var cell in currentSelection)
        {
            if (isCorrect) { cell.PlayCorrectAnimation(); }
            else { cell.SetSelected(false); }
        }

        if (isCorrect)
        {
            StrikeThroughWordInUI(foundWord);
        }

        pageCollider.enabled = true;
        currentSelection.Clear();
    }

    // GNCELLEND: Metot ismi daha anlalr olacak ekilde deitirildi (UpdateWordListUI -> StrikeThroughWordInUI)
    private void StrikeThroughWordInUI(string wordToMark)
    {
        if (wordListText == null || string.IsNullOrEmpty(wordToMark)) return;

        string currentText = wordListText.text;
        // Kelimenin stn izmek iin Regex'e kar daha dayankl bir deitirme yaps
        string replacement = $"<s><color=red>{wordToMark}</color></s>";
        string pattern = $@"\b{Regex.Escape(wordToMark)}\b";

        wordListText.text = Regex.Replace(currentText, pattern, replacement, RegexOptions.IgnoreCase);
    }

    private void MarkPreSolvedWord()
    {
        if (string.IsNullOrEmpty(wordToPreSolve)) return;

        // YEN: nceden zlecek kelimenin de lokalize edilmi olmas gerekebilir.
        // Bu rnekte, wordToPreSolve string'inin de doru dilde yazld varsaylmtr.
        // Eer bu da dinamik olacaksa, benzer bir lokalizasyon mant buraya da eklenebilir.
        string upperWord = wordToPreSolve.ToUpper();

        if (placedWordLocations.ContainsKey(upperWord))
        {
            List<Vector2Int> locations = placedWordLocations[upperWord];

            foreach (Vector2Int pos in locations)
            {
                cellGrid[pos.x, pos.y].PlayCorrectAnimation();
            }

            StrikeThroughWordInUI(wordToPreSolve);
        }
        else
        {
            Debug.LogWarning($"nceden zlm olarak ayarlanan '{wordToPreSolve}' kelimesi yerletirilen kelimeler arasnda bulunamad. Lokalize edilmi kelime listesinde olduundan emin olun.");
        }
    }

    private bool TryPlaceWord(string word, int width, int height)
    {
        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            Direction direction = (Direction)Random.Range(0, 2);

            int startX = Random.Range(0, width);
            int startY = Random.Range(0, height);

            if (CanPlaceWordAt(word, startX, startY, direction, width, height))
            {
                PlaceWordAt(word, startX, startY, direction);
                return true;
            }
        }
        return false;
    }

    #region Unchanged Code
    private bool CanPlaceWordAt(string word, int x, int y, Direction dir, int width, int height)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int currentX = x, currentY = y;
            if (dir == Direction.Horizontal) currentX += i;
            else if (dir == Direction.Vertical) currentY += i;
            else if (dir == Direction.Diagonal) { currentX += i; currentY += i; }

            if (currentX < 0 || currentX >= width || currentY < 0 || currentY >= height) return false;

            if (gridData[currentX, currentY] != '\0') return false;
        }
        return true;
    }
    public void StartSelection(WordSearchCell cell)
    {
        pageCollider.enabled = false;
        isSelecting = true;
        currentSelection.Clear();
        currentSelection.Add(cell);
        cell.SetSelected(true);
    }

    public void AddToSelection(WordSearchCell cell)
    {
        if (!isSelecting || currentSelection.Contains(cell)) return;
        currentSelection.Add(cell);
        cell.SetSelected(true);
    }
    private void InstantiateGrid(int width, int height)
    {
        GameObject gridInstance = Instantiate(gridPrefab, transform);
        gridInstance.name = "Word Search Grid";

        GridLayoutGroup gridLayout = gridInstance.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cellInstance = Instantiate(cellPrefab, gridInstance.transform);
                cellInstance.name = $"Cell ({x}, {y})";

                WordSearchCell cell = cellInstance.GetComponent<WordSearchCell>();
                cell.SetLetter(gridData[x, y]);
                cell.gridPosition = new Vector2Int(x, y);
                cellGrid[x, y] = cell;
            }
        }
    }

    public void ClearGrid()
    {
        currentSelection.Clear();
        isSelecting = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    private void PlaceWordAt(string word, int x, int y, Direction dir)
    {
        List<Vector2Int> wordCells = new List<Vector2Int>();

        for (int i = 0; i < word.Length; i++)
        {
            int currentX = x, currentY = y;
            if (dir == Direction.Horizontal) currentX += i;
            else if (dir == Direction.Vertical) currentY += i;
            else if (dir == Direction.Diagonal) { currentX += i; currentY += i; }

            gridData[currentX, currentY] = word[i];

            wordCells.Add(new Vector2Int(currentX, currentY));
        }

        placedWordLocations[word] = wordCells;
    }

    private void FillEmptyCells(int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridData[x, y] == '\0')
                {
                    gridData[x, y] = AllowedRandomLetters[Random.Range(0, AllowedRandomLetters.Length)];
                }
            }
        }
    }
    #endregion
}