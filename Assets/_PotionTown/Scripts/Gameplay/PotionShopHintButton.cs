using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// İksir Satış Dükkanı için reklam karşılığı çalışan Tüyo / İpucu sistemi.
    /// Her kullanıldığında o bölümde gelecek iksirler ve sırada bekleyen müşteriler
    /// hakkında detaylı bilgi verir ve müşterinin sabrını tazeler.
    /// New Rocker fontu ve oyunun orijinal görsel temasıyla uyumludur.
    /// </summary>
    public class PotionShopHintButton : MonoBehaviour
    {
        private static PotionShopHintButton _instance;
        public static PotionShopHintButton Instance => _instance;

        [Header("UI Buton Referansları (Inspector Bağlantıları)")]
        [Tooltip("İksir dükkanındaki Tüyo butonu")]
        public Button hintButton;

        [Tooltip("Tüyo butonunun üzerindeki metin")]
        public TextMeshProUGUI hintButtonText;

        [Header("Tüyo Diyalog Paneli")]
        [Tooltip("Tüyo gösterilen diyalog popup objesi")]
        public GameObject hintDialog;

        [Tooltip("Tüyo metninin yazıldığı alan")]
        public TextMeshProUGUI hintContentText;

        [Tooltip("Diyaloğu kapatma butonu")]
        public Button closeDialogButton;

        private GameObject _hintDialog;
        private TextMeshProUGUI _hintContentText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIIfMissing();
        }

        private void Start()
        {
            if (hintButton != null)
            {
                hintButton.onClick.RemoveAllListeners();
                hintButton.onClick.AddListener(OnHintButtonClicked);
            }

            UpdateText();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (hintButtonText != null)
            {
                UIThemeHelper.ApplyNewRocker(hintButtonText);
                hintButtonText.text = LocalizationManager.Get("hint_button_label");
            }
        }

        private void OnHintButtonClicked()
        {
            Debug.Log("<color=yellow>[PotionShopHintButton]</color> Tüyo butonu tıklandı. Reklam talep ediliyor...");

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(OnAdSuccess, OnAdFailed);
            }
            else
            {
                // Fallback simülasyon
                OnAdSuccess();
            }
        }

        private void OnAdSuccess()
        {
            Debug.Log("<color=green>[PotionShopHintButton]</color> Reklam izlendi, iksir tüyosu hazırlanıyor...");

            string hintText = GenerateLevelPotionHint();
            ShowHintDialog(hintText);

            // Aktif müşteri varsa sabrını tazele (Mutlu yap)
            BoostActiveCustomerPatience();
        }

        private void OnAdFailed()
        {
            ToastNotificationUI.ShowWarning("hint_ad_failed");
        }

        /// <summary>
        /// O anki bölümdeki iksir dağılımı ve bekleyen siparişler üzerinden detaylı tüyo metni üretir.
        /// </summary>
        private string GenerateLevelPotionHint()
        {
            int currentLevel = LevelSystem.CurrentLevel;
            var config = LevelDesignDatabase.Instance != null ? LevelDesignDatabase.Instance.GetLevelConfig(currentLevel) : null;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

            if (config != null)
            {
                int totalCustomers = config.totalCustomers;
                int remainingCustomers = Mathf.Max(0, totalCustomers - (CustomerSpawner.Instance != null ? CustomerSpawner.Instance.completedCustomersThisLevel : 0));

                if (isEnglish)
                {
                    sb.AppendLine($"<color=#FFD700><b>🔮 LEVEL {currentLevel} ALCHEMY INTEL</b></color>\n");
                    sb.AppendLine($"• Total Customers in this shift: <b>{totalCustomers}</b> ({remainingCustomers} remaining)");
                    sb.AppendLine("• Expected Potions to be requested:");
                }
                else
                {
                    sb.AppendLine($"<color=#FFD700><b>🔮 {currentLevel}. BÖLÜM İKSİR TÜYOSU</b></color>\n");
                    sb.AppendLine($"• Bu bölümde toplam: <b>{totalCustomers} müşteri</b> gelecek ({remainingCustomers} kaldı)");
                    sb.AppendLine("• İstenmesi kesinleşen iksirler:");
                }

                if (config.possiblePotions != null && config.possiblePotions.Count > 0)
                {
                    // Havuzdaki iksirleri ve tahmini adetlerini listele
                    int potionCount = config.possiblePotions.Count;
                    int avgPerType = Mathf.Max(1, totalCustomers / potionCount);

                    foreach (var potion in config.possiblePotions)
                    {
                        if (potion == null) continue;
                        string pName = potion.LocalizedName;
                        if (isEnglish)
                        {
                            sb.AppendLine($"  - ~{avgPerType}x <b>{pName}</b>");
                        }
                        else
                        {
                            sb.AppendLine($"  - Yaklaşık {avgPerType} adet <b>{pName}</b>");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine(isEnglish ? "Level details currently unavailable." : "Bölüm detayları şu an okunamadı.");
            }

            // Aktif müşteri kontrolü
            Customer active = CustomerSpawner.Instance != null ? CustomerSpawner.Instance.CurrentCustomer : null;
            if (active != null && active.requestedPotion != null)
            {
                sb.AppendLine();
                if (isEnglish)
                {
                    sb.AppendLine($"<color=#55FF55>✨ Current customer waiting for: <b>{active.requestedPotion.LocalizedName}</b>!</color>");
                    sb.AppendLine("<size=20><i>(Customer patience has been restored to Maximum!)</i></size>");
                }
                else
                {
                    sb.AppendLine($"<color=#55FF55>✨ Sırada bekleyen müşteri: <b>{active.requestedPotion.LocalizedName}</b> istiyor!</color>");
                    sb.AppendLine("<size=20><i>(Müşterinin sabrı Mutlu seviyesine tazelendi!)</i></size>");
                }
            }

            return sb.ToString();
        }

        private void BoostActiveCustomerPatience()
        {
            if (CustomerSpawner.Instance == null) return;
            foreach (var c in CustomerSpawner.Instance.activeCustomers)
            {
                if (c != null)
                {
                    c.SetMood(Customer.Mood.Mutlu);
                }
            }
            ToastNotificationUI.ShowSuccess("hint_patience_boost");
        }

        private void ShowHintDialog(string content)
        {
            if (_hintDialog != null)
            {
                if (_hintContentText != null)
                {
                    UIThemeHelper.ApplyNewRocker(_hintContentText);
                    _hintContentText.text = content;
                }
                _hintDialog.SetActive(true);
            }
        }

        private void BuildUIIfMissing()
        {
            if (hintButton != null && hintDialog != null && hintContentText != null)
            {
                _hintDialog = hintDialog;
                _hintContentText = hintContentText;
                if (closeDialogButton != null)
                {
                    closeDialogButton.onClick.RemoveAllListeners();
                    closeDialogButton.onClick.AddListener(() => _hintDialog.SetActive(false));
                }
                _hintDialog.SetActive(false);
                return;
            }

            // Eğer sahnedeki PotionSelling room altına tüyo butonu eklenmemişse dinamik oluştur
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = gameObject.AddComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                parentCanvas.overrideSorting = true;
                parentCanvas.sortingOrder = 700; // Müşteri baloncuklarının üzerinde
                gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Buton Objesi
            if (hintButton == null)
            {
                GameObject btnObj = new GameObject("HintButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(transform, false);
                hintButton = btnObj.GetComponent<Button>();

                Image img = btnObj.GetComponent<Image>();
                Sprite frame = UIThemeHelper.GetButtonFrameSprite();
                if (frame != null)
                {
                    img.sprite = frame;
                    img.type = Image.Type.Sliced;
                }
                img.color = new Color(0.22f, 0.45f, 0.65f, 1f);

                RectTransform rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(30f, 40f);
                rt.sizeDelta = new Vector2(200f, 54f);

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
                txtObj.transform.SetParent(btnObj.transform, false);
                hintButtonText = txtObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(hintButtonText);
                hintButtonText.fontSize = 22;
                hintButtonText.color = UIThemeHelper.ColorTextWarm;
                hintButtonText.alignment = TextAlignmentOptions.Center;
                hintButtonText.fontStyle = FontStyles.Bold;

                RectTransform textRT = txtObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;
            }

            // Tüyo Gösterim Modal Paneli (Parchment Styled)
            if (_hintDialog == null)
            {
                _hintDialog = new GameObject("HintDialogModal", typeof(RectTransform), typeof(Image));
                _hintDialog.transform.SetParent(transform, false);
                Image modalBg = _hintDialog.GetComponent<Image>();
                modalBg.color = new Color(0.04f, 0.03f, 0.06f, 0.85f);
                RectTransform modalRT = _hintDialog.GetComponent<RectTransform>();
                modalRT.anchorMin = Vector2.zero;
                modalRT.anchorMax = Vector2.one;
                modalRT.sizeDelta = Vector2.zero;

                // İç Pencere (Parşömen)
                GameObject card = new GameObject("ParchmentCard", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(_hintDialog.transform, false);
                Image cardImg = card.GetComponent<Image>();
                Sprite parch = UIThemeHelper.GetParchmentSprite();
                if (parch != null)
                {
                    cardImg.sprite = parch;
                    cardImg.color = Color.white;
                }
                else
                {
                    cardImg.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
                }

                RectTransform cardRT = card.GetComponent<RectTransform>();
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.sizeDelta = new Vector2(580f, 420f);

                // İçerik Metni
                GameObject contentObj = new GameObject("ContentText", typeof(RectTransform), typeof(TextMeshProUGUI));
                contentObj.transform.SetParent(card.transform, false);
                _hintContentText = contentObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(_hintContentText);
                _hintContentText.fontSize = 24;
                _hintContentText.color = UIThemeHelper.ColorWoodDark; // Parşömen üzeri koyu mürekkep
                _hintContentText.alignment = TextAlignmentOptions.TopLeft;

                RectTransform contRT = contentObj.GetComponent<RectTransform>();
                contRT.anchorMin = new Vector2(0.08f, 0.22f);
                contRT.anchorMax = new Vector2(0.92f, 0.92f);
                contRT.sizeDelta = Vector2.zero;

                // Kapat Butonu
                GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                closeBtnObj.transform.SetParent(card.transform, false);
                Image cImg = closeBtnObj.GetComponent<Image>();
                cImg.color = new Color(0.35f, 0.22f, 0.12f, 1f);
                Button closeBtn = closeBtnObj.GetComponent<Button>();
                closeBtn.onClick.AddListener(() => _hintDialog.SetActive(false));

                RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
                closeRT.anchorMin = new Vector2(0.5f, 0.06f);
                closeRT.anchorMax = new Vector2(0.5f, 0.06f);
                closeRT.pivot = new Vector2(0.5f, 0f);
                closeRT.sizeDelta = new Vector2(180f, 45f);

                GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                cTxtObj.transform.SetParent(closeBtnObj.transform, false);
                TextMeshProUGUI cTmp = cTxtObj.GetComponent<TextMeshProUGUI>();
                UIThemeHelper.ApplyNewRocker(cTmp);
                cTmp.text = LocalizationManager.Get("btn_ok");
                cTmp.fontSize = 22;
                cTmp.color = Color.white;
                cTmp.alignment = TextAlignmentOptions.Center;
                RectTransform cTxtRT = cTxtObj.GetComponent<RectTransform>();
                cTxtRT.anchorMin = Vector2.zero;
                cTxtRT.anchorMax = Vector2.one;
                cTxtRT.sizeDelta = Vector2.zero;

                _hintDialog.SetActive(false);
            }
        }
    }
}
