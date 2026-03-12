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
            Vector3 targetPos = _originalPosition + (isPlayer ? Vector3.right : Vector3.left) * 1.5f;

            // 冲过去
            float t = 0;
            while (t < 0.1f)
            {
                transform.position = Vector3.Lerp(_originalPosition, targetPos, t / 0.1f);
                t += Time.deltaTime;
                yield return null;
            }

            // 退回来
            t = 0;
            while (t < 0.15f)
            {
                transform.position = Vector3.Lerp(targetPos, _originalPosition, t / 0.15f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.position = _originalPosition;
        }

        // 简单的“受击闪红”动画
        public IEnumerator PlayHitAnimation()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }
}