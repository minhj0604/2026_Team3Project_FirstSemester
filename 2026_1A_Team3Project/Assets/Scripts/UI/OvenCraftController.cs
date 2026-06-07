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
                    craftedScrollText.text = "Need base resource";
                }
                return;
            }

            if (scrollSlot != null && !scrollSlot.HasEmptyScroll)
            {
                if (craftedScrollText != null)
                {
                    craftedScrollText.text = "Need empty scroll";
                }
                return;
            }

            var card = ScrollCard.Craft(baseSlot.CurrentResource.Value, toppingSlot == null ? null : toppingSlot.CurrentResource);
            if (scrollSlot != null && scrollSlot.PlacedScrollItem != null)
            {
                scrollSlot.PlacedScrollItem.SetCraftedCard(card);
            }

            if (craftedScrollText != null)
            {
                craftedScrollText.text = $"{card.DisplayName}\nCost {card.Cost} / Power {card.Power}\nClick card to use";
            }

            baseSlot.ReturnPlacedObject();
            toppingSlot?.ReturnPlacedObject();
            scrollSlot?.ReturnPlacedObject();
        }

        private void TryAutoBake()
        {
            if (baseSlot != null)
            {
                return;
            }

            if (directBaseResource.HasValue && directScrollItem != null)
            {
                BakeDirect();
            }
        }

        private void BakeDirect()
        {
            if (!directBaseResource.HasValue)
            {
                SetResult("Need base resource");
                return;
            }

            if (directScrollItem == null)
            {
                SetResult("Need empty scroll");
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
                SetResult("Cannot craft scroll");
                directBaseItem?.ReturnToStart();
                directToppingItem?.ReturnToStart();
                directScrollItem.ReturnToOriginalSlot();
                ClearDirectSlots();
                return;
            }

            directScrollItem.SetCraftedCard(card);
            directScrollItem.ReturnToOriginalSlot();
            directBaseItem?.ReturnToStart();
            directToppingItem?.ReturnToStart();
            SetResult($"{card.DisplayName}\nCost {card.Cost} / Power {card.Power}\nClick card to use");

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
            target.SetParent(transform, false);
            if (target.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = role switch
                {
                    ResourceRole.Base => new Vector2(-72f, 18f),
                    ResourceRole.Topping => new Vector2(72f, 18f),
                    _ => new Vector2(0f, -54f)
                };
            }
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
