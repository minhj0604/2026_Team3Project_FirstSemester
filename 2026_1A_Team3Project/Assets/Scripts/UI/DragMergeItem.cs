using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class DragMergeItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ResourceFamily family;
        [SerializeField] private int stage;

        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform startParent;
        private Vector2 startPosition;
        private bool isDragging;
        private Text labelText;

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
                labelText = GetComponentInChildren<Text>(true);
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            BeginDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / Mathf.Max(1f, canvas.scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag(eventData.position);
        }

        private void BeginDrag()
        {
            if (isDragging)
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
                    ReturnToStart();
                    other.ReturnToStart();
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

        public void MarkPlacedInOven()
        {
            IsPlacedInOven = true;
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
            if (TryGetComponent<Image>(out var image) && sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
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
                labelText.text = $"{family}\nLv.{stage + 1}";
            }
        }

        private void EnsureLabel()
        {
            if (labelText != null)
            {
                return;
            }

            var textObject = new GameObject("Resource Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(-10f, -18f);
            textRect.offsetMax = new Vector2(10f, 10f);

            labelText = textObject.GetComponent<Text>();
            labelText.alignment = TextAnchor.LowerCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (labelText.font == null)
            {
                labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            labelText.fontSize = 10;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 7;
            labelText.resizeTextMaxSize = 12;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
        }
    }
}
