using UnityEngine;
using TMPro;
using System.Collections;

public class MazeCell : MonoBehaviour
{
    [Header("Wall GameObjects")]
    [SerializeField] private GameObject wallTop;
    [SerializeField] private GameObject wallBottom;
    [SerializeField] private GameObject wallLeft;
    [SerializeField] private GameObject wallRight;

    [Header("Letter Display")]
    [SerializeField] private TextMeshPro letterText;
    [Tooltip("Harfin baþlangýç rengi.")]
    public Color defaultLetterColor = Color.white;

    [Header("Interaction")]
    [SerializeField] private CircleCollider2D letterCollider;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float popScaleMultiplier = 1.5f;

    public bool IsTopWallOpen { get; private set; }
    public bool IsBottomWallOpen { get; private set; }
    public bool IsLeftWallOpen { get; private set; }
    public bool IsRightWallOpen { get; private set; }

    public bool Visited { get; set; } = false;
    private bool isCollected = false;

    // --- YENÝ: Hücreyi tamamen sýfýrlayan metot ---
    // MazeCell.cs içinde...

    public void ResetCell()
    {
        StopAllCoroutines();
        isCollected = false;

        if (letterText != null)
        {
            // Bu satýr en önemlisi
            letterText.gameObject.SetActive(true);

            letterText.transform.localScale = Vector3.one;
            letterText.color = defaultLetterColor;
            letterText.text = "";
        }

        if (letterCollider != null)
        {
            letterCollider.enabled = false;
        }
    }

    public void SetLetter(char letter)
    {
        // Bu metot çaðrýlmadan önce ResetCell'in çaðrýldýðýndan emin oluyoruz.
        if (letterText != null)
        {
            letterText.text = letter.ToString();
            // isCollected zaten ResetCell tarafýndan false yapýldý, burada tekrar yapmaya gerek yok.
            if (letterCollider != null)
            {
                letterCollider.enabled = true;
            }
        }
    }

    // ClearLetter metodunu artýk kullanmýyoruz, ResetCell daha kapsamlý.
    public void ClearLetter()
    {
        if (letterText != null)
        {
            letterText.text = "";
            if (letterCollider != null) letterCollider.enabled = false;
        }
    }

    #region Unchanged Code
    void Awake() { if (letterText != null) { ClearLetter(); } }
    public char Collect() { if (isCollected || letterText == null || string.IsNullOrEmpty(letterText.text)) { return ' '; } isCollected = true; if (letterCollider != null) { letterCollider.enabled = false; } StartCoroutine(AnimateCollection()); return letterText.text[0]; }
    private IEnumerator AnimateCollection() { Transform textTransform = letterText.transform; Vector3 initialScale = textTransform.localScale; Vector3 targetScale = initialScale * popScaleMultiplier; Color initialColor = letterText.color; Color targetColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0); float elapsedTime = 0f; while (elapsedTime < animationDuration) { float t = elapsedTime / animationDuration; textTransform.localScale = Vector3.Lerp(initialScale, targetScale, t); letterText.color = Color.Lerp(initialColor, targetColor, t); elapsedTime += Time.deltaTime; yield return null; } letterText.gameObject.SetActive(false); }
    public void Initialize() { wallTop.SetActive(false); wallBottom.SetActive(false); wallLeft.SetActive(false); wallRight.SetActive(false); IsTopWallOpen = false; IsBottomWallOpen = false; IsLeftWallOpen = false; IsRightWallOpen = false; }
    public void OpenTopWall() { IsTopWallOpen = true; }
    public void OpenBottomWall() { IsBottomWallOpen = true; }
    public void OpenLeftWall() { IsLeftWallOpen = true; }
    public void OpenRightWall() { IsRightWallOpen = true; }
    public void SetTopWallActive(bool active) { wallTop.SetActive(active); }
    public void SetBottomWallActive(bool active) { wallBottom.SetActive(active); }
    public void SetLeftWallActive(bool active) { wallLeft.SetActive(active); }
    public void SetRightWallActive(bool active) { wallRight.SetActive(active); }
    #endregion
}