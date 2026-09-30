#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// SettingsMenu prefabını ve TopDropdownMenu entegrasyonunu otomatik oluşturan Editor aracı.
    /// MenuItems: GameObject > UI > PotionTavern - Ayarlar Menüsü Prefab Oluştur
    /// </summary>
    public static class SettingsMenuBuilder
    {
        // Renk paleti - PotionTown fantezi simyacı teması
        private static readonly Color COLOR_BG_OVERLAY = new Color(0.05f, 0.03f, 0.08f, 0.93f);
        private static readonly Color COLOR_SECTION_BG = new Color(0.12f, 0.08f, 0.05f, 0.75f);
        private static readonly Color COLOR_TAB_ACTIVE = new Color(1f, 0.88f, 0.4f, 1f);
        private static readonly Color COLOR_TAB_INACTIVE = new Color(0.45f, 0.32f, 0.20f, 1f);
        private static readonly Color COLOR_TEXT_WARM = new Color(1f, 0.96f, 0.85f, 1f);
        private static readonly Color COLOR_TEXT_MUTED = new Color(0.78f, 0.70f, 0.58f, 1f);
        private static readonly Color COLOR_GOLD = new Color(1f, 0.88f, 0.4f, 1f);
        private static readonly Color COLOR_BUTTON = new Color(0.38f, 0.24f, 0.14f, 1f);
        private static readonly Color COLOR_INPUT_BG = new Color(0.08f, 0.05f, 0.03f, 0.90f);
        private static readonly Color COLOR_SLIDER_TRACK = new Color(0.18f, 0.12f, 0.08f, 1f);
        private static readonly Color COLOR_SLIDER_FILL = new Color(1f, 0.78f, 0.25f, 1f);

        // Varlık referansları
        private static TMP_FontAsset _tavernFont;
        private static Sprite _btnSprite;
        private static Sprite _panelBgSprite;
        private static Sprite _settingsGearSprite;
        private static Sprite _closeButtonSprite;
        private static Sprite _tabAccountSprite;
        private static Sprite _tabAudioSprite;
        private static Sprite _tabCreditsSprite;
        private static Sprite _musicNoteSprite;
        private static Sprite _sfxHornSprite;
        private static Sprite _toggleOnSprite;
        private static Sprite _toggleOffSprite;

        [MenuItem("GameObject/UI/PotionTavern - Ayarlar Menüsü Prefab Oluştur", false, 11)]
        public static void CreateSettingsMenuPrefab()
        {
            LoadAssets();

            // 1. Root Canvas Objesi
            GameObject root = new GameObject("SettingsMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 3000;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Tam Ekran Karartma Arka Planı
            GameObject bgOverlay = CreateUIObject("BackgroundOverlay", root.transform);
            StretchFull(bgOverlay);
            Image bgImg = bgOverlay.AddComponent<Image>();
            bgImg.color = COLOR_BG_OVERLAY;
            bgImg.raycastTarget = true;

            // 3. Ana Ahşap Pano (16:9 oranında tasarlanmış Settings_Panel_Bg görseli ile)
            GameObject mainPanel = CreateUIObject("MainPanel", bgOverlay.transform);
            RectTransform mainRT = mainPanel.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.07f, 0.05f);
            mainRT.anchorMax = new Vector2(0.93f, 0.95f);
            mainRT.offsetMin = Vector2.zero;
            mainRT.offsetMax = Vector2.zero;
            Image mainPanelImg = mainPanel.AddComponent<Image>();
            mainPanelImg.color = Color.white;
            if (_panelBgSprite != null)
            {
                mainPanelImg.sprite = _panelBgSprite;
                mainPanelImg.preserveAspect = false;
            }

            // 4. Header Alanı: Başlık İkonu + Başlık Yazısı + Kapatma Butonu
            GameObject headerArea = CreateUIObject("HeaderArea", mainPanel.transform);
            RectTransform headerRT = headerArea.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.05f, 0.88f);
            headerRT.anchorMax = new Vector2(0.95f, 0.97f);
            headerRT.offsetMin = Vector2.zero;
            headerRT.offsetMax = Vector2.zero;

            // Başlık İkonu (Settings Gear)
            GameObject titleIconObj = CreateImageObject("TitleIcon", headerArea.transform, _settingsGearSprite, new Vector2(50, 50));
            RectTransform tiRT = titleIconObj.GetComponent<RectTransform>();
            tiRT.anchorMin = new Vector2(0f, 0.5f);
            tiRT.anchorMax = new Vector2(0f, 0.5f);
            tiRT.pivot = new Vector2(0f, 0.5f);
            tiRT.anchoredPosition = new Vector2(10f, 0f);

            // Başlık Metni (Ayarlar - Emojisiz)
            GameObject titleTextObj = CreateTextObject("TitleText", headerArea.transform, "Ayarlar", 34f, COLOR_GOLD);
            RectTransform ttRT = titleTextObj.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 0f);
            ttRT.anchorMax = new Vector2(0.6f, 1f);
            ttRT.offsetMin = new Vector2(70f, 0f);
            ttRT.offsetMax = Vector2.zero;
            titleTextObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Kapatma Butonu (X İkonlu Mühür Görseli)
            GameObject closeBtnObj = CreateIconButton("CloseButton", headerArea.transform, _closeButtonSprite, new Vector2(56, 56));
            RectTransform cbRT = closeBtnObj.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1f, 0.5f);
            cbRT.anchorMax = new Vector2(1f, 0.5f);
            cbRT.pivot = new Vector2(1f, 0.5f);
            cbRT.anchoredPosition = new Vector2(-10f, 0f);

            // 5. Sekme Çubuğu (Tab Bar)
            GameObject tabBar = CreateUIObject("TabBar", mainPanel.transform);
            RectTransform tabBarRT = tabBar.GetComponent<RectTransform>();
            tabBarRT.anchorMin = new Vector2(0.05f, 0.79f);
            tabBarRT.anchorMax = new Vector2(0.95f, 0.87f);
            tabBarRT.offsetMin = Vector2.zero;
            tabBarRT.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabHLG.spacing = 16;
            tabHLG.childForceExpandWidth = true;
            tabHLG.childForceExpandHeight = true;
            tabHLG.padding = new RectOffset(6, 6, 2, 2);

            GameObject tabAccount = CreateTabButton("TabAccountButton", tabBar.transform, _tabAccountSprite, "Hesap");
            GameObject tabAudio = CreateTabButton("TabAudioButton", tabBar.transform, _tabAudioSprite, "Ses Ayarlari");
            GameObject tabCredits = CreateTabButton("TabCreditsButton", tabBar.transform, _tabCreditsSprite, "Credits");

            // 6. İçerik Alanı
            GameObject contentArea = CreateUIObject("ContentArea", mainPanel.transform);
            RectTransform contentRT = contentArea.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.05f, 0.05f);
            contentRT.anchorMax = new Vector2(0.95f, 0.78f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            // Sekme İçerikleri
            GameObject accountTab = CreateUIObject("AccountTabContent", contentArea.transform);
            StretchFull(accountTab);
            BuildAccountTab(accountTab);

            GameObject audioTab = CreateUIObject("AudioTabContent", contentArea.transform);
            StretchFull(audioTab);
            BuildAudioTab(audioTab);

            GameObject creditsTab = CreateUIObject("CreditsTabContent", contentArea.transform);
            StretchFull(creditsTab);
            BuildCreditsTab(creditsTab);

            // 7. SettingsMenuUI Bileşenini Ekle ve Bağla
            SettingsMenuUI menuUI = root.AddComponent<SettingsMenuUI>();
            SerializedObject so = new SerializedObject(menuUI);

            so.FindProperty("rootPanel").objectReferenceValue = bgOverlay;
            so.FindProperty("closeButton").objectReferenceValue = closeBtnObj.GetComponent<Button>();

            so.FindProperty("tabAccountButton").objectReferenceValue = tabAccount.GetComponent<Button>();
            so.FindProperty("tabAudioButton").objectReferenceValue = tabAudio.GetComponent<Button>();
            so.FindProperty("tabCreditsButton").objectReferenceValue = tabCredits.GetComponent<Button>();

            so.FindProperty("accountTabContent").objectReferenceValue = accountTab;
            so.FindProperty("audioTabContent").objectReferenceValue = audioTab;
            so.FindProperty("creditsTabContent").objectReferenceValue = creditsTab;

            // Hesap Sekmesi Referansları
            so.FindProperty("accountStatusText").objectReferenceValue = accountTab.transform.Find("StatusCard/AccountStatusText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("accountPlayerIdText").objectReferenceValue = accountTab.transform.Find("StatusCard/PlayerIdText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("accountMessageText").objectReferenceValue = accountTab.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("accountUsernameInput").objectReferenceValue = accountTab.transform.Find("UsernameInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("accountPasswordInput").objectReferenceValue = accountTab.transform.Find("PasswordInput")?.GetComponent<TMP_InputField>();
            so.FindProperty("accountGuestSignInButton").objectReferenceValue = accountTab.transform.Find("ButtonRow/GuestSignInButton")?.GetComponent<Button>();
            so.FindProperty("accountSignInButton").objectReferenceValue = accountTab.transform.Find("ButtonRow/SignInButton")?.GetComponent<Button>();
            so.FindProperty("accountRegisterButton").objectReferenceValue = accountTab.transform.Find("ButtonRow/RegisterButton")?.GetComponent<Button>();
            so.FindProperty("accountSignOutButton").objectReferenceValue = accountTab.transform.Find("ButtonRow/SignOutButton")?.GetComponent<Button>();

            // Ses Sekmesi Referansları
            so.FindProperty("musicVolumeSlider").objectReferenceValue = audioTab.transform.Find("MusicSection/SliderRow/MusicVolumeSlider")?.GetComponent<Slider>();
            so.FindProperty("musicVolumeLabel").objectReferenceValue = audioTab.transform.Find("MusicSection/SliderRow/MusicVolumeLabel")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("musicMuteToggle").objectReferenceValue = audioTab.transform.Find("MusicSection/TitleRow/MusicMuteToggle")?.GetComponent<Toggle>();

            so.FindProperty("sfxVolumeSlider").objectReferenceValue = audioTab.transform.Find("SFXSection/SliderRow/SFXVolumeSlider")?.GetComponent<Slider>();
            so.FindProperty("sfxVolumeLabel").objectReferenceValue = audioTab.transform.Find("SFXSection/SliderRow/SFXVolumeLabel")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("sfxMuteToggle").objectReferenceValue = audioTab.transform.Find("SFXSection/TitleRow/SFXMuteToggle")?.GetComponent<Toggle>();

            so.FindProperty("toggleOnSprite").objectReferenceValue = _toggleOnSprite;
            so.FindProperty("toggleOffSprite").objectReferenceValue = _toggleOffSprite;
            so.FindProperty("musicToggleImage").objectReferenceValue = audioTab.transform.Find("MusicSection/TitleRow/MusicMuteToggle/ToggleGraphic")?.GetComponent<Image>();
            so.FindProperty("sfxToggleImage").objectReferenceValue = audioTab.transform.Find("SFXSection/TitleRow/SFXMuteToggle/ToggleGraphic")?.GetComponent<Image>();

            // Credits Sekmesi Referansı
            so.FindProperty("creditsText").objectReferenceValue = creditsTab.transform.Find("Scroll View/Viewport/CreditsText")?.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedPropertiesWithoutUndo();

            // 8. Prefab Olarak Kaydet
            string prefabDir = "Assets/_PotionTown/Prefabs/UI/Menus";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_PotionTown/Prefabs/UI"))
                    AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs", "UI");
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs/UI", "Menus");
            }

            string prefabPath = $"{prefabDir}/SettingsMenuUI.prefab";
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
            Object.DestroyImmediate(root);

            if (success)
            {
                Debug.Log($"<color=green>[SettingsMenuBuilder]</color> Ayarlar Menüsü prefabı oluşturuldu: {prefabPath}");
                // TopDropdownMenu prefabını da güncelle
                UpdateTopDropdownMenuPrefab(prefabPath);
            }
            else
            {
                Debug.LogError("[SettingsMenuBuilder] SettingsMenuUI prefabı kaydedilemedi!");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TOPDROPDOWNMENU PREFAB ENTEGRASYONU
        // ═══════════════════════════════════════════════════════

        [MenuItem("GameObject/UI/PotionTavern - TopDropdownMenu İkonlarını Güncelle", false, 12)]
        public static void UpdateTopDropdownMenuOnly()
        {
            UpdateTopDropdownMenuPrefab("Assets/_PotionTown/Prefabs/UI/Menus/SettingsMenuUI.prefab");
        }

        private static void UpdateTopDropdownMenuPrefab(string settingsPrefabPath)
        {
            string topMenuPrefabPath = "Assets/_PotionTown/Prefabs/UI/Menus/TopDropdownMenu.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(topMenuPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning("[SettingsMenuBuilder] TopDropdownMenu prefabı bulunamadı!");
                return;
            }

            TopDropdownMenu menu = prefabRoot.GetComponent<TopDropdownMenu>();
            Sprite settingsIcon = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/TopMenu/Icon_Settings.png");
            Sprite shopOpenIcon = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/TopMenu/Icon_Shop_Open.png");
            Sprite shopCloseIcon = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/TopMenu/Icon_Shop_Close.png");
            GameObject settingsMenuPrefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(settingsPrefabPath);

            // Settings butonunu bul ve ikonunu güncelle
            Transform settingsT = prefabRoot.transform.Find("MenuPanel/Settings") ??
                                 prefabRoot.transform.Find("MenuPanel/SettingsButton");
            if (settingsT != null)
            {
                Image sImg = settingsT.GetComponent<Image>();
                if (sImg != null && settingsIcon != null)
                {
                    sImg.sprite = settingsIcon;
                    sImg.preserveAspect = true;
                    sImg.color = Color.white;
                }

                // Settings butonundaki Text (varsa) temizle, sadece simge gözüksün
                TextMeshProUGUI sTxt = settingsT.GetComponentInChildren<TextMeshProUGUI>(true);
                if (sTxt != null) sTxt.text = "";

                if (menu != null)
                {
                    menu.settingsButton = settingsT.GetComponent<Button>();
                    menu.settingsButtonImage = sImg;
                    menu.settingsIconSprite = settingsIcon;
                }
            }

            if (menu != null)
            {
                if (settingsMenuPrefabObj != null)
                    menu.settingsMenuPrefab = settingsMenuPrefabObj;

                if (shopOpenIcon != null)
                    menu.shopOpenSprite = shopOpenIcon;
                if (shopCloseIcon != null)
                    menu.shopCloseSprite = shopCloseIcon;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, topMenuPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"<color=green>[SettingsMenuBuilder]</color> TopDropdownMenu prefabı güncellendi! Settings butonu ve dükkan ikonları bağlandı.");
        }

        // ═══════════════════════════════════════════════════════
        //  HESAP SEKMESİ İNŞASI
        // ═══════════════════════════════════════════════════════

        private static void BuildAccountTab(GameObject parent)
        {
            // Başlık Satırı: İkon + Metin
            GameObject headerRow = CreateUIObject("HeaderRow", parent.transform);
            RectTransform hrRT = headerRow.GetComponent<RectTransform>();
            hrRT.anchorMin = new Vector2(0.04f, 0.88f);
            hrRT.anchorMax = new Vector2(0.96f, 0.98f);
            hrRT.offsetMin = Vector2.zero;
            hrRT.offsetMax = Vector2.zero;

            GameObject accIcon = CreateImageObject("Icon", headerRow.transform, _tabAccountSprite, new Vector2(40, 40));
            RectTransform aiRT = accIcon.GetComponent<RectTransform>();
            aiRT.anchorMin = new Vector2(0f, 0.5f);
            aiRT.anchorMax = new Vector2(0f, 0.5f);
            aiRT.pivot = new Vector2(0f, 0.5f);
            aiRT.anchoredPosition = Vector2.zero;

            GameObject titleText = CreateTextObject("Title", headerRow.transform, "Hesap Yonetimi", 26f, COLOR_GOLD);
            RectTransform ttRT = titleText.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 0f);
            ttRT.anchorMax = new Vector2(1f, 1f);
            ttRT.offsetMin = new Vector2(50f, 0f);
            ttRT.offsetMax = Vector2.zero;

            // Durum Kartı (Durum + Player ID)
            GameObject statusCard = CreateUIObject("StatusCard", parent.transform);
            RectTransform scRT = statusCard.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0.04f, 0.68f);
            scRT.anchorMax = new Vector2(0.96f, 0.86f);
            scRT.offsetMin = Vector2.zero;
            scRT.offsetMax = Vector2.zero;
            Image scImg = statusCard.AddComponent<Image>();
            scImg.color = COLOR_SECTION_BG;

            GameObject statusLabel = CreateTextObject("StatusLabel", statusCard.transform, "Durum:", 20f, COLOR_TEXT_MUTED);
            RectTransform slRT = statusLabel.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.05f, 0.5f);
            slRT.anchorMax = new Vector2(0.3f, 0.95f);
            slRT.offsetMin = Vector2.zero;
            slRT.offsetMax = Vector2.zero;

            GameObject statusVal = CreateTextObject("AccountStatusText", statusCard.transform, "Misafir", 22f, new Color(1f, 0.75f, 0.25f));
            RectTransform svRT = statusVal.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0.35f, 0.5f);
            svRT.anchorMax = new Vector2(0.95f, 0.95f);
            svRT.offsetMin = Vector2.zero;
            svRT.offsetMax = Vector2.zero;

            GameObject idLabel = CreateTextObject("IdLabel", statusCard.transform, "Simyaci ID:", 18f, COLOR_TEXT_MUTED);
            RectTransform ilRT = idLabel.GetComponent<RectTransform>();
            ilRT.anchorMin = new Vector2(0.05f, 0.05f);
            ilRT.anchorMax = new Vector2(0.3f, 0.5f);
            ilRT.offsetMin = Vector2.zero;
            ilRT.offsetMax = Vector2.zero;

            GameObject idVal = CreateTextObject("PlayerIdText", statusCard.transform, "Giris yapilmadi", 18f, COLOR_TEXT_WARM);
            RectTransform ivRT = idVal.GetComponent<RectTransform>();
            ivRT.anchorMin = new Vector2(0.35f, 0.05f);
            ivRT.anchorMax = new Vector2(0.95f, 0.5f);
            ivRT.offsetMin = Vector2.zero;
            ivRT.offsetMax = Vector2.zero;

            // Kullanıcı Adı Input
            GameObject usrInput = CreateInputField("UsernameInput", parent.transform, "Kullanici Adi...");
            RectTransform uiRT = usrInput.GetComponent<RectTransform>();
            uiRT.anchorMin = new Vector2(0.12f, 0.52f);
            uiRT.anchorMax = new Vector2(0.88f, 0.64f);
            uiRT.offsetMin = Vector2.zero;
            uiRT.offsetMax = Vector2.zero;

            // Şifre Input
            GameObject passInput = CreateInputField("PasswordInput", parent.transform, "Sifre...");
            RectTransform piRT = passInput.GetComponent<RectTransform>();
            piRT.anchorMin = new Vector2(0.12f, 0.38f);
            piRT.anchorMax = new Vector2(0.88f, 0.50f);
            piRT.offsetMin = Vector2.zero;
            piRT.offsetMax = Vector2.zero;
            TMP_InputField pif = passInput.GetComponent<TMP_InputField>();
            if (pif != null) pif.contentType = TMP_InputField.ContentType.Password;

            // Butonlar Satırı
            GameObject btnRow = CreateUIObject("ButtonRow", parent.transform);
            RectTransform brRT = btnRow.GetComponent<RectTransform>();
            brRT.anchorMin = new Vector2(0.04f, 0.16f);
            brRT.anchorMax = new Vector2(0.96f, 0.34f);
            brRT.offsetMin = Vector2.zero;
            brRT.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(4, 4, 4, 4);

            CreateButton("GuestSignInButton", btnRow.transform, "Misafir Giris", 18f);
            CreateButton("SignInButton", btnRow.transform, "Giris Yap", 18f);
            CreateButton("RegisterButton", btnRow.transform, "Kayit Ol", 18f);
            CreateButton("SignOutButton", btnRow.transform, "Cikis Yap", 18f);

            // Geri Bildirim Mesajı
            GameObject msgText = CreateTextObject("MessageText", parent.transform, "", 18f, COLOR_TEXT_WARM);
            RectTransform mtRT = msgText.GetComponent<RectTransform>();
            mtRT.anchorMin = new Vector2(0.04f, 0.02f);
            mtRT.anchorMax = new Vector2(0.96f, 0.14f);
            mtRT.offsetMin = Vector2.zero;
            mtRT.offsetMax = Vector2.zero;
            msgText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        // ═══════════════════════════════════════════════════════
        //  SES AYARLARI SEKMESİ İNŞASI
        // ═══════════════════════════════════════════════════════

        private static void BuildAudioTab(GameObject parent)
        {
            // ─── MÜZİK BÖLÜMÜ ───
            GameObject musicSection = CreateUIObject("MusicSection", parent.transform);
            RectTransform msRT = musicSection.GetComponent<RectTransform>();
            msRT.anchorMin = new Vector2(0.04f, 0.52f);
            msRT.anchorMax = new Vector2(0.96f, 0.94f);
            msRT.offsetMin = Vector2.zero;
            msRT.offsetMax = Vector2.zero;
            Image msBg = musicSection.AddComponent<Image>();
            msBg.color = COLOR_SECTION_BG;

            // Başlık Satırı
            GameObject mTitleRow = CreateUIObject("TitleRow", musicSection.transform);
            RectTransform mtrRT = mTitleRow.GetComponent<RectTransform>();
            mtrRT.anchorMin = new Vector2(0.04f, 0.60f);
            mtrRT.anchorMax = new Vector2(0.96f, 0.95f);
            mtrRT.offsetMin = Vector2.zero;
            mtrRT.offsetMax = Vector2.zero;

            GameObject mIcon = CreateImageObject("Icon", mTitleRow.transform, _musicNoteSprite, new Vector2(44, 44));
            RectTransform miRT = mIcon.GetComponent<RectTransform>();
            miRT.anchorMin = new Vector2(0f, 0.5f);
            miRT.anchorMax = new Vector2(0f, 0.5f);
            miRT.pivot = new Vector2(0f, 0.5f);
            miRT.anchoredPosition = Vector2.zero;

            GameObject mTitle = CreateTextObject("Title", mTitleRow.transform, "Arka Plan Muzigi", 24f, COLOR_GOLD);
            RectTransform mtRT = mTitle.GetComponent<RectTransform>();
            mtRT.anchorMin = new Vector2(0f, 0f);
            mtRT.anchorMax = new Vector2(0.65f, 1f);
            mtRT.offsetMin = new Vector2(55f, 0f);
            mtRT.offsetMax = Vector2.zero;

            // Özel Kristal Toggle
            GameObject mToggle = CreateCustomToggle("MusicMuteToggle", mTitleRow.transform);
            RectTransform mtgRT = mToggle.GetComponent<RectTransform>();
            mtgRT.anchorMin = new Vector2(0.75f, 0.1f);
            mtgRT.anchorMax = new Vector2(0.98f, 0.9f);
            mtgRT.offsetMin = Vector2.zero;
            mtgRT.offsetMax = Vector2.zero;

            // Slider Satırı
            GameObject mSliderRow = CreateUIObject("SliderRow", musicSection.transform);
            RectTransform msrRT = mSliderRow.GetComponent<RectTransform>();
            msrRT.anchorMin = new Vector2(0.04f, 0.08f);
            msrRT.anchorMax = new Vector2(0.96f, 0.55f);
            msrRT.offsetMin = Vector2.zero;
            msrRT.offsetMax = Vector2.zero;

            GameObject mSlider = CreateSlider("MusicVolumeSlider", mSliderRow.transform);
            RectTransform mslRT = mSlider.GetComponent<RectTransform>();
            mslRT.anchorMin = new Vector2(0f, 0.35f);
            mslRT.anchorMax = new Vector2(0.70f, 0.75f);
            mslRT.offsetMin = Vector2.zero;
            mslRT.offsetMax = Vector2.zero;

            GameObject mLabel = CreateTextObject("MusicVolumeLabel", mSliderRow.transform, "Muzik: %50", 20f, COLOR_TEXT_WARM);
            RectTransform mlRT = mLabel.GetComponent<RectTransform>();
            mlRT.anchorMin = new Vector2(0.72f, 0.2f);
            mlRT.anchorMax = new Vector2(1f, 0.85f);
            mlRT.offsetMin = Vector2.zero;
            mlRT.offsetMax = Vector2.zero;
            mLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // ─── SFX BÖLÜMÜ ───
            GameObject sfxSection = CreateUIObject("SFXSection", parent.transform);
            RectTransform ssRT = sfxSection.GetComponent<RectTransform>();
            ssRT.anchorMin = new Vector2(0.04f, 0.06f);
            ssRT.anchorMax = new Vector2(0.96f, 0.48f);
            ssRT.offsetMin = Vector2.zero;
            ssRT.offsetMax = Vector2.zero;
            Image ssBg = sfxSection.AddComponent<Image>();
            ssBg.color = COLOR_SECTION_BG;

            // SFX Başlık Satırı
            GameObject sTitleRow = CreateUIObject("TitleRow", sfxSection.transform);
            RectTransform strRT = sTitleRow.GetComponent<RectTransform>();
            strRT.anchorMin = new Vector2(0.04f, 0.60f);
            strRT.anchorMax = new Vector2(0.96f, 0.95f);
            strRT.offsetMin = Vector2.zero;
            strRT.offsetMax = Vector2.zero;

            GameObject sIcon = CreateImageObject("Icon", sTitleRow.transform, _sfxHornSprite, new Vector2(44, 44));
            RectTransform siRT = sIcon.GetComponent<RectTransform>();
            siRT.anchorMin = new Vector2(0f, 0.5f);
            siRT.anchorMax = new Vector2(0f, 0.5f);
            siRT.pivot = new Vector2(0f, 0.5f);
            siRT.anchoredPosition = Vector2.zero;

            GameObject sTitle = CreateTextObject("Title", sTitleRow.transform, "Ses Efektleri", 24f, COLOR_GOLD);
            RectTransform stRT = sTitle.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0f, 0f);
            stRT.anchorMax = new Vector2(0.65f, 1f);
            stRT.offsetMin = new Vector2(55f, 0f);
            stRT.offsetMax = Vector2.zero;

            GameObject sToggle = CreateCustomToggle("SFXMuteToggle", sTitleRow.transform);
            RectTransform stgRT = sToggle.GetComponent<RectTransform>();
            stgRT.anchorMin = new Vector2(0.75f, 0.1f);
            stgRT.anchorMax = new Vector2(0.98f, 0.9f);
            stgRT.offsetMin = Vector2.zero;
            stgRT.offsetMax = Vector2.zero;

            // SFX Slider Satırı
            GameObject sSliderRow = CreateUIObject("SliderRow", sfxSection.transform);
            RectTransform ssrRT = sSliderRow.GetComponent<RectTransform>();
            ssrRT.anchorMin = new Vector2(0.04f, 0.08f);
            ssrRT.anchorMax = new Vector2(0.96f, 0.55f);
            ssrRT.offsetMin = Vector2.zero;
            ssrRT.offsetMax = Vector2.zero;

            GameObject sSlider = CreateSlider("SFXVolumeSlider", sSliderRow.transform);
            RectTransform sslRT = sSlider.GetComponent<RectTransform>();
            sslRT.anchorMin = new Vector2(0f, 0.35f);
            sslRT.anchorMax = new Vector2(0.70f, 0.75f);
            sslRT.offsetMin = Vector2.zero;
            sslRT.offsetMax = Vector2.zero;

            GameObject sLabel = CreateTextObject("SFXVolumeLabel", sSliderRow.transform, "SFX: %80", 20f, COLOR_TEXT_WARM);
            RectTransform slRT = sLabel.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.72f, 0.2f);
            slRT.anchorMax = new Vector2(1f, 0.85f);
            slRT.offsetMin = Vector2.zero;
            slRT.offsetMax = Vector2.zero;
            sLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        // ═══════════════════════════════════════════════════════
        //  CREDITS SEKMESİ İNŞASI
        // ═══════════════════════════════════════════════════════

        private static void BuildCreditsTab(GameObject parent)
        {
            // Scroll View
            GameObject scrollView = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(parent.transform, false);
            StretchFull(scrollView);
            Image scrollBg = scrollView.GetComponent<Image>();
            scrollBg.color = COLOR_SECTION_BG;

            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.1f;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);
            StretchFull(viewport);
            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = Color.white;
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            sr.viewport = viewport.GetComponent<RectTransform>();

            // İçerik Metni (Credits)
            GameObject creditsTextObj = CreateTextObject("CreditsText", viewport.transform, "Credits yukleniyor...", 20f, COLOR_TEXT_WARM);
            RectTransform creditsRT = creditsTextObj.GetComponent<RectTransform>();
            creditsRT.anchorMin = new Vector2(0f, 1f);
            creditsRT.anchorMax = new Vector2(1f, 1f);
            creditsRT.pivot = new Vector2(0.5f, 1f);
            creditsRT.sizeDelta = new Vector2(0f, 1400f);
            creditsRT.anchoredPosition = Vector2.zero;

            TextMeshProUGUI creditsTMP = creditsTextObj.GetComponent<TextMeshProUGUI>();
            creditsTMP.alignment = TextAlignmentOptions.Top;
            creditsTMP.enableWordWrapping = true;
            creditsTMP.overflowMode = TextOverflowModes.Overflow;
            creditsTMP.richText = true;
            creditsTMP.margin = new Vector4(24, 24, 24, 24);

            ContentSizeFitter fitter = creditsTextObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.content = creditsRT;
        }

        // ═══════════════════════════════════════════════════════
        //  UI ELEMANI YARDIMCILARI
        // ═══════════════════════════════════════════════════════

        private static void LoadAssets()
        {
            _tavernFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset");
            _btnSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/General Buton.png");
            _panelBgSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Settings_Panel_Bg.png");
            _settingsGearSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Settings_Gear.png");
            _closeButtonSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Close_Button.png");
            _tabAccountSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Tab_Account.png");
            _tabAudioSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Tab_Audio.png");
            _tabCreditsSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Tab_Credits.png");
            _musicNoteSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Music_Note.png");
            _sfxHornSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_SFX_Horn.png");
            _toggleOnSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Toggle_On.png");
            _toggleOffSprite = LoadOrCreateSprite("Assets/_PotionTown/Art/UI/SettingsMenu/Icon_Toggle_Off.png");
        }

        private static Sprite LoadOrCreateSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void StretchFull(GameObject obj)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            Image img = obj.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            return obj;
        }

        private static GameObject CreateIconButton(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.15f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.75f, 0.65f, 1f);
            btn.colors = cb;

            return btnObj;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text, float fontSize, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            tmp.raycastTarget = false;
            tmp.richText = true;
            if (_tavernFont != null) tmp.font = _tavernFont;

            return obj;
        }

        private static GameObject CreateTabButton(string name, Transform parent, Sprite iconSprite, string label)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            img.color = COLOR_BUTTON;
            img.raycastTarget = true;
            if (_btnSprite != null)
            {
                img.sprite = _btnSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.1f, 0.95f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.7f);
            btn.colors = cb;

            // İkon + Metin Yatay Düzen
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            content.transform.SetParent(btnObj.transform, false);
            StretchFull(content);
            HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            if (iconSprite != null)
            {
                GameObject iconObj = CreateImageObject("Icon", content.transform, iconSprite, new Vector2(36, 36));
                iconObj.GetComponent<Image>().preserveAspect = true;
            }

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(content.transform, false);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.color = COLOR_TEXT_WARM;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (_tavernFont != null) tmp.font = _tavernFont;

            return btnObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float fontSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            img.color = COLOR_BUTTON;
            img.raycastTarget = true;
            if (_btnSprite != null)
            {
                img.sprite = _btnSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.7f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            StretchFull(txtObj);
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.offsetMin = new Vector2(4, 2);
            txtRT.offsetMax = new Vector2(-4, -2);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = COLOR_TEXT_WARM;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (_tavernFont != null) tmp.font = _tavernFont;

            return btnObj;
        }

        private static GameObject CreateCustomToggle(string name, Transform parent)
        {
            GameObject toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(parent, false);

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = true;

            // Özel kristal görseli (ON/OFF spriti dinamik değişir)
            GameObject graphicObj = new GameObject("ToggleGraphic", typeof(RectTransform), typeof(Image));
            graphicObj.transform.SetParent(toggleObj.transform, false);
            RectTransform grRT = graphicObj.GetComponent<RectTransform>();
            grRT.sizeDelta = new Vector2(48, 48);
            grRT.anchorMin = new Vector2(0.5f, 0.5f);
            grRT.anchorMax = new Vector2(0.5f, 0.5f);
            grRT.pivot = new Vector2(0.5f, 0.5f);
            grRT.anchoredPosition = Vector2.zero;

            Image gImg = graphicObj.GetComponent<Image>();
            gImg.color = Color.white;
            gImg.raycastTarget = true;
            if (_toggleOnSprite != null)
            {
                gImg.sprite = _toggleOnSprite;
                gImg.preserveAspect = true;
            }

            toggle.targetGraphic = gImg;
            toggle.graphic = gImg;

            return toggleObj;
        }

        private static GameObject CreateInputField(string name, Transform parent, string placeholder)
        {
            GameObject inputObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);

            Image bg = inputObj.GetComponent<Image>();
            bg.color = COLOR_INPUT_BG;

            // Text Area
            GameObject textArea = CreateUIObject("Text Area", inputObj.transform);
            StretchFull(textArea);
            RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.offsetMin = new Vector2(12, 4);
            textAreaRT.offsetMax = new Vector2(-12, -4);
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(textArea.transform, false);
            StretchFull(phObj);
            TextMeshProUGUI phTMP = phObj.GetComponent<TextMeshProUGUI>();
            phTMP.text = placeholder;
            phTMP.fontSize = 20f;
            phTMP.color = new Color(0.6f, 0.5f, 0.4f, 0.6f);
            phTMP.fontStyle = FontStyles.Italic;
            if (_tavernFont != null) phTMP.font = _tavernFont;

            // Text
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(textArea.transform, false);
            StretchFull(txtObj);
            TextMeshProUGUI txtTMP = txtObj.GetComponent<TextMeshProUGUI>();
            txtTMP.text = "";
            txtTMP.fontSize = 20f;
            txtTMP.color = COLOR_TEXT_WARM;
            if (_tavernFont != null) txtTMP.font = _tavernFont;

            TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = txtTMP;
            inputField.placeholder = phTMP;
            inputField.fontAsset = _tavernFont;

            return inputObj;
        }

        private static GameObject CreateSlider(string name, Transform parent)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            // Background Track
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.35f);
            bgRT.anchorMax = new Vector2(1f, 0.65f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = COLOR_SLIDER_TRACK;

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.35f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRT.offsetMin = new Vector2(4, 0);
            fillAreaRT.offsetMax = new Vector2(-4, 0);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = COLOR_SLIDER_FILL;
            slider.fillRect = fillRT;

            // Handle Slide Area
            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10, 0);
            handleAreaRT.offsetMax = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(28, 28);
            handle.GetComponent<Image>().color = COLOR_GOLD;
            slider.handleRect = handleRT;
            slider.targetGraphic = handle.GetComponent<Image>();

            return sliderObj;
        }
    }
}
#endif
