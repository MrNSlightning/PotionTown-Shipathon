#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using PotionShop.UI;

namespace PotionShop.Editor
{
    /// <summary>
    /// Topdownmenü Market butonları ve EndOfDayPanel butonlarının (Yeni Güne Başla, Kapat)
    /// Hiyerarşi ve Inspector referanslarını eksiksiz bağlayan merkezi kurulum aracı.
    /// </summary>
    [InitializeOnLoad]
    public static class UIConnectionSetupTool
    {
        private const string ScenePath = "Assets/_PotionTown/Scenes/PotionTown.unity";
        private const string PathTopDropdownMenuPrefab = "Assets/_PotionTown/Prefabs/UI/Menus/TopDropdownMenu.prefab";
        private const string PathEndOfDayPanelPrefab = "Assets/_PotionTown/Prefabs/UI/Menus/EndOfDayPanel.prefab";

        static UIConnectionSetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                RunFullConnectionSetup();
            };
        }

        [MenuItem("Tools/PotionTown/Connect All UI Hierarchy and Inspector", false, 1)]
        public static void RunFullConnectionSetup()
        {
            Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> Tüm UI hiyerarşi ve Inspector bağlantıları kuruluyor...");

            try
            {
                // 1. TopDropdownMenu Prefab Bağlantıları
                ConnectTopDropdownMenuPrefab();

                // 2. EndOfDayPanel Prefab Bağlantıları
                ConnectEndOfDayPanelPrefab();

                // 3. Sahne İçi Bağlantılar (PotionTown.unity)
                ConnectSceneInstances();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>[UIConnectionSetupTool] TEBRİKLER! Tüm Market ve EndOfDayPanel butonları hiyerarşi ve Inspector üzerinden başarıyla bağlandı!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIConnectionSetupTool] Bağlantı kurulumunda hata oluştu: {ex}");
            }
        }

        private static void ConnectTopDropdownMenuPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathTopDropdownMenuPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIConnectionSetupTool] TopDropdownMenu prefab bulunamadı: {PathTopDropdownMenuPrefab}");
                return;
            }

            TopDropdownMenu menu = prefab.GetComponent<TopDropdownMenu>();
            if (menu == null) return;

            Undo.RecordObject(menu, "Connect TopDropdownMenu Market Buttons");

            // GoldSlot Market Butonu
            Transform goldMarketT = prefab.transform.Find("MenuPanel/Panel/Coins/GoldSlot/Market") ??
                                   prefab.transform.Find("MenuPanel/Coins/GoldSlot/Market") ??
                                   prefab.transform.Find("MenuPanel/GoldSlot/Market");
            Button goldMarketBtn = goldMarketT != null ? goldMarketT.GetComponent<Button>() : null;

            // KadimSlot Market Butonu
            Transform kadimMarketT = prefab.transform.Find("MenuPanel/Panel/Coins/KadimSlot/Market") ??
                                    prefab.transform.Find("MenuPanel/Coins/KadimSlot/Market") ??
                                    prefab.transform.Find("MenuPanel/KadimSlot/Market");
            Button kadimMarketBtn = kadimMarketT != null ? kadimMarketT.GetComponent<Button>() : null;

            if (goldMarketBtn != null)
            {
                menu.goldMarketButton = goldMarketBtn;
                Undo.RecordObject(goldMarketBtn, "Hook Gold Market Button Persistent Listener");
                ClearPersistentCalls(goldMarketBtn.onClick);
                UnityEventTools.AddPersistentListener(goldMarketBtn.onClick, menu.OpenShopUI);
                goldMarketBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                EditorUtility.SetDirty(goldMarketBtn);
            }

            if (kadimMarketBtn != null)
            {
                menu.kadimMarketButton = kadimMarketBtn;
                Undo.RecordObject(kadimMarketBtn, "Hook Kadim Market Button Persistent Listener");
                ClearPersistentCalls(kadimMarketBtn.onClick);
                UnityEventTools.AddPersistentListener(kadimMarketBtn.onClick, menu.OpenShopUI);
                kadimMarketBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                EditorUtility.SetDirty(kadimMarketBtn);
            }

            EditorUtility.SetDirty(menu);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> TopDropdownMenu prefab'ındaki Market butonları ShopUI'a bağlandı.");
        }

        private static void ConnectEndOfDayPanelPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathEndOfDayPanelPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIConnectionSetupTool] EndOfDayPanel prefab bulunamadı: {PathEndOfDayPanelPrefab}");
                return;
            }

            EndOfDayPanel panel = prefab.GetComponent<EndOfDayPanel>();
            if (panel == null) return;

            Undo.RecordObject(panel, "Connect EndOfDayPanel Buttons");

            Transform winT = prefab.transform.Find("DialogWindow");
            if (winT != null)
            {
                // NextDayButton
                Transform nextBtnT = winT.Find("NextDayButton");
                if (nextBtnT != null)
                {
                    Button nextBtn = nextBtnT.GetComponent<Button>();
                    if (nextBtn != null)
                    {
                        panel.nextDayButton = nextBtn;
                        Undo.RecordObject(nextBtn, "Hook NextDayButton Persistent Listener");
                        ClearPersistentCalls(nextBtn.onClick);
                        UnityEventTools.AddPersistentListener(nextBtn.onClick, panel.ConfirmEndDay);
                        nextBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                        EditorUtility.SetDirty(nextBtn);
                    }
                }

                // CloseButton
                Transform closeBtnT = winT.Find("CloseButton");
                if (closeBtnT != null)
                {
                    Button closeBtn = closeBtnT.GetComponent<Button>();
                    if (closeBtn != null)
                    {
                        panel.closeButton = closeBtn;
                        Undo.RecordObject(closeBtn, "Hook CloseButton Persistent Listener");
                        ClearPersistentCalls(closeBtn.onClick);
                        UnityEventTools.AddPersistentListener(closeBtn.onClick, panel.ClosePanel);
                        closeBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                        EditorUtility.SetDirty(closeBtn);
                    }
                }
            }

            EditorUtility.SetDirty(panel);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> EndOfDayPanel prefab'ındaki Yeni Güne Başla ve Kapat butonları bağlandı.");
        }

        private static void ConnectSceneInstances()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                if (System.IO.File.Exists(ScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
            }

            var coordinator = UnityEngine.Object.FindFirstObjectByType<PotionTownSceneCoordinator>(FindObjectsInactive.Include);
            var startMenu = UnityEngine.Object.FindFirstObjectByType<GameStartMenuUI>(FindObjectsInactive.Include);
            var endOfDay = UnityEngine.Object.FindFirstObjectByType<EndOfDayPanel>(FindObjectsInactive.Include);
            var topMenu = UnityEngine.Object.FindFirstObjectByType<TopDropdownMenu>(FindObjectsInactive.Include);
            var shopUI = UnityEngine.Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);

            // 1. EndOfDayPanel Sahne Bağlantıları
            if (endOfDay != null)
            {
                Undo.RecordObject(endOfDay, "Connect Scene EndOfDayPanel References");
                endOfDay.sceneCoordinator = coordinator;
                endOfDay.gameStartMenuUI = startMenu;

                // Buton persistent listener'ları
                if (endOfDay.nextDayButton != null)
                {
                    Undo.RecordObject(endOfDay.nextDayButton, "Connect Scene NextDayButton");
                    ClearPersistentCalls(endOfDay.nextDayButton.onClick);
                    UnityEventTools.AddPersistentListener(endOfDay.nextDayButton.onClick, endOfDay.ConfirmEndDay);
                    endOfDay.nextDayButton.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                    EditorUtility.SetDirty(endOfDay.nextDayButton);
                }

                if (endOfDay.closeButton != null)
                {
                    Undo.RecordObject(endOfDay.closeButton, "Connect Scene CloseButton");
                    ClearPersistentCalls(endOfDay.closeButton.onClick);
                    UnityEventTools.AddPersistentListener(endOfDay.closeButton.onClick, endOfDay.ClosePanel);
                    endOfDay.closeButton.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                    EditorUtility.SetDirty(endOfDay.closeButton);
                }

                EditorUtility.SetDirty(endOfDay);
                Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> Sahnedeki EndOfDayPanel: SceneCoordinator ve GameStartMenuUI bağlandı.");
            }

            // 2. TopDropdownMenu Sahne Bağlantıları
            if (topMenu != null)
            {
                Undo.RecordObject(topMenu, "Connect Scene TopDropdownMenu References");

                Transform goldMarketT = topMenu.transform.Find("MenuPanel/Panel/Coins/GoldSlot/Market") ??
                                       topMenu.transform.Find("MenuPanel/Coins/GoldSlot/Market") ??
                                       topMenu.transform.Find("MenuPanel/GoldSlot/Market");
                if (goldMarketT != null)
                {
                    Button gBtn = goldMarketT.GetComponent<Button>();
                    if (gBtn != null)
                    {
                        topMenu.goldMarketButton = gBtn;
                        Undo.RecordObject(gBtn, "Connect Scene Gold Market Button");
                        ClearPersistentCalls(gBtn.onClick);
                        UnityEventTools.AddPersistentListener(gBtn.onClick, topMenu.OpenShopUI);
                        gBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                        EditorUtility.SetDirty(gBtn);
                    }
                }

                Transform kadimMarketT = topMenu.transform.Find("MenuPanel/Panel/Coins/KadimSlot/Market") ??
                                        topMenu.transform.Find("MenuPanel/Coins/KadimSlot/Market") ??
                                        topMenu.transform.Find("MenuPanel/KadimSlot/Market");
                if (kadimMarketT != null)
                {
                    Button kBtn = kadimMarketT.GetComponent<Button>();
                    if (kBtn != null)
                    {
                        topMenu.kadimMarketButton = kBtn;
                        Undo.RecordObject(kBtn, "Connect Scene Kadim Market Button");
                        ClearPersistentCalls(kBtn.onClick);
                        UnityEventTools.AddPersistentListener(kBtn.onClick, topMenu.OpenShopUI);
                        kBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                        EditorUtility.SetDirty(kBtn);
                    }
                }

                EditorUtility.SetDirty(topMenu);
                Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> Sahnedeki TopDropdownMenu: Market butonları bağlandı.");
            }

            // 3. PotionTownSceneCoordinator Sahne Bağlantıları
            if (coordinator != null)
            {
                Undo.RecordObject(coordinator, "Connect Coordinator UI References");
                if (endOfDay != null) coordinator.endOfDayPanel = endOfDay;
                if (startMenu != null) coordinator.startMenuUI = startMenu;
                if (topMenu != null) coordinator.topDropdownMenu = topMenu;
                if (shopUI != null) coordinator.shopUI = shopUI;
                EditorUtility.SetDirty(coordinator);
                Debug.Log("<color=cyan>[UIConnectionSetupTool]</color> Sahnedeki PotionTownSceneCoordinator: EndOfDayPanel, StartMenuUI, TopDropdownMenu ve ShopUI bağlandı.");
            }

            // Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=green>[UIConnectionSetupTool]</color> PotionTown sahnesi başarıyla kaydedildi.");
        }

        private static void ClearPersistentCalls(UnityEvent unityEvent)
        {
            if (unityEvent == null) return;
            while (unityEvent.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, 0);
            }
        }
    }
}
#endif
