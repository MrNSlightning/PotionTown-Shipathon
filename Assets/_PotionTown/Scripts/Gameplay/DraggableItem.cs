using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [HideInInspector] public Transform parentAfterDrag;
        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        
        // Bu sürüklenebilir öğenin verisi, bulunduğu ShelfSlot'tan alınabilir
        public ItemData itemData;

        // Bu sürükleme sırasında teslimat gerçekleşti mi kontrolü
        [HideInInspector] public bool wasDeliveredThisDrag = false;

        // Bu öğenin bir rafa ait olup olmadığını kontrol eder
        public bool isFromShelf => GetComponentInParent<ShelfSlot>() != null || 
                                   (parentAfterDrag != null && parentAfterDrag.GetComponentInParent<ShelfSlot>() != null);

        // Orijinal yerleşim ve koordinat hafızası
        protected Vector2 originalAnchorMin = Vector2.zero;
        protected Vector2 originalAnchorMax = Vector2.one;
        protected Vector2 originalPivot = new Vector2(0.5f, 0.5f);
        protected Vector2 originalSizeDelta = Vector2.zero;
        protected Vector2 originalAnchoredPosition = Vector2.zero;
        protected Vector3 originalLocalScale = Vector3.one;
        protected Transform originalParent;
        protected int originalSiblingIndex;
        protected bool hasSavedOriginalState = false;

        // Sürükleme anında gizlenen sayaç yazıları
        private TextMeshProUGUI[] _childTexts;

        protected virtual void Awake()
        {
            EnsureComponents();
            SaveOriginalState();
        }

        protected void EnsureComponents()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        }

        public virtual void SaveOriginalState()
        {
            EnsureComponents();
            if (!hasSavedOriginalState && rectTransform != null)
            {
                originalAnchorMin = rectTransform.anchorMin;
                originalAnchorMax = rectTransform.anchorMax;
                originalPivot = rectTransform.pivot;
                originalSizeDelta = rectTransform.sizeDelta;
                originalAnchoredPosition = rectTransform.anchoredPosition;
                originalLocalScale = rectTransform.localScale != Vector3.zero ? rectTransform.localScale : Vector3.one;
                originalParent = transform.parent;
                originalSiblingIndex = transform.GetSiblingIndex();
                hasSavedOriginalState = true;
            }
        }

        protected virtual void Start()
        {
            EnsureComponents();
            SaveOriginalState();

            // Eğer ItemData atanmamışsa, üst objedeki ShelfSlot'tan çekmeyi dene
            if (itemData == null)
            {
                ShelfSlot slot = GetComponentInParent<ShelfSlot>();
                if (slot != null)
                {
                    itemData = slot.slotItemData;
                }
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            // Sürükleme yapılmadıysa tıklamayı üstteki rafa ilet (envanteri açması için, sabit raf değilse)
            if (!eventData.dragging)
            {
                ShelfSlot slot = GetComponentInParent<ShelfSlot>();
                if (slot != null)
                {
                    if (slot.WasLongPressed)
                    {
                        return;
                    }

                    if (!slot.isFixedSlot)
                    {
                        slot.OnSlotClicked();
                    }
                }
            }
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            ShelfSlot slot = GetComponentInParent<ShelfSlot>();
            if (slot != null)
            {
                slot.OnPointerDown(eventData);
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            ShelfSlot slot = GetComponentInParent<ShelfSlot>();
            if (slot != null)
            {
                slot.OnPointerUp(eventData);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            ShelfSlot slot = GetComponentInParent<ShelfSlot>();
            if (slot != null)
            {
                slot.OnPointerExit(eventData);
            }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            wasDeliveredThisDrag = false;

            // Sürükleme başladığında rafın basılı tutma (eşya silme) sayacını iptal et
            ShelfSlot parentSlot = GetComponentInParent<ShelfSlot>();
            if (parentSlot != null)
            {
                parentSlot.CancelLongPress();
            }

            // Eğer envanterde bu eşyadan kalmadıysa, sürüklemeye izin verme!
            if (PlayerInventory.Instance != null && itemData != null)
            {
                if (PlayerInventory.Instance.GetItemCount(itemData) <= 0)
                {
                    eventData.pointerDrag = null; // Sürüklemeyi iptal et
                    return;
                }
            }

            EnsureComponents();
            SaveOriginalState();

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            parentAfterDrag = transform.parent;

            // Sürüklerken sayı yazısının (x5 vb.) imlece yapışıp kaymasını önle
            _childTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (_childTexts != null)
            {
                foreach (var t in _childTexts)
                {
                    if (t != null) t.enabled = false;
                }
            }
            
            // Sürüklerken diğer objelerin üstünde görünmesi için kök canvas'a taşı
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform dragRoot = (canvas != null && canvas.rootCanvas != null) ? canvas.rootCanvas.transform : transform.root;
            transform.SetParent(dragRoot, true);
            transform.SetAsLastSibling();

            // Sürükleme esnasında imlecin altında düzgün, sabit boyutta görünmesi için
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(65f, 65f);
                rectTransform.localScale = Vector3.one;
            }
            
            // Sürüklerken altında kalan objeleri (Müşteriler, Kazan, Servis Alanı vs.) algılayabilmesi için raycast'i kapat
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
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
            else
            {
                rectTransform.position = eventData.position;
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            EnsureComponents();

            // Eğer OnDrop herhangi bir sebeple doğrudan tetiklenmediyse ama imleç bir müşteri üzerindeyse teslimatı dene
            if (!wasDeliveredThisDrag)
            {
                Customer targetCustomer = null;
                if (eventData.pointerCurrentRaycast.gameObject != null)
                {
                    targetCustomer = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Customer>();
                }

                if (targetCustomer == null && eventData.hovered != null)
                {
                    foreach (var obj in eventData.hovered)
                    {
                        if (obj != null)
                        {
                            targetCustomer = obj.GetComponentInParent<Customer>();
                            if (targetCustomer != null) break;
                        }
                    }
                }

                // 2D Sprite/Transform tabanlı müşteriler (HeroEditor4D) için Collider2D kontrolü
                if (targetCustomer == null)
                {
                    Camera cam = eventData.pressEventCamera;
                    if (cam == null) cam = Camera.main;
                    if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

                    if (cam != null)
                    {
                        Vector3 worldPos = cam.ScreenToWorldPoint(eventData.position);
                        Collider2D col = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
                        if (col != null)
                        {
                            targetCustomer = col.GetComponentInParent<Customer>();
                        }

                        if (targetCustomer == null)
                        {
                            Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(worldPos.x, worldPos.y), 1.5f);
                            foreach (var hit in hits)
                            {
                                var c = hit.GetComponentInParent<Customer>();
                                if (c != null)
                                {
                                    targetCustomer = c;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (targetCustomer != null && !targetCustomer.IsWalkingIn && !targetCustomer.IsWalkingOut && targetCustomer.currentMood != Customer.Mood.Gitti)
                {
                    targetCustomer.OnDrop(eventData);
                }
            }

            // Sürükleme bittiğinde orijinal ebeveynine dön ve sırasını koru
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            else if (parentAfterDrag != null)
            {
                transform.SetParent(parentAfterDrag, false);
            }

            // Orijinal boyut, çapa ve pozisyonu KESİN olarak geri yükle
            if (rectTransform != null && hasSavedOriginalState)
            {
                rectTransform.anchorMin = originalAnchorMin;
                rectTransform.anchorMax = originalAnchorMax;
                rectTransform.pivot = originalPivot;
                rectTransform.sizeDelta = originalSizeDelta;
                rectTransform.anchoredPosition = originalAnchoredPosition;
                rectTransform.localScale = originalLocalScale != Vector3.zero ? originalLocalScale : Vector3.one;
                rectTransform.localPosition = new Vector3(originalAnchoredPosition.x, originalAnchoredPosition.y, 0f);
            }

            // Sayaç yazılarını tekrar görünür yap
            if (_childTexts != null)
            {
                foreach (var t in _childTexts)
                {
                    if (t != null) t.enabled = true;
                }
                _childTexts = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            // Raf durumunu ve görselini güncelle
            ShelfSlot slot = GetComponentInParent<ShelfSlot>();
            if (slot == null && parentAfterDrag != null)
            {
                slot = parentAfterDrag.GetComponentInParent<ShelfSlot>();
            }

            if (slot != null)
            {
                slot.UpdateSlotState();
            }
            else
            {
                UpdateVisuals();
            }
        }

        public virtual void UpdateVisuals()
        {
            if (itemData != null)
            {
                Image img = GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    img.sprite = itemData.itemIcon;
                }
            }
        }
    }
}
