namespace Pokemon.Application
{
    public static class GameApplicationService
    {
        /// <summary>
        /// 结束当前游戏；在 Unity 编辑器中停止运行模式。
        /// </summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
