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

        public MergeResource? CurrentResource { get; private set; }

        public void Configure(SlotKind kind, Text label)
        {
            slotKind = kind;
            labelText = label;
            Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
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
            item.transform.SetParent(transform, false);
            if (item.TryGetComponent<RectTransform>(out var rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }

            if (labelText != null)
            {
                labelText.text = $"{resource.Family} Lv.{resource.Stage}";
            }
        }

        public void Clear()
        {
            CurrentResource = null;
            if (labelText != null)
            {
                labelText.text = slotKind.ToString();
            }
        }
    }
}
