using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    public enum LootBoxTier 
    { 
        Bronze = 0, 
        Silver = 1, 
        Gold = 2 
    }

    public enum RewardType
    {
        Gold,
        KadimPara,
        Item
    }

    [Serializable]
    public struct LootReward
    {
        public RewardType Type;
        public int amount;
        public ItemData item; 
    }

    /// <summary>
    /// Ganimet kutusu sistemini yöneten statik sınıf.
    /// </summary>
    public static class LootBoxSystem
    {
        private static int[] lootBoxCounts = new int[3];

        /// <summary>
        /// Kutu açıldığında tetiklenen event. (Tier, Ödüller)
        /// </summary>
        public static event Action<LootBoxTier, List<LootReward>> OnLootBoxOpened;

        /// <summary>
        /// Belirtilen tier'den bir adet kutu ekler.
        /// </summary>
        /// <param name="tier">Kutu seviyesi</param>
        public static void AddLootBox(LootBoxTier tier)
        {
            int index = (int)tier;
            if (index >= 0 && index < lootBoxCounts.Length)
            {
                lootBoxCounts[index]++;
                Debug.Log($"{tier} Loot Box eklendi. Toplam {tier}: {lootBoxCounts[index]}");
            }
        }

        /// <summary>
        /// Belirtilen tier'den kutu açar. Kutunun içeriğini döner.
        /// </summary>
        /// <param name="tier">Açılacak kutu seviyesi</param>
        /// <returns>Kazanılan ödül listesi</returns>
        public static List<LootReward> OpenLootBox(LootBoxTier tier)
        {
            int index = (int)tier;
            if (index < 0 || index >= lootBoxCounts.Length || lootBoxCounts[index] <= 0)
            {
                Debug.LogWarning($"{tier} kutusu bulunmuyor!");
                return new List<LootReward>();
            }

            lootBoxCounts[index]--;
            List<LootReward> rewards = new List<LootReward>();

            int goldAmount = 0;
            int kadimParaChance = 0;
            int itemChance = 0;

            switch (tier)
            {
                case LootBoxTier.Bronze:
                    goldAmount = UnityEngine.Random.Range(50, 201);
                    kadimParaChance = 10;
                    itemChance = 0;
                    break;
                case LootBoxTier.Silver:
                    goldAmount = UnityEngine.Random.Range(200, 501);
                    kadimParaChance = 30;
                    itemChance = 20;
                    break;
                case LootBoxTier.Gold:
                    goldAmount = UnityEngine.Random.Range(500, 1501);
                    kadimParaChance = 60;
                    itemChance = 50;
                    break;
            }

            // Altın Ödülü
            rewards.Add(new LootReward { Type = RewardType.Gold, amount = goldAmount, item = null });
            
            // Kadim Para Şansı
            if (UnityEngine.Random.Range(0, 100) < kadimParaChance)
            {
                rewards.Add(new LootReward { Type = RewardType.KadimPara, amount = UnityEngine.Random.Range(1, 4), item = null });
            }

            // Eşya Şansı
            if (itemChance > 0 && UnityEngine.Random.Range(0, 100) < itemChance)
            {
                // ItemData rastgele atanabilir, varsayılan null
                rewards.Add(new LootReward { Type = RewardType.Item, amount = 1, item = null });
            }

            OnLootBoxOpened?.Invoke(tier, rewards);
            return rewards;
        }

        public static int GetLootBoxCount(LootBoxTier tier)
        {
            return lootBoxCounts[(int)tier];
        }
    }
}
