using Pokemon.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pokemon.Presentation
{
    public class SavedPlayerPositionLoader : MonoBehaviour
    {
        /// <summary>
        /// 场景加载完成时将读档中的世界坐标应用到当前玩家对象。
        /// </summary>
        private void Start()
        {
            if (SaveGameService.TryConsumePlayerPosition(
                SceneManager.GetActiveScene().name,
                out Vector3 playerPosition))
            {
                transform.position = playerPosition;
            }
        }
    }
}
