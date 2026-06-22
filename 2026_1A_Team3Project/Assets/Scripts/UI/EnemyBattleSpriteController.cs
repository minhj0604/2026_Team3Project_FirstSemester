using System.Collections;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        private Image formIcon;
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
            EnsureFormIcon();
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

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (battle == null || rectTransform == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Mouse.current.position.ReadValue(), camera))
            {
                battle.SelectEnemy(enemyIndex);
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
            enemyImage.sprite = ResolveEnemySprite(pose) ?? (pose switch
            {
                EnemyPose.Attack => attackSprite,
                EnemyPose.Hit => hitSprite,
                _ => idleSprite
            });
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

            UpdateFormIcon(enemy);

            lastPose = pose;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            battle?.SelectEnemy(enemyIndex);
        }

        private Sprite ResolveEnemySprite(EnemyPose pose)
        {
            var prefix = GetEnemySpritePrefix();
            if (string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            var suffix = pose switch
            {
                EnemyPose.Attack => "attack",
                EnemyPose.Hit => "hit",
                _ => "idle"
            };

            return RuntimeSpriteLoader.LoadFromAssetPath("Resource", "Character Sprites", $"{prefix}_{suffix}.png");
        }

        private string GetEnemySpritePrefix()
        {
            if (battle == null)
            {
                return null;
            }

            if (battle.StageIndex == 1)
            {
                return "slime";
            }

            if (battle.StageIndex == 2)
            {
                return enemyIndex == 0 ? "strawberry_guard" : "mandarin_guard";
            }

            if (battle.StageIndex == 3 && enemyIndex > 0)
            {
                var enemyName = battle.GetEnemy(enemyIndex)?.Name ?? string.Empty;
                if (enemyName.Contains("딸기"))
                {
                    return "strawberry_guard";
                }

                if (enemyName.Contains("감귤"))
                {
                    return "mandarin_guard";
                }
            }

            return null;
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
            arrowRect.anchoredPosition = new Vector2(0f, 64f);
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

        private void EnsureFormIcon()
        {
            if (formIcon != null)
            {
                return;
            }

            var iconObject = new GameObject("Slime Form Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            iconObject.transform.SetParent(transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 0f);
            iconRect.anchoredPosition = new Vector2(0f, 18f);
            iconRect.sizeDelta = new Vector2(46f, 46f);

            formIcon = iconObject.GetComponent<Image>();
            formIcon.raycastTarget = false;
            formIcon.preserveAspect = true;

            var outline = iconObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void UpdateFormIcon(CombatantState enemy)
        {
            EnsureFormIcon();
            if (formIcon == null)
            {
                return;
            }

            var shouldShow = enemy != null && enemy.ChangesFormOnWeaknessHit && gameObject.activeSelf;
            formIcon.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            var formElement = FormIndexToElement(enemy.FormIndex);
            formIcon.sprite = GetElementIcon(formElement);
            formIcon.color = GetElementIconColor(formElement, formIcon.sprite != null);
        }

        private static ElementType FormIndexToElement(int formIndex)
        {
            return formIndex switch
            {
                1 => ElementType.PoppingCandy,
                2 => ElementType.Marshmallow,
                3 => ElementType.Chocolate,
                _ => ElementType.Berry
            };
        }

        private static Sprite GetElementIcon(ElementType element)
        {
            return element switch
            {
                ElementType.Berry => RuntimeSpriteLoader.LoadFromAssetPath("Resource", "Merge Item", "\uB538\uAE30.png"),
                ElementType.Chocolate => RuntimeSpriteLoader.LoadFromAssetPath("Resource", "Merge Item", "\uCD08\uCF5C\uB9BF \uCCAD\uD06C.png"),
                ElementType.Marshmallow => RuntimeSpriteLoader.LoadFromAssetPath("Resource", "Merge Item", "\uB9C8\uC2DC\uBA5C\uB85C.png"),
                _ => null
            };
        }

        private static Color GetElementIconColor(ElementType element, bool hasSprite)
        {
            if (hasSprite)
            {
                return Color.white;
            }

            return element switch
            {
                ElementType.PoppingCandy => new Color(0.55f, 0.86f, 1f, 1f),
                ElementType.Chocolate => new Color(0.72f, 0.55f, 0.42f, 1f),
                ElementType.Marshmallow => new Color(0.92f, 0.92f, 0.88f, 1f),
                ElementType.Berry => new Color(1f, 0.62f, 0.72f, 1f),
                _ => Color.clear
            };
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
