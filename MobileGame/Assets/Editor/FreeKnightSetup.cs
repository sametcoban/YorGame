using System;
using System.Collections.Generic;
using System.IO;
using Ashlight;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class FreeKnightImporter : AssetPostprocessor {
    void OnPreprocessModel() {
        if(assetPath!="Assets/ThirdParty/KayKit/Source/Knight.fbx") return;
        var importer=(ModelImporter)assetImporter;
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true; importer.optimizeGameObjects=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
    }
    void OnPreprocessAnimation() {
        if(assetPath!="Assets/ThirdParty/KayKit/Source/Knight.fbx") return;
        var importer=(ModelImporter)assetImporter;
        var selected=new List<ModelImporterClipAnimation>();
        foreach(var clip in importer.defaultClipAnimations) {
            string state=FreeKnightSetup.StateFor(clip.name);
            if(state==null) continue;
            clip.name=state; clip.loopTime=state=="Idle";
            selected.Add(clip);
        }
        if(selected.Count==7) importer.clipAnimations=selected.ToArray();
    }
    void OnPreprocessTexture() {
        if(assetPath!="Assets/ThirdParty/KayKit/Source/knight_texture.png") return;
        var importer=(TextureImporter)assetImporter;
        importer.maxTextureSize=512; importer.mipmapEnabled=true;
    }
}

public static class FreeKnightSetup {
    const string Folder="Assets/Resources/Heroes";
    const string PrefabPath=Folder+"/Knight.prefab";
    [MenuItem("Ashlight/Characters/Build Free Knight")]
    public static void Build() { Ensure(true); }
    public static string StateFor(string name) {
        int separator=name.LastIndexOf('|'); if(separator>=0) name=name.Substring(separator+1);
        if(name=="Idle") return "Idle";
        if(name=="1H_Melee_Attack_Slice_Horizontal") return "Attack";
        if(name=="Dodge_Backward") return "Dodge";
        if(name=="Block") return "Parry";
        if(name=="Hit_A") return "Hit";
        if(name=="Death_A") return "Death";
        if(name=="Spellcast_Raise") return "Cast";
        return null;
    }
    public static void Ensure(bool rebuild=false) {
        if(!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null) return;
        const string source="Assets/ThirdParty/KayKit/Source/Knight.fbx";
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(source);
        if(model==null) return; // Initial import may still be running; preparation can be repeated.
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var clips=new Dictionary<string,AnimationClip>();
        foreach(var item in AssetDatabase.LoadAllAssetsAtPath(source)) {
            var clip=item as AnimationClip;
            if(clip==null || clip.name.StartsWith("__preview__")) continue;
            string state=StateFor(clip.name) ?? (Array.IndexOf(new[]{"Idle","Attack","Dodge","Parry","Hit","Death","Cast"},clip.name)>=0?clip.name:null);
            if(state!=null) clips[state]=clip;
        }
        foreach(string state in new[]{"Idle","Attack","Dodge","Parry","Hit","Death","Cast"})
            if(!clips.ContainsKey(state)) throw new InvalidOperationException("Knight animation missing: "+state+". Reimport Knight.fbx, then prepare again.");
        string controllerPath=Folder+"/Knight.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states) machine.RemoveState(state.state);
        foreach(var entry in clips) {
            var state=machine.AddState(entry.Key); state.motion=entry.Value;
            if(entry.Key=="Idle") machine.defaultState=state;
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/KnightDark.mat");
        if(material==null) {
            var shader=Shader.Find("Standard");
            if(shader==null) throw new InvalidOperationException("Built-in Standard shader missing; use the project's built-in render pipeline.");
            material=new Material(shader); AssetDatabase.CreateAsset(material,Folder+"/KnightDark.mat");
        }
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/KayKit/Source/knight_texture.png");
        material.color=new Color(.6f,.58f,.62f); material.SetFloat("_Metallic",.45f); material.SetFloat("_Glossiness",.25f);
        var root=new GameObject("Dark Knight");
        try {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model); instance.transform.SetParent(root.transform,false);
            foreach(var child in instance.GetComponentsInChildren<Transform>(true)) {
                if(child.name=="1H_Sword_Offhand" || child.name=="2H_Sword" || child.name=="Badge_Shield" || child.name=="Round_Shield" || child.name=="Spike_Shield") child.gameObject.SetActive(false);
                if(child.name=="1H_Sword" || child.name=="Rectangle_Shield") child.gameObject.SetActive(true);
            }
            var renderers=instance.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) throw new InvalidOperationException("Knight has no visible mesh renderers.");
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers) { bounds.Encapsulate(renderer.bounds); renderer.sharedMaterial=material; }
            float scale=2f/Mathf.Max(.01f,bounds.size.y);
            instance.transform.localScale=Vector3.one*scale;
            instance.transform.localPosition=new Vector3(0,-1-bounds.min.y*scale,0);
            instance.transform.localRotation=Quaternion.Euler(0,90,0);
            var animator=instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
            root.AddComponent<HeroVisual>().Animator=animator;
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); AssetDatabase.SaveAssets();
        } finally { UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log("Free KayKit Knight ready. Rigged model and seven combat states generated; test in Play mode.");
    }
}
