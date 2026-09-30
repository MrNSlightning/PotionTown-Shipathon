using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    /// <summary>
    /// Herhangi bir UI Button nesnesine eklenebilen ve tıklandığında arka plan müziğini
    /// açıp kapatan (Mute/Unmute) yardımcı bileşen.
    /// - Üzerindeki buton veya TextMeshPro/Image bileşenini otomatik bulur.
    /// - Ses açılıp kapandığında simge ve yazıyı anında günceller.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SoundToggleButton : MonoBehaviour
    {
        [Header("UI Referansları (Otomatik Bulunur)")]
        [Tooltip("Tıklanacak buton")]
        public Button button;

        [Tooltip("Durum yazısını gösterecek TextMeshPro bileşeni (İsteğe bağlı)")]
        public TextMeshProUGUI statusText;

        [Tooltip("Durum ikonunu gösterecek Image bileşeni (İsteğe bağlı)")]
        public Image iconImage;

        [Header("Görsel / Metin Ayarları")]
        [Tooltip("Müzik açıkken gösterilecek metin")]
        public string onText = "Müzik: Açık";

        [Tooltip("Müzik kapalıyken gösterilecek metin")]
        public string offText = "Müzik: Kapalı";

        [Tooltip("Müzik açıkken gösterilecek ikon")]
        public Sprite onSprite;

        [Tooltip("Müzik kapalıyken gösterilecek ikon")]
        public Sprite offSprite;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (statusText == null) statusText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);

            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnEnable()
        {
            AudioManager.OnMusicMuteChanged += HandleMuteChanged;
            RefreshVisuals();
        }

        private void OnDisable()
        {
            AudioManager.OnMusicMuteChanged -= HandleMuteChanged;
        }

        private void Start()
        {
            RefreshVisuals();
        }

        private void OnButtonClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMusicMute();
            }
            else
            {
                // Eğer sahnede AudioManager objesi henüz yoksa otomatik oluştur
                var manager = FindFirstObjectByType<AudioManager>();
                if (manager == null)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    manager = go.AddComponent<AudioManager>();
                }
                manager.ToggleMusicMute();
            }
            RefreshVisuals();
        }

        private void HandleMuteChanged(bool isMuted)
        {
            RefreshVisuals();
        }

        /// <summary>
        /// Buton üzerindeki metin ve görsel durumunu yeniler.
        /// </summary>
        public void RefreshVisuals()
        {
            bool isMuted = false;
            if (AudioManager.Instance != null)
            {
                isMuted = AudioManager.Instance.IsMusicMuted;
            }
            else
            {
                isMuted = PlayerPrefs.GetInt("AudioManager_MusicMuted", 0) == 1;
            }

            if (statusText != null)
            {
                statusText.text = isMuted ? offText : onText;
            }

            if (iconImage != null)
            {
                if (isMuted && offSprite != null)
                {
                    iconImage.sprite = offSprite;
                }
                else if (!isMuted && onSprite != null)
                {
                    iconImage.sprite = onSprite;
                }
            }
        }
    }
}
