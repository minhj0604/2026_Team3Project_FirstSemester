using Team3Project.GameSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public static class BattleInteractionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (_, _) => SetupIfBattleScene();
            SetupIfBattleScene();
        }

        private static void SetupIfBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "BattleScene")
            {
                return;
            }

            SetupResourceDrags();
            SetupOven();
        }

        private static void SetupResourceDrags()
        {
            var families = new[]
            {
                ResourceFamily.Egg,
                ResourceFamily.Berry,
                ResourceFamily.Egg,
                ResourceFamily.Dough,
                ResourceFamily.Dairy,
                ResourceFamily.Sugar,
                ResourceFamily.Dairy,
                ResourceFamily.Dairy,
                ResourceFamily.Chocolate
            };

            for (var i = 0; i < families.Length; i++)
            {
                var target = GameObject.Find($"Resource Icon {i + 1}");
                if (target == null)
                {
                    continue;
                }

                var dragItem = target.GetComponent<DragMergeItem>();
                if (dragItem == null)
                {
                    dragItem = target.AddComponent<DragMergeItem>();
                }

                dragItem.Configure(families[i], 1);

                if (target.GetComponent<CanvasGroup>() == null)
                {
                    target.AddComponent<CanvasGroup>();
                }
            }
        }

        private static void SetupOven()
        {
            var ovenArea = GameObject.Find("Oven Craft Area");
            if (ovenArea == null || ovenArea.transform.Find("Base Drop Slot") != null)
            {
                return;
            }

            var baseSlot = CreateDropSlot(ovenArea.transform, "Base Drop Slot", new Vector2(-120, -10), OvenDropSlot.SlotKind.Base);
            var toppingSlot = CreateDropSlot(ovenArea.transform, "Topping Drop Slot", new Vector2(120, -10), OvenDropSlot.SlotKind.Topping);
            var resultText = CreateText(ovenArea.transform, "Crafted Scroll Result", new Vector2(0, -72), new Vector2(360, 44), "Drag base/topping here");

            var craftButtonObject = CreateButton(ovenArea.transform, "Bake Button", new Vector2(0, 58), new Vector2(180, 46), "Bake");
            var oven = ovenArea.GetComponent<OvenCraftController>();
            if (oven == null)
            {
                oven = ovenArea.AddComponent<OvenCraftController>();
            }

            oven.Configure(baseSlot, toppingSlot, resultText);
            craftButtonObject.GetComponent<Button>().onClick.AddListener(oven.Bake);
        }

        private static OvenDropSlot CreateDropSlot(Transform parent, string name, Vector2 position, OvenDropSlot.SlotKind kind)
        {
            var slotObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(OvenDropSlot));
            slotObject.transform.SetParent(parent, false);

            var rect = slotObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120, 70);

            var image = slotObject.GetComponent<Image>();
            image.color = new Color(0.95f, 0.8f, 0.55f, 0.25f);
            image.raycastTarget = true;

            var label = CreateText(slotObject.transform, "Label", Vector2.zero, new Vector2(120, 70), kind.ToString());
            var slot = slotObject.GetComponent<OvenDropSlot>();
            slot.Configure(kind, label);
            return slot;
        }

        private static GameObject CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            buttonObject.GetComponent<Image>().color = new Color(0.55f, 0.22f, 0.12f, 0.95f);
            CreateText(buttonObject.transform, "Label", Vector2.zero, size, label);
            return buttonObject;
        }

        private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, string value)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.text = value;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
