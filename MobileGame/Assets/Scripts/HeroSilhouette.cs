using UnityEngine;
namespace Ashlight {
    // Small role ornaments follow an existing head bone; no new rigs or paid assets.
    public sealed class HeroSilhouette : MonoBehaviour {
        Material accent;
        public void Build(HeroDefinition hero) {
            var renderers=GetComponentsInChildren<Renderer>(); if(renderers.Length==0) return;
            Bounds bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Transform head=transform; foreach(var bone in GetComponentsInChildren<Transform>()) if(bone.name.ToLowerInvariant().EndsWith("head")) { head=bone; break; }
            var shader=Shader.Find("Standard"); if(shader==null) return;
            accent=new Material(shader); accent.color=Color.Lerp(CombatEffects.Tint(hero.Affinity),new Color(.22f,.19f,.15f),.65f);
            int count=hero.Class==HeroClass.Knight || hero.Class==HeroClass.Rogue?2:hero.Class==HeroClass.Ranger?1:3;
            for(int i=0;i<count;i++) {
                var crest=GameObject.CreatePrimitive(hero.Class==HeroClass.Cleric?PrimitiveType.Sphere:PrimitiveType.Cube); crest.name="Role Crest";
                var collider=crest.GetComponent<Collider>(); if(collider!=null) Destroy(collider);
                float side=i-(count-1)*.5f;
                crest.transform.position=new Vector3(bounds.center.x+side*.11f,bounds.max.y+.025f+(hero.Class==HeroClass.Ranger?.05f:0),bounds.center.z);
                crest.transform.localScale=hero.Class==HeroClass.Knight?new Vector3(.045f,.16f,.05f):hero.Class==HeroClass.Ranger?new Vector3(.035f,.19f,.05f):new Vector3(.05f,.09f+.025f*i,.04f);
                crest.GetComponent<Renderer>().sharedMaterial=accent; crest.transform.SetParent(head,true);
            }
        }
        void OnDestroy() { if(accent!=null) Destroy(accent); }
    }
}
