using UnityEngine;
using UnityEngine.UI;

// A small procedural badge: no imported texture or material is required.
// The menu presenter supplies the tier colour; placement uses a neutral colour.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RankShieldGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float size = Mathf.Min(rect.width, rect.height);
        if (size <= 0f) return;
        Vector2 centre = rect.center;
        Vector2[] outline =
        {
            new Vector2(-0.43f, 0.44f),
            new Vector2(0.43f, 0.44f),
            new Vector2(0.38f, -0.10f),
            new Vector2(0f, -0.48f),
            new Vector2(-0.38f, -0.10f)
        };
        Color rim = new Color(0.20f, 0.12f, 0.07f, color.a);
        for (int i = 0; i < outline.Length; i++)
            mesh.AddVert(centre + outline[i] * size, rim, Vector2.zero);
        for (int i = 0; i < outline.Length; i++)
            mesh.AddVert(centre + outline[i] * (size * 0.79f), color, Vector2.zero);
        mesh.AddVert(centre, color, Vector2.zero);
        for (int i = 0; i < outline.Length; i++)
        {
            int next = (i + 1) % outline.Length;
            mesh.AddTriangle(i, next, i + 5);
            mesh.AddTriangle(next, next + 5, i + 5);
            mesh.AddTriangle(10, i + 5, next + 5);
        }
    }
}
