using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    [RequireComponent(typeof(Button))]
    public class EssenceButton : MonoBehaviour
    {
        [Header("Özsu / Parşömen Ayarları")]
        [Tooltip("Bu butona basıldığında kazana eklenecek olan özsu veya parşömen (Örn: Mavi Özsu, Ateş Parşömeni)")]
        public ItemData essenceItem;
        
        [Tooltip("Kazanın bürüneceği renk (Dolu kırmızı kazan, dolu mavi kazan resmi vs.)")]
        public Sprite colorCauldronSprite;

        [Header("Referanslar")]
        public Cauldron targetCauldron;
        
        [Tooltip("Seçim yapıldıktan sonra otomatik kapanacak Hilal Menüsü (İsteğe Bağlı)")]
        public GameObject parentMenuToClose;

        [Header("Miktar ve Görsel Ayarları")]
        [Tooltip("Öz veya scroll miktarını gösteren metin bileşeni (Atanmazsa child'lardan otomatik bulunur)")]
        public TextMeshProUGUI amountText;

        [Tooltip("Miktar yazısının butonun ne kadar altında duracağı")]
        public float textYOffset = -28f;

        [Tooltip("Miktarı 0 olduğunda buton görselinin rengi")]
        public Color disabledColor = new Color(0.42f, 0.42f, 0.42f, 1f);

        [Tooltip("Miktarı 0'dan fazla olduğunda buton görselinin rengi")]
        public Color normalColor = Color.white;

        private Button _button;
        private Image _image;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();

            if (_button != null)
            {
                // Unity'nin Selectable rengi soluklaştırmasını engelle (Kendi kontrolümüz altında tam opak gri renk veriyoruz)
                _button.transition = Selectable.Transition.None;
            }

            if (amountText == null)
            {
                amountText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (amountText != null)
            {
                amountText.alignment = TextAlignmentOptions.Center;
                RectTransform textRT = amountText.rectTransform;
                if (textRT != null)
                {
                    textRT.anchorMin = new Vector2(0.5f, 0.5f);
                    textRT.anchorMax = new Vector2(0.5f, 0.5f);
                    textRT.pivot = new Vector2(0.5f, 0.5f);
                    textRT.sizeDelta = new Vector2(60f, 28f);
                }
            }
        }

        private void OnEnable()
        {
            PlayerInventory.onInventoryChangedStatic += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            PlayerInventory.onInventoryChangedStatic -= RefreshUI;
        }

        private void Start()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.transition = Selectable.Transition.None;
                _button.onClick.AddListener(OnButtonClicked);
            }
            RefreshUI();
        }

        private void LateUpdate()
        {
            // Hilal menüsü veya buton döndürülmüş olsa bile metni daima dik ve butonun tam altında tut
            if (amountText != null && amountText.gameObject.activeInHierarchy)
            {
                amountText.transform.rotation = Quaternion.identity;
                RectTransform myRT = transform as RectTransform;
                float halfHeight = myRT != null ? myRT.rect.height * 0.5f : 32f;
                float scaleY = Mathf.Abs(transform.lossyScale.y);
                if (scaleY < 0.0001f) scaleY = 1f;
                float downOffset = (halfHeight + 12f) * scaleY;
                amountText.transform.position = new Vector3(transform.position.x, transform.position.y - downOffset, transform.position.z);
            }
        }

        /// <summary>
        /// Envanterdeki miktarı kontrol eder, miktarı günceller ve 0 ise butonu grileştirip tıklanamaz yapar.
        /// </summary>
        public void RefreshUI()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_image == null) _image = GetComponent<Image>();

            if (_button != null)
            {
                _button.transition = Selectable.Transition.None;
            }

            int count = 0;
            if (PlayerInventory.Instance != null && essenceItem != null)
            {
                count = PlayerInventory.Instance.GetItemCount(essenceItem);
            }

            if (amountText != null)
            {
                amountText.text = count.ToString();
                amountText.color = count > 0 ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            if (count > 0)
            {
                if (_button != null) _button.interactable = true;
                if (_image != null) _image.color = normalColor;
            }
            else
            {
                if (_button != null) _button.interactable = false;
                if (_image != null)
                {
                    Color col = disabledColor;
                    col.a = 1f; // Tam opak alfa: Koyu kazan arka planında net bir gri ikon olarak görünmesini sağlar
                    _image.color = col;
                }
            }
        }

        private void OnButtonClicked()
        {
            // Envanterde hiç yoksa işlem yapma
            if (PlayerInventory.Instance != null && essenceItem != null)
            {
                if (PlayerInventory.Instance.GetItemCount(essenceItem) <= 0)
                {
                    Debug.LogWarning($"EssenceButton: {essenceItem.itemName} envanterde kalmadı!");
                    RefreshUI();
                    return;
                }
            }

            if (targetCauldron == null)
            {
                Debug.LogError("EssenceButton: Hedef kazan (Cauldron) atanmamış!");
                return;
            }

            if (essenceItem == null || colorCauldronSprite == null)
            {
                Debug.LogWarning("EssenceButton: Özsu/Parşömen veya kazan resmi atanmamış!");
                return;
            }

            targetCauldron.SetEssence(essenceItem, colorCauldronSprite);
            
            // Eğer hilal menüsü atanmışsa, seçim yapıldığı an menüyü gizle
            if (parentMenuToClose != null)
            {
                parentMenuToClose.SetActive(false);
            }
        }
    }
}
