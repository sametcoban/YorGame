using System;
using System.Collections.Generic;
using System.IO;
using Ashlight;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class HumanModelImporter : AssetPostprocessor {
    internal const string SourceFolder = "Assets/ThirdParty/MakeHuman/Models/";
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        foreach (string path in imported) {
            if (!path.StartsWith(SourceFolder, StringComparison.Ordinal)) continue;
            EditorApplication.delayCall += () => RealisticHeroSetup.Ensure();
            break;
        }
    }
    void OnPreprocessModel() {
        if (!assetPath.StartsWith(SourceFolder, StringComparison.Ordinal) || !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.optimizeGameObjects = false; // Hand bones remain accessible for class equipment.
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Low;
    }
    void OnPreprocessAnimation() {
        if (!assetPath.StartsWith(SourceFolder, StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        var clips = new List<ModelImporterClipAnimation>();
        var seen = new HashSet<string>();
        foreach (var clip in importer.defaultClipAnimations) {
            string state = RealisticHeroSetup.StateFor(clip.name);
            if (state == null || !seen.Add(state)) continue;
            clip.name = state; clip.loopTime = state == "Idle";
            clips.Add(clip);
        }
        if (clips.Count == 7) importer.clipAnimations = clips.ToArray();
    }
}

public static class RealisticHeroSetup {
    const string Folder = "Assets/Resources/Heroes";
    const string Generated = Folder + "/HumanAssets";
    static readonly string[] States = { "Idle", "Attack", "Cast", "Dodge", "Parry", "Hit", "Death" };
    [MenuItem("Ashlight/Characters/Build Realistic Humans")]
    public static void Build() { Ensure(true); }
    public static string StateFor(string name) {
        int separator = name.LastIndexOf('|');
        if (separator >= 0) name = name.Substring(separator + 1);
        foreach (string state in States) if (name == state) return state;
        return null;
    }
    public static void Ensure(bool rebuild = false) {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        foreach (HeroGender gender in Enum.GetValues(typeof(HeroGender))) {
            string source = HumanModelImporter.SourceFolder + "Human_" + gender + ".fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (model == null) continue;
            bool missing = false;
            foreach (HeroClass kind in Enum.GetValues(typeof(HeroClass)))
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + kind + "_" + gender + ".prefab") == null) missing = true;
            if (!rebuild && !missing) continue;
            Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(source)) {
                var clip = asset as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                string state = StateFor(clip.name);
                if (state != null) clips[state] = clip;
            }
            foreach (string state in States)
                if (!clips.ContainsKey(state)) throw new InvalidOperationException("Human animation missing: " + state + ". Reimport " + source + ", then build realistic humans again.");
            var controller = Controller(gender, clips);
            foreach (HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
                string path = Folder + "/" + kind + "_" + gender + ".prefab";
                if (!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                var root = new GameObject(kind + " " + gender);
                try {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    instance.transform.SetParent(root.transform, false);
                    foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                        if (child.name.StartsWith("Robe", StringComparison.Ordinal)) child.gameObject.SetActive(kind == HeroClass.Sorceress || kind == HeroClass.Cleric);
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) throw new InvalidOperationException("Human model has no renderers: " + source);
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) {
                        bounds.Encapsulate(renderer.bounds);
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = MaterialFor(materials[i], kind, gender);
                        renderer.sharedMaterials = materials;
                        var skinned = renderer as SkinnedMeshRenderer;
                        if (skinned != null) skinned.updateWhenOffscreen = true;
                    }
                    float scale = 2f / Mathf.Max(.01f, bounds.size.y);
                    instance.transform.localScale = Vector3.one * scale;
                    instance.transform.localPosition = new Vector3(0, -1f - bounds.min.y * scale, 0);
                    // Blender's -Y facing exports to +Z. Turn toward the enemy
                    // (+X) while keeping the face visible to the battle camera.
                    instance.transform.localRotation = Quaternion.Euler(0, 145, 0);
                    var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                    root.AddComponent<HeroVisual>().Animator = animator;
                    AddEquipment(instance.transform, kind, gender);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                } finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        AssetDatabase.SaveAssets();
    }
    static AnimatorController Controller(HeroGender gender, Dictionary<string, AnimationClip> clips) {
        string path = Generated + "/Human_" + gender + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        foreach (string name in States) {
            var state = machine.AddState(name); state.motion = clips[name];
            if (name == "Idle") machine.defaultState = state;
        }
        return controller;
    }
    static Material MaterialFor(Material original, HeroClass kind, HeroGender gender) {
        string role = original != null ? original.name.Split('.')[0] : "Skin";
        string path = Generated + "/" + kind + "_" + gender + "_" + role + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) {
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Human materials need Unity's built-in Standard shader.");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path);
        }
        Color color = original != null ? original.color : new Color(.65f, .43f, .32f);
        if (role == "Cloth") color = ClassColor(kind);
        if (role == "Metal") color = kind == HeroClass.Paladin ? new Color(.75f,.59f,.28f) : new Color(.45f,.48f,.52f);
        if (role == "Cloth" && (kind == HeroClass.Knight || kind == HeroClass.Paladin)) color = kind == HeroClass.Paladin ? new Color(.65f,.51f,.26f) : new Color(.38f,.42f,.48f);
        material.color = color;
        bool metal = role == "Metal" || role == "Cloth" && (kind == HeroClass.Knight || kind == HeroClass.Paladin);
        material.SetFloat("_Metallic", metal ? .65f : 0f);
        material.SetFloat("_Glossiness", role.StartsWith("Eye", StringComparison.Ordinal) || role == "Iris" || role == "Pupil" ? .7f : metal ? .35f : .2f);
        EditorUtility.SetDirty(material); return material;
    }
    static Color ClassColor(HeroClass kind) {
        switch (kind) {
            case HeroClass.Knight: return new Color(.25f,.34f,.45f);
            case HeroClass.Paladin: return new Color(.65f,.51f,.26f);
            case HeroClass.Sorceress: return new Color(.32f,.15f,.42f);
            case HeroClass.Ranger: return new Color(.2f,.32f,.19f);
            case HeroClass.Rogue: return new Color(.3f,.12f,.16f);
            default: return new Color(.64f,.61f,.5f);
        }
    }
    static void AddEquipment(Transform model, HeroClass kind, HeroGender gender) {
        Transform right = null, left = null;
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) {
            if (child.name == "hand_r") right = child;
            if (child.name == "hand_l") left = child;
        }
        if (right == null || left == null) throw new InvalidOperationException("Human skeleton is missing hand attachment bones.");
        string materialPath = Generated + "/Equipment.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) {
            material = new Material(Shader.Find("Standard")); material.color = new Color(.42f,.45f,.49f);
            material.SetFloat("_Metallic",.7f); AssetDatabase.CreateAsset(material, materialPath);
        }
        if (kind == HeroClass.Sorceress || kind == HeroClass.Cleric) {
            Prop("Staff", PrimitiveType.Cylinder, right, new Vector3(0,.03f,.2f), new Vector3(.018f,.45f,.018f), material);
            var jewel = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/" + kind + "_" + gender + "_Cloth.mat");
            Prop("Staff Jewel", PrimitiveType.Sphere, right, new Vector3(0,.48f,.2f), Vector3.one*.08f, jewel ?? material);
        } else if (kind == HeroClass.Ranger) {
            Prop("Bow", PrimitiveType.Cube, left, new Vector3(0,.05f,.08f), new Vector3(.02f,.6f,.035f), material);
        } else {
            float length = kind == HeroClass.Rogue ? .3f : .6f;
            Prop("Blade", PrimitiveType.Cube, right, new Vector3(0,length*.5f,.06f), new Vector3(.025f,length,.01f), material);
            Prop("Guard", PrimitiveType.Cube, right, new Vector3(0,0,.06f), new Vector3(.12f,.025f,.025f), material);
            if (kind == HeroClass.Rogue) Prop("Second Dagger", PrimitiveType.Cube, left, new Vector3(0,.15f,.06f), new Vector3(.025f,.3f,.01f), material);
            else Prop("Shield", PrimitiveType.Cube, left, new Vector3(0,.03f,.08f), new Vector3(.25f,.36f,.04f), material);
        }
    }
    static void Prop(string name, PrimitiveType shape, Transform bone, Vector3 position, Vector3 scale, Material material) {
        var prop = GameObject.CreatePrimitive(shape); prop.name = name;
        UnityEngine.Object.DestroyImmediate(prop.GetComponent<Collider>());
        prop.transform.SetParent(bone, false); prop.transform.localPosition = position; prop.transform.localScale = scale;
        prop.GetComponent<Renderer>().sharedMaterial = material;
    }
}
