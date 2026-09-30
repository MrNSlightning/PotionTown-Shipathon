#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PotionShop.Editor
{
    /// <summary>
    /// MalzemeDükkanı'ndaki tüm satın alınabilir eşyaların altına fiyat etiketi (PriceTag)
    /// ve yanına para birimi simgesini (Altın için Gold Coin, Kadim Para için Ancient Seal)
    /// ekleyen ve ShopItemButton ile tam hiyerarşik bağlayan editör aracı.
    /// </summary>
    public static class MaterialShopPriceTagEditor
    {
        private const string GOLD_COIN_PATH = "Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Gold Coin.png";
        private const string ANCIENT_SEAL_PATH = "Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Ancient Seal.png";
        private const string FONT_PATH = "Assets/_PotionTown/Fonts/NewRocker-Regular SDF.asset";

        [MenuItem("Tools/Potion Shop/Malzeme Dükkanı Fiyat Etiketlerini ve Coinlerini Kur", priority = 106)]
        public static void SetupAllPriceTagsMenu()
        {
            SetupAllPriceTags(true);
        }

        public static void RunBatchAll()
        {
            Debug.Log("[Batch] Starting RunBatchAll...");

            // 1. Mevcut Lisans Prefablarını güncelle (BulkBuyBtn: Text, SubText, Coin)
            LicensePrefabBuilder.UpdateExistingLicensePrefabBulkBuy();

            // 2. Sahneyi aç ve tüm dükkan eşyalarına PriceTag ekle
            var scene = EditorSceneManager.OpenScene("Assets/_PotionTown/Scenes/SampleScene.unity");
            int count = SetupAllPriceTags(false);
            Debug.Log($"[Batch] Setup {count} price tags in SampleScene.");

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Batch] RunBatchAll tamamlandı!");
            EditorApplication.Exit(0);
        }

        public static int SetupAllPriceTags(bool showDialog = false)
        {
            Sprite goldCoinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GOLD_COIN_PATH);
            Sprite ancientSealSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ANCIENT_SEAL_PATH);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

            if (goldCoinSprite == null)
            {
                Debug.LogWarning($"[MaterialShopPriceTagEditor] Altın Coin görseli bulunamadı: {GOLD_COIN_PATH}");
            }
            if (ancientSealSprite == null)
            {
                Debug.LogWarning($"[MaterialShopPriceTagEditor] Kadim Para / Elmas görseli bulunamadı: {ANCIENT_SEAL_PATH}");
            }

            // Sahnedeki tüm ShopItemButton bileşenlerini bul
            ShopItemButton[] allButtons = Object.FindObjectsByType<ShopItemButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allButtons == null || allButtons.Length == 0)
            {
                Debug.LogWarning("[MaterialShopPriceTagEditor] Sahnede hiçbir ShopItemButton bulunamadı!");
                return 0;
            }

            int configuredCount = 0;

            foreach (var btn in allButtons)
            {
                if (btn == null) continue;

                // Sadece MalzemeDükkanı altında olanları veya sahnedeki dükkan eşyalarını hedefle
                Transform parent = btn.transform;
                bool isUnderShop = false;
                while (parent != null)
                {
                    string pName = parent.name.ToLower();
                    if (pName.Contains("malzemedükkan") || pName.Contains("malzemedukkan") || pName.Contains("malzeme") || pName.Contains("raf") || pName.Contains("special"))
                    {
                        isUnderShop = true;
                        break;
                    }
                    parent = parent.parent;
                }

                // Eğer dükkan hiyerarşisinde değilse bile geçerli bir itemData'sı varsa işle
                if (!isUnderShop && btn.itemData == null) continue;

                Undo.RegisterFullObjectHierarchyUndo(btn.gameObject, "Setup ShopItemButton PriceTag");

                btn.goldCoinSprite = goldCoinSprite;
                btn.kadimParaSprite = ancientSealSprite;

                bool isKadim = btn.itemData != null && btn.itemData.kadimParaPrice > 0;
                Sprite currentCurrencySprite = isKadim ? ancientSealSprite : goldCoinSprite;

                // PriceTag container'ı bul veya oluştur
                Transform priceTagT = btn.transform.Find("PriceTag");
                GameObject priceTagGO;

                if (priceTagT == null)
                {
                    priceTagGO = new GameObject("PriceTag", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    Undo.RegisterCreatedObjectUndo(priceTagGO, "Create PriceTag Container");
                    priceTagGO.transform.SetParent(btn.transform, false);

                    RectTransform ptRt = priceTagGO.GetComponent<RectTransform>();
                    // Eşyanın hemen altına yerleştir
                    ptRt.anchorMin = new Vector2(0.5f, 0f);
                    ptRt.anchorMax = new Vector2(0.5f, 0f);
                    ptRt.pivot = new Vector2(0.5f, 1f);
                    ptRt.anchoredPosition = new Vector2(0f, -6f);
                    ptRt.sizeDelta = new Vector2(100f, 48f);

                    HorizontalLayoutGroup hlg = priceTagGO.GetComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 3f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }
                else
                {
                    priceTagGO = priceTagT.gameObject;
                    RectTransform ptRt = priceTagGO.GetComponent<RectTransform>();
                    ptRt.anchorMin = new Vector2(0.5f, 0f);
                    ptRt.anchorMax = new Vector2(0.5f, 0f);
                    ptRt.pivot = new Vector2(0.5f, 1f);
                    ptRt.anchoredPosition = new Vector2(0f, -6f);
                    ptRt.sizeDelta = new Vector2(100f, 48f);

                    HorizontalLayoutGroup hlg = priceTagGO.GetComponent<HorizontalLayoutGroup>();
                    if (hlg == null) hlg = Undo.AddComponent<HorizontalLayoutGroup>(priceTagGO);
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 3f;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }

                // 1. Coin / Currency İkonu bul veya oluştur
                Transform coinT = priceTagGO.transform.Find("Coin");
                GameObject coinGO;
                Image coinImg;

                if (coinT == null)
                {
                    coinGO = new GameObject("Coin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(coinGO, "Create PriceTag Coin");
                    coinGO.transform.SetParent(priceTagGO.transform, false);

                    RectTransform cRt = coinGO.GetComponent<RectTransform>();
                    cRt.sizeDelta = new Vector2(20f, 20f);

                    coinImg = coinGO.GetComponent<Image>();
                    coinImg.sprite = currentCurrencySprite;
                    coinImg.preserveAspect = true;
                    coinImg.raycastTarget = false;
                }
                else
                {
                    coinGO = coinT.gameObject;
                    RectTransform cRt = coinGO.GetComponent<RectTransform>();
                    cRt.sizeDelta = new Vector2(20f, 20f);

                    coinImg = coinGO.GetComponent<Image>();
                    if (coinImg == null) coinImg = Undo.AddComponent<Image>(coinGO);
                    coinImg.sprite = currentCurrencySprite;
                    coinImg.preserveAspect = true;
                    coinImg.raycastTarget = false;
                }

                // 2. Fiyat Metni bul veya oluştur
                Transform priceTextT = priceTagGO.transform.Find("Price");
                GameObject priceTextGO;
                TextMeshProUGUI pTmp;

                if (priceTextT == null)
                {
                    priceTextGO = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
                    Undo.RegisterCreatedObjectUndo(priceTextGO, "Create PriceTag Price Text");
                    priceTextGO.transform.SetParent(priceTagGO.transform, false);

                    RectTransform pRt = priceTextGO.GetComponent<RectTransform>();
                    pRt.sizeDelta = new Vector2(50f, 24f);

                    pTmp = priceTextGO.GetComponent<TextMeshProUGUI>();
                    if (font != null) pTmp.font = font;
                    pTmp.fontSize = 18f;
                    pTmp.fontStyle = FontStyles.Bold;
                    pTmp.alignment = TextAlignmentOptions.Left;
                    pTmp.lineSpacing = -15f;
                    pTmp.color = isKadim ? new Color(0.6f, 0.9f, 1f, 1f) : new Color(1f, 0.85f, 0.35f, 1f);
                    pTmp.raycastTarget = false;
                }
                else
                {
                    priceTextGO = priceTextT.gameObject;
                    RectTransform pRt = priceTextGO.GetComponent<RectTransform>();
                    pRt.sizeDelta = new Vector2(50f, 24f);

                    pTmp = priceTextGO.GetComponent<TextMeshProUGUI>();
                    if (font != null) pTmp.font = font;
                    pTmp.fontSize = 18f;
                    pTmp.fontStyle = FontStyles.Bold;
                    pTmp.alignment = TextAlignmentOptions.Left;
                    pTmp.lineSpacing = -15f;
                    pTmp.color = isKadim ? new Color(0.6f, 0.9f, 1f, 1f) : new Color(1f, 0.85f, 0.35f, 1f);
                    pTmp.raycastTarget = false;
                }

                // 3. Lisans Gerekli Metni bul veya oluştur (Hiyerarşide açıkça görünen ve düzenlenebilen nesne)
                Transform licTextT = priceTagGO.transform.Find("LicenseRequired") ?? priceTagGO.transform.Find("LisansGerekli");
                GameObject licTextGO;
                TextMeshProUGUI licTmp;

                if (licTextT == null)
                {
                    licTextGO = new GameObject("LicenseRequired", typeof(RectTransform), typeof(TextMeshProUGUI));
                    Undo.RegisterCreatedObjectUndo(licTextGO, "Create PriceTag LicenseRequired Text");
                    licTextGO.transform.SetParent(priceTagGO.transform, false);

                    RectTransform lRt = licTextGO.GetComponent<RectTransform>();
                    lRt.sizeDelta = new Vector2(100f, 48f);

                    licTmp = licTextGO.GetComponent<TextMeshProUGUI>();
                    if (font != null) licTmp.font = font;
                    licTmp.text = "Lisans\nGerekli";
                    licTmp.fontSize = 30f;
                    licTmp.fontStyle = FontStyles.Bold;
                    licTmp.alignment = TextAlignmentOptions.Center;
                    licTmp.lineSpacing = -15f;
                    licTmp.enableWordWrapping = false;
                    licTmp.overflowMode = TextOverflowModes.Overflow;
                    licTmp.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    licTmp.raycastTarget = false;
                }
                else
                {
                    licTextGO = licTextT.gameObject;
                    RectTransform lRt = licTextGO.GetComponent<RectTransform>();
                    lRt.sizeDelta = new Vector2(100f, 48f);

                    licTmp = licTextGO.GetComponent<TextMeshProUGUI>();
                    if (font != null) licTmp.font = font;
                    licTmp.text = "Lisans\nGerekli";
                    licTmp.fontSize = 30f;
                    licTmp.fontStyle = FontStyles.Bold;
                    licTmp.alignment = TextAlignmentOptions.Center;
                    licTmp.lineSpacing = -15f;
                    licTmp.enableWordWrapping = false;
                    licTmp.overflowMode = TextOverflowModes.Overflow;
                    licTmp.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    licTmp.raycastTarget = false;
                }

                // Sıralamayı garanti et: önce Coin, sonra Price, sonra LicenseRequired
                coinGO.transform.SetSiblingIndex(0);
                priceTextGO.transform.SetSiblingIndex(1);
                licTextGO.transform.SetSiblingIndex(2);

                // ShopItemButton referanslarını bağla
                btn.priceText = pTmp;
                btn.currencyIcon = coinImg;
                btn.licenseRequiredText = licTmp;
                btn.licenseRequiredFontSize = 30f;
                btn.licenseRequiredLineSpacing = -15f;
                btn.licenseRequiredSizeDelta = new Vector2(100f, 48f);
                btn.licenseRequiredTextFormat = "Lisans\nGerekli";
                btn.licenseRequiredColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                btn.priceFormat = "{0}";

                btn.AutoFindUIReferences();
                btn.ApplyItemData();
                btn.UpdateUI();

                EditorUtility.SetDirty(btn);
                configuredCount++;
            }

            if (configuredCount > 0)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            string resultMsg = $"Malzeme Dükkanı'ndaki {configuredCount} adet eşyanın altına fiyat ve coin/elmas simgeleri başarıyla eklendi ve hiyerarşide yapılandırıldı!";
            Debug.Log($"<color=green><b>[BAŞARILI]</b></color> {resultMsg}");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Fiyat Etiketi Kurulumu", resultMsg, "Tamam");
            }

            return configuredCount;
        }
    }
}
#endif
