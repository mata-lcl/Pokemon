using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pokemon.Domain
{
    /// <summary>
    /// 统计数值改变时的事件参数
    /// </summary>
    public struct StatChangedEventArgs
    {
        public int NewValue;
        public int MaxValue;
    }

    /// <summary>
    /// 等级提升时的事件参数
    /// </summary>
    public struct LevelUpEventArgs
    {
        public int NewLevel;
        public int LevelsGained;
    }

    public class MonsterRuntime
    {
        // ==========================================
        // 1. 观察者事件 (Observer Events)
        // ==========================================
        public event Action<StatChangedEventArgs> OnHPChanged;      // HP变化时触发
        public event Action<LevelUpEventArgs> OnLeveledUp;         // 升级时触发
        public event Action OnStatsRecalculated;                   // 数值面板更新时触发（EV/IV改变）

        // ==========================================
        // 2. 基础数据字段
        // ==========================================
        public PokemonSpeciesData Species { get; private set; }
        public int Level { get; private set; }

        public bool HasTriggeredCrisisAbility { get; set; } // 用于标记是否已触发过危机特性，防止重复弹窗

        private int _currentHP;
        public int CurrentHP
        {
            get => _currentHP;
            private set
            {
                _currentHP = Mathf.Clamp(value, 0, MaxHP);
                // 触发观察者：HP变了
                OnHPChanged?.Invoke(new StatChangedEventArgs { NewValue = _currentHP, MaxValue = MaxHP });
            }
        }

        // --- 特性与道具 ---
        public AbilityData ActiveAbility { get; private set; } // 修改为单数，通常一只宝可梦只有一个活动特性
        public ItemData HeldItem { get; set; }

        // --- 状态与技能 ---
        public StatusCondition CurrentStatus { get; private set; }
        public Dictionary<SkillData, int> CurrentPP { get; private set; }
        public bool IsFainted => CurrentHP <= 0;
        public int CurrentExp { get; private set; }

        // ==========================================
        // 3. 个体值 (IVs) 与 努力值 (EVs) 字段
        // 使用字段而非属性，以便在 AddEVs 中使用 ref 关键字
        // ==========================================

        // 个体值 (0-31)
        public int IvHP { get; private set; }
        public int IvAttack { get; private set; }
        public int IvDefense { get; private set; }
        public int IvSpeed { get; private set; }
        public int IvSpecialAttack { get; private set; }
        public int IvSpecialDefense { get; private set; }

        // 努力值 (0-255)
        private int _evHP;
        private int _evAttack;
        private int _evDefense;
        private int _evSpeed;
        private int _evSpecialAttack;
        private int _evSpecialDefense;

        public int EvHP => _evHP;
        public int EvAttack => _evAttack;
        public int EvDefense => _evDefense;
        public int EvSpeed => _evSpeed;
        public int EvSpecialAttack => _evSpecialAttack;
        public int EvSpecialDefense => _evSpecialDefense;

        // ==========================================
        // 4. 最终面板数值 (基于公式动态计算)
        // ==========================================
        public int MaxHP => CalculateHpStat();
        public int Attack => CalculateStandardStat(Species.BaseAttack, IvAttack, _evAttack);
        public int Defense => CalculateStandardStat(Species.BaseDefense, IvDefense, _evDefense);
        public int Speed => CalculateStandardStat(Species.BaseSpeed, IvSpeed, _evSpeed);
        public int SpecialAttack => CalculateStandardStat(Species.BaseSpAttack, IvSpecialAttack, _evSpecialAttack);
        public int SpecialDefense => CalculateStandardStat(Species.BaseSpDefense, IvSpecialDefense, _evSpecialDefense);

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonsterRuntime(PokemonSpeciesData species, int level)
        {
            Species = species;
            Level = Mathf.Clamp(level, 1, 100);
            CurrentStatus = StatusCondition.None;

            // 初始化经验 (立方曲线)
            CurrentExp = Level * Level * Level;

            // 1. 生成天赋 (IVs)
            GenerateIVs();

            // 2. 初始化努力值 (EVs)
            ResetEVs();

            // 3. 初始化特性 (从特性池中随机选一个)
            if (species.Abilities != null && species.Abilities.Count > 0)
            {
                ActiveAbility = species.Abilities[Random.Range(0, species.Abilities.Count)];
            }

            // 4. 计算并设定初始血量
            _currentHP = MaxHP;

            // 5. 初始化技能
            CurrentPP = new Dictionary<SkillData, int>();
            foreach (var skill in species.InitialSkills)
            {
                if (skill != null) CurrentPP[skill] = skill.MaxPP;
            }
        }

        /// <summary>
        /// 使用存档中的完整状态恢复当前精灵。
        /// </summary>
        /// <param name="level">存档中的等级。</param>
        /// <param name="currentExp">存档中的当前经验值。</param>
        /// <param name="currentHP">存档中的当前生命值。</param>
        /// <param name="status">存档中的异常状态。</param>
        /// <param name="ivHP">生命个体值。</param>
        /// <param name="ivAttack">攻击个体值。</param>
        /// <param name="ivDefense">防御个体值。</param>
        /// <param name="ivSpeed">速度个体值。</param>
        /// <param name="ivSpecialAttack">特攻个体值。</param>
        /// <param name="ivSpecialDefense">特防个体值。</param>
        /// <param name="evHP">生命努力值。</param>
        /// <param name="evAttack">攻击努力值。</param>
        /// <param name="evDefense">防御努力值。</param>
        /// <param name="evSpeed">速度努力值。</param>
        /// <param name="evSpecialAttack">特攻努力值。</param>
        /// <param name="evSpecialDefense">特防努力值。</param>
        /// <param name="ability">存档中的当前特性。</param>
        /// <param name="heldItem">存档中的携带道具。</param>
        /// <param name="skillPP">存档中的技能及剩余 PP。</param>
        public void RestoreState(
            int level,
            int currentExp,
            int currentHP,
            StatusCondition status,
            int ivHP,
            int ivAttack,
            int ivDefense,
            int ivSpeed,
            int ivSpecialAttack,
            int ivSpecialDefense,
            int evHP,
            int evAttack,
            int evDefense,
            int evSpeed,
            int evSpecialAttack,
            int evSpecialDefense,
            AbilityData ability,
            ItemData heldItem,
            IReadOnlyDictionary<SkillData, int> skillPP)
        {
            Level = level;
            CurrentExp = currentExp;
            CurrentStatus = status;
            IvHP = ivHP;
            IvAttack = ivAttack;
            IvDefense = ivDefense;
            IvSpeed = ivSpeed;
            IvSpecialAttack = ivSpecialAttack;
            IvSpecialDefense = ivSpecialDefense;
            _evHP = evHP;
            _evAttack = evAttack;
            _evDefense = evDefense;
            _evSpeed = evSpeed;
            _evSpecialAttack = evSpecialAttack;
            _evSpecialDefense = evSpecialDefense;
            ActiveAbility = ability;
            HeldItem = heldItem;
            CurrentPP = new Dictionary<SkillData, int>(skillPP);
            CurrentHP = currentHP;
        }

        private void GenerateIVs()
        {
            IvHP = Random.Range(0, 32);
            IvAttack = Random.Range(0, 32);
            IvDefense = Random.Range(0, 32);
            IvSpeed = Random.Range(0, 32);
            IvSpecialAttack = Random.Range(0, 32);
            IvSpecialDefense = Random.Range(0, 32);
        }

        private void ResetEVs()
        {
            _evHP = 0; _evAttack = 0; _evDefense = 0; _evSpeed = 0; _evSpecialAttack = 0; _evSpecialDefense = 0;
        }

        /// <summary>
        /// 增加努力值。利用观察者模式，增加后会自动通知面板刷新。
        /// </summary>
        public void AddEVs(int hp, int atk, int def, int spd, int spatk, int spdef)
        {
            const int MaxSingleEV = 252;
            const int MaxTotalEV = 510;

            int currentTotal = _evHP + _evAttack + _evDefense + _evSpeed + _evSpecialAttack + _evSpecialDefense;
            int remainingTotal = MaxTotalEV - currentTotal;

            if (remainingTotal <= 0) return;

            // 内部辅助方法：处理字段的引用增加
            void ApplyEv(ref int evField, int amount)
            {
                int actualAdd = Mathf.Min(amount, MaxSingleEV - evField);
                actualAdd = Mathf.Min(actualAdd, remainingTotal);
                evField += actualAdd;
                remainingTotal -= actualAdd;
            }

            ApplyEv(ref _evHP, hp);
            ApplyEv(ref _evAttack, atk);
            ApplyEv(ref _evDefense, def);
            ApplyEv(ref _evSpeed, spd);
            ApplyEv(ref _evSpecialAttack, spatk);
            ApplyEv(ref _evSpecialDefense, spdef);

            // 触发观察者：面板数值逻辑上已经改变
            OnStatsRecalculated?.Invoke();
        }

        // ==========================================
        // 5. 战斗及成长逻辑
        // ==========================================

        public bool AddExp(int amount)
        {
            if (Level >= 100) return false;

            CurrentExp += amount;
            int levelsGained = 0;

            while (Level < 100 && CurrentExp >= GetExpToNextLevel())
            {
                int oldMaxHp = MaxHP;
                Level++;
                levelsGained++;

                // 升级补血 (按增加的HP上限补回)
                int hpIncrease = MaxHP - oldMaxHp;
                Heal(hpIncrease);
            }

            if (levelsGained > 0)
            {
                OnLeveledUp?.Invoke(new LevelUpEventArgs { NewLevel = Level, LevelsGained = levelsGained });
                OnStatsRecalculated?.Invoke();
                return true;
            }
            return false;
        }

        private int GetExpToNextLevel() => (Level + 1) * (Level + 1) * (Level + 1);

        public void Heal(int amount) => CurrentHP += amount;

        public void ApplyDamage(int amount) => CurrentHP -= amount;

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

        // ==========================================
        // 6. 核心公式计算
        // ==========================================
        private int CalculateHpStat()
        {
            int baseCalc = (Species.BaseHP * 2) + IvHP + (_evHP / 4);
            return (baseCalc * Level / 100) + 10 + Level;
        }

        private int CalculateStandardStat(int baseStat, int iv, int ev)
        {
            int baseCalc = (baseStat * 2) + iv + (ev / 4);
            return (baseCalc * Level / 100) + 5;
        }

        // 在 MonsterRuntime.cs 内部
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugSetHP(int value)
        {
            this.CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        }
#endif
    }
}
