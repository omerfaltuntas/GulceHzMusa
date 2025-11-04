using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class CellInputHandler : MonoBehaviour
{
    private PuzzleGenerator puzzleGenerator;
    private TMP_InputField myField;

    private void Awake()
    {
        myField = GetComponent<TMP_InputField>();
        myField.onValueChanged.AddListener(OnInputValueChanged);
    }

    public void Initialize(PuzzleGenerator generator)
    {
        this.puzzleGenerator = generator;
    }

    /// <summary>
    /// Bu metot, metin DEÐÝÞTÝÐÝNDE çalýþýr.
    /// Hem ileri gitmeyi hem de BÝR KARAKTER SÝLEREK geri gitmeyi yönetir.
    /// </summary>
    private void OnInputValueChanged(string newText)
    {
        if (puzzleGenerator == null) return;

        if (!string.IsNullOrEmpty(newText))
        {
            // Senaryo 1: Hücreye bir karakter yazýldý. Ýleri git.
            puzzleGenerator.FocusNextCell(this);
        }
        else
        {
            // Senaryo 2: Hücreden mevcut karakter silindi. Geri git.
            puzzleGenerator.FocusPreviousCell(this);
        }
    }

    /// <summary>
    /// Bu metot, her frame çalýþýr.
    /// SADECE ZATEN BOÞ OLAN bir hücrede Backspace'e basýldýðýnda çalýþýr.
    /// </summary>
    private void Update()
    {
        if (puzzleGenerator == null) return;

        // Senaryo 3: Hücre zaten boþ ve Backspace'e basýldý. Geri git.
        if (myField.isFocused && Input.GetKeyDown(KeyCode.Backspace) && string.IsNullOrEmpty(myField.text))
        {
            puzzleGenerator.FocusPreviousCell(this);
        }
    }
}