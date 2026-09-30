using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace PotionShop
{
    public enum UnlockType
    {
        Free,
        Gold,
        Ad,
        SlotMechanic
    }

    public class ShelfSlot : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private static readonly HashSet<ShelfSlot> _allShelfSlots = new HashSet<ShelfSlot>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            _allShelfSlots.Clear();
        }

        /// <summary>
        /// Sahnedeki aktif veya inaktif tüm ShelfSlot bileşenlerini statik kayıt listesine ekler.
        /// </summary>
        public static void EnsureAllSlotsCached()
        {
            ShelfSlot[] found = FindObjectsByType<ShelfSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    _allShelfSlots.Add(found[i]);
                }
            }
        }

        /// <summary>
        /// Tüm raf slotlarının görsel ve kilit durumunu günceller.
        /// </summary>
        public static void UpdateAllShelfSlots()
        {
            if (_allShelfSlots.Count == 0)
            {
                EnsureAllSlotsCached();
            }
            foreach (var slot in _allShelfSlots)
            {
                if (slot != null)
                {
                    slot.UpdateSlotState();
                }
            }
        }

        /// <summary>
        /// Verilen eşya herhangi bir açık/aktif raf slotunda veya sabit rafta yer alıyor mu?
        /// Rafta olan eşyalar envanter genel listesinde gizlenir.
        /// </summary>
        public static bool IsItemOnAnyShelf(ItemData item)
        {
            if (item == null) return false;

            // 1. Sabit raflardaki eşyalar (İksirYapmaDükkanı)
            if (FixedShelfManager.IsFixedShelfItem(item))
            {
                return true;
            }

            // 2. Dinamik raf slotları (İksirSatışAna vb.)
            if (_allShelfSlots.Count == 0)
            {
                EnsureAllSlotsCached();
            }

            foreach (var slot in _allShelfSlots)
            {
                if (slot != null && slot.isUnlocked && slot.slotItemData == item)
                {
                    return true;
                }
            }

            return false;
        }

        [Header("Raf Ayarları")]
        public bool isUnlocked = false;
        [Tooltip("Sabit raf slotu ise envanter seçim paneli açılamaz (Örn: İksirYapmaDükkanı)")]
        public bool isFixedSlot = false;
        public UnlockType unlockType = UnlockType.Gold;
        public int unlockCost = 50;

        [Header("İçerik (Atanacak Eşya)")]
        public ItemData slotItemData;

        [Header("UI Referansları")]
        public GameObject lockUI;        
        public TextMeshProUGUI costText; 
        public GameObject itemUI;        
        public Image itemImage;          
        public TextMeshProUGUI itemCountText; // Envanterdeki miktar

        private bool _unlockedByAdForOneDay = false;
        private bool _wasLongPressed = false;
        private Coroutine _longPressCoroutine = null;

        public bool WasLongPressed
        {
            get
            {
                if (_wasLongPressed)
                {
                    _wasLongPressed = false;
                    return true;
                }
                return false;
            }
        }

        private void Awake()
        {
            _allShelfSlots.Add(this);
            EnsureItemUI();

            // Slotun kendisinde raycast alabilen bir Image olmasını garantiye al
            Image bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }
            bg.color = Color.clear;
            // Sabit raflarda (örn: İksirYapmaDükkanı) arkadaki slot kutusu raycast alıp komşu slotları engellemesin!
            bg.raycastTarget = !isFixedSlot;

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.targetGraphic = bg;
                btn.onClick.RemoveListener(OnSlotClicked);
                btn.onClick.AddListener(OnSlotClicked);
            }
        }

        private void OnEnable()
        {
            _allShelfSlots.Add(this);
            EnsureItemUI();
            UpdateSlotState();

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateSlotState;
                PlayerInventory.Instance.OnInventoryChanged += UpdateSlotState;
            }
            LevelSystem.OnLevelUp -= HandleDayChanged;
            LevelSystem.OnLevelUp += HandleDayChanged;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
                GameManager.Instance.OnShopStatusChanged += HandleShopStatusChanged;
            }
            GameManager.OnShopStatusChangedStatic -= HandleShopStatusChanged;
            GameManager.OnShopStatusChangedStatic += HandleShopStatusChanged;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            CancelLongPress();
            _wasLongPressed = false;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateSlotState;
            }
            LevelSystem.OnLevelUp -= HandleDayChanged;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
            }
            GameManager.OnShopStatusChangedStatic -= HandleShopStatusChanged;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateSlotState();
        }

        private void Start()
        {
            EnsureItemUI();
            UpdateSlotState();
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateSlotState;
                PlayerInventory.Instance.OnInventoryChanged += UpdateSlotState;
            }
            LevelSystem.OnLevelUp -= HandleDayChanged;
            LevelSystem.OnLevelUp += HandleDayChanged;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
                GameManager.Instance.OnShopStatusChanged += HandleShopStatusChanged;
            }
            GameManager.OnShopStatusChangedStatic -= HandleShopStatusChanged;
            GameManager.OnShopStatusChangedStatic += HandleShopStatusChanged;
        }

        private void OnDestroy()
        {
            CancelLongPress();
            _allShelfSlots.Remove(this);
            try
            {
                if (PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.OnInventoryChanged -= UpdateSlotState;
                }
                LevelSystem.OnLevelUp -= HandleDayChanged;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
                }
                GameManager.OnShopStatusChangedStatic -= HandleShopStatusChanged;
            }
            catch (System.Exception)
            {
                // PlayerInventory zaten yok edilmiş olabilir — güvenli şekilde geç
            }
        }

        private void HandleShopStatusChanged(bool isOpen)
        {
            UpdateSlotState();
        }

        private void HandleDayChanged(int newDay)
        {
            // Eğer bu slot reklam ile geçici olarak açıldıysa, yeni günde tekrar kilitlensin
            if (_unlockedByAdForOneDay)
            {
                _unlockedByAdForOneDay = false;
                isUnlocked = false;
                
                // Üzerindeki eşyayı envantere geri döndür (temizle)
                ClearItem(true);
                
                UpdateSlotState();
                Debug.Log("Yeni gün başladı, reklamla açılan raf süresi dolduğu için tekrar kilitlendi.");
            }
        }

        public void UpdateSlotState()
        {
            // Yok edilmiş obje üzerinde çağrılmasını engelle (MissingReferenceException)
            if (this == null) return;

            if (isUnlocked)
            {
                if (lockUI != null) lockUI.SetActive(false);
                if (costText != null) costText.gameObject.SetActive(false);
                EnsureItemUI();
                if (itemUI != null) itemUI.SetActive(true);

                if (slotItemData != null)
                {
                    // Otomatik referans bağlama (null kalmasını önle)
                    if (itemImage == null && itemUI != null) itemImage = itemUI.GetComponent<Image>();
                    if (itemCountText == null && itemUI != null) itemCountText = itemUI.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (itemImage != null)
                    {
                        itemImage.gameObject.SetActive(true);
                        itemImage.enabled = true;
                        itemImage.sprite = slotItemData.itemIcon;
                        itemImage.preserveAspect = true;
                    }

                    // Eşyanın envanterdeki sayısını göster
                    int count = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemCount(slotItemData) : 0;
                    if (itemCountText != null)
                    {
                        itemCountText.gameObject.SetActive(true);
                        itemCountText.text = "x" + count.ToString();
                    }
                    
                    // Eğer eşya bitmişse rengini gri yap, varsa normal parlak göster
                    if (itemImage != null)
                    {
                        if (count <= 0)
                        {
                            float alpha = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.emptyItemAlpha : 0.5f;
                            // Gri renk: eşyanın envanterde olmadığını belirtir
                            itemImage.color = new Color(0.35f, 0.35f, 0.35f, alpha);
                        }
                        else
                        {
                            itemImage.color = new Color(1f, 1f, 1f, 1f); // Tam parlak
                        }
                    }

                    // itemUI'ın stretch çapa ve sıfır sizeDelta'sını garantiye al
                    if (itemUI != null && itemUI.transform.parent == transform)
                    {
                        RectTransform rt = itemUI.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchorMin = Vector2.zero;
                            rt.anchorMax = Vector2.one;
                            rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.sizeDelta = Vector2.zero;
                            rt.anchoredPosition = Vector2.zero;
                            rt.localScale = Vector3.one;
                        }
                    }
                }
                else
                {
                    // Boş raf slotu
                    if (itemImage != null)
                    {
                        itemImage.sprite = null;
                        itemImage.color = Color.clear;
                        itemImage.gameObject.SetActive(false);
                    }
                    if (itemCountText != null)
                    {
                        itemCountText.text = "";
                        itemCountText.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (itemUI != null) itemUI.SetActive(false);
                
                // Eğer dükkan açıksa (level başlamışsa) kilitler komple kaybolmak yerine gri ve şeffaf görünür
                bool isShopOpen = GameManager.Instance != null && GameManager.Instance.isShopOpen;
                
                if (lockUI != null)
                {
                    lockUI.SetActive(true);

                    // Gri renk ve şeffaflık ayarı
                    CanvasGroup cg = lockUI.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = lockUI.AddComponent<CanvasGroup>();
                    }
                    cg.alpha = isShopOpen ? 0.38f : 1f;
                    cg.blocksRaycasts = false; // Tıklamaların arkadaki slot butonuna geçmesini sağla

                    Image[] lockImages = lockUI.GetComponentsInChildren<Image>(true);
                    Color targetColor = isShopOpen ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
                    for (int i = 0; i < lockImages.Length; i++)
                    {
                        if (lockImages[i] != null)
                        {
                            lockImages[i].color = targetColor;
                        }
                    }
                }

                if (costText != null)
                {
                    costText.gameObject.SetActive(true);
                    costText.color = isShopOpen ? new Color(0.75f, 0.75f, 0.75f, 0.45f) : Color.white;

                    switch (unlockType)
                    {
                        case UnlockType.Free:
                            costText.text = LocalizationManager.Get("shelf_free");
                            break;
                        case UnlockType.Gold:
                            costText.text = unlockCost.ToString();
                            break;
                        case UnlockType.Ad:
                            costText.text = LocalizationManager.Get("shelf_watch_ad");
                            break;
                        case UnlockType.SlotMechanic:
                            costText.text = LocalizationManager.Get("shelf_locked_special");
                            break;
                    }
                }
            }
        }

        // Pointer click arayüzü (Button haricinde veya üst nesnelerden gelen doğrudan tıklamalar için)
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_wasLongPressed)
            {
                _wasLongPressed = false;
                return;
            }
            OnSlotClicked();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (isFixedSlot) return;
            if (!isUnlocked) return;
            if (slotItemData == null) return;

            CancelLongPress();
            _longPressCoroutine = StartCoroutine(HoldToClearRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            CancelLongPress();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelLongPress();
        }

        public void CancelLongPress()
        {
            if (_longPressCoroutine != null)
            {
                StopCoroutine(_longPressCoroutine);
                _longPressCoroutine = null;
                if (itemUI != null)
                {
                    itemUI.transform.localScale = Vector3.one;
                }
            }
        }

        private System.Collections.IEnumerator HoldToClearRoutine()
        {
            float elapsed = 0f;
            const float holdDuration = 3.0f;
            Vector3 originalScale = itemUI != null ? itemUI.transform.localScale : Vector3.one;

            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / holdDuration);

                if (itemUI != null)
                {
                    // Basılı tutulduğunu gösteren yumuşak küçülme efekti
                    float s = Mathf.Lerp(1.0f, 0.82f, progress);
                    itemUI.transform.localScale = originalScale * s;
                }

                yield return null;
            }

            if (itemUI != null)
            {
                itemUI.transform.localScale = originalScale;
            }

            _wasLongPressed = true;
            _longPressCoroutine = null;

            bool isShopOpen = GameManager.Instance != null && GameManager.Instance.isShopOpen;
            if (isShopOpen)
            {
                ToastNotificationUI.ShowWarning("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!", transform);
                yield break;
            }

            if (slotItemData != null)
            {
                string itemName = slotItemData.itemName;
                ClearItem(true);
                ToastNotificationUI.ShowInfo($"{itemName} raftan kaldırıldı ve envantere döndü.", transform);
            }
        }

        // UI'daki butondan bu metodu çağır (Eskiden AttemptUnlock idi)
        public void OnSlotClicked()
        {
            if (_wasLongPressed)
            {
                _wasLongPressed = false;
                return;
            }

            // Sabit raf slotları (Örn: İksirYapmaDükkanı) envanter seçim menüsü açamaz!
            if (isFixedSlot)
            {
                return;
            }

            bool isShopOpen = GameManager.Instance != null && GameManager.Instance.isShopOpen;

            // 1. Kilitli Slot Durumu
            if (!isUnlocked)
            {
                // Dükkan açıkken yeni kilit açılamaz!
                if (isShopOpen)
                {
                    ToastNotificationUI.ShowWarning("Dükkan açıkken yeni raf kilidi açılamaz!", transform);
                    Debug.Log("Dükkan açıkken yeni raf kilidi açılamaz!");
                    return;
                }

                switch (unlockType)
                {
                    case UnlockType.Free:
                        UnlockSlot();
                        // Kilit açıldıktan sonra otomatik envanter açılmaz, rafa tıklayınca açılır
                        break;
                    case UnlockType.Gold:
                        if (GameManager.Instance != null && GameManager.Instance.SpendGold(unlockCost))
                        {
                            CurrencyFeedbackUI.ShowLoss(unlockCost, transform);
                            UnlockSlot();
                            // Kilit açıldıktan sonra otomatik envanter açılmaz, rafa tıklayınca açılır
                        }
                        else
                        {
                            ToastNotificationUI.ShowWarning("err_not_enough_gold", transform);
                            Debug.Log("Yeterli Altın yok!");
                        }
                        break;
                    case UnlockType.Ad:
                        Debug.Log("Reklam gösteriliyor...");
                        if (AdsManager.Instance != null)
                        {
                            AdsManager.Instance.ShowRewardedAd(() => 
                            {
                                // Başarılı izleme
                                _unlockedByAdForOneDay = true;
                                UnlockSlot();
                                // Kilit açıldıktan sonra otomatik envanter açılmaz, rafa tıklayınca açılır
                                Debug.Log("Reklam izlendi, raf 1 günlük açıldı!");
                            }, 
                            () => 
                            {
                                // Başarısız veya iptal
                                Debug.Log("Reklam izlenmedi veya iptal edildi.");
                            });
                        }
                        else
                        {
                            Debug.LogWarning("AdsManager bulunamadı!");
                        }
                        break;
                    case UnlockType.SlotMechanic:
                        // TODO: Slot mekaniği satın alınmış mı kontrol et
                        Debug.Log("Bu slot özel bir mekanikle açılır.");
                        break;
                }
                return;
            }

            // 2. Kilidi Açılmış (isUnlocked == true) Slot Durumu
            if (isShopOpen)
            {
                // Dükkan açıldıktan sonra raflara zaten yerleştirilmiş eşya varsa veya boş rafa eşya yerleştirilirse tekrardan değiştirilemesin
                if (slotItemData != null)
                {
                    ToastNotificationUI.ShowWarning("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!", transform);
                    Debug.Log("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!");
                    return;
                }

                // Öncesinde açılmış boş raflara eşya koyabilmek için envanter açılsın
                OpenStorage();
                return;
            }

            // Dükkan kapalıyken eşya yerleştirme veya değiştirme serbesttir
            OpenStorage();
        }

        public void OpenStorage()
        {
            if (isFixedSlot) return;

            StorageManager sm = StorageManager.Instance;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                StorageManager localSM = canvas.GetComponentInChildren<StorageManager>(true);
                if (localSM != null) sm = localSM;
            }

            if (sm != null)
            {
                sm.OpenStorageForSlot(this);
            }
            else
            {
                Debug.LogWarning("StorageManager bulunamadı!");
            }
        }

        private void UnlockSlot()
        {
            isUnlocked = true;
            UpdateSlotState();
            if (slotItemData != null)
            {
                PlayerInventory.onInventoryChangedStatic?.Invoke();
            }
        }
        
        // Raftaki eşyayı kaldırmak için kullanılır
        public void ClearItem(bool notifyInventory = true)
        {
            ItemData oldItem = slotItemData;
            slotItemData = null;
            if (itemUI != null)
            {
                DraggableItem dragScript = itemUI.GetComponent<DraggableItem>();
                if (dragScript != null)
                {
                    dragScript.itemData = null;
                }
            }
            UpdateSlotState();

            if (notifyInventory && oldItem != null)
            {
                PlayerInventory.onInventoryChangedStatic?.Invoke();
            }
        }

        // StorageManager veya FixedShelfManager tarafından seçilen eşyayı bu rafa atamak için kullanılır
        public void AssignItem(ItemData item, bool notifyInventory = true)
        {
            if (item == null)
            {
                ClearItem(notifyInventory);
                return;
            }

            ItemData oldItem = slotItemData;
            slotItemData = item;
            EnsureItemUI();
            
            // Eğer DraggableItem objesi varsa (itemUI'ın üzerindeyse) ona da veriyi verelim
            if (itemUI != null)
            {
                DraggableItem dragScript = itemUI.GetComponent<DraggableItem>();
                if (dragScript == null)
                {
                    dragScript = itemUI.AddComponent<DraggableItem>();
                }
                dragScript.itemData = item;
                dragScript.SaveOriginalState();
                dragScript.UpdateVisuals();
            }
            
            UpdateSlotState();

            if (notifyInventory)
            {
                PlayerInventory.onInventoryChangedStatic?.Invoke();
            }
        }

        /// <summary>
        /// Raf slotundaki görsel/sürüklenebilir UI objesinin varlığını garantiye alır,
        /// daha önceden silinmişse veya prefabta eksikse otomatik olarak oluşturup bağlar.
        /// </summary>
        public void EnsureItemUI()
        {
            if (itemUI == null)
            {
                Transform childUI = transform.Find("İtemUI") ?? transform.Find("ItemUI");
                if (childUI == null)
                {
                    var drag = GetComponentInChildren<DraggableItem>(true);
                    if (drag != null) childUI = drag.transform;
                }

                if (childUI != null)
                {
                    itemUI = childUI.gameObject;
                }
                else
                {
                    GameObject newUI = new GameObject("İtemUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(DraggableItem));
                    newUI.transform.SetParent(transform, false);
                    RectTransform rt = newUI.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    itemUI = newUI;
                }
            }

            if (itemImage == null && itemUI != null)
            {
                itemImage = itemUI.GetComponent<Image>();
            }

            if (itemCountText == null && itemUI != null)
            {
                itemCountText = itemUI.GetComponentInChildren<TextMeshProUGUI>(true);
                if (itemCountText == null)
                {
                    GameObject countObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    countObj.transform.SetParent(itemUI.transform, false);
                    RectTransform crt = countObj.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0.2f, 0.05f);
                    crt.anchorMax = new Vector2(0.95f, 0.45f);
                    crt.offsetMin = Vector2.zero;
                    crt.offsetMax = Vector2.zero;
                    itemCountText = countObj.GetComponent<TextMeshProUGUI>();
                    itemCountText.alignment = TextAlignmentOptions.BottomRight;
                    itemCountText.fontSize = 14;
                    itemCountText.fontStyle = FontStyles.Bold;
                    itemCountText.color = Color.white;
                }
            }

            if (itemUI != null)
            {
                DraggableItem dragScript = itemUI.GetComponent<DraggableItem>();
                if (dragScript == null)
                {
                    dragScript = itemUI.AddComponent<DraggableItem>();
                }
                if (slotItemData != null)
                {
                    dragScript.itemData = slotItemData;
                }
            }
        }
    }
}
