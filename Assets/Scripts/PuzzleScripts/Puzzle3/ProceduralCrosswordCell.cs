using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
[RequireComponent(typeof(Image))]
public class ProceduralCrosswordCell : MonoBehaviour, IPointerDownHandler
{
    [Header("Components")]
    [SerializeField] private TMP_InputField inputField;
    [Header("Visuals")]
[SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    private Image backgroundImage;
    private Color originalColor;

    public Vector2Int gridPosition;
    public ProceduralCrosswordGenerator generator;
    private char correctLetter;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        originalColor = backgroundImage.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        generator.SelectCell(this);
    }

    public void Initialize(char letter)
    {
        correctLetter = letter;
        inputField.text = "";
        inputField.interactable = true;
    }

    // --- YENÝ METOT ---
    // Bu metot, Generator'un hücrenin metnini temizlemesini saðlar.
    public void ClearText()
    {
        inputField.text = "";
    }

    public void ShowLetterAsClue()
    {
        inputField.text = correctLetter.ToString();
        inputField.interactable = false;
        if (inputField.textComponent != null) { inputField.textComponent.color = Color.blue; }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (backgroundImage.color == successColor) return;
        backgroundImage.color = isHighlighted ? highlightColor : originalColor;
    }

    public void Focus()
    {
        if (inputField.interactable)
            inputField.Select();
    }

    public bool IsCorrect() { return !string.IsNullOrEmpty(inputField.text) && inputField.text.ToUpper()[0] == correctLetter; }
    public bool IsInteractable() { return inputField.interactable; }
    public string GetInputText() { return inputField.text; }

    public void PlaySuccessAnimation()
    {
        inputField.interactable = false;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(backgroundImage.DOColor(successColor, 0.25f));
        sequence.Join(transform.DOPunchScale(Vector3.one * 0.1f, 0.25f).SetEase(Ease.OutBack));
    }

    public void PlayFailAnimation()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(backgroundImage.DOColor(failColor, 0.15f));
        sequence.Join(transform.DOShakePosition(0.25f, new Vector3(10, 0, 0), 20, 90, false, true));
        sequence.Append(backgroundImage.DOColor(highlightColor, 0.15f).OnComplete(() => {
            inputField.text = "";
        }));
    }
}