using System;
using System.Collections;
using System.Collections.Generic;
using Team3Project.Dialogue;
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
        private const string ChapterEntrySeenKeyPrefix = "Team3.ChapterEntrySeen.";

        [Header("Stage")]
        [SerializeField] private int chapterIndex = 1;
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private int baseMaxCost = 4;
        [SerializeField] private int maxCostCap = 10;
        [SerializeField] private int cardsDrawnPerTurn = 3;
        [SerializeField] private int maxHandSize = 5;
        [SerializeField] private int resourcesPerTurn = 14;
        [SerializeField] private int maxResources = 36;
        [SerializeField] private int startingEmptyScrollCount = 15;
        [SerializeField] private string gameOverSceneName = "GameOverScene";

        public event Action StateChanged;

        public BattlePhase Phase { get; private set; }
        public CombatantState Player { get; } = new();
        public CombatantState Enemy => SelectedEnemy;
        public List<CombatantState> Enemies { get; } = new();
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
        public int MaxHandSize => maxHandSize;
        public int DrawDeckCount => drawDeck.Count;
        public int DiscardCount => DiscardPile.Count;
        public int EnemyIntentDamage => GetEnemyIntentDamage(SelectedEnemyIndex);
        public string EnemyIntentText => GetEnemyIntentText(SelectedEnemyIndex);
        public int SelectedEnemyIndex { get; private set; }
        public int EnemyCount => Enemies.Count;
        public CombatantState SelectedEnemy => GetEnemy(SelectedEnemyIndex);
        public string LastLog { get; private set; } = string.Empty;
        public EnemyPose EnemyPose => GetEnemyPose(SelectedEnemyIndex);
        public bool CardResetModeActive => cardResetMode;
        public bool CanEnterCardResetMode => Phase == BattlePhase.PlayerTurn && !InputLocked && (ChapterCardResetUnlocked.TryGetValue(chapterIndex, out var unlocked) && unlocked || Hand.Exists(CanResetCard));
        public int PlayerHitPulse { get; private set; }
        public bool InputLocked { get; private set; }
        public string TurnBannerText { get; private set; } = string.Empty;
        public int TurnBannerPulse { get; private set; }
        public string EnemyActionLog { get; private set; } = string.Empty;
        public int EnemyActionPulse { get; private set; }
        public int MergePulse { get; private set; }
        public int LastMergedResourceIndex { get; private set; } = -1;

        private readonly Queue<ScrollCard> drawDeck = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDrawDecks = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterDiscardDecks = new();
        private static readonly Dictionary<int, List<ScrollCard>> ChapterHandCards = new();
        private static readonly Dictionary<int, List<MergeResource>> ChapterResources = new();
        private static readonly Dictionary<int, bool> ChapterCardResetUnlocked = new();
        private readonly CombatantState fallbackEnemy = new();
        private Coroutine enemyPoseRoutine;
        private Coroutine turnRoutine;
        private Coroutine stageStartRoutine;
        private int inputLockToken;
        private bool gameOverTriggered;
        private bool cardResetMode;
        private bool duCookieAttackGuardSummoned;
        private bool duCookieAttackGuardDefeated;
        private bool duCookieShieldGuardSummoned;
        private readonly List<EnemyPose> enemyPoses = new();
        private int ResourceLimit => maxResources > 0 ? maxResources : 36;

        public static void ResetChapterRun(int chapter)
        {
            ChapterDrawDecks.Remove(chapter);
            ChapterDiscardDecks.Remove(chapter);
            ChapterHandCards.Remove(chapter);
            ChapterResources.Remove(chapter);
            ChapterCardResetUnlocked.Remove(chapter);
            PlayerPrefs.SetInt($"{ClearedStageKeyPrefix}{chapter}", 0);
            PlayerPrefs.SetInt($"{ChapterEntrySeenKeyPrefix}{chapter}", 0);
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
                Player.Reset("마카롱", 50, ElementType.None, 0);
            }
        }

        private void Start()
        {
            StartStage();
        }

        public void StartStage()
        {
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
                turnRoutine = null;
            }

            if (enemyPoseRoutine != null)
            {
                StopCoroutine(enemyPoseRoutine);
                enemyPoseRoutine = null;
            }

            InputLocked = false;
            inputLockToken++;
            MaxCost = baseMaxCost + Mathf.Max(0, stageIndex - 1);
            CurrentCost = 0;
            gameOverTriggered = false;
            Player.Reset("마카롱", 50, ElementType.None, 0);
            BuildStageEnemies();
            Hand.Clear();
            SetEnemyPose(EnemyPose.Idle);
            LoadChapterDeckState();
            NotifyChanged();

            if (stageStartRoutine != null)
            {
                StopCoroutine(stageStartRoutine);
            }

            stageStartRoutine = StartCoroutine(BeginStageAfterDialogueRoutine());
        }

        public void DebugGoToStageDelta(int delta)
        {
            DebugGoToStage(stageIndex + delta);
        }

        public void DebugGoToStage(int targetStage)
        {
            stageIndex = Mathf.Clamp(targetStage, 1, FinalStageIndex);
            PlayerPrefs.SetInt(SelectedChapterKey, chapterIndex);
            PlayerPrefs.SetInt(SelectedStageKey, stageIndex);
            PlayerPrefs.Save();
            StartStage();
        }

        private IEnumerator BeginStageAfterDialogueRoutine()
        {
            var isWaitingDialogue = true;
            DialogueManager.PlayResource(DialogueKeys.StageIntro(chapterIndex, stageIndex), () => isWaitingDialogue = false);

            while (isWaitingDialogue)
            {
                yield return null;
            }

            BeginPlayerTurn();
            stageStartRoutine = null;
        }

        public CombatantState GetEnemy(int index)
        {
            if (index >= 0 && index < Enemies.Count)
            {
                return Enemies[index];
            }

            return fallbackEnemy;
        }

        public EnemyPose GetEnemyPose(int index)
        {
            if (index >= 0 && index < enemyPoses.Count)
            {
                return enemyPoses[index];
            }

            return EnemyPose.Idle;
        }

        public void SelectEnemy(int index)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn || index < 0 || index >= Enemies.Count || Enemies[index].IsDead)
            {
                return;
            }

            SelectedEnemyIndex = index;
            LastLog = $"{Enemies[index].Name} 선택";
            NotifyChanged();
        }

        private void BuildStageEnemies()
        {
            Enemies.Clear();
            enemyPoses.Clear();
            ClearBossRuntimeFlags();

            switch (stageIndex)
            {
                case 1:
                    AddFormChangingEnemy("요거트 아이스크림 슬라임 A", 38, ElementType.Berry, 2);
                    AddFormChangingEnemy("요거트 아이스크림 슬라임 B", 38, ElementType.PoppingCandy, 2);
                    break;
                case 2:
                    AddEnemy("딸기 탕후루 경호원(삼단봉)", 72, ElementType.Chocolate, 2);
                    AddEnemy("감귤 탕후루 경호원(방패)", 82, ElementType.Marshmallow, 3);
                    break;
                default:
                    AddEnemy("두바이 쫀득 쿠키", 150, ElementType.Berry, 4);
                    break;
            }

            SelectFirstLivingEnemy();
        }

        private void AddEnemy(string displayName, int maxHp, ElementType weakness, int shieldCount)
        {
            var enemy = new CombatantState();
            enemy.Reset(displayName, maxHp, weakness, shieldCount);
            Enemies.Add(enemy);
            enemyPoses.Add(EnemyPose.Idle);
        }

        private void AddFormChangingEnemy(string displayName, int maxHp, ElementType weakness, int shieldCount)
        {
            AddEnemy(displayName, maxHp, weakness, shieldCount);
            var enemy = Enemies[Enemies.Count - 1];
            enemy.ChangesFormOnWeaknessHit = true;
            enemy.FormIndex = WeaknessToFormIndex(weakness);
        }

        private bool SelectFirstLivingEnemy()
        {
            for (var i = 0; i < Enemies.Count; i++)
            {
                if (!Enemies[i].IsDead)
                {
                    SelectedEnemyIndex = i;
                    return true;
                }
            }

            SelectedEnemyIndex = 0;
            return false;
        }

        public void BeginPlayerTurn()
        {
            Phase = BattlePhase.PlayerTurn;
            Player.Guard = 0;
            CurrentCost = Mathf.Min(CurrentCost + MaxCost, maxCostCap);
            DrawEmptyScrolls(cardsDrawnPerTurn);
            LastLog = "플레이어 턴";
            AddTurnResources();
            ShowTurnBanner("플레이어 턴!", 2f);
            NotifyChanged();
        }

        public bool MergeFirstPair(ResourceFamily family)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

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
            LastLog = "자원 합성";
            MarkMerged(first);
            CompactResourceStorage();
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryMergeResources(MergeResource firstResource, MergeResource secondResource)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            if (firstResource.Family != secondResource.Family || firstResource.Stage != secondResource.Stage || firstResource.Stage >= 3)
            {
                LastLog = "같은 자원만 합성 가능";
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
            LastLog = "자원 합성";
            MarkMerged(firstIndex);
            CompactResourceStorage();
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryMergeResourceSlots(int firstIndex, int secondIndex)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            if (firstIndex == secondIndex || firstIndex < 0 || secondIndex < 0 || firstIndex >= Resources.Count || secondIndex >= Resources.Count)
            {
                return false;
            }

            var firstResource = Resources[firstIndex];
            var secondResource = Resources[secondIndex];
            if (!firstResource.CanUse || !secondResource.CanUse || firstResource.Family != secondResource.Family || firstResource.Stage != secondResource.Stage || firstResource.Stage >= 3)
            {
                LastLog = "같은 자원만 합성 가능";
                NotifyChanged();
                return false;
            }

            var resultResource = new MergeResource(firstResource.Family, firstResource.Stage + 1);
            Resources[secondIndex] = resultResource;
            Resources[firstIndex] = MergeResource.Empty;
            var resultIndex = firstIndex < secondIndex ? secondIndex - 1 : secondIndex;
            CompactResourceStorage();
            LastLog = "자원 합성";
            MarkMerged(resultIndex);
            SaveChapterDeckState();
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
            CompactResourceStorage();
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryConsumeResourceSlots(params int[] resourceIndices)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

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

            CompactResourceStorage();
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool CraftFirstAvailableScroll()
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            var handIndex = Hand.FindIndex(card => card != null && card.IsEmpty);
            if (handIndex < 0)
            {
                LastLog = "빈 스크롤 없음";
                NotifyChanged();
                return false;
            }

            var baseIndex = Resources.FindIndex(resource => resource.CanUse && resource.Role == ResourceRole.Base && resource.Stage > 0);
            if (baseIndex < 0)
            {
                LastLog = "베이스 자원 필요";
                NotifyChanged();
                return false;
            }

            var toppingIndex = Resources.FindIndex(resource => resource.CanUse && resource.Role == ResourceRole.Topping && resource.Stage > 0);
            if (toppingIndex < 0)
            {
                LastLog = "토핑 자원 필요";
                NotifyChanged();
                return false;
            }

            var baseResource = Resources[baseIndex];
            var toppingResource = Resources[toppingIndex];
            Resources[baseIndex] = MergeResource.Empty;
            Resources[toppingIndex] = MergeResource.Empty;
            CompactResourceStorage();
            Hand[handIndex] = ScrollCard.Craft(baseResource, toppingResource, Hand[handIndex].Id);
            LastLog = "스크롤 제작";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryCraftHandScroll(int handIndex, MergeResource baseResource, MergeResource? toppingResource, int baseResourceIndex, int toppingResourceIndex, out ScrollCard craftedCard)
        {
            craftedCard = null;
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            if (handIndex < 0 || handIndex >= Hand.Count || Hand[handIndex] == null || !Hand[handIndex].IsEmpty)
            {
                LastLog = "빈 스크롤 필요";
                NotifyChanged();
                return false;
            }

            if (baseResource.Stage <= 0)
            {
                LastLog = "베이스 자원 필요";
                NotifyChanged();
                return false;
            }

            if (!toppingResource.HasValue || toppingResource.Value.Stage <= 0)
            {
                LastLog = "토핑 자원 필요";
                NotifyChanged();
                return false;
            }

            if (!ResourceSlotMatches(baseResourceIndex, baseResource) || !ResourceSlotMatches(toppingResourceIndex, toppingResource.Value))
            {
                LastLog = "자원 변경됨";
                NotifyChanged();
                return false;
            }

            craftedCard = ScrollCard.Craft(baseResource, toppingResource, Hand[handIndex].Id);
            Resources[baseResourceIndex] = MergeResource.Empty;
            Resources[toppingResourceIndex] = MergeResource.Empty;
            CompactResourceStorage();
            MoveCraftedCardToFront(handIndex, craftedCard);
            LastLog = "스크롤 제작";
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
                LastLog = "손패에 없음";
                NotifyChanged();
                return false;
            }

            return TryPlayCardAt(handIndex);
        }

        public void ToggleCardResetMode()
        {
            if (!CanEnterCardResetMode)
            {
                cardResetMode = false;
                LastLog = "초기화 가능한 카드 없음";
                NotifyChanged();
                return;
            }

            cardResetMode = !cardResetMode;
            LastLog = cardResetMode ? "초기화할 카드 선택" : "카드 초기화 취소";
            NotifyChanged();
        }

        public bool TryResetHandCard(int handIndex)
        {
            if (!cardResetMode || handIndex < 0 || handIndex >= Hand.Count)
            {
                return false;
            }

            var card = Hand[handIndex];
            if (!CanResetCard(card))
            {
                LastLog = "초기화 가능한 카드 아님";
                cardResetMode = false;
                NotifyChanged();
                return false;
            }

            ReturnCraftResourcesToBag(card);
            Hand[handIndex] = CreateEmptyScroll(card.Id);
            cardResetMode = false;
            LastLog = "스크롤 초기화";
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public bool TryResetCardById(int cardId)
        {
            var handIndex = Hand.FindIndex(card => card != null && card.Id == cardId);
            return TryResetHandCard(handIndex);
        }

        private static bool CanResetCard(ScrollCard card)
        {
            return card != null && !card.IsEmpty && (card.UpgradeLevel > 0 || card.IsContaminated);
        }

        private bool TryPlayCardAt(int handIndex)
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn)
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
                LastLog = "빈 스크롤";
                NotifyChanged();
                return false;
            }

            if (CurrentCost < card.Cost)
            {
                LastLog = "행동력 부족";
                NotifyChanged();
                return false;
            }

            CurrentCost -= card.Cost;
            if (ApplyContaminatedCardUsePenalty(card) && Player.IsDead)
            {
                TriggerGameOver();
                return false;
            }

            ResolveCard(card);
            DiscardPile.Add(card);
            Hand.RemoveAt(handIndex);
            SaveChapterDeckState();
            NotifyChanged();
            return true;
        }

        public void PlayFirstScroll()
        {
            if (InputLocked || Phase != BattlePhase.PlayerTurn || Hand.Count == 0)
            {
                return;
            }

            var handIndex = Hand.FindIndex(card => card != null && !card.IsEmpty);
            if (handIndex < 0)
            {
                LastLog = "제작 스크롤 없음";
                NotifyChanged();
                return;
            }

            var card = Hand[handIndex];
            if (CurrentCost < card.Cost)
            {
                LastLog = "행동력 부족";
                NotifyChanged();
                return;
            }

            CurrentCost -= card.Cost;
            if (ApplyContaminatedCardUsePenalty(card) && Player.IsDead)
            {
                TriggerGameOver();
                return;
            }

            ResolveCard(card);
            DiscardPile.Add(card);
            Hand.RemoveAt(handIndex);
            SaveChapterDeckState();
            NotifyChanged();
        }

        public void EndTurn()
        {
            if (InputLocked)
            {
                return;
            }

            if (Phase == BattlePhase.StageClear)
            {
                ContinueAfterStageClear();
                return;
            }

            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            cardResetMode = false;
            ApplyContaminatedHandPenalty();
            if (Player.IsDead)
            {
                TriggerGameOver();
                return;
            }

            SaveChapterDeckState();
            Phase = BattlePhase.EnemyTurn;
            ShowTurnBanner("적 턴!", 2f);
            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
            }

            turnRoutine = StartCoroutine(EnemyTurnRoutine());
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
                LastLog = "챕터 클리어";
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
            var target = Enemy;
            var targetIndex = SelectedEnemyIndex;
            switch (card.EffectType)
            {
                case ScrollEffectType.Attack:
                    var damage = card.Power + Player.Strength;
                    if (card.Element == target.Weakness)
                    {
                        damage = Mathf.RoundToInt(damage * 1.5f);
                        BreakShieldOrReward(target, card.Element);
                        if (target.ChangesFormOnWeaknessHit)
                        {
                            target.PendingFormChange = true;
                        }
                    }

                    if (target.IsBroken)
                    {
                        damage *= 2;
                    }

                    var dealt = target.TakeDamage(damage);
                    SetEnemyPose(targetIndex, EnemyPose.Hit, 0.45f);
                    LastLog = $"피해 {dealt}";
                    break;
                case ScrollEffectType.Guard:
                    Player.Guard += card.Power;
                    LastLog = $"방어 {card.Power}";
                    break;
                case ScrollEffectType.Buff:
                    Player.Strength += card.Power;
                    LastLog = $"공격 강화 {card.Power}";
                    break;
                case ScrollEffectType.Debuff:
                    target.Strength -= card.Power;
                    LastLog = $"적 약화 {card.Power}";
                    break;
            }

            if (target.IsDead)
            {
                if (stageIndex == 3 && IsStageThreeAttackGuard(targetIndex, true))
                {
                    duCookieAttackGuardDefeated = true;
                }

                SelectFirstLivingEnemy();
            }

            if (AllEnemiesDead())
            {
                Phase = BattlePhase.StageClear;
                LastLog = "스테이지 클리어";
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

        private void BreakShieldOrReward(CombatantState target, ElementType element)
        {
            if (target.WeaknessHitsRemaining > 0)
            {
                target.WeaknessHitsRemaining--;
                if (target.WeaknessHitsRemaining == 0)
                {
                    target.IsBroken = true;
                    LastLog = "브레이크";
                }
            }

            // Breaking the enemy should not inject extra inventory resources.
        }

        private bool AllEnemiesDead()
        {
            if (Enemies.Count == 0)
            {
                return false;
            }

            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator EnemyTurnRoutine()
        {
            NotifyChanged();
            yield return new WaitForSeconds(2f);
            yield return EnemyActionsRoutine();
            turnRoutine = null;
        }

        private IEnumerator EnemyActionsRoutine()
        {
            for (var i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                if (enemy.IsDead)
                {
                    continue;
                }

                SelectedEnemyIndex = i;
                NotifyChanged();

                if (ApplyPendingFormChange(enemy))
                {
                    NotifyChanged();
                    yield return new WaitForSeconds(0.45f);
                }

                if (enemy.IsBroken)
                {
                    enemy.IsBroken = false;
                    enemy.WeaknessHitsRemaining = enemy.WeaknessHitsRequired;
                    SetEnemyPose(i, EnemyPose.Idle);
                    LastLog = $"{enemy.Name} 행동 불가";
                    NotifyChanged();
                    yield return new WaitForSeconds(0.45f);
                    continue;
                }

                yield return EnemyActionRoutine(i);
                if (Player.IsDead)
                {
                    yield break;
                }
            }

            SelectFirstLivingEnemy();
            BeginPlayerTurn();
        }

        private bool ApplyPendingFormChange(CombatantState enemy)
        {
            if (enemy == null || !enemy.ChangesFormOnWeaknessHit || !enemy.PendingFormChange || enemy.IsDead)
            {
                return false;
            }

            enemy.PendingFormChange = false;
            enemy.FormIndex = (enemy.FormIndex + 1) % 4;
            enemy.Weakness = FormIndexToWeakness(enemy.FormIndex);
            enemy.WeaknessHitsRemaining = enemy.WeaknessHitsRequired;
            LastLog = $"{enemy.Name} 폼 변경";
            return true;
        }

        private static int WeaknessToFormIndex(ElementType weakness)
        {
            return weakness switch
            {
                ElementType.PoppingCandy => 0,
                ElementType.Marshmallow => 1,
                ElementType.Chocolate => 2,
                ElementType.Berry => 3,
                _ => 0
            };
        }

        private static ElementType FormIndexToWeakness(int formIndex)
        {
            return formIndex switch
            {
                0 => ElementType.PoppingCandy,
                1 => ElementType.Marshmallow,
                2 => ElementType.Chocolate,
                _ => ElementType.Berry
            };
        }

        private bool ApplyContaminatedCardUsePenalty(ScrollCard card)
        {
            if (card == null || !card.IsContaminated)
            {
                return false;
            }

            const int damage = 5;
            var dealt = Player.TakeDamage(damage);
            PlayerHitPulse++;
            LastLog = $"오염 스크롤 피해 {dealt}";
            NotifyChanged();
            return true;
        }

        private void ApplyContaminatedHandPenalty()
        {
            var contaminatedCount = Hand.FindAll(card => card != null && !card.IsEmpty && card.IsContaminated).Count;
            if (contaminatedCount <= 0)
            {
                return;
            }

            var damage = contaminatedCount * 3;
            var dealt = Player.TakeDamage(damage);
            PlayerHitPulse++;
            LastLog = $"오염 스크롤 방치 피해 {dealt}";
            NotifyChanged();
        }

        private bool ContaminateHandCard()
        {
            var index = Hand.FindIndex(card => card != null && !card.IsEmpty && !card.IsContaminated);
            if (index < 0)
            {
                return false;
            }

            Hand[index].IsContaminated = true;
            SaveChapterDeckState();
            return true;
        }

        private bool HasContaminatableCard()
        {
            return Hand.Exists(card => card != null && !card.IsEmpty && !card.IsContaminated);
        }

        private IEnumerator EnemyActionRoutine(int enemyIndex)
        {
            if (stageIndex == 3)
            {
                if (enemyIndex == 0)
                {
                    yield return DuCookieActionRoutine(enemyIndex);
                    yield break;
                }

                if (IsStageThreeAttackGuard(enemyIndex))
                {
                    yield return StageThreeSummonedGuardActionRoutine(enemyIndex, true);
                    yield break;
                }

                if (IsStageThreeShieldGuard(enemyIndex))
                {
                    yield return StageThreeSummonedGuardActionRoutine(enemyIndex, false);
                    yield break;
                }
            }

            if (stageIndex == 2)
            {
                if (enemyIndex == 0)
                {
                    yield return StrawberryGuardActionRoutine(enemyIndex);
                    yield break;
                }

                if (enemyIndex == 1)
                {
                    yield return MandarinGuardActionRoutine(enemyIndex);
                    yield break;
                }
            }

            yield return EnemyAttackRoutine(enemyIndex);
        }

        private IEnumerator DuCookieActionRoutine(int enemyIndex)
        {
            var boss = GetEnemy(enemyIndex);
            var actionCount = boss.AiTurnCount;
            boss.AiTurnCount++;

            if (!duCookieAttackGuardSummoned)
            {
                duCookieAttackGuardSummoned = true;
                SummonStageThreeGuard("딸기 탕후루 경호원(삼단봉)", 62, ElementType.Chocolate, 2);
                EnemyActionLog = "두바이 쫀득 쿠키 경호원 소환";
                EnemyActionPulse++;
                LastLog = "공격 경호원 소환";
                NotifyChanged();
                yield return new WaitForSeconds(0.85f);
                yield break;
            }

            if (!duCookieShieldGuardSummoned && duCookieAttackGuardDefeated)
            {
                duCookieShieldGuardSummoned = true;
                SummonStageThreeGuard("감귤 탕후루 경호원(방패)", 70, ElementType.Marshmallow, 3);
                EnemyActionLog = "두바이 쫀득 쿠키 방패 호출";
                EnemyActionPulse++;
                LastLog = "방어 경호원 소환";
                NotifyChanged();
                yield return new WaitForSeconds(0.85f);
                yield break;
            }

            if (actionCount % 3 == 2 && ContaminateHandCard())
            {
                EnemyActionLog = "두바이 쫀득 쿠키 초코 마시멜로";
                EnemyActionPulse++;
                LastLog = "스크롤 오염";
                NotifyChanged();
                yield return new WaitForSeconds(0.85f);
                yield break;
            }

            yield return EnemyAttackRoutine(enemyIndex, GetEnemyIntentDamage(enemyIndex));
        }

        private IEnumerator StageThreeSummonedGuardActionRoutine(int enemyIndex, bool attackGuard)
        {
            var enemy = GetEnemy(enemyIndex);
            if (enemy.AiTurnCount < 0)
            {
                enemy.AiTurnCount = 0;
                EnemyActionLog = $"{enemy.Name} 대기";
                EnemyActionPulse++;
                LastLog = "소환 직후 대기";
                NotifyChanged();
                yield return new WaitForSeconds(0.55f);
                yield break;
            }

            if (attackGuard)
            {
                yield return StrawberryGuardActionRoutine(enemyIndex);
                yield break;
            }

            yield return MandarinGuardActionRoutine(enemyIndex);
        }

        private void SummonStageThreeGuard(string displayName, int maxHp, ElementType weakness, int shieldCount)
        {
            AddEnemy(displayName, maxHp, weakness, shieldCount);
            var summoned = Enemies[Enemies.Count - 1];
            summoned.AiTurnCount = -1;
        }

        private IEnumerator StrawberryGuardActionRoutine(int enemyIndex)
        {
            var enemy = GetEnemy(enemyIndex);
            if (enemy.AiTurnCount % 2 == 1)
            {
                const int chargeBonus = 4;
                enemy.Strength += chargeBonus;
                enemy.AttackChargeBonus += chargeBonus;
                enemy.AiTurnCount++;
                EnemyActionLog = $"{enemy.Name} 예열";
                EnemyActionPulse++;
                LastLog = "딸기 경호원 공격 준비";
                NotifyChanged();
                yield return new WaitForSeconds(0.75f);
                yield break;
            }

            var attack = GetEnemyIntentDamage(enemyIndex);
            enemy.AiTurnCount++;
            yield return EnemyAttackRoutine(enemyIndex, attack);
            if (enemy.AttackChargeBonus > 0)
            {
                enemy.Strength -= enemy.AttackChargeBonus;
                enemy.AttackChargeBonus = 0;
                NotifyChanged();
            }
        }

        private IEnumerator MandarinGuardActionRoutine(int enemyIndex)
        {
            var enemy = GetEnemy(enemyIndex);
            var shouldHeal = ShouldMandarinHeal(enemy, true);
            var shouldShield = ShouldMandarinShieldTarget();
            var attack = GetEnemyIntentDamage(enemyIndex);
            enemy.AiTurnCount++;

            if (shouldHeal)
            {
                var amount = Mathf.Min(10, enemy.MaxHp - enemy.Hp);
                enemy.Hp += amount;
                EnemyActionLog = $"{enemy.Name} 회복 {amount}";
                EnemyActionPulse++;
                LastLog = "감귤 경호원 회복";
                NotifyChanged();
                yield return new WaitForSeconds(0.75f);
                yield break;
            }

            if (shouldShield)
            {
                var strawberry = GetMandarinShieldTarget();
                const int shieldAmount = 9;
                strawberry.Guard += shieldAmount;
                EnemyActionLog = $"{enemy.Name} 방어막 {shieldAmount}";
                EnemyActionPulse++;
                LastLog = "딸기 경호원 보호";
                NotifyChanged();
                yield return new WaitForSeconds(0.75f);
                yield break;
            }

            yield return EnemyAttackRoutine(enemyIndex, attack);
        }

        private static bool ShouldMandarinHeal(CombatantState enemy, bool nextAction = false)
        {
            var turnCount = enemy == null ? 0 : enemy.AiTurnCount + (nextAction ? 1 : 0);
            return enemy != null
                && enemy.Hp > 0
                && enemy.Hp <= Mathf.CeilToInt(enemy.MaxHp * 0.45f)
                && enemy.Hp < enemy.MaxHp
                && turnCount % 3 == 0;
        }

        private bool ShouldMandarinShieldTarget()
        {
            var strawberry = GetMandarinShieldTarget();
            return strawberry != null && !strawberry.IsDead && strawberry.Guard <= 0;
        }

        private CombatantState GetMandarinShieldTarget()
        {
            if (stageIndex == 3)
            {
                for (var i = 1; i < Enemies.Count; i++)
                {
                    if (IsStageThreeAttackGuard(i))
                    {
                        return Enemies[i];
                    }
                }

                return null;
            }

            return GetEnemy(0);
        }

        private bool IsStageThreeAttackGuard(int enemyIndex, bool includeDead = false)
        {
            return stageIndex == 3
                && enemyIndex > 0
                && enemyIndex < Enemies.Count
                && (includeDead || !Enemies[enemyIndex].IsDead)
                && Enemies[enemyIndex].Name.Contains("딸기");
        }

        private bool IsStageThreeShieldGuard(int enemyIndex, bool includeDead = false)
        {
            return stageIndex == 3
                && enemyIndex > 0
                && enemyIndex < Enemies.Count
                && (includeDead || !Enemies[enemyIndex].IsDead)
                && Enemies[enemyIndex].Name.Contains("감귤");
        }

        private IEnumerator EnemyAttackRoutine(int enemyIndex = -1)
        {
            yield return EnemyAttackRoutine(enemyIndex, GetEnemyIntentDamage(enemyIndex));
        }

        private IEnumerator EnemyAttackRoutine(int enemyIndex, int attack)
        {
            var actingIndex = enemyIndex >= 0 ? enemyIndex : SelectedEnemyIndex;
            var enemy = GetEnemy(actingIndex);
            attack = Mathf.Max(1, attack);
            EnemyActionLog = $"{enemy.Name} 공격 {attack}";
            EnemyActionPulse++;
            SetEnemyPose(actingIndex, EnemyPose.Attack, 0.7f);
            yield return new WaitForSeconds(0.55f);

            var dealt = Player.TakeDamage(attack);
            PlayerHitPulse++;
            LastLog = $"피해 받음 {dealt}";
            SetEnemyPose(actingIndex, EnemyPose.Idle);
            NotifyChanged();

            if (Player.IsDead)
            {
                TriggerGameOver();
                yield break;
            }
        }

        private int GetEnemyIntentDamage(int enemyIndex)
        {
            var enemy = GetEnemy(enemyIndex);
            if (enemy == null || enemy.IsBroken || enemy.IsDead)
            {
                return 0;
            }

            if (stageIndex == 2)
            {
                if (enemyIndex == 0)
                {
                    return enemy.AiTurnCount % 2 == 1 ? 0 : Mathf.Max(1, 14 + enemy.Strength);
                }

                if (enemyIndex == 1)
                {
                    return ShouldMandarinHeal(enemy, true) || ShouldMandarinShieldTarget() ? 0 : Mathf.Max(1, 8 + enemy.Strength);
                }
            }

            if (stageIndex == 3)
            {
                if (enemyIndex == 0)
                {
                    return Mathf.Max(1, 14 + enemy.Strength);
                }

                if (IsStageThreeAttackGuard(enemyIndex))
                {
                    return enemy.AiTurnCount < 0 || enemy.AiTurnCount % 2 == 1 ? 0 : Mathf.Max(1, 14 + enemy.Strength);
                }

                if (IsStageThreeShieldGuard(enemyIndex))
                {
                    return enemy.AiTurnCount < 0 || ShouldMandarinHeal(enemy, true) || ShouldMandarinShieldTarget() ? 0 : Mathf.Max(1, 8 + enemy.Strength);
                }
            }

            return Mathf.Max(1, 8 + enemy.Strength);
        }

        private string GetEnemyIntentText(int enemyIndex)
        {
            var enemy = GetEnemy(enemyIndex);
            if (enemy == null || enemy.IsDead)
            {
                return "-";
            }

            if (enemy.IsBroken)
            {
                return "행동 불가";
            }

            if (stageIndex == 2)
            {
                if (enemyIndex == 0 && enemy.AiTurnCount % 2 == 1)
                {
                    return "예열";
                }

                if (enemyIndex == 1)
                {
                    if (ShouldMandarinHeal(enemy, true))
                    {
                        return "회복";
                    }

                    if (ShouldMandarinShieldTarget())
                    {
                        return "방어막";
                    }
                }
            }

            if (stageIndex == 3)
            {
                if (enemyIndex == 0)
                {
                    if (!duCookieAttackGuardSummoned)
                    {
                        return "소환";
                    }

                    if (!duCookieShieldGuardSummoned && duCookieAttackGuardDefeated)
                    {
                        return "소환";
                    }

                    if (enemy.AiTurnCount % 3 == 2 && HasContaminatableCard())
                    {
                        return "오염";
                    }
                }

                if (IsStageThreeAttackGuard(enemyIndex))
                {
                    if (enemy.AiTurnCount < 0)
                    {
                        return "대기";
                    }

                    if (enemy.AiTurnCount % 2 == 1)
                    {
                        return "예열";
                    }
                }

                if (IsStageThreeShieldGuard(enemyIndex))
                {
                    if (enemy.AiTurnCount < 0)
                    {
                        return "대기";
                    }

                    if (ShouldMandarinHeal(enemy, true))
                    {
                        return "회복";
                    }

                    if (ShouldMandarinShieldTarget())
                    {
                        return "방어막";
                    }
                }
            }

            return $"공격 {GetEnemyIntentDamage(enemyIndex)}";
        }

        private void TriggerGameOver()
        {
            if (gameOverTriggered)
            {
                return;
            }

            gameOverTriggered = true;
            Phase = BattlePhase.GameOver;
            InputLocked = true;
            LastLog = "게임 오버";
            SaveChapterDeckState();
            NotifyChanged();
            turnRoutine = null;
            StartCoroutine(LoadGameOverSceneRoutine());
        }

        private IEnumerator LoadGameOverSceneRoutine()
        {
            yield return new WaitForSeconds(0.65f);
            var targetScene = string.IsNullOrWhiteSpace(gameOverSceneName) ? "GameOverScene" : gameOverSceneName;
            SceneManager.LoadScene(targetScene);
        }

        private void LoadChapterDeckState()
        {
            drawDeck.Clear();
            DiscardPile.Clear();
            Hand.Clear();

            var hasSavedDraw = ChapterDrawDecks.TryGetValue(chapterIndex, out var savedDrawDeck);
            var hasSavedDiscard = ChapterDiscardDecks.TryGetValue(chapterIndex, out var savedDiscardDeck);
            var hasSavedHand = ChapterHandCards.TryGetValue(chapterIndex, out var savedHand);
            var hasSavedResources = ChapterResources.TryGetValue(chapterIndex, out var savedResources);
            if (!hasSavedDraw && !hasSavedDiscard && !hasSavedHand)
            {
                savedDrawDeck = BuildEmptyScrollDeck();
                ChapterDrawDecks[chapterIndex] = savedDrawDeck;
                hasSavedDraw = true;
            }

            if (hasSavedDraw)
            {
                foreach (var card in savedDrawDeck)
                {
                    drawDeck.Enqueue(CloneCard(card));
                }
            }

            if (hasSavedDiscard)
            {
                foreach (var card in savedDiscardDeck)
                {
                    DiscardPile.Add(CloneCard(card));
                }
            }

            if (hasSavedHand)
            {
                foreach (var card in savedHand)
                {
                    Hand.Add(CloneCard(card));
                }
            }

            Resources.Clear();
            if (hasSavedResources)
            {
                Resources.AddRange(CloneResources(savedResources));
                CompactResourceStorage();
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

        private static ScrollCard CreateEmptyScroll(int sourceScrollId = 0)
        {
            var card = new ScrollCard
            {
                EffectType = ScrollEffectType.Attack,
                Element = ElementType.None,
                Power = 0,
                Cost = 0,
                DisplayName = "빈 스크롤",
                IsContaminated = false
            };
            card.Id = sourceScrollId > 0 ? sourceScrollId : ScrollCard.CreateId();
            return card;
        }

        private void SaveChapterDeckState()
        {
            ChapterDrawDecks[chapterIndex] = new List<ScrollCard>(CloneCards(drawDeck));
            ChapterDiscardDecks[chapterIndex] = new List<ScrollCard>(CloneCards(DiscardPile));
            ChapterHandCards[chapterIndex] = new List<ScrollCard>(CloneCards(Hand));
            ChapterResources[chapterIndex] = new List<MergeResource>(CloneResources(Resources));
        }

        private IEnumerable<ScrollCard> CloneCards(IEnumerable<ScrollCard> cards)
        {
            foreach (var card in cards)
            {
                if (card == null)
                {
                    continue;
                }

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
                DisplayName = card.DisplayName,
                UpgradeLevel = card.UpgradeLevel,
                BaseFamily = card.BaseFamily,
                BaseStage = card.BaseStage,
                ToppingFamily = card.ToppingFamily,
                ToppingStage = card.ToppingStage,
                IsContaminated = card.IsContaminated
            };
        }

        private static IEnumerable<MergeResource> CloneResources(IEnumerable<MergeResource> resources)
        {
            foreach (var resource in resources)
            {
                if (resource.CanUse)
                {
                    yield return new MergeResource(resource.Family, resource.Stage);
                }
            }
        }

        private void ClearBossRuntimeFlags()
        {
            duCookieAttackGuardSummoned = false;
            duCookieAttackGuardDefeated = false;
            duCookieShieldGuardSummoned = false;
        }

        private void SetEnemyPose(EnemyPose pose, float resetDelay = 0f)
        {
            SetEnemyPose(SelectedEnemyIndex, pose, resetDelay);
        }

        private void SetEnemyPose(int enemyIndex, EnemyPose pose, float resetDelay = 0f)
        {
            if (enemyIndex < 0 || enemyIndex >= enemyPoses.Count)
            {
                return;
            }

            enemyPoses[enemyIndex] = pose;
            NotifyChanged();

            if (enemyPoseRoutine != null)
            {
                StopCoroutine(enemyPoseRoutine);
                enemyPoseRoutine = null;
            }

            if (resetDelay > 0f)
            {
                enemyPoseRoutine = StartCoroutine(ResetEnemyPoseAfter(enemyIndex, resetDelay));
            }
        }

        private IEnumerator ResetEnemyPoseAfter(int enemyIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enemyIndex >= 0 && enemyIndex < enemyPoses.Count)
            {
                enemyPoses[enemyIndex] = EnemyPose.Idle;
            }

            enemyPoseRoutine = null;
            NotifyChanged();
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
                if (VisibleHandCount >= maxHandSize)
                {
                    break;
                }

                if (drawDeck.Count == 0)
                {
                    if (HasEmptyScrollInHand())
                    {
                        break;
                    }

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
            TrimHandSlots();
            var emptySlot = Hand.FindIndex(item => item == null);
            if (emptySlot >= 0)
            {
                Hand[emptySlot] = card;
                return;
            }

            if (Hand.Count < maxHandSize)
            {
                Hand.Add(card);
            }
        }

        private void MoveCraftedCardToFront(int sourceIndex, ScrollCard craftedCard)
        {
            if (sourceIndex >= 0 && sourceIndex < Hand.Count)
            {
                Hand.RemoveAt(sourceIndex);
            }

            Hand.Insert(0, craftedCard);
            while (Hand.Count > maxHandSize)
            {
                Hand.RemoveAt(Hand.Count - 1);
            }
        }

        private void RefillDrawDeckFromDiscard()
        {
            if (DiscardPile.Count == 0)
            {
                return;
            }

            foreach (var card in DiscardPile)
            {
                var loopedCard = CloneCard(card);
                loopedCard.UpgradeFromLoop();
                drawDeck.Enqueue(loopedCard);
            }

            DiscardPile.Clear();
            ChapterCardResetUnlocked[chapterIndex] = true;
            LastLog = "버림덱 회수";
        }

        private bool HasEmptyScrollInHand()
        {
            return Hand.Exists(card => card != null && card.IsEmpty);
        }

        private void TrimHandSlots()
        {
            while (Hand.Count > maxHandSize && Hand[Hand.Count - 1] == null)
            {
                Hand.RemoveAt(Hand.Count - 1);
            }
        }

        private void AddTurnResources()
        {
            CompactResourceStorage();

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
            var guaranteedBaseB = nonSugarBaseFamilies[UnityEngine.Random.Range(0, nonSugarBaseFamilies.Length)];
            var guaranteedToppingA = toppingFamilies[UnityEngine.Random.Range(0, toppingFamilies.Length)];
            var guaranteedToppingB = toppingFamilies[UnityEngine.Random.Range(0, toppingFamilies.Length)];
            var guaranteedToppingC = toppingFamilies[UnityEngine.Random.Range(0, toppingFamilies.Length)];
            var plannedResources = new List<ResourceFamily>
            {
                ResourceFamily.Sugar,
                ResourceFamily.Sugar,
                guaranteedBase,
                guaranteedBase,
                guaranteedBaseB,
                guaranteedBaseB,
                guaranteedToppingA,
                guaranteedToppingA,
                guaranteedToppingB,
                guaranteedToppingB,
                guaranteedToppingC,
                guaranteedToppingC
            };

            var weightedFamilies = new[]
            {
                ResourceFamily.Sugar,
                ResourceFamily.Sugar,
                ResourceFamily.Dough,
                ResourceFamily.Dairy,
                ResourceFamily.Egg,
                ResourceFamily.Berry,
                ResourceFamily.Chocolate,
                ResourceFamily.Marshmallow,
                ResourceFamily.PoppingCandy,
                ResourceFamily.Berry,
                ResourceFamily.Chocolate,
                ResourceFamily.Marshmallow,
                ResourceFamily.PoppingCandy
            };

            while (plannedResources.Count < resourcesPerTurn)
            {
                plannedResources.Add(weightedFamilies[UnityEngine.Random.Range(0, weightedFamilies.Length)]);
            }

            InsertResourcePackageAtFront(plannedResources);
            SaveChapterDeckState();
        }

        private void CompactResourceStorage()
        {
            Resources.RemoveAll(resource => !resource.CanUse);
        }

        private void InsertResourcePackageAtFront(List<ResourceFamily> plannedResources)
        {
            var resources = new List<MergeResource>();
            foreach (var family in plannedResources)
            {
                resources.Add(new MergeResource(family, 0));
            }

            InsertResourcesAtFront(resources, plannedResources.Count);
        }

        private void ReturnCraftResourcesToBag(ScrollCard card)
        {
            var returnedResources = new List<MergeResource>
            {
                new(card.BaseFamily, card.BaseStage)
            };

            if (card.ToppingStage > 0)
            {
                returnedResources.Add(new MergeResource(card.ToppingFamily, card.ToppingStage));
            }

            InsertResourcesAtFront(returnedResources, returnedResources.Count);
            CompactResourceStorage();
        }

        private void InsertResourcesAtFront(List<MergeResource> resources, int protectedFrontCount)
        {
            if (resources == null || resources.Count == 0)
            {
                return;
            }

            CompactResourceStorage();
            var overflow = Mathf.Max(0, ActiveResourceCount + resources.Count - ResourceLimit);
            for (var i = resources.Count - 1; i >= 0; i--)
            {
                Resources.Insert(0, resources[i]);
            }

            TrimResourceListToLimit(protectedFrontCount);
            if (overflow > 0)
            {
                LastLog = $"자원 +{resources.Count} / 보관함 가득 참";
            }
        }

        private void TrimResourceListToLimit(int protectedFrontCount)
        {
            while (Resources.Count > ResourceLimit)
            {
                var removeIndex = FindDisposableResourceIndex(protectedFrontCount);
                if (removeIndex < protectedFrontCount)
                {
                    removeIndex = Resources.Count - 1;
                }

                Resources.RemoveAt(removeIndex);
            }
        }

        private int FindDisposableResourceIndex(int protectedFrontCount = 0)
        {
            var emptyIndex = Resources.FindLastIndex(resource => !resource.CanUse);
            if (emptyIndex >= protectedFrontCount)
            {
                return emptyIndex;
            }

            for (var disposableStage = 0; disposableStage <= 3; disposableStage++)
            {
                var index = Resources.FindLastIndex(resource =>
                    resource.CanUse &&
                    resource.Stage == disposableStage &&
                    resource.Family != ResourceFamily.Sugar);
                if (index >= protectedFrontCount)
                {
                    return index;
                }
            }

            return Resources.FindLastIndex(resource => resource.CanUse);
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }

        private void MarkMerged(int index)
        {
            LastMergedResourceIndex = index;
            MergePulse++;
        }

        private void ShowTurnBanner(string text, float duration)
        {
            TurnBannerText = text;
            TurnBannerPulse++;
            if (duration > 0f)
            {
                inputLockToken++;
                StartCoroutine(InputLockRoutine(duration, inputLockToken));
            }
        }

        private IEnumerator InputLockRoutine(float duration, int token)
        {
            InputLocked = true;
            NotifyChanged();
            yield return new WaitForSeconds(duration);
            if (token == inputLockToken)
            {
                InputLocked = false;
                NotifyChanged();
            }
        }
    }
}
