using System;
using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Özel iksirlerin aktif etkilerini (buff'larını) ve sürelerini yöneten merkezi sistemdir.
    /// 5 gerçek dakikalık (300 saniye) etkileri takip eder, süresi bitenleri sıfırlar.
    /// </summary>
    public class SpecialPotionManager : MonoBehaviour
    {
        private static SpecialPotionManager _instance;
        public static SpecialPotionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SpecialPotionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[SpecialPotionManager]");
                        _instance = go.AddComponent<SpecialPotionManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Aktif Süreler (Kalan Saniyeler)")]
        [SerializeField] private float slowMoodDecayTimer = 0f;
        [SerializeField] private float doubleGoldTimer = 0f;
        [SerializeField] private float rushHourTimer = 0f;
        [SerializeField] private float chainBreakerTimer = 0f;

        [Header("Etki Çarpanları")]
        [Tooltip("Sükunet Merhemi aktifken mod düşüş hız çarpanı (Örn: 0.4 = %60 daha yavaş)")]
        public float moodDecayRateMultiplier = 0.4f;

        [Tooltip("Coşku Toniği aktifken satışlardan kazanılan altın çarpanı")]
        public float goldMultiplier = 2.0f;

        [Tooltip("Rüzgarın Hızı İksiri aktifken müşteri yürüme hızı çarpanı")]
        public float rushWalkSpeedMultiplier = 2.0f;

        [Tooltip("Rüzgarın Hızı İksiri aktifken müşteri spawn bekleme süresi çarpanı (0.5 = iki kat daha sık)")]
        public float rushSpawnDelayMultiplier = 0.5f;

        // Durum Sorguları
        public bool IsSlowMoodDecayActive => slowMoodDecayTimer > 0f;
        public bool IsDoubleGoldActive => doubleGoldTimer > 0f;
        public bool IsRushHourActive => rushHourTimer > 0f;
        public bool IsChainBreakerActive => chainBreakerTimer > 0f;

        // Kalan Süreler
        public float RemainingSlowMoodDecay => slowMoodDecayTimer;
        public float RemainingDoubleGold => doubleGoldTimer;
        public float RemainingRushHour => rushHourTimer;
        public float RemainingChainBreaker => chainBreakerTimer;

        // Olaylar (UI güncellemeleri veya ses efektleri için)
        public event Action<SpecialItemEffect, float> OnEffectActivated;
        public event Action<SpecialItemEffect> OnEffectExpired;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            // Zamanlayıcıları güncelle
            float dt = Time.deltaTime;

            if (slowMoodDecayTimer > 0f)
            {
                slowMoodDecayTimer -= dt;
                if (slowMoodDecayTimer <= 0f)
                {
                    slowMoodDecayTimer = 0f;
                    Debug.Log("<color=cyan>[Özel İksir]</color> Sükunet Merhemi etkisi sona erdi.");
                    OnEffectExpired?.Invoke(SpecialItemEffect.SlowMoodDecay);
                }
            }

            if (doubleGoldTimer > 0f)
            {
                doubleGoldTimer -= dt;
                if (doubleGoldTimer <= 0f)
                {
                    doubleGoldTimer = 0f;
                    Debug.Log("<color=yellow>[Özel İksir]</color> Coşku Toniği (2x Altın) etkisi sona erdi.");
                    OnEffectExpired?.Invoke(SpecialItemEffect.DoubleGold);
                }
            }

            if (rushHourTimer > 0f)
            {
                rushHourTimer -= dt;
                if (rushHourTimer <= 0f)
                {
                    rushHourTimer = 0f;
                    Debug.Log("<color=teal>[Özel İksir]</color> Rüzgarın Hızı (Müşteri Akını) etkisi sona erdi.");
                    OnEffectExpired?.Invoke(SpecialItemEffect.RushHour);
                }
            }

            if (chainBreakerTimer > 0f)
            {
                chainBreakerTimer -= dt;
                if (chainBreakerTimer <= 0f)
                {
                    chainBreakerTimer = 0f;
                    Debug.Log("<color=purple>[Özel İksir]</color> Zincir Kıran etkisi sona erdi.");
                    OnEffectExpired?.Invoke(SpecialItemEffect.ChainBreaker);
                }
            }
        }

        /// <summary>
        /// Özel iksir etkisini belirtilen süre boyunca aktif eder.
        /// </summary>
        public void ActivateEffect(SpecialItemEffect effect, float duration = 300f, float multiplier = 1f)
        {
            if (duration <= 0f) duration = 300f; // Varsayılan 5 dakika

            switch (effect)
            {
                case SpecialItemEffect.SlowMoodDecay:
                    slowMoodDecayTimer = Mathf.Max(slowMoodDecayTimer, duration);
                    if (multiplier > 0f) moodDecayRateMultiplier = multiplier;
                    Debug.Log($"<color=cyan>[Özel İksir]</color> Sükunet Merhemi aktif edildi! ({duration} saniye - Mod düşüşü %{(1f - moodDecayRateMultiplier) * 100f:0} yavaşlatıldı)");
                    break;

                case SpecialItemEffect.DoubleGold:
                    doubleGoldTimer = Mathf.Max(doubleGoldTimer, duration);
                    if (multiplier > 1f) goldMultiplier = multiplier;
                    Debug.Log($"<color=yellow>[Özel İksir]</color> Coşku Toniği aktif edildi! ({duration} saniye - Tüm satışlar {goldMultiplier}x Altın!)");
                    break;

                case SpecialItemEffect.RushHour:
                    rushHourTimer = Mathf.Max(rushHourTimer, duration);
                    if (multiplier > 1f) rushWalkSpeedMultiplier = multiplier;
                    Debug.Log($"<color=teal>[Özel İksir]</color> Rüzgarın Hızı İksiri aktif edildi! ({duration} saniye - Müşteri Akını devrede!)");
                    break;

                case SpecialItemEffect.ChainBreaker:
                    chainBreakerTimer = Mathf.Max(chainBreakerTimer, duration);
                    Debug.Log($"<color=purple>[Özel İksir]</color> Zincir Kıran İksiri aktif edildi! ({duration} saniye - İtibar koruması, temiz kazan, +1 Kadim Para!)");

                    // Kazanı anında temizle
                    var cauldron = FindFirstObjectByType<Cauldron>();
                    if (cauldron != null && cauldron.isDirty)
                    {
                        cauldron.CleanCauldron();
                        Debug.Log("<color=purple>[Zincir Kıran]</color> Kazan anında arındırıldı ve temizlendi!");
                    }
                    break;

                default:
                    return;
            }

            OnEffectActivated?.Invoke(effect, duration);
        }

        /// <summary>
        /// Kalan süreyi "MM:SS" formatında metin olarak döndürür.
        /// </summary>
        public static string FormatTimer(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
