using UnityEngine;

namespace Pokemon.Application
{
    public static class LoadedGameContext
    {
        private static bool _hasPendingPlayerPosition;
        private static string _pendingSceneName;
        private static Vector3 _pendingPlayerPosition;

        /// <summary>
        /// 保存读档完成后等待目标场景应用的玩家位置。
        /// </summary>
        /// <param name="sceneName">存档记录的场景名称。</param>
        /// <param name="playerPosition">存档记录的玩家位置。</param>
        public static void SetPendingPlayerPosition(
            string sceneName,
            Vector3 playerPosition)
        {
            _hasPendingPlayerPosition = true;
            _pendingSceneName = sceneName;
            _pendingPlayerPosition = playerPosition;
        }

        /// <summary>
        /// 在场景名称匹配时返回并清除等待应用的玩家位置。
        /// </summary>
        /// <param name="sceneName">当前加载完成的场景名称。</param>
        /// <param name="playerPosition">返回存档中的玩家世界坐标。</param>
        public static bool TryConsumePlayerPosition(
            string sceneName,
            out Vector3 playerPosition)
        {
            if (_hasPendingPlayerPosition && _pendingSceneName == sceneName)
            {
                playerPosition = _pendingPlayerPosition;
                ClearPendingPlayerPosition();
                return true;
            }

            playerPosition = default;
            return false;
        }

        /// <summary>
        /// 清除尚未应用的读档玩家位置。
        /// </summary>
        public static void ClearPendingPlayerPosition()
        {
            _hasPendingPlayerPosition = false;
            _pendingSceneName = string.Empty;
        }
    }
}
