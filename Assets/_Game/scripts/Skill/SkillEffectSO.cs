using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    // 抽象基类：所有具体技能效果（伤害、回血、上毒）都要继承它
    public abstract class SkillEffectSO : ScriptableObject, ISkillEffect
    {
        // abstract 强制子类必须实现这两个方法
        public abstract bool CanProcess(EffectContext context);
        public abstract void Execute(EffectContext context);
    }
}