using System;
using System.Collections.Generic;
using System.IO;
using Ashlight;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Read-only diagnostics. These never load, reset, or overwrite the player's save.
public static class MobilePlaytestChecks {
    [MenuItem("Ashlight/Diagnostics/Check Combat Assets")]
    public static void CheckAssets() {
        int errors=0;
        foreach(var hero in HeroDefinition.Catalog) {
            if(Resources.Load<Sprite>("HeroPortraits/"+hero.Id)==null) { Debug.LogError("Missing portrait: "+hero.Id+". Run Build Hero Portraits."); errors++; }
            errors+=CheckActor(Resources.Load<GameObject>("Heroes/NamedVariants/"+hero.Id),hero.Id,false);
        }
        foreach(var foe in EnemyVisualDefinition.Catalog) errors+=CheckActor(Resources.Load<GameObject>(foe.ResourcePath),foe.EnemyName,true);
        foreach(string sound in CombatAudio.RequiredClips)
            if(Resources.Load<AudioClip>("Audio/Combat/"+sound)==null) { Debug.LogError("Missing combat sound: "+sound); errors++; }
        if(errors>0) throw new InvalidOperationException("Combat assets: "+errors+" issues. Run Ashlight > Art > Build All Named Heroes and Build Enemy Roster, then check again.");
        Debug.Log("Combat assets passed: all 30 named heroes, portraits, enemy variants and "+CombatAudio.RequiredClips.Count+" sounds exist. This verifies imports, not animation playback or device performance.");
    }
    static int CheckActor(GameObject actor,string name,bool enemy) {
        if(actor==null) { Debug.LogError("Missing actor prefab: "+name); return 1; }
        var animator=actor.GetComponentInChildren<Animator>(true);
        if(animator==null || animator.runtimeAnimatorController==null) { Debug.LogError("Missing animation controller: "+name); return 1; }
        int errors=0; var clips=new HashSet<string>();
        foreach(var clip in animator.runtimeAnimatorController.animationClips) {
            string state=clip.name.Substring(clip.name.LastIndexOf('|')+1);
            if(clip.length>0) clips.Add(state);
        }
        foreach(string state in new[]{"Idle","Attack","Cast","Hit","Dodge","Parry","Death"})
            if(!clips.Contains(state)) { Debug.LogError(name+" is missing a nonempty "+state+" clip."); errors++; }
        if(enemy && !clips.Contains("Windup")) { Debug.LogError(name+" is missing a windup clip."); errors++; }
        return errors;
    }
    [MenuItem("Ashlight/Diagnostics/Check Current Screen")]
    public static void CheckScreen() {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Press Play and open the screen you want to check first.");
        Canvas.ForceUpdateCanvases();
        int listeners=0;
        foreach(var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)) if(listener.enabled) listeners++;
        if(listeners!=1) throw new InvalidOperationException("Expected one active audio listener, found "+listeners+".");
        var prototype=UnityEngine.Object.FindFirstObjectByType<Prototype>(); if(prototype==null) throw new InvalidOperationException("Ashlight is not running.");
        RectTransform safe=null;
        foreach(var rect in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)) if(rect.name=="Safe Area") { safe=rect; break; }
        if(safe==null) throw new InvalidOperationException("Safe Area was not found.");
        Transform top=safe;
        for(int i=0;i<safe.childCount;i++) {
            var child=safe.GetChild(i); var rect=child as RectTransform;
            if(child.gameObject.activeInHierarchy && rect!=null && child.GetComponent<Image>()!=null && rect.anchorMin==Vector2.zero && rect.anchorMax==Vector2.one) top=child;
        }
        int checkedButtons=0, issues=0; var corners=new Vector3[4]; var clickable=new List<Button>(); var boxes=new List<Rect>();
        foreach(var button in top.GetComponentsInChildren<Button>(false)) {
            var rect=button.transform as RectTransform; if(rect==null) continue;
            rect.GetWorldCorners(corners); var lo=RectTransformUtility.WorldToScreenPoint(null,corners[0]); var hi=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            var box=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y); checkedButtons++;
            var area=Screen.safeArea;
            if(box.width<=0 || box.height<=0 || lo.x<area.xMin-2 || lo.y<area.yMin-2 || hi.x>area.xMax+2 || hi.y>area.yMax+2) { Debug.LogWarning("Button outside safe area or empty: "+button.name); issues++; }
            if(button.interactable) { clickable.Add(button); boxes.Add(box); }
        }
        for(int i=0;i<boxes.Count;i++) for(int j=i+1;j<boxes.Count;j++) {
            float width=Mathf.Min(boxes[i].xMax,boxes[j].xMax)-Mathf.Max(boxes[i].xMin,boxes[j].xMin);
            float height=Mathf.Min(boxes[i].yMax,boxes[j].yMax)-Mathf.Max(boxes[i].yMin,boxes[j].yMin);
            if(width>2 && height>2) { Debug.LogWarning("Overlapping tap targets: "+clickable[i].name+" and "+clickable[j].name); issues++; }
        }
        if(issues>0) throw new InvalidOperationException(top.name+": "+issues+" UI layout issues at "+Screen.width+"×"+Screen.height+". Check Console and the Game view.");
        Debug.Log(top.name+": "+checkedButtons+" button bounds checked, no overlapping tap targets, one audio listener at "+Screen.width+"×"+Screen.height+". Readability and touch comfort still require visual/device testing.");
    }
    [MenuItem("Ashlight/Diagnostics/Capture Current Screen")]
    public static void Capture() {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Press Play before capturing the current screen.");
        Directory.CreateDirectory("Builds/Playtest");
        string path=Path.GetFullPath("Builds/Playtest/"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Screen.width+"x"+Screen.height+".png");
        ScreenCapture.CaptureScreenshot(path); Debug.Log("Screenshot requested: "+path+". Unity writes it after the frame renders.");
    }
    static double sampleStarted; static int firstFrame;
    [MenuItem("Ashlight/Diagnostics/Measure Editor Playback (10 seconds)")]
    public static void Measure() {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Press Play and start a fight before measuring.");
        EditorApplication.update-=Sample; sampleStarted=EditorApplication.timeSinceStartup; firstFrame=Time.frameCount; EditorApplication.update+=Sample;
        Debug.Log("Sampling 10 seconds of Editor playback. Keep the Game view visible.");
    }
    static void Sample() {
        if(!EditorApplication.isPlaying) { EditorApplication.update-=Sample; return; }
        double elapsed=EditorApplication.timeSinceStartup-sampleStarted; if(elapsed<10) return;
        EditorApplication.update-=Sample;
        Debug.Log("EDITOR ONLY: "+((Time.frameCount-firstFrame)/elapsed).ToString("F1")+" frames/s · managed heap "+(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()/1048576.0).ToString("F1")+" MiB. This is not a phone benchmark.");
    }
}
