using UnityEngine;

namespace PotionShop
{
    [RequireComponent(typeof(Camera))]
    public class MobileScreenFitter : MonoBehaviour
    {
        [Tooltip("Oyununuzun tasarlandığı hedef çözünürlük (Örn: 1920x1080)")]
        public Vector2 targetResolution = new Vector2(1920, 1080);

        private Camera cam;
        private float targetAspect;
        private float originalOrthoSize;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                originalOrthoSize = cam.orthographicSize;
            }
        }

        void Start()
        {
            targetAspect = targetResolution.x / targetResolution.y;
            AdjustCamera();
        }

#if UNITY_EDITOR
        void Update()
        {
            // Editörde ekran boyutu değişirse anlık güncelle
            AdjustCamera();
        }
#endif

        private void AdjustCamera()
        {
            if (cam == null || !cam.orthographic) return;

            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            // Eğer mevcut ekran, hedef ekrandan daha genişse (örneğin güncel telefonlar)
            if (scaleHeight > 1.0f)
            {
                // Boşluk kalmaması için kamerayı zoom-in yap (genişliğe uydur):
                cam.orthographicSize = originalOrthoSize / scaleHeight;
            }
            else
            {
                // Ekran daha darsa (Kare ekranlar vs), normal boyutta tut
                cam.orthographicSize = originalOrthoSize;
            }
        }
    }
}
