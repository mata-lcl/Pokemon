using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minMoveDistanceToEnableCollider = 1f; // 需要移动的最小距离
    [SerializeField] private LayerMask grassLayer;
    [SerializeField] private LayerMask obstacleLayer;

    private Rigidbody2D rb;
    private Vector2 input;
    private bool isMoving;
    private Vector3 targetPosition;
    private Vector3 positionWhenDisabled; // 禁用碰撞器时的位置
    private Collider2D playerCollider;
    private Vector3 lastPosition;
    private bool colliderDisabledByBattle = false; // 新增标志

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // 确保是 Kinematic
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            //if (input != Vector2.zero)
            //{
            //    input = input.normalized;
            //    targetPosition = transform.position + new Vector3(input.x, input.y, 0);
            //    StartCoroutine(Move());
            //}

            if (colliderDisabledByBattle)
            {
                float distanceMoved = Vector3.Distance(transform.position, positionWhenDisabled);
                if (distanceMoved >= minMoveDistanceToEnableCollider)
                {
                    playerCollider.enabled = true;
                    colliderDisabledByBattle = false;
                }
            }

            if (Vector3.Distance(transform.position, lastPosition) > 0)
            {
                lastPosition = transform.position;
                CheckForGrass();
            }
        }
    }
    private void FixedUpdate()
    {
        if (input != Vector2.zero)
        {
            Vector2 movement = input.normalized * moveSpeed * Time.fixedDeltaTime;
            Vector2 newPosition = rb.position + movement;

            // 只检测障碍物层
            RaycastHit2D hit = Physics2D.Raycast(rb.position, input.normalized, movement.magnitude + 0.1f, obstacleLayer);

            if (hit.collider != null)
            {
                //Debug.Log($"检测到障碍: {hit.collider.name}");
                return; // 阻止移动
            }

            rb.MovePosition(newPosition);
        }
    }

    private System.Collections.IEnumerator Move()
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;

        CheckForGrass();
    }

    private void OnPlayerMoved()
    {
        // 这里可以添加一些玩家移动后的逻辑，比如播放脚步声、更新动画状态等
        if (playerCollider != null && !playerCollider.enabled)
            playerCollider.enabled = true;
    }
    /// <summary>
    /// 负责检测Tilemap上的草丛图层，如果玩家在草丛上并且触发率满足条件，则进入战斗场景
    /// </summary>
    private void CheckForGrass()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.2f, grassLayer);
        if (hit != null && Random.value < 0.5f) // 20%触发率
        {
            TriggerBattle();
        }
    }
    public void DisableCollider()
    {
        if (playerCollider != null)
            playerCollider.enabled = false;
            colliderDisabledByBattle = true;
            positionWhenDisabled = transform.position; // 记录禁用时的位置
    }

    private void TriggerBattle()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Fight");
    }
}