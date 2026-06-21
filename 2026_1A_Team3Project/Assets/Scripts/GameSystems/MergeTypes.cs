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

    public enum EnemyPose
    {
        Idle,
        Attack,
        Hit
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

        public string DisplayName => IsEmpty ? string.Empty : Family switch
        {
            ResourceFamily.Sugar => Stage switch
            {
                0 => "사탕수수",
                1 => "설탕",
                2 => "시럽",
                _ => "카라멜"
            },
            ResourceFamily.Dough => Stage switch
            {
                0 => "밀",
                1 => "밀가루",
                2 => "반죽",
                _ => "빵"
            },
            ResourceFamily.Dairy => Stage switch
            {
                0 => "우유",
                1 => "우유",
                2 => "생크림",
                _ => "버터"
            },
            ResourceFamily.Egg => Stage switch
            {
                0 => "날계란",
                1 => "깐 계란",
                2 => "머랭",
                _ => "커스터드"
            },
            ResourceFamily.Berry => Stage <= 1 ? "베리" : "딸기",
            ResourceFamily.Chocolate => Stage <= 1 ? "초콜릿 칩" : "초콜릿 청크",
            ResourceFamily.Marshmallow => Stage <= 1 ? "마시멜로" : "꽈배기 마시멜로",
            ResourceFamily.PoppingCandy => Stage <= 1 ? "팝핑 캔디" : "별 팝핑 캔디",
            _ => string.Empty
        };
    }
}
