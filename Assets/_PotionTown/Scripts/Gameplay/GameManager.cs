using UnityEngine;
using System;
using TMPro;

namespace PotionShop
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Ortak ve kalıcı ekonomi verileri (Oda ve sahne geçişlerinde sıfırlanmaz)
        private static int _sharedGold = 5000;
        private static int _sharedKadimPara = 0;
        private static int _sharedReputation = 100;
        private static bool _isEconomyInitialized = false;

        public static int SharedGold => _sharedGold;
        public static int SharedKadimPara => _sharedKadimPara;
        public static int SharedVipTickets => _sharedKadimPara;
        public static int SharedReputation => _sharedReputation;

        [Header("Ekonomi (Başlangıç Değerleri)")]
        public int initialGold = 5000;
        [UnityEngine.Serialization.FormerlySerializedAs("initialVipTickets")]
        public int initialKadimPara = 0;

        [Header("UI Referansları")]
        public TextMeshProUGUI goldText;
        [UnityEngine.Serialization.FormerlySerializedAs("vipTicketText")]
        public TextMeshProUGUI kadimParaText;
        public GameObject startButtonUI; // Hazırlığı bitirip oyunu başlatan buton

        // Anlık Miktarlar (Ortak statik veriye bağlıdır)
        public int CurrentGold => _sharedGold;
        public int CurrentKadimPara => _sharedKadimPara;
        public int CurrentVipTickets => _sharedKadimPara;
        
        [Header("İtibar (Puan) Sistemi")]
        public int initialReputation = 100;
        public int CurrentReputation => _sharedReputation;

        [Header("Günlük İstatistikler")]
        public int dailyTotalCustomers = 0;
        public int dailyHappy = 0;
        public int dailyNormal = 0;
        public int dailyGrumpy = 0;
        public int dailyAngry = 0;
        public int dailyLeft = 0;
        public int dailyEarnedGold = 0;
        public int dailyLostGold = 0;
        public int DailyNetProfit => dailyEarnedGold - dailyLostGold;

        [Header("Dükkan Durumu")]
        public bool isShopOpen = false; // Dükkan açık mı?
        public bool isPrepPhase => !isShopOpen; // Diğer scriptlerin hata vermemesi için geriye dönük uyumluluk
        public TextMeshProUGUI shopStatusText; // Butonun üstündeki yazı (Aç/Kapat)

        // Ortak Statik Değişim Eventleri
        public static event Action<int> OnGoldChangedStatic;
        public static event Action<int> OnKadimParaChangedStatic;
        public static event Action<int> OnReputationChangedStatic;
        public static event Action<bool> OnShopStatusChangedStatic;

        // Geriye dönük uyumluluk için instance eventleri
        public event Action<int> OnGoldChanged
        {
            add => OnGoldChangedStatic += value;
            remove => OnGoldChangedStatic -= value;
        }

        public event Action<int> OnKadimParaChanged
        {
            add => OnKadimParaChangedStatic += value;
            remove => OnKadimParaChangedStatic -= value;
        }

        public event Action<int> OnReputationChanged
        {
            add => OnReputationChangedStatic += value;
            remove => OnReputationChangedStatic -= value;
        }

        public event Action<bool> OnShopStatusChanged; // Dükkan açıldı/kapandı habercisi

        private void Awake()
        {
            Instance = this;

            if (!_isEconomyInitialized)
            {
                _isEconomyInitialized = true;
                var config = GameBalanceConfig.Instance;
                _sharedGold = config != null ? config.startingGold : initialGold;
                _sharedKadimPara = config != null ? config.startingKadimPara : initialKadimPara;
                _sharedReputation = config != null ? config.startingReputation : initialReputation;
            }

            isShopOpen = false; // Başlangıçta dükkan kapalı
            AutoAttachParaveElmasUI();
        }

        private void OnEnable()
        {
            Instance = this;
            OnGoldChangedStatic += HandleGoldChangedStatic;
            OnKadimParaChangedStatic += HandleKadimParaChangedStatic;
            UpdateUI();
            AutoAttachParaveElmasUI();
        }

        private void OnDisable()
        {
            OnGoldChangedStatic -= HandleGoldChangedStatic;
            OnKadimParaChangedStatic -= HandleKadimParaChangedStatic;
        }

        private void HandleGoldChangedStatic(int newGold)
        {
            if (goldText != null) goldText.text = newGold.ToString();
        }

        private void HandleKadimParaChangedStatic(int newKadimPara)
        {
            if (kadimParaText != null) kadimParaText.text = newKadimPara.ToString();
        }

        
        private void Start()
        {
            UpdateUI();
            UpdateShopButtonUI();
        }


        public void ModifyReputation(int amount)
        {
            _sharedReputation += amount;
            
            // İtibar 0'ın altına düşmesin
            if (_sharedReputation < 0) _sharedReputation = 0;
            
            OnReputationChangedStatic?.Invoke(_sharedReputation);
            Debug.Log($"İtibar değişti: {amount}. Yeni İtibar: {_sharedReputation}");
        }

        // Dükkanı açıp kapatan metod (Butona atanacak)
        public void ToggleShop()
        {
            isShopOpen = !isShopOpen;
            
            UpdateShopButtonUI();
            OnShopStatusChanged?.Invoke(isShopOpen);
            OnShopStatusChangedStatic?.Invoke(isShopOpen);
            ShelfSlot.UpdateAllShelfSlots();
            
            if (isShopOpen)
            {
                Debug.Log("Dükkan AÇILDI! Müşteriler gelebilir.");
            }
            else
            {
                Debug.Log("Dükkan KAPATILDI! Yeni müşteri gelmeyecek.");
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopCauldronBoiling(0f);
                }
            }
        }
        
        private void UpdateShopButtonUI()
        {
            if (shopStatusText != null)
            {
                shopStatusText.text = isShopOpen ? "Dükkanı Kapat" : "Dükkanı Aç";
            }
        }

        public void AddGold(int amount)
        {
            _sharedGold += amount;
            UpdateUI();
            OnGoldChangedStatic?.Invoke(_sharedGold);
        }

        public bool SpendGold(int amount)
        {
            if (_sharedGold >= amount)
            {
                _sharedGold -= amount;
                UpdateUI();
                OnGoldChangedStatic?.Invoke(_sharedGold);
                return true;
            }
            return false;
        }

        public void AddKadimPara(int amount)
        {
            _sharedKadimPara += amount;
            UpdateUI();
            OnKadimParaChangedStatic?.Invoke(_sharedKadimPara);
        }

        public bool SpendKadimPara(int amount)
        {
            if (_sharedKadimPara >= amount)
            {
                _sharedKadimPara -= amount;
                UpdateUI();
                OnKadimParaChangedStatic?.Invoke(_sharedKadimPara);
                return true;
            }
            return false;
        }

        // Geriye dönük uyumluluk için alias metotlar
        public void AddVipTicket(int amount) => AddKadimPara(amount);
        public bool SpendVipTicket(int amount) => SpendKadimPara(amount);

        public void UpdateUI()
        {
            if (goldText != null)
                goldText.text = _sharedGold.ToString();
            
            if (kadimParaText != null)
                kadimParaText.text = _sharedKadimPara.ToString();
        }

        /// <summary>
        /// Müşteri istatistiğini kaydeder (Customer.cs tarafından çağrılır).
        /// </summary>
        public void RecordCustomerStat(Customer.Mood mood)
        {
            switch (mood)
            {
                case Customer.Mood.Mutlu:
                    dailyHappy++;
                    break;
                case Customer.Mood.Normal:
                    dailyNormal++;
                    break;
                case Customer.Mood.Huysuz:
                    dailyGrumpy++;
                    break;
                case Customer.Mood.Ofkeli:
                case Customer.Mood.Gitti:
                default:
                    dailyAngry++;
                    dailyLeft++;
                    break;
            }

            dailyTotalCustomers = dailyHappy + dailyNormal + dailyGrumpy + dailyAngry;
        }

        public void RecordDailyEarning(int amount)
        {
            if (amount > 0) dailyEarnedGold += amount;
        }

        public void RecordDailyLoss(int amount)
        {
            if (amount > 0) dailyLostGold += amount;
        }

        /// <summary>
        /// Günlük istatistikleri sıfırlar (yeni gün başında çağrılır).
        /// </summary>
        public void ResetDailyStats()
        {
            dailyTotalCustomers = 0;
            dailyHappy = 0;
            dailyNormal = 0;
            dailyGrumpy = 0;
            dailyAngry = 0;
            dailyLeft = 0;
            dailyEarnedGold = 0;
            dailyLostGold = 0;
        }

        /// <summary>
        /// Sahnedeki Para ve Elmas UI text'lerini otomatik bulur ve atar.
        /// </summary>
        private void AutoAttachParaveElmasUI()
        {
            if (goldText == null)
            {
                GameObject goldObj = GameObject.Find("GoldText");
                if (goldObj != null)
                    goldText = goldObj.GetComponent<TextMeshProUGUI>();
            }

            if (kadimParaText == null)
            {
                GameObject kadimObj = GameObject.Find("KadimParaText");
                if (kadimObj != null)
                    kadimParaText = kadimObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
