using System.Collections;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    [RequireComponent(typeof(Image))]
    public class EnemyBattleSpriteController : MonoBehaviour
    {
        [SerializeField] private BattleController battle;
        [SerializeField] private Image enemyImage;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite hitSprite;
        [SerializeField] private float hitDuration = 0.32f;
        [SerializeField] private float shakeDistance = 18f;

        private RectTransform rectTransform;
        private Coroutine hitRoutine;
        private EnemyPose lastPose = EnemyPose.Idle;

        private void Awake()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            if (enemyImage == null)
            {
                enemyImage = GetComponent<Image>();
            }

            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            if (battle != null)
            {
                battle.StateChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.StateChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            if (enemyImage == null)
            {
                enemyImage = GetComponent<Image>();
            }

            if (enemyImage == null)
            {
                return;
            }

            var pose = battle == null ? EnemyPose.Idle : battle.EnemyPose;
            enemyImage.sprite = pose switch
            {
                EnemyPose.Attack => attackSprite,
                EnemyPose.Hit => hitSprite,
                _ => idleSprite
            };
            if (pose == EnemyPose.Hit && lastPose != EnemyPose.Hit)
            {
                PlayHitFeedback();
            }

            if (hitRoutine == null)
            {
                enemyImage.color = Color.white;
            }

            enemyImage.preserveAspect = true;
            lastPose = pose;
        }

        private void PlayHitFeedback()
        {
            if (hitRoutine != null)
            {
                StopCoroutine(hitRoutine);
            }

            hitRoutine = StartCoroutine(HitFeedbackRoutine());
        }

        private IEnumerator HitFeedbackRoutine()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            var originalPosition = rectTransform == null ? Vector2.zero : rectTransform.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < hitDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / hitDuration);
                var shake = Mathf.Sin(progress * Mathf.PI * 8f) * shakeDistance * (1f - progress);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = originalPosition + new Vector2(shake, 0f);
                }

                enemyImage.color = Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), Color.white, progress);
                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            enemyImage.color = Color.white;
            hitRoutine = null;
        }
    }
}
