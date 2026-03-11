using System.Collections.Generic;

namespace Pokemon.Domain
{
    /// <summary>
    /// 密封类，不能被继承
    /// </summary>
    public sealed class MonsterRuntime
    {
        //在构造时赋值，之后不可更改
        public PokemonSpeciesData Species { get; }
        public int CurrentHP { get; private set; }  //外部只能读取，内部才可以修改
        //私有的只读字典，用于存放技能以及技能点数，键为SkillData，值为PP值
        private readonly Dictionary<SkillData, int> _currentPP = new Dictionary<SkillData, int>();

        #region 每次访问动态计算数值，返回攻击，防御，速度，属性以及是否濒死
        public int Attack => Species.BaseAttack;
        public int Defense => Species.BaseDefense;
        public int Speed => Species.BaseSpeed;
        public ElementType PrimaryType => Species.PrimaryType;
        public bool IsFainted => CurrentHP <= 0;
        #endregion
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param 物种参数="species"></param>
        public MonsterRuntime(PokemonSpeciesData species)
        {
            Species = species;
            CurrentHP = species.BaseHP;

            //对技能列表遍历，如果字典技能不为空，且字典中存在技能列表中有的技能则加入
            for (int i = 0; i < species.InitialSkills.Count; i++)
            {
                SkillData skill = species.InitialSkills[i];
                if (skill != null && !_currentPP.ContainsKey(skill))    //
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