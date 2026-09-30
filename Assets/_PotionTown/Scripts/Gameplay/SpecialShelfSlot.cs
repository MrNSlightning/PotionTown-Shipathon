using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PotionShop
{
    /// <summary>
    /// SpecialsShelf (Özel Eşyalar Rafı) üzerindeki 6 sabit yuvayı yönetir.
    /// Satın alınmadan önce gri (silüet) görünür ve sürüklenemez.
    /// Malzeme dükkanından satın alındığında canlı renge döner ve sürüklenebilir:
    /// - Malzeme tipi (Kadim Gölge Taşı) -> Kazana (Cauldron) sürüklenir.
    /// - İksir tipleri (Buff / Joker iksirler) -> Servis Alanına (ServingArea) sürüklenir.
    /// Sürükleme bittiğinde daima raftaki sabit koordinatına geri döner.
    /// </summary>
    [SelectionBase]
    [RequireComponent(typeof(CanvasGroup))]
    public class SpecialShelfSlot : DraggableItem
    {
        [Header("Görsel Renk Ayarları")]
        [Tooltip("Sahip olunmadığında (envanterde 0 adet varken) uygulanacak gri ton")]
        public Color unownedColor = new Color(0.28f, 0.28f, 0.28f, 0.65f);

        [Tooltip("Sahip olunduğunda uygulanacak canlı renk")]
        public Color ownedColor = Color.white;

        [Header("Bileşen Referansları")]
        public Image iconImage;

        // Sabit koordinat koruması
        private Vector2 _originalAnchoredPosition;
        private Vector2 _originalSizeDelta = new Vector2(90, 90);
        private Vector3 _originalScale = Vector3.one;
        private int _originalSiblingIndex;
        private Transform _originalParent;
        private bool _hasSavedPosition = false;

        protected override void Awake()
        {
            base.Awake();

            if (iconImage == null)
                iconImage = GetComponent<Image>();

            SaveOriginalState();
        }

        private void OnEnable()
        {
            PlayerInventory.onInventoryChangedStatic += OnInventoryUpdated;
            UpdateVisuals();
        }

        private void OnDisable()
        {
            PlayerInventory.onInventoryChangedStatic -= OnInventoryUpdated;
        }

        protected override void Start()
        {
            base.Start();

            SaveOriginalState();
            UpdateVisuals();
        }

        public override void SaveOriginalState()
        {
            if (!_hasSavedPosition && rectTransform != null)
            {
                _originalAnchoredPosition = rectTransform.anchoredPosition;
                _originalSizeDelta = rectTransform.sizeDelta;
                _originalScale = rectTransform.localScale != Vector3.zero ? rectTransform.localScale : Vector3.one;
                _originalParent = transform.parent;
                _originalSiblingIndex = transform.GetSiblingIndex();
                _hasSavedPosition = true;
            }
        }

        private void OnInventoryUpdated()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Sahiplik durumuna göre eşyanın gri veya canlı renkli görünümünü günceller.
        /// </summary>
        public override void UpdateVisuals()
        {
            if (iconImage == null)
                iconImage = GetComponent<Image>();

            if (itemData == null) return;

            if (itemData.itemIcon != null && iconImage != null)
            {
                iconImage.sprite = itemData.itemIcon;
                iconImage.preserveAspect = true;
            }

            int count = (PlayerInventory.Instance != null) ? PlayerInventory.Instance.GetItemCount(itemData) : 0;
            bool isOwned = count > 0;

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.color = isOwned ? ownedColor : unownedColor;
            }

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            // 1. Envanterde bu özel eşyadan yoksa (henüz satın alınmamışsa) sürüklemeye İZİN VERME!
            int count = (PlayerInventory.Instance != null && itemData != null) ? PlayerInventory.Instance.GetItemCount(itemData) : 0;
            if (count <= 0)
            {
                eventData.pointerDrag = null;
                Debug.LogWarning($"<color=orange>[Özel Raf]</color> {itemData?.itemName ?? "Bu eşya"} henüz satın alınmadı. Malzeme Dükkanı'ndan alabilirsiniz.");
                return;
            }

            EnsureComponents();
            SaveOriginalState();

            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalParent = transform.parent;
            parentAfterDrag = transform.parent;

            // Sürüklerken diğer tüm UI ve sahne objelerinin üstünde görünmesi için kök canvas'a taşı
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform dragRoot = (canvas != null && canvas.rootCanvas != null) ? canvas.rootCanvas.transform : transform.root;
            transform.SetParent(dragRoot, true);
            transform.SetAsLastSibling();

            // Altındaki hedeflerin (Kazan, Servis Alanı) algılanabilmesi için raycast'i kapat
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            EnsureComponents();
            if (rectTransform == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            RectTransform parentRect = transform.parent as RectTransform;

            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 localPoint))
            {
                rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            EnsureComponents();

            // Sürükleme bittiğinde orijinal ebeveynine dön ve sırasını koru
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalSiblingIndex);
            }
            else if (parentAfterDrag != null)
            {
                transform.SetParent(parentAfterDrag, false);
            }

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            // DAİMA orijinal koordinat, ölçek ve boyuta pürüzsüzce geri yerleş
            if (rectTransform != null)
            {
                if (_hasSavedPosition)
                {
                    rectTransform.anchoredPosition = _originalAnchoredPosition;
                }
                rectTransform.sizeDelta = _originalSizeDelta;
                rectTransform.localScale = _originalScale != Vector3.zero ? _originalScale : Vector3.one;
                rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0f);
            }

            // Görsel durumu güncelle
            UpdateVisuals();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;

            if (itemData == null) return;

            int count = (PlayerInventory.Instance != null) ? PlayerInventory.Instance.GetItemCount(itemData) : 0;
            if (count > 0)
            {
                string targetArea = (itemData.itemType == ItemType.Ingredient || itemData.specialEffect == SpecialItemEffect.UniversalIngredient)
                    ? "Kazana (Cauldron)"
                    : "Müşteri Servis Alanına";

                Debug.Log($"<color=cyan>[Özel Raf]</color> {itemData.itemName} ({count} adet). Kullanmak için {targetArea} sürükleyip bırakabilirsiniz.");
            }
            else
            {
                Debug.Log($"<color=yellow>[Özel Raf]</color> {itemData.itemName}: Henüz satın alınmadı. Malzeme Dükkanı'ndan satın alabilirsiniz.");
            }
        }
    }
}
