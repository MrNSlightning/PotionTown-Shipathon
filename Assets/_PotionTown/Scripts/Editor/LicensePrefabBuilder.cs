using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace PotionShop.Editor
{
    /// <summary>
    /// Lisans Parşömeni ve Envanter Eşya Detay Popup'ı için kullanılan tüm UI bileşenlerini
    /// Unity Prefabları (.prefab) olarak oluşturan ve 'Assets/_PotionTown/Prefabs/UI/License/' klasörüne kaydeden editör aracı.
    /// Geliştiriciler bu prefabları Unity Inspector ve Prefab Mode üzerinden diledikleri gibi düzenleyebilirler.
    /// </summary>
    public static class LicensePrefabBuilder
    {
        private const string PREFAB_DIR = "Assets/_PotionTown/Prefabs/UI/License";
        private const string INVENTORY_PREFAB_DIR = "Assets/_PotionTown/Prefabs/UI/Inventory";

        [MenuItem("Tools/Potion Shop/Tüm Lisans Prefablarını Oluştur", priority = 105)]
        public static void BuildAllPrefabs()
        {
            EnsureDirectoryExists(PREFAB_DIR);
            EnsureDirectoryExists(INVENTORY_PREFAB_DIR);

            // Görseller ve Font
            Sprite headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Header_Banner.png");
            Sprite rowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Row_Plate.png");
            Sprite buyBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Buy_Button.png");
            Sprite bulkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Bulk_Banner.png");
            Sprite closeSealSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Close_Seal.png");
            Sprite sealStampSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Seal_Stamp.png");
            Sprite dividerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/License/License_Divider.png");
            Sprite goldCoinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Gold Coin.png");

            Sprite slotSprite = LoadSubSprite("Assets/_PotionTown/Art/UI/Slot.png", "Slot_0");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset");

            // 1. Kapatma Butonu Prefabı
            BuildCloseButtonPrefab(closeSealSprite);

            // 2. Başlık Panosu Prefabı
            BuildHeaderPrefab(headerSprite, font);

            // 3. Toplu Satın Alma Panosu Prefabı
            BuildBulkBuyPrefab(bulkSprite, dividerSprite, goldCoinSprite, font);

            // 4. İksir Satır Plakası Prefabı
            BuildRowPrefab(rowSprite, slotSprite, buyBtnSprite, sealStampSprite, goldCoinSprite, font);

            // 5. Sayfa Kapsayıcı Şablon Prefabı
            BuildPagePrefab();

            // 6. Tam Lisans Parşömeni Popup Prefabı
            BuildCompleteParchmentPrefab(closeSealSprite);

            // 7. Eşya Detay Popup'ı Prefabı (Assets/_PotionTown/Prefabs/UI/Inventory/ItemDetailPopup.prefab)
            BuildItemDetailPopupPrefab(slotSprite, font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[LisansPrefabBuilder]</b> Tüm lisans ve arayüz prefabları başarıyla 'Assets/_PotionTown/Prefabs/UI/License/' ve 'Assets/_PotionTown/Prefabs/UI/Inventory/' içerisine kaydedildi!</color>");
        }

        public static void BuildAllPrefabsBatch()
        {
            BuildAllPrefabs();
            EditorApplication.Exit(0);
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                AssetDatabase.Refresh();
            }
        }

        private static Sprite LoadSubSprite(string path, string subName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s && s.name == subName) return s;
            }
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            return null;
        }

        // =========================================================================
        // 1. Kapatma Balmumu Mührü Butonu
        // =========================================================================
        private static void BuildCloseButtonPrefab(Sprite sealSprite)
        {
            string path = $"{PREFAB_DIR}/Lisans_KapatButonu.prefab";

            GameObject go = new GameObject("Lisans_KapatButonu", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(52f, 52f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = sealSprite;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 0.95f, 0.95f, 1f);
            cb.pressedColor = new Color(0.85f, 0.7f, 0.7f, 1f);
            btn.colors = cb;

            SavePrefab(go, path);
        }

        // =========================================================================
        // 2. Üst Başlık Panosu
        // =========================================================================
        private static void BuildHeaderPrefab(Sprite headerSprite, TMP_FontAsset font)
        {
            string path = $"{PREFAB_DIR}/Lisans_Başlık.prefab";

            GameObject go = new GameObject("Lisans_Başlık", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640f, 68f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 68f;
            le.minHeight = 60f;
            le.flexibleWidth = 1f;

            Image img = go.GetComponent<Image>();
            img.sprite = headerSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;

            // Header Metni
            GameObject textObj = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(go.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(20f, 4f);
            trt.offsetMax = new Vector2(-20f, -4f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = "LİSANS FERMANI";
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.96f, 0.82f, 1f);
            tmp.raycastTarget = false;

            SavePrefab(go, path);
        }

        // =========================================================================
        // 3. Toplu Satın Alma Panosu
        // =========================================================================
        private static void BuildBulkBuyPrefab(Sprite bulkSprite, Sprite dividerSprite, Sprite goldCoinSprite, TMP_FontAsset font)
        {
            string path = $"{PREFAB_DIR}/Lisans_TopluSatınAl.prefab";

            GameObject go = new GameObject("Lisans_TopluSatınAl", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640f, 156f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 156f;
            le.minHeight = 140f;
            le.flexibleWidth = 1f;

            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Yakut Taşlı Telkari Ayırıcı
            GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            sepObj.transform.SetParent(go.transform, false);
            LayoutElement sepLE = sepObj.GetComponent<LayoutElement>();
            sepLE.preferredHeight = 22f;
            sepLE.minHeight = 20f;

            Image sepImg = sepObj.GetComponent<Image>();
            sepImg.sprite = dividerSprite;
            sepImg.preserveAspect = true;
            sepImg.color = Color.white;
            sepImg.raycastTarget = false;

            // 2. Ferman Şeklinde Toplu Buton (2589 x 534 Oranında: 600 x 124)
            GameObject bulkBtnObj = new GameObject("BulkBuyBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            bulkBtnObj.transform.SetParent(go.transform, false);
            LayoutElement btnLE = bulkBtnObj.GetComponent<LayoutElement>();
            btnLE.preferredWidth = 600f;
            btnLE.preferredHeight = 124f;
            btnLE.minWidth = 580f;
            btnLE.minHeight = 115f;

            Image bulkImg = bulkBtnObj.GetComponent<Image>();
            bulkImg.sprite = bulkSprite;
            bulkImg.type = Image.Type.Sliced;
            bulkImg.pixelsPerUnitMultiplier = 1.8f;
            bulkImg.color = Color.white;
            bulkImg.raycastTarget = true;

            Button btn = bulkBtnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.60f, 1f);
            btn.colors = cb;

            // 2a. Başlık Metni (Hiyerarşide açıkça görünen ve düzenlenebilen Text objesi)
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(bulkBtnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.42f);
            trt.anchorMax = new Vector2(1f, 0.95f);
            trt.offsetMin = new Vector2(20f, 0f);
            trt.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = "HEPSİNİ BİRLİKTE AL";
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.90f, 0.55f, 1f);
            tmp.raycastTarget = false;

            // 2b. Alt Açıklama Yazısı (Hiyerarşide açıkça görünen SubText objesi)
            GameObject subTextObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTextObj.transform.SetParent(bulkBtnObj.transform, false);
            RectTransform subTrt = subTextObj.GetComponent<RectTransform>();
            subTrt.anchorMin = new Vector2(0f, 0.08f);
            subTrt.anchorMax = new Vector2(1f, 0.45f);
            subTrt.offsetMin = new Vector2(20f, 0f);
            subTrt.offsetMax = new Vector2(-20f, 0f);

            TextMeshProUGUI subTmp = subTextObj.GetComponent<TextMeshProUGUI>();
            if (font != null) subTmp.font = font;
            subTmp.text = "500       (%10 İndirim)";
            subTmp.fontSize = 22f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(1f, 0.94f, 0.78f, 1f);
            subTmp.raycastTarget = false;

            // 2c. Altın Coin Görseli (Hiyerarşide açıkça görünen Coin objesi)
            GameObject bulkCoinObj = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            bulkCoinObj.transform.SetParent(bulkBtnObj.transform, false);
            RectTransform bulkCoinRt = bulkCoinObj.GetComponent<RectTransform>();
            bulkCoinRt.anchorMin = new Vector2(0.5f, 0.26f);
            bulkCoinRt.anchorMax = new Vector2(0.5f, 0.26f);
            bulkCoinRt.pivot = new Vector2(0.5f, 0.5f);
            bulkCoinRt.sizeDelta = new Vector2(26f, 26f);
            bulkCoinRt.anchoredPosition = new Vector2(-60f, 0f); // Fiyatın hemen yanında (500 ile indirim arası)

            Image bulkCoinImg = bulkCoinObj.GetComponent<Image>();
            if (goldCoinSprite != null)
            {
                bulkCoinImg.sprite = goldCoinSprite;
                bulkCoinImg.preserveAspect = true;
                bulkCoinImg.color = Color.white;
            }
            else
            {
                bulkCoinImg.color = new Color(1f, 0.84f, 0.2f, 1f);
            }
            bulkCoinImg.raycastTarget = false;

            SavePrefab(go, path);
        }

        // =========================================================================
        // 4. İksir Satır Plakası
        // =========================================================================
        private static void BuildRowPrefab(Sprite rowSprite, Sprite slotSprite, Sprite buyBtnSprite, Sprite sealStampSprite, Sprite goldCoinSprite, TMP_FontAsset font)
        {
            string path = $"{PREFAB_DIR}/Lisans_Satır.prefab";

            GameObject rowObj = new GameObject("Lisans_Satır", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            RectTransform rt = rowObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(580f, 76f);

            LayoutElement le = rowObj.GetComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 70f;
            le.flexibleWidth = 1f;

            Image rowImg = rowObj.GetComponent<Image>();
            rowImg.sprite = rowSprite;
            rowImg.type = Image.Type.Sliced;
            rowImg.color = Color.white;
            rowImg.raycastTarget = false;

            HorizontalLayoutGroup hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. SlotFrame — büyük belirgin çerçeve
            GameObject slotObj = new GameObject("SlotFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObj.transform.SetParent(rowObj.transform, false);
            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(60f, 60f);
            LayoutElement slotLE = slotObj.GetComponent<LayoutElement>();
            slotLE.preferredWidth = 60f;
            slotLE.preferredHeight = 60f;

            Image slotImg = slotObj.GetComponent<Image>();
            slotImg.sprite = slotSprite;
            slotImg.color = Color.white;
            slotImg.raycastTarget = false;

            // 1a. PotionIcon — geniş anchor alanı ile belirgin ikon
            GameObject iconObj = new GameObject("PotionIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.08f, 0.08f);
            iconRt.anchorMax = new Vector2(0.92f, 0.92f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = Color.white;

            // 2. İsim Metni
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObj.transform.SetParent(rowObj.transform, false);
            LayoutElement nameLE = nameObj.GetComponent<LayoutElement>();
            nameLE.preferredWidth = 180f;
            nameLE.flexibleWidth = 1f;

            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            if (font != null) nameTmp.font = font;
            nameTmp.text = "İksir Adı";
            nameTmp.fontSize = 19f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;

            // 3. Fiyat ve Altın Coin Alanı
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
            if (font != null) costTmp.font = font;
            costTmp.text = "100";
            costTmp.fontSize = 18f;
            costTmp.fontStyle = FontStyles.Bold;
            costTmp.color = new Color(1f, 0.84f, 0.32f, 1f);
            costTmp.alignment = TextAlignmentOptions.Left;
            costTmp.raycastTarget = false;

            // 3b. Altın Coin Görseli (Hiyerarşide açıkça görünen Coin objesi, fiyatın sağında)
            GameObject coinObj = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            coinObj.transform.SetParent(costAreaObj.transform, false);
            RectTransform coinRt = coinObj.GetComponent<RectTransform>();
            coinRt.sizeDelta = new Vector2(26f, 26f);

            Image coinImg = coinObj.GetComponent<Image>();
            if (goldCoinSprite != null)
            {
                coinImg.sprite = goldCoinSprite;
                coinImg.preserveAspect = true;
                coinImg.color = Color.white;
            }
            else
            {
                coinImg.color = new Color(1f, 0.84f, 0.2f, 1f);
            }
            coinImg.raycastTarget = false;

            // 4. ActionArea
            GameObject actionAreaObj = new GameObject("ActionArea", typeof(RectTransform), typeof(LayoutElement));
            actionAreaObj.transform.SetParent(rowObj.transform, false);
            LayoutElement actionLE = actionAreaObj.GetComponent<LayoutElement>();
            actionLE.preferredWidth = 150f;
            actionLE.preferredHeight = 52f;
            actionLE.minWidth = 140f;

            // 4a. BuyBtn
            GameObject btnObj = new GameObject("BuyBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(actionAreaObj.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.sprite = buyBtnSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
            btnImg.pixelsPerUnitMultiplier = 1.6f;
            btnImg.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.60f, 1f);
            btn.colors = cb;

            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btrt = btnTextObj.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = Vector2.zero;
            btrt.offsetMax = Vector2.zero;

            TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
            if (font != null) btnTmp.font = font;
            btnTmp.text = "Lisans Al";
            btnTmp.fontSize = 16f;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = new Color(1f, 0.95f, 0.82f, 1f);
            btnTmp.raycastTarget = false;

            // 4b. UnlockedBadge
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

            GameObject sealObj = new GameObject("SealIcon", typeof(RectTransform), typeof(Image));
            sealObj.transform.SetParent(unlockedObj.transform, false);
            RectTransform sealRt = sealObj.GetComponent<RectTransform>();
            sealRt.sizeDelta = new Vector2(44f, 44f);

            Image sealImg = sealObj.GetComponent<Image>();
            sealImg.sprite = sealStampSprite;
            sealImg.preserveAspect = true;
            sealImg.color = Color.white;
            sealImg.raycastTarget = false;

            GameObject unlTextObj = new GameObject("SealText", typeof(RectTransform), typeof(TextMeshProUGUI));
            unlTextObj.transform.SetParent(unlockedObj.transform, false);
            RectTransform unlTrt = unlTextObj.GetComponent<RectTransform>();
            unlTrt.sizeDelta = new Vector2(85f, 32f);

            TextMeshProUGUI unlTmp = unlTextObj.GetComponent<TextMeshProUGUI>();
            if (font != null) unlTmp.font = font;
            unlTmp.text = "LİSANSLI";
            unlTmp.fontSize = 16f;
            unlTmp.fontStyle = FontStyles.Bold;
            unlTmp.alignment = TextAlignmentOptions.Left;
            unlTmp.color = new Color(0.70f, 0.98f, 0.75f, 1f);
            unlTmp.raycastTarget = false;

            unlockedObj.SetActive(false); // Başlangıçta kilitli görünür

            SavePrefab(rowObj, path);
        }

        // =========================================================================
        // 5. Sayfa Kapsayıcı Prefabı
        // =========================================================================
        private static void BuildPagePrefab()
        {
            string path = $"{PREFAB_DIR}/Lisans_Sayfa.prefab";

            GameObject pageObj = new GameObject("Lisans_Sayfa", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform pageRt = pageObj.GetComponent<RectTransform>();
            pageRt.sizeDelta = new Vector2(736f, 760f);

            VerticalLayoutGroup vlg = pageObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 10, 10);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            SavePrefab(pageObj, path);
        }

        // =========================================================================
        // 6. Tam Lisans Parşömeni Popup Prefabı
        // =========================================================================
        private static void BuildCompleteParchmentPrefab(Sprite closeSealSprite)
        {
            string path = $"{PREFAB_DIR}/LisansParşomeni.prefab";

            // Temel Parşomen 1 1 prefabından kopyalayarak oluştur
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Parşomen 1 1.prefab");
            GameObject popup = null;
            if (basePrefab != null)
            {
                popup = Object.Instantiate(basePrefab);
                popup.name = "LisansParşomeni";
            }
            else
            {
                popup = new GameObject("LisansParşomeni", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            // Canvas ve Scaler ayarları
            Canvas canvas = popup.GetComponent<Canvas>();
            if (canvas == null) canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = popup.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = popup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // LicenseParchmentUI bileşeni ekle
            if (popup.GetComponent<LicenseParchmentUI>() == null)
            {
                popup.AddComponent<LicenseParchmentUI>();
            }

            // Kapatma Mührü Ekle
            Transform existingClose = popup.transform.Find("ParchmentCloseBtn");
            if (existingClose == null)
            {
                GameObject closeBtnObj = new GameObject("ParchmentCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                closeBtnObj.transform.SetParent(popup.transform, false);
                RectTransform crt = closeBtnObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(440f, 390f);
                crt.sizeDelta = new Vector2(52f, 52f);

                Image cImg = closeBtnObj.GetComponent<Image>();
                cImg.sprite = closeSealSprite;
                cImg.preserveAspect = true;
                cImg.raycastTarget = true;
            }

            SavePrefab(popup, path);
        }

        // =========================================================================
        // 7. Envanter Eşya Detay Popup Prefabı
        // =========================================================================
        private static void BuildItemDetailPopupPrefab(Sprite slotSprite, TMP_FontAsset font)
        {
            string path = $"{INVENTORY_PREFAB_DIR}/ItemDetailPopup.prefab";

            GameObject root = new GameObject("ItemDetailPopup", typeof(RectTransform), typeof(Image), typeof(ItemDetailPopup));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 440f);
            rt.anchoredPosition = Vector2.zero;

            Image bg = root.GetComponent<Image>();
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Envanter.png");
            if (bgSprite != null) { bg.sprite = bgSprite; bg.color = Color.white; }
            else { bg.color = new Color(0.15f, 0.11f, 0.08f, 0.98f); }

            ItemDetailPopup popup = root.GetComponent<ItemDetailPopup>();
            popup.panelRoot = root;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(root.transform, false);
            RectTransform crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(25f, 25f);
            crt.offsetMax = new Vector2(-25f, -25f);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (font != null) titleTmp.font = font;
            titleTmp.text = "Eşya Adı";
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.96f, 0.85f, 0.55f, 1f);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35f);
            popup.itemNameText = titleTmp;

            // Slot Frame & Icon
            GameObject iconSlotObj = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
            iconSlotObj.transform.SetParent(contentObj.transform, false);
            RectTransform slotRt = iconSlotObj.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(85f, 85f);
            Image slotBg = iconSlotObj.GetComponent<Image>();
            slotBg.sprite = slotSprite;
            slotBg.color = Color.white;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(iconSlotObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(6f, 6f);
            irt.offsetMax = new Vector2(-6f, -6f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            popup.itemIconImage = iconImg;

            // Type Text
            GameObject typeObj = new GameObject("TypeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI typeTmp = typeObj.GetComponent<TextMeshProUGUI>();
            if (font != null) typeTmp.font = font;
            typeTmp.text = "Tür: -";
            typeTmp.fontSize = 16f;
            typeTmp.alignment = TextAlignmentOptions.Center;
            typeTmp.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            typeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24f);
            popup.itemTypeText = typeTmp;

            // Value Text
            GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI valTmp = valObj.GetComponent<TextMeshProUGUI>();
            if (font != null) valTmp.font = font;
            valTmp.text = "Değer: - Altın";
            valTmp.fontSize = 16f;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.alignment = TextAlignmentOptions.Center;
            valTmp.color = new Color(1f, 0.85f, 0.3f, 1f);
            valObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24f);
            popup.itemValueText = valTmp;

            // Owned Count Text
            GameObject countObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI countTmp = countObj.GetComponent<TextMeshProUGUI>();
            if (font != null) countTmp.font = font;
            countTmp.text = "Envanterde: 0 Adet";
            countTmp.fontSize = 16f;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            countObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24f);
            popup.ownedCountText = countTmp;

            // Buttons
            Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/General Buton.png");
            popup.showRecipesButton = CreatePopupBtn(contentObj.transform, "ShowRecipesBtn", "Tarifleri Gör", new Vector2(240f, 40f), btnSprite, font);
            popup.assignToShelfButton = CreatePopupBtn(contentObj.transform, "AssignToShelfBtn", "Rafa Yerleştir", new Vector2(240f, 40f), btnSprite, font);

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(root.transform, false);
            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-10f, -10f);
            closeRt.sizeDelta = new Vector2(34f, 34f);

            Image cImg = closeBtnObj.GetComponent<Image>();
            cImg.color = new Color(0.6f, 0.2f, 0.2f, 1f);

            GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform ctRt = closeTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
            TextMeshProUGUI cTmp = closeTextObj.GetComponent<TextMeshProUGUI>();
            cTmp.text = "X";
            cTmp.fontSize = 20f;
            cTmp.alignment = TextAlignmentOptions.Center;
            cTmp.color = Color.white;

            popup.closeButton = closeBtnObj.GetComponent<Button>();

            SavePrefab(root, path);
        }

        private static Button CreatePopupBtn(Transform parent, string name, string text, Vector2 size, Sprite sprite, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
            else { img.color = new Color(0.7f, 0.45f, 0.2f, 1f); }

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj.GetComponent<Button>();
        }

        [MenuItem("Tools/Potion Shop/Mevcut Lisans Prefabına Toplu Satın Alma Coin ve Yazılarını Ekle", priority = 107)]
        public static void UpdateExistingLicensePrefabBulkBuyMenu()
        {
            UpdateExistingLicensePrefabBulkBuy();
        }

        public static void UpdateExistingLicensePrefabBulkBuy()
        {
            string[] prefabPaths = new string[]
            {
                "Assets/_PotionTown/Prefabs/UI/License/License_Prefab.prefab",
                "Assets/_PotionTown/Resources/LicenseUI/License_Prefab.prefab"
            };

            Sprite goldCoinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Gold Coin.png");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset");

            foreach (string path in prefabPaths)
            {
                if (!System.IO.File.Exists(path)) continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null) continue;

                try
                {
                    Transform sayfa = prefabRoot.transform.Find("Sayfa");
                    if (sayfa == null) sayfa = prefabRoot.transform;

                    Transform bulkT = sayfa.Find("BulkBuyBtn");
                    if (bulkT != null)
                    {
                        // 1. Title Text
                        Transform textT = bulkT.Find("Text");
                        if (textT != null)
                        {
                            RectTransform trt = textT.GetComponent<RectTransform>();
                            trt.anchorMin = new Vector2(0f, 0.42f);
                            trt.anchorMax = new Vector2(1f, 0.95f);
                            trt.offsetMin = new Vector2(20f, 0f);
                            trt.offsetMax = new Vector2(-20f, 0f);

                            TextMeshProUGUI tmp = textT.GetComponent<TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                tmp.text = "HEPSİNİ BİRLİKTE AL";
                                tmp.alignment = TextAlignmentOptions.Center;
                                tmp.color = new Color(1f, 0.90f, 0.55f, 1f);
                                tmp.fontSize = 24f;
                                tmp.fontStyle = FontStyles.Bold;
                                if (font != null) tmp.font = font;
                            }
                        }

                        // 2. SubText
                        Transform subTextT = bulkT.Find("SubText");
                        if (subTextT == null)
                        {
                            GameObject subTextObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
                            subTextObj.transform.SetParent(bulkT, false);
                            subTextT = subTextObj.transform;
                        }

                        RectTransform subTrt = subTextT.GetComponent<RectTransform>();
                        subTrt.anchorMin = new Vector2(0f, 0.08f);
                        subTrt.anchorMax = new Vector2(1f, 0.45f);
                        subTrt.offsetMin = new Vector2(20f, 0f);
                        subTrt.offsetMax = new Vector2(-20f, 0f);

                        TextMeshProUGUI subTmp = subTextT.GetComponent<TextMeshProUGUI>();
                        if (subTmp != null)
                        {
                            if (font != null) subTmp.font = font;
                            subTmp.text = "500       (%10 İndirim)";
                            subTmp.fontSize = 24f; // Text ile birebir aynı boyut
                            subTmp.fontStyle = FontStyles.Bold; // Text ile birebir aynı stil
                            subTmp.alignment = TextAlignmentOptions.Center;
                            subTmp.color = new Color(1f, 0.94f, 0.78f, 1f);
                            subTmp.raycastTarget = false;
                        }

                        // 3. Coin
                        Transform coinT = bulkT.Find("Coin");
                        if (coinT == null)
                        {
                            GameObject coinObj = new GameObject("Coin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                            coinObj.transform.SetParent(bulkT, false);
                            coinT = coinObj.transform;
                        }

                        RectTransform coinRt = coinT.GetComponent<RectTransform>();
                        coinRt.anchorMin = new Vector2(0.5f, 0.26f);
                        coinRt.anchorMax = new Vector2(0.5f, 0.26f);
                        coinRt.pivot = new Vector2(0.5f, 0.5f);
                        coinRt.localScale = subTrt.localScale;
                        coinRt.sizeDelta = new Vector2(26f, 26f);
                        coinRt.anchoredPosition = new Vector2(-221f, 64f);

                        Image coinImg = coinT.GetComponent<Image>();
                        if (coinImg != null)
                        {
                            if (goldCoinSprite != null) coinImg.sprite = goldCoinSprite;
                            coinImg.preserveAspect = true;
                            coinImg.raycastTarget = false;
                        }
                    }

                    // Satırlardaki Coin'leri kontrol et
                    for (int i = 0; i < 5; i++)
                    {
                        string rowName = i == 0 ? "Satır" : $"Satır ({i})";
                        Transform rT = sayfa.Find(rowName);
                        if (rT == null) continue;

                        string costName = i == 0 ? "Cost" : $"Cost ({i})";
                        Transform costT = rT.Find(costName);
                        if (costT != null)
                        {
                            string coinName = i == 0 ? "Coin" : $"Coin ({i})";
                            Transform cT = costT.Find(coinName) ?? costT.Find("Coin");
                            if (cT == null)
                            {
                                GameObject cObj = new GameObject(coinName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                                cObj.transform.SetParent(costT, false);
                                cT = cObj.transform;
                            }
                            Image cImg = cT.GetComponent<Image>();
                            if (cImg != null && goldCoinSprite != null)
                            {
                                cImg.sprite = goldCoinSprite;
                                cImg.preserveAspect = true;
                                cImg.raycastTarget = false;
                            }
                            RectTransform cRt = cT.GetComponent<RectTransform>();
                            if (cRt != null)
                            {
                                cRt.anchorMin = new Vector2(0.5f, 0.5f);
                                cRt.anchorMax = new Vector2(0.5f, 0.5f);
                                cRt.pivot = new Vector2(0.5f, 0.5f);
                                cRt.anchoredPosition = new Vector2(48f, cRt.anchoredPosition.y != 0 ? cRt.anchoredPosition.y : 0f);
                                cRt.sizeDelta = new Vector2(512f, 512f);
                                cRt.localScale = new Vector3(0.0537778f, 0.0537778f, 0.0537778f);
                            }
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"<color=green>[LisansPrefabBuilder]</color> {path} başarıyla güncellendi (BulkBuyBtn: Text, SubText, Coin eklendi)!");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=cyan>[LisansPrefabBuilder]</color> Prefab oluşturuldu: {path}");
        }
    }
}
