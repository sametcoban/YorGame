using UnityEngine;
namespace Ashlight {
    public sealed class HeroVisual : MonoBehaviour {
        public Animator Animator;
        bool defeated;
        float returnAt, pauseUntil;
        public void Play(string state) {
            if(Animator==null || defeated || !Animator.HasState(0,UnityEngine.Animator.StringToHash(state))) return;
            pauseUntil = 0; Animator.speed = 1;
            Animator.CrossFadeInFixedTime(state,state == "Hit" || state == "Parry" ? .025f : .055f,0,0f);
            float duration=.6f;
            foreach(var clip in Animator.runtimeAnimatorController.animationClips) if(clip.name==state) duration=clip.length;
            returnAt=Time.time+duration;
        }
        public void ContactPause(float seconds = .035f) {
            if (Animator == null || defeated) return;
            seconds = Mathf.Clamp(seconds,0,.07f); pauseUntil = Time.unscaledTime+seconds;
            Animator.speed = 0; returnAt += seconds;
        }
        public void SetDefeated(bool value) {
            if(value==defeated || Animator==null) return;
            pauseUntil = 0; Animator.speed = 1; defeated=value;
            Animator.CrossFadeInFixedTime(value?"Death":"Idle",.1f); returnAt=0;
        }
        void OnEnable() { if(Animator!=null) { Animator.speed=1; pauseUntil=0; defeated=false; Animator.Play("Idle",0,0); } returnAt=0; }
        void OnDisable() { pauseUntil=0; if(Animator!=null) Animator.speed=1; }
        void Update() {
            if(Animator!=null && pauseUntil>0 && Time.unscaledTime>=pauseUntil) { pauseUntil=0; Animator.speed=1; }
            if(!defeated && returnAt>0 && Time.time>=returnAt) { returnAt=0; Animator.CrossFadeInFixedTime("Idle",.1f); }
        }
    }
}
