using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using PotionShop;

namespace PotionShop.Editor
{
    /// <summary>
    /// Lisans sisteminin tüm kurallarını ve çalışma durumunu otomatik doğrulayan ve test eden araç.
    /// Unity Editor menüsünden çalıştırılabilir: Tools > Potion Shop > Lisans Sistemini Doğrula ve Test Et
    /// </summary>
    public static class LicenseSystemValidator
    {
        private static int _passedCount = 0;
        private static int _failedCount = 0;

        [MenuItem("Tools/Potion Shop/Lisans Sistemini Doğrula ve Test Et", priority = 100)]
        public static void RunAllTests()
        {
            _passedCount = 0;
            _failedCount = 0;

            // Sahneyi yükle (GameManager ve LicenseClickArea sahnede aktif olsun)
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_PotionTown/Scenes/SampleScene.unity");
            }

            Test1_RecipeDatabaseIntegrity();
            Test2_DefaultLicenseInitialization();
            Test3_MaterialShopIngredientGating();
            Test4_SinglePurchaseAndAutoEssenceUnlock();
            Test5_BulkPurchaseAndDiscountMath();
            Test6_SceneSetupCheck();
            Test7_SpecialsShelfSystem();

            Debug.Log("<color=cyan><b>==================================================</b></color>");
            if (_failedCount == 0)
            {
                Debug.Log($"<color=green><b>✔ TÜM TESTLER BAŞARIYLA GEÇTİ! ({_passedCount}/{_passedCount + _failedCount})</b></color>");
                Debug.Log("<color=green>Lisans sistemi gereksinimlere tam ve doğru şekilde uyuyor.</color>");
            }
            else
            {
                Debug.LogError($"<b>✖ BAZI TESTLER BAŞARISIZ OLDU! (Başarılı: {_passedCount}, Başarısız: {_failedCount})</b>");
            }
            Debug.Log("<color=cyan><b>==================================================</b></color>");
        }

        public static void RunAllTestsBatch()
        {
            RunAllTests();
            if (_failedCount > 0)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Tools/Potion Shop/Lisans Sistemini Sahneye Kur (Otomatik)", priority = 101)]
        public static void SetupLicenseSystemInScene()
        {
            // 1. LicenseManager kontrolü
            LicenseManager lm = Object.FindFirstObjectByType<LicenseManager>();
            if (lm == null)
            {
                GameObject lmGo = new GameObject("LicenseManager");
                lm = lmGo.AddComponent<LicenseManager>();
                Undo.RegisterCreatedObjectUndo(lmGo, "Create LicenseManager");
                Debug.Log("<color=green>[Kurulum]</color> 'LicenseManager' sahneye eklendi.");
            }
            else
            {
                Debug.Log("<color=cyan>[Kurulum]</color> 'LicenseManager' sahnede zaten mevcut.");
            }

            // 2. Lisans Tıklama Alanını bul ve LicenseClickArea bileşenini ekle
            LicenseClickArea existingArea = Object.FindFirstObjectByType<LicenseClickArea>();
            if (existingArea == null)
            {
                // Kullanıcının eklediği şeffaf Image veya Lisans adındaki objeyi ara
                UnityEngine.UI.Image targetImage = null;

                var allImages = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var img in allImages)
                {
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("lisans") || n.Contains("license"))
                    {
                        targetImage = img;
                        break;
                    }
                }

                // Bulunamadıysa, Canvas altındaki yeni eklenen şeffaf Image'ı ara (a < 0.1 ve raycastTarget == true)
                if (targetImage == null)
                {
                    foreach (var img in allImages)
                    {
                        if (img.color.a < 0.05f && img.raycastTarget && img.transform.parent != null)
                        {
                            string parentName = img.transform.parent.name.ToLower();
                            if (parentName.Contains("canvas") || parentName.Contains("shop") || parentName.Contains("specials") || parentName.Contains("dükkan"))
                            {
                                targetImage = img;
                                break;
                            }
                        }
                    }
                }

                if (targetImage != null)
                {
                    LicenseClickArea ca = targetImage.gameObject.GetComponent<LicenseClickArea>();
                    if (ca == null)
                    {
                        ca = Undo.AddComponent<LicenseClickArea>(targetImage.gameObject);
                        targetImage.gameObject.name = "Lisans_ClickArea";
                        Debug.Log($"<color=green>[Kurulum]</color> '{targetImage.gameObject.name}' objesine 'LicenseClickArea' başarıyla eklendi.");
                    }
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[Kurulum]</color> Lisans tıklama alanı için hedef Image bulunamadı. Lütfen sahnede belirlediğiniz Image objesine Inspector'dan 'LicenseClickArea' bileşenini ekleyin.");
                }
            }
            else
            {
                Debug.Log($"<color=cyan>[Kurulum]</color> 'LicenseClickArea' sahnede zaten mevcut: {existingArea.gameObject.name}");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Kurulum Tamamlandı]</color> Sahne güncellendi. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        #region Testler

        private static void Test1_RecipeDatabaseIntegrity()
        {
            LogSection("Test 1: RecipeDatabase & 7 Sayfa Bütünlük Kontrolü");

            RecipeDatabase db = RecipeDatabase.Instance;
            AssertCondition(db != null, "RecipeDatabase.Instance başarıyla yüklendi.");
            if (db == null) return;

            AssertCondition(db.allRecipes != null && db.allRecipes.Count >= 40,
                $"Toplam tarif sayısı yeterli (Mevcut: {db.allRecipes.Count}).");

            int validResultCount = db.allRecipes.Count(r => r != null && r.resultPotion != null);
            AssertCondition(validResultCount == db.allRecipes.Count,
                $"Tüm tariflerin geçerli bir resultPotion'ı var ({validResultCount}/{db.allRecipes.Count}).");

            var essences = db.allRecipes.Where(r => r != null && r.resultPotion != null && r.resultPotion.itemType == ItemType.Essence).ToList();
            AssertCondition(essences.Count == 5,
                $"Tam 5 özsu tarifi tanımlı (Kırmızı, Mavi, Yeşil, Sarı, Beyaz) (Mevcut: {essences.Count}).");

            // 7 Sayfa Kontrolü
            var pages = LicenseManager.GetRecipesGroupedByPage();
            AssertCondition(pages.Count == 7,
                $"İksirler tam 7 sayfaya ayrılmış (Mevcut: {pages.Count} sayfa).");

            bool allPagesHave5 = pages.All(p => p.Count == 5);
            AssertCondition(allPagesHave5,
                $"Her sayfada tam 5 iksir yer alıyor (Toplam: {pages.Sum(p => p.Count)} iksir).");

            // Sayfa 1 (Tier 1) iksirleri kontrolü
            if (pages.Count > 0)
            {
                bool page1AllTier1 = pages[0].All(r => r.tier == 1);
                AssertCondition(page1AllTier1,
                    $"Sayfa 1'deki ilk iksirlerin tümünün tier değeri 1 (Tier 1).");
            }
        }

        private static void Test2_DefaultLicenseInitialization()
        {
            LogSection("Test 2: Varsayılan Lisans Başlangıç Durumu (Sayfa 1)");

            // Lisansları varsayılana sıfırla
            LicenseManager.ResetToDefaultLicenses();

            RecipeDatabase db = RecipeDatabase.Instance;
            if (db == null) return;

            var pages = LicenseManager.GetRecipesGroupedByPage();
            AssertCondition(pages.Count > 0, "Sayfalar başarıyla alındı.");
            if (pages.Count == 0) return;

            var page1 = pages[0];

            // 1. Kırmızı Öz tarifi açık olmalı
            var redEssenceRecipe = db.allRecipes.FirstOrDefault(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                r.name.ToLower().Contains("red"));

            AssertCondition(redEssenceRecipe != null && LicenseManager.HasLicense(redEssenceRecipe),
                $"Kırmızı Öz tarifi ({redEssenceRecipe?.name}) varsayılan olarak AÇIK.");

            // 2. Diğer 4 özsu tarifi (Mavi, Yeşil, Sarı, Beyaz) kapalı olmalı
            var otherEssenceRecipes = db.allRecipes.Where(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                !r.name.ToLower().Contains("red")).ToList();

            bool allOtherEssencesLocked = otherEssenceRecipes.All(r => !LicenseManager.HasLicense(r));
            AssertCondition(allOtherEssencesLocked,
                $"Diğer tüm özsu tarifleri (Mavi, Yeşil, Sarı, Beyaz) varsayılan olarak KİLİTLİ.");

            // 3. İlk sayfadaki kırmızı iksir açık olmalı
            var page1RedPotion = page1.FirstOrDefault(r => r.name.ToLower().Contains("red") || r.name.ToLower().Contains("regen"));
            AssertCondition(page1RedPotion != null && LicenseManager.HasLicense(page1RedPotion),
                $"İlk sayfadaki Kırmızı İksir ({page1RedPotion?.name}) varsayılan olarak AÇIK.");

            // 4. İlk sayfadaki diğer 4 iksir (Mavi, Yeşil, Sarı, Beyaz) kapalı olmalı
            var page1OtherPotions = page1.Where(r => r != page1RedPotion).ToList();
            bool otherPage1PotionsLocked = page1OtherPotions.All(r => !LicenseManager.HasLicense(r));
            AssertCondition(otherPage1PotionsLocked,
                $"İlk sayfadaki diğer 4 iksir ({string.Join(", ", page1OtherPotions.Select(r => r.name))}) varsayılan olarak KİLİTLİ.");

            // 5. Sayfa 2..7'deki tüm iksirler kapalı olmalı
            int laterPagesUnlocked = 0;
            for (int p = 1; p < pages.Count; p++)
            {
                laterPagesUnlocked += pages[p].Count(r => LicenseManager.HasLicense(r));
            }
            AssertCondition(laterPagesUnlocked == 0,
                $"Sayfa 2..7'deki tüm iksirler başlangıçta KİLİTLİ (Açık: {laterPagesUnlocked}).");
        }

        private static void Test3_MaterialShopIngredientGating()
        {
            LogSection("Test 3: Malzeme Dükkanı Eşya/Öz Kilit Mantığı");

            RecipeDatabase db = RecipeDatabase.Instance;
            if (db == null) return;

            // 1. Kırmızı Öz ItemData'sı alınabilir olmalı
            var redEssenceItem = db.allRecipes.FirstOrDefault(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                r.name.ToLower().Contains("red"))?.resultPotion;

            AssertCondition(redEssenceItem != null && LicenseManager.HasLicenseForIngredient(redEssenceItem),
                $"Kırmızı Öz ({redEssenceItem?.itemName}) dükkanda SATIN ALINABİLİR (HasLicense == true).");

            // 2. Mavi Öz ItemData'sı kilitli olmalı
            var blueEssenceItem = db.allRecipes.FirstOrDefault(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                r.name.ToLower().Contains("blue"))?.resultPotion;

            AssertCondition(blueEssenceItem != null && !LicenseManager.HasLicenseForIngredient(blueEssenceItem),
                $"Mavi Öz ({blueEssenceItem?.itemName}) dükkanda KİLİTLİ/GRİ (HasLicense == false).");

            // 3. Parşömenler: Sadece ilgili öz tarifi açık olan parşömenler açık olmalı
            var redScroll = db.allRecipes
                .FirstOrDefault(r => r != null && r.resultPotion != null && r.resultPotion.itemType == ItemType.Essence && r.name.ToLower().Contains("red"))
                ?.requiredItems?.FirstOrDefault(i => i != null && i.itemType == ItemType.Scroll);

            var blueScroll = db.allRecipes
                .FirstOrDefault(r => r != null && r.resultPotion != null && r.resultPotion.itemType == ItemType.Essence && r.name.ToLower().Contains("blue"))
                ?.requiredItems?.FirstOrDefault(i => i != null && i.itemType == ItemType.Scroll);

            if (redScroll != null)
            {
                AssertCondition(LicenseManager.HasLicenseForIngredient(redScroll),
                    $"Kırmızı Parşömen ({redScroll.itemName}) Kırmızı Öz açık olduğu için AÇIK.");
            }

            if (blueScroll != null)
            {
                AssertCondition(!LicenseManager.HasLicenseForIngredient(blueScroll),
                    $"Mavi Parşömen ({blueScroll.itemName}) Mavi Öz kilitli olduğu için KİLİTLİ/GRİ.");
            }

            // 4. Kırmızı Öz tarifinde kullanılan malzemeler açık olmalı
            var redEssenceRecipe = db.allRecipes.FirstOrDefault(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                r.name.ToLower().Contains("red"));

            if (redEssenceRecipe != null && redEssenceRecipe.requiredItems != null)
            {
                bool allRedIngredientsUnlocked = redEssenceRecipe.requiredItems
                    .Where(i => i != null)
                    .All(i => LicenseManager.HasLicenseForIngredient(i));

                AssertCondition(allRedIngredientsUnlocked,
                    "Kırmızı Öz üretiminde kullanılan tüm hammaddeler dükkanda SATIN ALINABİLİR.");
            }
        }

        private static void Test4_SinglePurchaseAndAutoEssenceUnlock()
        {
            LogSection("Test 4: Tekil Satın Alma & Öz Tarifinin Otomatik Açılması");

            RecipeDatabase db = RecipeDatabase.Instance;
            if (db == null) return;

            var pages = LicenseManager.GetRecipesGroupedByPage();
            if (pages.Count == 0) return;

            // Sayfa 1'deki Mavi İksir tarifini bul (Mana_Potion_Recipe)
            var bluePotionOnPage1 = pages[0].FirstOrDefault(r =>
                r != null && r.name.ToLower().Contains("mana"));

            var blueEssenceRecipe = db.allRecipes.FirstOrDefault(r =>
                r != null && r.resultPotion != null &&
                r.resultPotion.itemType == ItemType.Essence &&
                r.name.ToLower().Contains("blue"));

            var blueEssenceItem = blueEssenceRecipe?.resultPotion;

            if (bluePotionOnPage1 == null || blueEssenceRecipe == null)
            {
                AssertCondition(false, "Sayfa 1 Mavi tarifleri bulunamadı.");
                return;
            }

            // Satın almadan önce ikisi de kilitli olmalı
            AssertCondition(!LicenseManager.HasLicense(bluePotionOnPage1), $"Satın almadan önce: {bluePotionOnPage1.name} kilitli.");
            AssertCondition(!LicenseManager.HasLicense(blueEssenceRecipe), $"Satın almadan önce: {blueEssenceRecipe.name} kilitli.");
            AssertCondition(!LicenseManager.HasLicenseForIngredient(blueEssenceItem), $"Satın almadan önce: Mavi Öz dükkanda kilitli/gri.");

            // Altın garantile ve tekil satın almayı simüle et
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(10000);
            }

            bool unlockResult = LicenseManager.Instance.UnlockLicense(bluePotionOnPage1, 0); // Ücretsiz test
            AssertCondition(unlockResult, $"UnlockLicense({bluePotionOnPage1.name}) çağrısı başarılı.");

            // Satın alma sonrası kontroller:
            AssertCondition(LicenseManager.HasLicense(bluePotionOnPage1),
                $"Satın alma sonrası: {bluePotionOnPage1.name} AÇILDI.");

            // KURAL: "ilk sayfadaki diğer iksirleri açınca aynı renkdeki öz tarifi de açılsın."
            AssertCondition(LicenseManager.HasLicense(blueEssenceRecipe),
                $"KURAL DOĞRULANDI: Sayfa 1 Mavi iksir lisansı alınınca Mavi Öz tarifi ({blueEssenceRecipe.name}) OTOMATİK AÇILDI!");

            AssertCondition(LicenseManager.HasLicenseForIngredient(blueEssenceItem),
                $"DÜKKAN KONTROLÜ: Mavi Öz ({blueEssenceItem.itemName}) artık dükkanda SATIN ALINABİLİR!");
        }

        private static void Test5_BulkPurchaseAndDiscountMath()
        {
            LogSection("Test 5: Sayfa Bazlı Toplu Lisans Satın Alma ve İndirim Hesabı");

            RecipeDatabase db = RecipeDatabase.Instance;
            if (db == null) return;

            var pages = LicenseManager.GetRecipesGroupedByPage();
            if (pages.Count < 2) return;

            var page1 = pages[0];
            var page2 = pages[1];

            // 1. Sayfa 1 için toplu satın alma kontrolü
            // Sayfa 1'de şu an 3 kilitli iksir olmalı (Stamina, Defense, Purify)
            int page1UnlicensedCount = LicenseManager.GetUnlicensedCountForPage(page1);
            int page1BulkCost = LicenseManager.GetBulkCostForPage(page1);

            AssertCondition(page1UnlicensedCount == 3, $"Sayfa 1'de kalan lisanssız iksir sayısı: {page1UnlicensedCount} adet.");
            AssertCondition(page1BulkCost > 0, $"Sayfa 1 indirimli toplu lisans maliyeti: {page1BulkCost} Altın.");

            // İndirim oranı kontrolü
            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
            AssertCondition(discount > 0f && discount < 1f, $"Toplu lisans indirimi ayarlı (%{discount * 100}).");

            // Altın ver ve Sayfa 1'i toplu satın al
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(50000);
            }

            bool page1BulkResult = LicenseManager.Instance.UnlockAllForPage(page1, out int spentCost);
            AssertCondition(page1BulkResult, $"Sayfa 1 toplu lisans alımı başarılı! Harcanan: {spentCost} Altın.");

            int page1Remaining = LicenseManager.GetUnlicensedCountForPage(page1);
            AssertCondition(page1Remaining == 0, $"Sayfa 1'deki tüm iksirler artık AÇIK (Kalan: {page1Remaining}).");

            // KURAL: Sayfa 1'deki tüm iksirler açılınca Yeşil, Sarı, Beyaz Öz tarifleri de otomatik açılmış olmalı!
            var greenEssence = db.allRecipes.FirstOrDefault(r => r.resultPotion?.itemType == ItemType.Essence && r.name.ToLower().Contains("green"));
            var yellowEssence = db.allRecipes.FirstOrDefault(r => r.resultPotion?.itemType == ItemType.Essence && r.name.ToLower().Contains("yellow"));
            var whiteEssence = db.allRecipes.FirstOrDefault(r => r.resultPotion?.itemType == ItemType.Essence && r.name.ToLower().Contains("white"));

            AssertCondition(greenEssence != null && LicenseManager.HasLicense(greenEssence), "Yeşil Öz tarifi otomatik AÇILDI.");
            AssertCondition(yellowEssence != null && LicenseManager.HasLicense(yellowEssence), "Sarı Öz tarifi otomatik AÇILDI.");
            AssertCondition(whiteEssence != null && LicenseManager.HasLicense(whiteEssence), "Beyaz Öz tarifi otomatik AÇILDI.");

            // 2. Sayfa 2 toplu alım kontrolü
            int page2UnlicensedCount = LicenseManager.GetUnlicensedCountForPage(page2);
            AssertCondition(page2UnlicensedCount == 5, $"Sayfa 2'de henüz 5 iksir kilitli (Mevcut: {page2UnlicensedCount}).");

            bool page2BulkResult = LicenseManager.Instance.UnlockAllForPage(page2, out int page2Spent);
            AssertCondition(page2BulkResult, $"Sayfa 2 toplu lisans alımı başarılı! Harcanan: {page2Spent} Altın.");
            AssertCondition(LicenseManager.GetUnlicensedCountForPage(page2) == 0, "Sayfa 2'deki tüm iksirler AÇILDI.");
        }

        private static void Test6_SceneSetupCheck()
        {
            LogSection("Test 6: Sahne ve Prefab Entegrasyon Kontrolü");

            // LicenseClickArea var mı?
            LicenseClickArea ca = Object.FindFirstObjectByType<LicenseClickArea>();
            if (ca != null)
            {
                AssertCondition(true, $"Sahnede 'LicenseClickArea' bileşeni mevcut ({ca.gameObject.name}).");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[Sahne]</color> Sahnede henüz 'LicenseClickArea' bulunamadı. Menüden 'Tools > Potion Shop > Lisans Sistemini Sahneye Kur (Otomatik)' seçeneğiyle ekleyebilirsiniz.");
            }

            // Parşömen prefabları var mı?
            GameObject parchmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Parşomen 1 1.prefab");
            AssertCondition(parchmentPrefab != null, "Ana Parşömen prefabı ('Parşomen 1 1.prefab') mevcut.");

            GameObject pagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Sayfa 1 (1).prefab");
            AssertCondition(pagePrefab != null, "Sayfa şablon prefabı ('Sayfa 1 (1).prefab') mevcut.");

            // GameBalanceConfig kontrolü
            GameBalanceConfig config = GameBalanceConfig.Instance;
            AssertCondition(config != null, "GameBalanceConfig ScriptableObject mevcut.");
            if (config != null)
            {
                AssertCondition(config.defaultLicensePriceMultiplier > 0,
                    $"Varsayılan lisans fiyat çarpanı: {config.defaultLicensePriceMultiplier}x");
                AssertCondition(config.bulkLicenseDiscount > 0,
                    $"Toplu lisans indirimi: %{config.bulkLicenseDiscount * 100}");
                AssertCondition(config.lockedItemAlpha > 0,
                    $"Kilitli eşya saydamlığı (alpha): {config.lockedItemAlpha}");
            }
        }

        private static void Test7_SpecialsShelfSystem()
        {
            LogSection("Test 7: SpecialsShelf (Özel Eşyalar Kullanım Alanı) Kontrolü");

            // 1. Sahne Objesi ve Manager Kontrolü
            SpecialsShelfManager manager = Object.FindFirstObjectByType<SpecialsShelfManager>(FindObjectsInactive.Include);
            AssertCondition(manager != null, "Sahnede 'SpecialsShelfManager' bileşeni mevcut.");

            if (manager == null) return;

            manager.AutoDiscoverSlots();
            var slots = manager.Slots;
            AssertCondition(slots != null && slots.Count == 6, 
                $"SpecialsShelf altında tam 6 adet özel yuva mevcut (Mevcut: {slots?.Count ?? 0}).");

            if (slots != null)
            {
                // 2. Her slotun geçerli bir ItemData'sı ve CanvasGroup'u var mı?
                bool allHaveItems = slots.All(s => s != null && s.itemData != null);
                AssertCondition(allHaveItems, "Tüm 6 yuvanın ItemData ScriptableObject ataması yapılmış.");

                bool allHaveCanvasGroup = slots.All(s => s != null && s.GetComponent<CanvasGroup>() != null);
                AssertCondition(allHaveCanvasGroup, "Tüm 6 yuvada sürükle-bırak için CanvasGroup bileşeni mevcut.");

                // 3. Eşyaların IsSpecialItem özelliği true mu?
                bool allAreSpecials = slots.All(s => s != null && s.itemData != null && s.itemData.IsSpecialItem);
                AssertCondition(allAreSpecials, "Yuvalardaki tüm eşyalar IsSpecialItem == true olarak tanımlı.");

                // 4. Envanter İzolasyonu Testi: Özel eşyalar normal çantada filtrelenmeli
                if (PlayerInventory.Instance != null && slots.Count > 0 && slots[0].itemData != null)
                {
                    ItemData testSpecial = slots[0].itemData;
                    PlayerInventory.Instance.AddItem(testSpecial, 5);

                    var bagItems = PlayerInventory.Instance.GetInventoryItems(filterHiddenTypes: true);
                    bool leakedToBag = bagItems.Any(kvp => kvp.Key == testSpecial);
                    AssertCondition(!leakedToBag, 
                        $"Özel eşya ({testSpecial.itemName}) satın alındığında normal envanter çantasında GÖRÜNMÜYOR (İzolasyon başarılı).");

                    // Temizle
                    PlayerInventory.Instance.RemoveItem(testSpecial, 5);
                }

                // 5. Görsel Sahiplik Durumu Testi (0 adet -> Gri, >=1 adet -> Renkli)
                if (slots.Count > 0 && slots[0] != null && slots[0].itemData != null)
                {
                    var slot0 = slots[0];
                    if (PlayerInventory.Instance != null)
                    {
                        // 0 adet iken gri olmalı
                        PlayerInventory.Instance.RemoveItem(slot0.itemData, PlayerInventory.Instance.GetItemCount(slot0.itemData));
                        slot0.UpdateVisuals();
                        bool isGreyWhen0 = slot0.iconImage != null && slot0.iconImage.color == slot0.unownedColor;
                        AssertCondition(isGreyWhen0, "Envanterde 0 adet varken görsel GRİ (unownedColor) olarak görünüyor.");

                        // 1 adet iken renkli olmalı
                        PlayerInventory.Instance.AddItem(slot0.itemData, 1);
                        slot0.UpdateVisuals();
                        bool isColorWhenOwned = slot0.iconImage != null && slot0.iconImage.color == slot0.ownedColor;
                        AssertCondition(isColorWhenOwned, "Satın alınıp envantere eklendiğinde görsel CANLI RENK (ownedColor) alıyor.");

                        // Temizle
                        PlayerInventory.Instance.RemoveItem(slot0.itemData, 1);
                        slot0.UpdateVisuals();
                    }
                }
            }
        }

        #endregion

        #region Yardımcılar

        private static void LogSection(string title)
        {
            Debug.Log($"\n<color=orange><b>--- {title} ---</b></color>");
        }

        private static void AssertCondition(bool condition, string message)
        {
            if (condition)
            {
                _passedCount++;
                Debug.Log($"<color=green>✔ [GEÇTİ]</color> {message}");
            }
            else
            {
                _failedCount++;
                Debug.LogError($"<color=red>✖ [BAŞARISIZ]</color> {message}");
            }
        }

        #endregion
    }
}
