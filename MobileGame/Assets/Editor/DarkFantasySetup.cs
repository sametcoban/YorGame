using System;
using System.Collections.Generic;
using System.IO;
using Ashlight;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class DarkFantasyImporter : AssetPostprocessor {
    internal const string Source = "Assets/Art/DarkFantasy/";
    void OnPreprocessModel() {
        if (!assetPath.StartsWith(Source + "Models/", StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true; importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.meshCompression = ModelImporterMeshCompression.Low; importer.isReadable = false;
    }
    void OnPreprocessAnimation() {
        if (!assetPath.StartsWith(Source + "Models/", StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        var selected = new List<ModelImporterClipAnimation>(); var names = new HashSet<string>();
        foreach (var clip in importer.defaultClipAnimations) {
            string state = RealisticHeroSetup.StateFor(clip.name);
            if (state == null || !names.Add(state)) continue;
            clip.name = state; clip.loopTime = state == "Idle"; selected.Add(clip);
        }
        if (selected.Count == 7) importer.clipAnimations = selected.ToArray();
    }
    void OnPreprocessTexture() {
        if (!assetPath.StartsWith(Source + "Textures/", StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 512; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
        if (assetPath.EndsWith("Normal.png", StringComparison.Ordinal)) importer.textureType = TextureImporterType.NormalMap;
    }
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        foreach (string path in imported) if (path.StartsWith(Source,StringComparison.Ordinal)) {
            EditorApplication.delayCall += () => DarkFantasySetup.Ensure(); break;
        }
    }
}

public static class DarkFantasySetup {
    const string Generated = "Assets/Resources/DarkFantasy";
    static readonly string[] States = { "Idle", "Attack", "Cast", "Dodge", "Parry", "Hit", "Death" };
    [MenuItem("Ashlight/Art/Build Dark Fantasy Reference")]
    public static void Build() { Ensure(true); }
    public static void Ensure(bool rebuild = false) {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Generated + "/Materials"); Directory.CreateDirectory(Generated + "/Controllers");
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Stone", "PaleStone", "Iron", "Blade", "Brass", "Leather", "Wool", "RustWool", "Recess", "Ember" }) Material(name);
        BuildActor("Rowan", "Assets/Resources/Heroes/Named/Knight_Common.prefab", 2f, 1f, 145f, rebuild);
        BuildActor("LanternWarden", "Assets/Resources/Enemies/LanternWarden.prefab", 2.35f, 1.3f, -145f, rebuild);
        AssetDatabase.SaveAssets();
    }
    static Material Material(string role) {
        string path = Generated + "/Materials/" + role + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) {
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Dark fantasy reference requires Unity's built-in Standard shader.");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path);
        }
        Color color;
        switch (role) {
            case "Stone": color = new Color(.17f,.18f,.20f); break;
            case "PaleStone": color = new Color(.23f,.24f,.25f); break;
            case "Iron": color = new Color(.20f,.23f,.27f); break;
            case "Blade": color = new Color(.48f,.50f,.53f); break;
            case "Brass": color = new Color(.46f,.32f,.13f); break;
            case "Leather": color = new Color(.08f,.045f,.03f); break;
            case "Wool": color = new Color(.045f,.07f,.105f); break;
            case "RustWool": color = new Color(.10f,.04f,.023f); break;
            case "Ember": color = new Color(1,.39f,.05f); break;
            default: color = new Color(.009f,.013f,.019f); break;
        }
        material.color = color;
        bool metal = role == "Iron" || role == "Blade" || role == "Brass";
        material.SetFloat("_Metallic",metal ? .65f : 0); material.SetFloat("_Glossiness",metal ? .28f : .12f);
        string texture = metal ? "ForgedMetal" : role == "Stone" || role == "PaleStone" ? "CourtyardStone" : null;
        material.mainTexture = texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(DarkFantasyImporter.Source + "Textures/" + texture + ".png");
        var normal = metal ? AssetDatabase.LoadAssetAtPath<Texture2D>(DarkFantasyImporter.Source + "Textures/ForgedMetalNormal.png") : null;
        material.SetTexture("_BumpMap",normal);
        if (normal != null) { material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale",.45f); }
        else material.DisableKeyword("_NORMALMAP");
        // Retain emission for the eyes/lantern and the timed enemy warning flash.
        material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",role == "Ember" ? color*2 : Color.black);
        EditorUtility.SetDirty(material); return material;
    }
    static void BuildActor(string name, string path, float height, float pivot, float facing, bool rebuild) {
        if (!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        string source = DarkFantasyImporter.Source + "Models/" + name + ".fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(source); if (model == null) return;
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(source)) {
            var clip = asset as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__",StringComparison.Ordinal)) continue;
            string state = RealisticHeroSetup.StateFor(clip.name); if (state != null) clips[state] = clip;
        }
        foreach (string state in States) if (!clips.ContainsKey(state)) throw new InvalidOperationException(name + " is missing " + state + ". Reimport the FBX.");
        string controllerPath = Generated + "/Controllers/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        foreach (string state in States) { var entry = machine.AddState(state); entry.motion = clips[state]; if (state == "Idle") machine.defaultState = entry; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.Refresh();
        var root = new GameObject(name);
        try {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model); instance.transform.SetParent(root.transform,false);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(name + " has no visible mesh.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) {
                bounds.Encapsulate(renderer.bounds); var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) {
                    string role = materials[i] == null ? "Iron" : materials[i].name.Split('.')[0];
                    materials[i] = Material(name == "LanternWarden" && role == "Wool" ? "RustWool" : role);
                }
                renderer.sharedMaterials = materials;
                var skinned = renderer as SkinnedMeshRenderer; if (skinned != null) skinned.updateWhenOffscreen = true;
            }
            float scale = height/Mathf.Max(.01f,bounds.size.y); instance.transform.localScale = Vector3.one*scale;
            instance.transform.localPosition = new Vector3(0,-pivot-bounds.min.y*scale,0);
            instance.transform.localRotation = Quaternion.Euler(0,facing,0);
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            root.AddComponent<HeroVisual>().Animator = animator;
            if (name == "LanternWarden") root.AddComponent<EnemyPresentation>();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
