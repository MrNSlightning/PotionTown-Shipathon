using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PotionShop.Editor
{
    public static class ChibiCustomerManagerEditor
    {
        private const string ChibiPrefabsFolder = "Assets/_PotionTown/Prefabs/Characters";
        private const string ChibiSpritesFolder = "Assets/_PotionTown/Art/Characters/Chibi";

        [MenuItem("Tools/Potion Tavern/Chibi Müşterileri Sahneye Bağla (Assign Chibi to Scene Spawner)", false, 3)]
        public static void AssignChibiPrefabsToScene()
        {
            List<GameObject> chibiPrefabs = new List<GameObject>();
            for (int i = 1; i <= 25; i++)
            {
                string path = $"{ChibiPrefabsFolder}/ChibiCustomer_{i:D2}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    chibiPrefabs.Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"[ChibiCustomerManager] Prefab bulunamadı: {path}");
                }
            }

            if (chibiPrefabs.Count == 0)
            {
                Debug.LogError("[ChibiCustomerManager] Hiç Chibi prefabı bulunamadı!");
                return;
            }

            var spawners = Object.FindObjectsByType<CustomerSpawner>(FindObjectsSortMode.None);
            if (spawners.Length == 0)
            {
                Debug.LogWarning("[ChibiCustomerManager] Sahnede aktif CustomerSpawner bileşeni bulunamadı.");
                return;
            }

            foreach (var spawner in spawners)
            {
                Undo.RecordObject(spawner, "Assign Chibi Customer Prefabs");
                if (spawner.customerPrefabs == null)
                {
                    spawner.customerPrefabs = new List<GameObject>();
                }

                foreach (var p in chibiPrefabs)
                {
                    if (!spawner.customerPrefabs.Contains(p))
                    {
                        spawner.customerPrefabs.Add(p);
                    }
                }

                EditorUtility.SetDirty(spawner);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log($"<color=green>[ChibiCustomerManager]</color> {chibiPrefabs.Count} Chibi müşteri prefabı başarıyla sahnedeki CustomerSpawner'a bağlandı!");
        }

        [MenuItem("Tools/Potion Tavern/Chibi Müşteri Prefablarını Doğrula (Validate Chibi Prefabs)", false, 4)]
        public static void ValidateChibiPrefabs()
        {
            int validCount = 0;
            for (int i = 1; i <= 25; i++)
            {
                string path = $"{ChibiPrefabsFolder}/ChibiCustomer_{i:D2}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"[Validation] Prefab eksik: {path}");
                    continue;
                }

                Customer customer = prefab.GetComponent<Customer>();
                if (customer == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} Customer bileşeni eksik!");
                    continue;
                }

                if (customer.uiContainer == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} uiContainer referansı boş!");
                    continue;
                }

                if (customer.moodIcon == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} moodIcon referansı boş!");
                    continue;
                }

                if (customer.potionRequestIcon == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} potionRequestIcon referansı boş!");
                    continue;
                }

                if (customer.moodSprites == null || customer.moodSprites.Length < 4)
                {
                    Debug.LogError($"[Validation] {prefab.name} moodSprites referansı eksik!");
                    continue;
                }

                if (customer.orderCountText == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} orderCountText referansı eksik!");
                    continue;
                }

                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr == null || sr.sprite == null)
                {
                    Debug.LogError($"[Validation] {prefab.name} SpriteRenderer veya Sprite eksik!");
                    continue;
                }

                validCount++;
            }

            Debug.Log($"<color=green>[Validation Tamamlandı]</color> {validCount}/25 Chibi prefabı tam uyumlu ve geçerli!");
        }
    }
}
