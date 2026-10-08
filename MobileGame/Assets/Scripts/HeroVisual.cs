using UnityEngine;
namespace Ashlight {
    public sealed class HeroVisual : MonoBehaviour {
        public Animator Animator;
        bool defeated;
        float returnAt;
        public void Play(string state) {
            if(Animator==null || defeated || !Animator.HasState(0,UnityEngine.Animator.StringToHash(state))) return;
            Animator.CrossFadeInFixedTime(state,.08f);
            float duration=.6f;
            foreach(var clip in Animator.runtimeAnimatorController.animationClips) if(clip.name==state) duration=clip.length;
            returnAt=Time.time+duration;
        }
        public void SetDefeated(bool value) {
            if(value==defeated || Animator==null) return;
            defeated=value;
            Animator.CrossFadeInFixedTime(value?"Death":"Idle",.1f); returnAt=0;
        }
        void OnEnable() { if(Animator!=null) { defeated=false; Animator.Play("Idle",0,0); } returnAt=0; }
        void Update() {
            if(!defeated && returnAt>0 && Time.time>=returnAt) { returnAt=0; Animator.CrossFadeInFixedTime("Idle",.1f); }
        }
    }
}
