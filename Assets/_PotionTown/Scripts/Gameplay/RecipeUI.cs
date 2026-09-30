using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PotionShop
{
    public class RecipeUI : MonoBehaviour
    {
        [Header("Tarif Verileri")]
        public List<RecipeData> allRecipes;

        [Header("UI Referansları")]
        public GameObject recipePanel; // Duvardaki parşömene tıklayınca açılacak büyük panel
        public Transform recipeListContainer; // Tarif UI ögelerinin parent'ı (ScrollRect Content)
        public GameObject recipeTemplatePrefab; // Tek bir tarifi gösterecek Prefab

        private void Start()
        {
            if (recipePanel != null)
                recipePanel.SetActive(false);

            PopulateRecipes();
        }

        private void PopulateRecipes()
        {
            if (recipeTemplatePrefab == null || recipeListContainer == null) return;

            // Önceki çocukları temizle
            foreach (Transform child in recipeListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var recipe in allRecipes)
            {
                GameObject newRecipeEntry = Instantiate(recipeTemplatePrefab, recipeListContainer);
                
                // Prefab içindeki UI öğelerini bul
                // Bu kısım projenin UI tasarımına göre şekillenir.
                // Örneğin: Image bileşenlerini sırasıyla bulup Özsu, Malzeme ve Sonuç İksirini atayabiliriz.
                Image[] icons = newRecipeEntry.GetComponentsInChildren<Image>();
                
                // İlk ikonları gereken malzemelere ata (prefabdaki ikon sayısı kadar)
                int i = 0;
                for (; i < recipe.requiredItems.Count && i < icons.Length - 1; i++)
                {
                    icons[i].sprite = recipe.requiredItems[i].itemIcon;
                }
                
                // En son ikonu sonuç iksirine ata (eğer en az 1 ikon varsa)
                if (icons.Length > 0)
                {
                    // Eğer istenilen malzeme sayısından daha az image varsa en sondakini sonuç olarak kullanır
                    int resultIndex = Mathf.Min(i, icons.Length - 1);
                    icons[resultIndex].sprite = recipe.resultPotion.itemIcon;
                }
            }
        }

        public void OpenRecipePanel()
        {
            if (recipePanel != null)
                recipePanel.SetActive(true);
        }

        public void CloseRecipePanel()
        {
            if (recipePanel != null)
                recipePanel.SetActive(false);
        }
    }
}
