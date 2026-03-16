using Pokemon.Application;
using Pokemon.Domain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pokemon.Presentation
{
    public class BattleCoordinator : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PokemonSpeciesData playerSpecies;
        [SerializeField] private PokemonSpeciesData enemySpecies;
        [SerializeField] private TypeChartData typeChart;

        [Header("UI Reference")]
        [SerializeField] private BattleUIController uiController;

        // 设置播放每个步骤的停顿时间
        [Header("Settings")]
        [SerializeField] private float stepDelaySeconds = 1.5f;

        [Header("Views")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        public ItemData testPotion;


        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private ExecuteTurnUseCase _turnUseCase;
        private List<SkillData> _playerSkills;
        private bool _battleEnded;

        private void Start()
        {
            if (uiController == null) return;
            InitBattle();
            if (playerView != null) playerView.Setup(playerSpecies.BattleSprite);
            if (enemyView != null) enemyView.Setup(enemySpecies.BattleSprite);
        }

        private void InitBattle()
        {
            // 在 BattleCoordinator.cs 的 InitBattle 里手动给个药水测试
            PlayerParty.AddItem(testPotion, 3); // 假设你已经在 Inspector 拖入了一个 ItemData 资源
            // V0.1.2新增 可持续作战，并在游戏过程中保存宝可梦
            if (PlayerParty.ActivePokemon == null)
            {
                // 如果是第一次玩，就送一只 5 级的宝可梦并存入全局
                PlayerParty.ActivePokemon = new MonsterRuntime(playerSpecies, 5);
            }
            // 从全局队伍中获取玩家宝可梦
            _player = PlayerParty.ActivePokemon;

            // 敌人每次依然是全新生成的（可以通过 Random.Range 随机一下等级）
            _enemy = new MonsterRuntime(enemySpecies, Random.Range(3, 7));

            _turnUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));

            _playerSkills = new List<SkillData>();

            // 【修复3】GetSkillPP() 替换为了公有属性 CurrentPP
            foreach (var skill in _player.CurrentPP.Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);

            // 【修复2】界面的最大血量不再是 Species.BaseHP，而是经过个体值和等级计算后的 MaxHP
            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);

            // 【修复3】同样替换为 CurrentPP
            uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
            uiController.SetLog("战斗开始！请选择技能。");
        }

        private void OnDestroy()
        {
            if (uiController != null) uiController.OnSkillClicked -= HandlePlayerAction;
        }

        private void HandlePlayerAction(int skillIndex)
        {
            if (_battleEnded) return;

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            // 瞬间生成本回合的“录像”
            List<TurnStep> steps = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);

            // 启动协程，按时间慢慢播放录像
            StartCoroutine(PlayTurnRoutine(steps));
        }

        // 在 BattleCoordinator 类中添加这些逻辑

        public void HandleUseItem(ItemData item)
        {
            if (_battleEnded) return;

            // 1. 消耗道具
            if (PlayerParty.UseItem(item))
            {
                StartCoroutine(ItemTurnRoutine(item));
            }
        }

        private IEnumerator ItemTurnRoutine(ItemData item)
        {
            uiController.SetInteractable(false);

            // 1. 玩家使用道具步骤
            if (item.Type == ItemType.HealHP)
            {
                _player.Heal(item.EffectValue);
                uiController.SetLog($"使用了 {item.DisplayName}！{_player.Species.DisplayName} 恢复了生命值。");
                uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
                yield return new WaitForSeconds(stepDelaySeconds);
            }

            // 2. 敌人反击阶段 (因为玩家用了道具，没攻击)
            if (!_enemy.IsFainted)
            {
                // 随机选一个敌方技能
                var enemySkill = _enemy.Species.InitialSkills[Random.Range(0, _enemy.Species.InitialSkills.Count)];

                // 我们利用现有的 ExecuteTurnUseCase，但传入 null 作为玩家技能，或者手动简单处理
                // 为了严谨，我们直接复用 ResolveAction 的逻辑，或者简单写一个：
                uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 乘机发起了攻击！");

                // 这里为了演示简单，我们直接让敌人打一次
                // 在正式项目中，你可能需要重构 ExecuteTurnUseCase 使其支持 "SkipAction"
                ExecuteTurnUseCase mockUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));
                // 这里我们让玩家“发呆”，产生一个单向攻击的列表
                var singleStep = new List<TurnStep>();

                // 这里你可以通过修改 Execute 类来更优雅地实现，现在我们手动模拟一步：
                // 建议：直接调用你之前写的 ResolveAction (如果是 public 的话) 
                // 或者简单让敌人打玩家：

                // 临时逻辑：使用现有的 Execute 逻辑，给玩家一个“空技能”
                var steps = mockUseCase.Execute(_player, null, _enemy, enemySkill);
                // 过滤掉玩家的行动，只展示敌人的行动
                yield return StartCoroutine(PlayTurnRoutine(steps));
            }
        }

        private IEnumerator PlayTurnRoutine(List<TurnStep> steps)
        {
            // 锁定UI，防止连点
            uiController.SetInteractable(false);

            foreach (var step in steps)
            {
                // 更新UI显示
                uiController.SetLog(step.Message);

                // 【修复2】这里也改成 MaxHP
                uiController.UpdateHp(step.PlayerHpAfter, _player.MaxHP, step.EnemyHpAfter, _enemy.MaxHP);

                // 根据标记播放对应动画，并等待动画完成
                if (step.AnimType == StepAnimType.PlayerAttack) yield return playerView.PlayAttackAnimation(true);
                else if (step.AnimType == StepAnimType.EnemyAttack) yield return enemyView.PlayAttackAnimation(false);
                else if (step.AnimType == StepAnimType.EnemyHit) yield return enemyView.PlayHitAnimation();
                else if (step.AnimType == StepAnimType.PlayerHit) yield return playerView.PlayHitAnimation();

                // 停顿，让玩家看清文字
                yield return new WaitForSeconds(stepDelaySeconds);

                // 回合结束判断 //V0.1.2新增
                if (step.IsBattleEnd)
                {
                    _battleEnded = true;
                    uiController.SetInteractable(false);

                    // --- 新增：胜利结算阶段 ---
                    if (step.PlayerWon)
                    {
                        // 1. 获取学习力/努力值
                        _player.AddEVs(
                            _enemy.Species.EvYieldHP,
                            _enemy.Species.EvYieldAttack,
                            _enemy.Species.EvYieldDefense,
                            _enemy.Species.EvYieldSpeed);

                        // 2. 计算获得经验：(敌方基础经验 * 敌方等级) / 7
                        int gainedExp = (_enemy.Species.BaseExpYield * _enemy.Level) / 7;

                        uiController.SetLog($"获得了 {gainedExp} 点经验值！");
                        yield return new WaitForSeconds(stepDelaySeconds);

                        // 3. 增加经验并检测升级
                        if (_player.AddExp(gainedExp, out int levelsGained))
                        {
                            uiController.SetLog($"{_player.Species.DisplayName} 升到了 Lv.{_player.Level}！");

                            // 刷新一次UI上的血条（因为最大血量增加了，甚至升级回了一点血）
                            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
                            yield return new WaitForSeconds(stepDelaySeconds);

                            uiController.SetLog($"攻击变成了 {_player.Attack}，速度变成了 {_player.Speed}...");
                            yield return new WaitForSeconds(stepDelaySeconds);
                        }
                        else
                        {
                            uiController.SetLog("你眼前一黑...(可以在这里做回到城镇的逻辑)");
                            yield return new WaitForSeconds(stepDelaySeconds);
                        }
                    }

                    // === 新增：战斗结束，2秒后自动进入下一场战斗（重新加载当前场景） ===V0.1.2
                    uiController.SetLog("正在寻找下一个对手...");
                    yield return new WaitForSeconds(2f);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

                    yield break; // 结束协程
                }
            }

            // 回合播放完毕没死，解锁UI并刷新PP文本
            if (!_battleEnded)
            {
                uiController.SetLog("请选择下一步行动。");

                // 【修复3】替换为 CurrentPP
                uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            // 【修复3】替换为 CurrentPP
            foreach (var pair in monster.CurrentPP)
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null;
        }
    }
}