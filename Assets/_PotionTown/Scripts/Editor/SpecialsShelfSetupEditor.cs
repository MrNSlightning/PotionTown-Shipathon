#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace PotionShop.Editor
{
    /// <summary>
    /// SpecialsShelf (Özel Eşyalar Kullanım Rafı) otomasyon aracı.
    /// Sahnede İksirSatışDükkanı altındaki SpecialsShelf objesini (yoksa otomatik oluşturarak)
    /// ve 6 özel yuvayı (SpecialShelfSlot) GUID ile güvenli bağlar.
    /// </summary>
    public static class SpecialsShelfSetupEditor
    {
        private static readonly (string guid, string fallbackPath)[] SpecialItemGuids = new (string, string)[]
        {
            ("d0269e3e686788242a91a53c8d5f508f", "Assets/_PotionTown/Data/Items/Special Potions/ShadowIngredient.asset"),
            ("a97d7f52883f7c84fa890e4b572e0777", "Assets/_PotionTown/Data/Items/Special Potions/Burn.asset"),
            ("444559935d599414092b1080f1b555cf", "Assets/_PotionTown/Data/Items/Special Potions/Curse.asset"),
            ("da005d9b30a217243ad36a1d027d2c96", "Assets/_PotionTown/Data/Items/Special Potions/Rage.asset"),
            ("44e70cc217770b946b9c384072b52d7a", "Assets/_PotionTown/Data/Items/Special Potions/Runner.asset"),
            ("54f0c18a65d3a0843b42a5e05c4e1c90", "Assets/_PotionTown/Data/Items/Special Potions/Dispel.asset")
        };

        [MenuItem("Potion Shop/SpecialsShelf Kurulumunu Yap ve Güncelle", priority = 150)]
        public static void SetupSpecialsShelfMenu()
        {
            SetupSpecialsShelf(true);
        }

        public static bool SetupSpecialsShelf(bool showDialog = false)
        {
            GameObject shelfRoot = FindOrCreateSpecialsShelf();
            if (shelfRoot == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Hata", 
                        "Sahnede 'İksirSatisDükkani' veya 'SpecialsShelf' bulunamadı!\nLütfen SampleScene sahnesinin açık olduğundan emin olun.", "Tamam");
                }
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(shelfRoot, "Setup SpecialsShelf");

            // 1. İsim standardizasyonu
            if (shelfRoot.name != "SpecialsShelf")
            {
                shelfRoot.name = "SpecialsShelf";
                Debug.Log("<color=green>[SpecialsShelf]</color> Kök obje adı 'SpecialsShelf' olarak güncellendi.");
            }

            // 2. SpecialsShelfManager bileşeni
            SpecialsShelfManager manager = shelfRoot.GetComponent<SpecialsShelfManager>();
            if (manager == null)
            {
                manager = Undo.AddComponent<SpecialsShelfManager>(shelfRoot);
                Debug.Log("<color=green>[SpecialsShelf]</color> SpecialsShelfManager bileşeni eklendi.");
            }

            // 3. Özel Eşya ScriptableObject'lerini GUID ile güvenli yükle
            ItemData[] assignedItems = new ItemData[SpecialItemGuids.Length];
            for (int i = 0; i < SpecialItemGuids.Length; i++)
            {
                assignedItems[i] = LoadItem(SpecialItemGuids[i].guid, SpecialItemGuids[i].fallbackPath);
                if (assignedItems[i] != null)
                {
                    Debug.Log($"<color=cyan>[SpecialsShelf]</color> Eşya {i} yüklendi: {assignedItems[i].itemName}");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[SpecialsShelf]</color> Eşya {i} bulunamadı! GUID: {SpecialItemGuids[i].guid}");
                }
            }

            // 4. Eğer çocuk obje sayısı 6'dan azsa eksik olanları oluştur
            int existingChildCount = shelfRoot.transform.childCount;
            for (int i = existingChildCount; i < 6; i++)
            {
                ItemData item = (i < assignedItems.Length) ? assignedItems[i] : null;
                string slotName = (item != null) ? $"Slot_{item.itemName}" : $"Slot_{i + 1}";

                GameObject slotGO = new GameObject(slotName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(SpecialShelfSlot));
                Undo.RegisterCreatedObjectUndo(slotGO, "Create Special Slot");
                slotGO.transform.SetParent(shelfRoot.transform, false);

                RectTransform rt = slotGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90, 90);
                rt.anchoredPosition = new Vector2(-250 + i * 100, 0);

                Image img = slotGO.GetComponent<Image>();
                img.preserveAspect = true;
                if (item != null && item.itemIcon != null)
                    img.sprite = item.itemIcon;

                SpecialShelfSlot slotComp = slotGO.GetComponent<SpecialShelfSlot>();
                slotComp.itemData = item;
                slotComp.iconImage = img;
            }

            // 5. Mevcut ve yeni tüm çocukları kontrol et ve bağla
            int configuredCount = 0;
            int totalChildren = shelfRoot.transform.childCount;
            for (int i = 0; i < totalChildren; i++)
            {
                Transform child = shelfRoot.transform.GetChild(i);
                if (child == null) continue;

                // Yanlışlıkla ShopItemButton varsa kaldır (Bu alan satın alım değil, kullanım alanıdır)
                ShopItemButton oldShopBtn = child.GetComponent<ShopItemButton>();
                if (oldShopBtn != null)
                {
                    Undo.DestroyObjectImmediate(oldShopBtn);
                }

                // CanvasGroup ekle (Sürükleme için zorunlu)
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = Undo.AddComponent<CanvasGroup>(child.gameObject);
                }

                // SpecialShelfSlot ekle
                SpecialShelfSlot slot = child.GetComponent<SpecialShelfSlot>();
                if (slot == null)
                {
                    slot = Undo.AddComponent<SpecialShelfSlot>(child.gameObject);
                }

                // ItemData ata (eğer atanmamışsa sıra numarasına göre ata)
                if (slot.itemData == null && i < assignedItems.Length && assignedItems[i] != null)
                {
                    slot.itemData = assignedItems[i];
                }

                // Image ata
                Image img = child.GetComponent<Image>();
                if (img != null)
                {
                    img.preserveAspect = true;
                    if (slot.itemData != null && slot.itemData.itemIcon != null)
                    {
                        img.sprite = slot.itemData.itemIcon;
                    }
                    slot.iconImage = img;
                }

                // Görsel güncelle
                slot.UpdateVisuals();
                configuredCount++;
            }

            // 6. Manager'a slotları tanıt
            manager.AutoDiscoverSlots();
            manager.RefreshAllSlots();

            // Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(shelfRoot.scene);
            EditorSceneManager.SaveScene(shelfRoot.scene);

            string msg = $"SpecialsShelf kurulumu başarıyla tamamlandı!\nToplam {configuredCount} yuva bağlandı.";
            Debug.Log($"<color=green><b>[BAŞARILI]</b></color> {msg}");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("SpecialsShelf Kurulumu", msg, "Harika");
            }

            return true;
        }

        private static ItemData LoadItem(string guid, string fallbackPath)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item != null) return item;
                }
            }

            if (!string.IsNullOrEmpty(fallbackPath))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(fallbackPath);
                if (item != null) return item;
            }

            return null;
        }

        private static GameObject FindOrCreateSpecialsShelf()
        {
            // 1. Önce sahnede MalzemeDükkanı altında OLMAYAN bir SpecialsShelf var mı bak
            var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name == "SpecialsShelf" || go.name == "Specials")
                {
                    Transform p = go.transform.parent;
                    bool isUnderMalzeme = false;
                    while (p != null)
                    {
                        if (p.name.Contains("MalzemeDükkan") || p.name.Contains("Malzeme"))
                        {
                            isUnderMalzeme = true;
                            break;
                        }
                        p = p.parent;
                    }

                    if (!isUnderMalzeme)
                        return go;
                }
            }

            // 2. Eğer yoksa, İksirSatışDükkanı'nı bul ve altında SpecialsShelf oluştur
            GameObject iksirSatis = null;
            foreach (var go in allGOs)
            {
                if (go.name.Contains("İksirSatis") || go.name.Contains("İksirSatış") || go.name.Contains("IksirSatis"))
                {
                    iksirSatis = go;
                    break;
                }
            }

            if (iksirSatis != null)
            {
                GameObject newShelf = new GameObject("SpecialsShelf", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(newShelf, "Create SpecialsShelf");
                newShelf.transform.SetParent(iksirSatis.transform, false);

                RectTransform rt = newShelf.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 100);
                rt.anchoredPosition = new Vector2(0, -200);

                Debug.Log("<color=green>[SpecialsShelf]</color> 'İksirSatisDükkani' altında yeni 'SpecialsShelf' nesnesi oluşturuldu.");
                return newShelf;
            }

            return null;
        }
    }
}
#endif
