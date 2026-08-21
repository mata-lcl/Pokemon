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
    private GrassEncounter currentGrassEncounter;
    private float distanceSinceEncounterCheck;

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

            float movementDelta = Vector3.Distance(transform.position, lastPosition);
            if (movementDelta > 0f)
            {
                lastPosition = transform.position;
                CheckForGrass(movementDelta);
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

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        CheckForGrass(distanceMoved);
    }

    private void OnPlayerMoved()
    {
        // 这里可以添加一些玩家移动后的逻辑，比如播放脚步声、更新动画状态等
        if (playerCollider != null && !playerCollider.enabled)
            playerCollider.enabled = true;
    }
    /// <summary>
    /// 设置玩家是否可以移动，并在禁用移动时清除当前方向输入。
    /// </summary>
    /// <param name="movementEnabled">玩家是否可以读取输入并移动。</param>
    public void SetMovementEnabled(bool movementEnabled)
    {
        if (!movementEnabled)
            input = Vector2.zero;
        enabled = movementEnabled;
    }

    /// <summary>
    /// Finds the configured grass area and asks it to perform an encounter check.
    /// </summary>
    private void CheckForGrass(float distanceMoved)
    {
        if (colliderDisabledByBattle)
        {
            return;
        }

        GrassEncounter encounter = GrassEncounter.FindAtPosition(transform.position, grassLayer);
        if (encounter != currentGrassEncounter)
        {
            currentGrassEncounter = encounter;
            distanceSinceEncounterCheck = 0f;

            if (currentGrassEncounter != null)
            {
                currentGrassEncounter.TryStartEncounter(transform);
            }

            return;
        }

        if (currentGrassEncounter == null)
        {
            return;
        }

        distanceSinceEncounterCheck += distanceMoved;
        if (distanceSinceEncounterCheck < currentGrassEncounter.DistancePerCheck)
        {
            return;
        }

        distanceSinceEncounterCheck %= currentGrassEncounter.DistancePerCheck;
        currentGrassEncounter.TryStartEncounter(transform);
    }
    public void DisableCollider()
    {
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        colliderDisabledByBattle = true;
        positionWhenDisabled = transform.position; // 记录禁用时的位置
        currentGrassEncounter = null;
        distanceSinceEncounterCheck = 0f;
    }
}
