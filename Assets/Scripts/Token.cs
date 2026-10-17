using System.Collections;
using UnityEngine;

public class Token : MonoBehaviour
{
    public bool HasArrived { get; private set; }
    [SerializeField] float fallSpeed = 20f;

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
        HasArrived = true;
    }
}
