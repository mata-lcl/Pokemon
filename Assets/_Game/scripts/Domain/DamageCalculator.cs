using UnityEngine;

namespace Pokemon.Domain
{
    public sealed class DamageCalculator
    {
        private readonly TypeChartData _typeChart;

        //属性
        public DamageCalculator(TypeChartData typeChart)
        {
            _typeChart = typeChart;
        }

        /// <summary>
        /// MVP版本 无其他条件判断
        /// 可配置性高，通过_typeChart修改平衡性
        /// 没有相应克制表依然可以运行，确保空值安全
        /// </summary>
        /// <param 攻击方="attacker"></param>
        /// <param 防御方="defender"></param>
        /// <param 技能="skill"></param>
        /// <returns></returns>

        //伤害计算
        public int CalculateDamage(MonsterRuntime attacker, MonsterRuntime defender, SkillData skill)
        {
           
            //基础伤害计算 = 技能威力 + 攻击方攻击力 - 防御方防御力 * 0.5
            float baseDamage = skill.Power + attacker.Attack - defender.Defense * 1.5f;
            if (baseDamage < 1f) baseDamage = 1f;

            //属性加成系数 如果为同属性则技能威力*1.5 检查属性相克表，如果不存在则默认威力*1
            float stab = attacker.PrimaryType == skill.Type ? 1.5f : 1f;
            float typeMultiplier = _typeChart != null
                ? _typeChart.GetMultiplier(skill.Type, defender.PrimaryType)
                : 1f;

            float random = Random.Range(0.85f, 1f); //伤害修正
            float total = baseDamage * stab * typeMultiplier * random;

            return Mathf.Max(1, Mathf.FloorToInt(total));       //向下取整。伤害取整，返回int类型整数
        }

        /// <summary>
        /// 检查技能是否命中
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public bool CheckHit(SkillData skill)
        {
            return Random.value <= skill.Accuracy;
        }
    }
}