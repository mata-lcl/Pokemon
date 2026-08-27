using Pokemon.Application;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "精灵球_", menuName = "宝可梦/配置/道具/精灵球")]
    public class PokeballData : ItemData, IUsable
    {
        [Tooltip("捕获率修正倍率，默认1.0不修改")]
        public float CatchRateModifier = 1.0f;
        //是否消耗品，默认为true，表示使用后会消耗掉一个道具
        public bool IsConsumable => true;

        public bool CanUse(EffectContext context)
        {
            if (!PlayerParty.CanReceiveMonster)
                return false;
            return true;// 只要是野外战斗就能用
        }

        public void OnUse(EffectContext context)
        {
            int catchRate = context.Target.Species.CatchRate;
            float statusBonus = 1f; // 未来可扩展：中毒/麻痹/冰冻增加捕获率

            // 捕获率公式：(1 - 2*当前HP/3*最大HP) * 种族捕获率 * 球修正 / 255
            float hpFactor = (3f * context.Target.MaxHP - 2f * context.Target.CurrentHP) / (3f * context.Target.MaxHP);
            float chance = hpFactor * catchRate * CatchRateModifier * statusBonus / 255f;
            chance = Mathf.Clamp01(chance);

            bool success = Random.value < chance;

            if (success)
            {
                // 捕捉成功：结束战斗
                context.Steps.Add(new TurnStep
                {
                    Message = $"成功捕捉了 {context.Target.Species.DisplayName}！",
                    PlayerHpAfter = context.PlayerRef.CurrentHP,
                    EnemyHpAfter = context.EnemyRef.CurrentHP,
                    IsBattleEnd = true,
                    CaughtSuccess = true,
                    AnimType = StepAnimType.None
                });
            }
            else
            {
                // 捕捉失败：战斗继续
                context.AddStep($"哎呀，{context.Target.Species.DisplayName} 挣脱了！");
            }
        }
    }
}
