using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WoodenBackgroundSetup
{
    const string ObjectName = "WoodenTableBackground";

    [MenuItem("Kinetic Rank/Apply Wooden Backgrounds")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before applying backgrounds.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before applying backgrounds.");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Wood/table_background.png");
        if (!sprite) throw new InvalidOperationException("Wooden table sprite is missing.");
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ApplyScene("Assets/Scenes/MainMenu.unity", sprite);
            ApplyScene("Assets/Scenes/Match.unity", sprite);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }

    static void ApplyScene(string path, Sprite sprite)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Camera target = null;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var camera in root.GetComponentsInChildren<Camera>(true))
        {
            if (!camera.CompareTag("MainCamera")) continue;
            if (target) throw new InvalidOperationException(path + " has multiple main cameras.");
            target = camera;
        }
        if (!target || !target.orthographic)
            throw new InvalidOperationException(path + " requires an orthographic main camera.");
        if (target.transform.lossyScale != Vector3.one)
            throw new InvalidOperationException(path + " requires an unscaled camera hierarchy.");
        if (target.nearClipPlane >= 10f || target.farClipPlane <= 10f)
            throw new InvalidOperationException(path + " camera does not include background depth 10.");

        Transform existing = target.transform.Find(ObjectName);
        var background = existing ? existing.gameObject : new GameObject(ObjectName);
        background.transform.SetParent(target.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 10f);
        background.transform.localRotation = Quaternion.identity;
        var renderer = background.GetComponent<SpriteRenderer>();
        if (!renderer) renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingLayerID = 0;
        renderer.sortingOrder = -100;
        var cover = background.GetComponent<TableBackgroundCover>();
        if (!cover) cover = background.AddComponent<TableBackgroundCover>();
        cover.targetCamera = target;
        target.backgroundColor = new Color32(0x26, 0x15, 0x0B, 0xFF);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save " + path);
    }
}
