using UnityEngine;

namespace PotionShop
{
    public enum ItemType
    {
        Essence,    // Özsu (Sol Raf)
        Ingredient, // Malzeme (Sağ Raf)
        Potion,     // Üretilen İksir
        Scroll,     // Parşömen / Büyü Parşömeni
        Special     // Özel Eşya (SpecialsShelf)
    }

    public enum SpecialItemEffect
    {
        None = 0,
        UniversalIngredient = 1, // Tüm malzemelerin yerine geçer (Joker Malzeme)
        UniversalPotion = 2,     // Müşteriye herhangi bir iksir yerine verilebilir (Joker İksir)
        SlowMoodDecay = 3,       // Müşteri modunun düşme hızını yavaşlatır (Sükunet Merhemi)
        DoubleGold = 4,          // Satışlardan 2x altın kazandırır (Coşku Toniği)
        RushHour = 5,            // Müşterileri hızlandırır ve akın başlatır (Rüzgarın Hızı İksiri)
        ChainBreaker = 6         // İtibar kalkanı, kazanı arındırma ve +1 Kadim Para (Zincir Kıran İksiri)
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "Potion Shop/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Eşya Bilgileri")]
        public string itemName;
        public Sprite itemIcon;
        public Sprite icon => itemIcon; // Alias for compatibility
        public ItemType itemType;

        [Header("Ekonomi")]
        [Tooltip("Sadece Potion türündeyse kullanılır, satıldığında baz alınacak fiyat")]
        public int basePrice;

        [Tooltip("Dükkandan / pazardan satın alınırken ödenecek altın miktarı")]
        public int buyPrice;

        [Header("Kadim Para")]
        [Tooltip("Bu eşyayı Kadim Para ile satın almak için gereken miktar. 0 ise sadece altınla alınır.")]
        public int kadimParaPrice = 0;

        [Header("Özel Eşya Efekti")]
        [Tooltip("Bu eşya özel bir eşyaysa sahip olduğu özel mekanik etki")]
        public SpecialItemEffect specialEffect = SpecialItemEffect.None;

        [Tooltip("Etkinin aktif kalacağı süre (Saniye cinsinden, örn: 300 = 5 dakika)")]
        public float effectDuration = 300f;

        [Tooltip("Etki katsayısı / çarpanı (Örn: yavaşlatma için 0.4, çift altın için 2.0)")]
        public float effectMultiplier = 1f;

        /// <summary>
        /// Bu eşyanın özel bir eşya olup olmadığını belirtir.
        /// </summary>
        public bool IsSpecialItem => itemType == ItemType.Special || specialEffect != SpecialItemEffect.None || kadimParaPrice > 0;

        /// <summary>
        /// Geçerli dildeki eşya adını döndürür.
        /// </summary>
        public string LocalizedName => LocalizationManager.GetItemName(this);

        /// <summary>
        /// Eşya türünün geçerli dildeki görüntüleme adı. Tüm UI'larda kullanılır.
        /// </summary>
        public string TypeDisplayName => LocalizationManager.GetItemType(itemType);
    }
}
