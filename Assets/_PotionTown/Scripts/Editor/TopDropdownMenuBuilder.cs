#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using PotionShop;

namespace PotionShop.Editor
{
    /// <summary>
    /// PotionTown sahne atmosferine (Lobi ve Dükkanlar) uyumlu, tüm parçaları
    /// (Arka plan, buton çerçeveleri, para çerçeveleri, buton dizaynları, aşağı ok)
    /// hiyerarşide bağımsız ve düzenlenebilir olarak yeni TopDropdownMenu prefabını oluşturan araç.
    /// </summary>
    public static class TopDropdownMenuBuilder
    {
        private const string PrefabDir = "Assets/_PotionTown/Prefabs/UI/Menus";
        private const string PrefabPath = "Assets/_PotionTown/Prefabs/UI/Menus/TopDropdownMenu_New.prefab";

        private const string FontPath = "Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset";
        private const string BgWoodPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Background_Wood.png";
        private const string BtnFramePath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Button_Frame.png";
        private const string BtnBodyPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Button_Body.png";
        private const string CurrencyFramePath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Currency_Frame.png";
        private const string PullTabPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Pull_Tab.png";
        private const string ArrowIconPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Arrow_Icon.png";

        private const string GoldCoinPath = "Assets/_PotionTown/Art/UI/Icon_Gold_Coin.png";
        private const string KadimCoinPath = "Assets/_PotionTown/Art/UI/Icon_Kadim_Coin.png";

        private const string IconShopPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Icon_Shop.png";
        private const string IconInventoryPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Icon_Inventory.png";
        private const string IconLobbyPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Icon_Lobby.png";
        private const string IconSoundPath = "Assets/_PotionTown/Art/UI/TopMenu/TopMenu_Icon_Sound.png";

        [MenuItem("PotionTown/UI/🔄 Varlıkları Yenile ve Menüyü Güncelle")]
        public static void RefreshAssetsAndRebuild()
        {
            ImportTopMenuAssets();
            CreateAndSaveNewMenuPrefab();
            EnsureMenuInActiveScene();
            Debug.Log("<color=green>[TopDropdownMenuBuilder]</color> Varlıklar ve menü başarıyla güncellendi!");
        }

        private static void ImportTopMenuAssets()
        {
            string[] assets = new string[]
            {
                BgWoodPath,
                BtnFramePath,
                BtnBodyPath,
                CurrencyFramePath,
                PullTabPath,
                ArrowIconPath,
                IconShopPath,
                IconInventoryPath,
                IconLobbyPath,
                IconSoundPath
            };

            foreach (var a in assets)
            {
                AssetDatabase.ImportAsset(a, ImportAssetOptions.ForceUpdate);
            }
        }

        public static void EnsureMenuInActiveScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            var existing = GameObject.Find("TopDropdownMenu_New");
            if (existing == null)
            {
                InstantiateInScene();
            }
        }

        [MenuItem("PotionTown/UI/✨ Yeni TopDropdownMenu Prefabını Oluştur & Kaydet")]
        public static GameObject CreateAndSaveNewMenuPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_PotionTown/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefab");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/_PotionTown/Prefabs", "UI");
            }

            GameObject root = BuildMenuHierarchy();
            if (root == null)
            {
                Debug.LogError("[TopDropdownMenuBuilder] Menü oluşturulurken bir hata oluştu!");
                return null;
            }

            // Prefab olarak kaydet
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"<color=green>[TopDropdownMenuBuilder]</color> Yeni modüler TopDropdownMenu prefabı başarıyla oluşturuldu ve kaydedildi: <b>{PrefabPath}</b>");

            // Sahnedeki geçici kökü temizle (prefab kaydedildi)
            Object.DestroyImmediate(root);

            return prefabAsset;
        }

        [MenuItem("PotionTown/UI/📥 Sahneye Yeni TopDropdownMenu Ekle (Mevcut Olanı Koru)")]
        public static void InstantiateInScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = CreateAndSaveNewMenuPrefab();
            }

            if (prefab != null)
            {
                var existing = GameObject.Find("TopDropdownMenu_New");
                if (existing != null)
                {
                    Debug.Log("[TopDropdownMenuBuilder] Sahnede zaten 'TopDropdownMenu_New' mevcut.");
                    Selection.activeGameObject = existing;
                    return;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "TopDropdownMenu_New";
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate New TopDropdownMenu");
                Selection.activeGameObject = instance;

                // Sahnedeki dükkan ve lobi referanslarını otomatik bulup bağla
                TopDropdownMenu menu = instance.GetComponent<TopDropdownMenu>();
                if (menu != null)
                {
                    LinkSceneReferences(menu);
                    EditorUtility.SetDirty(menu);
                }

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);

                Debug.Log("<color=green>[TopDropdownMenuBuilder]</color> Yeni TopDropdownMenu sahneye başarıyla eklendi! Mevcut olan korunmuştur.");
            }
        }

        private static GameObject BuildMenuHierarchy()
        {
            // Varlıkları yükle
            TMP_FontAsset tavernFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite bgWoodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgWoodPath);
            Sprite btnFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BtnFramePath);
            Sprite btnBodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(BtnBodyPath);
            Sprite currencyFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CurrencyFramePath);
            Sprite pullTabSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PullTabPath);
            Sprite arrowIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowIconPath);

            Sprite goldCoinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldCoinPath);
            Sprite kadimCoinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KadimCoinPath);

            Sprite iconShopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconShopPath);
            Sprite iconInvSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconInventoryPath);
            Sprite iconLobbySprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconLobbyPath);
            Sprite iconSoundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconSoundPath);

            // 1. Root GameObject
            GameObject root = new GameObject("TopDropdownMenu_New", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TopDropdownMenu));
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 2100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            TopDropdownMenu menu = root.GetComponent<TopDropdownMenu>();

            // 2. MenuPanel
            GameObject menuPanelObj = new GameObject("MenuPanel", typeof(RectTransform));
            menuPanelObj.transform.SetParent(root.transform, false);
            RectTransform menuPanelRT = menuPanelObj.GetComponent<RectTransform>();
            menuPanelRT.anchorMin = new Vector2(0f, 1f);
            menuPanelRT.anchorMax = new Vector2(1f, 1f);
            menuPanelRT.pivot = new Vector2(0.5f, 1f);
            menuPanelRT.anchoredPosition = new Vector2(0f, 160f);
            menuPanelRT.sizeDelta = new Vector2(0f, 160f);
            menu.menuPanel = menuPanelRT;

            // 2.1 Menü Arka Planı (Ayrı GameObject)
            GameObject bgObj = new GameObject("PanelBackground", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(menuPanelObj.transform, false);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = bgWoodSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            // 2.2 Currency Section (Sol Para Alanı)
            GameObject currencySec = new GameObject("CurrencySection", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            currencySec.transform.SetParent(menuPanelObj.transform, false);
            RectTransform currRT = currencySec.GetComponent<RectTransform>();
            currRT.anchorMin = new Vector2(0f, 0.5f);
            currRT.anchorMax = new Vector2(0f, 0.5f);
            currRT.pivot = new Vector2(0f, 0.5f);
            currRT.anchoredPosition = new Vector2(40f, -6f);
            currRT.sizeDelta = new Vector2(560f, 80f);

            HorizontalLayoutGroup currHLG = currencySec.GetComponent<HorizontalLayoutGroup>();
            currHLG.spacing = 20f;
            currHLG.childAlignment = TextAnchor.MiddleLeft;
            currHLG.childControlWidth = false;
            currHLG.childControlHeight = false;
            currHLG.childForceExpandWidth = false;
            currHLG.childForceExpandHeight = false;

            // 2.2.1 Gold Slot
            GameObject goldSlot = new GameObject("GoldSlot", typeof(RectTransform));
            goldSlot.transform.SetParent(currencySec.transform, false);
            RectTransform goldSlotRT = goldSlot.GetComponent<RectTransform>();
            goldSlotRT.sizeDelta = new Vector2(260f, 76f);

            // Gold Slot Frame (Ayrı)
            GameObject goldFrameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            goldFrameObj.transform.SetParent(goldSlot.transform, false);
            RectTransform gfRT = goldFrameObj.GetComponent<RectTransform>();
            gfRT.anchorMin = Vector2.zero;
            gfRT.anchorMax = Vector2.one;
            gfRT.offsetMin = Vector2.zero;
            gfRT.offsetMax = Vector2.zero;
            Image gfImg = goldFrameObj.GetComponent<Image>();
            gfImg.sprite = currencyFrameSprite;
            gfImg.type = Image.Type.Sliced;
            gfImg.color = Color.white;
            gfImg.raycastTarget = false;

            // Gold Icon (Ayrı)
            GameObject goldIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            goldIconObj.transform.SetParent(goldSlot.transform, false);
            RectTransform giRT = goldIconObj.GetComponent<RectTransform>();
            giRT.anchorMin = new Vector2(0f, 0.5f);
            giRT.anchorMax = new Vector2(0f, 0.5f);
            giRT.pivot = new Vector2(0.5f, 0.5f);
            giRT.anchoredPosition = new Vector2(40f, 0f);
            giRT.sizeDelta = new Vector2(50f, 50f);
            Image giImg = goldIconObj.GetComponent<Image>();
            giImg.sprite = goldCoinSprite;
            giImg.preserveAspect = true;
            giImg.raycastTarget = false;
            menu.goldIcon = giImg;

            // Gold Value Text (Ayrı)
            GameObject goldTextObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldTextObj.transform.SetParent(goldSlot.transform, false);
            RectTransform gtRT = goldTextObj.GetComponent<RectTransform>();
            gtRT.anchorMin = Vector2.zero;
            gtRT.anchorMax = Vector2.one;
            gtRT.offsetMin = new Vector2(76f, 0f);
            gtRT.offsetMax = new Vector2(-16f, 0f);
            TextMeshProUGUI gtTMP = goldTextObj.GetComponent<TextMeshProUGUI>();
            gtTMP.font = tavernFont;
            gtTMP.fontSize = 30f;
            gtTMP.fontStyle = FontStyles.Bold;
            gtTMP.color = new Color(1f, 0.89f, 0.4f);
            gtTMP.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            gtTMP.text = "0";
            gtTMP.raycastTarget = false;
            menu.goldText = gtTMP;

            // 2.2.2 Kadim Para Slot
            GameObject kadimSlot = new GameObject("KadimSlot", typeof(RectTransform));
            kadimSlot.transform.SetParent(currencySec.transform, false);
            RectTransform kadimSlotRT = kadimSlot.GetComponent<RectTransform>();
            kadimSlotRT.sizeDelta = new Vector2(260f, 76f);

            // Kadim Frame (Ayrı)
            GameObject kadimFrameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            kadimFrameObj.transform.SetParent(kadimSlot.transform, false);
            RectTransform kfRT = kadimFrameObj.GetComponent<RectTransform>();
            kfRT.anchorMin = Vector2.zero;
            kfRT.anchorMax = Vector2.one;
            kfRT.offsetMin = Vector2.zero;
            kfRT.offsetMax = Vector2.zero;
            Image kfImg = kadimFrameObj.GetComponent<Image>();
            kfImg.sprite = currencyFrameSprite;
            kfImg.type = Image.Type.Sliced;
            kfImg.color = new Color(0.95f, 0.85f, 1f, 1f); // Mistik kadim eflatun/mor parlama
            kfImg.raycastTarget = false;

            // Kadim Icon (Ayrı)
            GameObject kadimIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            kadimIconObj.transform.SetParent(kadimSlot.transform, false);
            RectTransform kiRT = kadimIconObj.GetComponent<RectTransform>();
            kiRT.anchorMin = new Vector2(0f, 0.5f);
            kiRT.anchorMax = new Vector2(0f, 0.5f);
            kiRT.pivot = new Vector2(0.5f, 0.5f);
            kiRT.anchoredPosition = new Vector2(40f, 0f);
            kiRT.sizeDelta = new Vector2(50f, 50f);
            Image kiImg = kadimIconObj.GetComponent<Image>();
            kiImg.sprite = kadimCoinSprite;
            kiImg.preserveAspect = true;
            kiImg.raycastTarget = false;
            menu.kadimIcon = kiImg;

            // Kadim Value Text (Ayrı)
            GameObject kadimTextObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            kadimTextObj.transform.SetParent(kadimSlot.transform, false);
            RectTransform ktRT = kadimTextObj.GetComponent<RectTransform>();
            ktRT.anchorMin = Vector2.zero;
            ktRT.anchorMax = Vector2.one;
            ktRT.offsetMin = new Vector2(76f, 0f);
            ktRT.offsetMax = new Vector2(-16f, 0f);
            TextMeshProUGUI ktTMP = kadimTextObj.GetComponent<TextMeshProUGUI>();
            ktTMP.font = tavernFont;
            ktTMP.fontSize = 30f;
            ktTMP.fontStyle = FontStyles.Bold;
            ktTMP.color = new Color(0.88f, 0.72f, 1f);
            ktTMP.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            ktTMP.text = "0";
            ktTMP.raycastTarget = false;
            menu.kadimParaText = ktTMP;

            // 2.3 Action Buttons Section (Sağ Butonlar Alanı)
            GameObject btnSection = new GameObject("ActionButtonsSection", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnSection.transform.SetParent(menuPanelObj.transform, false);
            RectTransform bsRT = btnSection.GetComponent<RectTransform>();
            bsRT.anchorMin = new Vector2(1f, 0.5f);
            bsRT.anchorMax = new Vector2(1f, 0.5f);
            bsRT.pivot = new Vector2(1f, 0.5f);
            bsRT.anchoredPosition = new Vector2(-40f, -6f);
            bsRT.sizeDelta = new Vector2(860f, 80f);

            HorizontalLayoutGroup bsHLG = btnSection.GetComponent<HorizontalLayoutGroup>();
            bsHLG.spacing = 16f;
            bsHLG.childAlignment = TextAnchor.MiddleRight;
            bsHLG.childControlWidth = false;
            bsHLG.childControlHeight = false;
            bsHLG.childForceExpandWidth = false;
            bsHLG.childForceExpandHeight = false;

            // 2.3.1 Shop Toggle Button
            GameObject shopBtnObj = CreateActionButton(
                "ShopToggleButton",
                btnSection.transform,
                new Vector2(230f, 76f),
                btnFrameSprite,
                btnBodySprite,
                iconShopSprite,
                tavernFont,
                "DÜKKAN",
                out Button shopBtn,
                out TextMeshProUGUI shopBtnText
            );
            menu.shopToggleButton = shopBtn;
            menu.shopToggleButtonText = shopBtnText;

            // 2.3.2 Inventory Button
            GameObject invBtnObj = CreateActionButton(
                "InventoryButton",
                btnSection.transform,
                new Vector2(230f, 76f),
                btnFrameSprite,
                btnBodySprite,
                iconInvSprite,
                tavernFont,
                "ENVANTER",
                out Button invBtn,
                out TextMeshProUGUI invBtnText
            );
            menu.inventoryButton = invBtn;

            // 2.3.3 Lobby Button
            GameObject lobbyBtnObj = CreateActionButton(
                "LobbyButton",
                btnSection.transform,
                new Vector2(230f, 76f),
                btnFrameSprite,
                btnBodySprite,
                iconLobbySprite,
                tavernFont,
                "KASABAYA DÖN",
                out Button lobbyBtn,
                out TextMeshProUGUI lobbyBtnText
            );
            menu.lobbyButton = lobbyBtn;

            // 2.3.4 Sound Toggle Button (Kompakt Kare Buton)
            GameObject soundBtnObj = CreateIconButton(
                "SoundButton",
                btnSection.transform,
                new Vector2(80f, 76f),
                btnFrameSprite,
                btnBodySprite,
                iconSoundSprite,
                out Button soundBtn
            );
            menu.soundButton = soundBtn;

            // 2.4 Pull Tab Section (Aşağı Oku Ayrı Öğe)
            GameObject arrowBtnObj = new GameObject("ArrowButton", typeof(RectTransform), typeof(Button));
            arrowBtnObj.transform.SetParent(menuPanelObj.transform, false);
            RectTransform abRT = arrowBtnObj.GetComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0.5f, 0f);
            abRT.anchorMax = new Vector2(0.5f, 0f);
            abRT.pivot = new Vector2(0.5f, 1f);
            abRT.anchoredPosition = new Vector2(0f, 6f);
            abRT.sizeDelta = new Vector2(140f, 64f);

            // Tab Frame (Ayrı Ahşap Çekme Sekmesi)
            GameObject tabFrameObj = new GameObject("TabFrame", typeof(RectTransform), typeof(Image));
            tabFrameObj.transform.SetParent(arrowBtnObj.transform, false);
            RectTransform tfRT = tabFrameObj.GetComponent<RectTransform>();
            tfRT.anchorMin = Vector2.zero;
            tfRT.anchorMax = Vector2.one;
            tfRT.offsetMin = Vector2.zero;
            tfRT.offsetMax = Vector2.zero;
            Image tfImg = tabFrameObj.GetComponent<Image>();
            tfImg.sprite = pullTabSprite;
            tfImg.color = Color.white;
            tfImg.raycastTarget = true; // Tıklama hedefi

            Button abBtn = arrowBtnObj.GetComponent<Button>();
            abBtn.targetGraphic = tfImg;
            menu.arrowButton = abBtn;

            // Arrow Icon (Ayrı Görsel Aşağı Oku)
            GameObject arrowIconObj = new GameObject("ArrowIcon", typeof(RectTransform), typeof(Image));
            arrowIconObj.transform.SetParent(arrowBtnObj.transform, false);
            RectTransform aiRT = arrowIconObj.GetComponent<RectTransform>();
            aiRT.anchorMin = new Vector2(0.5f, 0.5f);
            aiRT.anchorMax = new Vector2(0.5f, 0.5f);
            aiRT.pivot = new Vector2(0.5f, 0.5f);
            aiRT.anchoredPosition = new Vector2(0f, -4f);
            aiRT.sizeDelta = new Vector2(50f, 22f);
            Image aiImg = arrowIconObj.GetComponent<Image>();
            aiImg.sprite = arrowIconSprite;
            aiImg.preserveAspect = true;
            aiImg.raycastTarget = false;
            menu.arrowIcon = aiRT;

            // Menü ayarları
            menu.startOpen = false;
            menu.animationDuration = 0.35f;

            return root;
        }

        private static GameObject CreateActionButton(
            string name,
            Transform parent,
            Vector2 size,
            Sprite frameSprite,
            Sprite bodySprite,
            Sprite iconSprite,
            TMP_FontAsset font,
            string label,
            out Button button,
            out TextMeshProUGUI text)
        {
            GameObject btnRoot = new GameObject(name, typeof(RectTransform), typeof(Button));
            btnRoot.transform.SetParent(parent, false);
            RectTransform rootRT = btnRoot.GetComponent<RectTransform>();
            rootRT.sizeDelta = size;

            // 1. Button Body (Ayrı Buton Gövdesi - Arka Planda Çizilir)
            GameObject bodyObj = new GameObject("ButtonBody", typeof(RectTransform), typeof(Image));
            bodyObj.transform.SetParent(btnRoot.transform, false);
            RectTransform bodyRT = bodyObj.GetComponent<RectTransform>();
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.offsetMin = Vector2.zero;
            bodyRT.offsetMax = Vector2.zero;
            Image bodyImg = bodyObj.GetComponent<Image>();
            bodyImg.sprite = bodySprite;
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = Color.white;
            bodyImg.raycastTarget = true;

            // 2. Frame (Ayrı Çerçeve GameObject'i - Gövdenin Üstünde Çizilir)
            GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(btnRoot.transform, false);
            RectTransform frameRT = frameObj.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = Vector2.zero;
            frameRT.offsetMax = Vector2.zero;
            Image frameImg = frameObj.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            button = btnRoot.GetComponent<Button>();
            button.targetGraphic = bodyImg;

            // Renk geçişleri
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.85f);
            cb.pressedColor = new Color(0.85f, 0.72f, 0.55f);
            cb.selectedColor = Color.white;
            button.colors = cb;

            // 3. Icon (Ayrı İkon GameObject'i)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnRoot.transform, false);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(34f, 0f);
            iconRT.sizeDelta = new Vector2(42f, 42f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 4. Label Text (Ayrı Metin GameObject'i)
            GameObject textObj = new GameObject("LabelText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnRoot.transform, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(62f, 0f);
            textRT.offsetMax = new Vector2(-12f, 0f);
            text = textObj.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 24f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.95f, 0.85f);
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;

            return btnRoot;
        }

        private static GameObject CreateIconButton(
            string name,
            Transform parent,
            Vector2 size,
            Sprite frameSprite,
            Sprite bodySprite,
            Sprite iconSprite,
            out Button button)
        {
            GameObject btnRoot = new GameObject(name, typeof(RectTransform), typeof(Button));
            btnRoot.transform.SetParent(parent, false);
            RectTransform rootRT = btnRoot.GetComponent<RectTransform>();
            rootRT.sizeDelta = size;

            // 1. Button Body (Arka Planda Çizilir)
            GameObject bodyObj = new GameObject("ButtonBody", typeof(RectTransform), typeof(Image));
            bodyObj.transform.SetParent(btnRoot.transform, false);
            RectTransform bodyRT = bodyObj.GetComponent<RectTransform>();
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.offsetMin = Vector2.zero;
            bodyRT.offsetMax = Vector2.zero;
            Image bodyImg = bodyObj.GetComponent<Image>();
            bodyImg.sprite = bodySprite;
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = Color.white;
            bodyImg.raycastTarget = true;

            // 2. Frame (Gövdenin Üstünde Çizilir)
            GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(btnRoot.transform, false);
            RectTransform frameRT = frameObj.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = Vector2.zero;
            frameRT.offsetMax = Vector2.zero;
            Image frameImg = frameObj.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            button = btnRoot.GetComponent<Button>();
            button.targetGraphic = bodyImg;

            // 3. Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnRoot.transform, false);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;
            iconRT.sizeDelta = new Vector2(42f, 42f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // SoundToggleButton bileşenini ekle
            var stb = btnRoot.AddComponent<SoundToggleButton>();
            stb.button = button;
            stb.iconImage = iconImg;

            return btnRoot;
        }

        private static void LinkSceneReferences(TopDropdownMenu menu)
        {
            // Sahnedeki Lobi ve Dükkan canvaslarını bul
            GameObject lobi = GameObject.Find("LobiSahnesi");
            GameObject iksirSatis = GameObject.Find("İksirSatışAna");
            if (iksirSatis == null) iksirSatis = GameObject.Find("IksirSatisAna");

            if (lobi != null) menu.mapCanvas = lobi;
            if (iksirSatis != null) menu.gameCanvas = iksirSatis;
        }
    }
}
#endif
