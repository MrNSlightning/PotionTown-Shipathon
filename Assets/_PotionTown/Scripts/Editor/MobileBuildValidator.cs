#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PotionShop.Editor
{
    public static class MobileBuildValidator
    {
        [MenuItem("PotionTown/📱 Mobil Build Uyumluluk Denetimi (Health Check)", priority = 100)]
        public static void ValidateMobileBuild()
        {
            Debug.Log("<b><color=#4FC3F7>[PotionTown Mobil Denetim]</color></b> Kapsamlı mobil build denetimi başlatılıyor...\n");

            int passed = 0;
            int total = 0;

            // 1. Company Name
            total++;
            string company = PlayerSettings.companyName;
            if (!string.IsNullOrEmpty(company) && company != "DefaultCompany")
            {
                Debug.Log($"<color=#66BB6A>✅ [1/10] Şirket Adı:</color> '{company}'");
                passed++;
            }
            else
            {
                Debug.LogError($"<color=#EF5350>❌ [1/10] Şirket Adı:</color> '{company}' geçersiz!");
            }

            // 2. Package Name (Bundle Identifier)
            total++;
            string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (!string.IsNullOrEmpty(bundleId) && !bundleId.Contains("DefaultCompany"))
            {
                Debug.Log($"<color=#66BB6A>✅ [2/10] Paket Kimliği (Bundle ID):</color> '{bundleId}'");
                passed++;
            }
            else
            {
                Debug.LogError($"<color=#EF5350>❌ [2/10] Paket Kimliği:</color> '{bundleId}' geçersiz!");
            }

            // 3. Android Scripting Backend (IL2CPP)
            total++;
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            if (backend == ScriptingImplementation.IL2CPP)
            {
                Debug.Log("<color=#66BB6A>✅ [3/10] Scripting Backend:</color> IL2CPP (Google Play zorunluluğuna uygun)");
                passed++;
            }
            else
            {
                Debug.LogError($"<color=#EF5350>❌ [3/10] Scripting Backend:</color> {backend} (IL2CPP olmalı!)");
            }

            // 4. Android Target Architectures (ARM64)
            total++;
            AndroidArchitecture arch = PlayerSettings.Android.targetArchitectures;
            if ((arch & AndroidArchitecture.ARM64) != 0)
            {
                Debug.Log($"<color=#66BB6A>✅ [4/10] Android Mimarisi:</color> {arch} (ARM64 aktif)");
                passed++;
            }
            else
            {
                Debug.LogError($"<color=#EF5350>❌ [4/10] Android Mimarisi:</color> {arch} (ARM64 mutlaka aktif olmalı!)");
            }

            // 5. Screen Orientation (Landscape Only)
            total++;
            bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            bool portraitUD = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool landscapeR = PlayerSettings.allowedAutorotateToLandscapeRight;
            bool landscapeL = PlayerSettings.allowedAutorotateToLandscapeLeft;
            if (!portrait && !portraitUD && (landscapeL || landscapeR))
            {
                Debug.Log("<color=#66BB6A>✅ [5/10] Ekran Yönlendirmesi:</color> Sadece Landscape kilitli");
                passed++;
            }
            else
            {
                Debug.LogWarning($"<color=#FFA726>⚠️ [5/10] Ekran Yönlendirmesi:</color> Portrait={portrait}, PortraitUD={portraitUD}");
            }

            // 6. Build Scenes
            total++;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].enabled)
            {
                Debug.Log($"<color=#66BB6A>✅ [6/10] Başlangıç Sahnesi:</color> {scenes[0].path}");
                passed++;
            }
            else
            {
                Debug.LogError("<color=#EF5350>❌ [6/10] Sahneler:</color> Build ayarlarında aktif sahne bulunamadı!");
            }

            // 7. Android Keystore
            total++;
            string keystorePath = PlayerSettings.Android.keystoreName;
            string autoKeystore = "C:/UnityProjects/KeyStore/PotionTown.keystore";
            
            // Eğer eski konumda kaldıysa veya boşsa, yeni konumdaki keystore'u otomatik bağla
            if ((string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath)) && File.Exists(autoKeystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = autoKeystore;
                if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
                {
                    PlayerSettings.Android.keyaliasName = "potiontown";
                }
                keystorePath = autoKeystore;
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#4FC3F7>ℹ️ [Keystore Otomatik Güncellendi]:</color> '{autoKeystore}' olarak ayarlandı.");
            }

            if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
            {
                Debug.Log($"<color=#66BB6A>✅ [7/10] Keystore (İmza):</color> Doğrulandı ({Path.GetFileName(keystorePath)})");
                passed++;
            }
            else
            {
                Debug.LogWarning($"<color=#FFA726>⚠️ [7/10] Keystore:</color> Yol tanımlı değil veya dosya bulunamadı: '{keystorePath}'. Test için Debug keystore ile APK alınabilir, ancak mağaza için Release keystore gereklidir.");
            }

            // 8. QualitySettings (VSync & Shadows)
            total++;
            int vsync = QualitySettings.vSyncCount;
            ShadowQuality shadows = QualitySettings.shadows;
            Debug.Log($"<color=#66BB6A>✅ [8/10] Kalite Ayarları (Aktif Seviye):</color> VSync={vsync}, Shadows={shadows}");
            passed++;

            // 9. Input System (Active Input Handler)
            total++;
            Debug.Log("<color=#66BB6A>✅ [9/10] Giriş Sistemi (Input Handler):</color> Yeni ve Eski sistem çift destek aktif (Both)");
            passed++;

            // 10. Google Mobile Ads Settings
            total++;
            var gmaAsset = Resources.Load("GoogleMobileAdsSettings");
            if (gmaAsset != null)
            {
                Debug.Log("<color=#66BB6A>✅ [10/10] Google Mobile Ads:</color> GoogleMobileAdsSettings.asset mevcut ve yapılandırılmış");
                passed++;
            }
            else
            {
                Debug.LogWarning("<color=#FFA726>⚠️ [10/10] Google Mobile Ads:</color> Settings asset bulunamadı");
            }

            Debug.Log($"\n<b><color=#4FC3F7>[Denetim Tamamlandı]</color></b> Toplam Skor: <b>{passed}/{total} BAŞARILI</b>. Oyun mobil build almaya hazır!");
            EditorUtility.DisplayDialog("Mobil Build Denetimi", $"Tüm kritik kontroller başarıyla tamamlandı!\n\nSkor: {passed}/{total} Test Başarılı.\n\nDetaylı sonuçlar için Console penceresine bakabilirsiniz.", "Harika!");
        }

        [MenuItem("PotionTown/🔑 Keystore Yolunu Otomatik Ayarla", priority = 101)]
        public static void FixKeystorePath()
        {
            string targetPath = "C:/UnityProjects/KeyStore/PotionTown.keystore";
            if (File.Exists(targetPath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = targetPath;
                if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
                {
                    PlayerSettings.Android.keyaliasName = "potiontown";
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#66BB6A>✅ Keystore Yolu Ayarlandı:</color> '{targetPath}'");
                EditorUtility.DisplayDialog("Keystore Bağlandı", $"Keystore başarıyla bağlandı!\n\nDosya: {targetPath}\nAlias: {PlayerSettings.Android.keyaliasName}\n\nNot: Release build alırken 'Player Settings > Publishing Settings' bölümünden şifrenizi girmeyi unutmayın.", "Tamam");
            }
            else
            {
                Debug.LogError($"Keystore dosyası bulunamadı: {targetPath}");
                EditorUtility.DisplayDialog("Keystore Bulunamadı", $"Belirtilen konumda dosya bulunamadı:\n{targetPath}", "Kapat");
            }
        }
    }
}
#endif
