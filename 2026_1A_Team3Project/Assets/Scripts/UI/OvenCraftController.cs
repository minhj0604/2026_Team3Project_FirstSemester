using System.Collections;
using System.Collections.Generic;
using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class OvenCraftController : MonoBehaviour
    {
        [SerializeField] private OvenDropSlot baseSlot;
        [SerializeField] private OvenDropSlot toppingSlot;
        [SerializeField] private OvenDropSlot scrollSlot;
        [SerializeField] private Text craftedScrollText;

        private MergeResource? directBaseResource;
        private MergeResource? directToppingResource;
        private DragMergeItem directBaseItem;
        private DragMergeItem directToppingItem;
        private DragScrollItem directScrollItem;
        private int directBaseIndex = -1;
        private int directToppingIndex = -1;
        private Transform directBaseAnchor;
        private Transform directToppingAnchor;
        private Transform directScrollAnchor;
        private RectTransform ovenLid;
        private Vector3 closedLidScale = Vector3.one;
        private Vector2 closedLidPosition;
        private Vector2 openLidPosition;
        private readonly List<RectTransform> ovenAnimationTargets = new();
        private readonly Dictionary<RectTransform, Vector3> ovenOriginalScales = new();
        private Coroutine ovenSquishRoutine;
        private bool isBaking;

        public void Configure(OvenDropSlot baseDropSlot, OvenDropSlot toppingDropSlot, Text resultText)
        {
            Configure(baseDropSlot, toppingDropSlot, null, resultText);
        }

        public void Configure(OvenDropSlot baseDropSlot, OvenDropSlot toppingDropSlot, OvenDropSlot emptyScrollSlot, Text resultText)
        {
            baseSlot = baseDropSlot;
            toppingSlot = toppingDropSlot;
            scrollSlot = emptyScrollSlot;
            craftedScrollText = resultText;
        }

        private void Awake()
        {
            ApplyOvenSprites();

            if (craftedScrollText == null)
            {
                var resultObject = GameObject.Find("Crafted Scroll Result");
                if (resultObject != null)
                {
                    craftedScrollText = resultObject.GetComponent<Text>();
                }
            }

            var lidObject = GameObject.Find("Oven Lid Visual");
            ovenLid = lidObject == null ? null : lidObject.GetComponent<RectTransform>();
            if (ovenLid != null)
            {
                closedLidScale = ovenLid.localScale;
                closedLidPosition = ovenLid.anchoredPosition;
                openLidPosition = closedLidPosition + new Vector2(0f, -86f);
                ovenLid.pivot = new Vector2(0.5f, 0.5f);
            }

            CacheOvenAnimationTargets();
        }

        private static void ApplyOvenSprites()
        {
            SetImageSprite("Oven Visual Panel", RuntimeSpriteLoader.LoadFromAssetPath("Resource", "oven", "\uC624\uBE10.png"));
            SetImageSprite("Oven Lid Visual", RuntimeSpriteLoader.LoadFromAssetPath("Resource", "oven", "\uC624\uBE10 \uB69C\uAED1.png"));
            MakeDropSlotInvisible("Oven Base Slot Visual");
            MakeDropSlotInvisible("Oven Topping Slot Visual");
            HideText("Oven Base Label");
            HideText("Oven Topping Label");
            HideText("Oven Plus Label");
        }

        private static void SetImageSprite(string objectName, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var target = GameObject.Find(objectName);
            if (target == null || !target.TryGetComponent<Image>(out var image))
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        private static void MakeDropSlotInvisible(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null || !target.TryGetComponent<Image>(out var image))
            {
                return;
            }

            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        private static void HideText(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null || !target.TryGetComponent<Text>(out var text))
            {
                return;
            }

            text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
            text.raycastTarget = false;
        }

        private void Update()
        {
            AnimateLid();
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            var rect = GetComponent<RectTransform>();
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
        }

        public bool TryPlaceResource(DragMergeItem item)
        {
            var battle = FindFirstObjectByType<BattleController>();
            if (battle != null && (battle.InputLocked || battle.Phase != BattlePhase.PlayerTurn))
            {
                return false;
            }

            if (item == null)
            {
                return false;
            }

            var resource = item.Resource;
            if (resource.Stage <= 0)
            {
                SetResult("먼저 합성");
                return false;
            }

            if (resource.Role == ResourceRole.Base)
            {
                if (directBaseItem != null && directBaseItem != item)
                {
                    directBaseItem.ReturnToStart();
                }

                directBaseResource = resource;
                directBaseItem = item;
                directBaseIndex = item.InventoryIndex;
            }
            else
            {
                if (directToppingItem != null && directToppingItem != item)
                {
                    directToppingItem.ReturnToStart();
                }

                directToppingResource = resource;
                directToppingItem = item;
                directToppingIndex = item.InventoryIndex;
            }

            item.MarkPlacedInOven();
            PlaceInOven(item.transform, resource.Role);
            TryAutoBake();
            return true;
        }

        public bool TryPlaceScroll(DragScrollItem item)
        {
            var battle = FindFirstObjectByType<BattleController>();
            if (battle != null && (battle.InputLocked || battle.Phase != BattlePhase.PlayerTurn))
            {
                return false;
            }

            if (item == null || !item.IsEmptyScroll)
            {
                return false;
            }

            directScrollItem = item;
            PlaceInOven(item.transform, null);
            SetLidOpen(true);
            TryAutoBake();
            return true;
        }

        public void Bake()
        {
            if (baseSlot == null && directBaseResource.HasValue)
            {
                BakeDirect();
                return;
            }

            if (baseSlot == null || !baseSlot.CurrentResource.HasValue)
            {
                if (craftedScrollText != null)
                {
                    craftedScrollText.text = "베이스 필요";
                }
                return;
            }

            if (scrollSlot != null && !scrollSlot.HasEmptyScroll)
            {
                if (craftedScrollText != null)
                {
                    craftedScrollText.text = "빈 스크롤 필요";
                }
                return;
            }

            var scrollItem = scrollSlot == null ? null : scrollSlot.PlacedScrollItem;
            var battle = FindFirstObjectByType<BattleController>();
            if (battle == null || scrollItem == null || !battle.TryCraftHandScroll(
                    scrollItem.HandIndex,
                    baseSlot.CurrentResource.Value,
                    toppingSlot == null ? null : toppingSlot.CurrentResource,
                    baseSlot.PlacedResourceItem == null ? -1 : baseSlot.PlacedResourceItem.InventoryIndex,
                    toppingSlot == null || toppingSlot.PlacedResourceItem == null ? -1 : toppingSlot.PlacedResourceItem.InventoryIndex,
                    out var card))
            {
                if (craftedScrollText != null)
                {
                    craftedScrollText.text = "제작 불가";
                }

                baseSlot.ReturnPlacedObject();
                toppingSlot?.ReturnPlacedObject();
                scrollSlot?.ReturnPlacedObject();
                return;
            }

            scrollItem.ReturnToOriginalSlot();
            if (craftedScrollText != null)
            {
                craftedScrollText.text = $"{card.DisplayName}\n비용 {card.Cost} / 위력 {card.Power}";
            }

            baseSlot.Clear();
            toppingSlot?.Clear();
            scrollSlot?.Clear();
        }

        private void TryAutoBake()
        {
            if (baseSlot != null)
            {
                return;
            }

            if (!isBaking && directBaseResource.HasValue && directToppingResource.HasValue && directScrollItem != null)
            {
                StartCoroutine(BakeDirectAfterSquish());
            }
        }

        private IEnumerator BakeDirectAfterSquish()
        {
            isBaking = true;
            SetLidOpen(false);
            SetPlacedObjectsVisible(false);
            PlayOvenSquish();
            yield return new WaitForSeconds(0.78f);
            BakeDirect();
            isBaking = false;
        }

        private void BakeDirect()
        {
            if (!directBaseResource.HasValue)
            {
                SetPlacedObjectsVisible(true);
                SetResult("베이스 필요");
                return;
            }

            if (directScrollItem == null)
            {
                SetPlacedObjectsVisible(true);
                SetResult("빈 스크롤 필요");
                return;
            }

            var battle = FindFirstObjectByType<BattleController>();
            if (battle == null || !battle.TryCraftHandScroll(
                    directScrollItem.HandIndex,
                    directBaseResource.Value,
                    directToppingResource,
                    directBaseIndex,
                    directToppingIndex,
                    out var card))
            {
                SetPlacedObjectsVisible(true);
                SetResult("제작 불가");
                directBaseItem?.ReturnToStart();
                directToppingItem?.ReturnToStart();
                directScrollItem.ReturnToOriginalSlot();
                ClearDirectSlots();
                return;
            }

            directScrollItem.ReturnToOriginalSlot();
            SetResult($"{card.DisplayName}\n비용 {card.Cost} / 위력 {card.Power}");
            SetLidOpen(false);

            ClearDirectSlots();
        }

        private void ClearDirectSlots()
        {
            directBaseResource = null;
            directToppingResource = null;
            directBaseItem = null;
            directToppingItem = null;
            directScrollItem = null;
            directBaseIndex = -1;
            directToppingIndex = -1;
        }

        private void PlaceInOven(Transform target, ResourceRole? role)
        {
            var anchor = GetPlacementAnchor(role);
            if (anchor == directScrollAnchor)
            {
                anchor.SetAsLastSibling();
            }

            target.SetParent(anchor, false);
            target.SetAsLastSibling();
            if (target.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = anchor == transform ? GetFallbackPosition(role) : Vector2.zero;
            }
        }

        private static Vector2 GetFallbackPosition(ResourceRole? role)
        {
            return role switch
            {
                ResourceRole.Base => new Vector2(-72f, 18f),
                ResourceRole.Topping => new Vector2(72f, 18f),
                _ => new Vector2(0f, 0f)
            };
        }

        private Transform GetPlacementAnchor(ResourceRole? role)
        {
            if (role == ResourceRole.Base)
            {
                return baseSlot != null ? baseSlot.transform : FindAnchor("Oven Base Slot Visual", ref directBaseAnchor);
            }

            if (role == ResourceRole.Topping)
            {
                return toppingSlot != null ? toppingSlot.transform : FindAnchor("Oven Topping Slot Visual", ref directToppingAnchor);
            }

            return scrollSlot != null ? scrollSlot.transform : GetScrollAnchor();
        }

        private Transform GetScrollAnchor()
        {
            if (directScrollAnchor != null)
            {
                return directScrollAnchor;
            }

            var anchorObject = new GameObject("Oven Scroll Anchor", typeof(RectTransform));
            var anchorRect = anchorObject.GetComponent<RectTransform>();
            var parent = ovenLid == null || ovenLid.parent == null ? transform : ovenLid.parent;
            anchorRect.SetParent(parent, false);
            if (ovenLid != null)
            {
                anchorRect.anchorMin = ovenLid.anchorMin;
                anchorRect.anchorMax = ovenLid.anchorMax;
                anchorRect.pivot = ovenLid.pivot;
                anchorRect.anchoredPosition = closedLidPosition + new Vector2(0f, -12f);
                anchorRect.sizeDelta = ovenLid.sizeDelta;
            }
            else if (transform is RectTransform rect)
            {
                anchorRect.anchorMin = rect.anchorMin;
                anchorRect.anchorMax = rect.anchorMax;
                anchorRect.pivot = rect.pivot;
                anchorRect.anchoredPosition = rect.anchoredPosition;
                anchorRect.sizeDelta = rect.sizeDelta;
            }

            directScrollAnchor = anchorRect;
            directScrollAnchor.SetAsLastSibling();
            return directScrollAnchor;
        }

        private Transform FindAnchor(string objectName, ref Transform cachedAnchor)
        {
            if (cachedAnchor != null)
            {
                return cachedAnchor;
            }

            var found = GameObject.Find(objectName);
            cachedAnchor = found == null ? transform : found.transform;
            return cachedAnchor;
        }

        private void SetResult(string value)
        {
            if (craftedScrollText != null)
            {
                craftedScrollText.text = value;
            }
        }

        private void AnimateLid()
        {
            if (ovenLid == null)
            {
                return;
            }

            var shouldOpen = !isBaking && (directScrollItem != null || IsEmptyScrollDraggingOver());
            var targetPosition = shouldOpen ? openLidPosition : closedLidPosition;
            ovenLid.anchoredPosition = Vector2.Lerp(ovenLid.anchoredPosition, targetPosition, Time.deltaTime * 9f);
            ovenLid.localScale = Vector3.Lerp(ovenLid.localScale, closedLidScale, Time.deltaTime * 9f);
        }

        private bool IsEmptyScrollDraggingOver()
        {
            if (Mouse.current == null)
            {
                return false;
            }

            var mousePosition = Mouse.current.position.ReadValue();
            foreach (var scroll in FindObjectsOfType<DragScrollItem>())
            {
                if (scroll != null && scroll.IsDragging && scroll.IsEmptyScroll && ContainsScreenPoint(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetLidOpen(bool open)
        {
            if (ovenLid != null)
            {
                ovenLid.anchoredPosition = open ? openLidPosition : closedLidPosition;
                ovenLid.localScale = closedLidScale;
            }
        }

        private void PlayOvenSquish()
        {
            if (ovenSquishRoutine != null)
            {
                StopCoroutine(ovenSquishRoutine);
            }

            ovenSquishRoutine = StartCoroutine(OvenSquishRoutine());
        }

        private IEnumerator OvenSquishRoutine()
        {
            if (ovenAnimationTargets.Count == 0)
            {
                CacheOvenAnimationTargets();
            }

            const float duration = 0.75f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var wave = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                foreach (var target in ovenAnimationTargets)
                {
                    if (target == null || !ovenOriginalScales.TryGetValue(target, out var originalScale))
                    {
                        continue;
                    }

                    target.localScale = new Vector3(originalScale.x * (1f + wave * 0.16f), originalScale.y * (1f - wave * 0.12f), originalScale.z);
                }

                yield return null;
            }

            foreach (var target in ovenAnimationTargets)
            {
                if (target != null && ovenOriginalScales.TryGetValue(target, out var originalScale))
                {
                    target.localScale = originalScale;
                }
            }

            ovenSquishRoutine = null;
        }

        private void SetPlacedObjectsVisible(bool visible)
        {
            SetObjectVisible(directBaseItem, visible);
            SetObjectVisible(directToppingItem, visible);
            SetObjectVisible(directScrollItem, visible);
        }

        private static void SetObjectVisible(Component component, bool visible)
        {
            if (component == null)
            {
                return;
            }

            var canvasGroup = component.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = component.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
        }

        private void CacheOvenAnimationTargets()
        {
            ovenAnimationTargets.Clear();
            ovenOriginalScales.Clear();
            foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect == null || rect.transform == transform || rect.name == "Oven Craft Area" || rect.name == "Oven Scroll Anchor")
                {
                    continue;
                }

                if (!rect.name.StartsWith("Oven"))
                {
                    continue;
                }

                ovenAnimationTargets.Add(rect);
                ovenOriginalScales[rect] = rect.localScale;
            }
        }
    }
}
