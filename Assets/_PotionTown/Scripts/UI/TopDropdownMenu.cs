using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PotionShop.UI;

namespace PotionShop
{
    /// <summary>
    /// Ekranın üstünde oyma ahşap pano tasarımıyla açılıp kapanan dropdown menü.
    /// Her ekranda görünür. Sahne bağlamına göre butonlar gizlenir/gösterilir.
    /// İçinde: 
    /// - Sol 2 oval yuva: Altın ve Kadim Para (ikonlar ve miktarlar).
    /// - Sağ 3 oyma çerçeve: Dükkan Aç/Kapat, Envanter, Lobiye Dön.
    /// - Alt oyma çekmece dili (ArrowButton): Animasyonlu açılıp kapanma.
    /// </summary>
    public class TopDropdownMenu : MonoBehaviour
    {
        public static TopDropdownMenu Instance { get; private set; }

        [Header("Ana Referanslar")]
        [Tooltip("Açılıp kapanacak menü paneli")]
        public RectTransform menuPanel;

        [Tooltip("Aşağı/Yukarı sarkan çekmece dili butonu")]
        public Button arrowButton;

        [Header("Para Göstergeleri (Sol Oval Yuvalar)")]
        [Tooltip("Altın miktarını gösteren text")]
        public TextMeshProUGUI goldText;
        [Tooltip("Altın ikonu görseli")]
        public Image goldIcon;

        [Tooltip("Kadim Para miktarını gösteren text")]
        public TextMeshProUGUI kadimParaText;
        [Tooltip("Kadim Para ikonu görseli")]
        public Image kadimIcon;

        [Tooltip("İtibar (Fame / Reputation) miktarını gösteren text")]
        public TextMeshProUGUI fameText;
        [Tooltip("İtibar (Fame / Reputation) ikonu görseli")]
        public Image fameIcon;

        [Header("Sağ Çerçeve Butonları")]
        [Tooltip("1. Çerçeve: Dükkanı Aç/Kapat butonu")]
        public Button shopToggleButton;
        [Tooltip("Dükkan butonunun üzerindeki yazı")]
        public TextMeshProUGUI shopToggleButtonText;
        [Tooltip("Dükkan butonundaki ikon görseli")]
        public Image shopButtonIcon;
        [Tooltip("Dükkan açıkken gösterilecek ikon")]
        public Sprite shopOpenSprite;
        [Tooltip("Dükkan kapalıyken gösterilecek ikon")]
        public Sprite shopCloseSprite;

        [Tooltip("2. Çerçeve: Envanter butonu")]
        public Button inventoryButton;

        [Tooltip("3. Çerçeve: Lobiye Dön butonu")]
        public Button lobbyButton;

        [Tooltip("Ses / Müzik Aç-Kapat butonu (Opsiyonel)")]
        public Button soundButton;

        [Tooltip("Ayarlar menüsünü açan buton")]
        public Button settingsButton;
        [Tooltip("Ayarlar butonu görseli / ikonu")]
        public Image settingsButtonImage;
        [Tooltip("Ayarlar butonu ikon spriti")]
        public Sprite settingsIconSprite;

        [Header("Ayarlar Menüsü")]
        [Tooltip("Ayarlar menüsü prefab'ı (SettingsMenuUI). Resources veya Inspector'dan atanır.")]
        public GameObject settingsMenuPrefab;

        [Header("Market / Satın Alım Butonları")]
        [Tooltip("Altın yuvasındaki market butonu")]
        public Button goldMarketButton;
        [Tooltip("Kadim Para yuvasındaki market butonu")]
        public Button kadimMarketButton;
        [Tooltip("Çoklu / alternatif market butonları")]
        public Button[] marketButtons;

        [Header("Canvas Geçiş Referansları")]
        [Tooltip("Lobiye dönüldüğünde kapatılacak canvas (dükkan)")]
        public GameObject gameCanvas;
        [Tooltip("Lobiye dönüldüğünde açılacak canvas (lobi/harita)")]
        public GameObject mapCanvas;

        [Header("Ok / Çekmece Simgesi")]
        [Tooltip("Ok ikonunun RectTransform'u (döndürmek için)")]
        public RectTransform arrowIcon;
        [Tooltip("Ok metni (▲ / ▼)")]
        public TextMeshProUGUI arrowText;

        [Header("Başlangıç & Animasyon Ayarları")]
        [Tooltip("Menü başlangıçta açık mı olsun?")]
        public bool startOpen = false;

        [Tooltip("Menünün açılıp kapanma süresi (saniye)")]
        public float animationDuration = 0.35f;

        [Tooltip("Animasyon eğrisi")]
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Durum Değişkenleri
        private bool _isOpen = false;
        private bool _isDukkanContext = true;
        private Coroutine _animCoroutine;
        private float _panelHeight = 200f;
        private Vector2 _closedPosition;
        private Vector2 _openPosition;

        // Aktif Dükkan Takibi
        private GameObject _currentShopCanvas;

        // Ayarlar menüsü instance'ı
        private SettingsMenuUI _settingsMenuInstance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureSortingCanvas();
            EnsureSceneReferences();
            AutoSetup();
            HookAllButtons();

            UIThemeHelper.ApplyNewRocker(goldText);
            UIThemeHelper.ApplyNewRocker(kadimParaText);
            UIThemeHelper.ApplyNewRocker(fameText);
            UIThemeHelper.ApplyNewRocker(shopToggleButtonText);
        }

        private void Start()
        {
            EnsureSceneReferences();
            HookAllButtons();
            UpdateAllLocalizedTexts();
        }

        public void EnsureSortingCanvas()
        {
            Canvas c = GetComponent<Canvas>();
            if (c == null)
            {
                c = gameObject.AddComponent<Canvas>();
            }
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.overrideSorting = true;
            c.sortingLayerName = "UI";
            c.sortingOrder = 500; // Envanter (2000) ve Parşömenler (2500+) daima menünün ve okun önündedir.

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void OnEnable()
        {
            // Event abonelikleri
            GameManager.OnGoldChangedStatic += HandleGoldChanged;
            GameManager.OnKadimParaChangedStatic += HandleKadimParaChanged;
            GameManager.OnReputationChangedStatic += HandleReputationChanged;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged += HandleShopStatusChanged;
            }

            HookAllButtons();
            RefreshCurrencyUI();
            UpdateAllLocalizedTexts();
            UpdateContextVisibility();
        }

        private void OnDisable()
        {
            GameManager.OnGoldChangedStatic -= HandleGoldChanged;
            GameManager.OnKadimParaChangedStatic -= HandleKadimParaChanged;
            GameManager.OnReputationChangedStatic -= HandleReputationChanged;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateAllLocalizedTexts();
        }

        public void UpdateAllLocalizedTexts()
        {
            UpdateShopButtonText();

            if (inventoryButton != null)
            {
                var invTxt = inventoryButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (invTxt != null)
                {
                    invTxt.text = LocalizationManager.Get("top_inventory");
                    UIThemeHelper.ApplyNewRocker(invTxt);
                }
            }

            if (lobbyButton != null)
            {
                var lobbyTxt = lobbyButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (lobbyTxt != null)
                {
                    lobbyTxt.text = LocalizationManager.Get("top_return_lobby");
                    UIThemeHelper.ApplyNewRocker(lobbyTxt);
                }
            }
        }

        private void Update()
        {
            bool isBlocked = IsAnyBlockingUIOpen();

            // Eğer menü açıkken başka bir arayüz (Envanter, Parşömen vb.) açılırsa menüyü hemen kapat
            if (_isOpen && isBlocked)
            {
                CloseMenu();
            }

            // Başka bir arayüz açıkken ok butonunun tıklanmasını engelle
            if (arrowButton != null)
            {
                if (arrowButton.interactable == isBlocked)
                {
                    arrowButton.interactable = !isBlocked;
                }
            }
        }

        /// <summary>
        /// Envanter, parşömenler veya başka bir modal panel açık mı kontrol eder.
        /// </summary>
        public bool IsAnyBlockingUIOpen()
        {
            // 1. Depo / Envanter açık mı?
            if (StorageManager.Instance != null && StorageManager.Instance.IsStorageOpen)
            {
                return true;
            }

            if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
            {
                return true;
            }

            // 2. Lisans Parşömeni açık mı?
            if (LicenseParchmentUI.Instance != null && LicenseParchmentUI.Instance.IsOpen)
            {
                return true;
            }

            // 3. Eşya / Tarif Parşömenleri açık mı?
            if (InventoryRecipeParchmentUI.Instance != null && InventoryRecipeParchmentUI.Instance.IsOpen)
            {
                return true;
            }

            if (ParchmentClickArea.IsAnyParchmentOpen)
            {
                return true;
            }

            // 4. Ayarlar Menüsü açık mı?
            if (_settingsMenuInstance != null && _settingsMenuInstance.IsOpen)
            {
                return true;
            }
            if (SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen)
            {
                return true;
            }

            // 5. Başlangıç Menüsü veya Mağaza açık mı?
            if (PotionShop.UI.GameStartMenuUI.IsOpen || ShopUI.IsOpen)
            {
                return true;
            }

            // 6. Sahnede aktif olan herhangi bir parşömen veya modal popup kontrolü
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas.gameObject == gameObject) continue;
                if (canvas.overrideSorting && canvas.sortingOrder >= 2000)
                {
                    string n = canvas.name.ToLower();
                    if (n.Contains("popup") || n.Contains("parsomen") || n.Contains("parşomen") || 
                        n.Contains("envanter") || n.Contains("license") || n.Contains("lisans") ||
                        n.Contains("storage") || n.Contains("settings"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Otomatik referans bulma, hizalama ve buton event bağlantıları.
        /// </summary>
        public void AutoSetup()
        {
            FindMissingReferences();

            if (menuPanel != null)
            {
                float h = menuPanel.rect.height > 10f ? menuPanel.rect.height : (menuPanel.sizeDelta.y > 10f ? menuPanel.sizeDelta.y : 200f);
                _panelHeight = h;

                _openPosition = new Vector2(menuPanel.anchoredPosition.x, 0f);
                float closedY = menuPanel.anchoredPosition.y > 10f ? menuPanel.anchoredPosition.y : _panelHeight;
                _closedPosition = new Vector2(menuPanel.anchoredPosition.x, closedY);

                _isOpen = startOpen;
                menuPanel.anchoredPosition = _isOpen ? _openPosition : _closedPosition;
                UpdateArrowVisual(_isOpen);
                menuPanel.gameObject.SetActive(true);
            }

            HookAllButtons();
            RefreshCurrencyUI();
            UpdateShopButtonText();
        }

        /// <summary>
        /// Tüm butonların onClick dinleyicilerini temizleyip yeniden bağlar ve tıklanabilirliği güvenceye alır.
        /// Menüdeki herhangi bir butona tıklandığında menünün yukarı toplanıp kapanması sağlanır.
        /// </summary>
        public void HookAllButtons()
        {
            FindMissingReferences();

            if (arrowButton != null)
            {
                arrowButton.onClick.RemoveListener(ToggleMenu);
                arrowButton.onClick.AddListener(ToggleMenu);
                EnsureButtonInteractable(arrowButton);
            }

            if (shopToggleButton != null)
            {
                shopToggleButton.onClick.RemoveListener(OnShopToggleClicked);
                shopToggleButton.onClick.AddListener(OnShopToggleClicked);
                EnsureButtonInteractable(shopToggleButton);
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveListener(OnInventoryClicked);
                inventoryButton.onClick.AddListener(OnInventoryClicked);
                EnsureButtonInteractable(inventoryButton);
            }

            if (lobbyButton != null)
            {
                lobbyButton.onClick.RemoveListener(OnLobbyClicked);
                lobbyButton.onClick.AddListener(OnLobbyClicked);
                EnsureButtonInteractable(lobbyButton);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
                EnsureButtonInteractable(settingsButton);
            }

            if (kadimIcon != null)
            {
                Button kBtn = kadimIcon.GetComponent<Button>();
                if (kBtn == null) kBtn = kadimIcon.gameObject.AddComponent<Button>();
                kBtn.onClick.RemoveListener(OpenShopUI);
                kBtn.onClick.AddListener(OpenShopUI);
                EnsureButtonInteractable(kBtn);
            }

            if (goldMarketButton != null)
            {
                goldMarketButton.onClick.RemoveListener(OpenShopUI);
                goldMarketButton.onClick.AddListener(OpenShopUI);
                EnsureButtonInteractable(goldMarketButton);
            }

            if (kadimMarketButton != null)
            {
                kadimMarketButton.onClick.RemoveListener(OpenShopUI);
                kadimMarketButton.onClick.AddListener(OpenShopUI);
                EnsureButtonInteractable(kadimMarketButton);
            }

            if (marketButtons != null)
            {
                foreach (var mb in marketButtons)
                {
                    if (mb == null) continue;
                    mb.onClick.RemoveListener(OpenShopUI);
                    mb.onClick.AddListener(OpenShopUI);
                    EnsureButtonInteractable(mb);
                }
            }

            // Menü panelindeki diğer tüm butonlara (varsa ses, ayarlar vb. dahil) tıklandığında menüyü kapatma kancası ekle
            if (menuPanel != null)
            {
                Button[] allButtons = menuPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                    if (btn == arrowButton) continue;
                    btn.onClick.RemoveListener(CloseMenu);
                    btn.onClick.AddListener(CloseMenu);
                    EnsureButtonInteractable(btn);
                }
            }
        }

        private void EnsureButtonInteractable(Button btn)
        {
            if (btn == null) return;
            btn.interactable = true;
        }

        private void FindMissingReferences()
        {
            if (menuPanel == null)
            {
                Transform p = transform.Find("MenuPanel");
                if (p != null) menuPanel = p.GetComponent<RectTransform>();
            }

            if (arrowButton == null)
            {
                Transform a = transform.Find("ArrowButton");
                if (a == null && menuPanel != null) a = menuPanel.Find("ArrowButton");
                if (a != null) arrowButton = a.GetComponent<Button>();
            }

            if (menuPanel != null)
            {
                if (goldText == null)
                {
                    Transform t = menuPanel.Find("CurrencySection/GoldSlot/ValueText") ?? 
                                  menuPanel.Find("GoldSlot/ValueText") ?? 
                                  menuPanel.Find("GoldSlot/Text") ?? 
                                  menuPanel.Find("GoldText");
                    if (t != null) goldText = t.GetComponent<TextMeshProUGUI>();
                }
                if (goldIcon == null)
                {
                    Transform gi = menuPanel.Find("CurrencySection/GoldSlot/Icon") ?? menuPanel.Find("GoldSlot/Icon");
                    if (gi != null) goldIcon = gi.GetComponent<Image>();
                }
                if (kadimParaText == null)
                {
                    Transform t = menuPanel.Find("CurrencySection/KadimSlot/ValueText") ?? 
                                  menuPanel.Find("KadimSlot/ValueText") ?? 
                                  menuPanel.Find("KadimSlot/Text") ?? 
                                  menuPanel.Find("KadimParaText");
                    if (t != null) kadimParaText = t.GetComponent<TextMeshProUGUI>();
                }
                if (kadimIcon == null)
                {
                    Transform ki = menuPanel.Find("CurrencySection/KadimSlot/Icon") ?? menuPanel.Find("KadimSlot/Icon");
                    if (ki != null) kadimIcon = ki.GetComponent<Image>();
                }
                if (fameText == null)
                {
                    Transform fameSlot = menuPanel.Find("FameSlot") ?? 
                                         menuPanel.Find("CurrencySection/FameSlot") ?? 
                                         menuPanel.Find("ItibarSlot");
                    if (fameSlot != null)
                    {
                        fameText = fameSlot.Find("FameText")?.GetComponent<TextMeshProUGUI>() ??
                                   fameSlot.Find("ValueText")?.GetComponent<TextMeshProUGUI>() ??
                                   fameSlot.Find("Text")?.GetComponent<TextMeshProUGUI>() ??
                                   fameSlot.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                    else
                    {
                        Transform t = menuPanel.Find("CurrencySection/FameSlot/ValueText") ?? 
                                      menuPanel.Find("FameSlot/ValueText") ?? 
                                      menuPanel.Find("FameSlot/FameText") ?? 
                                      menuPanel.Find("FameSlot/Text") ?? 
                                      menuPanel.Find("FameText") ??
                                      menuPanel.Find("ItibarText");
                        if (t != null) fameText = t.GetComponent<TextMeshProUGUI>();
                    }
                }
                if (fameIcon == null)
                {
                    Transform fameSlot = menuPanel.Find("FameSlot") ?? 
                                         menuPanel.Find("CurrencySection/FameSlot") ?? 
                                         menuPanel.Find("ItibarSlot");
                    if (fameSlot != null)
                    {
                        Transform fi = fameSlot.Find("Icon") ?? fameSlot.Find("FameIcon") ?? fameSlot.Find("ItibarIcon");
                        if (fi != null) fameIcon = fi.GetComponent<Image>();
                    }
                    else
                    {
                        Transform fi = menuPanel.Find("CurrencySection/FameSlot/Icon") ?? menuPanel.Find("FameSlot/Icon");
                        if (fi != null) fameIcon = fi.GetComponent<Image>();
                    }
                }
                if (shopToggleButton == null)
                {
                    Transform t = menuPanel.Find("ActionButtonsSection/ShopToggleButton") ?? 
                                  menuPanel.Find("ShopToggleButton") ?? 
                                  menuPanel.Find("DükkanButton") ?? 
                                  menuPanel.Find("DukkanButton");
                    if (t != null)
                    {
                        shopToggleButton = t.GetComponent<Button>();
                        shopToggleButtonText = t.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
                if (inventoryButton == null)
                {
                    Transform t = menuPanel.Find("ActionButtonsSection/InventoryButton") ?? 
                                  menuPanel.Find("InventoryButton") ?? 
                                  menuPanel.Find("Inventory Button") ?? 
                                  menuPanel.Find("EnvanterButton") ?? 
                                  menuPanel.Find("Envanter Button") ?? 
                                  menuPanel.Find("Envanter");
                    if (t != null) inventoryButton = t.GetComponent<Button>();
                }
                if (lobbyButton == null)
                {
                    Transform t = menuPanel.Find("ActionButtonsSection/LobbyButton") ?? 
                                  menuPanel.Find("LobbyButton") ?? 
                                  menuPanel.Find("Lobby_Button") ?? 
                                  menuPanel.Find("Lobi_Button") ?? 
                                  menuPanel.Find("LobiButton") ?? 
                                  menuPanel.Find("Lobi");
                    if (t != null) lobbyButton = t.GetComponent<Button>();
                }
                if (soundButton == null)
                {
                    Transform t = menuPanel.Find("ActionButtonsSection/SoundButton") ?? 
                                  menuPanel.Find("SoundButton") ?? 
                                  menuPanel.Find("SoundToggleButton");
                    if (t != null)
                    {
                        soundButton = t.GetComponent<Button>();
                    }
                }
                if (settingsButton == null)
                {
                    Transform t = menuPanel.Find("Settings") ??
                                  menuPanel.Find("ActionButtonsSection/SettingsButton") ?? 
                                  menuPanel.Find("SettingsButton") ?? 
                                  menuPanel.Find("AyarlarButton");
                    if (t != null)
                    {
                        settingsButton = t.GetComponent<Button>();
                        settingsButtonImage = t.GetComponent<Image>();
                    }
                }
                if (goldMarketButton == null)
                {
                    Transform t = menuPanel.Find("Panel/Coins/GoldSlot/Market") ?? 
                                  menuPanel.Find("Coins/GoldSlot/Market") ?? 
                                  menuPanel.Find("CurrencySection/GoldSlot/Market") ?? 
                                  menuPanel.Find("GoldSlot/Market") ?? 
                                  menuPanel.Find("GoldSlot/MarketButton");
                    if (t != null) goldMarketButton = t.GetComponent<Button>();
                }
                if (kadimMarketButton == null)
                {
                    Transform t = menuPanel.Find("Panel/Coins/KadimSlot/Market") ?? 
                                  menuPanel.Find("Coins/KadimSlot/Market") ?? 
                                  menuPanel.Find("CurrencySection/KadimSlot/Market") ?? 
                                  menuPanel.Find("KadimSlot/Market") ?? 
                                  menuPanel.Find("KadimSlot/MarketButton");
                    if (t != null) kadimMarketButton = t.GetComponent<Button>();
                }
#if UNITY_EDITOR
                if (settingsMenuPrefab == null)
                {
                    settingsMenuPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/Menus/SettingsMenuUI.prefab");
                }
                if (settingsIconSprite == null)
                {
                    settingsIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/TopMenu/Icon_Settings.png");
                }
                if (settingsButtonImage != null && settingsIconSprite != null && settingsButtonImage.sprite != settingsIconSprite)
                {
                    settingsButtonImage.sprite = settingsIconSprite;
                    settingsButtonImage.preserveAspect = true;
                }
                if (shopOpenSprite == null)
                {
                    shopOpenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/TopMenu/Icon_Shop_Open.png");
                }
                if (shopCloseSprite == null)
                {
                    shopCloseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/TopMenu/Icon_Shop_Close.png");
                }
#endif
            }

            if (arrowButton != null)
            {
                if (arrowIcon == null)
                {
                    Transform ai = arrowButton.transform.Find("ArrowIcon");
                    if (ai != null) arrowIcon = ai.GetComponent<RectTransform>();
                    else arrowIcon = arrowButton.GetComponent<RectTransform>();
                }
                if (arrowText == null)
                {
                    arrowText = arrowButton.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Menü paneli ve içindeki tüm bileşenleri ahşap pano görseline (Gemini_Generated_Image_lx4qndlx4qndlx4q)
        /// göre piksel ve oran hassasiyetiyle hizalar. YALNIZCA Editor Context Menu üzerinden manuel tetiklenir.
        /// Çalışma anında (runtime) prefab tasarımını ezmemesi için asla otomatik çağrılmaz.
        /// </summary>
        [ContextMenu("Ahşap Panoya Göre Otomatik Hizala (Auto-Align)")]
        public void AlignToWoodenBoard()
        {
            if (menuPanel == null)
            {
                FindMissingReferences();
                if (menuPanel == null) return;
            }

            // 1. MenuPanel üzerindeki HorizontalLayoutGroup'u kaldır
            HorizontalLayoutGroup hlg = menuPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(hlg);
                else
                    Destroy(hlg);
            }

            LayoutGroup lg = menuPanel.GetComponent<LayoutGroup>();
            if (lg != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(lg);
                else
                    Destroy(lg);
            }

            // 2. MenuPanel RectTransform ayarları (Ekranın üstüne tam oturur, 200px yükseklik)
            menuPanel.anchorMin = new Vector2(0f, 1f);
            menuPanel.anchorMax = new Vector2(1f, 1f);
            menuPanel.pivot = new Vector2(0.5f, 1f);
            menuPanel.sizeDelta = new Vector2(0f, 200f);

            Sprite boardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/Environments/Gemini_Generated_Image_lx4qndlx4qndlx4q.png");
            Sprite btnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/General Buton.png");
            Sprite goldIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Icon_Gold_Coin.png");
            Sprite kadimIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Icon_Kadim_Coin.png");
            Sprite pullTabSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Wood_Pull_Tab.png");
            TMP_FontAsset tavernFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset");

            Image panelImg = menuPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = Color.white;
                if (panelImg.sprite == null && boardSprite != null)
                    panelImg.sprite = boardSprite;
            }

            // 3. Sol Üst Oval: Altın Yuvası
            Transform goldSlotT = menuPanel.Find("GoldSlot");
            GameObject goldSlotObj;
            if (goldSlotT == null)
            {
                goldSlotObj = new GameObject("GoldSlot", typeof(RectTransform));
                goldSlotObj.transform.SetParent(menuPanel, false);
            }
            else
            {
                goldSlotObj = goldSlotT.gameObject;
            }

            RectTransform goldSlotRT = goldSlotObj.GetComponent<RectTransform>();
            goldSlotRT.anchorMin = new Vector2(0.065f, 0.54f);
            goldSlotRT.anchorMax = new Vector2(0.245f, 0.88f);
            goldSlotRT.pivot = new Vector2(0.5f, 0.5f);
            goldSlotRT.offsetMin = Vector2.zero;
            goldSlotRT.offsetMax = Vector2.zero;

            // Altın İkonu
            Transform gIconT = goldSlotObj.transform.Find("Icon");
            GameObject gIconObj = gIconT != null ? gIconT.gameObject : new GameObject("Icon", typeof(RectTransform), typeof(Image));
            gIconObj.transform.SetParent(goldSlotObj.transform, false);
            RectTransform gIconRT = gIconObj.GetComponent<RectTransform>();
            gIconRT.anchorMin = new Vector2(0f, 0.5f);
            gIconRT.anchorMax = new Vector2(0f, 0.5f);
            gIconRT.pivot = new Vector2(0.5f, 0.5f);
            gIconRT.anchoredPosition = new Vector2(30f, 0f);
            gIconRT.sizeDelta = new Vector2(46f, 46f);
            goldIcon = gIconObj.GetComponent<Image>();
            goldIcon.preserveAspect = true;
            if (goldIconSprite != null) goldIcon.sprite = goldIconSprite;

            // Altın Text
            if (goldText != null)
            {
                goldText.transform.SetParent(goldSlotObj.transform, false);
                RectTransform gTextRT = goldText.GetComponent<RectTransform>();
                gTextRT.anchorMin = new Vector2(0f, 0f);
                gTextRT.anchorMax = new Vector2(1f, 1f);
                gTextRT.offsetMin = new Vector2(62f, 0f);
                gTextRT.offsetMax = new Vector2(-10f, 0f);
                goldText.fontSize = 26f;
                goldText.fontStyle = FontStyles.Bold;
                goldText.color = new Color(1f, 0.88f, 0.4f);
                goldText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
                if (tavernFont != null) goldText.font = tavernFont;
                var le = goldText.GetComponent<LayoutElement>();
                if (le != null) DestroyImmediateSafe(le);
            }

            // 4. Sol Alt Oval: Kadim Para Yuvası
            Transform kadimSlotT = menuPanel.Find("KadimSlot");
            GameObject kadimSlotObj;
            if (kadimSlotT == null)
            {
                kadimSlotObj = new GameObject("KadimSlot", typeof(RectTransform));
                kadimSlotObj.transform.SetParent(menuPanel, false);
            }
            else
            {
                kadimSlotObj = kadimSlotT.gameObject;
            }

            RectTransform kadimSlotRT = kadimSlotObj.GetComponent<RectTransform>();
            kadimSlotRT.anchorMin = new Vector2(0.065f, 0.12f);
            kadimSlotRT.anchorMax = new Vector2(0.245f, 0.46f);
            kadimSlotRT.pivot = new Vector2(0.5f, 0.5f);
            kadimSlotRT.offsetMin = Vector2.zero;
            kadimSlotRT.offsetMax = Vector2.zero;

            // Kadim Para İkonu
            Transform kIconT = kadimSlotObj.transform.Find("Icon");
            GameObject kIconObj = kIconT != null ? kIconT.gameObject : new GameObject("Icon", typeof(RectTransform), typeof(Image));
            kIconObj.transform.SetParent(kadimSlotObj.transform, false);
            RectTransform kIconRT = kIconObj.GetComponent<RectTransform>();
            kIconRT.anchorMin = new Vector2(0f, 0.5f);
            kIconRT.anchorMax = new Vector2(0f, 0.5f);
            kIconRT.pivot = new Vector2(0.5f, 0.5f);
            kIconRT.anchoredPosition = new Vector2(30f, 0f);
            kIconRT.sizeDelta = new Vector2(46f, 46f);
            kadimIcon = kIconObj.GetComponent<Image>();
            kadimIcon.preserveAspect = true;
            if (kadimIconSprite != null) kadimIcon.sprite = kadimIconSprite;

            // Kadim Para Text
            if (kadimParaText != null)
            {
                kadimParaText.transform.SetParent(kadimSlotObj.transform, false);
                RectTransform kTextRT = kadimParaText.GetComponent<RectTransform>();
                kTextRT.anchorMin = new Vector2(0f, 0f);
                kTextRT.anchorMax = new Vector2(1f, 1f);
                kTextRT.offsetMin = new Vector2(62f, 0f);
                kTextRT.offsetMax = new Vector2(-10f, 0f);
                kadimParaText.fontSize = 26f;
                kadimParaText.fontStyle = FontStyles.Bold;
                kadimParaText.color = new Color(0.85f, 0.7f, 1f);
                kadimParaText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
                if (tavernFont != null) kadimParaText.font = tavernFont;
                var le = kadimParaText.GetComponent<LayoutElement>();
                if (le != null) DestroyImmediateSafe(le);
            }

            // 5. Sağ 1. Çerçeve: Dükkan Butonu (RÜNE 1 Bölgesi)
            if (shopToggleButton != null)
            {
                shopToggleButton.transform.SetParent(menuPanel, false);
                RectTransform rt = shopToggleButton.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchorMin = new Vector2(0.265f, 0.28f);
                rt.anchorMax = new Vector2(0.485f, 0.72f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                Image img = shopToggleButton.GetComponent<Image>();
                if (img == null) img = shopToggleButton.gameObject.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = true;
                if (btnSprite != null) img.sprite = btnSprite;

                if (shopToggleButtonText != null)
                {
                    RectTransform txtRT = shopToggleButtonText.GetComponent<RectTransform>();
                    txtRT.anchorMin = Vector2.zero;
                    txtRT.anchorMax = Vector2.one;
                    txtRT.offsetMin = Vector2.zero;
                    txtRT.offsetMax = Vector2.zero;
                    txtRT.localScale = Vector3.one;
                    shopToggleButtonText.fontSize = 24f;
                    shopToggleButtonText.color = new Color(1f, 0.96f, 0.85f);
                    shopToggleButtonText.alignment = TextAlignmentOptions.Center;
                    shopToggleButtonText.raycastTarget = false;
                    if (tavernFont != null) shopToggleButtonText.font = tavernFont;
                }
                var le = shopToggleButton.GetComponent<LayoutElement>();
                if (le != null) DestroyImmediateSafe(le);
            }

            // 6. Sağ 2. Çerçeve: Envanter Butonu (RÜNE 2 Bölgesi)
            if (inventoryButton != null)
            {
                inventoryButton.transform.SetParent(menuPanel, false);
                RectTransform rt = inventoryButton.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchorMin = new Vector2(0.505f, 0.28f);
                rt.anchorMax = new Vector2(0.728f, 0.72f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                Image img = inventoryButton.GetComponent<Image>();
                if (img == null) img = inventoryButton.gameObject.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = true;
                if (btnSprite != null) img.sprite = btnSprite;

                TextMeshProUGUI invTxt = inventoryButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (invTxt != null)
                {
                    RectTransform txtRT = invTxt.GetComponent<RectTransform>();
                    txtRT.anchorMin = Vector2.zero;
                    txtRT.anchorMax = Vector2.one;
                    txtRT.offsetMin = Vector2.zero;
                    txtRT.offsetMax = Vector2.zero;
                    txtRT.localScale = Vector3.one;
                    invTxt.fontSize = 24f;
                    invTxt.color = new Color(1f, 0.96f, 0.85f);
                    invTxt.alignment = TextAlignmentOptions.Center;
                    invTxt.raycastTarget = false;
                    if (tavernFont != null) invTxt.font = tavernFont;
                }
                var le = inventoryButton.GetComponent<LayoutElement>();
                if (le != null) DestroyImmediateSafe(le);
            }

            // 7. Sağ 3. Çerçeve: Lobiye Dön Butonu (RÜNE 3 Bölgesi)
            if (lobbyButton != null)
            {
                lobbyButton.transform.SetParent(menuPanel, false);
                RectTransform rt = lobbyButton.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchorMin = new Vector2(0.748f, 0.28f);
                rt.anchorMax = new Vector2(0.970f, 0.72f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                Image img = lobbyButton.GetComponent<Image>();
                if (img == null) img = lobbyButton.gameObject.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = true;
                if (btnSprite != null) img.sprite = btnSprite;

                TextMeshProUGUI lobbyTxt = lobbyButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (lobbyTxt != null)
                {
                    RectTransform txtRT = lobbyTxt.GetComponent<RectTransform>();
                    txtRT.anchorMin = Vector2.zero;
                    txtRT.anchorMax = Vector2.one;
                    txtRT.offsetMin = Vector2.zero;
                    txtRT.offsetMax = Vector2.zero;
                    txtRT.localScale = Vector3.one;
                    lobbyTxt.fontSize = 24f;
                    lobbyTxt.color = new Color(1f, 0.96f, 0.85f);
                    lobbyTxt.alignment = TextAlignmentOptions.Center;
                    lobbyTxt.raycastTarget = false;
                    if (tavernFont != null) lobbyTxt.font = tavernFont;
                }
                var le = lobbyButton.GetComponent<LayoutElement>();
                if (le != null) DestroyImmediateSafe(le);
            }

            // 8. Alt Çekmece Dili (ArrowButton): MenuPanel'e bağlanır ve alt ortadan sarkar
            if (arrowButton != null)
            {
                arrowButton.transform.SetParent(menuPanel, false);
                RectTransform rt = arrowButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, 4f);
                rt.sizeDelta = new Vector2(100f, 44f);

                Image img = arrowButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                    if (pullTabSprite != null) img.sprite = pullTabSprite;
                }

                if (arrowText != null)
                {
                    arrowText.fontSize = 22f;
                    arrowText.color = new Color(1f, 0.88f, 0.4f);
                    arrowText.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void DestroyImmediateSafe(Component c)
        {
            if (!Application.isPlaying)
                DestroyImmediate(c);
            else
                Destroy(c);
        }
#endif

        public void SetCurrentShop(GameObject shopCanvas, bool isPotionSellingShop)
        {
            _currentShopCanvas = shopCanvas;
            _isDukkanContext = true;

            if (shopToggleButton != null)
                shopToggleButton.gameObject.SetActive(isPotionSellingShop);

            if (lobbyButton != null)
            {
                lobbyButton.gameObject.SetActive(true);
                EnsureButtonInteractable(lobbyButton);
            }

            UpdateShopButtonText();
            RefreshCurrencyUI();
        }

        public void SetContext(bool isDukkan)
        {
            _isDukkanContext = isDukkan;
            UpdateContextVisibility();
        }

        private void UpdateContextVisibility()
        {
            if (shopToggleButton != null)
                shopToggleButton.gameObject.SetActive(_isDukkanContext);

            if (lobbyButton != null)
            {
                lobbyButton.gameObject.SetActive(_isDukkanContext);
                if (_isDukkanContext) EnsureButtonInteractable(lobbyButton);
            }
        }

        public void ToggleMenu()
        {
            if (!_isOpen && IsAnyBlockingUIOpen())
            {
                Debug.Log("[TopDropdownMenu] Başka bir pencere veya arayüz açıkken menü açılamaz.");
                return;
            }

            _isOpen = !_isOpen;

            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
            }

            _animCoroutine = StartCoroutine(AnimateMenu(_isOpen));
        }

        private IEnumerator AnimateMenu(bool open)
        {
            if (menuPanel == null) yield break;

            UpdateArrowVisual(open);

            Vector2 startPos = menuPanel.anchoredPosition;
            Vector2 targetPos = open ? _openPosition : _closedPosition;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float curveT = animationCurve.Evaluate(t);

                menuPanel.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curveT);
                yield return null;
            }

            menuPanel.anchoredPosition = targetPos;
            UpdateArrowVisual(open);
            _animCoroutine = null;
        }

        private void UpdateArrowVisual(bool open)
        {
            if (arrowText != null)
            {
                arrowText.text = open ? "▲" : "▼";
            }

            if (arrowIcon != null && arrowIcon != arrowButton.transform)
            {
                arrowIcon.localEulerAngles = new Vector3(0, 0, open ? 180f : 0f);
            }
        }

        private void RefreshCurrencyUI()
        {
            if (goldText != null)
                goldText.text = GameManager.SharedGold.ToString("N0");

            if (kadimParaText != null)
                kadimParaText.text = GameManager.SharedKadimPara.ToString("N0");

            if (fameText != null)
                fameText.text = GameManager.SharedReputation.ToString("N0");
        }

        private void HandleGoldChanged(int newGold)
        {
            if (goldText != null)
                goldText.text = newGold.ToString("N0");
        }

        private void HandleKadimParaChanged(int newKadimPara)
        {
            if (kadimParaText != null)
                kadimParaText.text = newKadimPara.ToString("N0");
        }

        private void HandleReputationChanged(int newReputation)
        {
            if (fameText != null)
                fameText.text = newReputation.ToString("N0");
        }

        private void OnShopToggleClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ToggleShop();
            }
            CloseMenu();
        }

        private void HandleShopStatusChanged(bool isOpen)
        {
            UpdateShopButtonText();
        }

        private void UpdateShopButtonText()
        {
            if (GameManager.Instance != null)
            {
                bool isOpen = GameManager.Instance.isShopOpen;
                if (shopToggleButtonText != null)
                {
                    UIThemeHelper.ApplyNewRocker(shopToggleButtonText);
                    shopToggleButtonText.text = isOpen ? LocalizationManager.Get("top_close_shop") : LocalizationManager.Get("top_open_shop");
                }
                if (shopButtonIcon != null)
                {
                    shopButtonIcon.sprite = isOpen ? shopOpenSprite : shopCloseSprite;
                }
            }
        }

        /// <summary>
        /// O an sahnede aktif olan dükkan kök GameObject'ini döndürür.
        /// </summary>
        public GameObject GetActiveShop()
        {
            if (PotionTownSceneCoordinator.Instance != null)
            {
                var curRoom = PotionTownSceneCoordinator.Instance.CurrentRoom;
                switch (curRoom)
                {
                    case PotionTownRoom.PotionSelling: return PotionTownSceneCoordinator.Instance.potionSellingRoom;
                    case PotionTownRoom.PotionCrafting: return PotionTownSceneCoordinator.Instance.potionCraftingRoom;
                    case PotionTownRoom.IngredientShop: return PotionTownSceneCoordinator.Instance.ingredientShopRoom;
                    default: return null;
                }
            }

            if (_currentShopCanvas != null && _currentShopCanvas.activeInHierarchy)
            {
                return _currentShopCanvas;
            }

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                string rName = root.name.Trim();
                if ((rName.Contains("İksirSatışAna") || rName.Contains("İksirYapma") || rName.Contains("MalzemeDükkan")) && root.activeInHierarchy)
                {
                    _currentShopCanvas = root;
                    return root;
                }
            }

            return null;
        }

        /// <summary>
        /// Sahnede o an açık ve görünür olan InventoryUI nesnesini bulur.
        /// </summary>
        private InventoryUI FindOpenInventoryUI()
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
            {
                return InventoryUI.Instance;
            }

            var activeInvs = FindObjectsByType<InventoryUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (activeInvs.Length > 0)
            {
                return activeInvs[0];
            }

            return null;
        }

        private void OnInventoryClicked()
        {
            Debug.Log("<color=cyan>[TopDropdownMenu]</color> Envanter butonuna tıklandı.");

            // 1. StorageManager üzerinden öncelikli toggle
            if (StorageManager.Instance != null)
            {
                StorageManager sm = StorageManager.Instance;
                sm.EnsureStoragePanel();
                if (sm.storagePanel != null && sm.storagePanel.activeInHierarchy)
                {
                    sm.CloseStorage();
                }
                else
                {
                    sm.OpenStorageViewOnly();
                }
                CloseMenu();
                return;
            }

            // 2. Halihazırda açık bir InventoryUI varsa, kapat (Toggle)
            InventoryUI openInv = FindOpenInventoryUI();
            if (openInv != null)
            {
                openInv.Close();
                CloseMenu();
                return;
            }

            // 3. Fallback: Sahnede InventoryUI bul ve aç
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.OpenInventory();
                CloseMenu();
                return;
            }

            var allInvs = Resources.FindObjectsOfTypeAll<InventoryUI>();
            foreach (var inv in allInvs)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(inv.gameObject)) continue;
#endif
                inv.gameObject.SetActive(true);
                inv.OpenInventory();
                CloseMenu();
                return;
            }

            CloseMenu();
            Debug.LogWarning("[TopDropdownMenu] Sahnede hiçbir StorageManager veya InventoryUI bulunamadı!");
        }

        public void EnsureSceneReferences()
        {
            if (PotionTownSceneCoordinator.Instance != null)
            {
                if (mapCanvas == null) mapCanvas = PotionTownSceneCoordinator.Instance.lobbyRoom;
                if (gameCanvas == null) gameCanvas = PotionTownSceneCoordinator.Instance.potionSellingRoom;
                return;
            }

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                string rName = root.name.Trim();
                if (mapCanvas == null && rName.Contains("LobiSahnesi"))
                {
                    mapCanvas = root;
                }
                else if (gameCanvas == null && rName.Contains("İksirSatışAna"))
                {
                    gameCanvas = root;
                }
            }
        }

        /// <summary>
        /// Sahnede önceden kalmış olan eski tabela ve butonları (Dükkanı Aç, Envanter, Lobi) gizler.
        /// </summary>
        public void HideLegacySceneButtons()
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var allButtons = root.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    // Menüye atanmış butonları veya menünün içindeki butonları ASLA kapatma
                    if (b == inventoryButton || b == lobbyButton || b == shopToggleButton || b == arrowButton || b.transform.IsChildOf(transform))
                    {
                        continue;
                    }

                    string bName = b.name;
                    if (bName == "OyunBaşlatmaİconu" || bName == "OyunBaslatmaIconu" ||
                        bName == "Inventory Button" ||
                        bName == "Lobby_Button" || bName == "Lobi_Button")
                    {
                        b.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnLobbyClicked()
        {
            Debug.Log("<color=cyan>[TopDropdownMenu]</color> Lobiye Dön butonuna tıklandı.");

            // Açık envanter veya depo varsa kapat
            if (StorageManager.Instance != null)
            {
                StorageManager.Instance.CloseStorage();
            }
            InventoryUI openInv = FindOpenInventoryUI();
            if (openInv != null)
            {
                openInv.Close();
            }

            // Tüm dükkanları kapat ve Lobiyi aç
            CloseAllShopsAndOpenLobby();

            // Menü bağlamını lobiye geçir (Dükkan Aç/Kapat ve Lobiye Dön butonları gizlenir, Envanter kalır)
            SetContext(false);

            CloseMenu();

            Debug.Log("<color=green>[TopDropdownMenu]</color> Lobiye başarıyla dönüldü.");
        }

        /// <summary>
        /// Sahnedeki tüm dükkan kök objelerini kapatır ve LobiSahnesi'ni açar.
        /// </summary>
        public void CloseAllShopsAndOpenLobby()
        {
            if (PotionTownSceneCoordinator.Instance != null)
            {
                PotionTownSceneCoordinator.Instance.ShowLobby();
                _currentShopCanvas = null;
                return;
            }

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject lobiObj = null;

            foreach (var root in rootObjects)
            {
                string rName = root.name.Trim();
                if (rName.Contains("LobiSahnesi"))
                {
                    lobiObj = root;
                }
                else if (rName.Contains("İksirSatışAna") || rName.Contains("İksirYapma") || rName.Contains("MalzemeDükkan"))
                {
                    root.SetActive(false);
                }
            }

            if (lobiObj != null)
            {
                lobiObj.SetActive(true);
                mapCanvas = lobiObj;
            }
            else if (mapCanvas != null)
            {
                mapCanvas.SetActive(true);
            }

            _currentShopCanvas = null;
        }

        public void OpenMenu()
        {
            if (IsAnyBlockingUIOpen()) return;
            if (!_isOpen) ToggleMenu();
        }

        public void CloseMenu()
        {
            if (_isOpen) ToggleMenu();
        }

        public bool IsMenuOpen => _isOpen;

        /// <summary>
        /// Ayarlar butonuna tıklandığında çağrılır.
        /// SettingsMenuUI prefab'ını instantiate eder ve tam ekran olarak açar.
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("<color=cyan>[TopDropdownMenu]</color> Ayarlar butonuna tıklandı.");
            CloseMenu();

            // Zaten açık bir instance varsa, onu göster
            if (_settingsMenuInstance != null)
            {
                _settingsMenuInstance.Open();
                return;
            }

            // Prefab atanmış mı kontrol et
            if (settingsMenuPrefab == null)
            {
                Debug.LogWarning("[TopDropdownMenu] settingsMenuPrefab atanmamış! Inspector'dan SettingsMenuUI prefabını sürükleyin.");
                return;
            }

            // Prefab'ı instantiate et
            GameObject instance = Instantiate(settingsMenuPrefab);
            instance.name = "SettingsMenuUI_Instance";
            _settingsMenuInstance = instance.GetComponent<SettingsMenuUI>();

            if (_settingsMenuInstance != null)
            {
                _settingsMenuInstance.Open();
            }
            else
            {
                Debug.LogError("[TopDropdownMenu] Instantiate edilen prefab'da SettingsMenuUI bileşeni bulunamadı!");
                Destroy(instance);
            }
        }

        public void OpenShopUI()
        {
            CloseMenu();
            ShopUI.Open();
        }

        public void OpenGoldMarket()
        {
            CloseMenu();
            ShopUI.Open(ShopUI.ShopCategory.Bundles);
        }

        public void OpenKadimMarket()
        {
            CloseMenu();
            ShopUI.Open(ShopUI.ShopCategory.KadimPara);
        }

        public void OpenSettingsMenu()
        {
            OnSettingsClicked();
        }

        /// <summary>
        /// Farenin ekran koordinatının üst menünün ahşap paneli veya çekmece dili (ok butonu)
        /// üzerinde olup olmadığını tam olarak denetler.
        /// </summary>
        public bool IsPointerOverMenu(Vector2 screenPos)
        {
            if (arrowButton != null)
            {
                RectTransform arrowRT = arrowButton.GetComponent<RectTransform>();
                if (arrowRT != null && RectTransformUtility.RectangleContainsScreenPoint(arrowRT, screenPos))
                    return true;
            }

            if (_isOpen && menuPanel != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(menuPanel, screenPos))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/PotionTavern - Üst Dropdown Menü", false, 10)]
        private static void CreateTopDropdownMenu()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject root = new GameObject("TopDropdownMenu", typeof(RectTransform), typeof(TopDropdownMenu));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0, 1);
            rootRT.anchorMax = new Vector2(1, 1);
            rootRT.pivot = new Vector2(0.5f, 1);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(0, 200);

            GameObject panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 1);
            panelRT.anchorMax = new Vector2(1, 1);
            panelRT.pivot = new Vector2(0.5f, 1);
            panelRT.anchoredPosition = new Vector2(0, 200);
            panelRT.sizeDelta = new Vector2(0, 200);

            GameObject goldObj = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldObj.transform.SetParent(panel.transform, false);
            var goldTMP = goldObj.GetComponent<TextMeshProUGUI>();
            goldTMP.text = "0";

            GameObject kadimObj = new GameObject("KadimParaText", typeof(RectTransform), typeof(TextMeshProUGUI));
            kadimObj.transform.SetParent(panel.transform, false);
            var kadimTMP = kadimObj.GetComponent<TextMeshProUGUI>();
            kadimTMP.text = "0";

            GameObject shopBtnObj = CreateEmptyButton(panel.transform, "ShopToggleButton", "Dükkanı Aç");
            GameObject invBtnObj = CreateEmptyButton(panel.transform, "InventoryButton", "Envanter");
            GameObject lobbyBtnObj = CreateEmptyButton(panel.transform, "LobbyButton", "Kasabaya Dön");

            GameObject arrowObj = new GameObject("ArrowButton", typeof(RectTransform), typeof(Image), typeof(Button));
            arrowObj.transform.SetParent(panel.transform, false);
            GameObject arrowTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowTxtObj.transform.SetParent(arrowObj.transform, false);
            var arrowTMP = arrowTxtObj.GetComponent<TextMeshProUGUI>();
            arrowTMP.text = "▼";

            TopDropdownMenu menu = root.GetComponent<TopDropdownMenu>();
            menu.startOpen = false;
            menu.menuPanel = panelRT;
            menu.arrowButton = arrowObj.GetComponent<Button>();
            menu.arrowText = arrowTMP;
            menu.goldText = goldTMP;
            menu.kadimParaText = kadimTMP;
            menu.shopToggleButton = shopBtnObj.GetComponent<Button>();
            menu.shopToggleButtonText = shopBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            menu.inventoryButton = invBtnObj.GetComponent<Button>();
            menu.lobbyButton = lobbyBtnObj.GetComponent<Button>();

            menu.AlignToWoodenBoard();

            UnityEditor.Selection.activeGameObject = root;
            UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Create Top Dropdown Menu");

            Debug.Log("<color=green>[TopDropdownMenu]</color> Üst Ahşap Dropdown Menü başarıyla oluşturuldu ve hizalandı!");
        }

        private static GameObject CreateEmptyButton(Transform parent, string name, string label)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var txtTMP = txtObj.GetComponent<TextMeshProUGUI>();
            txtTMP.text = label;
            txtTMP.fontSize = 24;
            txtTMP.color = Color.white;
            txtTMP.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }
#endif
    }
}
