using System;
using UnityEngine;

namespace Team3Project.GameSystems
{
    [Serializable]
    public class ScrollCard
    {
        private static int nextId = 1;

        public int Id;
        public ScrollEffectType EffectType;
        public ElementType Element;
        public int Power;
        public int Cost;
        public string DisplayName;
        public int UpgradeLevel;
        public ResourceFamily BaseFamily;
        public int BaseStage;
        public ResourceFamily ToppingFamily;
        public int ToppingStage;

        public bool TargetsEnemy => EffectType is ScrollEffectType.Attack or ScrollEffectType.Debuff;
        public bool IsEmpty => string.IsNullOrEmpty(DisplayName) || DisplayName == "Empty Scroll" || DisplayName == "빈 스크롤";

        public static int CreateId()
        {
            return nextId++;
        }

        public static ScrollCard Craft(MergeResource baseResource, MergeResource? toppingResource, int sourceScrollId = 0)
        {
            var effect = baseResource.Family switch
            {
                ResourceFamily.Sugar => ScrollEffectType.Attack,
                ResourceFamily.Dough => ScrollEffectType.Guard,
                ResourceFamily.Dairy => ScrollEffectType.Buff,
                ResourceFamily.Egg => ScrollEffectType.Debuff,
                _ => ScrollEffectType.Attack
            };

            var element = toppingResource?.Family switch
            {
                ResourceFamily.Berry => ElementType.Berry,
                ResourceFamily.Chocolate => ElementType.Chocolate,
                ResourceFamily.Marshmallow => ElementType.Marshmallow,
                ResourceFamily.PoppingCandy => ElementType.PoppingCandy,
                _ => ElementType.None
            };

            var stage = Math.Max(1, baseResource.Stage);
            var power = effect switch
            {
                ScrollEffectType.Attack => 6 + stage * 5,
                ScrollEffectType.Guard => 5 + stage * 4,
                ScrollEffectType.Buff => 2 + stage * 2,
                ScrollEffectType.Debuff => 2 + stage * 2,
                _ => 1
            };

            if (element != ElementType.None)
            {
                power += 2;
            }

            return new ScrollCard
            {
                Id = sourceScrollId > 0 ? sourceScrollId : CreateId(),
                EffectType = effect,
                Element = element,
                Power = power,
                Cost = Mathf.Clamp(stage, 1, 3),
                DisplayName = BuildName(effect, element, stage),
                UpgradeLevel = 0,
                BaseFamily = baseResource.Family,
                BaseStage = baseResource.Stage,
                ToppingFamily = toppingResource?.Family ?? ResourceFamily.Berry,
                ToppingStage = toppingResource?.Stage ?? 0
            };
        }

        public void UpgradeFromLoop()
        {
            if (IsEmpty)
            {
                return;
            }

            UpgradeLevel++;
            Power += 2;
            DisplayName = $"{BaseName(DisplayName)} +{UpgradeLevel}";
        }

        private static string BuildName(ScrollEffectType effect, ElementType element, int stage)
        {
            var prefix = element == ElementType.None ? string.Empty : $"{ElementName(element)} ";
            return $"{prefix}{EffectName(effect)} {stage}";
        }

        private static string EffectName(ScrollEffectType effect)
        {
            return effect switch
            {
                ScrollEffectType.Attack => "공격",
                ScrollEffectType.Guard => "방어",
                ScrollEffectType.Buff => "강화",
                ScrollEffectType.Debuff => "약화",
                _ => "스크롤"
            };
        }

        private static string ElementName(ElementType element)
        {
            return element switch
            {
                ElementType.Berry => "딸기",
                ElementType.Chocolate => "초콜릿",
                ElementType.Marshmallow => "마시멜로",
                ElementType.PoppingCandy => "팝핑",
                _ => string.Empty
            };
        }

        private static string BaseName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return string.Empty;
            }

            var upgradeIndex = displayName.LastIndexOf(" +", StringComparison.Ordinal);
            return upgradeIndex < 0 ? displayName : displayName.Substring(0, upgradeIndex);
        }
    }
}
