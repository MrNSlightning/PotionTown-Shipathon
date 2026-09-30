using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PotionShop
{
    /// <summary>
    /// Parşomen prefabına eklenir. Sayfa prefabları arasında geçiş özelliği sağlar.
    /// Her sayfa ayrı bir Prefab olarak ayarlanmıştır.
    /// Ok butonları ve swipe (kaydırma) ile sayfalar arası geçiş yapılabilir.
    /// Sayfa geçişinde parşomen açılıp kapanma animasyonu oynatılır (biri tam kapanıp diğeri açılır).
    /// </summary>
    public class ParchmentPageController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum PageTransitionAnimation
        {
            ParchmentRoll, // Parşomen açılıp kapanma animasyonu (biri tam kapanır, sonra diğeri açılır)
            Slide          // Kayma animasyonu
        }

        [Header("Sayfa Prefabları")]
        [Tooltip("Sırasıyla tüm sayfa prefablarını buraya ekleyin (Sayfa 1, Sayfa 2, ... Sayfa 7)")]
        [FormerlySerializedAs("pages")]
        public List<GameObject> pagePrefabs = new List<GameObject>();

        [Header("Sayfa Kapsayıcısı (Container)")]
        [Tooltip("Sayfa prefablarının instantiate edileceği parent Transform. Boş bırakılırsa bu objenin altına oluşturulur.")]
        public Transform pageContainer;

        [Header("Navigasyon Butonları")]
        [Tooltip("Sonraki sayfaya geçiş butonu (sağ ok). Boş bırakılırsa otomatik bulunur.")]
        public Button nextButton;
        [Tooltip("Önceki sayfaya geçiş butonu (sol ok). Boş bırakılırsa otomatik bulunur.")]
        public Button prevButton;

        [Header("Sayfa Göstergesi (Opsiyonel)")]
        [Tooltip("Aktif sayfa numarasını gösteren metin (örn: '3 / 7')")]
        public TextMeshProUGUI pageIndicatorText;

        [Header("Animasyon Ayarları")]
        [Tooltip("Sayfa geçiş animasyonu türü: Parşomen açılıp kapanma veya Kayma")]
        public PageTransitionAnimation transitionType = PageTransitionAnimation.ParchmentRoll;

        [Tooltip("Açılma ve kapanma animasyonlarının her birinin süresi (saniye)")]
        public float transitionDuration = 0.25f;

        [Tooltip("Açılma/kapanma yönü: true = dikey (yukarıdan aşağı), false = yatay (soldan sağa)")]
        public bool verticalUnroll = true;

        [Header("Swipe Ayarları")]
        [Tooltip("Swipe olarak sayılması için gerekli minimum piksel mesafesi")]
        public float swipeThreshold = 50f;

        [Header("Önbellek Ayarları")]
        [Tooltip("Oluşturulan sayfalar hafızada tutulsun mu? Açıkken geçişler çok akıcı olur. Kapalıyken eski sayfa yok edilir.")]
        public bool cachePages = true;

        // Geriye dönük uyumluluk için property
        public List<GameObject> pages
        {
            get => pagePrefabs;
            set => pagePrefabs = value;
        }

        /// <summary>
        /// Sayfa değiştiğinde (yeni sayfa aktif olduğunda) tetiklenen event.
        /// Parametre: Yeni sayfanın 0-tabanlı indeksi.
        /// </summary>
        public event System.Action<int> OnPageChanged;

        /// <summary>
        /// Sayfa geçiş animasyonu devam ediyor mu?
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        // İç durum
        private int _currentPageIndex = 0;
        private bool _isTransitioning = false;
        private GameObject[] _instantiatedPages;
        private Dictionary<int, Vector3> _originalScales = new Dictionary<int, Vector3>();

        // Swipe takibi
        private Vector2 _dragStartPos;
        private bool _isDragging = false;

        public Transform TargetContainer
        {
            get
            {
                if (pageContainer != null) return pageContainer;

                Transform found = transform.Find("PageContainer");
                if (found == null) found = transform.Find("SayfaContainer");
                if (found != null)
                {
                    pageContainer = found;
                    return pageContainer;
                }

                return transform;
            }
        }

        private void Awake()
        {
            if (pagePrefabs != null && pagePrefabs.Count > 0)
            {
                _instantiatedPages = new GameObject[pagePrefabs.Count];
            }

            // Sahnedeki eski statik sayfa objelerini gizle
            HideOldStaticPages();
        }

        private void Start()
        {
            // Butonları otomatik bul (Inspector'da atanmamışsa)
            AutoFindButtons();

            // Buton event'lerini bağla
            if (nextButton != null)
                nextButton.onClick.AddListener(GoToNextPage);

            if (prevButton != null)
                prevButton.onClick.AddListener(GoToPreviousPage);

            // Sahnedeki eski statik sayfa objelerini gizle
            HideOldStaticPages();

            // İlk sayfayı göster
            ShowPage(_currentPageIndex, false);
        }

        private void OnDestroy()
        {
            // Event listener'ları temizle
            if (nextButton != null)
                nextButton.onClick.RemoveListener(GoToNextPage);

            if (prevButton != null)
                prevButton.onClick.RemoveListener(GoToPreviousPage);
        }

        /// <summary>
        /// Dışarıdan dinamik olarak sayfa listesi atamak için kullanılır (örn: Tarifler filtre popup'ı).
        /// </summary>
        public void SetPages(List<GameObject> newPages)
        {
            if (_instantiatedPages != null)
            {
                for (int i = 0; i < _instantiatedPages.Length; i++)
                {
                    if (_instantiatedPages[i] != null && (newPages == null || !newPages.Contains(_instantiatedPages[i])))
                    {
                        Destroy(_instantiatedPages[i]);
                    }
                }
            }

            pagePrefabs = newPages != null ? new List<GameObject>(newPages) : new List<GameObject>();
            _instantiatedPages = new GameObject[pagePrefabs.Count];
            if (_originalScales == null) _originalScales = new Dictionary<int, Vector3>();
            _originalScales.Clear();
            _currentPageIndex = 0;

            AutoFindButtons();

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(GoToNextPage);
                nextButton.onClick.AddListener(GoToNextPage);
            }
            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(GoToPreviousPage);
                prevButton.onClick.AddListener(GoToPreviousPage);
            }

            ShowPage(0, false);
        }

        #region Buton ve Obje Bulma Yardımcıları

        /// <summary>
        /// nextButton veya prevButton atanmamışsa hiyerarşide otomatik arar.
        /// </summary>
        private void AutoFindButtons()
        {
            if (nextButton != null && prevButton != null) return;

            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string n = btn.gameObject.name.ToLower();
                if (nextButton == null && (n.Contains("next") || n.Contains("ileri") || n.Contains("sag") || n.Contains("sağ") || n.Contains("ok işareti_1") || n.Contains("right")))
                {
                    nextButton = btn;
                }
                else if (prevButton == null && (n.Contains("prev") || n.Contains("geri") || n.Contains("sol") || n.Contains("left") || n.Contains("ok işareti_0")))
                {
                    prevButton = btn;
                }
            }

            // Button componenti eklenmemiş görsel objeler varsa Button ekle
            if (nextButton == null || prevButton == null)
            {
                Transform[] transforms = GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    string n = t.gameObject.name.ToLower();
                    if (nextButton == null && (n.Contains("next") || n.Contains("ileri") || n.Contains("sag") || n.Contains("sağ") || n.Contains("ok işareti_1") || n.Contains("right")))
                    {
                        Button b = t.gameObject.GetComponent<Button>();
                        if (b == null) b = t.gameObject.AddComponent<Button>();
                        Image img = t.gameObject.GetComponent<Image>();
                        if (img != null) img.raycastTarget = true;
                        nextButton = b;
                    }
                    else if (prevButton == null && (n.Contains("prev") || n.Contains("geri") || n.Contains("sol") || n.Contains("left") || n.Contains("ok işareti_0")))
                    {
                        Button b = t.gameObject.GetComponent<Button>();
                        if (b == null) b = t.gameObject.AddComponent<Button>();
                        Image img = t.gameObject.GetComponent<Image>();
                        if (img != null) img.raycastTarget = true;
                        prevButton = b;
                    }
                }
            }

            // Sayfa gösterge metnini otomatik bul
            if (pageIndicatorText == null)
            {
                TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    string n = txt.gameObject.name.ToLower();
                    if (n.Contains("indicator") || n.Contains("sayfa") || n.Contains("page") || n.Contains("sayac") || n.Contains("counter"))
                    {
                        pageIndicatorText = txt;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Hiyerarşide eskiden kalan statik 'Sayfa 1', 'Öz Tarifleri' vb. objeler varsa çakışmayı önlemek için gizler.
        /// </summary>
        private void HideOldStaticPages()
        {
            if (pagePrefabs == null || pagePrefabs.Count == 0) return;

            Transform container = TargetContainer;
            foreach (Transform child in container)
            {
                string n = child.name.ToLower();
                // UI butonları, metinleri veya karartma arka planını atla
                if (child.GetComponent<Button>() != null || child.GetComponent<TextMeshProUGUI>() != null)
                    continue;
                if (n.Contains("button") || n.Contains("ok işareti") || n.Contains("backdrop") || n.Contains("indicator") || n.Contains("dark"))
                    continue;

                // Dinamik olarak oluşturulan instance'ları atla
                if (n.Contains("_instance"))
                    continue;

                bool isTracked = false;
                if (_instantiatedPages != null)
                {
                    foreach (var p in _instantiatedPages)
                    {
                        if (p != null && p.transform == child)
                        {
                            isTracked = true;
                            break;
                        }
                    }
                }
                if (!isTracked)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Navigasyon butonlarının sayfa prefablarının arkasında kalmasını önler.
        /// </summary>
        private void BringNavUItoFront()
        {
            if (TargetContainer == transform)
            {
                if (prevButton != null) prevButton.transform.SetAsLastSibling();
                if (nextButton != null) nextButton.transform.SetAsLastSibling();
                if (pageIndicatorText != null) pageIndicatorText.transform.SetAsLastSibling();
            }
        }

        #endregion

        #region Sayfa Yönetimi (Prefab Instantiation)

        /// <summary>
        /// İlgili indexteki sayfa örneğini döndürür. Henüz oluşturulmamışsa prefabtan instantiate eder.
        /// Yeni oluşturulan sayfa varsayılan olarak KAPALI (inactive) durumdadır.
        /// </summary>
        private GameObject GetOrCreatePage(int index)
        {
            if (pagePrefabs == null || index < 0 || index >= pagePrefabs.Count) return null;
            GameObject prefab = pagePrefabs[index];
            if (prefab == null) return null;

            if (_instantiatedPages == null || _instantiatedPages.Length != pagePrefabs.Count)
            {
                _instantiatedPages = new GameObject[pagePrefabs.Count];
            }

            // Daha önce oluşturulmuşsa döndür
            if (_instantiatedPages[index] != null)
            {
                return _instantiatedPages[index];
            }

            Transform container = TargetContainer;
            GameObject pageInstance;

            // Eğer prefab aslında sahnede var olan bir obje ise doğrudan kullan
            if (prefab.scene.IsValid() && !string.IsNullOrEmpty(prefab.scene.name))
            {
                pageInstance = prefab;
            }
            else
            {
                // Prefab asset: Instantiate et (worldPositionStays = false ile prefab local değerlerini koru)
                pageInstance = Instantiate(prefab, container, false);
                pageInstance.name = $"{prefab.name}_Instance";

                // Prefabın orijinal scale değerini kaydet ve uygula
                RectTransform prefabRect = prefab.GetComponent<RectTransform>();
                RectTransform instRect = pageInstance.GetComponent<RectTransform>();
                if (prefabRect != null && instRect != null)
                {
                    instRect.localScale = prefabRect.localScale;
                    _originalScales[index] = prefabRect.localScale;
                }
            }

            if (!_originalScales.ContainsKey(index))
            {
                RectTransform rt = pageInstance.GetComponent<RectTransform>();
                _originalScales[index] = rt != null ? rt.localScale : pageInstance.transform.localScale;
            }

            // ÇOK ÖNEMLİ: Yeni oluşturulan obje ilk başta KAPALI olmalıdır!
            // Böylece diğer sayfanın üzerine aniden görünür olarak binmez.
            pageInstance.SetActive(false);

            // Sayfa üzerinde swipe yapabilmek için drag iletici ekle
            EnsureDragForwarder(pageInstance);

            _instantiatedPages[index] = pageInstance;
            BringNavUItoFront();

            return pageInstance;
        }

        /// <summary>
        /// Sayfa objesini yok eder (cachePages kapalıysa kullanılır).
        /// </summary>
        private void DestroyPageInstance(int index)
        {
            if (_instantiatedPages == null || index < 0 || index >= _instantiatedPages.Length) return;

            GameObject pageObj = _instantiatedPages[index];
            if (pageObj != null)
            {
                bool isInstantiatedCopy = pagePrefabs != null && index < pagePrefabs.Count && pagePrefabs[index] != pageObj;
                if (isInstantiatedCopy)
                {
                    Destroy(pageObj);
                }
                else
                {
                    pageObj.SetActive(false);
                }
                _instantiatedPages[index] = null;
            }
        }

        /// <summary>
        /// Sayfanın orijinal localScale değerini döndürür.
        /// </summary>
        private Vector3 GetPageOriginalScale(int index, GameObject pageObj)
        {
            if (_originalScales.TryGetValue(index, out Vector3 scale))
                return scale;

            if (pagePrefabs != null && index >= 0 && index < pagePrefabs.Count && pagePrefabs[index] != null)
            {
                RectTransform rt = pagePrefabs[index].GetComponent<RectTransform>();
                if (rt != null)
                {
                    _originalScales[index] = rt.localScale;
                    return rt.localScale;
                }
            }

            if (pageObj != null)
            {
                RectTransform rt = pageObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    _originalScales[index] = rt.localScale;
                    return rt.localScale;
                }
            }

            return Vector3.one;
        }

        /// <summary>
        /// Orijinal ölçeğe göre kapalı (rulo sarılmış) ölçeği hesaplar.
        /// </summary>
        private Vector3 GetClosedScale(Vector3 openScale)
        {
            return verticalUnroll
                ? new Vector3(openScale.x, 0f, openScale.z)
                : new Vector3(0f, openScale.y, openScale.z);
        }

        /// <summary>
        /// Sayfa üzerinde tıklandığında/kaydırıldığında drag event'ini bu controller'a yönlendirir.
        /// </summary>
        private void EnsureDragForwarder(GameObject pageObj)
        {
            if (pageObj == null) return;
            ParchmentDragForwarder forwarder = pageObj.GetComponent<ParchmentDragForwarder>();
            if (forwarder == null)
            {
                forwarder = pageObj.AddComponent<ParchmentDragForwarder>();
            }
            forwarder.controller = this;
        }

        #endregion

        #region Sayfa Navigasyonu

        /// <summary>
        /// Sonraki sayfaya geçer.
        /// </summary>
        public void GoToNextPage()
        {
            if (_isTransitioning) return;
            if (pagePrefabs == null || _currentPageIndex >= pagePrefabs.Count - 1) return;

            int oldIndex = _currentPageIndex;
            int newIndex = _currentPageIndex + 1;
            StartCoroutine(TransitionPages(oldIndex, newIndex, SlideDirection.Left));
        }

        /// <summary>
        /// Önceki sayfaya geçer.
        /// </summary>
        public void GoToPreviousPage()
        {
            if (_isTransitioning) return;
            if (pagePrefabs == null || _currentPageIndex <= 0) return;

            int oldIndex = _currentPageIndex;
            int newIndex = _currentPageIndex - 1;
            StartCoroutine(TransitionPages(oldIndex, newIndex, SlideDirection.Right));
        }

        /// <summary>
        /// Belirli bir sayfaya atlar.
        /// </summary>
        public void GoToPage(int index, bool animated = false)
        {
            if (pagePrefabs == null || index < 0 || index >= pagePrefabs.Count) return;
            if (index == _currentPageIndex) return;

            if (animated && !_isTransitioning)
            {
                SlideDirection dir = index > _currentPageIndex ? SlideDirection.Left : SlideDirection.Right;
                StartCoroutine(TransitionPages(_currentPageIndex, index, dir));
            }
            else
            {
                _currentPageIndex = index;
                ShowPage(_currentPageIndex, false);
            }
        }

        /// <summary>
        /// Aktif sayfa numarası.
        /// </summary>
        public int CurrentPageIndex => _currentPageIndex;

        /// <summary>
        /// Toplam sayfa sayısı.
        /// </summary>
        public int TotalPages => pagePrefabs != null ? pagePrefabs.Count : 0;

        #endregion

        #region Swipe (Drag) Desteği

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isTransitioning) return;
            _dragStartPos = eventData.position;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Drag sırasında görsel feedback verilebilir
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || _isTransitioning) return;
            _isDragging = false;

            Vector2 dragEndPos = eventData.position;
            float horizontalDelta = dragEndPos.x - _dragStartPos.x;

            if (Mathf.Abs(horizontalDelta) >= swipeThreshold)
            {
                if (horizontalDelta < 0)
                {
                    GoToNextPage();
                }
                else
                {
                    GoToPreviousPage();
                }
            }
        }

        #endregion

        #region Animasyon ve Görüntüleme

        private enum SlideDirection { Left, Right }

        /// <summary>
        /// Sayfa geçiş animasyonunu başlatır.
        /// Seçilen transitionType'a göre açılıp kapanma veya kayma oynatır.
        /// </summary>
        private IEnumerator TransitionPages(int oldIndex, int newIndex, SlideDirection direction)
        {
            _isTransitioning = true;

            if (transitionType == PageTransitionAnimation.ParchmentRoll)
            {
                yield return StartCoroutine(ParchmentRollTransition(oldIndex, newIndex));
            }
            else
            {
                yield return StartCoroutine(SlideTransition(oldIndex, newIndex, direction));
            }

            _isTransitioning = false;
            OnPageChanged?.Invoke(newIndex);
        }

        /// <summary>
        /// Parşomen Açılıp Kapanma Geçişi:
        /// 1. Mevcut sayfa parşomen gibi tamamen kapanır (rulo sarılır).
        /// 2. Sayfa kapandıktan sonra sayfa göstergesi ve butonlar güncellenir.
        /// 3. Yeni sayfa kapalı olarak hazırlanır, aktif edilir ve tamamen açılır (rulo açılır).
        /// Sayfalar ASLA üst üste binmez!
        /// </summary>
        private IEnumerator ParchmentRollTransition(int oldIndex, int newIndex)
        {
            float duration = transitionDuration > 0f ? transitionDuration : 0.25f;

            // --- 1. ADIM: MEVCUT SAYFAYI TAMAMEN KAPAT ---
            GameObject oldPage = GetOrCreatePage(oldIndex);
            if (oldPage != null && oldPage.activeSelf)
            {
                RectTransform oldRect = oldPage.GetComponent<RectTransform>();
                CanvasGroup oldCg = oldPage.GetComponent<CanvasGroup>();
                if (oldCg == null) oldCg = oldPage.AddComponent<CanvasGroup>();

                Vector3 oldOpenScale = GetPageOriginalScale(oldIndex, oldPage);
                Vector3 oldClosedScale = GetClosedScale(oldOpenScale);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = EaseInBack(t);

                    if (oldRect != null)
                    {
                        oldRect.localScale = Vector3.LerpUnclamped(oldOpenScale, oldClosedScale, easedT);
                    }
                    if (oldCg != null)
                    {
                        oldCg.alpha = 1f - Mathf.Clamp01(t * 1.5f - 0.3f);
                    }
                    yield return null;
                }

                // Kapanma tamamlandı: eski sayfayı hemen kapat
                oldPage.SetActive(false);

                // Orijinal durumunu geri yükle (gelecekte tekrar açılmaya hazır olsun)
                if (oldRect != null)
                {
                    oldRect.localScale = oldOpenScale;
                    oldRect.anchoredPosition = Vector2.zero;
                }
                if (oldCg != null) oldCg.alpha = 1f;

                if (!cachePages)
                {
                    DestroyPageInstance(oldIndex);
                }
            }

            // --- 2. ADIM: SAYFA NUMARASINI VE ARAYÜZÜ GÜNCELLE ---
            _currentPageIndex = newIndex;
            UpdateUI();

            // --- 3. ADIM: YENİ SAYFAYI AL, KAPALI DURUMDA HAZIRLA VE AÇ ---
            // newPage yalnızca oldPage kapandıktan sonra oluşturulur veya çağrılır!
            GameObject newPage = GetOrCreatePage(newIndex);
            if (newPage == null)
            {
                yield break;
            }

            RectTransform newRect = newPage.GetComponent<RectTransform>();
            CanvasGroup newCg = newPage.GetComponent<CanvasGroup>();
            if (newCg == null) newCg = newPage.AddComponent<CanvasGroup>();

            Vector3 newOpenScale = GetPageOriginalScale(newIndex, newPage);
            Vector3 newClosedScale = GetClosedScale(newOpenScale);

            // ÖNEMLİ: Görünür yapmadan önce kapalı ölçeğe ve 0 alfaya ayarla!
            if (newRect != null)
            {
                newRect.anchoredPosition = Vector2.zero;
                newRect.localScale = newClosedScale;
            }
            if (newCg != null)
            {
                newCg.alpha = 0f;
            }

            // Şimdi aktif et (böylece asla açık halde ekranda parlamaz veya üst üste binmez)
            newPage.SetActive(true);
            BringNavUItoFront();

            float elapsedOpen = 0f;
            while (elapsedOpen < duration)
            {
                elapsedOpen += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedOpen / duration);
                float easedT = EaseOutBack(t);

                if (newRect != null)
                {
                    newRect.localScale = Vector3.LerpUnclamped(newClosedScale, newOpenScale, easedT);
                }
                if (newCg != null)
                {
                    newCg.alpha = Mathf.Clamp01(t * 2f);
                }
                yield return null;
            }

            // Açılma tamamlandı: kesin değerlere oturt
            if (newRect != null) newRect.localScale = newOpenScale;
            if (newCg != null) newCg.alpha = 1f;
        }

        /// <summary>
        /// Slide (Kayma) Geçişi (Alternatif).
        /// </summary>
        private IEnumerator SlideTransition(int oldIndex, int newIndex, SlideDirection direction)
        {
            GameObject oldPage = GetOrCreatePage(oldIndex);
            GameObject newPage = GetOrCreatePage(newIndex);

            if (newPage == null)
            {
                yield break;
            }

            RectTransform oldRect = oldPage != null ? oldPage.GetComponent<RectTransform>() : null;
            RectTransform newRect = newPage.GetComponent<RectTransform>();

            float slideDistance = GetSlideDistance();

            Vector2 oldStartPos = Vector2.zero;
            Vector2 oldEndPos = direction == SlideDirection.Left
                ? new Vector2(-slideDistance, 0f)
                : new Vector2(slideDistance, 0f);

            Vector2 newStartPos = direction == SlideDirection.Left
                ? new Vector2(slideDistance, 0f)
                : new Vector2(-slideDistance, 0f);
            Vector2 newEndPos = Vector2.zero;

            if (newRect != null)
            {
                newRect.anchoredPosition = newStartPos;
                newRect.localScale = GetPageOriginalScale(newIndex, newPage);
            }
            CanvasGroup newCg = newPage.GetComponent<CanvasGroup>();
            if (newCg != null) newCg.alpha = 1f;

            newPage.SetActive(true);
            BringNavUItoFront();

            if (oldRect == null || newRect == null || transitionDuration <= 0f)
            {
                if (oldPage != null)
                {
                    if (cachePages)
                        oldPage.SetActive(false);
                    else
                        DestroyPageInstance(oldIndex);
                }

                if (newRect != null)
                    newRect.anchoredPosition = Vector2.zero;

                _currentPageIndex = newIndex;
                UpdateUI();
                yield break;
            }

            oldRect.anchoredPosition = oldStartPos;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float easedT = EaseInOutQuad(t);

                oldRect.anchoredPosition = Vector2.Lerp(oldStartPos, oldEndPos, easedT);
                newRect.anchoredPosition = Vector2.Lerp(newStartPos, newEndPos, easedT);

                yield return null;
            }

            oldRect.anchoredPosition = oldEndPos;
            newRect.anchoredPosition = newEndPos;

            if (cachePages)
            {
                oldPage.SetActive(false);
                oldRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                DestroyPageInstance(oldIndex);
            }

            newRect.anchoredPosition = Vector2.zero;
            _currentPageIndex = newIndex;
            UpdateUI();
        }

        /// <summary>
        /// Belirtilen sayfayı gösterir, diğerlerini kapatır (animasyonsuz doğrudan açılış).
        /// </summary>
        private void ShowPage(int index, bool animated = false)
        {
            if (pagePrefabs == null || pagePrefabs.Count == 0)
            {
                UpdateUI();
                return;
            }

            index = Mathf.Clamp(index, 0, pagePrefabs.Count - 1);
            _currentPageIndex = index;

            // Diğer sayfaları kapat veya yok et
            if (_instantiatedPages != null)
            {
                for (int i = 0; i < _instantiatedPages.Length; i++)
                {
                    if (i != index && _instantiatedPages[i] != null)
                    {
                        if (cachePages)
                        {
                            _instantiatedPages[i].SetActive(false);
                            RectTransform r = _instantiatedPages[i].GetComponent<RectTransform>();
                            if (r != null)
                            {
                                r.anchoredPosition = Vector2.zero;
                                r.localScale = GetPageOriginalScale(i, _instantiatedPages[i]);
                            }
                            CanvasGroup cg = _instantiatedPages[i].GetComponent<CanvasGroup>();
                            if (cg != null) cg.alpha = 1f;
                        }
                        else
                        {
                            DestroyPageInstance(i);
                        }
                    }
                }
            }

            // Aktif sayfayı göster
            GameObject activePage = GetOrCreatePage(index);
            if (activePage != null)
            {
                RectTransform rect = activePage.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = GetPageOriginalScale(index, activePage);
                }
                CanvasGroup cg = activePage.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;

                activePage.SetActive(true);
            }

            BringNavUItoFront();
            UpdateUI();
            OnPageChanged?.Invoke(_currentPageIndex);
        }

        /// <summary>
        /// Buton görünürlüğünü ve sayfa göstergesini günceller.
        /// </summary>
        private void UpdateUI()
        {
            int total = TotalPages;

            // İlk sayfada Geri butonunu gizle
            if (prevButton != null)
                prevButton.gameObject.SetActive(_currentPageIndex > 0 && total > 1);

            // Son sayfada İleri butonunu gizle
            if (nextButton != null)
                nextButton.gameObject.SetActive(_currentPageIndex < total - 1 && total > 1);

            // Sayfa göstergesini güncelle
            if (pageIndicatorText != null)
            {
                if (total > 0)
                    pageIndicatorText.text = $"{_currentPageIndex + 1} / {total}";
                else
                    pageIndicatorText.text = "";
            }
        }

        /// <summary>
        /// Sayfa genişliğini dinamik hesaplar (Slide geçişi için).
        /// </summary>
        private float GetSlideDistance()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null && rect.rect.width > 0)
            {
                return rect.rect.width;
            }

            CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null && scaler.referenceResolution.x > 0)
            {
                return scaler.referenceResolution.x;
            }

            return Screen.width > 0 ? Screen.width : 1080f;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Sayfa Prefablarını Otomatik Bul ve Ata")]
        public void AutoFindPagePrefabs()
        {
            pagePrefabs.Clear();
            string[] guids = AssetDatabase.FindAssets("Sayfa t:Prefab", new[] { "Assets/_PotionTown/Prefabs" });
            var sortedPrefabs = new SortedDictionary<int, GameObject>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                    var match = System.Text.RegularExpressions.Regex.Match(filename, @"Sayfa\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNum))
                    {
                        sortedPrefabs[pageNum] = prefab;
                    }
                }
            }

            foreach (var kvp in sortedPrefabs)
            {
                pagePrefabs.Add(kvp.Value);
            }

            AutoFindButtons();
            EditorUtility.SetDirty(this);
            Debug.Log($"ParchmentPageController: {pagePrefabs.Count} adet sayfa prefabı otomatik olarak atandı.");
        }
#endif
    }

    /// <summary>
    /// Prefab sayfaların üzerine tıklandığında ve kaydırıldığında
    /// drag olaylarını ana ParchmentPageController'a iletir.
    /// </summary>
    public class ParchmentDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [HideInInspector]
        public ParchmentPageController controller;

        public void OnBeginDrag(PointerEventData eventData) => controller?.OnBeginDrag(eventData);
        public void OnDrag(PointerEventData eventData) => controller?.OnDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => controller?.OnEndDrag(eventData);
    }
}
