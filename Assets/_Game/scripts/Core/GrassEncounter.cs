using UnityEngine;
using UnityEngine.SceneManagement;

public class GrassEncounter : MonoBehaviour
{
    [SerializeField] private float encounterRate = 0.2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Random.value < encounterRate)
        {
            // 保存位置并进入战斗
            SceneTransitionManager.Instance.EnterBattle(
                other.transform.position,
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
}