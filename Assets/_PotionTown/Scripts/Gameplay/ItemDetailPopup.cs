using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Envanterdeki bir eşyaya tıklandığında açılan bilgi penceresidir.
    /// Eşyanın adını, türünü, değerini ve envanterdeki adetini gösterir.
    /// "Tarifleri Gör" ve "Rafa Yerleştir" butonlarını barındırır.
    /// </summary>
    public class ItemDetailPopup : MonoBehaviour
    {
        public static ItemDetailPopup Instance { get; private set; }

        [Header("Panel Kapsayıcısı")]
        public GameObject panelRoot;

        [Header("Görsel ve Metin Referansları")]
        public Image itemIconImage;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemTypeText;
        public TextMeshProUGUI itemValueText;
        public TextMeshProUGUI ownedCountText;

        [Header("Butonlar")]
        public Button showRecipesButton;
        public Button assignToShelfButton;
        public Button closeButton;

        [Header("Tarif Parşömeni Referansı")]
        public InventoryRecipeParchmentUI recipeParchmentUI;

        private ItemData _currentItem;

        private void Awake()
        {
            Instance = this;
            if (panelRoot == null) panelRoot = gameObject;

            EnsureAutoSetup();

            if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
                closeButton.onClick.AddListener(Close);

            if (showRecipesButton != null && showRecipesButton.onClick.GetPersistentEventCount() == 0)
                showRecipesButton.onClick.AddListener(OnShowRecipesClicked);

            if (assignToShelfButton != null && assignToShelfButton.onClick.GetPersistentEventCount() == 0)
                assignToShelfButton.onClick.AddListener(OnAssignToShelfClicked);

            // Başlangıçta gizle
            if (panelRoot != null && panelRoot == gameObject)
                gameObject.SetActive(false);
            else if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            if (_currentItem != null)
            {
                Open(_currentItem);
            }
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (_currentItem != null && (panelRoot != null ? panelRoot.activeInHierarchy : gameObject.activeInHierarchy))
            {
                Open(_currentItem);
            }
        }

        private void EnsureAutoSetup()
        {
            if (itemIconImage == null)
            {
                Transform iconT = transform.Find("Content/IconSlot/Icon");
                if (iconT != null) itemIconImage = iconT.GetComponent<Image>();
            }

            if (itemNameText == null)
            {
                Transform t = transform.Find("Content/TitleText");
                if (t != null) itemNameText = t.GetComponent<TextMeshProUGUI>();
            }

            if (itemTypeText == null)
            {
                Transform t = transform.Find("Content/TypeText");
                if (t != null) itemTypeText = t.GetComponent<TextMeshProUGUI>();
            }

            if (itemValueText == null)
            {
                Transform t = transform.Find("Content/ValueText");
                if (t != null) itemValueText = t.GetComponent<TextMeshProUGUI>();
            }

            if (ownedCountText == null)
            {
                Transform t = transform.Find("Content/CountText");
                if (t != null) ownedCountText = t.GetComponent<TextMeshProUGUI>();
            }

            if (showRecipesButton == null)
            {
                Transform t = transform.Find("Content/ShowRecipesBtn");
                if (t != null) showRecipesButton = t.GetComponent<Button>();
            }

            if (assignToShelfButton == null)
            {
                Transform t = transform.Find("Content/AssignToShelfBtn");
                if (t != null) assignToShelfButton = t.GetComponent<Button>();
            }

            if (closeButton == null)
            {
                Transform t = transform.Find("CloseBtn");
                if (t != null) closeButton = t.GetComponent<Button>();
            }

            if (recipeParchmentUI == null)
            {
                recipeParchmentUI = FindFirstObjectByType<InventoryRecipeParchmentUI>(FindObjectsInactive.Include);
            }
        }

        public void Open(ItemData item)
        {
            if (item == null) return;
            _currentItem = item;

            if (panelRoot != null) panelRoot.SetActive(true);
            else gameObject.SetActive(true);

            // 1. İkon
            if (itemIconImage != null)
            {
                itemIconImage.sprite = item.itemIcon;
                itemIconImage.gameObject.SetActive(item.itemIcon != null);
            }

            // 2. İsim
            if (itemNameText != null)
            {
                itemNameText.text = item.LocalizedName;
            }

            // 3. Tür
            if (itemTypeText != null)
            {
                itemTypeText.text = string.Format(LocalizationManager.Get("inv_type_format"), item.TypeDisplayName);
            }

            // 4. Değer (Altın)
            if (itemValueText != null)
            {
                int val = item.basePrice > 0 ? item.basePrice : item.buyPrice;
                itemValueText.text = string.Format(LocalizationManager.Get("inv_value_format"), val);
            }

            // 5. Mevcut Miktar
            if (ownedCountText != null && PlayerInventory.Instance != null)
            {
                int count = PlayerInventory.Instance.GetItemCount(item);
                ownedCountText.text = string.Format(LocalizationManager.Get("inv_count_format"), count);
            }

            // 6. Buton Metinleri
            if (showRecipesButton != null)
            {
                var txt = showRecipesButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = LocalizationManager.Get("btn_recipes");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }

            if (assignToShelfButton != null)
            {
                var txt = assignToShelfButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = LocalizationManager.Get("btn_place_on_shelf");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }

            if (closeButton != null)
            {
                var txt = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null && !string.IsNullOrEmpty(txt.text) && txt.text.Length > 2)
                {
                    txt.text = LocalizationManager.Get("btn_close");
                    UIThemeHelper.ApplyNewRocker(txt);
                }
            }

            // 7. Rafa Koyma Butonu Kontrolü
            if (assignToShelfButton != null)
            {
                bool canAssign = StorageManager.Instance != null && StorageManager.Instance.CurrentSelectedSlot != null;
                assignToShelfButton.gameObject.SetActive(canAssign);
            }

            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null && panelRoot != gameObject)
                panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);

            _currentItem = null;
        }

        public void ShowRecipes() => OnShowRecipesClicked();
        public void AssignToShelf() => OnAssignToShelfClicked();

        public void OnShowRecipesClicked()
        {
            if (_currentItem == null) return;

            recipeParchmentUI = InventoryRecipeParchmentUI.Open(_currentItem);
        }

        public void OnAssignToShelfClicked()
        {
            if (_currentItem == null) return;

            if (StorageManager.Instance != null)
            {
                StorageManager.Instance.SelectItemForSlot(_currentItem);
            }
            Close();
        }



        /// <summary>
        /// Sahnede hazır popup UI yoksa dinamik olarak şık bir popup oluşturur.
        /// </summary>
        public static ItemDetailPopup CreateDefaultPopup(Transform parent)
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_PotionTown/Prefabs/UI/Inventory/ItemDetailPopup.prefab");
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, parent, false);
                instance.name = "ItemDetailPopup";
                instance.SetActive(false);
                return instance.GetComponent<ItemDetailPopup>();
            }
#endif
            GameObject root = new GameObject("ItemDetailPopup", typeof(RectTransform), typeof(Image), typeof(ItemDetailPopup));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 440);
            rt.anchoredPosition = Vector2.zero;

            Image bg = root.GetComponent<Image>();
#if UNITY_EDITOR
            Sprite bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Envanter.png");
#else
            Sprite bgSprite = null;
#endif
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.15f, 0.11f, 0.08f, 0.98f);
            }

            ItemDetailPopup popup = root.GetComponent<ItemDetailPopup>();
            popup.panelRoot = root;

            // Content container
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(root.transform, false);
            RectTransform crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(25, 25);
            crt.offsetMax = new Vector2(-25, -25);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = LocalizationManager.Get("inv_select_item");
            titleTmp.fontSize = 22;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.96f, 0.85f, 0.55f, 1f);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            popup.itemNameText = titleTmp;

            // 2. Icon Slot Frame
            GameObject iconSlotObj = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
            iconSlotObj.transform.SetParent(contentObj.transform, false);
            RectTransform slotRt = iconSlotObj.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(85, 85);
            Image slotBg = iconSlotObj.GetComponent<Image>();
#if UNITY_EDITOR
            Sprite slotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/Slot.png");
#else
            Sprite slotSprite = null;
#endif
            if (slotSprite != null) { slotBg.sprite = slotSprite; slotBg.color = Color.white; }
            else { slotBg.color = new Color(0.25f, 0.18f, 0.12f, 0.9f); }

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(iconSlotObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(6, 6);
            irt.offsetMax = new Vector2(-6, -6);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            popup.itemIconImage = iconImg;

            // 3. Type Text
            GameObject typeObj = new GameObject("TypeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI typeTmp = typeObj.GetComponent<TextMeshProUGUI>();
            typeTmp.text = "";
            typeTmp.fontSize = 16;
            typeTmp.alignment = TextAlignmentOptions.Center;
            typeTmp.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            typeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            popup.itemTypeText = typeTmp;

            // 4. Value Text
            GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI valTmp = valObj.GetComponent<TextMeshProUGUI>();
            valTmp.text = "";
            valTmp.fontSize = 16;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.alignment = TextAlignmentOptions.Center;
            valTmp.color = new Color(1f, 0.85f, 0.3f, 1f);
            valObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            popup.itemValueText = valTmp;

            // 5. Owned Count Text
            GameObject countObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI countTmp = countObj.GetComponent<TextMeshProUGUI>();
            countTmp.text = "";
            countTmp.fontSize = 16;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            countObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            popup.ownedCountText = countTmp;

            // 6. Show Recipes Button
            GameObject recipesBtnObj = CreateButton(contentObj.transform, "ShowRecipesBtn", LocalizationManager.Get("btn_recipes"), new Vector2(240, 40), new Color(0.7f, 0.45f, 0.2f, 1f));
            popup.showRecipesButton = recipesBtnObj.GetComponent<Button>();

            // 7. Assign to Shelf Button
            GameObject shelfBtnObj = CreateButton(contentObj.transform, "AssignToShelfBtn", LocalizationManager.Get("btn_place_on_shelf"), new Vector2(240, 40), new Color(0.2f, 0.6f, 0.3f, 1f));
            popup.assignToShelfButton = shelfBtnObj.GetComponent<Button>();

            // Close Button (Sağ üst)
            GameObject closeBtnObj = CreateButton(root.transform, "CloseBtn", "X", new Vector2(34, 34), new Color(0.6f, 0.2f, 0.2f, 1f));
            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-10, -10);
            popup.closeButton = closeBtnObj.GetComponent<Button>();

            root.SetActive(false);
            return popup;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 size, Color fallbackColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
#if UNITY_EDITOR
            Sprite btnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_PotionTown/Art/UI/General Buton.png");
#else
            Sprite btnSprite = null;
#endif
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = fallbackColor;
            }

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }
    }
}
