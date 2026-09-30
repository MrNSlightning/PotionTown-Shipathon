using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace PotionShop
{
    public class Customer : MonoBehaviour, IDropHandler
    {
        public enum Mood
        {
            Mutlu = 1,      
            Memnun = 2,     
            Normal = 2,     
            Huysuz = 3,     
            Sinirli = 4,    
            Ofkeli = 4,     
            Gitti = 5       
        }

        [Header("Müşteri Durumu")]
        public List<ItemData> requestedPotions = new List<ItemData>(); // Çoklu sipariş
        public Mood currentMood = Mood.Mutlu;

        private bool _isAngryMusicPlaying = false; // Müzik değişimi kontrolü

        [Header("Kuyruk / Slot")]
        public int queueSlot = 0;
        public bool IsWalkingIn => isWalkingIn;
        public bool IsWalkingOut => isWalkingOut;

        [Header("Zamanlayıcı")]
        public float timePerMood = 10f; 
        private float timer = 0f;

        [Header("Animasyon (Yürüme)")]
        public float walkSpeed = 300f; 
        private bool isWalkingIn = true;
        private bool isWalkingOut = false;
        private bool _hasRecordedStat = false;
        private Vector2 targetPosition;
        private Vector2 offScreenPosition;
        private RectTransform rectT;
        private CustomerVisualAnimator visualAnimator;

        [Header("UI Referansları")]
        [Tooltip("İçinde yüz ifadesi ve iksir balonlarının olduğu ana obje (Örn: Canvas)")]
        public GameObject uiContainer;
        [Tooltip("Baloncuğun Y ekseninde ne kadar yukarıda duracağı (piksel). Prefab'ın orijinal yüksekliğini korumak için 0 bırakın.")]
        public float bubbleYOffset = 0f;
        public Image moodIcon;          
        public Image potionRequestIcon; // İlk/aktif iksir ikonu
        public Sprite[] moodSprites;    

        [Header("Çoklu Sipariş UI")]
        [Tooltip("Sipariş sayısını gösteren text (Ör: '2/3')")]
        public TextMeshProUGUI orderCountText;

        // Teslimat takibi
        private int _deliveredCount = 0;
        private int _totalOrders = 0;

        // Geriye dönük uyumluluk: Tek iksir atama
        public ItemData requestedPotion
        {
            get => requestedPotions.Count > _deliveredCount ? requestedPotions[_deliveredCount] : null;
            set
            {
                if (requestedPotions.Count == 0)
                    requestedPotions.Add(value);
                else
                    requestedPotions[0] = value;
            }
        }

        [Header("Animasyon Entegrasyonu")]
        [Tooltip("Harici karakterler (ör: HeroEditor4D) için Animator. Eğer yoksa boş bırakılabilir.")]
        public Animator characterAnimator;
        public string walkAnimationParam = "Walk";
        public string idleAnimationParam = "Idle";

        private MonoBehaviour character4DComponent;
        private float initialOffX;

        private void Awake()
        {
            rectT = GetComponent<RectTransform>();

            // Sadece halihazırda RectTransform kullanan (UI tabanlı) müşteriler için raycast kontrolü
            // 2D Sprite/Transform tabanlı müşterilere (HeroEditor4D) asla Image eklenmemelidir (ölçek ve render bozulur)
            if (rectT != null)
            {
                Image bg = GetComponent<Image>();
                if (bg == null)
                {
                    bg = gameObject.AddComponent<Image>();
                    bg.color = Color.clear;
                }
                bg.raycastTarget = true;
            }
        }

        private void Start()
        {
            if (rectT == null) rectT = GetComponent<RectTransform>();
            _totalOrders = requestedPotions.Count;
            _deliveredCount = 0;
            
            initialOffX = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.customerOffScreenX : 1000f;
            targetPosition = CustomerSpawner.Instance != null ? CustomerSpawner.Instance.GetSlotPosition(queueSlot) : Vector2.zero;
            offScreenPosition = new Vector2(initialOffX, targetPosition.y);

            if (rectT != null)
            {
                rectT.anchoredPosition = offScreenPosition;
            }
            else
            {
                // Fallback for non-UI 2D characters (like HeroEditor4D) - Tezgahın arkasında Z=2f
                transform.localPosition = new Vector3(offScreenPosition.x, offScreenPosition.y, 2f);
            }

            visualAnimator = GetComponent<CustomerVisualAnimator>();
            if (visualAnimator == null)
            {
                visualAnimator = gameObject.AddComponent<CustomerVisualAnimator>();
            }

            // 4D Karakter desteği
            character4DComponent = GetComponent("Character4D") as MonoBehaviour;

            // Baloncuk ve alt objelere bırakıldığında ana müşteriye iletilmesi için alıcıları bağla
            if (uiContainer != null)
            {
                var receiver = uiContainer.GetComponent<CustomerDropReceiver>();
                if (receiver == null) receiver = uiContainer.AddComponent<CustomerDropReceiver>();
                receiver.targetCustomer = this;

                // Baloncuğu Y ekseninde sadece özel bir ofset belirtilmişse kaydır (varsayılan 0 ise prefabın orijinal konumunu koru)
                if (bubbleYOffset != 0f)
                {
                    RectTransform uiRt = uiContainer.GetComponent<RectTransform>();
                    if (uiRt != null)
                    {
                        uiRt.anchoredPosition += new Vector2(0, bubbleYOffset);
                    }
                }
            }

            if (visualAnimator != null && visualAnimator.speechBubble != null)
            {
                var receiver = visualAnimator.speechBubble.GetComponent<CustomerDropReceiver>();
                if (receiver == null) receiver = visualAnimator.speechBubble.gameObject.AddComponent<CustomerDropReceiver>();
                receiver.targetCustomer = this;
            }

            if (uiContainer != null) uiContainer.SetActive(false);

            PlayWalkAnimation(true);
            SetCharacterDirection(initialOffX > 0 ? Vector2.left : Vector2.right);

            UpdatePotionRequestUI();
            UpdateMoodUI();
            UpdateOrderCountUI();
        }

        private void PlayWalkAnimation(bool isWalking)
        {
            if (visualAnimator != null)
            {
                visualAnimator.PlayWalk(isWalking);
            }

            if (characterAnimator != null)
            {
                // HeroEditor4D gibi State (int) parametresi kullananlar için (0=Idle, 2=Walk)
                bool hasStateParam = false;
                foreach (AnimatorControllerParameter p in characterAnimator.parameters)
                {
                    if (p.name == "State" && p.type == AnimatorControllerParameterType.Int)
                    {
                        characterAnimator.SetInteger("State", isWalking ? 2 : 0);
                        hasStateParam = true;
                        break;
                    }
                }

                // Yoksa standart isimle state oynatma denemesi
                if (!hasStateParam)
                {
                    string stateName = isWalking ? walkAnimationParam : idleAnimationParam;
                    characterAnimator.Play(stateName);
                }
            }
        }

        private void SetCharacterDirection(Vector2 dir)
        {
            // HeroEditor4D gibi 4 yönlü karakterler için reflection (Hata vermemesi için direkt referans kullanmıyoruz)
            if (character4DComponent != null)
            {
                var method = character4DComponent.GetType().GetMethod("SetDirection");
                if (method != null)
                {
                    method.Invoke(character4DComponent, new object[] { dir });
                }
            }
            else
            {
                // Eğer standart bir 2D karakterse, sadece X ekseninde çevir (Flip)
                if (dir == Vector2.left && transform.localScale.x < 0)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
                else if (dir == Vector2.right && transform.localScale.x > 0)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
        }

        private void Update()
        {
            // Rüzgarın Hızı İksiri aktifse yürüme hızı artar
            float effectiveWalkSpeed = walkSpeed;
            if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsRushHourActive)
            {
                effectiveWalkSpeed *= SpecialPotionManager.Instance.rushWalkSpeedMultiplier;
            }

            if (isWalkingIn)
            {
                if (rectT != null)
                {
                    rectT.anchoredPosition = Vector2.MoveTowards(rectT.anchoredPosition, targetPosition, effectiveWalkSpeed * Time.deltaTime);
                    if (Vector2.Distance(rectT.anchoredPosition, targetPosition) < 1f)
                    {
                        isWalkingIn = false;
                        PlayWalkAnimation(false); // Idle
                        SetCharacterDirection(Vector2.down); // Oyuncuya dön
                        if (uiContainer != null) uiContainer.SetActive(true);
                        OnArrived();
                    }
                }
                else
                {
                    Vector2 currentPos2D = new Vector2(transform.localPosition.x, transform.localPosition.y);
                    Vector2 nextPos2D = Vector2.MoveTowards(currentPos2D, targetPosition, effectiveWalkSpeed * Time.deltaTime);
                    transform.localPosition = new Vector3(nextPos2D.x, nextPos2D.y, 2f);
                    if (Vector2.Distance(nextPos2D, targetPosition) < 1f)
                    {
                        transform.localPosition = new Vector3(targetPosition.x, targetPosition.y, 2f);
                        isWalkingIn = false;
                        PlayWalkAnimation(false); // Idle
                        SetCharacterDirection(Vector2.down); // Oyuncuya dön
                        if (uiContainer != null)
                        {
                            uiContainer.SetActive(true);
                            UpdatePotionRequestUI();
                            UpdateMoodUI();
                            UpdateOrderCountUI();
                        }
                        OnArrived();
                    }
                }
                return;
            }

            if (isWalkingOut)
            {
                if (rectT != null)
                {
                    rectT.anchoredPosition = Vector2.MoveTowards(rectT.anchoredPosition, offScreenPosition, effectiveWalkSpeed * Time.deltaTime);
                    if (Vector2.Distance(rectT.anchoredPosition, offScreenPosition) < 1f)
                    {
                        if (CustomerSpawner.Instance != null) CustomerSpawner.Instance.OnCustomerCompleted();
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Vector2 currentPos2D = new Vector2(transform.localPosition.x, transform.localPosition.y);
                    Vector2 nextPos2D = Vector2.MoveTowards(currentPos2D, offScreenPosition, effectiveWalkSpeed * Time.deltaTime);
                    transform.localPosition = new Vector3(nextPos2D.x, nextPos2D.y, 2f);
                    if (Vector2.Distance(nextPos2D, offScreenPosition) < 1f)
                    {
                        if (CustomerSpawner.Instance != null) CustomerSpawner.Instance.OnCustomerCompleted();
                        Destroy(gameObject);
                    }
                }
                return;
            }

            if (currentMood == Mood.Gitti) return;

            // Sükunet Merhemi aktifse mod düşüş hızı yavaşlar
            float decayRate = (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsSlowMoodDecayActive)
                ? SpecialPotionManager.Instance.moodDecayRateMultiplier
                : 1f;

            timer += Time.deltaTime * decayRate;

            if (timer >= timePerMood)
            {
                timer = 0f;
                WorsenMood();
            }
        }

        private void OnArrived()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.customerArriveSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.customerArriveSFX);
            }
            UpdateMoodMusic();
        }

        private void UpdateMoodMusic()
        {
            if (AudioManager.Instance == null) return;

            bool isAngry = currentMood >= Mood.Huysuz;

            if (isAngry && !_isAngryMusicPlaying && AudioManager.Instance.angryMoodMusic != null)
            {
                // Müzik olarak değil, tek seferlik SFX olarak çal
                AudioManager.Instance.PlaySFX(AudioManager.Instance.angryMoodMusic);
                _isAngryMusicPlaying = true;
            }
            else if (!isAngry && _isAngryMusicPlaying && AudioManager.Instance.happyMoodMusic != null)
            {
                // Müzik olarak değil, tek seferlik SFX olarak çal
                AudioManager.Instance.PlaySFX(AudioManager.Instance.happyMoodMusic);
                _isAngryMusicPlaying = false;
            }
        }

        private void WorsenMood()
        {
            if (currentMood < Mood.Gitti)
            {
                currentMood++;
                UpdateMoodUI();
                UpdateMoodMusic();

                if (visualAnimator != null)
                {
                    visualAnimator.PlayAnger();
                }

                if (currentMood == Mood.Gitti)
                {
                    LeaveWithoutPaying();
                }
            }
        }

        public void SetMood(Mood newMood)
        {
            currentMood = newMood;
            timer = 0f;
            UpdateMoodUI();
            UpdateMoodMusic();
        }

        public void UpdateMoodUI()
        {
            int moodIndex = (int)currentMood - 1;
            if (moodIcon != null && moodSprites != null && moodSprites.Length > moodIndex && moodSprites[moodIndex] != null)
            {
                moodIcon.sprite = moodSprites[moodIndex];
            }
        }

        /// <summary>
        /// Aktif siparişin iksir ikonunu günceller.
        /// </summary>
        public void UpdatePotionRequestUI()
        {
            ItemData currentRequest = requestedPotion;
            if (currentRequest != null && potionRequestIcon != null)
            {
                potionRequestIcon.sprite = currentRequest.itemIcon;
                potionRequestIcon.enabled = true;
            }
            else if (potionRequestIcon != null)
            {
                potionRequestIcon.enabled = false;
            }
        }

        /// <summary>
        /// Sipariş sayacı UI'ını günceller (Ör: "1/3").
        /// </summary>
        public void UpdateOrderCountUI()
        {
            if (_totalOrders == 0 && requestedPotions != null)
            {
                _totalOrders = requestedPotions.Count;
            }

            if (orderCountText != null)
            {
                if (_totalOrders > 1)
                {
                    orderCountText.text = $"{_deliveredCount + 1}/{_totalOrders}";
                    orderCountText.gameObject.SetActive(true);
                }
                else
                {
                    orderCountText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// İstatistiği kaydeder.
        /// </summary>
        private void RecordStat()
        {
            if (!_hasRecordedStat && GameManager.Instance != null)
            {
                GameManager.Instance.RecordCustomerStat(currentMood);
                _hasRecordedStat = true;
            }
        }

        /// <summary>
        /// Müşteriye iksir teslim edildiğinde çağrılır.
        /// Çoklu siparişte: her doğru teslimat para kazandırır ve bonus süre ekler.
        /// Tüm siparişler tamamlanınca müşteri ayrılır.
        /// </summary>
        public bool ReceivePotion(ItemData deliveredPotion)
        {
            if (currentMood == Mood.Gitti || isWalkingIn || isWalkingOut) return false;

            ItemData currentRequest = requestedPotion;
            if (currentRequest == null) return false;

            // Sadece müşterinin istediği iksir veya tüm iksirlerin yerine geçen Uyum/Joker İksiri (UniversalPotion) kabul edilir!
            bool isUniversalPotion = deliveredPotion != null && deliveredPotion.specialEffect == SpecialItemEffect.UniversalPotion;

            // Çoklu sipariş esnekliği: Müşterinin sonraki siparişlerinde istenen bir iksir mi verildi?
            if (deliveredPotion != currentRequest && !isUniversalPotion && requestedPotions != null)
            {
                for (int i = _deliveredCount + 1; i < requestedPotions.Count; i++)
                {
                    if (requestedPotions[i] == deliveredPotion)
                    {
                        ItemData temp = requestedPotions[_deliveredCount];
                        requestedPotions[_deliveredCount] = requestedPotions[i];
                        requestedPotions[i] = temp;
                        currentRequest = requestedPotions[_deliveredCount];
                        break;
                    }
                }
            }

            if (deliveredPotion == currentRequest || isUniversalPotion)
            {
                if (visualAnimator != null)
                {
                    visualAnimator.PlayCelebration();
                }

                // Ödeme hesapla: Müşterinin talep ettiği iksirin değeri üzerinden ödenir
                int basePrice = currentRequest.basePrice;
                if (basePrice <= 0 && deliveredPotion != null && deliveredPotion.basePrice > 0)
                {
                    basePrice = deliveredPotion.basePrice;
                }

                int finalPayment = CalculatePayment(basePrice);
                if (finalPayment > 0 && GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(finalPayment);
                    CurrencyFeedbackUI.ShowGain(finalPayment, transform);
                    GameManager.Instance.RecordDailyEarning(finalPayment);
                    int repGain = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.reputationOnCorrectPotion : 5;
                    GameManager.Instance.ModifyReputation(repGain);
                }

                // XP kazandır
                // Removed XP logic

                // Kadim Para kontrolü
                if (GameManager.Instance != null)
                {
                    // Zincir Kıran bonusu: Her başarılı satışta +1 Kadim Para!
                    if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsChainBreakerActive)
                    {
                        GameManager.Instance.AddKadimPara(1);
                        CurrencyFeedbackUI.ShowGain(1, transform, isKadim: true);
                        Debug.Log("<color=purple>[Zincir Kıran]</color> Bonus Kadim Para kazanıldı: +1");
                    }

                    if (RecipeDatabase.Instance != null)
                    {
                        var recipes = RecipeDatabase.Instance.GetRecipesForResult(currentRequest);
                        foreach (var recipe in recipes)
                        {
                            if (recipe.kadimParaReward > 0)
                            {
                                GameManager.Instance.AddKadimPara(recipe.kadimParaReward);
                                CurrencyFeedbackUI.ShowGain(recipe.kadimParaReward, transform, isKadim: true);
                                Debug.Log($"Kadim Para kazanıldı: +{recipe.kadimParaReward}");
                                break; // İlk eşleşen tariften al
                            }
                        }
                    }
                }

                if (isUniversalPotion)
                {
                    Debug.Log($"<color=magenta>[Özel İksir]</color> Müşteriye Kozmik Uyum İksiri verildi! {finalPayment} altın kazanıldı. [{_deliveredCount + 1}/{_totalOrders}]");
                }
                else
                {
                    Debug.Log($"Müşteriye doğru iksir verildi. {finalPayment} altın kazanıldı. (Durum: {currentMood}) [{_deliveredCount + 1}/{_totalOrders}]");
                }

                _deliveredCount++;

                // Tüm siparişler tamamlandı mı?
                if (_deliveredCount >= _totalOrders)
                {
                    // Tüm siparişler bitti, müşteri mutlu ayrılır
                    if (AudioManager.Instance != null && AudioManager.Instance.happyMoodMusic != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.happyMoodMusic);
                    }

                    RecordStat();
                    isWalkingOut = true;
                    PlayWalkAnimation(true);
                    SetCharacterDirection(initialOffX > 0 ? Vector2.right : Vector2.left);
                    if (uiContainer != null) uiContainer.SetActive(false);
                    return true;
                }
                else
                {
                    // Daha sipariş var — bonus süre ekle ve sonraki iksiri göster
                    int currentLevel = LevelSystem.CurrentLevel;
                    var config = LevelDesignDatabase.Instance != null ? LevelDesignDatabase.Instance.GetLevelConfig(currentLevel) : null;
                    float bonusTime = config != null ? config.deliveryBonusTime : 10f;
                    timer = Mathf.Max(0f, timer - bonusTime); // Zamanlayıcıyı geri al

                    // Mood'u bir kademe iyileştir (bonus olarak)
                    if (currentMood > Mood.Mutlu)
                    {
                        currentMood--;
                        UpdateMoodUI();
                        UpdateMoodMusic();
                    }

                    // Sonraki siparişin ikonunu göster
                    UpdatePotionRequestUI();
                    UpdateOrderCountUI();

                    Debug.Log($"Sipariş teslim edildi! Kalan: {_totalOrders - _deliveredCount}. Bonus süre: +{bonusTime}s");
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("Müşteri bu iksiri istemiyor!");
                return false;
            }
        }

        private int CalculatePayment(int basePrice)
        {
            float finalAmount = basePrice;
            var config = GameBalanceConfig.Instance;
            
            switch (currentMood)
            {
                case Mood.Mutlu:
                    finalAmount += config != null ? config.moodBonusHappy : 5;
                    break;
                case Mood.Memnun:
                    finalAmount += config != null ? config.moodBonusNormal : 0;
                    break;
                case Mood.Huysuz:
                    finalAmount += config != null ? config.moodBonusGrumpy : -5;
                    break;
                case Mood.Sinirli:
                    finalAmount += config != null ? config.moodBonusAngry : -10;
                    break;
                case Mood.Gitti:
                    finalAmount = 0;
                    break;
            }
            
            // 2. İtibar (Reputation) Çarpanı
            if (GameManager.Instance != null)
            {
                int repBase = config != null ? config.reputationBase : 100;
                float reputationMultiplier = GameManager.Instance.CurrentReputation / (float)repBase;
                finalAmount *= reputationMultiplier;
            }

            // 3. Coşku Toniği (2x Altın) Çarpanı
            if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsDoubleGoldActive)
            {
                finalAmount *= SpecialPotionManager.Instance.goldMultiplier;
            }

            // 4. Seviyeye Özel Ekonomi Çarpanı
            if (LevelDesignDatabase.Instance != null)
            {
                var levelConfig = LevelDesignDatabase.Instance.GetLevelConfig(LevelSystem.CurrentLevel);
                if (levelConfig != null)
                {
                    finalAmount *= levelConfig.potionPriceMultiplier;
                }
            }

            return Mathf.Max(0, Mathf.RoundToInt(finalAmount));
        }

        private int CalculateLostEarnings()
        {
            int lost = 0;
            if (requestedPotions != null && requestedPotions.Count > _deliveredCount)
            {
                for (int i = _deliveredCount; i < requestedPotions.Count; i++)
                {
                    if (requestedPotions[i] != null)
                        lost += requestedPotions[i].basePrice;
                }
            }
            if (lost == 0 && requestedPotion != null)
            {
                lost = requestedPotion.basePrice;
            }
            if (lost == 0)
            {
                lost = 20; // Varsayılan asgari sipariş kaybı
            }
            return lost;
        }

        private void LeaveWithoutPaying()
        {
            Debug.Log("Müşteri çok bekledi ve sinirlenip gitti! İtibar düştü.");
            if (GameManager.Instance != null)
            {
                // Zincir Kıran aktifken itibar kaybı engellenir
                if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsChainBreakerActive)
                {
                    Debug.Log("<color=purple>[Zincir Kıran]</color> İtibar kalkanı korudu, ceza puanı düşmedi!");
                }
                else
                {
                    int repLoss = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.reputationOnCustomerLeft : -20;
                    GameManager.Instance.ModifyReputation(repLoss); // Çok bekletme cezası
                }
                GameManager.Instance.RecordDailyLoss(CalculateLostEarnings());
            }

            if (AudioManager.Instance != null && AudioManager.Instance.angryMoodMusic != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.angryMoodMusic);
            }

            RecordStat();
            isWalkingOut = true;
            PlayWalkAnimation(true);
            SetCharacterDirection(initialOffX > 0 ? Vector2.right : Vector2.left);
            if (uiContainer != null) uiContainer.SetActive(false);
        }

        public void RejectCustomer()
        {
            if (isWalkingIn || isWalkingOut || currentMood == Mood.Gitti) return;

            Debug.Log("Müşteri reddedildi. İtibar ciddi şekilde düştü!");
            if (GameManager.Instance != null)
            {
                // Zincir Kıran aktifken itibar kaybı engellenir
                if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsChainBreakerActive)
                {
                    Debug.Log("<color=purple>[Zincir Kıran]</color> İtibar kalkanı korudu, ceza puanı düşmedi!");
                }
                else
                {
                    int repLoss = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.reputationOnCustomerRejected : -15;
                    GameManager.Instance.ModifyReputation(repLoss); // Yok çekme cezası
                }
                GameManager.Instance.RecordDailyLoss(CalculateLostEarnings());
            }

            if (AudioManager.Instance != null && AudioManager.Instance.angryMoodMusic != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.angryMoodMusic);
            }

            currentMood = Mood.Gitti;
            RecordStat();
            isWalkingOut = true;
            PlayWalkAnimation(true);
            SetCharacterDirection(initialOffX > 0 ? Vector2.right : Vector2.left);
            if (uiContainer != null) uiContainer.SetActive(false);
        }

        /// <summary>
        /// Envanter veya tam ekran paneller açıldığında müşterinin görsel render bileşenlerini gizler/gösterir.
        /// </summary>
        public void SetVisible(bool visible)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = visible;
            }

            if (uiContainer != null)
            {
                if (!isWalkingOut || !visible)
                {
                    uiContainer.SetActive(visible);
                }
            }

            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
            }
        }

        /// <summary>
        /// Oyuncu iksiri doğrudan bu müşterinin üzerine sürükleyip bıraktığında tetiklenir.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            // Hazırlık evresindeyse servis yapılamaz
            if (GameManager.Instance != null && GameManager.Instance.isPrepPhase)
            {
                Debug.Log("Hazırlık evresinde servis yapılamaz.");
                return;
            }

            if (currentMood == Mood.Gitti || isWalkingIn || isWalkingOut)
            {
                Debug.LogWarning("Müşteri şu an sipariş alamıyor!");
                return;
            }

            GameObject dropped = eventData.pointerDrag;
            if (dropped == null) return;

            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            if (draggableItem == null || draggableItem.itemData == null || draggableItem.wasDeliveredThisDrag)
            {
                return;
            }

            ItemData item = draggableItem.itemData;

            // Bu müşteriye teslim etmeyi dene
            bool wasAccepted = ReceivePotion(item);

            if (wasAccepted)
            {
                draggableItem.wasDeliveredThisDrag = true;
                Debug.Log($"<color=green>[Müşteri Servisi Başarılı]</color> {item.itemName} teslim alındı. Müşteri: {gameObject.name}");

                // Envanterden 1 adet düş
                if (PlayerInventory.Instance != null)
                {
                    bool removed = PlayerInventory.Instance.RemoveItem(item, 1);
                    Debug.Log($"[Envanter] {item.itemName} -1 eksiltildi (Sonuç: {removed})");
                }

                // Raftan geliyorsa ASLA yok etme! Eski yerine dönsün ve sayacı güncellensin
                if (draggableItem.isFromShelf)
                {
                    draggableItem.transform.SetParent(draggableItem.parentAfterDrag);
                    RectTransform rt = draggableItem.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = Vector2.zero;

                    CanvasGroup cg = draggableItem.GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = true;

                    ShelfSlot slot = draggableItem.GetComponentInParent<ShelfSlot>();
                    if (slot == null && draggableItem.parentAfterDrag != null)
                    {
                        slot = draggableItem.parentAfterDrag.GetComponentInParent<ShelfSlot>();
                    }

                    if (slot != null)
                    {
                        slot.UpdateSlotState();
                    }
                }
                else
                {
                    // Bağımsız bir sahne nesnesiyse yok et
                    Destroy(dropped);
                }
            }
            else
            {
                Debug.LogWarning($"Müşteri ({gameObject.name}) bu iksiri ({item.itemName}) istemiyor!");
            }
        }
    }

    /// <summary>
    /// Müşterinin çocuk nesnelerine (baloncuk, ikon vs.) bırakılan iksirleri ana Müşteriye iletir.
    /// </summary>
    public class CustomerDropReceiver : MonoBehaviour, IDropHandler
    {
        public Customer targetCustomer;

        public void OnDrop(PointerEventData eventData)
        {
            if (targetCustomer != null)
            {
                targetCustomer.OnDrop(eventData);
            }
            else
            {
                Customer parent = GetComponentInParent<Customer>();
                if (parent != null) parent.OnDrop(eventData);
            }
        }
    }
}
