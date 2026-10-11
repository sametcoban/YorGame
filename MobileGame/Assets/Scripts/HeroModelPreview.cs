using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    // Isolated model/camera, active only while the details panel is visible.
    public sealed class HeroModelPreview : MonoBehaviour {
        GameObject stage, model;
        Camera view;
        RenderTexture target;
        const int PreviewLayer=31;
        public bool Show(HeroDefinition hero) {
            Clear();
            var prefab=Resources.Load<GameObject>("Heroes/NamedVariants/"+hero.Id) ?? Resources.Load<GameObject>("Heroes/Named/"+hero.Id) ??
                Resources.Load<GameObject>("Heroes/DarkFantasy/"+hero.Class+"_"+hero.Gender) ??
                Resources.Load<GameObject>("Heroes/"+hero.Class+"_"+hero.Gender);
            if(prefab==null) return false;
            stage=new GameObject("Hero Preview Stage"); stage.transform.position=Vector3.one*10000;
            model=Instantiate(prefab,stage.transform); model.transform.localPosition=Vector3.zero;
            model.transform.localRotation=Quaternion.Euler(0,35,0);
            if(model.GetComponentsInChildren<Renderer>().Length==0) { Clear(); return false; }
            foreach(var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=PreviewLayer;
            foreach(var collider in model.GetComponentsInChildren<Collider>()) collider.enabled=false;
            foreach(var animator in model.GetComponentsInChildren<Animator>()) animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var cameraRoot=new GameObject("Hero Preview Camera"); cameraRoot.transform.SetParent(stage.transform,false);
            view=cameraRoot.AddComponent<Camera>(); view.cullingMask=1<<PreviewLayer;
            view.clearFlags=CameraClearFlags.SolidColor; view.backgroundColor=new Color(.035f,.045f,.065f);
            view.orthographic=true; view.aspect=.75f; view.nearClipPlane=.01f; view.farClipPlane=30;
            target=new RenderTexture(384,512,16); target.Create(); view.targetTexture=target;
            GetComponent<RawImage>().texture=target; GetComponent<RawImage>().color=Color.white;
            var lightRoot=new GameObject("Hero Preview Light"); lightRoot.transform.SetParent(stage.transform,false);
            lightRoot.transform.rotation=Quaternion.Euler(35,-25,0);
            var light=lightRoot.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f;
            light.cullingMask=1<<PreviewLayer; light.shadows=LightShadows.None;
            Frame(); return true;
        }
        public void Rotate(float degrees) { if(model!=null) { model.transform.Rotate(0,degrees,0); Frame(); } }
        void Frame() {
            var renderers=model.GetComponentsInChildren<Renderer>(); if(renderers.Length==0) return;
            var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            view.orthographicSize=Mathf.Max(.5f,Mathf.Max(bounds.extents.y,bounds.extents.x/.75f)*1.15f);
            view.transform.position=bounds.center+Vector3.back*(bounds.size.z+5);
            view.transform.LookAt(bounds.center);
        }
        void Clear() {
            if(view!=null) view.targetTexture=null;
            var image=GetComponent<RawImage>(); if(image!=null) { image.texture=null; image.color=new Color(.035f,.045f,.065f); }
            if(stage!=null) { stage.SetActive(false); Destroy(stage); }
            if(target!=null) { target.Release(); Destroy(target); }
            stage=model=null; target=null; view=null;
        }
        void OnDisable() { Clear(); }
        void OnDestroy() { Clear(); }
    }
}
