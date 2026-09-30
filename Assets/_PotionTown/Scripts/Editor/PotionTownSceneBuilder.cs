#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PotionShop.Editor
{
    public static class PotionTownSceneBuilder
    {
        private const string ScenePath = "Assets/_PotionTown/Scenes/PotionTown.unity";

        [MenuItem("PotionTown/Build PotionTown Scene", false, 1)]
        public static void BuildSceneFromMenu()
        {
            BuildPotionTownScene();
        }

        public static void BuildSceneCommandLine()
        {
            Debug.Log("<color=yellow>[PotionTownSceneBuilder]</color> Command line build started...");
            bool success = BuildPotionTownScene();
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static bool BuildPotionTownScene()
        {
            Debug.Log($"<color=cyan>[PotionTownSceneBuilder]</color> Opening scene: {ScenePath}");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[PotionTownSceneBuilder] Failed to open scene at {ScenePath}");
                return false;
            }

            // 1. Root Gruplarını Oluştur
            GameObject coreManagersRoot = GetOrCreateRoot("--- [00_CORE_MANAGERS] ---");
            GameObject camerasLightingRoot = GetOrCreateRoot("--- [01_CAMERAS & LIGHTING] ---");
            GameObject roomsRoot = GetOrCreateRoot("--- [02_ENVIRONMENT & ROOMS] ---");
            GameObject globalUIRoot = GetOrCreateRoot("--- [03_GLOBAL_UI] ---");
            GameObject systemsRoot = GetOrCreateRoot("--- [04_SYSTEMS] ---");

            GameObject camerasFolder = GetOrCreateChild(camerasLightingRoot, "Cameras");
            GameObject lightingFolder = GetOrCreateChild(camerasLightingRoot, "Lighting");

            // 2. Mevcut Objeleri Sahne Genelinde Bul
            GameObject playerSystems = FindGameObjectInScene("PlayerSystems");
            GameObject managerObj = FindGameObjectInScene("TimeManager") ?? FindGameObjectInScene("Manager");
            GameObject licenseManagerObj = FindGameObjectInScene("[LicenseManager]");
            GameObject rafUcretiObj = FindGameObjectInScene("Raf Ücreti");
            GameObject eventSystemObj = FindGameObjectInScene("EventSystem");
            GameObject topDropdownMenuObj = FindGameObjectInScene("TopDropdownMenu");
            GameObject endOfDaySignObj = FindGameObjectInScene("EndOfDaySign");

            GameObject lobiSahnesi = FindGameObjectInScene("Room_LobiSahnesi") ?? FindGameObjectInScene("LobiSahnesi");
            GameObject iksirSatisAna = FindGameObjectInScene("Room_İksirSatışAna") ?? FindGameObjectInScene("İksirSatışAna");
            GameObject iksirYapmaDukkani = FindGameObjectInScene("Room_İksirYapmaDükkani") ?? FindGameObjectInScene("İksirYapmaDükkani");
            GameObject malzemeDukkani = FindGameObjectInScene("Room_MalzemeDükkani") ?? FindGameObjectInScene("MalzemeDükkani");

            // 3. Kameraları Odaları Altında Doğru Koordinatlarıyla Organize Et
            Camera lobiCam = FindCameraInChildren(lobiSahnesi, "LobiKamera") ?? FindCameraInChildren(lobiSahnesi, "LobbyCamera") ?? FindCameraInChildren(camerasFolder, "LobbyCamera");
            Camera iksirSatisCam = FindCameraInChildren(iksirSatisAna, "İksirSatışKamera") ?? FindCameraInChildren(iksirSatisAna, "PotionSellingCamera") ?? FindCameraInChildren(camerasFolder, "PotionSellingCamera");
            Camera iksirYapmaCam = FindCameraInChildren(iksirYapmaDukkani, "İksirYapmaKamera") ?? FindCameraInChildren(iksirYapmaDukkani, "PotionCraftingCamera") ?? FindCameraInChildren(camerasFolder, "PotionCraftingCamera");
            Camera malzemeCam = FindCameraInChildren(malzemeDukkani, "MalzememKamera") ?? FindCameraInChildren(malzemeDukkani, "IngredientShopCamera") ?? FindCameraInChildren(camerasFolder, "IngredientShopCamera");

            if (lobiCam != null && lobiSahnesi != null)
            {
                lobiCam.gameObject.name = "LobbyCamera";
                lobiCam.transform.SetParent(lobiSahnesi.transform, false);
                lobiCam.transform.localPosition = new Vector3(-399.81f, -218.9f, -139.61f);
                lobiCam.transform.localScale = new Vector3(5f, 5f, 5f);
                lobiCam.transform.localRotation = new Quaternion(0.010796402f, 0.010797386f, -0.000116151656f, 0.9998834f);
                lobiCam.orthographic = true;
                lobiCam.orthographicSize = 22.9f;
                lobiCam.nearClipPlane = 0.3f;
                lobiCam.farClipPlane = 1000f;
                lobiCam.gameObject.SetActive(true);
                lobiCam.enabled = true;
                lobiCam.tag = "MainCamera";
                EditorUtility.SetDirty(lobiCam.gameObject);
                EditorUtility.SetDirty(lobiCam);
            }
            if (iksirSatisCam != null && iksirSatisAna != null)
            {
                iksirSatisCam.gameObject.name = "PotionSellingCamera";
                iksirSatisCam.transform.SetParent(iksirSatisAna.transform, false);
                iksirSatisCam.transform.localPosition = new Vector3(-27.624735f, -17.927898f, -9.600151f);
                iksirSatisCam.transform.localScale = new Vector3(0.38026747f, 0.38026747f, 0.38026747f);
                iksirSatisCam.transform.localRotation = Quaternion.identity;
                iksirSatisCam.gameObject.SetActive(false);
                iksirSatisCam.enabled = false;
                iksirSatisCam.tag = "Untagged";
                EditorUtility.SetDirty(iksirSatisCam.gameObject);
                EditorUtility.SetDirty(iksirSatisCam);
            }
            if (iksirYapmaCam != null && iksirYapmaDukkani != null)
            {
                iksirYapmaCam.gameObject.name = "PotionCraftingCamera";
                iksirYapmaCam.transform.SetParent(iksirYapmaDukkani.transform, false);
                iksirYapmaCam.transform.localPosition = Vector3.zero;
                iksirYapmaCam.transform.localScale = Vector3.one;
                iksirYapmaCam.transform.localRotation = Quaternion.identity;
                iksirYapmaCam.gameObject.SetActive(false);
                iksirYapmaCam.enabled = false;
                iksirYapmaCam.tag = "Untagged";
                EditorUtility.SetDirty(iksirYapmaCam.gameObject);
                EditorUtility.SetDirty(iksirYapmaCam);
            }
            if (malzemeCam != null && malzemeDukkani != null)
            {
                malzemeCam.gameObject.name = "IngredientShopCamera";
                malzemeCam.transform.SetParent(malzemeDukkani.transform, false);
                malzemeCam.transform.localPosition = Vector3.zero;
                malzemeCam.transform.localScale = Vector3.one;
                malzemeCam.transform.localRotation = Quaternion.identity;
                malzemeCam.gameObject.SetActive(false);
                malzemeCam.enabled = false;
                malzemeCam.tag = "Untagged";
                EditorUtility.SetDirty(malzemeCam.gameObject);
                EditorUtility.SetDirty(malzemeCam);
            }

            if (camerasFolder != null && camerasFolder.transform.childCount == 0)
            {
                Object.DestroyImmediate(camerasFolder);
            }

            // 4. Odaları Organize Et (02_ENVIRONMENT & ROOMS)
            if (lobiSahnesi != null)
            {
                lobiSahnesi.name = "Room_LobiSahnesi";
                lobiSahnesi.transform.SetParent(roomsRoot.transform, true);
                lobiSahnesi.SetActive(true);
                ResetTransformScale(lobiSahnesi);
                EditorUtility.SetDirty(lobiSahnesi);
            }
            if (iksirSatisAna != null)
            {
                iksirSatisAna.name = "Room_İksirSatışAna";
                iksirSatisAna.transform.SetParent(roomsRoot.transform, true);
                iksirSatisAna.SetActive(false);
                ResetTransformScale(iksirSatisAna);
                EditorUtility.SetDirty(iksirSatisAna);
            }
            if (iksirYapmaDukkani != null)
            {
                iksirYapmaDukkani.name = "Room_İksirYapmaDükkani";
                iksirYapmaDukkani.transform.SetParent(roomsRoot.transform, true);
                iksirYapmaDukkani.SetActive(false);
                ResetTransformScale(iksirYapmaDukkani);
                EditorUtility.SetDirty(iksirYapmaDukkani);
            }
            if (malzemeDukkani != null)
            {
                malzemeDukkani.name = "Room_MalzemeDükkani";
                malzemeDukkani.transform.SetParent(roomsRoot.transform, true);
                malzemeDukkani.SetActive(false);
                ResetTransformScale(malzemeDukkani);
                EditorUtility.SetDirty(malzemeDukkani);
            }

            // EndOfDaySign'ı Lobi içine taşı ve çift script sorununu temizle
            if (endOfDaySignObj != null && lobiSahnesi != null)
            {
                endOfDaySignObj.transform.SetParent(lobiSahnesi.transform, true);
                DeduplicateEndOfDaySign(endOfDaySignObj);
                EditorUtility.SetDirty(endOfDaySignObj);
            }

            // 5. Yöneticileri Tekilleştir ve Taşı (00_CORE_MANAGERS)
            if (playerSystems != null)
            {
                playerSystems.transform.SetParent(coreManagersRoot.transform, true);
                playerSystems.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(playerSystems);
            }
            if (managerObj != null)
            {
                managerObj.name = "TimeManager";
                managerObj.transform.SetParent(coreManagersRoot.transform, true);
                managerObj.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(managerObj);
            }
            if (licenseManagerObj != null)
            {
                licenseManagerObj.transform.SetParent(coreManagersRoot.transform, true);
                licenseManagerObj.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(licenseManagerObj);
            }
            if (rafUcretiObj != null)
            {
                rafUcretiObj.transform.SetParent(coreManagersRoot.transform, true);
                EditorUtility.SetDirty(rafUcretiObj);
            }

            // GameManager tekilleştirme:
            GameManager primaryGM = coreManagersRoot.GetComponentInChildren<GameManager>(true);
            if (primaryGM == null && iksirSatisAna != null)
            {
                primaryGM = iksirSatisAna.GetComponentInChildren<GameManager>(true);
            }
            if (primaryGM == null && malzemeDukkani != null)
            {
                primaryGM = malzemeDukkani.GetComponentInChildren<GameManager>(true);
            }
            if (primaryGM != null)
            {
                primaryGM.gameObject.name = "GameManager";
                primaryGM.transform.SetParent(coreManagersRoot.transform, true);
                primaryGM.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(primaryGM.gameObject);
                EditorUtility.SetDirty(primaryGM);
            }

            // Diğer dükkanlardaki mükerrer GameManager objelerini tamamen yok et
            DestroyChildrenByName(malzemeDukkani, "GameManager");
            DestroyChildrenByName(iksirYapmaDukkani, "GameManager");

            // StorageManager tekilleştirme:
            StorageManager primarySM = coreManagersRoot.GetComponentInChildren<StorageManager>(true);
            if (primarySM == null && iksirSatisAna != null)
            {
                primarySM = iksirSatisAna.GetComponentInChildren<StorageManager>(true);
            }
            if (primarySM == null && malzemeDukkani != null)
            {
                primarySM = malzemeDukkani.GetComponentInChildren<StorageManager>(true);
            }
            if (primarySM != null)
            {
                primarySM.gameObject.name = "StorageManager";
                primarySM.transform.SetParent(coreManagersRoot.transform, true);
                EditorUtility.SetDirty(primarySM.gameObject);
                EditorUtility.SetDirty(primarySM);
            }
            DestroyChildrenByName(malzemeDukkani, "StorageManager");
            DestroyChildrenByName(iksirYapmaDukkani, "StorageManager");

            // 6. Global UI & Sistemler
            TopDropdownMenu menu = null;
            if (topDropdownMenuObj != null)
            {
                topDropdownMenuObj.transform.SetParent(globalUIRoot.transform, true);
                topDropdownMenuObj.transform.localScale = Vector3.one;

                menu = topDropdownMenuObj.GetComponent<TopDropdownMenu>();
                if (menu != null)
                {
                    menu.gameCanvas = iksirSatisAna;
                    menu.mapCanvas = lobiSahnesi;
                    menu.AutoSetup();

                    // GameManager ile menüdeki metinleri bağla
                    if (primaryGM != null)
                    {
                        primaryGM.goldText = menu.goldText;
                        primaryGM.kadimParaText = menu.kadimParaText;
                        primaryGM.shopStatusText = menu.shopToggleButtonText;
                        primaryGM.UpdateUI();
                        EditorUtility.SetDirty(primaryGM);
                    }
                    EditorUtility.SetDirty(menu);
                }
                EditorUtility.SetDirty(topDropdownMenuObj);
            }

            if (eventSystemObj != null)
            {
                eventSystemObj.transform.SetParent(systemsRoot.transform, true);
                EditorUtility.SetDirty(eventSystemObj);
            }

            // 7. PotionTownSceneCoordinator Ekle ve Bağla
            GameObject coordinatorObj = GetOrCreateChild(coreManagersRoot, "PotionTownSceneCoordinator");
            var coordinator = coordinatorObj.GetComponent<PotionTownSceneCoordinator>();
            if (coordinator == null) coordinator = coordinatorObj.AddComponent<PotionTownSceneCoordinator>();

            coordinator.lobbyRoom = lobiSahnesi;
            coordinator.potionSellingRoom = iksirSatisAna;
            coordinator.potionCraftingRoom = iksirYapmaDukkani;
            coordinator.ingredientShopRoom = malzemeDukkani;

            coordinator.lobbyCamera = lobiCam;
            coordinator.potionSellingCamera = iksirSatisCam;
            coordinator.potionCraftingCamera = iksirYapmaCam;
            coordinator.ingredientShopCamera = malzemeCam;

            coordinator.gameManager = primaryGM;

            if (playerSystems != null) coordinator.playerInventory = playerSystems.GetComponentInChildren<PlayerInventory>(true);
            if (licenseManagerObj != null) coordinator.licenseManager = licenseManagerObj.GetComponent<LicenseManager>();
            coordinator.storageManager = primarySM;
            coordinator.topDropdownMenu = menu;
            coordinator.initialRoom = PotionTownRoom.Lobby;

            EditorUtility.SetDirty(coordinatorObj);
            EditorUtility.SetDirty(coordinator);

            // 8. Lobideki Binalara PotionTownBuildingTrigger Ekle ve Bağla
            SetupLobbyBuildingTriggers(lobiSahnesi, coordinator, lobiCam);

            // 9. CanvasScaler Standartlaştırması
            StandardizeCanvases();

            // 10. Sahneyi Kaydet
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=green>[PotionTownSceneBuilder]</color> Scene rebuilt and saved successfully: {saved}");
            return saved;
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (go.name == name) return go;
            }
            return new GameObject(name);
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static GameObject FindGameObjectInScene(string partialName)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name.Trim().Contains(partialName)) return r;
                foreach (Transform c in r.GetComponentsInChildren<Transform>(true))
                {
                    if (c.name.Trim().Contains(partialName)) return c.gameObject;
                }
            }
            return null;
        }

        private static Camera FindCameraInChildren(GameObject parent, string partialName)
        {
            if (parent == null) return null;
            var cams = parent.GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                if (c.name.Contains(partialName)) return c;
            }
            return cams.Length > 0 ? cams[0] : null;
        }

        private static void ResetTransformScale(GameObject go)
        {
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
            else
            {
                go.transform.localScale = Vector3.one;
            }
        }

        private static void DestroyChildrenByName(GameObject parent, string childName)
        {
            if (parent == null) return;
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.gameObject != parent && child.name == childName)
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                Debug.Log($"[PotionTownSceneBuilder] Destroying redundant child GameObject: {go.name} in {parent.name}");
                Object.DestroyImmediate(go);
            }
        }

        private static void DeduplicateEndOfDaySign(GameObject signRoot)
        {
            if (signRoot == null) return;

            var allSigns = signRoot.GetComponentsInChildren<EndOfDaySign>(true);
            if (allSigns.Length > 1)
            {
                for (int i = 1; i < allSigns.Length; i++)
                {
                    Debug.Log($"[PotionTownSceneBuilder] Removing duplicate EndOfDaySign component from {allSigns[i].gameObject.name}");
                    Object.DestroyImmediate(allSigns[i]);
                }
            }

            var mainSign = signRoot.GetComponent<EndOfDaySign>();
            if (mainSign != null)
            {
                var existingPanel = Object.FindFirstObjectByType<PotionShop.UI.EndOfDayPanel>(FindObjectsInactive.Include);
                if (existingPanel != null)
                {
                    mainSign.endOfDayPanel = existingPanel.gameObject;
                }
                else
                {
                    Transform panelTransform = signRoot.transform.Find("tabela/Gün sonu paneli") ?? 
                                               signRoot.transform.Find("Gün sonu paneli");
                    if (panelTransform != null)
                    {
                        mainSign.endOfDayPanel = panelTransform.gameObject;
                    }
                }
            }
        }

        private static void SetupLobbyBuildingTriggers(GameObject lobbyRoot, PotionTownSceneCoordinator coordinator, Camera lobbyCam)
        {
            if (lobbyRoot == null) return;

            Transform houses = lobbyRoot.transform.Find("Lobi/houses") ?? lobbyRoot.transform.Find("houses");
            if (houses == null)
            {
                foreach (Transform c in lobbyRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (c.name == "houses") { houses = c; break; }
                }
            }

            if (houses != null)
            {
                AttachTrigger(houses, "h-03", PotionTownRoom.PotionSelling, "İksir Satış Dükkanı", coordinator, lobbyCam);
                AttachTrigger(houses, "h-01", PotionTownRoom.PotionCrafting, "İksir Yapma Dükkanı", coordinator, lobbyCam);
                AttachTrigger(houses, "h-04(m-r)", PotionTownRoom.IngredientShop, "Malzeme Dükkanı", coordinator, lobbyCam);
            }
        }

        private static void AttachTrigger(Transform parent, string childName, PotionTownRoom room, string displayName, PotionTownSceneCoordinator coordinator, Camera lobbyCam)
        {
            Transform t = parent.Find(childName);
            if (t == null) return;

            var trigger = t.GetComponent<PotionTownBuildingTrigger>();
            if (trigger == null) trigger = t.gameObject.AddComponent<PotionTownBuildingTrigger>();

            trigger.targetRoom = room;
            trigger.buildingDisplayName = displayName;
            trigger.coordinator = coordinator;
            trigger.lobbyCamera = lobbyCam;

            var legacyHover = t.GetComponent<BuildingHover>();
            if (legacyHover != null)
            {
                legacyHover.enabled = false;
            }

            EditorUtility.SetDirty(trigger);
        }

        private static void StandardizeCanvases()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                    EditorUtility.SetDirty(scaler);
                }
            }
        }
    }
}
#endif
