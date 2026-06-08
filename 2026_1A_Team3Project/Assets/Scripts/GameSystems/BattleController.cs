using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Team3Project.GameSystems
{
    public class BattleController : MonoBehaviour
    {
        private const int FinalStageIndex = 3;
        private const string SelectedChapterKey = "Team3.SelectedChapter";
        private const string SelectedStageKey = "Team3.SelectedStage";
        private const string ClearedStageKeyPrefix = "Team3.ClearedStage.";
        private const string UnlockedChapterKey = "Team3.UnlockedChapter";

        [Header("Stage")]
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private int baseMaxCost = 4;
        [SerializeField] private int maxCostCap = 10;
        [SerializeField] private int cardsDrawnPerTurn = 3;
        [SerializeField] private int resourcesPerTurn = 10;
        [SerializeField] private int maxResources = 56;
        [SerializeField] private int startingEmptyScrollCount = 18;

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
        public int CostCap => maxCostCap;
        public int CurrentCost { get; private set; }
        public int MaxResources => ResourceLimit;
        public int ActiveResourceCount => Resources.FindAll(resource => resource.CanUse).Count;
        public int VisibleHandCount => Hand.FindAll(card => card != null).Count;
        public string LastLog { get; private set; } = "Ready.";

        private readonly Queue<ScrollCard> drawDeck = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDrawDecks = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDiscardDecks = new();
        private int ResourceLimit => maxResources > 0 ? maxResources : 56;

        public static void ResetChapterRun(int chapter)
        {
            ChapterDrawDecks.Remove(chapter);
            ChapterDiscardDecks.Remove(chapter);
            PlayerPrefs.SetInt($"{ClearedStageKeyPrefix}{chapter}", 0);
            PlayerPrefs.SetInt(SelectedChapterKey, chapter);
            PlayerPrefs.SetInt(SelectedStageKey, 1);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            chapterIndex = PlayerPrefs.GetInt(SelectedChapterKey, chapterIndex);
            stageIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedStageKey, stageIndex), 1, FinalStageIndex);

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
            CurrentCost = 0;
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
            CurrentCost = Mathf.Min(CurrentCost + MaxCost, maxCostCap);
            DrawEmptyScrolls(cardsDrawnPerTurn);
            AddTurnResources();
            LastLog = "Player turn. Craft scrolls, then use them.";
            NotifyChanged();
        }

        public bool MergeFirstPair(ResourceFamily family)
        {
            var first = Resources.FindIndex(item => item.CanUse && item.Family == family);
            if (first < 0)
            {
                return false;
            }

            var stage = Resources[first].Stage;
            var second = Resources.FindIndex(first + 1, item => item.CanUse && item.Family == family && item.Stage == stage);
            if (second < 0 || stage >= 3)
            {
                return false;
            }

            Resources[first] = new MergeResource(family, stage + 1);
            Resources[second] = MergeResource.Empty;
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

            var firstIndex = Resources.FindIndex(item => item.CanUse && item.Family == firstResource.Family && item.Stage == firstResource.Stage);
            if (firstIndex < 0)
            {
                return false;
            }

            var secondIndex = Resources.FindIndex(firstIndex + 1, item => item.CanUse && item.Family == secondResource.Family && item.Stage == secondResource.Stage);
            if (secondIndex < 0)
            {
                return false;
            }

            Resources[firstIndex] = new MergeResource(firstResource.Family, firstResource.Stage + 1);
            Resources[secondIndex] = MergeResource.Empty;
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
            if (!firstResource.CanUse || !secondResource.CanUse || firstResource.Family != secondResource.Family || firstResource.Stage != secondResource.Stage || firstResource.Stage >= 3)
            {
                LastLog = "Only matching resources can merge.";
                NotifyChanged();
                return false;
            }

            var resultResource = new MergeResource(firstResource.Family, firstResource.Stage + 1);
            Resources[secondIndex] = resultResource;
            Resources[firstIndex] = MergeResource.Empty;
            LastLog = $"Merged {firstResource.Family} to Lv.{firstResource.Stage + 2}.";
            NotifyChanged();
            return true;
        }

        public bool TryConsumeResource(MergeResource resource)
        {
            var index = Resources.FindIndex(item => item.CanUse && item.Family == resource.Family && item.Stage == resource.Stage);
            if (index < 0)
            {
                return false;
            }

            Resources[index] = MergeResource.Empty;
            NotifyChanged();
            return true;
        }

        public bool TryConsumeResourceSlots(params int[] resourceIndices)
        {
            var sortedIndices = new List<int>();
            foreach (var index in resourceIndices)
            {
                if (index < 0 || index >= Resources.Count || sortedIndices.Contains(index) || !Resources[index].CanUse)
                {
                    continue;
                }

                sortedIndices.Add(index);
            }

            if (sortedIndices.Count == 0)
            {
                return false;
            }

            foreach (var index in sortedIndices)
            {
                Resources[index] = MergeResource.Empty;
            }

            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool CraftFirstAvailableScroll()
        {
            var handIndex = Hand.FindIndex(card => card != null && card.IsEmpty);
            if (handIndex < 0)
            {
                LastLog = "No empty scroll in hand.";
                NotifyChanged();
                return false;
            }

            var baseIndex = Resources.FindIndex(resource => resource.CanUse && resource.Role == ResourceRole.Base && resource.Stage > 0);
            if (baseIndex < 0)
            {
                LastLog = "Need a stage 1+ base resource.";
                NotifyChanged();
                return false;
            }

            var toppingIndex = Resources.FindIndex(resource => resource.CanUse && resource.Role == ResourceRole.Topping && resource.Stage > 0);
            if (toppingIndex < 0)
            {
                LastLog = "Need a stage 1+ topping resource.";
                NotifyChanged();
                return false;
            }

            var baseResource = Resources[baseIndex];
            var toppingResource = Resources[toppingIndex];
            Resources[baseIndex] = MergeResource.Empty;
            Resources[toppingIndex] = MergeResource.Empty;
            Hand[handIndex] = ScrollCard.Craft(baseResource, toppingResource, Hand[handIndex].Id);
            LastLog = $"Crafted {Hand[handIndex].DisplayName}.";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryCraftHandScroll(int handIndex, MergeResource baseResource, MergeResource? toppingResource, int baseResourceIndex, int toppingResourceIndex, out ScrollCard craftedCard)
        {
            craftedCard = null;
            if (handIndex < 0 || handIndex >= Hand.Count || Hand[handIndex] == null || !Hand[handIndex].IsEmpty)
            {
                LastLog = "Need an empty scroll.";
                NotifyChanged();
                return false;
            }

            if (baseResource.Stage <= 0)
            {
                LastLog = "Base resource must be stage 1+.";
                NotifyChanged();
                return false;
            }

            if (!toppingResource.HasValue || toppingResource.Value.Stage <= 0)
            {
                LastLog = "Topping resource must be stage 1+.";
                NotifyChanged();
                return false;
            }

            if (!ResourceSlotMatches(baseResourceIndex, baseResource) || !ResourceSlotMatches(toppingResourceIndex, toppingResource.Value))
            {
                LastLog = "Selected resource changed.";
                NotifyChanged();
                return false;
            }

            if (!TryConsumeResourceSlots(baseResourceIndex, toppingResourceIndex))
            {
                LastLog = "Cannot use selected resources.";
                NotifyChanged();
                return false;
            }

            craftedCard = ScrollCard.Craft(baseResource, toppingResource, Hand[handIndex].Id);
            Hand[handIndex] = craftedCard;
            LastLog = $"Crafted {craftedCard.DisplayName}.";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        private bool ResourceSlotMatches(int index, MergeResource resource)
        {
            if (index < 0 || index >= Resources.Count)
            {
                return false;
            }

            var current = Resources[index];
            return current.CanUse && current.Family == resource.Family && current.Stage == resource.Stage;
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
            var handIndex = Hand.FindIndex(item => item != null && card != null && item.Id == card.Id);
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
            if (card == null)
            {
                return false;
            }

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
            Hand[handIndex] = null;
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

            var handIndex = Hand.FindIndex(card => card != null && !card.IsEmpty);
            if (handIndex < 0)
            {
                LastLog = "No crafted scroll in hand.";
                NotifyChanged();
                return;
            }

            var card = Hand[handIndex];
            if (CurrentCost < card.Cost)
            {
                LastLog = "Not enough cost.";
                NotifyChanged();
                return;
            }

            CurrentCost -= card.Cost;
            ResolveCard(card);
            DiscardPile.Add(card);
            Hand[handIndex] = null;
            SaveChapterDeckState();
            NotifyChanged();
        }

        public void EndTurn()
        {
            if (Phase == BattlePhase.StageClear)
            {
                ContinueAfterStageClear();
                return;
            }

            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            foreach (var card in Hand)
            {
                if (card != null)
                {
                    DiscardPile.Add(card);
                }
            }

            Hand.Clear();
            SaveChapterDeckState();
            Phase = BattlePhase.EnemyTurn;
            ResolveEnemyTurn();
        }

        public void ContinueAfterStageClear()
        {
            if (Phase != BattlePhase.StageClear)
            {
                return;
            }

            MarkStageCleared();
            if (stageIndex >= FinalStageIndex)
            {
                LastLog = "Chapter clear!";
                SaveChapterDeckState();
                PlayerPrefs.SetInt(UnlockedChapterKey, Mathf.Max(PlayerPrefs.GetInt(UnlockedChapterKey, 1), chapterIndex + 1));
                PlayerPrefs.SetInt(SelectedStageKey, 1);
                PlayerPrefs.Save();
                SceneManager.LoadScene("StageMapScene");
                return;
            }

            stageIndex++;
            PlayerPrefs.SetInt(SelectedChapterKey, chapterIndex);
            PlayerPrefs.SetInt(SelectedStageKey, stageIndex);
            PlayerPrefs.Save();
            StartStage();
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
                MarkStageCleared();
            }
        }

        private void MarkStageCleared()
        {
            var key = $"{ClearedStageKeyPrefix}{chapterIndex}";
            var clearedStage = Mathf.Max(PlayerPrefs.GetInt(key, 0), stageIndex);
            PlayerPrefs.SetInt(key, clearedStage);
            PlayerPrefs.SetInt(SelectedChapterKey, chapterIndex);
            PlayerPrefs.SetInt(SelectedStageKey, Mathf.Clamp(stageIndex, 1, FinalStageIndex));
            PlayerPrefs.Save();
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

            // Breaking the enemy should not inject extra inventory resources.
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

        private List<ScrollCard> BuildEmptyScrollDeck()
        {
            var deck = new List<ScrollCard>();
            for (var i = 0; i < startingEmptyScrollCount; i++)
            {
                deck.Add(CreateEmptyScroll());
            }

            return deck;
        }

        private static ScrollCard CreateEmptyScroll()
        {
            var card = new ScrollCard
            {
                EffectType = ScrollEffectType.Attack,
                Element = ElementType.None,
                Power = 0,
                Cost = 0,
                DisplayName = "Empty Scroll"
            };
            card.Id = ScrollCard.CreateId();
            return card;
        }

        private void SaveChapterDeckState()
        {
            ChapterDrawDecks[chapterIndex] = new List<ScrollCard>(CloneCards(drawDeck));
            var persistentDiscard = new List<ScrollCard>(CloneCards(DiscardPile));
            foreach (var card in Hand)
            {
                if (card != null && !card.IsEmpty)
                {
                    persistentDiscard.Add(CloneCard(card));
                }
            }

            ChapterDiscardDecks[chapterIndex] = persistentDiscard;
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
                Id = card.Id > 0 ? card.Id : ScrollCard.CreateId(),
                EffectType = card.EffectType,
                Element = card.Element,
                Power = card.Power,
                Cost = card.Cost,
                DisplayName = card.DisplayName
            };
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

                AddCardToHand(drawDeck.Dequeue());
            }

            SaveChapterDeckState();
        }

        private void AddCardToHand(ScrollCard card)
        {
            var emptySlot = Hand.FindIndex(item => item == null);
            if (emptySlot >= 0)
            {
                Hand[emptySlot] = card;
                return;
            }

            Hand.Add(card);
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
            if (ActiveResourceCount >= ResourceLimit)
            {
                LastLog = "Resource storage is full.";
                return;
            }

            var nonSugarBaseFamilies = new[]
            {
                ResourceFamily.Dough,
                ResourceFamily.Dairy,
                ResourceFamily.Egg
            };

            var toppingFamilies = new[]
            {
                ResourceFamily.Berry,
                ResourceFamily.Chocolate,
                ResourceFamily.Marshmallow,
                ResourceFamily.PoppingCandy
            };

            var guaranteedBase = nonSugarBaseFamilies[UnityEngine.Random.Range(0, nonSugarBaseFamilies.Length)];
            var guaranteedTopping = toppingFamilies[UnityEngine.Random.Range(0, toppingFamilies.Length)];
            AddResource(ResourceFamily.Sugar, 0);
            AddResource(ResourceFamily.Sugar, 0);
            AddResource(guaranteedBase, 0);
            AddResource(guaranteedBase, 0);
            AddResource(guaranteedTopping, 0);
            AddResource(guaranteedTopping, 0);

            var weightedFamilies = new[]
            {
                ResourceFamily.Sugar,
                ResourceFamily.Sugar,
                ResourceFamily.Sugar,
                ResourceFamily.Dough,
                ResourceFamily.Dairy,
                ResourceFamily.Egg,
                ResourceFamily.Berry,
                ResourceFamily.Chocolate,
                ResourceFamily.Marshmallow,
                ResourceFamily.PoppingCandy
            };

            for (var i = 6; i < resourcesPerTurn; i++)
            {
                AddResource(weightedFamilies[UnityEngine.Random.Range(0, weightedFamilies.Length)], 0);
            }
        }

        private void AddResource(ResourceFamily family, int stage)
        {
            if (ActiveResourceCount >= ResourceLimit)
            {
                return;
            }

            var emptySlot = Resources.FindIndex(resource => !resource.CanUse);
            if (emptySlot >= 0)
            {
                Resources[emptySlot] = new MergeResource(family, stage);
                return;
            }

            if (Resources.Count < ResourceLimit)
            {
                Resources.Add(new MergeResource(family, stage));
            }
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
