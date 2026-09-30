using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    [System.Serializable]
    public struct ElementalCrystal
    {
        public string name;
        public string symbol;
        public Color color;
        public Color glowColor;
    }

    /// <summary>
    /// Büyülü Kristal Arındırma (Simya Çemberi).
    /// 5 Element Kristali (Ateş, Su, Doğa, Şimşek, Arcane) ile refleks,
    /// kombo serileri ve Simya Coşkusu (Fever Rush) içerir.
    /// </summary>
    public class CrystalSortGame : MiniGameBase
    {
        public override string GameName => "Kristal Arındırma";
        public override string GameDescription => "Simya çemberindeki element kristallerini doğru sırayla arındır, Simya Coşkusuna ulaş!";

        [Header("Görsel Referanslar")]
        public Sprite altarCircleSprite;
        public Image altarBackgroundImage;

        [Header("Eski Referanslar (Geriye Uyumluluk)")]
        public Image targetColorImage;
        public Image currentCrystalImage;
        public Button crystalButton;

        [Header("UI Referansları")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI targetElementText;
        public TextMeshProUGUI feverText;
        public Transform crystalCircleContainer;
        public Image targetIndicatorImage;

        [Header("Element Kristalleri")]
        public ElementalCrystal[] elements = new ElementalCrystal[]
        {
            new ElementalCrystal { name = "ATEŞ",   symbol = "🔥", color = new Color(0.95f, 0.25f, 0.2f), glowColor = new Color(1f, 0.5f, 0.3f) },
            new ElementalCrystal { name = "SU",     symbol = "💧", color = new Color(0.2f, 0.65f, 0.98f), glowColor = new Color(0.5f, 0.85f, 1f) },
            new ElementalCrystal { name = "DOĞA",   symbol = "🌿", color = new Color(0.25f, 0.85f, 0.4f), glowColor = new Color(0.6f, 1f, 0.7f) },
            new ElementalCrystal { name = "ŞİMŞEK", symbol = "⚡", color = new Color(1f, 0.85f, 0.15f), glowColor = new Color(1f, 0.95f, 0.5f) },
            new ElementalCrystal { name = "ARCANE", symbol = "🔮", color = new Color(0.75f, 0.35f, 0.95f), glowColor = new Color(0.9f, 0.6f, 1f) }
        };

        private int score;
        private float timer;
        private int combo = 1;
        private int targetElementIndex = 0;

        // Fever / Simya Coşkusu Modu
        private float feverTimer = 0f;
        private bool isFever => feverTimer > 0f;

        private List<Button> spawnedButtons = new List<Button>();

        private void Awake()
        {
            EnsureSprites();
        }

        private void EnsureSprites()
        {
            if (altarCircleSprite == null)
            {
                altarCircleSprite = Resources.Load<Sprite>("MiniGames/crystal_altar_circle");
            }

            if (altarBackgroundImage != null && altarCircleSprite != null)
            {
                altarBackgroundImage.sprite = altarCircleSprite;
                altarBackgroundImage.color = Color.white;
            }
        }

        public override void StartGame()
        {
            EnsureSprites();
            if (gamePanel) gamePanel.SetActive(true);
            IsPlaying = true;

            score = 0;
            timer = gameDuration;
            combo = 1;
            feverTimer = 0f;

            SetupCircleButtons();
            PickNewTarget();
            UpdateUI();
        }

        public override void EndGame(bool won, int finalScore)
        {
            IsPlaying = false;
            if (gamePanel) gamePanel.SetActive(false);
            GrantRewards(finalScore);
        }

        private void SetupCircleButtons()
        {
            // Eğer çember container'ı varsa 5 kristali daire şeklinde diz
            if (crystalCircleContainer != null)
            {
                spawnedButtons.Clear();
                foreach (Transform child in crystalCircleContainer)
                {
                    Destroy(child.gameObject);
                }

                float radius = 170f;
                int count = elements.Length;

                for (int i = 0; i < count; i++)
                {
                    float angle = (i * (360f / count) - 90f) * Mathf.Deg2Rad;
                    Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                    GameObject btnObj = new GameObject($"Crystal_{elements[i].name}", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(crystalCircleContainer, false);
                    RectTransform rt = btnObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(85, 85);
                    rt.anchoredPosition = pos;

                    Image img = btnObj.GetComponent<Image>();
                    img.color = elements[i].color;

                    // İkon/Sembol Metni
                    GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lbl.transform.SetParent(btnObj.transform, false);
                    TextMeshProUGUI tmp = lbl.GetComponent<TextMeshProUGUI>();
                    tmp.text = $"{elements[i].symbol}\n<size=11>{elements[i].name}</size>";
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 26;
                    tmp.color = Color.white;
                    RectTransform lblRT = lbl.GetComponent<RectTransform>();
                    lblRT.anchorMin = Vector2.zero;
                    lblRT.anchorMax = Vector2.one;
                    lblRT.offsetMin = Vector2.zero;
                    lblRT.offsetMax = Vector2.zero;

                    Button btn = btnObj.GetComponent<Button>();
                    int idx = i;
                    btn.onClick.AddListener(() => OnElementTapped(idx));
                    spawnedButtons.Add(btn);
                }
            }
            else if (crystalButton != null)
            {
                // Geriye uyumluluk için tek buton
                crystalButton.onClick.RemoveAllListeners();
                crystalButton.onClick.AddListener(() => OnElementTapped(targetElementIndex));
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            timer -= Time.deltaTime;
            if (feverTimer > 0f)
            {
                feverTimer -= Time.deltaTime;
            }

            if (timer <= 0f)
            {
                timer = 0f;
                EndGame(true, score);
                return;
            }

            UpdateUI();
        }

        private void PickNewTarget()
        {
            int nextIdx = Random.Range(0, elements.Length);
            // Art arda aynı element gelme ihtimalini azalt
            if (nextIdx == targetElementIndex && elements.Length > 1)
            {
                nextIdx = (nextIdx + 1) % elements.Length;
            }
            targetElementIndex = nextIdx;

            var target = elements[targetElementIndex];

            if (targetElementText != null)
            {
                targetElementText.text = $"{target.symbol} ARINDIR: {target.name}!";
                targetElementText.color = target.glowColor;
            }

            if (targetIndicatorImage != null)
            {
                targetIndicatorImage.color = target.color;
            }

            if (targetColorImage != null)
            {
                targetColorImage.color = target.color;
            }
        }

        public void OnElementTapped(int elementIndex)
        {
            if (!IsPlaying) return;

            if (elementIndex == targetElementIndex)
            {
                // DOĞRU KRİSTAL!
                int multiplier = isFever ? 3 : 1;
                score += (15 * combo) * multiplier;
                combo++;

                // 5'li seride Simya Coşkusu (Fever)
                if (combo % 5 == 0 && !isFever)
                {
                    feverTimer = 5f;
                    timer = Mathf.Min(gameDuration, timer + 3f);
                }

                if (elementIndex < spawnedButtons.Count && spawnedButtons[elementIndex] != null)
                {
                    StartCoroutine(PunchScale(spawnedButtons[elementIndex].transform));
                }

                PickNewTarget();
            }
            else
            {
                // YANLIŞ KRİSTAL!
                combo = 1;
                feverTimer = 0f;
                timer = Mathf.Max(0f, timer - 1.5f); // Hata cezası
                StartCoroutine(WrongFlash());
            }

            UpdateUI();
        }

        private IEnumerator PunchScale(Transform tr)
        {
            Vector3 orig = Vector3.one;
            tr.localScale = new Vector3(1.25f, 1.25f, 1f);
            yield return new WaitForSeconds(0.08f);
            tr.localScale = orig;
        }

        private IEnumerator WrongFlash()
        {
            if (targetElementText != null)
            {
                Color orig = targetElementText.color;
                targetElementText.color = Color.red;
                yield return new WaitForSeconds(0.15f);
                targetElementText.color = orig;
            }
        }

        private void UpdateUI()
        {
            if (scoreText) scoreText.text = $"💎 Skor: {score}";
            if (timerText)
            {
                timerText.text = $"⏳ {Mathf.CeilToInt(timer)}s";
                timerText.color = timer < 10f ? Color.red : Color.white;
            }
            if (comboText)
            {
                if (combo > 1)
                {
                    comboText.text = $"🔥 Kombo: x{combo}";
                    comboText.gameObject.SetActive(true);
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }

            if (feverText)
            {
                if (isFever)
                {
                    feverText.text = $"✨ SİMYA COŞKUSU (3X): {feverTimer:F1}s ✨";
                    feverText.gameObject.SetActive(true);
                }
                else
                {
                    feverText.gameObject.SetActive(false);
                }
            }
        }
    }
}
