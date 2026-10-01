using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Lobideki "Gün Sonu" tabelasına eklenir.
    /// Mouse üzerine geldiğinde binalar gibi parlar,
    /// tıklanınca saat 22:00 veya sonrasıysa gün sonu panelini açar.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EndOfDaySign : MonoBehaviour
    {
        [Header("Tabela Bilgileri")]
        public string signName = "Gün Sonu";

        [Header("Tabela Görselleri")]
        [Tooltip("Tabelanın görsel objesinin kökü. Inspector'dan tabela objesini buraya sürükle. " +
                 "Boş bırakılırsa bu objenin kendisi ve child'ları kullanılır.")]
        public Transform signVisualRoot;

        [Header("Görsel Efekt (Parlama)")]
        public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f);

        [Header("Saat Kısıtlaması")]
        [Tooltip("Gün sonu almak için minimum saat (Örn: 22 = Akşam 10)")]
        public int minimumHour = 22;

        [Header("Gün Sonu Paneli")]
        [Tooltip("Tıklanınca açılacak olan Gün Sonu Özet Paneli")]
        public GameObject endOfDayPanel;

        private List<SpriteRenderer> _targetRenderers = new List<SpriteRenderer>();
        private List<Color> _originalColors = new List<Color>();

        private Collider2D _collider;
        private bool _isHovering = false;
        private Camera _mainCamera;

        private void Start()
        {
            _collider = GetComponent<Collider2D>();
            _mainCamera = Camera.main;

            // signVisualRoot atandıysa oradan, atanmadıysa bu objenin kendisinden ara
            Transform searchRoot = signVisualRoot != null ? signVisualRoot : transform;

            // Tüm alt objelerdeki SpriteRenderer'ları bul
            SpriteRenderer[] renderers = searchRoot.GetComponentsInChildren<SpriteRenderer>(true);

            // Bu objenin kendi renderer'ını da ekle (eğer searchRoot farklıysa)
            if (signVisualRoot != null)
            {
                SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();
                if (ownRenderer != null)
                {
                    _targetRenderers.Add(ownRenderer);
                    _originalColors.Add(ownRenderer.color == hoverColor ? Color.white : ownRenderer.color);
                }
            }

            foreach (var sr in renderers)
            {
                string objName = sr.gameObject.name.ToLower();
                // Sadece ışık/glow/particle objelerini atla (artık "ef" filtresi yok)
                if (!objName.Contains("light") && !objName.Contains("glow") && !objName.Contains("particle"))
                {
                    _targetRenderers.Add(sr);

                    if (sr.color == hoverColor)
                        _originalColors.Add(Color.white);
                    else
                        _originalColors.Add(sr.color);
                }
            }

            ApplyColor(false);
        }

        private void Update()
        {
            if (_mainCamera == null || !_mainCamera.gameObject.activeInHierarchy)
            {
                _mainCamera = Camera.main;
            }
            if (_mainCamera == null || _collider == null) return;

            Vector2 mousePos = Vector2.zero;
            bool isClickDown = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                isClickDown = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            }
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
            {
                mousePos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
                isClickDown = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                mousePos = touch.position;
                isClickDown = (touch.phase == TouchPhase.Began);
            }
            else
            {
                mousePos = Input.mousePosition;
                isClickDown = Input.GetMouseButtonDown(0);
            }
#endif

            Ray ray = _mainCamera.ScreenPointToRay(mousePos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            bool isOver = (hit.collider == _collider);

            if (!isOver)
            {
                Plane plane = new Plane(-_mainCamera.transform.forward, transform.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 worldPoint = ray.GetPoint(enter);
                    isOver = _collider.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y));
                }
            }

            // --- HOVER KONTROLÜ ---
            if (isOver && !_isHovering)
            {
                _isHovering = true;
                ApplyColor(true);

                if (BuildingTooltipUI.Instance != null)
                {
                    // Saat uygunsa normal isim, değilse uyarı göster
                    if (CanEndDay())
                        BuildingTooltipUI.Instance.ShowTooltip(LocalizationManager.Get(signName));
                    else
                        BuildingTooltipUI.Instance.ShowTooltip($"{LocalizationManager.Get(signName)} ({LocalizationManager.Get("lobby_shop_not_open")})");
                }
            }
            else if (!isOver && _isHovering)
            {
                _isHovering = false;
                ApplyColor(false);

                if (BuildingTooltipUI.Instance != null)
                {
                    BuildingTooltipUI.Instance.HideTooltip();
                }
            }

            // --- TIKLAMA KONTROLÜ ---
            if (isOver && isClickDown)
            {
                _isHovering = false;
                ApplyColor(false);
                if (BuildingTooltipUI.Instance != null)
                {
                    BuildingTooltipUI.Instance.HideTooltip();
                }

                if (!CanEndDay())
                {
                    Debug.LogWarning($"Günü bitirmek için dükkan açık olmalı!");
                    return;
                }

                // Gün Sonu Panelini aç
                UI.EndOfDayPanel panel = null;
                if (endOfDayPanel != null)
                {
                    panel = endOfDayPanel.GetComponent<UI.EndOfDayPanel>();
                }
                if (panel == null)
                {
                    panel = FindFirstObjectByType<UI.EndOfDayPanel>(FindObjectsInactive.Include);
                }

                if (panel != null)
                {
                    panel.gameObject.SetActive(true);
                    panel.OpenPanel();
                }
                else
                {
                    // Panel yoksa doğrudan gün sonu al
                    LevelSystem.AdvanceLevel();
                    Debug.Log("Gün sonu alındı! (Panel atanmamış, direkt geçildi)");
                }
            }
        }

        private bool CanEndDay()
        {
            return GameManager.Instance != null && GameManager.Instance.isShopOpen;
        }

        private void ApplyColor(bool highlight)
        {
            for (int i = 0; i < _targetRenderers.Count; i++)
            {
                if (_targetRenderers[i] != null)
                {
                    _targetRenderers[i].color = highlight ? hoverColor : _originalColors[i];
                }
            }
        }
    }
}
