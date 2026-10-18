using System.Collections;
using UnityEngine;

public class Token : MonoBehaviour
{
    public bool HasArrived { get; private set; }
    [SerializeField] float fallSpeed = 20f;
    [Header("Landing bounce (presentation only)")]
    [SerializeField] float bounceHeight = 0.12f;
    [SerializeField] float bounceDuration = 0.14f;

    void OnEnable()
    {
        HasArrived = false;
    }

    public void SettleAt(Vector2 targetWorldPos)
    {
        StopAllCoroutines();
        transform.position = targetWorldPos;
        HasArrived = true;
    }

    public void AnimateFall(Vector2 targetWorldPos)
    {
        StopAllCoroutines();
        HasArrived = false;
        StartCoroutine(FallTo(targetWorldPos));
    }

    IEnumerator FallTo(Vector2 target)
    {
        var t = transform;
        while ((Vector2)t.position != target)
        {
            t.position = Vector2.MoveTowards(
                t.position, target, fallSpeed * Time.deltaTime
            );
            yield return null;
        }
        t.position = target;
        yield return Bounce(target);
        HasArrived = true;
    }

    // HasArrived stays false until the bounce settles, so move completion waits for it.
    IEnumerator Bounce(Vector2 target)
    {
        var t = transform;
        if (bounceDuration > 0f && bounceHeight > 0f)
        {
            float elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / bounceDuration);
                t.position = target + Vector2.up * (bounceHeight * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
        }
        t.position = target;
    }
}
