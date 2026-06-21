using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class OvenDropSlot : MonoBehaviour, IDropHandler
    {
        public enum SlotKind
        {
            Base,
            Topping,
            Scroll
        }

        [SerializeField] private SlotKind slotKind;
        [SerializeField] private Text labelText;

        private GameObject placedObject;
        private DragMergeItem placedResourceItem;
        private DragScrollItem placedScrollItem;

        public MergeResource? CurrentResource { get; private set; }
        public bool HasEmptyScroll { get; private set; }
        public DragMergeItem PlacedResourceItem => placedResourceItem;
        public DragScrollItem PlacedScrollItem => placedScrollItem;

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            var rect = GetComponent<RectTransform>();
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
        }

        public void Configure(SlotKind kind, Text label)
        {
            slotKind = kind;
            labelText = label;
            Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (slotKind == SlotKind.Scroll)
            {
                DropScroll(eventData);
                return;
            }

            var item = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<DragMergeItem>();
            if (item == null)
            {
                return;
            }

            var resource = item.Resource;
            if (slotKind == SlotKind.Base && resource.Role != ResourceRole.Base)
            {
                return;
            }

            if (slotKind == SlotKind.Topping && resource.Role != ResourceRole.Topping)
            {
                return;
            }

            CurrentResource = resource;
            placedObject = item.gameObject;
            placedResourceItem = item;
            item.MarkPlacedInOven();
            item.transform.SetParent(transform, false);
            if (item.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }

            if (labelText != null)
            {
                labelText.text = resource.DisplayName;
            }
        }

        public bool TryPlaceResource(DragMergeItem item)
        {
            if (item == null || slotKind == SlotKind.Scroll)
            {
                return false;
            }

            var resource = item.Resource;
            if (slotKind == SlotKind.Base && resource.Role != ResourceRole.Base)
            {
                return false;
            }

            if (slotKind == SlotKind.Topping && resource.Role != ResourceRole.Topping)
            {
                return false;
            }

            CurrentResource = resource;
            placedObject = item.gameObject;
            placedResourceItem = item;
            item.MarkPlacedInOven();
            PlaceObject(item.transform);
            if (labelText != null)
            {
                labelText.text = resource.DisplayName;
            }

            return true;
        }

        public bool TryPlaceScroll(DragScrollItem item)
        {
            if (item == null || slotKind != SlotKind.Scroll || !item.IsEmptyScroll)
            {
                return false;
            }

            HasEmptyScroll = true;
            placedObject = item.gameObject;
            placedScrollItem = item;
            PlaceObject(item.transform);
            if (labelText != null)
            {
                labelText.text = "빈 스크롤";
            }

            return true;
        }

        private void DropScroll(PointerEventData eventData)
        {
            var item = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<DragScrollItem>();
            if (!TryPlaceScroll(item))
            {
                return;
            }
        }

        private void PlaceObject(Transform target)
        {
            target.SetParent(transform, false);
            if (target.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }
        }

        public void Clear()
        {
            Clear(false);
        }

        public void Clear(bool consumePlacedObject)
        {
            CurrentResource = null;
            HasEmptyScroll = false;
            placedResourceItem = null;
            placedScrollItem = null;
            if (consumePlacedObject && placedObject != null)
            {
                Destroy(placedObject);
            }

            placedObject = null;
            if (labelText != null)
            {
                labelText.text = slotKind switch
                {
                    SlotKind.Base => "베이스",
                    SlotKind.Topping => "토핑",
                    SlotKind.Scroll => "스크롤",
                    _ => string.Empty
                };
            }
        }

        public void ReturnPlacedObject()
        {
            placedResourceItem?.ReturnToStart();
            placedScrollItem?.ReturnToOriginalSlot();
            Clear();
        }

        public void ConsumePlacedResource()
        {
            placedResourceItem?.ConsumeFromOven();
            Clear();
        }

        public void ReleasePlacedScroll()
        {
            placedScrollItem?.ReturnToOriginalSlot();
            Clear();
        }
    }
}
