using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PotionShop
{
    public class ServingArea : MonoBehaviour, IDropHandler
    {
        [Header("Servis Alanı Görseli (İsteğe Bağlı)")]
        public Transform servingTrayPoint; 

        // Üretilen iksir bu alana sürüklenip bırakıldığında tetiklenir
        public void OnDrop(PointerEventData eventData)
        {
            // Hazırlık evresindeyse servis yapılamaz
            if (GameManager.Instance != null && GameManager.Instance.isPrepPhase)
            {
                Debug.Log("Hazırlık evresinde servis yapılamaz.");
                return;
            }

            GameObject dropped = eventData.pointerDrag;
            if (dropped == null) return;

            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            
            if (draggableItem != null && draggableItem.itemData != null && !draggableItem.wasDeliveredThisDrag)
            {
                ItemData item = draggableItem.itemData;
                
                // Eşya özel raftan mı (SpecialsShelf) yoksa normal raftan mı sürüklendi?
                bool isFromSpecialShelf = dropped.GetComponent<SpecialShelfSlot>() != null || 
                                         (draggableItem.parentAfterDrag != null && draggableItem.parentAfterDrag.GetComponentInParent<SpecialShelfSlot>() != null) ||
                                         item.IsSpecialItem;

                bool isFromNormalShelf = draggableItem.isFromShelf || 
                                         (draggableItem.parentAfterDrag != null && draggableItem.parentAfterDrag.GetComponentInParent<ShelfSlot>() != null);

                bool isBuffPotion = item.specialEffect == SpecialItemEffect.SlowMoodDecay ||
                                    item.specialEffect == SpecialItemEffect.DoubleGold ||
                                    item.specialEffect == SpecialItemEffect.RushHour ||
                                    item.specialEffect == SpecialItemEffect.ChainBreaker;

                // 1) BUFF İKSİRLERİ: Servis alanına bırakıldığında etki anında devreye girer!
                // (Müşteriye verilmez, sadece genel dükkan buff'ını aktif eder)
                if (isBuffPotion)
                {
                    draggableItem.wasDeliveredThisDrag = true;
                    if (SpecialPotionManager.Instance != null)
                    {
                        float duration = item.effectDuration > 0f ? item.effectDuration : 300f;
                        SpecialPotionManager.Instance.ActivateEffect(item.specialEffect, duration, item.effectMultiplier);
                    }

                    // Envanterden 1 adet düş
                    if (PlayerInventory.Instance != null)
                    {
                        PlayerInventory.Instance.RemoveItem(item, 1);
                    }

                    Debug.Log($"<color=green>[Servis Alanı]</color> Özel iksir etkisi devreye girdi: {item.itemName}");

                    // Raftan geldiyse geri dönsün, sayaç güncellensin
                    if (isFromNormalShelf || isFromSpecialShelf)
                    {
                        if (draggableItem.parentAfterDrag != null)
                        {
                            draggableItem.transform.SetParent(draggableItem.parentAfterDrag);
                            RectTransform rt = draggableItem.GetComponent<RectTransform>();
                            if (rt != null) rt.anchoredPosition = Vector2.zero;
                        }

                        ShelfSlot slot = draggableItem.GetComponentInParent<ShelfSlot>();
                        if (slot == null && draggableItem.parentAfterDrag != null)
                        {
                            slot = draggableItem.parentAfterDrag.GetComponentInParent<ShelfSlot>();
                        }
                        if (slot != null) slot.UpdateSlotState();
                    }
                    else
                    {
                        Destroy(dropped);
                    }
                    return;
                }

                // 2) NORMAL VEYA JOKER İKSİRLER: Müşteriye teslim etmeyi dene
                bool wasAccepted = CustomerSpawner.Instance != null && CustomerSpawner.Instance.TryDeliverItem(item);

                if (wasAccepted)
                {
                    draggableItem.wasDeliveredThisDrag = true;
                    Debug.Log($"Servis başarılı: {item.itemName}");

                    if (PlayerInventory.Instance != null)
                    {
                        PlayerInventory.Instance.RemoveItem(item, 1);
                    }

                    if (isFromNormalShelf || isFromSpecialShelf)
                    {
                        if (draggableItem.parentAfterDrag != null)
                        {
                            draggableItem.transform.SetParent(draggableItem.parentAfterDrag);
                            RectTransform rt = draggableItem.GetComponent<RectTransform>();
                            if (rt != null) rt.anchoredPosition = Vector2.zero;
                        }

                        ShelfSlot slot = draggableItem.GetComponentInParent<ShelfSlot>();
                        if (slot == null && draggableItem.parentAfterDrag != null)
                        {
                            slot = draggableItem.parentAfterDrag.GetComponentInParent<ShelfSlot>();
                        }
                        if (slot != null) slot.UpdateSlotState();
                    }
                    else
                    {
                        Destroy(dropped);
                    }
                }
                else
                {
                    Debug.LogWarning("Müşteri bu iksiri istemiyor veya müşteri yok!");
                }
            }
        }
    }
}
