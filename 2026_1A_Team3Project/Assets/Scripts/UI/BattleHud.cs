using System.Linq;
using Team3Project.GameSystems;
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

        private void Awake()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            mergeSugarButton?.onClick.AddListener(() => battle.MergeFirstPair(ResourceFamily.Sugar));
            craftButton?.onClick.AddListener(() => battle.CraftFirstAvailableScroll());
            playButton?.onClick.AddListener(battle.PlayFirstScroll);
            endTurnButton?.onClick.AddListener(battle.EndTurn);
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

            playerText.text = $"{battle.Player.Name}\nHP {battle.Player.Hp}/{battle.Player.MaxHp}  Guard {battle.Player.Guard}\nStrength {battle.Player.Strength}";
            enemyText.text = $"{battle.Enemy.Name}\nHP {battle.Enemy.Hp}/{battle.Enemy.MaxHp}\nWeak {battle.Enemy.Weakness}  Shield {battle.Enemy.WeaknessHitsRemaining}/{battle.Enemy.WeaknessHitsRequired}";
            costText.text = $"Cost {battle.CurrentCost}/{battle.MaxCost * 2}";
            handText.text = "Hand\n" + string.Join("\n", battle.Hand.Select(card => $"{card.DisplayName} / Cost {card.Cost} / Power {card.Power}"));
            resourceText.text = "Resources\n" + string.Join("\n", battle.Resources.Select(resource => $"{resource.Family} Lv.{resource.Stage}"));
            logText.text = battle.LastLog;
        }
    }
}
