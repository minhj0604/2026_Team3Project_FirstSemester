using Team3Project.GameSystems;
using System.Collections;
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
        [SerializeField] private Text deckText;
        [SerializeField] private Text drawDeckText;
        [SerializeField] private Text discardDeckText;
        [SerializeField] private Text playerEffectText;
        [SerializeField] private Text playerGuardStatusText;
        [SerializeField] private Text playerStrengthStatusText;
        [SerializeField] private Text enemyInfoText;
        [SerializeField] private Text enemyEffectText;
        [SerializeField] private Text enemyIntentText;
        [SerializeField] private Button mergeSugarButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image enemyHealthFill;
        [SerializeField] private Image enemyLocalHealthFill;
        [SerializeField] private Image enemyIconImage;
        [SerializeField] private Image actionPointIcon;
        [SerializeField] private Sprite enemyProfileSprite;
        [SerializeField] private Sprite enemyHitProfileSprite;
        [SerializeField] private Sprite dairyStage0Sprite;
        [SerializeField] private Sprite dairyStage1Sprite;
        [SerializeField] private Sprite dairyStage2Sprite;
        [SerializeField] private Sprite dairyStage3Sprite;
        [SerializeField] private Sprite eggStage0Sprite;
        [SerializeField] private Sprite eggStage1Sprite;
        [SerializeField] private Sprite eggStage2Sprite;
        [SerializeField] private Sprite eggStage3Sprite;

        private readonly Dictionary<ResourceFamily, Sprite> resourceSprites = new();
        private readonly Dictionary<RectTransform, float> fillWidths = new();
        private readonly Dictionary<RectTransform, Vector2> fillPositions = new();
        private readonly List<DragMergeItem> resourceSlots = new();
        private readonly List<Image> actionPointIcons = new();
        private DragMergeItem resourceTemplate;
        private DragScrollItem[] scrollItems = new DragScrollItem[0];
        private readonly List<EnemyBattleSpriteController> enemyVisuals = new();
        private EnemyBattleSpriteController enemyVisualTemplate;
        private Vector2 enemyVisualBasePosition;
        private RectTransform resourceStorage;
        private Sprite fallbackEnemyIconSprite;
        private Text turnBannerText;
        private Text enemyActionLogText;
        private int lastTurnBannerPulse = -1;
        private int lastEnemyActionPulse = -1;
        private int lastMergePulse = -1;
        private Coroutine turnBannerRoutine;
        private Coroutine enemyActionRoutine;

        private void Awake()
        {
            if (battle == null)
            {
                battle = FindFirstObjectByType<BattleController>();
            }

            playerText = playerText == null ? FindText("Player Text") : playerText;
            enemyText = enemyText == null ? FindText("Enemy Text") : enemyText;
            costText = FindText("Action Point Text") ?? costText ?? FindText("Cost Text");
            handText = handText == null ? FindText("Hand Text") : handText;
            resourceText = resourceText == null ? FindText("Resource Text") : resourceText;
            logText = logText == null ? FindText("Log Text") : logText;
            deckText = deckText == null ? FindText("Deck Count Text") : deckText;
            drawDeckText = drawDeckText == null ? FindText("Draw Deck Count Text") : drawDeckText;
            discardDeckText = discardDeckText == null ? FindText("Discard Deck Count Text") : discardDeckText;
            playerEffectText = playerEffectText == null ? FindText("Player Effect Text") : playerEffectText;
            playerGuardStatusText = playerGuardStatusText == null ? FindText("Player Guard Status Text") : playerGuardStatusText;
            playerStrengthStatusText = playerStrengthStatusText == null ? FindText("Player Strength Status Text") : playerStrengthStatusText;
            enemyInfoText = enemyInfoText == null ? FindText("Enemy Info Text") : enemyInfoText;
            enemyEffectText = enemyEffectText == null ? FindText("Enemy Effect Text") : enemyEffectText;
            enemyIntentText = enemyIntentText == null ? FindText("Enemy Intent Text") : enemyIntentText;
            playerHealthFill = playerHealthFill == null ? FindImage("Player Health Bar Fill") : playerHealthFill;
            enemyHealthFill = enemyHealthFill == null ? FindImage("Enemy Info Health Bar Fill") : enemyHealthFill;
            enemyLocalHealthFill = enemyLocalHealthFill == null ? FindImage("Enemy Local Health Bar Fill") : enemyLocalHealthFill;
            enemyIconImage = enemyIconImage == null ? FindImage("Enemy Info Icon") : enemyIconImage;
            actionPointIcon = actionPointIcon == null ? FindImage("Action Point Icon") : actionPointIcon;
            endTurnButton = endTurnButton == null ? FindButton("End Turn") : endTurnButton;
            fallbackEnemyIconSprite = enemyIconImage == null ? null : enemyIconImage.sprite;

            mergeSugarButton?.onClick.AddListener(() => battle.MergeFirstPair(ResourceFamily.Sugar));
            craftButton?.onClick.AddListener(() => battle.CraftFirstAvailableScroll());
            playButton?.onClick.AddListener(battle.PlayFirstScroll);
            endTurnButton?.onClick.AddListener(battle.EndTurn);
            resourceStorage = FindRectTransform("Resource Storage");
            EnsureFeedbackTexts();
            CacheResourceItems();
            RegisterKnownResourceSprites();
            CacheScrollItems();
            CacheEnemyVisuals();
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

            SetText(playerText, $"{battle.Player.Name}\n체력 {battle.Player.Hp}/{battle.Player.MaxHp}  방어 {battle.Player.Guard}\n공격 {battle.Player.Strength}");
            SetText(enemyText, string.Empty);
            SetText(costText, $"{battle.CurrentCost}/{battle.CostCap}");
            SetText(handText, $"손패 {battle.VisibleHandCount}/{battle.MaxHandSize}");
            SetText(resourceText, $"자원 {battle.ActiveResourceCount}/{battle.MaxResources}");
            SetText(logText, battle.LastLog);
            SetText(deckText, $"드로우 {battle.DrawDeckCount}\n버림 {battle.DiscardCount}");
            SetText(drawDeckText, battle.DrawDeckCount.ToString());
            SetText(discardDeckText, battle.DiscardCount.ToString());
            SetText(playerEffectText, "상태");
            RefreshPlayerStatusUi();
            SetText(enemyInfoText, $"{battle.Enemy.Name}\n체력 {battle.Enemy.Hp}/{battle.Enemy.MaxHp}\n약점 {ElementName(battle.Enemy.Weakness)}");
            SetText(enemyEffectText, BuildEnemyEffectText());
            SetText(enemyIntentText, battle.EnemyIntentText);
            SetBarFill(playerHealthFill, battle.Player.Hp, battle.Player.MaxHp);
            SetBarFill(enemyHealthFill, battle.Enemy.Hp, battle.Enemy.MaxHp);
            SetBarFill(enemyLocalHealthFill, battle.Enemy.Hp, battle.Enemy.MaxHp);
            RefreshActionPointIcons();
            RefreshButtons();
            RefreshFeedbackText();
            if (enemyIconImage != null)
            {
                var profileSprite = battle.EnemyPose == EnemyPose.Hit && enemyHitProfileSprite != null
                    ? enemyHitProfileSprite
                    : enemyProfileSprite != null ? enemyProfileSprite : fallbackEnemyIconSprite;

                if (profileSprite != null)
                {
                    enemyIconImage.sprite = profileSprite;
                }

                enemyIconImage.color = Color.white;
                enemyIconImage.preserveAspect = true;
            }

            SetButtonText(endTurnButton, battle.Phase == BattlePhase.StageClear ? "다음" : "턴 종료");
            RefreshEnemyVisuals();
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
                if (item.TryGetComponent<Image>(out var image) && image.sprite != null)
                {
                    MergeResourceVisuals.Register(item.Resource, image.sprite);
                    if (!resourceSprites.ContainsKey(item.Resource.Family))
                    {
                        resourceSprites.Add(item.Resource.Family, image.sprite);
                    }
                }
            }

            resourceTemplate = foundItems.FirstOrDefault();
            foreach (var item in foundItems)
            {
                item.ClearInventorySlot();
            }

            EnsureResourceSlotCapacity(battle == null ? 36 : battle.MaxResources);
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
                if (!MergeResourceVisuals.TryGetSprite(resource, out var sprite))
                {
                    resourceSprites.TryGetValue(resource.Family, out sprite);
                }

                if (hasResource)
                {
                    MergeResourceVisuals.Register(resource, sprite);
                }

                if (resourceStorage != null)
                {
                    item.SetInventorySlot(resourceStorage, GetResourceSlotPosition(i));
                }

                if (item.TryGetComponent<RectTransform>(out var rect))
                {
                    rect.sizeDelta = new Vector2(58f, 58f);
                }

                item.SetInventoryState(resource, sprite, hasResource, i);
            }

            for (var i = slotCount; i < resourceSlots.Count; i++)
            {
                resourceSlots[i]?.ClearInventorySlot();
            }

            if (battle.MergePulse != lastMergePulse)
            {
                lastMergePulse = battle.MergePulse;
                var index = battle.LastMergedResourceIndex;
                if (index >= 0 && index < resourceSlots.Count)
                {
                    resourceSlots[index]?.PlayMergePop();
                }
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
            const int columns = 6;
            const float cellWidth = 78f;
            const float cellHeight = 78f;
            var column = index % columns;
            var row = index / columns;
            if (resourceStorage == null)
            {
                return new Vector2(column * cellWidth, -row * cellHeight);
            }

            var rect = resourceStorage.rect;
            return new Vector2((-rect.width * 0.5f) + 42f + column * cellWidth, (rect.height * 0.5f) - 44f - row * cellHeight);
        }

        private void RegisterKnownResourceSprites()
        {
            RegisterResourceSprite(ResourceFamily.Dairy, 0, dairyStage0Sprite);
            RegisterResourceSprite(ResourceFamily.Dairy, 1, dairyStage1Sprite != null ? dairyStage1Sprite : dairyStage0Sprite);
            RegisterResourceSprite(ResourceFamily.Dairy, 2, dairyStage2Sprite);
            RegisterResourceSprite(ResourceFamily.Dairy, 3, dairyStage3Sprite);
            RegisterResourceSprite(ResourceFamily.Egg, 0, eggStage0Sprite);
            RegisterResourceSprite(ResourceFamily.Egg, 1, eggStage1Sprite != null ? eggStage1Sprite : eggStage0Sprite);
            RegisterResourceSprite(ResourceFamily.Egg, 2, eggStage2Sprite);
            RegisterResourceSprite(ResourceFamily.Egg, 3, eggStage3Sprite != null ? eggStage3Sprite : eggStage2Sprite);
        }

        private static void RegisterResourceSprite(ResourceFamily family, int stage, Sprite sprite)
        {
            if (sprite != null)
            {
                MergeResourceVisuals.Register(new MergeResource(family, stage), sprite);
            }
        }

        private void CacheScrollItems()
        {
            scrollItems = FindObjectsByType<DragScrollItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(item => item.name)
                .ToArray();
        }

        private void CacheEnemyVisuals()
        {
            if (enemyVisualTemplate == null)
            {
                enemyVisualTemplate = FindFirstObjectByType<EnemyBattleSpriteController>(FindObjectsInactive.Include);
            }

            if (enemyVisualTemplate == null)
            {
                return;
            }

            if (enemyVisualTemplate.TryGetComponent<RectTransform>(out var rect))
            {
                enemyVisualBasePosition = rect.anchoredPosition;
            }

            enemyVisuals.Clear();
            enemyVisuals.Add(enemyVisualTemplate);
        }

        private void RefreshEnemyVisuals()
        {
            if (battle == null)
            {
                return;
            }

            if (enemyVisualTemplate == null)
            {
                CacheEnemyVisuals();
            }

            if (enemyVisualTemplate == null)
            {
                return;
            }

            EnsureEnemyVisualCapacity(battle.EnemyCount);
            var spacing = battle.EnemyCount <= 1 ? 0f : 260f;
            var startX = -spacing * (battle.EnemyCount - 1) * 0.5f;
            for (var i = 0; i < enemyVisuals.Count; i++)
            {
                var visual = enemyVisuals[i];
                if (visual == null)
                {
                    continue;
                }

                var active = i < battle.EnemyCount && !battle.GetEnemy(i).IsDead;
                visual.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                visual.Configure(battle, i);
                if (visual.TryGetComponent<RectTransform>(out var rect))
                {
                    rect.anchoredPosition = enemyVisualBasePosition + new Vector2(startX + spacing * i, 0f);
                }
            }
        }

        private void EnsureEnemyVisualCapacity(int requiredCount)
        {
            if (enemyVisualTemplate == null)
            {
                return;
            }

            while (enemyVisuals.Count < requiredCount)
            {
                var clone = Instantiate(enemyVisualTemplate, enemyVisualTemplate.transform.parent);
                clone.name = $"Enemy Character {enemyVisuals.Count + 1}";
                enemyVisuals.Add(clone);
            }
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
                if (item == null || item.IsDragging || item.IsSelectedForPlay)
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

        private static Image FindImage(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target == null ? null : target.GetComponent<Image>();
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

        private void RefreshButtons()
        {
            var canAct = battle != null && !battle.InputLocked && battle.Phase == BattlePhase.PlayerTurn;
            SetButtonInteractable(mergeSugarButton, canAct);
            SetButtonInteractable(craftButton, canAct);
            SetButtonInteractable(playButton, canAct);
            SetButtonInteractable(endTurnButton, battle != null && !battle.InputLocked && (battle.Phase == BattlePhase.PlayerTurn || battle.Phase == BattlePhase.StageClear));
        }

        private static void SetButtonInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void RefreshFeedbackText()
        {
            EnsureFeedbackTexts();
            if (battle == null)
            {
                return;
            }

            if (battle.TurnBannerPulse != lastTurnBannerPulse)
            {
                lastTurnBannerPulse = battle.TurnBannerPulse;
                if (!string.IsNullOrEmpty(battle.TurnBannerText))
                {
                    if (turnBannerRoutine != null)
                    {
                        StopCoroutine(turnBannerRoutine);
                    }

                    turnBannerRoutine = StartCoroutine(SlideTextRoutine(turnBannerText, battle.TurnBannerText, 1.2f, 0f));
                }
            }

            if (battle.EnemyActionPulse != lastEnemyActionPulse)
            {
                lastEnemyActionPulse = battle.EnemyActionPulse;
                if (!string.IsNullOrEmpty(battle.EnemyActionLog))
                {
                    if (enemyActionRoutine != null)
                    {
                        StopCoroutine(enemyActionRoutine);
                    }

                    enemyActionRoutine = StartCoroutine(FadeTextRoutine(enemyActionLogText, battle.EnemyActionLog, 1.35f, 140f));
                }
            }
        }

        private void EnsureFeedbackTexts()
        {
            if (turnBannerText == null)
            {
                turnBannerText = CreateOverlayText("Turn Banner Text", 42, new Color(1f, 0.9f, 0.55f, 1f));
            }

            if (enemyActionLogText == null)
            {
                enemyActionLogText = CreateOverlayText("Enemy Action Log Text", 24, new Color(1f, 0.58f, 0.52f, 1f));
            }
        }

        private Text CreateOverlayText(string objectName, int fontSize, Color color)
        {
            var existing = FindText(objectName);
            if (existing != null)
            {
                return existing;
            }

            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 80f);
            rect.anchoredPosition = new Vector2(0f, 0f);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = new Color(color.r, color.g, color.b, 0f);

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private IEnumerator SlideTextRoutine(Text text, string value, float duration, float y)
        {
            if (text == null || !text.TryGetComponent<RectTransform>(out var rect))
            {
                yield break;
            }

            text.text = value;
            text.transform.SetAsLastSibling();
            var baseColor = text.color;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var x = Mathf.Lerp(-480f, 480f, Mathf.SmoothStep(0f, 1f, t));
                rect.anchoredPosition = new Vector2(x, y);
                var alpha = t < 0.2f ? t / 0.2f : t > 0.78f ? (1f - t) / 0.22f : 1f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
                yield return null;
            }

            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }

        private IEnumerator FadeTextRoutine(Text text, string value, float duration, float y)
        {
            if (text == null || !text.TryGetComponent<RectTransform>(out var rect))
            {
                yield break;
            }

            text.text = value;
            text.transform.SetAsLastSibling();
            rect.anchoredPosition = new Vector2(0f, y);
            var baseColor = text.color;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = t < 0.18f ? t / 0.18f : t > 0.72f ? (1f - t) / 0.28f : 1f;
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
                yield return null;
            }

            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }

        private void RefreshPlayerStatusUi()
        {
            if (battle == null)
            {
                return;
            }

            SetStatusText(
                playerGuardStatusText,
                "방어",
                battle.Player.Guard,
                false,
                new Color(0.42f, 0.74f, 1f, 1f));

            SetStatusText(
                playerStrengthStatusText,
                "공격",
                battle.Player.Strength,
                true,
                battle.Player.Strength >= 0 ? new Color(1f, 0.72f, 0.36f, 1f) : new Color(0.62f, 0.82f, 1f, 1f));
        }

        private static void SetStatusText(Text text, string label, int value, bool signed, Color activeColor)
        {
            if (text == null)
            {
                return;
            }

            var prefix = signed && value > 0 ? "+" : string.Empty;
            text.text = $"{label} {prefix}{value}";
            text.color = value == 0 ? new Color(0.58f, 0.55f, 0.5f, 0.75f) : activeColor;
        }

        private void SetBarFill(Image fill, int current, int max)
        {
            if (fill == null)
            {
                return;
            }

            var ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.fillAmount = 1f;

            if (!fill.TryGetComponent<RectTransform>(out var rect))
            {
                return;
            }

            if (!fillWidths.ContainsKey(rect))
            {
                fillWidths[rect] = rect.sizeDelta.x;
                fillPositions[rect] = rect.anchoredPosition;
            }

            var width = fillWidths[rect];
            var position = fillPositions[rect];
            rect.pivot = new Vector2(0f, rect.pivot.y);
            rect.anchoredPosition = new Vector2(position.x - width * 0.5f, position.y);
            rect.sizeDelta = new Vector2(width * ratio, rect.sizeDelta.y);
        }

        private void RefreshActionPointIcons()
        {
            if (actionPointIcon == null || battle == null)
            {
                return;
            }

            EnsureActionPointIcons(Mathf.Max(1, battle.CostCap));
            for (var i = 0; i < actionPointIcons.Count; i++)
            {
                var icon = actionPointIcons[i];
                if (icon == null)
                {
                    continue;
                }

                var visible = i < battle.CostCap;
                icon.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                icon.color = i < battle.CurrentCost
                    ? new Color(1f, 0.92f, 0.42f, 1f)
                    : new Color(0.24f, 0.24f, 0.24f, 0.55f);
            }
        }

        private void EnsureActionPointIcons(int requiredCount)
        {
            if (actionPointIcons.Count == 0)
            {
                actionPointIcons.Add(actionPointIcon);
            }

            var templateTransform = actionPointIcon.transform as RectTransform;
            var parent = actionPointIcon.transform.parent;
            while (actionPointIcons.Count < requiredCount)
            {
                var clone = Instantiate(actionPointIcon, parent);
                clone.name = $"Action Point Icon {actionPointIcons.Count + 1:00}";
                clone.raycastTarget = false;
                actionPointIcons.Add(clone);
            }

            for (var i = 0; i < actionPointIcons.Count; i++)
            {
                if (actionPointIcons[i] == null || !actionPointIcons[i].TryGetComponent<RectTransform>(out var rect))
                {
                    continue;
                }

                if (templateTransform != null)
                {
                    rect.anchorMin = templateTransform.anchorMin;
                    rect.anchorMax = templateTransform.anchorMax;
                    rect.pivot = templateTransform.pivot;
                    rect.sizeDelta = new Vector2(20f, 20f);
                    rect.anchoredPosition = templateTransform.anchoredPosition + new Vector2(i * 22f, 0f);
                }
            }

            if (costText != null && costText.TryGetComponent<RectTransform>(out var textRect) && templateTransform != null)
            {
                textRect.anchoredPosition = templateTransform.anchoredPosition + new Vector2(requiredCount * 22f + 34f, 0f);
                textRect.sizeDelta = new Vector2(66f, textRect.sizeDelta.y);
            }
        }

        private static string BuildEffectText(CombatantState state)
        {
            var parts = new List<string>();
            if (state.Guard > 0)
            {
                parts.Add($"방어 {state.Guard}");
            }

            if (state.Strength != 0)
            {
                parts.Add($"공격 {(state.Strength > 0 ? "+" : string.Empty)}{state.Strength}");
            }

            return parts.Count == 0 ? "효과 -" : $"효과 {string.Join(" / ", parts)}";
        }

        private string BuildEnemyEffectText()
        {
            var parts = new List<string>();
            if (battle.Enemy.IsBroken)
            {
                parts.Add("브레이크");
            }

            if (battle.Enemy.Strength != 0)
            {
                parts.Add($"공격 {(battle.Enemy.Strength > 0 ? "+" : string.Empty)}{battle.Enemy.Strength}");
            }

            parts.Add($"실드 {battle.Enemy.WeaknessHitsRemaining}/{battle.Enemy.WeaknessHitsRequired}");
            return string.Join(" / ", parts);
        }

        private static string ElementName(ElementType element)
        {
            return element switch
            {
                ElementType.Berry => "딸기",
                ElementType.Chocolate => "초콜릿",
                ElementType.Marshmallow => "마시멜로",
                ElementType.PoppingCandy => "팝핑",
                _ => "-"
            };
        }
    }
}
