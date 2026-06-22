using System.Collections;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    [RequireComponent(typeof(Image))]
    public class PlayerBattleFeedbackController : MonoBehaviour
    {
        [SerializeField] private BattleController battle;
        [SerializeField] private Image playerImage;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite hitSprite;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RectTransform screenShakeRoot;
        [SerializeField] private float feedbackDuration = 0.35f;
        [SerializeField] private float cameraShakeDistance = 0.16f;
        [SerializeField] private float canvasShakeDistance = 12f;

        private int lastHitPulse;
        private Coroutine feedbackRoutine;

        private void Awake()
        {
            CacheReferences();
            lastHitPulse = battle == null ? 0 : battle.PlayerHitPulse;
        }

        private void OnEnable()
        {
            CacheReferences();
            if (battle != null)
            {
                battle.StateChanged += Refresh;
                lastHitPulse = battle.PlayerHitPulse;
            }
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.StateChanged -= Refresh;
            }
        }

        private void CacheReferences()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            if (playerImage == null)
            {
                playerImage = GetComponent<Image>();
            }

            if (defaultSprite == null && playerImage != null)
            {
                defaultSprite = playerImage.sprite;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (screenShakeRoot == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                screenShakeRoot = canvas == null ? null : canvas.GetComponent<RectTransform>();
            }
        }

        private void Refresh()
        {
            if (battle == null)
            {
                return;
            }

            if (battle.PlayerHitPulse == lastHitPulse)
            {
                return;
            }

            lastHitPulse = battle.PlayerHitPulse;
            PlayFeedback();
        }

        private void PlayFeedback()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = StartCoroutine(FeedbackRoutine());
        }

        private IEnumerator FeedbackRoutine()
        {
            CacheReferences();
            var cameraTransform = targetCamera == null ? null : targetCamera.transform;
            var originalCameraPosition = cameraTransform == null ? Vector3.zero : cameraTransform.localPosition;
            var originalSprite = playerImage == null ? null : playerImage.sprite;
            var shakeRects = GetShakeRects();
            var originalShakePositions = new Vector2[shakeRects.Length];
            for (var i = 0; i < shakeRects.Length; i++)
            {
                originalShakePositions[i] = shakeRects[i].anchoredPosition;
            }

            if (playerImage != null && hitSprite != null)
            {
                playerImage.sprite = hitSprite;
            }

            var elapsed = 0f;

            while (elapsed < feedbackDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / feedbackDuration);
                var shake = Mathf.Sin(progress * Mathf.PI * 10f) * cameraShakeDistance * (1f - progress);
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = originalCameraPosition + new Vector3(shake, 0f, 0f);
                }

                var canvasShake = shake * canvasShakeDistance / Mathf.Max(cameraShakeDistance, 0.01f);
                for (var i = 0; i < shakeRects.Length; i++)
                {
                    shakeRects[i].anchoredPosition = originalShakePositions[i] + new Vector2(canvasShake, 0f);
                }

                if (playerImage != null)
                {
                    playerImage.color = Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), Color.white, progress);
                }

                yield return null;
            }

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = originalCameraPosition;
            }

            for (var i = 0; i < shakeRects.Length; i++)
            {
                shakeRects[i].anchoredPosition = originalShakePositions[i];
            }

            if (playerImage != null)
            {
                playerImage.sprite = defaultSprite != null ? defaultSprite : originalSprite;
                playerImage.color = Color.white;
            }

            feedbackRoutine = null;
        }

        private RectTransform[] GetShakeRects()
        {
            if (screenShakeRoot == null)
            {
                return new RectTransform[0];
            }

            var rects = new RectTransform[screenShakeRoot.childCount];
            var count = 0;
            for (var i = 0; i < screenShakeRoot.childCount; i++)
            {
                var child = screenShakeRoot.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                rects[count] = child;
                count++;
            }

            if (count == rects.Length)
            {
                return rects;
            }

            var trimmed = new RectTransform[count];
            for (var i = 0; i < count; i++)
            {
                trimmed[i] = rects[i];
            }

            return trimmed;
        }
    }
}
