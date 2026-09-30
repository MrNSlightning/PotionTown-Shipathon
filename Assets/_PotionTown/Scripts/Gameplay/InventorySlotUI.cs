using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PotionShop
{
    /// <summary>
    /// Envanterdeki tek bir slotu (kareyi) temsil eder.
    /// İkon, adet metni ve tıklama olayını dinamik olarak yönetir.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        public Image iconImage;
        public TextMeshProUGUI countText;
        public Button button;
        public Image slotBackgroundImage;

        [Header("Görsel Ayarlar")]
        public Color emptySlotColor = Color.clear;
        public Color filledSlotColor = Color.white;

        [HideInInspector]
        public ItemData currentItem;
        [HideInInspector]
        public int currentAmount;

        private Action<ItemData> _onSlotClicked;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            
            // Kullanıcının tasarımına göre slot üzerindeki Image doğrudan ikon görselidir
            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
                if (iconImage == null)
                {
                    Transform iconT = transform.Find("Icon");
                    if (iconT != null) iconImage = iconT.GetComponent<Image>();
                }
            }

            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
                if (currentItem == null || currentAmount <= 0)
                {
                    iconImage.sprite = null;
                    iconImage.color = Color.clear;
                    iconImage.enabled = false;
                }
            }

            if (countText == null)
            {
                countText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        public void SetupSlot(ItemData item, int amount, Action<ItemData> onClick)
        {
            if (this == null || gameObject == null) return;

            currentItem = item;
            currentAmount = amount;
            _onSlotClicked = onClick;

            if (item != null && amount > 0)
            {
                gameObject.SetActive(true);

                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = item.itemIcon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }

                if (countText != null)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = amount > 1 ? amount.ToString() : "";
                }

                if (slotBackgroundImage != null && slotBackgroundImage != iconImage)
                {
                    if (slotBackgroundImage.sprite == null)
                        slotBackgroundImage.color = Color.clear;
                    else
                        slotBackgroundImage.color = filledSlotColor;
                }

                if (button != null)
                {
                    button.interactable = true;
                }
            }
            else
            {
                // Boş slot
                currentItem = null;
                currentAmount = 0;

                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.color = Color.clear;
                    iconImage.enabled = false;
                }

                if (countText != null)
                {
                    countText.text = "";
                    countText.gameObject.SetActive(false);
                }

                if (slotBackgroundImage != null && slotBackgroundImage != iconImage)
                {
                    if (slotBackgroundImage.sprite == null)
                        slotBackgroundImage.color = Color.clear;
                    else
                        slotBackgroundImage.color = emptySlotColor;
                }

                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }

        private void HandleClick()
        {
            if (currentItem != null)
            {
                _onSlotClicked?.Invoke(currentItem);
            }
        }
    }
}
