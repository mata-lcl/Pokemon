using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "TypeChart_", menuName = "Pokemon/Type Chart Data")]
    public class TypeChartData : ScriptableObject
    {
        [System.Serializable]
        public class TypeModifierEntry
        {
            public ElementType AttackType = ElementType.Normal;
            public ElementType DefenseType = ElementType.Normal;
            public float Multiplier = 1f;
        }

        public List<TypeModifierEntry> Entries = new List<TypeModifierEntry>();

        public float GetMultiplier(ElementType attackType, ElementType defenseType)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry.AttackType == attackType && entry.DefenseType == defenseType)
                {
                    return entry.Multiplier;
                }
            }
            return 1f;
        }
    }
}