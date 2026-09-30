using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace PotionShop
{
    /// <summary>
    /// İksir yapma dükkanındaki raflara 10 sabit eşya yerleştirir.
    /// Her eşya belirli bir raf slotuna (ShelfSlot) Inspector'dan atanır.
    /// Envanterde varsa: renkli ve kullanılabilir.
    /// Envanterde yoksa: gri ve sürüklenemez (ShelfSlot + DraggableItem bunu zaten kontrol eder).
    /// 
    /// KURULUM:
    /// 1. Sahnenizde boş bir GameObject oluşturun, adını "FixedShelfManager" yapın.
    /// 2. Bu scripti o objeye ekleyin.
    /// 3. Inspector'daki "Fixed Items" listesine 10 adet eleman ekleyin.
    /// 4. Her elemana: İlgili ItemData (ScriptableObject) ve hedef ShelfSlot referansını sürükleyin.
    /// 5. Play'e bastığınızda her slot otomatik olarak atanmış eşyasını gösterecek
    ///    ve envantere göre renkli/gri olacaktır.
    /// </summary>
    public class FixedShelfManager : MonoBehaviour
    {
        [Serializable]
        public class FixedShelfEntry
        {
            [Tooltip("Bu rafa sabit olarak yerleştirilecek eşya (ItemData ScriptableObject)")]
            public ItemData item;

            [Tooltip("Eşyanın yerleştirileceği raf slotu (Sahnedeki ShelfSlot objesi)")]
            public ShelfSlot targetSlot;
        }

        [Header("Sabit Raf Eşyaları (10 Adet)")]
        [Tooltip("Her bir raf slotuna sabit olarak atanacak eşya listesi")]
        public List<FixedShelfEntry> fixedItems = new List<FixedShelfEntry>();

        /// <summary>
        /// Sabit raflara atanmış tüm eşyaların kayıt seti.
        /// Envanter UI'da bu eşyalar gizlenir (zaten İksirYapmaDükkanı'ndaki slotlarında görünürler).
        /// </summary>
        private static readonly HashSet<ItemData> _fixedShelfItems = new HashSet<ItemData>();
        private static bool _isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            _fixedShelfItems.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Sabit raf sistemini oda henüz aktif olmasa dahi erkenden bulup ilklendirir.
        /// </summary>
        public static void EnsureInitializedStatic()
        {
            if (_isInitialized && _fixedShelfItems.Count > 0) return;

            FixedShelfManager manager = FindFirstObjectByType<FixedShelfManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                manager.AssignAllFixedItems();
            }
        }

        /// <summary>
        /// Verilen eşya herhangi bir sabit raf slotuna atanmış mı?
        /// </summary>
        public static bool IsFixedShelfItem(ItemData item)
        {
            if (item == null) return false;

            if (!_isInitialized || _fixedShelfItems.Count == 0)
            {
                EnsureInitializedStatic();
            }

            return _fixedShelfItems.Contains(item);
        }

        private void Awake()
        {
            EnsureInitializedStatic();
        }

        private void Start()
        {
            EnsureInitializedStatic();

            // Envanter değiştiğinde tüm slotları güncelle
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshAllSlots;
                PlayerInventory.Instance.OnInventoryChanged += RefreshAllSlots;
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.OnInventoryChanged -= RefreshAllSlots;
                }
            }
            catch (System.Exception)
            {
                // PlayerInventory zaten yok edilmiş olabilir
            }
        }

        /// <summary>
        /// Tüm sabit eşyaları hedef slotlarına atar.
        /// Her slotu açık (unlocked) yapar ve eşyayı assign eder.
        /// </summary>
        public void AssignAllFixedItems()
        {
            _fixedShelfItems.Clear();

            for (int i = 0; i < fixedItems.Count; i++)
            {
                FixedShelfEntry entry = fixedItems[i];

                if (entry == null || entry.item == null || entry.targetSlot == null)
                {
                    Debug.LogWarning($"[FixedShelfManager] {i}. sıradaki eşya veya slot atanmamış! Inspector'dan kontrol edin.");
                    continue;
                }

                // Bu eşyayı sabit raf seti olarak kaydet (Envanter UI'da gizlenecek)
                _fixedShelfItems.Add(entry.item);

                // Slotu aç, sabit olarak işaretle ve eşyayı ata
                entry.targetSlot.isFixedSlot = true;
                entry.targetSlot.isUnlocked = true;

                // Slotun arka plan tıklamasını kapat (komşu rafların tıklanmasını engellemesin)
                Image bg = entry.targetSlot.GetComponent<Image>();
                if (bg != null) bg.raycastTarget = false;

                // Slotun yüksekliğini dikey raf aralığına uygun hale getir (Dikey çakışmayı önle)
                RectTransform rt = entry.targetSlot.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.y > 48f)
                {
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 44f);
                }

                entry.targetSlot.AssignItem(entry.item, notifyInventory: false);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Envanter değiştiğinde tüm sabit slotların görselini günceller.
        /// (ShelfSlot.UpdateSlotState zaten gri/renkli mantığını içerir)
        /// </summary>
        private void RefreshAllSlots()
        {
            // Yok edilmiş manager üzerinde çağrılmasını engelle
            if (this == null) return;

            foreach (var entry in fixedItems)
            {
                if (entry != null && entry.targetSlot != null)
                {
                    entry.targetSlot.UpdateSlotState();
                }
            }
        }

        /// <summary>
        /// Editor'da doğrulama: 10'dan fazla veya az eleman olduğunda uyarı verir.
        /// </summary>
        private void OnValidate()
        {
            if (fixedItems.Count > 0 && fixedItems.Count != 10)
            {
                Debug.LogWarning($"[FixedShelfManager] Listede {fixedItems.Count} eşya var, 10 olması önerilir.");
            }
        }
    }
}
