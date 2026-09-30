using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace PotionShop
{
    public class ParchmentClickArea : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Parşömen Prefabı")]
        public GameObject parchmentPrefab;

        [Header("Parlama Ayarları (Hover)")]
        public bool enableHoverGlow = true;
        public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f);
        
        private Image _myImage;
        private Color _originalColor;

        private GameObject _activePopup;
        private GameObject _darkOverlay;
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;
        private static int _activeParchmentCount = 0;
        public static bool IsAnyParchmentOpen => _activeParchmentCount > 0;

        private void Start()
        {
            _myImage = GetComponent<Image>();
            if (_myImage != null)
            {
                _originalColor = _myImage.color;
            }
        }

        private void Update()
        {
            if (_isOpen)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseParchment();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseParchment();
                }
#endif
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableHoverGlow && _myImage != null && _myImage.color.a > 0.1f)
            {
                _myImage.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (enableHoverGlow && _myImage != null && _myImage.color.a > 0.1f)
            {
                _myImage.color = _originalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isOpen)
                OpenParchment();
        }

        public void OpenParchment()
        {
            if (_isOpen) return;
            if (parchmentPrefab == null)
            {
                Debug.LogError("ParchmentClickArea: Prefab atanmamış!");
                return;
            }

            // TopDropdownMenu açıksa kapat
            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsMenuOpen)
            {
                TopDropdownMenu.Instance.CloseMenu();
            }

            _activeParchmentCount++;

            // 1) Parşömeni bağımsız bir root UI objesi olarak oluştur (Kameralara ASLA bağlama!)
            // Böylece sahnedeki kameraların farklı localScale (0, 8.96 vb.) veya pozisyonlarından etkilenmez.
            _activePopup = Instantiate(parchmentPrefab);
            _activePopup.name = $"{parchmentPrefab.name}_Popup";
            _activePopup.SetActive(true);

            // Canvas ayarlarını garantiye al: ScreenSpaceOverlay ve en üst sortingOrder
            Canvas canvas = _activePopup.GetComponent<Canvas>();
            if (canvas == null) canvas = _activePopup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 2500;

            // GraphicRaycaster ekle (UI tıklamalarının algılanması için)
            if (_activePopup.GetComponent<GraphicRaycaster>() == null)
            {
                _activePopup.AddComponent<GraphicRaycaster>();
            }

            // CanvasScaler ayarlarını garantiye al: ScaleWithScreenSize, 1920x1080, matchWidthOrHeight = 0.5f
            CanvasScaler scaler = _activePopup.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = _activePopup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _activePopup.transform.SetAsLastSibling();

            // RectTransform pozisyonunu sıfırla
            RectTransform rootRect = _activePopup.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchoredPosition = Vector2.zero;
            }

            // 2) Karanlık Arka Plan (Dark Backdrop) ekle — parşömenin arkasında (First Sibling) yer alır
            _darkOverlay = new GameObject("ParchmentDarkBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            _darkOverlay.transform.SetParent(_activePopup.transform, false);
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
            overlayBtn.onClick.AddListener(CloseParchment);

            // 3) Prefab içinde varsa kapatma butonlarını bul ve bağla
            Button[] allButtons = _activePopup.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                if (btn == overlayBtn) continue;
                string n = btn.gameObject.name.ToLower();
                if (n.Contains("close") || n.Contains("kapat") || n == "x" || n.Contains("x_btn") || n.Contains("back") || n.Contains("geri"))
                {
                    btn.onClick.RemoveListener(CloseParchment);
                    btn.onClick.AddListener(CloseParchment);
                }
            }

            _isOpen = true;
            Debug.Log($"Parşömen açıldı: {parchmentPrefab.name}");
        }

        public void CloseParchment()
        {
            if (!_isOpen) return;

            _activeParchmentCount = Mathf.Max(0, _activeParchmentCount - 1);

            if (_activePopup != null)
            {
                Destroy(_activePopup);
                _activePopup = null;
            }
            _darkOverlay = null;
            _isOpen = false;
            Debug.Log("Parşömen kapatıldı.");
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                CloseParchment();
            }
        }

        private void OnDestroy()
        {
            if (_isOpen)
            {
                _activeParchmentCount = Mathf.Max(0, _activeParchmentCount - 1);
            }
        }
    }
}
