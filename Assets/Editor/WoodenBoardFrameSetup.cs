using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WoodenBoardFrameSetup
{
    const string ObjectName = "WoodenBoardFrame";

    [MenuItem("Kinetic Rank/Apply Wooden Board Frame")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before applying the frame.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before applying the frame.");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Wood/board_frame.png");
        if (!sprite || sprite.border == Vector4.zero)
            throw new InvalidOperationException("A sliced wooden frame sprite is required.");

        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
            BoardManager board = null;
            GameObject frame = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<BoardManager>(true))
                {
                    if (board) throw new InvalidOperationException("Match requires exactly one board.");
                    board = candidate;
                }
                if (root.name != ObjectName) continue;
                if (frame) throw new InvalidOperationException("Multiple wooden frame roots found.");
                frame = root;
            }
            if (!board || board.columns != 7 || board.rows != 6)
                throw new InvalidOperationException("Frame geometry requires the standard 7 by 6 board.");
            if (frame && (frame.GetComponents<Component>().Length > 2 || frame.transform.childCount != 0))
                throw new InvalidOperationException("Existing frame contains unexpected components or children.");

            if (!frame) frame = new GameObject(ObjectName);
            frame.SetActive(true);
            frame.transform.position = new Vector3(3.5f, 3f, 0f);
            frame.transform.rotation = Quaternion.identity;
            frame.transform.localScale = Vector3.one;
            var renderer = frame.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(7.5f, 6.5f);
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = -20;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Match scene.");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
