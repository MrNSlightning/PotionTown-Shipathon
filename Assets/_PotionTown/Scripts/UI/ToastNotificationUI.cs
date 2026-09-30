using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Ekran üzerinde buton gibi olmayan, sadece yazı ve uyarı ikonu olarak görünen,
    /// New Rocker fontu ve oyunun orijinal uyarı grafiğiyle uyumlu bildirim sistemi.
    /// Hedef Transform verildiğinde o objenin üstünde, verilmediğinde ekranın üst-orta kısmında gösterir.
    /// </summary>
    public class ToastNotificationUI : MonoBehaviour
    {
        private static ToastNotificationUI _instance;
        public static ToastNotificationUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ToastNotificationUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ToastNotificationUI]");
                        _instance = go.AddComponent<ToastNotificationUI>();
                    }
                }
                return _instance;
            }
        }

        [Header("UI Referansları (Inspector Bağlantıları)")]
        [Tooltip("Bildirimlerin yerleştirileceği container")]
        public Transform toastContainer;

        [Tooltip("Uyarı ikonu görseli")]
        public Sprite warningSprite;

        [Tooltip("Varsayılan gösterim süresi")]
        public float displayDuration = 2.0f;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private Transform _toastContainer;
        private Coroutine _activeToastCoroutine;

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

        /// <summary>
        /// _canvas, _scaler ve _toastContainer referanslarını her koşulda güvenli şekilde başlatır.
        /// Inspector'dan toastContainer atanmış olsa bile _canvas asla null kalmaz.
        /// </summary>
        private void EnsureCanvasAndContainer()
        {
            // _canvas her zaman başlatılmalı — eski kodda toastContainer != null ise
            // erken return edildiğinden _canvas null kalıyordu ve NullReferenceException fırlatıyordu.
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 3100; // En üstte görünsün

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

            if (toastContainer != null)
            {
                _toastContainer = toastContainer;
            }
            else if (_toastContainer == null)
            {
                var existing = transform.Find("ToastContainer");
                if (existing != null)
                {
                    _toastContainer = existing;
                }
                else
                {
                    GameObject cont = new GameObject("ToastContainer", typeof(RectTransform));
                    cont.transform.SetParent(transform, false);
                    _toastContainer = cont.transform;
                    RectTransform contRT = cont.GetComponent<RectTransform>();
                    contRT.anchorMin = Vector2.zero;
                    contRT.anchorMax = Vector2.one;
                    contRT.sizeDelta = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// Belirtilen hedef Transform'un Canvas üzerindeki local koordinatını hesaplar.
        /// </summary>
        private Vector2 GetLocalCanvasPosition(Transform target)
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
            else
            {
                // Hedef yoksa ekranın üst-ortasında göster
                screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.75f);
            }

            Camera canvasCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
            RectTransform containerRT = (_toastContainer as RectTransform) ?? (_canvas != null ? _canvas.GetComponent<RectTransform>() : null);

            if (containerRT != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, screenPoint, canvasCam, out Vector2 localPoint);
                return localPoint;
            }

            return Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════
        //  STATİK KULLANIM METOTLARI (Geriye dönük uyumlu + hedef-bazlı)
        // ═══════════════════════════════════════════════════════

        public static void ShowWarning(string keyOrText)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorWarning, UIThemeHelper.GetWarningIcon(), null);
        }

        public static void ShowWarning(string keyOrText, Transform target)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorWarning, UIThemeHelper.GetWarningIcon(), target);
        }

        public static void ShowError(string keyOrText)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorRedSpent, UIThemeHelper.GetWarningIcon(), null);
        }

        public static void ShowError(string keyOrText, Transform target)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorRedSpent, UIThemeHelper.GetWarningIcon(), target);
        }

        public static void ShowInfo(string keyOrText)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorTextWarm, null, null);
        }

        public static void ShowInfo(string keyOrText, Transform target)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorTextWarm, null, target);
        }

        public static void ShowSuccess(string keyOrText)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorGreenEarned, null, null);
        }

        public static void ShowSuccess(string keyOrText, Transform target)
        {
            Instance.TriggerToast(keyOrText, UIThemeHelper.ColorGreenEarned, null, target);
        }

        private void TriggerToast(string keyOrText, Color textColor, Sprite icon, Transform target)
        {
            EnsureCanvasAndContainer();
            string message = LocalizationManager.Get(keyOrText);

            if (_activeToastCoroutine != null)
            {
                StopCoroutine(_activeToastCoroutine);
            }

            // Önceki toastları temizle
            if (_toastContainer != null)
            {
                foreach (Transform child in _toastContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            _activeToastCoroutine = StartCoroutine(DisplayToastRoutine(message, textColor, icon, target));
        }

        private IEnumerator DisplayToastRoutine(string message, Color textColor, Sprite icon, Transform target)
        {
            EnsureCanvasAndContainer();
            if (_toastContainer == null) yield break;

            GameObject toastObj = new GameObject("ToastMessage", typeof(RectTransform), typeof(CanvasGroup));
            toastObj.transform.SetParent(_toastContainer, false);
            RectTransform rt = toastObj.GetComponent<RectTransform>();
            CanvasGroup cg = toastObj.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Zarif yarı-saydam arka plan (butona benzemeyen yumuşak bir gölge bandı)
            GameObject bgObj = new GameObject("SubtleBand", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(toastObj.transform, false);
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.05f, 0.03f, 0.78f);
            bgImg.raycastTarget = false;
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = new Vector2(40f, 20f);

            // Horizontal Layout (İkon + Metin)
            HorizontalLayoutGroup hlg = toastObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Uyarı İkonu
            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(toastObj.transform, false);
                Image img = iconObj.GetComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(42f, 42f);
            }

            // Metin (New Rocker SDF)
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(toastObj.transform, false);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            UIThemeHelper.ApplyNewRocker(tmp);
            tmp.text = message;
            tmp.fontSize = 38;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            // Boyut ayarlama
            Vector2 preferred = tmp.GetPreferredValues(message);
            float totalWidth = preferred.x + (icon != null ? 70f : 40f);
            rt.sizeDelta = new Vector2(Mathf.Clamp(totalWidth, 250f, 900f), Mathf.Max(56f, preferred.y + 20f));
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(preferred.x + 10f, preferred.y + 10f);

            // Hedef bazlı veya varsayılan konum
            Vector2 baseLocalPos;
            if (target != null)
            {
                baseLocalPos = GetLocalCanvasPosition(target);
            }
            else
            {
                // Hedef yoksa ekranın üst-ortasında göster (container-relative)
                baseLocalPos = new Vector2(0f, 320f);
            }

            // Başlangıç: Hedefin 40 birim üstü
            Vector2 initialPos = baseLocalPos + new Vector2(0f, 40f);
            Vector2 targetPos = initialPos + new Vector2(0f, 55f); // Yukarı doğru süzülüş

            // Animasyon: Hızlı pop in + yukarı süzülme + yumuşak fade out (toplam 1.1 saniye)
            float duration = 1.1f;
            float elapsed = 0f;
            rt.anchoredPosition = initialPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Hafif pop efekti: 0.8 -> 1.1 -> 1.0
                float scale;
                if (t < 0.15f)
                {
                    scale = Mathf.Lerp(0.8f, 1.1f, t / 0.15f);
                }
                else
                {
                    scale = Mathf.Lerp(1.1f, 1.0f, (t - 0.15f) / 0.85f);
                }
                rt.localScale = Vector3.one * scale;

                // Yukarı doğru yumuşak kayma (Ease Out Sine)
                rt.anchoredPosition = Vector2.Lerp(initialPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));

                // İlk %40 tam görünür, sonra fade out
                if (t > 0.4f)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
                }
                else
                {
                    cg.alpha = 1f;
                }

                yield return null;
            }

            Destroy(toastObj);
            _activeToastCoroutine = null;
        }
    }
}
