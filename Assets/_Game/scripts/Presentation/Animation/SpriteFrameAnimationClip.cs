using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation.Animation
{
    [CreateAssetMenu(
        fileName = "序列帧动画_",
        menuName = "宝可梦/动画/序列帧动画")]
    public sealed class SpriteFrameAnimationClip : ScriptableObject
    {
        [Tooltip("按照播放顺序排列的精灵图片帧。")]
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [Tooltip("每秒播放的图片帧数量。")]
        [SerializeField, Min(0.01f)] private float framesPerSecond = 12f;
        [Tooltip("动画播放结束后是否从第一帧继续循环。")]
        [SerializeField] private bool loop;
        [Tooltip("在指定帧发送给外部系统的动画事件。")]
        [SerializeField] private List<SpriteFrameEventMarker> eventMarkers =
            new List<SpriteFrameEventMarker>();

        public IReadOnlyList<Sprite> Frames => frames;
        public int FrameCount => frames.Length;
        public float FramesPerSecond => framesPerSecond;
        public bool Loop => loop;
        public IReadOnlyList<SpriteFrameEventMarker> EventMarkers => eventMarkers;
    }
}
