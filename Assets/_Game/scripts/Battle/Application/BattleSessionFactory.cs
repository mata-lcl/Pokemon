using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    public sealed class BattleSessionFactory
    {
        /// <summary>
        /// 根据战斗配置、玩家队伍和待处理遭遇创建战斗会话。
        /// </summary>
        /// <param name="playerSpecies">队伍为空时使用的默认玩家精灵种族。</param>
        /// <param name="enemySpecies">没有待处理遭遇时使用的默认敌方精灵种族。</param>
        /// <param name="typeChart">战斗伤害计算使用的属性克制表。</param>
        /// <param name="defaultPokeball">首次进入战斗时发放的默认精灵球。</param>
        /// <param name="defaultPokeballCount">默认精灵球的发放数量。</param>
        /// <param name="encounterSpecies">场景层已经选定的遭遇精灵；为空时使用默认敌方配置。</param>
        /// <param name="encounterLevel">场景层已经选定的遭遇等级；无有效遭遇时传入 0。</param>
        /// <returns>完成初始化的战斗会话。</returns>
        public BattleSession Create(
            PokemonSpeciesData playerSpecies,
            PokemonSpeciesData enemySpecies,
            TypeChartData typeChart,
            ItemData defaultPokeball,
            int defaultPokeballCount,
            PokemonSpeciesData encounterSpecies = null,
            int encounterLevel = 0)
        {
            // 初始化默认道具（仅首次）
            if (defaultPokeball != null && !PlayerParty.HasItem(defaultPokeball))
            {
                PlayerParty.AddItem(defaultPokeball, defaultPokeballCount);
            }

            // Normalize legacy data first, then use the first party member as the
            // default active Pokemon when no valid active selection exists.
            PlayerParty.NormalizeParty();
            if (PlayerParty.GetPartySnapshot().Count == 0)
                PlayerParty.AddMonster(new MonsterRuntime(playerSpecies, 5));
            PlayerParty.EnsureActivePokemon();
            MonsterRuntime player = PlayerParty.ActivePokemon;

            PokemonSpeciesData selectedEnemySpecies = encounterSpecies != null
                ? encounterSpecies
                : enemySpecies;
            int selectedEnemyLevel = encounterSpecies != null && encounterLevel > 0
                ? encounterLevel
                : Random.Range(3, 7);

            var enemy = new MonsterRuntime(selectedEnemySpecies, selectedEnemyLevel);
            var turnUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));
            return new BattleSession(player, enemy, turnUseCase);
        }
    }
}
