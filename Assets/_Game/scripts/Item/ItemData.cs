using UnityEngine;

namespace Pokemon.Domain
{
    public abstract class ItemData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;
    }
}