using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro;

namespace PotionShop.Editor
{
    public static class TavernCustomerGeneratorEditor
    {
        private const string CustomersFolder = "Assets/_PotionTown/Resources/Customers";

        [InitializeOnLoadMethod]
        public static void AutoGenerateIfMissing()
        {
            if (!Directory.Exists(CustomersFolder) || Directory.GetFiles(CustomersFolder, "*.prefab").Length == 0)
            {
                GenerateAllCustomerPrefabs();
            }
        }

        [MenuItem("Tools/Potion Tavern/Müşteri Prefablarını Oluştur (Generate Customers)", false, 2)]
        public static void GenerateAllCustomerPrefabs()
        {
            if (!Directory.Exists("Assets/_PotionTown/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!Directory.Exists(CustomersFolder)) AssetDatabase.CreateFolder("Assets/_PotionTown/Resources", "Customers");

            // Spriteları Yükle
            Sprite wizardSprite = LoadSpriteByPath("Assets/_PotionTown/Art/Characters/NPCs/wizard_customer_1788094035323.png");
            Sprite knightSprite = LoadSpriteByPath("Assets/_PotionTown/Art/Characters/NPCs/knight_customer_1788094045629.png");
            Sprite elfSprite = LoadSpriteByPath("Assets/_PotionTown/Art/Characters/NPCs/elf_customer_1788094013685.png");
            Sprite dwarfSprite = LoadSpriteByPath("Assets/_PotionTown/Art/Characters/NPCs/dwarf_customer_1788094024121.png");

            Sprite bubbleSprite = LoadSpriteByPath("Assets/_PotionTown/Art/UI/Dialog baloncuğu 1.png");

            // Mood Emojileri
            Sprite[] moodSprites = new Sprite[4];
            moodSprites[0] = LoadSpriteByPath("Assets/_PotionTown/Art/Icons/Emojis/Gemini_Generated_Image_fb1fqzfb1fqzfb1f (1).png"); // Mutlu
            moodSprites[1] = LoadSpriteByPath("Assets/_PotionTown/Art/Icons/Emojis/Gemini_Generated_Image_pdvs0lpdvs0lpdvs (1).png"); // Normal
            moodSprites[2] = LoadSpriteByPath("Assets/_PotionTown/Art/Icons/Emojis/Gemini_Generated_Image_3k4ang3k4ang3k4a.png"); // Huysuz
            moodSprites[3] = LoadSpriteByPath("Assets/_PotionTown/Art/Icons/Emojis/Gemini_Generated_Image_xfg34nxfg34nxfg3 (1).png"); // Öfkeli

            List<GameObject> createdPrefabs = new List<GameObject>();

            // 1. Büyücü (Wizard)
            createdPrefabs.Add(BuildAndSavePrefab("Customer_Wizard", wizardSprite, bubbleSprite, moodSprites, 350, 490));

            // 2. Şövalye (Knight)
            createdPrefabs.Add(BuildAndSavePrefab("Customer_Knight", knightSprite, bubbleSprite, moodSprites, 360, 480));

            // 3. Elf
            createdPrefabs.Add(BuildAndSavePrefab("Customer_Elf", elfSprite, bubbleSprite, moodSprites, 340, 480));

            // 4. Cüce (Dwarf)
            createdPrefabs.Add(BuildAndSavePrefab("Customer_Dwarf", dwarfSprite, bubbleSprite, moodSprites, 380, 460));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Sahnede CustomerSpawner'a Ata
            AssignPrefabsToSpawnerInScene(createdPrefabs);

            Debug.Log("<color=green>[TavernCustomerGenerator]</color> 4 Canlı Müşteri Prefabı (Büyücü, Şövalye, Elf, Cüce) başarıyla oluşturuldu ve Spawner'a bağlandı!");
        }

        private static GameObject BuildAndSavePrefab(string prefabName, Sprite charSprite, Sprite bubbleSprite, Sprite[] moods, float width, float height)
        {
            string prefabPath = $"{CustomersFolder}/{prefabName}.prefab";

            // Kök Nesne
            GameObject root = new GameObject(prefabName, typeof(RectTransform), typeof(Customer), typeof(CustomerVisualAnimator));
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(width, height);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            Customer customer = root.GetComponent<Customer>();
            CustomerVisualAnimator animator = root.GetComponent<CustomerVisualAnimator>();

            // 1. Gölge (Shadow)
            GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowObj.transform.SetParent(root.transform, false);
            RectTransform shadowRT = shadowObj.GetComponent<RectTransform>();
            shadowRT.sizeDelta = new Vector2(width * 0.72f, 44f);
            shadowRT.anchoredPosition = new Vector2(0f, 18f);
            Image shadowImg = shadowObj.GetComponent<Image>();
            shadowImg.color = new Color(0f, 0f, 0f, 0.38f);
            shadowImg.raycastTarget = false;

            // 2. Karakter Görseli (Body / Customer)
            GameObject bodyObj = new GameObject("Customer", typeof(RectTransform), typeof(Image));
            bodyObj.transform.SetParent(root.transform, false);
            RectTransform bodyRT = bodyObj.GetComponent<RectTransform>();
            bodyRT.sizeDelta = new Vector2(width, height);
            bodyRT.anchoredPosition = new Vector2(0f, height * 0.48f); // Gövde pivotu
            Image bodyImg = bodyObj.GetComponent<Image>();
            bodyImg.sprite = charSprite;
            bodyImg.preserveAspect = true;
            bodyImg.raycastTarget = false;

            // 3. Sipariş Baloncuğu (SpeechBubble / UIContainer)
            GameObject bubbleObj = new GameObject("SpeechBubble", typeof(RectTransform), typeof(Image));
            bubbleObj.transform.SetParent(root.transform, false);
            RectTransform bubbleRT = bubbleObj.GetComponent<RectTransform>();
            bubbleRT.sizeDelta = new Vector2(230f, 190f);
            bubbleRT.anchoredPosition = new Vector2(width * 0.48f, height * 0.78f);
            Image bubbleImg = bubbleObj.GetComponent<Image>();
            bubbleImg.sprite = bubbleSprite;
            bubbleImg.color = new Color(1f, 0.98f, 0.92f, 1f);
            bubbleImg.raycastTarget = false;

            // 3a. İksir İsteği İkonu
            GameObject iconObj = new GameObject("RequestIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(bubbleObj.transform, false);
            RectTransform iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(76f, 76f);
            iconRT.anchoredPosition = new Vector2(-28f, 14f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 3b. Sabır Çubuğu Arka Planı
            GameObject barBg = new GameObject("PatienceBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(bubbleObj.transform, false);
            RectTransform barBgRT = barBg.GetComponent<RectTransform>();
            barBgRT.sizeDelta = new Vector2(110f, 13f);
            barBgRT.anchoredPosition = new Vector2(-28f, -44f);
            Image barBgImg = barBg.GetComponent<Image>();
            barBgImg.color = new Color(0.12f, 0.08f, 0.16f, 0.85f);
            barBgImg.raycastTarget = false;

            // 3c. Sabır Çubuğu Dolumu (PatienceFill)
            GameObject barFill = new GameObject("PatienceFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform barFillRT = barFill.GetComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = Vector2.one;
            barFillRT.offsetMin = new Vector2(2f, 2f);
            barFillRT.offsetMax = new Vector2(-2f, -2f);
            Image barFillImg = barFill.GetComponent<Image>();
            barFillImg.color = new Color(0.24f, 0.82f, 0.44f, 1f);
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFillImg.fillAmount = 1f;
            barFillImg.raycastTarget = false;

            // 3d. Mood İkonu (Yüz İfadesi)
            GameObject moodObj = new GameObject("MoodIcon", typeof(RectTransform), typeof(Image));
            moodObj.transform.SetParent(bubbleObj.transform, false);
            RectTransform moodRT = moodObj.GetComponent<RectTransform>();
            moodRT.sizeDelta = new Vector2(46f, 46f);
            moodRT.anchoredPosition = new Vector2(56f, 18f);
            Image moodImg = moodObj.GetComponent<Image>();
            if (moods != null && moods.Length > 0 && moods[0] != null) moodImg.sprite = moods[0];
            moodImg.preserveAspect = true;
            moodImg.raycastTarget = false;

            // 3e. Sipariş Sayacı (OrderCountText)
            GameObject countObj = new GameObject("OrderCountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(bubbleObj.transform, false);
            RectTransform countRT = countObj.GetComponent<RectTransform>();
            countRT.sizeDelta = new Vector2(80f, 24f);
            countRT.anchoredPosition = new Vector2(-28f, 62f);
            TextMeshProUGUI countTxt = countObj.GetComponent<TextMeshProUGUI>();
            countTxt.text = "1/1";
            countTxt.fontSize = 16f;
            countTxt.fontStyle = FontStyles.Bold;
            countTxt.alignment = TextAlignmentOptions.Center;
            countTxt.color = new Color(1f, 0.85f, 0.35f, 1f);
            countTxt.raycastTarget = false;

            // 3f. Reddet Butonu (Reject Button)
            GameObject rejObj = new GameObject("RejectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            rejObj.transform.SetParent(bubbleObj.transform, false);
            RectTransform rejRT = rejObj.GetComponent<RectTransform>();
            rejRT.sizeDelta = new Vector2(32f, 32f);
            rejRT.anchoredPosition = new Vector2(76f, 66f);
            Image rejImg = rejObj.GetComponent<Image>();
            rejImg.color = new Color(0.85f, 0.20f, 0.25f, 0.90f);
            Button rejBtn = rejObj.GetComponent<Button>();

            GameObject rejTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rejTxtObj.transform.SetParent(rejObj.transform, false);
            RectTransform rejTxtRT = rejTxtObj.GetComponent<RectTransform>();
            rejTxtRT.anchorMin = Vector2.zero;
            rejTxtRT.anchorMax = Vector2.one;
            rejTxtRT.offsetMin = Vector2.zero;
            rejTxtRT.offsetMax = Vector2.zero;
            TextMeshProUGUI rejTxt = rejTxtObj.GetComponent<TextMeshProUGUI>();
            rejTxt.text = "X";
            rejTxt.fontSize = 18f;
            rejTxt.fontStyle = FontStyles.Bold;
            rejTxt.alignment = TextAlignmentOptions.Center;
            rejTxt.color = Color.white;
            rejTxt.raycastTarget = false;

            UnityEditor.Events.UnityEventTools.AddPersistentListener(rejBtn.onClick, customer.RejectCustomer);

            // ─── Customer Bileşen Referansları ───
            customer.uiContainer = bubbleObj;
            customer.potionRequestIcon = iconImg;
            customer.moodIcon = moodImg;
            customer.moodSprites = moods;
            customer.orderCountText = countTxt;
            customer.walkSpeed = 380f;
            customer.timePerMood = 12f;

            // ─── VisualAnimator Bileşen Referansları ───
            animator.characterBody = bodyRT;
            animator.characterImage = bodyImg;
            animator.speechBubble = bubbleRT;
            animator.shadowObj = shadowRT;

            bubbleObj.SetActive(false);

            // Prefab Olarak Kaydet
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return savedPrefab;
        }

        private static Sprite LoadSpriteByPath(string path)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var s in sprites)
            {
                if (s is Sprite spr) return spr;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignPrefabsToSpawnerInScene(List<GameObject> prefabs)
        {
            var spawners = Object.FindObjectsByType<CustomerSpawner>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                spawner.customerPrefabs = new List<GameObject>(prefabs);
                EditorUtility.SetDirty(spawner);
            }

            if (spawners.Length > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );
            }
        }
    }
}
