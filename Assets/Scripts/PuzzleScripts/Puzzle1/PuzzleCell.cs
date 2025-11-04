using DG.Tweening;
using TMPro;
using UnityEngine;

public class PuzzleCell : MonoBehaviour
{
    public TextMeshProUGUI txtLetter;
    public TextMeshProUGUI txtNumber;
    public PuzzleGenerator puzzleGenerator;

    [Header("Animation Settings")]
    [SerializeField] private Color shineColor = new Color(1f, 0.84f, 0f); // A nice gold color
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float animationScalePunch = 0.3f;

    private TMP_InputField inputField;
    private Color originalColor;

    private void Awake()
    {
        puzzleGenerator = this.transform.parent.GetComponentInParent<PuzzleGenerator>();
        // Get the InputField component on this same GameObject.
        inputField = GetComponent<TMP_InputField>();

        // *** THE FIX IS HERE ***
        // We check if the input field and its text component exist...
        if (inputField != null && inputField.textComponent != null)
        {
            // ...and if they do, we store the text's starting color.
            originalColor = inputField.textComponent.color;
        }
    }

    public void CheckSol()
    {
        if (puzzleGenerator != null)
        {
            puzzleGenerator.CheckSolution();
        }
        else
        {
            Debug.LogError("This cell is not connected to a PuzzleGenerator.", this.gameObject);
        }
    }

    public void PlayShineAnimation()
    {
        if (inputField == null || !inputField.enabled) return;

        DOTween.Init();
        Sequence shineSequence = DOTween.Sequence();

        // This will now work correctly because originalColor has the right value.
        shineSequence.Append(inputField.textComponent.DOColor(shineColor, animationDuration / 2));
        shineSequence.Join(transform.DOPunchScale(Vector3.one * animationScalePunch, animationDuration, 10, 1));
        shineSequence.Append(inputField.textComponent.DOColor(originalColor, animationDuration / 2));
    }
}