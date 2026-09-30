using UnityEngine;

namespace PotionShop
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class LobbyVisualController : MonoBehaviour
    {
        [Header("Arka Plan Resimleri")]
        public Sprite morningSprite; // Sabah/Gündüz resmi
        public Sprite eveningSprite; // Akşam/Gece resmi

        [Header("Zaman Ayarları")]
        [Tooltip("Saat kaçtan sonra akşam resmi çıksın?")]
        public int eveningStartHour = 18; // 18:00
        
        [Tooltip("Saat kaçtan sonra tekrar gündüz resmi çıksın?")]
        public int morningStartHour = 6;  // 06:00

        private SpriteRenderer _bgRenderer;

        private void Awake()
        {
            _bgRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            // Sabit olarak sabah sprite'ını kullan
            UpdateBackground();
        }

        private void OnDestroy()
        {
            // İptal edilecek event yok
        }

        private void UpdateBackground()
        {
            if (morningSprite != null)
            {
                _bgRenderer.sprite = morningSprite;
            }
        }
    }
}
