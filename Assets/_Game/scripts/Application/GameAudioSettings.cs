using System;
using UnityEngine;

namespace Pokemon.Application
{
    public static class GameAudioSettings
    {
        private const string MusicVolumeKey = "Audio.MusicVolume";
        private const string SoundEffectsVolumeKey = "Audio.SoundEffectsVolume";

        public static float MusicVolume { get; private set; } =
            PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

        public static float SoundEffectsVolume { get; private set; } =
            PlayerPrefs.GetFloat(SoundEffectsVolumeKey, 1f);

        public static event Action<float> MusicVolumeChanged;
        public static event Action<float> SoundEffectsVolumeChanged;

        /// <summary>
        /// 保存音乐音量并通知后续接入的音乐系统。
        /// </summary>
        /// <param name="volume">范围为零到一的音乐音量。</param>
        public static void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            MusicVolumeChanged?.Invoke(MusicVolume);
        }

        /// <summary>
        /// 保存音效音量并通知后续接入的音效系统。
        /// </summary>
        /// <param name="volume">范围为零到一的音效音量。</param>
        public static void SetSoundEffectsVolume(float volume)
        {
            SoundEffectsVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SoundEffectsVolumeKey, SoundEffectsVolume);
            PlayerPrefs.Save();
            SoundEffectsVolumeChanged?.Invoke(SoundEffectsVolume);
        }
    }
}
