#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using PotionShop.UI;
using UnityEditor.SceneManagement;

namespace PotionShop.Editor
{
    public static class EndOfDayPanelBuilder
    {
        private const string PrefabDir = "Assets/_PotionTown/Prefabs/UI/Menus";
        private const string PrefabPath = "Assets/_PotionTown/Prefabs/UI/Menus/EndOfDayPanel.prefab";

        private const string GuidParchment = "9b1fa501532c11d48ad8d3685dc36211"; // Parşomen.png
        private const string GuidHeaderBanner = "f4c8adc4cd3e45d47b16e1fc81d4aadb"; // License_Header_Banner.png
        private const string GuidRowPlate = "0bfea1ea152f0264ca14e9a0cba9c1ac"; // License_Row_Plate.png
        private const string GuidDivider = "67e7222d2bec55b44a207a32327f3bba"; // License_Divider.png
        private const string GuidBuyButton = "4f3d7761a3766d54caeec9d065d33dd3"; // License_Buy_Button.png
        private const string GuidCloseSeal = "f401702c121253c4c9b34c636d70c3dd"; // License_Close_Seal.png
        private const string GuidFont = "8904e588fdfe5274998b8a5a195864b5"; // NewRocker-Regular SDF.asset

        [MenuItem("Tools/PotionTown/Rebuild EndOfDayPanel Prefab", false, 10)]
        public static GameObject CreateOrRebuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs/UI", "Menus");
            }

            Sprite parchmentSprite = LoadSprite(GuidParchment);
            Sprite headerSprite = LoadSprite(GuidHeaderBanner);
            Sprite rowPlateSprite = LoadSprite(GuidRowPlate);
            Sprite dividerSprite = LoadSprite(GuidDivider);
            Sprite buttonSprite = LoadSprite(GuidBuyButton);
            Sprite closeSprite = LoadSprite(GuidCloseSeal);
            TMP_FontAsset tavernFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(GuidFont));

            // --- 1. ROOT GAMEOBJECT ---
            GameObject root = new GameObject("EndOfDayPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(EndOfDayPanel));
            
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 2200;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup cGroup = root.GetComponent<CanvasGroup>();
            cGroup.alpha = 1f;

            EndOfDayPanel panelComp = root.GetComponent<EndOfDayPanel>();
            panelComp.canvasGroup = cGroup;
            panelComp.animateOpen = true;
            panelComp.lineDelay = 0.18f;

            // --- 2. DARK OVERLAY ---
            GameObject overlay = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            overlayRT.pivot = new Vector2(0.5f, 0.5f);

            Image overlayImg = overlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.68f);
            overlayImg.raycastTarget = true;

            // --- 3. DIALOG WINDOW (PARCHMENT CARD) ---
            GameObject window = new GameObject("DialogWindow", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(root.transform, false);
            RectTransform windowRT = window.GetComponent<RectTransform>();
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.anchoredPosition = Vector2.zero;
            windowRT.sizeDelta = new Vector2(740f, 800f);

            Image windowImg = window.GetComponent<Image>();
            windowImg.sprite = parchmentSprite;
            windowImg.color = Color.white;
            windowImg.raycastTarget = true;

            panelComp.dialogWindow = windowRT;
            panelComp.panelRect = windowRT;

            // --- 4. HEADER BANNER ---
            GameObject header = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(window.transform, false);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.5f, 1f);
            headerRT.anchorMax = new Vector2(0.5f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0f, 22f);
            headerRT.sizeDelta = new Vector2(620f, 105f);

            Image headerImg = header.GetComponent<Image>();
            headerImg.sprite = headerSprite;
            headerImg.color = Color.white;
            headerImg.preserveAspect = true;

            // Title Text
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.25f);
            titleRT.anchorMax = new Vector2(1f, 0.95f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "📜 GÜN 1 ÖZETİ 📜";
            titleTMP.fontSize = 32f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.91f, 0.62f); // Warm Radiant Gold
            titleTMP.alignment = TextAlignmentOptions.Center;
            if (tavernFont != null) titleTMP.font = tavernFont;
            panelComp.titleText = titleTMP;

            // Subtitle Text
            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(header.transform, false);
            RectTransform subRT = subObj.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 0f);
            subRT.anchorMax = new Vector2(1f, 0.35f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;

            TextMeshProUGUI subTMP = subObj.GetComponent<TextMeshProUGUI>();
            subTMP.text = "Dükkan Gün Sonu Raporu";
            subTMP.fontSize = 15f;
            subTMP.color = new Color(0.9f, 0.84f, 0.72f);
            subTMP.alignment = TextAlignmentOptions.Center;
            if (tavernFont != null) subTMP.font = tavernFont;
            panelComp.subtitleText = subTMP;

            // --- 5. CLOSE SEAL BUTTON ---
            GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(window.transform, false);
            RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(0.5f, 0.5f);
            closeRT.anchoredPosition = new Vector2(-22f, -22f);
            closeRT.sizeDelta = new Vector2(50f, 50f);

            Image closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.sprite = closeSprite;
            closeImg.color = Color.white;
            closeImg.preserveAspect = true;

            Button closeBtn = closeBtnObj.GetComponent<Button>();
            panelComp.closeButton = closeBtn;

            // --- 6. CONTENT CARDS ---

            // === CARD A: CUSTOMER REPORT ===
            GameObject custCard = new GameObject("CustomerCard", typeof(RectTransform), typeof(Image));
            custCard.transform.SetParent(window.transform, false);
            RectTransform custRT = custCard.GetComponent<RectTransform>();
            custRT.anchorMin = new Vector2(0.5f, 1f);
            custRT.anchorMax = new Vector2(0.5f, 1f);
            custRT.pivot = new Vector2(0.5f, 1f);
            custRT.anchoredPosition = new Vector2(0f, -100f);
            custRT.sizeDelta = new Vector2(640f, 250f);

            Image custImg = custCard.GetComponent<Image>();
            custImg.sprite = rowPlateSprite;
            custImg.color = new Color(1f, 1f, 1f, 0.95f);

            // Card Header: Müşteri Raporu
            GameObject cTitleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTitleObj.transform.SetParent(custCard.transform, false);
            RectTransform cTitleRT = cTitleObj.GetComponent<RectTransform>();
            cTitleRT.anchorMin = new Vector2(0f, 1f);
            cTitleRT.anchorMax = new Vector2(1f, 1f);
            cTitleRT.pivot = new Vector2(0.5f, 1f);
            cTitleRT.anchoredPosition = new Vector2(0f, -14f);
            cTitleRT.sizeDelta = new Vector2(600f, 28f);

            TextMeshProUGUI cTitleTMP = cTitleObj.GetComponent<TextMeshProUGUI>();
            cTitleTMP.text = "MÜŞTERİ RAPORU & MEMNUNİYET";
            cTitleTMP.fontSize = 19f;
            cTitleTMP.fontStyle = FontStyles.Bold;
            cTitleTMP.color = new Color(0.94f, 0.77f, 0.44f);
            cTitleTMP.alignment = TextAlignmentOptions.Center;
            if (tavernFont != null) cTitleTMP.font = tavernFont;

            // Total Customers Line
            GameObject totalObj = new GameObject("TotalCustomersText", typeof(RectTransform), typeof(TextMeshProUGUI));
            totalObj.transform.SetParent(custCard.transform, false);
            RectTransform totalRT = totalObj.GetComponent<RectTransform>();
            totalRT.anchorMin = new Vector2(0f, 1f);
            totalRT.anchorMax = new Vector2(1f, 1f);
            totalRT.pivot = new Vector2(0.5f, 1f);
            totalRT.anchoredPosition = new Vector2(0f, -48f);
            totalRT.sizeDelta = new Vector2(600f, 28f);

            TextMeshProUGUI totalTMP = totalObj.GetComponent<TextMeshProUGUI>();
            totalTMP.text = "Bugün Ağırlanan Toplam: <b><color=#FFF7D6>0</color></b> Müşteri";
            totalTMP.fontSize = 18f;
            totalTMP.color = new Color(0.93f, 0.88f, 0.80f);
            totalTMP.alignment = TextAlignmentOptions.Center;
            panelComp.totalCustomersText = totalTMP;

            // Mood Row Container (Horizontal)
            GameObject moodRow = new GameObject("MoodContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            moodRow.transform.SetParent(custCard.transform, false);
            RectTransform moodRT = moodRow.GetComponent<RectTransform>();
            moodRT.anchorMin = new Vector2(0.05f, 0.5f);
            moodRT.anchorMax = new Vector2(0.95f, 0.5f);
            moodRT.pivot = new Vector2(0.5f, 0.5f);
            moodRT.anchoredPosition = new Vector2(0f, -18f);
            moodRT.sizeDelta = new Vector2(0f, 44f);

            HorizontalLayoutGroup moodHlg = moodRow.GetComponent<HorizontalLayoutGroup>();
            moodHlg.childAlignment = TextAnchor.MiddleCenter;
            moodHlg.spacing = 16f;
            moodHlg.childControlWidth = true;
            moodHlg.childControlHeight = true;
            moodHlg.childForceExpandWidth = true;
            moodHlg.childForceExpandHeight = true;

            panelComp.happyText = CreateMoodBadge(moodRow.transform, "HappyBadge", "Mutlu: <color=#2ECC71><b>0</b></color>", new Color(0.18f, 0.8f, 0.44f));
            panelComp.normalText = CreateMoodBadge(moodRow.transform, "NormalBadge", "Normal: <color=#F1C40F><b>0</b></color>", new Color(0.95f, 0.77f, 0.06f));
            panelComp.grumpyText = CreateMoodBadge(moodRow.transform, "GrumpyBadge", "Huysuz: <color=#E67E22><b>0</b></color>", new Color(0.9f, 0.5f, 0.13f));
            panelComp.angryText = CreateMoodBadge(moodRow.transform, "AngryBadge", "Sinirli: <color=#E74C3C><b>0</b></color>", new Color(0.91f, 0.3f, 0.24f));

            // Emoji Summary Text
            GameObject emojiObj = new GameObject("EmojiSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            emojiObj.transform.SetParent(custCard.transform, false);
            RectTransform emojiRT = emojiObj.GetComponent<RectTransform>();
            emojiRT.anchorMin = new Vector2(0f, 0f);
            emojiRT.anchorMax = new Vector2(1f, 0f);
            emojiRT.pivot = new Vector2(0.5f, 0f);
            emojiRT.anchoredPosition = new Vector2(0f, 16f);
            emojiRT.sizeDelta = new Vector2(600f, 32f);

            TextMeshProUGUI emojiTMP = emojiObj.GetComponent<TextMeshProUGUI>();
            emojiTMP.text = "";
            emojiTMP.fontSize = 20f;
            emojiTMP.alignment = TextAlignmentOptions.Center;
            emojiObj.SetActive(false);
            panelComp.emojiSummaryText = emojiTMP;

            // === DIVIDER ===
            GameObject dividerObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerObj.transform.SetParent(window.transform, false);
            RectTransform divRT = dividerObj.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.5f, 1f);
            divRT.anchorMax = new Vector2(0.5f, 1f);
            divRT.pivot = new Vector2(0.5f, 1f);
            divRT.anchoredPosition = new Vector2(0f, -365f);
            divRT.sizeDelta = new Vector2(620f, 16f);

            Image divImg = dividerObj.GetComponent<Image>();
            divImg.sprite = dividerSprite;
            divImg.color = new Color(1f, 1f, 1f, 0.85f);
            divImg.preserveAspect = true;

            // === CARD B: FINANCIAL LEDGER ===
            GameObject finCard = new GameObject("FinancialCard", typeof(RectTransform), typeof(Image));
            finCard.transform.SetParent(window.transform, false);
            RectTransform finRT = finCard.GetComponent<RectTransform>();
            finRT.anchorMin = new Vector2(0.5f, 1f);
            finRT.anchorMax = new Vector2(0.5f, 1f);
            finRT.pivot = new Vector2(0.5f, 1f);
            finRT.anchoredPosition = new Vector2(0f, -395f);
            finRT.sizeDelta = new Vector2(640f, 245f);

            Image finImg = finCard.GetComponent<Image>();
            finImg.sprite = rowPlateSprite;
            finImg.color = new Color(1f, 1f, 1f, 0.95f);

            // Card Header: Mali Bilanço
            GameObject fTitleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            fTitleObj.transform.SetParent(finCard.transform, false);
            RectTransform fTitleRT = fTitleObj.GetComponent<RectTransform>();
            fTitleRT.anchorMin = new Vector2(0f, 1f);
            fTitleRT.anchorMax = new Vector2(1f, 1f);
            fTitleRT.pivot = new Vector2(0.5f, 1f);
            fTitleRT.anchoredPosition = new Vector2(0f, -14f);
            fTitleRT.sizeDelta = new Vector2(600f, 28f);

            TextMeshProUGUI fTitleTMP = fTitleObj.GetComponent<TextMeshProUGUI>();
            fTitleTMP.text = "MALİ BİLANÇO & KASA";
            fTitleTMP.fontSize = 19f;
            fTitleTMP.fontStyle = FontStyles.Bold;
            fTitleTMP.color = new Color(0.94f, 0.77f, 0.44f);
            fTitleTMP.alignment = TextAlignmentOptions.Center;
            if (tavernFont != null) fTitleTMP.font = tavernFont;

            // Earned Gold Text
            GameObject earnedObj = new GameObject("EarnedGoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            earnedObj.transform.SetParent(finCard.transform, false);
            RectTransform earnedRT = earnedObj.GetComponent<RectTransform>();
            earnedRT.anchorMin = new Vector2(0.08f, 0.5f);
            earnedRT.anchorMax = new Vector2(0.92f, 0.5f);
            earnedRT.pivot = new Vector2(0.5f, 0.5f);
            earnedRT.anchoredPosition = new Vector2(0f, 42f);
            earnedRT.sizeDelta = new Vector2(0f, 32f);

            TextMeshProUGUI earnedTMP = earnedObj.GetComponent<TextMeshProUGUI>();
            earnedTMP.text = "Kazanılan Gelir: <color=#2ECC71><b>+0 Altın</b></color>";
            earnedTMP.fontSize = 19f;
            earnedTMP.color = new Color(0.92f, 0.88f, 0.82f);
            earnedTMP.alignment = TextAlignmentOptions.MidlineLeft;
            panelComp.earnedGoldText = earnedTMP;

            // Lost Gold Text
            GameObject lostObj = new GameObject("LostGoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            lostObj.transform.SetParent(finCard.transform, false);
            RectTransform lostRT = lostObj.GetComponent<RectTransform>();
            lostRT.anchorMin = new Vector2(0.08f, 0.5f);
            lostRT.anchorMax = new Vector2(0.92f, 0.5f);
            lostRT.pivot = new Vector2(0.5f, 0.5f);
            lostRT.anchoredPosition = new Vector2(0f, 6f);
            lostRT.sizeDelta = new Vector2(0f, 32f);

            TextMeshProUGUI lostTMP = lostObj.GetComponent<TextMeshProUGUI>();
            lostTMP.text = "Kaçan / Kaybedilen: <color=#E74C3C><b>-0 Altın</b></color>";
            lostTMP.fontSize = 19f;
            lostTMP.color = new Color(0.92f, 0.88f, 0.82f);
            lostTMP.alignment = TextAlignmentOptions.MidlineLeft;
            panelComp.lostGoldText = lostTMP;

            // Net Profit Background Plate / Box
            GameObject netBox = new GameObject("NetProfitBox", typeof(RectTransform), typeof(Image));
            netBox.transform.SetParent(finCard.transform, false);
            RectTransform netBoxRT = netBox.GetComponent<RectTransform>();
            netBoxRT.anchorMin = new Vector2(0.06f, 0f);
            netBoxRT.anchorMax = new Vector2(0.94f, 0f);
            netBoxRT.pivot = new Vector2(0.5f, 0f);
            netBoxRT.anchoredPosition = new Vector2(0f, 14f);
            netBoxRT.sizeDelta = new Vector2(0f, 48f);

            Image netBoxImg = netBox.GetComponent<Image>();
            netBoxImg.color = new Color(0.12f, 0.10f, 0.08f, 0.55f); // Soft dark frame

            GameObject netObj = new GameObject("NetProfitText", typeof(RectTransform), typeof(TextMeshProUGUI));
            netObj.transform.SetParent(netBox.transform, false);
            RectTransform netRT = netObj.GetComponent<RectTransform>();
            netRT.anchorMin = Vector2.zero;
            netRT.anchorMax = Vector2.one;
            netRT.offsetMin = new Vector2(16f, 0f);
            netRT.offsetMax = new Vector2(-16f, 0f);

            TextMeshProUGUI netTMP = netObj.GetComponent<TextMeshProUGUI>();
            netTMP.text = "Günlük Net Kâr: <color=#FFD700><b>0 Altın</b></color>";
            netTMP.fontSize = 22f;
            netTMP.fontStyle = FontStyles.Bold;
            netTMP.color = new Color(1f, 0.95f, 0.8f);
            netTMP.alignment = TextAlignmentOptions.Midline;
            panelComp.netProfitText = netTMP;

            // --- 7. NEXT DAY BUTTON ---
            GameObject nextBtnObj = new GameObject("NextDayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnObj.transform.SetParent(window.transform, false);
            RectTransform nextBtnRT = nextBtnObj.GetComponent<RectTransform>();
            nextBtnRT.anchorMin = new Vector2(0.5f, 0f);
            nextBtnRT.anchorMax = new Vector2(0.5f, 0f);
            nextBtnRT.pivot = new Vector2(0.5f, 0f);
            nextBtnRT.anchoredPosition = new Vector2(0f, 40f);
            nextBtnRT.sizeDelta = new Vector2(340f, 66f);

            Image nextBtnImg = nextBtnObj.GetComponent<Image>();
            nextBtnImg.sprite = buttonSprite;
            nextBtnImg.color = Color.white;
            nextBtnImg.type = Image.Type.Simple;
            nextBtnImg.preserveAspect = false;

            Button nextBtn = nextBtnObj.GetComponent<Button>();
            panelComp.nextDayButton = nextBtn;

            GameObject nextTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            nextTxtObj.transform.SetParent(nextBtnObj.transform, false);
            RectTransform nextTxtRT = nextTxtObj.GetComponent<RectTransform>();
            nextTxtRT.anchorMin = Vector2.zero;
            nextTxtRT.anchorMax = Vector2.one;
            nextTxtRT.offsetMin = Vector2.zero;
            nextTxtRT.offsetMax = Vector2.zero;

            TextMeshProUGUI nextTMP = nextTxtObj.GetComponent<TextMeshProUGUI>();
            nextTMP.text = "Yeni Güne Başla";
            nextTMP.fontSize = 24f;
            nextTMP.fontStyle = FontStyles.Bold;
            nextTMP.color = new Color(1f, 0.97f, 0.88f);
            nextTMP.alignment = TextAlignmentOptions.Center;
            if (tavernFont != null) nextTMP.font = tavernFont;

            // Başlangıçta paneli kapalı (inaktif) yap
            root.SetActive(false);

            // Prefab olarak kaydet
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"<color=green>[EndOfDayPanelBuilder] Başarıyla güncellendi ve prefab kaydedildi: {PrefabPath}</color>");
            return savedPrefab;
        }

        private static TextMeshProUGUI CreateMoodBadge(Transform parent, string name, string text, Color barColor)
        {
            GameObject badge = new GameObject(name, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(parent, false);

            Image img = badge.GetComponent<Image>();
            img.color = new Color(0.12f, 0.10f, 0.08f, 0.5f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(badge.transform, false);
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8f, 0f);
            txtRT.offsetMax = new Vector2(-8f, 0f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private static Sprite LoadSprite(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [InitializeOnLoadMethod]
        private static void AutoInitOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                string triggerPath = "Assets/_PotionTown/Prefabs/UI/Menus/rebuild_endofday.trigger";
                bool hasTrigger = System.IO.File.Exists(triggerPath);
                if (hasTrigger || !System.IO.File.Exists(PrefabPath))
                {
                    if (hasTrigger)
                    {
                        try { System.IO.File.Delete(triggerPath); } catch {}
                    }
                    Debug.Log("<color=cyan>[EndOfDayPanelBuilder]</color> EndOfDayPanel prefab'i Unity yerel motoru ile yeniden inşa ediliyor...");
                    CreateOrRebuildPrefab();
                    DeployToCurrentScene();
                }

                // Doğrulama: Prefab'ı yüklemeyi dene ve logla
                try
                {
                    GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                    if (loaded != null)
                    {
                        Debug.Log($"<color=green>[EndOfDayPanelBuilder] DOĞRULAMA: Prefab hatasız yüklendi! ({loaded.name})</color>");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EndOfDayPanelBuilder] Prefab yüklenemedi: {ex.Message}");
                }
            };
        }

        [MenuItem("Tools/PotionTown/Deploy EndOfDayPanel To Current Scene", false, 11)]
        public static void DeployToCurrentScene()
        {
            // 1. Prefab'i derle veya yükle
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = CreateOrRebuildPrefab();
            }

            // 2. Sahnedeki eski 'Gün sonu paneli' veya 'EndOfDayPanel' nesnelerini bul
            var oldPanels = Object.FindObjectsByType<EndOfDayPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var op in oldPanels)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(op.gameObject)) continue;
                Debug.Log($"[EndOfDayPanelBuilder] Eski sahne paneli kaldırılıyor: {op.gameObject.name}");
                Undo.DestroyObjectImmediate(op.gameObject);
            }

            // Ayrıca tabela altındaki veya sahnedeki eski 'Gün sonu paneli' isimli objeleri de bulup temizle
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (PrefabUtility.IsPartOfPrefabAsset(t.gameObject)) continue;
                if (t.gameObject.name == "Gün sonu paneli" || t.gameObject.name.Contains("G\xFCn sonu paneli"))
                {
                    Debug.Log($"[EndOfDayPanelBuilder] Eski tabela alt paneli kaldırılıyor: {t.gameObject.name}");
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }

            // 3. Prefab'ı sahneye instantiate et
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "EndOfDayPanel";
            instance.SetActive(false);
            Undo.RegisterCreatedObjectUndo(instance, "Deploy EndOfDayPanel Prefab");

            // Global UI kökü varsa oraya koy, yoksa sahnede bırak
            GameObject globalUIRoot = GameObject.Find("--- [03_GLOBAL_UI] ---");
            if (globalUIRoot != null)
            {
                instance.transform.SetParent(globalUIRoot.transform, false);
            }

            // 4. Sahnedeki EndOfDaySign referansını bağla
            var allSigns = Object.FindObjectsByType<EndOfDaySign>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sign in allSigns)
            {
                Undo.RecordObject(sign, "Update EndOfDaySign panel reference");
                sign.endOfDayPanel = instance;
                EditorUtility.SetDirty(sign);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[EndOfDayPanelBuilder] EndOfDayPanel sahneye başarıyla yerleştirildi ve EndOfDaySign'a bağlandı!</color>");
        }
    }
}
#endif
