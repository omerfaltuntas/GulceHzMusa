using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AgeOfKids.Localization;

public class MazeInputManager : MonoBehaviour
{
    [Header("References")]
    public LineRenderer trailRenderer;
    public TextMeshPro collectedLettersText;
    [Tooltip("Çizime devam etmek için oluþturulacak prefab.")]
    public GameObject entryTriggerPrefab;
    [Tooltip("Sahnedeki baþlangýç trigger'ý.")]
    public BoxCollider2D initialEntryTrigger;

    CustomLocalizationString wordLocalization;


    [Header("Puzzle Content")]
    public string targetSentence = "FIND EXIT";
    public TextMeshPro[] letterDisplays;

    [Header("Win Animation Settings")]
    public Color winFlashColor = Color.green;
    public float winAnimationDuration = 2f;
    public int flashCount = 4;

    private MazeCell[] allMazeCells;
    private Camera mainCamera;
    private bool isDrawing = false;
    private bool isGameWon = false;
    private bool isPathBlocked = false;

    private List<Vector3> pathPoints = new List<Vector3>();
    private struct PathCheckpoint
    {
        public BoxCollider2D Trigger;
        public int PathIndex;
        public GameObject Instance;
    }
    private List<PathCheckpoint> checkpoints = new List<PathCheckpoint>();

    private StringBuilder displaySentenceBuilder;
    private Color originalTextColor;
    private Vector3 originalTextScale;

    void Awake()
    {
        wordLocalization = GetComponent<CustomLocalizationString>();
        targetSentence = wordLocalization.GetString();
        mainCamera = Camera.main;
        if (trailRenderer == null) Debug.LogError("TrailRenderer atanmamýþ!", this);
        else trailRenderer.positionCount = 0;

        allMazeCells = GetComponentsInChildren<MazeCell>(true);

        if (collectedLettersText != null)
        {
            originalTextColor = collectedLettersText.color;
            originalTextScale = collectedLettersText.transform.localScale;
        }
    }

    void OnEnable()
    {
        ResetPuzzle();
    }

    // --- YENÝ EKLENEN METOT ---
    // Obje devre dýþý býrakýldýðýnda bulmacayý tamamen sýfýrla.
    // Bu, tekrar etkinleþtirildiðinde temiz bir baþlangýç yapýlmasýný garanti eder.
    void OnDisable()
    {
        // LineRenderer'ýn pozisyon sayýsýný sýfýrlamak, disable/enable döngüsündeki
        // hatalarý önlemek için kritik öneme sahiptir.
        if (trailRenderer != null)
        {
            trailRenderer.positionCount = 0;
        }
        ResetPuzzle();
    }

    public void ResetPuzzle()
    {
        StopAllCoroutines();
        isDrawing = false;
        isPathBlocked = false;
        isGameWon = false;

        if (checkpoints != null)
        {
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint.Instance != null && checkpoint.Instance != initialEntryTrigger.gameObject)
                {
                    Destroy(checkpoint.Instance);
                }
            }
            checkpoints.Clear();
        }

        if (pathPoints != null)
        {
            pathPoints.Clear();
        }

        if (initialEntryTrigger != null)
        {
            Vector3 startPoint = initialEntryTrigger.bounds.center;
            startPoint.z = -1;
            pathPoints.Add(startPoint);

            checkpoints.Add(new PathCheckpoint
            {
                Trigger = initialEntryTrigger,
                PathIndex = 0,
                Instance = initialEntryTrigger.gameObject
            });
        }

        UpdateTrail();

        if (collectedLettersText != null)
        {
            collectedLettersText.color = originalTextColor;
            collectedLettersText.transform.localScale = originalTextScale;
        }

        ResetAllCells();
        InitializeDisplaySentence();
        UpdateCollectedTextUI();
        DistributeLetters();

        // Bu log artýk hem enable hem de disable olduðunda görünecek
        // Debug.Log("Puzzle and Finish Text have been reset.");
    }

    // ... (Geri kalan tüm metotlar ayný kalacak) ...
    void Update() { if (isGameWon) return; HandleInput(); }
    private void HandleInput() { if (Input.GetMouseButtonDown(0)) { BoxCollider2D clickedTrigger = IsPointerOverAnyTrigger(); if (clickedTrigger != null) { StartOrResumeDrawing(clickedTrigger); } } else if (Input.GetMouseButton(0) && isDrawing) { ContinueDrawing(); } else if (Input.GetMouseButtonUp(0) && isDrawing) { PauseDrawing(); } }
    private BoxCollider2D IsPointerOverAnyTrigger() { Vector2 mousePos = GetMouseWorldPosition(); Collider2D[] hits = Physics2D.OverlapPointAll(mousePos); foreach (var hit in hits) { foreach (var checkpoint in checkpoints) { if (checkpoint.Trigger == hit) { return checkpoint.Trigger; } } } return null; }
    private void StartOrResumeDrawing(BoxCollider2D clickedTrigger)
    {
        // --- YENÝ MANTIK: Eðer týklanan trigger baþlangýç trigger'ý ise, her þeyi sýfýrla. ---
        if (clickedTrigger == initialEntryTrigger)
        {
            // 1. Oluþturulmuþ tüm checkpoint'larý (baþlangýç hariç) yok et.
            //    Listeyi sondan baþa doðru gezmek, eleman silerken problem çýkmasýný önler.
            for (int i = checkpoints.Count - 1; i >= 0; i--)
            {
                if (checkpoints[i].Trigger != initialEntryTrigger)
                {
                    if (checkpoints[i].Instance != null)
                    {
                        Destroy(checkpoints[i].Instance);
                    }
                    checkpoints.RemoveAt(i);
                }
            }

            // 2. Çizim yolunu (pathPoints) tamamen temizle.
            pathPoints.Clear();

            // 3. Yola sadece baþlangýç noktasýný tekrar ekle.
            Vector3 startPoint = initialEntryTrigger.bounds.center;
            startPoint.z = -1;
            pathPoints.Add(startPoint);
        }
        // --- Eðer ara bir checkpoint'e týklandýysa, eski mantýk devam eder. ---
        else
        {
            int clickedCheckpointIndex = -1;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i].Trigger == clickedTrigger)
                {
                    clickedCheckpointIndex = i;
                    break;
                }
            }

            if (clickedCheckpointIndex == -1) return;

            // Týklanan noktadan sonrasýný sil
            int pathIndexToKeep = checkpoints[clickedCheckpointIndex].PathIndex;
            if (pathPoints.Count > pathIndexToKeep + 1)
            {
                pathPoints.RemoveRange(pathIndexToKeep + 1, pathPoints.Count - (pathIndexToKeep + 1));
            }

            // Týklanan checkpoint'ten sonraki checkpoint'leri yok et
            for (int i = checkpoints.Count - 1; i > clickedCheckpointIndex; i--)
            {
                if (checkpoints[i].Instance != null && checkpoints[i].Instance != initialEntryTrigger.gameObject)
                {
                    Destroy(checkpoints[i].Instance);
                }
                checkpoints.RemoveAt(i);
            }
        }

        // --- Bu kýsým her iki durumda da çalýþýr ---
        isDrawing = true;
        isPathBlocked = false;
        UpdateTrail();
    }
    private void PauseDrawing() { isDrawing = false; isPathBlocked = false; if (pathPoints.Count <= 1) return; Vector3 lastPoint = pathPoints[pathPoints.Count - 1]; if (entryTriggerPrefab != null) { GameObject newTriggerInstance = Instantiate(entryTriggerPrefab, lastPoint, Quaternion.identity, transform); BoxCollider2D newCollider = newTriggerInstance.GetComponent<BoxCollider2D>(); if (newCollider != null) { checkpoints.Add(new PathCheckpoint { Trigger = newCollider, PathIndex = pathPoints.Count - 1, Instance = newTriggerInstance }); } else { Debug.LogError("EntryTrigger prefab'ýnda BoxCollider2D bulunamadý!"); } } }
    private void ContinueDrawing() { if (!isDrawing) return; Vector3 currentPoint = GetMouseWorldPosition(); if (pathPoints.Count == 0) return; if (isPathBlocked) { Collider2D[] collidersUnderMouse = Physics2D.OverlapPointAll(currentPoint); bool isMouseOverWall = false; foreach (var col in collidersUnderMouse) { if (col.GetComponent<Wall>() != null) { isMouseOverWall = true; break; } } if (isMouseOverWall) { return; } else { isPathBlocked = false; } } Vector3 lastPoint = pathPoints[pathPoints.Count - 1]; RaycastHit2D[] hits = Physics2D.LinecastAll(lastPoint, currentPoint); bool foundWallInPath = false; foreach (var hit in hits) { if (hit.collider.GetComponent<Wall>() != null) { foundWallInPath = true; break; } } if (foundWallInPath) { isPathBlocked = true; return; } CheckForLetters(currentPoint); if (Vector3.Distance(lastPoint, currentPoint) > 0.1f) { pathPoints.Add(currentPoint); UpdateTrail(); } }
    private void CheckForLetters(Vector3 position) { float collectionRadius = 0.05f; Collider2D[] hits = Physics2D.OverlapCircleAll(position, collectionRadius); foreach (Collider2D hit in hits) { MazeCell cell = hit.GetComponentInParent<MazeCell>(); if (cell != null) { char collectedChar = cell.Collect(); if (collectedChar != ' ') { for (int i = 0; i < targetSentence.Length; i++) { if (char.ToUpper(targetSentence[i]) == collectedChar && displaySentenceBuilder[i] == '_') { displaySentenceBuilder[i] = targetSentence[i]; break; } } UpdateCollectedTextUI(); if (!displaySentenceBuilder.ToString().Contains("_")) { if (!isGameWon) { isGameWon = true; StartCoroutine(WinAnimation()); } } } break; } } }
    private void InitializeDisplaySentence() { if (string.IsNullOrEmpty(targetSentence)) return; displaySentenceBuilder = new StringBuilder(targetSentence.Length); foreach (char c in targetSentence) { if (char.IsWhiteSpace(c)) { displaySentenceBuilder.Append(' '); } else { displaySentenceBuilder.Append('_'); } } }
    private void UpdateCollectedTextUI() { if (collectedLettersText != null && displaySentenceBuilder != null) { collectedLettersText.text = displaySentenceBuilder.ToString(); } }
    private void UpdateTrail() { if (trailRenderer == null) return; trailRenderer.positionCount = pathPoints.Count; trailRenderer.SetPositions(pathPoints.ToArray()); }
    private Vector3 GetMouseWorldPosition() { Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); mousePos.z = -1; return mousePos; }
    private IEnumerator WinAnimation() { Debug.Log("KAZANDIN!"); if (trailRenderer != null) { trailRenderer.positionCount = 0; } Color originalColor = collectedLettersText.color; Transform textTransform = collectedLettersText.transform; Vector3 originalScale = textTransform.localScale; float flashPhaseDuration = winAnimationDuration / (flashCount * 2); for (int i = 0; i < flashCount; i++) { textTransform.localScale = originalScale * 1.1f; collectedLettersText.color = winFlashColor; yield return new WaitForSeconds(flashPhaseDuration); textTransform.localScale = originalScale; collectedLettersText.color = originalColor; yield return new WaitForSeconds(flashPhaseDuration); } collectedLettersText.color = winFlashColor; textTransform.localScale = originalScale * 1.2f; }
    private void ResetAllCells() { if (allMazeCells == null) return; foreach (var cell in allMazeCells) { cell.ResetCell(); } }
    void DistributeLetters() { if (letterDisplays == null || letterDisplays.Length == 0 || string.IsNullOrEmpty(targetSentence)) return; string sentenceToPlace = targetSentence.Replace(" ", "").ToUpper(); int numLetters = sentenceToPlace.Length; int numSlots = letterDisplays.Length; if (numLetters > numSlots) return; for (int i = 0; i < numLetters; i++) { float t = (numLetters == 1) ? 0.0f : (float)i / (numLetters - 1); int index = Mathf.RoundToInt(t * (numSlots - 1)); MazeCell cell = letterDisplays[index].GetComponentInParent<MazeCell>(); if (cell != null) { cell.SetLetter(sentenceToPlace[i]); } } }
}