using UnityEngine;

namespace PotionShop
{
    [CreateAssetMenu(menuName = "Potion Shop/MiniGame Reward")]
    public class MiniGameRewardData : ScriptableObject
    {
        [Header("Altın Ödülleri")]
        [Tooltip("Her 1 skor puanı için verilecek altın miktarı.")]
        public int goldPerPoint = 2;

        [Header("Kadim Para Ödülleri")]
        [Tooltip("Kadim Para kazanma şansı (Yüzde).")]
        [Range(0, 100)] public int kadimParaChance = 10;
        
        [Tooltip("Kazanılacak Kadim Para miktarı.")]
        public int kadimParaAmount = 1;

        [Header("Eşya Düşürme")]
        [Tooltip("Düşebilecek olası eşyalar.")]
        public ItemData[] possibleItemDrops;
        
        [Tooltip("Eşya düşme şansı (Yüzde).")]
        [Range(0, 100)] public int itemDropChance = 15;

        [Header("Loot Box Ödülleri")]
        [Tooltip("Loot Box (Ganimet Kutusu) kazanma şansı (Yüzde).")]
        [Range(0, 100)] public int lootBoxChance = 5;
    }
}
