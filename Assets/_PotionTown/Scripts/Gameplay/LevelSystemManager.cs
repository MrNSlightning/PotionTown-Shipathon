using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PotionShop
{
    [CreateAssetMenu(fileName = "LevelSystemManager", menuName = "Potion Shop/Level System Manager")]
    public class LevelSystemManager : ScriptableObject
    {
        [Header("Gün (Seviye) Kontrolü")]
        [Tooltip("Test etmek istediğiniz günü (seviyeyi) buraya girin ve 'Günü Ayarla' butonuna basın.")]
        public int targetLevel = 1;

        [Header("Kapasite (Servis) Kontrolü")]
        [Tooltip("Aynı anda kaç müşteriye servis verileceğini belirler (Maksimum 3).")]
        [Range(1, 3)]
        public int targetMaxSlots = 1;

        [Header("Gelişmiş Kontroller")]
        [Tooltip("Seviyeyi tamamen 1'e sıfırlamak istiyorsanız 'Sıfırla' butonunu kullanabilirsiniz.")]
        public bool showAdvancedOptions = true;
        
#if UNITY_EDITOR
        [MenuItem("Tools/Level System Manager Oluştur veya Seç", false, 15)]
        public static void CreateOrSelectAsset()
        {
            string path = "Assets/_PotionTown/Resources/LevelSystemManager.asset";
            LevelSystemManager asset = AssetDatabase.LoadAssetAtPath<LevelSystemManager>(path);
            
            if (asset == null)
            {
                asset = CreateInstance<LevelSystemManager>();
                if (!AssetDatabase.IsValidFolder("Assets/_PotionTown/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/_PotionTown", "Resources");
                }
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[LevelSystemManager]</color> Resources klasöründe oluşturuldu!");
            }
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LevelSystemManager))]
    public class LevelSystemManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelSystemManager manager = (LevelSystemManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            EditorGUILayout.LabelField($"Şu Anki Gün/Seviye: {currentLevel}", EditorStyles.boldLabel);

            if (GUILayout.Button($"Günü (Seviyeyi) {manager.targetLevel} Yap", GUILayout.Height(30)))
            {
                PlayerPrefs.SetInt("CurrentLevel", manager.targetLevel);
                PlayerPrefs.Save();
                
                if (Application.isPlaying)
                {
                    var type = typeof(LevelSystem);
                    var field = type.GetField("_currentLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (field != null)
                    {
                        field.SetValue(null, manager.targetLevel);
                    }
                    
                    if (CustomerSpawner.Instance != null)
                    {
                        CustomerSpawner.Instance.InitializeLevel();
                    }
                }
                
                Debug.Log($"<color=green>[LevelSystemManager]</color> Gün {manager.targetLevel} olarak ayarlandı!");
            }

            EditorGUILayout.Space(10);
            
            int currentSlots = PlayerPrefs.GetInt("MaxCustomerSlots", 1);
            EditorGUILayout.LabelField($"Şu Anki Müşteri Kapasitesi: {currentSlots}", EditorStyles.boldLabel);

            if (GUILayout.Button($"Kapasiteyi {manager.targetMaxSlots} Yap", GUILayout.Height(30)))
            {
                PlayerPrefs.SetInt("MaxCustomerSlots", manager.targetMaxSlots);
                PlayerPrefs.Save();
                
                if (Application.isPlaying && CustomerSpawner.Instance != null)
                {
                    CustomerSpawner.Instance.maxSimultaneousCustomers = manager.targetMaxSlots;
                }
                
                Debug.Log($"<color=green>[LevelSystemManager]</color> Kapasite {manager.targetMaxSlots} olarak ayarlandı!");
            }

            if (manager.showAdvancedOptions)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Sıfırla (1. Güne Dön)", GUILayout.Height(25)))
                {
                    manager.targetLevel = 1;
                    manager.targetMaxSlots = 1;
                    PlayerPrefs.SetInt("CurrentLevel", 1);
                    PlayerPrefs.SetInt("MaxCustomerSlots", 1);
                    PlayerPrefs.Save();
                    
                    if (Application.isPlaying)
                    {
                        var type = typeof(LevelSystem);
                        var field = type.GetField("_currentLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (field != null)
                        {
                            field.SetValue(null, 1);
                        }
                        if (CustomerSpawner.Instance != null)
                        {
                            CustomerSpawner.Instance.maxSimultaneousCustomers = 1;
                            CustomerSpawner.Instance.InitializeLevel();
                        }
                    }
                    
                    Debug.Log("<color=red>[LevelSystemManager]</color> Seviye ve Kapasite 1'e sıfırlandı!");
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }
    }
#endif
}
