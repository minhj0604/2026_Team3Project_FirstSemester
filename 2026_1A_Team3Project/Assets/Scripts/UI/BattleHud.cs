using Team3Project.GameSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Team3Project.UI
{
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] private BattleController battle;
        [SerializeField] private Text playerText;
        [SerializeField] private Text enemyText;
        [SerializeField] private Text costText;
        [SerializeField] private Text handText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text logText;
        [SerializeField] private Button mergeSugarButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button endTurnButton;

        private readonly Dictionary<ResourceFamily, Sprite> resourceSprites = new();
        private readonly List<DragMergeItem> resourceSlots = new();
        private DragMergeItem resourceTemplate;
        private DragScrollItem[] scrollItems = new DragScrollItem[0];
        private RectTransform resourceStorage;

        private void Awake()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            playerText = playerText == null ? FindText("Player Text") : playerText;
            enemyText = enemyText == null ? FindText("Enemy Text") : enemyText;
            costText = costText == null ? FindText("Cost Text") : costText;
            handText = handText == null ? FindText("Hand Text") : handText;
            resourceText = resourceText == null ? FindText("Resource Text") : resourceText;
            logText = logText == null ? FindText("Log Text") : logText;
            endTurnButton = endTurnButton == null ? FindButton("End Turn") : endTurnButton;

            mergeSugarButton?.onClick.AddListener(() => battle.MergeFirstPair(ResourceFamily.Sugar));
            craftButton?.onClick.AddListener(() => battle.CraftFirstAvailableScroll());
            playButton?.onClick.AddListener(battle.PlayFirstScroll);
            endTurnButton?.onClick.AddListener(battle.EndTurn);
            resourceStorage = FindRectTransform("Resource Storage");
            CacheResourceItems();
            CacheScrollItems();
        }

        private void OnEnable()
        {
            if (battle != null)
            {
                battle.StateChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (battle != null)
            {
                battle.StateChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (battle == null)
            {
                return;
            }

            SetText(playerText, $"{battle.Player.Name}\nHP {battle.Player.Hp}/{battle.Player.MaxHp}  Guard {battle.Player.Guard}\nStrength {battle.Player.Strength}");
            SetText(enemyText, $"{battle.Enemy.Name}\nHP {battle.Enemy.Hp}/{battle.Enemy.MaxHp}\nWeak {battle.Enemy.Weakness}  Shield {battle.Enemy.WeaknessHitsRemaining}/{battle.Enemy.WeaknessHitsRequired}");
            SetText(costText, $"Cost {battle.CurrentCost}/{battle.CostCap}");
            SetText(handText, $"Hand {battle.VisibleHandCount}");
            SetText(resourceText, $"Resources {battle.ActiveResourceCount}/{battle.MaxResources}");
            SetText(logText, battle.LastLog);
            SetButtonText(endTurnButton, battle.Phase == BattlePhase.StageClear ? "Next Stage" : "End Turn");
            RefreshResourceItems();
            RefreshScrollItems();
        }

        private void CacheResourceItems()
        {
            var foundItems = FindObjectsByType<DragMergeItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(item => item.transform.GetSiblingIndex())
                .ThenBy(item => item.name)
                .ToArray();

            foreach (var item in foundItems)
            {
                if (item.TryGetComponent<Image>(out var image) && image.sprite != null && !resourceSprites.ContainsKey(item.Resource.Family))
                {
                    resourceSprites.Add(item.Resource.Family, image.sprite);
                }
            }

            resourceTemplate = foundItems.FirstOrDefault();
            foreach (var item in foundItems)
            {
                item.ClearInventorySlot();
            }

            EnsureResourceSlotCapacity(battle == null ? 56 : battle.MaxResources);
        }

        private void RefreshResourceItems()
        {
            if (resourceSlots.Count == 0)
            {
                CacheResourceItems();
            }

            EnsureResourceSlotCapacity(battle.MaxResources);
            var slotCount = Mathf.Min(battle.MaxResources, resourceSlots.Count);
            for (var i = 0; i < slotCount; i++)
            {
                var item = resourceSlots[i];
                if (item == null)
                {
                    continue;
                }

                var hasResource = i < battle.Resources.Count && battle.Resources[i].CanUse;
                var resource = hasResource ? battle.Resources[i] : MergeResource.Empty;
                resourceSprites.TryGetValue(resource.Family, out var sprite);
                if (resourceStorage != null)
                {
                    item.SetInventorySlot(resourceStorage, GetResourceSlotPosition(i));
                }

                item.SetInventoryState(resource, sprite, hasResource, i);
            }

            for (var i = slotCount; i < resourceSlots.Count; i++)
            {
                resourceSlots[i]?.ClearInventorySlot();
            }
        }

        private void EnsureResourceSlotCapacity(int requiredCount)
        {
            if (resourceTemplate == null || requiredCount <= resourceSlots.Count)
            {
                return;
            }

            var parent = resourceStorage == null ? resourceTemplate.transform.parent : resourceStorage;
            for (var i = resourceSlots.Count; i < requiredCount; i++)
            {
                var clone = Instantiate(resourceTemplate, parent);
                clone.name = $"Resource Slot {i + 1:00}";
                if (clone.TryGetComponent<RectTransform>(out var rect))
                {
                    rect.anchoredPosition = GetResourceSlotPosition(i);
                }

                clone.gameObject.SetActive(false);
                resourceSlots.Add(clone);
            }
        }

        private Vector2 GetResourceSlotPosition(int index)
        {
            const int columns = 8;
            const float cellWidth = 54f;
            const float cellHeight = 58f;
            var column = index % columns;
            var row = index / columns;
            if (resourceStorage == null)
            {
                return new Vector2(column * cellWidth, -row * cellHeight);
            }

            var rect = resourceStorage.rect;
            return new Vector2((-rect.width * 0.5f) + 30f + column * cellWidth, (rect.height * 0.5f) - 38f - row * cellHeight);
        }

        private void CacheScrollItems()
        {
            scrollItems = FindObjectsByType<DragScrollItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(item => item.name)
                .ToArray();
        }

        private void RefreshScrollItems()
        {
            if (scrollItems.Length == 0)
            {
                CacheScrollItems();
            }

            for (var i = 0; i < scrollItems.Length; i++)
            {
                var item = scrollItems[i];
                if (item == null || item.IsDragging)
                {
                    continue;
                }

                var hasCard = i < battle.Hand.Count && battle.Hand[i] != null;
                item.SetHandState(hasCard, i, hasCard ? battle.Hand[i] : null);
            }
        }

        private static Text FindText(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<Text>();
        }

        private static Button FindButton(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<Button>();
        }

        private static RectTransform FindRectTransform(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<RectTransform>();
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
