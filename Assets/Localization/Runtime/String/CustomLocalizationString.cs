using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgeOfKids.Localization
{
    /// <summary>
    /// Her dil için bir string değeri tutan veri yapısı.
    /// </summary>
    [Serializable]
    public class StringEntry
    {
        [Tooltip("Dil adı (Otomatik belirlenir)")]
        public string language;

        [Tooltip("Bu dile karşılık gelen metin.")]
        [TextArea(3, 5)]
        public string value;
    }

    /// <summary>
    /// Bu bileşen, bir GameObject'e dil desteği olan bir string değişkeni ekler.
    /// Dışarıdan 'GetString()' metoduyla mevcut dile ait string değerini döndürür.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("BOBU/Localization/Custom Localization String")]
    public class CustomLocalizationString : MonoBehaviour
    {
        [Header("Localization Package")]
        [SerializeField] private LocalizationData localization;

        [Header("Localized Strings")]
        [SerializeField] private List<StringEntry> strings = new List<StringEntry>();

        [Header("Current Value")]
        [Tooltip("Mevcut dile göre güncellenen string değeri.")]
        [SerializeField][ReadOnly] private string currentString;

        private void Awake()
        {
            // Başlangıçta dile göre string'i ayarla
            ApplyLocalization();
        }

        private void OnEnable()
        {
            // Obje tekrar aktif olduğunda güncel dile göre string'i ayarla
            ApplyLocalization();
        }

        /// <summary>
        /// Seçili dili alıp, ilgili string'i 'currentString' değişkenine atar.
        /// </summary>
        public void ApplyLocalization()
        {
            if (localization == null)
            {
                Debug.LogError("Dil seçeneği için LocalizationData ataması eksik!", this);
                return;
            }

            // Seçili dili getir
            string selectedLang = localization.selectedLanguage.ToString();

            // Dili string listesinde bul
            var currentEntry = strings.Find(e => e.language == selectedLang);
            if (currentEntry == null)
            {
                Debug.LogError($"'{selectedLang}' dil seçeneği için bir string değeri bulunamadı!", this);
                currentString = string.Empty; // Değer bulunamazsa boş ata
                return;
            }

            // Mevcut string'i güncelle
            currentString = currentEntry.value;
        }

        /// <summary>
        /// Dışarıdan erişim için mevcut dile ait string'i döndürür.
        /// </summary>
        /// <returns>Aktif dile karşılık gelen string değerini döndürür.</returns>
        public string GetString()
        {
            // Her ihtimale karşı en güncel değeri döndürmek için tekrar kontrol edilebilir
            // ApplyLocalization(); 
            return currentString;
        }

        /// <summary>
        /// Editörde değişiklik yapıldığında dil alanlarını günceller.
        /// </summary>
        private void OnValidate()
        {
            if (localization == null) return;

            // LocalizationData içindeki mevcut dillerin listesini alır.
            List<string> languageList = localization.languages;
            if (languageList == null) return;

            // Mevcut diller listesine göre string alanlarını senkronize et
            for (int i = strings.Count - 1; i >= 0; i--)
            {
                if (!languageList.Contains(strings[i].language))
                {
                    strings.RemoveAt(i); // Listede olup dillerde olmayanları kaldır
                }
            }

            // Eğer yeni bir dil eklendiyse, o dil için yeni bir giriş oluştur
            foreach (string lang in languageList)
            {
                if (!strings.Exists(e => e.language == lang))
                {
                    strings.Add(new StringEntry { language = lang, value = "" });
                }
            }

            // Değişiklik sonrası anında güncelle
            ApplyLocalization();
        }
    }

    // currentString alanının Inspector'da sadece okunabilir olmasını sağlayan yardımcı attribute.
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}