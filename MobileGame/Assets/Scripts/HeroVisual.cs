using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    public sealed class HeroVisual : MonoBehaviour {
        public Animator Animator;
        bool defeated;
        float returnAt,pauseUntil;
        RuntimeAnimatorController cachedController;
        readonly Dictionary<string,float> clipLengths=new Dictionary<string,float>();
        readonly Dictionary<string,int> states=new Dictionary<string,int>();
        static readonly string[] StateNames={"Idle","Attack","Cast","Dodge","Parry","Hit","Death","Windup"};
        void CacheController() {
            if(Animator==null || Animator.runtimeAnimatorController==cachedController && states.Count>0) return;
            cachedController=Animator.runtimeAnimatorController; clipLengths.Clear(); states.Clear();
            if(cachedController==null) return;
            foreach(var clip in cachedController.animationClips) if(clip!=null) clipLengths[clip.name]=clip.length;
            foreach(string name in StateNames) { int hash=UnityEngine.Animator.StringToHash(name); if(Animator.HasState(0,hash)) states[name]=hash; }
        }
        bool Transition(string state,float blend) {
            CacheController(); int hash;
            if(Animator==null || !states.TryGetValue(state,out hash)) return false;
            Animator.CrossFadeInFixedTime(hash,blend,0,0f); return true;
        }
        public void Play(string state) {
            if(Animator==null || defeated) return;
            float blend=state=="Hit"?.025f:state=="Parry"?.04f:state=="Cast"?.10f:state=="Dodge"?.065f:.075f;
            if(!Transition(state,blend)) return;
            pauseUntil=0; Animator.speed=1;
            float duration; if(!clipLengths.TryGetValue(state,out duration)) duration=.6f;
            returnAt=Time.time+duration;
        }
        // Retain authored clip speed/contact frames; only align the return to idle with the presentation.
        public void PlayAction(string state,float duration) {
            CacheController(); if(!states.ContainsKey(state)) return;
            Play(state); if(!defeated && returnAt>0) returnAt=Time.time+Mathf.Max(.05f,duration);
        }
        public void ContactPause(float seconds=.035f) {
            if(Animator==null || defeated || AccessibilityOptions.ReducedMotion) return;
            seconds=Mathf.Clamp(seconds,0,.07f); pauseUntil=Mathf.Max(pauseUntil,Time.unscaledTime+seconds/Mathf.Max(1,Time.timeScale));
            Animator.speed=0; returnAt+=seconds;
        }
        public void SetDefeated(bool value) {
            if(value==defeated || Animator==null) return;
            pauseUntil=0; Animator.speed=1; defeated=value;
            Transition(value?"Death":"Idle",value?.08f:.16f); returnAt=0;
        }
        void OnEnable() {
            defeated=false; pauseUntil=returnAt=0;
            if(Animator!=null) { CacheController(); Animator.speed=1; int hash; if(states.TryGetValue("Idle",out hash)) Animator.Play(hash,0,0); }
        }
        void OnDisable() { pauseUntil=returnAt=0; if(Animator!=null) Animator.speed=1; }
        void Update() {
            if(Animator!=null && pauseUntil>0 && Time.unscaledTime>=pauseUntil) { pauseUntil=0; Animator.speed=1; }
            if(!defeated && pauseUntil==0 && returnAt>0 && Time.time>=returnAt) { returnAt=0; Transition("Idle",.16f); }
        }
    }
}
