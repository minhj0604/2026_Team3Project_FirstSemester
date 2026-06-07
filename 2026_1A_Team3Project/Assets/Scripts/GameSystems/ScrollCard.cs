using System;
using UnityEngine;

namespace Team3Project.GameSystems
{
    [Serializable]
    public class ScrollCard
    {
        public ScrollEffectType EffectType;
        public ElementType Element;
        public int Power;
        public int Cost;
        public string DisplayName;

        public bool TargetsEnemy => EffectType is ScrollEffectType.Attack or ScrollEffectType.Debuff;

        public static ScrollCard Craft(MergeResource baseResource, MergeResource? toppingResource)
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
                EffectType = effect,
                Element = element,
                Power = power,
                Cost = Mathf.Clamp(stage, 1, 3),
                DisplayName = BuildName(effect, element, stage)
            };
        }

        private static string BuildName(ScrollEffectType effect, ElementType element, int stage)
        {
            var prefix = element == ElementType.None ? string.Empty : $"{element} ";
            return $"{prefix}{effect} Lv.{stage}";
        }
    }
}
