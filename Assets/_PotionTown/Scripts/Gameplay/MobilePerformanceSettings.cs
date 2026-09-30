using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Mobil platformlarda hedef frame rate'i ve ekran uyku davranışını ayarlar.
    /// VSync kapalıyken Application.targetFrameRate pil ömrü ve termal kontrol için elzemdir.
    /// </summary>
    [AddComponentMenu("PotionTown/Mobile Performance Settings")]
    public class MobilePerformanceSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Mobil cihazlarda hedef kare hızı (FPS).")]
        private int targetFrameRate = 60;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        }
    }
}
