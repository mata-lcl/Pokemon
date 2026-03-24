

using UnityEngine;

namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "New Pokeball", menuName = "Pokemon/Item/Pokeball")]
    public class PokeballData : ItemData, IUsable
    {
        //抓捕率修正值，默认为1.0f，表示不修改抓捕率
        public float CatchRateModifier = 1.0f;
        //是否消耗品，默认为true，表示使用后会消耗掉一个道具
        public bool IsConsumable => true;

        public bool CanUse(EffectContext context)
        {
            return true;// 只要是野外战斗就能用
        }

        public void OnUse(EffectContext context)
        {
            // 捕捉计算逻辑
            float chance = (3f * context.Target.MaxHP - 2f * context.Target.CurrentHP) / (3f * context.Target.MaxHP);
            bool success = Random.value < (chance * CatchRateModifier * 0.3f);

            context.Steps.Add(new Application.TurnStep
            {
                Message = success ? $"成功捕捉了 {context.Target.Species.DisplayName}！" : "哎呀，没抓到！"
            });
        }
    }
}