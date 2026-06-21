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
            var originalCanvasPosition = screenShakeRoot == null ? Vector2.zero : screenShakeRoot.anchoredPosition;
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

                if (screenShakeRoot != null)
                {
                    screenShakeRoot.anchoredPosition = originalCanvasPosition + new Vector2(shake * canvasShakeDistance / Mathf.Max(cameraShakeDistance, 0.01f), 0f);
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

            if (screenShakeRoot != null)
            {
                screenShakeRoot.anchoredPosition = originalCanvasPosition;
            }

            if (playerImage != null)
            {
                playerImage.color = Color.white;
            }

            feedbackRoutine = null;
        }
    }
}
