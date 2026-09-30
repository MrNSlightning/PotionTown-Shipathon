#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PotionShop.Editor
{
    /// <summary>
    /// Potion Tavern için Canlı Mağaza, Potion Pass, Reklam ve IAP Yönetim Merkezi.
    /// Geliştiricinin oyunu canlıya çıkarırken veya test ederken tüm ürünleri, fiyatları,
    /// sezon ödüllerini ve servis anahtarlarını tek bir görsel merkezden yönetmesini sağlar.
    /// </summary>
    public class TavernLiveDashboardEditor : EditorWindow
    {
        private int selectedTab = 0;
        private readonly string[] tabTitles = new string[]
        {
            "🛍️ Mağaza Kataloğu",
            "📜 Potion Pass Düzenleyici",
            "⚙️ Canlı Servisler & Reklam",
            "🎁 Hazine Kasaları",
            "📤 Google Play Dışa Aktar"
        };

        private ShopCatalogData shopCatalog;
        private PotionPassData potionPassData;
        private LiveMonetizationConfig monetizationConfig;

        private Vector2 scrollPos;
        private bool showNewProductFold = false;
        private ShopProductData newProductTemplate = new ShopProductData();

        [MenuItem("Tools/Potion Tavern/Canlı Yönetim Merkezi (Live Dashboard)", false, 1)]
        public static void OpenWindow()
        {
            var window = GetWindow<TavernLiveDashboardEditor>("Taverna Canlı Yönetim");
            window.minSize = new Vector2(850, 680);
            window.Show();
        }

        [InitializeOnLoadMethod]
        public static void EnsureDefaultAssetsExist()
        {
            LoadOrCreateAssets();
        }

        private void OnEnable()
        {
            LoadOrCreateAssets();
        }

        private static void LoadOrCreateAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_PotionTown/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // 1. ShopCatalogData
            var shop = Resources.Load<ShopCatalogData>("ShopCatalogData");
            if (shop == null)
            {
                shop = CreateInstance<ShopCatalogData>();
                shop.PopulateDefaults();
                AssetDatabase.CreateAsset(shop, "Assets/_PotionTown/Resources/ShopCatalogData.asset");
                Debug.Log("<color=green>[TavernDashboard]</color> Assets/_PotionTown/Resources/ShopCatalogData.asset oluşturuldu.");
            }

            // 2. LiveMonetizationConfig
            var config = Resources.Load<LiveMonetizationConfig>("LiveMonetizationConfig");
            if (config == null)
            {
                config = CreateInstance<LiveMonetizationConfig>();
                config.SetDefaults();
                AssetDatabase.CreateAsset(config, "Assets/_PotionTown/Resources/LiveMonetizationConfig.asset");
                Debug.Log("<color=green>[TavernDashboard]</color> Assets/_PotionTown/Resources/LiveMonetizationConfig.asset oluşturuldu.");
            }

            // 3. PotionPassData
            var pass = Resources.Load<PotionPassData>("PotionPassData");
            if (pass == null)
            {
                pass = CreateInstance<PotionPassData>();
                pass.PopulateDefaults(10);
                AssetDatabase.CreateAsset(pass, "Assets/_PotionTown/Resources/PotionPassData.asset");
                Debug.Log("<color=green>[TavernDashboard]</color> Assets/_PotionTown/Resources/PotionPassData.asset oluşturuldu.");
            }

            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            if (shopCatalog == null) shopCatalog = Resources.Load<ShopCatalogData>("ShopCatalogData");
            if (monetizationConfig == null) monetizationConfig = Resources.Load<LiveMonetizationConfig>("LiveMonetizationConfig");
            if (potionPassData == null) potionPassData = Resources.Load<PotionPassData>("PotionPassData");

            DrawHeaderBanner();

            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles, GUILayout.Height(36));
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0:
                    DrawShopCatalogTab();
                    break;
                case 1:
                    DrawPotionPassTab();
                    break;
                case 2:
                    DrawLiveServicesTab();
                    break;
                case 3:
                    DrawLootBoxTab();
                    break;
                case 4:
                    DrawGooglePlayExportTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawBottomBar();
        }

        private void DrawHeaderBanner()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };

            var subStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(6);
            GUILayout.Label("⚔️ POTION TAVERN: CANLI YÖNETİM & GELİR MERKEZİ ⚔️", headerStyle);
            GUILayout.Label("Mağaza Ürünleri, Potion Pass Sezonları, IAP & Reklam Entegrasyon Paneli", subStyle);
            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        // =========================================================================
        //  TAB 0: MAĞAZA (SHOP) KATALOĞU
        // =========================================================================
        private void DrawShopCatalogTab()
        {
            if (shopCatalog == null)
            {
                EditorGUILayout.HelpBox("ShopCatalogData bulunamadı. Lütfen yenileyin.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛍️ MAĞAZA ÜRÜN LİSTESİ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Buradaki ürünler oyun içi mağazada ('Taverna Mağazası') dinamik olarak gösterilir. Fiyatları, açıklamaları ve ödülleri canlı değiştirebilirsiniz.", MessageType.Info);
            GUILayout.Space(5);

            for (int i = 0; i < shopCatalog.products.Count; i++)
            {
                var prod = shopCatalog.products[i];
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                string popularTag = prod.isPopular ? " ★ [POPÜLER]" : "";
                EditorGUILayout.LabelField($"#{i + 1}  {prod.displayName} ({prod.GetFormattedPrice()}){popularTag}", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("Sil", GUILayout.Width(50)))
                {
                    shopCatalog.products.RemoveAt(i);
                    EditorUtility.SetDirty(shopCatalog);
                    GUI.backgroundColor = Color.white;
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                prod.id = EditorGUILayout.TextField("Ürün ID (Google Play ID):", prod.id);
                prod.displayName = EditorGUILayout.TextField("Başlık:", prod.displayName);
                prod.subtitle = EditorGUILayout.TextField("Alt Başlık / Paket Adı:", prod.subtitle);
                prod.description = EditorGUILayout.TextField("Açıklama:", prod.description);
                prod.category = (ShopCategory)EditorGUILayout.EnumPopup("Kategori:", prod.category);
                prod.priceType = (ShopPriceType)EditorGUILayout.EnumPopup("Ödeme Türü:", prod.priceType);

                if (prod.priceType == ShopPriceType.RealMoney)
                {
                    EditorGUILayout.BeginHorizontal();
                    prod.realMoneyPrice = EditorGUILayout.FloatField("Gerçek Fiyat:", prod.realMoneyPrice);
                    prod.currencySymbol = EditorGUILayout.TextField("Para Birimi:", prod.currencySymbol, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();
                }
                else if (prod.priceType == ShopPriceType.KadimPara || prod.priceType == ShopPriceType.Gold)
                {
                    prod.ingamePrice = EditorGUILayout.IntField("Oyun İçi Tutar:", prod.ingamePrice);
                }

                prod.rewardType = (ShopRewardType)EditorGUILayout.EnumPopup("Ödül Türü:", prod.rewardType);
                if (prod.rewardType == ShopRewardType.LootBox)
                {
                    prod.lootBoxTier = EditorGUILayout.TextField("Kasa Türü (Bronze/Silver/Gold):", prod.lootBoxTier);
                }
                else
                {
                    prod.rewardAmount = EditorGUILayout.IntField("Ödül Miktarı:", prod.rewardAmount);
                }

                prod.isPopular = EditorGUILayout.Toggle("Popüler Rozeti Göster:", prod.isPopular);
                prod.cardAccentColor = EditorGUILayout.ColorField("Kart Vurgu Rengi:", prod.cardAccentColor);
                prod.icon = (Sprite)EditorGUILayout.ObjectField("Ürün İkonu (Opsiyonel):", prod.icon, typeof(Sprite), false);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("+ Yeni Ürün Ekle", GUILayout.Height(32)))
            {
                shopCatalog.products.Add(new ShopProductData
                {
                    id = $"com.potiontavern.item.{shopCatalog.products.Count + 1}",
                    displayName = "YENİ ÜRÜN",
                    subtitle = "Özel Paket",
                    description = "Açıklama metni buraya gelecek.",
                    realMoneyPrice = 29.99f,
                    rewardAmount = 25
                });
                EditorUtility.SetDirty(shopCatalog);
            }

            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            if (GUILayout.Button("Varsayılan Paketleri Yükle", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Varsayılana Dönülsün mü?", "Mevcut ürün listesi silinip varsayılan 6 ürün yüklenecek. Emin misiniz?", "Evet", "İptal"))
                {
                    shopCatalog.PopulateDefaults();
                    EditorUtility.SetDirty(shopCatalog);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        //  TAB 1: POTION PASS DÜZENLEYİCİ
        // =========================================================================
        private void DrawPotionPassTab()
        {
            if (potionPassData == null)
            {
                EditorGUILayout.HelpBox("PotionPassData bulunamadı. Lütfen yenileyin.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📜 POTION PASS SEZON VE ÖDÜL PİSTİ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Potion Pass sezon adını, aşama sayısını ve her aşamadaki Ücretsiz & Premium ödülleri canlı olarak buradan değiştirebilirsiniz.", MessageType.Info);
            GUILayout.Space(5);

            potionPassData.seasonName = EditorGUILayout.TextField("Sezon Adı:", potionPassData.seasonName);
            potionPassData.totalTiers = EditorGUILayout.IntSlider("Toplam Aşama Sayısı:", potionPassData.totalTiers, 1, 30);
            potionPassData.xpPerTier = EditorGUILayout.IntField("Aşama Başına Gerekli XP:", potionPassData.xpPerTier);
            potionPassData.premiumPriceKadimPara = EditorGUILayout.IntField("Premium Bilet Fiyatı (Kadim Para):", potionPassData.premiumPriceKadimPara);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("--- AŞAMA ÖDÜLLERİ TABLOSU ---", EditorStyles.boldLabel);

            // Tablo Listesi
            for (int i = 1; i <= potionPassData.totalTiers; i++)
            {
                int tierIndex = i - 1;
                while (potionPassData.freeTrackRewards.Count < i)
                {
                    potionPassData.freeTrackRewards.Add(new PassTierReward { tier = i, rewardType = PassRewardType.Gold, amount = i * 100, rewardDescription = $"+{i * 100} Altın" });
                }
                while (potionPassData.premiumTrackRewards.Count < i)
                {
                    potionPassData.premiumTrackRewards.Add(new PassTierReward { tier = i, rewardType = PassRewardType.KadimPara, amount = i * 5, rewardDescription = $"+{i * 5} Kadim Para" });
                }

                var fReward = potionPassData.freeTrackRewards[tierIndex];
                var pReward = potionPassData.premiumTrackRewards[tierIndex];

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"★ AŞAMA {i}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                // Sol: Free
                EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
                EditorGUILayout.LabelField("Ücretsiz Yol:", EditorStyles.miniBoldLabel);
                fReward.rewardType = (PassRewardType)EditorGUILayout.EnumPopup("Tür:", fReward.rewardType);
                fReward.amount = EditorGUILayout.IntField("Miktar:", fReward.amount);
                fReward.rewardDescription = EditorGUILayout.TextField("Açıklama:", fReward.rewardDescription);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                // Sağ: Premium
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("★ Premium Yol:", EditorStyles.miniBoldLabel);
                pReward.rewardType = (PassRewardType)EditorGUILayout.EnumPopup("Tür:", pReward.rewardType);
                pReward.amount = EditorGUILayout.IntField("Miktar:", pReward.amount);
                pReward.rewardDescription = EditorGUILayout.TextField("Açıklama:", pReward.rewardDescription);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Varsayılan 10 Aşamalı Ödülleri Yükle", GUILayout.Height(30)))
            {
                potionPassData.PopulateDefaults(10);
                EditorUtility.SetDirty(potionPassData);
            }

            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        //  TAB 2: CANLI SERVİSLER & REKLAM AYARLARI
        // =========================================================================
        private void DrawLiveServicesTab()
        {
            if (monetizationConfig == null)
            {
                EditorGUILayout.HelpBox("LiveMonetizationConfig bulunamadı.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚙️ IAP (UYGULAMA İÇİ SATIN ALMA) MODU", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("LiveStore: Canlı Google Play/App Store satın alımları için.\nFakeStore: Unity Editor'de gerçek Google Play dialog penceresi simülasyonu için.\nLocalSimulation: Harici paket gerektirmeyen anında test modu.", MessageType.Info);

            monetizationConfig.iapStoreMode = (IAPStoreMode)EditorGUILayout.EnumPopup("Mağaza Modu:", monetizationConfig.iapStoreMode);
            monetizationConfig.fallbackToSimulation = EditorGUILayout.Toggle("Hata Durumunda Simülasyona Geç:", monetizationConfig.fallbackToSimulation);
            monetizationConfig.googlePlayLicenseKey = EditorGUILayout.TextField("Google Play Lisans Anahtarı:", monetizationConfig.googlePlayLicenseKey);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("📺 REKLAM (UNITY ADS / ADMOB) AYARLARI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("LiveProduction: Canlı yayın modu (Gerçek reklamlar gösterilir).\nTestMode: Test modu (5 saniyelik görsel reklam simülatörü veya test reklamları gösterilir).", MessageType.Info);

            monetizationConfig.adsMode = (AdsMode)EditorGUILayout.EnumPopup("Reklam Modu:", monetizationConfig.adsMode);
            monetizationConfig.androidGameId = EditorGUILayout.TextField("Android Game ID:", monetizationConfig.androidGameId);
            monetizationConfig.iosGameId = EditorGUILayout.TextField("iOS Game ID:", monetizationConfig.iosGameId);
            monetizationConfig.rewardedPlacementId = EditorGUILayout.TextField("Ödüllü Reklam Placement ID:", monetizationConfig.rewardedPlacementId);
            monetizationConfig.interstitialPlacementId = EditorGUILayout.TextField("Geçiş Reklamı Placement ID:", monetizationConfig.interstitialPlacementId);
            monetizationConfig.rewardedGoldReward = EditorGUILayout.IntField("Reklam Başına Altın Ödülü:", monetizationConfig.rewardedGoldReward);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("👤 HESAP & BULUT KAYIT (UGS) AYARLARI", EditorStyles.boldLabel);
            monetizationConfig.ugsEnvironment = EditorGUILayout.TextField("UGS Ortamı:", monetizationConfig.ugsEnvironment);
            monetizationConfig.autoSignInOnStart = EditorGUILayout.Toggle("Başlangıçta Otomatik Oturum Aç:", monetizationConfig.autoSignInOnStart);

            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        //  TAB 3: HAZİNE KASALARI (LOOT BOX)
        // =========================================================================
        private void DrawLootBoxTab()
        {
            if (monetizationConfig == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎁 HAZİNE KASALARI FİYAT VE MALİYET AYARLARI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Oyuncuların kasaları doğrudan satın alıp açabilmesi için altın ve kadim para tutarlarını buradan ayarlayabilirsiniz.", MessageType.Info);
            GUILayout.Space(5);

            monetizationConfig.bronzeBoxPriceGold = EditorGUILayout.IntField("Bronz Kasa Fiyatı (Altın):", monetizationConfig.bronzeBoxPriceGold);
            monetizationConfig.silverBoxPriceGold = EditorGUILayout.IntField("Gümüş Kasa Fiyatı (Altın):", monetizationConfig.silverBoxPriceGold);
            monetizationConfig.goldBoxPriceKadimPara = EditorGUILayout.IntField("Altın Kasa Fiyatı (Kadim Para):", monetizationConfig.goldBoxPriceKadimPara);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Kasa Olasılık Bilgileri (Görsel Temsil):", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("• Bronz: 50 - 200 Altın, %10 Kadim Para şansı, temel malzemeler\n• Gümüş: 200 - 500 Altın, %30 Kadim Para (3-5 Adet), %20 nadir malzeme\n• Altın: 500 - 1500 Altın, %60 Kadim Para (10-25 Adet), %50 özel eşya", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        //  TAB 4: GOOGLE PLAY DIŞA AKTAR (CSV)
        // =========================================================================
        private void DrawGooglePlayExportTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📤 GOOGLE PLAY CONSOLE CSV DIŞA AKTARICI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Google Play Console > Uygulama İçi Ürünler bölümüne tek seferde toplu yükleme (Batch Import) yapabileceğiniz resmi CSV dosyasını üretir.", MessageType.Info);
            GUILayout.Space(10);

            if (shopCatalog != null)
            {
                EditorGUILayout.LabelField($"Katalogdaki Gerçek Para Ürün Sayısı: {shopCatalog.products.FindAll(p => p.priceType == ShopPriceType.RealMoney).Count}", EditorStyles.boldLabel);
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.2f, 0.65f, 0.35f);
            if (GUILayout.Button("📁 GooglePlay_InAppProducts.csv Dosyasını Oluştur ve Aç", GUILayout.Height(44)))
            {
                ExportGooglePlayCSV();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void ExportGooglePlayCSV()
        {
            if (shopCatalog == null) return;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "GooglePlay_InAppProducts.csv");

            var lines = new List<string>
            {
                "Product ID,Published State,Purchase Type,Auto Translate,Locale,Title,Description,Price"
            };

            foreach (var prod in shopCatalog.products)
            {
                if (prod.priceType != ShopPriceType.RealMoney) continue;

                string purchaseType = (prod.rewardType == ShopRewardType.RemoveAds || prod.rewardType == ShopRewardType.PotionPassPremium)
                    ? "managed_by_android"
                    : "managed_by_android";

                // Fiyat mikro-birim (19.99 TL = 19990000 mikrobirim veya standart format)
                string priceInMicros = ((long)(prod.realMoneyPrice * 1000000)).ToString();
                string cleanDesc = prod.description.Replace("\n", " ").Replace(",", ";");
                string cleanTitle = prod.displayName.Replace(",", " ");

                lines.Add($"{prod.id},active,{purchaseType},false,tr-TR,{cleanTitle},{cleanDesc},{priceInMicros}");
            }

            File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
            EditorUtility.DisplayDialog("CSV Başarıyla Oluşturuldu!", $"Google Play Console için hazırlanan CSV masaüstünüze kaydedildi:\n\n{filePath}", "Tamam");
            EditorUtility.RevealInFinder(filePath);
        }

        // =========================================================================
        //  ALT KAYDETME & CANLI YENİLEME BARI
        // =========================================================================
        private void DrawBottomBar()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.45f);
            if (GUILayout.Button("💾 DEĞİŞİKLİKLERİ KAYDET", GUILayout.Height(38)))
            {
                SaveAllAssets();
                EditorUtility.DisplayDialog("Kayıt Başarılı", "Tüm Mağaza, Potion Pass ve Canlı Servis ayarları başarıyla kaydedildi!", "Tamam");
            }

            GUI.backgroundColor = new Color(0.40f, 0.25f, 0.65f);
            if (GUILayout.Button("🔄 OYUNDAKİ ARAYÜZÜ CANLI YENİLE (REBUILD UI)", GUILayout.Height(38)))
            {
                SaveAllAssets();
                // MiniGameManager.CreateMiniGameArea'yı tetikle
                var method = typeof(MiniGameManager).GetMethod("CreateMiniGameArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    EditorUtility.DisplayDialog("Arayüz Yenilendi", "Sahnedeki Büyü Arenası arayüzü yeni mağaza ürünleri ve Potion Pass ödülleriyle güncellendi!", "Tamam");
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void SaveAllAssets()
        {
            if (shopCatalog != null) EditorUtility.SetDirty(shopCatalog);
            if (monetizationConfig != null) EditorUtility.SetDirty(monetizationConfig);
            if (potionPassData != null) EditorUtility.SetDirty(potionPassData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
