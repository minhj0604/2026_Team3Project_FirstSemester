using System;
using UnityEngine;

namespace Team3Project.GameSystems
{
    [Serializable]
    public class CombatantState
    {
        public string Name;
        public int MaxHp = 40;
        public int Hp = 40;
        public int Guard;
        public int Strength;
        public int WeaknessHitsRequired = 3;
        public int WeaknessHitsRemaining = 3;
        public ElementType Weakness = ElementType.Berry;
        public bool IsBroken;

        public bool IsDead => Hp <= 0;

        public void Reset(string displayName, int maxHp, ElementType weakness, int shieldCount)
        {
            Name = displayName;
            MaxHp = maxHp;
            Hp = maxHp;
            Guard = 0;
            Strength = 0;
            Weakness = weakness;
            WeaknessHitsRequired = Mathf.Max(0, shieldCount);
            WeaknessHitsRemaining = WeaknessHitsRequired;
            IsBroken = false;
        }

        public int TakeDamage(int amount)
        {
            var blocked = Mathf.Min(Guard, amount);
            Guard -= blocked;
            var finalDamage = Mathf.Max(0, amount - blocked);
            Hp = Mathf.Max(0, Hp - finalDamage);
            return finalDamage;
        }
    }
}
