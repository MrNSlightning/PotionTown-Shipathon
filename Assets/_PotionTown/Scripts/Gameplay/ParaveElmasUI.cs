using UnityEngine;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Tüm dükkanlarda ve alanlarda Para/Kadim Para UI panelini ortak ve senkronize tutar.
    /// Altın, Kadim Para miktarlarını ve Seviye/XP bilgisini GameManager ve LevelSystem ile günceller.
    /// </summary>
    public class ParaveElmasUI : MonoBehaviour
    {
        [Header("UI Metin Referansları")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI kadimParaText;

        [Header("Seviye UI (Opsiyonel)")]
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI xpText;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            GameManager.OnGoldChangedStatic += HandleGoldChanged;
            GameManager.OnKadimParaChangedStatic += HandleKadimParaChanged;
            LevelSystem.OnLevelUp += HandleLevelUp;
            LevelSystem.OnXPGained += HandleXPGained;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            RefreshUI();
        }

        private void OnDisable()
        {
            GameManager.OnGoldChangedStatic -= HandleGoldChanged;
            GameManager.OnKadimParaChangedStatic -= HandleKadimParaChanged;
            LevelSystem.OnLevelUp -= HandleLevelUp;
            LevelSystem.OnXPGained -= HandleXPGained;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            RefreshUI();
        }

        public void EnsureReferences()
        {
            if (goldText != null && kadimParaText != null) return;

            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                string name = tmp.gameObject.name;

                if (goldText == null && (name.IndexOf("alt", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         name.IndexOf("para", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         name.IndexOf("gold", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    goldText = tmp;
                }
                else if (kadimParaText == null && (name.IndexOf("kadim", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    name.IndexOf("elmas", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    name.IndexOf("diamond", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    name.IndexOf("vip", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    kadimParaText = tmp;
                }
                else if (levelText == null && (name.IndexOf("level", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                name.IndexOf("seviye", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    levelText = tmp;
                }
                else if (xpText == null && (name.IndexOf("xp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             name.IndexOf("tecrube", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             name.IndexOf("deneyim", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    xpText = tmp;
                }
            }

            if (goldText != null) UIThemeHelper.ApplyNewRocker(goldText);
            if (kadimParaText != null) UIThemeHelper.ApplyNewRocker(kadimParaText);
            if (levelText != null) UIThemeHelper.ApplyNewRocker(levelText);
            if (xpText != null) UIThemeHelper.ApplyNewRocker(xpText);
        }

        public void RefreshUI()
        {
            EnsureReferences();

            if (goldText != null)
                goldText.text = GameManager.SharedGold.ToString();

            if (kadimParaText != null)
                kadimParaText.text = GameManager.SharedKadimPara.ToString();

            if (levelText != null)
                levelText.text = string.Format(LocalizationManager.Get("lvl_text_format"), LevelSystem.CurrentLevel, LevelSystem.GetLevelTitle());

            if (xpText != null)
            {
                xpText.text = string.Format(LocalizationManager.Get("day_text_format"), LevelSystem.CurrentLevel, LevelSystem.MAX_LEVEL);
            }
        }

        private void HandleGoldChanged(int newGold)
        {
            if (goldText != null)
                goldText.text = newGold.ToString();
        }

        private void HandleKadimParaChanged(int newKadimPara)
        {
            if (kadimParaText != null)
                kadimParaText.text = newKadimPara.ToString();
        }

        private void HandleLevelUp(int newLevel)
        {
            RefreshUI();
        }

        private void HandleXPGained(int amount)
        {
            RefreshUI();
        }
    }
}
