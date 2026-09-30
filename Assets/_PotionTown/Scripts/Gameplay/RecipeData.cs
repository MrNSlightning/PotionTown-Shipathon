using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    [CreateAssetMenu(fileName = "NewRecipeData", menuName = "Potion Shop/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Header("Gereken Malzemeler")]
        [Tooltip("Bu iksiri yapmak için kazana atılması gereken tüm eşyalar (Sıra önemli değildir)")]
        public List<ItemData> requiredItems = new List<ItemData>();

        [Header("Sonuç")]
        [Tooltip("Doğru birleşim sonucunda ortaya çıkacak iksir")]
        public ItemData resultPotion;

        [Header("Seviye & Açılma")]
        [Tooltip("Bu tarif hangi seviyede açılır (1-70)")]
        public int unlockLevel = 1;

        [Tooltip("Tarif zorluk kademesi (1-7, tarif sayfa sıralamasına bağlıdır)")]
        [Range(1, 7)]
        public int tier = 1;

        [Header("Kadim Para Ödülü")]
        [Tooltip("Bu iksir satıldığında kazanılan Kadim Para miktarı (0 = Kadim Para kazandırmaz)")]
        public int kadimParaReward = 0;

        [Header("Lisans")]
        [Tooltip("Bu tarif için lisans ücreti (Altın). 0 ise otomatik hesaplanır (basePrice * çarpan).")]
        public int licenseCost = 0;
    }
}
