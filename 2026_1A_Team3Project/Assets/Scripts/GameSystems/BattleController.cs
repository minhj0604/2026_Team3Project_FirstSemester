using System;
using System.Collections.Generic;
using UnityEngine;

namespace Team3Project.GameSystems
{
    public class BattleController : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private int baseMaxCost = 4;
        [SerializeField] private int cardsDrawnPerTurn = 3;
        [SerializeField] private int resourcesPerTurn = 4;

        public event Action StateChanged;

        public BattlePhase Phase { get; private set; }
        public CombatantState Player { get; } = new();
        public CombatantState Enemy { get; } = new();
        public List<MergeResource> Resources { get; } = new();
        public List<ScrollCard> Hand { get; } = new();
        public List<ScrollCard> DiscardPile { get; } = new();

        public int ChapterIndex => chapterIndex;
        public int StageIndex => stageIndex;
        public int MaxCost { get; private set; }
        public int CurrentCost { get; private set; }
        public string LastLog { get; private set; } = "Ready.";

        private readonly Queue<ScrollCard> drawDeck = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDrawDecks = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDiscardDecks = new();

        private void Awake()
        {
            if (Player.MaxHp <= 0)
            {
                Player.Reset("Player", 50, ElementType.None, 0);
            }
        }

        private void Start()
        {
            StartStage();
        }

        public void StartStage()
        {
            MaxCost = baseMaxCost + Mathf.Max(0, stageIndex - 1);
            CurrentCost = MaxCost;
            Player.Reset("Macaroon", 50, ElementType.None, 0);
            Enemy.Reset(stageIndex % 3 == 0 ? "Boss DuCookie" : "DuCookie", stageIndex % 3 == 0 ? 70 : 45, ElementType.Berry, stageIndex % 3 == 0 ? 3 : 0);
            Hand.Clear();
            Resources.Clear();
            LoadChapterDeckState();
            BeginPlayerTurn();
        }

        public void BeginPlayerTurn()
        {
            Phase = BattlePhase.PlayerTurn;
            Player.Guard = 0;
            CurrentCost = Mathf.Min(CurrentCost + MaxCost, MaxCost * 2);
            DrawEmptyScrolls(cardsDrawnPerTurn);
            AddTurnResources();
            LastLog = "Player turn. Craft scrolls, then use them.";
            NotifyChanged();
        }

        public bool MergeFirstPair(ResourceFamily family)
        {
            var first = Resources.FindIndex(item => item.Family == family);
            if (first < 0)
            {
                return false;
            }

            var stage = Resources[first].Stage;
            var second = Resources.FindIndex(first + 1, item => item.Family == family && item.Stage == stage);
            if (second < 0 || stage >= 3)
            {
                return false;
            }

            Resources.RemoveAt(second);
            Resources.RemoveAt(first);
            Resources.Add(new MergeResource(family, stage + 1));
            LastLog = $"Merged {family} to Lv.{stage + 2}.";
            NotifyChanged();
            return true;
        }

        public bool TryMergeResources(MergeResource firstResource, MergeResource secondResource)
        {
            if (firstResource.Family != secondResource.Family || firstResource.Stage != secondResource.Stage || firstResource.Stage >= 3)
            {
                LastLog = "Only matching resources can merge.";
                NotifyChanged();
                return false;
            }

            var firstIndex = Resources.FindIndex(item => item.Family == firstResource.Family && item.Stage == firstResource.Stage);
            if (firstIndex < 0)
            {
                return false;
            }

            var secondIndex = Resources.FindIndex(firstIndex + 1, item => item.Family == secondResource.Family && item.Stage == secondResource.Stage);
            if (secondIndex < 0)
            {
                return false;
            }

            Resources.RemoveAt(secondIndex);
            Resources.RemoveAt(firstIndex);
            Resources.Add(new MergeResource(firstResource.Family, firstResource.Stage + 1));
            LastLog = $"Merged {firstResource.Family} to Lv.{firstResource.Stage + 2}.";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryMergeResourceSlots(int firstIndex, int secondIndex)
        {
            if (firstIndex == secondIndex || firstIndex < 0 || secondIndex < 0 || firstIndex >= Resources.Count || secondIndex >= Resources.Count)
            {
                return false;
            }

            var firstResource = Resources[firstIndex];
            var secondResource = Resources[secondIndex];
            if (firstResource.Family != secondResource.Family || firstResource.Stage != secondResource.Stage || firstResource.Stage >= 3)
            {
                LastLog = "Only matching resources can merge.";
                NotifyChanged();
                return false;
            }

            Resources.RemoveAt(Mathf.Max(firstIndex, secondIndex));
            Resources.RemoveAt(Mathf.Min(firstIndex, secondIndex));
            Resources.Add(new MergeResource(firstResource.Family, firstResource.Stage + 1));
            LastLog = $"Merged {firstResource.Family} to Lv.{firstResource.Stage + 2}.";
            NotifyChanged();
            return true;
        }

        public bool TryConsumeResource(MergeResource resource)
        {
            var index = Resources.FindIndex(item => item.Family == resource.Family && item.Stage == resource.Stage);
            if (index < 0)
            {
                return false;
            }

            Resources.RemoveAt(index);
            NotifyChanged();
            return true;
        }

        public bool TryConsumeResourceSlots(params int[] resourceIndices)
        {
            var sortedIndices = new List<int>();
            foreach (var index in resourceIndices)
            {
                if (index < 0 || index >= Resources.Count || sortedIndices.Contains(index))
                {
                    continue;
                }

                sortedIndices.Add(index);
            }

            if (sortedIndices.Count == 0)
            {
                return false;
            }

            sortedIndices.Sort();
            sortedIndices.Reverse();
            foreach (var index in sortedIndices)
            {
                Resources.RemoveAt(index);
            }

            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool CraftFirstAvailableScroll()
        {
            if (Hand.Count == 0)
            {
                LastLog = "No empty scroll in hand.";
                NotifyChanged();
                return false;
            }

            var baseIndex = Resources.FindIndex(resource => resource.Role == ResourceRole.Base && resource.Stage > 0);
            if (baseIndex < 0)
            {
                LastLog = "Need a stage 1+ base resource first.";
                NotifyChanged();
                return false;
            }

            var baseResource = Resources[baseIndex];
            Resources.RemoveAt(baseIndex);

            MergeResource? toppingResource = null;
            var toppingIndex = Resources.FindIndex(resource => resource.Role == ResourceRole.Topping && resource.Stage > 0);
            if (toppingIndex >= 0)
            {
                toppingResource = Resources[toppingIndex];
                Resources.RemoveAt(toppingIndex);
            }

            Hand[0] = ScrollCard.Craft(baseResource, toppingResource);
            LastLog = $"Crafted {Hand[0].DisplayName}.";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryCraftHandScroll(int handIndex, MergeResource baseResource, MergeResource? toppingResource, int baseResourceIndex, int toppingResourceIndex, out ScrollCard craftedCard)
        {
            craftedCard = null;
            if (handIndex < 0 || handIndex >= Hand.Count || !Hand[handIndex].IsEmpty)
            {
                LastLog = "Need an empty scroll.";
                NotifyChanged();
                return false;
            }

            if (toppingResource.HasValue)
            {
                if (!TryConsumeResourceSlots(baseResourceIndex, toppingResourceIndex))
                {
                    LastLog = "Cannot use selected resources.";
                    NotifyChanged();
                    return false;
                }
            }
            else if (!TryConsumeResourceSlots(baseResourceIndex))
            {
                LastLog = "Cannot use selected base resource.";
                NotifyChanged();
                return false;
            }

            craftedCard = ScrollCard.Craft(baseResource, toppingResource);
            Hand[handIndex] = craftedCard;
            LastLog = $"Crafted {craftedCard.DisplayName}.";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryPlayHandCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count)
            {
                return false;
            }

            return TryPlayCardAt(handIndex);
        }

        public bool TryPlayCard(ScrollCard card)
        {
            var handIndex = Hand.FindIndex(item => ReferenceEquals(item, card) || SameCard(item, card));
            if (handIndex < 0)
            {
                LastLog = "Card is not in hand.";
                NotifyChanged();
                return false;
            }

            return TryPlayCardAt(handIndex);
        }

        private bool TryPlayCardAt(int handIndex)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            var card = Hand[handIndex];
            if (card.IsEmpty)
            {
                LastLog = "Empty scroll has no effect.";
                NotifyChanged();
                return false;
            }

            if (CurrentCost < card.Cost)
            {
                LastLog = "Not enough cost.";
                NotifyChanged();
                return false;
            }

            CurrentCost -= card.Cost;
            ResolveCard(card);
            DiscardPile.Add(card);
            Hand.RemoveAt(handIndex);
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public void PlayFirstScroll()
        {
            if (Phase != BattlePhase.PlayerTurn || Hand.Count == 0)
            {
                return;
            }

            var card = Hand[0];
            if (CurrentCost < card.Cost)
            {
                LastLog = "Not enough cost.";
                NotifyChanged();
                return;
            }

            CurrentCost -= card.Cost;
            ResolveCard(card);
            DiscardPile.Add(card);
            Hand.RemoveAt(0);
            SaveChapterDeckState();
            NotifyChanged();
        }

        public void EndTurn()
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            DiscardPile.AddRange(Hand);
            Hand.Clear();
            SaveChapterDeckState();
            Phase = BattlePhase.EnemyTurn;
            ResolveEnemyTurn();
        }

        private void ResolveCard(ScrollCard card)
        {
            switch (card.EffectType)
            {
                case ScrollEffectType.Attack:
                    var damage = card.Power + Player.Strength;
                    if (card.Element == Enemy.Weakness)
                    {
                        damage = Mathf.RoundToInt(damage * 1.5f);
                        BreakShieldOrReward(card.Element);
                    }

                    if (Enemy.IsBroken)
                    {
                        damage *= 2;
                    }

                    var dealt = Enemy.TakeDamage(damage);
                    LastLog = $"{card.DisplayName} dealt {dealt} damage.";
                    break;
                case ScrollEffectType.Guard:
                    Player.Guard += card.Power;
                    LastLog = $"{card.DisplayName} gave {card.Power} guard.";
                    break;
                case ScrollEffectType.Buff:
                    Player.Strength += card.Power;
                    LastLog = $"{card.DisplayName} raised strength by {card.Power}.";
                    break;
                case ScrollEffectType.Debuff:
                    Enemy.Strength -= card.Power;
                    LastLog = $"{card.DisplayName} weakened enemy by {card.Power}.";
                    break;
            }

            if (Enemy.IsDead)
            {
                Phase = BattlePhase.StageClear;
                LastLog = "Stage clear!";
            }
        }

        private void BreakShieldOrReward(ElementType element)
        {
            if (Enemy.WeaknessHitsRemaining > 0)
            {
                Enemy.WeaknessHitsRemaining--;
                if (Enemy.WeaknessHitsRemaining == 0)
                {
                    Enemy.IsBroken = true;
                    LastLog = "Enemy is broken.";
                }
            }

            AddResource(ResourceFamily.Berry, 0);
            AddResource(ResourceFamily.Berry, 0);
        }

        private void ResolveEnemyTurn()
        {
            if (Enemy.IsBroken)
            {
                Enemy.IsBroken = false;
                Enemy.WeaknessHitsRemaining = Enemy.WeaknessHitsRequired;
                LastLog = "Enemy skipped turn while recovering.";
                BeginPlayerTurn();
                return;
            }

            var attack = Mathf.Max(1, 8 + Enemy.Strength);
            var dealt = Player.TakeDamage(attack);
            LastLog = $"Enemy dealt {dealt} damage.";

            if (Player.IsDead)
            {
                Phase = BattlePhase.GameOver;
                NotifyChanged();
                return;
            }

            BeginPlayerTurn();
        }

        private void LoadChapterDeckState()
        {
            drawDeck.Clear();
            DiscardPile.Clear();

            if (!ChapterDrawDecks.TryGetValue(chapterIndex, out var savedDrawDeck) || savedDrawDeck.Count == 0)
            {
                savedDrawDeck = BuildEmptyScrollDeck();
                ChapterDrawDecks[chapterIndex] = savedDrawDeck;
            }

            foreach (var card in savedDrawDeck)
            {
                drawDeck.Enqueue(CloneCard(card));
            }

            if (ChapterDiscardDecks.TryGetValue(chapterIndex, out var savedDiscardDeck))
            {
                foreach (var card in savedDiscardDeck)
                {
                    DiscardPile.Add(CloneCard(card));
                }
            }
        }

        private static List<ScrollCard> BuildEmptyScrollDeck()
        {
            var deck = new List<ScrollCard>();
            for (var i = 0; i < 7; i++)
            {
                deck.Add(CreateEmptyScroll());
            }

            return deck;
        }

        private static ScrollCard CreateEmptyScroll()
        {
            return new ScrollCard
            {
                EffectType = ScrollEffectType.Attack,
                Element = ElementType.None,
                Power = 0,
                Cost = 0,
                DisplayName = "Empty Scroll"
            };
        }

        private void SaveChapterDeckState()
        {
            ChapterDrawDecks[chapterIndex] = new List<ScrollCard>(CloneCards(drawDeck));
            ChapterDiscardDecks[chapterIndex] = new List<ScrollCard>(CloneCards(DiscardPile));
        }

        private IEnumerable<ScrollCard> CloneCards(IEnumerable<ScrollCard> cards)
        {
            foreach (var card in cards)
            {
                yield return CloneCard(card);
            }
        }

        private static ScrollCard CloneCard(ScrollCard card)
        {
            if (card == null)
            {
                return CreateEmptyScroll();
            }

            return new ScrollCard
            {
                EffectType = card.EffectType,
                Element = card.Element,
                Power = card.Power,
                Cost = card.Cost,
                DisplayName = card.DisplayName
            };
        }

        private static bool SameCard(ScrollCard first, ScrollCard second)
        {
            return first != null && second != null
                && first.EffectType == second.EffectType
                && first.Element == second.Element
                && first.Power == second.Power
                && first.Cost == second.Cost
                && first.DisplayName == second.DisplayName;
        }

        private void BuildStarterDeck()
        {
            drawDeck.Clear();
            foreach (var card in BuildEmptyScrollDeck())
            {
                drawDeck.Enqueue(card);
            }
        }

        private void DrawEmptyScrolls(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (drawDeck.Count == 0)
                {
                    RefillDrawDeckFromDiscard();
                }

                if (drawDeck.Count == 0)
                {
                    break;
                }

                Hand.Add(drawDeck.Dequeue());
            }

            SaveChapterDeckState();
        }

        private void RefillDrawDeckFromDiscard()
        {
            if (DiscardPile.Count == 0)
            {
                BuildStarterDeck();
                return;
            }

            foreach (var card in DiscardPile)
            {
                drawDeck.Enqueue(card);
            }

            DiscardPile.Clear();
        }

        private void AddTurnResources()
        {
            AddResource(ResourceFamily.Sugar, 0);
            AddResource(ResourceFamily.Sugar, 0);

            var families = new[]
            {
                ResourceFamily.Dough,
                ResourceFamily.Dairy,
                ResourceFamily.Egg,
                ResourceFamily.Berry,
                ResourceFamily.Chocolate
            };

            for (var i = 2; i < resourcesPerTurn; i++)
            {
                AddResource(families[UnityEngine.Random.Range(0, families.Length)], 0);
            }
        }

        private void AddResource(ResourceFamily family, int stage)
        {
            Resources.Add(new MergeResource(family, stage));
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
