using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Parşömen tarzı lisans satın alma popup sistemi.
    /// Tarifleri sayfalara ayırır (Sayfa 1 - 7).
    /// Her sayfada iksir listesi, tek tek lisans alma ve indirimli toplu lisans alma seçenekleri sunar.
    /// Oyunun el çizimi fantezi sanat tarzına (oymalı ahşap plakalar, rünler, balmumu mühürler,
    /// Slot çerçeveleri ve NewRocker yazı tipi) tam uyumlu, görsel olarak zengin bir arayüz oluşturur.
    /// </summary>
    public class LicenseParchmentUI : MonoBehaviour
    {
        public static LicenseParchmentUI Instance { get; private set; }

        private const float PARCHMENT_WIDTH = 1150f;
        private const float PARCHMENT_HEIGHT = 950f;

        private GameObject _popupRoot;
        private GameObject _darkOverlay;
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;
        private ParchmentPageController _controller;

        // Görsel varlık önbelleği (Sprite'lar & Font)
        private static bool _assetsLoaded = false;
        private static Sprite _headerBannerSprite;
        private static Sprite _rowPlateSprite;
        private static Sprite _slotFrameSprite;
        private static Sprite _buyButtonSprite;
        private static Sprite _bulkBannerSprite;
        private static Sprite _closeSealSprite;
        private static Sprite _sealStampSprite;
        private static Sprite _dividerSprite;
        private static Sprite _goldCoinSprite;
        private static TMP_FontAsset _fantasyFont;

        // Modüler Prefab önbelleği (Assets/_PotionTown/Prefabs/UI/License/)
        private static GameObject _rowPrefab;
        private static GameObject _headerPrefab;
        private static GameObject _bulkBuyPrefab;
        private static GameObject _closeButtonPrefab;
        private static GameObject _pagePrefab;
        private static GameObject _mainParchmentPrefab;

        // Sayfa içeriklerini güncellemek için referanslar
        private List<LicensePageData> _pageDataList = new List<LicensePageData>();

        private class LicensePageData
        {
            public int pageIndex;
            public List<RecipeData> pageRecipes = new List<RecipeData>();
            public List<LicenseRowData> rows = new List<LicenseRowData>();
            public TextMeshProUGUI bulkButtonText;
            public Button bulkButton;
            public Image bulkButtonImage;
            public Image bulkCoinIcon; // Toplu satın al butonundaki altın coin görseli
            public TextMeshProUGUI bulkSubText; // Alt açıklama yazısı (ayrı obje olarak)
            public GameObject pageObj;
        }

        private class LicenseRowData
        {
            public RecipeData recipe;
            public Image slotFrameImage;
            public Image iconImage;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI costText;
            public Image coinIcon; // Fiyatın yanındaki altın coin görseli
            public GameObject buyBtnObj;
            public Button buyButton;
            public Image buyButtonImage;
            public TextMeshProUGUI buyButtonText;
            public GameObject unlockedBadgeObj;
            public Image unlockedSealImage;
            public TextMeshProUGUI unlockedBadgeText;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            LicenseManager.OnLicenseChanged -= RefreshAllPages;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            RefreshAllPages();
        }

        private void Update()
        {
            if (_isOpen)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Close();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }
#endif
            }
        }

        // ============================================================
        // GÖRSEL VARLIKLARI YÜKLEME (SPRITES & FONTS)
        // ============================================================

        private static void EnsureVisualAssetsLoaded()
        {
            if (_assetsLoaded && _headerBannerSprite != null) return;

            // 1. Yeni üretilen özel Lisans UI sprite'ları
            _headerBannerSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Header_Banner.png", "LicenseUI/License_Header_Banner");
            _rowPlateSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Row_Plate.png", "LicenseUI/License_Row_Plate");
            _buyButtonSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Buy_Button.png", "LicenseUI/License_Buy_Button");
            _bulkBannerSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Bulk_Banner.png", "LicenseUI/License_Bulk_Banner");
            _closeSealSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Close_Seal.png", "LicenseUI/License_Close_Seal");
            _sealStampSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Seal_Stamp.png", "LicenseUI/License_Seal_Stamp");
            _dividerSprite = LoadSpriteAsset("Assets/_PotionTown/Art/UI/License/License_Divider.png", "LicenseUI/License_Divider");
            _goldCoinSprite = LoadSpriteAsset("Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Gold Coin.png", "LicenseUI/Gold Coin");

            // 2. Oyunun orijinal Slot çerçevesi (Slot.png)
            _slotFrameSprite = LoadSubSpriteAsset("Assets/_PotionTown/Art/UI/Slot.png", "Slot_0");

            // 3. Oyunun NewRocker fantezi yazı tipi
            _fantasyFont = LoadFantasyFont();

            // 4. Modüler Prefablar (Assets/_PotionTown/Prefabs/UI/License/ veya Resources/LicenseUI/)
            _rowPrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/Lisans_Satır.prefab", "LicenseUI/Lisans_Satır");
            _headerPrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/Lisans_Başlık.prefab", "LicenseUI/Lisans_Başlık");
            _bulkBuyPrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/Lisans_TopluSatınAl.prefab", "LicenseUI/Lisans_TopluSatınAl");
            _closeButtonPrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/Lisans_KapatButonu.prefab", "LicenseUI/Lisans_KapatButonu");
            _pagePrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/License_Prefab.prefab", "LicenseUI/License_Prefab");
            if (_pagePrefab == null)
            {
                _pagePrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/Lisans_Sayfa.prefab", "LicenseUI/Lisans_Sayfa");
            }
            _mainParchmentPrefab = LoadPrefabAsset("Assets/_PotionTown/Prefabs/UI/License/LisansParşomeni.prefab", "LicenseUI/LisansParsomeni");

            _assetsLoaded = true;
        }

        private static GameObject LoadPrefabAsset(string assetPath, string resourcesPath = null)
        {
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                GameObject res = Resources.Load<GameObject>(resourcesPath);
                if (res != null) return res;
            }
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            return null;
#endif
        }

        private static Sprite LoadSpriteAsset(string assetPath, string resourcesPath)
        {
            Sprite s = null;
#if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (s != null) return s;
#endif
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                s = Resources.Load<Sprite>(resourcesPath);
                if (s != null) return s;
            }
            return null;
        }

        private static Sprite LoadSubSpriteAsset(string assetPath, string subSpriteName)
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var a in assets)
            {
                if (a is Sprite sp && (string.IsNullOrEmpty(subSpriteName) || sp.name == subSpriteName))
                    return sp;
            }
            // İlk sprite'ı döndür
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
#endif
            return null;
        }

        private static TMP_FontAsset LoadFantasyFont()
        {
#if UNITY_EDITOR
            var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset");
            if (f != null) return f;
#endif
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var fnt in allFonts)
            {
                if (fnt.name.Contains("NewRocker")) return fnt;
            }
            return TMP_Settings.defaultFontAsset;
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        /// <summary>
        /// Lisans parşömenini açar.
        /// </summary>
        public static LicenseParchmentUI Open(GameObject customParchmentPrefab = null)
        {
            if (Instance != null && Instance._isOpen)
            {
                Instance.Close();
            }

            // Görsel varlıkları yükle
            EnsureVisualAssetsLoaded();

            // 1. Ana Parşömen prefabını ve sayfa prefabını belirle
            GameObject parchmentPrefab = null;
            if (customParchmentPrefab != null)
            {
                if (customParchmentPrefab.name.Contains("License") || customParchmentPrefab.transform.Find("Sayfa") != null)
                {
                    _pagePrefab = customParchmentPrefab;
                    parchmentPrefab = FindMainParchmentPrefab();
                }
                else
                {
                    parchmentPrefab = customParchmentPrefab;
                }
            }
            if (parchmentPrefab == null)
            {
                parchmentPrefab = FindMainParchmentPrefab();
            }

            if (parchmentPrefab == null)
            {
                Debug.LogError("[LisansParşömen] Parşömen prefabı bulunamadı!");
                return null;
            }

            // TopDropdownMenu açıksa kapat
            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsMenuOpen)
            {
                TopDropdownMenu.Instance.CloseMenu();
            }

            // 2. Parşömen Popup'ını Instantiate et
            GameObject popup = Instantiate(parchmentPrefab);
            popup.name = $"{parchmentPrefab.name}_LicensePopup";
            popup.SetActive(true);

            // Canvas ayarlarını garantiye al
            Canvas canvas = popup.GetComponent<Canvas>();
            if (canvas == null) canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 2500;

            if (popup.GetComponent<GraphicRaycaster>() == null)
            {
                popup.AddComponent<GraphicRaycaster>();
            }

            // CanvasScaler ayarları: 1920x1080 ve matchWidthOrHeight = 0.5f (diğer parşömenlerle birebir aynı boyut)
            CanvasScaler scaler = popup.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = popup.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = popup.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = Vector2.zero;
                if (rootRect.localScale == Vector3.zero)
                {
                    rootRect.localScale = Vector3.one;
                }
            }

            // UI script ekle
            LicenseParchmentUI ui = popup.AddComponent<LicenseParchmentUI>();
            ui._popupRoot = popup;
            ui._isOpen = true;
            Instance = ui;

            // Karanlık Arka Plan (tıklayınca kapatır)
            ui.CreateDarkOverlay(popup);

            // Kapatma butonu (Kırmızı Balmumu Mührü)
            ui.CreateCloseButton(popup);

            // Mevcut kapatma butonlarını bağla
            ui.BindCloseButtons(popup);

            // 3. ParchmentPageController'ı al
            ParchmentPageController controller = popup.GetComponent<ParchmentPageController>();
            if (controller == null) controller = popup.AddComponent<ParchmentPageController>();
            ui._controller = controller;

            Transform pageContainer = controller.TargetContainer;

            // 4. Parşömen arkaplan sprite'ını bul
            Sprite parchmentSprite = FindParchmentSprite();

            // 5. Sayfa bazlı sayfaları oluştur (Sayfa 1 - Sayfa 7)
            List<GameObject> dynamicPages = new List<GameObject>();
            ui._pageDataList.Clear();

            var pages = LicenseManager.GetRecipesGroupedByPage();
            GameObject licensePagePrefab = FindAnyPagePrefab();

            for (int p = 0; p < pages.Count; p++)
            {
                int pageNum = p + 1;
                List<RecipeData> recipes = pages[p];
                LicensePageData pageData;

                if (licensePagePrefab != null && (licensePagePrefab.name.Contains("License") || licensePagePrefab.transform.Find("Sayfa") != null))
                {
                    pageData = CreatePageFromLicensePrefab(licensePagePrefab, pageContainer, pageNum, recipes, ui);
                }
                else
                {
                    pageData = CreateDynamicPageFallback(pageContainer, pageNum, parchmentSprite, recipes, ui, pages.Count);
                }

                pageData.pageObj.SetActive(false);
                dynamicPages.Add(pageData.pageObj);
                ui._pageDataList.Add(pageData);
            }

            // Eğer hiç tarif yoksa boş sayfa göster
            if (dynamicPages.Count == 0)
            {
                GameObject emptyPage = CreatePage(pageContainer, 1, parchmentSprite);
                Transform emptyContent = emptyPage.transform.Find("Content");
                if (emptyContent == null) emptyContent = emptyPage.transform;
                CreateTextElement(emptyContent, "Henüz lisanslanabilecek tarif bulunmuyor.", 22, new Color(0.96f, 0.90f, 0.75f, 1f), TextAlignmentOptions.Center);
                emptyPage.SetActive(false);
                dynamicPages.Add(emptyPage);
            }

            // 6. Controller'a sayfaları teslim et
            controller.SetPages(dynamicPages);

            // Navigasyon butonlarını ve kapatma mührünü sayfanın üstüne (öne) getir
            if (controller.prevButton != null) controller.prevButton.transform.SetAsLastSibling();
            if (controller.nextButton != null) controller.nextButton.transform.SetAsLastSibling();
            Transform closeSeal = popup.transform.Find("ParchmentCloseSeal");
            if (closeSeal != null) closeSeal.SetAsLastSibling();

            // Lisans değişikliklerini dinle
            LicenseManager.OnLicenseChanged += ui.RefreshAllPages;
            LocalizationManager.OnLanguageChanged += ui.HandleLanguageChanged;

            // Sayfa değişimlerini dinle (yeni sayfa açıldığında coin ikonunu o sayfa için hizala)
            controller.OnPageChanged += ui.OnPageChangedHandler;

            // İlk güncelleme
            ui.RefreshAllPages();

            // İlk sayfanın coin hizalamasını garantiye al (1 frame sonra mesh oturduğunda)
            if (ui._pageDataList.Count > 0)
            {
                ui.StartCoroutine(ui.AlignBulkCoinIconWhenReadyCoroutine(ui._pageDataList[0]));
            }

            Debug.Log($"[LisansParşömen] {dynamicPages.Count} sayfa fantezi lisans parşömeni açıldı.");
            return ui;
        }

        public void Close()
        {
            _isOpen = false;
            LicenseManager.OnLicenseChanged -= RefreshAllPages;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (_controller != null)
            {
                _controller.OnPageChanged -= OnPageChangedHandler;
            }

            if (_popupRoot != null)
            {
                Destroy(_popupRoot);
                _popupRoot = null;
            }
            _darkOverlay = null;
            _controller = null;
            _pageDataList.Clear();
            Instance = null;
            Debug.Log("[LisansParşömen] Parşömen kapatıldı.");
        }

        private void OnDisable()
        {
            if (_isOpen) Close();
        }

        // ============================================================
        // SAYFA OLUŞTURMA & TEMİZ MİMARİ
        // ============================================================

        /// <summary>
        /// Kullanıcının hazırladığı License_Prefab prefabından lisans sayfası oluşturur ve verileri bağlar.
        /// </summary>
        private static LicensePageData CreatePageFromLicensePrefab(GameObject prefab, Transform parent, int pageNum, List<RecipeData> recipes, LicenseParchmentUI ui)
        {
            GameObject pageObj = Instantiate(prefab, parent, false);
            pageObj.name = $"Lisans_Sayfa_{pageNum}";

            RectTransform prt = pageObj.GetComponent<RectTransform>();
            if (prt != null)
            {
                prt.anchorMin = new Vector2(0.5f, 0.5f);
                prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = Vector2.zero;

                RectTransform prefabRt = prefab.GetComponent<RectTransform>();
                if (prefabRt != null)
                {
                    prt.localScale = prefabRt.localScale;
                    prt.sizeDelta = prefabRt.sizeDelta;
                }
            }

            LicensePageData pageData = new LicensePageData();
            pageData.pageIndex = pageNum;
            pageData.pageRecipes = recipes;
            pageData.pageObj = pageObj;

            Transform sayfa = pageObj.transform.Find("Sayfa");
            if (sayfa == null) sayfa = pageObj.transform;

            // 5 satırı bağla
            for (int i = 0; i < 5; i++)
            {
                Transform rowT = FindRowTransform(sayfa, i);
                if (rowT == null) continue;

                if (i < recipes.Count)
                {
                    RecipeData recipe = recipes[i];
                    rowT.gameObject.SetActive(true);
                    LicenseRowData rowData = BindLicenseRowFromPrefab(rowT, recipe, i, ui);
                    pageData.rows.Add(rowData);
                }
                else
                {
                    rowT.gameObject.SetActive(false);
                }
            }

            // Toplu Satın Alma Butonu (BulkBuyBtn)
            Transform bulkT = sayfa.Find("BulkBuyBtn");
            if (bulkT == null)
            {
                for (int c = 0; c < sayfa.childCount; c++)
                {
                    if (sayfa.GetChild(c).name.ToLower().Contains("bulk"))
                    {
                        bulkT = sayfa.GetChild(c);
                        break;
                    }
                }
            }

            if (bulkT != null)
            {
                Button bulkBtn = bulkT.GetComponent<Button>();
                Image bulkImg = bulkT.GetComponent<Image>();
                
                // Ana metin (ilk TMP)
                TextMeshProUGUI bulkTmp = bulkT.GetComponentInChildren<TextMeshProUGUI>();

                pageData.bulkButton = bulkBtn;
                pageData.bulkButtonImage = bulkImg;
                pageData.bulkButtonText = bulkTmp;

                // Coin ikonu (BulkBuyBtn altındaki Coin objesi)
                Transform bulkCoinT = bulkT.Find("Coin") ?? FindChildByPrefix(bulkT, "Coin");
                if (bulkCoinT != null)
                {
                    pageData.bulkCoinIcon = bulkCoinT.GetComponent<Image>();
                    if (pageData.bulkCoinIcon != null)
                    {
                        pageData.bulkCoinIcon.preserveAspect = true;
                    }
                }

                // Alt açıklama yazısı (SubText objesi — hiyerarşiden düzenlenebilir)
                Transform subTextT = bulkT.Find("SubText") ?? bulkT.Find("AltYazı") ?? FindChildByPrefix(bulkT, "Sub");
                if (subTextT != null)
                {
                    pageData.bulkSubText = subTextT.GetComponent<TextMeshProUGUI>();
                }

                if (bulkBtn != null)
                {
                    List<RecipeData> targetRecipes = pageData.pageRecipes;
                    bulkBtn.onClick.RemoveAllListeners();
                    bulkBtn.onClick.AddListener(() =>
                    {
                        if (LicenseManager.Instance != null)
                        {
                            LicenseManager.Instance.UnlockAllForPage(targetRecipes, out int cost);
                        }
                    });
                }
            }

            // Sayfa açıldığında toplu satın alma coin ikonunun doğru hizalanması için watcher ekle
            LicensePageWatcher watcher = pageObj.AddComponent<LicensePageWatcher>();
            var capturedData = pageData;
            watcher.onPageEnabled = () =>
            {
                AlignAllCoinsForPage(capturedData);
                if (ui != null && ui.gameObject.activeInHierarchy)
                {
                    ui.StartCoroutine(ui.AlignBulkCoinIconWhenReadyCoroutine(capturedData));
                }
            };

            // Sayfa oluşturulduğu anda henüz inaktifken bile tüm coin'leri tam yerlerine hizala
            AlignAllCoinsForPage(pageData);

            return pageData;
        }

        private static LicenseRowData BindLicenseRowFromPrefab(Transform rowT, RecipeData recipe, int rowIndex, LicenseParchmentUI ui)
        {
            LicenseRowData rowData = new LicenseRowData();
            rowData.recipe = recipe;

            // 1. Slot & PotionIcon
            Transform slotT = rowT.Find("SlotFrame") ?? rowT.Find($"SlotFrame ({rowIndex})") ?? FindChildByPrefix(rowT, "SlotFrame");
            if (slotT != null)
            {
                rowData.slotFrameImage = slotT.GetComponent<Image>();
                Transform iconT = slotT.Find("PotionIcon") ?? FindChildByPrefix(slotT, "PotionIcon");
                if (iconT != null)
                {
                    rowData.iconImage = iconT.GetComponent<Image>();
                }
                else
                {
                    rowData.iconImage = slotT.GetComponentInChildren<Image>();
                }

                if (rowData.iconImage != null)
                {
                    rowData.iconImage.enabled = (recipe.resultPotion != null && recipe.resultPotion.itemIcon != null);
                    if (recipe.resultPotion != null && recipe.resultPotion.itemIcon != null)
                    {
                        rowData.iconImage.sprite = recipe.resultPotion.itemIcon;
                    }
                    rowData.iconImage.color = Color.white;
                }
            }

            // 2. Name Text
            Transform nameT = rowT.Find("Name") ?? rowT.Find($"Name ({rowIndex})") ?? FindChildByPrefix(rowT, "Name");
            if (nameT != null)
            {
                rowData.nameText = nameT.GetComponent<TextMeshProUGUI>();
                if (rowData.nameText != null)
                {
                    rowData.nameText.text = recipe.resultPotion != null ? recipe.resultPotion.itemName : recipe.name;
                }
            }

            // 3. Cost Text
            int cost = LicenseManager.GetLicenseCost(recipe);
            Transform costT = rowT.Find("Cost") ?? rowT.Find($"Cost ({rowIndex})") ?? FindChildByPrefix(rowT, "Cost");
            if (costT != null)
            {
                rowData.costText = costT.GetComponent<TextMeshProUGUI>();
                if (rowData.costText != null)
                {
                    rowData.costText.text = cost.ToString();
                }
            }

            // 3b. Coin Icon (Fiyatın yanındaki altın coin görseli — satır altında veya Cost altında olabilir)
            Transform coinT = rowT.Find("Coin") ?? rowT.Find($"Coin ({rowIndex})") ?? FindChildByPrefix(rowT, "Coin");
            if (coinT == null && costT != null)
            {
                coinT = costT.Find("Coin") ?? FindChildByPrefix(costT, "Coin");
            }
            if (coinT != null)
            {
                rowData.coinIcon = coinT.GetComponent<Image>();
                if (rowData.coinIcon != null)
                {
                    rowData.coinIcon.gameObject.SetActive(true);
                    rowData.coinIcon.preserveAspect = true;
                }
            }

            if (rowData.costText != null && rowData.coinIcon != null)
            {
                AlignRowCoinIcon(rowData.costText, rowData.coinIcon);
            }

            // 4. ActionArea (BuyBtn & UnlockedBadge)
            Transform actionT = rowT.Find("ActionArea") ?? rowT.Find($"ActionArea ({rowIndex})") ?? FindChildByPrefix(rowT, "ActionArea");
            if (actionT != null)
            {
                Transform btnT = actionT.Find("BuyBtn");
                if (btnT != null)
                {
                    rowData.buyBtnObj = btnT.gameObject;
                    rowData.buyButton = btnT.GetComponent<Button>();
                    rowData.buyButtonImage = btnT.GetComponent<Image>();
                    rowData.buyButtonText = btnT.GetComponentInChildren<TextMeshProUGUI>();

                    RecipeData captured = recipe;
                    if (rowData.buyButton != null)
                    {
                        rowData.buyButton.onClick.RemoveAllListeners();
                        rowData.buyButton.onClick.AddListener(() =>
                        {
                            if (LicenseManager.Instance != null)
                            {
                                LicenseManager.Instance.UnlockLicense(captured);
                            }
                        });
                    }
                }

                Transform unlT = actionT.Find("UnlockedBadge");
                if (unlT != null)
                {
                    rowData.unlockedBadgeObj = unlT.gameObject;
                    rowData.unlockedBadgeObj.SetActive(false);
                    Transform sealT = unlT.Find("SealIcon");
                    if (sealT != null) rowData.unlockedSealImage = sealT.GetComponent<Image>();
                    Transform sealTxtT = unlT.Find("SealText");
                    if (sealTxtT != null) rowData.unlockedBadgeText = sealTxtT.GetComponent<TextMeshProUGUI>();
                }
            }

            return rowData;
        }

        private static Transform FindRowTransform(Transform sayfa, int index)
        {
            if (index == 0)
            {
                Transform r = sayfa.Find("Satır");
                if (r != null) return r;
                r = sayfa.Find("Satir");
                if (r != null) return r;
            }
            Transform rIndexed = sayfa.Find($"Satır ({index})");
            if (rIndexed != null) return rIndexed;
            rIndexed = sayfa.Find($"Satir ({index})");
            if (rIndexed != null) return rIndexed;

            int count = 0;
            for (int c = 0; c < sayfa.childCount; c++)
            {
                Transform ch = sayfa.GetChild(c);
                string n = ch.name.ToLower();
                if (n.Contains("satır") || n.Contains("satir") || n.Contains("row"))
                {
                    if (count == index) return ch;
                    count++;
                }
            }
            return null;
        }

        private static Transform FindChildByPrefix(Transform parent, string prefix)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }

        private static LicensePageData CreateDynamicPageFallback(Transform pageContainer, int pageNum, Sprite parchmentSprite, List<RecipeData> recipes, LicenseParchmentUI ui, int totalPages)
        {
            GameObject pageObj = CreatePage(pageContainer, pageNum, parchmentSprite);
            Transform contentParent = pageObj.transform.Find("Content");
            if (contentParent == null) contentParent = pageObj.transform;

            LicensePageData pageData = new LicensePageData();
            pageData.pageIndex = pageNum;
            pageData.pageRecipes = recipes;
            pageData.pageObj = pageObj;

            CreateHeaderRow(contentParent, pageNum, totalPages);

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeData recipe = recipes[i];
                LicenseRowData rowData = CreateLicenseRow(contentParent, recipe, ui);
                pageData.rows.Add(rowData);
            }

            CreateBulkBuyButton(contentParent, pageData, ui);

            // Sayfa açıldığında toplu satın alma coin ikonunun doğru hizalanması için watcher ekle
            LicensePageWatcher watcher = pageObj.AddComponent<LicensePageWatcher>();
            var capturedData = pageData;
            watcher.onPageEnabled = () =>
            {
                AlignAllCoinsForPage(capturedData);
                if (ui != null && ui.gameObject.activeInHierarchy)
                {
                    ui.StartCoroutine(ui.AlignBulkCoinIconWhenReadyCoroutine(capturedData));
                }
            };

            AlignAllCoinsForPage(pageData);

            return pageData;
        }

        /// <summary>
        /// Standart Canvas ölçeğinde (1150x950) fantezi parşömen sayfası oluşturur.
        /// </summary>
        private static GameObject CreatePage(Transform parent, int pageNum, Sprite parchmentSprite)
        {
            GameObject pageObj = new GameObject($"Lisans_Sayfa_{pageNum}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pageObj.transform.SetParent(parent, false);

            RectTransform rt = pageObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(PARCHMENT_WIDTH, PARCHMENT_HEIGHT);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image img = pageObj.GetComponent<Image>();
            if (parchmentSprite != null)
            {
                img.sprite = parchmentSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.92f, 0.85f, 0.72f, 1f); // Parşömen krem arkaplan
            }
            img.raycastTarget = true; // Sayfa üzerinde swipe/drag desteği

            // İçerik Alanı (Parchment ahşap makaraları ve çerçevesi içinde güvenli alan)
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(pageObj.transform, false);

            RectTransform crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.10f, 0.08f);
            crt.anchorMax = new Vector2(0.90f, 0.92f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            crt.localScale = Vector3.one;
            crt.localRotation = Quaternion.identity;

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 6, 6);

            return pageObj;
        }

        /// <summary>
        /// Başlık satırı oluşturur (Oymalı Kelt Rünlü Ahşap Tabela Banner).
        /// </summary>
        private static void CreateHeaderRow(Transform parent, int pageNum, int totalPages)
        {
            if (_headerPrefab != null)
            {
                GameObject headerObj = Instantiate(_headerPrefab, parent, false);
                headerObj.name = "Header";
                TextMeshProUGUI tmp = headerObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = string.Format(LocalizationManager.Get("license_tier_header"), pageNum, totalPages);
                }
                return;
            }

            GameObject headerObjDynamic = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            headerObjDynamic.transform.SetParent(parent, false);

            LayoutElement le = headerObjDynamic.GetComponent<LayoutElement>();
            le.preferredHeight = 54f;
            le.minHeight = 50f;

            Image bannerImg = headerObjDynamic.GetComponent<Image>();
            if (_headerBannerSprite != null)
            {
                bannerImg.sprite = _headerBannerSprite;
                bannerImg.type = Image.Type.Simple;
                bannerImg.preserveAspect = false;
                bannerImg.color = Color.white;
            }
            else
            {
                bannerImg.color = new Color(0.18f, 0.12f, 0.06f, 0.88f);
            }
            bannerImg.raycastTarget = false;

            GameObject textObj = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(headerObjDynamic.transform, false);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(20f, 4f);
            trt.offsetMax = new Vector2(-20f, -4f);

            TextMeshProUGUI tmpDynamic = textObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) tmpDynamic.font = _fantasyFont;
            tmpDynamic.text = string.Format(LocalizationManager.Get("license_tier_header"), pageNum, totalPages);
            tmpDynamic.fontSize = 20f;
            tmpDynamic.fontStyle = FontStyles.Bold;
            tmpDynamic.color = new Color(1f, 0.90f, 0.55f, 1f); // Parlak sıcak altın
            tmpDynamic.alignment = TextAlignmentOptions.Center;
            tmpDynamic.raycastTarget = false;
        }

        private static LicenseRowData CreateLicenseRowFromPrefab(Transform parent, RecipeData recipe, LicenseParchmentUI ui)
        {
            LicenseRowData rowData = new LicenseRowData();
            rowData.recipe = recipe;

            GameObject rowObj = Instantiate(_rowPrefab, parent, false);
            rowObj.name = $"LisansSatır_{recipe.name}";

            Transform slotT = rowObj.transform.Find("SlotFrame");
            if (slotT != null)
            {
                rowData.slotFrameImage = slotT.GetComponent<Image>();
                Transform iconT = slotT.Find("PotionIcon");
                if (iconT != null)
                {
                    rowData.iconImage = iconT.GetComponent<Image>();
                    if (rowData.iconImage != null)
                    {
                        rowData.iconImage.enabled = true;
                        rowData.iconImage.color = Color.white; // Başlangıçta tam görünür; RefreshAllPages ayarlayacak
                        if (recipe.resultPotion != null && recipe.resultPotion.itemIcon != null)
                        {
                            rowData.iconImage.sprite = recipe.resultPotion.itemIcon;
                        }
                    }
                }
            }

            Transform nameT = rowObj.transform.Find("Name");
            if (nameT != null)
            {
                rowData.nameText = nameT.GetComponent<TextMeshProUGUI>();
                if (rowData.nameText != null)
                    rowData.nameText.text = recipe.resultPotion != null ? recipe.resultPotion.LocalizedName : LocalizationManager.Get(recipe.name);
            }

            int cost = LicenseManager.GetLicenseCost(recipe);
            Transform costT = rowObj.transform.Find("Cost");
            if (costT != null)
            {
                rowData.costText = costT.GetComponent<TextMeshProUGUI>();
                if (rowData.costText != null)
                    rowData.costText.text = cost.ToString();
            }

            // Coin Icon (prefab'taki altın coin görseli — satır altında veya Cost altında olabilir)
            Transform coinT = rowObj.transform.Find("Coin") ?? FindChildByPrefix(rowObj.transform, "Coin");
            if (coinT == null && costT != null)
            {
                coinT = costT.Find("Coin") ?? FindChildByPrefix(costT, "Coin");
            }
            if (coinT != null)
            {
                rowData.coinIcon = coinT.GetComponent<Image>();
                if (rowData.coinIcon != null)
                {
                    rowData.coinIcon.gameObject.SetActive(true);
                    rowData.coinIcon.preserveAspect = true;
                }
            }

            if (rowData.costText != null && rowData.coinIcon != null)
            {
                AlignRowCoinIcon(rowData.costText, rowData.coinIcon);
            }

            Transform actionT = rowObj.transform.Find("ActionArea");
            if (actionT != null)
            {
                Transform btnT = actionT.Find("BuyBtn");
                if (btnT != null)
                {
                    rowData.buyBtnObj = btnT.gameObject;
                    rowData.buyButton = btnT.GetComponent<Button>();
                    rowData.buyButtonImage = btnT.GetComponent<Image>();
                    rowData.buyButtonText = btnT.GetComponentInChildren<TextMeshProUGUI>();

                    RecipeData captured = recipe;
                    if (rowData.buyButton != null)
                    {
                        rowData.buyButton.onClick.AddListener(() =>
                        {
                            if (LicenseManager.Instance != null)
                            {
                                LicenseManager.Instance.UnlockLicense(captured);
                            }
                        });
                    }
                }

                Transform unlT = actionT.Find("UnlockedBadge");
                if (unlT != null)
                {
                    rowData.unlockedBadgeObj = unlT.gameObject;
                    rowData.unlockedBadgeObj.SetActive(false);
                    Transform sealT = unlT.Find("SealIcon");
                    if (sealT != null) rowData.unlockedSealImage = sealT.GetComponent<Image>();
                    Transform sealTxtT = unlT.Find("SealText");
                    if (sealTxtT != null) rowData.unlockedBadgeText = sealTxtT.GetComponent<TextMeshProUGUI>();
                }
            }

            return rowData;
        }

        /// <summary>
        /// Tek bir lisans satırı oluşturur (Slot Çerçevesi + İsim + Fiyat + Satın Alma Butonu / Balmumu Mühür).
        /// </summary>
        private static LicenseRowData CreateLicenseRow(Transform parent, RecipeData recipe, LicenseParchmentUI ui)
        {
            if (_rowPrefab != null)
            {
                return CreateLicenseRowFromPrefab(parent, recipe, ui);
            }

            LicenseRowData rowData = new LicenseRowData();
            rowData.recipe = recipe;

            // Ana satır container (Oymalı Maun Ahşap Plaka)
            GameObject rowObj = new GameObject($"LisansSatır_{recipe.name}",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObj.transform.SetParent(parent, false);

            LayoutElement rowLE = rowObj.GetComponent<LayoutElement>();
            rowLE.preferredHeight = 76f;
            rowLE.minHeight = 70f;

            Image rowBg = rowObj.GetComponent<Image>();
            if (_rowPlateSprite != null)
            {
                rowBg.sprite = _rowPlateSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = new Color(1f, 1f, 1f, 0.96f);
            }
            else
            {
                rowBg.color = new Color(0.14f, 0.09f, 0.05f, 0.85f);
            }
            rowBg.raycastTarget = false;

            HorizontalLayoutGroup hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(8, 8, 4, 4);

            // 1. İksir Yuvası (Slot Çerçevesi + İkon)
            GameObject slotObj = new GameObject("SlotFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObj.transform.SetParent(rowObj.transform, false);

            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(60f, 60f);

            LayoutElement slotLE = slotObj.GetComponent<LayoutElement>();
            slotLE.preferredWidth = 60f;
            slotLE.preferredHeight = 60f;

            Image slotImg = slotObj.GetComponent<Image>();
            if (_slotFrameSprite != null)
            {
                slotImg.sprite = _slotFrameSprite;
                slotImg.preserveAspect = true;
                slotImg.color = Color.white;
            }
            else
            {
                slotImg.color = new Color(0.2f, 0.15f, 0.1f, 0.8f);
            }
            slotImg.raycastTarget = false;
            rowData.slotFrameImage = slotImg;

            // İksir İkonu (Slot içine ortalanmış)
            GameObject iconObj = new GameObject("PotionIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.08f, 0.08f);
            iconRt.anchorMax = new Vector2(0.92f, 0.92f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            Image iconImg = iconObj.GetComponent<Image>();
            if (recipe.resultPotion != null && recipe.resultPotion.itemIcon != null)
            {
                iconImg.sprite = recipe.resultPotion.itemIcon;
            }
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = Color.white;
            rowData.iconImage = iconImg;

            // 2. İsim Metni
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObj.transform.SetParent(rowObj.transform, false);

            LayoutElement nameLE = nameObj.GetComponent<LayoutElement>();
            nameLE.preferredWidth = 180f;
            nameLE.flexibleWidth = 1f;

            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) nameTmp.font = _fantasyFont;
            nameTmp.text = recipe.resultPotion != null ? recipe.resultPotion.LocalizedName : LocalizationManager.Get(recipe.name);
            nameTmp.fontSize = 19f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.96f, 0.92f, 0.82f, 1f); // Sıcak fildişi
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;
            rowData.nameText = nameTmp;

            // 3. Fiyat ve Altın Coin Alanı
            int cost = LicenseManager.GetLicenseCost(recipe);
            GameObject costAreaObj = new GameObject("CostArea", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            costAreaObj.transform.SetParent(rowObj.transform, false);

            LayoutElement costAreaLE = costAreaObj.GetComponent<LayoutElement>();
            costAreaLE.preferredWidth = 85f;
            costAreaLE.minWidth = 75f;

            HorizontalLayoutGroup costHlg = costAreaObj.GetComponent<HorizontalLayoutGroup>();
            costHlg.childAlignment = TextAnchor.MiddleCenter;
            costHlg.spacing = 4f;
            costHlg.childControlWidth = false;
            costHlg.childControlHeight = false;
            costHlg.childForceExpandWidth = false;
            costHlg.childForceExpandHeight = false;

            // 3a. Fiyat Metni
            GameObject costObj = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI));
            costObj.transform.SetParent(costAreaObj.transform, false);
            RectTransform costRt = costObj.GetComponent<RectTransform>();
            costRt.sizeDelta = new Vector2(50f, 30f);

            TextMeshProUGUI costTmp = costObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) costTmp.font = _fantasyFont;
            costTmp.text = cost.ToString();
            costTmp.fontSize = 18f;
            costTmp.fontStyle = FontStyles.Bold;
            costTmp.color = new Color(1f, 0.84f, 0.32f, 1f); // Işıltılı altın sarısı
            costTmp.alignment = TextAlignmentOptions.Left;
            costTmp.raycastTarget = false;
            rowData.costText = costTmp;

            // 3b. Altın Coin Görseli (Fiyatın hemen sağında)
            GameObject coinObj = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            coinObj.transform.SetParent(costAreaObj.transform, false);
            RectTransform coinRt = coinObj.GetComponent<RectTransform>();
            coinRt.sizeDelta = new Vector2(26f, 26f);

            Image coinImg = coinObj.GetComponent<Image>();
            if (_goldCoinSprite != null)
            {
                coinImg.sprite = _goldCoinSprite;
                coinImg.preserveAspect = true;
                coinImg.color = Color.white;
            }
            else
            {
                coinImg.color = new Color(1f, 0.84f, 0.2f, 1f);
            }
            coinImg.raycastTarget = false;
            rowData.coinIcon = coinImg;

            // 4. Aksiyon Alanı Container (Satın Alma Butonu VEYA Balmumu Mühür)
            GameObject actionAreaObj = new GameObject("ActionArea", typeof(RectTransform), typeof(LayoutElement));
            actionAreaObj.transform.SetParent(rowObj.transform, false);

            LayoutElement actionLE = actionAreaObj.GetComponent<LayoutElement>();
            actionLE.preferredWidth = 150f;
            actionLE.preferredHeight = 54f;
            actionLE.minWidth = 140f;

            // 4a. Satın Alma Butonu (Ahşap Rünlü Buton)
            GameObject btnObj = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(actionAreaObj.transform, false);

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            Image btnImg = btnObj.GetComponent<Image>();
            if (_buyButtonSprite != null)
            {
                btnImg.sprite = _buyButtonSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.color = Color.white;
            }
            else
            {
                btnImg.color = new Color(0.18f, 0.58f, 0.28f, 1f);
            }
            btnImg.raycastTarget = true;
            rowData.buyButtonImage = btnImg;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.60f, 1f);
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            btn.colors = cb;
            rowData.buyButton = btn;

            // Buton metni
            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero;
            btnTextRt.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) btnTmp.font = _fantasyFont;
            btnTmp.text = LocalizationManager.Get("license_btn_buy");
            btnTmp.fontSize = 15f;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = new Color(1f, 0.95f, 0.82f, 1f);
            btnTmp.raycastTarget = false;
            rowData.buyButtonText = btnTmp;
            rowData.buyBtnObj = btnObj;

            // 4b. Açılmış Durum Rozeti (Yeşil-Altın Balmumu Mühür Damgası)
            GameObject unlockedObj = new GameObject("UnlockedBadge", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            unlockedObj.transform.SetParent(actionAreaObj.transform, false);

            RectTransform unlRt = unlockedObj.GetComponent<RectTransform>();
            unlRt.anchorMin = Vector2.zero;
            unlRt.anchorMax = Vector2.one;
            unlRt.offsetMin = Vector2.zero;
            unlRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup unlHlg = unlockedObj.GetComponent<HorizontalLayoutGroup>();
            unlHlg.childAlignment = TextAnchor.MiddleCenter;
            unlHlg.spacing = 6f;
            unlHlg.childControlWidth = false;
            unlHlg.childControlHeight = false;

            // Balmumu damga ikonu
            GameObject sealObj = new GameObject("SealIcon", typeof(RectTransform), typeof(Image));
            sealObj.transform.SetParent(unlockedObj.transform, false);
            RectTransform sealRt = sealObj.GetComponent<RectTransform>();
            sealRt.sizeDelta = new Vector2(38f, 38f);
            Image sealImg = sealObj.GetComponent<Image>();
            if (_sealStampSprite != null)
            {
                sealImg.sprite = _sealStampSprite;
                sealImg.preserveAspect = true;
                sealImg.color = Color.white;
            }
            sealImg.raycastTarget = false;
            rowData.unlockedSealImage = sealImg;

            // "LİSANSLI" yazısı
            GameObject unlTextObj = new GameObject("SealText", typeof(RectTransform), typeof(TextMeshProUGUI));
            unlTextObj.transform.SetParent(unlockedObj.transform, false);
            RectTransform unlTrt = unlTextObj.GetComponent<RectTransform>();
            unlTrt.sizeDelta = new Vector2(75f, 32f);

            TextMeshProUGUI unlTmp = unlTextObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) unlTmp.font = _fantasyFont;
            unlTmp.text = LocalizationManager.Get("license_licensed");
            unlTmp.fontSize = 15f;
            unlTmp.fontStyle = FontStyles.Bold;
            unlTmp.alignment = TextAlignmentOptions.Left;
            unlTmp.color = new Color(0.70f, 0.98f, 0.75f, 1f);
            unlTmp.raycastTarget = false;
            rowData.unlockedBadgeText = unlTmp;

            rowData.unlockedBadgeObj = unlockedObj;
            unlockedObj.SetActive(false);

            // Buton tıklama olayı
            RecipeData capturedRecipe = recipe;
            btn.onClick.AddListener(() =>
            {
                if (LicenseManager.Instance != null)
                {
                    LicenseManager.Instance.UnlockLicense(capturedRecipe);
                }
            });

            return rowData;
        }

        private static void CreateBulkBuyFromPrefab(Transform parent, LicensePageData pageData, LicenseParchmentUI ui)
        {
            GameObject bulkObj = Instantiate(_bulkBuyPrefab, parent, false);
            bulkObj.name = "BulkBuyArea";
            pageData.bulkButton = bulkObj.GetComponentInChildren<Button>();
            pageData.bulkButtonText = bulkObj.GetComponentInChildren<TextMeshProUGUI>();
            pageData.bulkButtonImage = pageData.bulkButton != null ? pageData.bulkButton.GetComponent<Image>() : bulkObj.GetComponentInChildren<Image>();

            if (pageData.bulkButton != null)
            {
                List<RecipeData> targetRecipes = pageData.pageRecipes;
                pageData.bulkButton.onClick.AddListener(() =>
                {
                    if (LicenseManager.Instance != null)
                    {
                        LicenseManager.Instance.UnlockAllForPage(targetRecipes, out int cost);
                    }
                });
            }
        }

        /// <summary>
        /// Bu sayfadaki tüm iksirleri açma butonu oluşturur (Altın Varaklı Ferman Çubuğu & Yakut Ayırıcı).
        /// </summary>
        private static void CreateBulkBuyButton(Transform parent, LicensePageData pageData, LicenseParchmentUI ui)
        {
            if (_bulkBuyPrefab != null)
            {
                CreateBulkBuyFromPrefab(parent, pageData, ui);
                return;
            }

            // Ayırıcı Altın Gem Filigree Çizgisi
            GameObject separator = new GameObject("Separator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            separator.transform.SetParent(parent, false);
            LayoutElement sepLE = separator.GetComponent<LayoutElement>();
            sepLE.preferredHeight = 22f;
            sepLE.minHeight = 20f;

            Image sepImg = separator.GetComponent<Image>();
            if (_dividerSprite != null)
            {
                sepImg.sprite = _dividerSprite;
                sepImg.preserveAspect = true;
                sepImg.color = Color.white;
            }
            else
            {
                sepImg.color = new Color(0.96f, 0.85f, 0.55f, 0.45f);
            }
            sepImg.raycastTarget = false;

            // Toplu satın alma butonu (Oymalı Ferman Tabela Çubuğu)
            GameObject bulkObj = new GameObject("BulkBuyBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            bulkObj.transform.SetParent(parent, false);

            LayoutElement bulkLE = bulkObj.GetComponent<LayoutElement>();
            bulkLE.preferredWidth = 600f;
            bulkLE.preferredHeight = 124f;
            bulkLE.minWidth = 580f;
            bulkLE.minHeight = 115f;

            Image bulkImg = bulkObj.GetComponent<Image>();
            if (_bulkBannerSprite != null)
            {
                bulkImg.sprite = _bulkBannerSprite;
                bulkImg.type = Image.Type.Sliced;
                bulkImg.pixelsPerUnitMultiplier = 1.8f;
                bulkImg.color = Color.white;
            }
            else
            {
                bulkImg.color = new Color(0.65f, 0.38f, 0.15f, 1f);
            }
            bulkImg.raycastTarget = true;
            pageData.bulkButtonImage = bulkImg;

            Button bulkBtn = bulkObj.GetComponent<Button>();
            ColorBlock cb = bulkBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.60f, 1f);
            cb.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.6f);
            bulkBtn.colors = cb;
            pageData.bulkButton = bulkBtn;

            // Buton metni (Hiyerarşide açıkça görünen Başlık ve Alt Yazı)
            GameObject bulkTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bulkTextObj.transform.SetParent(bulkObj.transform, false);
            RectTransform bulkTextRt = bulkTextObj.GetComponent<RectTransform>();
            bulkTextRt.anchorMin = new Vector2(0f, 0.42f);
            bulkTextRt.anchorMax = new Vector2(1f, 0.95f);
            bulkTextRt.offsetMin = new Vector2(20f, 0f);
            bulkTextRt.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI bulkTmp = bulkTextObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) bulkTmp.font = _fantasyFont;
            bulkTmp.text = LocalizationManager.Get("license_buy_all");
            bulkTmp.fontSize = 22f;
            bulkTmp.fontStyle = FontStyles.Bold;
            bulkTmp.alignment = TextAlignmentOptions.Center;
            bulkTmp.color = new Color(1f, 0.90f, 0.55f, 1f);
            bulkTmp.raycastTarget = false;
            pageData.bulkButtonText = bulkTmp;

            // Alt Açıklama Yazısı (SubText — hiyerarşide ayrı obje olarak düzenlenebilir; başlıkla aynı boyutta)
            GameObject subTextObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTextObj.transform.SetParent(bulkObj.transform, false);
            RectTransform subTextRt = subTextObj.GetComponent<RectTransform>();
            subTextRt.anchorMin = new Vector2(0f, 0.08f);
            subTextRt.anchorMax = new Vector2(1f, 0.45f);
            subTextRt.offsetMin = new Vector2(20f, 0f);
            subTextRt.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI subTmp = subTextObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) subTmp.font = _fantasyFont;
            subTmp.text = "";
            subTmp.fontSize = 22f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(1f, 0.94f, 0.78f, 1f);
            subTmp.raycastTarget = false;
            pageData.bulkSubText = subTmp;

            // Altın Coin Görseli (Toplu satın alma butonundaki coin)
            GameObject bulkCoinObj = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            bulkCoinObj.transform.SetParent(bulkObj.transform, false);
            RectTransform bulkCoinRt = bulkCoinObj.GetComponent<RectTransform>();
            bulkCoinRt.anchorMin = new Vector2(0.5f, 0.26f);
            bulkCoinRt.anchorMax = new Vector2(0.5f, 0.26f);
            bulkCoinRt.pivot = new Vector2(0.5f, 0.5f);
            bulkCoinRt.sizeDelta = new Vector2(26f, 26f);
            bulkCoinRt.anchoredPosition = new Vector2(-60f, 0f);

            Image bulkCoinImg = bulkCoinObj.GetComponent<Image>();
            if (_goldCoinSprite != null)
            {
                bulkCoinImg.sprite = _goldCoinSprite;
                bulkCoinImg.preserveAspect = true;
                bulkCoinImg.color = Color.white;
            }
            else
            {
                bulkCoinImg.color = new Color(1f, 0.84f, 0.2f, 1f);
            }
            bulkCoinImg.raycastTarget = false;
            pageData.bulkCoinIcon = bulkCoinImg;

            // Toplu satın alma tıklama
            List<RecipeData> targetRecipes = pageData.pageRecipes;
            bulkBtn.onClick.AddListener(() =>
            {
                if (LicenseManager.Instance != null)
                {
                    LicenseManager.Instance.UnlockAllForPage(targetRecipes, out int cost);
                }
            });
        }

        // ============================================================
        // UI GÜNCELLEME
        // ============================================================

        /// <summary>
        /// Tüm sayfaların lisans durumlarını günceller.
        /// </summary>
        private void RefreshAllPages()
        {
            foreach (var pageData in _pageDataList)
            {
                // Sayfa başlığını yerelleştir
                Transform headerT = pageData.pageObj != null ? (pageData.pageObj.transform.Find("Content/Header/HeaderText") ?? pageData.pageObj.transform.Find("Content/Header")) : null;
                if (headerT != null)
                {
                    var headerTmp = headerT.GetComponent<TextMeshProUGUI>() ?? headerT.GetComponentInChildren<TextMeshProUGUI>();
                    if (headerTmp != null)
                    {
                        headerTmp.text = string.Format(LocalizationManager.Get("license_tier_header"), pageData.pageIndex, _pageDataList.Count);
                    }
                }

                // Satırları güncelle
                foreach (var rowData in pageData.rows)
                {
                    bool hasLicense = LicenseManager.HasLicense(rowData.recipe);

                    if (rowData.nameText != null)
                    {
                        rowData.nameText.text = rowData.recipe != null && rowData.recipe.resultPotion != null
                            ? rowData.recipe.resultPotion.LocalizedName
                            : (rowData.recipe != null ? LocalizationManager.Get(rowData.recipe.name) : "");
                    }

                    if (rowData.buyButtonText != null)
                    {
                        rowData.buyButtonText.text = LocalizationManager.Get("license_btn_buy");
                    }

                    if (rowData.unlockedBadgeText != null)
                    {
                        rowData.unlockedBadgeText.text = LocalizationManager.Get("license_licensed");
                    }

                    if (hasLicense)
                    {
                        // Lisanslı: Butonu gizle, kraliyet balmumu mühür damgasını göster
                        if (rowData.buyBtnObj != null)
                            rowData.buyBtnObj.SetActive(false);

                        if (rowData.unlockedBadgeObj != null)
                            rowData.unlockedBadgeObj.SetActive(true);

                        if (rowData.costText != null)
                        {
                            rowData.costText.text = LocalizationManager.Get("license_unlocked");
                            rowData.costText.color = new Color(0.60f, 0.88f, 0.65f, 0.85f);
                        }

                        // Lisanslıysa coin görseli gizlenir
                        if (rowData.coinIcon != null)
                            rowData.coinIcon.gameObject.SetActive(false);

                        if (rowData.nameText != null)
                            rowData.nameText.color = new Color(1f, 0.98f, 0.92f, 1f);

                        if (rowData.iconImage != null)
                            rowData.iconImage.color = Color.white;
                    }
                    else
                    {
                        // Lisanssız: Satın alma butonu aktif, mühür gizli
                        int cost = LicenseManager.GetLicenseCost(rowData.recipe);

                        if (rowData.buyBtnObj != null)
                            rowData.buyBtnObj.SetActive(true);

                        if (rowData.unlockedBadgeObj != null)
                            rowData.unlockedBadgeObj.SetActive(false);

                        if (rowData.buyButton != null)
                            rowData.buyButton.interactable = true;

                        if (rowData.costText != null)
                        {
                            rowData.costText.text = cost.ToString();
                            rowData.costText.color = new Color(1f, 0.84f, 0.32f, 1f);
                        }

                        // Lisanssızsa coin görseli gösterilir ve fiyatın sağına hizalanır
                        if (rowData.coinIcon != null)
                        {
                            rowData.coinIcon.gameObject.SetActive(true);
                            if (rowData.costText != null)
                            {
                                AlignRowCoinIcon(rowData.costText, rowData.coinIcon);
                            }
                        }

                        if (rowData.nameText != null)
                            rowData.nameText.color = new Color(0.96f, 0.92f, 0.82f, 1f);

                        if (rowData.iconImage != null)
                        {
                            float alpha = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.lockedItemAlpha : 0.6f;
                            // Minimum 0.7 alpha floor: ikon her zaman belirgin olsun
                            alpha = Mathf.Max(alpha, 0.7f);
                            rowData.iconImage.color = new Color(0.85f, 0.85f, 0.85f, alpha);
                        }
                    }
                }

                // Bu sayfadaki toplu satın alma butonunu güncelle
                int unlicensedCount = LicenseManager.GetUnlicensedCountForPage(pageData.pageRecipes);
                int bulkCost = LicenseManager.GetBulkCostForPage(pageData.pageRecipes);

                if (pageData.bulkButton != null)
                {
                    pageData.bulkButton.interactable = unlicensedCount > 0;
                }

                if (pageData.bulkButtonImage != null)
                {
                    pageData.bulkButtonImage.color = unlicensedCount > 0
                        ? Color.white
                        : new Color(0.70f, 0.70f, 0.70f, 0.65f);
                }

                // Toplu satın al coin görseli
                if (pageData.bulkCoinIcon != null)
                {
                    pageData.bulkCoinIcon.gameObject.SetActive(unlicensedCount > 0);
                }

                if (pageData.bulkButtonText != null)
                {
                    if (unlicensedCount > 0)
                    {
                        if (pageData.bulkSubText != null)
                        {
                            // Ayrı SubText varsa ana metin sadece başlık olsun
                            pageData.bulkButtonText.text = LocalizationManager.Get("license_buy_all");
                            pageData.bulkButtonText.color = new Color(1f, 0.90f, 0.55f, 1f);
                        }
                        else
                        {
                            // Tek metin objesi varsa zengin formatla birlikte göster (eşit boyut)
                            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
                            int discountPct = Mathf.RoundToInt(discount * 100f);
                            string discountStr = string.Format(LocalizationManager.Get("license_discount"), discountPct);
                            pageData.bulkButtonText.text = $"{LocalizationManager.Get("license_buy_all")}\n<color=#FFF0C8>{bulkCost}   {discountStr}</color>";
                            pageData.bulkButtonText.color = new Color(1f, 0.90f, 0.55f, 1f);
                        }
                    }
                    else
                    {
                        if (pageData.bulkSubText != null)
                        {
                            pageData.bulkButtonText.text = LocalizationManager.Get("license_all_completed");
                            pageData.bulkButtonText.color = new Color(0.75f, 0.95f, 0.80f, 0.92f);
                        }
                        else
                        {
                            pageData.bulkButtonText.text = $"{LocalizationManager.Get("license_all_completed")}\n<color=#C0FFD0>{LocalizationManager.Get("license_all_licensed")}</color>";
                            pageData.bulkButtonText.color = new Color(0.75f, 0.95f, 0.80f, 0.92f);
                        }
                    }
                }

                // Alt açıklama yazısı (ayrı obje — hiyerarşiden düzenlenebilir)
                if (pageData.bulkSubText != null)
                {
                    // İki yazının font boyutu ve stilini birebir aynı yap
                    if (pageData.bulkButtonText != null)
                    {
                        pageData.bulkSubText.fontSize = pageData.bulkButtonText.fontSize;
                        pageData.bulkSubText.fontStyle = pageData.bulkButtonText.fontStyle;
                    }

                    if (unlicensedCount > 0)
                    {
                        float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
                        int discountPct = Mathf.RoundToInt(discount * 100f);
                        string costStr = bulkCost.ToString();
                        string discountStr = string.Format(LocalizationManager.Get("license_discount"), discountPct);
                        // Sadece Altın miktarı, coin sembolü boşluğu ve indirim bilgisi
                        pageData.bulkSubText.text = $"{costStr}       {discountStr}";
                        pageData.bulkSubText.color = new Color(1f, 0.94f, 0.78f, 1f);
                        pageData.bulkSubText.gameObject.SetActive(true);

                        // Coin sembolünü fiyat ile indirim arasına tam ortala
                        if (pageData.bulkCoinIcon != null)
                        {
                            pageData.bulkCoinIcon.gameObject.SetActive(true);
                            AlignBulkCoinIcon(pageData.bulkSubText, pageData.bulkCoinIcon, costStr.Length);
                        }
                    }
                    else
                    {
                        pageData.bulkSubText.text = LocalizationManager.Get("license_all_licensed");
                        pageData.bulkSubText.color = new Color(0.75f, 0.95f, 0.80f, 0.92f);
                        pageData.bulkSubText.gameObject.SetActive(true);

                        if (pageData.bulkCoinIcon != null)
                        {
                            pageData.bulkCoinIcon.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnPageChangedHandler(int newIndex)
        {
            if (newIndex >= 0 && newIndex < _pageDataList.Count)
            {
                var pageData = _pageDataList[newIndex];
                AlignAllCoinsForPage(pageData);
                StartCoroutine(AlignBulkCoinIconWhenReadyCoroutine(pageData));
            }
        }

        /// <summary>
        /// Bir sayfadaki tüm satır ve toplu satın alma coin ikonlarını beklemeden anında doğru yerlerine hizalar.
        /// Sayfa inaktif olsa bile hesaplama yaparak geçiş animasyonunda asla hatalı konumda görünmemesini sağlar.
        /// </summary>
        private static void AlignAllCoinsForPage(LicensePageData pageData)
        {
            if (pageData == null) return;

            // 1. Satırlardaki coin'leri hizala
            if (pageData.rows != null)
            {
                foreach (var row in pageData.rows)
                {
                    if (row != null && row.coinIcon != null && row.costText != null)
                    {
                        AlignRowCoinIcon(row.costText, row.coinIcon);
                    }
                }
            }

            // 2. Toplu satın alma butonundaki coin'i hizala
            if (pageData.bulkSubText != null && pageData.bulkCoinIcon != null)
            {
                int unlicensedCount = LicenseManager.GetUnlicensedCountForPage(pageData.pageRecipes);
                if (unlicensedCount > 0)
                {
                    int bulkCost = LicenseManager.GetBulkCostForPage(pageData.pageRecipes);
                    string costStr = bulkCost.ToString();
                    AlignBulkCoinIcon(pageData.bulkSubText, pageData.bulkCoinIcon, costStr.Length);
                }
            }
        }

        private System.Collections.IEnumerator AlignBulkCoinIconWhenReadyCoroutine(LicensePageData pageData)
        {
            if (pageData == null) yield break;

            // 1. ADIM: Anında hizala (animasyon başladığı andan itibaren tüm coin'ler olmaları gereken yerde olsun)
            AlignAllCoinsForPage(pageData);

            // 2. ADIM: 1 frame bekle ve TMP mesh güncellemeleri oturduğunda tekrar hizala
            yield return null;
            AlignAllCoinsForPage(pageData);

            // Eğer sayfa animasyonu varsa, animasyon bittiğinde son kontrolü yap (asla sıçrama olmadan)
            if (_controller != null && _controller.IsTransitioning)
            {
                while (_controller != null && _controller.IsTransitioning)
                {
                    yield return null;
                }
                AlignAllCoinsForPage(pageData);
            }
        }

        /// <summary>
        /// Satırdaki altın coin görselini, fiyat metninin hemen sağına dinamik olarak hizalar.
        /// Her zaman miktarın sağında kalmasını garanti eder.
        /// Obje inaktif olsa bile (animasyon öncesi) anında doğru konuma yerleştirir.
        /// </summary>
        public static void AlignRowCoinIcon(TextMeshProUGUI costText, Image coinIcon)
        {
            if (costText == null || coinIcon == null) return;

            // Eğer ebeveynde HorizontalLayoutGroup varsa hizalamayı layout group otomatik yönetir
            if (coinIcon.transform.parent != null && coinIcon.transform.parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                return;
            }

            float rightX = 0f;
            float centerY = 0f;
            bool calculated = false;

            if (costText.gameObject.activeInHierarchy)
            {
                costText.ForceMeshUpdate();
                TMP_TextInfo textInfo = costText.textInfo;
                if (textInfo != null && textInfo.characterCount > 0)
                {
                    // Son karakteri bul (sondaki boşlukları atla)
                    int lastCharIdx = textInfo.characterCount - 1;
                    while (lastCharIdx >= 0 && char.IsWhiteSpace(textInfo.characterInfo[lastCharIdx].character))
                    {
                        lastCharIdx--;
                    }
                    if (lastCharIdx >= 0)
                    {
                        var lastChar = textInfo.characterInfo[lastCharIdx];
                        rightX = lastChar.topRight.x;
                        centerY = (lastChar.topRight.y + lastChar.bottomRight.y) * 0.5f;
                        calculated = true;
                    }
                }
            }

            if (!calculated)
            {
                // Obje inaktifken veya mesh henüz oluşmamışken GetPreferredValues ile tam sağ kenarı bul
                float textWidth = costText.GetPreferredValues(costText.text).x;
                rightX = textWidth * 0.5f; // Center aligned olduğu için
                centerY = 0f;
            }

            if (coinIcon.transform.parent == costText.transform)
            {
                // Coin, Cost objesinin doğrudan çocuğu
                float visualHalfWidth = (coinIcon.rectTransform.rect.width * Mathf.Abs(coinIcon.rectTransform.localScale.x)) * 0.5f;
                if (visualHalfWidth <= 0.01f) visualHalfWidth = 14f;

                float gap = 8f;
                float targetX = rightX + visualHalfWidth + gap;

                Vector2 currentPos = coinIcon.rectTransform.anchoredPosition;
                coinIcon.rectTransform.anchoredPosition = new Vector2(targetX, currentPos.y != 0 ? currentPos.y : centerY);
            }
            else
            {
                // Farklı hiyerarşide ise
                if (costText.gameObject.activeInHierarchy)
                {
                    Vector3 worldRightEdge = costText.transform.TransformPoint(new Vector3(rightX, centerY, 0f));
                    float iconWidth = Mathf.Max(24f, coinIcon.rectTransform.rect.width);
                    Vector3 targetWorld = worldRightEdge + costText.transform.right * (iconWidth * 0.5f + 8f);
                    coinIcon.transform.position = targetWorld;
                    Vector3 lp = coinIcon.transform.localPosition;
                    coinIcon.transform.localPosition = new Vector3(lp.x, lp.y, 0f);
                }
            }
        }

        /// <summary>
        /// Toplu satın al butonundaki coin ikonunu, altın miktarı ile indirim metni arasındaki boşluğa tam ortalar.
        /// Hem aktif hem inaktif durumda (animasyon öncesi) anında doğru konuma yerleştirir.
        /// </summary>
        private static void AlignBulkCoinIcon(TextMeshProUGUI subText, Image coinIcon, int costLength)
        {
            if (subText == null || coinIcon == null) return;

            // Boyutunu ve ölçeğini subText ile orantılı yap
            coinIcon.transform.localScale = subText.transform.localScale;
            float iconSize = Mathf.Max(24f, subText.fontSize * 1.15f);
            coinIcon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            coinIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (subText.gameObject.activeInHierarchy)
            {
                subText.ForceMeshUpdate();
                TMP_TextInfo textInfo = subText.textInfo;

                if (textInfo != null && textInfo.characterCount >= costLength && costLength > 0)
                {
                    int lastCostCharIdx = costLength - 1;
                    int openParenIdx = subText.text.IndexOf('(');

                    if (openParenIdx > lastCostCharIdx && openParenIdx < textInfo.characterCount)
                    {
                        var lastCostInfo = textInfo.characterInfo[lastCostCharIdx];
                        var openParenInfo = textInfo.characterInfo[openParenIdx];

                        float costRightX = lastCostInfo.topRight.x;
                        float parenLeftX = openParenInfo.topLeft.x;

                        if (parenLeftX > costRightX)
                        {
                            float midX = (costRightX + parenLeftX) * 0.5f;
                            float midY = (lastCostInfo.topRight.y + lastCostInfo.bottomRight.y) * 0.5f;

                            Vector3 worldPos = subText.transform.TransformPoint(new Vector3(midX, midY, 0f));
                            coinIcon.transform.position = worldPos;
                            Vector3 lp = coinIcon.transform.localPosition;
                            coinIcon.transform.localPosition = new Vector3(lp.x, lp.y, 0f);
                            return;
                        }
                    }
                }
            }

            // Fallback (Obje inaktifken veya mesh henüz oluşmamışken anında doğru konumu hesapla)
            string fullText = subText.text;
            if (string.IsNullOrEmpty(fullText) || costLength <= 0) return;

            int parenIdx = fullText.IndexOf('(');
            string costPart = costLength <= fullText.Length ? fullText.Substring(0, costLength) : fullText;
            float totalW = subText.GetPreferredValues(fullText).x;
            float costW = subText.GetPreferredValues(costPart).x;
            float leftX = -totalW * 0.5f;
            float cRightX = leftX + costW;
            float pLeftX = parenIdx > 0 ? (leftX + subText.GetPreferredValues(fullText.Substring(0, parenIdx)).x) : (cRightX + 40f);
            float mX = (cRightX + pLeftX) * 0.5f;
            float targetXInBulk = mX * subText.transform.localScale.x;
            coinIcon.rectTransform.anchoredPosition = new Vector2(targetXInBulk, 64f);
        }

        // ============================================================
        // YARDIMCI METODLAR & PREFAB ARAMA
        // ============================================================

        private static TextMeshProUGUI CreateTextElement(Transform parent, string text, float fontSize,
            Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal, float height = 30)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObj.transform.SetParent(parent, false);

            LayoutElement le = textObj.GetComponent<LayoutElement>();
            le.preferredHeight = height;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (_fantasyFont != null) tmp.font = _fantasyFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;

            return tmp;
        }

        /// <summary>
        /// Parşömen arkaplan görselini yükler (UI taslak_r1_c1_r1_c7 veya Parşomen_0).
        /// </summary>
        private static Sprite FindParchmentSprite()
        {
            // 1. Sayfa prefabından al
            GameObject pagePrefab = FindAnyPagePrefab();
            if (pagePrefab != null)
            {
                Image img = pagePrefab.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;

                Image[] images = pagePrefab.GetComponentsInChildren<Image>(true);
                foreach (var i in images)
                {
                    if (i.sprite != null && (i.sprite.name.Contains("taslak") || i.sprite.name.Contains("Sayfa") || i.sprite.name.Contains("Parşomen")))
                        return i.sprite;
                }
            }

            // 2. Ana Parşömen prefabından al
            GameObject mainParchment = FindMainParchmentPrefab();
            if (mainParchment != null)
            {
                Image[] images = mainParchment.GetComponentsInChildren<Image>(true);
                foreach (var i in images)
                {
                    if (i.sprite != null && (i.sprite.name.Contains("taslak") || i.sprite.name.Contains("Sayfa") || i.sprite.name.Contains("Parşomen")))
                        return i.sprite;
                }
            }

#if UNITY_EDITOR
            // 3. AssetDatabase üzerinden doğrudan al
            string texturePath = "Assets/_PotionTown/Art/UI/Sliced/UI taslak_r1_c1_r1_c7.png";
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(texturePath);
            foreach (var a in assets)
            {
                if (a is Sprite s)
                    return s;
            }

            string altPath = "Assets/_PotionTown/Art/UI/Parşomen.png";
            Object[] altAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(altPath);
            foreach (var a in altAssets)
            {
                if (a is Sprite s)
                    return s;
            }
#endif
            return null;
        }

        private static GameObject FindMainParchmentPrefab()
        {
            if (_mainParchmentPrefab != null) return _mainParchmentPrefab;

            // 1. Önce Resources içindeki Lisans Parşömeni prefabını dene (Build ve Runtime için garantili)
            GameObject licRes = Resources.Load<GameObject>("LicenseUI/LisansParsomeni");
            if (licRes != null) return licRes;

#if UNITY_EDITOR
            // 2. Editor'de Assets içinden yükle
            GameObject licParchment = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/License/LisansParşomeni.prefab");
            if (licParchment != null) return licParchment;
#endif

            // 3. Lisans butonunun (LicenseClickArea) üzerindeki prefab referansı
            LicenseClickArea licArea = Object.FindFirstObjectByType<LicenseClickArea>(FindObjectsInactive.Include);
            if (licArea != null && licArea.customParchmentPrefab != null)
                return licArea.customParchmentPrefab;

            return null;
        }

        private static GameObject FindAnyPagePrefab()
        {
            if (_pagePrefab != null) return _pagePrefab;

            // 1. Önce Resources içindeki Lisans Sayfası prefabını dene (Build ve Runtime için garantili)
            GameObject resPrefab = Resources.Load<GameObject>("LicenseUI/License_Prefab");
            if (resPrefab != null) return resPrefab;

#if UNITY_EDITOR
            // 2. Editor'de Assets içinden yükle
            GameObject lp = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/License/License_Prefab.prefab");
            if (lp != null) return lp;
#endif

            // 3. Ana parşömenin içindeki lisans sayfalarını ara (asla Sayfa 1 tarif sayfasına düşme)
            GameObject mainParchment = FindMainParchmentPrefab();
            if (mainParchment != null)
            {
                ParchmentPageController ctrl = mainParchment.GetComponent<ParchmentPageController>();
                if (ctrl != null && ctrl.pagePrefabs != null && ctrl.pagePrefabs.Count > 0)
                {
                    foreach (var item in ctrl.pagePrefabs)
                    {
                        if (item != null && (item.name.Contains("License") || item.name.Contains("Lisans")))
                            return item;
                    }
                }
            }

            return null;
        }

        // ============================================================
        // OVERLAY VE BUTON BAĞLANTILARI
        // ============================================================

        private void CreateDarkOverlay(GameObject popup)
        {
            _darkOverlay = new GameObject("ParchmentDarkBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            _darkOverlay.transform.SetParent(popup.transform, false);
            _darkOverlay.transform.SetAsFirstSibling();

            RectTransform overlayRect = _darkOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = new Vector2(-5000f, -5000f);
            overlayRect.offsetMax = new Vector2(5000f, 5000f);

            Image overlayImg = _darkOverlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.72f);
            overlayImg.raycastTarget = true;

            Button overlayBtn = _darkOverlay.GetComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(Close);
        }

        private void CreateCloseButton(GameObject popup)
        {
            Transform existing = popup.transform.Find("ParchmentCloseBtn");
            if (existing != null)
            {
                Button existingBtn = existing.GetComponent<Button>();
                if (existingBtn != null)
                {
                    existingBtn.onClick.RemoveAllListeners();
                    existingBtn.onClick.AddListener(Close);
                }
                return;
            }

            if (_closeButtonPrefab != null)
            {
                GameObject closeBtnObj = Instantiate(_closeButtonPrefab, popup.transform, false);
                closeBtnObj.name = "ParchmentCloseBtn";
                RectTransform crt = closeBtnObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(480f, 385f);

                Button btn = closeBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(Close);
                }
                return;
            }

            GameObject closeBtnObjDynamic = new GameObject("ParchmentCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObjDynamic.transform.SetParent(popup.transform, false);

            RectTransform rt = closeBtnObjDynamic.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Parşömenin sağ üst köşesi (genişlik 1150, yükseklik 950)
            rt.anchoredPosition = new Vector2(480f, 385f);
            rt.sizeDelta = new Vector2(48f, 48f);

            Image img = closeBtnObjDynamic.GetComponent<Image>();
            if (_closeSealSprite != null)
            {
                img.sprite = _closeSealSprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.65f, 0.18f, 0.18f, 0.92f);
            }
            img.raycastTarget = true;

            Button dynamicBtn = closeBtnObjDynamic.GetComponent<Button>();
            ColorBlock cb = dynamicBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.8f, 1f);
            dynamicBtn.colors = cb;
            dynamicBtn.onClick.AddListener(Close);

            // Eğer özel mühür yoksa metin olarak "✕" ekle, mühür varsa altın 'X' zaten üzerinde kabartmalıdır
            if (_closeSealSprite == null)
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(closeBtnObjDynamic.transform, false);
                RectTransform trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "✕";
                tmp.fontSize = 24f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }

            closeBtnObjDynamic.transform.SetAsLastSibling();
        }

        private void BindCloseButtons(GameObject popup)
        {
            Button[] allButtons = popup.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string n = btn.gameObject.name.ToLower();
                if (n.Contains("close") || n.Contains("kapat") || n == "x" || n.Contains("x_btn") || n.Contains("back") || n.Contains("geri"))
                {
                    btn.onClick.RemoveListener(Close);
                    btn.onClick.AddListener(Close);
                }
            }
        }
    }

    /// <summary>
    /// Sayfa GameObject'inin OnEnable olayını dinleyerek sayfa açıldığında UI güncellemelerini tetikler.
    /// </summary>
    public class LicensePageWatcher : MonoBehaviour
    {
        public System.Action onPageEnabled;
        private void OnEnable()
        {
            onPageEnabled?.Invoke();
        }
    }
}
