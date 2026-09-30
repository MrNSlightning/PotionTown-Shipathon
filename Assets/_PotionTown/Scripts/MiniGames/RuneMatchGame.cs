using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Büyülü Rün & İksir Eşleştirme Oyunu.
    /// Gerçek iksir sprite'ları, kart çevirme animasyonu, kombo çarpanı ve süre bonusu içerir.
    /// </summary>
    public class RuneMatchGame : MiniGameBase
    {
        public override string GameName => "Simya Rün Eşleştirme";
        public override string GameDescription => "Gizemli iksir ve rün kartlarını eşleştir, kombo yaparak büyük ödüller kazan!";

        [Header("Görsel Referanslar")]
        [Tooltip("Kartların arkasında görünecek süslemeli büyü arkalığı")]
        public Sprite cardBackSprite;
        [Tooltip("Eşleştirilecek 8 farklı iksir/rün görseli")]
        public Sprite[] potionSprites = new Sprite[8];
        public Color[] runeColors = new Color[8];

        [Header("UI Referansları")]
        public GameObject cardPrefab;
        public RectTransform gridContainer;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI comboText;

        private int score;
        private float timer;
        private int currentCombo = 1;
        private float lastMatchTime = -10f;

        private List<Button> cards = new List<Button>();
        private List<Image> cardIconImages = new List<Image>();
        private List<int> cardSpriteIndices = new List<int>();
        private List<Color> cardColors = new List<Color>();
        private List<bool> cardMatched = new List<bool>();

        private int firstSelectedIndex = -1;
        private bool isChecking = false;
        private int matchCount = 0;

        private void Awake()
        {
            EnsureSprites();
        }

        private void EnsureSprites()
        {
            if (cardBackSprite == null)
            {
                cardBackSprite = Resources.Load<Sprite>("MiniGames/rune_card_back");
            }

            bool needsSprites = false;
            if (potionSprites == null || potionSprites.Length < 8) needsSprites = true;
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    if (potionSprites[i] == null) { needsSprites = true; break; }
                }
            }

            if (needsSprites)
            {
                potionSprites = new Sprite[8];
                string[] defaultNames = {
                    "potion_red", "potion_blue", "potion_green", "potion_yellow",
                    "potion_purple", "potion_cyan", "potion_gold", "potion_orange"
                };
                for (int i = 0; i < defaultNames.Length; i++)
                {
                    potionSprites[i] = Resources.Load<Sprite>($"MiniGames/Potions/{defaultNames[i]}");
                }
            }
        }

        public override void StartGame()
        {
            EnsureSprites();
            if (gamePanel) gamePanel.SetActive(true);
            IsPlaying = true;
            score = 0;
            timer = gameDuration;
            matchCount = 0;
            currentCombo = 1;
            lastMatchTime = -10f;
            firstSelectedIndex = -1;
            isChecking = false;

            UpdateUI();
            SetupGrid();
        }

        public override void EndGame(bool won, int finalScore)
        {
            IsPlaying = false;
            if (gamePanel) gamePanel.SetActive(false);
            GrantRewards(finalScore);
        }

        private void SetupGrid()
        {
            if (gridContainer == null) return;

            // Önceki kartları temizle
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }
            cards.Clear();
            cardIconImages.Clear();
            cardSpriteIndices.Clear();
            cardColors.Clear();
            cardMatched.Clear();

            // 8 Çift (16 Kart) Hazırla
            List<int> deck = new List<int>();
            Color[] defaultColors = new Color[] {
                new Color(0.95f, 0.25f, 0.35f), new Color(0.25f, 0.65f, 0.95f),
                new Color(0.3f, 0.85f, 0.45f),  new Color(0.95f, 0.8f, 0.25f),
                new Color(0.75f, 0.35f, 0.95f), new Color(0.2f, 0.85f, 0.85f),
                new Color(0.95f, 0.55f, 0.15f), new Color(1f, 0.95f, 0.75f)
            };

            for (int i = 0; i < 8; i++)
            {
                deck.Add(i);
                deck.Add(i);
            }

            // Fisher-Yates Karıştırma
            for (int i = 0; i < deck.Count; i++)
            {
                int temp = deck[i];
                int randomIndex = Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }

            // 16 Kartı Grid'e Oluştur
            for (int i = 0; i < 16; i++)
            {
                GameObject newCard;
                if (cardPrefab != null)
                {
                    newCard = Instantiate(cardPrefab, gridContainer);
                }
                else
                {
                    newCard = new GameObject($"Card_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    newCard.transform.SetParent(gridContainer, false);
                }

                Button btn = newCard.GetComponent<Button>();
                Image cardBg = newCard.GetComponent<Image>();
                cardBg.color = Color.white;

                if (cardBackSprite != null)
                {
                    cardBg.sprite = cardBackSprite;
                    cardBg.type = Image.Type.Simple;
                }
                else
                {
                    cardBg.color = new Color(0.22f, 0.16f, 0.32f);
                }

                // Ön yüz için ikon objesi
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(newCard.transform, false);
                RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.15f, 0.15f);
                iconRT.anchorMax = new Vector2(0.85f, 0.85f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.gameObject.SetActive(false); // Başta kapalı

                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCardClicked(index));

                cards.Add(btn);
                cardIconImages.Add(iconImg);
                int spriteIdx = deck[i];
                cardSpriteIndices.Add(spriteIdx);
                cardColors.Add(defaultColors[spriteIdx]);
                cardMatched.Add(false);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                EndGame(false, score);
            }

            // Kombo süresi aşımı kontrolü (4 saniye)
            if (currentCombo > 1 && Time.time - lastMatchTime > 4f)
            {
                currentCombo = 1;
                UpdateUI();
            }

            UpdateUI();
        }

        private void OnCardClicked(int index)
        {
            if (isChecking || cardMatched[index] || firstSelectedIndex == index) return;

            // Kartı animasyonla aç
            StartCoroutine(FlipAnimation(index, true));

            if (firstSelectedIndex == -1)
            {
                firstSelectedIndex = index;
            }
            else
            {
                StartCoroutine(CheckMatch(firstSelectedIndex, index));
            }
        }

        private IEnumerator FlipAnimation(int index, bool showFront)
        {
            Button btn = cards[index];
            Image bg = btn.GetComponent<Image>();
            Image icon = cardIconImages[index];
            Transform t = btn.transform;

            float duration = 0.12f;
            float elapsed = 0f;

            // Yarıya kadar büzül (Scale X -> 0)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                t.localScale = new Vector3(Mathf.Lerp(1f, 0f, progress), 1f, 1f);
                yield return null;
            }

            // İçeriği değiştir
            int spriteIdx = cardSpriteIndices[index];
            if (showFront)
            {
                if (potionSprites != null && spriteIdx < potionSprites.Length && potionSprites[spriteIdx] != null)
                {
                    icon.sprite = potionSprites[spriteIdx];
                    icon.color = Color.white;
                    icon.gameObject.SetActive(true);
                    bg.color = new Color(0.18f, 0.12f, 0.28f);
                }
                else
                {
                    bg.color = cardColors[index];
                }
            }
            else
            {
                icon.gameObject.SetActive(false);
                if (cardBackSprite != null)
                {
                    bg.sprite = cardBackSprite;
                    bg.color = Color.white;
                }
                else
                {
                    bg.color = new Color(0.22f, 0.16f, 0.32f);
                }
            }

            // Tekrar açıl (Scale X -> 1)
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                t.localScale = new Vector3(Mathf.Lerp(0f, 1f, progress), 1f, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator CheckMatch(int firstIndex, int secondIndex)
        {
            isChecking = true;
            yield return new WaitForSeconds(0.45f);

            if (cardSpriteIndices[firstIndex] == cardSpriteIndices[secondIndex])
            {
                // EŞLEŞTİ!
                cardMatched[firstIndex] = true;
                cardMatched[secondIndex] = true;

                // Kombo kontrolü
                if (Time.time - lastMatchTime <= 4f)
                {
                    currentCombo++;
                }
                else
                {
                    currentCombo = 1;
                }
                lastMatchTime = Time.time;

                int gainedPoints = 15 * currentCombo;
                score += gainedPoints;
                matchCount++;

                // Süre bonusu (+2.5 saniye)
                timer = Mathf.Min(gameDuration, timer + 2.5f);

                // Eşleşen kartları hafif saydamlaştır
                cards[firstIndex].GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 0.7f);
                cards[secondIndex].GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 0.7f);

                if (matchCount >= 8)
                {
                    // TÜM KARTLAR BİTTİ - BÜYÜK ZAFER BONUSU
                    score += 50 + Mathf.CeilToInt(timer * 2);
                    yield return new WaitForSeconds(0.4f);
                    EndGame(true, score);
                }
            }
            else
            {
                // Eşleşmedi, geri çevir
                currentCombo = 1;
                StartCoroutine(FlipAnimation(firstIndex, false));
                StartCoroutine(FlipAnimation(secondIndex, false));
            }

            firstSelectedIndex = -1;
            isChecking = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (scoreText) scoreText.text = $"✨ Skor: {score}";
            if (timerText)
            {
                timerText.text = $"⏳ {Mathf.CeilToInt(timer)}s";
                timerText.color = timer < 10f ? Color.red : Color.white;
            }
            if (comboText)
            {
                if (currentCombo > 1)
                {
                    comboText.text = $"🔥 Kombo: x{currentCombo}!";
                    comboText.gameObject.SetActive(true);
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }
        }
    }
}
