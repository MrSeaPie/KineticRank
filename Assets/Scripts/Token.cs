using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Token : MonoBehaviour
{
    const float DROP_SPEED = 20f;     // units/second

    /// <summary>Called by BoardManager right after Instantiate.</summary>
    public void AnimateFall (Vector3 target)
    {
        // start one unit above the target so it visibly “drops”
        transform.position = new Vector3(target.x, target.y + 1f);
        StartCoroutine(MoveDown(target));
    }

    IEnumerator MoveDown (Vector3 target)
    {
        while (Vector2.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                                                     target,
                                                     DROP_SPEED * Time.deltaTime);
            yield return null;
        }
        transform.position = target;   // final snap
    }
}
