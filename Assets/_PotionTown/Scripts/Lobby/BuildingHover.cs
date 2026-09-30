using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Binalara eklenir. Mouse üzerine geldiğinde parlamayı tetikler,
/// ismini ortada gösterir ve tıklanınca oyun canvasına geçirir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BuildingHover : MonoBehaviour
{
    [Header("Bina Bilgileri")]
    public string buildingName = "İksir Dükkanı";

    [Header("Görsel Efekt (Parlama)")]
    public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f); 

    [Header("Canvas Geçişi")]
    [Tooltip("Şu anki bulunduğumuz harita/kasaba canvası (Kapatılacak)")]
    public GameObject currentMapCanvas;
    [Tooltip("Tıklanınca açılacak olan oyunun canvası (Açılacak)")]
    public GameObject targetGameCanvas;

    private List<SpriteRenderer> _targetRenderers = new List<SpriteRenderer>();
    private List<Color> _originalColors = new List<Color>();
    
    private Collider2D _collider;
    private bool _isHovering = false;
    private Camera _mainCamera;

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
        _mainCamera = Camera.main;

        // Tüm alt objelerdeki SpriteRenderer'ları bul
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            string objName = sr.gameObject.name.ToLower();
            if (!objName.Contains("light") && !objName.Contains("glow") && !objName.Contains("ef") && !objName.Contains("particle"))
            {
                _targetRenderers.Add(sr);
                
                // Eğer oyunu kaydederken bina yanlışlıkla sarı (hoverColor) kalmışsa, orijinal rengi beyaz kabul et.
                if (sr.color == hoverColor)
                    _originalColors.Add(Color.white);
                else
                    _originalColors.Add(sr.color);
            }
        }

        // Oyun başlar başlamaz parlamayı zorla kapat ki bug'ta kalmasın.
        ApplyColor(false);
    }

    private void Update()
    {
        if (_mainCamera == null || !_mainCamera.gameObject.activeInHierarchy)
        {
            _mainCamera = Camera.main;
        }
        if (_mainCamera == null || _collider == null) return;

        // Farenin ekrandaki yerini al (Yeni ve Eski Input sistemleriyle uyumlu)
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
            
            // İsmi UI'da göster (Örn. ekranın ortasındaki UI)
            if (BuildingTooltipUI.Instance != null)
            {
                BuildingTooltipUI.Instance.ShowTooltip(buildingName);
            }
        }
        else if (!isOver && _isHovering)
        {
            _isHovering = false;
            ApplyColor(false);
            
            // İsmi UI'dan gizle
            if (BuildingTooltipUI.Instance != null)
            {
                BuildingTooltipUI.Instance.HideTooltip();
            }
        }

        // --- TIKLAMA KONTROLÜ ---
        if (isOver && isClickDown)
        {
            // Tıklanınca hover efektini ve yazıyı temizle
            _isHovering = false;
            ApplyColor(false);
            if (BuildingTooltipUI.Instance != null)
            {
                BuildingTooltipUI.Instance.HideTooltip();
            }

            // Artık kamerayı taşımıyoruz çünkü her sahnenin kendi kamerası var!
            // (Eski teleport kodu buradan silindi)

            // Canvasları (Dükkanları) değiştir
            if (currentMapCanvas != null)
                currentMapCanvas.SetActive(false);
                
            if (targetGameCanvas != null)
                targetGameCanvas.SetActive(true);

            // Üst menüyü dükkan moduna geçir ve aktif dükkanı bildir
            if (PotionShop.TopDropdownMenu.Instance != null)
            {
                bool isSellingShop = (targetGameCanvas != null && targetGameCanvas.name.Contains("Satış")) ||
                                     buildingName.Contains("Satış") ||
                                     buildingName == "İksir Dükkanı";
                PotionShop.TopDropdownMenu.Instance.SetCurrentShop(targetGameCanvas, isSellingShop);
            }
        }
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
