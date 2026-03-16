using UnityEngine;

namespace Pokemon.Domain
{
    public enum ItemType
    {
        HealHP,    // 回复HP
        HealPP,    // 回复PP (未来扩展)
        Pokeball   // 精灵球 (未来扩展)
    }

    [CreateAssetMenu(fileName = "Item_", menuName = "Pokemon/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public ItemType Type;
        public int EffectValue; // 回复多少血？
        public Sprite Icon;     // 道具图标
    }
}