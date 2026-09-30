using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Envanterdeki "Tarifler" butonuna tıklandığında açılan parşömen sistemidir.
    /// Dükkandaki parşömen tıklama alanı (ParchmentClickArea) ile aynı görsel, Canvas ve hiyerarşi yapısını kullanır.
    /// Seçili eşyanın yer aldığı tarif satırlarını tüm sayfa prefablarından tarar,
    /// Öz Tarifleri önce, Diğer Tarifler sonra olacak şekilde,
    /// her sayfada 5 satır limitiyle orijinal parşömen görselinde gösterir.
    /// </summary>
    public class InventoryRecipeParchmentUI : MonoBehaviour
    {
        public static InventoryRecipeParchmentUI Instance { get; private set; }

        private const int ROWS_PER_PAGE = 5;

        // Orijinal sayfa prefablarındaki 5 satırın dikey localPosition yuvaları
        private static readonly Vector3[] SLOT_POSITIONS = new Vector3[]
        {
            new Vector3(-2f,  0.7f, 0f),
            new Vector3(-2f,  0.0f, 0f),
            new Vector3(-2f, -0.7f, 0f),
            new Vector3(-2f, -1.4f, 0f),
            new Vector3(-2f, -2.1f, 0f)
        };

        private GameObject _popupRoot;
        private GameObject _darkOverlay;
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;
        private ParchmentPageController _controller;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_isOpen)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Close();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }
#endif
            }
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        /// <summary>
        /// Seçili eşyaya ait tarif satırlarını orijinal parşömen popup'ı içinde açar.
        /// </summary>
        public static InventoryRecipeParchmentUI Open(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[InventoryRecipeParchmentUI] Tariflerini görüntülemek için bir eşya seçmelisiniz.");
                return null;
            }

            if (Instance != null && Instance._isOpen)
            {
                Instance.Close();
            }

            // 1. Ana Parşömen prefabını bul (Parşomen 1 1)
            GameObject parchmentPrefab = FindMainParchmentPrefab();
            if (parchmentPrefab == null)
            {
                Debug.LogError("[InventoryRecipeParchmentUI] Parşömen prefabı bulunamadı!");
                return null;
            }

            // 2. Sayfa prefablarını bul (Öz Tarifleri ve Diğer Sayfalar)
            GameObject ozPagePrefab = FindOzRecipesPrefab();
            List<GameObject> digerPagePrefabs = FindDigerPagePrefabs();

            // 3. Eşleşen satırları topla (klonlayarak bağımsız hale getir)
            List<GameObject> ozMatchedRows = new List<GameObject>();
            List<GameObject> digerMatchedRows = new List<GameObject>();

            if (ozPagePrefab != null)
            {
                ScanPagePrefabForRows(ozPagePrefab, item, ozMatchedRows);
            }

            foreach (var pagePrefab in digerPagePrefabs)
            {
                if (pagePrefab != null)
                {
                    ScanPagePrefabForRows(pagePrefab, item, digerMatchedRows);
                }
            }

            // TopDropdownMenu açıksa kapat
            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsMenuOpen)
            {
                TopDropdownMenu.Instance.CloseMenu();
            }

            // 4. Parşömen Popup'ını Instantiate et (ParchmentClickArea ile birebir aynı)
            GameObject popup = Instantiate(parchmentPrefab);
            popup.name = $"{parchmentPrefab.name}_RecipePopup";
            popup.SetActive(true);

            // Canvas ayarlarını garantiye al: ScreenSpaceOverlay ve en üst sortingOrder (Envanter 2000 ve Menü 2100'ün üstünde)
            Canvas canvas = popup.GetComponent<Canvas>();
            if (canvas == null) canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 3000;

            // CanvasScaler ayarlarını StorageManager/Envanter ile birebir senkronize et
            CanvasScaler scaler = popup.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = popup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (popup.GetComponent<GraphicRaycaster>() == null)
            {
                popup.AddComponent<GraphicRaycaster>();
            }

            popup.transform.SetAsLastSibling();

            RectTransform rootRect = popup.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = Vector2.zero;
            }

            // UI script ekle
            InventoryRecipeParchmentUI ui = popup.AddComponent<InventoryRecipeParchmentUI>();
            ui._popupRoot = popup;
            ui._isOpen = true;
            Instance = ui;

            // Karanlık Arka Plan (ParchmentDarkBackdrop)
            ui.CreateDarkOverlay(popup);

            // Kapatma butonlarını bağla
            ui.BindCloseButtons(popup);

            // 5. ParchmentPageController'ı al
            ParchmentPageController controller = popup.GetComponent<ParchmentPageController>();
            if (controller == null) controller = popup.AddComponent<ParchmentPageController>();
            ui._controller = controller;

            Transform pageContainer = controller.TargetContainer;

            // 6. Dinamik sayfaları oluştur (Öz Tarifleri önce, Diğer Tarifler sonra - 5 satır/sayfa)
            List<GameObject> dynamicPages = new List<GameObject>();

            // A) Öz Tarifleri sayfaları
            if (ozMatchedRows.Count > 0 && ozPagePrefab != null)
            {
                CreatePagesFromRows(ozMatchedRows, ozPagePrefab, pageContainer, "Oz_Sayfa", dynamicPages);
            }

            // B) Diğer Tarifler sayfaları
            GameObject templateDiger = null;
            if (digerPagePrefabs.Count > 0)
            {
                // Sayfa 3 gibi geniş 3-malzemeli şablonları tercih et, yoksa ilkini al
                templateDiger = digerPagePrefabs.Find(p => p != null && (p.name.Contains("3") || p.name.Contains("4"))) ?? digerPagePrefabs[0];
            }
            if (digerMatchedRows.Count > 0 && templateDiger != null)
            {
                CreatePagesFromRows(digerMatchedRows, templateDiger, pageContainer, "Iksir_Sayfa", dynamicPages);
            }

            // C) Hiç satır bulunamadıysa boş sayfa şablonu göster
            if (dynamicPages.Count == 0)
            {
                GameObject fallbackTemplate = templateDiger != null ? templateDiger : ozPagePrefab;
                if (fallbackTemplate != null)
                {
                    GameObject emptyPage = Instantiate(fallbackTemplate, pageContainer, false);
                    emptyPage.name = "Empty_Sayfa";
                    Transform sayfaTf = FindSayfaTransform(emptyPage.transform);
                    if (sayfaTf != null)
                    {
                        Vector3 sPos = sayfaTf.localPosition;
                        sPos.x = STANDARD_SAYFA_X;
                        sayfaTf.localPosition = sPos;
                        DestroyRowsUnder(sayfaTf);
                    }
                    emptyPage.SetActive(false);
                    dynamicPages.Add(emptyPage);
                }
            }

            // 7. Controller'a yeni sayfaları teslim et (0. sayfayı hemen açar ve butonları bağlar)
            controller.SetPages(dynamicPages);

            Debug.Log($"[InventoryRecipeParchmentUI] '{item.itemName}' için {dynamicPages.Count} sayfa açıldı " +
                      $"(Öz: {ozMatchedRows.Count} satır, Diğer: {digerMatchedRows.Count} satır).");

            return ui;
        }

        public static InventoryRecipeParchmentUI GetOrCreateInstance()
        {
            if (Instance != null) return Instance;
            return Object.FindFirstObjectByType<InventoryRecipeParchmentUI>(FindObjectsInactive.Include);
        }

        public void OpenFor(ItemData item)
        {
            Open(item);
        }

        public void Close()
        {
            _isOpen = false;
            if (_popupRoot != null)
            {
                Destroy(_popupRoot);
                _popupRoot = null;
            }
            _darkOverlay = null;
            _controller = null;
            Instance = null;
            Debug.Log("[InventoryRecipeParchmentUI] Parşömen kapatıldı.");
        }

        private void OnDisable()
        {
            if (_isOpen) Close();
        }

        // ============================================================
        // DİNAMİK SAYFA OLUŞTURMA
        // ============================================================

        private const float STANDARD_SAYFA_X = 26.550837f;

        private static void CreatePagesFromRows(List<GameObject> matchedRows, GameObject templatePrefab,
            Transform container, string namePrefix, List<GameObject> outPages)
        {
            int total = matchedRows.Count;
            int pageCount = Mathf.CeilToInt((float)total / ROWS_PER_PAGE);

            for (int p = 0; p < pageCount; p++)
            {
                GameObject pageObj = Instantiate(templatePrefab, container, false);
                pageObj.name = $"{namePrefix}_{p + 1}";

                Transform sayfaTf = FindSayfaTransform(pageObj.transform);
                if (sayfaTf != null)
                {
                    // Sayfa container pozisyonunu standart 26.55 değerine hizala
                    Vector3 sPos = sayfaTf.localPosition;
                    sPos.x = STANDARD_SAYFA_X;
                    sayfaTf.localPosition = sPos;

                    // Şablondaki eski 5 satırı temizle
                    DestroyRowsUnder(sayfaTf);

                    // Bu sayfaya ait satırları (en fazla 5) parşömen üzerinde tam ortalanmış şekilde yerleştir
                    int start = p * ROWS_PER_PAGE;
                    int count = Mathf.Min(ROWS_PER_PAGE, total - start);

                    for (int s = 0; s < count; s++)
                    {
                        GameObject row = matchedRows[start + s];
                        row.transform.SetParent(sayfaTf, false);

                        // Satırın genişliğini ve malzeme sayısını hesaplayarak parşömende tam ortala
                        float centeredX = CalculateRowCenterX(row.transform, sayfaTf);
                        row.transform.localPosition = new Vector3(centeredX, SLOT_POSITIONS[s].y, 0f);
                        row.transform.localScale = Vector3.one;
                        row.transform.localRotation = Quaternion.identity;
                        row.SetActive(true);
                    }
                }

                pageObj.SetActive(false);
                outPages.Add(pageObj);
            }
        }

        private static float CalculateRowCenterX(Transform row, Transform sayfaTf)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            int childCount = 0;

            foreach (Transform child in row)
            {
                float x = GetChildLocalOrAnchoredX(child);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                childCount++;
            }

            float midX = (childCount > 0 && maxX > minX) ? (minX + maxX) * 0.5f : 0f;

            float sayfaX = sayfaTf.localPosition.x;
            float sayfaScaleX = sayfaTf.localScale.x;
            if (Mathf.Abs(sayfaScaleX) < 0.0001f) sayfaScaleX = 14.486781f;

            // sayfaX + (rowX + midX) * sayfaScaleX = 0 (parşömen merkezi)
            return (-sayfaX / sayfaScaleX) - midX;
        }

        private static float GetChildLocalOrAnchoredX(Transform child)
        {
            RectTransform rt = child as RectTransform;
            if (rt != null && Mathf.Abs(rt.anchoredPosition.x) > 0.0001f)
            {
                return rt.anchoredPosition.x;
            }
            return child.localPosition.x;
        }

        private static Transform FindSayfaTransform(Transform root)
        {
            Transform found = root.Find("Sayfa");
            if (found != null) return found;

            foreach (Transform child in root)
            {
                if (child.name == "Sayfa") return child;
                Transform sub = FindSayfaTransform(child);
                if (sub != null) return sub;
            }
            return root;
        }

        private static void DestroyRowsUnder(Transform sayfaTf)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in sayfaTf)
            {
                string n = child.name;
                if (n == "Satır" || n == "Sat\u0131r" || n.StartsWith("Satır (") || n.StartsWith("Sat\u0131r ("))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var g in toDestroy)
            {
                Destroy(g);
            }
        }

        // ============================================================
        // PREFAB TARAMA VE EŞLEŞTİRME
        // ============================================================

        private static void ScanPagePrefabForRows(GameObject pagePrefab, ItemData targetItem, List<GameObject> outRows)
        {
            if (pagePrefab == null || targetItem == null || targetItem.itemIcon == null) return;

            Sprite targetSprite = targetItem.itemIcon;
            string targetIconName = targetSprite.name;

            // Geçici olarak sayfayı oluşturup satırları incele
            GameObject tempPage = Instantiate(pagePrefab);
            tempPage.SetActive(false);

            Transform sayfaTf = FindSayfaTransform(tempPage.transform);
            if (sayfaTf != null)
            {
                foreach (Transform child in sayfaTf)
                {
                    string n = child.name;
                    if (n == "Satır" || n == "Sat\u0131r" || n.StartsWith("Satır (") || n.StartsWith("Sat\u0131r ("))
                    {
                        if (RowContainsItem(child, targetSprite, targetIconName))
                        {
                            // Eşleşen satırı klonla
                            GameObject cloned = Instantiate(child.gameObject);
                            cloned.name = child.name;
                            cloned.SetActive(false);
                            outRows.Add(cloned);
                        }
                    }
                }
            }

            Destroy(tempPage);
        }

        private static bool RowContainsItem(Transform row, Sprite targetSprite, string targetIconName)
        {
            Image[] images = row.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img == null || img.sprite == null) continue;
                string sName = img.sprite.name;

                // Dekoratif veya sembolik sprite'ları filtrele
                if (sName.Contains("Plus") || sName.Contains("Equal") || sName.Contains("Slot") || sName.Contains("taslak"))
                    continue;

                if (img.sprite == targetSprite || sName.Equals(targetIconName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // ============================================================
        // PREFAB BULMA YARDIMCILARI
        // ============================================================

        private static GameObject FindMainParchmentPrefab()
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.recipeParchmentPrefab != null)
                return InventoryUI.Instance.recipeParchmentPrefab;

            ParchmentClickArea[] allAreas = Object.FindObjectsByType<ParchmentClickArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ca in allAreas)
            {
                if (ca.parchmentPrefab != null && ca.parchmentPrefab.name.Contains("1"))
                    return ca.parchmentPrefab;
            }
            if (allAreas.Length > 0 && allAreas[0].parchmentPrefab != null)
                return allAreas[0].parchmentPrefab;

#if UNITY_EDITOR
            GameObject editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/Parchment/Parşomen 1 1.prefab");
            if (editorPrefab == null) editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Parşomen 1 1.prefab");
            if (editorPrefab != null) return editorPrefab;
#endif
            return null;
        }

        private static GameObject FindOzRecipesPrefab()
        {
#if UNITY_EDITOR
            GameObject oz = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/Parchment/Öz Tarifleri (1).prefab");
            if (oz == null) oz = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/Parşomenler/Öz Tarifleri (1).prefab");
            if (oz != null) return oz;
#endif
            ParchmentClickArea[] allAreas = Object.FindObjectsByType<ParchmentClickArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ca in allAreas)
            {
                if (ca.parchmentPrefab != null)
                {
                    ParchmentPageController ctrl = ca.parchmentPrefab.GetComponent<ParchmentPageController>();
                    if (ctrl != null && ctrl.pagePrefabs != null)
                    {
                        foreach (var p in ctrl.pagePrefabs)
                        {
                            if (p != null && (p.name.Contains("Öz") || p.name.Contains("Oz")))
                                return p;
                        }
                    }
                }
            }
            return null;
        }

        private static List<GameObject> FindDigerPagePrefabs()
        {
            var list = new List<GameObject>();

            GameObject mainParchment = FindMainParchmentPrefab();
            if (mainParchment != null)
            {
                ParchmentPageController ctrl = mainParchment.GetComponent<ParchmentPageController>();
                if (ctrl != null && ctrl.pagePrefabs != null && ctrl.pagePrefabs.Count > 0)
                {
                    foreach (var p in ctrl.pagePrefabs)
                    {
                        if (p != null) list.Add(p);
                    }
                    return list;
                }
            }

#if UNITY_EDITOR
            for (int i = 1; i <= 7; i++)
            {
                GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_PotionTown/Prefabs/UI/Parchment/Sayfa {i} (1).prefab");
                if (p == null) p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_PotionTown/Prefabs/Parşomenler/Sayfa {i} (1).prefab");
                if (p != null) list.Add(p);
            }
#endif
            return list;
        }

        // ============================================================
        // OVERLAY VE BUTON BAĞLANTILARI
        // ============================================================

        private void CreateDarkOverlay(GameObject popup)
        {
            _darkOverlay = new GameObject("ParchmentDarkBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            _darkOverlay.transform.SetParent(popup.transform, false);
            _darkOverlay.transform.SetAsFirstSibling();

            RectTransform overlayRect = _darkOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = new Vector2(-5000f, -5000f);
            overlayRect.offsetMax = new Vector2(5000f, 5000f);

            Image overlayImg = _darkOverlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
            overlayImg.raycastTarget = true;

            Button overlayBtn = _darkOverlay.GetComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(Close);
        }

        private void BindCloseButtons(GameObject popup)
        {
            Button[] allButtons = popup.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string n = btn.gameObject.name.ToLower();
                if (n.Contains("close") || n.Contains("kapat") || n == "x" || n.Contains("x_btn") || n.Contains("back") || n.Contains("geri"))
                {
                    btn.onClick.RemoveListener(Close);
                    btn.onClick.AddListener(Close);
                }
            }
        }
    }
}
