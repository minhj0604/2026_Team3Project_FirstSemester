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
        private Image baseIcon;
        private Image toppingIcon;
        private Outline scrollOutline;
        private int boundCardId = -1;

        public bool IsEmptyScroll => !hasCraftedCard;
        public bool IsDragging => isDragging;
        public int HandIndex { get; private set; } = -1;
        public int BoundCardId => boundCardId;

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
            boundCardId = card == null ? -1 : card.Id;
            hasCraftedCard = true;
            if (TryGetComponent<Image>(out var image))
            {
                image.color = GetScrollColor(card.ToppingFamily);
            }

            EnsureScrollOutline();
            if (scrollOutline != null)
            {
                scrollOutline.enabled = true;
                scrollOutline.effectColor = GetScrollOutlineColor(card.ToppingFamily);
                scrollOutline.effectDistance = new Vector2(3f, -3f);
            }

            EnsureCardText();
            ConfigureCardTextLayout();
            EnsureIngredientIcons();
            SetIngredientIcons(card);
            if (cardText != null)
            {
                cardText.text = $"{card.DisplayName}\n비용 {card.Cost}\n위력 {card.Power}";
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
            if (battle == null || !battle.TryPlayCard(craftedCard))
            {
                ReturnToOriginalSlot();
                return;
            }

            ReturnToOriginalSlot();
            isDragging = false;
            ResetToEmptyScroll();
            gameObject.SetActive(false);
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
            boundCardId = -1;
            if (TryGetComponent<Image>(out var image))
            {
                image.color = Color.white;
            }

            if (scrollOutline != null)
            {
                scrollOutline.enabled = false;
            }

            if (cardText != null)
            {
                cardText.text = string.Empty;
            }

            SetIconActive(baseIcon, false);
            SetIconActive(toppingIcon, false);
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

        private void EnsureScrollOutline()
        {
            if (scrollOutline == null)
            {
                scrollOutline = GetComponent<Outline>();
            }

            if (scrollOutline == null)
            {
                scrollOutline = gameObject.AddComponent<Outline>();
            }
        }

        private void ConfigureCardTextLayout()
        {
            if (cardText == null || !cardText.TryGetComponent<RectTransform>(out var textRect))
            {
                return;
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 4f);
            textRect.offsetMax = new Vector2(-4f, -34f);
            cardText.alignment = TextAnchor.MiddleCenter;
        }

        private void EnsureIngredientIcons()
        {
            if (baseIcon == null)
            {
                baseIcon = CreateIngredientIcon("Base Ingredient Icon", new Vector2(-17f, -8f));
            }

            if (toppingIcon == null)
            {
                toppingIcon = CreateIngredientIcon("Topping Ingredient Icon", new Vector2(17f, -8f));
            }
        }

        private Image CreateIngredientIcon(string iconName, Vector2 anchoredPosition)
        {
            var iconObject = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = anchoredPosition;
            iconRect.sizeDelta = new Vector2(28f, 28f);

            var icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private void SetIngredientIcons(ScrollCard card)
        {
            if (card == null)
            {
                SetIconActive(baseIcon, false);
                SetIconActive(toppingIcon, false);
                return;
            }

            SetIcon(baseIcon, new MergeResource(card.BaseFamily, card.BaseStage));
            SetIcon(toppingIcon, new MergeResource(card.ToppingFamily, card.ToppingStage));
        }

        private static void SetIcon(Image icon, MergeResource resource)
        {
            if (icon == null)
            {
                return;
            }

            if (MergeResourceVisuals.TryGetSprite(resource, out var sprite))
            {
                icon.sprite = sprite;
                icon.color = MergeResourceVisuals.GetTint(resource);
            }
            else
            {
                icon.sprite = null;
                icon.color = GetIngredientFallbackColor(resource.Family);
            }

            SetIconActive(icon, true);
        }

        private static void SetIconActive(Image icon, bool active)
        {
            if (icon != null)
            {
                icon.gameObject.SetActive(active);
            }
        }

        private static Color GetScrollColor(ResourceFamily family)
        {
            return family switch
            {
                ResourceFamily.Berry => new Color(1f, 0.75f, 0.82f, 1f),
                ResourceFamily.Chocolate => new Color(0.78f, 0.62f, 0.48f, 1f),
                ResourceFamily.Marshmallow => new Color(0.96f, 0.96f, 0.91f, 1f),
                ResourceFamily.PoppingCandy => new Color(0.72f, 0.9f, 1f, 1f),
                _ => new Color(1f, 0.92f, 0.62f, 1f)
            };
        }

        private static Color GetScrollOutlineColor(ResourceFamily family)
        {
            return family switch
            {
                ResourceFamily.Berry => new Color(0.88f, 0.42f, 0.55f, 1f),
                ResourceFamily.Chocolate => new Color(0.48f, 0.34f, 0.24f, 1f),
                ResourceFamily.Marshmallow => new Color(0.6f, 0.6f, 0.56f, 1f),
                ResourceFamily.PoppingCandy => new Color(0.35f, 0.67f, 0.86f, 1f),
                _ => new Color(0.78f, 0.62f, 0.34f, 1f)
            };
        }

        private static Color GetIngredientFallbackColor(ResourceFamily family)
        {
            return family switch
            {
                ResourceFamily.Sugar => new Color(0.95f, 0.9f, 0.62f, 1f),
                ResourceFamily.Dough => new Color(0.78f, 0.58f, 0.36f, 1f),
                ResourceFamily.Dairy => new Color(0.9f, 0.95f, 1f, 1f),
                ResourceFamily.Egg => new Color(1f, 0.86f, 0.45f, 1f),
                ResourceFamily.Berry => new Color(1f, 0.75f, 0.82f, 1f),
                ResourceFamily.Chocolate => new Color(0.78f, 0.62f, 0.48f, 1f),
                ResourceFamily.Marshmallow => new Color(0.98f, 0.97f, 0.92f, 1f),
                ResourceFamily.PoppingCandy => new Color(0.72f, 0.9f, 1f, 1f),
                _ => Color.gray
            };
        }
    }
}
