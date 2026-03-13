using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleDebugRunner : MonoBehaviour
    {
        [Header("Assign Data Assets")]
        [SerializeField] private PokemonSpeciesData playerSpecies;
        [SerializeField] private PokemonSpeciesData enemySpecies;
        [SerializeField] private TypeChartData typeChart;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;

        private DamageCalculator _damageCalculator;
        private ExecuteTurnUseCase_Old _executeTurnUseCase;

        private bool _battleEnded;

        private void Start()
        {
            if (playerSpecies == null || enemySpecies == null)
            {
                Debug.LogError("[BattleDebugRunner] 请先在 Inspector 绑定 playerSpecies / enemySpecies。");
                enabled = false;
                return;
            }

            _player = new MonsterRuntime(playerSpecies, 5);
            _enemy = new MonsterRuntime(enemySpecies, 5);

            _damageCalculator = new DamageCalculator(typeChart);
            _executeTurnUseCase = new ExecuteTurnUseCase_Old(_damageCalculator);

            Debug.Log("=== Battle Start ===");
            Debug.Log($"Player: {_player.Species.DisplayName} HP={_player.CurrentHP} SPD={_player.Speed}");
            Debug.Log($"Enemy : {_enemy.Species.DisplayName} HP={_enemy.CurrentHP} SPD={_enemy.Speed}");
            Debug.Log("按 Space 执行一回合。");
        }

        private void Update()
        {
            if (_battleEnded) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                RunOneTurn();
            }
        }

        private void RunOneTurn()
        {
            SkillData playerSkill = PickFirstAvailableSkill(_player);
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            if (playerSkill == null)
            {
                Debug.LogWarning("玩家无可用技能（PP耗尽）");
                _battleEnded = true;
                return;
            }

            if (enemySkill == null)
            {
                Debug.LogWarning("敌人无可用技能（PP耗尽）");
                _battleEnded = true;
                return;
            }

            bool playerFirst = _player.Speed >= _enemy.Speed;
            Debug.Log($"\n--- Turn --- 先手: {(playerFirst ? "Player" : "Enemy")}");

            var result = _executeTurnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);

            if (result.PlayerActed)
            {
                Debug.Log($"{_player.Species.DisplayName} 使用 {playerSkill.DisplayName} -> {(result.PlayerHit ? "命中" : "未命中")} 伤害: {result.DamageToEnemy}");
            }

            if (result.EnemyActed)
            {
                Debug.Log($"{_enemy.Species.DisplayName} 使用 {enemySkill.DisplayName} -> {(result.EnemyHit ? "命中" : "未命中")} 伤害: {result.DamageToPlayer}");
            }

            Debug.Log($"当前HP => {_player.Species.DisplayName}:{_player.CurrentHP} | {_enemy.Species.DisplayName}:{_enemy.CurrentHP}");

            if (result.BattleEnded)
            {
                _battleEnded = true;
                Debug.Log(result.PlayerWon ? "=== Battle End: Player Won ===" : "=== Battle End: Enemy Won ===");
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            IReadOnlyDictionary<SkillData, int> ppMap = monster.CurrentPP;
            foreach (var pair in ppMap)
            {
                if (pair.Key != null && pair.Value > 0)
                    return pair.Key;
            }
            return null;
        }
    }
}