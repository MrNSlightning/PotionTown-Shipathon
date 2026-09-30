using UnityEngine;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// TextMeshProUGUI bileşenlerine eklenen, dil değiştiğinde metni ve New Rocker fontunu otomatik güncelleyen bileşen.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("LocalizationManager sözlüğündeki anahtar (Örn: 'menu_start_day')")]
        public string localizationKey;

        [Tooltip("True ise büyük harfe dönüştürür (UPPERCASE)")]
        public bool forceUppercase = false;

        [Tooltip("New Rocker fontunu otomatik uygula")]
        public bool enforceNewRockerFont = true;

        private TextMeshProUGUI _tmpText;

        private void Awake()
        {
            _tmpText = GetComponent<TextMeshProUGUI>();
            if (enforceNewRockerFont)
            {
                UIThemeHelper.ApplyNewRocker(_tmpText);
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage newLang)
        {
            UpdateText();
        }

        public void SetKey(string newKey)
        {
            localizationKey = newKey;
            UpdateText();
        }

        public void UpdateText()
        {
            if (_tmpText == null) _tmpText = GetComponent<TextMeshProUGUI>();
            if (_tmpText == null) return;

            if (enforceNewRockerFont)
            {
                UIThemeHelper.ApplyNewRocker(_tmpText);
            }

            if (!string.IsNullOrEmpty(localizationKey))
            {
                string localized = LocalizationManager.Get(localizationKey);
                if (forceUppercase)
                {
                    var culture = (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Turkish)
                        ? new System.Globalization.CultureInfo("tr-TR")
                        : System.Globalization.CultureInfo.InvariantCulture;
                    _tmpText.text = localized.ToUpper(culture);
                }
                else
                {
                    _tmpText.text = localized;
                }
            }
        }
    }
}
