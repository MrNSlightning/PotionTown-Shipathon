using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace PotionShop
{
    /// <summary>
    /// Oyuncu profil basligi ve acilir hesap yonetimi penceresi.
    /// AccountManager ile entegre calisir. Misafir veya kullanici adi/sifre ile giris imkani sunar.
    /// </summary>
    public class AccountUI : MonoBehaviour
    {
        public static AccountUI Instance { get; private set; }

        [Header("Profil Baslik Widget'i")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI accountStatusBadge;
        public Button openAccountModalButton;

        [Header("Hesap Modali")]
        public GameObject accountModalPanel;
        public Button closeModalButton;
        public TextMeshProUGUI modalTitleText;
        public TextMeshProUGUI modalPlayerIdText;
        public TextMeshProUGUI modalStatusMessageText;

        [Header("Giris / Kayit Formu")]
        public TMP_InputField usernameInput;
        public TMP_InputField passwordInput;
        public Button guestSignInButton;
        public Button signInButton;
        public Button registerButton;
        public Button signOutButton;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            SetupButtons();
        }

        private void Start()
        {
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.OnSignedIn += HandleSignedIn;
                AccountManager.Instance.OnSignedOut += HandleSignedOut;
                AccountManager.Instance.OnError += HandleError;
            }

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            RefreshProfileUI();
            if (accountModalPanel != null)
                accountModalPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.OnSignedIn -= HandleSignedIn;
                AccountManager.Instance.OnSignedOut -= HandleSignedOut;
                AccountManager.Instance.OnError -= HandleError;
            }

            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            RefreshProfileUI();
            RefreshModalUI();
        }

        private void SetupButtons()
        {
            if (openAccountModalButton != null)
                openAccountModalButton.onClick.AddListener(OpenModal);

            if (closeModalButton != null)
                closeModalButton.onClick.AddListener(CloseModal);

            if (guestSignInButton != null)
                guestSignInButton.onClick.AddListener(async () => await OnGuestSignInClicked());

            if (signInButton != null)
                signInButton.onClick.AddListener(async () => await OnSignInClicked());

            if (registerButton != null)
                registerButton.onClick.AddListener(async () => await OnRegisterClicked());

            if (signOutButton != null)
                signOutButton.onClick.AddListener(OnSignOutClicked);
        }

        public void OpenModal()
        {
            if (accountModalPanel != null)
            {
                accountModalPanel.SetActive(true);
                RefreshModalUI();
            }
        }

        public void CloseModal()
        {
            if (accountModalPanel != null)
            {
                accountModalPanel.SetActive(false);
            }
        }

        public void RefreshProfileUI()
        {
            bool isSignedIn = AccountManager.Instance != null && AccountManager.Instance.IsSignedIn;
            string playerId = AccountManager.Instance != null ? AccountManager.Instance.PlayerId : null;

            if (playerNameText != null)
            {
                if (isSignedIn && !string.IsNullOrEmpty(playerId))
                {
                    string shortId = playerId.Length > 8 ? playerId.Substring(0, 8) : playerId;
                    playerNameText.text = $"{LocalizationManager.Get("settings_alchemist_prefix")} #{shortId}";
                }
                else
                {
                    playerNameText.text = LocalizationManager.Get("settings_not_signed_in");
                }
            }

            if (accountStatusBadge != null)
            {
                accountStatusBadge.text = isSignedIn ? $"[{LocalizationManager.Get("settings_connected")}]" : $"[{LocalizationManager.Get("settings_guest")}]";
                accountStatusBadge.color = isSignedIn ? new Color(0.3f, 0.95f, 0.55f) : new Color(1f, 0.75f, 0.25f);
            }
        }

        public void RefreshModalUI()
        {
            bool isSignedIn = AccountManager.Instance != null && AccountManager.Instance.IsSignedIn;
            string playerId = AccountManager.Instance != null ? AccountManager.Instance.PlayerId : "-";

            if (modalPlayerIdText != null)
            {
                modalPlayerIdText.text = $"{LocalizationManager.Get("account_player_id")}: {playerId}";
            }

            if (modalStatusMessageText != null)
            {
                modalStatusMessageText.text = isSignedIn 
                    ? LocalizationManager.Get("account_cloud_connected")
                    : LocalizationManager.Get("account_cloud_disconnected");
                modalStatusMessageText.color = isSignedIn ? new Color(0.3f, 0.95f, 0.55f) : new Color(0.85f, 0.85f, 0.9f);
            }

            if (guestSignInButton != null)
            {
                guestSignInButton.gameObject.SetActive(!isSignedIn);
                var t = guestSignInButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = LocalizationManager.Get("settings_guest_btn"); UIThemeHelper.ApplyNewRocker(t); }
            }
            if (signInButton != null)
            {
                signInButton.gameObject.SetActive(!isSignedIn);
                var t = signInButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = LocalizationManager.Get("settings_signin_btn"); UIThemeHelper.ApplyNewRocker(t); }
            }
            if (registerButton != null)
            {
                registerButton.gameObject.SetActive(!isSignedIn);
                var t = registerButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = LocalizationManager.Get("settings_register_btn"); UIThemeHelper.ApplyNewRocker(t); }
            }
            if (signOutButton != null)
            {
                signOutButton.gameObject.SetActive(isSignedIn);
                var t = signOutButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) { t.text = LocalizationManager.Get("settings_signout_btn"); UIThemeHelper.ApplyNewRocker(t); }
            }
            if (usernameInput != null && usernameInput.placeholder is TextMeshProUGUI uPh)
            {
                uPh.text = LocalizationManager.Get("settings_username_placeholder");
                UIThemeHelper.ApplyNewRocker(uPh);
            }
            if (passwordInput != null && passwordInput.placeholder is TextMeshProUGUI pPh)
            {
                pPh.text = LocalizationManager.Get("settings_password_placeholder");
                UIThemeHelper.ApplyNewRocker(pPh);
            }
        }

        private async Task OnGuestSignInClicked()
        {
            if (AccountManager.Instance == null) return;
            SetStatus(LocalizationManager.Get("account_signing_in_guest"), Color.yellow);
            await AccountManager.Instance.SignInAnonymously();
        }

        private async Task OnSignInClicked()
        {
            if (AccountManager.Instance == null) return;
            string user = usernameInput != null ? usernameInput.text.Trim() : "";
            string pass = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                SetStatus(LocalizationManager.Get("account_empty_fields"), new Color(1f, 0.35f, 0.35f));
                return;
            }

            SetStatus(LocalizationManager.Get("account_signing_in"), Color.yellow);
            await AccountManager.Instance.SignInWithUsernamePassword(user, pass);
        }

        private async Task OnRegisterClicked()
        {
            if (AccountManager.Instance == null) return;
            string user = usernameInput != null ? usernameInput.text.Trim() : "";
            string pass = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(user) || pass.Length < 6)
            {
                SetStatus(LocalizationManager.Get("account_password_short"), new Color(1f, 0.35f, 0.35f));
                return;
            }

            SetStatus(LocalizationManager.Get("account_creating"), Color.yellow);
            await AccountManager.Instance.CreateAccount(user, pass);
        }

        private void OnSignOutClicked()
        {
            if (AccountManager.Instance == null) return;
            AccountManager.Instance.SignOut();
            SetStatus(LocalizationManager.Get("account_signed_out"), Color.gray);
        }

        private void HandleSignedIn(string playerId)
        {
            SetStatus(string.Format(LocalizationManager.Get("account_sign_in_success"), playerId), new Color(0.3f, 0.95f, 0.55f));
            RefreshProfileUI();
            RefreshModalUI();
        }

        private void HandleSignedOut()
        {
            SetStatus(LocalizationManager.Get("account_signed_out"), Color.gray);
            RefreshProfileUI();
            RefreshModalUI();
        }

        private void HandleError(string error)
        {
            SetStatus($"Hata: {error}", new Color(1f, 0.35f, 0.35f));
        }

        private void SetStatus(string message, Color color)
        {
            if (modalStatusMessageText != null)
            {
                modalStatusMessageText.text = message;
                modalStatusMessageText.color = color;
            }
        }
    }
}