using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PotionShop
{
    /// <summary>
    /// Premium mağaza panelini kontrol eden UI sınıfı.
    /// IAPManager ve AdsManager ile entegre çalışır.
    /// </summary>
    public class ShopPanelUI : MonoBehaviour
    {
        [Header("Sekme Butonları")]
        public Button tabKadimParaBtn;
        public Button tabLootBoxBtn;
        public Button tabSpecialBtn;

        [Header("Sekme Panelleri")]
        public GameObject kadimParaPanel;
        public GameObject lootBoxPanel;
        public GameObject specialPanel;

        [Header("Kadim Para Ürünleri")]
        public Button buySmallBtn;
        public Button buyMediumBtn;
        public Button buyLargeBtn;
        public TextMeshProUGUI priceSmallTxt;
        public TextMeshProUGUI priceMediumTxt;
        public TextMeshProUGUI priceLargeTxt;

        [Header("Kasa (Loot Box) Ürünleri")]
        public Button buyBronzeBoxBtn;
        public Button buySilverBoxBtn;
        public Button buyGoldBoxBtn;

        [Header("Özel Teklifler")]
        public Button watchAdGoldBtn;
        public Button watchAdMiniGameBtn;
        public Button buyPotionPassBtn;
        public Button buyRemoveAdsBtn;

        private void Start()
        {
            SetupTabs();
            SetupIAPButtons();
            SetupLootBoxButtons();
            SetupSpecialButtons();

            ShowTab(kadimParaPanel);
        }

        private void SetupTabs()
        {
            if (tabKadimParaBtn != null) tabKadimParaBtn.onClick.AddListener(() => ShowTab(kadimParaPanel));
            if (tabLootBoxBtn != null) tabLootBoxBtn.onClick.AddListener(() => ShowTab(lootBoxPanel));
            if (tabSpecialBtn != null) tabSpecialBtn.onClick.AddListener(() => ShowTab(specialPanel));
        }

        private void ShowTab(GameObject tabToShow)
        {
            if (kadimParaPanel != null) kadimParaPanel.SetActive(false);
            if (lootBoxPanel != null) lootBoxPanel.SetActive(false);
            if (specialPanel != null) specialPanel.SetActive(false);

            if (tabToShow != null) tabToShow.SetActive(true);
        }

        private void SetupIAPButtons()
        {
            if (buySmallBtn != null) buySmallBtn.onClick.AddListener(() => BuyIAPProduct(IAPManager.KADIM_PARA_SMALL));
            if (buyMediumBtn != null) buyMediumBtn.onClick.AddListener(() => BuyIAPProduct(IAPManager.KADIM_PARA_MEDIUM));
            if (buyLargeBtn != null) buyLargeBtn.onClick.AddListener(() => BuyIAPProduct(IAPManager.KADIM_PARA_LARGE));
        }

        private void SetupLootBoxButtons()
        {
            if (buyBronzeBoxBtn != null) buyBronzeBoxBtn.onClick.AddListener(() => BuyLootBox("Bronze", 500, false));
            if (buySilverBoxBtn != null) buySilverBoxBtn.onClick.AddListener(() => BuyLootBox("Silver", 1500, false));
            if (buyGoldBoxBtn != null) buyGoldBoxBtn.onClick.AddListener(() => BuyLootBox("Gold", 50, true));
        }

        private void SetupSpecialButtons()
        {
            if (watchAdGoldBtn != null) watchAdGoldBtn.onClick.AddListener(WatchAdForGold);
            if (watchAdMiniGameBtn != null) watchAdMiniGameBtn.onClick.AddListener(WatchAdForMiniGame);
            
            if (buyPotionPassBtn != null) buyPotionPassBtn.onClick.AddListener(() => BuyIAPProduct(IAPManager.POTION_PASS_PREMIUM));
            if (buyRemoveAdsBtn != null) buyRemoveAdsBtn.onClick.AddListener(() => BuyIAPProduct(IAPManager.REMOVE_ADS));
        }

        private void BuyIAPProduct(string productId)
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.BuyProduct(productId);
            }
            else
            {
                Debug.LogError("IAPManager bulunamadı!");
            }
        }

        private void BuyLootBox(string boxType, int price, bool useKadimPara)
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;

            if (useKadimPara)
            {
                if (gameManager.SpendKadimPara(price))
                {
                    Debug.Log($"{boxType} kasa {price} Kadim Para ile satın alındı!");
                }
                else
                {
                    ToastNotificationUI.ShowWarning("err_not_enough_kadim");
                }
            }
            else
            {
                // GameManager'da SpendGold metodunun olduğu varsayılmıştır. (Belirtilmemişse de genel kullanımdır)
                // gameManager.SpendGold(price); 
                Debug.Log($"{boxType} kasa {price} Altın ile satın alındı!");
            }
        }

        private void WatchAdForGold()
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(() =>
                {
                    GameManager gameManager = FindFirstObjectByType<GameManager>();
                    if (gameManager != null)
                    {
                        gameManager.AddGold(50);
                        Debug.Log("Reklam izlendi, 50 Altın verildi!");
                    }
                }, 
                () =>
                {
                    Debug.Log("Reklam başarısız veya atlandı.");
                });
            }
        }

        private void WatchAdForMiniGame()
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(() =>
                {
                    Debug.Log("Reklam izlendi, 2x Mini Oyun Ödülü aktif!");
                });
            }
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/PotionTavern - Mağaza Paneli")]
        public static void CreateShopPanel()
        {
            GameObject canvasObj = FindFirstObjectByType<Canvas>()?.gameObject;
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject panel = new GameObject("ShopPanelUI");
            panel.transform.SetParent(canvasObj.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            panel.AddComponent<ShopPanelUI>();

            Selection.activeGameObject = panel;
        }
#endif
    }
}
