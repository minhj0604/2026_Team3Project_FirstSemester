using System.Collections;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class DragMergeItem : MonoBehaviour
    {
        [SerializeField] private ResourceFamily family;
        [SerializeField] private int stage;

        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform startParent;
        private Vector2 startPosition;
        private Vector2 startScreenPosition;
        private bool isDragging;
        private Text labelText;
        private Coroutine popRoutine;
        private const string LabelObjectName = "Resource Level Label";

        public MergeResource Resource => new(family, stage);
        public bool IsDragging => isDragging;
        public bool IsPlacedInOven { get; private set; }
        public int InventoryIndex { get; private set; } = -1;

        public void Configure(ResourceFamily resourceFamily, int resourceStage)
        {
            family = resourceFamily;
            stage = resourceStage;
            EnsureLabel();
            UpdateLabel();
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            isDragging = false;
        }

        private void CacheComponents()
        {
            canvas = GetComponentInParent<Canvas>();
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (labelText == null)
            {
                var labelTransform = transform.Find(LabelObjectName);
                labelText = labelTransform == null ? null : labelTransform.GetComponent<Text>();
            }
        }

        private void Update()
        {
            if (canvas == null || rectTransform == null || canvasGroup == null)
            {
                CacheComponents();
            }

            if (Mouse.current == null || canvas == null || rectTransform == null)
            {
                return;
            }

            var mousePosition = Mouse.current.position.ReadValue();
            if (!isDragging && Mouse.current.leftButton.wasPressedThisFrame && Contains(mousePosition))
            {
                BeginDrag();
            }

            if (!isDragging)
            {
                return;
            }

            MoveToMouse(mousePosition);
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndDrag(mousePosition);
            }
        }

        private void BeginDrag()
        {
            if (isDragging)
            {
                return;
            }

            var battle = Object.FindFirstObjectByType<BattleController>();
            if (battle != null && (battle.InputLocked || battle.Phase != BattlePhase.PlayerTurn))
            {
                return;
            }

            if (canvas == null || rectTransform == null || canvasGroup == null)
            {
                CacheComponents();
            }

            if (canvas == null || rectTransform == null || canvasGroup == null)
            {
                return;
            }

            startParent = transform.parent;
            startPosition = rectTransform.anchoredPosition;
            startScreenPosition = Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(canvas.transform, true);
            isDragging = true;
        }

        private void EndDrag(Vector2 screenPosition)
        {
            if (!isDragging)
            {
                return;
            }

            canvasGroup.blocksRaycasts = true;
            isDragging = false;
            if ((screenPosition - startScreenPosition).sqrMagnitude < 64f)
            {
                ReturnToStart();
                return;
            }

            DropOrReturn(screenPosition);
        }

        private void DropOrReturn(Vector2 screenPosition)
        {
            foreach (var slot in Object.FindObjectsOfType<OvenDropSlot>())
            {
                if (slot.ContainsScreenPoint(screenPosition) && slot.TryPlaceResource(this))
                {
                    return;
                }
            }

            foreach (var oven in Object.FindObjectsOfType<OvenCraftController>())
            {
                if (oven.ContainsScreenPoint(screenPosition) && oven.TryPlaceResource(this))
                {
                    return;
                }
            }

            foreach (var other in Object.FindObjectsOfType<DragMergeItem>())
            {
                if (other == this || !other.isActiveAndEnabled || !other.Contains(screenPosition))
                {
                    continue;
                }

                var battle = Object.FindFirstObjectByType<BattleController>();
                if (battle != null && battle.TryMergeResourceSlots(InventoryIndex, other.InventoryIndex))
                {
                    return;
                }
            }

            transform.SetParent(startParent, false);
            rectTransform.anchoredPosition = startPosition;
        }

        private bool Contains(Vector2 screenPosition)
        {
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, camera);
        }

        private void MoveToMouse(Vector2 screenPosition)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out var localPoint))
            {
                rectTransform.localPosition = localPoint;
            }
        }

        public void ReturnToStart()
        {
            if (startParent != null)
            {
                transform.SetParent(startParent, false);
                rectTransform.anchoredPosition = startPosition;
            }

            IsPlacedInOven = false;
        }

        public void ConsumeFromOven()
        {
            IsPlacedInOven = false;
            InventoryIndex = -1;
            gameObject.SetActive(false);
        }

        public void ClearInventorySlot()
        {
            isDragging = false;
            IsPlacedInOven = false;
            InventoryIndex = -1;
            gameObject.SetActive(false);
        }

        public void MarkPlacedInOven()
        {
            IsPlacedInOven = true;
        }

        public void SetInventorySlot(Transform parent, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
            {
                CacheComponents();
            }

            transform.SetParent(parent, false);
            startParent = parent;
            startPosition = anchoredPosition;
            rectTransform.anchoredPosition = anchoredPosition;
            IsPlacedInOven = false;
        }

        public void SetInventoryState(MergeResource resource, Sprite sprite, bool active, int inventoryIndex)
        {
            InventoryIndex = active ? inventoryIndex : -1;
            gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            Configure(resource.Family, resource.Stage);
            IsPlacedInOven = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (TryGetComponent<Image>(out var image))
            {
                image.sprite = sprite;
                image.color = sprite == null ? GetFallbackColor(resource.Family) : MergeResourceVisuals.GetTint(resource);
            }

            if (startParent != null && rectTransform != null)
            {
                ReturnToStart();
            }
        }

        private void UpdateLabel()
        {
            if (labelText != null)
            {
                labelText.text = stage.ToString();
            }
        }

        private void EnsureLabel()
        {
            if (labelText == null)
            {
                var labelTransform = transform.Find(LabelObjectName);
                if (labelTransform != null)
                {
                    labelText = labelTransform.GetComponent<Text>();
                }
            }

            if (labelText == null)
            {
                var textObject = new GameObject(LabelObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(transform, false);
                labelText = textObject.GetComponent<Text>();
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (labelText.font == null)
                {
                    labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            labelText.transform.SetAsLastSibling();
            var textRect = labelText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 2f);
            textRect.sizeDelta = new Vector2(0f, 20f);

            labelText.alignment = TextAnchor.LowerCenter;
            labelText.fontSize = 16;
            labelText.fontStyle = FontStyle.Bold;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 10;
            labelText.resizeTextMaxSize = 18;
            labelText.color = new Color(0.08f, 0.07f, 0.06f, 1f);
            labelText.raycastTarget = false;

            var outline = labelText.GetComponent<Outline>();
            if (outline == null)
            {
                outline = labelText.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(1f, 1f, 1f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public void PlayMergePop()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
            }

            popRoutine = StartCoroutine(MergePopRoutine());
        }

        private IEnumerator MergePopRoutine()
        {
            var originalScale = transform.localScale;
            const float duration = 0.32f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.35f
                    ? Mathf.Lerp(0.78f, 1.22f, t / 0.35f)
                    : Mathf.Lerp(1.22f, 1f, (t - 0.35f) / 0.65f);

                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
            popRoutine = null;
        }

        private static Color GetFallbackColor(ResourceFamily resourceFamily)
        {
            return resourceFamily switch
            {
                ResourceFamily.Sugar => new Color(0.95f, 0.9f, 0.62f, 1f),
                ResourceFamily.Dough => new Color(0.78f, 0.58f, 0.36f, 1f),
                ResourceFamily.Dairy => new Color(0.9f, 0.95f, 1f, 1f),
                ResourceFamily.Egg => new Color(1f, 0.86f, 0.45f, 1f),
                ResourceFamily.Berry => new Color(0.9f, 0.18f, 0.28f, 1f),
                ResourceFamily.Chocolate => new Color(0.22f, 0.11f, 0.07f, 1f),
                ResourceFamily.Marshmallow => new Color(0.96f, 0.96f, 0.9f, 1f),
                ResourceFamily.PoppingCandy => new Color(0.4f, 0.9f, 1f, 1f),
                _ => Color.gray
            };
        }
    }
}
