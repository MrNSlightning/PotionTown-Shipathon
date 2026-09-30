using System;
using UnityEngine;

namespace PotionShop
{
    public enum IAPStoreMode
    {
        [Tooltip("Gerçek Google Play / App Store mağazası (Canlı yayın için)")]
        LiveStore,
        [Tooltip("Unity IAP Fake Store (Editor'de gerçek Google Play dialog penceresi simülasyonu)")]
        FakeStore,
        [Tooltip("Yerel test simülasyonu (Harici paket gerektirmez)")]
        LocalSimulation,
        [Tooltip("RevenueCat SDK üzerinden satın alma (Shipaton 2026 için önerilir)")]
        RevenueCat
    }

    public enum AdsMode
    {
        [Tooltip("Canlı reklamlar (Gerçek para kazandıran canlı Google/Unity reklamları)")]
        LiveProduction,
        [Tooltip("Test modu (Google ve Unity test reklamları veya görsel simülasyon)")]
        TestMode
    }

    [CreateAssetMenu(fileName = "LiveMonetizationConfig", menuName = "Potion Shop/Live Monetization Config")]
    public class LiveMonetizationConfig : ScriptableObject
    {
        private static LiveMonetizationConfig _instance;
        public static LiveMonetizationConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LiveMonetizationConfig>("LiveMonetizationConfig");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<LiveMonetizationConfig>();
                        _instance.SetDefaults();
                    }
                }
                return _instance;
            }
        }

        [Header("--- IAP (GERÇEK PARA SATIN ALMA) AYARLARI ---")]
        [Tooltip("Mağaza çalışma modu. Canlıya alırken 'LiveStore' seçilmelidir.")]
        public IAPStoreMode iapStoreMode = IAPStoreMode.FakeStore;

        [Tooltip("Unity IAP paketi yüklü değilse otomatik simülasyona geçilsin mi?")]
        public bool fallbackToSimulation = true;

        [Tooltip("Google Play Lisans Anahtarı (Base64 RSA - Google Play Console'dan kopyalanır)")]
        [TextArea(2, 3)]
        public string googlePlayLicenseKey = "";

        [Header("--- REVENUECAT SDK AYARLARI ---")]
        [Tooltip("RevenueCat Dashboard > Project > API Keys > Public Android SDK Key")]
        public string revenueCatAndroidApiKey = "YOUR_REVENUECAT_ANDROID_API_KEY";

        [Tooltip("RevenueCat Dashboard > Project > API Keys > Public iOS SDK Key")]
        public string revenueCatIOSApiKey = "YOUR_REVENUECAT_IOS_API_KEY";

        [Header("--- REKLAM (UNITY ADS / ADMOB) AYARLARI ---")]
        [Tooltip("Reklam yayın modu. Canlı yayında 'LiveProduction' seçilmelidir.")]
        public AdsMode adsMode = AdsMode.TestMode;

        [Tooltip("Unity Cloud Dashboard'dan alınan Android Game ID (Örn: 5489123)")]
        public string androidGameId = "1234566";

        [Tooltip("Unity Cloud Dashboard'dan alınan iOS Game ID (Örn: 5489124)")]
        public string iosGameId = "1234567";

        [Tooltip("Ödüllü Video Reklam Birimi Kimliği")]
        public string rewardedPlacementId = "rewardedVideo";

        [Tooltip("Geçiş Reklamı (Interstitial) Birimi Kimliği")]
        public string interstitialPlacementId = "interstitialVideo";

        [Tooltip("Banner Reklam Birimi Kimliği")]
        public string bannerPlacementId = "bannerAd";

        [Tooltip("Reklam izlendiğinde verilecek varsayılan altın")]
        public int rewardedGoldReward = 250;

        [Header("--- HESAP & BULUT KAYIT (UGS) AYARLARI ---")]
        [Tooltip("Unity Gaming Services Ortamı (production veya development)")]
        public string ugsEnvironment = "production";

        [Tooltip("Oyun açıldığında otomatik misafir girişi yapılsın mı?")]
        public bool autoSignInOnStart = true;

        [Header("--- KASALAR (LOOT BOX) EKONOMİ AYARLARI ---")]
        public int bronzeBoxPriceGold = 500;
        public int silverBoxPriceGold = 1500;
        public int goldBoxPriceKadimPara = 50;

        public void SetDefaults()
        {
            iapStoreMode = IAPStoreMode.FakeStore;
            fallbackToSimulation = true;
            adsMode = AdsMode.TestMode;
            androidGameId = "1234566";
            iosGameId = "1234567";
            rewardedPlacementId = "rewardedVideo";
            interstitialPlacementId = "interstitialVideo";
            bannerPlacementId = "bannerAd";
            rewardedGoldReward = 250;
            ugsEnvironment = "production";
            autoSignInOnStart = true;
            bronzeBoxPriceGold = 500;
            silverBoxPriceGold = 1500;
            goldBoxPriceKadimPara = 50;
        }
    }
}
