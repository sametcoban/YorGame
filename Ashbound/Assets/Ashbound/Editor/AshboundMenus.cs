using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ashbound.Editor
{
    public static class AshboundMenus
    {
        public const string ArenaScene = "Assets/Ashbound/Scenes/Arena.unity";

        [MenuItem("Ashbound/Open Arena")]
        public static void OpenArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ArenaScene);
        }

        [MenuItem("Ashbound/Build/Desktop")]
        public static void BuildDesktop()
        {
            BuildTarget target;
            string path;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    target = BuildTarget.StandaloneWindows64;
                    path = "Builds/Windows/Ashbound.exe";
                    break;
                case RuntimePlatform.OSXEditor:
                    target = BuildTarget.StandaloneOSX;
                    path = "Builds/macOS/Ashbound.app";
                    break;
                default:
                    target = BuildTarget.StandaloneLinux64;
                    path = "Builds/Linux/Ashbound";
                    break;
            }
            Build(target, path);
        }

        [MenuItem("Ashbound/Build/Android APK")]
        public static void BuildAndroid()
        {
            // Uses the Android SDK/NDK supplied by the user's Unity Hub module.
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            Build(BuildTarget.Android, "Builds/Android/Ashbound.apk");
        }

        private static void Build(BuildTarget target, string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("Install the " + target + " build support module in Unity Hub before building.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ArenaScene }, locationPathName = output,
                target = target, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Ashbound build failed. See the Console for details: " + report.summary.result);
            Debug.Log("Ashbound built successfully: " + output);
        }
    }
}
