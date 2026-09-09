namespace Pokemon.Presentation.Animation
{
    public readonly struct SpriteFrameEventContext
    {
        public SpriteFrameAnimationClip Clip { get; }
        public string EventId { get; }
        public int FrameIndex { get; }

        /// <summary>
        /// 创建一次序列帧事件的只读上下文。
        /// </summary>
        /// <param name="clip">触发事件的动画配置。</param>
        /// <param name="eventId">供外部系统识别的事件标识。</param>
        /// <param name="frameIndex">事件所在的帧下标。</param>
        public SpriteFrameEventContext(
            SpriteFrameAnimationClip clip,
            string eventId,
            int frameIndex)
        {
            Clip = clip;
            EventId = eventId;
            FrameIndex = frameIndex;
        }
    }
}
