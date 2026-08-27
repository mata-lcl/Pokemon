using System;
using UnityEngine;

namespace Pokemon.Presentation.Animation
{
    [Serializable]
    public sealed class SpriteFrameEventMarker
    {
        [Tooltip("事件所在的帧下标，从 0 开始。")]
        [SerializeField, Min(0)] private int frameIndex;
        [Tooltip("外部系统识别该事件所使用的标识。")]
        [SerializeField] private string eventId;

        public int FrameIndex => frameIndex;
        public string EventId => eventId;
    }
}
