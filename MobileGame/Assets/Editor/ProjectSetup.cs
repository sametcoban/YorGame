using UnityEditor.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
[InitializeOnLoad]
public static class ProjectSetup {
    static ProjectSetup() { EditorApplication.delayCall += EnsureScene; }
    [MenuItem("Ashlight/Prepare Mobile Project")]
    public static void EnsureScene() {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        EnsureEffectMaterial();
        FreeKnightSetup.Ensure();
        const string path = "Assets/Scenes/Battle.unity";
        if (!File.Exists(path)) {
            // Never discard an unsaved editor scene.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PlayerSettings.companyName = "AshlightStudio";
            PlayerSettings.productName = "Ashlight";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ashlightstudio.ashlight");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ashlightstudio.ashlight");
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }
        if (EditorBuildSettings.scenes.Length == 0)
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    }
    static void EnsureEffectMaterial() {
        const string path = "Assets/Resources/CombatEffects.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find("Unlit/Color");
        if (shader == null) throw new System.InvalidOperationException("Unity's built-in Unlit/Color shader was not found.");
        if (!Directory.Exists("Assets/Resources")) {
            Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
        }
        // A Resources asset retains the shader in player builds; Shader.Find alone can be stripped.
        AssetDatabase.CreateAsset(new Material(shader), path);
        AssetDatabase.SaveAssets();
    }
}
