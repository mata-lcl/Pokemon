using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private Vector3 playerPositionBeforeBattle;
    private string worldSceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnterBattle(Vector3 playerPosition, string currentSceneName)
    {
        playerPositionBeforeBattle = playerPosition;
        worldSceneName = currentSceneName;
        SceneManager.LoadScene("Fight");
    }

    public void ReturnToWorld()
    {
        SceneManager.sceneLoaded += OnWorldSceneLoaded;
        SceneManager.LoadScene(worldSceneName);
    }

    private void OnWorldSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnWorldSceneLoaded;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerPositionBeforeBattle;

            // 禁用碰撞器，直到玩家移动
            PlayerMovement controller = player.GetComponent<PlayerMovement>();
            if (controller != null)
            {
                controller.DisableCollider();
            }
        }
    }
}