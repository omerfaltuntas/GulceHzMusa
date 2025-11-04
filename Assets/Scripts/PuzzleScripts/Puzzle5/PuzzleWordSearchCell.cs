using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class PuzzleWordSearchCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Component References")]
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image backgroundImage;

    [Header("State Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color scribbledColor = Color.black;
    [SerializeField] private Color winShineColor = Color.yellow;

    // --- State Variables ---
    private bool isScribbled = false;
    private bool isSolutionCell = false;
    private PuzzleWordSearchGenerator generator;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        backgroundImage.color = defaultColor;
    }

    public void Setup(char letter, PuzzleWordSearchGenerator owner)
    {
        letterText.text = letter.ToString();
        generator = owner;
    }

    public void MarkAsSolution()
    {
        isSolutionCell = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isScribbled = !isScribbled;
        backgroundImage.DOKill();
        backgroundImage.DOColor(isScribbled ? scribbledColor : defaultColor, 0.2f);

        generator.CheckForWinCondition();
    }

    /// <summary>
    /// Returns true if the cell's current state is correct for the final solution.
    /// </summary>
    public bool IsInCorrectState()
    {
        // *** THIS IS THE CORRECTED LOGIC ***
        // The state is correct if:
        // 1. It is a SOLUTION cell and it is NOT scribbled. (true != false -> true)
        // 2. It is a FILLER cell and it IS scribbled. (false != true -> true)
        return isSolutionCell != isScribbled;
    }

    public void PlayWinAnimation()
    {
        // The animation now plays on the UNSCRIBBLED solution letters.
        if (isSolutionCell)
        {
            Sequence seq = DOTween.Sequence();
            // We animate from the default color, not the scribbled color.
            seq.Append(backgroundImage.DOColor(winShineColor, 0.3f));
            seq.Append(backgroundImage.DOColor(defaultColor, 0.3f));
        }
    }
}