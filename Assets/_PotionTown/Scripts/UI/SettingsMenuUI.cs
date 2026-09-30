using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Tam ekran ayarlar menüsü. Prefab olarak instantiate edilir.
    /// 3 sekmesi vardır: Hesap, Ses Ayarları, Credits.
    /// TopDropdownMenu'deki settings butonuyla açılır.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        public static SettingsMenuUI Instance { get; private set; }

        [Header("Ana Panel")]
        [Tooltip("Tam ekranı kaplayan arka plan (karartma)")]
        [SerializeField] private GameObject rootPanel;

        [Tooltip("Kapatma (X) butonu")]
        [SerializeField] private Button closeButton;

        // ─── TAB SİSTEMİ ───────────────────────────────────────
        [Header("Tab Butonları")]
        [SerializeField] private Button tabAccountButton;
        [SerializeField] private Button tabAudioButton;
        [SerializeField] private Button tabCreditsButton;

        [Header("Tab İçerikleri")]
        [SerializeField] private GameObject accountTabContent;
        [SerializeField] private GameObject audioTabContent;
        [SerializeField] private GameObject creditsTabContent;

        // ─── HESAP SEKMESİ ─────────────────────────────────────
        [Header("Hesap Sekmesi")]
        [Tooltip("Oyuncu durumunu gösteren metin")]
        [SerializeField] private TextMeshProUGUI accountStatusText;
        [Tooltip("Oyuncu ID'sini gösteren metin")]
        [SerializeField] private TextMeshProUGUI accountPlayerIdText;
        [Tooltip("Durum mesajı")]
        [SerializeField] private TextMeshProUGUI accountMessageText;

        [SerializeField] private TMP_InputField accountUsernameInput;
        [SerializeField] private TMP_InputField accountPasswordInput;
        [SerializeField] private Button accountGuestSignInButton;
        [SerializeField] private Button accountSignInButton;
        [SerializeField] private Button accountGoogleSignInButton;
        [SerializeField] private Button accountRegisterButton;
        [SerializeField] private Button accountSignOutButton;

        // ─── SES AYARLARI SEKMESİ ──────────────────────────────
        [Header("Ses Ayarları Sekmesi")]
        [Tooltip("Arka plan müziği ses seviyesi slider")]
        [SerializeField] private Slider musicVolumeSlider;
        [Tooltip("Müzik durumunu gösteren metin")]
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [Tooltip("Müzik açma/kapatma toggle")]
        [SerializeField] private Toggle musicMuteToggle;

        [Tooltip("Ses efektleri ses seviyesi slider")]
        [SerializeField] private Slider sfxVolumeSlider;
        [Tooltip("SFX durumunu gösteren metin")]
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
        [Tooltip("SFX açma/kapatma toggle")]
        [SerializeField] private Toggle sfxMuteToggle;

        [Header("Özel Toggle Görselleri")]
        [Tooltip("Açık durum için ikon spriti")]
        [SerializeField] private Sprite toggleOnSprite;
        [Tooltip("Kapalı durum için ikon spriti")]
        [SerializeField] private Sprite toggleOffSprite;
        [Tooltip("Müzik toggle'ının ikon görseli")]
        [SerializeField] private Image musicToggleImage;
        [Tooltip("SFX toggle'ının ikon görseli")]
        [SerializeField] private Image sfxToggleImage;

        // ─── CREDITS SEKMESİ ───────────────────────────────────
        [Header("Credits Sekmesi")]
        [Tooltip("Credits metninin gösterileceği alan (Inspector'daki 'Credits yukleniyor...' nesnesi)")]
        [SerializeField] private TextMeshProUGUI creditsText;

        [Header("Credits Boyut Oranları (% Çarpanları)")]
        [Tooltip("Credits ana başlık boyutu oranı (creditsText.fontSize baz alınır)")]
        [Range(1f, 3f)]
        [SerializeField] private float titleSizeMultiplier = 1.6f;

        [Tooltip("Alt başlık boyutu oranı ('Bir Simyacının Hikayesi')")]
        [Range(0.5f, 2f)]
        [SerializeField] private float subtitleSizeMultiplier = 0.95f;

        [Tooltip("Bölüm başlıkları boyutu oranı ('[ YAPIMCILAR ]' vb.)")]
        [Range(0.8f, 2.5f)]
        [SerializeField] private float sectionHeaderSizeMultiplier = 1.25f;

        [Tooltip("Geliştirici isimleri boyutu oranı")]
        [Range(0.8f, 2f)]
        [SerializeField] private float nameSizeMultiplier = 1.1f;

        [Tooltip("Alt bilgi / telif boyutu oranı")]
        [Range(0.4f, 1.5f)]
        [SerializeField] private float footerSizeMultiplier = 0.75f;

        [Header("Credits Renk Ayarları")]
        [Tooltip("Açık ise başlık ve isimler için özel vurgu renkleri kullanılır; kapalı ise tüm metin CreditsText rengini kullanır.")]
        [SerializeField] private bool useAccentColors = true;

        [Tooltip("Ana başlık rengi (POTIONTOWN)")]
        [SerializeField] private Color titleColor = new Color(1f, 0.88f, 0.4f, 1f);

        [Tooltip("Bölüm başlıkları rengi ([ YAPIMCILAR ] vb.)")]
        [SerializeField] private Color sectionHeaderColor = new Color(1f, 0.84f, 0f, 1f);

        [Tooltip("1. Geliştirici isim rengi (Enes)")]
        [SerializeField] private Color name1Color = new Color(1f, 0.62f, 0.26f, 1f);

        [Tooltip("2. Geliştirici isim rengi (Utku)")]
        [SerializeField] private Color name2Color = new Color(0.7f, 0.53f, 1f, 1f);

        [Header("Özel Credits Metni (İsteğe Bağlı)")]
        [Tooltip("Dolu ise otomatik liste yerine doğrudan bu metin gösterilir. Boş ise yukarıdaki ayarlarla dinamik oluşturulur.")]
        [TextArea(5, 15)]
        [SerializeField] private string customCreditsText = "";

        // ─── TAB RENK AYARLARI ─────────────────────────────────
        [Header("Tab Renk Ayarları")]
        [Tooltip("Aktif tab rengi")]
        [SerializeField] private Color activeTabColor = new Color(1f, 0.88f, 0.4f, 1f);
        [Tooltip("Pasif tab rengi")]
        [SerializeField] private Color inactiveTabColor = new Color(0.55f, 0.4f, 0.25f, 1f);
        [Tooltip("Aktif tab metin rengi")]
        [SerializeField] private Color activeTabTextColor = new Color(0.15f, 0.08f, 0.02f, 1f);
        [Tooltip("Pasif tab metin rengi")]
        [SerializeField] private Color inactiveTabTextColor = new Color(1f, 0.96f, 0.85f, 1f);

        private int _currentTab = 0; // 0=Hesap, 1=Ses, 2=Credits

        private void Awake()
        {
            Instance = this;
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                UIThemeHelper.ApplyNewRocker(t);
            }
            SetupButtons();
            SetupAudioControls();
            SetupCredits();
        }

        private void OnEnable()
        {
            // Audio event abonelikleri
            AudioManager.OnMusicMuteChanged += OnMusicMuteChanged;
            AudioManager.OnMusicVolumeChanged += OnMusicVolumeChanged;
            AudioManager.OnSFXMuteChanged += OnSFXMuteChanged;
            AudioManager.OnSFXVolumeChanged += OnSFXVolumeChanged;

            // Account event abonelikleri
            if (AccountManager.HasInstance)
            {
                AccountManager.Instance.OnSignedIn += OnAccountSignedIn;
                AccountManager.Instance.OnSignedOut += OnAccountSignedOut;
                AccountManager.Instance.OnError += OnAccountError;
            }

            // Google Auth event abonelikleri
            GoogleAuthManager.OnGoogleSignInSuccess += OnGoogleSignInSuccess;
            GoogleAuthManager.OnGoogleSignInFailed += OnGoogleSignInFailed;

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateAllLocalizedTexts();
            ShowTab(0);
        }

        private void OnDisable()
        {
            AudioManager.OnMusicMuteChanged -= OnMusicMuteChanged;
            AudioManager.OnMusicVolumeChanged -= OnMusicVolumeChanged;
            AudioManager.OnSFXMuteChanged -= OnSFXMuteChanged;
            AudioManager.OnSFXVolumeChanged -= OnSFXVolumeChanged;

            if (AccountManager.HasInstance)
            {
                AccountManager.Instance.OnSignedIn -= OnAccountSignedIn;
                AccountManager.Instance.OnSignedOut -= OnAccountSignedOut;
                AccountManager.Instance.OnError -= OnAccountError;
            }

            GoogleAuthManager.OnGoogleSignInSuccess -= OnGoogleSignInSuccess;
            GoogleAuthManager.OnGoogleSignInFailed -= OnGoogleSignInFailed;

            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateAllLocalizedTexts();
        }

        public void UpdateAllLocalizedTexts()
        {
            if (tabAccountButton != null)
            {
                var t = tabAccountButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                {
                    t.text = LocalizationManager.Get("settings_tab_account");
                    UIThemeHelper.ApplyNewRocker(t);
                }
            }

            if (tabAudioButton != null)
            {
                var t = tabAudioButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                {
                    t.text = LocalizationManager.Get("settings_tab_audio");
                    UIThemeHelper.ApplyNewRocker(t);
                }
            }

            if (tabCreditsButton != null)
            {
                var t = tabCreditsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                {
                    t.text = LocalizationManager.Get("settings_tab_credits");
                    UIThemeHelper.ApplyNewRocker(t);
                }
            }

            RefreshAudioUI();
            RefreshAccountUI();
            SetupCredits();
            UpdateTabStaticTexts();
        }

        private void UpdateTabStaticTexts()
        {
            // 1. Hesap Sekmesi Statik Metinleri
            if (accountTabContent != null)
            {
                Transform headerRow = accountTabContent.transform.Find("HeaderRow");
                if (headerRow != null)
                {
                    var title = headerRow.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    if (title != null) { title.text = LocalizationManager.Get("settings_account_title"); UIThemeHelper.ApplyNewRocker(title); }
                }

                Transform statusCard = accountTabContent.transform.Find("StatusCard");
                if (statusCard != null)
                {
                    var sLabel = statusCard.Find("StatusLabel")?.GetComponent<TextMeshProUGUI>();
                    if (sLabel != null) { sLabel.text = LocalizationManager.Get("settings_status_label"); UIThemeHelper.ApplyNewRocker(sLabel); }

                    var idLabel = statusCard.Find("IdLabel")?.GetComponent<TextMeshProUGUI>();
                    if (idLabel != null) { idLabel.text = LocalizationManager.Get("settings_alchemist_prefix"); UIThemeHelper.ApplyNewRocker(idLabel); }
                }

                if (accountGuestSignInButton != null)
                {
                    var t = accountGuestSignInButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) { t.text = LocalizationManager.Get("settings_guest_btn"); UIThemeHelper.ApplyNewRocker(t); }
                }
                if (accountSignInButton != null)
                {
                    var t = accountSignInButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) { t.text = LocalizationManager.Get("settings_signin_btn"); UIThemeHelper.ApplyNewRocker(t); }
                }
                if (accountRegisterButton != null)
                {
                    var t = accountRegisterButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null)
                    {
                        bool isEn = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
                        t.text = isEn ? "Register (Google)" : "Kayıt Ol (Google)";
                        UIThemeHelper.ApplyNewRocker(t);
                    }
                }
                if (accountSignOutButton != null)
                {
                    var t = accountSignOutButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) { t.text = LocalizationManager.Get("settings_signout_btn"); UIThemeHelper.ApplyNewRocker(t); }
                }

                if (accountUsernameInput != null && accountUsernameInput.placeholder is TextMeshProUGUI uPh)
                {
                    uPh.text = LocalizationManager.Get("settings_username_placeholder");
                    UIThemeHelper.ApplyNewRocker(uPh);
                }
                if (accountPasswordInput != null && accountPasswordInput.placeholder is TextMeshProUGUI pPh)
                {
                    pPh.text = LocalizationManager.Get("settings_password_placeholder");
                    UIThemeHelper.ApplyNewRocker(pPh);
                }
            }

            // 2. Ses Sekmesi Statik Metinleri
            if (audioTabContent != null)
            {
                Transform musicSec = audioTabContent.transform.Find("MusicSection");
                if (musicSec != null)
                {
                    var mTitle = musicSec.Find("TitleRow/Title")?.GetComponent<TextMeshProUGUI>();
                    if (mTitle != null) { mTitle.text = LocalizationManager.Get("settings_music_title"); UIThemeHelper.ApplyNewRocker(mTitle); }
                }

                Transform sfxSec = audioTabContent.transform.Find("SFXSection");
                if (sfxSec != null)
                {
                    var sTitle = sfxSec.Find("TitleRow/Title")?.GetComponent<TextMeshProUGUI>();
                    if (sTitle != null) { sTitle.text = LocalizationManager.Get("settings_sfx_title"); UIThemeHelper.ApplyNewRocker(sTitle); }
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  BUTON BAĞLANTILARI
        // ═══════════════════════════════════════════════════════

        private void SetupButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (tabAccountButton != null)
                tabAccountButton.onClick.AddListener(() => ShowTab(0));
            if (tabAudioButton != null)
                tabAudioButton.onClick.AddListener(() => ShowTab(1));
            if (tabCreditsButton != null)
                tabCreditsButton.onClick.AddListener(() => ShowTab(2));

            // Hesap butonları referans güvenliği
            if (accountTabContent != null)
            {
                Transform btnRow = accountTabContent.transform.Find("ButtonRow");
                if (btnRow != null)
                {
                    if (accountGuestSignInButton == null)
                        accountGuestSignInButton = btnRow.Find("GuestSignInButton")?.GetComponent<Button>();
                    if (accountSignInButton == null)
                        accountSignInButton = btnRow.Find("SignInButton")?.GetComponent<Button>();
                    if (accountRegisterButton == null || accountRegisterButton == accountGoogleSignInButton)
                    {
                        var regBtn = btnRow.Find("RegisterButton")?.GetComponent<Button>();
                        if (regBtn != null)
                        {
                            if (accountGoogleSignInButton == regBtn) accountGoogleSignInButton = null;
                            accountRegisterButton = regBtn;
                        }
                    }
                    if (accountSignOutButton == null)
                        accountSignOutButton = btnRow.Find("SignOutButton")?.GetComponent<Button>();
                }
            }

            if (accountGuestSignInButton != null)
                accountGuestSignInButton.onClick.AddListener(async () =>
                {
                    try
                    {
                        if (AccountManager.Instance != null)
                        {
                            SetAccountMessage(LocalizationManager.Get("settings_guest_signing_in"), Color.yellow);
                            await AccountManager.Instance.SignInAnonymously();
                            RefreshAccountUI();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SettingsMenuUI] Misafir girişi hatası: {ex}");
                    }
                });

            if (accountSignInButton != null)
                accountSignInButton.onClick.AddListener(async () =>
                {
                    try
                    {
                        if (AccountManager.Instance == null) return;
                        string user = accountUsernameInput != null ? accountUsernameInput.text.Trim() : "";
                        string pass = accountPasswordInput != null ? accountPasswordInput.text : "";
                        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                        {
                            SetAccountMessage("Kullanıcı adı ve şifre boş bırakılamaz!", new Color(1f, 0.35f, 0.35f));
                            return;
                        }
                        SetAccountMessage(LocalizationManager.Get("settings_signing_in"), Color.yellow);
                        await AccountManager.Instance.SignInWithUsernamePassword(user, pass);
                        RefreshAccountUI();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SettingsMenuUI] Giriş hatası: {ex}");
                        SetAccountMessage("Giriş yapılamadı!", new Color(1f, 0.35f, 0.35f));
                    }
                });

            if (accountRegisterButton != null)
            {
                accountRegisterButton.onClick.RemoveAllListeners();
                accountRegisterButton.onClick.AddListener(() =>
                {
                    GoogleAuthManager googleAuth = GoogleAuthManager.Instance ?? FindFirstObjectByType<GoogleAuthManager>();
                    if (googleAuth == null)
                    {
                        GameObject go = new GameObject("[GoogleAuthManager]");
                        googleAuth = go.AddComponent<GoogleAuthManager>();
                        DontDestroyOnLoad(go);
                    }
                    SetAccountMessage(LocalizationManager.Get("settings_google_signing_in"), Color.yellow);
                    googleAuth.OnSignInButtonClicked();
                });
            }

            if (accountSignOutButton != null)
                accountSignOutButton.onClick.AddListener(() =>
                {
                    if (AccountManager.Instance != null)
                    {
                        AccountManager.Instance.SignOut();
                        SetAccountMessage(LocalizationManager.Get("settings_signed_out_msg"), Color.gray);
                        RefreshAccountUI();
                    }
                });

            if (accountGoogleSignInButton != null && accountGoogleSignInButton != accountRegisterButton)
                accountGoogleSignInButton.onClick.AddListener(() =>
                {
                    GoogleAuthManager googleAuth = FindFirstObjectByType<GoogleAuthManager>();
                    if (googleAuth != null)
                    {
                        SetAccountMessage(LocalizationManager.Get("settings_google_signing_in"), Color.yellow);
                        googleAuth.OnSignInButtonClicked();
                    }
                    else
                    {
                        Debug.LogError("GoogleAuthManager sahnede bulunamadı! Lütfen sahneye ekleyin.");
                    }
                });
        }

        // ═══════════════════════════════════════════════════════
        //  TAB SİSTEMİ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Sekme gösterir. 0=Hesap, 1=Ses, 2=Credits.
        /// </summary>
        public void ShowTab(int tabIndex)
        {
            _currentTab = tabIndex;

            if (accountTabContent != null) accountTabContent.SetActive(tabIndex == 0);
            if (audioTabContent != null) audioTabContent.SetActive(tabIndex == 1);
            if (creditsTabContent != null) creditsTabContent.SetActive(tabIndex == 2);

            UpdateTabButtonVisuals(tabAccountButton, tabIndex == 0);
            UpdateTabButtonVisuals(tabAudioButton, tabIndex == 1);
            UpdateTabButtonVisuals(tabCreditsButton, tabIndex == 2);

            if (tabIndex == 0) RefreshAccountUI();
            if (tabIndex == 1) RefreshAudioUI();
            if (tabIndex == 2) SetupCredits();
        }

        private void UpdateTabButtonVisuals(Button btn, bool isActive)
        {
            if (btn == null) return;

            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = isActive ? activeTabColor : inactiveTabColor;

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = isActive ? activeTabTextColor : inactiveTabTextColor;
        }

        // ═══════════════════════════════════════════════════════
        //  SES AYARLARI
        // ═══════════════════════════════════════════════════════

        private void SetupAudioControls()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }

            if (musicMuteToggle != null)
                musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggleChanged);

            if (sfxMuteToggle != null)
                sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteToggleChanged);
        }

        private void RefreshAudioUI()
        {
            if (AudioManager.Instance == null) return;

            // Müzik
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            }
            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(!AudioManager.Instance.IsMusicMuted);
            }

            // SFX
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
            }
            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(!AudioManager.Instance.IsSFXMuted);
            }

            UpdateMusicLabel();
            UpdateSFXLabel();
            UpdateToggleVisuals();
        }

        private void UpdateToggleVisuals()
        {
            if (AudioManager.Instance == null) return;

            if (musicToggleImage != null && toggleOnSprite != null && toggleOffSprite != null)
            {
                bool isOn = !AudioManager.Instance.IsMusicMuted;
                musicToggleImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
            }

            if (sfxToggleImage != null && toggleOnSprite != null && toggleOffSprite != null)
            {
                bool isOn = !AudioManager.Instance.IsSFXMuted;
                sfxToggleImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
            }
        }

        private void OnMusicSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
            UpdateMusicLabel();
        }

        private void OnSFXSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
            UpdateSFXLabel();
        }

        private void OnMusicMuteToggleChanged(bool isOn)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicMute(!isOn); // isOn=true → müzik açık
            UpdateMusicLabel();
            UpdateToggleVisuals();
        }

        private void OnSFXMuteToggleChanged(bool isOn)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXMute(!isOn);
            UpdateSFXLabel();
            UpdateToggleVisuals();
        }

        private void OnMusicMuteChanged(bool isMuted)
        {
            if (musicMuteToggle != null)
                musicMuteToggle.SetIsOnWithoutNotify(!isMuted);
            UpdateMusicLabel();
            UpdateToggleVisuals();
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(volume);
            UpdateMusicLabel();
        }

        private void OnSFXMuteChanged(bool isMuted)
        {
            if (sfxMuteToggle != null)
                sfxMuteToggle.SetIsOnWithoutNotify(!isMuted);
            UpdateSFXLabel();
            UpdateToggleVisuals();
        }

        private void OnSFXVolumeChanged(float volume)
        {
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(volume);
            UpdateSFXLabel();
        }

        private void UpdateMusicLabel()
        {
            if (musicVolumeLabel == null) return;
            string musicName = LocalizationManager.Get("settings_music");
            if (AudioManager.Instance == null)
            {
                musicVolumeLabel.text = $"{musicName}: ---";
                return;
            }
            bool muted = AudioManager.Instance.IsMusicMuted;
            float vol = AudioManager.Instance.MusicVolume;
            string offName = LocalizationManager.Get("settings_off");
            bool isEn = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            int pct = Mathf.RoundToInt(vol * 100);
            musicVolumeLabel.text = muted ? $"{musicName}: {offName}" : (isEn ? $"{musicName}: {pct}%" : $"{musicName}: %{pct}");
        }

        private void UpdateSFXLabel()
        {
            if (sfxVolumeLabel == null) return;
            string sfxName = LocalizationManager.Get("settings_sfx");
            if (AudioManager.Instance == null)
            {
                sfxVolumeLabel.text = $"{sfxName}: ---";
                return;
            }
            bool muted = AudioManager.Instance.IsSFXMuted;
            float vol = AudioManager.Instance.SFXVolume;
            string offName = LocalizationManager.Get("settings_off");
            bool isEn = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            int pct = Mathf.RoundToInt(vol * 100);
            sfxVolumeLabel.text = muted ? $"{sfxName}: {offName}" : (isEn ? $"{sfxName}: {pct}%" : $"{sfxName}: %{pct}");
        }

        // ═══════════════════════════════════════════════════════
        //  HESAP YÖNETİMİ
        // ═══════════════════════════════════════════════════════

        private void RefreshAccountUI()
        {
            bool isSignedIn = AccountManager.Instance != null && AccountManager.Instance.IsSignedIn;
            string playerId = AccountManager.Instance != null ? AccountManager.Instance.PlayerId : null;

            if (accountStatusText != null)
            {
                accountStatusText.text = isSignedIn ? LocalizationManager.Get("settings_connected") : LocalizationManager.Get("settings_guest");
                accountStatusText.color = isSignedIn ? new Color(0.3f, 0.95f, 0.55f) : new Color(1f, 0.75f, 0.25f);
            }

            if (accountPlayerIdText != null)
            {
                if (isSignedIn && !string.IsNullOrEmpty(playerId))
                {
                    string shortId = playerId.Length > 8 ? playerId.Substring(0, 8) : playerId;
                    accountPlayerIdText.text = $"{LocalizationManager.Get("settings_alchemist_prefix")} #{shortId}";
                }
                else
                {
                    accountPlayerIdText.text = LocalizationManager.Get("settings_not_signed_in");
                }
            }

            // Buton görünürlükleri
            if (accountGuestSignInButton != null) accountGuestSignInButton.gameObject.SetActive(!isSignedIn);
            if (accountSignInButton != null) accountSignInButton.gameObject.SetActive(!isSignedIn);
            if (accountGoogleSignInButton != null) accountGoogleSignInButton.gameObject.SetActive(!isSignedIn);
            if (accountRegisterButton != null) accountRegisterButton.gameObject.SetActive(!isSignedIn);
            if (accountSignOutButton != null) accountSignOutButton.gameObject.SetActive(isSignedIn);
            if (accountUsernameInput != null) accountUsernameInput.gameObject.SetActive(!isSignedIn);
            if (accountPasswordInput != null) accountPasswordInput.gameObject.SetActive(!isSignedIn);
        }

        private void SetAccountMessage(string msg, Color color)
        {
            if (accountMessageText != null)
            {
                accountMessageText.text = msg;
                accountMessageText.color = color;
            }
        }

        private void OnAccountSignedIn(string playerId)
        {
            SetAccountMessage(string.Format(LocalizationManager.Get("settings_signed_in_id"), playerId), new Color(0.3f, 0.95f, 0.55f));
            RefreshAccountUI();
        }

        private void OnAccountSignedOut()
        {
            SetAccountMessage(LocalizationManager.Get("settings_signed_out_msg"), Color.gray);
            RefreshAccountUI();
        }

        private void OnAccountError(string error)
        {
            SetAccountMessage($"Hata: {error}", new Color(1f, 0.35f, 0.35f));
        }

        private void OnGoogleSignInSuccess(string userNameOrId)
        {
            SetAccountMessage($"Google ile başarıyla bağlanıldı: {userNameOrId}", new Color(0.3f, 0.95f, 0.55f));
            if (accountStatusText != null)
            {
                accountStatusText.text = "Bağlandı (Google)";
                accountStatusText.color = new Color(0.3f, 0.95f, 0.55f);
            }
            if (accountPlayerIdText != null)
            {
                accountPlayerIdText.text = $"{LocalizationManager.Get("settings_alchemist_prefix")}: {userNameOrId}";
            }
            if (accountRegisterButton != null) accountRegisterButton.gameObject.SetActive(false);
            if (accountGoogleSignInButton != null) accountGoogleSignInButton.gameObject.SetActive(false);
            if (accountSignInButton != null) accountSignInButton.gameObject.SetActive(false);
            if (accountGuestSignInButton != null) accountGuestSignInButton.gameObject.SetActive(false);
            if (accountSignOutButton != null) accountSignOutButton.gameObject.SetActive(true);
        }

        private void OnGoogleSignInFailed(string error)
        {
            SetAccountMessage($"Google Giriş Hatası: {error}", new Color(1f, 0.35f, 0.35f));
        }

        // ═══════════════════════════════════════════════════════
        //  CREDITS
        // ═══════════════════════════════════════════════════════

        public void SetupCredits()
        {
            if (creditsText == null) return;

            if (!string.IsNullOrWhiteSpace(customCreditsText))
            {
                creditsText.text = customCreditsText;
                return;
            }

            creditsText.text = BuildDefaultCreditsString();
        }

        private string BuildDefaultCreditsString()
        {
            int titlePct = Mathf.RoundToInt(titleSizeMultiplier * 100f);
            int subPct = Mathf.RoundToInt(subtitleSizeMultiplier * 100f);
            int headerPct = Mathf.RoundToInt(sectionHeaderSizeMultiplier * 100f);
            int namePct = Mathf.RoundToInt(nameSizeMultiplier * 100f);
            int footerPct = Mathf.RoundToInt(footerSizeMultiplier * 100f);

            string titleColorTag = useAccentColors ? $"<color=#{ColorUtility.ToHtmlStringRGB(titleColor)}>" : "";
            string titleColorEnd = useAccentColors ? "</color>" : "";

            string headerColorTag = useAccentColors ? $"<color=#{ColorUtility.ToHtmlStringRGB(sectionHeaderColor)}>" : "";
            string headerColorEnd = useAccentColors ? "</color>" : "";

            string name1ColorTag = useAccentColors ? $"<color=#{ColorUtility.ToHtmlStringRGB(name1Color)}>" : "";
            string name1ColorEnd = useAccentColors ? "</color>" : "";

            string name2ColorTag = useAccentColors ? $"<color=#{ColorUtility.ToHtmlStringRGB(name2Color)}>" : "";
            string name2ColorEnd = useAccentColors ? "</color>" : "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool isEn = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

            // 1. Ana Başlık & Alt Başlık
            string subtitle = isEn ? "An Alchemist's Tale" : "Bir Simyacinin Hikayesi";
            sb.Append($"<size={titlePct}%><b>{titleColorTag}POTIONTOWN{titleColorEnd}</b></size>\n");
            sb.Append($"<size={subPct}%><alpha=#CC>{subtitle}</alpha></size>\n\n");

            // 2. Yapımcılar
            string prodHeader = isEn ? "[ PRODUCERS ]" : "[ YAPIMCILAR ]";
            sb.Append($"<size={headerPct}%><b>{headerColorTag}{prodHeader}{headerColorEnd}</b></size>\n\n");

            // Enes (MrNSLightning)
            sb.Append($"<size={namePct}%><b>{name1ColorTag}Enes (MrNSLightning){name1ColorEnd}</b></size>\n");
            if (isEn)
            {
                sb.Append("Project Lead & Lead Developer\n");
                sb.Append("Game Architecture & System Design\n");
                sb.Append("Potion Crafting & Selling Systems\n");
                sb.Append("Inventory & Storage Management\n");
                sb.Append("License & Level System\n");
                sb.Append("TopDropdown Menu & UI Architecture\n");
                sb.Append("Lobby & Town Design\n");
                sb.Append("Minigames & Reward Systems\n");
                sb.Append("Monetization & Store\n");
                sb.Append("Potion Passport System\n");
                sb.Append("Project Organization & Configuration\n");
                sb.Append("Editor Tools & Automations\n\n");
            }
            else
            {
                sb.Append("Proje Yoneticisi & Bas Gelistirici\n");
                sb.Append("Oyun Mimarisi & Sistem Tasarimi\n");
                sb.Append("Iksir Yapim & Satis Sistemleri\n");
                sb.Append("Envanter & Depo Yonetimi\n");
                sb.Append("Lisans & Seviye Sistemi\n");
                sb.Append("TopDropdown Menu & UI Mimarisi\n");
                sb.Append("Lobi & Sehir Tasarimi\n");
                sb.Append("Mini Oyunlar & Odul Sistemleri\n");
                sb.Append("Monetizasyon & Magaza\n");
                sb.Append("Iksir Pasaportu Sistemi\n");
                sb.Append("Proje Organizasyonu & Yapilandirma\n");
                sb.Append("Editor Araclari & Otomasyonlar\n\n");
            }

            // Utku Mustafa Ercan
            sb.Append($"<size={namePct}%><b>{name2ColorTag}Utku Mustafa Ercan{name2ColorEnd}</b></size>\n");
            if (isEn)
            {
                sb.Append("Customer & Order System\n");
                sb.Append("Audio & Music System\n");
                sb.Append("Mobile Optimization\n");
                sb.Append("Account Management & Cloud Services\n");
                sb.Append("Ads & Rewarded Video Integration\n");
                sb.Append("Touch Input & Screen Settings\n\n");
            }
            else
            {
                sb.Append("Musteri & Siparis Sistemi\n");
                sb.Append("Ses & Muzik Sistemi\n");
                sb.Append("Mobil Optimizasyon\n");
                sb.Append("Hesap Yonetimi & Bulut Servisleri\n");
                sb.Append("Reklam & Odullu Video Entegrasyonu\n");
                sb.Append("Dokunmatik Giris & Ekran Ayarlari\n\n");
            }

            // 3. Özel Teşekkürler
            string thanksHeader = isEn ? "[ SPECIAL THANKS ]" : "[ OZEL TESEKKURLER ]";
            sb.Append($"<size={headerPct}%><b>{headerColorTag}{thanksHeader}{headerColorEnd}</b></size>\n\n");
            if (isEn)
            {
                sb.Append("Unity Technologies - Game Engine\n");
                sb.Append("RPG Consumables & Potions Icons Pack\n");
                sb.Append("TextMeshPro - Advanced Text System\n");
                sb.Append("All open-source communities and\n");
                sb.Append("all alchemists who inspired this project!\n\n");
            }
            else
            {
                sb.Append("Unity Technologies - Game Engine\n");
                sb.Append("RPG Consumables & Potions Icons Pack\n");
                sb.Append("TextMeshPro - Gelismis Metin Sistemi\n");
                sb.Append("Tum acik kaynak topluluklari ve\n");
                sb.Append("bu projeye ilham veren tum simyacilar!\n\n");
            }

            // 4. Alt Bilgi (Telif & Versiyon)
            sb.Append($"<size={footerPct}%><alpha=#80>");
            sb.Append("-----------------------------------------\n");
            sb.Append("(c) 2024-2026 EUGames\n");
            sb.Append(isEn ? "All rights reserved.\n" : "Tum haklari saklidir.\n");
            sb.Append("PotionTown v1.0\n");
            sb.Append(isEn ? "Developed with Unity URP 2D.\n" : "Unity URP 2D ile gelistirilmistir.\n");
            sb.Append("-----------------------------------------");
            sb.Append("</alpha></size>");

            return sb.ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (creditsText != null)
            {
                SetupCredits();
            }
        }
#endif

        // ═══════════════════════════════════════════════════════
        //  AÇ / KAPAT
        // ═══════════════════════════════════════════════════════

        public bool IsOpen => (rootPanel != null ? rootPanel.activeInHierarchy : gameObject.activeInHierarchy);

        public void Open()
        {
            if (rootPanel != null)
                rootPanel.SetActive(true);
            gameObject.SetActive(true);
            RefreshAudioUI();
            RefreshAccountUI();
            ShowTab(0);
        }

        public void Close()
        {
            if (rootPanel != null)
                rootPanel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
