using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

namespace PotionShop
{
    /// <summary>
    /// Google AdMob entegrasyonu için yönetici sınıf.
    /// Rewarded (Ödüllü) reklamları yükler ve gösterir.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        private static AdsManager _instance;
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AdsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[AdsManager]");
                        _instance = go.AddComponent<AdsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        // Test Reklam ID'leri (Kendi reklamlarınızı eklemeden önce bunlarla test edin)
#if UNITY_ANDROID
        private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
        private string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
        private string rewardedAdUnitId = "unused";
#endif

        private RewardedAd rewardedAd;

        public event Action OnAdLoaded;
        public event Action OnAdFailed;

        /// <summary>
        /// Simülatör / Test modunda reklam göstermek için
        /// </summary>
        public static event Action<string, int, Action, Action> OnShowVideoAdSimulator;

        private Action onRewardCallback;
        private Action onFailedCallback;

        public bool AdsRemoved
        {
            get
            {
                if (IAPManager.Instance != null)
                {
                    return IAPManager.Instance.IsProductOwned(IAPManager.REMOVE_ADS);
                }
                return PlayerPrefs.GetInt("AdsRemoved", 0) == 1;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            // AdMob SDK'sını Başlat
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdsManager] Google AdMob SDK Başlatıldı.");
                // Başladıktan sonra reklamı önden yükle
                LoadAd();
            });
        }

        public void LoadAd()
        {
            if (AdsRemoved) return;
            
            // Eğer daha önceden yüklenmiş bir reklam varsa onu temizle
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            var config = LiveMonetizationConfig.Instance;
            bool isLive = (config != null && config.adsMode == AdsMode.LiveProduction);
            
            // Canlı moddaysak config'den gerçek Ad Unit ID'sini al, değilse Test ID'sini kullan
            string adUnitId = isLive ? config.rewardedPlacementId : rewardedAdUnitId;
            
            Debug.Log("[AdsManager] Ödüllü Reklam Yükleniyor: " + adUnitId);

            AdRequest adRequest = new AdRequest();
            
            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("[AdsManager] Reklam yüklenemedi: " + error);
                    OnAdFailed?.Invoke();
                    return;
                }

                Debug.Log("[AdsManager] Reklam başarıyla yüklendi.");
                rewardedAd = ad;
                OnAdLoaded?.Invoke();
                
                RegisterEventHandlers(rewardedAd);
            });
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdsManager] Reklam kapatıldı.");
                // Kapatılınca yeni bir reklam yükle
                LoadAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("[AdsManager] Reklam gösterilirken hata: " + error);
                onFailedCallback?.Invoke();
                LoadAd();
            };
        }

        public bool IsRewardedAdReady()
        {
            return rewardedAd != null && rewardedAd.CanShowAd();
        }

        public void ShowRewardedAd(Action onReward, Action onFailed = null)
        {
            onRewardCallback = onReward;
            onFailedCallback = onFailed;

            if (AdsRemoved)
            {
                Debug.Log("[AdsManager] Reklamlar kaldırıldığı için hemen ödül veriliyor.");
                onRewardCallback?.Invoke();
                return;
            }

            var config = LiveMonetizationConfig.Instance;
            bool isLive = (config != null && config.adsMode == AdsMode.LiveProduction);

            if (IsRewardedAdReady())
            {
                // Reklam hazırsa göster
                rewardedAd.Show((Reward reward) =>
                {
                    // Kullanıcı reklamı sonuna kadar izledi
                    Debug.Log("[AdsManager] Reklam sonuna kadar izlendi! Ödül veriliyor.");
                    onRewardCallback?.Invoke();
                });
            }
            else
            {
                // Reklam hazır değilse (İnternet yok vs.) Simülatöre düşür veya hata ver
                Debug.LogWarning("[AdsManager] Gösterilecek reklam hazır değil.");
                
                if (!isLive && OnShowVideoAdSimulator != null)
                {
                    OnShowVideoAdSimulator.Invoke("Sponsorlu Taverna Simyası", 5,
                        () =>
                        {
                            Debug.Log("<color=green>[VideoAd-Simülatör]</color> Reklam tamamlandı! Ödül veriliyor...");
                            onRewardCallback?.Invoke();
                        },
                        () =>
                        {
                            Debug.LogWarning("[VideoAd-Simülatör] Reklam erken kapatıldı veya iptal edildi.");
                            onFailedCallback?.Invoke();
                        });
                }
                else if (!isLive)
                {
                    // Test modunda fallback olarak 1 saniye bekleyip aç
                    Debug.Log("[AdsManager] Ödül simüle ediliyor (Test Modu)...");
                    StartCoroutine(SimulateAdRoutine());
                }
                else
                {
                    // Canlı modda hazır değilse hata ver (Oyuncuya "İnternetinizi kontrol edin" diyebiliriz)
                    onFailedCallback?.Invoke();
                    
                    // Tekrar yüklemeyi dene
                    LoadAd();
                }
            }
        }

        private IEnumerator SimulateAdRoutine()
        {
            yield return new WaitForSeconds(1f);
            onRewardCallback?.Invoke();
        }
    }
}
