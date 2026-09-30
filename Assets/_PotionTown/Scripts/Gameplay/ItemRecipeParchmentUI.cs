using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Geriye dönük uyumluluk köprüsüdür.
    /// Envanter tarif parşömeni için <see cref="InventoryRecipeParchmentUI"/> kullanılmaktadır.
    /// Sahnede dükkan masası üzerindeki parşömen için <see cref="ParchmentPageController"/> kullanılmaktadır.
    /// </summary>
    public class ItemRecipeParchmentUI : MonoBehaviour
    {
        public static ItemRecipeParchmentUI Instance { get; private set; }

        public static InventoryRecipeParchmentUI GetOrCreateInstance()
        {
            return InventoryRecipeParchmentUI.GetOrCreateInstance();
        }

        public static InventoryRecipeParchmentUI CreateDefaultParchment(Transform parent = null)
        {
            return InventoryRecipeParchmentUI.GetOrCreateInstance();
        }

        public void OpenFor(ItemData item)
        {
            InventoryRecipeParchmentUI ui = InventoryRecipeParchmentUI.GetOrCreateInstance();
            if (ui != null) ui.OpenFor(item);
        }

        public void Close()
        {
            if (InventoryRecipeParchmentUI.Instance != null)
            {
                InventoryRecipeParchmentUI.Instance.Close();
            }
        }
    }
}
