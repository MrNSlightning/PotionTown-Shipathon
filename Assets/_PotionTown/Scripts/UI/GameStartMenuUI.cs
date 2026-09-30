using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop.UI
{
    /// <summary>
    /// Oyun ilk açıldığında gösterilen ana başlangıç menüsü.
    /// Günü Başlat, Ayarlar ve Dil butonlarını barındırır.
    /// "Günü Başlat" denmeden önce arkadaki lobi sahnesindeki hiçbir şeye tıklanamaz.
    /// </summary>
    public class GameStartMenuUI : MonoBehaviour
    {
        private static GameStartMenuUI _instance;
        public static GameStartMenuUI Instance => _instance;
        public static bool IsOpen => _instance != null && _instance.gameObject.activeInHierarchy;

        [Header("UI Elemanları (Inspector Bağlantıları)")]
        [Tooltip("Arka planı karartan ve lobi tıklamalarını engelleyen panel")]
        public GameObject overlayBlocker;

        [Tooltip("Menünün ana ahşap/parşömen çerçevesi")]
        public GameObject windowPanel;

        [Tooltip("Başlık metni (örn: POTION TOWN)")]
        public TextMeshProUGUI titleText;

        [Tooltip("Alt başlık metni (örn: Simya Dükkanı)")]
        public TextMeshProUGUI subtitleText;

        [Tooltip("Günü Başlat butonu")]
        public Button startLevelButton;

        [Tooltip("Ayarlar menüsünü açan buton")]
        public Button settingsButton;

        [Tooltip("Dili değiştiren buton")]
        public Button languageButton;

        [Tooltip("Dil butonunun üzerindeki metin (DİL: TÜRKÇE / LANGUAGE: ENGLISH)")]
        public TextMeshProUGUI languageButtonText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIIfEmpty();
        }

        private void Start()
        {
            HookButtons();
            UpdateAllLocalizedTexts();
            Show();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateAllLocalizedTexts();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateAllLocalizedTexts();
        }

        public void UpdateAllLocalizedTexts()
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Get("menu_welcome");
                UIThemeHelper.ApplyNewRocker(titleText);
            }

            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.Get("menu_subtitle");
                UIThemeHelper.ApplyNewRocker(subtitleText);
            }

            UpdateStartButtonLabel();
            UpdateSettingsButtonLabel();
            UpdateLanguageButtonLabel();
        }

        public void UpdateSettingsButtonLabel()
        {
            if (settingsButton != null)
            {
                var txt = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = LocalizationManager.Get("menu_settings");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (overlayBlocker != null) overlayBlocker.SetActive(true);
            if (windowPanel != null) windowPanel.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HookButtons()
        {
            if (startLevelButton != null)
            {
                startLevelButton.onClick.RemoveAllListeners();
                startLevelButton.onClick.AddListener(OnStartLevelClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (languageButton != null)
            {
                languageButton.onClick.RemoveAllListeners();
                languageButton.onClick.AddListener(OnLanguageClicked);
            }
        }

        public void OnStartLevelClicked()
        {
            Debug.Log("<color=cyan>[GameStartMenuUI]</color> Günü Başlat tıklandı. Lobi kilidi açılıyor...");
            Hide();
            Debug.Log("<color=green>[GameStartMenuUI]</color> Gün başladı! Dükkan ve lobi etkileşime açıldı.");
        }

        public void StartDay()
        {
            OnStartLevelClicked();
        }

        public void UpdateStartButtonLabel()
        {
            if (startLevelButton != null)
            {
                var txt = startLevelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = LocalizationManager.Get("menu_start_day");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }
        }

        private void OnSettingsClicked()
        {
            var topMenu = TopDropdownMenu.Instance ?? FindFirstObjectByType<TopDropdownMenu>();
            if (topMenu != null)
            {
                topMenu.OpenSettingsMenu();
            }
            else
            {
                var settings = SettingsMenuUI.Instance ?? FindFirstObjectByType<SettingsMenuUI>();
                if (settings != null)
                {
                    settings.gameObject.SetActive(true);
                }
            }
        }

        private void OnLanguageClicked()
        {
            LocalizationManager.Instance.ToggleLanguage();
            UpdateLanguageButtonLabel();
        }

        private void UpdateLanguageButtonLabel()
        {
            if (languageButtonText != null)
            {
                string langName = LocalizationManager.GetCurrentLanguageName();
                string label = LocalizationManager.Instance.CurrentLanguage == GameLanguage.Turkish ? "DİL: TÜRKÇE" : "LANGUAGE: ENGLISH";
                languageButtonText.text = label;
                UIThemeHelper.ApplyNewRocker(languageButtonText);
            }
        }

        private void BuildUIIfEmpty()
        {
            // Prefab veya Inspector üzerinden nesneler zaten atanmışsa dinamik oluşturma yapma
            if (overlayBlocker != null && windowPanel != null && startLevelButton != null)
            {
                return;
            }

            // Canvas kontrolü
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2400; // Lobi üstünde, Settings altında

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (overlayBlocker == null)
            {
                overlayBlocker = new GameObject("OverlayBlocker", typeof(RectTransform), typeof(Image));
                overlayBlocker.transform.SetParent(transform, false);
                Image blockImg = overlayBlocker.GetComponent<Image>();
                blockImg.color = new Color(0.04f, 0.03f, 0.06f, 0.85f);
                blockImg.raycastTarget = true; // Arkadaki lobi tıklamalarını kesinlikle engeller
                RectTransform blockRT = overlayBlocker.GetComponent<RectTransform>();
                blockRT.anchorMin = Vector2.zero;
                blockRT.anchorMax = Vector2.one;
                blockRT.sizeDelta = Vector2.zero;
            }

            if (windowPanel == null)
            {
                windowPanel = new GameObject("WindowPanel", typeof(RectTransform), typeof(Image));
                windowPanel.transform.SetParent(transform, false);
                Image winImg = windowPanel.GetComponent<Image>();
                Sprite bgSprite = UIThemeHelper.GetSettingsBgSprite();
                if (bgSprite != null)
                {
                    winImg.sprite = bgSprite;
                    winImg.type = Image.Type.Simple;
                    winImg.color = Color.white;
                }
                else
                {
                    winImg.color = new Color(0.14f, 0.09f, 0.06f, 0.96f);
                }

                RectTransform winRT = windowPanel.GetComponent<RectTransform>();
                winRT.anchorMin = new Vector2(0.5f, 0.5f);
                winRT.anchorMax = new Vector2(0.5f, 0.5f);
                winRT.pivot = new Vector2(0.5f, 0.5f);
                winRT.sizeDelta = new Vector2(650f, 620f);

                // Başlık Metni
                GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
                titleObj.transform.SetParent(windowPanel.transform, false);
                TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(titleTMP);
                titleTMP.text = "POTION TOWN";
                titleTMP.fontSize = 52;
                titleTMP.color = UIThemeHelper.ColorGold;
                titleTMP.alignment = TextAlignmentOptions.Center;
                titleTMP.fontStyle = FontStyles.Bold;
                RectTransform titleRT = titleObj.GetComponent<RectTransform>();
                titleRT.anchoredPosition = new Vector2(0f, 210f);
                titleRT.sizeDelta = new Vector2(500f, 70f);

                // Alt Başlık
                GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
                subObj.transform.SetParent(windowPanel.transform, false);
                var subLT = subObj.GetComponent<LocalizedText>();
                subLT.localizationKey = "menu_subtitle";
                TextMeshProUGUI subTMP = subObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(subTMP);
                subTMP.fontSize = 26;
                subTMP.color = UIThemeHelper.ColorTextMuted;
                subTMP.alignment = TextAlignmentOptions.Center;
                RectTransform subRT = subObj.GetComponent<RectTransform>();
                subRT.anchoredPosition = new Vector2(0f, 155f);
                subRT.sizeDelta = new Vector2(400f, 40f);

                // Butonlar Dikey Container
                GameObject btnCont = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                btnCont.transform.SetParent(windowPanel.transform, false);
                RectTransform btnContRT = btnCont.GetComponent<RectTransform>();
                btnContRT.anchoredPosition = new Vector2(0f, -40f);
                btnContRT.sizeDelta = new Vector2(420f, 320f);

                VerticalLayoutGroup vlg = btnCont.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 22f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // 1. Günü Başlat Butonu
                startLevelButton = CreateFantasyButton(btnCont.transform, "StartLevelButton", "menu_start_day", new Color(0.2f, 0.65f, 0.25f, 1f));

                // 2. Ayarlar Butonu
                settingsButton = CreateFantasyButton(btnCont.transform, "SettingsButton", "menu_settings", new Color(0.28f, 0.2f, 0.15f, 1f));

                // 3. Dil Butonu
                languageButton = CreateFantasyButton(btnCont.transform, "LanguageButton", "menu_language", new Color(0.22f, 0.25f, 0.38f, 1f));
                languageButtonText = languageButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private Button CreateFantasyButton(Transform parent, string objName, string locKey, Color baseColor)
        {
            GameObject btnObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            Sprite btnFrame = UIThemeHelper.GetButtonFrameSprite();
            if (btnFrame != null)
            {
                img.sprite = btnFrame;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = baseColor;
            }

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 0.9f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 65f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
            textObj.transform.SetParent(btnObj.transform, false);
            var loc = textObj.GetComponent<LocalizedText>();
            loc.localizationKey = locKey;
            loc.forceUppercase = true;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(tmp);
            tmp.text = LocalizationManager.Get(locKey);
            tmp.fontSize = 28;
            tmp.color = UIThemeHelper.ColorTextWarm;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            return btn;
        }
    }
}
