using UnityEngine;
using UnityEngine.Events;
using System;

namespace PotionShop
{
    public enum PotionTownRoom
    {
        Lobby,
        PotionSelling,
        PotionCrafting,
        IngredientShop
    }

    /// <summary>
    /// PotionTown sahnesindeki tüm oda ve kamera geçişlerini, yöneticileri ve global UI bağlantılarını
    /// tamamen Inspector üzerinden kontrol edilebilir hale getiren merkezi koordinatör.
    /// Kod içi 'GetRootGameObjects()' ve string aramalarını ortadan kaldırır.
    /// </summary>
    [DisallowMultipleComponent]
    public class PotionTownSceneCoordinator : MonoBehaviour
    {
        public static PotionTownSceneCoordinator Instance { get; private set; }

        [Header("Başlangıç Odası")]
        [Tooltip("Oyun açıldığında ilk görüntülenecek oda")]
        public PotionTownRoom initialRoom = PotionTownRoom.Lobby;

        [Header("Odalar / Görünümler (Rooms)")]
        [Tooltip("Lobi / Kasaba sahnesi kök objesi")]
        public GameObject lobbyRoom;

        [Tooltip("İksir Satış Dükkanı kök objesi")]
        public GameObject potionSellingRoom;

        [Tooltip("İksir Yapma / Crafting Dükkanı kök objesi")]
        public GameObject potionCraftingRoom;

        [Tooltip("Malzeme Dükkanı kök objesi")]
        public GameObject ingredientShopRoom;

        [Header("Kameralar (Cameras)")]
        [Tooltip("Lobi kamerası")]
        public Camera lobbyCamera;

        [Tooltip("İksir Satış Dükkanı kamerası")]
        public Camera potionSellingCamera;

        [Tooltip("İksir Yapma Dükkanı kamerası")]
        public Camera potionCraftingCamera;

        [Tooltip("Malzeme Dükkanı kamerası")]
        public Camera ingredientShopCamera;

        [Header("Çekirdek Yöneticiler (Core Managers)")]
        public GameManager gameManager;

        public PlayerInventory playerInventory;
        public LicenseManager licenseManager;
        public StorageManager storageManager;

        [Header("Global UI Referansları")]
        public TopDropdownMenu topDropdownMenu;
        public ParaveElmasUI currencyHUD;
        public InventoryUI sharedInventoryUI;

        [Header("Global Menü ve Panel Prefab / Sahne Referansları")]
        [Tooltip("Oyun açılışında gösterilen başlangıç menüsü")]
        public PotionShop.UI.GameStartMenuUI startMenuUI;

        [Tooltip("Premium pazar / market paneli")]
        public ShopUI shopUI;

        [Tooltip("Kazanılan ve harcanan altın/kadim para geri bildirim sistemi")]
        public CurrencyFeedbackUI currencyFeedbackUI;

        [Tooltip("Yazı biçiminde ekran hata ve uyarı bildirim sistemi")]
        public ToastNotificationUI toastNotificationUI;

        [Tooltip("İksir satış dükkanındaki reklamlı tüyo butonu")]
        public PotionShopHintButton hintButton;

        [Tooltip("Gün sonu özet paneli")]
        public PotionShop.UI.EndOfDayPanel endOfDayPanel;

        [Header("Olaylar (Unity Events)")]
        public UnityEvent<PotionTownRoom> onRoomChanged;

        public PotionTownRoom CurrentRoom { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            
            // Force Lobby as requested by user
            initialRoom = PotionTownRoom.Lobby;
            
            EnsureInspectorReferences();

            // 1. Kritik raf ve envanter sistemlerini EN BAŞTA ilklendir
            try
            {
                FixedShelfManager.EnsureInitializedStatic();
                ShelfSlot.EnsureAllSlotsCached();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PotionTownSceneCoordinator] Raf sistemi ilklendirme hatası: {ex.Message}");
            }

            // 2. Ses ve Müzik fonksiyonlarını oyun açılır açılmaz başlat
            try
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.EnsurePlaying();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PotionTownSceneCoordinator] AudioManager başlatma hatası: {ex.Message}");
            }

            // 3. Lokalizasyon, bildirim ve mağaza sistemlerini başlat
            try
            {
                var _loc = LocalizationManager.Instance;
                var _toast = ToastNotificationUI.Instance;
                var _feedback = CurrencyFeedbackUI.Instance;
                var _shop = ShopUI.Instance;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PotionTownSceneCoordinator] UI sistemleri başlatma uyarısı: {ex.Message}");
            }
        }

        private void Start()
        {
            SwitchToRoom(initialRoom);
            ConnectTopDropdownMenu();
            EnsureStartMenuUI();
            EnsureHintButton();
        }

        private void EnsureStartMenuUI()
        {
            if (startMenuUI != null)
            {
                startMenuUI.gameObject.SetActive(true);
                startMenuUI.Show();
                return;
            }

            var existing = PotionShop.UI.GameStartMenuUI.Instance ?? FindFirstObjectByType<PotionShop.UI.GameStartMenuUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                startMenuUI = existing;
                startMenuUI.gameObject.SetActive(true);
                startMenuUI.Show();
                return;
            }

            var prefab = Resources.Load<GameObject>("UI/GameStartMenuUI");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                startMenuUI = go.GetComponent<PotionShop.UI.GameStartMenuUI>();
                startMenuUI.Show();
            }
        }

        private void EnsureHintButton()
        {
            if (hintButton != null)
            {
                return;
            }

            var existing = PotionShopHintButton.Instance ?? FindFirstObjectByType<PotionShopHintButton>(FindObjectsInactive.Include);
            if (existing != null)
            {
                hintButton = existing;
                return;
            }

            var prefab = Resources.Load<GameObject>("UI/PotionShopHintButton");
            if (prefab != null && potionSellingRoom != null)
            {
                var go = Instantiate(prefab, potionSellingRoom.transform, false);
                hintButton = go.GetComponent<PotionShopHintButton>();
            }
        }

        /// <summary>
        /// Eksik referansları otomatik keşfetmeye çalışır (Inspector emniyeti).
        /// </summary>
        public void EnsureInspectorReferences()
        {
            if (gameManager == null) gameManager = GetComponentInChildren<GameManager>(true);

            if (playerInventory == null) playerInventory = GetComponentInChildren<PlayerInventory>(true);
            if (licenseManager == null) licenseManager = GetComponentInChildren<LicenseManager>(true);
            if (storageManager == null) storageManager = GetComponentInChildren<StorageManager>(true);
            if (topDropdownMenu == null) topDropdownMenu = TopDropdownMenu.Instance ?? FindFirstObjectByType<TopDropdownMenu>();

            if (startMenuUI == null) startMenuUI = FindFirstObjectByType<PotionShop.UI.GameStartMenuUI>(FindObjectsInactive.Include);
            if (shopUI == null) shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (currencyFeedbackUI == null) currencyFeedbackUI = FindFirstObjectByType<CurrencyFeedbackUI>(FindObjectsInactive.Include);
            if (toastNotificationUI == null) toastNotificationUI = FindFirstObjectByType<ToastNotificationUI>(FindObjectsInactive.Include);
            if (hintButton == null) hintButton = FindFirstObjectByType<PotionShopHintButton>(FindObjectsInactive.Include);
            if (endOfDayPanel == null) endOfDayPanel = FindFirstObjectByType<PotionShop.UI.EndOfDayPanel>(FindObjectsInactive.Include);
        }

        private void ConnectTopDropdownMenu()
        {
            if (topDropdownMenu == null)
            {
                topDropdownMenu = TopDropdownMenu.Instance ?? FindFirstObjectByType<TopDropdownMenu>();
            }

            if (topDropdownMenu != null)
            {
                // TopDropdownMenu'nün lobi ve canvas referanslarını doğrudan bağla
                topDropdownMenu.mapCanvas = lobbyRoom;
                topDropdownMenu.gameCanvas = potionSellingRoom;

                // Lobiye dön butonunu koordinatöre bağla
                if (topDropdownMenu.lobbyButton != null)
                {
                    topDropdownMenu.lobbyButton.onClick.RemoveListener(ShowLobby);
                    topDropdownMenu.lobbyButton.onClick.AddListener(ShowLobby);
                }
            }
        }

        /// <summary>
        /// Belirtilen odaya geçiş yapar, kamerayı ve menü bağlamını günceller.
        /// </summary>
        public void SwitchToRoom(PotionTownRoom room)
        {
            CurrentRoom = room;

            // Oda geçişinde açık envanter varsa kapat
            if (StorageManager.Instance != null && StorageManager.Instance.IsStorageOpen)
            {
                StorageManager.Instance.CloseStorage();
            }
            if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeSelf)
            {
                InventoryUI.Instance.Close();
            }

            // 1. Odaların aktiflik durumunu ayarla
            if (lobbyRoom != null) lobbyRoom.SetActive(room == PotionTownRoom.Lobby);
            if (potionSellingRoom != null) potionSellingRoom.SetActive(room == PotionTownRoom.PotionSelling);
            if (potionCraftingRoom != null) potionCraftingRoom.SetActive(room == PotionTownRoom.PotionCrafting);
            if (ingredientShopRoom != null) ingredientShopRoom.SetActive(room == PotionTownRoom.IngredientShop);

            // Tüyo butonunun sadece İksir Satış Odasında görünmesini sağla
            if (PotionShopHintButton.Instance != null)
            {
                PotionShopHintButton.Instance.gameObject.SetActive(room == PotionTownRoom.PotionSelling);
            }

            // 2. Kameraları güncelle ve aktif olana 'MainCamera' etiketini ver
            UpdateCamera(lobbyCamera, room == PotionTownRoom.Lobby);
            UpdateCamera(potionSellingCamera, room == PotionTownRoom.PotionSelling);
            UpdateCamera(potionCraftingCamera, room == PotionTownRoom.PotionCrafting);
            UpdateCamera(ingredientShopCamera, room == PotionTownRoom.IngredientShop);

            // 3. TopDropdownMenu durumunu güncelle
            ConnectTopDropdownMenu();
            if (topDropdownMenu != null)
            {
                switch (room)
                {
                    case PotionTownRoom.Lobby:
                        topDropdownMenu.SetContext(false);
                        break;
                    case PotionTownRoom.PotionSelling:
                        topDropdownMenu.SetCurrentShop(potionSellingRoom, true);
                        EnsurePotionSellingCanvasLayers();
                        break;
                    case PotionTownRoom.PotionCrafting:
                        topDropdownMenu.SetCurrentShop(potionCraftingRoom, false);
                        break;
                    case PotionTownRoom.IngredientShop:
                        topDropdownMenu.SetCurrentShop(ingredientShopRoom, false);
                        break;
                }
            }

            if (room == PotionTownRoom.PotionSelling)
            {
                EnsurePotionSellingCanvasLayers();
            }

            onRoomChanged?.Invoke(room);
            Debug.Log($"<color=cyan>[PotionTownSceneCoordinator]</color> Aktif Oda: {room}");
        }

        /// <summary>
        /// İksir Satış Odasındaki Canvas katmanlarını doğrular; tezgahın müşterilerin önünde kalmasını garantiler.
        /// </summary>
        private void EnsurePotionSellingCanvasLayers()
        {
            if (potionSellingRoom == null) return;

            Canvas mainCanvas = potionSellingRoom.GetComponent<Canvas>();
            if (mainCanvas != null && potionSellingCamera != null)
            {
                mainCanvas.worldCamera = potionSellingCamera;
            }

            Transform fgShopFrame = potionSellingRoom.transform.Find("Foreground_ShopFrame");
            if (fgShopFrame != null)
            {
                Canvas fgCanvas = fgShopFrame.GetComponent<Canvas>();
                if (fgCanvas != null)
                {
                    fgCanvas.overrideSorting = true;
                    fgCanvas.sortingLayerName = "Foreground";
                    fgCanvas.sortingOrder = 500;
                    if (potionSellingCamera != null) fgCanvas.worldCamera = potionSellingCamera;
                }
            }

            Transform spawnerTr = potionSellingRoom.transform.Find("Spawner");
            if (spawnerTr != null)
            {
                Canvas spawnerCanvas = spawnerTr.GetComponent<Canvas>();
                if (spawnerCanvas != null)
                {
                    spawnerCanvas.overrideSorting = true;
                    spawnerCanvas.sortingLayerName = "Characters";
                    spawnerCanvas.sortingOrder = 0;
                    if (potionSellingCamera != null) spawnerCanvas.worldCamera = potionSellingCamera;
                }
            }
        }

        private void UpdateCamera(Camera cam, bool isActive)
        {
            if (cam == null) return;
            cam.gameObject.SetActive(isActive);
            cam.enabled = isActive;
            if (isActive)
            {
                cam.tag = "MainCamera";
            }
            else if (cam.CompareTag("MainCamera"))
            {
                cam.tag = "Untagged";
            }
        }

        // --- Hızlı Buton & UnityEvent Metotları ---
        public void ShowLobby() => SwitchToRoom(PotionTownRoom.Lobby);
        public void ShowPotionSelling() => SwitchToRoom(PotionTownRoom.PotionSelling);
        public void ShowPotionCrafting() => SwitchToRoom(PotionTownRoom.PotionCrafting);
        public void ShowIngredientShop() => SwitchToRoom(PotionTownRoom.IngredientShop);
    }
}
