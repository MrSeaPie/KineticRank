using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WoodenMenuSetup
{
    [MenuItem("Kinetic Rank/Apply Wooden Menu Buttons")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before styling the menu.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before styling the menu.");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Wood/ui_button.png");
        if (!sprite || sprite.border == Vector4.zero)
            throw new InvalidOperationException("A sliced wooden button sprite is required.");

        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var buttons = new System.Collections.Generic.List<Button>();
            foreach (var root in scene.GetRootGameObjects())
                buttons.AddRange(root.GetComponentsInChildren<Button>(true));
            if (buttons.Count == 0)
                throw new InvalidOperationException("MainMenu contains no buttons.");
            foreach (var button in buttons)
                if (!(button.targetGraphic is Image))
                    throw new InvalidOperationException("Every menu button must target an Image: " + button.name);

            foreach (var button in buttons)
            {
                var image = (Image)button.targetGraphic;
                image.sprite = sprite;
                image.overrideSprite = null;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.76f, 0.64f, 0.49f, 1f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
                colors.colorMultiplier = 1f;
                button.colors = colors;
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    label.color = new Color32(243, 232, 210, 255);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save MainMenu scene.");
        }
        finally
        {
            if (Array.Exists(previous, setup => setup.isLoaded && setup.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
