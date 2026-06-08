using System;
using UnityEngine;

namespace Team3Project.GameSystems
{
    public enum BattlePhase
    {
        PlayerTurn,
        EnemyTurn,
        StageClear,
        GameOver
    }

    public enum ResourceFamily
    {
        Sugar,
        Dough,
        Dairy,
        Egg,
        Berry,
        Chocolate,
        Marshmallow,
        PoppingCandy
    }

    public enum ResourceRole
    {
        Base,
        Topping
    }

    public enum ScrollEffectType
    {
        Attack,
        Guard,
        Buff,
        Debuff
    }

    public enum ElementType
    {
        None,
        Berry,
        Chocolate,
        Marshmallow,
        PoppingCandy
    }

    [Serializable]
    public struct MergeResource
    {
        public ResourceFamily Family;
        public int Stage;
        public bool IsEmpty;

        public static MergeResource Empty => new()
        {
            Family = ResourceFamily.Sugar,
            Stage = 0,
            IsEmpty = true
        };

        public MergeResource(ResourceFamily family, int stage)
        {
            Family = family;
            Stage = Mathf.Clamp(stage, 0, 3);
            IsEmpty = false;
        }

        public bool CanUse => !IsEmpty;

        public ResourceRole Role => Family is ResourceFamily.Sugar or ResourceFamily.Dough or ResourceFamily.Dairy or ResourceFamily.Egg
            ? ResourceRole.Base
            : ResourceRole.Topping;

        public string DisplayName => IsEmpty ? "Empty" : Family switch
        {
            ResourceFamily.Sugar => Stage switch
            {
                0 => "Cane",
                1 => "Sugar",
                2 => "Syrup",
                _ => "Caramel"
            },
            ResourceFamily.Dough => Stage switch
            {
                0 => "Wheat",
                1 => "Flour",
                2 => "Dough",
                _ => "Bread"
            },
            ResourceFamily.Dairy => Stage switch
            {
                0 => "Milk Drop",
                1 => "Milk",
                2 => "Cream",
                _ => "Butter"
            },
            ResourceFamily.Egg => Stage switch
            {
                0 => "Egg",
                1 => "Peeled Egg",
                2 => "Meringue",
                _ => "Custard"
            },
            _ => $"{Family} Lv.{Stage}"
        };
    }
}
