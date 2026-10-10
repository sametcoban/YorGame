using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    public sealed class CombatAudio : MonoBehaviour {
        const string MuteKey = "Ashlight.SoundMuted";
        static readonly string[] Names = { "SwordSwing", "HeavySwing", "DaggerSwipe", "BowDraw", "BowRelease", "Fire", "Cold", "Poison", "Lightning", "Light", "Charge", "Parry", "Dodge", "Hit", "Guard", "BossSlam", "HoundGrowl", "HoundBite", "Heal", "Victory", "Defeat" };
        readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
        readonly AudioSource[] voices = new AudioSource[8];
        int cursor;
        public bool Muted { get; private set; }
        void Awake() {
            Muted = PlayerPrefs.GetInt(MuteKey,0) != 0;
            foreach (string name in Names) clips[name] = Resources.Load<AudioClip>("Audio/Combat/"+name);
            for (int i = 0; i < voices.Length; i++) {
                var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
                source.spatialBlend = 0; source.loop = false; voices[i] = source;
            }
        }
        public void ToggleMute() { Muted = !Muted; Clear(); PlayerPrefs.SetInt(MuteKey,Muted?1:0); PlayerPrefs.Save(); }
        public void Play(string name, float gain = 1, float pitch = 1) {
            if (Muted) return;
            AudioClip clip;
            if (!clips.TryGetValue(name,out clip) || clip == null) return;
            AudioSource voice = null;
            foreach (var candidate in voices) if (!candidate.isPlaying) { voice = candidate; break; }
            if (voice == null) { voice = voices[cursor]; cursor = (cursor+1)%voices.Length; voice.Stop(); }
            voice.clip = clip; voice.volume = Mathf.Clamp01(.55f*gain); voice.pitch = Mathf.Clamp(pitch,.8f,1.2f);
            voice.priority = name == "BossSlam" ? 64 : 128; voice.Play();
        }
        public void PlayElement(Element element, float gain = 1) { Play(element == Ashlight.Element.Physical ? "Hit" : element.ToString(),gain); }
        public void Clear() { foreach (var source in voices) if (source != null) source.Stop(); }
        void OnDisable() { Clear(); }
        void OnApplicationPause(bool paused) { if (paused) Clear(); }
    }
}
