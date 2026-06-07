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
        private DragMergeItem[] resourceItems = new DragMergeItem[0];
        private DragScrollItem[] scrollItems = new DragScrollItem[0];

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
            SetText(costText, $"Cost {battle.CurrentCost}/{battle.MaxCost * 2}");
            SetText(handText, $"Hand {battle.Hand.Count}");
            SetText(resourceText, $"Resources {battle.Resources.Count}");
            SetText(logText, battle.LastLog);
            RefreshResourceItems();
            RefreshScrollItems();
        }

        private void CacheResourceItems()
        {
            resourceItems = FindObjectsByType<DragMergeItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(item => item.name)
                .ToArray();

            foreach (var item in resourceItems)
            {
                if (item.TryGetComponent<Image>(out var image) && image.sprite != null && !resourceSprites.ContainsKey(item.Resource.Family))
                {
                    resourceSprites.Add(item.Resource.Family, image.sprite);
                }
            }
        }

        private void RefreshResourceItems()
        {
            if (resourceItems.Length == 0)
            {
                CacheResourceItems();
            }

            for (var i = 0; i < resourceItems.Length; i++)
            {
                var item = resourceItems[i];
                if (item == null || item.IsDragging || item.IsPlacedInOven)
                {
                    continue;
                }

                var hasResource = i < battle.Resources.Count;
                var resource = hasResource ? battle.Resources[i] : default;
                resourceSprites.TryGetValue(resource.Family, out var sprite);
                item.SetInventoryState(resource, sprite, hasResource, i);
            }
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

                var hasCard = i < battle.Hand.Count;
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

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
