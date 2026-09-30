using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace PotionShop
{
    public class Cauldron : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [Header("Kazan Durumu")]
        public List<ItemData> currentItems = new List<ItemData>();
        public bool isDirty = false;
        
        [Header("Özsu Sistemi")]
        public ItemData selectedEssence; // Seçilen Özsu
        private Coroutine _fillAnimationCoroutine;

        [Header("Tarifler")]
        public List<RecipeData> allRecipes;

        [Header("Referanslar")]
        public CauldronCleaningMinigame cleaningMinigame;
        public Animator cauldronAnimator; // Kaynama animasyonları için
        
        [Header("Görsel Ayarlar")]
        public UnityEngine.UI.Image cauldronImage; // Kazanın kendi Image bileşeni
        public Sprite emptyCauldronSprite; // Boş kazan resmi
        public Sprite fullCauldronSprite;  // Dolu kazan resmi (Varsayılan/İlk Dolum)
        public GameObject warningSign; // Kirlendiğinde çıkacak ünlem işareti vb.
        
        [Header("Dükkan ve Evre Ayarları")]
        [Tooltip("Bu kazan hazırlık evresinde (Oyun başlatma butonuna basılmadan önce) çalışabilir mi? (İksirYapmaDükkanı için true)")]
        public bool canBrewInPrepPhase = false;

        [Tooltip("Bu kazan sadece Öz (Essence) üretimi mi yapar? (İksirYapmaDükkanı için true)")]
        public bool onlyEssenceCrafting = false;

        [Header("Lisans Uyarısı UI")]
        [Tooltip("Lisansı olmayan bir iksir yapılmaya çalışıldığında gösterilecek uyarı objesi")]
        public GameObject licenseWarningObj;
        [Tooltip("Uyarı metni bileşeni (Atanmazsa licenseWarningObj içinden otomatik aranır)")]
        public TMPro.TextMeshProUGUI licenseWarningText;
        private Coroutine _warningCoroutine;

        [Header("UI Referansları")]
        [Tooltip("Sadece malzeme atıldığında görünecek olan 'Kaynat' butonu objesi")]
        public GameObject brewButtonObj;

        [Header("Fırlatma (Eject) Ayarları")]
        [Tooltip("Üretilen iksirin belireceği obje/slot (İsteğe bağlı, Servis alanı için)")]
        public Transform craftedItemSpawnPoint;
        public GameObject draggableItemPrefab; // Üretilen iksiri yaratmak için prefab
        public Transform jumpTargetPoint; // İksirin fırlayıp düşeceği nokta (Tezgah üstü vs)
        
        [Tooltip("İksirin fırladığındaki boyutu (Genişlik ve Yükseklik)")]
        public Vector2 potionEjectSize = new Vector2(50f, 50f);
        [Tooltip("İksirin fırladığındaki 3D Ölçeği (Örn: 1,1,1 veya 0.5,0.5,0.5)")]
        public Vector3 potionEjectScale = new Vector3(2f, 2f, 2f);

        private void Awake()
        {
            SetupDropForwarders();
        }

        private void SetupDropForwarders()
        {
            if (brewButtonObj != null)
            {
                var forwarder = brewButtonObj.GetComponent<CauldronDropForwarder>();
                if (forwarder == null) forwarder = brewButtonObj.AddComponent<CauldronDropForwarder>();
                forwarder.targetCauldron = this;

                foreach (Transform child in brewButtonObj.transform)
                {
                    var childForwarder = child.GetComponent<CauldronDropForwarder>();
                    if (childForwarder == null) childForwarder = child.gameObject.AddComponent<CauldronDropForwarder>();
                    childForwarder.targetCauldron = this;
                }

                // Butonun içindeki metinlerin gereksiz raycast yakalamasını engelle
                foreach (var tmp in brewButtonObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    tmp.raycastTarget = false;
                }
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateBrewButtonText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCauldronBoiling(0f);
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            UpdateBrewButtonText();
        }

        public void UpdateBrewButtonText()
        {
            if (brewButtonObj != null)
            {
                var tmps = brewButtonObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    t.text = LocalizationManager.Get("cauldron_brew");
                    UIThemeHelper.ApplyNewRocker(t);
                }

                var legacyTexts = brewButtonObj.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                foreach (var lt in legacyTexts)
                {
                    lt.text = LocalizationManager.Get("cauldron_brew");
                }
            }
        }

        private void Start()
        {
            SetupDropForwarders();
            UpdateBrewButtonText();
            if (warningSign != null) warningSign.SetActive(false);
            if (licenseWarningObj != null) licenseWarningObj.SetActive(false);
            if (cauldronImage != null && emptyCauldronSprite != null) cauldronImage.sprite = emptyCauldronSprite;
            
            // Başlangıçta kazan boş olduğu için kaynat butonunu gizle
            if (brewButtonObj != null) brewButtonObj.SetActive(false);

            if (cauldronImage != null)
            {
                cauldronImage.raycastTarget = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDirty)
            {
                OpenCleaningMinigame();
            }
        }

        // --- YENİ: ÖZSU SEÇİMİ VE ANİMASYONU ---
        public void SetEssence(ItemData essence, Sprite colorSprite)
        {
            if (isDirty)
            {
                Debug.LogWarning("Kazan kirli! Özsu değiştirmeden önce temizle.");
                return;
            }

            selectedEssence = essence;

            if (cauldronImage != null && colorSprite != null)
            {
                // Yumuşak dolum animasyonunu başlat
                if (_fillAnimationCoroutine != null) StopCoroutine(_fillAnimationCoroutine);
                _fillAnimationCoroutine = StartCoroutine(AnimateLiquidFill(colorSprite));
            }

            Debug.Log($"Kazan özsuyu ayarlandı: {essence.itemName}");
        }

        private System.Collections.IEnumerator AnimateLiquidFill(Sprite colorSprite)
        {
            // Animasyon sırasında ana resmi "Boş Kazan" yapıyoruz ki kazanın metalleri kaybolmasın
            if (emptyCauldronSprite != null)
                cauldronImage.sprite = emptyCauldronSprite;
            
            // Sıvının yavaşça belirmesi için tam kazanın üstüne geçici bir "Dolu Kazan" resmi oluşturuyoruz
            GameObject tempObj = new GameObject("LiquidFillOverlay");
            tempObj.transform.SetParent(cauldronImage.transform, false);
            
            // Geçici resmi ana resimle birebir aynı boyuta getir
            RectTransform tempRect = tempObj.AddComponent<RectTransform>();
            tempRect.anchorMin = Vector2.zero;
            tempRect.anchorMax = Vector2.one;
            tempRect.offsetMin = Vector2.zero;
            tempRect.offsetMax = Vector2.zero;
            tempRect.localScale = Vector3.one;

            UnityEngine.UI.Image tempImg = tempObj.AddComponent<UnityEngine.UI.Image>();
            tempImg.sprite = colorSprite;
            tempImg.raycastTarget = false; // Tıklamaları engellemesin
            
            Color c = tempImg.color;
            tempImg.color = new Color(c.r, c.g, c.b, 0f); // Tamamen şeffaf başla

            float duration = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.cauldronFillDuration : 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Sadece dolu kazanın opaklığını yavaşça artır
                tempImg.color = new Color(c.r, c.g, c.b, t);
                yield return null;
            }

            // Animasyon pürüzsüzce bittiğinde, asıl resmi değiştir ve geçici kopyayı sil
            cauldronImage.sprite = colorSprite;
            Destroy(tempObj);
        }

        // Kazana eşya bırakıldığında tetiklenir
        public void OnDrop(PointerEventData eventData)
        {
            // Hazırlık evresindeyse ve bu dükkanda serbest çalışmaya izin verilmemişse engelle
            if (!canBrewInPrepPhase && GameManager.Instance != null && GameManager.Instance.isPrepPhase)
            {
                Debug.Log("Bu dükkanda hazırlık evresinde kazan kullanılamaz. Önce oyunu başlat!");
                return;
            }

            if (isDirty)
            {
                Debug.LogWarning("Kazan şu an kirli! Önce temizlemelisin.");
                return;
            }

            GameObject dropped = eventData != null ? eventData.pointerDrag : null;
            if (dropped == null) return;
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            
            if (draggableItem != null && draggableItem.itemData != null)
            {
                ItemData item = draggableItem.itemData;

                // Sadece Malzeme (Ingredient), Özsu (Essence), Parşömen (Scroll) veya Kadim Gölge Taşı (UniversalIngredient) kazana atılabilir!
                bool canAddToCauldron = item.itemType == ItemType.Ingredient ||
                                        item.itemType == ItemType.Essence ||
                                        item.itemType == ItemType.Scroll ||
                                        item.specialEffect == SpecialItemEffect.UniversalIngredient;

                if (!canAddToCauldron)
                {
                    Debug.LogWarning($"<color=orange>[Kazan]</color> {item.itemName} bir malzeme değil, kazana atılamaz!");
                    return;
                }

                // Envanterden 1 adet eksilt, başarısız olursa atılamasın
                if (PlayerInventory.Instance != null && !PlayerInventory.Instance.RemoveItem(item, 1))
                {
                    Debug.LogWarning("Envanterde yeterli eşya yok!");
                    return;
                }

                currentItems.Add(item);
                Debug.Log($"<color=cyan>[Kazan]</color> Kazana eklendi: {item.itemName} (Toplam kazandaki eşya: {currentItems.Count})");

                // Kazana malzeme eklendiği için Kaynat butonunu görünür yap ve drop ileticiyi garantile
                if (brewButtonObj != null)
                {
                    SetupDropForwarders();
                    UpdateBrewButtonText();
                    brewButtonObj.SetActive(true);
                }

                // Ses efektini çal
                if (AudioManager.Instance != null && AudioManager.Instance.cauldronDropItemSFX != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.cauldronDropItemSFX);
                }

                // Animasyonu başlat (Eğer eklenmişse)
                if (cauldronAnimator != null)
                {
                    cauldronAnimator.SetBool("isBoiling", true);
                }
            }
        }

        // UI Butonundan çağrılır (Kaynat Butonu)
        public void Brew()
        {
            if (!canBrewInPrepPhase && GameManager.Instance != null && GameManager.Instance.isPrepPhase)
            {
                Debug.Log("Bu dükkanda hazırlık evresinde kaynatma yapılamaz. Önce oyunu başlat!");
                return;
            }

            if (isDirty)
            {
                Debug.LogWarning("Kazan kirli olduğu için kaynatılamaz. Temizleme oyunu açılıyor...");
                OpenCleaningMinigame();
                return;
            }

            if (currentItems.Count == 0)
            {
                Debug.LogWarning("Kazan boş!");
                return;
            }

            if (selectedEssence == null)
            {
                Debug.LogWarning("Önce kazanın yanındaki butonlardan bir Özsu veya Parşömen seçmelisin!");
                return;
            }

            // 1) Envanterde seçili özsuyu/parşömen var mı kontrol et
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.GetItemCount(selectedEssence) <= 0)
            {
                Debug.LogWarning($"Envanterinde hiç {selectedEssence.itemName} kalmamış!");
                return; 
            }

            // 2) Tarife Özsuyunu dahil edip kontrol et (Öz henüz harcanmadan!)
            RecipeData matchedRecipe = CheckRecipe(selectedEssence);

            if (matchedRecipe != null)
            {
                // Kural: İksirYapmaDükkanı sadece Öz (Essence) üretebilir
                if (onlyEssenceCrafting && matchedRecipe.resultPotion != null && matchedRecipe.resultPotion.itemType != ItemType.Essence)
                {
                    Debug.LogWarning("[Kazan] Bu kazanda sadece Öz üretilebilir!");
                    ShowLicenseWarning(LocalizationManager.Get("cauldron_only_essence"));
                    return;
                }

                // Kural: İksirSatışDükkanı sadece İksir (Potion) üretebilir
                if (!onlyEssenceCrafting && matchedRecipe.resultPotion != null && matchedRecipe.resultPotion.itemType == ItemType.Essence)
                {
                    Debug.LogWarning("[Kazan] İksir Satış Dükkanı'nda sadece İksir üretilebilir!");
                    ShowLicenseWarning(LocalizationManager.Get("cauldron_only_potion"));
                    return;
                }

                // Lisans Kontrolü: Lisansı alınmamış iksirler üretilemez!
                if (!LicenseManager.HasLicense(matchedRecipe))
                {
                    Debug.LogWarning($"<color=red>[Kazan]</color> İksirin Lisansı Yok! ({matchedRecipe.resultPotion?.itemName ?? matchedRecipe.name})");
                    ShowLicenseWarning(LocalizationManager.Get("cauldron_no_license"));
                    return;
                }

                // Tüm kontroller başarılı -> Özsuyu/Parşömeni envanterden düş
                PlayerInventory.Instance.RemoveItem(selectedEssence, 1);
                Debug.Log($"{selectedEssence.itemName} harcandı.");

                // Başarılı Üretim
                Debug.Log("İksir Üretildi: " + matchedRecipe.resultPotion.itemName);

                if (AudioManager.Instance != null && AudioManager.Instance.cauldronBoilingSFX != null)
                {
                    AudioManager.Instance.PlayCauldronBoiling();
                }

                if (cauldronAnimator != null) cauldronAnimator.SetBool("isBoiling", false);

                // İksiri fırlat (Eject)
                if (draggableItemPrefab != null && craftedItemSpawnPoint != null)
                {
                    GameObject crafted = Instantiate(draggableItemPrefab, craftedItemSpawnPoint.parent);
                    crafted.transform.position = craftedItemSpawnPoint.position;
                    
                    // Inspector üzerinden ayarlanabilen ÖLÇEK (Scale) değeri
                    crafted.transform.localScale = potionEjectScale;

                    // Çapa ayarlarının resmi bozmasını engellemek için çapa değerlerini sıfırlayıp ortala
                    RectTransform rect = crafted.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        
                        // Inspector üzerinden ayarlanabilen BOYUT (Genişlik Yükseklik) değeri
                        rect.sizeDelta = potionEjectSize; 
                    }

                    // ŞEFFAFLIĞI GİDERMEK İÇİN KESİN ÇÖZÜM:
                    CanvasGroup canvasGroup = crafted.GetComponent<CanvasGroup>();
                    if (canvasGroup != null) canvasGroup.alpha = 1f;

                    UnityEngine.UI.Image img = crafted.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        Color fixedColor = img.color;
                        fixedColor.a = 1f;
                        img.color = fixedColor;
                    }

                    // ALTINDA ÇIKAN "NEW TEXT" YAZILARINI OTOMATİK GİZLE
                    TMPro.TextMeshProUGUI[] texts = crafted.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                    foreach(var t in texts) { t.gameObject.SetActive(false); }
                    
                    UnityEngine.UI.Text[] oldTexts = crafted.GetComponentsInChildren<UnityEngine.UI.Text>();
                    foreach(var t in oldTexts) { t.gameObject.SetActive(false); }

                    DraggableItem dragScript = crafted.GetComponent<DraggableItem>();
                    if (dragScript != null)
                    {
                        dragScript.itemData = matchedRecipe.resultPotion;
                        dragScript.UpdateVisuals();
                    }

                    // Fırlatma Animasyonunu Başlat
                    StartCoroutine(EjectPotionAnimation(crafted.transform));
                }
                else
                {
                    Debug.LogError("HATA: İksir üretildi ama fırlatılamadı! Lütfen Cauldron ayarlarına 'Draggable Item Prefab' ve 'Crafted Item Spawn Point' atadığınızdan emin olun!");
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopCauldronBoiling(0.15f);
                    }
                }

                ClearCauldron(true);
            }
            else
            {
                // Yanlış Üretim -> Özsu/Parşömen harcanır, kazan taşar / kirlenir
                PlayerInventory.Instance.RemoveItem(selectedEssence, 1);
                Debug.LogWarning("Yanlış Tarif! Kazan taştı ve kirlendi! (1 Özsu/Parşömen yandı)");
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopCauldronBoiling(0.1f);
                    if (AudioManager.Instance.cauldronErrorSFX != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.cauldronErrorSFX);
                    }
                }

                if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsChainBreakerActive)
                {
                    Debug.Log("<color=purple>[Zincir Kıran]</color> Kazan korundu! Yanlış tarif olsa bile kirlenmedi.");
                    isDirty = false;
                    ClearCauldron(true);
                }
                else
                {
                    isDirty = true;
                    if (warningSign != null) warningSign.SetActive(true); // Uyarı çıkar
                }

                if (cauldronAnimator != null) cauldronAnimator.SetBool("isBoiling", false);
            }
        }

        private System.Collections.IEnumerator EjectPotionAnimation(Transform potionTransform)
        {
            Vector3 startPos = potionTransform.position;
            
            // Eğer fırlatma hedefi atanmışsa onu kullan, yoksa rastgele sağa/sola kavis yap
            var config = GameBalanceConfig.Instance;
            Vector3 endPos = jumpTargetPoint != null 
                ? jumpTargetPoint.position 
                : startPos + new Vector3(
                    UnityEngine.Random.Range(
                        config != null ? config.potionEjectMinX : 100f,
                        config != null ? config.potionEjectMaxX : 200f
                    ) * (UnityEngine.Random.value > 0.5f ? 1f : -1f),
                    config != null ? config.potionEjectFallY : -150f, 0f);

            float duration = config != null ? config.potionEjectDuration : 0.5f;
            float elapsed = 0f;
            float jumpHeight = config != null ? config.potionJumpHeight : 150f; // Ne kadar yükseğe zıplayacak (piksel)

            while (elapsed < duration)
            {
                if (potionTransform == null) yield break;
                
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                float parabola = 4f * jumpHeight * t * (1f - t);
                currentPos.y += parabola;
                
                potionTransform.position = currentPos;
                yield return null;
            }

            if (potionTransform != null)
            {
                potionTransform.position = endPos;

                // Üretilen iksiri envantere ekle
                DraggableItem dragItem = potionTransform.GetComponent<DraggableItem>();
                if (dragItem != null && dragItem.itemData != null && PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.AddItem(dragItem.itemData, 1);
                    Debug.Log($"<color=green>[Kazan]</color> İksir envantere eklendi: {dragItem.itemData.itemName}");
                }

                // İksir üretildi ve envantere girdi -> Kaynama / kazan ses efekti burada bitmeli
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopCauldronBoiling(0.2f);
                }

                // Görsel olarak kısa süre görünüp kaybolur
                float landWait = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.potionLandWaitTime : 0.4f;
                yield return new WaitForSeconds(landWait);
                if (potionTransform != null)
                {
                    Destroy(potionTransform.gameObject);
                }
            }
            else
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopCauldronBoiling(0.15f);
                }
            }
        }

        private RecipeData CheckRecipe(ItemData usedEssence)
        {
            if (RecipeDatabase.Instance != null && (allRecipes == null || allRecipes.Count == 0))
            {
                allRecipes = new List<RecipeData>(RecipeDatabase.Instance.allRecipes);
            }

            // Kontrol edeceğimiz geçici liste (Kazan içindekiler + Harcanan Özsu)
            List<ItemData> itemsToCheck = new List<ItemData>(currentItems);
            itemsToCheck.Add(usedEssence);

            if (allRecipes == null || allRecipes.Count == 0) return null;

            // Kazandaki Joker Malzeme (UniversalIngredient) ve normal malzeme ayrımı
            int wildcardCount = currentItems.Count(i => i != null && i.specialEffect == SpecialItemEffect.UniversalIngredient);
            List<ItemData> normalIngredients = currentItems.Where(i => i != null && i.specialEffect != SpecialItemEffect.UniversalIngredient).ToList();

            List<RecipeData> matchingRecipes = new List<RecipeData>();

            foreach (var recipe in allRecipes)
            {
                if (recipe == null || recipe.requiredItems == null) continue;
                if (recipe.requiredItems.Count != itemsToCheck.Count) continue;

                // Tarif seçilen özsuyunu içeriyor mu?
                if (!recipe.requiredItems.Contains(usedEssence)) continue;

                // Tarifin özsuyu dışındaki gereken malzemelerini kopyala
                List<ItemData> remainingRecipeItems = new List<ItemData>(recipe.requiredItems);
                remainingRecipeItems.Remove(usedEssence);

                // Normal malzemeleri eşleştir ve çıkar
                bool normalMatch = true;
                foreach (var normalItem in normalIngredients)
                {
                    if (!remainingRecipeItems.Remove(normalItem))
                    {
                        normalMatch = false;
                        break;
                    }
                }

                if (!normalMatch) continue;

                // Kalan malzemeler tam olarak kazandaki joker malzeme (Kadim Gölge Taşı) adediyle karşılanabiliyor mu?
                if (remainingRecipeItems.Count == wildcardCount)
                {
                    matchingRecipes.Add(recipe);
                }
            }

            if (matchingRecipes.Count == 0) return null;

            // Öncelik Sıralaması:
            // 1. En az joker malzeme gerektiren (varsa tam eşleşme)
            // 2. Lisansı açık olan tarifler
            // 3. En yüksek tier ve unlockLevel
            return matchingRecipes
                .OrderBy(r => GetWildcardsNeeded(r, normalIngredients, usedEssence))
                .ThenByDescending(r => LicenseManager.HasLicense(r))
                .ThenByDescending(r => r.tier)
                .ThenByDescending(r => r.unlockLevel)
                .FirstOrDefault();
        }

        private int GetWildcardsNeeded(RecipeData recipe, List<ItemData> normalIngredients, ItemData usedEssence)
        {
            var req = new List<ItemData>(recipe.requiredItems);
            req.Remove(usedEssence);
            foreach (var norm in normalIngredients)
            {
                req.Remove(norm);
            }
            return req.Count;
        }

        public void ClearCauldron(bool keepEssence = false)
        {
            currentItems.Clear();
            isDirty = false;
            
            if (warningSign != null) warningSign.SetActive(false);
            if (brewButtonObj != null) brewButtonObj.SetActive(false); // Kazan boşaldı, butonu gizle

            // Eğer kazan tamamen boşaltılıyorsa (üretim dışı sıfırlama veya hata), kaynama sesini durdur
            if (!keepEssence && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCauldronBoiling(0.15f);
            }

            // Eğer kazan temizleniyorsa (hata veya manuel), özsuyu sıfırlansın mı?
            // "keepEssence" true ise, iksir üretildikten sonra kazan eski renginde kalmaya devam eder.
            if (!keepEssence)
            {
                selectedEssence = null;
                if (cauldronImage != null && emptyCauldronSprite != null) 
                {
                    cauldronImage.sprite = emptyCauldronSprite; 
                }
            }
        }
        
        public void CleanCauldron()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCauldronBoiling(0.1f);
            }
            ClearCauldron(false);
            Debug.Log("Kazan tertemiz oldu!");
        }

        // Uyarı butonuna, kazana veya kaynat butonuna basıldığında bu metod çağrılır
        public void OpenCleaningMinigame()
        {
            Debug.Log("[Cauldron] OpenCleaningMinigame çağrıldı! isDirty: " + isDirty);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopCauldronBoiling(0.1f);
            }

            if (warningSign != null) warningSign.SetActive(false); // Uyarıyı kapat

            if (cleaningMinigame == null)
            {
                cleaningMinigame = GetComponentInChildren<CauldronCleaningMinigame>(true)
                    ?? FindFirstObjectByType<CauldronCleaningMinigame>(FindObjectsInactive.Include);
            }

            if (cleaningMinigame != null)
            {
                isDirty = true; // Temizleme mini oyunu açıldı, kazan kirli işaretlenir
                Debug.Log("[Cauldron] Mini oyun paneli tetikleniyor...");
                cleaningMinigame.StartMinigame();
            }
            else
            {
                Debug.LogError("HATA: Cauldron objesindeki 'Cleaning Minigame' boş ve sahnede bulunamadı!");
            }
        }

        /// <summary>
        /// Lisanssız iksir üretilmeye çalışıldığında veya dükkan kurallarına aykırı üretimde uyarı gösterir.
        /// </summary>
        public void ShowLicenseWarning(string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = LocalizationManager.Get("cauldron_no_license");
            }
            else
            {
                message = LocalizationManager.Get(message);
            }

            if (licenseWarningObj == null)
            {
                Transform warningTr = transform.parent != null ? transform.parent.Find("LisansUyarisi") : transform.Find("LisansUyarisi");
                if (warningTr != null) licenseWarningObj = warningTr.gameObject;
            }

            if (licenseWarningObj != null)
            {
                if (licenseWarningText == null)
                {
                    licenseWarningText = licenseWarningObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                }

                if (licenseWarningText != null)
                {
                    licenseWarningText.text = message;
                }

                if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
                _warningCoroutine = StartCoroutine(AnimateLicenseWarning());
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Kazan Uyarısı]</color> {message}");
            }
        }

        private System.Collections.IEnumerator AnimateLicenseWarning()
        {
            if (licenseWarningObj == null) yield break;

            licenseWarningObj.SetActive(true);

            // Pop / punch animasyonu efekti (varsa CanvasGroup ile alpha, yoksa scale)
            Vector3 originalScale = licenseWarningObj.transform.localScale;
            licenseWarningObj.transform.localScale = originalScale * 0.85f;

            float punchTime = 0.15f;
            float elapsed = 0f;
            while (elapsed < punchTime)
            {
                if (licenseWarningObj == null) yield break;
                elapsed += Time.deltaTime;
                licenseWarningObj.transform.localScale = Vector3.Lerp(originalScale * 0.85f, originalScale * 1.08f, elapsed / punchTime);
                yield return null;
            }

            elapsed = 0f;
            float settleTime = 0.1f;
            while (elapsed < settleTime)
            {
                if (licenseWarningObj == null) yield break;
                elapsed += Time.deltaTime;
                licenseWarningObj.transform.localScale = Vector3.Lerp(originalScale * 1.08f, originalScale, elapsed / settleTime);
                yield return null;
            }

            licenseWarningObj.transform.localScale = originalScale;

            // 2 saniye görünür kalır
            yield return new WaitForSeconds(2.0f);

            // Yumuşakça kapanma
            if (licenseWarningObj != null)
            {
                CanvasGroup cg = licenseWarningObj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    float fadeDuration = 0.3f;
                    float fadeElapsed = 0f;
                    while (fadeElapsed < fadeDuration)
                    {
                        fadeElapsed += Time.deltaTime;
                        cg.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                        yield return null;
                    }
                    cg.alpha = 1f;
                }
                licenseWarningObj.SetActive(false);
            }
            _warningCoroutine = null;
        }
    }

    /// <summary>
    /// Kaynat butonu veya kazanın üstündeki UI objelerine bırakılan malzemeleri doğrudan kazana iletir.
    /// </summary>
    public class CauldronDropForwarder : MonoBehaviour, IDropHandler
    {
        public Cauldron targetCauldron;

        public void OnDrop(PointerEventData eventData)
        {
            if (targetCauldron == null)
            {
                targetCauldron = GetComponentInParent<Cauldron>();
            }

            if (targetCauldron != null)
            {
                targetCauldron.OnDrop(eventData);
            }
        }
    }
}
