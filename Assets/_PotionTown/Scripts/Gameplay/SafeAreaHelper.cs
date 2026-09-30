using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Mobil cihazlardaki çentik (notch) ve punch-hole alanlarında
    /// UI elemanlarının güvenli bölge dışına taşmasını engeller.
    /// Canvas'ın kök paneline ekleyin.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("PotionTown/Safe Area Helper")]
    public class SafeAreaHelper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("SafeArea'yı her frame'de kontrol etmek yerine belirli aralıklarla kontrol eder (saniye).")]
        private float checkInterval = 0.5f;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private float _checkTimer;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // Canvas'ın kendisine eklenmişse root canvas'ın bozulmaması ve UI kilitlenmemesi için devre dışı bırak
            if (GetComponent<Canvas>() != null)
            {
                Debug.LogWarning("[SafeAreaHelper] SafeAreaHelper doğrudan bir Canvas bileşeni taşıyan objeye eklenemez! Lütfen Canvas'ın altındaki bir panel objesine ekleyin.", this);
                enabled = false;
                return;
            }

            ApplySafeArea();
        }

        private void Update()
        {
            _checkTimer += Time.unscaledDeltaTime;
            if (_checkTimer >= checkInterval)
            {
                _checkTimer = 0f;
                if (_lastSafeArea != Screen.safeArea)
                {
                    ApplySafeArea();
                }
            }
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;
            if (GetComponent<Canvas>() != null) return;

            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
