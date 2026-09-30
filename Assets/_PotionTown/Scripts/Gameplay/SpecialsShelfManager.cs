using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// SpecialsShelf (Özel Eşyalar Rafı) kök objesine eklenen yönetici sınıftır.
    /// Altındaki tüm SpecialShelfSlot bileşenlerini takip eder ve envanter 
    /// değişikliklerinde tüm yuvaların gri/renkli durumunu otomatik senkronize eder.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class SpecialsShelfManager : MonoBehaviour
    {
        public static SpecialsShelfManager Instance { get; private set; }

        [Header("Özel Raf Yuvaları")]
        [Tooltip("Raftaki 6 sabit özel yuva. Boş bırakılırsa çocuk objelerden otomatik bulunur.")]
        [SerializeField] private List<SpecialShelfSlot> slots = new List<SpecialShelfSlot>();

        public IReadOnlyList<SpecialShelfSlot> Slots => slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            AutoDiscoverSlots();
        }

        private void OnEnable()
        {
            PlayerInventory.onInventoryChangedStatic += RefreshAllSlots;
            RefreshAllSlots();
        }

        private void OnDisable()
        {
            PlayerInventory.onInventoryChangedStatic -= RefreshAllSlots;
        }

        private void Start()
        {
            AutoDiscoverSlots();
            RefreshAllSlots();
        }

        /// <summary>
        /// Çocuk objelerdeki SpecialShelfSlot bileşenlerini otomatik bulur.
        /// </summary>
        [ContextMenu("Slotları Otomatik Keşfet")]
        public void AutoDiscoverSlots()
        {
            if (slots == null)
                slots = new List<SpecialShelfSlot>();

            slots.Clear();
            GetComponentsInChildren(true, slots);
            Debug.Log($"<color=cyan>[SpecialsShelfManager]</color> {slots.Count} adet özel yuva keşfedildi.");
        }

        /// <summary>
        /// Raftaki tüm yuvaların görsel durumunu günceller.
        /// </summary>
        [ContextMenu("Tüm Yuvaları Yenile")]
        public void RefreshAllSlots()
        {
            if (slots == null || slots.Count == 0)
            {
                AutoDiscoverSlots();
            }

            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.UpdateVisuals();
                }
            }
        }

        /// <summary>
        /// Belirli bir ItemData'ya ait olan raf yuvasını döndürür.
        /// </summary>
        public SpecialShelfSlot GetSlotForItem(ItemData data)
        {
            if (data == null || slots == null) return null;

            foreach (var slot in slots)
            {
                if (slot != null && slot.itemData == data)
                    return slot;
            }
            return null;
        }
    }
}
