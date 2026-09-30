#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionShop
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AudioManager manager = (AudioManager)target;

            // Bilgi Kutusu
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎵 PotionShop Ses Yöneticisi (BGM System)", titleStyle);

            EditorGUILayout.Space(3);
            if (manager.backgroundMusic != null)
            {
                EditorGUILayout.HelpBox($"✅ Seçili Müzik: {manager.backgroundMusic.name} (Süre: {manager.backgroundMusic.length:F1}s)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Henüz bir müzik dosyası atanmadı!\nLütfen aşağıdaki 'Background Music' kutusuna kendi müzik dosyanızı (.mp3, .wav, .ogg) sürükleyip bırakın.", MessageType.Warning);
            }

            if (Application.isPlaying)
            {
                bool isMuted = manager.IsMusicMuted;
                string muteStatus = isMuted ? "🔇 SES KAPALI (Sessiz)" : "🔊 SES AÇIK";
                EditorGUILayout.LabelField($"Durum: {muteStatus}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Ses Seviyesi: %{(manager.MusicVolume * 100f):F0}");

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(isMuted ? "🔊 Sesi Aç (Unmute)" : "🔇 Sesi Kapat (Mute)", GUILayout.Height(30)))
                {
                    manager.ToggleMusicMute();
                }

                if (GUILayout.Button("🔁 Müziği Yeniden Başlat", GUILayout.Height(30)))
                {
                    manager.PlayMusic(manager.backgroundMusic);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("💡 İpucu: Oyun başladığında müzik otomatik ve kesintisiz (loop) çalacaktır.\nOyundayken 'M' tuşuna basarak veya UI butonuna tıklayarak sesi anında açıp kapatabilirsiniz.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            // Standart Inspector alanlarını çiz
            DrawDefaultInspector();
        }

        [MenuItem("Tools/Audio/Sahnede AudioManager Oluştur", false, 10)]
        [MenuItem("GameObject/Audio/AudioManager (PotionShop)", false, 10)]
        public static void CreateAudioManagerInScene()
        {
            AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                EditorUtility.DisplayDialog("AudioManager Mevcut", "Sahnede zaten bir AudioManager nesnesi bulunuyor. Nesne seçildi, Inspector'dan 'Background Music' alanına müziğinizi sürükleyebilirsiniz.", "Tamam");
                return;
            }

            GameObject go = new GameObject("[AudioManager]");
            AudioManager audioMgr = go.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            Debug.Log("<color=green>[AudioManager]</color> Sahnede başarıyla oluşturuldu! Inspector'dan 'Background Music' alanına istediğiniz müzik dosyasını sürükleyin.");
            EditorUtility.DisplayDialog("AudioManager Oluşturuldu", "[AudioManager] nesnesi sahneye eklendi ve seçildi.\n\nŞimdi Inspector panelindeki 'Background Music' kutusuna çalmasını istediğiniz müziği sürükleyip bırakabilirsiniz.", "Harika!");
        }

        [MenuItem("Tools/Audio/UI Ses Aç-Kapa Butonu Ekle", false, 11)]
        public static void CreateSoundToggleButton()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Canvas Bulunamadı", "Lütfen sahnede bir Canvas olduğundan emin olun.", "Tamam");
                return;
            }

            // Buton nesnesi oluştur
            GameObject btnObj = new GameObject("SoundToggleButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SoundToggleButton));
            btnObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(140f, 45f);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

            // Buton içi metin
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Müzik: Açık";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            SoundToggleButton stb = btnObj.GetComponent<SoundToggleButton>();
            stb.button = btnObj.GetComponent<Button>();
            stb.statusText = tmp;

            Undo.RegisterCreatedObjectUndo(btnObj, "Create Sound Toggle Button");
            Selection.activeGameObject = btnObj;
            EditorGUIUtility.PingObject(btnObj);

            Debug.Log("<color=green>[SoundToggleButton]</color> Canvas altına başarıyla eklendi!");
            EditorUtility.DisplayDialog("Ses Butonu Eklendi", "Canvas altına 'SoundToggleButton' oluşturuldu.\nOyundayken bu butona tıklandığında müzik açılıp kapanacaktır.", "Tamam");
        }
    }
}
#endif
