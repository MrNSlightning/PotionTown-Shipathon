using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PotionShop
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Potion Shop/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        public List<RecipeData> allRecipes = new List<RecipeData>();

        private static RecipeDatabase _instance;
        public static RecipeDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<RecipeDatabase>("RecipeDatabase");
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeDatabase");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
                        }
                    }
#endif
                }
                return _instance;
            }
        }

        /// <summary>
        /// Belirli bir iksiri üreten tarifleri bulur.
        /// </summary>
        public List<RecipeData> GetRecipesForResult(ItemData potion)
        {
            if (potion == null) return new List<RecipeData>();
            return allRecipes.Where(r => r != null && r.resultPotion == potion).ToList();
        }

        /// <summary>
        /// Belirli bir malzemenin kullanıldığı tüm tarifleri bulur.
        /// </summary>
        public List<RecipeData> GetRecipesUsingItem(ItemData ingredient)
        {
            if (ingredient == null) return new List<RecipeData>();
            return allRecipes.Where(r => r != null && r.requiredItems != null && r.requiredItems.Contains(ingredient)).ToList();
        }

#if UNITY_EDITOR
        [ContextMenu("Tüm Tarif Verilerini Otomatik Bul ve Doldur")]
        public void AutoPopulateAllRecipes()
        {
            allRecipes.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeData");
            foreach (string g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                RecipeData r = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (r != null && !allRecipes.Contains(r))
                {
                    allRecipes.Add(r);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"RecipeDatabase: Toplam {allRecipes.Count} tarif kaydedildi.");
        }
#endif
    }
}
