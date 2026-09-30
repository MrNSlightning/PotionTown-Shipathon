using UnityEngine;
using UnityEngine.UI;

namespace PotionShop
{
    public class StorageButton : MonoBehaviour
    {
        public ItemData itemData;
        public Image iconImage;

        private void Start()
        {
            if (itemData != null && iconImage != null)
            {
                iconImage.sprite = itemData.itemIcon;
            }
        }

        // Butonun OnClick Eventine bunu bağlayın
        public void OnButtonClicked()
        {
            if (StorageManager.Instance != null && itemData != null)
            {
                StorageManager.Instance.SelectItemForSlot(itemData);
            }
        }
    }
}
