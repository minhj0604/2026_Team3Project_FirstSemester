using System.Collections;
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
        private bool selectedForPlay;
        private Coroutine motionRoutine;
        private float lastCraftedClickTime = -10f;
        private Transform homeParent;
        private Vector2 homePosition;
        private bool hasHomeSlot;
        private static DragScrollItem selectedItem;

        public bool IsEmptyScroll => !hasCraftedCard;
        public bool IsDragging => isDragging;
        public bool IsSelectedForPlay => selectedForPlay;
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

        private void OnDisable()
        {
            if (selectedItem == this)
            {
                selectedItem = null;
            }
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

            RememberHomeSlot();
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
                HandleCraftedCardClick();
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

            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalPosition;
                return;
            }

            RestoreHomePlacement(false);
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

            var battle = Object.FindFirstObjectByType<BattleController>();
            if (battle != null && (battle.InputLocked || battle.Phase != BattlePhase.PlayerTurn))
            {
                return;
            }

            if (!selectedForPlay)
            {
                originalParent = transform.parent;
                originalPosition = rectTransform.anchoredPosition;
            }

            dragStartMousePosition = screenPosition;
            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
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
            if (selectedForPlay && moved >= 12f && IsDroppedBackToHand(screenPosition))
            {
                CancelSelection();
                isDragging = false;
                return;
            }

            if (hasCraftedCard && moved < 12f)
            {
                HandleCraftedCardClick();
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
                image.color = card.IsContaminated ? new Color(0.38f, 0.72f, 0.28f, 1f) : GetScrollColor(card.ToppingFamily);
            }

            EnsureScrollOutline();
            if (scrollOutline != null)
            {
                scrollOutline.enabled = true;
                scrollOutline.effectColor = card.IsContaminated ? new Color(0.14f, 0.38f, 0.1f, 1f) : GetScrollOutlineColor(card.ToppingFamily);
                scrollOutline.effectDistance = card.IsContaminated ? new Vector2(5f, -5f) : new Vector2(3f, -3f);
            }

            EnsureCardText();
            ConfigureCardTextLayout();
            EnsureIngredientIcons();
            SetIngredientIcons(card);
            if (cardText != null)
            {
                var statusLine = card.IsContaminated ? "\n오염" : string.Empty;
                cardText.text = $"{card.DisplayName}{statusLine}\n비용 {card.Cost}\n위력 {card.Power}";
                cardText.color = card.IsContaminated ? new Color(0.03f, 0.12f, 0.03f, 1f) : new Color(0.15f, 0.08f, 0.03f, 1f);
            }
        }

        public void ReturnToOriginalSlot()
        {
            StopMotion();
            selectedForPlay = false;
            if (selectedItem == this)
            {
                selectedItem = null;
            }

            isDragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalPosition;
                rectTransform.localScale = Vector3.one;
                return;
            }

            RestoreHomePlacement(false);
        }

        public void CancelSelection()
        {
            StopMotion();
            selectedForPlay = false;
            if (selectedItem == this)
            {
                selectedItem = null;
            }

            originalParent = null;
            isDragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            RestoreHomePlacement(false);
        }

        private void RememberHomeSlot()
        {
            if (hasHomeSlot || rectTransform == null || transform.parent == null)
            {
                return;
            }

            homeParent = transform.parent;
            homePosition = rectTransform.anchoredPosition;
            hasHomeSlot = true;
        }

        private void RestoreHomePlacement(bool stopMotion)
        {
            if (stopMotion)
            {
                StopMotion();
            }

            if (!hasHomeSlot || rectTransform == null || homeParent == null)
            {
                return;
            }

            if (transform.parent != homeParent)
            {
                transform.SetParent(homeParent, false);
            }

            rectTransform.anchoredPosition = homePosition;
            rectTransform.localScale = Vector3.one;
        }

        private void StopMotion()
        {
            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
                motionRoutine = null;
            }
        }

        private void HandleCraftedCardClick()
        {
            if (Time.unscaledTime - lastCraftedClickTime < 0.08f)
            {
                return;
            }

            lastCraftedClickTime = Time.unscaledTime;
            var battle = FindFirstObjectByType<BattleController>();
            if (battle == null || battle.InputLocked || battle.Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            if (battle.CardResetModeActive)
            {
                if (battle.TryResetCardById(boundCardId))
                {
                    ReturnToOriginalSlot();
                    FindFirstObjectByType<BattleHud>()?.Refresh();
                }

                return;
            }

            if (selectedItem != null && selectedItem != this)
            {
                selectedItem.CancelSelection();
            }

            if (!selectedForPlay)
            {
                MoveToPreviewSlot();
                return;
            }

            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
            }

            motionRoutine = StartCoroutine(PlayCraftedCardRoutine());
        }

        private void MoveToPreviewSlot()
        {
            if (canvas == null || rectTransform == null)
            {
                CacheComponents();
            }

            if (canvas == null || rectTransform == null)
            {
                return;
            }

            if (!selectedForPlay)
            {
                originalParent = transform.parent;
                originalPosition = rectTransform.anchoredPosition;
            }

            selectedForPlay = true;
            selectedItem = this;
            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
            var target = GetPreviewPosition();
            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
            }

            motionRoutine = StartCoroutine(MoveToRoutine(target, 0.22f));
        }

        private IEnumerator PlayCraftedCardRoutine()
        {
            var battle = FindFirstObjectByType<BattleController>();
            if (battle == null)
            {
                ReturnToOriginalSlot();
                yield break;
            }

            if (craftedCard != null && craftedCard.TargetsEnemy)
            {
                var target = GetNamedCanvasPosition("Enemy Character", rectTransform.anchoredPosition + new Vector2(0f, 160f));
                yield return MoveToRoutine(target, 0.28f);
            }
            else
            {
                yield return FadeRoutine(0.25f);
            }

            if (!battle.TryPlayCard(craftedCard))
            {
                ReturnToOriginalSlot();
                yield break;
            }

            ReturnToOriginalSlot();
            isDragging = false;
            selectedForPlay = false;
            FindFirstObjectByType<BattleHud>()?.Refresh();
        }

        public void SetHandState(bool active, int handIndex, ScrollCard card)
        {
            var wasActive = gameObject.activeSelf;
            var previousCardId = boundCardId;
            var incomingCardId = card == null ? -1 : card.Id;
            var sameCard = wasActive && previousCardId == incomingCardId;
            HandIndex = active ? handIndex : -1;
            if (!isDragging && !selectedForPlay && !sameCard)
            {
                RestoreHomePlacement(true);
            }

            if (!active)
            {
                ResetToEmptyScroll();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (card == null || card.IsEmpty)
            {
                ResetToEmptyScroll(incomingCardId);
                if (!sameCard)
                {
                    PlayEnterFromRight();
                }
                return;
            }

            SetCraftedCard(card);
            if (!sameCard)
            {
                PlayEnterFromRight();
            }
        }

        private void ResetToEmptyScroll(int cardId = -1)
        {
            hasCraftedCard = false;
            craftedCard = null;
            boundCardId = cardId;
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

        private bool IsDroppedBackToHand(Vector2 screenPosition)
        {
            if (canvas == null || rectTransform == null)
            {
                CacheComponents();
            }

            var handArea = GameObject.Find("Hand Area");
            if (handArea != null && handArea.TryGetComponent<RectTransform>(out var handRect))
            {
                var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (RectTransformUtility.RectangleContainsScreenPoint(handRect, screenPosition, camera))
                {
                    return true;
                }
            }

            if (!hasHomeSlot || canvas == null)
            {
                return false;
            }

            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                return false;
            }

            var cameraForCanvas = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var homeWorld = homeParent is RectTransform parentRect
                ? parentRect.TransformPoint(homePosition)
                : transform.TransformPoint(homePosition);
            var homeScreen = RectTransformUtility.WorldToScreenPoint(cameraForCanvas, homeWorld);
            return Vector2.Distance(screenPosition, homeScreen) < 180f;
        }

        private void PlayEnterFromRight()
        {
            if (!gameObject.activeInHierarchy || rectTransform == null || isDragging)
            {
                return;
            }

            StopMotion();

            if (!selectedForPlay)
            {
                RestoreHomePlacement(false);
            }

            var target = hasHomeSlot ? homePosition : rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = target + new Vector2(160f, 0f);
            motionRoutine = StartCoroutine(MoveToRoutine(target, 0.42f));
        }

        private Vector2 GetPreviewPosition()
        {
            var resourceStorage = GameObject.Find("Resource Storage");
            var canvasRect = canvas == null ? null : canvas.GetComponent<RectTransform>();
            if (resourceStorage != null && canvasRect != null && resourceStorage.TryGetComponent<RectTransform>(out var storageRect))
            {
                var worldPoint = storageRect.TransformPoint(new Vector3(storageRect.rect.width * 0.5f + 82f, 0f, 0f));
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    RectTransformUtility.WorldToScreenPoint(camera, worldPoint),
                    camera,
                    out var localPoint);
                return localPoint;
            }

            return rectTransform.anchoredPosition + new Vector2(0f, 130f);
        }

        private Vector2 GetNamedCanvasPosition(string objectName, Vector2 fallback)
        {
            var targetObject = GameObject.Find(objectName);
            var canvasRect = canvas == null ? null : canvas.GetComponent<RectTransform>();
            if (targetObject == null || canvasRect == null || !targetObject.TryGetComponent<RectTransform>(out var targetRect))
            {
                return fallback;
            }

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(camera, targetRect.position),
                camera,
                out var localPoint);
            return localPoint;
        }

        private IEnumerator MoveToRoutine(Vector2 target, float duration)
        {
            if (rectTransform == null)
            {
                yield break;
            }

            var start = rectTransform.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                rectTransform.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }

            rectTransform.anchoredPosition = target;
            motionRoutine = null;
        }

        private IEnumerator FadeRoutine(float duration)
        {
            if (canvasGroup == null)
            {
                CacheComponents();
            }

            var startAlpha = canvasGroup == null ? 1f : canvasGroup.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration));
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
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
            cardText.fontSize = 22;
            cardText.resizeTextForBestFit = true;
            cardText.resizeTextMinSize = 16;
            cardText.resizeTextMaxSize = 24;
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
            textRect.offsetMin = new Vector2(3f, 6f);
            textRect.offsetMax = new Vector2(-3f, -30f);
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
