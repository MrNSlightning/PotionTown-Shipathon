using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Kazanılan (+) ve harcanan (-) altın ve kadim para miktarlarını
    /// ekranın ortasında değil, doğrudan ilgili eşyanın veya müşterinin üstünde
    /// yukarı doğru süzülen ve kısa sürede kaybolan (floating feedback) efekt olarak gösteren sistem.
    /// </summary>
    public class CurrencyFeedbackUI : MonoBehaviour
    {
        private static CurrencyFeedbackUI _instance;
        public static CurrencyFeedbackUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CurrencyFeedbackUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[CurrencyFeedbackUI]");
                        _instance = go.AddComponent<CurrencyFeedbackUI>();
                    }
                }
                return _instance;
            }
        }

        [Header("UI Referansları (Inspector Bağlantıları)")]
        [Tooltip("Uçuşan para yazılarının ekleneceği container")]
        public Transform feedbackContainer;

        [Tooltip("Altın sikke görseli")]
        public Sprite goldSprite;

        [Tooltip("Kadim para sikke görseli")]
        public Sprite kadimSprite;

        [Tooltip("Kazanılan para metin rengi")]
        public Color gainColor = new Color(0.25f, 0.95f, 0.35f, 1f);

        [Tooltip("Harcanan para metin rengi")]
        public Color lossColor = new Color(1f, 0.3f, 0.3f, 1f);

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private Transform _container;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            EnsureCanvasAndContainer();
        }

        private void EnsureCanvasAndContainer()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 2800; // En üst UI katmanı

            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
                if (_scaler == null) _scaler = GetComponentInParent<CanvasScaler>();
                if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
            }
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                var raycaster = gameObject.AddComponent<GraphicRaycaster>();
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            if (feedbackContainer != null)
            {
                _container = feedbackContainer;
            }
            else if (_container == null)
            {
                var existing = transform.Find("FeedbackContainer");
                if (existing != null)
                {
                    _container = existing;
                }
                else
                {
                    GameObject contObj = new GameObject("FeedbackContainer", typeof(RectTransform));
                    contObj.transform.SetParent(transform, false);
                    _container = contObj.transform;
                    RectTransform contRT = contObj.GetComponent<RectTransform>();
                    contRT.anchorMin = Vector2.zero;
                    contRT.anchorMax = Vector2.one;
                    contRT.sizeDelta = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// Belirtilen hedef Transform veya World pozisyonunun Canvas üzerindeki local koordinatını hesaplar.
        /// </summary>
        public Vector2 GetLocalCanvasPosition(Transform target, Vector3? customWorldPos = null)
        {
            EnsureCanvasAndContainer();

            Vector2 screenPoint;

            if (target != null)
            {
                Canvas parentCanvas = target.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenPoint = new Vector2(target.position.x, target.position.y);
                }
                else
                {
                    Camera cam = (parentCanvas != null && parentCanvas.worldCamera != null) ? parentCanvas.worldCamera : Camera.main;
                    if (cam != null)
                    {
                        screenPoint = cam.WorldToScreenPoint(target.position);
                    }
                    else
                    {
                        screenPoint = new Vector2(target.position.x, target.position.y);
                    }
                }
            }
            else if (customWorldPos.HasValue)
            {
                Vector3 wPos = customWorldPos.Value;
                Camera cam = Camera.main;
                // Koordinat zaten ekran pikseli büyüklüğündeyse
                if (Mathf.Abs(wPos.x) > 100f || Mathf.Abs(wPos.y) > 100f)
                {
                    screenPoint = new Vector2(wPos.x, wPos.y);
                }
                else if (cam != null)
                {
                    screenPoint = cam.WorldToScreenPoint(wPos);
                }
                else
                {
                    screenPoint = new Vector2(wPos.x, wPos.y);
                }
            }
            else
            {
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.75f);
            }

            Camera canvasCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
            RectTransform containerRT = (_container as RectTransform) ?? (_canvas != null ? _canvas.GetComponent<RectTransform>() : null);

            if (containerRT != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, screenPoint, canvasCam, out Vector2 localPoint);
                return localPoint;
            }

            return Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════
        //  STATİK KULLANIM METOTLARI
        // ═══════════════════════════════════════════════════════

        public static void ShowGain(int amount, Transform target, bool isKadim = false)
        {
            if (amount <= 0) return;
            Instance.TriggerFeedback(amount, target, null, isGain: true, isKadim: isKadim);
        }

        public static void ShowGain(int amount, Vector3 worldOrScreenPos, bool isKadim = false)
        {
            if (amount <= 0) return;
            Instance.TriggerFeedback(amount, null, worldOrScreenPos, isGain: true, isKadim: isKadim);
        }

        public static void ShowLoss(int amount, Transform target, bool isKadim = false)
        {
            if (amount <= 0) return;
            Instance.TriggerFeedback(amount, target, null, isGain: false, isKadim: isKadim);
        }

        public static void ShowLoss(int amount, Vector3 worldOrScreenPos, bool isKadim = false)
        {
            if (amount <= 0) return;
            Instance.TriggerFeedback(amount, null, worldOrScreenPos, isGain: false, isKadim: isKadim);
        }

        // Geriye dönük uyumluluk
        public void ShowGoldFeedback(int delta, Vector2? customPos = null)
        {
            if (delta == 0) return;
            bool isGain = delta > 0;
            int amount = Mathf.Abs(delta);
            TriggerFeedback(amount, null, customPos, isGain, isKadim: false);
        }

        public void ShowKadimFeedback(int delta, Vector2? customPos = null)
        {
            if (delta == 0) return;
            bool isGain = delta > 0;
            int amount = Mathf.Abs(delta);
            TriggerFeedback(amount, null, customPos, isGain, isKadim: true);
        }

        public void TriggerFeedback(int amount, Transform target, Vector3? customPos, bool isGain, bool isKadim)
        {
            EnsureCanvasAndContainer();
            if (_container == null) return;

            Sprite coinSprite;
            if (isKadim)
            {
                coinSprite = kadimSprite != null ? kadimSprite : UIThemeHelper.GetAncientCoinSprite();
            }
            else
            {
                coinSprite = goldSprite != null ? goldSprite : UIThemeHelper.GetGoldCoinSprite();
            }

            string text = (isGain ? "+" : "-") + amount;
            Color textColor = isGain ? gainColor : lossColor;

            Vector2 baseLocalPos = GetLocalCanvasPosition(target, customPos);
            StartCoroutine(AnimateFlyout(text, coinSprite, textColor, baseLocalPos));
        }

        private IEnumerator AnimateFlyout(string text, Sprite iconSprite, Color textColor, Vector2 startLocalPos)
        {
            EnsureCanvasAndContainer();
            if (_container == null) yield break;

            GameObject item = new GameObject("FlyoutItem", typeof(RectTransform), typeof(CanvasGroup));
            item.transform.SetParent(_container, false);
            RectTransform itemRT = item.GetComponent<RectTransform>();
            CanvasGroup group = item.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Horizontal Layout
            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Coin Icon
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(item.transform, false);
                Image img = iconObj.GetComponent<Image>();
                img.sprite = iconSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(36f, 36f);
            }

            // Amount Text (New Rocker SDF)
            GameObject txtObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(item.transform, false);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(tmp);
            tmp.text = text;
            tmp.fontSize = 34;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.sizeDelta = new Vector2(130f, 45f);

            itemRT.sizeDelta = new Vector2(175f, 45f);

            // Başlangıç: Hedefin 35 birim hemen üstü
            Vector2 initialPos = startLocalPos + new Vector2(0f, 35f);
            Vector2 targetPos = initialPos + new Vector2(0f, 65f); // Yukarı doğru süzülüş
            itemRT.anchoredPosition = initialPos;

            // Animasyon: Hızlı pop, yukarı süzülme ve yumuşak fade out (0.85 saniye)
            float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Hafif pop efekti: 0.75 -> 1.15 -> 1.0
                float scale = (t < 0.2f) ? Mathf.Lerp(0.75f, 1.15f, t / 0.2f) : Mathf.Lerp(1.15f, 1.0f, (t - 0.2f) / 0.8f);
                itemRT.localScale = Vector3.one * scale;

                // Yukarı doğru yumuşak kayma (Ease Out Sine)
                itemRT.anchoredPosition = Vector2.Lerp(initialPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));

                // Son %50'lik dilimde yumuşak fade out
                if (t > 0.5f)
                {
                    group.alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                }
                else
                {
                    group.alpha = 1f;
                }

                yield return null;
            }

            Destroy(item);
        }
    }
}
