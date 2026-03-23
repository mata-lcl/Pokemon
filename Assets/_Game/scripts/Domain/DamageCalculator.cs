using Pokemon.Application;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pokemon.Domain
{
    // 定义伤害计算结果
    public struct DamageResult
    {
        public int FinalDamage;
        public float TypeMultiplier;
    }

    public sealed class DamageCalculator
    {
        private readonly TypeChartData _typeChart;

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
        public DamageResult CalculateDamage(MonsterRuntime attacker, MonsterRuntime defender, SkillData skill, List<TurnStep> steps = null)
        {

            // 选择攻击和防御属性
            int atk = (skill.Category == SkillCategory.Special)? attacker.SpecialAttack : attacker.Attack;
            int def = (skill.Category == SkillCategory.Special)? defender.SpecialDefense : defender.Defense;

            //基础伤害计算 = 技能威力 + 攻击方攻击力 - 防御方防御力 * 2
            //如果是复刻正作，公式会更复杂，这里保留简易版但修正了属性选择
            float baseDamage = skill.Power + atk - def * 2f;
            if (baseDamage < 1f) baseDamage = 1f;

            // 2. 完善 STAB (本系加成)：判断第一或第二属性
            float stab = 1f;
            if (attacker.Species.PrimaryType == skill.Type ||
                (attacker.Species.SecondaryType != PokemonType.None && attacker.Species.SecondaryType == skill.Type))
            {
                stab = 1.5f;
            }

            // 3. 完善属性相克 (计算防御方所有属性)
            float typeMultiplier = 1f;
            if (_typeChart != null)
            {
                // 乘上第一属性倍率
                typeMultiplier *= _typeChart.GetMultiplier(skill.Type, defender.Species.PrimaryType);
                // 如果有第二属性，再乘上去
                if (defender.Species.SecondaryType != PokemonType.None)
                {
                    typeMultiplier *= _typeChart.GetMultiplier(skill.Type, defender.Species.SecondaryType);
                }
            }

            //4. 【关键】整合特性系统(Ability System)
            float abilityMod = 1.0f;
            if (attacker.ActiveAbility != null)
            {
                // 调用特性基类中的虚方法，实现一特性一脚本的解耦
                abilityMod = attacker.ActiveAbility.GetDamageMultiplier(attacker, defender, skill, steps);
            }

            // 5. 最终计算
            float random = Random.Range(0.85f, 1f);
            float total = baseDamage * stab * typeMultiplier * random * abilityMod;

            // --- 调试日志：这行代码会解决你“无法知道数值变化”的问题 ---
            //Debug.Log($"<color=cyan>[伤害计算]</color>技能:{skill.DisplayName} | 基础:{baseDmg} | 属性:{typeMod} | 特性:{abilityMod} | 随机:{rand:F2} | 最终:{finalDamage}");

            return new DamageResult
            {
                FinalDamage = Mathf.Max(1, Mathf.FloorToInt(total)), //向下取整。伤害取整，返回int类型整数
                TypeMultiplier = typeMultiplier
            };
        }

        public bool CheckHit(SkillData skill)
        {
            return Random.value <= skill.Accuracy;
        }
    }
}