using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PotionShop
{
    /// <summary>
    /// PotionTown UI görsel ve tipografi standartlarını merkezi olarak sağlayan yardımcı sınıf.
    /// New Rocker fontunun ve oyun içi görsellerin tüm sistemlerde tutarlı kullanılmasını garanti eder.
    /// </summary>
    public static class UIThemeHelper
    {
        public const string FONT_ASSET_PATH = "Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset";
        public const string GOLD_COIN_PATH = "Assets/_PotionTown/Art/UI/TopMenu/Gold coin.png";
        public const string ANCIENT_COIN_PATH = "Assets/_PotionTown/Art/UI/TopMenu/Ancient coin.png";
        public const string FAME_COIN_PATH = "Assets/_PotionTown/Art/UI/TopMenu/Fame coin.png";
        public const string BUTTON_FRAME_PATH = "Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png";
        public const string COIN_FRAME_PATH = "Assets/_PotionTown/Art/UI/TopMenu/CoinFrame.png";
        public const string PARCHMENT_PATH = "Assets/_PotionTown/Art/UI/Parsomen.png";
        public const string SETTINGS_PANEL_PATH = "Assets/_PotionTown/Art/UI/SettingsMenu/Settings_Panel_Bg.png";
        public const string WARNING_ICON_PATH = "Assets/_PotionTown/Art/UI/Uyarı.png";
        public const string GENERAL_BUTTON_PATH = "Assets/_PotionTown/Art/UI/General Buton.png";

        // Renk Paleti (PotionTown Teması)
        public static readonly Color ColorGold = new Color(1f, 0.88f, 0.4f, 1f);
        public static readonly Color ColorTextWarm = new Color(1f, 0.96f, 0.85f, 1f);
        public static readonly Color ColorTextMuted = new Color(0.78f, 0.70f, 0.58f, 1f);
        public static readonly Color ColorWoodDark = new Color(0.14f, 0.09f, 0.06f, 0.95f);
        public static readonly Color ColorWoodBorder = new Color(0.38f, 0.24f, 0.14f, 1f);
        public static readonly Color ColorGreenEarned = new Color(0.35f, 1f, 0.35f, 1f);
        public static readonly Color ColorRedSpent = new Color(1f, 0.35f, 0.35f, 1f);
        public static readonly Color ColorWarning = new Color(1f, 0.82f, 0.25f, 1f);

        private static TMP_FontAsset _cachedFont;
        private static Sprite _cachedGoldCoin;
        private static Sprite _cachedAncientCoin;
        private static Sprite _cachedWarningIcon;
        private static Sprite _cachedParchment;
        private static Sprite _cachedSettingsBg;
        private static Sprite _cachedButtonFrame;

        /// <summary>
        /// New Rocker SDF fontunu her zaman güvenle döndürür.
        /// </summary>
        public static TMP_FontAsset GetNewRockerFont()
        {
            if (_cachedFont != null) return _cachedFont;

            // 1. Resources üzerinden ara
            _cachedFont = Resources.Load<TMP_FontAsset>("Fonts/NewRocker-Regular SDF");
            if (_cachedFont != null) return _cachedFont;

            // 2. Halihazırda bellekte yüklü olan fontları ara
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in allFonts)
            {
                if (f != null && f.name.Contains("NewRocker"))
                {
                    _cachedFont = f;
                    return _cachedFont;
                }
            }

#if UNITY_EDITOR
            // 3. Editör ortamında AssetDatabase ile yükle
            _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
            if (_cachedFont != null) return _cachedFont;
#endif

            // 4. Son çare varsayılan TMP fontu
            return TMP_Settings.defaultFontAsset;
        }

        public static Sprite GetGoldCoinSprite()
        {
            if (_cachedGoldCoin != null) return _cachedGoldCoin;
#if UNITY_EDITOR
            _cachedGoldCoin = AssetDatabase.LoadAssetAtPath<Sprite>(GOLD_COIN_PATH);
#endif
            return _cachedGoldCoin;
        }

        public static Sprite GetAncientCoinSprite()
        {
            if (_cachedAncientCoin != null) return _cachedAncientCoin;
#if UNITY_EDITOR
            _cachedAncientCoin = AssetDatabase.LoadAssetAtPath<Sprite>(ANCIENT_COIN_PATH);
#endif
            return _cachedAncientCoin;
        }

        public static Sprite GetWarningIcon()
        {
            if (_cachedWarningIcon != null) return _cachedWarningIcon;
#if UNITY_EDITOR
            _cachedWarningIcon = AssetDatabase.LoadAssetAtPath<Sprite>(WARNING_ICON_PATH);
#endif
            return _cachedWarningIcon;
        }

        public static Sprite GetParchmentSprite()
        {
            if (_cachedParchment != null) return _cachedParchment;
#if UNITY_EDITOR
            _cachedParchment = AssetDatabase.LoadAssetAtPath<Sprite>(PARCHMENT_PATH);
#endif
            return _cachedParchment;
        }

        public static Sprite GetSettingsBgSprite()
        {
            if (_cachedSettingsBg != null) return _cachedSettingsBg;
#if UNITY_EDITOR
            _cachedSettingsBg = AssetDatabase.LoadAssetAtPath<Sprite>(SETTINGS_PANEL_PATH);
#endif
            return _cachedSettingsBg;
        }

        public static Sprite GetButtonFrameSprite()
        {
            if (_cachedButtonFrame != null) return _cachedButtonFrame;
#if UNITY_EDITOR
            _cachedButtonFrame = AssetDatabase.LoadAssetAtPath<Sprite>(BUTTON_FRAME_PATH);
#endif
            return _cachedButtonFrame;
        }

        /// <summary>
        /// Belirtilen TextMeshProUGUI bileşenine New Rocker fontunu uygular.
        /// </summary>
        public static void ApplyNewRocker(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            TMP_FontAsset font = GetNewRockerFont();
            if (font != null)
            {
                tmp.font = font;
            }
        }
    }
}
