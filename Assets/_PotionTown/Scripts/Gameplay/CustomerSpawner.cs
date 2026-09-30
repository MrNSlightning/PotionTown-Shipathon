using UnityEngine;
using System.Collections.Generic;

namespace PotionShop
{
    public class CustomerSpawner : MonoBehaviour
    {
        public static CustomerSpawner Instance { get; private set; }

        [Header("Müşteri Ayarları (Farklı Karakterler)")]
        [Tooltip("İçinden rastgele seçilecek müşteri şablonları")]
        public List<GameObject> customerPrefabs;
        public Transform spawnPoint; 

        [Header("Çoklu Müşteri / Sıra Ayarları")]
        public int maxSimultaneousCustomers 
        {
            get { return PlayerPrefs.GetInt("MaxCustomerSlots", 1); } // Varsayılan 1 müşteri
            set { PlayerPrefs.SetInt("MaxCustomerSlots", value); PlayerPrefs.Save(); }
        }
        [Tooltip("Müşteri sayısını Editör'den sıfırlamak veya test etmek için bu butonu kullanabilirsiniz.")]
        public int debugMaxCustomers = 1;

        public float customerSpacing = 240f;
        public float customerY = 0f;

        [Header("Aktif Müşteriler")]
        public List<Customer> activeCustomers = new List<Customer>();
        public Customer CurrentCustomer => (activeCustomers != null && activeCustomers.Count > 0) ? activeCustomers[0] : null;

        private float spawnTimer;
        private bool _customersVisible = true;

        [Header("Seviye İlerleme Durumu (BİLGİ)")]
        public int spawnedCustomersThisLevel = 0;
        public int completedCustomersThisLevel = 0;
        public int targetCustomersForLevel = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged += HandleShopStatusChanged;
            }
            InitializeLevel();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopStatusChanged -= HandleShopStatusChanged;
            }
        }

        private void HandleShopStatusChanged(bool isOpen)
        {
            if (!isOpen)
            {
                // Dükkan kapatıldığında leveli sıfırla (yeniden başlasın)
                Debug.Log("Dükkan kapatıldı! Level sıfırlanıyor...");
                
                // Sahnedeki tüm müşterileri yok et
                foreach (var c in activeCustomers)
                {
                    if (c != null && c.gameObject != null)
                    {
                        Destroy(c.gameObject);
                    }
                }
                activeCustomers.Clear();

                InitializeLevel();
            }
        }

        // Yeni seviyeye geçildiğinde (veya gün başladığında) çağrılır
        public void InitializeLevel()
        {
            spawnedCustomersThisLevel = 0;
            completedCustomersThisLevel = 0;
            activeCustomers.Clear();
            
            int currentLevel = LevelSystem.CurrentLevel;
            var config = LevelDesignDatabase.Instance != null ? LevelDesignDatabase.Instance.GetLevelConfig(currentLevel) : null;
            
            if (config != null)
            {
                targetCustomersForLevel = config.totalCustomers;
                SetRandomTimer(config);
            }
            else
            {
                targetCustomersForLevel = 5; // Fallback
                spawnTimer = 2f;
            }

            Debug.Log($"<color=cyan>[Seviye {currentLevel}]</color> Hedef Müşteri: {targetCustomersForLevel}");
        }

        private void Update()
        {
            // Dükkan kapalıysa müşteri gelmez
            if (GameManager.Instance != null && GameManager.Instance.isPrepPhase) return;

            // Seviyedeki tüm müşteriler geldiyse daha fazla spawnlama
            if (spawnedCustomersThisLevel >= targetCustomersForLevel) return;

            // Temizlik: Yok edilmiş objeleri listeden çıkar
            activeCustomers.RemoveAll(c => c == null);

            // Ekranda yer var mı?
            if (activeCustomers.Count < maxSimultaneousCustomers)
            {
                float spawnSpeedMultiplier = (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsRushHourActive) ? 2f : 1f;
                spawnTimer -= Time.deltaTime * spawnSpeedMultiplier;
                if (spawnTimer <= 0)
                {
                    SpawnCustomer();
                }
            }
        }

        /// <summary>
        /// Boş olan ilk slot indeksini (0, 1, 2...) bulur.
        /// </summary>
        private int FindAvailableSlot()
        {
            bool[] occupiedSlots = new bool[maxSimultaneousCustomers];
            foreach (var c in activeCustomers)
            {
                if (c != null && c.queueSlot >= 0 && c.queueSlot < maxSimultaneousCustomers)
                {
                    occupiedSlots[c.queueSlot] = true;
                }
            }

            for (int i = 0; i < maxSimultaneousCustomers; i++)
            {
                if (!occupiedSlots[i]) return i;
            }
            return -1; // Hepsi dolu
        }

        /// <summary>
        /// Slot indeksinden hedef pozisyon hesaplar.
        /// </summary>
        public Vector2 GetSlotPosition(int slot)
        {
            // Ortalanmış (Centered) pozisyon hesaplama
            int totalSlots = maxSimultaneousCustomers;
            float totalWidth = (totalSlots - 1) * customerSpacing;
            float startX = -totalWidth / 2f;
            
            float x = startX + (slot * customerSpacing);
            return new Vector2(x, customerY);
        }

        private void SpawnCustomer()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0) return;
            if (spawnPoint == null) return;

            int currentLevel = LevelSystem.CurrentLevel;
            var config = LevelDesignDatabase.Instance != null ? LevelDesignDatabase.Instance.GetLevelConfig(currentLevel) : null;

            if (config == null) return; // Veritabanı yoksa dur
            
            if (config.possiblePotions == null || config.possiblePotions.Count == 0)
            {
                Debug.LogWarning($"<color=red>HATA:</color> Seviye {currentLevel} için LevelDesignDatabase'de hiç iksir seçilmemiş!");
                return; 
            }

            int slot = FindAvailableSlot();
            if (slot < 0) return; // Slot yok

            // Boş prefabları filtrele
            List<GameObject> validPrefabs = new List<GameObject>();
            foreach (var p in customerPrefabs)
            {
                if (p != null) validPrefabs.Add(p);
            }
            if (validPrefabs.Count == 0) return;

            // Halihazırda sahnede bulunan karakterlerin isimlerini topla (aynı karakter tekrar gelmesin)
            HashSet<string> activeCustomerNames = new HashSet<string>();
            foreach (var c in activeCustomers)
            {
                if (c != null)
                {
                    string cleanName = c.gameObject.name.Replace("(Clone)", "").Trim();
                    activeCustomerNames.Add(cleanName);
                }
            }

            // Sahnede henüz bulunmayan prefabları seç
            List<GameObject> availablePrefabs = new List<GameObject>();
            foreach (var p in validPrefabs)
            {
                if (!activeCustomerNames.Contains(p.name))
                {
                    availablePrefabs.Add(p);
                }
            }

            // Eğer tüm karakterler zaten sahnedeyse validPrefabs içinden seç
            List<GameObject> poolToUse = (availablePrefabs.Count > 0) ? availablePrefabs : validPrefabs;
            GameObject randomPrefab = poolToUse[Random.Range(0, poolToUse.Count)];
            GameObject customerObj = Instantiate(randomPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
            
            // Müşterinin tezgahın (Foreground) arkasında kalması için yerel Z pozisyonunu 2f yapıyoruz
            Vector3 curLocalPos = customerObj.transform.localPosition;
            customerObj.transform.localPosition = new Vector3(curLocalPos.x, curLocalPos.y, 2f);

            // Müşteri görsel katmanını 'Characters' olarak ayarla (Foreground tezgahının arkasında, arkaplanın önünde)
            // 1. SortingGroup bileşenlerini ayarla (HeroEditor4D karakterleri ve prefablar için asıl render belirleyici)
            foreach (var sg in customerObj.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
            {
                sg.sortingLayerName = "Characters";
                sg.sortingOrder = 10 + slot * 5;
            }

            // 2. SpriteRenderer bileşenlerini ayarla
            foreach (var sr in customerObj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 10 + slot * 5;
            }

            // İlgili oda kamerasını bul (Canvas'ların worldCamera ihtiyacı için)
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Camera roomCamera = (parentCanvas != null && parentCanvas.worldCamera != null) ? parentCanvas.worldCamera : Camera.main;

            // 3. Müşteri konuşma balonu ve sipariş UI'ı (uiContainer) tezgahın ve müşterinin önünde net görünmelidir!
            foreach (var c in customerObj.GetComponentsInChildren<Canvas>(true))
            {
                c.overrideSorting = true;
                c.sortingLayerName = "UI";
                c.sortingOrder = 1100;
                if (roomCamera != null)
                {
                    c.worldCamera = roomCamera;
                }
            }

            RectTransform rectT = customerObj.GetComponent<RectTransform>();
            if (rectT != null)
            {
                // Anchor'ları ortala
                rectT.anchorMin = new Vector2(0.5f, 0.5f);
                rectT.anchorMax = new Vector2(0.5f, 0.5f);
                rectT.pivot = new Vector2(0.5f, 0.5f);
                rectT.localScale = Vector3.one;
                
                // Boyut ayarı — prefab'ın kendi boyutunu koru, çok küçükse düzelt
                if (rectT.sizeDelta.x < 10 || rectT.sizeDelta.y < 10)
                {
                    rectT.sizeDelta = new Vector2(200f, 300f);
                }
            }
            else
            {
                // Prefab'ın orijinal yerel ölçeğini koru (Canvas içi ölçek çarpanı 115f ile orantılı)
                Vector3 basePrefabScale = randomPrefab.transform.localScale;
                customerObj.transform.localScale = new Vector3(basePrefabScale.x * 115f, basePrefabScale.y * 115f, basePrefabScale.z);
            }
            
            Customer customerScript = customerObj.GetComponent<Customer>();
            if (customerScript != null)
            {
                // Kuyruk slot'unu ata
                customerScript.queueSlot = slot;
                
                // Zorluk parametrelerini ayarla (Config'den çek)
                customerScript.timePerMood = config.timePerMood;

                // Sipariş sayısını belirle
                int orderCount = Random.Range(config.minOrderCount, config.maxOrderCount + 1);

                // Sipariş listesi oluştur
                customerScript.requestedPotions.Clear();
                for (int i = 0; i < orderCount; i++)
                {
                    ItemData randomPotion = config.possiblePotions[Random.Range(0, config.possiblePotions.Count)];
                    customerScript.requestedPotions.Add(randomPotion);
                }

                // UI'ı sipariş listesi oluşturulduktan hemen sonra senkronize et
                customerScript.UpdatePotionRequestUI();
                customerScript.UpdateMoodUI();
                customerScript.UpdateOrderCountUI();

                activeCustomers.Add(customerScript);
                if (!_customersVisible)
                {
                    customerScript.SetVisible(false);
                }
                spawnedCustomersThisLevel++;
                Debug.Log($"Müşteri geldi! ({spawnedCustomersThisLevel}/{targetCustomersForLevel}) - Slot: {slot} - Sipariş: {orderCount}");

                // Sonraki müşteri için zamanlayıcıyı kur
                SetRandomTimer(config);
            }
        }

        /// <summary>
        /// Tüm aktif müşterilerin görünürlüğünü açar veya kapatır.
        /// </summary>
        public void SetCustomersVisible(bool visible)
        {
            _customersVisible = visible;
            if (activeCustomers == null) return;
            foreach (var c in activeCustomers)
            {
                if (c != null)
                {
                    c.SetVisible(visible);
                }
            }
        }

        private void SetRandomTimer(LevelDesignConfig config)
        {
            if (config != null)
                spawnTimer = Random.Range(config.minSpawnDelay, config.maxSpawnDelay);
            else
                spawnTimer = 2f;

            if (SpecialPotionManager.Instance != null && SpecialPotionManager.Instance.IsRushHourActive)
            {
                spawnTimer *= SpecialPotionManager.Instance.rushSpawnDelayMultiplier;
            }
        }

        /// <summary>
        /// En öndeki (en düşük slot numaralı) müşteriye iksir teslim eder.
        /// </summary>
        public bool TryDeliverItem(ItemData item)
        {
            // Ölü referansları temizle
            activeCustomers.RemoveAll(c => c == null);
            
            // En düşük slot'taki müşteriyi bul (en öndeki)
            Customer frontCustomer = null;
            int lowestSlot = int.MaxValue;
            
            foreach (var c in activeCustomers)
            {
                if (c != null && !c.IsWalkingIn && !c.IsWalkingOut && c.queueSlot < lowestSlot)
                {
                    lowestSlot = c.queueSlot;
                    frontCustomer = c;
                }
            }

            if (frontCustomer != null)
            {
                return frontCustomer.ReceivePotion(item);
            }
            return false;
        }

        // Customer.cs içinden müşteri ekrandan çıkınca çağrılır
        public void OnCustomerCompleted()
        {
            completedCustomersThisLevel++;
            Debug.Log($"Müşteri ayrıldı. (Tamamlanan: {completedCustomersThisLevel}/{targetCustomersForLevel})");

            // Eğer seviyedeki TÜM müşteriler tamamlandıysa günü/seviyeyi bitir!
            if (completedCustomersThisLevel >= targetCustomersForLevel)
            {
                Debug.Log($"<color=green>TÜM MÜŞTERİLER BİTTİ! GÜN SONU...</color>");
                
                // Dükkanı otomatik kapat
                if (GameManager.Instance != null && GameManager.Instance.isShopOpen)
                {
                    GameManager.Instance.ToggleShop();
                }

                // Gün sonu panelini aç (kullanıcı inceledikten sonra yeni güne geçecek)
                var panel = UI.EndOfDayPanel.Instance != null ? UI.EndOfDayPanel.Instance : Object.FindFirstObjectByType<UI.EndOfDayPanel>(FindObjectsInactive.Include);
                if (panel != null)
                {
                    panel.gameObject.SetActive(true);
                    panel.OpenPanel();
                }
                else
                {
                    if (LevelSystem.Instance != null)
                    {
                        LevelSystem.Instance.AdvanceLevel();
                        InitializeLevel();
                    }
                }
            }
        }
    }
}
