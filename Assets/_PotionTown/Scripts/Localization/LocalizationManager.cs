using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    public enum GameLanguage
    {
        Turkish,
        English
    }

    /// <summary>
    /// PotionTown merkezi yerelleştirme (Localization) yöneticisi.
    /// Türkçe ve İngilizce tam metin desteği sağlar, dil değişiminde tüm arayüzü anında günceller.
    /// Çift yönlü çözümleme (key veya doğrudan Türkçe metin) sayesinde tüm UI anında çevrilir.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LocalizationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[LocalizationManager]");
                        _instance = go.AddComponent<LocalizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const string PREFS_LANG_KEY = "PotionTown_SelectedLanguage";
        public static event Action<GameLanguage> OnLanguageChanged;

        private GameLanguage _currentLanguage = GameLanguage.Turkish;
        public GameLanguage CurrentLanguage => _currentLanguage;

        // Metin Sözlüğü: [Key, (Turkish, English)]
        private static readonly Dictionary<string, (string tr, string en)> _translations = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            // ─── BAŞLANGIÇ & LOBİ MENÜSÜ ───
            { "menu_start_day", ("GÜNÜ BAŞLAT", "START DAY") },
            { "menu_settings", ("AYARLAR", "SETTINGS") },
            { "menu_language", ("DİL: TÜRKÇE", "LANGUAGE: ENGLISH") },
            { "menu_welcome", ("POTION TOWN", "POTION TOWN") },
            { "menu_subtitle", ("Simyacının Dükkanı", "Alchemist's Shop") },

            // ─── TOP DROPDOWN MENÜSÜ ───
            { "top_open_shop", ("Dükkanı Aç", "Open Shop") },
            { "top_close_shop", ("Dükkanı Kapat", "Close Shop") },
            { "top_inventory", ("Envanter", "Inventory") },
            { "top_lobby", ("Kasabaya Dön", "Return to Town") },
            { "top_return_lobby", ("Kasabaya Dön", "Return to Town") },
            { "top_market", ("Market", "Market") },
            { "top_gold", ("Altın", "Gold") },
            { "top_ancient_coins", ("Kadim Para", "Ancient Coins") },
            { "top_reputation", ("İtibar", "Reputation") },

            // ─── GÜN SONU PANELİ (END OF DAY PANEL) ───
            { "eod_title", ("SEVİYE {0} ÖZETİ", "LEVEL {0} SUMMARY") },
            { "eod_subtitle", ("Dükkan Gün Sonu Raporu", "End of Day Shop Report") },
            { "eod_total_customers", ("Bugün Ağırlanan Toplam: <b><color=#FFF7D6>{0}</color></b> Müşteri", "Total Served Today: <b><color=#FFF7D6>{0}</color></b> Customers") },
            { "eod_happy", ("Mutlu: <color=#2ECC71><b>{0}</b></color>", "Happy: <color=#2ECC71><b>{0}</b></color>") },
            { "eod_normal", ("Normal: <color=#F1C40F><b>{0}</b></color>", "Normal: <color=#F1C40F><b>{0}</b></color>") },
            { "eod_grumpy", ("Huysuz: <color=#E67E22><b>{0}</b></color>", "Grumpy: <color=#E67E22><b>{0}</b></color>") },
            { "eod_angry", ("Sinirli: <color=#E74C3C><b>{0}</b></color>", "Angry: <color=#E74C3C><b>{0}</b></color>") },
            { "eod_earned_gold", ("Kazanılan Gelir: <color=#2ECC71><b>+{0} Altın</b></color>", "Earned Revenue: <color=#2ECC71><b>+{0} Gold</b></color>") },
            { "eod_lost_gold", ("Kaçan / Kaybedilen: <color=#E74C3C><b>-{0} Altın</b></color>", "Lost / Missed: <color=#E74C3C><b>-{0} Gold</b></color>") },
            { "eod_net_profit", ("Günlük Net Kâr: <color={0}><b>{1}{2} Altın</b></color>", "Daily Net Profit: <color={0}><b>{1}{2} Gold</b></color>") },
            { "eod_next_day", ("YENİ GÜNE BAŞLA", "START NEW DAY") },
            { "eod_start_new_day", ("YENİ GÜNE BAŞLA", "START NEW DAY") },
            { "eod_close", ("KAPAT", "CLOSE") },

            // ─── AYARLAR MENÜSÜ (SETTINGS) ───
            { "settings_title", ("Ayarlar", "Settings") },
            { "settings_tab_account", ("HESAP", "ACCOUNT") },
            { "settings_tab_audio", ("SES", "AUDIO") },
            { "settings_tab_credits", ("YAPIMCILAR", "CREDITS") },
            { "settings_account_title", ("Hesap Yönetimi", "Account Management") },
            { "settings_status_label", ("Durum:", "Status:") },
            { "settings_alchemist_prefix", ("Simyacı ID:", "Alchemist ID:") },
            { "settings_language", ("Dil", "Language") },
            { "settings_language_toggle", ("DİL: TÜRKÇE", "LANGUAGE: ENGLISH") },
            { "settings_music", ("Müzik", "Music") },
            { "settings_sfx", ("Ses Efektleri", "SFX") },
            { "settings_music_label", ("Müzik", "Music") },
            { "settings_sfx_label", ("Ses Efektleri", "SFX") },
            { "settings_music_title", ("Arka Plan Müziği", "Background Music") },
            { "settings_sfx_title", ("Ses Efektleri", "Sound Effects") },
            { "settings_off", ("Kapalı", "Off") },
            { "settings_connected", ("Bağlandı", "Connected") },
            { "settings_guest", ("Misafir", "Guest") },
            { "settings_close", ("Kapat", "Close") },
            { "settings_not_signed_in", ("Giriş yapılmadı", "Not signed in") },
            { "settings_guest_signing_in", ("Misafir olarak giriş yapılıyor...", "Signing in as guest...") },
            { "settings_signing_in", ("Giriş yapılıyor...", "Signing in...") },
            { "settings_creating_account", ("Hesap oluşturuluyor...", "Creating account...") },
            { "settings_google_signing_in", ("Google ile giriş yapılıyor...", "Signing in with Google...") },
            { "settings_signed_in_id", ("Giriş başarılı! ID: {0}", "Sign-in successful! ID: {0}") },
            { "settings_signed_out", ("Hesaptan çıkış yapıldı.", "Signed out.") },
            { "settings_signed_out_msg", ("Çıkış yapıldı.", "Signed out.") },
            { "settings_username_placeholder", ("Kullanıcı Adı...", "Username...") },
            { "settings_password_placeholder", ("Şifre...", "Password...") },
            { "settings_guest_btn", ("Misafir Giriş", "Guest Login") },
            { "settings_login_btn", ("Giriş Yap", "Sign In") },
            { "settings_signin_btn", ("Giriş Yap", "Sign In") },
            { "settings_register_btn", ("Kayıt Ol", "Register") },
            { "settings_google_btn", ("Google ile Bağlan", "Sign in with Google") },
            { "settings_logout_btn", ("Çıkış Yap", "Sign Out") },
            { "settings_signout_btn", ("Çıkış Yap", "Sign Out") },
            { "credits_header", ("POTION TOWN", "POTION TOWN") },
            { "credits_subtitle", ("Bir Simyacının Hikayesi", "Story of an Alchemist") },
            { "credits_developers", ("[ YAPIMCILAR ]", "[ DEVELOPERS ]") },
            { "credits_dev1_role", ("Oyun Tasarımı, Sanat & Kodlama", "Game Design, Art & Programming") },
            { "credits_dev2_role", ("Geliştirici & Sistem Mimarisi", "Developer & Systems Architecture") },
            { "credits_audio_role", ("Ses & Müzik Sistemi", "Audio & Music Systems") },
            { "credits_cloud_role", ("Hesap Yönetimi & Bulut Servisleri", "Account & Cloud Services") },

            // ─── ENVANTER, DEPO & DETAY POPUP ───
            { "inv_select_item", ("Eşya Seçin", "Select an Item") },
            { "inv_type_format", ("Tür: {0}", "Type: {0}") },
            { "inv_value_format", ("Değer: {0} Altın", "Value: {0} Gold") },
            { "inv_count_format", ("Envanterde: {0} Adet", "In Inventory: {0}") },
            { "btn_recipes", ("Tarifler", "Recipes") },
            { "btn_place_on_shelf", ("Rafa Yerleştir", "Place on Shelf") },
            { "btn_close", ("Kapat", "Close") },
            { "btn_ok", ("Tamam", "OK") },
            { "inv_btn_recipes", ("Tarifler", "Recipes") },
            { "inv_btn_assign", ("Rafa Yerleştir", "Place on Shelf") },
            { "inv_btn_close", ("Kapat", "Close") },
            { "inv_btn_show_recipes", ("Tarifleri Gör", "View Recipes") },

            // ─── EŞYA TÜRLERİ ───
            { "item_type_essence", ("Özsu", "Essence") },
            { "item_type_ingredient", ("Malzeme", "Ingredient") },
            { "item_type_potion", ("İksir", "Potion") },
            { "item_type_scroll", ("Parşömen", "Scroll") },
            { "item_type_special", ("Özel Eşya", "Special Item") },

            // ─── RAFLAR & KİLİTLER ───
            { "shelf_free", ("Ücretsiz", "Free") },
            { "shelf_watch_ad", ("Reklam İzle", "Watch Ad") },
            { "shelf_locked_special", ("Kilitli (Özel)", "Locked (Special)") },
            { "shelf_ad_expired", ("Yeni gün başladı, reklamla açılan raf süresi dolduğu için tekrar kilitlendi.", "New day started, temporary ad-unlocked shelf has expired and locked.") },
            { "shelf_item_cleared", ("{0} raftan kaldırıldı ve envantere döndü.", "{0} removed from shelf and returned to inventory.") },
            { "shelf_shop_open_lock_warn", ("Dükkan açıkken yeni raf kilidi açılamaz!", "New shelf locks cannot be unlocked while shop is open!") },
            { "shelf_shop_open_replace_warn", ("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!", "Placed items cannot be changed while shop is open!") },
            { "shelf_shop_open_remove_warn", ("Dükkan açıkken raftaki eşyalar kaldırılamaz!", "Items cannot be removed from shelves while shop is open!") },

            // ─── KAZAN & SİMYA ───
            { "cauldron_brew", ("KAYNAT", "BREW") },
            { "Kaynat", ("Kaynat", "Brew") },
            { "cauldron_only_essence", ("Burada Sadece Öz Yapılabilir!", "Only Essences Can Be Brewed Here!") },
            { "cauldron_only_potion", ("Burada Sadece İksir Yapılabilir!", "Only Potions Can Be Brewed Here!") },
            { "cauldron_no_license", ("İksirin Lisansı Yok!", "Potion Is Not Licensed!") },
            { "cauldron_clean_instruction", ("Kazanı temizle! ({0} tıklama kaldı)", "Clean the cauldron! ({0} clicks left)") },

            // ─── LİSANS PARŞÖMENİ ───
            { "license_tier_header", ("Kademe {0} / {1}", "Tier {0} / {1}") },
            { "license_btn_buy", ("Satın Al", "Buy") },
            { "license_licensed", ("Lisanslı", "Licensed") },
            { "license_buy_all", ("Tümünü Aç", "Unlock All") },
            { "license_discount", ("(%{0} İndirim)", "({0}% Off)") },
            { "license_all_completed", ("Tüm Lisanslar Alındı", "All Licenses Acquired") },
            { "license_all_licensed", ("TÜMÜ AÇIK", "ALL LICENSED") },
            { "license_unlocked", ("Açıldı", "Unlocked") },
            { "license_required", ("Lisans Gerekli", "License Required") },
            { "lic_page_tier", ("Sayfa {0} / {1}  -  Tier {0} İksir Lisansları", "Page {0} / {1}  -  Tier {0} Potion Licenses") },
            { "lic_get_license", ("Lisans Al", "Get License") },
            { "lic_licensed", ("LİSANSLI", "LICENSED") },
            { "lic_buy_all", ("HEPSİNİ BİRLİKTE AL", "BUY ALL TOGETHER") },
            { "lic_discount", ("(%{0} İndirim)", "({0}% Discount)") },
            { "lic_unlocked", ("Açıldı", "Unlocked") },
            { "lic_all_completed", ("LİSANSLAR TAMAMLANDI", "LICENSES COMPLETED") },
            { "lic_all_licensed", ("Tüm İksirler Lisanslandı", "All Potions Licensed") },

            // ─── MARKET / SHOP UI ───
            { "shop_title", ("KRALİYET PAZARI", "ROYAL MARKET") },
            { "shop_tab_kadim", ("Kadim Para", "Ancient Coins") },
            { "shop_tab_bundles", ("Özel Teklifler", "Special Offers") },
            { "shop_tab_pass", ("İksir Kartı", "Potion Pass") },
            { "shop_tab_noads", ("Reklamsız", "No Ads") },
            { "shop_buy", ("SATIN AL", "BUY") },
            { "shop_purchasing", ("İşleniyor...", "Processing...") },
            { "shop_purchased", ("Satın Alındı!", "Purchased!") },
            { "shop_owned", ("SAHİPSİN", "OWNED") },
            { "shop_best_deal", ("EN POPÜLER", "MOST POPULAR") },
            { "shop_extra_bonus", ("+%20 BONUS", "+20% BONUS") },
            { "shop_kadim_small_title", ("Küçük Kese", "Small Pouch") },
            { "shop_kadim_small_desc", ("100 Kadim Para", "100 Ancient Coins") },
            { "shop_kadim_med_title", ("Gümüş Sandık", "Silver Chest") },
            { "shop_kadim_med_desc", ("500 Kadim Para", "500 Ancient Coins") },
            { "shop_kadim_large_title", ("Kraliyet Hazinesi", "Royal Treasure") },
            { "shop_kadim_large_desc", ("1200 Kadim Para", "1200 Ancient Coins") },
            { "shop_pass_title", ("Premium İksir Kartı", "Premium Potion Pass") },
            { "shop_pass_desc", ("Tüm sezon boyunca özel ödüller ve avantajlar!", "Unlock all season tiers and exclusive rewards!") },
            { "shop_noads_title", ("Reklamları Kaldır", "Remove Ads") },
            { "shop_noads_desc", ("Zorunlu tüm reklamları kalıcı olarak kaldırın.", "Permanently remove all forced advertisements.") },
            { "shop_license_required", ("Lisans Gerekli", "License Required") },
            { "shop_bundle_apprentice", ("Çırak Simyacı Paketi", "Apprentice Alchemist Bundle") },
            { "shop_bundle_apprentice_desc", ("10,000 Altın + 150 Kadim Para", "10,000 Gold + 150 Ancient Coins") },
            { "shop_bundle_master", ("Usta Simyacı Sandığı", "Master Alchemist Chest") },
            { "shop_bundle_master_desc", ("35,000 Altın + 600 Kadim Para", "35,000 Gold + 600 Ancient Coins") },
            { "shop_starter_deal", ("BAŞLANGIÇ FIRSATI", "STARTER DEAL") },
            { "shop_extra_40", ("+%40 EKSTRA", "+40% EXTRA") },
            { "shop_season_1", ("SEZON 1", "SEASON 1") },
            { "shop_perm_advantage", ("KALICI AVANTAJ", "PERMANENT PERK") },

            // ─── TÜYO & İPUÇLARI ───
            { "hint_button_label", ("Tüyo (Reklam)", "Hint (Ad)") },
            { "hint_title", ("🔮 GÜNLÜK İKSİR TÜYOSU", "🔮 DAILY POTION HINT") },
            { "hint_no_customer", ("Şu anda sırada müşteri yok!", "No customer in queue right now!") },
            { "hint_level_summary", ("Bu bölümde toplam {0} müşteri bekleniyor.\nGelecek İksirler:", "Total {0} customers expected today.\nUpcoming Potions:") },
            { "hint_ad_failed", ("Reklam yüklenemedi. Lütfen tekrar deneyin.", "Ad failed to load. Please try again.") },
            { "hint_patience_boost", ("Müşteri sabrı tazelendi!", "Customer patience restored!") },

            // ─── SEVİYE & ÜNVANLAR ───
            { "lvl_prefix", ("Seviye", "Level") },
            { "level_format", ("SEVİYE {0}", "LEVEL {0}") },
            { "lvl_title_apprentice", ("Çırak", "Apprentice") },
            { "lvl_title_journeyman", ("Kalfa", "Journeyman") },
            { "lvl_title_master", ("Usta", "Master") },
            { "lvl_title_grandmaster", ("Büyük Usta", "Grandmaster") },
            { "lvl_title_legend", ("Efsane", "Legend") },
            { "lvl_text_format", ("Sv. {0} ({1})", "Lvl. {0} ({1})") },
            { "day_text_format", ("Gün {0} / {1}", "Day {0} / {1}") },
            { "customer_max_capacity", ("MAKS. KAPASİTE", "MAX CAPACITY") },
            { "gold_suffix", ("Altın", "Gold") },
            { "kadim_suffix", ("Kadim Para", "Ancient Coins") },
            { "toast_purchased", ("Satın Alındı!", "Purchased!") },

            // ─── BİNA TOOLTİPLERİ (LOBİ) ───
            { "building_potion_selling", ("İksir Satış Dükkanı", "Potion Selling Shop") },
            { "building_potion_crafting", ("İksir Yapma Dükkanı", "Potion Crafting Shop") },
            { "building_material_shop", ("Malzeme Dükkanı", "Material Shop") },
            { "building_potion_shop", ("İksir Dükkanı", "Potion Shop") },
            { "building_minigame_shop", ("Mini Game Dükkanı", "Mini Game Shop") },
            { "building_day_end", ("Gün Sonu", "End of Day") },
            { "lobby_shop_not_open", ("Dükkan Açık Değil", "Shop Not Open") },
            { "İksir Satış Dükkanı", ("İksir Satış Dükkanı", "Potion Selling Shop") },
            { "İksir Yapma Dükkanı", ("İksir Yapma Dükkanı", "Potion Crafting Shop") },
            { "Malzeme Dükkanı", ("Malzeme Dükkanı", "Material Shop") },
            { "İksir Dükkanı", ("İksir Dükkanı", "Potion Shop") },
            { "Mini Game Dükkanı", ("Mini Game Dükkanı", "Mini Game Shop") },

            // ─── HATA & UYARI MESAJLARI ───
            { "err_not_enough_gold", ("Yetersiz Altın!", "Not Enough Gold!") },
            { "err_not_enough_kadim", ("Yetersiz Kadim Para!", "Not Enough Ancient Coins!") },
            { "err_inventory_full", ("Envanter Dolu!", "Inventory is Full!") },
            { "err_shop_closed", ("Önce Dükkanı Açmalısın!", "Open The Shop First!") },
            { "err_invalid_recipe", ("Geçersiz İksir Tarifi!", "Invalid Potion Recipe!") },
            { "err_select_ingredient", ("Lütfen malzeme seçin!", "Please select an ingredient!") },
            { "err_already_owned", ("Bu ürün zaten alınmış!", "Already owned!") },

            // ─── PARA BİLDİRİMLERİ ───
            { "gold_earned", ("+{0} Altın", "+{0} Gold") },
            { "gold_spent", ("-{0} Altın", "-{0} Gold") },
            { "kadim_earned", ("+{0} Kadim", "+{0} Ancient") },
            { "kadim_spent", ("-{0} Kadim", "-{0} Ancient") },

            // ─── MİNİ OYUN & GEÇİŞ & REKLAM ───
            { "pass_premium_active", ("Premium Aktif!", "Premium Active!") },
            { "pass_upgrade_ticket", ("BİLETİ YÜKSELT", "UPGRADE PASS") },
            { "pass_claimed", ("ALINDI", "CLAIMED") },
            { "pass_claim", ("AL", "CLAIM") },
            { "pass_locked", ("KİLİTLİ", "LOCKED") },
            { "pass_premium", ("PREMİUM", "PREMIUM") },
            { "ad_playing", ("Reklam Oynatılıyor...", "Playing Advertisement...") },
            { "ad_reward_countdown", ("Ödüle Kalan Süre: {0} sn", "Time to reward: {0}s") },
            { "ad_completed", ("Tebrikler! Reklam tamamlandı.", "Congratulations! Ad completed.") },
            { "ad_claim_close", ("ÖDÜLÜ AL & KAPAT (X)", "CLAIM REWARD & CLOSE (X)") },
            { "lootbox_owned_format", ("Sahip Olunan: {0} Adet", "Owned: {0}") },
            { "minigame_remaining_plays", ("Kalan Hak: {0} / {1}", "Remaining Plays: {0} / {1}") },
            { "minigame_cooldown", ("Bekleme: {0:D2}:{1:D2}", "Cooldown: {0:D2}:{1:D2}") },
            { "minigame_high_scores", ("En Yüksek Skorlar: ", "High Scores: ") },
            { "minigame_none_yet", ("Henüz yok", "None yet") },
            { "account_player_id", ("Oyuncu ID", "Player ID") },
            { "account_cloud_connected", ("Bulut hesabı bağlı ve senkronize.", "Cloud account connected and synced.") },
            { "account_cloud_disconnected", ("Misafir veya çevrimiçi giriş yapabilirsiniz.", "You can play as guest or sign in.") },
            { "account_signing_in_guest", ("Misafir olarak giriş yapılıyor...", "Signing in as guest...") },
            { "account_signing_in", ("Giriş yapılıyor...", "Signing in...") },
            { "account_empty_fields", ("Kullanıcı adı ve şifre boş bırakılamaz!", "Username and password cannot be empty!") },
            { "account_password_short", ("Şifre en az 6 karakter olmalıdır!", "Password must be at least 6 characters!") },
            { "account_creating", ("Hesap oluşturuluyor...", "Creating account...") },
            { "account_signed_out", ("Çıkış yapıldı.", "Signed out.") },
            { "account_sign_in_success", ("Giriş başarılı! ID: {0}", "Sign in successful! ID: {0}") },

            // ═══════════════════════════════════════════════════════
            //  86 EŞYANIN TAMAMI (İKİ YÖNLÜ ADLANDIRMA)
            // ═══════════════════════════════════════════════════════
            { "Kızıl Kök", ("Kızıl Kök", "Crimson Root") },
            { "Kültür Mantarı", ("Kültür Mantarı", "Culture Mushroom") },
            { "Köz Tohumu", ("Köz Tohumu", "Ember Seed") },
            { "Lav Taşı", ("Lav Taşı", "Lava Stone") },
            { "Aşk Çiçeği", ("Aşk Çiçeği", "Love Flower") },
            { "Kırmızı Öz", ("Kırmızı Öz", "Red Essence") },
            { "Kırmızı Toz", ("Kırmızı Toz", "Red Powder") },
            { "Yakut Kristali", ("Yakut Kristali", "Ruby Crystal") },
            { "Ateş Parşömeni", ("Ateş Parşömeni", "Scroll of Fire") },
            { "Kızıl İksir", ("Kızıl İksir", "Crimson Elixir") },
            { "Yaşam İksiri", ("Yaşam İksiri", "Elixir of Life") },
            { "Hayat Şişesi", ("Hayat Şişesi", "Life Vial") },
            { "Büyük İyileştirme İksiri", ("Büyük İyileştirme İksiri", "Major Healing Potion") },
            { "Hafif İyileştirme İksiri", ("Hafif İyileştirme İksiri", "Minor Healing Potion") },
            { "İyileştirme İksiri", ("İyileştirme İksiri", "Healing Potion") },
            { "Küçük İksir", ("Küçük İksir", "Small Potion") },

            { "Mavi Kristal Tozu", ("Mavi Kristal Tozu", "Blue Crystal Dust") },
            { "Mavi Öz", ("Mavi Öz", "Blue Essence") },
            { "Mana Kristali", ("Mana Kristali", "Mana Crystal") },
            { "Gece Çiçeği", ("Gece Çiçeği", "Night Flower") },
            { "Işınlanma Parşömeni", ("Işınlanma Parşömeni", "Scroll of Teleport") },
            { "Gök Mantarı", ("Gök Mantarı", "Sky Mushroom") },
            { "Gök Cevheri", ("Gök Cevheri", "Sky Ore") },
            { "Boşluk Taşı", ("Boşluk Taşı", "Void Stone") },
            { "Girdap Yaprağı", ("Girdap Yaprağı", "Vortex Leaf") },
            { "Eter Şişesi", ("Eter Şişesi", "Ether Vial") },
            { "Mana İksiri", ("Mana İksiri", "Mana Potion") },
            { "Hafif Mana İksiri", ("Hafif Mana İksiri", "Minor Mana Potion") },
            { "Runik İksir", ("Runik İksir", "Runic Potion") },
            { "Yıldız Ateşi İksiri", ("Yıldız Ateşi İksiri", "Starfire Potion") },
            { "Hiçlik İksiri", ("Hiçlik İksiri", "Void Potion") },
            { "Büyücü Karışımı", ("Büyücü Karışımı", "Wizard's Brew") },

            { "Ejderha Yumurtası", ("Ejderha Yumurtası", "Dragon Egg") },
            { "Zümrüt Mantarı", ("Zümrüt Mantarı", "Emerald Mushroom") },
            { "Orman Mantarı", ("Orman Mantarı", "Forest Mushroom") },
            { "Yeşil Öz", ("Yeşil Öz", "Green Essence") },
            { "Şifalı Ot", ("Şifalı Ot", "Healing Herb") },
            { "Can Tohumu", ("Can Tohumu", "Life Seed") },
            { "Şifa Parşömeni", ("Şifa Parşömeni", "Scroll of Healing") },
            { "Sürpriz Çiçeği", ("Sürpriz Çiçeği", "Surprise Flower") },
            { "Zehirli Diken", ("Zehirli Diken", "Venom Spike") },
            { "Çeviklik İksiri", ("Çeviklik İksiri", "Agility Potion") },
            { "Panzehir", ("Panzehir", "Antidote") },
            { "Odak İksiri", ("Odak İksiri", "Focus Potion") },
            { "Bitkisel Karışım", ("Bitkisel Karışım", "Herbal Brew") },
            { "Hız İksiri", ("Hız İksiri", "Sprint Elixir") },
            { "Dayanıklılık İksiri", ("Dayanıklılık İksiri", "Stamina Potion") },
            { "Çevik Karışım", ("Çevik Karışım", "Swift Brew") },

            { "Kehribar Reçinesi", ("Kehribar Reçinesi", "Amber Resin") },
            { "Alev Çiçeği", ("Alev Çiçeği", "Flame Flower") },
            { "Altın Mantar", ("Altın Mantar", "Golden Mushroom") },
            { "Yıldırım Tohumu", ("Yıldırım Tohumu", "Lightning Seed") },
            { "Koruma Parşömeni", ("Koruma Parşömeni", "Scroll of Protection") },
            { "Güneş Özütü", ("Güneş Özütü", "Solar Extract") },
            { "Güneş Çiçeği", ("Güneş Çiçeği", "Sun Flower") },
            { "Güneş Polenleri", ("Güneş Polenleri", "Sun Pollen") },
            { "Sarı Öz", ("Sarı Öz", "Yellow Essence") },
            { "Defans İksiri", ("Defans İksiri", "Defense Potion") },
            { "Kuvvet Toniği", ("Kuvvet Toniği", "Muscle Tonic") },
            { "Anka Yudumu", ("Anka Yudumu", "Phoenix Draught") },
            { "Hücum Şişesi", ("Hücum Şişesi", "Rally Vial") },
            { "Yıldırım Şifası", ("Yıldırım Şifası", "Shock Cure") },
            { "Taşlaşma Şifası", ("Taşlaşma Şifası", "Stone Cure") },
            { "Dönüşüm Şişesi", ("Dönüşüm Şişesi", "Transmutation Flask") },

            { "Donmuş Gözyaşı", ("Donmuş Gözyaşı", "Frozen Tear") },
            { "Üzüm Çiçeği", ("Üzüm Çiçeği", "Grape Flower") },
            { "Ay Özütü", ("Ay Özütü", "Lunar Essence") },
            { "Akça Mantarı", ("Akça Mantarı", "Maple Mushroom") },
            { "Ay Meyvesi", ("Ay Meyvesi", "Moon Fruit") },
            { "Kutup Çiçeği", ("Kutup Çiçeği", "Polar Flower") },
            { "Tanımlama Parşömeni", ("Tanımlama Parşömeni", "Scroll of Identify") },
            { "Ruh Parçası", ("Ruh Parçası", "Soul Fragment") },
            { "Beyaz Öz", ("Beyaz Öz", "White Essence") },
            { "Gizemli Şişe", ("Gizemli Şişe", "Arcane Flask") },
            { "Temizleme Şişesi", ("Temizleme Şişesi", "Cleanse Vial") },
            { "Kutsal Su", ("Kutsal Su", "Holy Water") },
            { "Arındırıcı İksir", ("Arındırıcı İksir", "Purge Elixir") },
            { "Arınma İksiri", ("Arınma İksiri", "Purify Potion") },
            { "Kutsanmış Su", ("Kutsanmış Su", "Sacred Water") },
            { "Çözülme İksiri", ("Çözülme İksiri", "Thaw Potion") },

            // ─── ÖZEL İKSİRLER (SPECIALS) ───
            { "Sükunet Merhemi", ("Sükunet Merhemi", "Soothing Balm") },
            { "Zincir Kıran İksiri", ("Zincir Kıran İksiri", "Chain Breaker Potion") },
            { "Kozmik Uyum İksiri", ("Kozmik Uyum İksiri", "Cosmic Harmony Potion") },
            { "Coşku Toniği", ("Coşku Toniği", "Rally Tonic") },
            { "Rüzgarın Hızı İksiri", ("Rüzgarın Hızı İksiri", "Wind's Swiftness Potion") },
            { "Kadim Gölge Taşı", ("Kadim Gölge Taşı", "Ancient Shadow Stone") }
        };

        // Hızlı ters arama haritaları (Türkçe <-> İngilizce)
        private static readonly Dictionary<string, string> _trToEnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _enToTrMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static LocalizationManager()
        {
            try
            {
                BuildReverseLookup();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalizationManager] Static init error: {ex}");
            }
        }

        private static void BuildReverseLookup()
        {
            _trToEnMap.Clear();
            _enToTrMap.Clear();
            foreach (var kvp in _translations)
            {
                if (!string.IsNullOrEmpty(kvp.Value.tr) && !string.IsNullOrEmpty(kvp.Value.en))
                {
                    _trToEnMap[kvp.Value.tr] = kvp.Value.en;
                    _enToTrMap[kvp.Value.en] = kvp.Value.tr;
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLanguage();
        }

        private void LoadLanguage()
        {
            string saved = PlayerPrefs.GetString(PREFS_LANG_KEY, "tr");
            _currentLanguage = (saved == "en") ? GameLanguage.English : GameLanguage.Turkish;
        }

        public void SetLanguage(GameLanguage language)
        {
            _currentLanguage = language;
            PlayerPrefs.SetString(PREFS_LANG_KEY, language == GameLanguage.English ? "en" : "tr");
            PlayerPrefs.Save();

            Debug.Log($"<color=yellow>[LocalizationManager]</color> Dil değiştirildi: {language}");
            OnLanguageChanged?.Invoke(_currentLanguage);
        }

        public void ToggleLanguage()
        {
            SetLanguage(_currentLanguage == GameLanguage.Turkish ? GameLanguage.English : GameLanguage.Turkish);
        }

        /// <summary>
        /// Belirtilen anahtara veya doğrudan metne göre geçerli dildeki metni döndürür.
        /// Çift yönlü arama sayesinde Türkçe metin İngilizceye, İngilizce metin Türkçeye otomatik çevrilir.
        /// </summary>
        public static string Get(string keyOrText, params object[] args)
        {
            if (string.IsNullOrEmpty(keyOrText)) return "";

            bool isEnglish = Instance != null && Instance.CurrentLanguage == GameLanguage.English;
            string text = keyOrText;

            // 1. Doğrudan anahtar arama
            if (_translations.TryGetValue(keyOrText, out var val))
            {
                text = isEnglish ? val.en : val.tr;
            }
            // 2. İngilizce iken ters arama (Türkçe metin -> İngilizce karşılık)
            else if (isEnglish && _trToEnMap.TryGetValue(keyOrText, out string enText))
            {
                text = enText;
            }
            // 3. Türkçe iken ters arama (İngilizce metin -> Türkçe karşılık)
            else if (!isEnglish && _enToTrMap.TryGetValue(keyOrText, out string trText))
            {
                text = trText;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(text, args);
                }
                catch
                {
                    return text;
                }
            }

            return text;
        }

        /// <summary>
        /// Bir ItemData nesnesinin geçerli dildeki adını döndürür.
        /// </summary>
        public static string GetItemName(ItemData item)
        {
            if (item == null) return "";
            if (!string.IsNullOrEmpty(item.itemName))
            {
                return Get(item.itemName);
            }
            return Get(item.name);
        }

        /// <summary>
        /// ItemType enum değerinin yerelleştirilmiş adını döndürür.
        /// </summary>
        public static string GetItemType(ItemType type)
        {
            return type switch
            {
                ItemType.Essence => Get("item_type_essence"),
                ItemType.Ingredient => Get("item_type_ingredient"),
                ItemType.Potion => Get("item_type_potion"),
                ItemType.Scroll => Get("item_type_scroll"),
                ItemType.Special => Get("item_type_special"),
                _ => type.ToString()
            };
        }

        public static string GetCurrentLanguageName()
        {
            return (Instance != null && Instance.CurrentLanguage == GameLanguage.English) ? "English" : "Türkçe";
        }
    }
}
