using System.Collections.Generic;

namespace Pokemon.Domain
{
    public sealed class MonsterRuntime
    {
        public PokemonSpeciesData Species { get; }
        public int CurrentHP { get; private set; }

        private readonly Dictionary<SkillData, int> _currentPP = new Dictionary<SkillData, int>();

        public int Attack => Species.BaseAttack;
        public int Defense => Species.BaseDefense;
        public int Speed => Species.BaseSpeed;
        public ElementType PrimaryType => Species.PrimaryType;
        public bool IsFainted => CurrentHP <= 0;

        public MonsterRuntime(PokemonSpeciesData species)
        {
            Species = species;
            CurrentHP = species.BaseHP;

            for (int i = 0; i < species.InitialSkills.Count; i++)
            {
                SkillData skill = species.InitialSkills[i];
                if (skill != null && !_currentPP.ContainsKey(skill))
                {
                    _currentPP.Add(skill, skill.MaxPP);
                }
            }
        }

        public IReadOnlyDictionary<SkillData, int> GetSkillPP() => _currentPP;

        public bool CanUseSkill(SkillData skill)
        {
            return skill != null && _currentPP.ContainsKey(skill) && _currentPP[skill] > 0;
        }

        public bool TryConsumePP(SkillData skill)
        {
            if (!CanUseSkill(skill))
                return false;

            _currentPP[skill]--;
            return true;
        }

        public void ApplyDamage(int amount)
        {
            if (amount < 0) amount = 0;
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }
    }
}