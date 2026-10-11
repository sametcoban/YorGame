using System;
using System.IO;
using Ashlight;
using UnityEditor;
using UnityEngine;
public static class NamedKnightSetup {
    [Serializable] public sealed class Look { public string id,name,gender,armor,cloth,trim,glow,feature; }
    [Serializable] sealed class Looks { public Look[] looks; }
    const string PaletteRoot="Assets/Resources/DarkFantasy/NamedMaterials";
    [MenuItem("Ashlight/Art/Build Named Knights")]
    public static void Build() { DarkFantasySetup.Ensure(); Ensure(true); }
    public static void Ensure(bool rebuild=false) {
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var data=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/DarkFantasy/Named/Knights.json"); if(data==null) return;
        var looks=JsonUtility.FromJson<Looks>(data.text); if(looks==null || looks.looks==null) throw new InvalidOperationException("Named Knight palettes are missing.");
        Directory.CreateDirectory(PaletteRoot); AssetDatabase.Refresh();
        foreach(var look in looks.looks) {
            HeroDefinition hero=null; foreach(var candidate in HeroDefinition.Catalog) if(candidate.Id==look.id) hero=candidate;
            if(hero==null || hero.Class!=HeroClass.Knight || hero.Gender.ToString()!=look.gender) throw new InvalidOperationException("Named Knight identity/gender mismatch: "+look.id);
            DarkFantasySetup.BuildActor(look.name,"Assets/Resources/Heroes/NamedVariants/"+look.id+".prefab",2f,1f,145f,rebuild,"Named_"+look.id,null,look);
        }
        AssetDatabase.SaveAssets();
    }
    public static Material Palette(Look look,string role,Material template) {
        string path=PaletteRoot+"/"+look.id+"_"+role+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(template); AssetDatabase.CreateAsset(material,path); }
        string hex=role=="Iron" || role=="Blade"?look.armor:role.EndsWith("Wool",StringComparison.Ordinal)?look.cloth:role=="Brass"?look.trim:role=="Ember"?look.glow:null;
        Color color;
        if(hex!=null && ColorUtility.TryParseHtmlString(hex,out color)) {
            material.color=color; material.SetColor("_EmissionColor",role=="Ember"?color*1.5f:Color.black);
        }
        EditorUtility.SetDirty(material); return material;
    }
}
