using Team3Project.GameSystems;
using UnityEngine;
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
            if (craftedScrollText == null)
            {
                var resultObject = GameObject.Find("Crafted Scroll Result");
                if (resultObject != null)
                {
                    craftedScrollText = resultObject.GetComponent<Text>();
                }
            }
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
            if (item == null || !item.IsEmptyScroll)
            {
                return false;
            }

            directScrollItem = item;
            PlaceInOven(item.transform, null);
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

            scrollItem.SetCraftedCard(card);
            if (craftedScrollText != null)
            {
                craftedScrollText.text = $"{card.DisplayName}\n비용 {card.Cost} / 위력 {card.Power}";
            }

            baseSlot.ConsumePlacedResource();
            toppingSlot?.ConsumePlacedResource();
            scrollSlot?.ReleasePlacedScroll();
        }

        private void TryAutoBake()
        {
            if (baseSlot != null)
            {
                return;
            }

            if (directBaseResource.HasValue && directToppingResource.HasValue && directScrollItem != null)
            {
                BakeDirect();
            }
        }

        private void BakeDirect()
        {
            if (!directBaseResource.HasValue)
            {
                SetResult("베이스 필요");
                return;
            }

            if (directScrollItem == null)
            {
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
                SetResult("제작 불가");
                directBaseItem?.ReturnToStart();
                directToppingItem?.ReturnToStart();
                directScrollItem.ReturnToOriginalSlot();
                ClearDirectSlots();
                return;
            }

            directScrollItem.SetCraftedCard(card);
            directScrollItem.ReturnToOriginalSlot();
            directBaseItem?.ConsumeFromOven();
            directToppingItem?.ConsumeFromOven();
            SetResult($"{card.DisplayName}\n비용 {card.Cost} / 위력 {card.Power}");

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
            target.SetParent(anchor, false);
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
                _ => new Vector2(0f, -54f)
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

            return scrollSlot != null ? scrollSlot.transform : FindAnchor("Oven Lid Visual", ref directScrollAnchor);
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
    }
}
