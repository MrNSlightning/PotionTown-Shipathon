#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using PotionShop.UI;

namespace PotionShop.Editor
{
    /// <summary>
    /// PotionTown projesindeki tüm UI sistemlerini (Başlangıç Menüsü, Mağaza, Önizleme, Geri Bildirim, Bildirim, Tüyo)
    /// Prefab olarak oluşturan, hiyerarşilerini kurup Inspector referanslarını eksiksiz bağlayan
    /// ve PotionTown sahnesine konuşlandıran merkezi Editor aracı.
    /// </summary>
    [InitializeOnLoad]
    public static class UIPrefabBuilder
    {
        private const string PrefabDirMenus = "Assets/_PotionTown/Prefabs/UI/Menus";
        private const string PrefabDirFeedback = "Assets/_PotionTown/Prefabs/UI/Feedback";
        private const string PrefabDirButtons = "Assets/_PotionTown/Prefabs/UI/Buttons";

        public const string PathStartMenu = "Assets/_PotionTown/Prefabs/UI/Menus/GameStartMenuUI.prefab";
        public const string PathProductCard = "Assets/_PotionTown/Prefabs/UI/Menus/ShopProductCard.prefab";
        public const string PathShopUI = "Assets/_PotionTown/Prefabs/UI/Menus/ShopUI.prefab";
        public const string PathCurrencyFeedback = "Assets/_PotionTown/Prefabs/UI/Feedback/CurrencyFeedbackUI.prefab";
        public const string PathToastNotification = "Assets/_PotionTown/Prefabs/UI/Feedback/ToastNotificationUI.prefab";
        public const string PathHintButton = "Assets/_PotionTown/Prefabs/UI/Buttons/PotionShopHintButton.prefab";

        private const string GuidFont = "8904e588fdfe5274998b8a5a195864b5"; // NewRocker-Regular SDF.asset
        private const string GuidParchment = "9b1fa501532c11d48ad8d3685dc36211"; // Parsomen.png

        static UIPrefabBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PathStartMenu) || !File.Exists(PathShopUI) || !File.Exists(PathToastNotification))
                {
                    Debug.Log("<color=cyan>[UIPrefabBuilder]</color> Eksik UI prefab'ları tespit edildi. Otomatik inşa ediliyor...");
                    RebuildAllPrefabs();
                    DeployAllUIToCurrentScene();
                }
            };
        }

        [MenuItem("Tools/PotionTown/Rebuild All UI Prefabs", false, 30)]
        public static void RebuildAllPrefabs()
        {
            EnsureDirectories();
            var font = LoadFont();

            Debug.Log("<color=cyan>[UIPrefabBuilder]</color> Tüm UI prefab'ları inşa ediliyor...");

            CreateStartMenuPrefab(font);
            CreateProductCardPrefab(font);
            CreateShopUIPrefab(font);
            CreateCurrencyFeedbackPrefab(font);
            CreateToastNotificationPrefab(font);
            CreateHintButtonPrefab(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[UIPrefabBuilder]</color> Tüm UI prefab'ları başarıyla oluşturuldu ve kaydedildi!");
        }

        [MenuItem("Tools/PotionTown/Deploy All UI To Current Scene", false, 31)]
        public static void DeployAllUIToCurrentScene()
        {
            RebuildAllPrefabs();

            GameObject globalUIRoot = GameObject.Find("--- [03_GLOBAL_UI] ---");
            if (globalUIRoot == null)
            {
                globalUIRoot = new GameObject("--- [03_GLOBAL_UI] ---");
                Undo.RegisterCreatedObjectUndo(globalUIRoot, "Create GLOBAL_UI");
            }

            var coordinator = PotionTownSceneCoordinator.Instance ?? Object.FindFirstObjectByType<PotionTownSceneCoordinator>();

            // 1. GameStartMenuUI
            var startMenuInstance = DeployPrefabToParent<GameStartMenuUI>(PathStartMenu, globalUIRoot.transform, "GameStartMenuUI");
            if (startMenuInstance != null) startMenuInstance.gameObject.SetActive(true);

            // 2. ShopUI
            var shopInstance = DeployPrefabToParent<ShopUI>(PathShopUI, globalUIRoot.transform, "ShopUI");
            if (shopInstance != null)
            {
                shopInstance.gameObject.SetActive(true);
                if (shopInstance.overlay != null) shopInstance.overlay.SetActive(false);
            }

            // 3. CurrencyFeedbackUI
            var feedbackInstance = DeployPrefabToParent<CurrencyFeedbackUI>(PathCurrencyFeedback, globalUIRoot.transform, "CurrencyFeedbackUI");
            if (feedbackInstance != null) feedbackInstance.gameObject.SetActive(true);

            // 4. ToastNotificationUI
            var toastInstance = DeployPrefabToParent<ToastNotificationUI>(PathToastNotification, globalUIRoot.transform, "ToastNotificationUI");
            if (toastInstance != null) toastInstance.gameObject.SetActive(true);

            // 5. PotionShopHintButton (İksir odasının altına veya global UI'ya)
            Transform hintParent = globalUIRoot.transform;
            if (coordinator != null && coordinator.potionSellingRoom != null)
            {
                hintParent = coordinator.potionSellingRoom.transform;
            }
            var hintInstance = DeployPrefabToParent<PotionShopHintButton>(PathHintButton, hintParent, "PotionShopHintButton");

            // Coordinator bağlantıları
            if (coordinator != null)
            {
                Undo.RecordObject(coordinator, "Link UI References to Coordinator");
                coordinator.startMenuUI = startMenuInstance;
                coordinator.shopUI = shopInstance;
                coordinator.currencyFeedbackUI = feedbackInstance;
                coordinator.toastNotificationUI = toastInstance;
                coordinator.hintButton = hintInstance;
                EditorUtility.SetDirty(coordinator);
            }

            // Sahneyi kaydet
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=green>[UIPrefabBuilder]</color> Tüm UI prefab'ları sahneye yerleştirildi ve Coordinator'a bağlandı!");
        }

        private static T DeployPrefabToParent<T>(string prefabPath, Transform parent, string objectName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;

            // Sahnedeki mevcut eski instance'ları bul
            var oldInstances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            T targetInstance = null;

            foreach (var inst in oldInstances)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(inst.gameObject)) continue;
                if (targetInstance == null)
                {
                    targetInstance = inst;
                }
                else
                {
                    // Fazla kopyaları temizle
                    Undo.DestroyObjectImmediate(inst.gameObject);
                }
            }

            if (targetInstance == null)
            {
                GameObject newGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                newGO.name = objectName;
                Undo.RegisterCreatedObjectUndo(newGO, "Deploy " + objectName);
                targetInstance = newGO.GetComponent<T>();
            }
            else
            {
                targetInstance.transform.SetParent(parent, false);
                targetInstance.gameObject.name = objectName;
            }

            return targetInstance;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_PotionTown/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs", "UI");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDirMenus))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs/UI", "Menus");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDirFeedback))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs/UI", "Feedback");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDirButtons))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs/UI", "Buttons");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  1. GAME START MENU PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateStartMenuPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("GameStartMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameStartMenuUI));
            SetupCanvas(root, 2400);

            GameStartMenuUI comp = root.GetComponent<GameStartMenuUI>();

            // Overlay Blocker
            GameObject overlay = CreateUIObject("OverlayBlocker", root.transform);
            StretchFull(overlay);
            Image ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0.04f, 0.03f, 0.06f, 0.88f);
            ovImg.raycastTarget = true;
            comp.overlayBlocker = overlay;

            // Window Panel
            GameObject win = CreateUIObject("WindowPanel", overlay.transform);
            RectTransform winRT = win.GetComponent<RectTransform>();
            winRT.anchorMin = new Vector2(0.5f, 0.5f);
            winRT.anchorMax = new Vector2(0.5f, 0.5f);
            winRT.pivot = new Vector2(0.5f, 0.5f);
            winRT.sizeDelta = new Vector2(650f, 620f);

            Image winImg = win.AddComponent<Image>();
            Sprite bgSprite = LoadSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Settings_Panel_Bg.png");
            if (bgSprite != null) winImg.sprite = bgSprite;
            else winImg.color = new Color(0.14f, 0.09f, 0.06f, 0.98f);
            comp.windowPanel = win;

            // Title
            GameObject titleObj = CreateTextObject("TitleText", win.transform, font, "POTION TOWN", 52, UIThemeHelper.ColorGold, TextAlignmentOptions.Center);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchoredPosition = new Vector2(0f, 210f);
            titleRT.sizeDelta = new Vector2(500f, 70f);
            comp.titleText = titleObj.GetComponent<TextMeshProUGUI>();

            // Subtitle
            GameObject subObj = CreateTextObject("SubtitleText", win.transform, font, "Simya Dükkanı", 26, UIThemeHelper.ColorTextMuted, TextAlignmentOptions.Center);
            RectTransform subRT = subObj.GetComponent<RectTransform>();
            subRT.anchoredPosition = new Vector2(0f, 155f);
            subRT.sizeDelta = new Vector2(400f, 40f);
            comp.subtitleText = subObj.GetComponent<TextMeshProUGUI>();

            // Buttons Container
            GameObject btnCont = CreateUIObject("ButtonsContainer", win.transform);
            RectTransform btnContRT = btnCont.GetComponent<RectTransform>();
            btnContRT.anchoredPosition = new Vector2(0f, -40f);
            btnContRT.sizeDelta = new Vector2(420f, 320f);
            VerticalLayoutGroup vlg = btnCont.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Sprite btnFrame = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png");

            // Start Level Button
            comp.startLevelButton = CreateButtonWithLabel(btnCont.transform, "StartLevelButton", font, "GÜNÜ BAŞLAT", btnFrame, new Color(0.2f, 0.65f, 0.25f, 1f));
            
            // Settings Button
            comp.settingsButton = CreateButtonWithLabel(btnCont.transform, "SettingsButton", font, "AYARLAR", btnFrame, new Color(0.28f, 0.2f, 0.15f, 1f));

            // Language Button
            comp.languageButton = CreateButtonWithLabel(btnCont.transform, "LanguageButton", font, "DİL: TÜRKÇE", btnFrame, new Color(0.22f, 0.25f, 0.38f, 1f));
            comp.languageButtonText = comp.languageButton.GetComponentInChildren<TextMeshProUGUI>();

            SavePrefab(root, PathStartMenu);
        }

        // ═══════════════════════════════════════════════════════
        //  2. SHOP PRODUCT CARD PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateProductCardPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("ShopProductCard", typeof(RectTransform), typeof(Image), typeof(ShopProductCard));
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(340f, 440f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.08f, 0.05f, 0.95f);

            ShopProductCard card = root.GetComponent<ShopProductCard>();
            card.cardBackground = bg;

            // Border Frame
            GameObject border = CreateUIObject("Border", root.transform);
            StretchFull(border);
            Image borderImg = border.AddComponent<Image>();
            borderImg.sprite = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png");
            borderImg.type = Image.Type.Sliced;
            borderImg.color = UIThemeHelper.ColorGold;
            card.borderFrame = borderImg;

            // Ribbon Badge
            GameObject ribbon = CreateUIObject("RibbonBadge", root.transform);
            RectTransform ribRT = ribbon.GetComponent<RectTransform>();
            ribRT.anchorMin = new Vector2(0.5f, 1f);
            ribRT.anchorMax = new Vector2(0.5f, 1f);
            ribRT.pivot = new Vector2(0.5f, 1f);
            ribRT.anchoredPosition = new Vector2(0f, -12f);
            ribRT.sizeDelta = new Vector2(220f, 32f);
            Image ribImg = ribbon.AddComponent<Image>();
            ribImg.color = new Color(0.85f, 0.25f, 0.15f, 1f);
            card.ribbonBadge = ribbon;

            GameObject ribTxt = CreateTextObject("RibbonText", ribbon.transform, font, "FIRSAT", 18, Color.white, TextAlignmentOptions.Center);
            StretchFull(ribTxt);
            card.ribbonText = ribTxt.GetComponent<TextMeshProUGUI>();

            // Product Icon
            GameObject iconObj = CreateUIObject("ProductIcon", root.transform);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1f);
            iconRT.anchorMax = new Vector2(0.5f, 1f);
            iconRT.pivot = new Vector2(0.5f, 1f);
            iconRT.anchoredPosition = new Vector2(0f, -55f);
            iconRT.sizeDelta = new Vector2(110f, 110f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            card.productIcon = iconImg;

            // Title
            GameObject title = CreateTextObject("TitleText", root.transform, font, "Ürün Başlığı", 26, UIThemeHelper.ColorGold, TextAlignmentOptions.Center);
            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.anchoredPosition = new Vector2(0f, -185f);
            titleRT.sizeDelta = new Vector2(310f, 40f);
            card.titleText = title.GetComponent<TextMeshProUGUI>();

            // Desc
            GameObject desc = CreateTextObject("DescText", root.transform, font, "Ürün açıklaması burada yer alır.", 20, UIThemeHelper.ColorTextMuted, TextAlignmentOptions.Center);
            RectTransform descRT = desc.GetComponent<RectTransform>();
            descRT.anchoredPosition = new Vector2(0f, -245f);
            descRT.sizeDelta = new Vector2(300f, 65f);
            card.descText = desc.GetComponent<TextMeshProUGUI>();

            // Buy Button
            Sprite btnFrame = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png");
            Button buyBtn = CreateButtonWithLabel(root.transform, "BuyButton", font, "₺34.99", btnFrame, new Color(0.2f, 0.65f, 0.25f, 1f));
            RectTransform buyRT = buyBtn.GetComponent<RectTransform>();
            buyRT.anchorMin = new Vector2(0.5f, 0f);
            buyRT.anchorMax = new Vector2(0.5f, 0f);
            buyRT.pivot = new Vector2(0.5f, 0f);
            buyRT.anchoredPosition = new Vector2(0f, 25f);
            buyRT.sizeDelta = new Vector2(250f, 55f);
            card.buyButton = buyBtn;
            card.priceText = buyBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Owned Overlay
            GameObject owned = CreateUIObject("OwnedOverlay", root.transform);
            StretchFull(owned);
            Image ownedImg = owned.AddComponent<Image>();
            ownedImg.color = new Color(0f, 0f, 0f, 0.65f);
            GameObject ownedTxt = CreateTextObject("Text", owned.transform, font, "SAHİPSİN", 32, UIThemeHelper.ColorGold, TextAlignmentOptions.Center);
            StretchFull(ownedTxt);
            card.ownedOverlay = owned;
            owned.SetActive(false);

            SavePrefab(root, PathProductCard);
        }

        // ═══════════════════════════════════════════════════════
        //  3. SHOP UI PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateShopUIPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("ShopUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ShopUI));
            SetupCanvas(root, 2600);

            ShopUI comp = root.GetComponent<ShopUI>();

            // Overlay
            GameObject overlay = CreateUIObject("Overlay", root.transform);
            StretchFull(overlay);
            Image ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0.04f, 0.03f, 0.06f, 0.88f);
            ovImg.raycastTarget = true;
            comp.overlay = overlay;

            // Main Panel
            GameObject main = CreateUIObject("MainPanel", overlay.transform);
            RectTransform mainRT = main.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.08f, 0.06f);
            mainRT.anchorMax = new Vector2(0.92f, 0.94f);
            mainRT.sizeDelta = Vector2.zero;
            Image mainImg = main.AddComponent<Image>();
            Sprite bgSprite = LoadSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Settings_Panel_Bg.png");
            if (bgSprite != null) mainImg.sprite = bgSprite;
            else mainImg.color = new Color(0.14f, 0.09f, 0.06f, 0.98f);
            comp.mainPanel = mainRT;

            // Header Area
            GameObject header = CreateUIObject("Header", main.transform);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.03f, 0.88f);
            headerRT.anchorMax = new Vector2(0.97f, 0.98f);
            headerRT.sizeDelta = Vector2.zero;

            // Title
            GameObject title = CreateTextObject("TitleText", header.transform, font, "SİMYACI PAZARI", 44, UIThemeHelper.ColorGold, TextAlignmentOptions.MidlineLeft);
            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.5f);
            titleRT.anchorMax = new Vector2(0f, 0.5f);
            titleRT.pivot = new Vector2(0f, 0.5f);
            titleRT.anchoredPosition = new Vector2(20f, 0f);
            titleRT.sizeDelta = new Vector2(400f, 60f);
            comp.titleText = title.GetComponent<TextMeshProUGUI>();

            // Currency Area
            GameObject curArea = CreateUIObject("CurrencyArea", header.transform);
            RectTransform curRT = curArea.GetComponent<RectTransform>();
            curRT.anchorMin = new Vector2(1f, 0.5f);
            curRT.anchorMax = new Vector2(1f, 0.5f);
            curRT.pivot = new Vector2(1f, 0.5f);
            curRT.anchoredPosition = new Vector2(-80f, 0f);
            curRT.sizeDelta = new Vector2(380f, 50f);
            HorizontalLayoutGroup curHlg = curArea.AddComponent<HorizontalLayoutGroup>();
            curHlg.spacing = 20f;
            curHlg.childAlignment = TextAnchor.MiddleRight;

            // Gold counter
            GameObject goldObj = CreateCurrencyCounter(curArea.transform, font, LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/Gold coin.png"), "0");
            comp.goldValueText = goldObj.GetComponentInChildren<TextMeshProUGUI>();

            // Kadim counter
            GameObject kadimObj = CreateCurrencyCounter(curArea.transform, font, LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/Ancient coin.png"), "0");
            comp.kadimValueText = kadimObj.GetComponentInChildren<TextMeshProUGUI>();

            // Close Button
            GameObject closeObj = CreateUIObject("CloseButton", header.transform);
            RectTransform closeRT = closeObj.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0.5f);
            closeRT.anchorMax = new Vector2(1f, 0.5f);
            closeRT.pivot = new Vector2(1f, 0.5f);
            closeRT.anchoredPosition = new Vector2(0f, 0f);
            closeRT.sizeDelta = new Vector2(50f, 50f);
            Image closeImg = closeObj.AddComponent<Image>();
            Sprite closeSprite = LoadSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Close_Button.png");
            if (closeSprite != null) closeImg.sprite = closeSprite;
            else closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button closeBtn = closeObj.AddComponent<Button>();
            comp.closeButton = closeBtn;

            // Tabs Area
            GameObject tabsObj = CreateUIObject("TabsContainer", main.transform);
            RectTransform tabsRT = tabsObj.GetComponent<RectTransform>();
            tabsRT.anchorMin = new Vector2(0.04f, 0.80f);
            tabsRT.anchorMax = new Vector2(0.96f, 0.87f);
            tabsRT.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup tabsHlg = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabsHlg.spacing = 15f;
            tabsHlg.childAlignment = TextAnchor.MiddleCenter;
            tabsHlg.childControlWidth = true;
            tabsHlg.childControlHeight = true;
            tabsHlg.childForceExpandWidth = true;
            tabsHlg.childForceExpandHeight = true;
            comp.tabsContainer = tabsObj.transform;

            Sprite tabFrame = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png");
            comp.tabKadimButton = CreateButtonWithLabel(tabsObj.transform, "Tab_Kadim", font, "KADİM PARA", tabFrame, UIThemeHelper.ColorGold);
            comp.tabBundlesButton = CreateButtonWithLabel(tabsObj.transform, "Tab_Bundles", font, "ÖZEL TEKLİFLER", tabFrame, new Color(0.25f, 0.16f, 0.10f, 0.85f));
            comp.tabPotionPassButton = CreateButtonWithLabel(tabsObj.transform, "Tab_Pass", font, "İKSİR KARTI", tabFrame, new Color(0.25f, 0.16f, 0.10f, 0.85f));
            comp.tabNoAdsButton = CreateButtonWithLabel(tabsObj.transform, "Tab_NoAds", font, "REKLAMSIZ", tabFrame, new Color(0.25f, 0.16f, 0.10f, 0.85f));

            // Cards Scroll View
            GameObject scrollObj = CreateUIObject("CardsScrollView", main.transform);
            RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.04f, 0.04f);
            scrollRT.anchorMax = new Vector2(0.96f, 0.78f);
            scrollRT.sizeDelta = Vector2.zero;
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = true;
            sr.vertical = false;

            GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
            StretchFull(viewport);
            viewport.AddComponent<RectMask2D>();
            sr.viewport = viewport.GetComponent<RectTransform>();

            GameObject content = CreateUIObject("CardsContainer", viewport.transform);
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 0.5f);
            contentRT.anchorMax = new Vector2(0f, 0.5f);
            contentRT.pivot = new Vector2(0f, 0.5f);
            contentRT.sizeDelta = new Vector2(1600f, 480f);
            HorizontalLayoutGroup cardHlg = content.AddComponent<HorizontalLayoutGroup>();
            cardHlg.spacing = 30f;
            cardHlg.childAlignment = TextAnchor.MiddleCenter;
            cardHlg.childControlWidth = false;
            cardHlg.childControlHeight = false;
            cardHlg.childForceExpandWidth = false;
            cardHlg.childForceExpandHeight = false;

            sr.content = contentRT;
            comp.cardsContainer = content.transform;

            // Product card prefab bağlantısı
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathProductCard);
            comp.productCardPrefab = cardPrefab;

            SavePrefab(root, PathShopUI);
        }

        // ═══════════════════════════════════════════════════════
        //  4. CURRENCY FEEDBACK PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateCurrencyFeedbackPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("CurrencyFeedbackUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CurrencyFeedbackUI));
            SetupCanvas(root, 2800);

            CurrencyFeedbackUI comp = root.GetComponent<CurrencyFeedbackUI>();

            GameObject cont = CreateUIObject("FeedbackContainer", root.transform);
            StretchFull(cont);
            comp.feedbackContainer = cont.transform;

            comp.goldSprite = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/Gold coin.png");
            comp.kadimSprite = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/Ancient coin.png");
            comp.gainColor = new Color(0.25f, 0.95f, 0.35f, 1f);
            comp.lossColor = new Color(1f, 0.3f, 0.3f, 1f);

            SavePrefab(root, PathCurrencyFeedback);
        }

        // ═══════════════════════════════════════════════════════
        //  6. TOAST NOTIFICATION PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateToastNotificationPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("ToastNotificationUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ToastNotificationUI));
            SetupCanvas(root, 3100);

            ToastNotificationUI comp = root.GetComponent<ToastNotificationUI>();

            GameObject cont = CreateUIObject("ToastContainer", root.transform);
            RectTransform contRT = cont.GetComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.2f, 0.65f);
            contRT.anchorMax = new Vector2(0.8f, 0.85f);
            contRT.sizeDelta = Vector2.zero;
            comp.toastContainer = cont.transform;

            comp.warningSprite = LoadSprite("Assets/_PotionTown/Art/UI/Uyarı.png");
            comp.displayDuration = 2.0f;

            SavePrefab(root, PathToastNotification);
        }

        // ═══════════════════════════════════════════════════════
        //  7. POTION SHOP HINT BUTTON PREFAB
        // ═══════════════════════════════════════════════════════
        private static void CreateHintButtonPrefab(TMP_FontAsset font)
        {
            GameObject root = new GameObject("PotionShopHintButton", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PotionShopHintButton));
            SetupCanvas(root, 700);

            PotionShopHintButton comp = root.GetComponent<PotionShopHintButton>();
            Sprite btnFrame = LoadSprite("Assets/_PotionTown/Art/UI/TopMenu/ButtonFrame.png");

            // Hint Button
            comp.hintButton = CreateButtonWithLabel(root.transform, "HintButton", font, "TÜYO (REKLAM)", btnFrame, new Color(0.22f, 0.45f, 0.65f, 1f));
            RectTransform rt = comp.hintButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(30f, 40f);
            rt.sizeDelta = new Vector2(210f, 55f);
            comp.hintButtonText = comp.hintButton.GetComponentInChildren<TextMeshProUGUI>();

            // Hint Dialog Popup
            GameObject dialog = CreateUIObject("HintDialog", root.transform);
            StretchFull(dialog);
            Image dImg = dialog.AddComponent<Image>();
            dImg.color = new Color(0f, 0f, 0f, 0.72f);
            dImg.raycastTarget = true;
            comp.hintDialog = dialog;

            // Parchment Card
            GameObject card = CreateUIObject("ParchmentCard", dialog.transform);
            RectTransform cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(580f, 420f);
            Image cardImg = card.AddComponent<Image>();
            Sprite parch = LoadSprite("Assets/_PotionTown/Art/UI/Parsomen.png");
            if (parch != null) cardImg.sprite = parch;
            else cardImg.color = new Color(0.9f, 0.85f, 0.7f, 1f);

            // Content Text
            GameObject content = CreateTextObject("ContentText", card.transform, font, "Günün iksir bilgileri hazırlanıyor...", 24, UIThemeHelper.ColorWoodDark, TextAlignmentOptions.TopLeft);
            RectTransform contRT = content.GetComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.08f, 0.22f);
            contRT.anchorMax = new Vector2(0.92f, 0.92f);
            contRT.sizeDelta = Vector2.zero;
            comp.hintContentText = content.GetComponent<TextMeshProUGUI>();

            // Close Button
            comp.closeDialogButton = CreateButtonWithLabel(card.transform, "CloseBtn", font, "TAMAM", btnFrame, new Color(0.35f, 0.22f, 0.12f, 1f));
            RectTransform cRT = comp.closeDialogButton.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.5f, 0.06f);
            cRT.anchorMax = new Vector2(0.5f, 0.06f);
            cRT.pivot = new Vector2(0.5f, 0f);
            cRT.sizeDelta = new Vector2(180f, 45f);

            dialog.SetActive(false);

            SavePrefab(root, PathHintButton);
        }

        // ═══════════════════════════════════════════════════════
        //  YARDIMCI ARAÇLAR
        // ═══════════════════════════════════════════════════════

        private static void SetupCanvas(GameObject go, int sortingOrder)
        {
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateTextObject(string name, Transform parent, TMP_FontAsset font, string text, float fontSize, Color color, TextAlignmentOptions align)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            return go;
        }

        private static Button CreateButtonWithLabel(Transform parent, string name, TMP_FontAsset font, string label, Sprite bgSprite, Color color)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image img = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
                img.color = color;
            }
            else
            {
                img.color = color;
            }

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 0.9f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            GameObject txtObj = CreateTextObject("Text", btnObj.transform, font, label, 26, UIThemeHelper.ColorTextWarm, TextAlignmentOptions.Center);
            StretchFull(txtObj);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static GameObject CreateCurrencyCounter(Transform parent, TMP_FontAsset font, Sprite coinSprite, string initialVal)
        {
            GameObject root = CreateUIObject("CurrencyItem", parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 45f);

            HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            if (coinSprite != null)
            {
                GameObject icon = CreateUIObject("Icon", root.transform);
                Image img = icon.AddComponent<Image>();
                img.sprite = coinSprite;
                img.preserveAspect = true;
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);
            }

            GameObject txt = CreateTextObject("Value", root.transform, font, initialVal, 26, UIThemeHelper.ColorTextWarm, TextAlignmentOptions.MidlineLeft);
            txt.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 40f);

            return root;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static TMP_FontAsset LoadFont()
        {
            string path = AssetDatabase.GUIDToAssetPath(GuidFont);
            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            return Resources.Load<TMP_FontAsset>("Fonts/NewRocker-Regular SDF");
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"<color=cyan>[UIPrefabBuilder]</color> Prefab kaydedildi: {path}");
        }
    }
}
#endif
