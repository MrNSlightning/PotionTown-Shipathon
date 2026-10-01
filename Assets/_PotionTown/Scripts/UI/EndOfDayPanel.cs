using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PotionShop.UI
{
    /// <summary>
    /// Gün sonu özet paneli.
    /// - Günlük karşılanan müşteri sayısı ve memnuniyet kırılımı (Mutlu, Normal, Huysuz, Sinirli)
    /// - Finansal bilanço: Kazanılan Gelir, Kaçan/Kayıp Gelir ve Net Kâr
    /// - Görsel emoji özeti
    /// - "Yeni Güne Başla" butonu ve kapatma "X" (Mühür) butonu
    /// NOT: Bu script prefab üzerindeki RectTransform boyutlarını, fontları veya renkleri
    /// ezmez; tamamen prefab üzerinden Inspector ile özelleştirilebilir.
    /// </summary>
    public class EndOfDayPanel : MonoBehaviour
    {
        public static EndOfDayPanel Instance { get; private set; }

        [Header("Pencere & Animasyon")]
        [Tooltip("Pencere gövdesi (Parchment/Window)")]
        public RectTransform dialogWindow;
        [Tooltip("Görünürlük geçişi için CanvasGroup")]
        public CanvasGroup canvasGroup;
        [Tooltip("Açılışta yumuşak fade ve scale efekti olsun mu?")]
        public bool animateOpen = true;

        [Header("Başlık & Alt Başlık")]
        [Tooltip("Örn: '📜 GÜN 1 ÖZETİ 📜'")]
        public TextMeshProUGUI titleText;
        [Tooltip("Örn: 'Dükkan Gün Sonu Raporu'")]
        public TextMeshProUGUI subtitleText;

        [Header("Müşteri Raporu Metinleri")]
        [Tooltip("Toplam müşteri sayısı metni")]
        public TextMeshProUGUI totalCustomersText;
        [Tooltip("Mutlu müşteri sayısı metni")]
        public TextMeshProUGUI happyText;
        [Tooltip("Normal müşteri sayısı metni")]
        public TextMeshProUGUI normalText;
        [Tooltip("Huysuz müşteri sayısı metni")]
        public TextMeshProUGUI grumpyText;
        [Tooltip("Sinirli müşteri sayısı metni")]
        public TextMeshProUGUI angryText;
        [Tooltip("Eski sahnelerle uyumluluk için kaçan müşteri metni (opsiyonel)")]
        public TextMeshProUGUI leftText;

        [Header("Görsel Emoji / Durum Dizilimi")]
        [Tooltip("Günün emojili müşteri özeti")]
        public TextMeshProUGUI emojiSummaryText;
        [Tooltip("Mood ikonları (Opsiyonel)")]
        public Sprite[] moodSprites;
        public Transform emojiContainer;

        [Header("Finansal Rapor Metinleri")]
        [Tooltip("Kazanılan toplam altın")]
        public TextMeshProUGUI earnedGoldText;
        [Tooltip("Kaçan veya kaybedilen altın")]
        public TextMeshProUGUI lostGoldText;
        [Tooltip("Net kâr miktarı")]
        public TextMeshProUGUI netProfitText;

        [Header("Butonlar")]
        [Tooltip("Sonraki güne geçiren ana aksiyon butonu")]
        public Button nextDayButton;
        [Tooltip("Pencereyi kapatan mühür / X butonu")]
        public Button closeButton;

        [Header("Harici Lobi Butonu (Opsiyonel)")]
        [Tooltip("Lobi ekranında bulunacak olan ve paneli açan harici buton.")]
        public GameObject openPanelButton;

        [Header("Sahne ve Menü Bağlantıları (Inspector)")]
        [Tooltip("Sahne ve oda koordinatörü (Lobiyi açmak için)")]
        public PotionShop.PotionTownSceneCoordinator sceneCoordinator;

        [Tooltip("Lobi arka planlı oyun başlangıç menüsü")]
        public GameStartMenuUI gameStartMenuUI;

        [Header("Geriye Uyumluluk")]
        [Tooltip("Eski kodlarla uyumluluk için referans")]
        public RectTransform panelRect;

        [Header("Sıralı Yazı Efekti")]
        [Tooltip("Satırların sırayla ekrana gelme gecikmesi (0 = anında göster)")]
        public float lineDelay = 0.2f;

        private Coroutine _revealCoroutine;
        private bool _revealComplete = false;
        private bool _isTransitioning = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureReferences();
            HookButtons();
        }

        private void OnEnable()
        {
            EnsureReferences();
            HookButtons();
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateLocalizedButton();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateAllLocalizedTexts();
        }

        public void UpdateLocalizedButton()
        {
            if (nextDayButton != null)
            {
                var txt = nextDayButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = LocalizationManager.Get("eod_start_new_day");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }
        }

        public void EnsureReferences()
        {
            if (dialogWindow == null && panelRect != null)
                dialogWindow = panelRect;
            else if (dialogWindow == null)
                dialogWindow = GetComponent<RectTransform>();

            if (sceneCoordinator == null)
            {
                sceneCoordinator = PotionShop.PotionTownSceneCoordinator.Instance ?? FindFirstObjectByType<PotionShop.PotionTownSceneCoordinator>(FindObjectsInactive.Include);
            }

            if (gameStartMenuUI == null)
            {
                gameStartMenuUI = GameStartMenuUI.Instance ?? FindFirstObjectByType<GameStartMenuUI>(FindObjectsInactive.Include);
            }
        }

        private void HookButtons()
        {
            EnsureReferences();

            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(ConfirmEndDay);
                nextDayButton.onClick.AddListener(ConfirmEndDay);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        private void Update()
        {
            // Yalnızca harici bir buton atanmışsa ve bu panelin kendisi DEĞİLSE görünürlüğünü güncelle
            if (openPanelButton != null && openPanelButton != gameObject)
            {
                // Artık zaman yöneticisi olmadığı için bu butonu kullanıyorsak gizli tutabiliriz
                if (openPanelButton.activeSelf)
                {
                    openPanelButton.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Gün sonu özet panelini açar ve istatistikleri sırayla ekrana yansıtır.
        /// </summary>
        public void OpenPanel()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCauldronBoiling(0f);
            }

            gameObject.SetActive(true);
            _revealComplete = false;
            _isTransitioning = false;

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }

            if (animateOpen && canvasGroup != null)
            {
                StartCoroutine(AnimateOpenEffect());
            }

            _revealCoroutine = StartCoroutine(RevealStatsSequentially());
        }

        private IEnumerator AnimateOpenEffect()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            if (dialogWindow != null) dialogWindow.localScale = new Vector3(0.94f, 0.94f, 1f);

            float dur = 0.22f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float ease = Mathf.SmoothStep(0f, 1f, t);

                canvasGroup.alpha = ease;
                if (dialogWindow != null)
                {
                    float s = Mathf.Lerp(0.94f, 1f, ease);
                    dialogWindow.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (dialogWindow != null) dialogWindow.localScale = Vector3.one;
        }

        /// <summary>
        /// Günlük istatistikleri mevcut prefab tasarımına göre doldurur.
        /// Prefab üzerindeki fontları, boyutları ve hizalamaları bozmaz.
        /// </summary>
        private IEnumerator RevealStatsSequentially()
        {
            var gm = GameManager.Instance;

            int currentLevel = LevelSystem.CurrentLevel;
            int total = gm != null ? gm.dailyTotalCustomers : 0;
            int happy = gm != null ? gm.dailyHappy : 0;
            int normal = gm != null ? gm.dailyNormal : 0;
            int grumpy = gm != null ? gm.dailyGrumpy : 0;
            int angry = gm != null ? gm.dailyAngry : 0;
            int earned = gm != null ? gm.dailyEarnedGold : 0;
            int lost = gm != null ? gm.dailyLostGold : 0;
            int net = gm != null ? gm.DailyNetProfit : (earned - lost);

            // 1) Başlık
            if (titleText != null)
            {
                titleText.text = string.Format(LocalizationManager.Get("eod_title"), currentLevel);
                titleText.gameObject.SetActive(true);
            }
            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.Get("eod_subtitle");
                subtitleText.gameObject.SetActive(true);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 2) Toplam Müşteri
            if (totalCustomersText != null)
            {
                totalCustomersText.text = string.Format(LocalizationManager.Get("eod_total_customers"), total);
                totalCustomersText.gameObject.SetActive(true);
            }
            if (emojiSummaryText != null)
            {
                emojiSummaryText.text = "";
                emojiSummaryText.gameObject.SetActive(false);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 3) Mutlu
            if (happyText != null)
            {
                happyText.text = string.Format(LocalizationManager.Get("eod_happy"), happy);
                happyText.gameObject.SetActive(true);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 4) Normal
            if (normalText != null)
            {
                normalText.text = string.Format(LocalizationManager.Get("eod_normal"), normal);
                normalText.gameObject.SetActive(true);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 5) Huysuz
            if (grumpyText != null)
            {
                grumpyText.text = string.Format(LocalizationManager.Get("eod_grumpy"), grumpy);
                grumpyText.gameObject.SetActive(true);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 6) Sinirli
            if (angryText != null)
            {
                angryText.text = string.Format(LocalizationManager.Get("eod_angry"), angry);
                angryText.gameObject.SetActive(true);
            }

            if (lineDelay > 0f) yield return new WaitForSecondsRealtime(lineDelay);

            // 7) Finansal Durum
            string netSign = net >= 0 ? "+" : "";
            string netColor = net >= 0 ? "#FFD700" : "#E74C3C";

            if (earnedGoldText != null)
            {
                earnedGoldText.text = string.Format(LocalizationManager.Get("eod_earned_gold"), earned.ToString("N0"));
                earnedGoldText.gameObject.SetActive(true);
            }
            if (lostGoldText != null)
            {
                lostGoldText.text = string.Format(LocalizationManager.Get("eod_lost_gold"), lost.ToString("N0"));
                lostGoldText.gameObject.SetActive(true);
            }
            if (netProfitText != null)
            {
                netProfitText.text = string.Format(LocalizationManager.Get("eod_net_profit"), netColor, netSign, net.ToString("N0"));
                netProfitText.gameObject.SetActive(true);
            }

            // Geriye uyumluluk için fallback
            if (earnedGoldText == null && leftText != null)
            {
                leftText.text = $"Gelir: <color=#2ECC71>+{earned:N0}</color> | Kayıp: <color=#E74C3C>-{lost:N0}</color>\n<b>Net Kâr: <color={netColor}>{netSign}{net:N0} Altın</color></b>";
                leftText.gameObject.SetActive(true);
            }

            // "Yeni Güne Başla" butonunu aktif et
            UpdateLocalizedButton();
            if (nextDayButton != null)
            {
                nextDayButton.gameObject.SetActive(true);
                nextDayButton.interactable = true;
            }

            _revealCoroutine = null;
            _revealComplete = true;
        }

        public void UpdateAllLocalizedTexts()
        {
            UpdateLocalizedButton();

            if (!gameObject.activeInHierarchy || !_revealComplete) return;

            var gm = GameManager.Instance;
            int currentLevel = LevelSystem.CurrentLevel;
            int total = gm != null ? gm.dailyTotalCustomers : 0;
            int happy = gm != null ? gm.dailyHappy : 0;
            int normal = gm != null ? gm.dailyNormal : 0;
            int grumpy = gm != null ? gm.dailyGrumpy : 0;
            int angry = gm != null ? gm.dailyAngry : 0;
            int earned = gm != null ? gm.dailyEarnedGold : 0;
            int lost = gm != null ? gm.dailyLostGold : 0;
            int net = gm != null ? gm.DailyNetProfit : (earned - lost);

            if (titleText != null)
                titleText.text = string.Format(LocalizationManager.Get("eod_title"), currentLevel);

            if (subtitleText != null)
                subtitleText.text = LocalizationManager.Get("eod_subtitle");

            if (totalCustomersText != null)
                totalCustomersText.text = string.Format(LocalizationManager.Get("eod_total_customers"), total);

            if (happyText != null)
                happyText.text = string.Format(LocalizationManager.Get("eod_happy"), happy);

            if (normalText != null)
                normalText.text = string.Format(LocalizationManager.Get("eod_normal"), normal);

            if (grumpyText != null)
                grumpyText.text = string.Format(LocalizationManager.Get("eod_grumpy"), grumpy);

            if (angryText != null)
                angryText.text = string.Format(LocalizationManager.Get("eod_angry"), angry);

            string netSign = net >= 0 ? "+" : "";
            string netColor = net >= 0 ? "#FFD700" : "#E74C3C";

            if (earnedGoldText != null)
                earnedGoldText.text = string.Format(LocalizationManager.Get("eod_earned_gold"), earned.ToString("N0"));

            if (lostGoldText != null)
                lostGoldText.text = string.Format(LocalizationManager.Get("eod_lost_gold"), lost.ToString("N0"));

            if (netProfitText != null)
                netProfitText.text = string.Format(LocalizationManager.Get("eod_net_profit"), netColor, netSign, net.ToString("N0"));
        }

        /// <summary>
        /// Gün sonu panelini kapatır, lobi sahnesine geçiş yapar ve lobi arkaplanlı
        /// oyun başlatma menüsünü (GameStartMenuUI) açar.
        /// </summary>
        public void ClosePanel()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            _revealComplete = false;

            EnsureReferences();

            // 1. Yeni günün akışını hazırla (seviyeyi ilerlet ve müşteri akışını ilklendir)
            LevelSystem.AdvanceLevel();
            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.InitializeLevel();
            }

            // 2. Gün sonu panelini kapat
            gameObject.SetActive(false);

            // 3. Lobi sahnesine geçiş yap (Böylece arka plan lobi olur)
            if (sceneCoordinator != null)
            {
                sceneCoordinator.ShowLobby();
            }
            else if (PotionShop.PotionTownSceneCoordinator.Instance != null)
            {
                PotionShop.PotionTownSceneCoordinator.Instance.ShowLobby();
            }
            else if (PotionShop.TopDropdownMenu.Instance != null)
            {
                PotionShop.TopDropdownMenu.Instance.CloseAllShopsAndOpenLobby();
            }

            // 4. Lobi arkaplanlı oyun başlatma menüsünü aç
            if (gameStartMenuUI != null)
            {
                gameStartMenuUI.gameObject.SetActive(true);
                gameStartMenuUI.Show();
            }
            else if (GameStartMenuUI.Instance != null)
            {
                GameStartMenuUI.Instance.gameObject.SetActive(true);
                GameStartMenuUI.Instance.Show();
            }
            else
            {
                var startMenu = FindFirstObjectByType<GameStartMenuUI>(FindObjectsInactive.Include);
                if (startMenu != null)
                {
                    startMenu.gameObject.SetActive(true);
                    startMenu.Show();
                }
            }

            Debug.Log("<color=cyan>[EndOfDayPanel] Gün sonu paneli kapatıldı, lobiye dönüldü ve lobi arkaplanlı oyun başlatma menüsü açıldı.</color>");
        }

        /// <summary>
        /// Gün sonunu onaylar, sonraki güne geçer, oyun başlangıç menüsündeki "güne başla" işlemi ile
        /// aynı işlemi gerçekleştirir ve oyuncuyu lobi sahnesine atar.
        /// "Yeni Güne Başla" butonuna basıldığında tetiklenir.
        /// </summary>
        public void ConfirmEndDay()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            _revealComplete = false;

            EnsureReferences();

            // 1. Gün sonu al ve yeni güne geç
            LevelSystem.AdvanceLevel();

            // Yeni seviye/gün müşteri akışını başlat (Dükkan kapalı kalacak, açınca başlayacak)
            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.InitializeLevel();
            }

            // 2. Oyun başlangıç menüsündeki güne başla butonu ile aynı işlemi yap (günü başlat/menüyü kapat)
            if (gameStartMenuUI != null)
            {
                gameStartMenuUI.OnStartLevelClicked();
            }
            else if (GameStartMenuUI.Instance != null)
            {
                GameStartMenuUI.Instance.OnStartLevelClicked();
            }

            // 3. Lobi sahnesine geçiş yap
            if (sceneCoordinator != null)
            {
                sceneCoordinator.ShowLobby();
            }
            else if (PotionShop.PotionTownSceneCoordinator.Instance != null)
            {
                PotionShop.PotionTownSceneCoordinator.Instance.ShowLobby();
            }
            else if (PotionShop.TopDropdownMenu.Instance != null)
            {
                PotionShop.TopDropdownMenu.Instance.CloseAllShopsAndOpenLobby();
            }

            // 4. Gün sonu panelini kapat
            gameObject.SetActive(false);
            Debug.Log("<color=green>[EndOfDayPanel] Yeni güne başlandı, başlangıç menüsü işlemi tamamlandı ve lobi sahnesine geçildi!</color>");
        }

        public void OnNextDayButtonClicked() => ConfirmEndDay();
        public void OnCloseButtonClicked() => ClosePanel();

#if UNITY_EDITOR
        [ContextMenu("Test Open Panel (Editor)")]
        private void TestOpen()
        {
            OpenPanel();
        }

        [ContextMenu("Test Close Panel (Editor)")]
        private void TestClose()
        {
            ClosePanel();
        }
#endif
    }
}
