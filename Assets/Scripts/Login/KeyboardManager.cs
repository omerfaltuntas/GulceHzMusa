using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Globalization; // to handle Turkish character casing

public class KeyboardManager : MonoBehaviour
{

    [Header("UI Settings")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform loginPanel;
    [SerializeField] private BoxCollider2D pageCollider;

    // Aktif olan ve yazı yazılacak InputField'i burada saklıyoruz. BU REFERANSI KORUYACAĞIZ.
    private TMP_InputField activeInputField;

    [Header("Keyboard Settings")]
    [SerializeField] private List<RectTransform> lettersContent = new List<RectTransform>();
    private bool isUpperCase = true;
    private string lowerKeys = "qwertyuıopğüasdfghjklşizxcvbnmöç";

    private bool isInteractingWithKeyboardPanel = false;
    private bool isKeyboardPanelWorking = false;
    //private bool isKeyboardBeingDragged = false;

    private void Awake()
    {
    }

    void Start()
    {
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        panel.SetActive(false);
        isKeyboardPanelWorking = false;
        isInteractingWithKeyboardPanel = false;
        isUpperCase = true;
    }
    private void OnDisable()
    {
        panel.SetActive(false);
        isKeyboardPanelWorking = false;
        isInteractingWithKeyboardPanel = false;
        isUpperCase = true;
    }
    // --- TAMAMEN YENİDEN YAZILMIŞ UPDATE METODU ---
    void Update()
    {


        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        TMP_InputField selectedInputField = (currentSelected != null) ? currentSelected.GetComponent<TMP_InputField>() : null;
        // 1. Kural: Eğer klavye paneli ile etkileşim varsa (sürükleme gibi), hiçbir şey yapma.
        if (isInteractingWithKeyboardPanel && !isKeyboardPanelWorking)
        {
            if (selectedInputField != null && currentSelected != null)
            {
                return;
            }
        }
        // 2. Kural: Eğer yeni bir InputField seçildiyse...
        if (selectedInputField != null)
        {
            // ...bu alanı aktif alan olarak ata ve klavyenin açık olduğundan emin ol.
            activeInputField = selectedInputField;
            if (!panel.activeInHierarchy)
            {
                isKeyboardPanelWorking = true;
                PanelStatus(true);
            }
            return; // Bu frame için işimiz bitti.
        }

        // 3. Kural: Eğer klavye zaten kapalıysa, yapacak bir şey yok.
        if (!panel.activeInHierarchy)
        {
            return;
        }

        // 4. Kural: Eğer boş bir alana tıklandıysa (hiçbir şey seçili değilse)...
        if (currentSelected == null)
        {
            isKeyboardPanelWorking = false;
            // ...klavyeyi kapat.
            PanelStatus(false);
            return;
        }

        // 5. Kural: Eğer seçilen obje klavyenin kendisinin bir parçası DEĞİLSE...
        // (ve bir InputField de olmadığını zaten biliyoruz)
        if (!currentSelected.transform.IsChildOf(panel.transform))
        {
            isKeyboardPanelWorking = false;
            PanelStatus(false);
            return;
        }
    }

    public void InteractionStarted()
    {
        isInteractingWithKeyboardPanel = true;
    }

    public void InteractionEnded()
    {

        StartCoroutine(GracePeriodCoroutine());
    }

    private IEnumerator GracePeriodCoroutine()
    {
        yield return new WaitForEndOfFrame();
        isInteractingWithKeyboardPanel = false;
        isKeyboardPanelWorking = true;
    }

    public void PanelStatus(bool status)
    {
        panel.SetActive(status);
        if (pageCollider != null) pageCollider.enabled = !status;

        // Klavye kapatılıyorsa, aktif alanı temizle ki bir sonraki sefere sorun çıkmasın.
        if (!status)
        {
            activeInputField = null;
        }

        // ... Diğer panel animasyon kodlarınız ...
    }

    public void OnKeyPress(string key)
    {
        // Bu kontrol artık sürükleme sonrası da sorunsuz çalışacak.
        if (activeInputField != null && activeInputField.interactable == true)
        {
            string formattedKey = isUpperCase ? key.ToUpper(new CultureInfo("tr-TR")) : key.ToLower();
            activeInputField.text = formattedKey;
        }
    }

    // Geri kalan tüm metodlarınız (ToggleCapslock, OnBackspace, vs.) olduğu gibi kalabilir.
    public void ToggleCapslock() { isUpperCase = !isUpperCase; UpdateKeyLabels(); }
    private void UpdateKeyLabels() { foreach (RectTransform content in lettersContent) { foreach (Button btn in content.GetComponentsInChildren<Button>()) { TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>(); if (btnText != null) { string key = btnText.text; if (lowerKeys.Contains(key.ToLower())) { btnText.text = isUpperCase ? key.ToUpper(new CultureInfo("tr-TR")) : key.ToLower(); } } } } }
    public void OnBackspace()
    {
        if (activeInputField.interactable == true)
        {
            activeInputField.text = activeInputField.text.Substring(0, activeInputField.text.Length - 1);
        }
    }
    public void OnSpace() { OnKeyPress(" "); }
    public void OnEnter() { if (panel.activeInHierarchy) { PanelStatus(false); } }
}