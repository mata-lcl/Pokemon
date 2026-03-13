using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pokemon.Domain
{
    public class MonsterRuntime
    {
        public PokemonSpeciesData Species { get; private set; }
        public int Level { get; private set; }
        public int CurrentHP { get; private set; }

        // --- 异常状态 ---
        public StatusCondition CurrentStatus { get; private set; }
        public Dictionary<SkillData, int> CurrentPP { get; private set; }
        public bool IsFainted => CurrentHP <= 0;

        // --- 个体值 (IVs: 0~31) ---
        public int IvHP { get; private set; }
        public int IvAttack { get; private set; }
        public int IvDefense { get; private set; }
        public int IvSpeed { get; private set; }

        // --- 学习力/努力值 (EVs: 单项最高255，总和最高510) ---
        public int EvHP { get; private set; }
        public int EvAttack { get; private set; }
        public int EvDefense { get; private set; }
        public int EvSpeed { get; private set; }

        // --- 最终能力值 (动态计算) ---
        // 注意：这里调用的 Species 属性名必须和 PokemonSpeciesData.cs 里的变量名完全一致
        public int MaxHP => CalculateHpStat();
        public int Attack => CalculateStandardStat(Species.BaseAttack, IvAttack, EvAttack); // 已修正为 BaseAttack
        public int Defense => CalculateStandardStat(Species.BaseDefense, IvDefense, EvDefense); // 已修正为 BaseDefense
        public int Speed => CalculateStandardStat(Species.BaseSpeed, IvSpeed, EvSpeed); // 已修正为 BaseSpeed

        public ElementType PrimaryType => Species.PrimaryType;

        /// <summary>
        /// 构造函数，生成一个新的怪物实例
        /// </summary>
        public MonsterRuntime(PokemonSpeciesData species, int level)
        {
            Species = species;
            Level = Mathf.Clamp(level, 1, 100);
            CurrentStatus = StatusCondition.None;

            // 1. 生成随机个体值 (天赋)
            GenerateIVs();
            Debug.LogErrorFormat(species.DisplayName+"个体值为："+IvHP+","+ IvAttack + "," + IvDefense + "," + IvSpeed);

            // 2. 初始努力值为0
            ResetEVs();

            // 3. 计算生成后的最大血量，并回满血
            CurrentHP = MaxHP;
            Debug.LogErrorFormat(species.DisplayName + "具体值为：" + MaxHP + "," + Attack + "," + Defense + "," + Speed);


            // 4. 初始化技能PP
            CurrentPP = new Dictionary<SkillData, int>();
            foreach (var skill in species.InitialSkills)
            {
                if (skill != null)
                {
                    CurrentPP[skill] = skill.MaxPP;
                }
            }
        }

        // 随机个体值
        private void GenerateIVs()
        {
            IvHP = Random.Range(0, 32);
            IvAttack = Random.Range(0, 32);
            IvDefense = Random.Range(0, 32);
            IvSpeed = Random.Range(0, 32);
        }

        private void ResetEVs()
        {
            EvHP = 0; EvAttack = 0; EvDefense = 0; EvSpeed = 0;
        }

        // 增加努力值 (带上限检查)
        public void AddEVs(int hp, int atk, int def, int spd)
        {
            int GetTotalEVs() => EvHP + EvAttack + EvDefense + EvSpeed;

            // 这里使用 while 或简单的判断来确保总和不超过 510
            EvHP = Mathf.Min(255, EvHP + hp);
            EvAttack = Mathf.Min(255, EvAttack + atk);
            EvDefense = Mathf.Min(255, EvDefense + def);
            EvSpeed = Mathf.Min(255, EvSpeed + spd);

            // 如果总和溢出，简单地按比例回退（实际游戏中会有更复杂的取舍逻辑）
            int total = GetTotalEVs();
            if (total > 510)
            {
                // 暂时简单的防御性处理：禁止增加
                EvHP -= hp; EvAttack -= atk; EvDefense -= def; EvSpeed -= spd;
            }
        }

        // --- 经典能力值计算公式 ---

        // HP公式: ((种族值×2 + 个体值 + 努力值÷4) × 等级 ÷ 100) + 10 + 等级
        private int CalculateHpStat()
        {
            int baseCalc = (Species.BaseHP * 2) + IvHP + (EvHP / 4);
            return (baseCalc * Level / 100) + 10 + Level;
        }

        // 其他属性公式: ((种族值×2 + 个体值 + 努力值÷4) × 等级 ÷ 100) + 5
        private int CalculateStandardStat(int baseStat, int iv, int ev)
        {
            int baseCalc = (baseStat * 2) + iv + (ev / 4);
            return (baseCalc * Level / 100) + 5;
        }

        public void ApplyDamage(int amount)
        {
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public bool TryConsumePP(SkillData skill)
        {
            if (CurrentPP.TryGetValue(skill, out int pp) && pp > 0)
            {
                CurrentPP[skill] = pp - 1;
                return true;
            }
            return false;
        }

        public bool TryApplyStatus(StatusCondition status)
        {
            if (CurrentStatus != StatusCondition.None || IsFainted || status == StatusCondition.None)
                return false;
            CurrentStatus = status;
            return true;
        }
    }
}