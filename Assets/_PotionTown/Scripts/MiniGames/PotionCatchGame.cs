using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    public enum PotionDropType
    {
        RegularHealth,  // +10 Puan
        ManaArcane,     // +20 Puan
        StardustTime,   // +30 Puan & +3s Süre
        GoldenElixir,   // +50 Puan & 5s Çift Puan (Altın Çağ)
        ToxicHazard,    // -1 Can
        ChaosBomb       // -1 Can & Ekran Sarsıntısı
    }

    /// <summary>
    /// Büyülü Kazan İksir Yağmuru.
    /// Farklı iksir tipleri, altın çağ çılgınlığı, zehir tehlikeleri,
    /// fare/dokunmatik sürükleme ve klavye desteği içerir.
    /// </summary>
    public class PotionCatchGame : MiniGameBase
    {
        public override string GameName => "Kazan İksir Yağmuru";
        public override string GameDescription => "Gökten yağan büyülü iksirleri kazana topla, zehir ve bombalardan kaçın!";

        [Header("Görsel Referanslar")]
        public Sprite cauldronSprite;
        public Sprite healthPotionSprite;
        public Sprite manaPotionSprite;
        public Sprite stardustPotionSprite;
        public Sprite goldPotionSprite;
        public Sprite toxicSprite;
        public Sprite bombSprite;
        public Sprite heartSprite;

        [Header("UI Referansları")]
        public RectTransform cauldron;
        public RectTransform spawnArea;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI livesText;
        public TextMeshProUGUI frenzyText;
        public Transform heartsContainer;

        [Header("Oynanış Ayarları")]
        public float moveSpeed = 650f;
        public float baseFallSpeed = 220f;
        public float spawnInterval = 0.75f;

        private int score;
        private float timer;
        private int lives;
        private float currentFallSpeed;
        private float spawnTimer;

        // Fever / Altın Çağ Modu
        private float goldFrenzyTimer = 0f;
        private bool isFrenzy => goldFrenzyTimer > 0f;

        // Düşen iksirler havuzu
        private class ActivePotion
        {
            public RectTransform rect;
            public Image image;
            public PotionDropType type;
            public float speedMultiplier;
        }

        private List<ActivePotion> activePotions = new List<ActivePotion>();
        private List<Image> heartImages = new List<Image>();

        private void Awake()
        {
            EnsureSprites();
        }

        private void EnsureSprites()
        {
            if (cauldronSprite == null) cauldronSprite = Resources.Load<Sprite>("MiniGames/cauldron_sprite");
            if (healthPotionSprite == null) healthPotionSprite = Resources.Load<Sprite>("MiniGames/Potions/potion_red");
            if (manaPotionSprite == null) manaPotionSprite = Resources.Load<Sprite>("MiniGames/Potions/potion_blue");
            if (stardustPotionSprite == null) stardustPotionSprite = Resources.Load<Sprite>("MiniGames/Potions/potion_cyan");
            if (goldPotionSprite == null) goldPotionSprite = Resources.Load<Sprite>("MiniGames/Potions/potion_gold");
            if (toxicSprite == null) toxicSprite = Resources.Load<Sprite>("MiniGames/Potions/hazard_poison");
            if (bombSprite == null) bombSprite = Resources.Load<Sprite>("MiniGames/Potions/hazard_bomb");
            if (heartSprite == null) heartSprite = Resources.Load<Sprite>("MiniGames/Potions/heart");

            // Kazan görselini uygula
            if (cauldron != null && cauldronSprite != null)
            {
                Image cImg = cauldron.GetComponent<Image>();
                if (cImg != null)
                {
                    cImg.sprite = cauldronSprite;
                    cImg.color = Color.white;
                    cImg.preserveAspect = true;
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
            lives = 3;
            currentFallSpeed = baseFallSpeed;
            spawnTimer = spawnInterval;
            goldFrenzyTimer = 0f;

            ClearPotions();
            SetupHearts();
            UpdateUI();
        }

        public override void EndGame(bool won, int finalScore)
        {
            IsPlaying = false;
            ClearPotions();
            if (gamePanel) gamePanel.SetActive(false);
            GrantRewards(finalScore);
        }

        private void SetupHearts()
        {
            if (heartsContainer == null) return;
            heartImages.Clear();
            foreach (Transform child in heartsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < 3; i++)
            {
                GameObject hObj = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
                hObj.transform.SetParent(heartsContainer, false);
                Image hImg = hObj.GetComponent<Image>();
                if (heartSprite != null)
                {
                    hImg.sprite = heartSprite;
                    hImg.preserveAspect = true;
                }
                else
                {
                    hImg.color = new Color(0.95f, 0.2f, 0.3f);
                }
                heartImages.Add(hImg);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            // 1. Kazan Hareketi (Klavye + Fare / Dokunmatik Yumuşak Takip)
            HandleMovement();

            // 2. Süre ve Hızlanma
            timer -= Time.deltaTime;
            currentFallSpeed += Time.deltaTime * 2.2f;

            if (goldFrenzyTimer > 0f)
            {
                goldFrenzyTimer -= Time.deltaTime;
            }

            if (timer <= 0f)
            {
                timer = 0f;
                EndGame(true, score);
                return;
            }

            // 3. İksir Üretimi
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnPotion();
                float curInterval = isFrenzy ? spawnInterval * 0.5f : spawnInterval;
                spawnTimer = Mathf.Max(0.35f, curInterval - (60f - timer) * 0.005f);
            }

            // 4. İksirleri Düşürme ve Çarpışma Kontrolü
            UpdateFallingPotions();

            UpdateUI();
        }

        private void HandleMovement()
        {
            if (cauldron == null || spawnArea == null) return;

            float halfWidth = spawnArea.rect.width / 2f - cauldron.rect.width / 2f;
            Vector2 pos = cauldron.anchoredPosition;

            bool hasPointerInput = false;

            // Dokunmatik giriş (mobil öncelikli)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began)
                {
                    Vector2 localTouch;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnArea, touch.position, null, out localTouch);
                    pos.x = Mathf.MoveTowards(pos.x, localTouch.x, moveSpeed * 1.5f * Time.deltaTime);
                    hasPointerInput = true;
                }
            }
            // Fare / Desktop desteği
            else if (Input.GetMouseButton(0))
            {
                Vector2 localMouse;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnArea, Input.mousePosition, null, out localMouse);
                pos.x = Mathf.MoveTowards(pos.x, localMouse.x, moveSpeed * 1.5f * Time.deltaTime);
                hasPointerInput = true;
            }

            // Klavye desteği (yalnızca masaüstü - mobilde zaten dokunmatik kullanılır)
            if (!hasPointerInput)
            {
                float moveInput = Input.GetAxis("Horizontal");
                if (moveInput != 0f)
                {
                    pos.x += moveInput * moveSpeed * Time.deltaTime;
                }
            }

            pos.x = Mathf.Clamp(pos.x, -halfWidth, halfWidth);
            cauldron.anchoredPosition = pos;
        }

        private void SpawnPotion()
        {
            if (spawnArea == null) return;

            GameObject pObj = new GameObject("FallingPotion", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(spawnArea, false);
            RectTransform pRT = pObj.GetComponent<RectTransform>();
            Image pImg = pObj.GetComponent<Image>();
            pImg.preserveAspect = true;

            pRT.sizeDelta = new Vector2(48, 48);

            float halfWidth = spawnArea.rect.width / 2f - 30f;
            float randomX = Random.Range(-halfWidth, halfWidth);
            float startY = spawnArea.rect.height / 2f;
            pRT.anchoredPosition = new Vector2(randomX, startY);

            // İksir Tipini Belirle
            PotionDropType type;
            float roll = Random.value;
            if (roll < 0.45f) type = PotionDropType.RegularHealth;
            else if (roll < 0.70f) type = PotionDropType.ManaArcane;
            else if (roll < 0.82f) type = PotionDropType.ToxicHazard;
            else if (roll < 0.90f) type = PotionDropType.StardustTime;
            else if (roll < 0.96f) type = PotionDropType.ChaosBomb;
            else type = PotionDropType.GoldenElixir;

            // Sprite ve Renk Ata
            switch (type)
            {
                case PotionDropType.RegularHealth:
                    pImg.sprite = healthPotionSprite;
                    pImg.color = healthPotionSprite ? Color.white : new Color(0.9f, 0.25f, 0.35f);
                    break;
                case PotionDropType.ManaArcane:
                    pImg.sprite = manaPotionSprite;
                    pImg.color = manaPotionSprite ? Color.white : new Color(0.25f, 0.6f, 0.95f);
                    break;
                case PotionDropType.StardustTime:
                    pImg.sprite = stardustPotionSprite;
                    pImg.color = stardustPotionSprite ? Color.white : new Color(0.2f, 0.9f, 0.9f);
                    break;
                case PotionDropType.GoldenElixir:
                    pImg.sprite = goldPotionSprite;
                    pImg.color = goldPotionSprite ? Color.white : new Color(1f, 0.85f, 0.2f);
                    pRT.sizeDelta = new Vector2(56, 56);
                    break;
                case PotionDropType.ToxicHazard:
                    pImg.sprite = toxicSprite;
                    pImg.color = toxicSprite ? Color.white : new Color(0.3f, 0.8f, 0.2f);
                    break;
                case PotionDropType.ChaosBomb:
                    pImg.sprite = bombSprite;
                    pImg.color = bombSprite ? Color.white : new Color(0.85f, 0.2f, 0.2f);
                    break;
            }

            activePotions.Add(new ActivePotion
            {
                rect = pRT,
                image = pImg,
                type = type,
                speedMultiplier = Random.Range(0.9f, 1.25f)
            });
        }

        private void UpdateFallingPotions()
        {
            if (cauldron == null || spawnArea == null) return;

            float bottomY = -spawnArea.rect.height / 2f - 40f;
            Vector2 cauldronPos = cauldron.anchoredPosition;
            float cauldronWidth = cauldron.rect.width * 0.8f;
            float cauldronTop = cauldronPos.y + cauldron.rect.height * 0.4f;
            float cauldronBottom = cauldronPos.y - cauldron.rect.height * 0.4f;

            for (int i = activePotions.Count - 1; i >= 0; i--)
            {
                var pot = activePotions[i];
                if (pot.rect == null) { activePotions.RemoveAt(i); continue; }

                pot.rect.anchoredPosition += Vector2.down * (currentFallSpeed * pot.speedMultiplier) * Time.deltaTime;

                // Çarpışma Kontrolü (AABB / Bounding Box)
                Vector2 pPos = pot.rect.anchoredPosition;
                bool isColliding = Mathf.Abs(pPos.x - cauldronPos.x) < (cauldronWidth / 2f) &&
                                   pPos.y <= cauldronTop && pPos.y >= cauldronBottom;

                if (isColliding)
                {
                    OnPotionCaught(pot.type);
                    Destroy(pot.rect.gameObject);
                    activePotions.RemoveAt(i);
                    StartCoroutine(CauldronCatchBounce());
                }
                else if (pPos.y < bottomY)
                {
                    // Ekran dışına düştü
                    Destroy(pot.rect.gameObject);
                    activePotions.RemoveAt(i);
                }
            }
        }

        private void OnPotionCaught(PotionDropType type)
        {
            int multiplier = isFrenzy ? 2 : 1;

            switch (type)
            {
                case PotionDropType.RegularHealth:
                    score += 10 * multiplier;
                    break;
                case PotionDropType.ManaArcane:
                    score += 20 * multiplier;
                    break;
                case PotionDropType.StardustTime:
                    score += 30 * multiplier;
                    timer = Mathf.Min(gameDuration, timer + 3f);
                    break;
                case PotionDropType.GoldenElixir:
                    score += 50 * multiplier;
                    goldFrenzyTimer = 6f; // 6 saniye altın çağı!
                    break;
                case PotionDropType.ToxicHazard:
                case PotionDropType.ChaosBomb:
                    lives--;
                    StartCoroutine(CauldronHurtFlash());
                    if (lives <= 0)
                    {
                        lives = 0;
                        EndGame(false, score);
                    }
                    break;
            }
        }

        private IEnumerator CauldronCatchBounce()
        {
            if (cauldron == null) yield break;
            Vector3 originalScale = Vector3.one;
            cauldron.localScale = new Vector3(1.15f, 0.85f, 1f);
            yield return new WaitForSeconds(0.08f);
            cauldron.localScale = originalScale;
        }

        private IEnumerator CauldronHurtFlash()
        {
            if (cauldron == null) yield break;
            Image cImg = cauldron.GetComponent<Image>();
            if (cImg == null) yield break;

            Color origColor = cImg.color;
            cImg.color = new Color(1f, 0.2f, 0.2f, 1f);
            yield return new WaitForSeconds(0.15f);
            cImg.color = origColor;
        }

        private void ClearPotions()
        {
            foreach (var p in activePotions)
            {
                if (p.rect != null) Destroy(p.rect.gameObject);
            }
            activePotions.Clear();
        }

        private void UpdateUI()
        {
            if (scoreText) scoreText.text = $"🧪 Skor: {score}";
            if (timerText)
            {
                timerText.text = $"⏳ {Mathf.CeilToInt(timer)}s";
                timerText.color = timer < 10f ? Color.red : Color.white;
            }
            if (livesText) livesText.text = $"Can: {lives}/3";

            // Kalp ikonlarını güncelle
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].color = (i < lives) ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.4f);
                }
            }

            if (frenzyText)
            {
                if (isFrenzy)
                {
                    frenzyText.text = $"⭐ ALTIN ÇAĞ (2X): {goldFrenzyTimer:F1}s ⭐";
                    frenzyText.gameObject.SetActive(true);
                }
                else
                {
                    frenzyText.gameObject.SetActive(false);
                }
            }
        }
    }
}
