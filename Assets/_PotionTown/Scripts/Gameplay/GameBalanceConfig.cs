using UnityEngine;

namespace PotionShop
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Potion Shop/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Ekonomi Başlangıç Değerleri")]
        public int startingGold = 5000;
        [UnityEngine.Serialization.FormerlySerializedAs("startingVipTickets")]
        public int startingKadimPara = 0;
        public int startingVipTickets { get => startingKadimPara; set => startingKadimPara = value; }
        public int startingReputation = 100;

        [Header("İtibar Sistemi")]
        [Tooltip("Fiyat çarpanı hesaplanırken kullanılan taban değer (Örn: 100 ise itibar/100 = çarpan)")]
        public int reputationBase = 100;
        [Tooltip("Doğru iksir verildiğinde kazanılan itibar")]
        public int reputationOnCorrectPotion = 5;
        [Tooltip("Müşteri bekleyip gittiğinde kaybedilen itibar")]
        public int reputationOnCustomerLeft = -20;
        [Tooltip("Müşteri reddedildiğinde kaybedilen itibar")]
        public int reputationOnCustomerRejected = -15;

        [Header("Ruh Hali Fiyat Bonusları")]
        [Tooltip("Mutlu müşteri ek ödeme")]
        public int moodBonusHappy = 5;
        [Tooltip("Memnun müşteri ek ödeme")]
        public int moodBonusNormal = 0;
        [Tooltip("Huysuz müşteri indirim")]
        public int moodBonusGrumpy = -5;
        [Tooltip("Sinirli müşteri indirim")]
        public int moodBonusAngry = -10;

        [Header("Müşteri Zamanlaması")]
        [Tooltip("Müşterilerin gelmeye başladığı saat")]
        public int customerStartHour = 6;
        [Tooltip("Müşterilerin gelmeyi bıraktığı saat (tüm müşteriler bu saate kadar sığdırılır)")]
        public int customerEndHour = 22;

        [Header("Müşteri Kuyruk Sistemi")]
        [Tooltip("Aynı anda kuyrukta en fazla kaç müşteri olabilir")]
        public int maxQueueSize = 3;
        [Tooltip("Kuyrukta müşteriler arası piksel mesafe")]
        public float queueSpacing = 150f;

        [Header("Dükkan Çalışma Saatleri")]
        [Tooltip("Malzeme dükkanının açıldığı saat")]
        public int shopOpenHour = 6;
        [Tooltip("Malzeme dükkanının kapandığı saat")]
        public int shopCloseHour = 22;

        [Header("Zorluk İlerlemesi")]
        [Tooltip("İlk gün kaç çeşit iksir istenebilir (bu sayı + gün sayısı)")]
        public int basePotionVariety = 1;
        [Tooltip("Her gün kaç yeni iksir çeşidi eklenir")]
        public int potionVarietyPerDay = 1;
        [Tooltip("Her gün müşteri bekleme süresinin yüzde kaç kısaldığı (0.05 = %5)")]
        public float spawnSpeedReductionPerDay = 0.05f;
        [Tooltip("Bekleme süresinin kısalabileceği minimum faktör (0.2 = en fazla %80 hızlanma)")]
        public float minSpawnSpeedFactor = 0.2f;

        [Header("Müşteri Görsel Ayarları")]
        [Tooltip("Müşterinin ekran dışından giriş/çıkış X pozisyonu (piksel)")]
        public float customerOffScreenX = 1000f;

        [Header("Mini Oyun Ayarları")]
        [Tooltip("Günlük ücretsiz mini oyun oynama hakkı")]
        public int dailyFreeGamePlays = 3;
        [Tooltip("Mini oyunlar arası bekleme süresi (dakika)")]
        public float miniGameCooldownMinutes = 5f;
        [Tooltip("Mini oyun süresi (saniye)")]
        public float miniGameDuration = 60f;

        [Header("Kazan Animasyonları")]
        [Tooltip("Özsu dolum animasyonu süresi (saniye)")]
        public float cauldronFillDuration = 0.6f;
        [Tooltip("İksir fırlatma animasyonu süresi (saniye)")]
        public float potionEjectDuration = 0.5f;
        [Tooltip("İksirin fırlatıldığında zıplama yüksekliği (piksel)")]
        public float potionJumpHeight = 150f;
        [Tooltip("İksirin düştükten sonra kaybolmadan önce bekleme süresi (saniye)")]
        public float potionLandWaitTime = 0.4f;
        [Tooltip("Hedef yoksa rastgele fırlatma minimum X mesafesi (piksel)")]
        public float potionEjectMinX = 100f;
        [Tooltip("Hedef yoksa rastgele fırlatma maksimum X mesafesi (piksel)")]
        public float potionEjectMaxX = 200f;
        [Tooltip("Hedef yoksa rastgele fırlatma Y düşüş mesafesi (piksel)")]
        public float potionEjectFallY = -150f;

        [Header("Raf Görselleri")]
        [Tooltip("Envanterde bitmiş eşyanın saydamlık değeri (0-1)")]
        public float emptyItemAlpha = 0.4f;

        [Header("Lisans Sistemi")]
        [Tooltip("Lisans alınmamış malzemelerin görsel saydamlık değeri (0-1)")]
        public float lockedItemAlpha = 0.5f;

        [Tooltip("Tüm sayfa lisansı alırken indirim oranı (0.10 = %10)")]
        public float bulkLicenseDiscount = 0.10f;

        [Tooltip("Varsayılan tek iksir lisans fiyatı çarpanı (iksir fiyatı * bu değer)")]
        public float defaultLicensePriceMultiplier = 4f;

        // Singleton erişim (Resources klasöründen yükler)
        private static GameBalanceConfig _instance;
        public static GameBalanceConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
                return _instance;
            }
        }
    }
}
