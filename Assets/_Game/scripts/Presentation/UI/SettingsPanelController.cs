using System;
using Pokemon.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundEffectsVolumeSlider;
        [SerializeField] private Button closeButton;

        public event Action CloseRequested;

        /// <summary>
        /// 显示设置面板。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏设置面板。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 面板启用时同步音量并绑定控件事件。
        /// </summary>
        private void OnEnable()
        {
            musicVolumeSlider.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
            soundEffectsVolumeSlider.SetValueWithoutNotify(GameAudioSettings.SoundEffectsVolume);
            musicVolumeSlider.onValueChanged.AddListener(ChangeMusicVolume);
            soundEffectsVolumeSlider.onValueChanged.AddListener(ChangeSoundEffectsVolume);
            closeButton.onClick.AddListener(RequestClose);
        }

        /// <summary>
        /// 面板禁用时解除控件事件绑定。
        /// </summary>
        private void OnDisable()
        {
            musicVolumeSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
            soundEffectsVolumeSlider.onValueChanged.RemoveListener(ChangeSoundEffectsVolume);
            closeButton.onClick.RemoveListener(RequestClose);
        }

        /// <summary>
        /// 将音乐滑块值写入音频设置接口。
        /// </summary>
        /// <param name="volume">范围为零到一的音乐音量。</param>
        private void ChangeMusicVolume(float volume)
        {
            GameAudioSettings.SetMusicVolume(volume);
        }

        /// <summary>
        /// 将音效滑块值写入音频设置接口。
        /// </summary>
        /// <param name="volume">范围为零到一的音效音量。</param>
        private void ChangeSoundEffectsVolume(float volume)
        {
            GameAudioSettings.SetSoundEffectsVolume(volume);
        }

        /// <summary>
        /// 通知场景菜单关闭设置面板。
        /// </summary>
        private void RequestClose()
        {
            CloseRequested?.Invoke();
        }
    }
}
