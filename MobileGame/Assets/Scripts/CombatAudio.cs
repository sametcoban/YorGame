using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    public sealed class CombatAudio : MonoBehaviour {
        const string MuteKey = "Ashlight.SoundMuted";
        static readonly string[] Names = { "SwordSwing", "HeavySwing", "DaggerSwipe", "BowDraw", "BowRelease", "Fire", "Cold", "Poison", "Lightning", "Light", "Charge", "Parry", "Dodge", "Hit", "Guard", "BossSlam", "HoundGrowl", "HoundBite", "Heal", "Victory", "Defeat", "MenuTick", "BossArcane", "BossFrost" };
        readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
        readonly AudioSource[] voices = new AudioSource[8];
        AudioSource music,ambience; int chapterTheme=-1;
        int cursor;
        readonly Dictionary<string,int> variations=new Dictionary<string,int>();
        static readonly string[] Varied={"SwordSwing","HeavySwing","DaggerSwipe","BowRelease","Fire","Cold","Poison","Lightning","Light"};
        public static readonly IReadOnlyList<string> RequiredClips=BuildClipList();
        static IReadOnlyList<string> BuildClipList() { var names=new List<string>(Names); foreach(string name in Varied) for(int i=1;i<=2;i++) names.Add(name+"V"+i); return names.AsReadOnly(); }
        public bool Muted { get; private set; }
        void Awake() {
            Muted = PlayerPrefs.GetInt(MuteKey,0) != 0;
            foreach (string name in RequiredClips) clips[name] = Resources.Load<AudioClip>("Audio/Combat/"+name);
            for (int i = 0; i < voices.Length; i++) {
                var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
                source.spatialBlend = 0; source.loop = false; voices[i] = source;
            }
        }
        public void PlayChapter(int chapter) {
            int theme=chapter%6; if(theme==chapterTheme && music!=null) { ResumeAtmosphere(); return; } chapterTheme=theme;
            if(music==null) { music=gameObject.AddComponent<AudioSource>(); ambience=gameObject.AddComponent<AudioSource>(); foreach(var source in new[]{music,ambience}) { source.playOnAwake=false; source.loop=true; source.spatialBlend=0; source.priority=220; } }
            music.clip=Resources.Load<AudioClip>("Audio/Chapters/Music"+theme); ambience.clip=Resources.Load<AudioClip>("Audio/Chapters/Ambience"+theme); music.volume=.22f; ambience.volume=.15f;
            if(!Muted) { if(music.clip!=null) music.Play(); if(ambience.clip!=null) ambience.Play(); }
        }
        void ResumeAtmosphere() { if(Muted || music==null) return; if(music.clip!=null && !music.isPlaying) music.Play(); if(ambience.clip!=null && !ambience.isPlaying) ambience.Play(); }
        public void ToggleMute() { Muted = !Muted; Clear(); ResumeAtmosphere(); PlayerPrefs.SetInt(MuteKey,Muted?1:0); PlayerPrefs.Save(); }
        public void Play(string name, float gain = 1, float pitch = 1) {
            if (Muted) return;
            foreach(string varied in Varied) if(name==varied) {
                int previous; variations.TryGetValue(name,out previous); int variant=previous%3; variations[name]=(previous+1)%3;
                if(variant>0) { AudioClip alternative; if(clips.TryGetValue(name+"V"+variant,out alternative) && alternative!=null) name+="V"+variant; }
                break;
            }
            AudioClip clip;
            if (!clips.TryGetValue(name,out clip) || clip == null) return;
            AudioSource voice = null;
            foreach (var candidate in voices) if (!candidate.isPlaying) { voice = candidate; break; }
            if (voice == null) { voice = voices[cursor]; cursor = (cursor+1)%voices.Length; voice.Stop(); }
            voice.clip = clip; voice.volume = Mathf.Clamp01(.55f*gain); voice.pitch = Mathf.Clamp(pitch,.8f,1.2f);
            voice.priority = name.StartsWith("Boss") ? 64 : 128; voice.Play();
        }
        public void PlayBoss(Element element,float gain=1) {
            string name=element==Element.Cold?"BossFrost":element==Element.Lightning || element==Element.Light || element==Element.Poison?"BossArcane":"BossSlam";
            AudioClip clip; if(!clips.TryGetValue(name,out clip) || clip==null) name="BossSlam";
            Play(name,gain);
        }
        public void PlayElement(Element element, float gain = 1) { Play(element == Ashlight.Element.Physical ? "Hit" : element.ToString(),gain); }
        public void Clear() { foreach (var source in voices) if (source != null) source.Stop(); if(music!=null) music.Stop(); if(ambience!=null) ambience.Stop(); }
        void OnDisable() { Clear(); }
        void OnApplicationPause(bool paused) { if (paused) Clear(); else ResumeAtmosphere(); }
    }
}
