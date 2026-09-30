using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PotionShop
{
    public class CauldronCleaningMinigame : MonoBehaviour
    {
        [Header("Mini Oyun Ayarları")]
        public int requiredTaps = 10;
        private int currentTaps = 0;

        [Header("UI Referansları")]
        public GameObject minigamePanel;
        public Image cleaningProgressBar;
        public TextMeshProUGUI instructionsText;
        
        [Header("Kazan Görseli (Mini Oyun İçi)")]
        [Tooltip("Mini oyun panelinin içindeki kazan resmi - tıkladıkça sarsılacak")]
        public RectTransform cauldronVisual;

        [Header("Arka Plan Engelleyici")]
        [Tooltip("Mini oyun açıkken arkadaki her şeyi tıklanamaz yapan tam ekran şeffaf panel")]
        public GameObject blockerPanel;
        
        [Header("Kazan Referansı")]
        public Cauldron cauldron;

        [Header("Sarsılma Ayarları")]
        public float shakeIntensity = 10f;
        public float shakeDuration = 0.2f;

        private Quaternion _initialRotation = Quaternion.identity;
        private Coroutine _shakeCoroutine;
        private bool _isMinigameActive = false;

        private void Awake()
        {
            if (minigamePanel == null) minigamePanel = gameObject;
            if (cauldronVisual != null) _initialRotation = cauldronVisual.localRotation;
            EnsureHierarchyAndRaycaster();
        }

        private void Start()
        {
            EnsureHierarchyAndRaycaster();

            // Sadece mini oyun aktif değilken başlangıçta paneli gizle!
            // (Eğer minigame yeni açılmışsa Start() onu tekrar kapatmamalı)
            if (!_isMinigameActive)
            {
                if (minigamePanel != null && minigamePanel != gameObject)
                    minigamePanel.SetActive(false);
                else if (minigamePanel == gameObject)
                    gameObject.SetActive(false);

                if (blockerPanel != null)
                    blockerPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            EnsureHierarchyAndRaycaster();
            UpdateUI();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateUI();
        }

        public void EnsureHierarchyAndRaycaster()
        {
            GameObject targetObj = minigamePanel != null ? minigamePanel : gameObject;

            // 1. Paneli hiyerarşide en öne taşı (Tüm kardeş UI nesnelerinin üzerinde görünsün)
            targetObj.transform.SetAsLastSibling();

            // 2. Canvas ve Sorting Order ayarla
            // Eğer sahneden WorldSpace olarak kaydedilmiş hatalı bir Canvas geldiyse temizle
            Canvas canvas = targetObj.GetComponent<Canvas>();
            if (canvas != null && (canvas.renderMode == RenderMode.WorldSpace || (canvas.worldCamera == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)))
            {
                DestroyImmediate(canvas);
                canvas = null;
            }

            if (canvas == null)
            {
                canvas = targetObj.AddComponent<Canvas>();
            }

            Canvas rootCanvas = targetObj.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas != canvas)
            {
                canvas.sortingLayerID = rootCanvas.sortingLayerID;
                canvas.sortingLayerName = rootCanvas.sortingLayerName;
            }
            else
            {
                canvas.sortingLayerName = "UI";
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;

            // 3. GraphicRaycaster (Tıklamaların alınması için ZORUNLU)
            GraphicRaycaster gr = targetObj.GetComponent<GraphicRaycaster>();
            if (gr == null)
            {
                gr = targetObj.AddComponent<GraphicRaycaster>();
            }

            // 4. Kazan görseli üzerine Button ve tıklama raycast'ini bağla
            if (cauldronVisual != null)
            {
                Button cBtn = cauldronVisual.GetComponent<Button>();
                if (cBtn == null) cBtn = cauldronVisual.gameObject.AddComponent<Button>();
                cBtn.onClick.RemoveListener(OnTap);
                cBtn.onClick.AddListener(OnTap);

                Image cImg = cauldronVisual.GetComponent<Image>();
                if (cImg != null) cImg.raycastTarget = true;
            }

            // 5. Panel arkasında da tıklama yakalamak için panel butonunu dinle
            Button panelBtn = targetObj.GetComponent<Button>();
            if (panelBtn == null)
            {
                Image panelImg = targetObj.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.raycastTarget = true;
                    panelBtn = targetObj.AddComponent<Button>();
                }
            }

            if (panelBtn != null)
            {
                panelBtn.onClick.RemoveListener(OnTap);
                panelBtn.onClick.AddListener(OnTap);
            }
        }

        public void StartMinigame()
        {
            _isMinigameActive = true;
            currentTaps = 0;

            GameObject targetObj = minigamePanel != null ? minigamePanel : gameObject;
            
            // Eğer script minigamePanel üzerindeyse veya ayrıysa önce nesneyi aktif yap
            if (minigamePanel != null && minigamePanel != gameObject)
            {
                minigamePanel.SetActive(true);
            }
            gameObject.SetActive(true);

            if (cauldronVisual != null)
            {
                if (_initialRotation == Quaternion.identity) _initialRotation = cauldronVisual.localRotation;
                cauldronVisual.localRotation = _initialRotation;
            }

            EnsureHierarchyAndRaycaster();
            UpdateUI();
            
            // Önce blocker paneli aç
            if (blockerPanel != null)
                blockerPanel.SetActive(true);

            // Uyarı ikonunu kapat
            if (cauldron != null && cauldron.warningSign != null)
                cauldron.warningSign.SetActive(false);

            Debug.Log("<color=green>[CauldronCleaningMinigame]</color> Temizleme mini oyunu başlatıldı! Panel aktif, tıklanabilir ve en üst katmanda.");
        }

        // Oyuncu tıkladıkça çağrılacak (Mini oyun panelindeki büyük butona veya kazana bağla)
        public void OnTap()
        {
            currentTaps++;
            UpdateUI();

            // Kazanı sarsma efekti
            if (cauldronVisual != null)
            {
                if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = StartCoroutine(ShakeCauldron());
            }

            // Hafif dokunma ses efekti (varsa)
            if (AudioManager.Instance != null && AudioManager.Instance.cauldronDropItemSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.cauldronDropItemSFX);
            }

            if (currentTaps >= requiredTaps)
            {
                WinMinigame();
            }
        }

        private IEnumerator ShakeCauldron()
        {
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float angle = Random.Range(-shakeIntensity, shakeIntensity);
                cauldronVisual.localRotation = _initialRotation * Quaternion.Euler(0, 0, angle);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Sarsılma bitince düz pozisyona dön
            cauldronVisual.localRotation = _initialRotation;
            _shakeCoroutine = null;
        }

        private void UpdateUI()
        {
            if (cleaningProgressBar != null)
            {
                cleaningProgressBar.fillAmount = (float)currentTaps / requiredTaps;
            }

            if (instructionsText != null)
            {
                int kalan = Mathf.Max(0, requiredTaps - currentTaps);
                instructionsText.text = string.Format(LocalizationManager.Get("cauldron_clean_instruction"), kalan);
                UIThemeHelper.ApplyNewRocker(instructionsText);
            }
        }

        private void WinMinigame()
        {
            _isMinigameActive = false;
            Debug.Log("<color=green>[CauldronCleaningMinigame]</color> Mini oyun başarıyla tamamlandı! Kazan temizlendi.");

            if (minigamePanel != null)
                minigamePanel.SetActive(false);
            else
                gameObject.SetActive(false);

            if (blockerPanel != null)
                blockerPanel.SetActive(false);
                
            if (cauldron != null)
            {
                cauldron.CleanCauldron();
            }
        }

        // Oyun yarım bırakılırsa
        public void CloseWithoutFinishing()
        {
            _isMinigameActive = false;
            if (minigamePanel != null)
                minigamePanel.SetActive(false);
            else
                gameObject.SetActive(false);

            if (blockerPanel != null)
                blockerPanel.SetActive(false);
                
            // Uyarı tekrar çıksın çünkü kazan hala kirli
            if (cauldron != null && cauldron.warningSign != null)
                cauldron.warningSign.SetActive(true);
                
            Debug.LogWarning("Temizlik tamamlanmadı. Kazan hala kirli!");
        }
    }
}
