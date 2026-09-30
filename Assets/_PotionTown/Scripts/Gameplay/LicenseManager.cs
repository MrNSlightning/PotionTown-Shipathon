using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PotionShop
{
    /// <summary>
    /// Lisans yönetim sistemi.
    /// Hangi iksir/öz tariflerinin lisanslarının alındığını takip eder.
    /// Lisansı olmayan malzemeler malzeme dükkanında gri görünür ve satın alınamaz.
    /// Sahne geçişlerinde veriler korunur (statik veri).
    /// </summary>
    public class LicenseManager : MonoBehaviour
    {
        private static LicenseManager _instance;
        public static LicenseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LicenseManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[LicenseManager]");
                        _instance = go.AddComponent<LicenseManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        // Statik kalıcı veriler (sahne geçişlerinde korunur)
        private static HashSet<string> _unlockedRecipeNames = new HashSet<string>();
        private static bool _isInitialized = false;
        private static bool _isInitializing = false;

        // Olaylar
        private static Action _onLicenseChanged;
        public static event Action OnLicenseChanged
        {
            add => _onLicenseChanged += value;
            remove => _onLicenseChanged -= value;
        }

        [Header("Varsayılan Lisans Ayarları")]
        [Tooltip("Başlangıçta açık olan tarifler. Boş bırakılırsa otomatik bulunur (Kırmızı Öz + İlk Kırmızı İksir).")]
        public List<RecipeData> defaultUnlockedRecipes = new List<RecipeData>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }

            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (_instance == null) _instance = this;
        }

        /// <summary>
        /// Lisans sisteminin başlatıldığından emin olur. Statik sorgulamalarda güvenle çağrılabilir.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized || _isInitializing) return;

            if (_instance != null)
            {
                _instance.InitializeDefaultLicenses();
            }
            else
            {
                // Instance getter'ı tetikle (Awake -> InitializeDefaultLicenses)
                _ = Instance;
            }
        }

        /// <summary>
        /// Test veya yeni oyun başlatma amacıyla lisansları varsayılan duruma sıfırlar.
        /// </summary>
        public static void ResetToDefaultLicenses()
        {
            _unlockedRecipeNames.Clear();
            _isInitialized = false;
            EnsureInitialized();
            _onLicenseChanged?.Invoke();
        }

        /// <summary>
        /// Başlangıçta açık olacak lisansları belirler.
        /// Inspector'dan atanmışsa onları kullanır, yoksa otomatik bulur:
        /// - Kırmızı Öz tarifi
        /// - İlk kırmızı iksir (en düşük tier)
        /// </summary>
        private void InitializeDefaultLicenses()
        {
            if (_isInitialized || _isInitializing) return;
            _isInitializing = true;

            try
            {
                // Inspector'dan atanmışsa onları kullan
                if (defaultUnlockedRecipes != null && defaultUnlockedRecipes.Count > 0)
                {
                    foreach (var recipe in defaultUnlockedRecipes)
                    {
                        if (recipe != null)
                            _unlockedRecipeNames.Add(recipe.name);
                    }
                    _isInitialized = true;
                    Debug.Log($"<color=cyan>[Lisans]</color> {_unlockedRecipeNames.Count} varsayılan lisans Inspector'dan yüklendi.");
                    return;
                }

                // Otomatik bul: Kırmızı Öz tarifi + İlk Sayfadaki Kırmızı İksir
                if (RecipeDatabase.Instance == null)
                {
                    Debug.LogWarning("[Lisans] RecipeDatabase bulunamadı, varsayılan lisanslar atanamadı.");
                    return;
                }

                // 1. Kırmızı Öz tarifini aç
                foreach (var recipe in RecipeDatabase.Instance.allRecipes)
                {
                    if (recipe == null || recipe.resultPotion == null) continue;

                    // Kırmızı Öz tarifini bul
                    if (recipe.resultPotion.itemType == ItemType.Essence && IsRedItem(recipe.resultPotion))
                    {
                        _unlockedRecipeNames.Add(recipe.name);
                        Debug.Log($"<color=cyan>[Lisans]</color> Varsayılan açık: {recipe.name} (Kırmızı Öz)");
                    }
                }

                // 2. İlk sayfadaki (Sayfa 1 / Tier 1) ilk kırmızı iksiri bul ve aç
                RecipeData firstPageRedPotion = null;
                var pages = GetRecipesGroupedByPage();
                if (pages.Count > 0 && pages[0].Count > 0)
                {
                    firstPageRedPotion = pages[0].FirstOrDefault(r => r != null && (IsRedRecipe(r) || IsRedItem(r.resultPotion)));
                }

                if (firstPageRedPotion == null)
                {
                    // Fallback: en düşük tier kırmızı iksir
                    foreach (var recipe in RecipeDatabase.Instance.allRecipes)
                    {
                        if (recipe != null && recipe.resultPotion != null &&
                            recipe.resultPotion.itemType == ItemType.Potion && IsRedRecipe(recipe))
                        {
                            if (firstPageRedPotion == null || recipe.tier < firstPageRedPotion.tier ||
                                (recipe.tier == firstPageRedPotion.tier && recipe.unlockLevel < firstPageRedPotion.unlockLevel))
                            {
                                firstPageRedPotion = recipe;
                            }
                        }
                    }
                }

                if (firstPageRedPotion != null)
                {
                    _unlockedRecipeNames.Add(firstPageRedPotion.name);
                    Debug.Log($"<color=cyan>[Lisans]</color> Varsayılan açık: {firstPageRedPotion.name} (İlk Sayfadaki Kırmızı İksir)");
                }

                _isInitialized = true;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        #region Renk Tespiti Yardımcıları

        /// <summary>
        /// Bir eşyanın kırmızı renk grubunda olup olmadığını kontrol eder.
        /// </summary>
        private static bool IsRedItem(ItemData item)
        {
            if (item == null) return false;
            string n = item.name.ToLower();
            string displayName = item.itemName != null ? item.itemName.ToLower() : "";
            return n.Contains("red") || n.Contains("kırmızı") || n.Contains("kirmizi") ||
                   displayName.Contains("red") || displayName.Contains("kırmızı") || displayName.Contains("kirmizi");
        }

        /// <summary>
        /// Bir tarifin kırmızı renk grubunda olup olmadığını kontrol eder.
        /// Tarifte kullanılan Özsu'ya bakarak belirler.
        /// </summary>
        private static bool IsRedRecipe(RecipeData recipe)
        {
            if (recipe == null || recipe.requiredItems == null) return false;
            foreach (var item in recipe.requiredItems)
            {
                if (item != null && item.itemType == ItemType.Essence && IsRedItem(item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Bir tariften kullanılan Özsu eşyasını döndürür (renk tespiti için).
        /// </summary>
        public static ItemData GetEssenceFromRecipe(RecipeData recipe)
        {
            if (recipe == null || recipe.requiredItems == null) return null;
            foreach (var item in recipe.requiredItems)
            {
                if (item != null && item.itemType == ItemType.Essence)
                    return item;
            }
            return null;
        }

        #endregion

        #region Lisans Sorgulama

        /// <summary>
        /// Belirli bir tarifin lisansı var mı?
        /// </summary>
        public static bool HasLicense(RecipeData recipe)
        {
            if (recipe == null) return false;
            EnsureInitialized();
            return _unlockedRecipeNames.Contains(recipe.name);
        }

        /// <summary>
        /// Belirli bir iksir/öz için herhangi bir lisanslı tarif var mı?
        /// </summary>
        public static bool HasLicenseForPotion(ItemData potion)
        {
            if (potion == null) return true;
            EnsureInitialized();
            if (RecipeDatabase.Instance == null) return true;

            var recipes = RecipeDatabase.Instance.GetRecipesForResult(potion);
            foreach (var recipe in recipes)
            {
                if (HasLicense(recipe)) return true;
            }
            return false;
        }

        /// <summary>
        /// Bir malzeme için en az bir lisanslı tarif var mı?
        /// Malzeme dükkanındaki gri/renkli görünüm için kullanılır.
        /// </summary>
        public static bool HasLicenseForIngredient(ItemData ingredient)
        {
            if (ingredient == null) return true;

            // Özel eşyalar ve Kadim Para ile satılan eşyalar lisans gerektirmez, daima satın alınabilir
            if (ingredient.kadimParaPrice > 0 || ingredient.specialEffect != SpecialItemEffect.None)
            {
                return true;
            }

            EnsureInitialized();

            // LicenseManager başlatılamadıysa varsayılan açık
            if (!_isInitialized) return true;

            // Özler: O renkte herhangi bir lisanslı iksir veya özsu tarifi varsa özsu da alınabilir
            if (ingredient.itemType == ItemType.Essence)
            {
                return HasLicenseForEssence(ingredient);
            }

            // Parşömenler: Sadece ilgili renkteki öz tarifi lisanslıysa alınabilir
            if (ingredient.itemType == ItemType.Scroll)
            {
                return HasLicenseForScroll(ingredient);
            }

            // İksirler: doğrudan kontrol
            if (ingredient.itemType == ItemType.Potion)
                return HasLicenseForPotion(ingredient);

            // Malzemeler: Bu malzemeyi kullanan herhangi bir lisanslı tarif var mı?
            if (RecipeDatabase.Instance == null) return true;

            var recipes = RecipeDatabase.Instance.GetRecipesUsingItem(ingredient);
            foreach (var recipe in recipes)
            {
                if (HasLicense(recipe)) return true;
            }
            return false;
        }

        /// <summary>
        /// Bir özsu için lisans kontrolü.
        /// Kendi tarifi veya onu kullanan herhangi bir iksir tarifi lisanslıysa açıktır.
        /// </summary>
        private static bool HasLicenseForEssence(ItemData essence)
        {
            if (essence == null) return true;
            if (RecipeDatabase.Instance == null) return true;

            // Öz tarifinin kendisi lisanslı mı?
            var essenceRecipes = RecipeDatabase.Instance.GetRecipesForResult(essence);
            foreach (var r in essenceRecipes)
            {
                if (HasLicense(r)) return true;
            }

            // Bu özsuyunu kullanan herhangi bir iksir tarifi lisanslı mı?
            var usingRecipes = RecipeDatabase.Instance.GetRecipesUsingItem(essence);
            foreach (var r in usingRecipes)
            {
                if (HasLicense(r)) return true;
            }

            return false;
        }

        /// <summary>
        /// Bir parşömen (Scroll) için lisans kontrolü.
        /// Sadece ilgili renkteki öz tarifi lisanslıysa açıktır.
        /// </summary>
        public static bool HasLicenseForScroll(ItemData scroll)
        {
            if (scroll == null) return true;
            EnsureInitialized();
            if (RecipeDatabase.Instance == null) return true;

            // 1. Bu parşömeni kullanan tariflerin (öz tarifleri) lisansını kontrol et
            var recipes = RecipeDatabase.Instance.GetRecipesUsingItem(scroll);
            foreach (var recipe in recipes)
            {
                if (HasLicense(recipe)) return true;
            }

            // 2. Eğer tarifte doğrudan bulunamazsa renk eşleştirmesi ile öz tarifine bak
            ItemData matchingEssence = FindMatchingEssenceForScroll(scroll);
            if (matchingEssence != null)
            {
                return HasLicenseForEssence(matchingEssence);
            }

            return false;
        }

        /// <summary>
        /// Parşömenin ait olduğu renk grubundaki özsu eşyasını bulur.
        /// </summary>
        private static ItemData FindMatchingEssenceForScroll(ItemData scroll)
        {
            if (scroll == null || RecipeDatabase.Instance == null) return null;

            string sName = (scroll.name + " " + (scroll.itemName ?? "")).ToLower();

            foreach (var recipe in RecipeDatabase.Instance.allRecipes)
            {
                if (recipe != null && recipe.resultPotion != null && recipe.resultPotion.itemType == ItemType.Essence)
                {
                    var ess = recipe.resultPotion;
                    string eName = (ess.name + " " + (ess.itemName ?? "")).ToLower();

                    // Kırmızı / Ateş
                    if ((sName.Contains("fire") || sName.Contains("ateş") || sName.Contains("ates") || sName.Contains("red") || sName.Contains("kırmızı") || sName.Contains("kirmizi")) &&
                        (eName.Contains("red") || eName.Contains("kırmızı") || eName.Contains("kirmizi")))
                        return ess;

                    // Mavi / Işınlanma
                    if ((sName.Contains("teleport") || sName.Contains("ışınlanma") || sName.Contains("isinlanma") || sName.Contains("blue") || sName.Contains("mavi")) &&
                        (eName.Contains("blue") || eName.Contains("mavi")))
                        return ess;

                    // Yeşil / İyileştirme
                    if ((sName.Contains("healing") || sName.Contains("iyileştirme") || sName.Contains("iyilestirme") || sName.Contains("green") || sName.Contains("yeşil") || sName.Contains("yesil")) &&
                        (eName.Contains("green") || eName.Contains("yeşil") || eName.Contains("yesil")))
                        return ess;

                    // Sarı / Koruma
                    if ((sName.Contains("protection") || sName.Contains("koruma") || sName.Contains("yellow") || sName.Contains("sarı") || sName.Contains("sari")) &&
                        (eName.Contains("yellow") || eName.Contains("sarı") || eName.Contains("sari")))
                        return ess;

                    // Beyaz / Tanımlama
                    if ((sName.Contains("identify") || sName.Contains("tanımlama") || sName.Contains("tanimlama") || sName.Contains("white") || sName.Contains("beyaz")) &&
                        (eName.Contains("white") || eName.Contains("beyaz")))
                        return ess;
                }
            }
            return null;
        }

        #endregion

        #region Lisans Satın Alma

        /// <summary>
        /// Tek bir tarif için lisans satın alır (Altın ile).
        /// Aynı renkteki öz tarifini otomatik açar.
        /// </summary>
        public bool UnlockLicense(RecipeData recipe, int overrideCost = -1)
        {
            if (recipe == null) return false;
            EnsureInitialized();
            if (HasLicense(recipe)) return true; // Zaten açık

            int actualCost = overrideCost >= 0 ? overrideCost : GetLicenseCost(recipe);

            if (actualCost == 0)
            {
                _unlockedRecipeNames.Add(recipe.name);

                // Aynı renkteki öz tarifini otomatik aç
                AutoUnlockEssenceRecipe(recipe);

                _onLicenseChanged?.Invoke();
                Debug.Log($"<color=cyan>[Lisans]</color> Lisans açıldı (ücretsiz): {recipe.name}");
                return true;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[Lisans] GameManager bulunamadı!");
                return false;
            }

            if (GameManager.Instance.SpendGold(actualCost))
            {
                _unlockedRecipeNames.Add(recipe.name);

                // Aynı renkteki öz tarifini otomatik aç
                AutoUnlockEssenceRecipe(recipe);

                _onLicenseChanged?.Invoke();
                Debug.Log($"<color=cyan>[Lisans]</color> Lisans alındı: {recipe.name} (-{actualCost} Altın)");
                return true;
            }
            else
            {
                ToastNotificationUI.ShowWarning("err_not_enough_gold");
                Debug.LogWarning($"<color=yellow>[Lisans]</color> Yetersiz Altın! Gerekli: {actualCost}, Mevcut: {GameManager.Instance.CurrentGold}");
                return false;
            }
        }

        /// <summary>
        /// Bir renk (özsu) grubundaki tüm lisanssız iksir tariflerini toplu satın alır.
        /// İndirimli fiyat uygulanır. Aynı renkteki öz tarifini otomatik açar.
        /// </summary>
        public bool UnlockAllForColor(ItemData essence, out int totalCost)
        {
            totalCost = 0;
            if (essence == null || RecipeDatabase.Instance == null) return false;
            EnsureInitialized();

            // Bu özsuyunu kullanan tüm lisanssız iksir tariflerini bul
            var recipes = RecipeDatabase.Instance.GetRecipesUsingItem(essence);
            var toUnlock = new List<RecipeData>();
            int rawCost = 0;

            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.resultPotion != null &&
                    recipe.resultPotion.itemType != ItemType.Essence && !HasLicense(recipe))
                {
                    toUnlock.Add(recipe);
                    rawCost += GetLicenseCost(recipe);
                }
            }

            if (toUnlock.Count == 0)
            {
                // Zaten hepsi açık, özsu tarifini de garantiye al
                UnlockEssenceRecipeOnly(essence);
                return true;
            }

            // Toplu indirim uygula
            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
            totalCost = Mathf.RoundToInt(rawCost * (1f - discount));

            if (GameManager.Instance == null) return false;

            if (GameManager.Instance.SpendGold(totalCost))
            {
                foreach (var recipe in toUnlock)
                {
                    _unlockedRecipeNames.Add(recipe.name);
                }

                // Aynı renkteki öz tarifini otomatik aç (ücretsiz)
                UnlockEssenceRecipeOnly(essence);

                _onLicenseChanged?.Invoke();
                Debug.Log($"<color=cyan>[Lisans]</color> {toUnlock.Count} lisans toplu alındı! (-{totalCost} Altın, %{discount * 100} indirim)");
                return true;
            }
            else
            {
                ToastNotificationUI.ShowWarning("err_not_enough_gold");
                Debug.LogWarning($"<color=yellow>[Lisans]</color> Yetersiz Altın! Gerekli: {totalCost}, Mevcut: {GameManager.Instance.CurrentGold}");
                return false;
            }
        }

        /// <summary>
        /// Bir tarifin lisans maliyetini hesaplar.
        /// RecipeData.licenseCost > 0 ise onu kullanır, yoksa basePrice * çarpan.
        /// </summary>
        public static int GetLicenseCost(RecipeData recipe)
        {
            if (recipe == null) return 0;

            // Tarifin kendi licenseCost'u varsa onu kullan
            if (recipe.licenseCost > 0)
                return recipe.licenseCost;

            // Otomatik hesapla: resultPotion.basePrice * çarpan (Varsayılan 4 katı)
            float multiplier = GameBalanceConfig.Instance != null
                ? GameBalanceConfig.Instance.defaultLicensePriceMultiplier : 4f;

            int basePrice = 100;
            if (recipe.resultPotion != null)
            {
                basePrice = recipe.resultPotion.basePrice > 0
                    ? recipe.resultPotion.basePrice
                    : (recipe.resultPotion.buyPrice > 0 ? recipe.resultPotion.buyPrice : 100);
            }

            return Mathf.Max(10, Mathf.RoundToInt(basePrice * multiplier));
        }

        /// <summary>
        /// Bir iksir tarifinin lisansı alındığında, aynı renkteki öz tarifini otomatik açar.
        /// </summary>
        private void AutoUnlockEssenceRecipe(RecipeData potionRecipe)
        {
            ItemData essence = GetEssenceFromRecipe(potionRecipe);
            if (essence != null)
            {
                UnlockEssenceRecipeOnly(essence);
            }
        }

        /// <summary>
        /// Belirli bir özsuyunun tarifini açar.
        /// </summary>
        private void UnlockEssenceRecipeOnly(ItemData essence)
        {
            if (essence == null || RecipeDatabase.Instance == null) return;
            var essenceRecipes = RecipeDatabase.Instance.GetRecipesForResult(essence);
            foreach (var r in essenceRecipes)
            {
                if (r != null && !_unlockedRecipeNames.Contains(r.name))
                {
                    _unlockedRecipeNames.Add(r.name);
                    Debug.Log($"<color=cyan>[Lisans]</color> Öz tarifi otomatik açıldı: {r.name}");
                }
            }
        }

        #endregion

        #region Renk Gruplama ve Toplam Maliyet

        /// <summary>
        /// Tüm tarifleri özsu rengine göre gruplar.
        /// Öz tarifleri hariç tutulur (otomatik açılırlar).
        /// Dictionary anahtarı: Özsu ItemData, değer: O renkteki iksir tarifleri listesi.
        /// </summary>
        public static Dictionary<ItemData, List<RecipeData>> GetRecipesGroupedByColor()
        {
            var grouped = new Dictionary<ItemData, List<RecipeData>>();
            if (RecipeDatabase.Instance == null) return grouped;

            foreach (var recipe in RecipeDatabase.Instance.allRecipes)
            {
                if (recipe == null || recipe.resultPotion == null) continue;

                // Öz tariflerini atla (otomatik açılırlar)
                if (recipe.resultPotion.itemType == ItemType.Essence) continue;

                ItemData essence = GetEssenceFromRecipe(recipe);
                if (essence == null) continue;

                if (!grouped.ContainsKey(essence))
                    grouped[essence] = new List<RecipeData>();

                grouped[essence].Add(recipe);
            }

            // Her grubu tier ve unlockLevel'a göre sırala
            foreach (var kvp in grouped)
            {
                kvp.Value.Sort((a, b) =>
                {
                    int cmp = a.tier.CompareTo(b.tier);
                    if (cmp != 0) return cmp;
                    return a.unlockLevel.CompareTo(b.unlockLevel);
                });
            }

            return grouped;
        }

        /// <summary>
        /// Bir renk grubundaki lisanssız iksir tariflerinin toplam indirimli maliyetini hesaplar.
        /// </summary>
        public static int GetBulkCostForColor(ItemData essence)
        {
            EnsureInitialized();
            int rawTotal = 0;
            if (RecipeDatabase.Instance == null) return 0;

            var recipes = RecipeDatabase.Instance.GetRecipesUsingItem(essence);
            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.resultPotion != null &&
                    recipe.resultPotion.itemType != ItemType.Essence && !HasLicense(recipe))
                {
                    rawTotal += GetLicenseCost(recipe);
                }
            }

            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
            return Mathf.RoundToInt(rawTotal * (1f - discount));
        }

        /// <summary>
        /// Bir renk grubundaki lisanssız tarif sayısını döndürür.
        /// </summary>
        public static int GetUnlicensedCountForColor(ItemData essence)
        {
            EnsureInitialized();
            int count = 0;
            if (RecipeDatabase.Instance == null) return 0;

            var recipes = RecipeDatabase.Instance.GetRecipesUsingItem(essence);
            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.resultPotion != null &&
                    recipe.resultPotion.itemType != ItemType.Essence && !HasLicense(recipe))
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Sayfa Bazlı Gruplama ve Toplu Satın Alma

        public const int RECIPES_PER_PAGE = 5;

        /// <summary>
        /// Orijinal parşömen sayfalarındaki (Sayfa 1 - 7) iksir tariflerinin sayfa eşleme sırası.
        /// Sayfa 1: İlk iksirler (Tier 1) - Kırmızı, Mavi, Yeşil, Sarı, Beyaz
        /// Sayfa 2..7: Sırasıyla sonraki sayfaların iksirleri.
        /// </summary>
        public static readonly string[][] PAGE_RECIPE_NAMES = new string[][]
        {
            // Sayfa 1 (Tier 1): İlk iksirler (Regen Potion, Mana Potion, Stamina Potion, Defense Potion, Purify Potion)
            new string[] { "Regen_Potion_Recipe", "Mana_Potion_Recipe", "Stamina_Potion_Recipe", "Defense_Potion_Recipe", "Purify_Potion_Recipe" },
            // Sayfa 2 (Tier 2)
            new string[] { "Small_Potion_Recipe", "Runic_Potion_Recipe", "Agility_Potion_Recipe", "Rally_Vial_Recipe", "Cleanse_Vial_Recipe" },
            // Sayfa 3 (Tier 3)
            new string[] { "Minor_Healing_Potion_Recipe", "Minor_Mana_Potion_Recipe", "Antidote_Recipe", "Shock_Cure_Recipe", "Holy_Water_Recipe" },
            // Sayfa 4 (Tier 4)
            new string[] { "Major_Healing_Potion_Recipe", "Starfire_Potion_Recipe", "Herbal_Brew_Recipe", "Transmutation_Flask_Recipe", "Sacred_Water_Recipe" },
            // Sayfa 5 (Tier 5)
            new string[] { "Elixir_Of_Life_Recipe", "Ether_Vial_Recipe", "Sprint_Elixir_Recipe", "Muscle_Tonic_Recipe", "Thaw_Potion_Recipe" },
            // Sayfa 6 (Tier 6)
            new string[] { "Life_Vial_Recipe", "Wizards_Brew_Recipe", "Sprint_Elixir_Alt_Recipe", "Phoenix_Draught_Recipe", "Purge_Elixir_Recipe" },
            // Sayfa 7 (Tier 7)
            new string[] { "Crimson_Elixir_Recipe", "Void_Potion_Recipe", "Swift_Brew_Recipe", "Stone_Cure_Recipe", "Arcane_Flask_Recipe" }
        };

        /// <summary>
        /// İksir tariflerini parşömen sayfa sıralamasına göre sayfalara ayırır (Her sayfa 5 iksir).
        /// Sayfa 1 (Tier 1): İlk iksirler.
        /// Sayfa 2 (Tier 2) .. Sayfa 7 (Tier 7) şeklinde ilerler.
        /// Öz tarifleri hariç tutulur (otomatik açılırlar).
        /// </summary>
        public static List<List<RecipeData>> GetRecipesGroupedByPage()
        {
            var pages = new List<List<RecipeData>>();
            if (RecipeDatabase.Instance == null) return pages;

            // Tüm iksir tariflerini sözlükte hazırla
            var recipeDict = new Dictionary<string, RecipeData>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in RecipeDatabase.Instance.allRecipes)
            {
                if (r != null && r.resultPotion != null && r.resultPotion.itemType != ItemType.Essence)
                {
                    recipeDict[r.name] = r;
                    if (r.name.EndsWith("_Recipe", StringComparison.OrdinalIgnoreCase))
                    {
                        string baseName = r.name.Substring(0, r.name.Length - 7);
                        if (!recipeDict.ContainsKey(baseName))
                            recipeDict[baseName] = r;
                    }
                    if (r.resultPotion != null && !string.IsNullOrEmpty(r.resultPotion.name))
                    {
                        if (!recipeDict.ContainsKey(r.resultPotion.name))
                            recipeDict[r.resultPotion.name] = r;
                    }
                }
            }

            var usedRecipes = new HashSet<RecipeData>();

            // 1. Tanımlı sayfa sıralamasına göre ekle (Sayfa 1 - 7)
            foreach (var pageNames in PAGE_RECIPE_NAMES)
            {
                var pageList = new List<RecipeData>();
                foreach (var name in pageNames)
                {
                    if (recipeDict.TryGetValue(name, out var recipe) && recipe != null)
                    {
                        pageList.Add(recipe);
                        usedRecipes.Add(recipe);
                    }
                }
                if (pageList.Count > 0)
                {
                    pages.Add(pageList);
                }
            }

            // 2. Eğer veritabanında harici iksirler varsa onları tier/unlockLevel sırasıyla ekle
            var remaining = recipeDict.Values
                .Where(r => !usedRecipes.Contains(r))
                .OrderBy(r => r.tier)
                .ThenBy(r => r.unlockLevel)
                .ThenBy(r => r.name)
                .ToList();

            if (remaining.Count > 0)
            {
                int remainingPages = Mathf.CeilToInt((float)remaining.Count / RECIPES_PER_PAGE);
                for (int p = 0; p < remainingPages; p++)
                {
                    int start = p * RECIPES_PER_PAGE;
                    int count = Mathf.Min(RECIPES_PER_PAGE, remaining.Count - start);
                    pages.Add(remaining.GetRange(start, count));
                }
            }

            return pages;
        }

        /// <summary>
        /// Bir sayfadaki tüm lisanssız iksirleri toplu satın alır.
        /// İndirimli fiyat uygulanır. Açılan iksirlerin öz tarifleri de otomatik açılır.
        /// </summary>
        public bool UnlockAllForPage(List<RecipeData> pageRecipes, out int totalCost)
        {
            totalCost = 0;
            if (pageRecipes == null || pageRecipes.Count == 0) return false;
            EnsureInitialized();

            var toUnlock = new List<RecipeData>();
            int rawCost = 0;

            foreach (var recipe in pageRecipes)
            {
                if (recipe != null && !HasLicense(recipe))
                {
                    toUnlock.Add(recipe);
                    rawCost += GetLicenseCost(recipe);
                }
            }

            if (toUnlock.Count == 0) return true; // Bu sayfadaki tüm iksirler zaten açık

            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
            totalCost = Mathf.RoundToInt(rawCost * (1f - discount));

            if (GameManager.Instance == null) return false;

            if (GameManager.Instance.SpendGold(totalCost))
            {
                foreach (var recipe in toUnlock)
                {
                    _unlockedRecipeNames.Add(recipe.name);
                    // İlgili renkteki öz tarifini otomatik aç
                    AutoUnlockEssenceRecipe(recipe);
                }

                _onLicenseChanged?.Invoke();
                Debug.Log($"<color=cyan>[Lisans]</color> Sayfadaki {toUnlock.Count} iksir lisansı toplu alındı! (-{totalCost} Altın, %{discount * 100} indirim)");
                return true;
            }
            else
            {
                ToastNotificationUI.ShowWarning("err_not_enough_gold");
                Debug.LogWarning($"<color=yellow>[Lisans]</color> Yetersiz Altın! Gerekli: {totalCost}, Mevcut: {GameManager.Instance.CurrentGold}");
                return false;
            }
        }

        /// <summary>
        /// Bir sayfadaki lisanssız iksirlerin toplam indirimli maliyetini hesaplar.
        /// </summary>
        public static int GetBulkCostForPage(List<RecipeData> pageRecipes)
        {
            EnsureInitialized();
            if (pageRecipes == null) return 0;

            int rawTotal = 0;
            foreach (var recipe in pageRecipes)
            {
                if (recipe != null && !HasLicense(recipe))
                {
                    rawTotal += GetLicenseCost(recipe);
                }
            }

            float discount = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bulkLicenseDiscount : 0.10f;
            return Mathf.RoundToInt(rawTotal * (1f - discount));
        }

        /// <summary>
        /// Bir sayfadaki lisanssız iksir sayısını döndürür.
        /// </summary>
        public static int GetUnlicensedCountForPage(List<RecipeData> pageRecipes)
        {
            EnsureInitialized();
            if (pageRecipes == null) return 0;
            int count = 0;
            foreach (var r in pageRecipes)
            {
                if (r != null && !HasLicense(r)) count++;
            }
            return count;
        }

        #endregion
    }
}
