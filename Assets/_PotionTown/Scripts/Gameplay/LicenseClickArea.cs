using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PotionShop
{
    /// <summary>
    /// Malzeme dükkanındaki lisans tıklama alanı.
    /// Herhangi bir Image objesine eklenir.
    /// Tıklandığında lisans satın alma parşömenini açar.
    /// Hover efekti (parlama) ve ESC ile kapatma desteği sağlar.
    /// ParchmentClickArea ile aynı pattern'i izler.
    /// </summary>
    public class LicenseClickArea : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Özel Parşömen Prefabı (Opsiyonel)")]
        [Tooltip("Boş bırakılırsa sahnede veya Resources içindeki ana parşömen prefabı otomatik bulunur.")]
        public GameObject customParchmentPrefab;

        [Header("Parlama Ayarları (Hover)")]
        public bool enableHoverGlow = true;
        public Color hoverColor = new Color(1.2f, 1.2f, 0.8f, 1f);

        private Image _myImage;
        private Color _originalColor;
        private bool _hasOriginalColor = false;

        private void Awake()
        {
            _myImage = GetComponent<Image>();
            if (_myImage != null)
            {
                _originalColor = _myImage.color;
                _hasOriginalColor = true;
                _myImage.raycastTarget = true;
            }

            // Button bileşeni yoksa ekle (tıklanabilirlik için)
            Button btn = GetComponent<Button>();
            if (btn == null)
            {
                btn = gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }

            btn.onClick.RemoveListener(OpenLicenseParchment);
            btn.onClick.AddListener(OpenLicenseParchment);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableHoverGlow && _myImage != null && _myImage.color.a > 0.05f)
            {
                if (!_hasOriginalColor)
                {
                    _originalColor = _myImage.color;
                    _hasOriginalColor = true;
                }
                _myImage.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (enableHoverGlow && _myImage != null && _hasOriginalColor)
            {
                _myImage.color = _originalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Eğer Button bileşeni yoksa doğrudan aç (Button varsa onClick zaten tetiklenecektir)
            Button btn = GetComponent<Button>();
            if (btn == null)
            {
                OpenLicenseParchment();
            }
        }

        /// <summary>
        /// Lisans parşömenini açar.
        /// </summary>
        public void OpenLicenseParchment()
        {
            LicenseParchmentUI.Open(customParchmentPrefab);
        }
    }
}
