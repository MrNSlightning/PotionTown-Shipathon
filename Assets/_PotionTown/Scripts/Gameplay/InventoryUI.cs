using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Envanter panelinin ana yöneticisidir.
    /// Slotları esnek biçimde keşfeder: Row yapısı, slotsContainer, veya tüm child'lar taranır.
    /// Seçili eşyanın bilgilerini 'Detail Tab' alanında gösterir.
    /// 'Button' nesnesini "Tarifler", 'Other Button' nesnesini "Rafa Yerleştir" olarak yönetir.
    /// Tek sayfa – tüm eşyalar mevcut slotlara sığdırılır.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Tarif Parşömeni Prefabı")]
        [Tooltip("Tarifler butonuna basıldığında açılacak Parşömen ekranı prefabi")]
        public GameObject recipeParchmentPrefab;

        [Header("Slot Yapılandırması")]
        [Tooltip("Slotların yerleşeceği parent Transform. Boşsa otomatik keşif yapılır.")]
        public Transform slotsContainer;
        [Tooltip("Tek bir slot prefabı (dinamik slot üretimi için, isteğe bağlı)")]
        public GameObject slotPrefab;

        [Header("Aksiyon Butonları")]
        [Tooltip("Seçili eşyanın tariflerini parşömen üzerinde açan buton ('Button')")]
        public Button showRecipesButton;
        [Tooltip("Seçili eşyayı açık olan rafa yerleştiren buton ('Other Button')")]
        public Button assignToShelfButton;

        [Header("Kapat Butonu")]
        [Tooltip("Kapat butonu")]
        public Button closeButton;

        [Header("Detail Tab Alanı")]
        public GameObject detailTabRoot;
        public Image detailItemIcon;
        public TextMeshProUGUI detailItemNameText;
        public TextMeshProUGUI detailItemTypeText;
        public TextMeshProUGUI detailItemValueText;
        public TextMeshProUGUI detailItemCountText;

        [Header("Filtreleme")]
        [Tooltip("Özleri ve Scroll'ları ana envanterde gizle")]
        public bool filterEssenceAndScroll = true;

        [Header("Buton Bağlantı Ayarı (OnClick)")]
        [Tooltip("Açık ise kod dinleyicileri otomatik bağlar.")]
        public bool autoHookButtons = true;

        [Header("Boyutlandırma Ayarları")]
        [Tooltip("Envanter panelinin ekran içi ölçeği (0.88 = ekran kenarlarından taşmayan ideal boyut)")]
        public float panelScale = 0.88f;

        private List<InventorySlotUI> _activeSlots = new List<InventorySlotUI>();
        private ItemData _selectedItem;

        private void Awake()
        {
            Instance = this;
            EnsureSortingCanvas();
            EnsureRecipeParchmentPrefab();
            SetupPrefabUI();
            EnsureActionButtons();
            EnsureCloseButton();
        }

        public void EnsureSortingCanvas()
        {
            Canvas parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null)
            {
                Canvas c = GetComponent<Canvas>();
                if (c != null)
                {
                    c.overrideSorting = true;
                    c.sortingLayerName = "UI";
                    c.sortingOrder = 2000;
                }
                ApplyPanelScale();
                return;
            }

            Canvas myCanvas = GetComponent<Canvas>();
            if (myCanvas == null)
            {
                myCanvas = gameObject.AddComponent<Canvas>();
            }
            if (myCanvas.isRootCanvas)
            {
                myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            myCanvas.overrideSorting = true;
            myCanvas.sortingLayerName = "UI";
            myCanvas.sortingOrder = 2000;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            ApplyPanelScale();
        }

        private void ApplyPanelScale()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null && panelScale > 0f && panelScale <= 1.5f)
            {
                rt.localScale = new Vector3(panelScale, panelScale, 1f);
            }
        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshUI;
            }
        }

        public void EnsureRecipeParchmentPrefab()
        {
            if (recipeParchmentPrefab == null)
            {
                ParchmentClickArea clickArea = FindFirstObjectByType<ParchmentClickArea>(FindObjectsInactive.Include);
                if (clickArea != null && clickArea.parchmentPrefab != null)
                {
                    recipeParchmentPrefab = clickArea.parchmentPrefab;
                }

#if UNITY_EDITOR
                if (recipeParchmentPrefab == null)
                {
                    recipeParchmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/Parchment/Parşomen 1 1.prefab");
                }
                if (recipeParchmentPrefab == null)
                {
                    recipeParchmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Parşomen 1 1.prefab");
                }
#endif
            }
        }

        private void OnEnable()
        {
            Instance = this;
            EnsureSortingCanvas();
            EnsureActionButtons();
            EnsureCloseButton();

            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.SetCustomersVisible(false);
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshUI; // Çift kayıt önle
                PlayerInventory.Instance.OnInventoryChanged += RefreshUI;
            }

            if (LicenseManager.Instance != null)
            {
                LicenseManager.OnLicenseChanged -= RefreshUI;
                LicenseManager.OnLicenseChanged += RefreshUI;
            }

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            UpdateAllLocalizedTexts();
            RefreshUI();
        }

        private void OnDisable()
        {
            if (InventoryRecipeParchmentUI.Instance != null)
            {
                InventoryRecipeParchmentUI.Instance.Close();
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshUI;
            }

            if (LicenseManager.Instance != null)
            {
                LicenseManager.OnLicenseChanged -= RefreshUI;
            }

            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.SetCustomersVisible(true);
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateAllLocalizedTexts();
        }

        public void UpdateAllLocalizedTexts()
        {
            if (showRecipesButton != null)
            {
                var tmp = showRecipesButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = LocalizationManager.Get("btn_recipes");
                    UIThemeHelper.ApplyNewRocker(tmp);
                }
            }

            if (assignToShelfButton != null)
            {
                var tmp = assignToShelfButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = LocalizationManager.Get("btn_place_on_shelf");
                    UIThemeHelper.ApplyNewRocker(tmp);
                }
            }

            if (closeButton != null)
            {
                var tmp = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.text.Length > 2)
                {
                    tmp.text = LocalizationManager.Get("btn_close");
                    UIThemeHelper.ApplyNewRocker(tmp);
                }
            }

            UpdateDetailTab(_selectedItem);
        }

        /// <summary>
        /// Slotları esnek biçimde keşfeder. Öncelik sırası:
        /// 1. Inspector'dan atanan slotsContainer altındaki tüm InventorySlotUI veya Image+Button'lı child'lar
        /// 2. Row yapısı (Row, Row (1), Row (2)...) ve altlarındaki "slot" içeren child'lar
        /// 3. Tüm child'lardaki InventorySlotUI component'leri
        /// 4. Son çare: Image + Button olan tüm child'lar otomatik slot olarak kabul edilir
        /// </summary>
        public void SetupPrefabUI()
        {
            _activeSlots.Clear();

            // === YÖNTEM 1: Inspector'dan atanan slotsContainer ===
            if (slotsContainer != null)
            {
                CollectSlotsFromParent(slotsContainer);
            }

            // === YÖNTEM 2: Row yapısı (Row, Row (1), ...) ===
            if (_activeSlots.Count == 0)
            {
                Transform rowParent = transform.Find("Image") ?? transform;
                var rows = new List<Transform>();
                for (int r = 0; r < 10; r++)
                {
                    string rowName = r == 0 ? "Row" : $"Row ({r})";
                    Transform rowT = rowParent.Find(rowName) ?? transform.Find(rowName);
                    if (rowT != null) rows.Add(rowT);
                }

                if (rows.Count > 0)
                {
                    foreach (var row in rows)
                    {
                        CollectSlotsFromParent(row);
                    }
                }
            }

            // === YÖNTEM 3: Mevcut InventorySlotUI component'leri ===
            if (_activeSlots.Count == 0)
            {
                var existing = GetComponentsInChildren<InventorySlotUI>(true);
                foreach (var slotUI in existing)
                {
                    ConfigureSlotElements(slotUI.transform, slotUI);
                    _activeSlots.Add(slotUI);
                }
            }

            // === YÖNTEM 4: Son çare – Image + Button olan tüm child'ları slot yap ===
            if (_activeSlots.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    // Detail Tab, Button, Close gibi bilinen UI elemanlarını atla
                    string lowerName = child.name.ToLower();
                    if (lowerName.Contains("detail") || lowerName.Contains("close") ||
                        lowerName.Contains("button") || lowerName.Contains("other") ||
                        lowerName.Contains("ok ") || lowerName.Contains("page"))
                        continue;

                    // Alt child'ları tara (satır mantığı olmasa bile Grid, Panel vs. içindeki slotları bul)
                    CollectSlotsFromParent(child);
                }
            }

            if (_activeSlots.Count == 0)
            {
                Debug.LogWarning($"<color=yellow>[InventoryUI]</color> Hiç slot bulunamadı! " +
                    "Inspector'dan 'Slots Container' alanına slotların bulunduğu parent objeyi sürükleyin.");
            }
            else
            {
                Debug.Log($"<color=green>[InventoryUI]</color> {_activeSlots.Count} slot keşfedildi.");
            }

            // Aksiyon Butonlarını bağla
            EnsureActionButtons();

            // Detail Tab Alanını yapılandır
            if (detailTabRoot == null)
            {
                Transform dtT = transform.Find("Detail Tab");
                if (dtT != null)
                {
                    detailTabRoot = dtT.gameObject;
                }
            }

            if (detailTabRoot != null)
            {
                Image dtBg = detailTabRoot.GetComponent<Image>();
                if (dtBg != null && dtBg.sprite == null)
                {
                    dtBg.color = Color.clear;
                }
                EnsureDetailTabElements();
            }

            EnsureCloseButton();
        }

        /// <summary>
        /// Belirtilen parent altındaki tüm uygun child'ları slot olarak toplar.
        /// "slot" kelimesi aramaz – Image bileşeni olan her child'ı kabul eder.
        /// </summary>
        private void CollectSlotsFromParent(Transform parent)
        {
            if (parent == null) return;

            foreach (Transform child in parent)
            {
                // UI butonlarını, panellerini ve satırları atla
                string lowerName = child.name.ToLower();
                if (lowerName.Contains("detail") || lowerName.Contains("close") ||
                    lowerName.Contains("button") || lowerName.Contains("other") ||
                    lowerName.Contains("ok ") || lowerName.Contains("page") ||
                    lowerName.StartsWith("row"))
                    continue;

                // Zaten eklenmiş mi kontrol et
                var existingSlotUI = child.GetComponent<InventorySlotUI>();
                if (existingSlotUI != null && _activeSlots.Contains(existingSlotUI))
                    continue;

                // Image bileşeni olan child'lar slot olarak kabul edilir
                Image img = child.GetComponent<Image>();
                if (img == null) continue;

                // Button yoksa ekle (tıklanabilir olması için)
                Button btn = child.GetComponent<Button>();
                if (btn == null)
                {
                    btn = child.gameObject.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                }

                // InventorySlotUI yoksa ekle
                InventorySlotUI slotUI = existingSlotUI;
                if (slotUI == null)
                {
                    slotUI = child.gameObject.AddComponent<InventorySlotUI>();
                }

                ConfigureSlotElements(child, slotUI);
                _activeSlots.Add(slotUI);
            }
        }

        private void ConfigureSlotElements(Transform slotChild, InventorySlotUI slotUI)
        {
            slotChild.gameObject.SetActive(true);

            // Eski StorageButton varsa kaldır
            var sb = slotChild.GetComponent<StorageButton>();
            if (sb != null)
            {
                Destroy(sb);
            }

            // Slot üzerindeki Image doğrudan ikon olarak kullanılacak
            Image slotImg = slotChild.GetComponent<Image>();
            if (slotImg != null)
            {
                slotImg.preserveAspect = true;
                if (slotImg.sprite == null)
                {
                    slotImg.color = Color.clear;
                }
            }

            // Icon child objesi varsa onu tercih et (slot arka planı + ayrı ikon yapısı)
            Transform childIcon = slotChild.Find("Icon");
            if (childIcon != null)
            {
                Image iconImg = childIcon.GetComponent<Image>();
                if (iconImg != null)
                {
                    slotUI.iconImage = iconImg;
                    slotUI.slotBackgroundImage = slotImg; // Ana Image arka plan olur
                }
                else
                {
                    slotUI.iconImage = slotImg;
                    slotUI.slotBackgroundImage = null;
                }
            }
            else
            {
                slotUI.iconImage = slotImg;
                slotUI.slotBackgroundImage = null;
            }

            // CountText Child'ı kontrol et / oluştur
            Transform countT = slotChild.Find("CountText");
            TextMeshProUGUI countTmp = null;
            if (countT == null)
            {
                // Mevcut TMP child var mı kontrol et
                countTmp = slotChild.GetComponentInChildren<TextMeshProUGUI>();
                if (countTmp == null)
                {
                    GameObject countObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    countObj.transform.SetParent(slotChild, false);
                    RectTransform crt = countObj.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0.2f, 0.05f);
                    crt.anchorMax = new Vector2(0.95f, 0.45f);
                    crt.offsetMin = Vector2.zero;
                    crt.offsetMax = Vector2.zero;
                    countTmp = countObj.GetComponent<TextMeshProUGUI>();
                    countTmp.alignment = TextAlignmentOptions.BottomRight;
                    countTmp.fontSize = 13;
                    countTmp.fontStyle = FontStyles.Bold;
                    countTmp.color = Color.white;
                    countTmp.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            else
            {
                countTmp = countT.GetComponent<TextMeshProUGUI>();
            }

            slotUI.button = slotChild.GetComponent<Button>();
            slotUI.countText = countTmp;
        }

        private void EnsureDetailTabElements()
        {
            if (detailTabRoot == null)
            {
                Transform dt = transform.Find("Detail Tab");
                if (dt != null) detailTabRoot = dt.gameObject;
            }

            if (detailTabRoot == null) return;

            Transform contentT = detailTabRoot.transform.Find("Content");
            Transform searchRoot = contentT != null ? contentT : detailTabRoot.transform;

            if (detailItemIcon == null)
            {
                Transform iconT = searchRoot.Find("IconSlot/Icon") ?? searchRoot.Find("Icon");
                if (iconT != null) detailItemIcon = iconT.GetComponent<Image>();
            }

            if (detailItemNameText == null)
            {
                Transform nameT = searchRoot.Find("ItemNameText");
                if (nameT != null) detailItemNameText = nameT.GetComponent<TextMeshProUGUI>();
            }

            if (detailItemTypeText == null)
            {
                Transform typeT = searchRoot.Find("ItemTypeText");
                if (typeT != null) detailItemTypeText = typeT.GetComponent<TextMeshProUGUI>();
            }

            if (detailItemValueText == null)
            {
                Transform valT = searchRoot.Find("ItemValueText");
                if (valT != null) detailItemValueText = valT.GetComponent<TextMeshProUGUI>();
            }

            if (detailItemCountText == null)
            {
                Transform countT = searchRoot.Find("ItemCountText");
                if (countT != null) detailItemCountText = countT.GetComponent<TextMeshProUGUI>();
            }
        }

        public void EnsureActionButtons()
        {
            // 1. Button (Tarifler)
            if (showRecipesButton == null)
            {
                Transform btnT = transform.Find("Button");
                if (btnT != null)
                {
                    showRecipesButton = btnT.GetComponent<Button>();
                }
            }

            if (showRecipesButton != null)
            {
                var cr = showRecipesButton.GetComponent<CanvasRenderer>();
                if (cr != null) cr.cullTransparentMesh = false;

                var img = showRecipesButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = Color.clear;
                    img.raycastTarget = true;
                }

                showRecipesButton.transition = Selectable.Transition.None;
                showRecipesButton.targetGraphic = null;

                var tmp = showRecipesButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.raycastTarget = false;
                }

                showRecipesButton.interactable = true;
                HookButton(showRecipesButton, OnShowRecipesClicked);
            }

            // 2. Other Button (Rafa Yerleştir)
            if (assignToShelfButton == null)
            {
                Transform otherBtnT = transform.Find("Other Button");
                if (otherBtnT != null)
                {
                    assignToShelfButton = otherBtnT.GetComponent<Button>();
                }
            }

            if (assignToShelfButton != null)
            {
                var cr = assignToShelfButton.GetComponent<CanvasRenderer>();
                if (cr != null) cr.cullTransparentMesh = false;

                var img = assignToShelfButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = Color.clear;
                    img.raycastTarget = true;
                }

                assignToShelfButton.transition = Selectable.Transition.None;
                assignToShelfButton.targetGraphic = null;

                var tmp = assignToShelfButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.raycastTarget = false;
                }

                assignToShelfButton.interactable = true;
                HookButton(assignToShelfButton, OnAssignToShelfClicked);
            }
        }

        private void EnsureCloseButton()
        {
            if (closeButton == null)
            {
                Transform closeT = transform.Find("Close_Button") ?? transform.Find("Close_Button (1)");
                if (closeT != null)
                {
                    closeButton = closeT.GetComponent<Button>();
                }
                else
                {
                    foreach (var b in GetComponentsInChildren<Button>(true))
                    {
                        if (b.name.ToLower().Contains("close"))
                        {
                            closeButton = b;
                            break;
                        }
                    }
                }
            }

            HookButton(closeButton, Close);
        }

        public void HookButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null || action == null) return;

            if (autoHookButtons || !HasValidPersistentListener(btn))
            {
                btn.onClick.RemoveListener(action);
                btn.onClick.AddListener(action);
            }
        }

        public bool HasValidPersistentListener(Button btn)
        {
            if (btn == null) return false;
            int count = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (btn.onClick.GetPersistentTarget(i) != null && !string.IsNullOrEmpty(btn.onClick.GetPersistentMethodName(i)))
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateDetailTab(ItemData item)
        {
            if (detailTabRoot == null) return;

            if (item == null)
            {
                if (detailItemIcon != null)
                {
                    detailItemIcon.sprite = null;
                    detailItemIcon.color = Color.clear;
                    detailItemIcon.gameObject.SetActive(false);
                }
                if (detailItemNameText != null) detailItemNameText.text = LocalizationManager.Get("inv_select_item");
                if (detailItemTypeText != null) detailItemTypeText.text = "";
                if (detailItemValueText != null) detailItemValueText.text = "";
                if (detailItemCountText != null) detailItemCountText.text = "";
                return;
            }

            if (detailItemIcon != null)
            {
                detailItemIcon.sprite = item.itemIcon;
                detailItemIcon.color = Color.white;
                detailItemIcon.preserveAspect = true;
                detailItemIcon.gameObject.SetActive(item.itemIcon != null);
            }

            if (detailItemNameText != null)
            {
                detailItemNameText.text = item.LocalizedName;
            }

            if (detailItemTypeText != null)
            {
                detailItemTypeText.text = string.Format(LocalizationManager.Get("inv_type_format"), item.TypeDisplayName);
            }

            if (detailItemValueText != null)
            {
                int val = item.basePrice > 0 ? item.basePrice : item.buyPrice;
                detailItemValueText.text = string.Format(LocalizationManager.Get("inv_value_format"), val);
            }

            if (detailItemCountText != null && PlayerInventory.Instance != null)
            {
                int count = PlayerInventory.Instance.GetItemCount(item);
                detailItemCountText.text = string.Format(LocalizationManager.Get("inv_count_format"), count);
            }
        }

        public void RefreshUI()
        {
            if (PlayerInventory.Instance == null) return;

            // 1. Envanterdeki eşyaları filtreli olarak çek
            var items = PlayerInventory.Instance.GetInventoryItems(filterEssenceAndScroll);

            // 2. Slotları hazırla ve doldur (tek sayfa)
            _activeSlots.RemoveAll(s => s == null);
            if (_activeSlots.Count == 0)
            {
                SetupPrefabUI();
            }

            for (int i = 0; i < _activeSlots.Count; i++)
            {
                InventorySlotUI slot = _activeSlots[i];
                if (slot == null) continue;

                if (i < items.Count)
                {
                    var entry = items[i];
                    slot.SetupSlot(entry.Key, entry.Value, OnSlotClicked);
                }
                else
                {
                    slot.SetupSlot(null, 0, null);
                }
            }

            // 3. Detail Tab'i güncelle
            if (_selectedItem != null)
            {
                UpdateDetailTab(_selectedItem);
            }
            else if (items.Count > 0)
            {
                _selectedItem = items[0].Key;
                UpdateDetailTab(_selectedItem);
            }
            else
            {
                UpdateDetailTab(null);
            }
        }

        private void OnSlotClicked(ItemData item)
        {
            if (item == null) return;
            _selectedItem = item;
            UpdateDetailTab(item);
        }

        public void ShowRecipes() => OnShowRecipesClicked();
        public void AssignToShelf() => OnAssignToShelfClicked();

        public void OnShowRecipesClicked()
        {
            Debug.Log("[InventoryUI] Tarifler butonuna tıklandı.");

            if (_selectedItem == null)
            {
                var items = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetInventoryItems(filterEssenceAndScroll) : null;
                if (items != null && items.Count > 0)
                {
                    _selectedItem = items[0].Key;
                    UpdateDetailTab(_selectedItem);
                }
            }

            if (_selectedItem != null)
            {
                InventoryRecipeParchmentUI.Open(_selectedItem);
            }
            else
            {
                Debug.LogWarning("[InventoryUI] Tarifler için seçili veya gösterilecek bir eşya bulunamadı.");
            }
        }

        public void OnAssignToShelfClicked()
        {
            if (_selectedItem == null)
            {
                Debug.LogWarning("Rafa yerleştirmek için önce bir eşya seçmelisiniz.");
                return;
            }

            if (StorageManager.Instance != null && StorageManager.Instance.CurrentSelectedSlot != null)
            {
                StorageManager.Instance.SelectItemForSlot(_selectedItem);
                Debug.Log($"{_selectedItem.itemName} rafa yerleştirildi.");
            }
            else
            {
                Debug.Log("Herhangi bir raf seçili değil.");
            }
        }

        /// <summary>
        /// Envanteri açar. Butondan çağırmak için bu metodu kullanın.
        /// StorageManager varsa onun üzerinden, yoksa direkt SetActive ile açar.
        /// </summary>
        public void OpenInventory()
        {
            if (StorageManager.Instance != null)
            {
                StorageManager.Instance.OpenStorageViewOnly();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Close()
        {
            if (InventoryRecipeParchmentUI.Instance != null)
            {
                InventoryRecipeParchmentUI.Instance.Close();
            }

            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.SetCustomersVisible(true);
            }

            if (StorageManager.Instance != null)
            {
                StorageManager.Instance.CloseStorage();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
