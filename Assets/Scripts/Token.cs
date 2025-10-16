using System.Collections;
using UnityEngine;

public class Token : MonoBehaviour
{
    // Public read-only flag BoardManager can poll
    public bool HasArrived { get; private set; }

    // Tune this if you want faster/slower falls
    [SerializeField] float fallSpeed = 20f;

    void OnEnable()
    {
        HasArrived = false;
    }

    public void AnimateFall(Vector2 targetWorldPos)
    {
        StopAllCoroutines();
        StartCoroutine(FallTo(targetWorldPos));
    }

    IEnumerator FallTo(Vector2 target)
    {
        // Simple MoveTowards with a hard snap at the end
        var t = transform;
        while ((Vector2)t.position != target)
        {
            t.position = Vector2.MoveTowards(
                t.position, target, fallSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Ensure exact alignment and mark as arrived
        t.position = target;
        HasArrived = true;
    }
}
