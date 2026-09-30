using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PotionShop
{
    /// <summary>
    /// Taverna mÃƒÂ¼Ã…Å¸terilerine canlÃ„Â±lÃ„Â±k katan prosedÃƒÂ¼rel gÃƒÂ¶rsel animasyon yÃƒÂ¶neticisi.
    /// - YÃƒÂ¼rÃƒÂ¼me adÃ„Â±mlarÃ„Â± ve yaylanma (Step Bobbing & Tilt)
    /// - DoÃ„Å¸al nefes alma ve sÃƒÂ¼zÃƒÂ¼lme (Idle Breathing)
    /// - SabÃ„Â±rsÃ„Â±zlÃ„Â±k titremesi ve ÃƒÂ¶fke nabzÃ„Â± (Mood Impatience & Rage Pulse)
    /// - Ã„Â°ksir teslimatÃ„Â±nda sevinÃƒÂ§ zÃ„Â±plamasÃ„Â± (Celebration Bounce & Squash)
    /// - SipariÃ…Å¸ baloncuÃ„Å¸u elastik aÃƒÂ§Ã„Â±lÃ„Â±Ã…Å¸Ã„Â± (Bubble Pop-In)
    /// </summary>
    public class CustomerVisualAnimator : MonoBehaviour
    {
        [Header("GÃƒÂ¶rsel ParÃƒÂ§alar")]
        public RectTransform characterBody;
        public Image characterImage;
        public RectTransform speechBubble;
        public RectTransform shadowObj;

        [Header("YÃƒÂ¼rÃƒÂ¼me Animasyon AyarlarÃ„Â±")]
        public float walkBobFrequency = 14f;
        public float walkBobHeight = 15f;
        public float walkTiltAngle = 8f;

        [Header("Nefes Alma (Idle) AyarlarÃ„Â±")]
        public float breathSpeed = 2.0f;
        public float breathScaleY = 0.025f;
        public float idleFloatAmount = 4f;

        // Dahili Durum
        private bool _isWalking = false;
        private bool _isCelebrating = false;
        private bool _isImpatient = false;
        private bool _isRaging = false;

        [Header("Transform Tabanlı Görsel (2D Sprite için)")]
        public Transform visualTransform;
        private SpriteRenderer visualRenderer;
        private Vector3 _visualBasePos;
        private Vector3 _visualBaseScale = Vector3.one;

        private Vector2 _bodyBasePos;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Vector2 _bubbleBasePos;
        private Vector3 _bubbleBaseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private float _animTimer = 0f;
        private Coroutine _celebrationRoutine;

        private void Awake()
        {
            if (characterBody == null)
            {
                var bodyTr = transform.Find("Customer") ?? transform.Find("Body");
                if (bodyTr != null) characterBody = bodyTr.GetComponent<RectTransform>();
                else characterBody = GetComponent<RectTransform>();
            }

            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual") ?? transform.Find("karakter");
            }

            if (visualTransform != null)
            {
                visualRenderer = visualTransform.GetComponent<SpriteRenderer>();
                _visualBasePos = visualTransform.localPosition;
                _visualBaseScale = visualTransform.localScale;
            }

            if (characterImage == null && characterBody != null)
            {
                characterImage = characterBody.GetComponent<Image>();
            }

            if (characterBody != null)
            {
                _bodyBasePos = characterBody.anchoredPosition;
                _bodyBaseScale = characterBody.localScale;
            }

            if (characterImage != null)
            {
                _baseColor = characterImage.color;
            }

            if (speechBubble != null)
            {
                _bubbleBasePos = speechBubble.anchoredPosition;
                _bubbleBaseScale = speechBubble.localScale;
            }
        }

        private void Update()
        {
            if (_isCelebrating) return; // ZÃ„Â±plama anÃ„Â±nda prosedÃƒÂ¼rel dÃƒÂ¶ngÃƒÂ¼yÃƒÂ¼ askÃ„Â±ya al

            _animTimer += Time.deltaTime;

            if (_isWalking)
            {
                UpdateWalkAnimation();
            }
            else
            {
                UpdateIdleAnimation();
            }

            if (_isRaging)
            {
                UpdateRageEffect();
            }
            else if (_isImpatient)
            {
                UpdateImpatientEffect();
            }
        }

        // Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬ YÃƒÂ¼rÃƒÂ¼me Animasyonu (Step Bobbing & Tilt) Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬

        public void PlayWalk(bool walking)
        {
            _isWalking = walking;
            if (characterBody != null)
            {
                characterBody.anchoredPosition = _bodyBasePos;
                characterBody.localRotation = Quaternion.identity;
                characterBody.localScale = _bodyBaseScale;
            }
            if (visualTransform != null)
            {
                visualTransform.localPosition = _visualBasePos;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = _visualBaseScale;
            }
        }

        private void UpdateWalkAnimation()
        {
            // Kullanıcı isteği üzerine yürüme animasyonu (sallanma/sekme/dönme) kaldırıldı.
            // Karakter düz ve stabil bir şekilde hedefine doğru kayar.
        }

        // Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬ Nefes Alma Animasyonu (Idle Breathing) Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬

        private void UpdateIdleAnimation()
        {
            float currentSpeed = _isImpatient ? breathSpeed * 1.8f : breathSpeed;
            float sine = Mathf.Sin(_animTimer * currentSpeed);

            if (characterBody != null)
            {
                // Nefes alıp verme (Y ekseni genişleme)
                float scaleY = _bodyBaseScale.y + (sine * breathScaleY);
                float scaleX = _bodyBaseScale.x - (sine * breathScaleY * 0.4f);
                characterBody.localScale = new Vector3(scaleX, scaleY, _bodyBaseScale.z);

                // Hafif dikey salınım
                float floatOffset = sine * idleFloatAmount;
                characterBody.anchoredPosition = _bodyBasePos + new Vector2(0f, floatOffset);
                characterBody.localRotation = Quaternion.identity;
            }
            else if (visualTransform != null)
            {
                // Nefes alma (2D Sprite için)
                float scaleY = _visualBaseScale.y + (sine * breathScaleY * _visualBaseScale.y);
                float scaleX = _visualBaseScale.x - (sine * breathScaleY * 0.4f * _visualBaseScale.x);
                visualTransform.localScale = new Vector3(scaleX, scaleY, _visualBaseScale.z);

                // Hafif süzülme
                float floatOffset = sine * 0.03f;
                visualTransform.localPosition = _visualBasePos + new Vector3(0f, floatOffset, 0f);
                visualTransform.localRotation = Quaternion.identity;
            }
        }

        // Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬ Duygu Tepkileri (Mood Reactions) Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬Ã¢â€ â‚¬

        public void SetMood(Customer.Mood mood)
        {
            switch (mood)
            {
                case Customer.Mood.Mutlu:
                    _isImpatient = false;
                    _isRaging = false;
                    ResetVisualEffects();
                    break;

                case Customer.Mood.Normal:
                    _isImpatient = false;
                    _isRaging = false;
                    ResetVisualEffects();
                    break;

                case Customer.Mood.Huysuz:
                    _isImpatient = true;
                    _isRaging = false;
                    break;

                case Customer.Mood.Ofkeli:
                    _isImpatient = true;
                    _isRaging = true;
                    break;
            }
        }

        public void PlayAnger()
        {
            SetMood(Customer.Mood.Ofkeli);
        }

        private void UpdateImpatientEffect()
        {
            if (characterBody == null) return;

            // Huysuz: arada bir hafif sabÃ„Â±rsÃ„Â±z seÃ„Å¸irme
            if (Mathf.Sin(_animTimer * 6f) > 0.85f)
            {
                float jitter = Mathf.Sin(_animTimer * 20f) * 1.5f;
                characterBody.localRotation = Quaternion.Euler(0f, 0f, jitter);
            }
        }

        private void UpdateRageEffect()
        {
            if (characterBody == null || characterImage == null) return;

            // Ãƒâ€“fkeli: Sinirli titreme
            Vector2 shake = UnityEngine.Random.insideUnitCircle * 2.5f;
            characterBody.anchoredPosition = _bodyBasePos + shake;

            // KÃ„Â±rmÃ„Â±zÃ„Â± ÃƒÂ¶fke nabzÃ„Â±
            float pulse = (Mathf.Sin(_animTimer * 8f) + 1f) * 0.5f;
            characterImage.color = Color.Lerp(_baseColor, new Color(1f, 0.4f, 0.4f, 1f), pulse * 0.75f);
        }

        private void ResetVisualEffects()
        {
            if (characterImage != null) characterImage.color = _baseColor;
            if (characterBody != null) characterBody.localRotation = Quaternion.identity;
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã„Â°ksir Teslimat SevinÃƒÂ§ ZÃ„Â±plamasÃ„Â± (Celebration Jump) Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

        public void PlayCelebration(Action onComplete = null)
        {
            if (_celebrationRoutine != null) StopCoroutine(_celebrationRoutine);
            _celebrationRoutine = StartCoroutine(CelebrationRoutine(onComplete));
        }

        public void TriggerCelebration() => PlayCelebration();

        private IEnumerator CelebrationRoutine(Action onComplete)
        {
            _isCelebrating = true;
            ResetVisualEffects();

            // 1. ZÃ„Â±plama ÃƒÂ¶ncesi hafif ÃƒÂ§ÃƒÂ¶kme (Squash)
            float prepTime = 0.12f;
            float elapsed = 0f;
            while (elapsed < prepTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / prepTime;
                if (characterBody != null)
                {
                    characterBody.localScale = new Vector3(_bodyBaseScale.x * 1.15f, _bodyBaseScale.y * 0.85f, 1f);
                    characterBody.anchoredPosition = _bodyBasePos - new Vector2(0f, 8f);
                }
                yield return null;
            }

            // 2. YukarÃ„Â± ZÃ„Â±plama & Esneme (Jump & Stretch)
            float jumpTime = 0.22f;
            elapsed = 0f;
            float jumpHeight = 32f;
            while (elapsed < jumpTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpTime;
                float curve = Mathf.Sin(t * Mathf.PI * 0.5f); // OutQuad

                if (characterBody != null)
                {
                    characterBody.anchoredPosition = _bodyBasePos + new Vector2(0f, curve * jumpHeight);
                    characterBody.localScale = new Vector3(_bodyBaseScale.x * 0.90f, _bodyBaseScale.y * 1.20f, 1f);
                }
                yield return null;
            }

            // 3. Yere Ã„Â°niÃ…Å¸ (Land & Bounce)
            float landTime = 0.18f;
            elapsed = 0f;
            while (elapsed < landTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / landTime;
                float curve = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // InQuad

                if (characterBody != null)
                {
                    characterBody.anchoredPosition = _bodyBasePos + new Vector2(0f, (1f - curve) * jumpHeight);
                    characterBody.localScale = Vector3.Lerp(
                        new Vector3(_bodyBaseScale.x * 1.12f, _bodyBaseScale.y * 0.88f, 1f),
                        _bodyBaseScale,
                        t
                    );
                }
                yield return null;
            }

            // 4. Normale dÃƒÂ¶nÃƒÂ¼Ã…Å¸
            if (characterBody != null)
            {
                characterBody.anchoredPosition = _bodyBasePos;
                characterBody.localScale = _bodyBaseScale;
            }

            _isCelebrating = false;
            onComplete?.Invoke();
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ SipariÃ…Å¸ BaloncuÃ„Å¸u Pop-In Animasyonu Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

        public void PlayBubblePopIn()
        {
            if (speechBubble == null) return;
            StopAllCoroutines();
            StartCoroutine(BubblePopInRoutine());
        }

        private IEnumerator BubblePopInRoutine()
        {
            speechBubble.gameObject.SetActive(true);
            speechBubble.localScale = Vector3.zero;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Elastik Overshoot Easing: 0 -> 1.15 -> 1.0
                float scale = ElasticEaseOut(t);
                speechBubble.localScale = new Vector3(_bubbleBaseScale.x * scale, _bubbleBaseScale.y * scale, _bubbleBaseScale.z);
                yield return null;
            }

            speechBubble.localScale = _bubbleBaseScale;
        }

        private float ElasticEaseOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
        }
    }
}
