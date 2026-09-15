using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 协调战斗菜单命令与玩家队伍状态，不包含界面播放和场景切换逻辑。
    /// </summary>
    public sealed class BattleCommandService
    {
        /// <summary>
        /// 判断当前技能下标是否可以作为玩家本回合的选择。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <param name="skillIndex">玩家技能列表中的下标。</param>
        /// <returns>战斗可操作且技能下标有效时返回 true。</returns>
        public bool CanExecuteSkillTurn(BattleSession session, int skillIndex)
        {
            return session != null &&
                !session.BattleEnded &&
                skillIndex >= 0 &&
                skillIndex < session.PlayerSkills.Count;
        }

        /// <summary>
        /// 根据技能下标执行玩家与敌方的完整技能回合。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <param name="skillIndex">玩家技能列表中的下标。</param>
        /// <param name="result">成功执行时返回本回合的业务结果。</param>
        /// <returns>技能下标有效且回合已经执行时返回 true。</returns>
        public bool TryExecuteSkillTurn(
            BattleSession session,
            int skillIndex,
            out TurnExecutionResult result)
        {
            result = null;
            if (!CanExecuteSkillTurn(session, skillIndex))
            {
                return false;
            }

            SkillData enemySkill = PickFirstAvailableSkill(session.Enemy);
            result = session.TurnUseCase.Execute(
                session.Player,
                session.PlayerSkills[skillIndex],
                session.Enemy,
                enemySkill);
            return true;
        }

        /// <summary>
        /// 返回玩家当前可以换上的队伍成员。
        /// </summary>
        /// <returns>不包含当前出战和已倒下精灵的快照。</returns>
        public IReadOnlyList<MonsterRuntime> GetSwitchablePokemon()
        {
            return PlayerParty.GetSwitchablePokemon();
        }

        /// <summary>
        /// 返回供战斗换人界面显示的队伍快照。
        /// </summary>
        /// <returns>玩家当前队伍的只读快照。</returns>
        public IReadOnlyList<MonsterRuntime> GetPartySnapshot()
        {
            return PlayerParty.GetPartySnapshot();
        }

        /// <summary>
        /// 返回供战斗背包界面显示的可使用道具快照。
        /// </summary>
        /// <returns>保持玩家当前手动顺序的道具及数量。</returns>
        public IReadOnlyList<InventoryItemStack> GetUsableItemSnapshot()
        {
            return PlayerParty.GetUsableItemSnapshot();
        }

        /// <summary>
        /// 调整战斗背包中的道具顺序。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        /// <returns>两个道具都存在且顺序已经调整时返回 true。</returns>
        public bool TryReorderItem(ItemData item, ItemData targetItem)
        {
            return PlayerParty.TryReorderItem(item, targetItem);
        }

        /// <summary>
        /// 将指定队伍成员设置为当前出战精灵，并同步战斗会话。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <param name="selectedPokemon">玩家选择换上的队伍成员。</param>
        /// <param name="previousPokemon">换人成功时返回原出战精灵。</param>
        /// <returns>队伍成员有效且切换成功时返回 true。</returns>
        public bool TrySwitchPokemon(
            BattleSession session,
            MonsterRuntime selectedPokemon,
            out MonsterRuntime previousPokemon)
        {
            previousPokemon = null;
            if (session == null ||
                session.BattleEnded ||
                selectedPokemon == null ||
                selectedPokemon == session.Player)
            {
                return false;
            }

            if (!PlayerParty.TrySetActivePokemon(selectedPokemon))
                return false;

            previousPokemon = session.Player;
            session.SetPlayer(selectedPokemon);
            return true;
        }

        /// <summary>
        /// 尝试执行战斗道具，并在成功使用消耗品后扣除一件库存。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <param name="item">玩家选择的道具定义。</param>
        /// <param name="result">可使用道具的执行结果。</param>
        /// <returns>传入对象实现战斗道具接口并已完成使用判定时返回 true。</returns>
        public bool TryExecuteItem(
            BattleSession session,
            ItemData item,
            out ItemUseExecutionResult result)
        {
            result = null;
            if (session == null ||
                session.BattleEnded ||
                !(item is IUsable usable))
            {
                return false;
            }

            result = session.TurnUseCase.ExecuteItem(
                usable,
                session.Player,
                session.Enemy);
            if (result.Succeeded && result.IsConsumable)
                PlayerParty.RemoveItem(item, 1);

            return true;
        }

        /// <summary>
        /// 在换人或使用道具后执行敌方行动及仍需发生的回合末结算。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <param name="result">敌方行动与回合末结算合并后的结果。</param>
        /// <returns>战斗仍可继续且敌方能够回应时返回 true。</returns>
        public bool TryExecuteEnemyResponse(
            BattleSession session,
            out TurnExecutionResult result)
        {
            result = null;
            if (!CanExecuteEnemyResponse(session))
                return false;

            SkillData enemySkill = PickFirstAvailableSkill(session.Enemy);
            result = session.TurnUseCase.ExecuteSingleAction(
                session.Player,
                session.Enemy,
                enemySkill,
                isPlayerAction: false);

            if (result.Conclusion == BattleConclusion.Ongoing)
            {
                TurnExecutionResult endOfTurnResult =
                    session.TurnUseCase.ExecuteEndOfTurn(
                        session.Player,
                        session.Enemy);
                result = TurnExecutionResult.Combine(result, endOfTurnResult);
            }

            return true;
        }

        /// <summary>
        /// 判断当前战斗状态是否允许敌方在玩家换人或使用道具后回应。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <returns>战斗未结束且敌方未倒下时返回 true。</returns>
        public bool CanExecuteEnemyResponse(BattleSession session)
        {
            return session != null &&
                !session.BattleEnded &&
                !session.Enemy.IsFainted;
        }

        /// <summary>
        /// 将捕获的敌方精灵加入玩家队伍或仓库，并结束当前战斗。
        /// </summary>
        /// <param name="session">发生成功捕获的战斗会话。</param>
        /// <returns>捕获精灵成功加入队伍或仓库时返回 true。</returns>
        public bool CompleteCapture(BattleSession session)
        {
            if (session == null)
                return false;

            bool stored = PlayerParty.TryAddMonster(new MonsterRuntime(
                session.Enemy.Species,
                session.Enemy.Level));
            session.BattleEnded = true;
            return stored;
        }

        /// <summary>
        /// 按当前战斗规则判断玩家是否能够逃跑。
        /// </summary>
        /// <param name="session">当前战斗会话。</param>
        /// <returns>战斗尚未结束时返回 true。</returns>
        public bool TryEscape(BattleSession session)
        {
            return session != null && !session.BattleEnded;
        }

        /// <summary>
        /// 返回指定精灵当前第一个仍有 PP 的技能。
        /// </summary>
        /// <param name="monster">需要选择技能的敌方精灵。</param>
        /// <returns>第一个可用技能；没有可用技能时返回 null。</returns>
        private static SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            foreach (KeyValuePair<SkillData, int> pair in monster.CurrentPP)
            {
                if (pair.Key != null && pair.Value > 0)
                    return pair.Key;
            }

            return null;
        }
    }
}
