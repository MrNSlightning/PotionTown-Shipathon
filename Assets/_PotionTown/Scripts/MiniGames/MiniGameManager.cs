using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Mini oyun alaninin (Simya & Buyu Arenasi) ana yoneticisi.
    /// - Veri odakli (Data-Driven) Magaza ve Potion Pass yapisi (ShopCatalogData, PotionPassData)
    /// - Gercekci Google Play Satin Alma Modali Simulatoru (IAP)
    /// - 5 saniyelik Sponsorlu Video Reklam Simulatoru (AdsManager)
    /// - 4 Zengin Sekme: Mini Oyunlar, Potion Pass, Magaza, Kasalar
    /// - Kamera ve Canvas guvencesi (Display 1 No cameras rendering cozuldu)
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Kamera & Canvas")]
        public Camera miniGameCamera;
        public GameObject mainPanel;
        public GameObject returnCanvas;

        [Header("Sekme Panelleri")]
        public GameObject miniGameSelectionPanel;
        public GameObject potionPassPanel;
        public GameObject shopPanel;
        public GameObject lootBoxPanel;

        [Header("Sekme Butonlari")]
        public Button tabMiniGames;
        public Button tabPotionPass;
        public Button tabShop;
        public Button tabLootBox;
        public Button closeButton;

        [Header("Mini Oyun Butonlari")]
        public Button runeMatchButton;
        public Button potionCatchButton;
        public Button crystalSortButton;

        [Header("Bilgi UI")]
        public TextMeshProUGUI remainingPlaysText;
        public TextMeshProUGUI cooldownText;
        public TextMeshProUGUI highScoreText;
        public Button watchAdBonusPlayBtn;

        [Header("Potion Pass Referanslari")]
        public TextMeshProUGUI passSeasonText;
        public Image passXpBarFill;
        public TextMeshProUGUI passXpText;
        public Button passUpgradeButton;
        public Transform passTierContainer;

        [Header("Magaza (Shop) Referanslari")]
        public Transform shopParaContainer;
        public Transform shopOffersContainer;
        public Button buyRemoveAdsBtn;

        [Header("Kasa (Loot Box) UI")]
        public TextMeshProUGUI bronzeCountText;
        public TextMeshProUGUI silverCountText;
        public TextMeshProUGUI goldCountText;
        public Button openBronzeBtn;
        public Button openSilverBtn;
        public Button openGoldBtn;
        public Button buyOpenBronzeBtn;
        public Button buyOpenSilverBtn;
        public Button buyOpenGoldBtn;

        [Header("Odul Popup")]
        public GameObject lootRewardPopupPanel;
        public TextMeshProUGUI lootRewardTitleText;
        public TextMeshProUGUI lootRewardDescText;
        public Button closeLootRewardBtn;

        [Header("Hesap UI")]
        public AccountUI accountUI;

        [Header("Google Play Satin Alma Simulator Modali")]
        public GameObject googlePlayModalPanel;
        public TextMeshProUGUI gpItemTitleText;
        public TextMeshProUGUI gpItemPriceText;
        public Button gpConfirmBuyBtn;
        public Button gpCancelBuyBtn;

        [Header("Video Reklam Simulator Modali")]
        public GameObject videoAdSimulatorPanel;
        public TextMeshProUGUI adCountdownText;
        public Image adProgressBar;
        public Button adCloseRewardBtn;
        public Button adCancelBtn;

        // Durum Verileri
        private int _dailyPlaysUsed = 0;
        private float _cooldownTimer = 0f;
        private Dictionary<string, int> _highScores = new Dictionary<string, int>();
        private string _lastPlayDate = "";
        private int _currentTabIndex = 0;

        // Mini oyun referanslari
        private MiniGameBase[] _allGames;

        // Renk Sabitleri
        private readonly Color TabActiveColor = new Color(0.38f, 0.20f, 0.60f, 1f);
        private readonly Color TabInactiveColor = new Color(0.14f, 0.10f, 0.22f, 0.95f);

        // Simulator Callback Delege ve Rutinleri
        private Action _pendingGpConfirm;
        private Action _pendingGpCancel;
        private Action _pendingAdComplete;
        private Action _pendingAdCancel;
        private Coroutine _adCountdownRoutine;

        // Eventler
        public static event Action<string, int> OnMiniGameCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCameraAndReturnCanvas();
            _allGames = GetComponentsInChildren<MiniGameBase>(true);

            // Sekme Butonlari
            HookButton(tabMiniGames, () => ShowTab(0));
            HookButton(tabPotionPass, () => ShowTab(1));
            HookButton(tabShop, () => ShowTab(2));
            HookButton(tabLootBox, () => ShowTab(3));
            HookButton(closeButton, CloseArea);

            // Mini Oyun Butonlari
            HookButton(runeMatchButton, () => TryStartGame("RuneMatch"));
            HookButton(potionCatchButton, () => TryStartGame("PotionCatch"));
            HookButton(crystalSortButton, () => TryStartGame("CrystalSort"));

            // Alt Bar
            HookButton(watchAdBonusPlayBtn, WatchAdForBonusPlay);

            // Potion Pass
            HookButton(passUpgradeButton, UpgradePotionPassPremium);

            // Kasa Butonlari
            HookButton(openBronzeBtn, () => OpenLootBox(LootBoxTier.Bronze));
            HookButton(openSilverBtn, () => OpenLootBox(LootBoxTier.Silver));
            HookButton(openGoldBtn, () => OpenLootBox(LootBoxTier.Gold));

            var config = LiveMonetizationConfig.Instance;
            int bCost = config != null ? config.bronzeBoxPriceGold : 500;
            int sCost = config != null ? config.silverBoxPriceGold : 1500;
            int gCost = config != null ? config.goldBoxPriceKadimPara : 50;

            HookButton(buyOpenBronzeBtn, () => BuyAndOpenLootBox(LootBoxTier.Bronze, bCost, false));
            HookButton(buyOpenSilverBtn, () => BuyAndOpenLootBox(LootBoxTier.Silver, sCost, false));
            HookButton(buyOpenGoldBtn, () => BuyAndOpenLootBox(LootBoxTier.Gold, gCost, true));

            // Odul Kapat Butonu
            HookButton(closeLootRewardBtn, CloseRewardPopup);

            // Google Play Simulator Butonlari
            HookButton(gpConfirmBuyBtn, ConfirmGooglePlayPurchase);
            HookButton(gpCancelBuyBtn, CancelGooglePlayPurchase);

            // Video Reklam Simulator Butonlari
            HookButton(adCloseRewardBtn, CompleteVideoAd);
            HookButton(adCancelBtn, CancelVideoAd);

            // Mini Oyun Sonu Dinleyicileri
            foreach (var game in _allGames)
            {
                game.OnGameCompleted -= OnGameFinished;
                game.OnGameCompleted += OnGameFinished;
            }
        }

        private void OnEnable()
        {
            IAPManager.OnPromptGooglePlayDialog -= HandleGooglePlayPrompt;
            IAPManager.OnPromptGooglePlayDialog += HandleGooglePlayPrompt;

            AdsManager.OnShowVideoAdSimulator -= HandleVideoAdSimulatorPrompt;
            AdsManager.OnShowVideoAdSimulator += HandleVideoAdSimulatorPrompt;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            IAPManager.OnPromptGooglePlayDialog -= HandleGooglePlayPrompt;
            AdsManager.OnShowVideoAdSimulator -= HandleVideoAdSimulatorPrompt;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateUI();
        }

        private void Start()
        {
            ShowTab(0);
            CheckDailyReset();
            UpdateUI();
        }

        private void Update()
        {
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0)
                {
                    _cooldownTimer = 0;
                    UpdateCooldownUI();
                }
                else
                {
                    UpdateCooldownUI();
                }
            }
        }

        // â”€â”€â”€ Kamera ve Sahne Kurtarma â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void EnsureCameraAndReturnCanvas()
        {
            if (miniGameCamera == null)
            {
                var camObj = GameObject.Find("MiniGameCamera");
                if (camObj != null) miniGameCamera = camObj.GetComponent<Camera>();
                else
                {
                    var newCamObj = new GameObject("MiniGameCamera", typeof(Camera));
                    newCamObj.transform.SetParent(transform, false);
                    miniGameCamera = newCamObj.GetComponent<Camera>();
                    miniGameCamera.clearFlags = CameraClearFlags.SolidColor;
                    miniGameCamera.backgroundColor = new Color(0.04f, 0.03f, 0.08f, 1f);
                    miniGameCamera.cullingMask = 0;
                    miniGameCamera.depth = -1;
                }
            }

            if (miniGameCamera != null && !miniGameCamera.gameObject.activeSelf)
            {
                miniGameCamera.gameObject.SetActive(true);
            }

            if (returnCanvas == null)
            {
                var lobi = GameObject.Find("LobiSahnesi");
                if (lobi != null) returnCanvas = lobi;
            }
        }

        // â”€â”€â”€ Sekme Yonetimi â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void ShowTab(int index)
        {
            _currentTabIndex = index;

            if (miniGameSelectionPanel != null) miniGameSelectionPanel.SetActive(index == 0);
            if (potionPassPanel != null) potionPassPanel.SetActive(index == 1);
            if (shopPanel != null) shopPanel.SetActive(index == 2);
            if (lootBoxPanel != null) lootBoxPanel.SetActive(index == 3);

            UpdateTabButtonVisuals(tabMiniGames, index == 0);
            UpdateTabButtonVisuals(tabPotionPass, index == 1);
            UpdateTabButtonVisuals(tabShop, index == 2);
            UpdateTabButtonVisuals(tabLootBox, index == 3);

            if (index == 1) RefreshPotionPassUI();
            else if (index == 2) RefreshShopUI();
            else if (index == 3) RefreshLootBoxUI();
        }

        private void UpdateTabButtonVisuals(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = isActive ? TabActiveColor : TabInactiveColor;

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = isActive ? new Color(1f, 0.9f, 0.4f) : new Color(0.8f, 0.8f, 0.85f);
        }

        // â”€â”€â”€ Potion Pass Arayuzu (Dinamik) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void RefreshPotionPassUI()
        {
            var ppm = PotionPassManager.Instance;
            if (ppm == null || ppm.passData == null) return;

            if (passSeasonText != null)
                passSeasonText.text = ppm.passData.seasonName;

            if (passXpBarFill != null)
                passXpBarFill.fillAmount = ppm.GetTierProgress();

            if (passXpText != null)
                passXpText.text = $"{LocalizationManager.Get("lvl_prefix")} {ppm.CurrentTier + 1}  -  {ppm.CurrentXP} / {ppm.passData.xpPerTier} XP";

            if (passUpgradeButton != null)
            {
                var btnTxt = passUpgradeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (ppm.IsPremium)
                {
                    passUpgradeButton.interactable = false;
                    if (btnTxt != null) btnTxt.text = $"[*] {LocalizationManager.Get("pass_premium_active")}";
                }
                else
                {
                    passUpgradeButton.interactable = true;
                    if (btnTxt != null) btnTxt.text = $"[*] {LocalizationManager.Get("pass_upgrade_ticket")} ({ppm.passData.premiumPriceKadimPara} {LocalizationManager.Get("kadim_suffix")})";
                }
            }

            if (passTierContainer != null)
            {
                int totalTiers = Mathf.Min(passTierContainer.childCount, ppm.passData.totalTiers);
                for (int i = 0; i < totalTiers; i++)
                {
                    int tier = i + 1;
                    Transform slotTr = passTierContainer.GetChild(i);
                    UpdateTierSlotUI(slotTr, tier);
                }
            }
        }

        private void UpdateTierSlotUI(Transform slotTr, int tier)
        {
            var ppm = PotionPassManager.Instance;
            if (ppm == null || slotTr == null) return;

            bool isReached = tier <= ppm.CurrentTier;

            // Ucretsiz Yol Butonu
            Transform freeBtnTr = slotTr.Find("FreeRewardBox/ClaimBtn");
            if (freeBtnTr != null)
            {
                Button freeBtn = freeBtnTr.GetComponent<Button>();
                TextMeshProUGUI freeTxt = freeBtnTr.GetComponentInChildren<TextMeshProUGUI>();

                bool canClaimFree = ppm.CanClaimReward(tier, false);
                bool isClaimedFree = isReached && !canClaimFree;

                if (freeBtn != null)
                {
                    freeBtn.interactable = canClaimFree;
                    freeBtn.onClick.RemoveAllListeners();
                    freeBtn.onClick.AddListener(() => ClaimPotionPassReward(tier, false));
                }

                if (freeTxt != null)
                {
                    if (isClaimedFree) { freeTxt.text = LocalizationManager.Get("pass_claimed"); freeTxt.color = Color.gray; }
                    else if (canClaimFree) { freeTxt.text = LocalizationManager.Get("pass_claim"); freeTxt.color = Color.white; }
                    else { freeTxt.text = LocalizationManager.Get("pass_locked"); freeTxt.color = new Color(0.6f, 0.6f, 0.6f); }
                }
            }

            // Premium Yol Butonu
            Transform premBtnTr = slotTr.Find("PremiumRewardBox/ClaimBtn");
            if (premBtnTr != null)
            {
                Button premBtn = premBtnTr.GetComponent<Button>();
                TextMeshProUGUI premTxt = premBtnTr.GetComponentInChildren<TextMeshProUGUI>();

                bool canClaimPrem = ppm.CanClaimReward(tier, true);
                bool isClaimedPrem = isReached && ppm.IsPremium && !canClaimPrem;

                if (premBtn != null)
                {
                    premBtn.interactable = canClaimPrem;
                    premBtn.onClick.RemoveAllListeners();
                    premBtn.onClick.AddListener(() => ClaimPotionPassReward(tier, true));
                }

                if (premTxt != null)
                {
                    if (isClaimedPrem) { premTxt.text = LocalizationManager.Get("pass_claimed"); premTxt.color = Color.gray; }
                    else if (canClaimPrem) { premTxt.text = LocalizationManager.Get("pass_claim"); premTxt.color = Color.white; }
                    else { premTxt.text = ppm.IsPremium ? LocalizationManager.Get("pass_locked") : LocalizationManager.Get("pass_premium"); premTxt.color = new Color(0.9f, 0.7f, 0.2f); }
                }
            }
        }

        public void ClaimPotionPassReward(int tier, bool isPremium)
        {
            if (PotionPassManager.Instance != null)
            {
                PotionPassManager.Instance.ClaimReward(tier, isPremium);
                RefreshPotionPassUI();
            }
        }

        public void UpgradePotionPassPremium()
        {
            var ppm = PotionPassManager.Instance;
            if (ppm == null || ppm.IsPremium) return;

            var gm = GameManager.Instance;
            int price = (ppm.passData != null) ? ppm.passData.premiumPriceKadimPara : 50;

            if (gm != null && gm.SpendKadimPara(price))
            {
                ppm.UpgradeToPremium();
                RefreshPotionPassUI();
                Debug.Log($"[PotionPass] {price} Kadim Para ile Premium Bilet acildi!");
            }
            else
            {
                BuyIAP(IAPManager.POTION_PASS_PREMIUM);
            }
        }

        // â”€â”€â”€ Magaza (Shop) Arayuzu (Dinamik) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void RefreshShopUI()
        {
            bool adsRemoved = AdsManager.Instance != null && AdsManager.Instance.AdsRemoved;
            if (buyRemoveAdsBtn != null)
            {
                if (adsRemoved)
                {
                    buyRemoveAdsBtn.interactable = false;
                    var txt = buyRemoveAdsBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Alindi";
                }
            }
        }

        public void OnShopProductClicked(ShopProductData prod)
        {
            if (prod == null) return;

            var gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();

            switch (prod.priceType)
            {
                case ShopPriceType.RealMoney:
                    BuyIAP(prod.id);
                    break;

                case ShopPriceType.RewardedAd:
                    if (AdsManager.Instance != null)
                    {
                        AdsManager.Instance.ShowRewardedAd(() =>
                        {
                            IAPManager.Instance.HandlePurchaseRewards(prod.id);
                            ShowRewardPopup("REKLAM ODULU", $"{prod.displayName} basariyla alindi!");
                        });
                    }
                    break;

                case ShopPriceType.Gold:
                    if (gm != null)
                    {
                        if (gm.SpendGold(prod.ingamePrice))
                        {
                            IAPManager.Instance.HandlePurchaseRewards(prod.id);
                            ShowRewardPopup("SATIN ALINDI", $"{prod.displayName} {prod.ingamePrice} Altin karsiliginda alindi!");
                        }
                        else
                        {
                            ShowRewardPopup("UYARI", "Yetersiz Altin!");
                        }
                    }
                    break;

                case ShopPriceType.KadimPara:
                    if (gm != null)
                    {
                        if (gm.SpendKadimPara(prod.ingamePrice))
                        {
                            IAPManager.Instance.HandlePurchaseRewards(prod.id);
                            ShowRewardPopup("SATIN ALINDI", $"{prod.displayName} {prod.ingamePrice} Kadim Para karsiliginda alindi!");
                        }
                        else
                        {
                            ShowRewardPopup("UYARI", "Yetersiz Kadim Para!");
                        }
                    }
                    break;
            }
        }

        public void BuyIAP(string productId)
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.BuyProduct(productId);
                RefreshShopUI();
            }
            else
            {
                Debug.LogError("IAPManager bulunamadi!");
            }
        }

        public void WatchAdForGold()
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(() =>
                {
                    var gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
                    var config = LiveMonetizationConfig.Instance;
                    int goldReward = config != null ? config.rewardedGoldReward : 250;

                    if (gm != null) gm.AddGold(goldReward);
                    ShowRewardPopup("REKLAM ODULU", $"* +{goldReward} Altin Hesabiniza Eklendi!");
                },
                () =>
                {
                    Debug.LogWarning("Reklam yuklenemedi veya atlandi.");
                });
            }
        }

        public void WatchAdForBonusPlay()
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(() =>
                {
                    GrantBonusPlay();
                    ShowRewardPopup("REKLAM ODULU", "* +1 Mini Oyun Oynama Hakki Kazandiniz!");
                },
                () =>
                {
                    Debug.LogWarning("Reklam yuklenemedi veya atlandi.");
                });
            }
        }

        // â”€â”€â”€ Google Play Satin Alma Simulator Modali â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void HandleGooglePlayPrompt(string productId, string title, string price, Action onConfirm, Action onCancel)
        {
            _pendingGpConfirm = onConfirm;
            _pendingGpCancel = onCancel;

            if (googlePlayModalPanel != null)
            {
                if (gpItemTitleText != null) gpItemTitleText.text = title;
                if (gpItemPriceText != null) gpItemPriceText.text = price;
                googlePlayModalPanel.SetActive(true);
            }
            else
            {
                onConfirm?.Invoke();
            }
        }

        public void ConfirmGooglePlayPurchase()
        {
            if (googlePlayModalPanel != null) googlePlayModalPanel.SetActive(false);
            _pendingGpConfirm?.Invoke();
            _pendingGpConfirm = null;
            _pendingGpCancel = null;
            RefreshShopUI();
        }

        public void CancelGooglePlayPurchase()
        {
            if (googlePlayModalPanel != null) googlePlayModalPanel.SetActive(false);
            _pendingGpCancel?.Invoke();
            _pendingGpConfirm = null;
            _pendingGpCancel = null;
        }

        // â”€â”€â”€ Video Reklam Simulator Modali â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void HandleVideoAdSimulatorPrompt(string title, int duration, Action onComplete, Action onCancel)
        {
            _pendingAdComplete = onComplete;
            _pendingAdCancel = onCancel;

            if (videoAdSimulatorPanel != null)
            {
                videoAdSimulatorPanel.SetActive(true);
                if (_adCountdownRoutine != null) StopCoroutine(_adCountdownRoutine);
                _adCountdownRoutine = StartCoroutine(RunVideoAdCountdown(duration));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator RunVideoAdCountdown(int duration)
        {
            if (adCloseRewardBtn != null)
            {
                adCloseRewardBtn.interactable = false;
                var btnTxt = adCloseRewardBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = LocalizationManager.Get("ad_playing");
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float remaining = Mathf.Max(0, duration - elapsed);

                if (adCountdownText != null)
                    adCountdownText.text = string.Format(LocalizationManager.Get("ad_reward_countdown"), Mathf.CeilToInt(remaining));

                if (adProgressBar != null)
                    adProgressBar.fillAmount = Mathf.Clamp01(elapsed / duration);

                yield return null;
            }

            if (adCountdownText != null)
                adCountdownText.text = LocalizationManager.Get("ad_completed");

            if (adCloseRewardBtn != null)
            {
                adCloseRewardBtn.interactable = true;
                var btnTxt = adCloseRewardBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = LocalizationManager.Get("ad_claim_close");
            }
        }

        public void CompleteVideoAd()
        {
            if (videoAdSimulatorPanel != null) videoAdSimulatorPanel.SetActive(false);
            _pendingAdComplete?.Invoke();
            _pendingAdComplete = null;
            _pendingAdCancel = null;
            UpdateUI();
        }

        public void CancelVideoAd()
        {
            if (_adCountdownRoutine != null) StopCoroutine(_adCountdownRoutine);
            if (videoAdSimulatorPanel != null) videoAdSimulatorPanel.SetActive(false);
            _pendingAdCancel?.Invoke();
            _pendingAdComplete = null;
            _pendingAdCancel = null;
        }

        // â”€â”€â”€ Kasa (Loot Box Vault) Arayuzu â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void RefreshLootBoxUI()
        {
            int bronze = LootBoxSystem.GetLootBoxCount(LootBoxTier.Bronze);
            int silver = LootBoxSystem.GetLootBoxCount(LootBoxTier.Silver);
            int gold = LootBoxSystem.GetLootBoxCount(LootBoxTier.Gold);

            if (bronzeCountText != null) bronzeCountText.text = string.Format(LocalizationManager.Get("lootbox_owned_format"), bronze);
            if (silverCountText != null) silverCountText.text = string.Format(LocalizationManager.Get("lootbox_owned_format"), silver);
            if (goldCountText != null) goldCountText.text = string.Format(LocalizationManager.Get("lootbox_owned_format"), gold);

            if (openBronzeBtn != null) openBronzeBtn.interactable = bronze > 0;
            if (openSilverBtn != null) openSilverBtn.interactable = silver > 0;
            if (openGoldBtn != null) openGoldBtn.interactable = gold > 0;
        }

        public void OpenLootBox(LootBoxTier tier)
        {
            if (LootBoxSystem.GetLootBoxCount(tier) <= 0)
            {
                Debug.LogWarning($"{tier} kasasi bulunmuyor!");
                return;
            }

            var rewards = LootBoxSystem.OpenLootBox(tier);
            ProcessLootRewards(tier.ToString(), rewards);
            RefreshLootBoxUI();
        }

        public void BuyAndOpenLootBox(LootBoxTier tier, int price, bool useKadimPara)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool success = false;
            if (useKadimPara)
            {
                if (gm.SpendKadimPara(price)) success = true;
                else ShowRewardPopup("UYARI", "Yetersiz Kadim Para!");
            }
            else
            {
                if (gm.SpendGold(price)) success = true;
                else ShowRewardPopup("UYARI", "Yetersiz Altin!");
            }

            if (success)
            {
                LootBoxSystem.AddLootBox(tier);
                OpenLootBox(tier);
            }
        }

        private void ProcessLootRewards(string tierName, List<LootReward> rewards)
        {
            string summary = "";
            var gm = GameManager.Instance;

            foreach (var r in rewards)
            {
                if (r.Type == RewardType.Gold)
                {
                    if (gm != null) gm.AddGold(r.amount);
                    summary += $"* +{r.amount} Altin\n";
                }
                else if (r.Type == RewardType.KadimPara)
                {
                    if (gm != null) gm.AddKadimPara(r.amount);
                    summary += $"* +{r.amount} Kadim Para\n";
                }
                else if (r.Type == RewardType.Item)
                {
                    summary += "* 1x Nadir Simya Esyasi\n";
                }
            }

            ShowRewardPopup($"{tierName.ToUpper()} KASA ACILDI!", summary);
        }

        private void ShowRewardPopup(string title, string content)
        {
            if (lootRewardPopupPanel != null)
            {
                if (lootRewardTitleText != null) lootRewardTitleText.text = title;
                if (lootRewardDescText != null) lootRewardDescText.text = content;
                lootRewardPopupPanel.SetActive(true);
            }
        }

        public void CloseRewardPopup()
        {
            if (lootRewardPopupPanel != null)
                lootRewardPopupPanel.SetActive(false);
        }

        // â”€â”€â”€ Mini Oyun Baslatma â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void TryStartGame(string gameName)
        {
            var config = GameBalanceConfig.Instance;
            int maxPlays = config != null ? config.dailyFreeGamePlays : 3;

            if (_dailyPlaysUsed >= maxPlays)
            {
                Debug.LogWarning("[MiniGame] Gunluk oynama hakkiniz doldu! Reklam izleyerek +1 hak kazanabilirsiniz.");
                ShowRewardPopup("HAK DOLDU", "Gunluk oynama hakkiniz doldu!\nReklam izleyerek +1 hak kazanabilirsiniz.");
                return;
            }

            if (_cooldownTimer > 0)
            {
                int mins = Mathf.CeilToInt(_cooldownTimer / 60f);
                Debug.LogWarning($"[MiniGame] Bekleme suresi aktif! {mins} dk bekleyin veya reklam izleyin.");
                return;
            }

            MiniGameBase game = FindGame(gameName);
            if (game != null)
            {
                game.gamePanel?.SetActive(true);
                miniGameSelectionPanel?.SetActive(false);
                game.StartGame();
                Debug.Log($"[MiniGame] {game.GameName} baslatildi!");
            }
        }

        public void GrantBonusPlay()
        {
            _dailyPlaysUsed = Mathf.Max(0, _dailyPlaysUsed - 1);
            _cooldownTimer = 0f;
            UpdateUI();
            Debug.Log("[MiniGame] Reklam izlenerek +1 oynama hakki kazanildi!");
        }

        private void OnGameFinished(int score)
        {
            _dailyPlaysUsed++;

            var config = GameBalanceConfig.Instance;
            float cooldown = config != null ? config.miniGameCooldownMinutes * 60f : 300f;
            _cooldownTimer = cooldown;

            foreach (var game in _allGames)
            {
                if (game.IsPlaying || game.gamePanel?.activeSelf == true)
                {
                    string name = game.GameName;
                    if (!_highScores.ContainsKey(name) || score > _highScores[name])
                    {
                        _highScores[name] = score;
                    }

                    int passXP = Mathf.Max(5, score / 4);
                    if (PotionPassManager.Instance != null)
                    {
                        PotionPassManager.Instance.AddXP(passXP);
                    }

                    OnMiniGameCompleted?.Invoke(name, score);
                    game.gamePanel?.SetActive(false);
                    break;
                }
            }

            miniGameSelectionPanel?.SetActive(true);
            UpdateUI();
        }

        private MiniGameBase FindGame(string gameName)
        {
            foreach (var game in _allGames)
            {
                if (game.GameName.Contains(gameName) ||
                    game.GetType().Name.Contains(gameName))
                {
                    return game;
                }
            }
            Debug.LogWarning($"[MiniGame] '{gameName}' adli oyun bulunamadi!");
            return null;
        }

        // â”€â”€â”€ Gunluk Sifirlama â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void CheckDailyReset()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (_lastPlayDate != today)
            {
                _lastPlayDate = today;
                _dailyPlaysUsed = 0;
                _cooldownTimer = 0;
            }
        }

        // â”€â”€â”€ UI Guncelleme â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void UpdateUI()
        {
            var config = GameBalanceConfig.Instance;
            int maxPlays = config != null ? config.dailyFreeGamePlays : 3;
            int remaining = Mathf.Max(0, maxPlays - _dailyPlaysUsed);

            if (remainingPlaysText != null)
                remainingPlaysText.text = string.Format(LocalizationManager.Get("minigame_remaining_plays"), remaining, maxPlays);

            UpdateCooldownUI();

            if (highScoreText != null)
            {
                string scores = LocalizationManager.Get("minigame_high_scores");
                if (_highScores.Count == 0) scores += LocalizationManager.Get("minigame_none_yet");
                else
                {
                    foreach (var kvp in _highScores)
                    {
                        scores += $"{kvp.Key}: {kvp.Value}  ";
                    }
                }
                highScoreText.text = scores;
            }
        }

        private void UpdateCooldownUI()
        {
            if (cooldownText != null)
            {
                if (_cooldownTimer > 0)
                {
                    int mins = Mathf.FloorToInt(_cooldownTimer / 60f);
                    int secs = Mathf.FloorToInt(_cooldownTimer % 60f);
                    cooldownText.text = string.Format(LocalizationManager.Get("minigame_cooldown"), mins, secs);
                    cooldownText.gameObject.SetActive(true);
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
        }

        // â”€â”€â”€ Alan Acma / Kapama â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void OpenArea()
        {
            EnsureCameraAndReturnCanvas();

            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                ShowTab(0);
                CheckDailyReset();
                UpdateUI();
            }
            if (miniGameCamera != null)
            {
                miniGameCamera.gameObject.SetActive(true);
            }
        }

        public void CloseArea()
        {
            if (miniGameCamera != null)
                miniGameCamera.gameObject.SetActive(false);

            if (mainPanel != null)
                mainPanel.SetActive(false);

            if (returnCanvas != null)
            {
                returnCanvas.SetActive(true);
            }
            else
            {
                foreach (var rootGO in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (rootGO.name == "LobiSahnesi")
                    {
                        rootGO.SetActive(true);
                        break;
                    }
                }
            }

            if (TopDropdownMenu.Instance != null)
                TopDropdownMenu.Instance.SetContext(false);

            gameObject.SetActive(false);
        }

        private void HookButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null || action == null) return;
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }

#if UNITY_EDITOR
        // =========================================================================
        //  EDITOR: OTOMATIK HIYERARSI OLUSTURUCU (CANLIYA HAZIR DATA-DRIVEN SISTEM)
        // =========================================================================

        [UnityEditor.MenuItem("GameObject/UI/PotionTavern - Buyu Arenasi (Mini Oyun Alani)", false, 11)]
        public static void CreateMiniGameArea()
        {
            // Varsa eski MiniGameCanvas'i temizle
            GameObject oldCanvas = GameObject.Find("MiniGameCanvas");
            if (oldCanvas != null)
            {
                DestroyImmediate(oldCanvas);
            }

            // Sprite Varliklarini Yukle
            Sprite arenaBgSprite = Resources.Load<Sprite>("MiniGames/minigame_bg_arena");
            Sprite cardBackSprite = Resources.Load<Sprite>("MiniGames/rune_card_back");
            Sprite cauldronSprite = Resources.Load<Sprite>("MiniGames/cauldron_sprite");
            Sprite altarSprite = Resources.Load<Sprite>("MiniGames/crystal_altar_circle");
            Sprite heartSprite = Resources.Load<Sprite>("MiniGames/Potions/heart");

            // Dinamik Veri Kaynaklarini Yukle
            var shopCatalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            if (shopCatalog == null)
            {
                shopCatalog = ScriptableObject.CreateInstance<ShopCatalogData>();
                shopCatalog.PopulateDefaults();
            }

            var passData = Resources.Load<PotionPassData>("PotionPassData");
            if (passData == null)
            {
                passData = ScriptableObject.CreateInstance<PotionPassData>();
                passData.PopulateDefaults(10);
            }

            var liveConfig = Resources.Load<LiveMonetizationConfig>("LiveMonetizationConfig");

            // â”€â”€ Kendi Bagimsiz Canvas'ini Olustur â”€â”€
            GameObject canvasObj = new GameObject("MiniGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // â”€â”€ BAGIMSIZ KAMERA (Display 1 No cameras rendering FIX) â”€â”€
            GameObject camObj = new GameObject("MiniGameCamera", typeof(Camera));
            camObj.transform.SetParent(canvasObj.transform, false);
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.08f, 1f);
            cam.cullingMask = 0;
            cam.depth = -1;

            // â”€â”€ Ana Arka Plan â”€â”€
            GameObject root = CreatePanel(canvas.transform, "MiniGameArea", Color.white);
            SetFullStretch(root);
            Image rootImg = root.GetComponent<Image>();
            if (arenaBgSprite != null)
            {
                rootImg.sprite = arenaBgSprite;
                rootImg.color = new Color(0.90f, 0.88f, 0.95f, 1f);
            }
            else
            {
                rootImg.color = new Color(0.08f, 0.06f, 0.14f, 0.98f);
            }

            // Koyu Karartma Katmani
            GameObject vignette = CreatePanel(root.transform, "DarkOverlay", new Color(0.04f, 0.03f, 0.08f, 0.65f));
            SetFullStretch(vignette);

            // â”€â”€ UST BASLIK BARI (HeaderBar) â”€â”€
            var headerBar = CreatePanel(root.transform, "HeaderBar", new Color(0.12f, 0.08f, 0.18f, 0.96f));
            var headerRT = headerBar.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.02f, 0.92f);
            headerRT.anchorMax = new Vector2(0.98f, 0.99f);
            headerRT.offsetMin = Vector2.zero;
            headerRT.offsetMax = Vector2.zero;

            // Sol Baslik
            var titleObj = CreateText(headerBar.transform, "Title", "* SIMYA & BUYU ARENASI *", 24, new Color(1f, 0.88f, 0.35f), TextAlignmentOptions.Left, true);
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.02f, 0);
            titleRT.anchorMax = new Vector2(0.40f, 1);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // Sag Profil / Hesap Alani
            GameObject profileObj = CreatePanel(headerBar.transform, "ProfileWidget", new Color(0.20f, 0.15f, 0.30f, 0.85f));
            var profRT = profileObj.GetComponent<RectTransform>();
            profRT.anchorMin = new Vector2(0.66f, 0.12f);
            profRT.anchorMax = new Vector2(0.93f, 0.88f);
            profRT.offsetMin = Vector2.zero;
            profRT.offsetMax = Vector2.zero;

            var profTxt = CreateText(profileObj.transform, "PlayerNameText", "Simyaci #8472", 15, Color.white, TextAlignmentOptions.Left, true);
            var profTxtRT = profTxt.GetComponent<RectTransform>();
            profTxtRT.anchorMin = new Vector2(0.04f, 0);
            profTxtRT.anchorMax = new Vector2(0.55f, 1);
            profTxtRT.offsetMin = Vector2.zero;
            profTxtRT.offsetMax = Vector2.zero;

            var badgeTxt = CreateText(profileObj.transform, "StatusBadge", "[Misafir]", 14, new Color(1f, 0.78f, 0.28f), TextAlignmentOptions.Center, true);
            var badgeRT = badgeTxt.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.56f, 0);
            badgeRT.anchorMax = new Vector2(0.76f, 1);
            badgeRT.offsetMin = Vector2.zero;
            badgeRT.offsetMax = Vector2.zero;

            var accountBtn = CreateButton(profileObj.transform, "AccountButton", "HESAP", new Color(0.40f, 0.24f, 0.62f));
            var accBtnRT = accountBtn.GetComponent<RectTransform>();
            accBtnRT.anchorMin = new Vector2(0.78f, 0.10f);
            accBtnRT.anchorMax = new Vector2(0.98f, 0.90f);
            accBtnRT.offsetMin = Vector2.zero;
            accBtnRT.offsetMax = Vector2.zero;

            // Kapat Butonu (Sag Ust)
            var closeBtn = CreateButton(headerBar.transform, "CloseButton", "X", new Color(0.75f, 0.18f, 0.22f));
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(0.945f, 0.12f);
            closeBtnRT.anchorMax = new Vector2(0.99f, 0.88f);
            closeBtnRT.offsetMin = Vector2.zero;
            closeBtnRT.offsetMax = Vector2.zero;

            // â”€â”€ SEKME CUBUGU (TabBar) â”€â”€
            GameObject tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(root.transform, false);
            var tabBarRT = tabBar.GetComponent<RectTransform>();
            tabBarRT.anchorMin = new Vector2(0.02f, 0.85f);
            tabBarRT.anchorMax = new Vector2(0.98f, 0.91f);
            tabBarRT.offsetMin = Vector2.zero;
            tabBarRT.offsetMax = Vector2.zero;

            var tabHLG = tabBar.GetComponent<HorizontalLayoutGroup>();
            tabHLG.spacing = 14;
            tabHLG.childControlWidth = true;
            tabHLG.childControlHeight = true;
            tabHLG.childForceExpandWidth = true;
            tabHLG.childForceExpandHeight = true;

            var tabMiniGamesBtn = CreateTabButton(tabBar.transform, "TabMiniGames", "[+] MINI OYUNLAR", new Color(0.38f, 0.20f, 0.60f));
            var tabPassBtn = CreateTabButton(tabBar.transform, "TabPotionPass", "[*] POTION PASS", new Color(0.14f, 0.10f, 0.22f));
            var tabShopBtn = CreateTabButton(tabBar.transform, "TabShop", "[$$] MAGAZA", new Color(0.14f, 0.10f, 0.22f));
            var tabLootBtn = CreateTabButton(tabBar.transform, "TabLootBox", "[#] KASALAR", new Color(0.14f, 0.10f, 0.22f));

            // =========================================================================
            //  TAB 0: MINI OYUN SECIM PANELI
            // =========================================================================
            GameObject selectionPanel = CreatePanel(root.transform, "MiniGameSelectionPanel", new Color(0.08f, 0.06f, 0.14f, 0.85f));
            SetContentStretch(selectionPanel);

            var selHLG = selectionPanel.AddComponent<HorizontalLayoutGroup>();
            selHLG.spacing = 24;
            selHLG.padding = new RectOffset(24, 24, 18, 18);
            selHLG.childAlignment = TextAnchor.MiddleCenter;
            selHLG.childControlWidth = true;
            selHLG.childControlHeight = true;
            selHLG.childForceExpandWidth = true;
            selHLG.childForceExpandHeight = true;

            var runeCard = CreateThemedGameCard(selectionPanel.transform, "RuneMatchCard", "SIMYA RUNLERI",
                "Kadim run kartlarini cevir, iksir ciftlerini bul!\nSeri kombolarla sureyi uzat ve rekor kir.",
                new Color(0.35f, 0.18f, 0.58f), cardBackSprite, "+Altin  |  +Pass XP  |  +Kasa");

            var potionCard = CreateThemedGameCard(selectionPanel.transform, "PotionCatchCard", "KAZAN IKSIR YAGMURU",
                "Gokten yagan iksirleri kazana topla!\nZehir ve bombalardan kac, Altin Caga ulas.",
                new Color(0.16f, 0.48f, 0.28f), cauldronSprite, "+Altin  |  +Kadim Para  |  +Malzeme");

            var crystalCard = CreateThemedGameCard(selectionPanel.transform, "CrystalSortCard", "KRISTAL ARINDIRMA",
                "5 Element kristalini simya cemberinde arindir!\nRefleksini test et, Simya Coskusuna ulas.",
                new Color(0.58f, 0.30f, 0.14f), altarSprite, "+Altin  |  +Buyuk Skor  |  +Fever");

            // =========================================================================
            //  TAB 1: POTION PASS PANELI (DINAMIK DATA-DRIVEN)
            // =========================================================================
            GameObject passPanel = CreatePanel(root.transform, "PotionPassPanel", new Color(0.08f, 0.06f, 0.14f, 0.95f));
            passPanel.SetActive(false);
            SetContentStretch(passPanel);

            var passHeader = CreatePanel(passPanel.transform, "PassHeader", new Color(0.14f, 0.11f, 0.24f, 0.95f));
            var passHeaderRT = passHeader.GetComponent<RectTransform>();
            passHeaderRT.anchorMin = new Vector2(0.02f, 0.85f);
            passHeaderRT.anchorMax = new Vector2(0.98f, 0.98f);
            passHeaderRT.offsetMin = Vector2.zero;
            passHeaderRT.offsetMax = Vector2.zero;

            string seasonTitleStr = passData != null ? passData.seasonName.ToUpper() : "SEZON 1: SIMYACININ YOLU";
            var seasonTxt = CreateText(passHeader.transform, "SeasonTitle", seasonTitleStr, 22, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Left, true);
            var seasonRT = seasonTxt.GetComponent<RectTransform>();
            seasonRT.anchorMin = new Vector2(0.02f, 0.52f);
            seasonRT.anchorMax = new Vector2(0.50f, 0.95f);
            seasonRT.offsetMin = Vector2.zero;
            seasonRT.offsetMax = Vector2.zero;

            var xpBg = CreatePanel(passHeader.transform, "XPBar_BG", new Color(0.18f, 0.14f, 0.28f));
            var xpBgRT = xpBg.GetComponent<RectTransform>();
            xpBgRT.anchorMin = new Vector2(0.02f, 0.10f);
            xpBgRT.anchorMax = new Vector2(0.55f, 0.48f);
            xpBgRT.offsetMin = Vector2.zero;
            xpBgRT.offsetMax = Vector2.zero;

            var xpFill = CreatePanel(xpBg.transform, "XPBar_Fill", new Color(0f, 0.90f, 0.45f));
            var xpFillRT = xpFill.GetComponent<RectTransform>();
            xpFillRT.anchorMin = Vector2.zero;
            xpFillRT.anchorMax = new Vector2(0.5f, 1f);
            xpFillRT.offsetMin = Vector2.zero;
            xpFillRT.offsetMax = Vector2.zero;
            var fillImg = xpFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.35f;

            var xpTxt = CreateText(xpBg.transform, "XPText", "Seviye 1  -  70 / 200 XP", 15, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(xpTxt);

            int premCost = passData != null ? passData.premiumPriceKadimPara : 50;
            var upgradeBtn = CreateButton(passHeader.transform, "UpgradePremiumBtn", $"[*] BILETI YUKSELT ({premCost} Kadim Para)", new Color(0.80f, 0.52f, 0.12f));
            var upRT = upgradeBtn.GetComponent<RectTransform>();
            upRT.anchorMin = new Vector2(0.60f, 0.12f);
            upRT.anchorMax = new Vector2(0.98f, 0.88f);
            upRT.offsetMin = Vector2.zero;
            upRT.offsetMax = Vector2.zero;

            GameObject tierScroll = new GameObject("TierScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            tierScroll.transform.SetParent(passPanel.transform, false);
            var tsRT = tierScroll.GetComponent<RectTransform>();
            tsRT.anchorMin = new Vector2(0.02f, 0.02f);
            tsRT.anchorMax = new Vector2(0.98f, 0.83f);
            tsRT.offsetMin = Vector2.zero;
            tsRT.offsetMax = Vector2.zero;
            tierScroll.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f, 0.50f);

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(tierScroll.transform, false);
            var contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 0);
            contentRT.anchorMax = new Vector2(0, 1);
            contentRT.pivot = new Vector2(0, 0.5f);
            contentRT.sizeDelta = new Vector2(2600, 0);

            var contentHLG = contentObj.GetComponent<HorizontalLayoutGroup>();
            contentHLG.spacing = 16;
            contentHLG.padding = new RectOffset(16, 16, 12, 12);
            contentHLG.childControlWidth = true;
            contentHLG.childControlHeight = true;
            contentHLG.childForceExpandHeight = true;
            contentHLG.childForceExpandWidth = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var srect = tierScroll.GetComponent<ScrollRect>();
            srect.content = contentRT;
            srect.horizontal = true;
            srect.vertical = false;

            int totalTiersCount = passData != null ? passData.totalTiers : 10;
            for (int t = 1; t <= totalTiersCount; t++)
            {
                CreateTierCardFromData(contentObj.transform, passData, t);
            }

            // =========================================================================
            //  TAB 2: MAGAZA (SHOP) PANELI (DINAMIK DATA-DRIVEN)
            // =========================================================================
            GameObject shopPnl = CreatePanel(root.transform, "ShopPanel", new Color(0.08f, 0.06f, 0.14f, 0.95f));
            shopPnl.SetActive(false);
            SetContentStretch(shopPnl);

            var shopTitle = CreateText(shopPnl.transform, "Title", "TAVERNA MAGAZASI & KADIM HAZINE", 22, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var sTitleRT = shopTitle.GetComponent<RectTransform>();
            sTitleRT.anchorMin = new Vector2(0.05f, 0.92f);
            sTitleRT.anchorMax = new Vector2(0.95f, 0.98f);
            sTitleRT.offsetMin = Vector2.zero;
            sTitleRT.offsetMax = Vector2.zero;

            // 1. Satir: Kadim Para Paketleri
            GameObject paraContainer = new GameObject("ParaPackages", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            paraContainer.transform.SetParent(shopPnl.transform, false);
            var pcRT = paraContainer.GetComponent<RectTransform>();
            pcRT.anchorMin = new Vector2(0.02f, 0.49f);
            pcRT.anchorMax = new Vector2(0.98f, 0.90f);
            pcRT.offsetMin = Vector2.zero;
            pcRT.offsetMax = Vector2.zero;

            var pcHLG = paraContainer.GetComponent<HorizontalLayoutGroup>();
            pcHLG.spacing = 20;
            pcHLG.padding = new RectOffset(16, 16, 8, 8);
            pcHLG.childControlWidth = true;
            pcHLG.childControlHeight = true;
            pcHLG.childForceExpandWidth = true;
            pcHLG.childForceExpandHeight = true;

            // 2. Satir: Ozel Firsatlar & Reklamlar
            GameObject offersContainer = new GameObject("SpecialOffers", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            offersContainer.transform.SetParent(shopPnl.transform, false);
            var ocRT = offersContainer.GetComponent<RectTransform>();
            ocRT.anchorMin = new Vector2(0.02f, 0.03f);
            ocRT.anchorMax = new Vector2(0.98f, 0.45f);
            ocRT.offsetMin = Vector2.zero;
            ocRT.offsetMax = Vector2.zero;

            var ocHLG = offersContainer.GetComponent<HorizontalLayoutGroup>();
            ocHLG.spacing = 20;
            ocHLG.padding = new RectOffset(16, 16, 8, 8);
            ocHLG.childControlWidth = true;
            ocHLG.childControlHeight = true;
            ocHLG.childForceExpandWidth = true;
            ocHLG.childForceExpandHeight = true;

            Button remAdsBtnRef = null;

            // Dinamik Urun Uretimi
            if (shopCatalog != null && shopCatalog.products.Count > 0)
            {
                foreach (var prod in shopCatalog.products)
                {
                    Transform targetParent = (prod.category == ShopCategory.KadimPara) ? paraContainer.transform : offersContainer.transform;
                    var btnObj = CreateDynamicShopCard(targetParent, prod);

                    if (prod.rewardType == ShopRewardType.RemoveAds)
                    {
                        remAdsBtnRef = btnObj.GetComponent<Button>();
                    }
                }
            }

            // =========================================================================
            //  TAB 3: KASALAR (LOOT BOX VAULT) PANELI
            // =========================================================================
            GameObject lootPnl = CreatePanel(root.transform, "LootBoxPanel", new Color(0.08f, 0.06f, 0.14f, 0.95f));
            lootPnl.SetActive(false);
            SetContentStretch(lootPnl);

            var vaultTitle = CreateText(lootPnl.transform, "Title", "HAZINE KASALARI ODASI", 22, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var vTitleRT = vaultTitle.GetComponent<RectTransform>();
            vTitleRT.anchorMin = new Vector2(0.05f, 0.92f);
            vTitleRT.anchorMax = new Vector2(0.95f, 0.98f);
            vTitleRT.offsetMin = Vector2.zero;
            vTitleRT.offsetMax = Vector2.zero;

            GameObject vaultContainer = new GameObject("VaultContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            vaultContainer.transform.SetParent(lootPnl.transform, false);
            var vcRT = vaultContainer.GetComponent<RectTransform>();
            vcRT.anchorMin = new Vector2(0.02f, 0.03f);
            vcRT.anchorMax = new Vector2(0.98f, 0.90f);
            vcRT.offsetMin = Vector2.zero;
            vcRT.offsetMax = Vector2.zero;

            var vcHLG = vaultContainer.GetComponent<HorizontalLayoutGroup>();
            vcHLG.spacing = 24;
            vcHLG.padding = new RectOffset(20, 20, 12, 12);
            vcHLG.childControlWidth = true;
            vcHLG.childControlHeight = true;
            vcHLG.childForceExpandWidth = true;
            vcHLG.childForceExpandHeight = true;

            int bPrice = liveConfig != null ? liveConfig.bronzeBoxPriceGold : 500;
            int sPrice = liveConfig != null ? liveConfig.silverBoxPriceGold : 1500;
            int gPrice = liveConfig != null ? liveConfig.goldBoxPriceKadimPara : 50;

            var bronzeChest = CreateChestPedestal(vaultContainer.transform, "BronzePedestal", "BRONZ KASA",
                "Olasiliklar:\n* 50 - 200 Altin\n* %10 Kadim Para Sansi\n* Temel Simya Malzemeleri",
                new Color(0.68f, 0.40f, 0.20f), $"{bPrice} Altin");

            var silverChest = CreateChestPedestal(vaultContainer.transform, "SilverPedestal", "GUMUS KASA",
                "Olasiliklar:\n* 200 - 500 Altin\n* %30 Kadim Para (3-5 Adet)\n* %20 Nadir Malzeme",
                new Color(0.40f, 0.58f, 0.72f), $"{sPrice} Altin");

            var goldChest = CreateChestPedestal(vaultContainer.transform, "GoldPedestal", "ALTIN KASA",
                "Olasiliklar:\n* 500 - 1500 Altin\n* %60 Kadim Para (10-25 Adet)\n* %50 Efsanevi Parsomen",
                new Color(0.90f, 0.72f, 0.18f), $"{gPrice} Kadim Para");

            // =========================================================================
            //  ALT BILGI CUBUGU (InfoBar)
            // =========================================================================
            var infoBar = CreatePanel(root.transform, "InfoBar", new Color(0.10f, 0.08f, 0.16f, 0.95f));
            var infoRT = infoBar.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0.02f, 0.015f);
            infoRT.anchorMax = new Vector2(0.98f, 0.07f);
            infoRT.offsetMin = Vector2.zero;
            infoRT.offsetMax = Vector2.zero;

            var infoHLG = infoBar.AddComponent<HorizontalLayoutGroup>();
            infoHLG.spacing = 20;
            infoHLG.padding = new RectOffset(20, 20, 0, 0);
            infoHLG.childAlignment = TextAnchor.MiddleCenter;
            infoHLG.childControlWidth = false;
            infoHLG.childControlHeight = true;
            infoHLG.childForceExpandWidth = false;
            infoHLG.childForceExpandHeight = true;

            var remainingTxt = CreateText(infoBar.transform, "RemainingPlaysText", "Kalan Hak: 3 / 3", 18, new Color(0.25f, 0.95f, 0.55f), TextAlignmentOptions.Left, true);
            var remLE = remainingTxt.AddComponent<LayoutElement>();
            remLE.preferredWidth = 200;

            var cooldownTxt = CreateText(infoBar.transform, "CooldownText", "", 18, new Color(1f, 0.70f, 0.25f), TextAlignmentOptions.Left, true);
            var cdLE = cooldownTxt.AddComponent<LayoutElement>();
            cdLE.preferredWidth = 180;

            var highScoreTxt = CreateText(infoBar.transform, "HighScoreText", "En Yuksek Skorlar: -", 15, new Color(0.90f, 0.85f, 0.65f), TextAlignmentOptions.Center, false);
            var hsLE = highScoreTxt.AddComponent<LayoutElement>();
            hsLE.preferredWidth = 800;
            hsLE.flexibleWidth = 1;

            var adPlayBtn = CreateButton(infoBar.transform, "AdBonusPlayBtn", "[+] Reklam Izle (+1 Hak)", new Color(0.18f, 0.55f, 0.35f));
            var adLE = adPlayBtn.AddComponent<LayoutElement>();
            adLE.preferredWidth = 260;

            // =========================================================================
            //  HESAP YONETIMI MODALI (AccountUI)
            // =========================================================================
            GameObject accountModal = CreatePanel(root.transform, "AccountModalPanel", new Color(0.04f, 0.03f, 0.08f, 0.92f));
            accountModal.SetActive(false);
            SetFullStretch(accountModal);

            var modalDialog = CreatePanel(accountModal.transform, "DialogBox", new Color(0.14f, 0.10f, 0.22f, 0.98f));
            var dialogRT = modalDialog.GetComponent<RectTransform>();
            dialogRT.anchorMin = new Vector2(0.32f, 0.22f);
            dialogRT.anchorMax = new Vector2(0.68f, 0.78f);
            dialogRT.offsetMin = Vector2.zero;
            dialogRT.offsetMax = Vector2.zero;

            var mTitle = CreateText(modalDialog.transform, "Title", "HESAP YONETIMI & BULUT KAYIT", 22, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var mTitleRT = mTitle.GetComponent<RectTransform>();
            mTitleRT.anchorMin = new Vector2(0.05f, 0.86f);
            mTitleRT.anchorMax = new Vector2(0.95f, 0.96f);
            mTitleRT.offsetMin = Vector2.zero;
            mTitleRT.offsetMax = Vector2.zero;

            var mIdTxt = CreateText(modalDialog.transform, "PlayerIdText", "Oyuncu ID: -", 16, new Color(0.85f, 0.85f, 0.90f), TextAlignmentOptions.Center, true);
            var mIdRT = mIdTxt.GetComponent<RectTransform>();
            mIdRT.anchorMin = new Vector2(0.05f, 0.74f);
            mIdRT.anchorMax = new Vector2(0.95f, 0.83f);
            mIdRT.offsetMin = Vector2.zero;
            mIdRT.offsetMax = Vector2.zero;

            var mStatusTxt = CreateText(modalDialog.transform, "StatusText", "Misafir veya cevrimici giris yapabilirsiniz.", 15, Color.gray, TextAlignmentOptions.Center, false);
            var mStatusRT = mStatusTxt.GetComponent<RectTransform>();
            mStatusRT.anchorMin = new Vector2(0.05f, 0.64f);
            mStatusRT.anchorMax = new Vector2(0.95f, 0.72f);
            mStatusRT.offsetMin = Vector2.zero;
            mStatusRT.offsetMax = Vector2.zero;

            var guestBtn = CreateButton(modalDialog.transform, "GuestSignInBtn", "Misafir Olarak Devam Et", new Color(0.28f, 0.20f, 0.45f));
            var gRT = guestBtn.GetComponent<RectTransform>();
            gRT.anchorMin = new Vector2(0.15f, 0.46f);
            gRT.anchorMax = new Vector2(0.85f, 0.58f);
            gRT.offsetMin = Vector2.zero;
            gRT.offsetMax = Vector2.zero;

            var signOutBtn = CreateButton(modalDialog.transform, "SignOutBtn", "Hesaptan Cikis Yap", new Color(0.60f, 0.20f, 0.20f));
            var soRT = signOutBtn.GetComponent<RectTransform>();
            soRT.anchorMin = new Vector2(0.15f, 0.30f);
            soRT.anchorMax = new Vector2(0.85f, 0.42f);
            soRT.offsetMin = Vector2.zero;
            soRT.offsetMax = Vector2.zero;

            var closeAccModalBtn = CreateButton(modalDialog.transform, "CloseModalBtn", "KAPAT", new Color(0.35f, 0.35f, 0.40f));
            var caRT = closeAccModalBtn.GetComponent<RectTransform>();
            caRT.anchorMin = new Vector2(0.30f, 0.10f);
            caRT.anchorMax = new Vector2(0.70f, 0.22f);
            caRT.offsetMin = Vector2.zero;
            caRT.offsetMax = Vector2.zero;

            AccountUI accUI = canvasObj.AddComponent<AccountUI>();
            accUI.playerNameText = profTxt.GetComponent<TextMeshProUGUI>();
            accUI.accountStatusBadge = badgeTxt.GetComponent<TextMeshProUGUI>();
            accUI.openAccountModalButton = accountBtn.GetComponent<Button>();
            accUI.accountModalPanel = accountModal;
            accUI.closeModalButton = closeAccModalBtn.GetComponent<Button>();
            accUI.modalTitleText = mTitle.GetComponent<TextMeshProUGUI>();
            accUI.modalPlayerIdText = mIdTxt.GetComponent<TextMeshProUGUI>();
            accUI.modalStatusMessageText = mStatusTxt.GetComponent<TextMeshProUGUI>();
            accUI.guestSignInButton = guestBtn.GetComponent<Button>();
            accUI.signOutButton = signOutBtn.GetComponent<Button>();

            // =========================================================================
            //  GERCEKCI GOOGLE PLAY SATIN ALMA SIMULATOR MODALI
            // =========================================================================
            GameObject gpModal = CreatePanel(root.transform, "GooglePlayModalPanel", new Color(0, 0, 0, 0.85f));
            gpModal.SetActive(false);
            SetFullStretch(gpModal);

            var gpBox = CreatePanel(gpModal.transform, "DialogBox", new Color(0.12f, 0.12f, 0.14f, 1f));
            var gpRT = gpBox.GetComponent<RectTransform>();
            gpRT.anchorMin = new Vector2(0.34f, 0.22f);
            gpRT.anchorMax = new Vector2(0.66f, 0.78f);
            gpRT.offsetMin = Vector2.zero;
            gpRT.offsetMax = Vector2.zero;

            var gpHeader = CreateText(gpBox.transform, "GPHeader", "Google Play", 20, new Color(0.01f, 0.75f, 0.45f), TextAlignmentOptions.Left, true);
            var gpH_RT = gpHeader.GetComponent<RectTransform>();
            gpH_RT.anchorMin = new Vector2(0.08f, 0.88f);
            gpH_RT.anchorMax = new Vector2(0.92f, 0.96f);
            gpH_RT.offsetMin = Vector2.zero;
            gpH_RT.offsetMax = Vector2.zero;

            var gpAppName = CreateText(gpBox.transform, "GPAppName", "Potion Tavern", 14, new Color(0.7f, 0.7f, 0.75f), TextAlignmentOptions.Left, false);
            var gpA_RT = gpAppName.GetComponent<RectTransform>();
            gpA_RT.anchorMin = new Vector2(0.08f, 0.82f);
            gpA_RT.anchorMax = new Vector2(0.92f, 0.88f);
            gpA_RT.offsetMin = Vector2.zero;
            gpA_RT.offsetMax = Vector2.zero;

            var gpTitle = CreateText(gpBox.transform, "ItemTitleText", "50 KADIM PARA (Usta Kesesi)", 18, Color.white, TextAlignmentOptions.Left, true);
            var gpT_RT = gpTitle.GetComponent<RectTransform>();
            gpT_RT.anchorMin = new Vector2(0.08f, 0.68f);
            gpT_RT.anchorMax = new Vector2(0.92f, 0.80f);
            gpT_RT.offsetMin = Vector2.zero;
            gpT_RT.offsetMax = Vector2.zero;

            var gpPrice = CreateText(gpBox.transform, "ItemPriceText", "79.99 TL", 22, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Right, true);
            var gpP_RT = gpPrice.GetComponent<RectTransform>();
            gpP_RT.anchorMin = new Vector2(0.08f, 0.56f);
            gpP_RT.anchorMax = new Vector2(0.92f, 0.66f);
            gpP_RT.offsetMin = Vector2.zero;
            gpP_RT.offsetMax = Vector2.zero;

            var gpPayMethod = CreateText(gpBox.transform, "PaymentMethod", "Odeme Yontemi:  **** 4821 (Google Play Test Karti)", 13, new Color(0.65f, 0.65f, 0.70f), TextAlignmentOptions.Left, false);
            var gpM_RT = gpPayMethod.GetComponent<RectTransform>();
            gpM_RT.anchorMin = new Vector2(0.08f, 0.44f);
            gpM_RT.anchorMax = new Vector2(0.92f, 0.52f);
            gpM_RT.offsetMin = Vector2.zero;
            gpM_RT.offsetMax = Vector2.zero;

            var gpConfirmBtn = CreateButton(gpBox.transform, "ConfirmBuyBtn", "1 TIKLA SATIN AL", new Color(0.01f, 0.65f, 0.38f));
            var gpC_RT = gpConfirmBtn.GetComponent<RectTransform>();
            gpC_RT.anchorMin = new Vector2(0.08f, 0.22f);
            gpC_RT.anchorMax = new Vector2(0.92f, 0.36f);
            gpC_RT.offsetMin = Vector2.zero;
            gpC_RT.offsetMax = Vector2.zero;

            var gpCancelBtn = CreateButton(gpBox.transform, "CancelBuyBtn", "IPTAL ET", new Color(0.35f, 0.35f, 0.38f));
            var gpCn_RT = gpCancelBtn.GetComponent<RectTransform>();
            gpCn_RT.anchorMin = new Vector2(0.25f, 0.06f);
            gpCn_RT.anchorMax = new Vector2(0.75f, 0.16f);
            gpCn_RT.offsetMin = Vector2.zero;
            gpCn_RT.offsetMax = Vector2.zero;

            // =========================================================================
            //  GERCEKCI VIDEO REKLAM SIMULATOR MODALI
            // =========================================================================
            GameObject adModal = CreatePanel(root.transform, "VideoAdSimulatorPanel", new Color(0.03f, 0.02f, 0.06f, 0.98f));
            adModal.SetActive(false);
            SetFullStretch(adModal);

            var adTopBar = CreatePanel(adModal.transform, "TopBar", new Color(0.12f, 0.08f, 0.18f, 0.95f));
            var atbRT = adTopBar.GetComponent<RectTransform>();
            atbRT.anchorMin = new Vector2(0.02f, 0.91f);
            atbRT.anchorMax = new Vector2(0.98f, 0.98f);
            atbRT.offsetMin = Vector2.zero;
            atbRT.offsetMax = Vector2.zero;

            var adTopTxt = CreateText(adTopBar.transform, "TopTitle", "SPONSORLU VIDEO REKLAM (CANLI TEST SIMULATORU)", 18, Color.white, TextAlignmentOptions.Left, true);
            var attRT = adTopTxt.GetComponent<RectTransform>();
            attRT.anchorMin = new Vector2(0.03f, 0);
            attRT.anchorMax = new Vector2(0.60f, 1);
            attRT.offsetMin = Vector2.zero;
            attRT.offsetMax = Vector2.zero;

            var adCountTxt = CreateText(adTopBar.transform, "CountdownText", "Odule Kalan Sure: 5 sn", 18, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Right, true);
            var actRT = adCountTxt.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(0.60f, 0);
            actRT.anchorMax = new Vector2(0.97f, 1);
            actRT.offsetMin = Vector2.zero;
            actRT.offsetMax = Vector2.zero;

            // Video Ekran Temsili
            var adScreen = CreatePanel(adModal.transform, "VideoScreenBox", new Color(0.16f, 0.12f, 0.24f, 1f));
            var asRT = adScreen.GetComponent<RectTransform>();
            asRT.anchorMin = new Vector2(0.15f, 0.18f);
            asRT.anchorMax = new Vector2(0.85f, 0.88f);
            asRT.offsetMin = Vector2.zero;
            asRT.offsetMax = Vector2.zero;

            var adScreenTxt = CreateText(adScreen.transform, "ScreenContent", "âœ¦ BÃœYÃœLÃœ TAVERNA SPONSORLUGU âœ¦\n\nGercek oyunculara gosterilecek video reklam akisi burada oynatilir.\nTest modunda 5 saniye sonra odul aktiflesir.", 22, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(adScreenTxt);

            // Alt Bar ve Ilerleme Cubugu
            var adProgBg = CreatePanel(adModal.transform, "ProgressBar_BG", new Color(0.2f, 0.2f, 0.25f, 1f));
            var apbRT = adProgBg.GetComponent<RectTransform>();
            apbRT.anchorMin = new Vector2(0.15f, 0.11f);
            apbRT.anchorMax = new Vector2(0.85f, 0.15f);
            apbRT.offsetMin = Vector2.zero;
            apbRT.offsetMax = Vector2.zero;

            var adProgFill = CreatePanel(adProgBg.transform, "ProgressBar_Fill", new Color(0.01f, 0.85f, 0.45f, 1f));
            var apfRT = adProgFill.GetComponent<RectTransform>();
            apfRT.anchorMin = Vector2.zero;
            apfRT.anchorMax = new Vector2(1f, 1f);
            apfRT.offsetMin = Vector2.zero;
            apfRT.offsetMax = Vector2.zero;
            var apfImg = adProgFill.GetComponent<Image>();
            apfImg.type = Image.Type.Filled;
            apfImg.fillMethod = Image.FillMethod.Horizontal;
            apfImg.fillAmount = 0f;

            var adCloseBtn = CreateButton(adModal.transform, "CloseRewardBtn", "Reklam Oynatiliyor...", new Color(0.2f, 0.65f, 0.35f));
            var acbRT = adCloseBtn.GetComponent<RectTransform>();
            acbRT.anchorMin = new Vector2(0.35f, 0.02f);
            acbRT.anchorMax = new Vector2(0.65f, 0.09f);
            acbRT.offsetMin = Vector2.zero;
            acbRT.offsetMax = Vector2.zero;
            adCloseBtn.GetComponent<Button>().interactable = false;

            // =========================================================================
            //  KASA ODUL POPUP MODALI
            // =========================================================================
            GameObject rewardPopup = CreatePanel(root.transform, "LootRewardPopup", new Color(0.04f, 0.03f, 0.08f, 0.92f));
            rewardPopup.SetActive(false);
            SetFullStretch(rewardPopup);

            var rDialog = CreatePanel(rewardPopup.transform, "DialogBox", new Color(0.14f, 0.11f, 0.22f, 0.98f));
            var rDialogRT = rDialog.GetComponent<RectTransform>();
            rDialogRT.anchorMin = new Vector2(0.33f, 0.25f);
            rDialogRT.anchorMax = new Vector2(0.67f, 0.75f);
            rDialogRT.offsetMin = Vector2.zero;
            rDialogRT.offsetMax = Vector2.zero;

            var rTitle = CreateText(rDialog.transform, "Title", "KUTU ACILDI!", 24, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var rTitleRT = rTitle.GetComponent<RectTransform>();
            rTitleRT.anchorMin = new Vector2(0.05f, 0.82f);
            rTitleRT.anchorMax = new Vector2(0.95f, 0.94f);
            rTitleRT.offsetMin = Vector2.zero;
            rTitleRT.offsetMax = Vector2.zero;

            var rDesc = CreateText(rDialog.transform, "RewardsList", "* +350 Altin\n* +2 Kadim Para", 20, Color.white, TextAlignmentOptions.Center, false);
            var rDescRT = rDesc.GetComponent<RectTransform>();
            rDescRT.anchorMin = new Vector2(0.08f, 0.32f);
            rDescRT.anchorMax = new Vector2(0.92f, 0.78f);
            rDescRT.offsetMin = Vector2.zero;
            rDescRT.offsetMax = Vector2.zero;

            var rCollectBtn = CreateButton(rDialog.transform, "CollectButton", "TOPLA VE DEVAM ET", new Color(0.18f, 0.65f, 0.35f));
            var rColRT = rCollectBtn.GetComponent<RectTransform>();
            rColRT.anchorMin = new Vector2(0.20f, 0.10f);
            rColRT.anchorMax = new Vector2(0.80f, 0.25f);
            rColRT.offsetMin = Vector2.zero;
            rColRT.offsetMax = Vector2.zero;

            // =========================================================================
            //  1. RUN ESLESTIRME OYUN PANELI
            // =========================================================================
            var runeGamePanel = CreatePanel(root.transform, "RuneMatchGamePanel", new Color(0.06f, 0.04f, 0.12f, 0.98f));
            runeGamePanel.SetActive(false);
            SetFullStretch(runeGamePanel);

            var runeScoreTxt = CreateText(runeGamePanel.transform, "ScoreText", "Skor: 0", 28, new Color(1f, 0.88f, 0.35f), TextAlignmentOptions.Left, true);
            PositionTopLeft(runeScoreTxt.GetComponent<RectTransform>());

            var runeComboTxt = CreateText(runeGamePanel.transform, "ComboText", "Kombo: x1", 28, new Color(1f, 0.60f, 0.20f), TextAlignmentOptions.Center, true);
            PositionTopCenter(runeComboTxt.GetComponent<RectTransform>());

            var runeTimerTxt = CreateText(runeGamePanel.transform, "TimerText", "Sure: 60s", 28, Color.white, TextAlignmentOptions.Right, true);
            PositionTopRight(runeTimerTxt.GetComponent<RectTransform>());

            GameObject gridObj = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(runeGamePanel.transform, false);
            var gridRT = gridObj.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.20f, 0.08f);
            gridRT.anchorMax = new Vector2(0.80f, 0.86f);
            gridRT.offsetMin = Vector2.zero;
            gridRT.offsetMax = Vector2.zero;
            var glg = gridObj.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(140, 140);
            glg.spacing = new Vector2(18, 18);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment = TextAnchor.MiddleCenter;

            var runeGame = runeGamePanel.AddComponent<RuneMatchGame>();
            runeGame.gamePanel = runeGamePanel;
            runeGame.gridContainer = gridRT;
            runeGame.scoreText = runeScoreTxt.GetComponent<TextMeshProUGUI>();
            runeGame.timerText = runeTimerTxt.GetComponent<TextMeshProUGUI>();
            runeGame.comboText = runeComboTxt.GetComponent<TextMeshProUGUI>();
            runeGame.cardBackSprite = cardBackSprite;

            // =========================================================================
            //  2. KAZAN IKSIR YAKALAMA OYUN PANELI
            // =========================================================================
            var potionGamePanel = CreatePanel(root.transform, "PotionCatchGamePanel", new Color(0.04f, 0.08f, 0.06f, 0.98f));
            potionGamePanel.SetActive(false);
            SetFullStretch(potionGamePanel);

            var potScoreTxt = CreateText(potionGamePanel.transform, "ScoreText", "Skor: 0", 28, new Color(0.30f, 0.95f, 0.55f), TextAlignmentOptions.Left, true);
            PositionTopLeft(potScoreTxt.GetComponent<RectTransform>());

            GameObject heartsObj = new GameObject("HeartsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            heartsObj.transform.SetParent(potionGamePanel.transform, false);
            PositionTopCenter(heartsObj.GetComponent<RectTransform>());
            var hHLG = heartsObj.GetComponent<HorizontalLayoutGroup>();
            hHLG.spacing = 10;
            hHLG.childAlignment = TextAnchor.MiddleCenter;

            var potTimerTxt = CreateText(potionGamePanel.transform, "TimerText", "Sure: 60s", 28, Color.white, TextAlignmentOptions.Right, true);
            PositionTopRight(potTimerTxt.GetComponent<RectTransform>());

            var frenzyTxt = CreateText(potionGamePanel.transform, "FrenzyText", "", 24, new Color(1f, 0.90f, 0.20f), TextAlignmentOptions.Center, true);
            var frenzyRT = frenzyTxt.GetComponent<RectTransform>();
            frenzyRT.anchorMin = new Vector2(0.20f, 0.82f);
            frenzyRT.anchorMax = new Vector2(0.80f, 0.89f);
            frenzyRT.offsetMin = Vector2.zero;
            frenzyRT.offsetMax = Vector2.zero;

            GameObject spawnAreaObj = CreatePanel(potionGamePanel.transform, "SpawnArea", new Color(0, 0, 0, 0.25f));
            var spawnAreaRT = spawnAreaObj.GetComponent<RectTransform>();
            spawnAreaRT.anchorMin = new Vector2(0.12f, 0.05f);
            spawnAreaRT.anchorMax = new Vector2(0.88f, 0.82f);
            spawnAreaRT.offsetMin = Vector2.zero;
            spawnAreaRT.offsetMax = Vector2.zero;

            GameObject cauldronObj = CreatePanel(spawnAreaObj.transform, "Cauldron", Color.white);
            var cauldronRT = cauldronObj.GetComponent<RectTransform>();
            cauldronRT.sizeDelta = new Vector2(170, 90);
            cauldronRT.anchoredPosition = new Vector2(0, -260);
            Image cImg = cauldronObj.GetComponent<Image>();
            if (cauldronSprite != null)
            {
                cImg.sprite = cauldronSprite;
                cImg.preserveAspect = true;
            }
            else
            {
                cImg.color = new Color(0.25f, 0.25f, 0.35f);
            }

            var potionCatchGame = potionGamePanel.AddComponent<PotionCatchGame>();
            potionCatchGame.gamePanel = potionGamePanel;
            potionCatchGame.cauldron = cauldronRT;
            potionCatchGame.spawnArea = spawnAreaRT;
            potionCatchGame.scoreText = potScoreTxt.GetComponent<TextMeshProUGUI>();
            potionCatchGame.timerText = potTimerTxt.GetComponent<TextMeshProUGUI>();
            potionCatchGame.frenzyText = frenzyTxt.GetComponent<TextMeshProUGUI>();
            potionCatchGame.heartsContainer = heartsObj.transform;
            potionCatchGame.cauldronSprite = cauldronSprite;
            potionCatchGame.heartSprite = heartSprite;

            // =========================================================================
            //  3. KRISTAL ARINDIRMA PANELI
            // =========================================================================
            var crystalGamePanel = CreatePanel(root.transform, "CrystalSortGamePanel", new Color(0.08f, 0.05f, 0.12f, 0.98f));
            crystalGamePanel.SetActive(false);
            SetFullStretch(crystalGamePanel);

            var cryScoreTxt = CreateText(crystalGamePanel.transform, "ScoreText", "Skor: 0", 28, new Color(1f, 0.88f, 0.35f), TextAlignmentOptions.Left, true);
            PositionTopLeft(cryScoreTxt.GetComponent<RectTransform>());

            var cryComboTxt = CreateText(crystalGamePanel.transform, "ComboText", "Kombo: x1", 28, new Color(0.40f, 0.85f, 1f), TextAlignmentOptions.Center, true);
            PositionTopCenter(cryComboTxt.GetComponent<RectTransform>());

            var cryTimerTxt = CreateText(crystalGamePanel.transform, "TimerText", "Sure: 60s", 28, Color.white, TextAlignmentOptions.Right, true);
            PositionTopRight(cryTimerTxt.GetComponent<RectTransform>());

            var cryFeverTxt = CreateText(crystalGamePanel.transform, "FeverText", "", 24, new Color(1f, 0.85f, 0.20f), TextAlignmentOptions.Center, true);
            var cryFeverRT = cryFeverTxt.GetComponent<RectTransform>();
            cryFeverRT.anchorMin = new Vector2(0.20f, 0.82f);
            cryFeverRT.anchorMax = new Vector2(0.80f, 0.89f);
            cryFeverRT.offsetMin = Vector2.zero;
            cryFeverRT.offsetMax = Vector2.zero;

            GameObject altarBgObj = CreatePanel(crystalGamePanel.transform, "AltarCircleBackground", Color.white);
            var altarBgRT = altarBgObj.GetComponent<RectTransform>();
            altarBgRT.anchorMin = new Vector2(0.5f, 0.44f);
            altarBgRT.anchorMax = new Vector2(0.5f, 0.44f);
            altarBgRT.sizeDelta = new Vector2(460, 460);
            altarBgRT.anchoredPosition = Vector2.zero;
            Image altarImg = altarBgObj.GetComponent<Image>();
            if (altarSprite != null)
            {
                altarImg.sprite = altarSprite;
                altarImg.preserveAspect = true;
            }
            else
            {
                altarImg.color = new Color(0.18f, 0.12f, 0.28f, 0.80f);
            }

            var targetTxt = CreateText(altarBgObj.transform, "TargetElementText", "ARINDIR: ATES", 24, Color.yellow, TextAlignmentOptions.Center, true);
            var targetRT = targetTxt.GetComponent<RectTransform>();
            targetRT.anchorMin = new Vector2(0.20f, 0.42f);
            targetRT.anchorMax = new Vector2(0.80f, 0.58f);
            targetRT.offsetMin = Vector2.zero;
            targetRT.offsetMax = Vector2.zero;

            GameObject circleContainer = new GameObject("CrystalCircleContainer", typeof(RectTransform));
            circleContainer.transform.SetParent(altarBgObj.transform, false);
            SetFullStretch(circleContainer);

            var crystalGame = crystalGamePanel.AddComponent<CrystalSortGame>();
            crystalGame.gamePanel = crystalGamePanel;
            crystalGame.altarBackgroundImage = altarImg;
            crystalGame.altarCircleSprite = altarSprite;
            crystalGame.crystalCircleContainer = circleContainer.transform;
            crystalGame.scoreText = cryScoreTxt.GetComponent<TextMeshProUGUI>();
            crystalGame.comboText = cryComboTxt.GetComponent<TextMeshProUGUI>();
            crystalGame.timerText = cryTimerTxt.GetComponent<TextMeshProUGUI>();
            crystalGame.targetElementText = targetTxt.GetComponent<TextMeshProUGUI>();
            crystalGame.feverText = cryFeverTxt.GetComponent<TextMeshProUGUI>();

            // =========================================================================
            //  MiniGameManager BILESENINI BAGLA
            // =========================================================================
            MiniGameManager mgm = canvasObj.AddComponent<MiniGameManager>();
            mgm.miniGameCamera = cam;
            mgm.mainPanel = root;
            mgm.miniGameSelectionPanel = selectionPanel;
            mgm.potionPassPanel = passPanel;
            mgm.shopPanel = shopPnl;
            mgm.lootBoxPanel = lootPnl;

            mgm.tabMiniGames = tabMiniGamesBtn.GetComponent<Button>();
            mgm.tabPotionPass = tabPassBtn.GetComponent<Button>();
            mgm.tabShop = tabShopBtn.GetComponent<Button>();
            mgm.tabLootBox = tabLootBtn.GetComponent<Button>();
            mgm.closeButton = closeBtn.GetComponent<Button>();

            mgm.runeMatchButton = runeCard.transform.Find("PlayButton")?.GetComponent<Button>() ?? runeCard.GetComponentInChildren<Button>();
            mgm.potionCatchButton = potionCard.transform.Find("PlayButton")?.GetComponent<Button>() ?? potionCard.GetComponentInChildren<Button>();
            mgm.crystalSortButton = crystalCard.transform.Find("PlayButton")?.GetComponent<Button>() ?? crystalCard.GetComponentInChildren<Button>();

            mgm.remainingPlaysText = remainingTxt.GetComponent<TextMeshProUGUI>();
            mgm.cooldownText = cooldownTxt.GetComponent<TextMeshProUGUI>();
            mgm.highScoreText = highScoreTxt.GetComponent<TextMeshProUGUI>();
            mgm.watchAdBonusPlayBtn = adPlayBtn.GetComponent<Button>();

            // Pass
            mgm.passSeasonText = seasonTxt.GetComponent<TextMeshProUGUI>();
            mgm.passXpBarFill = fillImg;
            mgm.passXpText = xpTxt.GetComponent<TextMeshProUGUI>();
            mgm.passUpgradeButton = upgradeBtn.GetComponent<Button>();
            mgm.passTierContainer = contentRT;

            // Shop
            mgm.shopParaContainer = paraContainer.transform;
            mgm.shopOffersContainer = offersContainer.transform;
            mgm.buyRemoveAdsBtn = remAdsBtnRef;

            // Vault
            mgm.bronzeCountText = bronzeChest.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            mgm.silverCountText = silverChest.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            mgm.goldCountText = goldChest.Find("CountText")?.GetComponent<TextMeshProUGUI>();

            mgm.openBronzeBtn = bronzeChest.Find("OpenBtn")?.GetComponent<Button>();
            mgm.openSilverBtn = silverChest.Find("OpenBtn")?.GetComponent<Button>();
            mgm.openGoldBtn = goldChest.Find("OpenBtn")?.GetComponent<Button>();

            mgm.buyOpenBronzeBtn = bronzeChest.Find("BuyBtn")?.GetComponent<Button>();
            mgm.buyOpenSilverBtn = silverChest.Find("BuyBtn")?.GetComponent<Button>();
            mgm.buyOpenGoldBtn = goldChest.Find("BuyBtn")?.GetComponent<Button>();

            // Reward popup
            mgm.lootRewardPopupPanel = rewardPopup;
            mgm.lootRewardTitleText = rTitle.GetComponent<TextMeshProUGUI>();
            mgm.lootRewardDescText = rDesc.GetComponent<TextMeshProUGUI>();
            mgm.closeLootRewardBtn = rCollectBtn.GetComponent<Button>();

            // Google Play Modal
            mgm.googlePlayModalPanel = gpModal;
            mgm.gpItemTitleText = gpTitle.GetComponent<TextMeshProUGUI>();
            mgm.gpItemPriceText = gpPrice.GetComponent<TextMeshProUGUI>();
            mgm.gpConfirmBuyBtn = gpConfirmBtn.GetComponent<Button>();
            mgm.gpCancelBuyBtn = gpCancelBtn.GetComponent<Button>();

            // Video Ad Simulator Modal
            mgm.videoAdSimulatorPanel = adModal;
            mgm.adCountdownText = adCountTxt.GetComponent<TextMeshProUGUI>();
            mgm.adProgressBar = apfImg;
            mgm.adCloseRewardBtn = adCloseBtn.GetComponent<Button>();

            mgm.accountUI = accUI;

            // Lobi Sahnesi ve BuildingHover baglantilari
            GameObject lobi = GameObject.Find("LobiSahnesi");
            if (lobi != null)
            {
                mgm.returnCanvas = lobi;
            }

            foreach (var bh in FindObjectsByType<BuildingHover>(FindObjectsSortMode.None))
            {
                if (bh.buildingName != null && bh.buildingName.Contains("Mini Game"))
                {
                    bh.targetGameCanvas = canvasObj;
                    UnityEditor.EditorUtility.SetDirty(bh);
                }
            }

            // Dinamik Shop Butonlarina Tiklama Olaylarini Bagla
            BindShopCardListeners(mgm, paraContainer.transform);
            BindShopCardListeners(mgm, offersContainer.transform);

            canvasObj.SetActive(false);

            UnityEditor.Selection.activeGameObject = canvasObj;
            UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObj, "Create Mini Game Area");

            Debug.Log("<color=green>[MiniGameManager]</color> Canliya hazir Buyu & Simya Arenasi tum dinamik veri baglantilari ve simulatorleriyle basariyla kuruldu!");
        }

        private static void BindShopCardListeners(MiniGameManager mgm, Transform container)
        {
            var catalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            if (catalog == null || container == null) return;

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                string prodId = child.name.Replace("ShopCard_", "");
                var prod = catalog.FindProduct(prodId);
                if (prod != null)
                {
                    Button btn = child.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => mgm.OnShopProductClicked(prod));
                    }
                }
            }
        }

        // â”€â”€ Yardimci Layout & UI Metotlari â”€â”€

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = bgColor;
            return panel;
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, bool isBold = false)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(11, fontSize - 6);
            tmp.fontSizeMax = fontSize;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            if (isBold) tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color bgColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = bgColor;

            var txtObj = CreateText(btnObj.transform, "Text", label, 18, Color.white, TextAlignmentOptions.Center, true);
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.enableWordWrapping = false;
            SetFullStretch(txtObj);

            return btnObj;
        }

        private static GameObject CreateTabButton(Transform parent, string name, string label, Color bgColor)
        {
            GameObject btnObj = CreateButton(parent, name, label, bgColor);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 52;
            le.minHeight = 44;
            le.flexibleWidth = 1;
            return btnObj;
        }

        private static GameObject CreateThemedGameCard(Transform parent, string name, string title, string desc, Color headerColor, Sprite previewSprite, string rewardTags)
        {
            GameObject card = CreatePanel(parent, name, new Color(0.12f, 0.09f, 0.19f, 0.96f));
            
            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 540;
            le.preferredHeight = 720;
            le.minWidth = 360;
            le.minHeight = 520;
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Baslik Banner
            var topBar = CreatePanel(card.transform, "Header", headerColor);
            var topLE = topBar.AddComponent<LayoutElement>();
            topLE.preferredHeight = 48;
            topLE.minHeight = 44;
            var titleObj = CreateText(topBar.transform, "Title", title, 20, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(titleObj);

            // 2. Onizleme Resim Alani
            var iconBox = CreatePanel(card.transform, "PreviewIconBox", new Color(0.07f, 0.05f, 0.12f, 0.85f));
            var iconLE = iconBox.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 230;
            iconLE.minHeight = 200;

            if (previewSprite != null)
            {
                GameObject iconObj = CreatePanel(iconBox.transform, "PreviewIcon", Color.white);
                var iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.15f, 0.10f);
                iconRT.anchorMax = new Vector2(0.85f, 0.90f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;
                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = previewSprite;
                iconImg.preserveAspect = true;
            }

            // 3. Aciklama
            var descObj = CreateText(card.transform, "Description", desc, 15, new Color(0.88f, 0.88f, 0.94f), TextAlignmentOptions.Center, false);
            var descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 85;
            descLE.minHeight = 70;
            var descTMP = descObj.GetComponent<TextMeshProUGUI>();
            descTMP.enableWordWrapping = true;
            descTMP.lineSpacing = 1.15f;

            // 4. Odul Etiketleri
            var rewBox = CreatePanel(card.transform, "RewardsBox", new Color(0.20f, 0.14f, 0.30f, 0.90f));
            var rewLE = rewBox.AddComponent<LayoutElement>();
            rewLE.preferredHeight = 38;
            rewLE.minHeight = 34;
            var rewardObj = CreateText(rewBox.transform, "Rewards", rewardTags, 14, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            SetFullStretch(rewardObj);

            // 5. Oyna Butonu
            var playBtn = CreateButton(card.transform, "PlayButton", "> OYNA <", new Color(0.18f, 0.65f, 0.35f));
            var playLE = playBtn.AddComponent<LayoutElement>();
            playLE.preferredHeight = 58;
            playLE.minHeight = 50;

            return card;
        }

        private static GameObject CreateDynamicShopCard(Transform parent, ShopProductData prod)
        {
            GameObject card = CreatePanel(parent, $"ShopCard_{prod.id}", new Color(0.13f, 0.09f, 0.20f, 0.96f));

            var le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 540;
            le.preferredHeight = 280;
            le.minWidth = 320;
            le.minHeight = 220;
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Baslik Banner
            var topBar = CreatePanel(card.transform, "TopBar", prod.cardAccentColor);
            var tbLE = topBar.AddComponent<LayoutElement>();
            tbLE.preferredHeight = 44;
            tbLE.minHeight = 40;
            string popTag = prod.isPopular ? " â˜…" : "";
            var titleObj = CreateText(topBar.transform, "Title", prod.displayName + popTag, 18, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(titleObj);

            // 2. Aciklama
            string fullDesc = string.IsNullOrEmpty(prod.subtitle) ? prod.description : $"[{prod.subtitle}]\n{prod.description}";
            var descObj = CreateText(card.transform, "Desc", fullDesc, 14, new Color(0.88f, 0.88f, 0.94f), TextAlignmentOptions.Center, false);
            var descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 70;
            descLE.minHeight = 60;
            var descTMP = descObj.GetComponent<TextMeshProUGUI>();
            descTMP.enableWordWrapping = true;
            descTMP.lineSpacing = 1.15f;

            // 3. Fiyat / Satin Alma Butonu
            Color btnColor = (prod.priceType == ShopPriceType.RewardedAd) ? new Color(0.18f, 0.45f, 0.65f) : new Color(0.20f, 0.65f, 0.38f);
            var buyBtn = CreateButton(card.transform, "BuyButton", prod.GetFormattedPrice(), btnColor);
            var bLE = buyBtn.AddComponent<LayoutElement>();
            bLE.preferredHeight = 52;
            bLE.minHeight = 46;

            return buyBtn;
        }

        private static Transform CreateChestPedestal(Transform parent, string name, string title, string rewards, Color chestColor, string buyPrice)
        {
            GameObject pedestal = CreatePanel(parent, name, new Color(0.13f, 0.09f, 0.20f, 0.96f));

            var le = pedestal.AddComponent<LayoutElement>();
            le.preferredWidth = 540;
            le.preferredHeight = 720;
            le.minWidth = 360;
            le.minHeight = 520;
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;

            var vlg = pedestal.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Baslik Banner
            var topBar = CreatePanel(pedestal.transform, "TopBar", chestColor);
            var tbLE = topBar.AddComponent<LayoutElement>();
            tbLE.preferredHeight = 48;
            tbLE.minHeight = 42;
            var titleObj = CreateText(topBar.transform, "Title", title, 20, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(titleObj);

            // 2. Kasa Ikon Temsili
            var iconBox = CreatePanel(pedestal.transform, "ChestIcon", new Color(0.08f, 0.06f, 0.12f, 0.85f));
            var iconLE = iconBox.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 180;
            iconLE.minHeight = 150;
            var boxSymbol = CreateText(iconBox.transform, "Symbol", "[ # ]", 36, chestColor, TextAlignmentOptions.Center, true);
            SetFullStretch(boxSymbol);

            // 3. Odul Listesi
            var rewObj = CreateText(pedestal.transform, "Rewards", rewards, 14, new Color(0.88f, 0.88f, 0.94f), TextAlignmentOptions.Center, false);
            var rewLE = rewObj.AddComponent<LayoutElement>();
            rewLE.preferredHeight = 90;
            rewLE.minHeight = 75;
            var rewTMP = rewObj.GetComponent<TextMeshProUGUI>();
            rewTMP.enableWordWrapping = true;
            rewTMP.lineSpacing = 1.15f;

            // 4. Sahip Olunan Adet
            var countObj = CreateText(pedestal.transform, "CountText", "Sahip Olunan: 0 Adet", 15, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var countLE = countObj.AddComponent<LayoutElement>();
            countLE.preferredHeight = 36;
            countLE.minHeight = 30;

            // 5. Kutuyu Ac Butonu
            var openBtn = CreateButton(pedestal.transform, "OpenBtn", "KUTUYU AC", new Color(0.18f, 0.65f, 0.35f));
            var openLE = openBtn.AddComponent<LayoutElement>();
            openLE.preferredHeight = 54;
            openLE.minHeight = 46;

            // 6. Al ve Ac Butonu
            var buyBtn = CreateButton(pedestal.transform, "BuyBtn", $"{buyPrice} ile Al & Ac", new Color(0.38f, 0.22f, 0.55f));
            var buyLE = buyBtn.AddComponent<LayoutElement>();
            buyLE.preferredHeight = 46;
            buyLE.minHeight = 40;

            return pedestal.transform;
        }

        private static void CreateTierCardFromData(Transform parent, PotionPassData passData, int tier)
        {
            GameObject tierCard = CreatePanel(parent, $"Tier_{tier}", new Color(0.12f, 0.08f, 0.18f, 0.96f));

            var le = tierCard.AddComponent<LayoutElement>();
            le.preferredWidth = 240;
            le.minWidth = 220;
            le.preferredHeight = 560;
            le.minHeight = 460;

            var vlg = tierCard.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Tier Header
            var tHeader = CreatePanel(tierCard.transform, "Header", new Color(0.24f, 0.16f, 0.38f));
            var thLE = tHeader.AddComponent<LayoutElement>();
            thLE.preferredHeight = 38;
            thLE.minHeight = 34;
            var tTxt = CreateText(tHeader.transform, "Text", $"ASAMA {tier}", 16, Color.white, TextAlignmentOptions.Center, true);
            SetFullStretch(tTxt);

            // Odul Bilgilerini Al
            int tierIndex = tier - 1;
            string freeDesc = (passData != null && tierIndex < passData.freeTrackRewards.Count)
                ? passData.freeTrackRewards[tierIndex].rewardDescription
                : $"+{tier * 100} Altin";

            string premDesc = (passData != null && tierIndex < passData.premiumTrackRewards.Count)
                ? passData.premiumTrackRewards[tierIndex].rewardDescription
                : $"+{tier * 10} Kadim Para";

            // 2. Ucretsiz Odul Kutusu
            var freeBox = CreatePanel(tierCard.transform, "FreeRewardBox", new Color(0.16f, 0.13f, 0.24f));
            var fbLE = freeBox.AddComponent<LayoutElement>();
            fbLE.preferredHeight = 220;
            fbLE.minHeight = 190;

            var freeVLG = freeBox.AddComponent<VerticalLayoutGroup>();
            freeVLG.padding = new RectOffset(8, 8, 8, 8);
            freeVLG.spacing = 6;
            freeVLG.childControlWidth = true;
            freeVLG.childControlHeight = false;
            freeVLG.childForceExpandWidth = true;
            freeVLG.childForceExpandHeight = false;

            var freeTag = CreateText(freeBox.transform, "Tag", "UCRETSIZ", 12, new Color(0.70f, 0.75f, 0.82f), TextAlignmentOptions.Center, true);
            var ftLE = freeTag.AddComponent<LayoutElement>();
            ftLE.preferredHeight = 22;

            var fbTxt = CreateText(freeBox.transform, "Desc", freeDesc, 15, Color.white, TextAlignmentOptions.Center, true);
            var fbTxtLE = fbTxt.AddComponent<LayoutElement>();
            fbTxtLE.preferredHeight = 100;
            var fbTMP = fbTxt.GetComponent<TextMeshProUGUI>();
            fbTMP.enableWordWrapping = true;

            var freeClaimBtn = CreateButton(freeBox.transform, "ClaimBtn", "KILITLI", new Color(0.25f, 0.55f, 0.35f));
            var fcLE = freeClaimBtn.AddComponent<LayoutElement>();
            fcLE.preferredHeight = 40;

            // 3. Ayirici cizgi
            var sep = CreatePanel(tierCard.transform, "Separator", new Color(0.35f, 0.25f, 0.50f, 0.8f));
            var sepLE = sep.AddComponent<LayoutElement>();
            sepLE.preferredHeight = 2;

            // 4. Premium Odul Kutusu
            var premBox = CreatePanel(tierCard.transform, "PremiumRewardBox", new Color(0.26f, 0.18f, 0.10f));
            var pbLE = premBox.AddComponent<LayoutElement>();
            pbLE.preferredHeight = 220;
            pbLE.minHeight = 190;

            var premVLG = premBox.AddComponent<VerticalLayoutGroup>();
            premVLG.padding = new RectOffset(8, 8, 8, 8);
            premVLG.spacing = 6;
            premVLG.childControlWidth = true;
            premVLG.childControlHeight = false;
            premVLG.childForceExpandWidth = true;
            premVLG.childForceExpandHeight = false;

            var premTag = CreateText(premBox.transform, "Tag", "[*] PREMIUM", 12, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, true);
            var ptLE = premTag.AddComponent<LayoutElement>();
            ptLE.preferredHeight = 22;

            var pbTxt = CreateText(premBox.transform, "Desc", premDesc, 15, new Color(1f, 0.88f, 0.40f), TextAlignmentOptions.Center, true);
            var pbTxtLE = pbTxt.AddComponent<LayoutElement>();
            pbTxtLE.preferredHeight = 100;
            var pbTMP = pbTxt.GetComponent<TextMeshProUGUI>();
            pbTMP.enableWordWrapping = true;

            var premClaimBtn = CreateButton(premBox.transform, "ClaimBtn", "PREMIUM", new Color(0.70f, 0.50f, 0.15f));
            var pcLE = premClaimBtn.AddComponent<LayoutElement>();
            pcLE.preferredHeight = 40;
        }

        private static void SetFullStretch(GameObject obj)
        {
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetContentStretch(GameObject obj)
        {
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.08f);
            rt.anchorMax = new Vector2(0.98f, 0.84f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionTopLeft(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.08f, 0.91f);
            rt.anchorMax = new Vector2(0.35f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionTopCenter(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.38f, 0.91f);
            rt.anchorMax = new Vector2(0.62f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PositionTopRight(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.65f, 0.91f);
            rt.anchorMax = new Vector2(0.92f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
#endif
    }
}