using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using DG.Tweening;
using AgeOfKids.Localization;

public class PuzzleWordSearchGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 11;
    [SerializeField] private int rows = 10;

    [Header("Puzzle Data")]
    [Tooltip("Lokalizasyon kullanýlmýyorsa, boþluklarý doldurmak için kullanýlacak harfler.")]
    [SerializeField] private string fillerLetters;
    [Tooltip("Lokalizasyon kullanýlmýyorsa, grid'e gizlenecek kelimelerin listesi.")]
    [SerializeField] private List<string> solutionWords;

    // YENÝ: Lokalizasyon için referans alanlarý
    [Header("Localization")]
    [Tooltip("Doldurma harflerini lokalize etmek için CustomLocalizationString bileþenini buraya sürükleyin.")]
    [SerializeField] private CustomLocalizationString fillerLettersLocalization;
    [Tooltip("Çözüm kelimelerini (tire ile ayrýlmýþ) lokalize etmek için CustomLocalizationString bileþenini buraya sürükleyin.")]
    [SerializeField] private CustomLocalizationString solutionWordsLocalization;

    [Header("Object Prefabs")]
    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private GameObject cellPrefab;

    [Header("Win Animation")]
    [SerializeField] private float delayBetweenShines = 0.05f;

    private List<PuzzleWordSearchCell> allCells = new List<PuzzleWordSearchCell>();

    private void OnEnable()
    {
        GenerateGrid();
    }

    private void OnDisable()
    {
        ClearGrid();
    }

    public void GenerateGrid()
    {
        ClearGrid();

        // GÜNCELLENDÝ: Lokalize edilmiþ veya varsayýlan veriyi belirle
        string finalFillerLetters;
        List<string> finalSolutionWords;

        // Doldurma harfleri için lokalizasyonu kontrol et
        if (fillerLettersLocalization != null && !string.IsNullOrEmpty(fillerLettersLocalization.GetString()))
        {
            finalFillerLetters = fillerLettersLocalization.GetString();
        }
        else
        {
            finalFillerLetters = fillerLetters; // Yedek olarak Inspector'daki deðeri kullan
        }

        // Çözüm kelimeleri için lokalizasyonu kontrol et
        if (solutionWordsLocalization != null && !string.IsNullOrEmpty(solutionWordsLocalization.GetString()))
        {
            string combinedWords = solutionWordsLocalization.GetString();
            finalSolutionWords = new List<string>(combinedWords.Split('-'));
            finalSolutionWords.RemoveAll(string.IsNullOrEmpty); // Olasý boþ girdileri temizle
        }
        else
        {
            finalSolutionWords = new List<string>(solutionWords); // Yedek olarak Inspector'daki listeyi kullan
        }

        // GÜNCELLENDÝ: Doðrulama artýk nihai veri listeleri üzerinden yapýlýyor
        if (!ValidateInputs(finalFillerLetters, finalSolutionWords)) return;

        // --- 1. Create a "Blueprint" of the Grid ---
        char[] gridChars = new char[rows * columns];
        HashSet<int> solutionIndices = new HashSet<int>();

        // --- 2. Place Solution Words with Ordered Letters and Random Spacing ---
        // GÜNCELLENDÝ: `finalSolutionWords` kullanýlýyor
        for (int i = 0; i < finalSolutionWords.Count; i++)
        {
            if (i >= rows) break;

            string word = finalSolutionWords[i].ToUpper();
            int wordLength = word.Length;
            int rowIndex = i;

            List<int> availableColumns = Enumerable.Range(0, columns).ToList();
            Shuffle(availableColumns);
            List<int> chosenColumns = availableColumns.GetRange(0, wordLength);
            chosenColumns.Sort();

            for (int j = 0; j < wordLength; j++)
            {
                int placementColumn = chosenColumns[j];
                int gridIndex = (rowIndex * columns) + placementColumn;

                gridChars[gridIndex] = word[j];
                solutionIndices.Add(gridIndex);
            }
        }

        // --- 3. Fill All Remaining Empty Spots with Filler Letters ---
        for (int i = 0; i < gridChars.Length; i++)
        {
            if (gridChars[i] == '\0')
            {
                // GÜNCELLENDÝ: `finalFillerLetters` kullanýlýyor
                gridChars[i] = finalFillerLetters[Random.Range(0, finalFillerLetters.Length)];
            }
        }

        // --- 4. Instantiate the Actual Grid from the Blueprint ---
        GameObject gridInstance = Instantiate(gridPrefab, transform);
        gridInstance.GetComponent<GridLayoutGroup>().constraintCount = columns;

        for (int i = 0; i < gridChars.Length; i++)
        {
            GameObject cellGO = Instantiate(cellPrefab, gridInstance.transform);
            PuzzleWordSearchCell cell = cellGO.GetComponent<PuzzleWordSearchCell>();

            cell.Setup(gridChars[i], this);

            if (solutionIndices.Contains(i))
            {
                cell.MarkAsSolution();
            }
            allCells.Add(cell);
        }
    }

    public void CheckForWinCondition()
    {
        if (allCells.All(cell => cell.IsInCorrectState()))
        {
            StartCoroutine(PlayWinSequence());
        }
    }

    private IEnumerator PlayWinSequence()
    {
        Debug.Log("YOU WIN!");
        foreach (var cell in allCells)
        {
            cell.PlayWinAnimation();
            yield return new WaitForSeconds(delayBetweenShines);
        }
    }

    public void ClearGrid()
    {
        allCells.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    // GÜNCELLENDÝ: Metot artýk parametre alýyor
    private bool ValidateInputs(string fillers, List<string> solutions)
    {
        if (string.IsNullOrEmpty(fillers))
        {
            Debug.LogError("Filler Letters string cannot be empty!");
            return false;
        }
        if (solutions.Any(word => word.Length > columns))
        {
            Debug.LogError($"One of the solution words is longer than the number of columns ({columns})!");
            return false;
        }
        if (solutions.Count > rows)
        {
            Debug.LogError($"There are more solution words ({solutions.Count}) than available rows ({rows})!");
            return false;
        }
        return true;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}