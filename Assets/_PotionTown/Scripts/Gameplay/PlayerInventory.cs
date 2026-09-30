using UnityEngine;
using System.Collections.Generic;
using System;

namespace PotionShop
{
    [Serializable]
    public class StartingInventory
    {
        public ItemData item;
        public int amount;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        public List<StartingInventory> startingItems;
        
        // Asıl envanter verisini tüm alanlarda/dükkanlarda ortak tutan sözlük (Dictionary)
        private static Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();
        private static HashSet<PlayerInventory> _initializedInstances = new HashSet<PlayerInventory>();

        public static Action onInventoryChangedStatic;
        public event Action OnInventoryChanged
        {
            add => onInventoryChangedStatic += value;
            remove => onInventoryChangedStatic -= value;
        }

        private void Awake()
        {
            Instance = this;

            // Sabit ve dinamik rafları erkenden ilklendir
            FixedShelfManager.EnsureInitializedStatic();
            ShelfSlot.EnsureAllSlotsCached();
            
            // Sahnedeki her PlayerInventory nesnesinin (farklı dükkanların) başlangıç eşyalarını ortak envantere ekle
            if (!_initializedInstances.Contains(this))
            {
                _initializedInstances.Add(this);
                if (startingItems != null)
                {
                    foreach (var startItem in startingItems)
                    {
                        if (startItem != null && startItem.item != null)
                        {
                            if (inventory.TryGetValue(startItem.item, out int existingCount))
                            {
                                inventory[startItem.item] = Mathf.Max(existingCount, startItem.amount);
                            }
                            else
                            {
                                inventory[startItem.item] = startItem.amount;
                            }
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            Instance = this;
        }

        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            if (inventory.TryGetValue(item, out int count))
            {
                return count;
            }
            return 0;
        }

        public void AddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return;

            if (inventory.ContainsKey(item))
                inventory[item] += amount;
            else
                inventory[item] = amount;

            onInventoryChangedStatic?.Invoke();
        }

        public bool RemoveItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            int currentCount = GetItemCount(item);
            if (currentCount >= amount)
            {
                inventory[item] = currentCount - amount;
                if (inventory[item] <= 0)
                {
                    inventory.Remove(item);
                }
                onInventoryChangedStatic?.Invoke();
                return true;
            }
            return false; // Yeterli miktar yok
        }

        /// <summary>
        /// Envanterdeki mevcut eşyaları döndürür.
        /// filterHiddenTypes true ise Özler (Essence) ve Parşömenler (Scroll) filtrelenir (gizlenir).
        /// </summary>
        public List<KeyValuePair<ItemData, int>> GetInventoryItems(bool filterHiddenTypes = true)
        {
            // Ekranda gösterilecek eşyaları ve adetlerini tutan geçici sözlük
            Dictionary<ItemData, int> displayDict = new Dictionary<ItemData, int>();

            // 1. Envanterde gerçekten var olan (adeti > 0) eşyaları ekle
            foreach (var kvp in inventory)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                displayDict[kvp.Key] = kvp.Value;
            }

            // 2. Lisansı alınmış ama elimizde 0 tane olan "yapılabilir iksirleri" ve "malzemeleri" de listeye dahil et
            if (RecipeDatabase.Instance != null)
            {
                foreach (var recipe in RecipeDatabase.Instance.allRecipes)
                {
                    if (recipe != null && LicenseManager.HasLicense(recipe))
                    {
                        // İksiri ekle (eğer listede yoksa 0 adet olarak eklenir)
                        if (recipe.resultPotion != null && !displayDict.ContainsKey(recipe.resultPotion))
                        {
                            displayDict[recipe.resultPotion] = 0;
                        }

                        // Tarifin malzemelerini ekle
                        if (recipe.requiredItems != null)
                        {
                            foreach (var reqItem in recipe.requiredItems)
                            {
                                if (reqItem != null && !displayDict.ContainsKey(reqItem))
                                {
                                    displayDict[reqItem] = 0;
                                }
                            }
                        }
                    }
                }
            }

            // 3. İstenmeyenleri (Özsular, Özel Eşyalar, Zaten Rafta Olanlar) Filtrele
            List<KeyValuePair<ItemData, int>> finalResult = new List<KeyValuePair<ItemData, int>>();
            
            foreach (var kvp in displayDict)
            {
                if (filterHiddenTypes)
                {
                    // Sadece İksir ve Malzeme (Ingredient) göster. Özsuları (Essence) GİZLE.
                    if (kvp.Key.itemType != ItemType.Ingredient && kvp.Key.itemType != ItemType.Potion)
                    {
                        continue;
                    }

                    // Raflara (sabit veya dinamik) yerleştirilmiş eşyaları envanterde gösterme
                    if (ShelfSlot.IsItemOnAnyShelf(kvp.Key))
                        continue;
                }

                finalResult.Add(kvp);
            }

            return finalResult;
        }

        /// <summary>
        /// Tüm envanter sözlüğüne salt okunur erişim sağlar.
        /// </summary>
        public IReadOnlyDictionary<ItemData, int> GetAllInventory()
        {
            return inventory;
        }
    }
}
