using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    public class ShelfManager : MonoBehaviour
    {
        [Header("Raf Ayarları")]
        [Tooltip("Sırasıyla sol (0-8) ve sağ (9-17) rafları ekleyin veya ayrı scriptler kullanın")]
        public List<ShelfSlot> allSlots;
        
        [Header("Otomatik Fiyatlandırma")]
        public bool autoAssignCosts = false;
        public int startingCost = 50;
        public int costIncrement = 50;
        
        private void Start()
        {
            if (autoAssignCosts && allSlots.Count > 0)
            {
                AssignCosts();
            }
        }

        private void AssignCosts()
        {
            for (int i = 0; i < allSlots.Count; i++)
            {
                ShelfSlot slot = allSlots[i];
                if (slot == null) continue; // Boş (None) bırakılmış slotlar hataya sebep olmasın
                
                // İlk raf (Index 0) her zaman açık
                if (i == 0)
                {
                    slot.isUnlocked = true;
                    slot.unlockType = UnlockType.Free;
                    slot.unlockCost = 0;
                }
                else if (i >= 1 && i <= 5)
                {
                    // 2-6. raflar altınla açılır (Index 1..5)
                    slot.unlockType = UnlockType.Gold;
                    slot.unlockCost = startingCost + (costIncrement * (i - 1));
                }
                else if (i == 6)
                {
                    // 7. raf (Index 6) Reklam izleyerek
                    slot.unlockType = UnlockType.Ad;
                }
                else if (i == 7 || i == 8)
                {
                    // 8-9. raflar (Index 7..8) Slot Mekaniği ile
                    slot.unlockType = UnlockType.SlotMechanic;
                }
                // Eğer liste 9'dan büyükse (örneğin sol ve sağ 18 rafı tek listede topladıysa)
                // İkinci 9'lu set için de aynı mantığı mod(9) alarak uygulayabiliriz.
                else
                {
                    int indexInGroup = i % 9;
                    if (indexInGroup == 0)
                    {
                        slot.isUnlocked = true;
                        slot.unlockType = UnlockType.Free;
                    }
                    else if (indexInGroup >= 1 && indexInGroup <= 5)
                    {
                        slot.unlockType = UnlockType.Gold;
                        slot.unlockCost = startingCost + (costIncrement * (indexInGroup - 1));
                    }
                    else if (indexInGroup == 6)
                    {
                        slot.unlockType = UnlockType.Ad;
                    }
                    else
                    {
                        slot.unlockType = UnlockType.SlotMechanic;
                    }
                }

                slot.UpdateSlotState();
            }
        }
    }
}
