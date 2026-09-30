using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    [System.Serializable]
    public class LevelDesignConfig
    {
        [Header("Müşteri Hedefi")]
        [Tooltip("Bu seviyede toplam kaç müşteri gelecek?")]
        public int totalCustomers = 5; 

        [Header("Sipariş Havuzu")]
        [Tooltip("Müşterilerin bu seviyede isteyebileceği iksirler")]
        public List<ItemData> possiblePotions = new List<ItemData>();
        
        [Header("Müşteri Geliş Hızı (Saniye)")]
        public float minSpawnDelay = 2f;
        public float maxSpawnDelay = 5f;
        
        [Header("Sipariş Adedi")]
        [Tooltip("Bir müşteri en az kaç iksir isteyebilir?")]
        public int minOrderCount = 1; 
        [Tooltip("Bir müşteri en fazla kaç iksir isteyebilir?")]
        public int maxOrderCount = 2; 
        
        [Header("Zorluk (Sabır Süreleri)")]
        [Tooltip("Müşterinin ruh halinin düşmesi için gereken süre (saniye). Düşük = Zor")]
        public float timePerMood = 15f; 
        [Tooltip("Çoklu siparişlerde doğru teslimat yapınca kazanılan ek süre")]
        public float deliveryBonusTime = 10f; 

        [Header("Ekonomi")]
        [Tooltip("Bu seviyedeki iksirlerden kazanılan paranın çarpanı (1 = normal fiyat, 1.5 = %50 daha pahalı vb.)")]
        public float potionPriceMultiplier = 1f;
    }

    [CreateAssetMenu(fileName = "LevelDesignDatabase", menuName = "Potion Shop/Level Design Database")]
    public class LevelDesignDatabase : ScriptableObject
    {
        [Tooltip("1. Seviyeden itibaren her seviyenin tasarımı (İndeks 0 = Seviye 1)")]
        public List<LevelDesignConfig> levels = new List<LevelDesignConfig>();

        private static LevelDesignDatabase _instance;
        public static LevelDesignDatabase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<LevelDesignDatabase>("LevelDesignDatabase");
                return _instance;
            }
        }

        public LevelDesignConfig GetLevelConfig(int level)
        {
            // Seviyeye karşılık gelen tasarımı döndür, eğer o seviye yoksa en son tasarımı kullan
            if (levels == null || levels.Count == 0) return null;
            
            int index = level - 1;
            if (index < 0) index = 0;
            if (index >= levels.Count) index = levels.Count - 1;

            return levels[index];
        }
    }
}
