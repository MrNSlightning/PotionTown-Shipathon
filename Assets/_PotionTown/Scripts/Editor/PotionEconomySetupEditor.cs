using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// PotionTown oyun ekonomisi ve fiyatlandırma otomasyonu.
    /// Tüm öz malzemeleri, ara malzemeler, mantarlar, çiçekler, nadir bileşenler
    /// ve 35 iksirin fiyatlarını (buyPrice ve basePrice) matematiksel dengeye göre toplu atar.
    /// 
    /// Kullanım: Unity menüsünden "Potion Shop / Ekonomi ve Fiyatlandırmayı Uygula" seçin.
    /// </summary>
    public class PotionEconomySetupEditor : EditorWindow
    {
        // Malzeme ve Öz fiyatları (buyPrice)
        private static readonly Dictionary<string, int> IngredientBuyPrices = new Dictionary<string, int>
        {
            // --- 1. Öz Tarifleri Bileşenleri (Sayfa 0) ---
            // Kırmızı Öz Bileşenleri (5 Altın)
            { "Scroll_Of_Fire", 5 },
            { "Red_Powder", 5 },
            { "Ember_Seed", 5 },
            // Mavi Öz Bileşenleri (6 Altın)
            { "Scroll_Of_Teleport", 6 },
            { "Blue_Crystal_Dust", 6 },
            { "Vortex_Leaf", 6 },
            // Yeşil Öz Bileşenleri (7 Altın)
            { "Scroll_Of_Healing", 7 },
            { "Healing_Herb", 7 },
            { "Life_Seed", 7 },
            // Sarı Öz Bileşenleri (8 Altın)
            { "Scroll_Of_Protection", 8 },
            { "Sun_Pollen", 8 },
            { "Amber_Resin", 8 },
            // Beyaz Öz Bileşenleri (9 Altın)
            { "Scroll_Of_Identify", 9 },
            { "Frozen_Tear", 9 },
            { "Soul_Fragment", 9 },

            // --- Özlerin Kendileri (Üretim Maliyetine Eşit Fiyat) ---
            { "Red_Essence", 15 },
            { "Blue_Essence", 18 },
            { "Green_Essence", 21 },
            { "Yellow_Essence", 24 },
            { "White_Essence", 27 },

            // --- 2. Sayfa 1: Mantarlar (10 - 14 Altın) ---
            { "Culture_Mushroom", 10 },
            { "Sky_Mushroom", 11 },
            { "Forest_Mushroom", 12 },
            { "Golden_Mushroom", 13 },
            { "Maple_Mushroom", 14 },

            // --- 3. Sayfa 2: Çiçekler (15 - 19 Altın) ---
            { "Love_Flower", 15 },
            { "Night_Flower", 16 },
            { "Suprise_Flower", 17 },
            { "Flame_Flower", 18 },
            { "Grape_Flower", 19 },

            // --- 4. Sayfa 3: Tier 3 Malzemeleri (20 - 24 Altın) ---
            { "Crimson_Root", 20 },
            { "Sky_Ore", 21 },
            { "Emerald_Mushroom", 22 },
            { "Sun_Flower", 23 },
            { "Moon_Fruit", 24 },

            // --- 5. Sayfa 5: Tier 5 Malzemeleri (25 - 34 Altın) ---
            { "Lava_Stone", 25 },
            { "Mana_Crystal", 26 },
            { "Venom_Spike", 27 },
            { "Solar_Extract", 28 },
            { "Lunar_Essence_Item", 29 },
            { "Ruby_Crystal", 30 },
            { "Void_Stone", 31 },
            { "Dragon_Egg", 32 },
            { "Lightning_Seed", 33 },
            { "Polar_Flower", 34 }
        };

        // İksir Satış Fiyatları (basePrice - 5'in katı, artan kazanç)
        private static readonly Dictionary<string, int> PotionBasePrices = new Dictionary<string, int>
        {
            // --- Sayfa 1 (Tier 1) ---
            // Regen: Maliyet 25 + Kazanç 15 = 40
            { "Regen_Potion", 40 },
            // Mana: Maliyet 29 + Kazanç 21 = 50
            { "Mana_Potion", 50 },
            // Stamina: Maliyet 33 + Kazanç 27 = 60
            { "Stamina_Potion", 60 },
            // Defense: Maliyet 37 + Kazanç 33 = 70
            { "Defense_Potion", 70 },
            // Purify: Maliyet 41 + Kazanç 39 = 80
            { "Purify_Potion", 80 },

            // --- Sayfa 2 (Tier 2) ---
            // Small: Maliyet 30 + Kazanç 60 = 90
            { "Small_Potion", 90 },
            // Runic: Maliyet 34 + Kazanç 66 = 100
            { "Runic_Potion", 100 },
            // Agility: Maliyet 38 + Kazanç 72 = 110
            { "Agility_Potion", 110 },
            // Rally: Maliyet 42 + Kazanç 78 = 120
            { "Rally_Vial", 120 },
            // Cleanse: Maliyet 46 + Kazanç 84 = 130
            { "Cleanse_Vial", 130 },

            // --- Sayfa 3 (Tier 3) ---
            // Minor Healing: Maliyet 45 + Kazanç 90 = 135
            { "Minor_Healing_Potion", 135 },
            // Minor Mana: Maliyet 50 + Kazanç 95 = 145
            { "Minor_Mana_Potion", 145 },
            // Antidote: Maliyet 55 + Kazanç 100 = 155
            { "Antidote", 155 },
            // Shock Cure: Maliyet 60 + Kazanç 105 = 165
            { "Shock_Cure", 165 },
            // Holy Water: Maliyet 65 + Kazanç 110 = 175
            { "Holy_Water", 175 },

            // --- Sayfa 4 (Tier 4) ---
            // Major Healing: Maliyet 50 + Kazanç 130 = 180
            { "Major_Healing_Potion", 180 },
            // Starfire: Maliyet 55 + Kazanç 135 = 190
            { "Starfire_Potion", 190 },
            // Herbal Brew: Maliyet 60 + Kazanç 140 = 200
            { "Herbal_Brew", 200 },
            // Transmutation: Maliyet 65 + Kazanç 145 = 210
            { "Transmutation_Flask", 210 },
            // Sacred Water: Maliyet 70 + Kazanç 150 = 220
            { "Sacred_Water", 220 },

            // --- Sayfa 5 (Tier 5) ---
            // Elixir of Life: Maliyet 70 + Kazanç 160 = 230
            { "Elixir_Of_Life", 230 },
            // Ether Vial: Maliyet 75 + Kazanç 165 = 240
            { "Ether_Vial", 240 },
            // Focus Potion: Maliyet 80 + Kazanç 170 = 250
            { "Focus_Potion", 250 },
            // Muscle Tonic: Maliyet 85 + Kazanç 175 = 260
            { "Muscle_Tonic", 260 },
            // Thaw Potion: Maliyet 90 + Kazanç 180 = 270
            { "Thaw_Potion", 270 },

            // --- Sayfa 6 (Tier 6) ---
            // Life Vial: Maliyet 60 + Kazanç 220 = 280
            { "Life_Vial", 280 },
            // Wizards Brew: Maliyet 65 + Kazanç 225 = 290
            { "Wizards_Brew", 290 },
            // Sprint Elixir: Maliyet 70 + Kazanç 230 = 300
            { "Sprint_Elixir", 300 },
            // Phoenix Draught: Maliyet 75 + Kazanç 235 = 310
            { "Phoenix_Draught", 310 },
            // Purge Elixir: Maliyet 80 + Kazanç 240 = 320
            { "Purge_Elixir", 320 },

            // --- Sayfa 7 (Tier 7) ---
            // Crimson Elixir: Maliyet 65 + Kazanç 265 = 330
            { "Crimson_Elixir", 330 },
            // Void Potion: Maliyet 70 + Kazanç 270 = 340
            { "Void_Potion", 340 },
            // Swift Brew: Maliyet 75 + Kazanç 275 = 350
            { "Swift_Brew", 350 },
            // Stone Cure: Maliyet 80 + Kazanç 280 = 360
            { "Stone_Cure", 360 },
            // Arcane Flask: Maliyet 85 + Kazanç 285 = 370
            { "Arcane_Flask", 370 },
        };

        [MenuItem("Potion Shop/Ekonomi ve Fiyatlandırmayı Uygula")]
        public static void ApplyEconomySettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            int updatedIngredients = 0;
            int updatedPotions = 0;
            int updatedEssences = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null) continue;

                string assetName = item.name;

                // 1. Öz mü?
                if (item.itemType == ItemType.Essence)
                {
                    if (IngredientBuyPrices.TryGetValue(assetName, out int essCost))
                    {
                        Undo.RecordObject(item, "Ekonomi Fiyat Güncelleme");
                        item.buyPrice = essCost;
                        item.basePrice = essCost;
                        EditorUtility.SetDirty(item);
                        updatedEssences++;
                    }
                }
                // 2. Malzeme mi?
                else if (item.itemType == ItemType.Ingredient || item.itemType == ItemType.Scroll)
                {
                    if (IngredientBuyPrices.TryGetValue(assetName, out int buyPrice))
                    {
                        Undo.RecordObject(item, "Ekonomi Fiyat Güncelleme");
                        item.buyPrice = buyPrice;
                        item.basePrice = 0;
                        EditorUtility.SetDirty(item);
                        updatedIngredients++;
                    }
                }
                // 3. İksir mi?
                else if (item.itemType == ItemType.Potion)
                {
                    if (PotionBasePrices.TryGetValue(assetName, out int basePrice))
                    {
                        Undo.RecordObject(item, "Ekonomi Fiyat Güncelleme");
                        item.basePrice = basePrice;
                        item.buyPrice = 0;
                        EditorUtility.SetDirty(item);
                        updatedPotions++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"Ekonomi ve Fiyatlandırma Başarıyla Uygulandı!\n\n" +
                         $"• Güncellenen Özler: {updatedEssences}\n" +
                         $"• Güncellenen Malzemeler: {updatedIngredients}\n" +
                         $"• Güncellenen İksirler (5'in Katı): {updatedPotions}\n\n" +
                         $"Tüm fiyatlar ve kârlar kaydedildi.";

            Debug.Log($"[PotionEconomySetupEditor] {msg}");
            EditorUtility.DisplayDialog("Fiyatlandırma Tamamlandı", msg, "Harika!");
        }
    }
}
