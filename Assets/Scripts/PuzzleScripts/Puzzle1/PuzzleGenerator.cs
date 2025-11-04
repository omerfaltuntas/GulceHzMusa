using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AgeOfKids.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleGenerator : MonoBehaviour
{
    [TextArea(3, 5)]
     string puzzleSentence;

    [SerializeField] private int columns = 8;
    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private GameObject cellPrefab;
    CustomLocalizationString PuzzleStringLocalizationKey;

    private List<CellInputHandler> activeInputHandlers;
    private void Awake()
    {
        PuzzleStringLocalizationKey = GetComponent<CustomLocalizationString>();
    }
    // Awake, OnEnable, OnDisable, TurkishAlphabetMap metotlar� oldu�u gibi kalabilir.
    // Onlar� referans i�in buraya ekliyorum.
    private void OnEnable() { GenerateGrid(); }
    private void OnDisable() { ClearGrid(); }
    private static readonly Dictionary<char, int> TurkishAlphabetMap = new Dictionary<char, int>
    {
        {'A', 1}, {'B', 2}, {'C', 3}, {'Ç', 4}, {'D', 5}, {'E', 6}, {'F', 7},
        {'G', 8}, {'Ğ', 9}, {'H', 10}, {'I', 11}, {'İ', 12}, {'J', 13}, {'K', 14},
        {'L', 15}, {'M', 16}, {'N', 17}, {'O', 18}, {'Ö', 19}, {'P', 20}, {'R', 21},
        {'S', 22}, {'Ş', 23}, {'T', 24}, {'U', 25}, {'Ü', 26}, {'V', 27}, {'Y', 28},
        {'Z', 29}
    };

    public void GenerateGrid()
    {
        ClearGrid();
        puzzleSentence = PuzzleStringLocalizationKey.GetString();
        if (string.IsNullOrEmpty(puzzleSentence)) return;
        if (gridPrefab == null || cellPrefab == null)
        {
            Debug.LogError("Grid veya cell prefab'� atanmam��!");
            return;
        }

        if (activeInputHandlers == null)
            activeInputHandlers = new List<CellInputHandler>();
        else
            activeInputHandlers.Clear();

        GameObject gridInstance = Instantiate(gridPrefab, transform);
        gridInstance.name = "Puzzle Grid";
        GridLayoutGroup gridLayout = gridInstance.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("Grid prefab'�nda GridLayoutGroup bile�eni bulunamad�.");
            Destroy(gridInstance);
            return;
        }

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        for (int i = 0; i < puzzleSentence.Length; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, gridInstance.transform);
            newCell.name = $"Cell {i}";

            // Gerekli t�m bile�enleri en ba�ta alal�m.
            PuzzleCell cellComponent = newCell.GetComponent<PuzzleCell>();
            CellInputHandler inputHandler = newCell.GetComponent<CellInputHandler>();
            TMP_InputField inputField = newCell.GetComponent<TMP_InputField>();

            if (cellComponent == null || inputHandler == null || inputField == null)
            {
                Debug.LogError("Cell prefab'�nda gerekli script'ler eksik.", newCell);
                continue;
            }

            // �nce y�neticiyi tan�tarak NullReferenceException'� �nle.
            inputHandler.Initialize(this);

            char character = puzzleSentence[i];
            char upperChar = char.ToUpper(character, new System.Globalization.CultureInfo("tr-TR"));

            // Orijinal kodunuzdaki do�ru mant��� buraya geri ekliyoruz.
            if (TurkishAlphabetMap.TryGetValue(upperChar, out int number))
            {
                // --- ��TE D�ZELTME BURADA ---
                // Eksik olan say�y� atama sat�r�n� geri ekledik.
                cellComponent.txtNumber.text = number.ToString();

                // Bu aktif bir h�cre oldu�u i�in navigasyon listesine ekle.
                activeInputHandlers.Add(inputHandler);
            }
            else
            {
                // Bu pasif bir h�cre (bo�luk, virg�l vb.)
                inputField.text = character.ToString();
                inputField.enabled = false;
            }
        }
    }

    // --- NAV�GASYON VE D��ER METOTLAR DE���MED� ---

    public void FocusNextCell(CellInputHandler currentCell)
    {
        int currentIndex = activeInputHandlers.IndexOf(currentCell);
        if (currentIndex != -1 && currentIndex < activeInputHandlers.Count - 1)
        {
            StartCoroutine(ActivateFieldAfterFrame(activeInputHandlers[currentIndex + 1].GetComponent<TMP_InputField>()));
        }
    }

    public void FocusPreviousCell(CellInputHandler currentCell)
    {
        int currentIndex = activeInputHandlers.IndexOf(currentCell);
        if (currentIndex != -1 && currentIndex > 0)
        {
            StartCoroutine(ActivateFieldAfterFrame(activeInputHandlers[currentIndex - 1].GetComponent<TMP_InputField>()));
        }
    }

    private IEnumerator ActivateFieldAfterFrame(TMP_InputField targetField)
    {
        yield return new WaitForEndOfFrame();
        if (targetField != null)
        {
            targetField.ActivateInputField();
            targetField.Select();
        }
    }

    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private IEnumerator PlayWinSequence()
    {
        PuzzleCell[] cells = GetComponentsInChildren<PuzzleCell>();

        foreach (var cell in cells)
        {
            var inputField = cell.GetComponent<TMP_InputField>();
            if (inputField != null)
            {
                inputField.interactable = false;
            }
        }

        foreach (PuzzleCell cell in cells)
        {
            cell.PlayShineAnimation();
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("<color=green>BULMACA TAMAMLANDI!</color>");
    }
    public void CheckSolution()
    {
        PuzzleCell[] cells = GetComponentsInChildren<PuzzleCell>();

        if (cells.Length != puzzleSentence.Length)
        {
            //Debug.LogError("H�cre say�s� c�mle uzunlu�uyla e�le�miyor. ��z�m kontrol edilemez.");
            return;
        }

        bool isSolutionCorrect = true;
        for (int i = 0; i < puzzleSentence.Length; i++)
        {
            char correctChar = puzzleSentence[i];
            char correctUpperChar = char.ToUpper(correctChar, new System.Globalization.CultureInfo("tr-TR"));

            if (TurkishAlphabetMap.ContainsKey(correctUpperChar))
            {
                TMP_InputField userInputField = cells[i].GetComponent<TMP_InputField>();

                if (userInputField == null ||
                    string.IsNullOrEmpty(userInputField.text) ||
                    char.ToUpper(userInputField.text[0], new System.Globalization.CultureInfo("tr-TR")) != correctUpperChar)
                {
                    isSolutionCorrect = false;
                    break;
                }
            }
        }

        if (isSolutionCorrect)
        {
            StartCoroutine(PlayWinSequence());
        }
        else
        {
            Debug.Log("<color=red>��z�m yanl��. Tekrar dene!</color>");
        }
    }
}