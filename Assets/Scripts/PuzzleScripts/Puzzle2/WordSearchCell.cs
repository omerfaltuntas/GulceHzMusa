using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class WordSearchCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI letterText;

    [Header("Visuals")]
    [SerializeField] private Color selectionColor = Color.yellow;
    [SerializeField] private Color correctWordColor = Color.green;

    // --- Public variables for the generator to access ---
    public char Letter;
    public bool IsFound { get; private set; } = false;

    // This is now a simple public field to ensure it gets saved with the prefab instance
    public Vector2Int gridPosition;
    private Image backgroundImage;
    private Color originalColor;

    private void Awake()
    {
        backgroundImage = this.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        originalColor = backgroundImage.color;
    }

    public void SetLetter(char letter)
    {
        this.Letter = letter;
        if (letterText != null)
        {
            letterText.text = letter.ToString();
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (IsFound) return;
        backgroundImage.color = isSelected ? selectionColor : originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsFound) return;
        WordSearchGenerator.Instance.StartSelection(this);
    }



    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsFound) return;
        WordSearchGenerator.Instance.AddToSelection(this);
    }

    public void PlayCorrectAnimation()
    {
        IsFound = true;
        backgroundImage.DOColor(correctWordColor, 0.5f);
        transform.DOPunchScale(Vector3.one * 0.1f, 1f).SetEase(Ease.OutBack);
    }
}