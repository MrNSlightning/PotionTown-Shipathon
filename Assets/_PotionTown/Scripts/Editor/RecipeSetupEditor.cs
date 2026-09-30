using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace PotionShop
{
    /// <summary>
    /// Unity Editor menüsünden çalıştırılacak yardımcı araç.
    /// Tüm RecipeData asset'lerine seviye, tier ve kadim para değerlerini toplu atar.
    /// Tüm nadir ItemData asset'lerine kadimParaPrice değeri atar.
    /// 
    /// Kullanım: Unity menüsünden "Potion Shop / Tarif ve Eşya Değerlerini Toplu Ata" seçin.
    /// </summary>
    public class RecipeSetupEditor : EditorWindow
    {
        [MenuItem("Potion Shop/Tarif ve Eşya Değerlerini Toplu Ata")]
        public static void SetupAll()
        {
            PotionEconomySetupEditor.ApplyEconomySettings();
            int recipeCount = SetupRecipes();
            int itemCount = SetupKadimParaItems();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Toplu Atama Tamamlandı",
                $"{recipeCount} tarif güncellendi.\n{itemCount} eşyaya Kadim Para fiyatı atandı.",
                "Tamam"
            );
        }

        /// <summary>
        /// Tüm RecipeData asset'lerini basePrice'a göre sıralar ve
        /// unlockLevel, tier, kadimParaReward değerlerini otomatik atar.
        /// </summary>
        private static int SetupRecipes()
        {
            // Tüm RecipeData asset'lerini bul
            string[] guids = AssetDatabase.FindAssets("t:RecipeData");
            List<RecipeData> allRecipes = new List<RecipeData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (recipe != null && recipe.resultPotion != null)
                {
                    allRecipes.Add(recipe);
                }
            }

            // Özsu tariflerini ve iksir tariflerini ayır
            List<RecipeData> essenceRecipes = new List<RecipeData>();
            List<RecipeData> potionRecipes = new List<RecipeData>();

            foreach (var recipe in allRecipes)
            {
                if (recipe.resultPotion.itemType == ItemType.Essence)
                {
                    essenceRecipes.Add(recipe);
                }
                else if (recipe.resultPotion.itemType == ItemType.Potion)
                {
                    potionRecipes.Add(recipe);
                }
            }

            // Özsu tarifleri: her zaman açık (seviye 1), tier 1
            foreach (var recipe in essenceRecipes)
            {
                Undo.RecordObject(recipe, "Tarif Değer Atama");
                recipe.unlockLevel = 1;
                recipe.tier = 1;
                recipe.kadimParaReward = 0;
                EditorUtility.SetDirty(recipe);
            }

            // İksir tariflerini fiyata göre sırala
            potionRecipes = potionRecipes.OrderBy(r => r.resultPotion.basePrice).ToList();

            // Fiyat-Seviye-Tier eşleştirme tablosu
            // İksir tierları tariflerin sayfa sıralamasına bağlıdır (Sayfa 1 = Tier 1, Sayfa 2 = Tier 2 ... Sayfa 7 = Tier 7)
            // Her sayfada 5 iksir yer alır (Kırmızı, Mavi, Yeşil, Sarı, Beyaz).
            var levelAssignments = new Dictionary<string, (int level, int tier, int kadimPara)>
            {
                // Sayfa 1 (Tier 1 - İlk İksirler) (Seviye 1-10)
                { "Regen_Potion", (1, 1, 0) },
                { "Mana_Potion", (3, 1, 0) },
                { "Stamina_Potion", (5, 1, 0) },
                { "Defense_Potion", (7, 1, 0) },
                { "Purify_Potion", (9, 1, 0) },
                
                // Sayfa 2 (Tier 2) (Seviye 11-20)
                { "Small_Potion", (11, 2, 0) },
                { "Runic_Potion", (13, 2, 0) },
                { "Agility_Potion", (15, 2, 0) },
                { "Rally_Vial", (17, 2, 0) },
                { "Cleanse_Vial", (19, 2, 0) },
                
                // Sayfa 3 (Tier 3) (Seviye 21-30)
                { "Minor_Healing_Potion", (21, 3, 0) },
                { "Minor_Mana_Potion", (23, 3, 0) },
                { "Antidote", (25, 3, 0) },
                { "Shock_Cure", (27, 3, 0) },
                { "Holy_Water", (29, 3, 0) },
                
                // Sayfa 4 (Tier 4) (Seviye 31-40)
                { "Major_Healing_Potion", (31, 4, 0) },
                { "Starfire_Potion", (33, 4, 0) },
                { "Herbal_Brew", (35, 4, 0) },
                { "Transmutation_Flask", (37, 4, 0) },
                { "Sacred_Water", (39, 4, 0) },
                
                // Sayfa 5 (Tier 5) (Seviye 41-50)
                { "Elixir_Of_Life", (41, 5, 5) },
                { "Ether_Vial", (43, 5, 0) },
                { "Focus_Potion", (45, 5, 0) },
                { "Muscle_Tonic", (47, 5, 0) },
                { "Thaw_Potion", (49, 5, 0) },
                
                // Sayfa 6 (Tier 6) (Seviye 51-60)
                { "Life_Vial", (51, 6, 6) },
                { "Wizards_Brew", (53, 6, 0) },
                { "Sprint_Elixir", (55, 6, 0) },
                { "Phoenix_Draught", (57, 6, 7) },
                { "Purge_Elixir", (59, 6, 7) },
                
                // Sayfa 7 (Tier 7) (Seviye 61-70)
                { "Crimson_Elixir", (61, 7, 8) },
                { "Void_Potion", (63, 7, 8) },
                { "Swift_Brew", (65, 7, 0) },
                { "Stone_Cure", (67, 7, 0) },
                { "Arcane_Flask", (70, 7, 10) },
            };

            int updatedCount = 0;

            foreach (var recipe in potionRecipes)
            {
                string potionName = recipe.resultPotion.name;
                
                if (levelAssignments.TryGetValue(potionName, out var assignment))
                {
                    Undo.RecordObject(recipe, "Tarif Değer Atama");
                    recipe.unlockLevel = assignment.level;
                    recipe.tier = assignment.tier;
                    recipe.kadimParaReward = assignment.kadimPara;
                    EditorUtility.SetDirty(recipe);
                    updatedCount++;
                    Debug.Log($"[Tarif Atama] {potionName}: Seviye={assignment.level}, Tier={assignment.tier}, KadimPara={assignment.kadimPara}");
                }
                else
                {
                    // Eşleşmeyen tarifler için basePrice'a göre otomatik ata
                    int bp = recipe.resultPotion.basePrice;
                    int autoLevel;
                    int autoTier;
                    int autoKadim = 0;

                    if (bp <= 80) { autoLevel = Mathf.Max(1, bp / 10); autoTier = 1; }
                    else if (bp <= 140) { autoLevel = 11 + (bp - 85) / 5; autoTier = 2; }
                    else if (bp <= 220) { autoLevel = 27 + (bp - 145) / 8; autoTier = 3; }
                    else if (bp <= 300) { autoLevel = 41 + (bp - 225) / 8; autoTier = 4; }
                    else { autoLevel = 55 + (bp - 300) / 15; autoTier = 5; autoKadim = bp / 50; }

                    autoLevel = Mathf.Clamp(autoLevel, 1, 70);

                    Undo.RecordObject(recipe, "Tarif Değer Atama");
                    recipe.unlockLevel = autoLevel;
                    recipe.tier = autoTier;
                    recipe.kadimParaReward = autoKadim;
                    EditorUtility.SetDirty(recipe);
                    updatedCount++;
                    Debug.Log($"[Tarif Atama - Otomatik] {potionName}: Seviye={autoLevel}, Tier={autoTier}, KadimPara={autoKadim}");
                }
            }

            return updatedCount + essenceRecipes.Count;
        }

        /// <summary>
        /// Pahalı/nadir malzemelere (buyPrice >= 40) Kadim Para fiyatı atar.
        /// </summary>
        private static int SetupKadimParaItems()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                
                if (item == null) continue;

                // Sadece Ingredient türü ve buyPrice >= 40 olan malzemeler
                if (item.itemType == ItemType.Ingredient && item.buyPrice >= 40)
                {
                    // Kadim Para fiyatı: buyPrice'ın yaklaşık 1/10'u
                    int kadimPrice = Mathf.Max(1, item.buyPrice / 10);
                    
                    Undo.RecordObject(item, "Kadim Para Fiyat Atama");
                    item.kadimParaPrice = kadimPrice;
                    EditorUtility.SetDirty(item);
                    updatedCount++;
                    Debug.Log($"[Kadim Para] {item.itemName}: kadimParaPrice={kadimPrice} (buyPrice={item.buyPrice})");
                }
            }

            return updatedCount;
        }
    }
}
