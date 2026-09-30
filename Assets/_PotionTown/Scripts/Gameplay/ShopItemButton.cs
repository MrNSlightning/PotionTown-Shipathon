using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Ürün satın alım alanındaki eşya butonlarına veya kartlarına eklenir.
    /// ItemData atandığı anda eşyanın görselini, ismini, türünü ve fiyatını 
    /// otomatik olarak UI bileşenlerine yansıtır.
    /// Tıklandığında ise altını düşüp eşyayı envantere ekler.
    /// </summary>
    [SelectionBase]
    public class ShopItemButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Satın Alınacak Eşya")]
        [Tooltip("Tüm bilgilerin (ikon, isim, fiyat, tür) çekileceği ItemData")]
        public ItemData itemData;

        [Tooltip("Her tıklamada satın alınacak adet")]
        [Min(1)]
        public int amountToBuy = 1;

        [Header("Fiyatlandırma (Otomatik / Özel)")]
        [Tooltip("İşaretlenirse ItemData'daki fiyat yerine aşağıdaki özel fiyat kullanılır. Kapalıysa fiyat doğrudan ItemData'dan (buyPrice veya basePrice) otomatik alınır.")]
        public bool useCustomPrice = false;

        [Tooltip("Sadece 'Use Custom Price' işaretliyse geçerlidir.")]
        public int customPrice = 10;

        [Header("UI Referansları (Otomatik Bulunur)")]
        [Tooltip("Eşya görselinin atanacağı UI Image (Boş bırakılırsa çocuk objelerden otomatik bulunur)")]
        public Image iconImage;

        [Tooltip("Eşya adının yazılacağı Text (Boş bırakılırsa otomatik bulunur)")]
        public TextMeshProUGUI nameText;

        [Tooltip("Eşya fiyatının yazılacağı Text (Boş bırakılırsa otomatik bulunur)")]
        public TextMeshProUGUI priceText;

        [Tooltip("Eşya türünün yazılacağı Text (Örn: Özsu, Malzeme, İksir - Opsiyonel)")]
        public TextMeshProUGUI typeText;

        [Tooltip("Oyuncunun envanterindeki mevcut adedi gösteren Text (Opsiyonel)")]
        public TextMeshProUGUI ownedCountText;

        [Tooltip("Satın alma butonu (Boş bırakılırsa bu objedeki veya çocuk objelerdeki Button otomatik bulunur)")]
        public Button buyButton;

        [Tooltip("Eğer 2D sahne objesiyse görsel için SpriteRenderer (Opsiyonel)")]
        public SpriteRenderer spriteRenderer;

        [Tooltip("Fiyatın yanındaki para/elmas simgesi UI Image (Boş bırakılırsa 'Coin', 'Para', 'PriceIcon' gibi alt objelerden otomatik bulunur)")]
        public Image currencyIcon;

        [Header("Para Birimi Simgeleri (Boşsa otomatik yüklenir)")]
        [Tooltip("Altın para simgesi")]
        public Sprite goldCoinSprite;

        [Tooltip("Kadim para / Elmas simgesi")]
        public Sprite kadimParaSprite;

        [Header("Metin Formatları")]
        [Tooltip("Fiyat formatı. {0} yerine fiyat gelir.")]
        public string priceFormat = "{0}";

        [Tooltip("Envanterdeki adet formatı. {0} yerine miktar gelir.")]
        public string ownedCountFormat = "Envanter: {0}";

        [Header("Lisans Gerekli Görünüm Ayarları (Hiyerarşi / Inspector)")]
        [Tooltip("Lisansı olmayan eşyalarda gösterilecek ayrı metin nesnesi (Hiyerarşideki 'LicenseRequired' objesi; boşsa priceText kullanılır)")]
        public TextMeshProUGUI licenseRequiredText;

        [Tooltip("Lisansı olmayan eşyalarda 'Lisans Gerekli' yazısının font boyutu")]
        public float licenseRequiredFontSize = 30f;

        [Tooltip("Lisansı olmayan eşyalarda metin satır aralığı")]
        public float licenseRequiredLineSpacing = -15f;

        [Tooltip("Lisansı olmayan eşyalarda metin kutusu boyutu (Genişlik, Yükseklik)")]
        public Vector2 licenseRequiredSizeDelta = new Vector2(100f, 48f);

        [Tooltip("Lisansı olmayan eşyalarda gösterilecek metin")]
        public string licenseRequiredTextFormat = "Lisans\nGerekli";

        [Tooltip("Lisansı olmayan eşyalarda metin rengi")]
        public Color licenseRequiredColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Yetersiz Altın Durumu")]
        [Tooltip("Yeterli altın yoksa butonu devre dışı (tıklanamaz) bırak")]
        public bool disableButtonIfCannotAfford = false;

        [Tooltip("Yeterli altın yoksa fiyat metninin rengini değiştir")]
        public bool highlightIfCannotAfford = true;
        public Color normalPriceColor = Color.white;
        public Color notEnoughGoldColor = new Color(0.9f, 0.25f, 0.25f, 1f);

        [Header("Geri Bildirim / Mesaj (Opsiyonel)")]
        public TextMeshProUGUI feedbackText;
        public string successMessage = "Satın Alındı!";
        public string notEnoughGoldMessage = "Yetersiz Altın!";
        public float feedbackDuration = 1.5f;

        [Header("Ses Efektleri (Opsiyonel)")]
        public AudioSource audioSource;
        public AudioClip successSound;
        public AudioClip failSound;

        [Header("Olaylar / Events (Opsiyonel)")]
        public UnityEvent onPurchaseSuccess;
        public UnityEvent onPurchaseFailed;

        [Header("Buton Tıklama Ayarı (OnClick)")]
        [Tooltip("Açık ise kod Button bileşenine otomatik olarak 'BuyItem' dinleyicisi ekler. Inspector'da zaten bir OnClick atanmışsa asla çift ekleme yapmaz.")]
        public bool autoHookButtonOnClick = true;

        // Dahili durum
        private bool _isSubscribed = false;
        private Coroutine _feedbackCoroutine;
        private float _defaultPriceFontSize = -1f;

        #region Unity Döngüsü ve Editör Entegrasyonu
        private void Awake()
        {
            AutoFindUIReferences();

            if (autoHookButtonOnClick && buyButton != null && buyButton.onClick.GetPersistentEventCount() == 0)
            {
                buyButton.onClick.RemoveListener(BuyItem);
                buyButton.onClick.AddListener(BuyItem);
            }
        }

        private void Start()
        {
            SubscribeEvents();
            ApplyItemData();
            UpdateUI();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            ApplyItemData();
            UpdateUI();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            if (autoHookButtonOnClick && buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuyItem);
            }
            UnsubscribeEvents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (licenseRequiredFontSize <= 0f) licenseRequiredFontSize = 30f;
            if (licenseRequiredSizeDelta == Vector2.zero) licenseRequiredSizeDelta = new Vector2(100f, 48f);
            if (string.IsNullOrEmpty(licenseRequiredTextFormat)) licenseRequiredTextFormat = "Lisans\nGerekli";

            // Editörde eşya seçildiği anda UI referanslarını bul ve değerleri ekrana yaz
            AutoFindUIReferences();
            if (itemData != null)
            {
                ApplyItemData();
            }
        }

        private void Reset()
        {
            AutoFindUIReferences();
            if (itemData != null)
            {
                ApplyItemData();
            }
        }
#endif
        #endregion

        #region Otomatik UI Eşleştirme (Auto-Find)
        /// <summary>
        /// Sürükle-bırak yapmanıza gerek kalmadan bu objedeki ve alt objelerdeki
        /// Image, TextMeshProUGUI ve Button bileşenlerini isimlerine göre otomatik bulur.
        /// </summary>
        [ContextMenu("UI Referanslarını Otomatik Bul")]
        public void AutoFindUIReferences()
        {
            // 1. Button bul
            if (buyButton == null)
            {
                buyButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
            }

            // 2. AudioSource bul
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>(true) ?? GetComponentInParent<AudioSource>();
            }

            // 3. SpriteRenderer bul
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
            }

            // 4. UI Image (İkon) bul
            if (iconImage == null)
            {
                Image[] allImages = GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("icon") || n.Contains("item") || n.Contains("gorsel") || n.Contains("görsel") || n.Contains("resim") || n.Contains("nesne"))
                    {
                        iconImage = img;
                        break;
                    }
                }

                // İsimden bulunamadıysa arka plan olmayan ilk alt Image'ı seç
                if (iconImage == null && allImages.Length > 0)
                {
                    foreach (var img in allImages)
                    {
                        string n = img.gameObject.name.ToLower();
                        if (img.gameObject != gameObject && !n.Contains("bg") && !n.Contains("background") && !n.Contains("panel") && !n.Contains("frame"))
                        {
                            iconImage = img;
                            break;
                        }
                    }
                    if (iconImage == null) iconImage = allImages[0];
                }
            }

            // 5. TextMeshProUGUI bileşenlerini bul
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                string n = txt.gameObject.name.ToLower();

                if (nameText == null && (n.Contains("name") || n.Contains("isim") || n.Contains("ad") || n.Contains("title") || n.Contains("baslik")))
                {
                    nameText = txt;
                }
                else if (priceText == null && (n.Contains("price") || n.Contains("fiyat") || n.Contains("cost") || n.Contains("altin") || n.Contains("gold") || n.Contains("para") || n.Contains("ucret")))
                {
                    priceText = txt;
                }
                else if (typeText == null && (n.Contains("type") || n.Contains("tur") || n.Contains("tür") || n.Contains("kategori")))
                {
                    typeText = txt;
                }
                else if (ownedCountText == null && (n.Contains("count") || n.Contains("owned") || n.Contains("adet") || n.Contains("envanter") || n.Contains("stok") || n.Contains("stock")))
                {
                    ownedCountText = txt;
                }
                else if (feedbackText == null && (n.Contains("feedback") || n.Contains("uyari") || n.Contains("uyarı") || n.Contains("mesaj") || n.Contains("warning")))
                {
                    feedbackText = txt;
                }
            }

            // 6. CurrencyIcon (Fiyatın yanındaki coin / elmas simgesi) bul
            if (currencyIcon == null)
            {
                Image[] allImages = GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img == iconImage) continue;
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("coin") || n.Contains("para") || n.Contains("kadim") || n.Contains("elmas") || n.Contains("gold") || n.Contains("currency") || n.Contains("priceicon"))
                    {
                        currencyIcon = img;
                        break;
                    }
                }
            }

            // 7. LicenseRequiredText (Lisans Gerekli metni) bul
            if (licenseRequiredText == null)
            {
                Transform lrt = transform.Find("PriceTag/LicenseRequired") ??
                                transform.Find("PriceTag/LisansGerekli") ??
                                transform.Find("LicenseRequired") ??
                                transform.Find("LisansGerekli");
                if (lrt != null) licenseRequiredText = lrt.GetComponent<TextMeshProUGUI>();
            }
        }
        #endregion

        #region ItemData'dan Değerleri Otomatik Çekme
        /// <summary>
        /// Eşyanın görselini (ikonunu), ismini, fiyatını ve türünü ItemData'dan
        /// otomatik alarak UI bileşenlerine atar.
        /// </summary>
        [ContextMenu("Eşya Bilgilerini Şimdi Yenile (Refresh)")]
        public void ApplyItemData()
        {
            if (itemData == null) return;

            // 1. Görsel (UI Image veya SpriteRenderer)
            if (iconImage != null && itemData.itemIcon != null)
            {
                iconImage.sprite = itemData.itemIcon;
                iconImage.enabled = true;
                iconImage.preserveAspect = true; // Boyut bozulmasını önle
            }

            if (spriteRenderer != null && itemData.itemIcon != null)
            {
                spriteRenderer.sprite = itemData.itemIcon;
                spriteRenderer.enabled = true;
            }

            // 2. İsim
            if (nameText != null)
            {
                nameText.text = itemData.LocalizedName;
            }

            // 3. Tür (Özsu, Malzeme, İksir)
            if (typeText != null)
            {
                typeText.text = itemData.TypeDisplayName;
            }

            // 4. Fiyat
            if (priceText != null)
            {
                bool isKadim = itemData.kadimParaPrice > 0;
                int totalPrice = isKadim ? (itemData.kadimParaPrice * Mathf.Max(1, amountToBuy)) : GetTotalPrice();
                priceText.text = string.Format(priceFormat, totalPrice);
            }

            // 5. Para Birimi Görseli (Coin / Kadim Para İkonu)
            EnsureCurrencySpritesLoaded();
            if (currencyIcon != null)
            {
                bool isKadim = itemData.kadimParaPrice > 0;
                Sprite targetSprite = isKadim ? kadimParaSprite : goldCoinSprite;
                if (targetSprite != null)
                {
                    currencyIcon.sprite = targetSprite;
                    currencyIcon.preserveAspect = true;
                    currencyIcon.gameObject.SetActive(true);
                }
            }

            // 6. Envanterdeki mevcut adet (Oyun çalışırken güncellenir)
            if (Application.isPlaying && ownedCountText != null && PlayerInventory.Instance != null)
            {
                int count = PlayerInventory.Instance.GetItemCount(itemData);
                ownedCountText.text = string.Format(LocalizationManager.Get("inv_count_format"), count);
            }
        }



        #endregion

        #region Fiyat Hesaplama
        /// <summary>
        /// Birim ürün fiyatını belirler.
        /// Eğer useCustomPrice açıksa customPrice döner.
        /// Değilse ItemData'nın buyPrice (veya basePrice) değerini otomatik alır.
        /// </summary>
        public int GetUnitPrice()
        {
            if (useCustomPrice)
            {
                return customPrice;
            }

            if (itemData != null)
            {
                if (itemData.buyPrice > 0)
                    return itemData.buyPrice;

                if (itemData.basePrice > 0)
                    return itemData.basePrice;
            }

            return 0;
        }

        /// <summary>
        /// Satın alınacak adetle çarpılmış toplam ödenecek altın tutarı.
        /// </summary>
        public int GetTotalPrice()
        {
            return GetUnitPrice() * Mathf.Max(1, amountToBuy);
        }
        #endregion

        #region Satın Alma Mantığı
        /// <summary>
        /// Butona tıklandığında çağrılır. Kadim Para veya Altın düşer ve eşyayı envantere ekler.
        /// Eğer itemData.kadimParaPrice > 0 ise Kadim Para ile satın alınır.
        /// </summary>
        public void BuyItem()
        {
            if (itemData == null)
            {
                Debug.LogWarning($"[{gameObject.name}] ShopItemButton: Satın alınacak ItemData atanmamış!");
                return;
            }

            // LİSANS KONTROLÜ: Lisanssız malzeme satın alınamaz
            if (itemData != null && !LicenseManager.HasLicenseForIngredient(itemData))
            {
                Debug.LogWarning($"<color=yellow>[Dükkan]</color> Bu malzeme için lisans gerekli: {itemData.itemName}");
                PlaySound(failSound);
                ShowFeedback("Lisans Gerekli!", notEnoughGoldColor);
                return;
            }

            // ZAMAN KONTROLÜ İPTAL EDİLDİ - Artık saat sistemi yok

            if (GameManager.Instance == null)
            {
                Debug.LogError("ShopItemButton: Sahnede GameManager bulunamadı!");
                return;
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("ShopItemButton: Sahnede PlayerInventory bulunamadı!");
                return;
            }

            // Kadim Para ile mi yoksa Altın ile mi satın alınacak?
            bool usesKadimPara = itemData.kadimParaPrice > 0;

            if (usesKadimPara)
            {
                int kadimCost = itemData.kadimParaPrice * Mathf.Max(1, amountToBuy);
                
                if (GameManager.Instance.SpendKadimPara(kadimCost))
                {
                    CurrencyFeedbackUI.ShowLoss(kadimCost, transform, isKadim: true);
                    PlayerInventory.Instance.AddItem(itemData, amountToBuy);
                    Debug.Log($"<color=magenta>[Dükkan]</color> Kadim Para ile satın alındı: {amountToBuy}x {itemData.itemName} (-{kadimCost} Kadim Para). Kalan: {GameManager.Instance.CurrentKadimPara}");
                    
                    PlaySound(successSound);
                    ShowFeedback(LocalizationManager.Get("toast_purchased"), Color.green);
                    onPurchaseSuccess?.Invoke();
                    UpdateUI();
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[Dükkan]</color> Yetersiz Kadim Para! Gerekli: {kadimCost}, Mevcut: {GameManager.Instance.CurrentKadimPara}");
                    ToastNotificationUI.ShowWarning("err_not_enough_kadim", transform);
                    PlaySound(failSound);
                    ShowFeedback(LocalizationManager.Get("err_not_enough_kadim"), notEnoughGoldColor);
                    onPurchaseFailed?.Invoke();
                }
            }
            else
            {
                int totalPrice = GetTotalPrice();

                if (GameManager.Instance.SpendGold(totalPrice))
                {
                    CurrencyFeedbackUI.ShowLoss(totalPrice, transform);
                    PlayerInventory.Instance.AddItem(itemData, amountToBuy);
                    Debug.Log($"<color=green>[Dükkan]</color> Satın alındı: {amountToBuy}x {itemData.itemName} (-{totalPrice} Altın). Kalan Altın: {GameManager.Instance.CurrentGold}");

                    PlaySound(successSound);
                    ShowFeedback(LocalizationManager.Get("toast_purchased"), Color.green);
                    onPurchaseSuccess?.Invoke();
                    UpdateUI();
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[Dükkan]</color> Yetersiz Altın! Gerekli: {totalPrice}, Mevcut: {GameManager.Instance.CurrentGold}");
                    ToastNotificationUI.ShowWarning("err_not_enough_gold", transform);
                    PlaySound(failSound);
                    ShowFeedback(LocalizationManager.Get("err_not_enough_gold"), notEnoughGoldColor);
                    onPurchaseFailed?.Invoke();
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Eğer nesnede bir UI Button bileşeni varsa, tıklamayı Button.onClick yönetir (çift satın almayı önler).
            // Eğer UI Button bileşeni yoksa (SampleScene'deki eşyalar gibi doğrudan Image/Collider varsa),
            // tıklamada satın almayı doğrudan tetikle:
            if (buyButton == null)
            {
                BuyItem();
            }
        }
        #endregion

        #region UI Güncelleme (Çalışma Zamanı)
        public void UpdateUI()
        {
            if (itemData == null) return;

            bool usesKadimPara = itemData.kadimParaPrice > 0;
            bool canAfford = true;

            if (usesKadimPara)
            {
                int kadimCost = itemData.kadimParaPrice * Mathf.Max(1, amountToBuy);
                if (GameManager.Instance != null)
                {
                    canAfford = GameManager.Instance.CurrentKadimPara >= kadimCost;
                }

                if (priceText != null)
                {
                    priceText.text = (currencyIcon != null) ? kadimCost.ToString() : $"{kadimCost} Kadim Para";

                    if (highlightIfCannotAfford)
                    {
                        priceText.color = canAfford ? normalPriceColor : notEnoughGoldColor;
                    }
                }
            }
            else
            {
                int totalPrice = GetTotalPrice();
                if (GameManager.Instance != null)
                {
                    canAfford = GameManager.Instance.CurrentGold >= totalPrice;
                }

                if (priceText != null)
                {
                    priceText.text = (currencyIcon != null) ? totalPrice.ToString() : string.Format(priceFormat, totalPrice);

                    if (highlightIfCannotAfford)
                    {
                        priceText.color = canAfford ? normalPriceColor : notEnoughGoldColor;
                    }
                }
            }

            // Para birimi görselini ayarla
            EnsureCurrencySpritesLoaded();
            if (currencyIcon != null)
            {
                Sprite targetSprite = usesKadimPara ? kadimParaSprite : goldCoinSprite;
                if (targetSprite != null)
                {
                    currencyIcon.sprite = targetSprite;
                    currencyIcon.preserveAspect = true;
                }
            }

            bool isShopOpenTime = true;

            // Envanterdeki mevcut adet
            if (ownedCountText != null && PlayerInventory.Instance != null)
            {
                int count = PlayerInventory.Instance.GetItemCount(itemData);
                ownedCountText.text = string.Format(LocalizationManager.Get("inv_count_format"), count);
            }

            // LİSANS KONTROLÜ: Lisanssız malzemeler gri ve devre dışı
            bool hasLicense = itemData == null || LicenseManager.HasLicenseForIngredient(itemData);

            if (!hasLicense)
            {
                // Gri renk uygula (ShelfSlot ile aynı mantık)
                float lockedAlpha = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.lockedItemAlpha : 0.5f;
                if (iconImage != null)
                    iconImage.color = new Color(0.35f, 0.35f, 0.35f, lockedAlpha);

                if (buyButton != null)
                    buyButton.interactable = false;

                if (currencyIcon != null)
                    currencyIcon.gameObject.SetActive(false);

                string licenseReqStr = LocalizationManager.Get("license_required");

                // Lisans Gerekli metnini göster (Hiyerarşiden ve Inspector'dan ayarlanabilir)
                if (licenseRequiredText != null)
                {
                    if (priceText != null && priceText != licenseRequiredText)
                        priceText.gameObject.SetActive(false);

                    licenseRequiredText.gameObject.SetActive(true);
                    licenseRequiredText.text = licenseReqStr;
                    licenseRequiredText.alignment = TextAlignmentOptions.Center;
                    licenseRequiredText.lineSpacing = licenseRequiredLineSpacing;
                    licenseRequiredText.fontSize = licenseRequiredFontSize > 0f ? licenseRequiredFontSize : 30f;
                    licenseRequiredText.enableWordWrapping = false;
                    licenseRequiredText.overflowMode = TextOverflowModes.Overflow;
                    licenseRequiredText.rectTransform.sizeDelta = licenseRequiredSizeDelta;
                    licenseRequiredText.color = licenseRequiredColor;
                }
                else if (priceText != null)
                {
                    if (_defaultPriceFontSize <= 0f) _defaultPriceFontSize = priceText.fontSize;
                    priceText.gameObject.SetActive(true);
                    priceText.text = licenseReqStr;
                    priceText.alignment = TextAlignmentOptions.Center;
                    priceText.lineSpacing = licenseRequiredLineSpacing;
                    priceText.fontSize = licenseRequiredFontSize > 0f ? licenseRequiredFontSize : 30f;
                    priceText.enableWordWrapping = false;
                    priceText.overflowMode = TextOverflowModes.Overflow;
                    priceText.rectTransform.sizeDelta = licenseRequiredSizeDelta;
                    priceText.color = licenseRequiredColor;
                }
            }
            else
            {
                // Lisans var: normal görünüm
                if (iconImage != null && itemData.itemIcon != null)
                    iconImage.color = Color.white;

                if (currencyIcon != null)
                    currencyIcon.gameObject.SetActive(true);

                if (licenseRequiredText != null && licenseRequiredText != priceText)
                    licenseRequiredText.gameObject.SetActive(false);

                if (priceText != null)
                {
                    priceText.gameObject.SetActive(true);
                    if (_defaultPriceFontSize > 0f) priceText.fontSize = _defaultPriceFontSize;
                    priceText.lineSpacing = 0f;
                    priceText.alignment = (currencyIcon != null) ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                    priceText.rectTransform.sizeDelta = new Vector2(50f, 24f);
                }

                // Buton tıklanabilirlik durumu
                if (disableButtonIfCannotAfford && buyButton != null)
                {
                    buyButton.interactable = canAfford && isShopOpenTime;
                }
                else if (buyButton != null)
                {
                    buyButton.interactable = isShopOpenTime;
                }
            }
        }

        /// <summary>
        /// Kod ile dinamik olarak eşya atamak için kullanılır.
        /// </summary>
        public void SetItem(ItemData newItemData, int amount = 1)
        {
            itemData = newItemData;
            amountToBuy = Mathf.Max(1, amount);

            AutoFindUIReferences();
            ApplyItemData();
            UpdateUI();
        }
        #endregion

        #region Event Abonelikleri
        private void SubscribeEvents()
        {
            if (_isSubscribed) return;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged += HandleGoldChanged;
                GameManager.Instance.OnKadimParaChanged += HandleKadimParaChanged;
            }

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;

            LevelSystem.OnLevelUp += HandleLevelChanged;

            LicenseManager.OnLicenseChanged -= HandleLicenseChanged;
            LicenseManager.OnLicenseChanged += HandleLicenseChanged;

            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

            if (GameManager.Instance != null && PlayerInventory.Instance != null)
            {
                _isSubscribed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            LicenseManager.OnLicenseChanged -= HandleLicenseChanged;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (!_isSubscribed) return;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged -= HandleGoldChanged;
                GameManager.Instance.OnKadimParaChanged -= HandleKadimParaChanged;
            }

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;

            LevelSystem.OnLevelUp -= HandleLevelChanged;

            _isSubscribed = false;
        }

        private void HandleGoldChanged(int currentGold) => UpdateUI();
        private void HandleKadimParaChanged(int currentKadimPara) => UpdateUI();
        private void HandleInventoryChanged() => UpdateUI();
        private void HandleLevelChanged(int level) => UpdateUI();
        private void HandleLicenseChanged() => UpdateUI();
        private void HandleLanguageChanged(GameLanguage lang)
        {
            ApplyItemData();
            UpdateUI();
        }
        #endregion

        #region Geri Bildirim ve Ses Yardımcıları
        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }

        private void ShowFeedback(string message, Color textColor)
        {
            if (feedbackText == null) return;

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }

            _feedbackCoroutine = StartCoroutine(FeedbackRoutine(message, textColor));
        }

        private IEnumerator FeedbackRoutine(string message, Color textColor)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;
            feedbackText.color = textColor;

            yield return new WaitForSeconds(feedbackDuration);

            feedbackText.gameObject.SetActive(false);
            _feedbackCoroutine = null;
        }
        #endregion

        #region Para Birimi Görselleri Yükleyici
        public void EnsureCurrencySpritesLoaded()
        {
            if (goldCoinSprite == null)
            {
#if UNITY_EDITOR
                goldCoinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Gold Coin.png");
#endif
                if (goldCoinSprite == null)
                {
                    goldCoinSprite = Resources.Load<Sprite>("LicenseUI/Gold Coin") ?? Resources.Load<Sprite>("Gold Coin");
                }
            }

            if (kadimParaSprite == null)
            {
#if UNITY_EDITOR
                kadimParaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/RPG Consumables & Potions Icons Pack/03_Art/08_Keys & Quest Items/Ancient Seal.png");
#endif
                if (kadimParaSprite == null)
                {
                    kadimParaSprite = Resources.Load<Sprite>("LicenseUI/Ancient Seal") ?? Resources.Load<Sprite>("Ancient Seal");
                }
            }
        }
        #endregion
    }
}
