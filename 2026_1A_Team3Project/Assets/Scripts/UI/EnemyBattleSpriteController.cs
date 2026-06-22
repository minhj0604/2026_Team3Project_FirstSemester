using System.Collections;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Team3Project.UI
{
    [RequireComponent(typeof(Image))]
    public class EnemyBattleSpriteController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private BattleController battle;
        [SerializeField] private Image enemyImage;
        [SerializeField] private int enemyIndex;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite hitSprite;
        [SerializeField] private float hitDuration = 0.32f;
        [SerializeField] private float shakeDistance = 18f;

        private RectTransform rectTransform;
        private Text targetArrow;
        private Coroutine hitRoutine;
        private Coroutine attackRoutine;
        private EnemyPose lastPose = EnemyPose.Idle;

        public int EnemyIndex => enemyIndex;

        public void Configure(BattleController controller, int index)
        {
            battle = controller;
            enemyIndex = index;
            Refresh();
        }

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

            if (enemyImage != null)
            {
                enemyImage.raycastTarget = true;
            }

            rectTransform = GetComponent<RectTransform>();
            EnsureTargetArrow();
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

            var enemy = battle == null ? null : battle.GetEnemy(enemyIndex);
            var pose = battle == null ? EnemyPose.Idle : battle.GetEnemyPose(enemyIndex);
            gameObject.SetActive(battle != null && enemy != null && enemyIndex < battle.EnemyCount && !enemy.IsDead);
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
            else if (pose == EnemyPose.Attack && lastPose != EnemyPose.Attack)
            {
                PlayAttackHop();
            }

            if (hitRoutine == null)
            {
                enemyImage.color = Color.white;
            }

            enemyImage.preserveAspect = true;
            if (targetArrow != null)
            {
                targetArrow.gameObject.SetActive(battle != null && battle.SelectedEnemyIndex == enemyIndex && gameObject.activeSelf);
            }

            lastPose = pose;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            battle?.SelectEnemy(enemyIndex);
        }

        private void EnsureTargetArrow()
        {
            if (targetArrow != null)
            {
                return;
            }

            var arrowObject = new GameObject("Target Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            arrowObject.transform.SetParent(transform, false);
            var arrowRect = arrowObject.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 1f);
            arrowRect.anchorMax = new Vector2(0.5f, 1f);
            arrowRect.pivot = new Vector2(0.5f, 0f);
            arrowRect.anchoredPosition = new Vector2(0f, 10f);
            arrowRect.sizeDelta = new Vector2(80f, 48f);

            targetArrow = arrowObject.GetComponent<Text>();
            targetArrow.text = "▼";
            targetArrow.alignment = TextAnchor.MiddleCenter;
            targetArrow.fontSize = 34;
            targetArrow.fontStyle = FontStyle.Bold;
            targetArrow.raycastTarget = false;
            targetArrow.color = Color.black;
            targetArrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (targetArrow.font == null)
            {
                targetArrow.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var outline = arrowObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.45f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
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

        private void PlayAttackHop()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackHopRoutine());
        }

        private IEnumerator AttackHopRoutine()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            var originalPosition = rectTransform == null ? Vector2.zero : rectTransform.anchoredPosition;
            const float duration = 0.45f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var hop = Mathf.Sin(t * Mathf.PI) * 42f;
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = originalPosition + new Vector2(0f, hop);
                }

                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            attackRoutine = null;
        }
    }
}
