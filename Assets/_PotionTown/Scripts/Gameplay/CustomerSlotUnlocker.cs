using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    public class CustomerSlotUnlocker : MonoBehaviour
    {
        [Header("Geliştirme Ayarları")]
        [Tooltip("Bu buton tıklandığında müşteri kapasitesini 1 artırır.")]
        public int upgradeCost = 500;
        public int maxCapacityLimit = 3; // En fazla kaç müşteri slotu olabilir?

        [Header("UI Referansları")]
        public TextMeshProUGUI costText;
        public GameObject maxCapacityReachedUI; // Kapasite dolduğunda gösterilecek UI (opsiyonel)
        public Button upgradeButton;

        private void Start()
        {
            if (upgradeButton == null)
                upgradeButton = GetComponent<Button>();

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged += HandleShopStatusChanged;
            }

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
            }

            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateUI();
        }

        private void HandleShopStatusChanged(bool isOpen)
        {
            // Dükkan açıksa (level başlamışsa) butonu gizle, kapalıysa göster
            gameObject.SetActive(!isOpen);
        }

        private void UpdateUI()
        {
            int currentCapacity = CustomerSpawner.Instance != null ? CustomerSpawner.Instance.maxSimultaneousCustomers : PlayerPrefs.GetInt("MaxCustomerSlots", 1);
            
            bool isMaxed = currentCapacity >= maxCapacityLimit;

            if (upgradeButton != null)
                upgradeButton.interactable = !isMaxed;

            if (costText != null)
            {
                costText.text = isMaxed ? LocalizationManager.Get("customer_max_capacity") : $"{upgradeCost} {LocalizationManager.Get("gold_suffix")}";
            }

            if (maxCapacityReachedUI != null)
            {
                maxCapacityReachedUI.SetActive(isMaxed);
            }
        }

        public void OnUpgradeClicked()
        {
            int currentCapacity = CustomerSpawner.Instance != null ? CustomerSpawner.Instance.maxSimultaneousCustomers : PlayerPrefs.GetInt("MaxCustomerSlots", 1);

            if (currentCapacity >= maxCapacityLimit)
            {
                Debug.Log("Maksimum müşteri kapasitesine zaten ulaştınız!");
                return;
            }

            // Altın kontrolü
            if (GameManager.Instance != null && GameManager.Instance.SpendGold(upgradeCost))
            {
                CurrencyFeedbackUI.ShowLoss(upgradeCost, transform);

                // Kapasiteyi artır
                currentCapacity++;
                if (CustomerSpawner.Instance != null)
                {
                    CustomerSpawner.Instance.maxSimultaneousCustomers = currentCapacity;
                }
                else
                {
                    PlayerPrefs.SetInt("MaxCustomerSlots", currentCapacity);
                    PlayerPrefs.Save();
                }

                Debug.Log("Yeni Müşteri Slotu Açıldı! Mevcut Kapasite: " + currentCapacity);
                UpdateUI();
            }
            else
            {
                ToastNotificationUI.ShowWarning("err_not_enough_gold", transform);
                Debug.LogWarning("Yeterli altın yok!");
            }
        }
    }
}
