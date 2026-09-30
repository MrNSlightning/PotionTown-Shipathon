using UnityEngine;
using TMPro;

namespace PotionShop
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ClockUI : MonoBehaviour
    {
        private TextMeshProUGUI _timeText;

        [Tooltip("Günü de yazsın mı? (Örn: '1. Gün - 08:30')")]
        public bool showDay = true;

        private void Awake()
        {
            _timeText = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateLevelUI(LevelSystem.CurrentLevel);
            LevelSystem.OnLevelUp -= UpdateLevelUI;
            LevelSystem.OnLevelUp += UpdateLevelUI;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            LevelSystem.OnLevelUp -= UpdateLevelUI;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateLevelUI(LevelSystem.CurrentLevel);
        }

        private void UpdateLevelUI(int level)
        {
            if (_timeText == null) _timeText = GetComponent<TextMeshProUGUI>();
            if (_timeText == null) return;

            string prefix = LocalizationManager.Get("lvl_prefix");
            var culture = (LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Turkish)
                ? new System.Globalization.CultureInfo("tr-TR")
                : System.Globalization.CultureInfo.InvariantCulture;

            _timeText.text = $"{prefix.ToUpper(culture)} {level}";
            UIThemeHelper.ApplyNewRocker(_timeText);
        }
    }
}
