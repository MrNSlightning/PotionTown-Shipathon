#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PotionShop.UI;

namespace PotionShop.Editor
{
    [CustomEditor(typeof(EndOfDayPanel))]
    public class EndOfDayPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EndOfDayPanel panel = (EndOfDayPanel)target;

            EditorGUILayout.HelpBox(
                "✨ Gün Sonu Özet Paneli (EndOfDayPanel)\n\n" +
                "• Bu panelin tüm tasarımı, boyutları, renkleri, fontları ve buton yerleşimleri " +
                "doğrudan bu Prefab / Inspector üzerinden serbestçe düzenlenebilir.\n" +
                "• Kod çalışma anında (runtime) prefab tasarımınızı, RectTransform boyutlarını veya metin stillerini asla ezmez.\n" +
                "• Aşağıdaki butonları kullanarak paneli test edebilir veya varsayılan prefab tasarımını yeniden inşa edebilirsiniz.",
                MessageType.Info);

            EditorGUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("👁️ Paneli Aç / Test Et", GUILayout.Height(30)))
            {
                panel.gameObject.SetActive(true);
                panel.OpenPanel();
            }

            if (GUILayout.Button("❌ Paneli Kapat", GUILayout.Height(30)))
            {
                panel.ClosePanel();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            if (GUILayout.Button("🔨 Varsayılan Tasarımla Prefab'i Sıfırla / Yeniden İnşa Et"))
            {
                if (EditorUtility.DisplayDialog("Prefab'i Sıfırla", 
                    "EndOfDayPanel prefab'i varsayılan şık tasarımla yeniden oluşturulacak. Devam edilsin mi?", "Evet", "İptal"))
                {
                    EndOfDayPanelBuilder.CreateOrRebuildPrefab();
                }
            }

            EditorGUILayout.Space(10);
            DrawDefaultInspector();
        }
    }
}
#endif
