#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace PotionShop.Editor
{
    public static class PotionTownSceneValidator
    {
        private const string PotionTownScenePath = "Assets/_PotionTown/Scenes/PotionTown.unity";
        private const string SampleScenePath = "Assets/_PotionTown/Scenes/SampleScene.unity";

        [MenuItem("PotionTown/Validate PotionTown Scene", false, 2)]
        public static void ValidateFromMenu()
        {
            ValidateAll();
        }

        public static void ValidateCommandLine()
        {
            Debug.Log("<color=yellow>[PotionTownSceneValidator]</color> Command line validation started...");
            bool success = ValidateAll();
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static bool ValidateAll()
        {
            int errorCount = 0;
            int warningCount = 0;

            Debug.Log("================ STARTING POTIONTOWN SCENE VALIDATION ================");

            // --- 1. POTIONTOWN.UNITY DOĞRULAMASI ---
            var scene = EditorSceneManager.OpenScene(PotionTownScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[FATAL] Could not open scene: {PotionTownScenePath}");
                return false;
            }

            var roots = scene.GetRootGameObjects();
            Debug.Log($"Root GameObject Count in PotionTown: {roots.Length}");

            // 5 Temel Kök Klasör Kontrolü
            string[] expectedRoots = {
                "--- [00_CORE_MANAGERS] ---",
                "--- [01_CAMERAS & LIGHTING] ---",
                "--- [02_ENVIRONMENT & ROOMS] ---",
                "--- [03_GLOBAL_UI] ---",
                "--- [04_SYSTEMS] ---"
            };

            foreach (var exp in expectedRoots)
            {
                bool found = false;
                foreach (var r in roots)
                {
                    if (r.name == exp) { found = true; break; }
                }
                if (found)
                    Debug.Log($"<color=green>[PASS]</color> Root folder exists: {exp}");
                else
                {
                    Debug.LogError($"[FAIL] Missing root folder: {exp}");
                    errorCount++;
                }
            }

            // PotionTownSceneCoordinator Kontrolü
            var coordinator = Object.FindFirstObjectByType<PotionTownSceneCoordinator>();
            if (coordinator == null)
            {
                Debug.LogError("[FAIL] PotionTownSceneCoordinator not found in scene!");
                errorCount++;
            }
            else
            {
                Debug.Log("<color=green>[PASS]</color> PotionTownSceneCoordinator found.");
                if (coordinator.lobbyRoom == null) { Debug.LogError("[FAIL] Coordinator: lobbyRoom is null!"); errorCount++; }
                if (coordinator.potionSellingRoom == null) { Debug.LogError("[FAIL] Coordinator: potionSellingRoom is null!"); errorCount++; }
                if (coordinator.potionCraftingRoom == null) { Debug.LogError("[FAIL] Coordinator: potionCraftingRoom is null!"); errorCount++; }
                if (coordinator.ingredientShopRoom == null) { Debug.LogError("[FAIL] Coordinator: ingredientShopRoom is null!"); errorCount++; }

                if (coordinator.lobbyCamera == null) { Debug.LogError("[FAIL] Coordinator: lobbyCamera is null!"); errorCount++; }
                if (coordinator.potionSellingCamera == null) { Debug.LogError("[FAIL] Coordinator: potionSellingCamera is null!"); errorCount++; }
                if (coordinator.potionCraftingCamera == null) { Debug.LogError("[FAIL] Coordinator: potionCraftingCamera is null!"); errorCount++; }
                if (coordinator.ingredientShopCamera == null) { Debug.LogError("[FAIL] Coordinator: ingredientShopCamera is null!"); errorCount++; }

                if (coordinator.gameManager == null) { Debug.LogError("[FAIL] Coordinator: gameManager is null!"); errorCount++; }

                if (coordinator.playerInventory == null) { Debug.LogError("[FAIL] Coordinator: playerInventory is null!"); errorCount++; }
                if (coordinator.licenseManager == null) { Debug.LogError("[FAIL] Coordinator: licenseManager is null!"); errorCount++; }
                if (coordinator.storageManager == null) { Debug.LogError("[FAIL] Coordinator: storageManager is null!"); errorCount++; }
                if (coordinator.topDropdownMenu == null) { Debug.LogError("[FAIL] Coordinator: topDropdownMenu is null!"); errorCount++; }
            }

            // Tekil Yönetici Kontrolleri (Deduplication)
            var allGMs = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allGMs.Length == 1)
                Debug.Log("<color=green>[PASS]</color> Exactly 1 GameManager in scene (Deduplication successful).");
            else
            {
                Debug.LogError($"[FAIL] Expected 1 GameManager, found {allGMs.Length}!");
                errorCount++;
            }

            var allSMs = Object.FindObjectsByType<StorageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allSMs.Length == 1)
                Debug.Log("<color=green>[PASS]</color> Exactly 1 StorageManager in scene (Deduplication successful).");
            else
            {
                Debug.LogError($"[FAIL] Expected 1 StorageManager, found {allSMs.Length}!");
                errorCount++;
            }

            var allEODs = Object.FindObjectsByType<EndOfDaySign>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allEODs.Length == 1)
                Debug.Log("<color=green>[PASS]</color> Exactly 1 EndOfDaySign in scene (Deduplication successful).");
            else
            {
                Debug.LogError($"[FAIL] Expected 1 EndOfDaySign, found {allEODs.Length}!");
                errorCount++;
            }

            // Lobideki Bina Tetikleyicileri (PotionTownBuildingTrigger) Kontrolü
            var triggers = Object.FindObjectsByType<PotionTownBuildingTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (triggers.Length >= 3)
                Debug.Log($"<color=green>[PASS]</color> Found {triggers.Length} PotionTownBuildingTrigger components in Lobby.");
            else
            {
                Debug.LogError($"[FAIL] Expected at least 3 PotionTownBuildingTrigger components, found {triggers.Length}!");
                errorCount++;
            }

            foreach (var trig in triggers)
            {
                if (trig.coordinator == null)
                {
                    Debug.LogWarning($"[WARN] Trigger on {trig.gameObject.name} coordinator is unassigned!");
                    warningCount++;
                }
            }

            // Oyun Mekanikleri Varlık Kontrolleri
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner != null && spawner.customerPrefabs != null && spawner.customerPrefabs.Count > 0)
                Debug.Log($"<color=green>[PASS]</color> CustomerSpawner found with {spawner.customerPrefabs.Count} customer templates.");
            else
            {
                Debug.LogError("[FAIL] CustomerSpawner missing or has no customer prefabs!");
                errorCount++;
            }

            var servingArea = Object.FindFirstObjectByType<ServingArea>(FindObjectsInactive.Include);
            if (servingArea != null)
                Debug.Log("<color=green>[PASS]</color> ServingArea drop handler found.");
            else
            {
                Debug.LogError("[FAIL] ServingArea component missing!");
                errorCount++;
            }

            var cauldrons = Object.FindObjectsByType<Cauldron>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cauldrons.Length >= 1)
                Debug.Log($"<color=green>[PASS]</color> Found {cauldrons.Length} Cauldron station(s).");
            else
            {
                Debug.LogError("[FAIL] Cauldron station missing!");
                errorCount++;
            }

            var fixedShelf = Object.FindFirstObjectByType<FixedShelfManager>(FindObjectsInactive.Include);
            if (fixedShelf != null && fixedShelf.fixedItems != null && fixedShelf.fixedItems.Count > 0)
                Debug.Log($"<color=green>[PASS]</color> FixedShelfManager found with {fixedShelf.fixedItems.Count} fixed item entries.");
            else
            {
                Debug.LogError("[FAIL] FixedShelfManager missing or has no fixed items!");
                errorCount++;
            }

            var shelfManagers = Object.FindObjectsByType<ShelfManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"<color=green>[PASS]</color> Found {shelfManagers.Length} ShelfManager instances (Potion and Ingredient shelves).");

            var specialsShelf = Object.FindFirstObjectByType<SpecialsShelfManager>(FindObjectsInactive.Include);
            if (specialsShelf != null)
                Debug.Log("<color=green>[PASS]</color> SpecialsShelfManager found.");
            else
            {
                Debug.LogError("[FAIL] SpecialsShelfManager missing!");
                errorCount++;
            }

            var shopButtons = Object.FindObjectsByType<ShopItemButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"<color=green>[PASS]</color> Found {shopButtons.Length} ShopItemButton instances in ingredient shop.");

            // --- 2. SAMPLESCENE.UNITY DOKUNULMAZLIK VE SAĞLAMLIĞI ---
            Debug.Log("\n--- VERIFYING ORIGINAL SAMPLESCENE.UNITY INTEGRITY ---");
            var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            if (!sampleScene.IsValid())
            {
                Debug.LogError($"[FATAL] SampleScene.unity could not be opened at {SampleScenePath}");
                return false;
            }

            var sampleGMs = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (sampleGMs.Length == 3)
                Debug.Log($"<color=green>[PASS]</color> SampleScene has its original {sampleGMs.Length} GameManager instances untouched.");
            else
            {
                Debug.LogError($"[FAIL] SampleScene GameManager count altered! Expected 3, got {sampleGMs.Length}");
                errorCount++;
            }

            var sampleSMs = Object.FindObjectsByType<StorageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (sampleSMs.Length == 3)
                Debug.Log($"<color=green>[PASS]</color> SampleScene has its original {sampleSMs.Length} StorageManager instances untouched.");
            else
            {
                Debug.LogError($"[FAIL] SampleScene StorageManager count altered! Expected 3, got {sampleSMs.Length}");
                errorCount++;
            }

            // Re-open PotionTown so it remains the active scene in Editor
            EditorSceneManager.OpenScene(PotionTownScenePath, OpenSceneMode.Single);

            Debug.Log($"\n================ VALIDATION RESULT: {errorCount} ERRORS, {warningCount} WARNINGS ================");
            return errorCount == 0;
        }
    }
}
#endif
