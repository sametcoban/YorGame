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
}
