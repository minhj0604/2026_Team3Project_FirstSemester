using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class OvenCraftController : MonoBehaviour
    {
        [SerializeField] private OvenDropSlot baseSlot;
        [SerializeField] private OvenDropSlot toppingSlot;
        [SerializeField] private Text craftedScrollText;

        public void Configure(OvenDropSlot baseDropSlot, OvenDropSlot toppingDropSlot, Text resultText)
        {
            baseSlot = baseDropSlot;
            toppingSlot = toppingDropSlot;
            craftedScrollText = resultText;
        }

        public void Bake()
        {
            if (baseSlot == null || !baseSlot.CurrentResource.HasValue)
            {
                if (craftedScrollText != null)
                {
                    craftedScrollText.text = "Need base resource";
                }
                return;
            }

            var card = ScrollCard.Craft(baseSlot.CurrentResource.Value, toppingSlot == null ? null : toppingSlot.CurrentResource);
            if (craftedScrollText != null)
            {
                craftedScrollText.text = $"{card.DisplayName}\nCost {card.Cost} / Power {card.Power}";
            }

            baseSlot.Clear();
            toppingSlot?.Clear();
        }
    }
}
