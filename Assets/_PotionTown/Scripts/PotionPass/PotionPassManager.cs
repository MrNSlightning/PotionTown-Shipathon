using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotionShop
{
    public class PotionPassManager : MonoBehaviour
    {
        public static PotionPassManager Instance { get; private set; }

        public static bool IsInitialized => Instance != null;

        [Header("Veri")]
        [Tooltip("Resources klasöründen yüklenecek olan bilet verisi.")]
        public PotionPassData passData;

        // Statik durum değişkenleri (İsteğe göre save sistemine entegre edilebilir)
        private static int _currentTier = 0;
        private static int _currentXP = 0;
        private static bool _isPremium = false;
        private static HashSet<int> _claimedFreeTiers = new HashSet<int>();
        private static HashSet<int> _claimedPremiumTiers = new HashSet<int>();

        // Etkinlikler (Events)
        public event Action<int> OnXPGained;
        public event Action<int> OnTierUp;
        public event Action<int, bool> OnRewardClaimed;

        public int CurrentTier => _currentTier;
        public int CurrentXP => _currentXP;
        public bool IsPremium => _isPremium;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Eğer inspector'dan atanmamışsa, Resources'dan yüklemeyi deneriz.
            if (passData == null)
            {
                passData = Resources.Load<PotionPassData>("PotionPassData");
            }

            if (passData == null)
            {
                Debug.Log("[PotionPassManager] Resources/PotionPassData bulunamadı. 10 aşamalı varsayılan bilet verisi dinamik oluşturuluyor.");
                passData = CreateDefaultPassData();
            }
        }

        private PotionPassData CreateDefaultPassData()
        {
            var data = ScriptableObject.CreateInstance<PotionPassData>();
            data.seasonName = "SEZON 1: SİMYACININ YOLU";
            data.totalTiers = 10;
            data.xpPerTier = 200;
            data.premiumPriceKadimPara = 50;

            for (int i = 1; i <= 10; i++)
            {
                // Free Track
                PassRewardType freeType = (i % 3 == 0) ? PassRewardType.LootBox : (i == 8 ? PassRewardType.KadimPara : PassRewardType.Gold);
                int freeAmount = freeType == PassRewardType.Gold ? i * 100 : (freeType == PassRewardType.LootBox ? 1 : 2);
                string freeDesc = freeType == PassRewardType.Gold ? $"+{freeAmount} Altın" : (freeType == PassRewardType.LootBox ? "1x Bronz Kasa" : $"+{freeAmount} Kadim Para");

                data.freeTrackRewards.Add(new PassTierReward
                {
                    tier = i,
                    rewardType = freeType,
                    amount = freeAmount,
                    rewardDescription = freeDesc
                });

                // Premium Track
                PassRewardType premType = (i % 3 == 0) ? PassRewardType.LootBox : (i % 2 == 0 ? PassRewardType.KadimPara : PassRewardType.Gold);
                int premAmount = premType == PassRewardType.Gold ? i * 250 : (premType == PassRewardType.KadimPara ? i * 5 : 1);
                string premDesc = premType == PassRewardType.Gold ? $"+{premAmount} Altın" : (premType == PassRewardType.KadimPara ? $"+{premAmount} Kadim Para" : "1x Altın Kasa");

                data.premiumTrackRewards.Add(new PassTierReward
                {
                    tier = i,
                    rewardType = premType,
                    amount = premAmount,
                    rewardDescription = premDesc
                });
            }

            return data;
        }

        /// <summary>
        /// Deneyim puanı (XP) ekler ve gerekiyorsa aşama (tier) atlatır.
        /// </summary>
        public void AddXP(int amount)
        {
            if (passData == null) return;
            if (_currentTier >= passData.totalTiers) return; // Maksimum aşamaya ulaşıldı

            _currentXP += amount;
            OnXPGained?.Invoke(amount);

            while (_currentXP >= passData.xpPerTier && _currentTier < passData.totalTiers)
            {
                _currentXP -= passData.xpPerTier;
                _currentTier++;
                OnTierUp?.Invoke(_currentTier);
            }

            // Maksimum seviyeye ulaştıysak ve fazla XP kaldıysa, onu maksimum değerde tut
            if (_currentTier >= passData.totalTiers)
            {
                _currentXP = passData.xpPerTier;
            }
        }

        /// <summary>
        /// Belirli bir aşama için ödülün alınıp alınamayacağını kontrol eder.
        /// </summary>
        public bool CanClaimReward(int tier, bool isPremiumTrack)
        {
            if (tier > _currentTier) return false; // Henüz o aşamaya ulaşılmadı

            if (isPremiumTrack)
            {
                if (!_isPremium) return false; // Premium bilet alınmamış
                return !_claimedPremiumTiers.Contains(tier); // Zaten alınmış mı?
            }
            else
            {
                return !_claimedFreeTiers.Contains(tier); // Zaten alınmış mı?
            }
        }

        /// <summary>
        /// Ödülü alır.
        /// </summary>
        public void ClaimReward(int tier, bool isPremiumTrack)
        {
            if (!CanClaimReward(tier, isPremiumTrack)) return;

            PassTierReward rewardToGrant = null;

            if (isPremiumTrack)
            {
                rewardToGrant = passData.premiumTrackRewards.Find(r => r.tier == tier);
                if (rewardToGrant != null)
                {
                    _claimedPremiumTiers.Add(tier);
                }
            }
            else
            {
                rewardToGrant = passData.freeTrackRewards.Find(r => r.tier == tier);
                if (rewardToGrant != null)
                {
                    _claimedFreeTiers.Add(tier);
                }
            }

            if (rewardToGrant != null)
            {
                GrantReward(rewardToGrant);
                OnRewardClaimed?.Invoke(tier, isPremiumTrack);
            }
        }

        /// <summary>
        /// Premium bileti aktif hale getirir.
        /// </summary>
        public void UpgradeToPremium()
        {
            if (_isPremium) return;
            _isPremium = true;
            Debug.Log("Premium Bilet aktif edildi!");
        }

        /// <summary>
        /// Mevcut aşamanın ilerleme durumunu 0.0 ile 1.0 arasında bir değer olarak döndürür.
        /// </summary>
        public float GetTierProgress()
        {
            if (passData == null) return 0f;
            if (_currentTier >= passData.totalTiers) return 1f;

            return (float)_currentXP / passData.xpPerTier;
        }

        /// <summary>
        /// Ödülü oyuncuya verir.
        /// </summary>
        private void GrantReward(PassTierReward reward)
        {
            switch (reward.rewardType)
            {
                case PassRewardType.Gold:
                    // Oyunun ana yöneticisinden altın eklendiği varsayılıyor
                    if (GameManager.Instance != null)
                        GameManager.Instance.AddGold(reward.amount);
                    break;

                case PassRewardType.KadimPara:
                    if (GameManager.Instance != null)
                        GameManager.Instance.AddKadimPara(reward.amount);
                    break;

                case PassRewardType.Item:
                    if (PlayerInventory.Instance != null && reward.itemReward != null)
                        PlayerInventory.Instance.AddItem(reward.itemReward, Mathf.Max(1, reward.amount));
                    break;

                case PassRewardType.Character:
                    Debug.Log($"Karakter açıldı: {reward.characterPrefab?.name}");
                    // Özel karakter açma mantığı buraya gelebilir
                    break;

                case PassRewardType.ShelfUnlock:
                    Debug.Log($"Raf açıldı: {reward.shelfId}");
                    // Raf açma mantığı buraya gelebilir
                    break;

                case PassRewardType.LootBox:
                    LootBoxTier boxTier = reward.amount >= 2 ? LootBoxTier.Gold : (reward.tier >= 5 ? LootBoxTier.Silver : LootBoxTier.Bronze);
                    LootBoxSystem.AddLootBox(boxTier);
                    Debug.Log($"Ganimet Kutusu eklendi! Seviye: {boxTier}");
                    break;
            }
        }
    }
}
