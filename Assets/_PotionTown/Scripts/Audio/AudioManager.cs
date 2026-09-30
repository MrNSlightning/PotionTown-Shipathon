using System;
using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Oyunun arka plan müziğini (BGM) ve ses efektlerini (SFX) yöneten ana ses yöneticisi.
    /// - Sahne ve oda geçişlerinde müzik kesilmeden sürekli (loop) olarak çalar.
    /// - Inspector üzerinden istediğiniz müziği (AudioClip) atayabilirsiniz.
    /// - Müzik açma/kapatma (Mute/Unmute) ve ses seviyesi ayarları PlayerPrefs ile kalıcı kaydedilir.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[AudioManager]");
                        _instance = go.AddComponent<AudioManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }



        private const string PREFS_MUSIC_MUTED = "AudioManager_MusicMuted";
        private const string PREFS_MUSIC_VOLUME = "AudioManager_MusicVolume";
        private const string PREFS_SFX_MUTED = "AudioManager_SFXMuted";
        private const string PREFS_SFX_VOLUME = "AudioManager_SFXVolume";

        [Header("🎵 Arka Plan Müziği (Seçeceğiniz Müzik Dosyası)")]
        [Tooltip("Oyun boyunca sürekli döngüde (loop) çalacak arka plan müziği. Kendi müzik dosyanızı buraya sürükleyebilirsiniz.")]
        public AudioClip backgroundMusic;

        [Header("Müşteri Sesleri ve Müzikleri (Genel)")]
        public AudioClip customerArriveSFX;
        public AudioClip happyMoodMusic;
        public AudioClip angryMoodMusic;

        [Header("Kazan (Cauldron) Ses Efektleri")]
        public AudioClip cauldronDropItemSFX;
        public AudioClip cauldronBoilingSFX;
        public AudioClip cauldronErrorSFX;

        [Header("🔊 Ses Ayarları")]
        [Range(0f, 1f)]
        [Tooltip("Arka plan müzik ses seviyesi (0 = Sessiz, 1 = En Yüksek)")]
        public float defaultMusicVolume = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Ses efektleri (SFX) ses seviyesi")]
        public float defaultSFXVolume = 0.8f;

        [Tooltip("İndirdiğiniz ses efektleri çok uzunsa, otomatik kesilmesi için saniye sınırı (Örn: 2). 0 yaparsanız sesin tamamı kesilmeden çalar.")]
        public float sfxMaxDuration = 0f;

        [Header("⚙️ Başlangıç Seçenekleri")]
        [Tooltip("Oyun açıldığında müzik otomatik olarak çalmaya başlasın mı?")]
        public bool playOnStart = true;

        [Tooltip("Müzik bittiğinde baştan tekrar çalsın mı (sürekli döngü)?")]
        public bool loopMusic = true;

        [Header("⌨️ Klavye Kısayolu")]
        [Tooltip("Oyundayken müziği anında açıp kapatmak için klavye tuşu (Varsayılan: M)")]
        public KeyCode toggleMuteKey = KeyCode.M;

        // Dahili AudioSource bileşenleri
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _cauldronBoilSource;
        private Coroutine _boilFadeCoroutine;

        private bool _isMusicMuted = false;
        private bool _isSFXMuted = false;
        private float _currentMusicVolume = 0.5f;
        private float _currentSFXVolume = 0.8f;

        /// <summary>
        /// Müziğin şu an sessize alınıp alınmadığını döndürür.
        /// </summary>
        public bool IsMusicMuted => _isMusicMuted;

        /// <summary>
        /// SFX'in şu an sessize alınıp alınmadığını döndürür.
        /// </summary>
        public bool IsSFXMuted => _isSFXMuted;

        /// <summary>
        /// Geçerli müzik ses seviyesi (0.0 - 1.0).
        /// </summary>
        public float MusicVolume => _currentMusicVolume;

        /// <summary>
        /// Geçerli SFX ses seviyesi (0.0 - 1.0).
        /// </summary>
        public float SFXVolume => _currentSFXVolume;

        /// <summary>
        /// Müzik açılıp kapatıldığında tetiklenen olay (true: Sessiz, false: Açık).
        /// </summary>
        public static event Action<bool> OnMusicMuteChanged;

        /// <summary>
        /// Müzik ses seviyesi değiştiğinde tetiklenen olay.
        /// </summary>
        public static event Action<float> OnMusicVolumeChanged;

        /// <summary>
        /// SFX açılıp kapatıldığında tetiklenen olay (true: Sessiz, false: Açık).
        /// </summary>
        public static event Action<bool> OnSFXMuteChanged;

        /// <summary>
        /// SFX ses seviyesi değiştiğinde tetiklenen olay.
        /// </summary>
        public static event Action<float> OnSFXVolumeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoStartAudio()
        {
            if (Instance != null)
            {
                Instance.EnsurePlaying();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Sahne veya oda geçişlerinde nesnenin ve müziğin yok olmasını engelle
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadClipsIfMissing();
            SetupAudioSources();
            LoadSettings();
        }

        public void LoadClipsIfMissing()
        {
            if (backgroundMusic == null)
                backgroundMusic = Resources.Load<AudioClip>("Audio/Counting_by_Candlelight");
            if (customerArriveSFX == null)
                customerArriveSFX = Resources.Load<AudioClip>("Audio/a_bell_sound_signali_#4-1790678052992");
            if (cauldronBoilingSFX == null)
                cauldronBoilingSFX = Resources.Load<AudioClip>("Audio/19845__jace__boiling-bubbles-ingredients");
            if (cauldronDropItemSFX == null)
                cauldronDropItemSFX = Resources.Load<AudioClip>("Audio/702806__lilmati__waterdrop-click-clean-ui-drop");
            if (happyMoodMusic == null)
                happyMoodMusic = Resources.Load<AudioClip>("Audio/Happy_customer_murmu_#2-1790678109915");
            if (angryMoodMusic == null)
                angryMoodMusic = Resources.Load<AudioClip>("Audio/Could_you_design_a_b_#4-1790679156734");
            if (cauldronErrorSFX == null)
                cauldronErrorSFX = Resources.Load<AudioClip>("Audio/Please_let_the_voice_#3-1790678290146");

#if UNITY_EDITOR
            if (backgroundMusic == null)
                backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/Counting_by_Candlelight.mp3");
            if (customerArriveSFX == null)
                customerArriveSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/a_bell_sound_signali_#4-1790678052992.mp3");
            if (cauldronBoilingSFX == null)
                cauldronBoilingSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/19845__jace__boiling-bubbles-ingredients.wav");
            if (cauldronDropItemSFX == null)
                cauldronDropItemSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/702806__lilmati__waterdrop-click-clean-ui-drop.wav");
            if (happyMoodMusic == null)
                happyMoodMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/Happy_customer_murmu_#2-1790678109915.mp3");
            if (angryMoodMusic == null)
                angryMoodMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/Could_you_design_a_b_#4-1790679156734.mp3");
            if (cauldronErrorSFX == null)
                cauldronErrorSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_PotionTown/SFX/Please_let_the_voice_#3-1790678290146.mp3");
#endif
        }

        public void EnsurePlaying()
        {
            LoadClipsIfMissing();
            SetupAudioSources();
            if (playOnStart && backgroundMusic != null && _musicSource != null && !_musicSource.isPlaying && !_isMusicMuted)
            {
                PlayMusic(backgroundMusic);
            }
        }

        private void Start()
        {
            EnsurePlaying();
        }

        private void Update()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (toggleMuteKey != KeyCode.None && Input.GetKeyDown(toggleMuteKey))
            {
                ToggleMusicMute();
            }
#endif
        }

        private void OnValidate()
        {
            if (_musicSource != null)
            {
                _musicSource.loop = loopMusic;
                ApplyMusicVolume();
            }
        }

        /// <summary>
        /// Müzik ve SFX için gerekli AudioSource bileşenlerini oluşturur veya yapılandırır.
        /// Mükerrer çağrılarda yeni kaynak eklemez, mevcutları korur ve zombi kaynakları temizler.
        /// </summary>
        private void SetupAudioSources()
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources != null && existingSources.Length > 0)
            {
                if (_musicSource == null) _musicSource = existingSources[0];
                if (existingSources.Length > 1 && _sfxSource == null) _sfxSource = existingSources[1];
                if (existingSources.Length > 2 && _cauldronBoilSource == null) _cauldronBoilSource = existingSources[2];

                // 3'ten fazla olan mükerrer (zombi) AudioSource'ları tamamen temizle
                for (int i = 0; i < existingSources.Length; i++)
                {
                    AudioSource src = existingSources[i];
                    if (src != null && src != _musicSource && src != _sfxSource && src != _cauldronBoilSource)
                    {
                        src.Stop();
                        Destroy(src);
                    }
                }
            }

            // Müzik kaynağı
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }
            _musicSource.playOnAwake = false;
            _musicSource.loop = loopMusic;
            _musicSource.spatialBlend = 0f; // 2D Ses (Her yerden eşit duyulur)
            _musicSource.ignoreListenerPause = true;

            // SFX kaynağı
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;

            // Kazan kaynama / fokurdama kaynağı (işlem bitince durdurulabilir özel kanal)
            if (_cauldronBoilSource == null)
            {
                _cauldronBoilSource = gameObject.AddComponent<AudioSource>();
            }
            _cauldronBoilSource.playOnAwake = false;
            _cauldronBoilSource.loop = true;
            _cauldronBoilSource.spatialBlend = 0f;
        }

        /// <summary>
        /// Kaydedilmiş ses tercihlerini PlayerPrefs'ten yükler.
        /// </summary>
        private void LoadSettings()
        {
            _isMusicMuted = PlayerPrefs.GetInt(PREFS_MUSIC_MUTED, 0) == 1;
            _currentMusicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC_VOLUME, defaultMusicVolume);
            _isSFXMuted = PlayerPrefs.GetInt(PREFS_SFX_MUTED, 0) == 1;
            _currentSFXVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, defaultSFXVolume);

            ApplyMusicVolume();
        }

        /// <summary>
        /// Arka plan müziğini başlatır veya değiştirir.
        /// </summary>
        /// <param name="clip">Çalınacak müzik dosyası. Boş verilirse Inspector'daki backgroundMusic kullanılır.</param>
        public void PlayMusic(AudioClip clip = null)
        {
            if (clip != null)
            {
                backgroundMusic = clip;
            }

            if (backgroundMusic == null || _musicSource == null) return;

            if (_musicSource.clip == backgroundMusic && _musicSource.isPlaying)
            {
                return; // Zaten çalıyor
            }

            _musicSource.clip = backgroundMusic;
            _musicSource.loop = loopMusic;
            ApplyMusicVolume();

            if (!_isMusicMuted)
            {
                _musicSource.Play();
            }
        }

        /// <summary>
        /// Müziği duraklatır (kaldığı yerden devam ettirilebilir).
        /// </summary>
        public void PauseMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Pause();
            }
        }

        /// <summary>
        /// Duraklatılmış müziği kaldığı yerden devam ettirir.
        /// </summary>
        public void ResumeMusic()
        {
            if (_musicSource != null && !_musicSource.isPlaying)
            {
                _musicSource.UnPause();
            }
        }

        /// <summary>
        /// Müziği tamamen durdurur.
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        /// <summary>
        /// Müziğin sesini açar veya kapatır (Mute Toggle).
        /// Her çağrıldığında açık ise kapatır, kapalı ise açar.
        /// </summary>
        /// <returns>Yeni sessiz durumu (true = sessiz, false = ses açık).</returns>
        public bool ToggleMusicMute()
        {
            SetMusicMute(!_isMusicMuted);
            return _isMusicMuted;
        }

        /// <summary>
        /// Müziğin sessiz durumunu doğrudan ayarlar.
        /// Mute edildiğinde müzik hem ses seviyesi 0 yapılır hem de duraklatılır (Pause); böylece asla ses sızmaz.
        /// </summary>
        /// <param name="mute">true ise müzik kapanır, false ise açılır.</param>
        public void SetMusicMute(bool mute)
        {
            _isMusicMuted = mute;
            PlayerPrefs.SetInt(PREFS_MUSIC_MUTED, _isMusicMuted ? 1 : 0);
            PlayerPrefs.Save();

            ApplyMusicVolume();

            OnMusicMuteChanged?.Invoke(_isMusicMuted);
            Debug.Log($"<color=yellow>[AudioManager]</color> Müzik durumu: {(_isMusicMuted ? "KAPALI (Sessiz)" : "AÇIK")}");
        }

        /// <summary>
        /// Arka plan müziğinin ses seviyesini ayarlar (0.0 - 1.0).
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _currentMusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOLUME, _currentMusicVolume);
            PlayerPrefs.Save();

            ApplyMusicVolume();
            OnMusicVolumeChanged?.Invoke(_currentMusicVolume);
        }

        /// <summary>
        /// SFX (ses efekti) ses seviyesini ayarlar (0.0 - 1.0).
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _currentSFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, _currentSFXVolume);
            PlayerPrefs.Save();
            if (_cauldronBoilSource != null)
            {
                _cauldronBoilSource.volume = _currentSFXVolume;
            }
            OnSFXVolumeChanged?.Invoke(_currentSFXVolume);
        }

        /// <summary>
        /// SFX sesini açar veya kapatır (Mute Toggle).
        /// </summary>
        public bool ToggleSFXMute()
        {
            SetSFXMute(!_isSFXMuted);
            return _isSFXMuted;
        }

        /// <summary>
        /// SFX sessiz durumunu doğrudan ayarlar.
        /// </summary>
        public void SetSFXMute(bool mute)
        {
            _isSFXMuted = mute;
            PlayerPrefs.SetInt(PREFS_SFX_MUTED, _isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
            if (_cauldronBoilSource != null)
            {
                _cauldronBoilSource.mute = _isSFXMuted;
                if (_isSFXMuted && _cauldronBoilSource.isPlaying)
                {
                    _cauldronBoilSource.Stop();
                }
            }
            OnSFXMuteChanged?.Invoke(_isSFXMuted);
            Debug.Log($"<color=yellow>[AudioManager]</color> SFX durumu: {(_isSFXMuted ? "KAPALI (Sessiz)" : "AÇIK")}");
        }

        private float _lastSFXTime = 0f;
        private AudioClip _lastSFXClip = null;

        /// <summary>
        /// Tek seferlik bir ses efekti çalar (Buton tıklaması, sikke sesi vs.).
        /// Uzun sesler sfxMaxDuration süresi sonunda otomatik kesilir (0 ise tamamı çalar).
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _isSFXMuted) return;

            // Kazan kaynama efekti için durdurulabilir özel AudioSource kullan
            if (clip == cauldronBoilingSFX)
            {
                PlayCauldronBoiling(volumeScale);
                return;
            }

            // Aynı ses klibinin çok kısa aralıklarla (0.05s) üst üste binip patlamasını engelle
            if (_lastSFXClip == clip && Time.unscaledTime - _lastSFXTime < 0.05f)
            {
                return;
            }
            _lastSFXClip = clip;
            _lastSFXTime = Time.unscaledTime;

            if (sfxMaxDuration <= 0f || clip.length <= sfxMaxDuration)
            {
                if (_sfxSource != null)
                {
                    _sfxSource.PlayOneShot(clip, _currentSFXVolume * volumeScale);
                }
            }
            else
            {
                // Uzun sesi kesmek için geçici bir AudioSource oluştur
                GameObject tempGO = new GameObject("TempSFX_" + clip.name);
                tempGO.transform.parent = transform;
                AudioSource tempSource = tempGO.AddComponent<AudioSource>();
                tempSource.clip = clip;
                tempSource.volume = _currentSFXVolume * volumeScale;
                tempSource.spatialBlend = 0f;
                tempSource.Play();

                // Belirlenen süre sonunda sesi (ve objeyi) yok et
                Destroy(tempGO, sfxMaxDuration);
            }
        }

        /// <summary>
        /// Kazan kaynama / fokurdama sesini başlatır.
        /// </summary>
        public void PlayCauldronBoiling(float volumeScale = 1f)
        {
            if (_isSFXMuted) return;

            if (_boilFadeCoroutine != null)
            {
                StopCoroutine(_boilFadeCoroutine);
                _boilFadeCoroutine = null;
            }

            if (cauldronBoilingSFX == null)
            {
                LoadClipsIfMissing();
            }

            if (cauldronBoilingSFX == null) return;

            if (_cauldronBoilSource == null)
            {
                SetupAudioSources();
            }

            if (_cauldronBoilSource != null)
            {
                _cauldronBoilSource.clip = cauldronBoilingSFX;
                _cauldronBoilSource.volume = _currentSFXVolume * volumeScale;
                _cauldronBoilSource.mute = _isSFXMuted;
                _cauldronBoilSource.loop = true;
                if (!_cauldronBoilSource.isPlaying)
                {
                    _cauldronBoilSource.Play();
                }
            }
        }

        /// <summary>
        /// Kazan kaynama sesini durdurur (kaynatma/üretim işlemi tamamlandığında çağrılır).
        /// </summary>
        public void StopCauldronBoiling(float fadeDuration = 0.2f)
        {
            if (_cauldronBoilSource == null || !_cauldronBoilSource.isPlaying) return;

            if (_boilFadeCoroutine != null)
            {
                StopCoroutine(_boilFadeCoroutine);
                _boilFadeCoroutine = null;
            }

            if (fadeDuration <= 0f || !gameObject.activeInHierarchy)
            {
                _cauldronBoilSource.Stop();
                _cauldronBoilSource.volume = _currentSFXVolume;
            }
            else
            {
                _boilFadeCoroutine = StartCoroutine(FadeOutBoilingRoutine(fadeDuration));
            }
        }

        private System.Collections.IEnumerator FadeOutBoilingRoutine(float duration)
        {
            if (_cauldronBoilSource == null) yield break;

            float startVol = _cauldronBoilSource.volume;
            float elapsed = 0f;

            while (elapsed < duration && _cauldronBoilSource != null && _cauldronBoilSource.isPlaying)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _cauldronBoilSource.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            if (_cauldronBoilSource != null)
            {
                _cauldronBoilSource.Stop();
                _cauldronBoilSource.volume = _currentSFXVolume;
            }
            _boilFadeCoroutine = null;
        }

        private void ApplyMusicVolume()
        {
            if (_musicSource != null)
            {
                _musicSource.mute = _isMusicMuted;
                _musicSource.volume = _isMusicMuted ? 0f : _currentMusicVolume;

                if (_isMusicMuted)
                {
                    if (_musicSource.isPlaying)
                    {
                        _musicSource.Pause();
                    }
                }
                else
                {
                    if (!_musicSource.isPlaying && backgroundMusic != null)
                    {
                        _musicSource.Play();
                    }
                    else
                    {
                        _musicSource.UnPause();
                    }
                }
            }
        }
    }
}
