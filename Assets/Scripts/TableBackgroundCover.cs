using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TableBackgroundCover : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (!targetCamera || !targetCamera.orthographic || !renderer.sprite) return;
        Vector2 size = renderer.sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;
        float height = 2f * targetCamera.orthographicSize;
        float scale = Mathf.Max(height / size.y, height * targetCamera.aspect / size.x);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
