using System;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace PotionShop
{
    /// <summary>
    /// Unity IAP entegrasyonu için yönetici sınıf.
    /// UNITY_PURCHASING sembolü tanımlandığında gerçek işlemleri yapar.
    /// </summary>
    public class IAPManager : MonoBehaviour
#if UNITY_PURCHASING
        , IDetailedStoreListener
#endif
    {
        private static IAPManager _instance;
        public static IAPManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<IAPManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[IAPManager]");
                        _instance = go.AddComponent<IAPManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const string KADIM_PARA_SMALL = "com.potiontavern.kadimpara.small";
        public const string KADIM_PARA_MEDIUM = "com.potiontavern.kadimpara.medium";
        public const string KADIM_PARA_LARGE = "com.potiontavern.kadimpara.large";
        public const string POTION_PASS_PREMIUM = "com.potiontavern.potionpass.premium";
        public const string REMOVE_ADS = "com.potiontavern.removeads";

        public event Action<string> OnPurchaseSuccess;
        public event Action<string, string> OnPurchaseFailed;

        /// <summary>
        /// Gerçekçi Google Play satın alma penceresi açılması istendiğinde tetiklenir:
        /// (productId, title, price, onConfirmAction, onCancelAction)
        /// </summary>
        public static event Action<string, string, string, Action, Action> OnPromptGooglePlayDialog;

#if UNITY_PURCHASING
        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
#endif

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePurchasing();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePurchasing()
        {
#if UNITY_PURCHASING
            if (IsInitialized()) return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(KADIM_PARA_SMALL, ProductType.Consumable);
            builder.AddProduct(KADIM_PARA_MEDIUM, ProductType.Consumable);
            builder.AddProduct(KADIM_PARA_LARGE, ProductType.Consumable);
            builder.AddProduct(POTION_PASS_PREMIUM, ProductType.NonConsumable);
            builder.AddProduct(REMOVE_ADS, ProductType.NonConsumable);

            // Katalogdaki ek IAP ürünlerini de kaydet
            var catalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            if (catalog != null)
            {
                foreach (var p in catalog.products)
                {
                    if (p.priceType == ShopPriceType.RealMoney && !string.IsNullOrEmpty(p.id))
                    {
                        var pType = (p.rewardType == ShopRewardType.RemoveAds || p.rewardType == ShopRewardType.PotionPassPremium)
                            ? ProductType.NonConsumable : ProductType.Consumable;
                        builder.AddProduct(p.id, pType);
                    }
                }
            }

            UnityPurchasing.Initialize(this, builder);
#else
            Debug.Log("[IAPManager] Test ve simülasyon modunda hazır.");
#endif
        }

        public bool IsInitialized()
        {
#if UNITY_PURCHASING
            return storeController != null && storeExtensionProvider != null;
#else
            return true;
#endif
        }

        public void BuyProduct(string productId)
        {
            // RevenueCat SDK entegre ise, tüm satın alma akışını oraya yönlendir
            if (RevenueCatManager.Instance != null && RevenueCatManager.Instance.IsConfigured)
            {
                Debug.Log($"[IAP] Satın alma RevenueCat'e yönlendiriliyor: {productId}");
                RevenueCatManager.Instance.PurchaseProduct(productId);
                return;
            }

            var config = LiveMonetizationConfig.Instance;
            bool isLive = (config != null && config.iapStoreMode == IAPStoreMode.LiveStore);

#if UNITY_PURCHASING
            if (isLive && IsInitialized())
            {
                Product product = storeController.products.WithID(productId);
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log($"[IAP] Canlı satın alma başlatılıyor: {product.definition.id}");
                    storeController.InitiatePurchase(product);
                    return;
                }
                else
                {
                    Debug.LogWarning("Satın alınmaya çalışılan ürün bulunamadı veya satın alınabilir değil.");
                    OnPurchaseFailed?.Invoke(productId, "Ürün bulunamadı");
                    return;
                }
            }
#endif

            // Gerçekçi Simülasyon veya FakeStore Modu
            var catalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            var prodData = catalog != null ? catalog.FindProduct(productId) : null;
            string title = prodData != null ? $"{prodData.displayName} - {prodData.subtitle}" : productId;
            string price = prodData != null ? prodData.GetFormattedPrice() : "19.99 ₺";

            if (OnPromptGooglePlayDialog != null)
            {
                OnPromptGooglePlayDialog.Invoke(productId, title, price,
                    () =>
                    {
                        Debug.Log($"<color=green>[GooglePlay-Simülatör]</color> Satın alma onaylandı: {productId}");
                        SimulatePurchaseSuccess(productId);
                    },
                    () =>
                    {
                        Debug.LogWarning($"[GooglePlay-Simülatör] Satın alma iptal edildi: {productId}");
                        OnPurchaseFailed?.Invoke(productId, "Kullanıcı İptal Etti");
                    });
            }
            else
            {
                Debug.Log($"[IAP] Doğrudan simüle ediliyor: {productId}");
                SimulatePurchaseSuccess(productId);
            }
        }

        public bool IsProductOwned(string id)
        {
            // Önce RevenueCat entitlement kontrolü
            if (RevenueCatManager.Instance != null && RevenueCatManager.Instance.IsConfigured)
            {
                if (id == REMOVE_ADS)
                    return RevenueCatManager.Instance.IsAdsRemoved;
                if (id == POTION_PASS_PREMIUM)
                    return RevenueCatManager.Instance.IsPotionPassPremiumOwned;
            }

#if UNITY_PURCHASING
            if (IsInitialized())
            {
                Product product = storeController.products.WithID(id);
                if (product != null && product.hasReceipt)
                {
                    return true;
                }
            }
            return false;
#else
            return false;
#endif
        }

        public void RestorePurchases()
        {
            // RevenueCat ile geri yükleme (tüm platformlarda çalışır)
            if (RevenueCatManager.Instance != null && RevenueCatManager.Instance.IsConfigured)
            {
                RevenueCatManager.Instance.RestorePurchases();
                return;
            }

#if UNITY_PURCHASING
            if (!IsInitialized())
            {
                Debug.LogWarning("Geri yükleme başarısız. IAP başlatılmadı.");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("Satın alımlar geri yükleniyor (iOS/Mac)...");
                var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result, message) =>
                {
                    Debug.Log($"Geri yükleme işlemi tamamlandı. Sonuç: {result}. Mesaj: {message}");
                });
            }
            else
            {
                Debug.LogWarning("Bu platform satın alımları geri yüklemeyi desteklemiyor.");
            }
#else
            Debug.LogWarning("UNITY_PURCHASING tanımlı değil. Satın alımlar geri yüklenemez.");
#endif
        }

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("IAP başarıyla başlatıldı.");
            storeController = controller;
            storeExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"IAP başlatma başarısız: {error}. Mesaj: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            string productId = purchaseEvent.purchasedProduct.definition.id;
            Debug.Log($"Satın alma başarılı: {productId}");

            HandlePurchaseRewards(productId);
            OnPurchaseSuccess?.Invoke(productId);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"Satın alma başarısız oldu: {product.definition.id}. Sebep: {failureReason}");
            OnPurchaseFailed?.Invoke(product.definition.id, failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"Satın alma başarısız oldu: {product.definition.id}. Mesaj: {failureDescription.message}");
            OnPurchaseFailed?.Invoke(product.definition.id, failureDescription.message);
        }
#endif

        public void SimulatePurchaseSuccess(string productId)
        {
            HandlePurchaseRewards(productId);
            OnPurchaseSuccess?.Invoke(productId);
        }

        public void HandlePurchaseRewards(string productId)
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
                        if (PotionPassManager.Instance != null) PotionPassManager.Instance.UpgradeToPremium();
                        break;
                    case ShopRewardType.RemoveAds:
                        PlayerPrefs.SetInt("AdsRemoved", 1);
                        PlayerPrefs.Save();
                        Debug.Log("[IAP] Reklamlar kaldırıldı olarak kaydedildi.");
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

            // Yedek sabitler
            switch (productId)
            {
                case KADIM_PARA_SMALL:
                    if (gameManager != null) gameManager.AddKadimPara(10);
                    break;
                case KADIM_PARA_MEDIUM:
                    if (gameManager != null) gameManager.AddKadimPara(50);
                    break;
                case KADIM_PARA_LARGE:
                    if (gameManager != null) gameManager.AddKadimPara(150);
                    break;
                case POTION_PASS_PREMIUM:
                    if (PotionPassManager.Instance != null) PotionPassManager.Instance.UpgradeToPremium();
                    break;
                case REMOVE_ADS:
                    PlayerPrefs.SetInt("AdsRemoved", 1);
                    PlayerPrefs.Save();
                    break;
            }
        }
    }
}
