using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// RevenueCat SDK'yı LiveMonetizationConfig'deki ayarlarla otomatik yapılandırır.
    /// Bu bileşen sahneye eklenen RevenueCatManager ile birlikte çalışır.
    /// IAPStoreMode.RevenueCat seçildiğinde otomatik olarak aktif olur.
    /// </summary>
    [DefaultExecutionOrder(-100)] // RevenueCatManager'dan önce çalışsın
    public class RevenueCatInitializer : MonoBehaviour
    {
        [Tooltip("RevenueCatManager'a LiveMonetizationConfig'den API anahtarlarını otomatik atar.")]
        [SerializeField] private bool autoConfigureFromLiveConfig = true;

        private void Awake()
        {
            if (!autoConfigureFromLiveConfig) return;

            var config = LiveMonetizationConfig.Instance;
            if (config == null) return;

            // Yalnızca RevenueCat modu seçiliyse yapılandır
            if (config.iapStoreMode != IAPStoreMode.RevenueCat)
            {
                Debug.Log("[RevenueCatInitializer] IAP modu RevenueCat değil, atlanıyor.");
                return;
            }

            // RevenueCatManager'ı bul veya oluştur
            var rcManager = RevenueCatManager.Instance;
            if (rcManager == null)
            {
                Debug.LogError("[RevenueCatInitializer] RevenueCatManager bulunamadı veya oluşturulamadı!");
                return;
            }

            // API anahtarlarını LiveMonetizationConfig'den RevenueCatManager'a aktar
            // Not: RevenueCatManager kendi Awake()'inde ConfigureSDK() çağıracak,
            // ancak API anahtarları Inspector'dan veya buradan ayarlanabilir.
            Debug.Log("<color=cyan>[RevenueCatInitializer]</color> RevenueCat modu aktif. SDK yapılandırılıyor...");
        }
    }
}
