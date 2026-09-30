#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// RevenueCat entegrasyonunu doğrulayan ve kurulum adımlarını gösteren Editor penceresi.
    /// Shipaton 2026 başvurusu için gerekli tüm adımları kontrol eder.
    /// </summary>
    public class RevenueCatSetupWizard : EditorWindow
    {
        private Vector2 scrollPos;
        private bool rcPackageInstalled;
        private bool rcDefineSet;
        private bool rcApiKeySet;
        private bool rcStoreModeSet;

        [MenuItem("PotionTown/RevenueCat Kurulum Sihirbazı", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<RevenueCatSetupWizard>("RevenueCat Kurulumu");
            window.minSize = new Vector2(480, 520);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshChecks();
        }

        private void RefreshChecks()
        {
            // 1. Paket kurulumu kontrolü
            rcPackageInstalled = System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath, "..", "Packages", "manifest.json")) &&
                System.IO.File.ReadAllText(
                    System.IO.Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"))
                    .Contains("com.revenuecat.purchases-unity");

            // 2. Scripting Define Symbols kontrolü
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTarget);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            rcDefineSet = defines.Contains("REVENUECAT_PURCHASES");

            // 3. API Key kontrolü
            var config = Resources.Load<LiveMonetizationConfig>("LiveMonetizationConfig");
            rcApiKeySet = config != null &&
                !string.IsNullOrEmpty(config.revenueCatAndroidApiKey) &&
                !config.revenueCatAndroidApiKey.StartsWith("YOUR_");

            // 4. Store mode kontrolü
            rcStoreModeSet = config != null && config.iapStoreMode == IAPStoreMode.RevenueCat;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Başlık
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("🧪 RevenueCat Kurulum Sihirbazı", headerStyle);
            GUILayout.Label("Shipaton 2026 — Next Gen Award", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 });
            GUILayout.Space(15);

            DrawChecklistItem("1. RevenueCat Unity SDK Paketi", rcPackageInstalled,
                "manifest.json'da com.revenuecat.purchases-unity paketi bulundu.",
                "Paket manifest.json'a eklenmedi. Packages/manifest.json dosyasını kontrol edin.");

            DrawChecklistItem("2. REVENUECAT_PURCHASES Tanımı", rcDefineSet,
                "Scripting Define Symbols'de REVENUECAT_PURCHASES tanımlı.",
                "Player Settings > Scripting Define Symbols'e REVENUECAT_PURCHASES ekleyin.");

            if (!rcDefineSet)
            {
                GUILayout.Space(5);
                if (GUILayout.Button("⚡ REVENUECAT_PURCHASES Otomatik Ekle", GUILayout.Height(30)))
                {
                    AddRevenueCatDefine();
                    RefreshChecks();
                }
                GUILayout.Space(5);
            }

            DrawChecklistItem("3. API Key Yapılandırması", rcApiKeySet,
                "LiveMonetizationConfig'de RevenueCat API Key ayarlanmış.",
                "Resources/LiveMonetizationConfig asset'inde revenueCatAndroidApiKey alanını doldurun.");

            DrawChecklistItem("4. IAP Store Modu = RevenueCat", rcStoreModeSet,
                "LiveMonetizationConfig'de IAP modu RevenueCat olarak ayarlanmış.",
                "LiveMonetizationConfig'de iapStoreMode'u 'RevenueCat' olarak değiştirin.");

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "RevenueCat Dashboard Kurulum Adımları:\n\n" +
                "1. app.revenuecat.com adresinde hesap açın\n" +
                "2. Yeni proje oluşturun: 'PotionTown'\n" +
                "3. Google Play / App Store bağlantısını yapın\n" +
                "4. Products bölümünde 5 ürün tanımlayın:\n" +
                "   • com.potiontavern.kadimpara.small (Consumable)\n" +
                "   • com.potiontavern.kadimpara.medium (Consumable)\n" +
                "   • com.potiontavern.kadimpara.large (Consumable)\n" +
                "   • com.potiontavern.potionpass.premium (Non-Consumable)\n" +
                "   • com.potiontavern.removeads (Non-Consumable)\n" +
                "5. Entitlements oluşturun:\n" +
                "   • potion_pass_premium\n" +
                "   • remove_ads\n" +
                "6. Offerings oluşturun ve paketleri bağlayın\n" +
                "7. API Keys > Public SDK Key'i kopyalayın",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("🔗 RevenueCat Dashboard'u Aç", GUILayout.Height(30)))
            {
                Application.OpenURL("https://app.revenuecat.com");
            }

            if (GUILayout.Button("📖 RevenueCat Unity Dokümanları", GUILayout.Height(28)))
            {
                Application.OpenURL("https://docs.revenuecat.com/docs/unity");
            }

            if (GUILayout.Button("🚀 Devpost Başvuru Sayfası", GUILayout.Height(28)))
            {
                Application.OpenURL("https://revenuecat-shipaton-2026.devpost.com/");
            }

            GUILayout.Space(10);
            if (GUILayout.Button("🔄 Kontrolleri Yenile", GUILayout.Height(25)))
            {
                RefreshChecks();
            }

            // Sonuç Özeti
            GUILayout.Space(15);
            int passed = (rcPackageInstalled ? 1 : 0) + (rcDefineSet ? 1 : 0) + (rcApiKeySet ? 1 : 0) + (rcStoreModeSet ? 1 : 0);
            string statusEmoji = passed == 4 ? "✅" : (passed >= 2 ? "⚠️" : "❌");
            var summaryStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label($"{statusEmoji} {passed}/4 adım tamamlandı", summaryStyle);

            if (passed == 4)
            {
                EditorGUILayout.HelpBox("Harika! RevenueCat entegrasyonu tamamlanmış görünüyor. " +
                    "Mobil cihazda test build alarak satın alma akışını doğrulayın.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawChecklistItem(string title, bool isComplete, string successMsg, string failMsg)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isComplete ? "✅" : "❌", GUILayout.Width(25));

            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            if (isComplete) titleStyle.normal.textColor = new Color(0.2f, 0.7f, 0.2f);
            GUILayout.Label(title, titleStyle);

            EditorGUILayout.EndHorizontal();
            
            var msgStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            msgStyle.normal.textColor = isComplete ? Color.gray : new Color(0.9f, 0.5f, 0.1f);
            GUILayout.Label("    " + (isComplete ? successMsg : failMsg), msgStyle);
        }

        private void AddRevenueCatDefine()
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTarget);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

            if (!defines.Contains("REVENUECAT_PURCHASES"))
            {
                defines += ";REVENUECAT_PURCHASES";
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                Debug.Log("[RevenueCat Setup] REVENUECAT_PURCHASES sembolü eklendi. Derleme yeniden başlayacak...");
            }
        }
    }
}
#endif
