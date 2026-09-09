using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation.Animation
{
    public sealed class SpriteFrameAnimationPlayer : MonoBehaviour
    {
        [Tooltip("实际显示并切换序列帧图片的 SpriteRenderer。")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private SpriteFrameAnimationClip _currentClip;
        private int _currentFrameIndex;
        private float _frameElapsedSeconds;
        private float _speedMultiplier = 1f;
        private bool _isPlaying;

        public event Action<SpriteFrameEventContext> FrameEventRaised;
        public event Action<SpriteFrameAnimationClip> AnimationCompleted;

        public SpriteFrameAnimationClip CurrentClip => _currentClip;
        public int CurrentFrameIndex => _currentFrameIndex;
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 按经过时间推进当前序列帧动画。
        /// </summary>
        private void Update()
        {
            if (!_isPlaying) return;

            float frameDuration = 1f / Mathf.Max(0.01f, _currentClip.FramesPerSecond);
            _frameElapsedSeconds += Time.deltaTime * _speedMultiplier;

            while (_isPlaying && _frameElapsedSeconds >= frameDuration)
            {
                _frameElapsedSeconds -= frameDuration;
                AdvanceFrame();
            }
        }

        /// <summary>
        /// 从第一帧开始播放指定动画，并应用播放速度倍率。
        /// </summary>
        /// <param name="clip">需要播放的序列帧动画配置。</param>
        /// <param name="speedMultiplier">动画播放速度倍率。</param>
        public void Play(SpriteFrameAnimationClip clip, float speedMultiplier = 1f)
        {
            _currentClip = clip;
            _currentFrameIndex = 0;
            _frameElapsedSeconds = 0f;
            _speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            _isPlaying = clip != null && clip.FrameCount > 0;

            if (!_isPlaying) return;

            ApplyCurrentFrame();
        }

        /// <summary>
        /// 停止当前动画并保留最后显示的精灵帧。
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _frameElapsedSeconds = 0f;
        }

        /// <summary>
        /// 推进到下一帧，并在非循环动画末尾发送完成通知。
        /// </summary>
        private void AdvanceFrame()
        {
            int nextFrameIndex = _currentFrameIndex + 1;
            if (nextFrameIndex >= _currentClip.FrameCount)
            {
                if (_currentClip.Loop)
                {
                    _currentFrameIndex = 0;
                    ApplyCurrentFrame();
                    return;
                }

                _isPlaying = false;
                AnimationCompleted?.Invoke(_currentClip);
                return;
            }

            _currentFrameIndex = nextFrameIndex;
            ApplyCurrentFrame();
        }

        /// <summary>
        /// 显示当前帧，并依次发送配置在该帧上的通用动画事件。
        /// </summary>
        private void ApplyCurrentFrame()
        {
            spriteRenderer.sprite = _currentClip.Frames[_currentFrameIndex];

            IReadOnlyList<SpriteFrameEventMarker> eventMarkers = _currentClip.EventMarkers;
            for (int i = 0; i < eventMarkers.Count; i++)
            {
                SpriteFrameEventMarker marker = eventMarkers[i];
                if (marker.FrameIndex != _currentFrameIndex) continue;

                FrameEventRaised?.Invoke(new SpriteFrameEventContext(
                    _currentClip,
                    marker.EventId,
                    _currentFrameIndex));
            }
        }
    }
}
