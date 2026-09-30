using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_SERVICES_CORE
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif

namespace PotionShop
{
    /// <summary>
    /// Unity Authentication servisleri ile oyuncu hesabı yönetimini sağlar.
    /// UNITY_SERVICES_CORE tanımlıysa gerçek işlemleri yapar, yoksa test değerleri döndürür.
    /// </summary>
    public class AccountManager : MonoBehaviour
    {
        private static AccountManager _instance;
        public static AccountManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AccountManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[AccountManager]");
                        _instance = go.AddComponent<AccountManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        public string PlayerId
        {
            get
            {
#if UNITY_SERVICES_CORE
                return IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
#else
                return IsSignedIn ? "fake_test_player_id_123" : null;
#endif
            }
        }

        public bool IsSignedIn
        {
            get
            {
#if UNITY_SERVICES_CORE
                return AuthenticationService.Instance.IsSignedIn;
#else
                return isFakeSignedIn;
#endif
            }
        }

        public event Action<string> OnSignedIn;
        public event Action OnSignedOut;
        public event Action<string> OnError;

#if !UNITY_SERVICES_CORE
        private bool isFakeSignedIn = false;
#endif

        private async void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                await InitializeAsync();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeAsync()
        {
#if UNITY_SERVICES_CORE
            try
            {
                await UnityServices.InitializeAsync();
                SetupEvents();
                await SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Unity Services başlatılamadı: {e.Message}");
                OnError?.Invoke(e.Message);
            }
#else
            Debug.LogWarning("UNITY_SERVICES_CORE tanımlı değil. Fake account başlatılıyor...");
            await Task.Delay(500);
            await SignInAnonymously(); // Test için anonim giriş simülasyonu
#endif
        }

#if UNITY_SERVICES_CORE
        private void SetupEvents()
        {
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"Giriş yapıldı! Player ID: {AuthenticationService.Instance.PlayerId}");
                OnSignedIn?.Invoke(AuthenticationService.Instance.PlayerId);
            };

            AuthenticationService.Instance.SignedOut += () =>
            {
                Debug.Log("Çıkış yapıldı.");
                OnSignedOut?.Invoke();
            };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError($"Giriş başarısız: {err}");
                OnError?.Invoke(err.ToString());
            };
        }
#endif

        public async Task SignInAnonymously()
        {
#if UNITY_SERVICES_CORE
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Anonim giriş hatası: {ex}");
                OnError?.Invoke(ex.Message);
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"İstek başarısız: {ex}");
                OnError?.Invoke(ex.Message);
            }
#else
            isFakeSignedIn = true;
            Debug.Log($"Sahte anonim giriş yapıldı. PlayerID: {PlayerId}");
            OnSignedIn?.Invoke(PlayerId);
            await Task.CompletedTask;
#endif
        }
        
        // Yardımcı asenkron çağrı versiyonu (Awake ve diğer iç çağrılar için)
        private async Task SignInAnonymouslyAsync()
        {
            await SignInAnonymously();
        }

        public async Task SignInWithUsernamePassword(string username, string password)
        {
#if UNITY_SERVICES_CORE
            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
                Debug.Log("Kullanıcı adı/şifre ile giriş yapıldı.");
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Giriş hatası: {ex}");
                OnError?.Invoke(ex.Message);
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"İstek başarısız: {ex}");
                OnError?.Invoke(ex.Message);
            }
#else
            isFakeSignedIn = true;
            Debug.Log($"Sahte kullanıcı girişi yapıldı: {username}");
            OnSignedIn?.Invoke(PlayerId);
            await Task.CompletedTask;
#endif
        }

        public async Task CreateAccount(string username, string password)
        {
#if UNITY_SERVICES_CORE
            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                Debug.Log("Hesap başarıyla oluşturuldu.");
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Hesap oluşturma hatası: {ex}");
                OnError?.Invoke(ex.Message);
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"İstek başarısız: {ex}");
                OnError?.Invoke(ex.Message);
            }
#else
            isFakeSignedIn = true;
            Debug.Log($"Sahte hesap oluşturuldu: {username}");
            OnSignedIn?.Invoke(PlayerId);
            await Task.CompletedTask;
#endif
        }

        public void SignOut()
        {
#if UNITY_SERVICES_CORE
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
#else
            isFakeSignedIn = false;
            Debug.Log("Sahte hesaptan çıkış yapıldı.");
            OnSignedOut?.Invoke();
#endif
        }
    }
}
