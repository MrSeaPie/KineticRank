using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WoodenPowerUpHudSetup
{
    [MenuItem("Kinetic Rank/Apply Wooden Power-Up HUD")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before styling the HUD.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before styling the HUD.");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Wood/ui_panel.png");
        if (!sprite || sprite.border == Vector4.zero)
            throw new InvalidOperationException("A sliced wooden panel sprite is required.");

        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
            PowerUpHud hud = null;
            PlayerController player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<PowerUpHud>(true))
                {
                    if (hud) throw new InvalidOperationException("Match contains multiple power-up HUD components.");
                    hud = candidate;
                }
                foreach (var candidate in root.GetComponentsInChildren<PlayerController>(true))
                {
                    if (player) throw new InvalidOperationException("Match requires exactly one player controller.");
                    player = candidate;
                }
            }
            if (!player) throw new InvalidOperationException("Match requires a player controller.");
            if (hud && (!hud.enabled || !hud.gameObject.activeInHierarchy))
                throw new InvalidOperationException("Existing power-up HUD must be active and enabled.");
            if (!hud)
            {
                var go = new GameObject("PowerUpHud");
                SceneManager.MoveGameObjectToScene(go, scene);
                hud = go.AddComponent<PowerUpHud>();
            }
            var serialized = new SerializedObject(hud);
            var property = serialized.FindProperty("panelSprite");
            if (property == null)
                throw new InvalidOperationException("PowerUpHud must expose its serialized panelSprite field.");
            property.objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Match scene.");
        }
        finally
        {
            if (Array.Exists(previous, setup => setup.isLoaded && setup.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
