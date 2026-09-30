using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    public enum ShopPriceType
    {
        RealMoney,
        KadimPara,
        Gold,
        RewardedAd
    }

    public enum ShopRewardType
    {
        KadimPara,
        Gold,
        RemoveAds,
        PotionPassPremium,
        LootBox,
        MiniGamePlay,
        SpecialItem
    }

    public enum ShopCategory
    {
        KadimPara,
        SpecialOffers,
        ItemBundles
    }

    [Serializable]
    public class ShopProductData
    {
        [Tooltip("Benzersiz Ürün Kimliği (Google Play / App Store ID'si ile aynı olmalıdır)")]
        public string id = "com.potiontavern.kadimpara.small";

        [Tooltip("Oyunda görünecek ana başlık")]
        public string displayName = "10 KADİM PARA";

        [Tooltip("Alt başlık veya paket türü")]
        public string subtitle = "Çırak Kesesi";

        [Tooltip("Ürün açıklaması")]
        [TextArea(2, 4)]
        public string description = "Temel iksirler ve sandıklar için Kadim Para kesesi.";

        [Tooltip("Ürün kategorisi")]
        public ShopCategory category = ShopCategory.KadimPara;

        [Tooltip("Ödeme türü")]
        public ShopPriceType priceType = ShopPriceType.RealMoney;

        [Tooltip("Gerçek para fiyatı (Örn: 19.99)")]
        public float realMoneyPrice = 19.99f;

        [Tooltip("Para birimi sembolü")]
        public string currencySymbol = "₺";

        [Tooltip("Oyun içi fiyat (Altın veya Kadim Para ise)")]
        public int ingamePrice = 500;

        [Tooltip("Verilecek ödül türü")]
        public ShopRewardType rewardType = ShopRewardType.KadimPara;

        [Tooltip("Ödül miktarı (Altın, Kadim Para vb.)")]
        public int rewardAmount = 10;

        [Tooltip("Eğer sandık ödülü ise sandık türü (Bronze, Silver, Gold)")]
        public string lootBoxTier = "Bronze";

        [Tooltip("Popüler veya En Çok Satan etiketi gösterilsin mi?")]
        public bool isPopular = false;

        [Tooltip("Kartın başlık vurgu rengi")]
        public Color cardAccentColor = new Color(0.35f, 0.20f, 0.55f, 1f);

        [Tooltip("Ürün ikonu (isteğe bağlı)")]
        public Sprite icon;

        public string GetFormattedPrice()
        {
            switch (priceType)
            {
                case ShopPriceType.RealMoney:
                    return $"{realMoneyPrice:F2} {currencySymbol}";
                case ShopPriceType.KadimPara:
                    return $"{ingamePrice} Kadim Para";
                case ShopPriceType.Gold:
                    return $"{ingamePrice} Altın";
                case ShopPriceType.RewardedAd:
                    return "[+] REKLAM İZLE";
                default:
                    return $"{realMoneyPrice:F2} {currencySymbol}";
            }
        }
    }

    [CreateAssetMenu(fileName = "ShopCatalogData", menuName = "Potion Shop/Shop Catalog Data")]
    public class ShopCatalogData : ScriptableObject
    {
        public List<ShopProductData> products = new List<ShopProductData>();

        public ShopProductData FindProduct(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return products.Find(p => p.id == id);
        }

        public void PopulateDefaults()
        {
            products.Clear();

            // Kadim Para Paketleri
            products.Add(new ShopProductData
            {
                id = IAPManager.KADIM_PARA_SMALL,
                displayName = "10 KADİM PARA",
                subtitle = "Çırak Kesesi",
                description = "Temel iksirler ve sandıklar için başlangıç kesesi.",
                category = ShopCategory.KadimPara,
                priceType = ShopPriceType.RealMoney,
                realMoneyPrice = 19.99f,
                currencySymbol = "₺",
                rewardType = ShopRewardType.KadimPara,
                rewardAmount = 10,
                isPopular = false,
                cardAccentColor = new Color(0.28f, 0.18f, 0.42f, 1f)
            });

            products.Add(new ShopProductData
            {
                id = IAPManager.KADIM_PARA_MEDIUM,
                displayName = "50 KADİM PARA",
                subtitle = "Usta Kesesi [POPÜLER]",
                description = "Taverna ustaları için en çok tercih edilen paket.\n+5 Ekstra Bonus Dahil!",
                category = ShopCategory.KadimPara,
                priceType = ShopPriceType.RealMoney,
                realMoneyPrice = 79.99f,
                currencySymbol = "₺",
                rewardType = ShopRewardType.KadimPara,
                rewardAmount = 50,
                isPopular = true,
                cardAccentColor = new Color(0.45f, 0.22f, 0.65f, 1f)
            });

            products.Add(new ShopProductData
            {
                id = IAPManager.KADIM_PARA_LARGE,
                displayName = "150 KADİM PARA",
                subtitle = "Kadim Hazine Sandığı",
                description = "Büyük simyacılar için devasa hazine.\n+30 Ekstra Bonus Dahil!",
                category = ShopCategory.KadimPara,
                priceType = ShopPriceType.RealMoney,
                realMoneyPrice = 199.99f,
                currencySymbol = "₺",
                rewardType = ShopRewardType.KadimPara,
                rewardAmount = 150,
                isPopular = false,
                cardAccentColor = new Color(0.65f, 0.45f, 0.15f, 1f)
            });

            // Özel Fırsatlar & Reklamlar
            products.Add(new ShopProductData
            {
                id = "offer.ad.gold",
                displayName = "ÜCRETSİZ ALTIN",
                subtitle = "Sponsorlu Destek",
                description = "Kısa bir sponsorlu reklam izle,\n+250 Altın anında çantana gelsin!",
                category = ShopCategory.SpecialOffers,
                priceType = ShopPriceType.RewardedAd,
                rewardType = ShopRewardType.Gold,
                rewardAmount = 250,
                isPopular = false,
                cardAccentColor = new Color(0.18f, 0.50f, 0.28f, 1f)
            });

            products.Add(new ShopProductData
            {
                id = "offer.ad.minigame",
                displayName = "EKSTRA HAK",
                subtitle = "Simya Enerjisi",
                description = "Kısa bir sponsorlu reklam izle,\n+1 Mini Oyun Hakkı anında kazan!",
                category = ShopCategory.SpecialOffers,
                priceType = ShopPriceType.RewardedAd,
                rewardType = ShopRewardType.MiniGamePlay,
                rewardAmount = 1,
                isPopular = false,
                cardAccentColor = new Color(0.18f, 0.45f, 0.60f, 1f)
            });

            products.Add(new ShopProductData
            {
                id = IAPManager.REMOVE_ADS,
                displayName = "REKLAMLARI KALDIR",
                subtitle = "Kusursuz Deneyim",
                description = "Tüm zorunlu ve geçiş reklamları kaldırılır.\nÖdüllü reklam hediyeleri tek tıkla alınır!",
                category = ShopCategory.SpecialOffers,
                priceType = ShopPriceType.RealMoney,
                realMoneyPrice = 49.99f,
                currencySymbol = "₺",
                rewardType = ShopRewardType.RemoveAds,
                rewardAmount = 1,
                isPopular = true,
                cardAccentColor = new Color(0.60f, 0.20f, 0.25f, 1f)
            });
        }
    }
}
