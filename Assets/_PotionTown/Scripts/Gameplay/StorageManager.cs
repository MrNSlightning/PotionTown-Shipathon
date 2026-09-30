using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    public class StorageManager : MonoBehaviour
    {
        public static StorageManager Instance { get; private set; }

        [Header("UI Referansları")]
        public GameObject storagePanel; // Eşya seçme ekranı (Scroll view vs.)

        [Header("Boyutlandırma Ayarları")]
        [Tooltip("Envanter panelinin ekran içi ölçeği (0.88 = ekran kenarlarından taşmayan ideal boyut)")]
        public float panelScale = 0.88f;
        
        // Hangi raf karesine tıkladıysak onu hafızada tutarız
        public ShelfSlot CurrentSelectedSlot => currentSelectedSlot;
        private ShelfSlot currentSelectedSlot;

        public bool IsStorageOpen => (storagePanel != null && storagePanel.activeInHierarchy) || 
                                     (transform.Find("EnvanterPaneli (1)") != null && transform.Find("EnvanterPaneli (1)").gameObject.activeInHierarchy);

        public event System.Action OnStorageOpened;
        public event System.Action OnStorageClosed;
        public event System.Action<ItemData> OnItemSelected;

        private void Awake()
        {
            Instance = this;
            RemoveDarkBackdrop();
            EnsureCanvasSetup();
            EnsureStoragePanel();
            if (storagePanel != null) storagePanel.SetActive(false);
        }

        private void OnEnable()
        {
            Instance = this;
            RemoveDarkBackdrop();
            EnsureCanvasSetup();
            EnsureStoragePanel();
            if (storagePanel != null) storagePanel.SetActive(false);
        }

        private void Start()
        {
            RemoveDarkBackdrop();
            if (storagePanel != null) storagePanel.SetActive(false);
            Transform localChild = transform.Find("EnvanterPaneli (1)");
            if (localChild != null) localChild.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureCanvasSetup()
        {
            RemoveDarkBackdrop();

            // StorageManager'ın kendisini bağımsız ScreenSpaceOverlay Canvas yaparak
            // tüm sahnelerde (Lobi, İksir Satış, İksir Yapma, Malzeme Dükkanı)
            // envanter panelinin ekranın tam ortasında düzgünce görünmesini sağlar.
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 2000;

            UnityEngine.UI.CanvasScaler scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            }
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            CenterAndNormalizePanel();
        }

        public void CenterAndNormalizePanel()
        {
            // 1. Envanter panel objesini bul
            Transform panelTransform = null;
            if (storagePanel != null)
            {
                panelTransform = storagePanel.transform;
            }
            else
            {
                panelTransform = transform.Find("EnvanterPaneli (1)");
                if (panelTransform == null) panelTransform = transform.Find("EnvanterPaneli (2)");
                if (panelTransform == null)
                {
                    foreach (Transform c in transform)
                    {
                        if (c.name.Contains("EnvanterPaneli")) { panelTransform = c; break; }
                    }
                }
                if (panelTransform != null) storagePanel = panelTransform.gameObject;
            }

            if (panelTransform != null)
            {
                NormalizeRect(panelTransform.GetComponent<RectTransform>(), panelScale);

                // Altındaki parşömen arkaplanı ve slot konteyneri olan Image objesi
                Transform imgChild = panelTransform.Find("Image");
                if (imgChild != null)
                {
                    NormalizeRect(imgChild.GetComponent<RectTransform>(), 1f);

                    // Image üzerinde Canvas varsa, nested olarak UI katmanına al
                    Canvas imgCanvas = imgChild.GetComponent<Canvas>();
                    if (imgCanvas != null)
                    {
                        imgCanvas.overrideSorting = true;
                        imgCanvas.sortingLayerName = "UI";
                        imgCanvas.sortingOrder = 2000;
                    }
                }
            }
        }

        private void NormalizeRect(RectTransform rt, float scale = 1f)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1823f, 1070f);
            rt.localScale = new Vector3(scale, scale, 1f);
            rt.localRotation = Quaternion.identity;
        }

        public void EnsureStoragePanel()
        {
            EnsureCanvasSetup();

            if (storagePanel == null)
            {
                // 1. Önce doğrudan kendi altındaki EnvanterPaneli objesini kontrol et
                Transform localChild = transform.Find("EnvanterPaneli (1)");
                if (localChild == null) localChild = transform.Find("EnvanterPaneli (2)");
                if (localChild == null)
                {
                    foreach (Transform c in transform)
                    {
                        if (c.name.Contains("EnvanterPaneli")) { localChild = c; break; }
                    }
                }
                if (localChild != null)
                {
                    storagePanel = localChild.gameObject;
                }

                // 2. Kendi altındaki InventoryUI objesini kontrol et
                if (storagePanel == null)
                {
                    var localInv = GetComponentInChildren<InventoryUI>(true);
                    if (localInv != null)
                    {
                        if (localInv.transform.parent != null && localInv.transform.parent != transform && localInv.transform.parent.name.Contains("EnvanterPaneli"))
                        {
                            storagePanel = localInv.transform.parent.gameObject;
                        }
                        else
                        {
                            storagePanel = localInv.gameObject;
                        }
                    }
                }

                // 3. Aynı Canvas altındaki EnvanterPaneli prefab örneğini bul
                if (storagePanel == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var invUIs = canvas.GetComponentsInChildren<InventoryUI>(true);
                        if (invUIs.Length > 0)
                        {
                            storagePanel = invUIs[0].gameObject;
                        }
                        else
                        {
                            foreach (Transform child in canvas.transform)
                            {
                                if (child.name.Contains("EnvanterPaneli"))
                                {
                                    storagePanel = child.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                }

                // 4. Sahne genelinde ara
                if (storagePanel == null)
                {
                    var allInv = FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (allInv.Length > 0)
                    {
                        storagePanel = allInv[0].gameObject;
                    }
                }
            }

            CenterAndNormalizePanel();

            if (storagePanel != null)
            {
                var invUI = storagePanel.GetComponentInChildren<InventoryUI>(true);
                if (invUI == null)
                {
                    storagePanel.AddComponent<InventoryUI>();
                }
            }
        }

        public void RemoveDarkBackdrop()
        {
            Transform found = transform.Find("StorageDarkBackdrop");
            while (found != null)
            {
                if (Application.isPlaying) Destroy(found.gameObject);
                else DestroyImmediate(found.gameObject);
                found = transform.Find("StorageDarkBackdrop");
            }

            // Canvas altındaki veya sahnedeki artık StorageDarkBackdrop objelerini de temizle
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    Transform t = canvas.transform.GetChild(i);
                    if (t != null && t.name == "StorageDarkBackdrop")
                    {
                        if (Application.isPlaying) Destroy(t.gameObject);
                        else DestroyImmediate(t.gameObject);
                    }
                }
            }
        }

        public void OpenStorageForSlot(ShelfSlot slot)
        {
            Instance = this;
            currentSelectedSlot = slot;

            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsMenuOpen)
            {
                TopDropdownMenu.Instance.CloseMenu();
            }

            RemoveDarkBackdrop();
            EnsureCanvasSetup();
            EnsureStoragePanel();

            if (storagePanel != null)
            {
                storagePanel.SetActive(true);
                Transform img = storagePanel.transform.Find("Image");
                if (img != null) img.gameObject.SetActive(true);

                storagePanel.transform.SetAsLastSibling();
                transform.SetAsLastSibling(); // StorageManager'ı da en öne çek
                CenterAndNormalizePanel();

                var invUI = storagePanel.GetComponentInChildren<InventoryUI>(true);
                if (invUI != null)
                {
                    invUI.RefreshUI();
                }
            }
            OnStorageOpened?.Invoke();
        }

        // Genel kullanım için (sadece envanteri görmek için)
        public void OpenStorageViewOnly()
        {
            Instance = this;
            currentSelectedSlot = null;

            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsMenuOpen)
            {
                TopDropdownMenu.Instance.CloseMenu();
            }

            RemoveDarkBackdrop();
            EnsureCanvasSetup();
            EnsureStoragePanel();

            if (storagePanel != null)
            {
                storagePanel.SetActive(true);
                Transform img = storagePanel.transform.Find("Image");
                if (img != null) img.gameObject.SetActive(true);

                storagePanel.transform.SetAsLastSibling();
                transform.SetAsLastSibling();
                CenterAndNormalizePanel();

                var invUI = storagePanel.GetComponentInChildren<InventoryUI>(true);
                if (invUI != null)
                {
                    invUI.RefreshUI();
                }
            }
            OnStorageOpened?.Invoke();
        }

        public void CloseStorage()
        {
            RemoveDarkBackdrop();

            if (storagePanel != null)
            {
                storagePanel.SetActive(false);
            }
            Transform localChild = transform.Find("EnvanterPaneli (1)");
            if (localChild != null) localChild.gameObject.SetActive(false);

            currentSelectedSlot = null;
            OnStorageClosed?.Invoke();
        }

        // Depodaki bir eşya butonuna tıklandığında çağrılacak
        public void SelectItemForSlot(ItemData selectedItem)
        {
            if (currentSelectedSlot != null && selectedItem != null)
            {
                // Dükkan açıkken rafta zaten eşya varsa değiştirilmesini engelle
                if (GameManager.Instance != null && GameManager.Instance.isShopOpen && currentSelectedSlot.slotItemData != null)
                {
                    ToastNotificationUI.ShowWarning("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!", currentSelectedSlot.transform);
                    return;
                }

                currentSelectedSlot.AssignItem(selectedItem);
                CloseStorage();
            }
            OnItemSelected?.Invoke(selectedItem);
        }

        // Seçili raftaki eşyayı temizler ve depoyu kapatır
        public void ClearItemFromCurrentSlot()
        {
            if (currentSelectedSlot != null)
            {
                if (GameManager.Instance != null && GameManager.Instance.isShopOpen && currentSelectedSlot.slotItemData != null)
                {
                    ToastNotificationUI.ShowWarning("Dükkan açıkken yerleştirilmiş eşyalar değiştirilemez!", currentSelectedSlot.transform);
                    return;
                }

                currentSelectedSlot.ClearItem();
                CloseStorage();
            }
        }
    }
}
