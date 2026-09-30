using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Modern kart ve sekme yapısına sahip, fantezi temalı ve New Rocker yazı tipiyle uyumlu
    /// RevenueCat entegrasyonlu premium mağaza arayüzü (ShopUI).
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        private static ShopUI _instance;
        public static ShopUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ShopUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ShopUI]");
                        _instance = go.AddComponent<ShopUI>();
                    }
                }
                return _instance;
            }
        }

        public static bool IsOpen => _instance != null && _instance._overlay != null && _instance._overlay.activeSelf;

        public static void Open()
        {
            Instance.Show();
        }

        public static void Close()
        {
            if (_instance != null)
            {
                _instance.Hide();
            }
        }

        public enum ShopCategory
        {
            KadimPara,
            Bundles,
            PotionPass,
            NoAds
        }

        [Header("UI Referansları (Inspector Bağlantıları)")]
        [Tooltip("Karartma ve arka plan modal paneli")]
        public GameObject overlay;

        [Tooltip("Mağazanın ana penceresi")]
        public RectTransform mainPanel;

        [Tooltip("Kapatma 'X' butonu")]
        public Button closeButton;

        [Tooltip("Mağaza başlık metni")]
        public TextMeshProUGUI titleText;

        [Tooltip("Mevcut altın göstergesi")]
        public TextMeshProUGUI goldValueText;

        [Tooltip("Mevcut kadim para göstergesi")]
        public TextMeshProUGUI kadimValueText;

        [Tooltip("Sekmeler alanı")]
        public Transform tabsContainer;

        [Tooltip("Kadim Para sekme butonu")]
        public Button tabKadimButton;

        [Tooltip("Özel Teklifler sekme butonu")]
        public Button tabBundlesButton;

        [Tooltip("İksir Kartı sekme butonu")]
        public Button tabPotionPassButton;

        [Tooltip("Reklamsız sekme butonu")]
        public Button tabNoAdsButton;

        [Tooltip("Ürün kartlarının dizildiği grid / container")]
        public Transform cardsContainer;

        [Tooltip("Ürün kartı prefab şablonu (ShopProductCard)")]
        public GameObject productCardPrefab;

        [Header("Aktif Kategori")]
        private ShopCategory _activeCategory = ShopCategory.KadimPara;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GameObject _overlay;
        private GameObject _mainPanel;
        private Transform _tabsContainer;
        private Transform _cardsContainer;
        private TextMeshProUGUI _goldValueText;
        private TextMeshProUGUI _kadimValueText;

        private List<Button> _tabButtons = new List<Button>();
        private List<GameObject> _activeCardObjects = new List<GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUI();
            GameObject targetOverlay = overlay != null ? overlay : _overlay;
            if (targetOverlay != null)
            {
                targetOverlay.SetActive(false); // Başlangıçta arayüz gizli
            }
        }

        private void OnEnable()
        {
            GameManager.OnGoldChangedStatic += UpdateCurrencyDisplay;
            GameManager.OnKadimParaChangedStatic += UpdateCurrencyDisplay;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            if (RevenueCatManager.Instance != null)
            {
                RevenueCatManager.Instance.OnPurchaseSuccess += HandlePurchaseSuccess;
                RevenueCatManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
                RevenueCatManager.Instance.OnOfferingsLoaded += RefreshCurrentCategory;
            }

            UpdateCurrencyDisplay(GameManager.SharedGold);
            RefreshCurrentCategory();
        }

        private void OnDisable()
        {
            GameManager.OnGoldChangedStatic -= UpdateCurrencyDisplay;
            GameManager.OnKadimParaChangedStatic -= UpdateCurrencyDisplay;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (RevenueCatManager.HasInstance)
            {
                RevenueCatManager.Instance.OnPurchaseSuccess -= HandlePurchaseSuccess;
                RevenueCatManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
                RevenueCatManager.Instance.OnOfferingsLoaded -= RefreshCurrentCategory;
            }
        }

        private void OnDestroy()
        {
            GameManager.OnGoldChangedStatic -= UpdateCurrencyDisplay;
            GameManager.OnKadimParaChangedStatic -= UpdateCurrencyDisplay;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (RevenueCatManager.HasInstance)
            {
                RevenueCatManager.Instance.OnPurchaseSuccess -= HandlePurchaseSuccess;
                RevenueCatManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
                RevenueCatManager.Instance.OnOfferingsLoaded -= RefreshCurrentCategory;
            }
        }

        public void HookTabButtons()
        {
            _tabButtons.Clear();
            if (tabKadimButton != null)
            {
                _tabButtons.Add(tabKadimButton);
                tabKadimButton.onClick.RemoveAllListeners();
                tabKadimButton.onClick.AddListener(() => SelectCategory(ShopCategory.KadimPara));
            }
            if (tabBundlesButton != null)
            {
                _tabButtons.Add(tabBundlesButton);
                tabBundlesButton.onClick.RemoveAllListeners();
                tabBundlesButton.onClick.AddListener(() => SelectCategory(ShopCategory.Bundles));
            }
            if (tabPotionPassButton != null)
            {
                _tabButtons.Add(tabPotionPassButton);
                tabPotionPassButton.onClick.RemoveAllListeners();
                tabPotionPassButton.onClick.AddListener(() => SelectCategory(ShopCategory.PotionPass));
            }
            if (tabNoAdsButton != null)
            {
                _tabButtons.Add(tabNoAdsButton);
                tabNoAdsButton.onClick.RemoveAllListeners();
                tabNoAdsButton.onClick.AddListener(() => SelectCategory(ShopCategory.NoAds));
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
        }

        public static void Open(ShopCategory category)
        {
            Instance.Show(category);
        }

        public void Show()
        {
            Show(ShopCategory.KadimPara);
        }

        public void Show(ShopCategory category)
        {
            GameObject targetOverlay = overlay != null ? overlay : _overlay;
            if (targetOverlay != null)
            {
                targetOverlay.SetActive(true);
            }
            UpdateCurrencyDisplay(GameManager.SharedGold);
            SelectCategory(category);
        }

        public void Hide()
        {
            GameObject targetOverlay = overlay != null ? overlay : _overlay;
            if (targetOverlay != null)
            {
                targetOverlay.SetActive(false);
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (titleText != null) titleText.text = LocalizationManager.Get("shop_title");
            if (tabKadimButton != null)
            {
                var txt = tabKadimButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.Get("shop_tab_kadim");
            }
            if (tabBundlesButton != null)
            {
                var txt = tabBundlesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.Get("shop_tab_bundles");
            }
            if (tabPotionPassButton != null)
            {
                var txt = tabPotionPassButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.Get("shop_tab_pass");
            }
            if (tabNoAdsButton != null)
            {
                var txt = tabNoAdsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.Get("shop_tab_noads");
            }

            foreach (var btn in _tabButtons)
            {
                if (btn == null) continue;
                var loc = btn.GetComponentInChildren<LocalizedText>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (loc != null && txt != null && !string.IsNullOrEmpty(loc.localizationKey))
                {
                    txt.text = LocalizationManager.Get(loc.localizationKey);
                }
            }

            RefreshCurrentCategory();
        }

        private void HandlePurchaseSuccess(string productId)
        {
            ToastNotificationUI.ShowSuccess("shop_purchased");
            UpdateCurrencyDisplay(GameManager.SharedGold);
            RefreshCurrentCategory();
        }

        private void HandlePurchaseFailed(string productId, string error)
        {
            ToastNotificationUI.ShowError(error ?? "Satın alma başarısız oldu.");
        }

        private void UpdateCurrencyDisplay(int _)
        {
            if (_goldValueText != null) _goldValueText.text = GameManager.SharedGold.ToString("N0");
            if (_kadimValueText != null) _kadimValueText.text = GameManager.SharedKadimPara.ToString("N0");
        }

        private void SelectCategory(ShopCategory category)
        {
            _activeCategory = category;
            UpdateTabStyles();
            RefreshCurrentCategory();
        }

        private void UpdateTabStyles()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool isSelected = (int)_activeCategory == i;
                var btn = _tabButtons[i];
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();

                if (img != null)
                {
                    img.color = isSelected ? UIThemeHelper.ColorGold : new Color(0.25f, 0.16f, 0.10f, 0.85f);
                }
                if (txt != null)
                {
                    txt.color = isSelected ? Color.black : UIThemeHelper.ColorTextWarm;
                    txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private void RefreshCurrentCategory()
        {
            // Önceki kartları temizle
            foreach (var card in _activeCardObjects)
            {
                if (card != null) Destroy(card);
            }
            _activeCardObjects.Clear();

            switch (_activeCategory)
            {
                case ShopCategory.KadimPara:
                    CreateKadimParaCards();
                    break;
                case ShopCategory.Bundles:
                    CreateBundleCards();
                    break;
                case ShopCategory.PotionPass:
                    CreatePotionPassCards();
                    break;
                case ShopCategory.NoAds:
                    CreateNoAdsCards();
                    break;
            }
        }

        private void CreateKadimParaCards()
        {
            Sprite coinSprite = UIThemeHelper.GetAncientCoinSprite();

            // Küçük Kese (100)
            CreateProductCard(
                "shop_kadim_small_title",
                "shop_kadim_small_desc",
                coinSprite,
                RevenueCatManager.PRODUCT_KADIM_PARA_SMALL,
                "₺34.99",
                null
            );

            // Gümüş Sandık (500)
            CreateProductCard(
                "shop_kadim_med_title",
                "shop_kadim_med_desc",
                coinSprite,
                RevenueCatManager.PRODUCT_KADIM_PARA_MEDIUM,
                "₺149.99",
                "shop_best_deal"
            );

            // Kraliyet Hazinesi (1200)
            CreateProductCard(
                "shop_kadim_large_title",
                "shop_kadim_large_desc",
                coinSprite,
                RevenueCatManager.PRODUCT_KADIM_PARA_LARGE,
                "₺329.99",
                "shop_extra_bonus"
            );
        }

        private void CreateBundleCards()
        {
            Sprite goldSprite = UIThemeHelper.GetGoldCoinSprite();
            Sprite ancientSprite = UIThemeHelper.GetAncientCoinSprite();

            CreateProductCard(
                "shop_bundle_apprentice",
                "shop_bundle_apprentice_desc",
                ancientSprite,
                RevenueCatManager.PRODUCT_KADIM_PARA_MEDIUM,
                "₺89.99",
                "shop_starter_deal"
            );

            CreateProductCard(
                "shop_bundle_master",
                "shop_bundle_master_desc",
                goldSprite,
                RevenueCatManager.PRODUCT_KADIM_PARA_LARGE,
                "₺249.99",
                "shop_extra_40"
            );
        }

        private void CreatePotionPassCards()
        {
            bool owned = RevenueCatManager.Instance != null && RevenueCatManager.Instance.IsPotionPassPremiumOwned;
            Sprite passSprite = UIThemeHelper.GetParchmentSprite();

            CreateProductCard(
                "shop_pass_title",
                "shop_pass_desc",
                passSprite,
                RevenueCatManager.PRODUCT_POTION_PASS_PREMIUM,
                owned ? LocalizationManager.Get("shop_owned") : "₺119.99",
                "shop_season_1",
                isPurchased: owned
            );
        }

        private void CreateNoAdsCards()
        {
            bool owned = RevenueCatManager.Instance != null && RevenueCatManager.Instance.IsAdsRemoved;
            Sprite icon = UIThemeHelper.GetWarningIcon();

            CreateProductCard(
                "shop_noads_title",
                "shop_noads_desc",
                icon,
                RevenueCatManager.PRODUCT_REMOVE_ADS,
                owned ? LocalizationManager.Get("shop_owned") : "₺69.99",
                "shop_perm_advantage",
                isPurchased: owned
            );
        }

        private void CreateProductCard(string titleKey, string descKey, Sprite icon, string productId, string fallbackPrice, string ribbonKey, bool isPurchased = false)
        {
            Transform targetContainer = cardsContainer != null ? cardsContainer : _cardsContainer;
            if (targetContainer == null) return;

            string price = fallbackPrice;
            string title = LocalizationManager.Get(titleKey);
            string desc = LocalizationManager.Get(descKey);
            string ribbon = !string.IsNullOrEmpty(ribbonKey) ? LocalizationManager.Get(ribbonKey) : null;

            if (productCardPrefab != null)
            {
                GameObject cardObj = Instantiate(productCardPrefab, targetContainer);
                _activeCardObjects.Add(cardObj);
                ShopProductCard cardComp = cardObj.GetComponent<ShopProductCard>();
                if (cardComp != null)
                {
                    cardComp.Setup(title, desc, icon, price, ribbon, isPurchased, () => OnBuyClicked(productId));
                    return;
                }
            }

            GameObject card = new GameObject("ProductCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(targetContainer, false);
            _activeCardObjects.Add(card);

            RectTransform cardRT = card.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(340f, 440f);

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.12f, 0.08f, 0.05f, 0.95f);

            // Çerçeve
            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(card.transform, false);
            Image borderImg = border.GetComponent<Image>();
            Sprite frameSprite = UIThemeHelper.GetButtonFrameSprite();
            if (frameSprite != null)
            {
                borderImg.sprite = frameSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = ribbonKey != null ? UIThemeHelper.ColorGold : UIThemeHelper.ColorWoodBorder;
            }
            RectTransform borderRT = border.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.sizeDelta = Vector2.zero;

            // Ribbon / Avantaj Rozeti
            if (!string.IsNullOrEmpty(ribbonKey))
            {
                GameObject ribbonObj = new GameObject("Ribbon", typeof(RectTransform), typeof(Image));
                ribbonObj.transform.SetParent(card.transform, false);
                Image ribImg = ribbonObj.GetComponent<Image>();
                ribImg.color = new Color(0.85f, 0.25f, 0.15f, 1f);
                RectTransform ribRT = ribbonObj.GetComponent<RectTransform>();
                ribRT.anchorMin = new Vector2(0.5f, 1f);
                ribRT.anchorMax = new Vector2(0.5f, 1f);
                ribRT.anchoredPosition = new Vector2(0f, -15f);
                ribRT.sizeDelta = new Vector2(220f, 32f);

                GameObject ribTxtObj = new GameObject("RibbonText", typeof(RectTransform), typeof(TextMeshProUGUI));
                ribTxtObj.transform.SetParent(ribbonObj.transform, false);
                TextMeshProUGUI ribTmp = ribTxtObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(ribTmp);
                ribTmp.text = LocalizationManager.Get(ribbonKey);
                ribTmp.fontSize = 18;
                ribTmp.color = Color.white;
                ribTmp.alignment = TextAlignmentOptions.Center;
                ribTmp.fontStyle = FontStyles.Bold;
                RectTransform ribTxtRT = ribTxtObj.GetComponent<RectTransform>();
                ribTxtRT.anchorMin = Vector2.zero;
                ribTxtRT.anchorMax = Vector2.one;
                ribTxtRT.sizeDelta = Vector2.zero;
            }

            // İkon
            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(card.transform, false);
                Image img = iconObj.GetComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.5f, 0.65f);
                iconRT.anchorMax = new Vector2(0.5f, 0.65f);
                iconRT.sizeDelta = new Vector2(100f, 100f);
            }

            // Başlık
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(titleTmp);
            titleTmp.text = LocalizationManager.Get(titleKey);
            titleTmp.fontSize = 24;
            titleTmp.color = UIThemeHelper.ColorGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.42f);
            titleRT.anchorMax = new Vector2(0.5f, 0.42f);
            titleRT.sizeDelta = new Vector2(300f, 35f);

            // Açıklama
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(descTmp);
            descTmp.text = LocalizationManager.Get(descKey);
            descTmp.fontSize = 18;
            descTmp.color = UIThemeHelper.ColorTextMuted;
            descTmp.alignment = TextAlignmentOptions.Center;
            RectTransform descRT = descObj.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.5f, 0.28f);
            descRT.anchorMax = new Vector2(0.5f, 0.28f);
            descRT.sizeDelta = new Vector2(300f, 45f);

            // Fiyat / Satın Alma Butonu
            GameObject btnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.sprite = frameSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = isPurchased ? Color.gray : new Color(0.2f, 0.65f, 0.25f, 1f);

            Button btn = btnObj.GetComponent<Button>();
            btn.interactable = !isPurchased;
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.1f);
            btnRT.anchorMax = new Vector2(0.5f, 0.1f);
            btnRT.sizeDelta = new Vector2(240f, 52f);

            // Fiyat Metni (RevenueCat veya Fallback)
            string displayPrice = fallbackPrice;
            if (RevenueCatManager.Instance != null && !isPurchased)
            {
                // Dynamic price from RC if available
                string rcPrice = RevenueCatManager.Instance.GetProductPrice(productId);
                if (!string.IsNullOrEmpty(rcPrice))
                {
                    displayPrice = rcPrice;
                }
            }

            GameObject priceTxtObj = new GameObject("PriceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceTxtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI priceTmp = priceTxtObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(priceTmp);
            priceTmp.text = displayPrice;
            priceTmp.fontSize = 24;
            priceTmp.color = Color.white;
            priceTmp.alignment = TextAlignmentOptions.Center;
            priceTmp.fontStyle = FontStyles.Bold;
            RectTransform priceTxtRT = priceTxtObj.GetComponent<RectTransform>();
            priceTxtRT.anchorMin = Vector2.zero;
            priceTxtRT.anchorMax = Vector2.one;
            priceTxtRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() =>
            {
                OnBuyClicked(productId);
            });
        }

        private void OnBuyClicked(string productId)
        {
            Debug.Log($"<color=yellow>[ShopUI]</color> Satın alma başlatılıyor: {productId}");
            if (RevenueCatManager.Instance != null)
            {
                RevenueCatManager.Instance.PurchaseProduct(productId);
            }
            else
            {
                ToastNotificationUI.ShowInfo("Satın alma simüle edildi.");
            }
        }

        private void BuildUI()
        {
            if (overlay != null && mainPanel != null && cardsContainer != null)
            {
                _overlay = overlay;
                _mainPanel = mainPanel.gameObject;
                _cardsContainer = cardsContainer;
                _goldValueText = goldValueText;
                _kadimValueText = kadimValueText;
                _tabsContainer = tabsContainer;

                HookTabButtons();
                return;
            }

            // Canvas
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 2600;

            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // Overlay
            _overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            _overlay.transform.SetParent(transform, false);
            Image ovImg = _overlay.GetComponent<Image>();
            ovImg.color = new Color(0.04f, 0.03f, 0.06f, 0.88f);
            RectTransform ovRT = _overlay.GetComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero;
            ovRT.anchorMax = Vector2.one;
            ovRT.sizeDelta = Vector2.zero;

            // Main Window Panel
            _mainPanel = new GameObject("MainPanel", typeof(RectTransform), typeof(Image));
            _mainPanel.transform.SetParent(_overlay.transform, false);
            Image mainImg = _mainPanel.GetComponent<Image>();
            Sprite bgSprite = UIThemeHelper.GetSettingsBgSprite();
            if (bgSprite != null)
            {
                mainImg.sprite = bgSprite;
                mainImg.color = Color.white;
            }
            else
            {
                mainImg.color = new Color(0.14f, 0.09f, 0.06f, 0.98f);
            }

            RectTransform mainRT = _mainPanel.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.08f, 0.06f);
            mainRT.anchorMax = new Vector2(0.92f, 0.94f);
            mainRT.sizeDelta = Vector2.zero;

            // Header Area
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(_mainPanel.transform, false);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.03f, 0.88f);
            headerRT.anchorMax = new Vector2(0.97f, 0.98f);
            headerRT.sizeDelta = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
            titleObj.transform.SetParent(header.transform, false);
            var loc = titleObj.GetComponent<LocalizedText>();
            loc.localizationKey = "shop_title";
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(titleTmp);
            titleTmp.text = LocalizationManager.Get("shop_title");
            titleTmp.fontSize = 44;
            titleTmp.color = UIThemeHelper.ColorGold;
            titleTmp.fontStyle = FontStyles.Bold;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.5f);
            titleRT.anchorMax = new Vector2(0f, 0.5f);
            titleRT.pivot = new Vector2(0f, 0.5f);
            titleRT.anchoredPosition = new Vector2(20f, 0f);
            titleRT.sizeDelta = new Vector2(400f, 60f);

            // Currency Badges (Sağ Üst: Altın & Kadim Para)
            GameObject currObj = new GameObject("CurrencyArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            currObj.transform.SetParent(header.transform, false);
            RectTransform currRT = currObj.GetComponent<RectTransform>();
            currRT.anchorMin = new Vector2(1f, 0.5f);
            currRT.anchorMax = new Vector2(1f, 0.5f);
            currRT.pivot = new Vector2(1f, 0.5f);
            currRT.anchoredPosition = new Vector2(-90f, 0f);
            currRT.sizeDelta = new Vector2(420f, 50f);

            HorizontalLayoutGroup curHlg = currObj.GetComponent<HorizontalLayoutGroup>();
            curHlg.spacing = 30f;
            curHlg.childAlignment = TextAnchor.MiddleRight;

            _goldValueText = CreateCurrencyBadge(currObj.transform, UIThemeHelper.GetGoldCoinSprite(), GameManager.SharedGold.ToString());
            _kadimValueText = CreateCurrencyBadge(currObj.transform, UIThemeHelper.GetAncientCoinSprite(), GameManager.SharedKadimPara.ToString());

            // Close Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(header.transform, false);
            Image closeImg = closeObj.GetComponent<Image>();
            closeImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);
            Button closeBtn = closeObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            RectTransform closeRT = closeObj.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0.5f);
            closeRT.anchorMax = new Vector2(1f, 0.5f);
            closeRT.pivot = new Vector2(1f, 0.5f);
            closeRT.anchoredPosition = new Vector2(-15f, 0f);
            closeRT.sizeDelta = new Vector2(48f, 48f);

            GameObject closeTxt = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxt.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI cTmp = closeTxt.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(cTmp);
            cTmp.text = "X";
            cTmp.fontSize = 28;
            cTmp.color = Color.white;
            cTmp.alignment = TextAlignmentOptions.Center;
            RectTransform cRT = closeTxt.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero;
            cRT.anchorMax = Vector2.one;
            cRT.sizeDelta = Vector2.zero;

            // Tabs Area
            GameObject tabsArea = new GameObject("TabsArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsArea.transform.SetParent(_mainPanel.transform, false);
            _tabsContainer = tabsArea.transform;
            RectTransform tabsRT = tabsArea.GetComponent<RectTransform>();
            tabsRT.anchorMin = new Vector2(0.04f, 0.77f);
            tabsRT.anchorMax = new Vector2(0.96f, 0.85f);
            tabsRT.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup tabHlg = tabsArea.GetComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 16f;
            tabHlg.childAlignment = TextAnchor.MiddleLeft;
            tabHlg.childControlWidth = false;
            tabHlg.childControlHeight = true;

            _tabButtons.Clear();
            _tabButtons.Add(CreateTabButton(tabsArea.transform, "shop_tab_kadim", ShopCategory.KadimPara));
            _tabButtons.Add(CreateTabButton(tabsArea.transform, "shop_tab_bundles", ShopCategory.Bundles));
            _tabButtons.Add(CreateTabButton(tabsArea.transform, "shop_tab_pass", ShopCategory.PotionPass));
            _tabButtons.Add(CreateTabButton(tabsArea.transform, "shop_tab_noads", ShopCategory.NoAds));

            // Content Area (Horizontal Scroll/Grid)
            GameObject cardsArea = new GameObject("CardsArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsArea.transform.SetParent(_mainPanel.transform, false);
            _cardsContainer = cardsArea.transform;
            RectTransform cardsRT = cardsArea.GetComponent<RectTransform>();
            cardsRT.anchorMin = new Vector2(0.04f, 0.05f);
            cardsRT.anchorMax = new Vector2(0.96f, 0.73f);
            cardsRT.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup cardHlg = cardsArea.GetComponent<HorizontalLayoutGroup>();
            cardHlg.spacing = 25f;
            cardHlg.childAlignment = TextAnchor.MiddleCenter;
            cardHlg.childControlWidth = false;
            cardHlg.childControlHeight = false;
        }

        private TextMeshProUGUI CreateCurrencyBadge(Transform parent, Sprite icon, string initialVal)
        {
            GameObject badge = new GameObject("Badge", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            badge.transform.SetParent(parent, false);
            RectTransform badgeRT = badge.GetComponent<RectTransform>();
            badgeRT.sizeDelta = new Vector2(180f, 40f);

            HorizontalLayoutGroup hlg = badge.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            if (icon != null)
            {
                GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icObj.transform.SetParent(badge.transform, false);
                Image img = icObj.GetComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                RectTransform icRT = icObj.GetComponent<RectTransform>();
                icRT.sizeDelta = new Vector2(36f, 36f);
            }

            GameObject valObj = new GameObject("Val", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(badge.transform, false);
            TextMeshProUGUI tmp = valObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(tmp);
            tmp.text = initialVal;
            tmp.fontSize = 26;
            tmp.color = UIThemeHelper.ColorGold;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform valRT = valObj.GetComponent<RectTransform>();
            valRT.sizeDelta = new Vector2(130f, 40f);

            return tmp;
        }

        private Button CreateTabButton(Transform parent, string locKey, ShopCategory category)
        {
            GameObject btnObj = new GameObject($"Tab_{category}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            Sprite frame = UIThemeHelper.GetButtonFrameSprite();
            if (frame != null)
            {
                img.sprite = frame;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.25f, 0.16f, 0.10f, 0.85f);

            Button btn = btnObj.GetComponent<Button>();
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 54f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
            textObj.transform.SetParent(btnObj.transform, false);
            var loc = textObj.GetComponent<LocalizedText>();
            loc.localizationKey = locKey;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(tmp);
            tmp.text = LocalizationManager.Get(locKey);
            tmp.fontSize = 22;
            tmp.color = UIThemeHelper.ColorTextWarm;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() =>
            {
                SelectCategory(category);
            });

            return btn;
        }
    }
}
