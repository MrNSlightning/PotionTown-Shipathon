#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PotionShop.Editor
{
    public static class GameBalanceConfigEditor
    {
        [MenuItem("Tools/Potion Shop/Game Balance Config'i Seç veya Oluştur", false, 1)]
        [MenuItem("Assets/Create/Potion Shop/Game Balance Config (Otomatik Oluştur)", false, 1)]
        public static void CreateOrSelectConfig()
        {
            const string folderPath = "Assets/_PotionTown/Resources";
            const string assetPath = "Assets/_PotionTown/Resources/GameBalanceConfig.asset";

            var config = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
                Debug.Log("<color=green>[PotionShop]</color> GameBalanceConfig bulundu ve seçildi.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var newConfig = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(newConfig, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newConfig;
            EditorGUIUtility.PingObject(newConfig);
            Debug.Log("<color=green>[PotionShop]</color> GameBalanceConfig.asset başarıyla oluşturuldu: " + assetPath);
        }
    }
}
#endif
