using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PotionShop
{
    public class PotionPassUI : MonoBehaviour
    {
        [Header("Arayüz Referansları")]
        [Tooltip("Aşama slotlarının ebeveyni")]
        public RectTransform tierContainer;
        [Tooltip("Bilet veri dosyası referansı (manager'dan da alınabilir)")]
        public PotionPassData passData;
        public TextMeshProUGUI seasonNameText;
        public Image xpProgressBar;
        public TextMeshProUGUI xpProgressText;
        public Button buyPremiumButton;
        public Button closeButton;

        [Header("Slot Prefab")]
        [Tooltip("Her bir aşamayı temsil edecek slot prefab'ı")]
        public GameObject tierSlotPrefab; 

        private void Start()
        {
            if (buyPremiumButton != null)
                buyPremiumButton.onClick.AddListener(OnBuyPremiumClicked);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (PotionPassManager.IsInitialized)
            {
                PotionPassManager.Instance.OnXPGained += UpdateProgress;
                PotionPassManager.Instance.OnTierUp += UpdateProgressOnTierUp;
                PotionPassManager.Instance.OnRewardClaimed += OnRewardClaimed;
            }

            InitializeUI();
        }

        private void OnDestroy()
        {
            if (PotionPassManager.IsInitialized)
            {
                PotionPassManager.Instance.OnXPGained -= UpdateProgress;
                PotionPassManager.Instance.OnTierUp -= UpdateProgressOnTierUp;
                PotionPassManager.Instance.OnRewardClaimed -= OnRewardClaimed;
            }
        }

        private void InitializeUI()
        {
            if (passData == null && PotionPassManager.IsInitialized)
            {
                passData = PotionPassManager.Instance.passData;
            }

            if (passData == null) return;

            if (seasonNameText != null)
                seasonNameText.text = passData.seasonName;

            UpdateProgress(0); // İlk baştaki XP barı güncellemesi
            UpdatePremiumButtonState();

            // Tüm slotları baştan oluştur / güncelle
            Refresh();
        }

        /// <summary>
        /// Tüm slotları günceller.
        /// </summary>
        public void Refresh()
        {
            // Bu kısımda Normalde instantiate edilen slotların güncellenmesi sağlanır.
            // Örneğin altın çerçeve ekleme gibi mantıklar mevcut aşamaya (CurrentTier) göre yapılmalıdır.
            Debug.Log("Potion Pass arayüzü güncelleniyor.");
            
            // Eğer PotionPassManager.Instance.CurrentTier mevcut slota eşitse, altın çerçeve vurgusu eklenir.
        }

        private void UpdateProgress(int xpGained)
        {
            if (PotionPassManager.IsInitialized && passData != null)
            {
                if (xpProgressBar != null)
                    xpProgressBar.fillAmount = PotionPassManager.Instance.GetTierProgress();
                
                if (xpProgressText != null)
                {
                    int currentXP = PotionPassManager.Instance.CurrentXP;
                    int requiredXP = passData.xpPerTier;
                    xpProgressText.text = $"{currentXP} / {requiredXP} XP";
                }
            }
        }

        private void UpdateProgressOnTierUp(int newTier)
        {
            UpdateProgress(0);
            Refresh();
        }

        private void OnRewardClaimed(int tier, bool isPremium)
        {
            Refresh();
        }

        private void OnBuyPremiumClicked()
        {
            if (PotionPassManager.IsInitialized)
            {
                // Premium satın alma işlemini tetikle
                PotionPassManager.Instance.UpgradeToPremium();
                UpdatePremiumButtonState();
                Refresh();
            }
        }

        private void UpdatePremiumButtonState()
        {
            if (buyPremiumButton != null && PotionPassManager.IsInitialized)
            {
                if (PotionPassManager.Instance.IsPremium)
                {
                    buyPremiumButton.interactable = false;
                    var btnText = buyPremiumButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "Premium Aktif";
                }
            }
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/PotionTavern - Potion Pass Panel")]
        public static void CreatePotionPassPanel()
        {
            // Kanvas bul veya oluştur
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Ana Panel
            GameObject panel = new GameObject("PotionPassPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // Header Bölümü
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.8f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = Vector2.zero;
            
            GameObject seasonTextGo = new GameObject("SeasonText");
            seasonTextGo.transform.SetParent(header.transform, false);
            TextMeshProUGUI seasonText = seasonTextGo.AddComponent<TextMeshProUGUI>();
            seasonText.text = "Sezon 1: Büyülü Başlangıç";
            seasonText.alignment = TextAlignmentOptions.Center;
            seasonText.fontSize = 36;
            RectTransform stRect = seasonText.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0, 0.5f);
            stRect.anchorMax = new Vector2(1, 1);
            stRect.sizeDelta = Vector2.zero;

            GameObject xpBarBg = new GameObject("XPBar_BG");
            xpBarBg.transform.SetParent(header.transform, false);
            RectTransform xpBgRect = xpBarBg.AddComponent<RectTransform>();
            xpBgRect.anchorMin = new Vector2(0.2f, 0.1f);
            xpBgRect.anchorMax = new Vector2(0.8f, 0.4f);
            xpBgRect.sizeDelta = Vector2.zero;
            xpBarBg.AddComponent<Image>().color = Color.gray;
            
            GameObject xpBarFill = new GameObject("XPBar_Fill");
            xpBarFill.transform.SetParent(xpBarBg.transform, false);
            RectTransform xpFillRect = xpBarFill.AddComponent<RectTransform>();
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = Vector2.one;
            xpFillRect.sizeDelta = Vector2.zero;
            Image fillImage = xpBarFill.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0.5f;

            // ScrollRect ve Container
            GameObject scrollView = new GameObject("Scroll View");
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform scrollRectTransform = scrollView.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.05f, 0.2f);
            scrollRectTransform.anchorMax = new Vector2(0.95f, 0.75f);
            scrollRectTransform.sizeDelta = Vector2.zero;
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(3000, 0);
            
            HorizontalLayoutGroup hLayout = content.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(10, 10, 10, 10);
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = false;

            ScrollRect sr = scrollView.AddComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = viewportRect;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;

            // Örnek Tier Slot'lar eklenebilir (30 adet)
            for (int i = 1; i <= 30; i++)
            {
                GameObject tierSlot = new GameObject($"TierSlot_{i}");
                tierSlot.transform.SetParent(content.transform, false);
                RectTransform slotRect = tierSlot.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(100, 0); // Yatay boyut
                tierSlot.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            // Alt Butonlar
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(panel.transform, false);
            RectTransform footerRect = footer.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0.15f);
            footerRect.sizeDelta = Vector2.zero;

            GameObject buyBtn = new GameObject("BuyPremiumBtn");
            buyBtn.transform.SetParent(footer.transform, false);
            RectTransform buyRect = buyBtn.AddComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.3f, 0.2f);
            buyRect.anchorMax = new Vector2(0.7f, 0.8f);
            buyRect.sizeDelta = Vector2.zero;
            buyBtn.AddComponent<Image>().color = Color.yellow;
            Button buyButtonComp = buyBtn.AddComponent<Button>();
            
            GameObject buyTextGo = new GameObject("Text");
            buyTextGo.transform.SetParent(buyBtn.transform, false);
            TextMeshProUGUI buyText = buyTextGo.AddComponent<TextMeshProUGUI>();
            buyText.text = "Premium Satın Al (500 Kadim Para)";
            buyText.alignment = TextAlignmentOptions.Center;
            buyText.color = Color.black;
            RectTransform bTextRect = buyText.GetComponent<RectTransform>();
            bTextRect.anchorMin = Vector2.zero;
            bTextRect.anchorMax = Vector2.one;
            bTextRect.sizeDelta = Vector2.zero;

            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(panel.transform, false);
            RectTransform closeRect = closeBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.9f, 0.9f);
            closeRect.anchorMax = new Vector2(0.98f, 0.98f);
            closeRect.sizeDelta = Vector2.zero;
            closeBtn.AddComponent<Image>().color = Color.red;
            Button closeButtonComp = closeBtn.AddComponent<Button>();
            
            GameObject closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeBtn.transform, false);
            TextMeshProUGUI closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.alignment = TextAlignmentOptions.Center;
            RectTransform cTextRect = closeText.GetComponent<RectTransform>();
            cTextRect.anchorMin = Vector2.zero;
            cTextRect.anchorMax = Vector2.one;
            cTextRect.sizeDelta = Vector2.zero;

            // Script'i bağla
            PotionPassUI uiScript = panel.AddComponent<PotionPassUI>();
            uiScript.seasonNameText = seasonText;
            uiScript.xpProgressBar = fillImage;
            uiScript.tierContainer = contentRect;
            uiScript.buyPremiumButton = buyButtonComp;
            uiScript.closeButton = closeButtonComp;

            Selection.activeGameObject = panel;
        }
#endif
    }
}
