using Pokemon.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private Vector3 playerPositionBeforeBattle;
    private string worldSceneName;
    private PokemonSpeciesData pendingEnemySpecies;
    private int pendingEnemyLevel;

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
        EnterBattle(playerPosition, currentSceneName, null, 0);
    }

    public void EnterBattle(
        Vector3 playerPosition,
        string currentSceneName,
        PokemonSpeciesData enemySpecies,
        int enemyLevel)
    {
        playerPositionBeforeBattle = playerPosition;
        worldSceneName = currentSceneName;
        pendingEnemySpecies = enemySpecies;
        pendingEnemyLevel = Mathf.Max(0, enemyLevel);
        SceneManager.LoadScene("Fight");
    }

    public bool TryGetPendingEncounter(out PokemonSpeciesData enemySpecies, out int enemyLevel)
    {
        enemySpecies = pendingEnemySpecies;
        enemyLevel = pendingEnemyLevel;
        return enemySpecies != null && enemyLevel > 0;
    }

    public void ReturnToWorld()
    {
        SceneManager.sceneLoaded += OnWorldSceneLoaded;
        SceneManager.LoadScene(worldSceneName);
    }

    private void OnWorldSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnWorldSceneLoaded;
        pendingEnemySpecies = null;
        pendingEnemyLevel = 0;

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
