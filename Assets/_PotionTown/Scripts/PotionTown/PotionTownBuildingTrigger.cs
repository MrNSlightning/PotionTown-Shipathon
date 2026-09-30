using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// Lobideki binalara eklenen, Inspector üzerinden hedef odayı seçmeyi sağlayan
    /// ve hover/tıklama etkileşimlerini yöneten bileşen.
    /// Camera.main arama ve string eşleme bağımlılıklarını ortadan kaldırır.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PotionTownBuildingTrigger : MonoBehaviour
    {
        [Header("Hedef Oda (Inspector Seçimi)")]
        [Tooltip("Bu binaya tıklandığında açılacak oda")]
        public PotionTownRoom targetRoom = PotionTownRoom.PotionSelling;

        [Header("Koordinatör & Kamera")]
        [Tooltip("Sahne koordinatörü (Atanmazsa sahneden otomatik bulunur)")]
        public PotionTownSceneCoordinator coordinator;

        [Tooltip("Lobi kamerası (Tıklama raycast'i için - Atanmazsa koordinatörden alınır)")]
        public Camera lobbyCamera;

        [Header("Bina Bilgisi & Görsel Efekt")]
        [Tooltip("Mouse üzerine gelindiğinde ekranda çıkacak dükkan adı")]
        public string buildingDisplayName = "İksir Dükkanı";

        [Tooltip("Hover sırasında uygulanacak parlama rengi")]
        public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f);

        [Header("Tıklama Olayı (Unity Event)")]
        public UnityEvent onBuildingClicked;

        private List<SpriteRenderer> _targetRenderers = new List<SpriteRenderer>();
        private List<Color> _originalColors = new List<Color>();
        private Collider2D _collider;
        private bool _isHovering = false;

        private void Start()
        {
            _collider = GetComponent<Collider2D>();

            if (coordinator == null)
            {
                coordinator = PotionTownSceneCoordinator.Instance ?? FindFirstObjectByType<PotionTownSceneCoordinator>();
            }

            if (lobbyCamera == null && coordinator != null)
            {
                lobbyCamera = coordinator.lobbyCamera;
            }

            if (lobbyCamera == null)
            {
                lobbyCamera = Camera.main;
            }

            // Sprite renderer'ları keşfet
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                string objName = sr.gameObject.name.ToLower();
                if (!objName.Contains("light") && !objName.Contains("glow") && !objName.Contains("ef") && !objName.Contains("particle"))
                {
                    _targetRenderers.Add(sr);
                    _originalColors.Add(sr.color == hoverColor ? Color.white : sr.color);
                }
            }

            ApplyColor(false);
        }

        private void Update()
        {
            if (_collider == null) return;

            Vector2 mousePos = Vector2.zero;
            bool isClickDown = false;

#if ENABLE_INPUT_SYSTEM
            // Handle both Mouse and Touch for new Input System
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                isClickDown = true;
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.touches.Count > 0)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.touches[0];
                mousePos = touch.position.ReadValue();
                isClickDown = touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
            }
#else
            // Fallback for old Input Manager
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                mousePos = t.position;
                isClickDown = (t.phase == TouchPhase.Began);
            }
            else
            {
                mousePos = Input.mousePosition;
                isClickDown = Input.GetMouseButtonDown(0);
            }
#endif

            // Eğer fare engelleyici bir UI öğesi (Dropdown menü, açık envanter vb.) üzerindeyse bina hover/tıklamasını yut
            if (IsPointerOverBlockingUI(mousePos))
            {
                if (_isHovering)
                {
                    _isHovering = false;
                    ApplyColor(false);
                    if (BuildingTooltipUI.Instance != null)
                    {
                        BuildingTooltipUI.Instance.HideTooltip();
                    }
                }
                return;
            }

            Camera activeCam = lobbyCamera != null ? lobbyCamera : Camera.main;
            if (activeCam == null || !activeCam.gameObject.activeInHierarchy) return;

            // 3D Ray'den 2D Physics Collider'a hassas kesişim testi (Kamera açılı veya farklı Z derinliğinde olsa bile tam çalışır)
            Ray ray = activeCam.ScreenPointToRay(mousePos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            bool isOver = (hit.collider == _collider);

            if (!isOver)
            {
                // Fallback: Binanın Z düzlemiyle ray kesişimi
                Plane plane = new Plane(-activeCam.transform.forward, transform.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 worldPoint = ray.GetPoint(enter);
                    isOver = _collider.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y));
                }
            }

            // Hover durumu
            if (isOver && !_isHovering)
            {
                _isHovering = true;
                ApplyColor(true);

                if (BuildingTooltipUI.Instance != null)
                {
                    BuildingTooltipUI.Instance.ShowTooltip(buildingDisplayName);
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

            // Tıklama durumu
            if (isOver && isClickDown)
            {
                _isHovering = false;
                ApplyColor(false);

                if (BuildingTooltipUI.Instance != null)
                {
                    BuildingTooltipUI.Instance.HideTooltip();
                }

                onBuildingClicked?.Invoke();

                if (coordinator != null)
                {
                    coordinator.SwitchToRoom(targetRoom);
                }
                else
                {
                    Debug.LogWarning($"[PotionTownBuildingTrigger] {buildingDisplayName} tıklandı fakat Coordinator atanmamış!");
                }
            }
        }

        private bool IsPointerOverBlockingUI(Vector2 mousePos)
        {
            // 0. Başlangıç Menüsü veya Shop açıkken kesinlikle engelle
            if (PotionShop.UI.GameStartMenuUI.IsOpen || ShopUI.IsOpen)
                return true;

            // 1. Üst menünün ahşap paneli veya çekmece dili üzerindeyse
            if (TopDropdownMenu.Instance != null && TopDropdownMenu.Instance.IsPointerOverMenu(mousePos))
                return true;

            // 2. Envanter veya depo açıkken
            if (StorageManager.Instance != null && StorageManager.Instance.storagePanel != null && StorageManager.Instance.storagePanel.activeInHierarchy)
                return true;

            if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
                return true;

            // 3. EventSystem üzerinde interaktif bir UI butonu/alanı var mı?
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    position = mousePos
                };
                var results = new List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

                foreach (var r in results)
                {
                    if (r.gameObject == null) continue;
                    string n = r.gameObject.name;
                    if (n.Contains("Room_Lobi") || n.Contains("LobiSahnesi") || n.Contains("Tooltip"))
                        continue;

                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                        r.gameObject.GetComponentInParent<UnityEngine.UI.ScrollRect>() != null ||
                        r.gameObject.GetComponentInParent<UnityEngine.UI.InputField>() != null ||
                        r.gameObject.GetComponentInParent<TMPro.TMP_InputField>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private void OnDisable()
        {
            if (_isHovering)
            {
                _isHovering = false;
                ApplyColor(false);
                if (BuildingTooltipUI.Instance != null)
                {
                    BuildingTooltipUI.Instance.HideTooltip();
                }
            }
        }
    }
}
