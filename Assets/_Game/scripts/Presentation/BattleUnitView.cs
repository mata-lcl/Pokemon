using System.Collections;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 _originalPosition;

        private void Awake()
        {
            _originalPosition = transform.position;
        }

        public void Setup(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        // 简单的“向前撞击”动画
        public IEnumerator PlayAttackAnimation(bool isPlayer)
        {
            yield return PlayAttackAnimation(isPlayer, 1f);
        }

        /// <summary>
        /// Plays the attack animation at the requested speed.
        /// </summary>
        public IEnumerator PlayAttackAnimation(bool isPlayer, float speedMultiplier)
        {
            Vector3 targetPos = _originalPosition + (isPlayer ? Vector3.right : Vector3.left) * 1.5f;
            float safeSpeed = Mathf.Max(0.01f, speedMultiplier);
            float forwardDuration = 0.1f / safeSpeed;
            float returnDuration = 0.15f / safeSpeed;

            // 冲过去
            float t = 0;
            while (t < forwardDuration)
            {
                transform.position = Vector3.Lerp(_originalPosition, targetPos, t / forwardDuration);
                t += Time.deltaTime;
                yield return null;
            }

            // 退回来
            t = 0;
            while (t < returnDuration)
            {
                transform.position = Vector3.Lerp(targetPos, _originalPosition, t / returnDuration);
                t += Time.deltaTime;
                yield return null;
            }
            transform.position = _originalPosition;
        }

        // 简单的“受击闪红”动画
        public IEnumerator PlayHitAnimation()
        {
            yield return PlayHitAnimation(1f);
        }

        /// <summary>
        /// Plays the hit flash animation at the requested speed.
        /// </summary>
        public IEnumerator PlayHitAnimation(float speedMultiplier)
        {
            if (spriteRenderer == null) yield break;

            float safeSpeed = Mathf.Max(0.01f, speedMultiplier);

            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.white;
        }
    }
}
