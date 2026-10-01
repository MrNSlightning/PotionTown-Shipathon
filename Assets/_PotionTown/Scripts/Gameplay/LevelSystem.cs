using UnityEngine;
using System;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Seviye ve XP yönetim sistemi.
    /// Maks seviye 70. İksir satışından XP kazanılır.
    /// Tarifler seviyeye göre açılır.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem Instance { get; private set; }

        // Statik kalıcı veriler (sahne geçişlerinde korunur)
        private static int _currentLevel = 1;
        private static bool _isInitialized = false;

        public const int MAX_LEVEL = 10; // Max level is now 10 based on LevelDesignDatabase

        // Genel erişim
        public static int CurrentLevel 
        {
            get
            {
                EnsureInitialized();
                return _currentLevel;
            }
        }
        public static int CurrentXP => 0;

        // Olaylar
        public static event Action<int> OnLevelUp;       // Yeni seviye
        public static event Action<int> OnXPGained;      // XP kazanıldığında (geriye dönük uyumluluk)

        public static int GetXPForLevel(int level) => 0;
        public static int GetXPToNextLevel() => 0;
        public static void AddXP(int amount) => OnXPGained?.Invoke(amount);

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            }
        }

        private void Awake()
        {
            Instance = this;
            EnsureInitialized();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        public static void AdvanceLevel()
        {
            EnsureInitialized();
            
            if (_currentLevel >= MAX_LEVEL)
            {
                Debug.Log("Maksimum seviyedesiniz.");
                return;
            }

            _currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", _currentLevel);
            PlayerPrefs.Save();
            
            Debug.Log($"<color=yellow>YENİ GÜN / SEVİYE!</color> Seviye: {_currentLevel}");
            OnLevelUp?.Invoke(_currentLevel);
        }

        /// <summary>
        /// Belirtilen tarifin mevcut seviyede açık olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsRecipeUnlocked(RecipeData recipe)
        {
            if (recipe == null) return false;
            return CurrentLevel >= recipe.unlockLevel;
        }

        /// <summary>
        /// Açılmış tariflerin listesini döndürür.
        /// RecipeDatabase'den tüm tarifleri çekip seviye filtresi uygular.
        /// </summary>
        public static List<RecipeData> GetUnlockedRecipes()
        {
            List<RecipeData> unlocked = new List<RecipeData>();

            if (RecipeDatabase.Instance == null) return unlocked;

            foreach (var recipe in RecipeDatabase.Instance.allRecipes)
            {
                if (recipe != null && IsRecipeUnlocked(recipe))
                {
                    unlocked.Add(recipe);
                }
            }

            return unlocked;
        }

        /// <summary>
        /// Müşterilerin isteyebileceği açılmış iksir listesini döndürür.
        /// Sadece Potion türündeki çıktıları döndürür (Essence ve Scroll hariç).
        /// </summary>
        public static List<ItemData> GetUnlockedPotions()
        {
            List<ItemData> potions = new List<ItemData>();

            if (RecipeDatabase.Instance == null) return potions;

            foreach (var recipe in RecipeDatabase.Instance.allRecipes)
            {
                if (recipe != null && IsRecipeUnlocked(recipe) && recipe.resultPotion != null)
                {
                    // Sadece Potion türü — Essence ve diğer türleri hariç tut
                    if (recipe.resultPotion.itemType == ItemType.Potion && !potions.Contains(recipe.resultPotion))
                    {
                        potions.Add(recipe.resultPotion);
                    }
                }
            }

            return potions;
        }

        /// <summary>
        /// Seviye adını döndürür.
        /// </summary>
        public static string GetLevelTitle()
        {
            int lvl = CurrentLevel;
            if (lvl <= 10) return LocalizationManager.Get("lvl_title_apprentice");
            if (lvl <= 25) return LocalizationManager.Get("lvl_title_journeyman");
            if (lvl <= 40) return LocalizationManager.Get("lvl_title_master");
            if (lvl <= 55) return LocalizationManager.Get("lvl_title_grandmaster");
            return LocalizationManager.Get("lvl_title_legend");
        }
    }
}
