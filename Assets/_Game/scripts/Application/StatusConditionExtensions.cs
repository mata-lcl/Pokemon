namespace Pokemon.Domain
{
    public static class StatusConditionExtensions
    {
        public static string ToChineseName(this StatusCondition status)
        {
            return status switch
            {
                StatusCondition.None => "ÎÞ",
                StatusCondition.Poison => "ÖÐ¶¾",
                StatusCondition.Burn => "×ÆÉÕ",
                StatusCondition.Paralyze => "Âé±Ô",
                StatusCondition.Sleep => "Ë¯Ãß",
                StatusCondition.Freeze => "±ù¶³",
                _ => status.ToString()
            };
        }
    }
}