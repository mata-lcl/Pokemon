using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "Species_",menuName = "Pokemon/Species Data")]

    public class PokemonSpeciesData : ScriptableObject
    {
        [Header("身份信息")]
        public int ID;
        public string DisplayName;

        [Header("类型")]
        public PokemonType PrimaryType = PokemonType.Normal;
        public PokemonType SecondaryType = PokemonType.None;

        [Header("种族值 (不可更改)")]
        [Tooltip("血量")] public int BaseHP = 50;
        [Tooltip("攻击")] public int BaseAttack = 10;
        [Tooltip("防御")] public int BaseDefense = 10;
        [Tooltip("速度")] public int BaseSpeed = 10;
        [Tooltip("特攻")] public int BaseSpAttack = 50;  // 【新增】基础特攻
        [Tooltip("特防")] public int BaseSpDefense = 50; // 【新增】基础特防

        // --- V0.1新增：战斗结算奖励 ---
        [Header("击败该宝可梦提供的奖励")]
        [Tooltip("基础经验产出")]
        public int BaseExpYield = 50;

        [Tooltip("击败后给胜利方加的各项学习力(EVs)")]
        public int EvYieldHP = 0;
        public int EvYieldAttack = 0;
        public int EvYieldDefense = 0;
        public int EvYieldSpeed = 0;
        public int EvYieldSpAttack = 0;  // 【新增】击败后提供的特攻EV
        public int EvYieldSpDefense = 0; // 【新增】击败后提供的特防EV
        // -------------------------

        [Header("特性列表")]
        public List<AbilityData> Abilities = new List<AbilityData>();

        [Header("学习技能")]
        public List<SkillData> InitialSkills = new List<SkillData>();

        //新增
        [Header("精灵图片")]
        public Sprite BattleSprite;
    }
}

