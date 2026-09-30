using UnityEngine;
using Firebase.Auth;
using Google;
using System.Threading.Tasks;
using System;

public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }

    public static event Action<string> OnGoogleSignInSuccess;
#pragma warning disable 0067
    public static event Action<string> OnGoogleSignInFailed;
#pragma warning restore 0067

    private FirebaseAuth auth;

    // BURAYA KOPYALADIĞINIZ WEB CLIENT ID'Yİ YAPIŞTIRIN
    public string webClientId = "455877717706-mibkrvmhmd77a4ks8ujsfnv15h9obtai.apps.googleusercontent.com";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        try
        {
            // Firebase'i başlat
            auth = FirebaseAuth.DefaultInstance;

            // Google Sign-In ayarlarını yapılandır
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true, // Firebase'e bağlanmak için bilet istiyoruz
                RequestEmail = true,   // Kullanıcının e-postasını istiyoruz
                WebClientId = webClientId
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoogleAuthManager] Firebase/GoogleSignIn başlatılırken hata oluştu: {ex.Message}");
        }
#else
        Debug.Log("[GoogleAuthManager] Editör / Masaüstü ortamında Google Sign-In ve Firebase Auth başlatması atlandı (Editör simülasyonu devrede).");
#endif
    }

    // Bu fonksiyonu UI'daki butona bağlıyoruz
    public async void OnSignInButtonClicked()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Debug.Log("Google Giriş penceresi açılıyor...");
        
        try
        {
            GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            Debug.Log("Google Girişi Başarılı! Hoş geldin: " + googleUser.DisplayName);

            // Şimdi Firebase hesabına bağlıyoruz
            await SignInWithFirebase(googleUser.IdToken, googleUser.DisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogError("Google Girişi başarısız oldu: " + ex.Message);
            OnGoogleSignInFailed?.Invoke(ex.Message);
        }
#else
        Debug.Log("[GoogleAuthManager] Editör Simülasyonu: Google hesabı ile giriş yapılıyor...");
        await Task.Delay(500);
        string mockUserName = "Simyaci_Google#1001";
        Debug.Log($"[GoogleAuthManager] Editör Simülasyonu Başarılı! Hoş geldin: {mockUserName}");
        OnGoogleSignInSuccess?.Invoke(mockUserName);
#endif
    }

    private async Task SignInWithFirebase(string idToken, string displayName)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        try
        {
            if (auth == null) auth = FirebaseAuth.DefaultInstance;
            FirebaseUser newUser = await auth.SignInWithCredentialAsync(credential);
            Debug.Log("Harika! Firebase girişi tamamlandı. Firebase Kullanıcı ID: " + newUser.UserId);
            string finalName = !string.IsNullOrEmpty(newUser.DisplayName) ? newUser.DisplayName : (!string.IsNullOrEmpty(displayName) ? displayName : newUser.UserId);
            OnGoogleSignInSuccess?.Invoke(finalName);
        }
        catch (Exception ex)
        {
            Debug.LogError("Firebase'e bağlanılamadı: " + ex.Message);
            OnGoogleSignInFailed?.Invoke(ex.Message);
        }
#else
        await Task.CompletedTask;
#endif
    }
}
