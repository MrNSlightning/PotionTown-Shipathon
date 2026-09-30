using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Mağazada satılan her bir ürünün kart bileşeni.
    /// Tüm görsel öğeleri, butonları ve metinleri Inspector üzerinden bağlanabilir;
    /// Unity Prefab Modu kullanılarak tasarımı kolayca değiştirilebilir.
    /// </summary>
    public class ShopProductCard : MonoBehaviour
    {
        [Header("Görsel Alanlar")]
        [Tooltip("Kartın arka plan görseli")]
        public Image cardBackground;

        [Tooltip("Kartın altın/ahşap çerçevesi")]
        public Image borderFrame;

        [Tooltip("Ürünün ikonu (Sikke, sandık, parşömen vb.)")]
        public Image productIcon;

        [Header("Metin Alanları")]
        [Tooltip("Ürün başlığı")]
        public TextMeshProUGUI titleText;

        [Tooltip("Ürün açıklaması / içeriği")]
        public TextMeshProUGUI descText;

        [Header("Satın Alma Butonu")]
        [Tooltip("Satın alma butonu")]
        public Button buyButton;

        [Tooltip("Fiyat veya 'SAHİPSİN' metni")]
        public TextMeshProUGUI priceText;

        [Header("Kurdele / Etiket (Opsiyonel)")]
        [Tooltip("Örn: 'BAŞLANGIÇ FIRSATI', 'POPÜLER' kurdelesi")]
        public GameObject ribbonBadge;

        [Tooltip("Kurdele üzerindeki metin")]
        public TextMeshProUGUI ribbonText;

        [Header("Sahip Olundu Durumu (Opsiyonel)")]
        [Tooltip("Eğer ürün tek seferlikse ve satın alınmışsa gösterilecek katman")]
        public GameObject ownedOverlay;

        /// <summary>
        /// Kart verilerini ayarlar ve buton eventini bağlar.
        /// </summary>
        public void Setup(string title, string desc, Sprite icon, string price, string ribbon, bool isPurchased, System.Action onBuyClicked)
        {
            if (titleText != null)
            {
                titleText.text = title;
                UIThemeHelper.ApplyNewRocker(titleText);
            }

            if (descText != null)
            {
                descText.text = desc;
                UIThemeHelper.ApplyNewRocker(descText);
            }

            if (productIcon != null)
            {
                productIcon.sprite = icon;
                productIcon.gameObject.SetActive(icon != null);
            }

            if (ribbonBadge != null)
            {
                ribbonBadge.SetActive(!string.IsNullOrEmpty(ribbon) && !isPurchased);
                if (ribbonText != null && !string.IsNullOrEmpty(ribbon))
                {
                    ribbonText.text = ribbon;
                    UIThemeHelper.ApplyNewRocker(ribbonText);
                }
            }

            if (priceText != null)
            {
                priceText.text = isPurchased ? "SAHİPSİN" : price;
                UIThemeHelper.ApplyNewRocker(priceText);
            }

            if (buyButton != null)
            {
                buyButton.interactable = !isPurchased;
                buyButton.onClick.RemoveAllListeners();
                if (!isPurchased && onBuyClicked != null)
                {
                    buyButton.onClick.AddListener(() => onBuyClicked());
                }
            }

            if (ownedOverlay != null)
            {
                ownedOverlay.SetActive(isPurchased);
            }
        }
    }
}
