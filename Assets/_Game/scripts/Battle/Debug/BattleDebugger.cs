using UnityEngine;
using Pokemon.Domain;

namespace Pokemon.Presentation
{
    /// <summary>
    /// 战斗调试工具：通过快捷键实时修改战斗状态
    /// </summary>
    public class BattleDebugger : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("绑定引用")]
        [SerializeField] private BattleCoordinator coordinator;

        [Header("调试配置")]
        [Tooltip("按下此键将玩家血量设定为猛火触发线 (1/4)")]
        [SerializeField] private KeyCode lowHPKey = KeyCode.H;

        [Tooltip("按下此键瞬间击败对手")]
        [SerializeField] private KeyCode instantWinKey = KeyCode.K;

        [Tooltip("按下此键恢复满血")]
        [SerializeField] private KeyCode healKey = KeyCode.R;
#endif

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (coordinator == null) return;

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
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

        private MonsterRuntime GetPlayer() => coordinator.CurrentPlayer;

        private MonsterRuntime GetEnemy() => coordinator.CurrentEnemy;

        private void RefreshUI()
        {
            coordinator.RefreshBattlePresentation();
        }
#endif
    }
}
