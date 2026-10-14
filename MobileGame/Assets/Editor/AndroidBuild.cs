using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild {
    [MenuItem("Ashlight/Android/Configure Android")]
    public static void Configure() {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support with SDK, NDK and OpenJDK in Unity Hub.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch to Android. Let Unity finish importing and try again.");
        ProjectSetup.EnsureScene();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        // Match the runtime HUD's StandaloneInputModule with the legacy input backend.
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"));
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedProperties(); }
        AssetDatabase.SaveAssets();
        Debug.Log("Android configured: IL2CPP, ARM64, Android 8.0 minimum, landscape, legacy touch input.");
    }

    [MenuItem("Ashlight/Android/Build Google Play Bundle")]
    public static void BuildGooglePlayBundle() {
        Configure();
        if(!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
            throw new InvalidOperationException("Configure your upload keystore and alias in Player Settings > Android > Publishing Settings. Keep the key backed up privately; do not commit it.");
        if(string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            throw new InvalidOperationException("Enter the keystore and alias passwords locally in Unity Publishing Settings. Never put passwords in source control or chat.");
        string identifier=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if(string.IsNullOrEmpty(identifier) || identifier.StartsWith("com.DefaultCompany.",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose your final unique Android package identifier in Player Settings before creating a Play release.");
        if(string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.Android.bundleVersionCode<1)
            throw new InvalidOperationException("Set an app version and a positive Android bundle version code in Player Settings. Increment the code for every Play upload.");
        const string scene="Assets/Scenes/Battle.unity";
        if(!File.Exists(scene)) throw new InvalidOperationException("Battle scene is missing; prepare the project first.");
        Directory.CreateDirectory("Builds/Android");
        bool previousBundle=EditorUserBuildSettings.buildAppBundle;
        try {
            EditorUserBuildSettings.buildAppBundle=true;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{scene},locationPathName="Builds/Android/Ashlight.aab",target=BuildTarget.Android,options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException("Google Play bundle build failed: "+report.summary.result+"; errors: "+report.summary.totalErrors);
            Debug.Log("Signed release bundle built: "+Path.GetFullPath("Builds/Android/Ashlight.aab")+". This has not been uploaded or published. Start with a Play Console internal test and review the candidate on Android before production.");
        } finally { EditorUserBuildSettings.buildAppBundle=previousBundle; }
    }

    [MenuItem("Ashlight/Android/Build Test APK")]
    public static void BuildTestApk() {
        Configure();
        const string scene = "Assets/Scenes/Battle.unity";
        if (!File.Exists(scene)) throw new InvalidOperationException("Battle scene is missing; prepare the project first.");
        Directory.CreateDirectory("Builds/Android");
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        try {
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scene },
                locationPathName = "Builds/Android/Ashlight.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result + "; errors: " + report.summary.totalErrors);
            Debug.Log("Test APK built: " + Path.GetFullPath("Builds/Android/Ashlight.apk"));
        } finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
    }
}
