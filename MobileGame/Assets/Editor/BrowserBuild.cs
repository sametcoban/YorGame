using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BrowserBuild {
    [MenuItem("Ashlight/Browser/Configure Browser")]
    public static void Configure() {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support (WebGL) for this editor in Unity Hub.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Could not switch to Web. Wait for importing to finish and try again.");
        ProjectSetup.EnsureScene();
        // Uncompressed files work with simple static hosts without custom encoding headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:MobileSafari";
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"));
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedProperties(); }
        AssetDatabase.SaveAssets();
        Debug.Log("Browser configured. Use Safari on a recent iPhone, in landscape. Serve the build over HTTP or HTTPS.");
    }

    [MenuItem("Ashlight/Browser/Build Browser Game")]
    public static void BuildBrowserGame() {
        Configure();
        const string scene = "Assets/Scenes/Battle.unity";
        if (!File.Exists(scene)) throw new InvalidOperationException("Battle scene is missing; prepare the project first.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene },
            locationPathName = "Builds/Browser",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Browser build failed: " + report.summary.result + "; errors: " + report.summary.totalErrors);
        Debug.Log("Browser game built: " + Path.GetFullPath("Builds/Browser/index.html") + ". Host the whole Builds/Browser folder, not just index.html.");
    }
}
