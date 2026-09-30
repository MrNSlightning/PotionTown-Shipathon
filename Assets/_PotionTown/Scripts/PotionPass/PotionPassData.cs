using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    public enum PassRewardType 
    { 
        Gold, 
        KadimPara, 
        Item, 
        Character, 
        ShelfUnlock, 
        LootBox 
    }

    [System.Serializable]
    public class PassTierReward
    {
        [Tooltip("Aşama numarası (1-30)")]
        public int tier;                    // 1-30
        
        [Tooltip("Ödül türü")]
        public PassRewardType rewardType;
        
        [Tooltip("Altın/Kadim Para veya miktar (Örn: LootBox)")]
        public int amount;                  // Gold/KadimPara amount
        
        [Tooltip("Eşya türü ödülü için eşya verisi")]
        public ItemData itemReward;         // For Item type
        
        [Tooltip("Karakter türü ödülü için karakter prefab'ı")]
        public GameObject characterPrefab;  // For Character type
        
        [Tooltip("Raf açma ödülü için raf ID'si")]
        public string shelfId;              // For ShelfUnlock type  
        
        [Tooltip("Kullanıcı arayüzünde gösterilecek ikon")]
        public Sprite rewardIcon;           // Icon shown in UI
        
        [Tooltip("Türkçe açıklama")]
        public string rewardDescription;    // Turkish description
    }

    [CreateAssetMenu(fileName = "NewPotionPassData", menuName = "Potion Shop/Potion Pass Data")]
    public class PotionPassData : ScriptableObject
    {
        [Header("Genel Ayarlar")]
        public string seasonName = "Sezon 1: Büyülü Başlangıç";
        public Sprite passIcon;
        public int totalTiers = 30;
        public int xpPerTier = 100;
        public int premiumPriceKadimPara = 500;

        [Header("Ödüller")]
        public List<PassTierReward> freeTrackRewards = new List<PassTierReward>();
        public List<PassTierReward> premiumTrackRewards = new List<PassTierReward>();

        public void PopulateDefaults(int count = 10)
        {
            totalTiers = count;
            seasonName = "SEZON 1: SİMYACININ YOLU";
            xpPerTier = 200;
            premiumPriceKadimPara = 50;

            freeTrackRewards.Clear();
            premiumTrackRewards.Clear();

            for (int i = 1; i <= count; i++)
            {
                // Free Track
                PassRewardType fType = (i % 3 == 0) ? PassRewardType.LootBox : (i == 8 ? PassRewardType.KadimPara : PassRewardType.Gold);
                int fAmount = (fType == PassRewardType.Gold) ? (i * 100) : ((fType == PassRewardType.LootBox) ? 1 : 2);
                string fDesc = (fType == PassRewardType.Gold) ? $"+{fAmount} Altın" : ((fType == PassRewardType.LootBox) ? "1x Bronz Kasa" : $"+{fAmount} Kadim Para");

                freeTrackRewards.Add(new PassTierReward
                {
                    tier = i,
                    rewardType = fType,
                    amount = fAmount,
                    rewardDescription = fDesc
                });

                // Premium Track
                PassRewardType pType = (i % 3 == 0) ? PassRewardType.LootBox : (i % 2 == 0 ? PassRewardType.KadimPara : PassRewardType.Gold);
                int pAmount = (pType == PassRewardType.Gold) ? (i * 250) : ((pType == PassRewardType.KadimPara) ? (i * 5) : 1);
                string pDesc = (pType == PassRewardType.Gold) ? $"+{pAmount} Altın" : ((pType == PassRewardType.KadimPara) ? $"+{pAmount} Kadim Para" : "1x Altın Kasa");

                premiumTrackRewards.Add(new PassTierReward
                {
                    tier = i,
                    rewardType = pType,
                    amount = pAmount,
                    rewardDescription = pDesc
                });
            }
        }
    }
}
