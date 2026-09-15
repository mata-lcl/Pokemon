using NUnit.Framework;
using Pokemon.Domain;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Pokemon.Tests
{
    public sealed class BattleCalculationTests
    {
        /// <summary>
        /// 验证本系加成和双属性克制倍率共同提高最终伤害。
        /// </summary>
        [Test]
        public void SameTypeAndDualWeaknessIncreaseDamage()
        {
            Random.State previousRandomState = Random.state;
            PokemonSpeciesData attackerSpecies = CreateSpecies(
                "attacker",
                PokemonType.Fire,
                PokemonType.None);
            PokemonSpeciesData defenderSpecies = CreateSpecies(
                "defender",
                PokemonType.Grass,
                PokemonType.Water);
            SkillData fireSkill = CreateSkill("fire", PokemonType.Fire, 40);
            SkillData normalSkill = CreateSkill("normal", PokemonType.Normal, 40);
            TypeChartData typeChart = ScriptableObject.CreateInstance<TypeChartData>();
            typeChart.Entries.Add(new TypeChartData.TypeModifierEntry
            {
                AttackType = PokemonType.Fire,
                DefenseType = PokemonType.Grass,
                Multiplier = 2f
            });
            typeChart.Entries.Add(new TypeChartData.TypeModifierEntry
            {
                AttackType = PokemonType.Fire,
                DefenseType = PokemonType.Water,
                Multiplier = 2f
            });

            try
            {
                var attacker = new MonsterRuntime(attackerSpecies, 20);
                var defender = new MonsterRuntime(defenderSpecies, 20);
                var calculator = new DamageCalculator(typeChart);

                Random.InitState(12345);
                DamageResult fireDamage = calculator.CalculateDamage(
                    attacker,
                    defender,
                    fireSkill);
                Random.InitState(12345);
                DamageResult normalDamage = calculator.CalculateDamage(
                    attacker,
                    defender,
                    normalSkill);

                Assert.That(fireDamage.TypeMultiplier, Is.EqualTo(4f));
                Assert.That(fireDamage.FinalDamage, Is.GreaterThan(normalDamage.FinalDamage));
            }
            finally
            {
                Random.state = previousRandomState;
                Object.DestroyImmediate(typeChart);
                Object.DestroyImmediate(fireSkill);
                Object.DestroyImmediate(normalSkill);
                Object.DestroyImmediate(attackerSpecies);
                Object.DestroyImmediate(defenderSpecies);
            }
        }

        /// <summary>
        /// 验证攻击力远低于防御力时最终伤害仍保持最低一点。
        /// </summary>
        [Test]
        public void DamageNeverFallsBelowOne()
        {
            PokemonSpeciesData attackerSpecies = CreateSpecies(
                "weak-attacker",
                PokemonType.Normal,
                PokemonType.None);
            PokemonSpeciesData defenderSpecies = CreateSpecies(
                "strong-defender",
                PokemonType.Normal,
                PokemonType.None);
            attackerSpecies.BaseAttack = 1;
            defenderSpecies.BaseDefense = 255;
            SkillData skill = CreateSkill("weak-skill", PokemonType.Normal, 0);

            try
            {
                DamageResult result = new DamageCalculator(null).CalculateDamage(
                    new MonsterRuntime(attackerSpecies, 5),
                    new MonsterRuntime(defenderSpecies, 100),
                    skill);

                Assert.That(result.FinalDamage, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(attackerSpecies);
                Object.DestroyImmediate(defenderSpecies);
            }
        }

        /// <summary>
        /// 创建伤害计算测试使用的精灵种类。
        /// </summary>
        /// <param name="assetName">测试资源名称。</param>
        /// <param name="primaryType">第一属性。</param>
        /// <param name="secondaryType">第二属性。</param>
        private static PokemonSpeciesData CreateSpecies(
            string assetName,
            PokemonType primaryType,
            PokemonType secondaryType)
        {
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            species.name = assetName;
            species.PrimaryType = primaryType;
            species.SecondaryType = secondaryType;
            species.BaseAttack = 50;
            species.BaseDefense = 50;
            return species;
        }

        /// <summary>
        /// 创建伤害计算测试使用的物理技能。
        /// </summary>
        /// <param name="assetName">测试资源名称。</param>
        /// <param name="type">技能属性。</param>
        /// <param name="power">技能威力。</param>
        private static SkillData CreateSkill(
            string assetName,
            PokemonType type,
            int power)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.name = assetName;
            skill.Type = type;
            skill.Category = SkillCategory.Physical;
            skill.Power = power;
            return skill;
        }
    }
}
