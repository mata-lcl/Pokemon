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
        public ElementType PrimaryType = ElementType.Normal;

        [Header("属性")]
        public int BaseHP = 50;
        public int BaseAttack = 10;
        public int BaseDefense = 10;
        public int BaseSpeed = 10;

        [Header("学习技能")]
        public List<SkillData> InitialSkills = new List<SkillData>();

        //新增
        [Header("精灵图片")]
        public Sprite BattleSprite;
    }
}

