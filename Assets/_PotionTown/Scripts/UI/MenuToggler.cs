using UnityEngine;

namespace PotionShop
{
    public class MenuToggler : MonoBehaviour
    {
        [Tooltip("Açılıp kapanacak olan menü (Örn: Hilal Görseli)")]
        public GameObject menuPanel;

        // Butona tıklandığında çağrılacak fonksiyon
        public void ToggleMenu()
        {
            if (menuPanel != null)
            {
                // Eğer açıksa kapatır, kapalıysa açar
                menuPanel.SetActive(!menuPanel.activeSelf);
            }
            else
            {
                Debug.LogWarning("MenuToggler: Açılacak menü objesi (menuPanel) atanmamış!");
            }
        }
    }
}
