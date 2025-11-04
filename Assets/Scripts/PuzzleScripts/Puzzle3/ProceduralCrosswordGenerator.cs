using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AgeOfKids.Localization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ProceduralCrosswordGenerator : MonoBehaviour
{
    // ... Deðiþkenleriniz ve diðer metotlarýnýz burada olduðu gibi kalacak ...
    #region Variables
    [Header("Word List")]
    [Tooltip("Lokalizasyon kullanýlmýyorsa veya anahtar kelimeler olarak kullanýlacak liste.")]
    public List<string> words;
    [Tooltip("Lokalizasyon kullanýlmýyorsa veya anahtar kelime olarak kullanýlacak kelime.")]
    public string clueWord;

    [Header("Localization")]
    [Tooltip("Kelimeleri lokalize etmek için tire (-) ile ayrýlmýþ string'i içeren CustomLocalizationString bileþenini buraya sürükleyin.")]
    public CustomLocalizationString wordListLocalization;
    [Tooltip("Ýpucu kelimesini lokalize etmek için CustomLocalizationString bileþenini buraya sürükleyin.")]
    public CustomLocalizationString clueWordLocalization;

    [Header("Generation Settings")]
    [Tooltip("Eðer iþaretlenirse, bulmaca her zaman 'Seed' deðerine göre ayný þekilde oluþturulur.")]
    public bool useFixedSeed = true;
    [Tooltip("Lokalize seed kullanýlmýyorsa veya geçersizse kullanýlacak varsayýlan seed deðeri.")]
    public int seed = 12345;

    // YENÝ: Seed için lokalizasyon alaný
    [Tooltip("Her dil için farklý bir seed string'i saðlamak için kullanýlýr. Boþ býrakýlýrsa, yukarýdaki sabit 'Seed' deðeri kullanýlýr.")]
    public CustomLocalizationString seedLocalization;


    [Header("Prefabs")]
    public GameObject gridPrefab;
    public GameObject activeCellPrefab;
    public GameObject inactiveCellPrefab;

    public enum Direction { Horizontal, Vertical }
    private readonly Vector2Int HorizontalDirectionVector = Vector2Int.right;
    private readonly Vector2Int VerticalDirectionVector = Vector2Int.down;

    private class PlacedWord { public string Word; public Vector2Int Position; public Direction WordDirection; }
    private List<PlacedWord> placedWords = new List<PlacedWord>();

    private Dictionary<Vector2Int, char> gridData = new Dictionary<Vector2Int, char>();
    private Dictionary<Vector2Int, ProceduralCrosswordCell> cellMap = new Dictionary<Vector2Int, ProceduralCrosswordCell>();
    private PlacedWord activeWord;
    private List<ProceduralCrosswordCell> highlightedCells = new List<ProceduralCrosswordCell>();
    private Direction currentDirection;
    #endregion

    private void OnEnable()
    {
        GenerateCrossword();
    }

    private void OnDisable()
    {
        ClearGrid();
    }

    // --- DEÐÝÞTÝRÝLEN METOT ---
    // --- DEÐÝÞTÝRÝLEN METOT ---
    public void GenerateCrossword()
    {
        ClearGrid();

        if (useFixedSeed)
        {
            int finalSeed = seed;
            if (seedLocalization != null)
            {
                string localizedSeedString = seedLocalization.GetString();
                if (!string.IsNullOrEmpty(localizedSeedString) && int.TryParse(localizedSeedString, out int parsedSeed))
                {
                    finalSeed = parsedSeed;
                }
                else if (!string.IsNullOrEmpty(localizedSeedString))
                {
                    Debug.LogWarning($"Lokalizasyon seed deðeri '{localizedSeedString}' geçerli bir tam sayý deðil. Varsayýlan seed '{seed}' kullanýlýyor.");
                }
            }
            Random.InitState(finalSeed);
        }

        List<string> wordsForPuzzle;
        if (wordListLocalization != null)
        {
            string localizedCombinedString = wordListLocalization.GetString();
            wordsForPuzzle = new List<string>(localizedCombinedString.Split('-'));
            wordsForPuzzle.RemoveAll(string.IsNullOrEmpty);
        }
        else
        {
            wordsForPuzzle = new List<string>(words);
        }

        string clueWordForPuzzle;
        if (clueWordLocalization != null && !string.IsNullOrEmpty(clueWordLocalization.GetString()))
        {
            clueWordForPuzzle = clueWordLocalization.GetString();
        }
        else
        {
            clueWordForPuzzle = clueWord;
        }

        if (wordsForPuzzle == null || wordsForPuzzle.Count == 0) return;

        gridData.Clear();
        placedWords.Clear();
        var wordsToPlace = wordsForPuzzle.Where(w => !string.IsNullOrEmpty(w)).Select(w => w.ToUpper()).Distinct().OrderByDescending(w => w.Length).ToList();

        if (!wordsToPlace.Any()) return;

        // ÝLK KELÝMEYÝ YERLEÞTÝR
        string firstWord = wordsToPlace[0];

        // --- DEÐÝÞTÝRÝLDÝ: En uzun kelime her zaman dikey olarak yerleþtirilir. ---
        PlaceWord(firstWord, Vector2Int.zero, Direction.Vertical);
        // --------------------------------------------------------------------

        wordsToPlace.RemoveAt(0);

        // Kalan kelimelerin listesini karýþtýrarak her seferinde farklý bir yerleþtirme sýrasý denemesini saðlýyoruz.
        Shuffle(wordsToPlace);

        int attempts = 0;
        while (wordsToPlace.Any() && attempts < 10)
        {
            bool wordPlacedInIteration = false;
            for (int i = wordsToPlace.Count - 1; i >= 0; i--)
            {
                string word = wordsToPlace[i];
                if (TryPlaceWordIntersecting(word))
                {
                    wordsToPlace.RemoveAt(i);
                    wordPlacedInIteration = true;
                }
            }
            if (!wordPlacedInIteration) break;
            attempts++;
        }

        if (wordsToPlace.Any())
        {
            foreach (var word in wordsToPlace)
            {
                if (!TryPlaceWordInEmptySpace(word))
                {
                    Debug.LogError("FATAL: Could not place '" + word + "'.");
                }
            }
        }

        InstantiateShrinkWrappedGrid(clueWordForPuzzle);
    }

    // ... Script'inizin InstantiateShrinkWrappedGrid ve diðer deðiþmeyen metotlarý ...
    #region Unchanged Code
    #region Grid Instantiation and Helpers
    private void InstantiateShrinkWrappedGrid(string currentClueWord)
    {
        cellMap.Clear();
        if (!gridData.Any()) return;
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (var pos in gridData.Keys) { min.x = Mathf.Min(min.x, pos.x); min.y = Mathf.Min(min.y, pos.y); max.x = Mathf.Max(max.x, pos.x); max.y = Mathf.Max(max.y, pos.y); }
        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;
        GameObject gridInstance = Instantiate(gridPrefab, transform);
        gridInstance.name = "ShrinkWrapped Crossword Grid";
        GridLayoutGroup gridLayout = gridInstance.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;

        PlacedWord clue = null;
        if (!string.IsNullOrEmpty(currentClueWord))
        {
            clue = placedWords.FirstOrDefault(w => w.Word == currentClueWord.ToUpper());
        }

        for (int y = max.y; y >= min.y; y--)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (gridData.TryGetValue(currentPos, out char letter))
                {
                    GameObject cellGO = Instantiate(activeCellPrefab, gridInstance.transform);
                    ProceduralCrosswordCell cell = cellGO.GetComponent<ProceduralCrosswordCell>();
                    cell.Initialize(letter);
                    cell.gridPosition = currentPos;
                    cell.generator = this;

                    CrosswordCellInputHandler handler = cell.GetComponent<CrosswordCellInputHandler>();
                    if (handler != null) { handler.myCell = cell; }
                    else { Debug.LogError("The 'activeCellPrefab' is missing the 'CellInputHandler' script!", cellGO); }

                    cellMap[currentPos] = cell;
                    if (clue != null && IsCellInWord(currentPos, clue)) { cell.ShowLetterAsClue(); }
                }
                else { Instantiate(inactiveCellPrefab, gridInstance.transform); }
            }
        }
    }
    #endregion
    public Direction GetCurrentDirection() { return currentDirection; }

    public void SelectCell(ProceduralCrosswordCell cell)
    {
        var horizontalWord = placedWords.FirstOrDefault(w => w.WordDirection == Direction.Horizontal && IsCellInWord(cell.gridPosition, w));
        var verticalWord = placedWords.FirstOrDefault(w => w.WordDirection == Direction.Vertical && IsCellInWord(cell.gridPosition, w));

        if (activeWord != null && IsCellInWord(cell.gridPosition, activeWord))
        {
            if (currentDirection == Direction.Horizontal && verticalWord != null) { SetActiveWord(verticalWord, Direction.Vertical); }
            else if (currentDirection == Direction.Vertical && horizontalWord != null) { SetActiveWord(horizontalWord, Direction.Horizontal); }
        }
        else
        {
            if (horizontalWord != null) { SetActiveWord(horizontalWord, Direction.Horizontal); }
            else if (verticalWord != null) { SetActiveWord(verticalWord, Direction.Vertical); }
        }
        cell.Focus();
    }

    public void CheckActiveWordCompletion()
    {
        if (activeWord == null) return;
        CheckWordCompletion(activeWord);
    }

    public void FocusNextAvailableCell(Vector2Int currentPosition)
    {
        if (activeWord == null) return;
        StartCoroutine(FocusNextCellCoroutine(currentPosition));
    }

    private IEnumerator FocusNextCellCoroutine(Vector2Int currentPosition)
    {
        yield return null;

        Vector2Int directionVector = (currentDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
        Vector2Int nextPos = currentPosition + directionVector;

        while (IsCellInWord(nextPos, activeWord))
        {
            if (cellMap.TryGetValue(nextPos, out ProceduralCrosswordCell nextCell))
            {
                if (nextCell.IsInteractable())
                {
                    nextCell.Focus();
                    yield break;
                }
                nextPos += directionVector;
            }
            else
            {
                break;
            }
        }
    }

    public void FocusPreviousAvailableCell(Vector2Int currentPosition)
    {
        Vector2Int directionVector = (currentDirection == Direction.Horizontal) ? Vector2Int.left : Vector2Int.up;
        Vector2Int prevPos = currentPosition + directionVector;

        while (IsCellInWord(prevPos, activeWord))
        {
            if (cellMap.TryGetValue(prevPos, out ProceduralCrosswordCell prevCell) && prevCell.IsInteractable())
            {
                prevCell.Focus();
                return;
            }

            if (!cellMap.ContainsKey(prevPos)) break;

            prevPos += directionVector;
        }
    }

    public void FocusAndClearPreviousCell(Vector2Int currentPosition)
    {
        Vector2Int directionVector = (currentDirection == Direction.Horizontal) ? Vector2Int.left : Vector2Int.up;
        Vector2Int prevPos = currentPosition + directionVector;

        while (IsCellInWord(prevPos, activeWord))
        {
            if (cellMap.TryGetValue(prevPos, out ProceduralCrosswordCell prevCell) && prevCell.IsInteractable())
            {
                prevCell.ClearText();
                prevCell.Focus();
                return;
            }

            if (!cellMap.ContainsKey(prevPos)) break;

            prevPos += directionVector;
        }
    }

    private void SetActiveWord(PlacedWord word, Direction direction)
    {
        activeWord = word;
        currentDirection = direction;

        foreach (var cell in highlightedCells) { cell.SetHighlight(false); }
        highlightedCells.Clear();

        Vector2Int directionVector = (word.WordDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
        for (int i = 0; i < word.Word.Length; i++)
        {
            Vector2Int pos = word.Position + (directionVector * i);
            if (cellMap.TryGetValue(pos, out ProceduralCrosswordCell cell)) { cell.SetHighlight(true); highlightedCells.Add(cell); }
        }
    }

    private void CheckWordCompletion(PlacedWord word)
    {
        if (word == null) return;

        List<ProceduralCrosswordCell> wordCells = new List<ProceduralCrosswordCell>();
        Vector2Int directionVector = (word.WordDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;

        for (int i = 0; i < word.Word.Length; i++)
        {
            Vector2Int pos = word.Position + (directionVector * i);
            if (cellMap.TryGetValue(pos, out ProceduralCrosswordCell cell))
            {
                wordCells.Add(cell);
            }
        }

        bool allFilled = wordCells.All(cell => !string.IsNullOrEmpty(cell.GetInputText()));

        if (allFilled)
        {
            bool allCorrect = wordCells.All(cell => cell.IsCorrect());

            if (allCorrect)
            {
                foreach (var cell in wordCells)
                {
                    cell.PlaySuccessAnimation();
                }
            }
        }
    }

    private bool IsCellInWord(Vector2Int cellPosition, PlacedWord word)
    {
        if (word == null) return false;
        Vector2Int directionVector = (word.WordDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
        for (int i = 0; i < word.Word.Length; i++) { if (word.Position + (directionVector * i) == cellPosition) { return true; } }
        return false;
    }

    #endregion

    #region Generation Algorithm
    // --- DEÐÝÞTÝRÝLEN METOT ---
    private bool TryPlaceWordIntersecting(string word)
    {
        // Mevcut kelimeler listesinin karýþtýrýlmýþ bir kopyasýný oluþturuyoruz.
        // Bu, her denemede farklý bir kelimeye baðlanma olasýlýðýný artýrýr.
        var shuffledPlacedWords = placedWords.ToList();
        Shuffle(shuffledPlacedWords);

        foreach (var existingWord in shuffledPlacedWords)
        {
            var commonIndices = FindCommonIndices(word, existingWord.Word);
            if (!commonIndices.Any()) continue;

            // Potansiyel kesiþim noktalarýný da karýþtýrýyoruz.
            // Bu, ayný harf birden fazla yerde varsa, her seferinde ayný yerden baðlanmasýný engeller.
            Shuffle(commonIndices);

            foreach (var (newIdx, existIdx) in commonIndices)
            {
                Direction newDirection = (existingWord.WordDirection == Direction.Horizontal) ? Direction.Vertical : Direction.Horizontal;
                Vector2Int directionVector = (newDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
                Vector2Int existingWordDirectionVector = (existingWord.WordDirection == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
                Vector2Int intersectionPosition = existingWord.Position + (existingWordDirectionVector * existIdx);
                Vector2Int newStartPosition = intersectionPosition - (directionVector * newIdx);
                if (CanPlaceWordAt(word, newStartPosition, newDirection)) { PlaceWord(word, newStartPosition, newDirection); return true; }
            }
        }
        return false;
    }

    private bool CanPlaceWordAt(string word, Vector2Int position, Direction direction)
    {
        Vector2Int directionVector = (direction == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
        for (int i = 0; i < word.Length; i++)
        {
            Vector2Int currentPos = position + (directionVector * i);
            if (gridData.TryGetValue(currentPos, out char gridChar)) { if (gridChar != word[i]) return false; }
            else
            {
                Vector2Int perpendicularVector = (direction == Direction.Horizontal) ? VerticalDirectionVector : HorizontalDirectionVector;
                if (gridData.ContainsKey(currentPos + perpendicularVector) || gridData.ContainsKey(currentPos - perpendicularVector)) { return false; }
            }
        }
        Vector2Int beforeStartPos = position - directionVector;
        Vector2Int afterEndPos = position + (directionVector * word.Length);
        if (gridData.ContainsKey(beforeStartPos) || gridData.ContainsKey(afterEndPos)) { return false; }
        return true;
    }
    private bool TryPlaceWordInEmptySpace(string word)
    {
        for (int i = 0; i < 200; i++)
        {
            Direction direction = (Direction)Random.Range(0, 2);
            int searchRange = 20;
            Vector2Int startPos = new Vector2Int(Random.Range(-searchRange, searchRange), Random.Range(-searchRange, searchRange));
            if (CanPlaceWordAt(word, startPos, direction)) { PlaceWord(word, startPos, direction); return true; }
        }
        return false;
    }
    private void PlaceWord(string word, Vector2Int position, Direction direction)
    {
        Vector2Int directionVector = (direction == Direction.Horizontal) ? HorizontalDirectionVector : VerticalDirectionVector;
        for (int i = 0; i < word.Length; i++) { Vector2Int currentPos = position + (directionVector * i); gridData[currentPos] = word[i]; }
        placedWords.Add(new PlacedWord { Word = word, Position = position, WordDirection = direction });
    }
    private List<(int, int)> FindCommonIndices(string newWord, string existingWord)
    {
        var list = new List<(int, int)>();
        for (int i = 0; i < newWord.Length; i++) { for (int j = 0; j < existingWord.Length; j++) { if (newWord[i] == existingWord[j]) { list.Add((i, j)); } } }
        return list;
    }

    // --- YENÝ EKLENEN YARDIMCI METOT ---
    // Fisher-Yates algoritmasýný kullanarak bir listeyi yerinde karýþtýran metot.
    // UnityEngine.Random kullandýðý için, Random.InitState ile belirlenen seed'e göre çalýþýr.
    private void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }


    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) { if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject); else DestroyImmediate(transform.GetChild(i).gameObject); }
        gridData.Clear();
        placedWords.Clear();
        cellMap.Clear();
    }
    #endregion

}