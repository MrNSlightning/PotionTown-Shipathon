using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace PotionShop
{
    public class UIFixerEditor
    {
        [MenuItem("Potion Shop/Mobil Ekranları Düzelt (Fix UI)")]
        public static void FixMobileUI()
        {
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int count = 0;
            foreach (var scaler in scalers)
            {
                Undo.RecordObject(scaler, "Fix Canvas Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Telefonlarda genellikle yüksekliğe göre eşitlemek arka planları kurtarır,
                // geniş telefonlarda sağ/sol boşluk bırakabilir. Eğer boşluk istemiyorsak 0.5 mantıklıdır.
                // 0.5 hem genişliği hem yüksekliği dengeli tutar.
                scaler.matchWidthOrHeight = 0.5f;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                count++;
                EditorUtility.SetDirty(scaler);
            }

            // Sahnede bulunan tüm kameraların arka planını siyah yap ve ScreenFitter ekle
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera cam in allCameras)
            {
                Undo.RecordObject(cam, "Fix Camera Color");
                cam.backgroundColor = Color.black;
                
                if (cam.GetComponent<MobileScreenFitter>() == null)
                {
                    Undo.AddComponent<MobileScreenFitter>(cam.gameObject);
                }
                EditorUtility.SetDirty(cam);
            }

            Debug.Log($"<color=green>[UI Fixer]</color> Başarıyla {count} adet Canvas ayarlandı ve {allCameras.Length} adet Kameraya (Lobby, Crafting vb.) ekran sabitleyici eklendi. Lütfen sahneyi kaydedin (Ctrl+S).");
        }
    }
}
