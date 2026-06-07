using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Team3Project.GameSystems;

namespace Team3Project.UI
{
    public class DragScrollItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalPosition;
        private Vector2 dragStartMousePosition;
        private ScrollCard craftedCard;
        private bool isDragging;
        private bool hasCraftedCard;
        private Text cardText;

        public bool IsEmptyScroll => !hasCraftedCard;
        public bool IsDragging => isDragging;
        public int HandIndex { get; private set; } = -1;

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
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (cardText == null)
            {
                cardText = GetComponentInChildren<Text>(true);
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
                BeginDrag(mousePosition);
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
            BeginDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / Mathf.Max(1f, canvas.scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hasCraftedCard)
            {
                TryPlayCraftedCard();
            }
        }

        private void DropOrReturn(Vector2 screenPosition)
        {
            foreach (var slot in Object.FindObjectsOfType<OvenDropSlot>())
            {
                if (slot.ContainsScreenPoint(screenPosition) && slot.TryPlaceScroll(this))
                {
                    return;
                }
            }

            foreach (var oven in Object.FindObjectsOfType<OvenCraftController>())
            {
                if (oven.ContainsScreenPoint(screenPosition) && oven.TryPlaceScroll(this))
                {
                    return;
                }
            }

            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalPosition;
        }

        private void BeginDrag(Vector2 screenPosition)
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

            originalParent = transform.parent;
            originalPosition = rectTransform.anchoredPosition;
            dragStartMousePosition = screenPosition;
            transform.SetParent(canvas.transform, true);
            canvasGroup.blocksRaycasts = false;
            isDragging = true;
        }

        private void EndDrag(Vector2 screenPosition)
        {
            if (!isDragging)
            {
                return;
            }

            canvasGroup.blocksRaycasts = true;
            var moved = Vector2.Distance(dragStartMousePosition, screenPosition);
            if (hasCraftedCard && moved < 12f)
            {
                TryPlayCraftedCard();
            }
            else
            {
                DropOrReturn(screenPosition);
            }

            isDragging = false;
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

        public void SetCraftedCard(ScrollCard card)
        {
            craftedCard = card;
            hasCraftedCard = true;
            if (TryGetComponent<Image>(out var image))
            {
                image.color = new Color(1f, 0.92f, 0.62f, 1f);
            }

            EnsureCardText();
            if (cardText != null)
            {
                cardText.text = $"{card.DisplayName}\nCost {card.Cost}\nPower {card.Power}";
                cardText.color = new Color(0.15f, 0.08f, 0.03f, 1f);
            }
        }

        public void ReturnToOriginalSlot()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalPosition;
            }
        }

        private void TryPlayCraftedCard()
        {
            var battle = FindFirstObjectByType<BattleController>();
            if (battle == null || !battle.TryPlayHandCard(HandIndex))
            {
                ReturnToOriginalSlot();
                return;
            }

            ReturnToOriginalSlot();
        }

        public void SetHandState(bool active, int handIndex, ScrollCard card)
        {
            HandIndex = active ? handIndex : -1;
            if (!active)
            {
                ResetToEmptyScroll();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (card == null || card.IsEmpty)
            {
                ResetToEmptyScroll();
                return;
            }

            SetCraftedCard(card);
        }

        private void ResetToEmptyScroll()
        {
            hasCraftedCard = false;
            craftedCard = null;
            if (TryGetComponent<Image>(out var image))
            {
                image.color = Color.white;
            }

            if (cardText != null)
            {
                cardText.text = string.Empty;
            }
        }

        private void EnsureCardText()
        {
            if (cardText != null)
            {
                return;
            }

            var textObject = new GameObject("Crafted Card Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 4f);
            textRect.offsetMax = new Vector2(-4f, -4f);

            cardText = textObject.GetComponent<Text>();
            cardText.alignment = TextAnchor.MiddleCenter;
            cardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cardText.font == null)
            {
                cardText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            cardText.fontSize = 12;
            cardText.resizeTextForBestFit = true;
            cardText.resizeTextMinSize = 8;
            cardText.resizeTextMaxSize = 14;
            cardText.raycastTarget = false;
        }
    }
}
