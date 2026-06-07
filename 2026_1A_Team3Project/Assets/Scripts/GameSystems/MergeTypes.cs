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

        public MergeResource(ResourceFamily family, int stage)
        {
            Family = family;
            Stage = Mathf.Clamp(stage, 0, 3);
        }

        public ResourceRole Role => Family is ResourceFamily.Sugar or ResourceFamily.Dough or ResourceFamily.Dairy or ResourceFamily.Egg
            ? ResourceRole.Base
            : ResourceRole.Topping;
    }
}
