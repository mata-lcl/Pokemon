namespace Pokemon.Domain
{
    /// <summary>
    /// 提供异常状态的显示名称转换。
    /// </summary>
    public static class StatusConditionExtensions
    {
        public static string ToChineseName(this StatusCondition status)
        {
            return status switch
            {
                StatusCondition.None => "无",
                StatusCondition.Poison => "中毒",
                StatusCondition.Burn => "灼烧",
                StatusCondition.Paralyze => "麻痹",
                StatusCondition.Sleep => "睡眠",
                StatusCondition.Freeze => "冰冻",
                _ => status.ToString()
            };
        }
    }
}
