using UnityEngine;
using Pokemon.Domain;

namespace Pokemon.Presentation
{
    /// <summary>
    /// 战斗调试工具：通过快捷键实时修改战斗状态
    /// </summary>
    public class BattleDebugger : MonoBehaviour
    {
        [Header("绑定引用")]
        [SerializeField] private BattleCoordinator coordinator;

        [Header("调试配置")]
        [Tooltip("按下此键将玩家血量设定为猛火触发线 (1/4)")]
        [SerializeField] private KeyCode lowHPKey = KeyCode.H;

        [Tooltip("按下此键瞬间击败对手")]
        [SerializeField] private KeyCode instantWinKey = KeyCode.K;

        [Tooltip("按下此键恢复满血")]
        [SerializeField] private KeyCode healKey = KeyCode.R;

        private void Update()
        {
            if (coordinator == null) return;

            // 获取当前战斗中的实例（利用反射或修改 BattleCoordinator 将其公开）
            // 这里假设我们在 BattleCoordinator 中添加了对 _player 和 _enemy 的访问权限

            if (Input.GetKeyDown(lowHPKey))
            {
                SetPlayerLowHP();
            }

            if (Input.GetKeyDown(instantWinKey))
            {
                KillEnemy();
            }

            if (Input.GetKeyDown(healKey))
            {
                FullHealPlayer();
            }
        }

        private void SetPlayerLowHP()
        {
            var player = GetPlayer();
            if (player == null) return;

            // 设置为 MaxHP 的 25%，确保触发 1/3 的猛火特性
            int targetHP = player.MaxHP / 4;
            player.DebugSetHP(targetHP);

            RefreshUI();
            Debug.Log($"<color=orange>[Debugger]</color> 玩家血量已设为残血: {player.CurrentHP}/{player.MaxHP}");
        }

        private void KillEnemy()
        {
            var enemy = GetEnemy();
            if (enemy == null) return;

            enemy.DebugSetHP(0);
            RefreshUI();
            Debug.Log("<color=red>[Debugger]</color> 对方已倒下");
        }

        private void FullHealPlayer()
        {
            var player = GetPlayer();
            if (player == null) return;

            player.DebugSetHP(player.MaxHP);
            RefreshUI();
            Debug.Log("<color=green>[Debugger]</color> 玩家已满血复活");
        }

        // 辅助方法：通过反射或公开字段获取私有变量
        private MonsterRuntime GetPlayer() =>
            (MonsterRuntime)typeof(BattleCoordinator).GetField("_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(coordinator);

        private MonsterRuntime GetEnemy() =>
            (MonsterRuntime)typeof(BattleCoordinator).GetField("_enemy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(coordinator);

        private void RefreshUI()
        {
            // 通过调用 coordinator 的 UI 更新逻辑同步画面
            // 假设我们稍微修改 BattleCoordinator 暴露一个刷新 UI 的方法
            coordinator.SendMessage("InitBattleUI", null, SendMessageOptions.DontRequireReceiver);

            // 或者直接强制刷新
            var ui = typeof(BattleCoordinator).GetField("uiController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(coordinator) as BattleUIController;
            if (ui != null)
            {
                var p = GetPlayer();
                var e = GetEnemy();
                ui.UpdateHp(p.CurrentHP, p.MaxHP, e.CurrentHP, e.MaxHP);
            }
        }
    }
}