using System.IO;
using UnityEditor;
using UnityEngine;
using Ashlight;
public static class HeroPortraitSetup {
    [MenuItem("Ashlight/Art/Build Hero Portraits")]
    public static void Ensure() {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string destination="Assets/Resources/HeroPortraits";
        Directory.CreateDirectory(destination); AssetDatabase.Refresh();
        foreach(var hero in HeroDefinition.Catalog) {
            string source="Assets/Art/DarkFantasy/Portraits/"+hero.Id+".png", target=destination+"/"+hero.Id+".png";
            if(!File.Exists(source)) continue;
            if(!File.Exists(target)) AssetDatabase.CopyAsset(source,target);
            else if(!System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(source),File.ReadAllBytes(target))) File.Copy(source,target,true);
            AssetDatabase.ImportAsset(target);
            var importer=AssetImporter.GetAtPath(target) as TextureImporter;
            if(importer==null) continue;
            if(importer.textureType==TextureImporterType.Sprite && importer.maxTextureSize==256 && !importer.mipmapEnabled) continue;
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=256; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }
}
