using System;
using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Tüm mini oyunların temelini oluşturan soyut sınıf.
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        [Header("Mini Oyun Bilgileri")]
        public abstract string GameName { get; }
        public abstract string GameDescription { get; }

        [Header("Ödül Verileri")]
        [Tooltip("Oyun bitiminde verilecek ödüllerin ayarları.")]
        public MiniGameRewardData rewardData;

        [Header("UI Referansları")]
        [Tooltip("Oyun sırasında gösterilecek ana panel.")]
        public GameObject gamePanel;

        /// <summary>
        /// Oyun tamamlandığında fırlatılacak event (Skor ile birlikte).
        /// </summary>
        public event Action<int> OnGameCompleted;

        [Header("Oyun Ayarları")]
        [Tooltip("Oyunun süresi (saniye).")]
        [SerializeField] protected float gameDuration = 60f;

        /// <summary>
        /// Oyunun şu an oynanıp oynanmadığını belirtir.
        /// </summary>
        public bool IsPlaying { get; protected set; }

        /// <summary>
        /// Oyunu başlatır.
        /// </summary>
        public abstract void StartGame();

        /// <summary>
        /// Oyunu bitirir ve ödülleri dağıtır.
        /// </summary>
        public abstract void EndGame(bool won, int score);

        /// <summary>
        /// Skora ve rewardData'ya göre oyuncuya ödüllerini verir.
        /// </summary>
        protected virtual void GrantRewards(int score)
        {
            if (rewardData == null)
            {
                Debug.LogWarning("[MiniGame] RewardData atanmamış, ödül verilemiyor!");
                OnGameCompleted?.Invoke(score);
                return;
            }

            // Altın ödülü
            int totalGold = score * rewardData.goldPerPoint;
            if (totalGold > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(totalGold);
                Debug.Log($"[MiniGame] +{totalGold} Altın kazanıldı!");
            }

            // Kadim Para şansı
            if (UnityEngine.Random.Range(0, 100) < rewardData.kadimParaChance)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddKadimPara(rewardData.kadimParaAmount);
                    Debug.Log($"[MiniGame] +{rewardData.kadimParaAmount} Kadim Para kazanıldı!");
                }
            }

            // Malzeme düşürme şansı
            if (rewardData.possibleItemDrops != null && rewardData.possibleItemDrops.Length > 0)
            {
                if (UnityEngine.Random.Range(0, 100) < rewardData.itemDropChance)
                {
                    var randomItem = rewardData.possibleItemDrops[UnityEngine.Random.Range(0, rewardData.possibleItemDrops.Length)];
                    if (PlayerInventory.Instance != null && randomItem != null)
                    {
                        PlayerInventory.Instance.AddItem(randomItem, 1);
                        Debug.Log($"[MiniGame] Malzeme düştü: {randomItem.itemName}!");
                    }
                }
            }

            // Kasa düşürme şansı
            if (UnityEngine.Random.Range(0, 100) < rewardData.lootBoxChance)
            {
                LootBoxSystem.AddLootBox(LootBoxTier.Bronze);
                Debug.Log("[MiniGame] Bronz Kasa düştü!");
            }

            IsPlaying = false;
            OnGameCompleted?.Invoke(score);
        }
    }
}
