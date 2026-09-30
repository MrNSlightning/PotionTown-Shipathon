#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PotionShop;

namespace PotionShop.Editor
{
    [CustomEditor(typeof(TopDropdownMenu))]
    public class TopDropdownMenuEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Menü tasarımı doğrudan Prefab üzerinden yönetilmektedir. Kod çalışma anında (runtime) prefab üzerindeki buton konumlarını, boyutlarını veya tasarımını zorla ezmez.", MessageType.Info);
        }
    }
}
#endif
