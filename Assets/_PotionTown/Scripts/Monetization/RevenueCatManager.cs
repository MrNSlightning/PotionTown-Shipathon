using System;
using System.Collections.Generic;
using UnityEngine;

// RevenueCat SDK kullanılabilir olduğunda bu sembol tanımlanır.
// Project Settings > Player > Scripting Define Symbols: REVENUECAT_PURCHASES
#if REVENUECAT_PURCHASES
using RevenueCat;
#endif

namespace PotionShop
{
    /// <summary>
    /// RevenueCat SDK entegrasyonu için yönetici sınıf.
    /// Tüm in-app purchase akışlarını RevenueCat üzerinden yönetir.
    /// Shipaton 2026 — Next Gen Award başvurusu için zorunlu entegrasyon.
    /// </summary>
#if REVENUECAT_PURCHASES
    public class RevenueCatManager : Purchases.UpdatedCustomerInfoListener
#else
    public class RevenueCatManager : MonoBehaviour
#endif
    {
        private static RevenueCatManager _instance;
        public static RevenueCatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RevenueCatManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[RevenueCatManager]");
                        _instance = go.AddComponent<RevenueCatManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        // ─── Entitlement Sabitleri ───
        // RevenueCat Dashboard'da tanımlanacak entitlement ID'leri
        public const string ENTITLEMENT_PREMIUM_PASS = "potion_pass_premium";
        public const string ENTITLEMENT_REMOVE_ADS = "remove_ads";

        // ─── Ürün Sabitleri (IAPManager ile uyumlu) ───
        public const string PRODUCT_KADIM_PARA_SMALL = "com.potiontavern.kadimpara.small";
        public const string PRODUCT_KADIM_PARA_MEDIUM = "com.potiontavern.kadimpara.medium";
        public const string PRODUCT_KADIM_PARA_LARGE = "com.potiontavern.kadimpara.large";
        public const string PRODUCT_POTION_PASS_PREMIUM = "com.potiontavern.potionpass.premium";
        public const string PRODUCT_REMOVE_ADS = "com.potiontavern.removeads";

        [Header("--- RevenueCat Yapılandırması ---")]
        [Tooltip("RevenueCat Dashboard > Project Settings > API Keys > Public SDK Key")]
        [SerializeField] private string apiKeyAndroid = "YOUR_REVENUECAT_ANDROID_API_KEY";
        
        [Tooltip("RevenueCat Dashboard > Project Settings > API Keys > Public SDK Key (iOS)")]
        [SerializeField] private string apiKeyiOS = "YOUR_REVENUECAT_IOS_API_KEY";

        [Tooltip("Opsiyonel: Bilinen kullanıcı kimliği (Firebase UID vs.). Boş bırakılırsa anonim kullanıcı oluşturulur.")]
        [SerializeField] private string appUserId = "";

        [Header("--- Debug ---")]
        [Tooltip("SDK loglarını göster (Geliştirme aşamasında aktif tutun)")]
        [SerializeField] private bool enableDebugLogs = true;

        // ─── Durum Değişkenleri ───
        private bool _isConfigured = false;

#if REVENUECAT_PURCHASES
        private Purchases.Offerings _cachedOfferings;
        private Purchases.CustomerInfo _cachedCustomerInfo;
        private Purchases _purchases;
#endif

        // ─── Events ───
        /// <summary>Satın alma başarılı olduğunda tetiklenir (productId).</summary>
        public event Action<string> OnPurchaseSuccess;
        /// <summary>Satın alma başarısız olduğunda tetiklenir (productId, errorMessage).</summary>
        public event Action<string, string> OnPurchaseFailed;
        /// <summary>Müşteri bilgisi güncellendiğinde tetiklenir.</summary>
        public event Action OnCustomerInfoUpdated;
        /// <summary>Offerings (ürün listesi) yüklendiğinde tetiklenir.</summary>
        public event Action OnOfferingsLoaded;

        // ─── Genel Durum Özellikleri ───
        public bool IsConfigured => _isConfigured;

        /// <summary>
        /// Potion Pass Premium satın alınmış mı?
        /// RevenueCat entitlement tabanlı kontrol.
        /// </summary>
        public bool IsPotionPassPremiumOwned
        {
            get
            {
#if REVENUECAT_PURCHASES
                if (_cachedCustomerInfo != null)
                {
                    return _cachedCustomerInfo.Entitlements.Active.ContainsKey(ENTITLEMENT_PREMIUM_PASS);
                }
#endif
                // Fallback: PlayerPrefs kontrolü
                return PlayerPrefs.GetInt("PotionPassPremium", 0) == 1;
            }
        }

        /// <summary>
        /// Reklamlar kaldırılmış mı?
        /// RevenueCat entitlement tabanlı kontrol.
        /// </summary>
        public bool IsAdsRemoved
        {
            get
            {
#if REVENUECAT_PURCHASES
                if (_cachedCustomerInfo != null)
                {
                    return _cachedCustomerInfo.Entitlements.Active.ContainsKey(ENTITLEMENT_REMOVE_ADS);
                }
#endif
                // Fallback: PlayerPrefs kontrolü
                return PlayerPrefs.GetInt("AdsRemoved", 0) == 1;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  YAŞAM DÖNGÜSÜ
        // ═══════════════════════════════════════════════════════

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                ConfigureSDK();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// RevenueCat SDK'yı yapılandırır ve başlatır.
        /// Uygulama yaşam döngüsünde yalnızca bir kez çağrılmalıdır.
        /// </summary>
        private void ConfigureSDK()
        {
#if REVENUECAT_PURCHASES
            if (_isConfigured)
            {
                Debug.LogWarning("[RevenueCat] SDK zaten yapılandırılmış.");
                return;
            }

#if UNITY_EDITOR
            // Unity Editor ortamında gerçek App Store / Google Play bulunmaz (PurchasesWrapperNoop devrededir).
            // Editor ortamında çökmeyi önlemek için simülasyon moduna geçiyoruz.
            Debug.Log("<color=cyan>[RevenueCat]</color> Unity Editor ortamı: Simülasyon modu devrede.");
            _isConfigured = true;
            OnOfferingsLoaded?.Invoke();
            return;
#else
            try
            {
                // Platform bazlı API key seçimi
                string apiKey;
#if UNITY_ANDROID
                apiKey = apiKeyAndroid;
#elif UNITY_IOS
                apiKey = apiKeyiOS;
#else
                apiKey = apiKeyAndroid;
#endif

                if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
                {
                    Debug.LogWarning("[RevenueCat] API Key ayarlanmamış! RevenueCat Dashboard'dan alınan Public SDK Key'i Inspector'dan girin.");
                    _isConfigured = true;
                    return;
                }

                // SDK Yapılandırması (Purchases GameObject oluşturarak)
                var purchasesGO = new GameObject("Purchases");
                purchasesGO.transform.SetParent(transform);
                _purchases = purchasesGO.AddComponent<Purchases>();
                
                _purchases.revenueCatAPIKeyApple = apiKeyiOS;
                _purchases.revenueCatAPIKeyGoogle = apiKeyAndroid;
                _purchases.appUserID = appUserId;
                _purchases.listener = this;

                // Purchases.Start() bir sonraki frame çalışacağı için ertelenmiş yapılandırma başlat
                StartCoroutine(DeferredConfigureRoutine());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RevenueCat] SDK yapılandırma hatası: {ex.Message}");
                _isConfigured = true;
            }
#endif
#else
            Debug.Log("[RevenueCat] REVENUECAT_PURCHASES tanımlı değil. Simülasyon modunda çalışıyor.");
            _isConfigured = true;
            OnOfferingsLoaded?.Invoke();
#endif
        }

        private System.Collections.IEnumerator DeferredConfigureRoutine()
        {
#if REVENUECAT_PURCHASES && !UNITY_EDITOR
            // Purchases.Start() çağrılıp _wrapper ilklendirilene kadar bekle
            yield return null;

            try
            {
                if (_purchases != null)
                {
                    if (enableDebugLogs)
                    {
                        _purchases.SetLogLevel(Purchases.LogLevel.Debug);
                    }

                    _isConfigured = true;
                    Debug.Log("<color=green>[RevenueCat]</color> Mobil SDK başarıyla yapılandırıldı.");

                    FetchOfferings();
                    FetchCustomerInfo();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RevenueCat] Ertelenmiş yapılandırma uyarısı: {ex.Message}");
                _isConfigured = true;
            }
#else
            yield break;
#endif
        }

        // ═══════════════════════════════════════════════════════
        //  OFFERINGS (ÜRÜN PAKETLERİ)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// RevenueCat Dashboard'da tanımlanmış Offerings (ürün paketleri) verisini çeker.
        /// </summary>
        public void FetchOfferings()
        {
#if REVENUECAT_PURCHASES && !UNITY_EDITOR
            if (_purchases == null) return;
            
            try
            {
                _purchases.GetOfferings((offerings, error) =>
                {
                    if (error != null)
                    {
                        Debug.LogError($"[RevenueCat] Offerings yüklenemedi: {error.Message}");
                        return;
                    }

                    _cachedOfferings = offerings;
                    Debug.Log($"<color=green>[RevenueCat]</color> Offerings yüklendi. Current: {offerings.Current?.Identifier ?? "null"}");

                    if (offerings.Current != null)
                    {
                        foreach (var pkg in offerings.Current.AvailablePackages)
                        {
                            Debug.Log($"  → Paket: {pkg.Identifier} | Ürün: {pkg.StoreProduct.Identifier} | Fiyat: {pkg.StoreProduct.PriceString}");
                        }
                    }

                    OnOfferingsLoaded?.Invoke();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RevenueCat] FetchOfferings uyarısı: {ex.Message}");
            }
#else
            Debug.Log("[RevenueCat] Simülasyon: Offerings yüklendi.");
            OnOfferingsLoaded?.Invoke();
#endif
        }

        // ═══════════════════════════════════════════════════════
        //  SATIN ALMA İŞLEMLERİ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Belirtilen product ID ile satın alma işlemi başlatır.
        /// IAPManager.BuyProduct() yerine kullanılmalıdır.
        /// </summary>
        public void PurchaseProduct(string productId)
        {
#if REVENUECAT_PURCHASES && !UNITY_EDITOR
            if (!_isConfigured || _purchases == null)
            {
                Debug.LogError("[RevenueCat] SDK yapılandırılmamış. Satın alma yapılamaz.");
                OnPurchaseFailed?.Invoke(productId, "SDK yapılandırılmamış");
                return;
            }

            if (_cachedOfferings == null || _cachedOfferings.Current == null)
            {
                Debug.LogWarning("[RevenueCat] Offerings henüz yüklenmedi. Tekrar çekiliyor...");
                FetchOfferings();
                OnPurchaseFailed?.Invoke(productId, "Ürünler henüz yüklenmedi, lütfen tekrar deneyin");
                return;
            }

            // Offerings içinden bu ürün ID'sine sahip paketi bul
            Purchases.Package targetPackage = null;
            foreach (var pkg in _cachedOfferings.Current.AvailablePackages)
            {
                if (pkg.StoreProduct.Identifier == productId)
                {
                    targetPackage = pkg;
                    break;
                }
            }

            // Eğer Current offering'de bulunamazsa, tüm offering'leri tara
            if (targetPackage == null)
            {
                foreach (var kvp in _cachedOfferings.All)
                {
                    foreach (var pkg in kvp.Value.AvailablePackages)
                    {
                        if (pkg.StoreProduct.Identifier == productId)
                        {
                            targetPackage = pkg;
                            break;
                        }
                    }
                    if (targetPackage != null) break;
                }
            }

            if (targetPackage == null)
            {
                Debug.LogError($"[RevenueCat] Ürün bulunamadı: {productId}. Dashboard'da Offerings ayarlarını kontrol edin.");
                OnPurchaseFailed?.Invoke(productId, "Ürün bulunamadı");
                return;
            }

            Debug.Log($"<color=yellow>[RevenueCat]</color> Satın alma başlatılıyor: {productId}");

            try
            {
                _purchases.PurchasePackage(targetPackage, (result) =>
                {
                    if (result.UserCancelled)
                    {
                        Debug.Log($"[RevenueCat] Kullanıcı satın almayı iptal etti: {productId}");
                        OnPurchaseFailed?.Invoke(productId, "Kullanıcı iptal etti");
                        return;
                    }

                    if (result.Error != null)
                    {
                        Debug.LogError($"[RevenueCat] Satın alma hatası: {result.Error.Message}");
                        OnPurchaseFailed?.Invoke(productId, result.Error.Message);
                        return;
                    }

                    // Başarılı satın alma!
                    Debug.Log($"<color=green>[RevenueCat]</color> Satın alma başarılı: {result.ProductIdentifier}");
                    _cachedCustomerInfo = result.CustomerInfo;

                    // Ödülleri ver
                    HandlePurchaseRewards(productId);
                    OnPurchaseSuccess?.Invoke(productId);
                    OnCustomerInfoUpdated?.Invoke();
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RevenueCat] PurchasePackage hatası: {ex.Message}");
                OnPurchaseFailed?.Invoke(productId, ex.Message);
            }
#else
            // Simülasyon modu (Unity Editor veya REVENUECAT_PURCHASES pasif)
            Debug.Log($"<color=green>[RevenueCat Simülasyon]</color> Satın alma simüle ediliyor: {productId}");
            HandlePurchaseRewards(productId);
            OnPurchaseSuccess?.Invoke(productId);
#endif
        }

        // ═══════════════════════════════════════════════════════
        //  MÜŞTERİ BİLGİSİ & ENTITLEMENT KONTROLÜ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Güncel müşteri bilgisini RevenueCat'ten çeker.
        /// </summary>
        public void FetchCustomerInfo()
        {
#if REVENUECAT_PURCHASES && !UNITY_EDITOR
            if (_purchases == null) return;
            
            try
            {
                _purchases.GetCustomerInfo((customerInfo, error) =>
                {
                    if (error != null)
                    {
                        Debug.LogError($"[RevenueCat] CustomerInfo alınamadı: {error.Message}");
                        return;
                    }

                    _cachedCustomerInfo = customerInfo;
                    Debug.Log("<color=green>[RevenueCat]</color> CustomerInfo güncellendi.");
                    SyncEntitlementsToLocal();
                    OnCustomerInfoUpdated?.Invoke();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RevenueCat] FetchCustomerInfo uyarısı: {ex.Message}");
            }
#else
            Debug.Log("[RevenueCat] Simülasyon: CustomerInfo güncellendi.");
            SyncEntitlementsToLocal();
            OnCustomerInfoUpdated?.Invoke();
#endif
        }

        /// <summary>
        /// RevenueCat entitlement durumunu yerel sistemlerle senkronize eder.
        /// Uygulama açılışında ve her satın alma sonrası çağrılır.
        /// </summary>
        private void SyncEntitlementsToLocal()
        {
#if REVENUECAT_PURCHASES
            if (_cachedCustomerInfo == null) return;

            // Potion Pass Premium senkronizasyonu
            if (_cachedCustomerInfo.Entitlements.Active.ContainsKey(ENTITLEMENT_PREMIUM_PASS))
            {
                PlayerPrefs.SetInt("PotionPassPremium", 1);
                if (PotionPassManager.Instance != null)
                {
                    PotionPassManager.Instance.UpgradeToPremium();
                }
                Debug.Log("[RevenueCat] Entitlement senkronize edildi: Potion Pass Premium ✓");
            }

            // Reklam kaldırma senkronizasyonu
            if (_cachedCustomerInfo.Entitlements.Active.ContainsKey(ENTITLEMENT_REMOVE_ADS))
            {
                PlayerPrefs.SetInt("AdsRemoved", 1);
                Debug.Log("[RevenueCat] Entitlement senkronize edildi: Remove Ads ✓");
            }

            PlayerPrefs.Save();
#endif
        }

        /// <summary>
        /// Satın alımları geri yükler (iOS için gerekli).
        /// </summary>
        public void RestorePurchases()
        {
#if REVENUECAT_PURCHASES && !UNITY_EDITOR
            if (_purchases == null) return;
            
            try
            {
                Debug.Log("[RevenueCat] Satın alımlar geri yükleniyor...");
                _purchases.RestorePurchases((customerInfo, error) =>
                {
                    if (error != null)
                    {
                        Debug.LogError($"[RevenueCat] Geri yükleme hatası: {error.Message}");
                        return;
                    }

                    _cachedCustomerInfo = customerInfo;
                    SyncEntitlementsToLocal();
                    Debug.Log("<color=green>[RevenueCat]</color> Satın alımlar başarıyla geri yüklendi.");
                    OnCustomerInfoUpdated?.Invoke();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RevenueCat] RestorePurchases uyarısı: {ex.Message}");
            }
#else
            Debug.Log("[RevenueCat] Simülasyon: Geri yükleme tamamlandı.");
#endif
        }

        // ═══════════════════════════════════════════════════════
        //  ÖDÜL YÖNETİMİ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Satın alma sonrası oyun içi ödülleri verir.
        /// IAPManager.HandlePurchaseRewards() ile aynı mantığı kullanır.
        /// </summary>
        private void HandlePurchaseRewards(string productId)
        {
            var gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            var catalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            var prod = catalog != null ? catalog.FindProduct(productId) : null;

            if (prod != null)
            {
                switch (prod.rewardType)
                {
                    case ShopRewardType.KadimPara:
                        if (gameManager != null) gameManager.AddKadimPara(prod.rewardAmount);
                        break;
                    case ShopRewardType.Gold:
                        if (gameManager != null) gameManager.AddGold(prod.rewardAmount);
                        break;
                    case ShopRewardType.PotionPassPremium:
                        PlayerPrefs.SetInt("PotionPassPremium", 1);
                        PlayerPrefs.Save();
                        if (PotionPassManager.Instance != null) PotionPassManager.Instance.UpgradeToPremium();
                        break;
                    case ShopRewardType.RemoveAds:
                        PlayerPrefs.SetInt("AdsRemoved", 1);
                        PlayerPrefs.Save();
                        Debug.Log("[RevenueCat] Reklamlar kaldırıldı olarak kaydedildi.");
                        break;
                    case ShopRewardType.MiniGamePlay:
                        if (MiniGameManager.Instance != null) MiniGameManager.Instance.GrantBonusPlay();
                        break;
                    case ShopRewardType.LootBox:
                        if (Enum.TryParse<LootBoxTier>(prod.lootBoxTier, out var tier))
                            LootBoxSystem.AddLootBox(tier);
                        break;
                }
                return;
            }

            // Yedek sabit ürün eşleştirmesi (katalogda bulunamazsa)
            switch (productId)
            {
                case PRODUCT_KADIM_PARA_SMALL:
                    if (gameManager != null) gameManager.AddKadimPara(10);
                    break;
                case PRODUCT_KADIM_PARA_MEDIUM:
                    if (gameManager != null) gameManager.AddKadimPara(50);
                    break;
                case PRODUCT_KADIM_PARA_LARGE:
                    if (gameManager != null) gameManager.AddKadimPara(150);
                    break;
                case PRODUCT_POTION_PASS_PREMIUM:
                    PlayerPrefs.SetInt("PotionPassPremium", 1);
                    PlayerPrefs.Save();
                    if (PotionPassManager.Instance != null) PotionPassManager.Instance.UpgradeToPremium();
                    break;
                case PRODUCT_REMOVE_ADS:
                    PlayerPrefs.SetInt("AdsRemoved", 1);
                    PlayerPrefs.Save();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  KULLANICI KİMLİĞİ YÖNETİMİ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Bilinen bir kullanıcı kimliğiyle oturum açar (örn. Firebase UID).
        /// Farklı cihazlardaki satın almaları birleştirmek için kullanılır.
        /// </summary>
        public void LoginUser(string userId)
        {
#if REVENUECAT_PURCHASES
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[RevenueCat] Boş userId ile login yapılamaz.");
                return;
            }
            if (_purchases == null) return;

            _purchases.LogIn(userId, (customerInfo, created, error) =>
            {
                if (error != null)
                {
                    Debug.LogError($"[RevenueCat] Login hatası: {error.Message}");
                    return;
                }

                _cachedCustomerInfo = customerInfo;
                Debug.Log($"<color=green>[RevenueCat]</color> Kullanıcı girişi başarılı. Yeni mi: {created}");
                SyncEntitlementsToLocal();
                OnCustomerInfoUpdated?.Invoke();
            });
#endif
        }

        /// <summary>
        /// Oturumu kapatır ve anonim kullanıcıya döner.
        /// </summary>
        public void LogoutUser()
        {
#if REVENUECAT_PURCHASES
            if (_purchases == null) return;
            
            _purchases.LogOut((customerInfo, error) =>
            {
                if (error != null)
                {
                    Debug.LogError($"[RevenueCat] Logout hatası: {error.Message}");
                    return;
                }

                _cachedCustomerInfo = customerInfo;
                Debug.Log("[RevenueCat] Kullanıcı çıkışı başarılı. Anonim moda dönüldü.");
            });
#endif
        }

        // ═══════════════════════════════════════════════════════
        //  REVENUECAT CALLBACK
        // ═══════════════════════════════════════════════════════

#if REVENUECAT_PURCHASES
        /// <summary>
        /// RevenueCat SDK tarafından müşteri bilgisi güncellendiğinde çağrılır.
        /// Purchases.UpdatedCustomerInfoListener arayüzünün implementasyonu.
        /// </summary>
        public override void CustomerInfoReceived(Purchases.CustomerInfo customerInfo)
        {
            _cachedCustomerInfo = customerInfo;
            Debug.Log("[RevenueCat] Müşteri bilgisi otomatik güncellendi.");
            SyncEntitlementsToLocal();
            OnCustomerInfoUpdated?.Invoke();
        }
#endif

        // ═══════════════════════════════════════════════════════
        //  YARDIMCI METOTLAR
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Belirli bir entitlement'ın aktif olup olmadığını kontrol eder.
        /// </summary>
        public bool HasActiveEntitlement(string entitlementId)
        {
#if REVENUECAT_PURCHASES
            if (_cachedCustomerInfo != null)
            {
                return _cachedCustomerInfo.Entitlements.Active.ContainsKey(entitlementId);
            }
#endif
            return false;
        }

        /// <summary>
        /// Mevcut offering'deki paketlerin fiyat bilgilerini döner.
        /// UI'da dinamik fiyat göstermek için kullanılır.
        /// </summary>
        public string GetProductPrice(string productId)
        {
#if REVENUECAT_PURCHASES
            if (_cachedOfferings?.Current == null) return null;

            foreach (var pkg in _cachedOfferings.Current.AvailablePackages)
            {
                if (pkg.StoreProduct.Identifier == productId)
                {
                    return pkg.StoreProduct.PriceString;
                }
            }
#endif
            return null;
        }
    }
}
