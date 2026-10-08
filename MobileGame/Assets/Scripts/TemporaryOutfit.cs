using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    // Original procedural wardrobe preview: adult proportions, opaque clothing, no external model dependency.
    public sealed class TemporaryOutfit : MonoBehaviour {
        readonly Dictionary<Color,Material> palette=new Dictionary<Color,Material>();
        Shader shader;
        static readonly Color[] SkinTones={new Color(.82f,.61f,.46f),new Color(.57f,.36f,.24f),new Color(.38f,.23f,.16f),new Color(.92f,.73f,.6f),new Color(.69f,.47f,.33f)};
        public static Transform Create(HeroDefinition identity, Vector3 position, Transform parent, Color cloth) {
            var root=new GameObject(identity.Name);root.transform.SetParent(parent,false);root.transform.position=position;
            root.AddComponent<TemporaryOutfit>().Build(identity,cloth);return root.transform;
        }
        GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color) {
            var obj=GameObject.CreatePrimitive(shape);obj.name=name;obj.transform.SetParent(transform,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;
            var collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var renderer=obj.GetComponent<Renderer>();
            if(shader==null) shader=renderer.sharedMaterial.shader;
            Material material;
            if(!palette.TryGetValue(color,out material)) {
                material=new Material(shader);material.color=color;
                if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.2f);
                palette.Add(color,material);
            }
            renderer.sharedMaterial=material;return obj;
        }
        void Build(HeroDefinition identity,Color cloth) {
            int index=0;foreach(char character in identity.Id) index=(index*31+character)%SkinTones.Length;
            var skin=SkinTones[index];var leather=new Color(.2f,.14f,.11f);
            bool woman=identity.Gender==HeroGender.Woman;
            bool armor=identity.Class==HeroClass.Knight || identity.Class==HeroClass.Paladin;
            bool robe=identity.Class==HeroClass.Sorceress || identity.Class==HeroClass.Cleric;
            var garment=armor?identity.Class==HeroClass.Paladin?new Color(.65f,.52f,.28f):new Color(.42f,.46f,.52f):cloth;
            // Approximately seven head-lengths tall; avoid the rejected oversized-head silhouette.
            Part("Head",PrimitiveType.Sphere,new Vector3(0,.76f,0),new Vector3(.27f,.33f,.25f),skin);
            Part("Neck",PrimitiveType.Cylinder,new Vector3(0,.53f,0),new Vector3(.12f,.08f,.12f),skin);
            Part("Torso",PrimitiveType.Sphere,new Vector3(0,.2f,0),new Vector3(woman?.4f:.46f,.62f,.25f),skin);
            Part("Waist",PrimitiveType.Cylinder,new Vector3(0,-.08f,0),new Vector3(.3f,.14f,.23f),skin);
            Part("Hips",PrimitiveType.Sphere,new Vector3(0,-.28f,0),new Vector3(.44f,.29f,.26f),skin);
            // Cropped armor/bodices leave midriff and arms visible; shorts/skirts cover the hips.
            Part(armor?"Cropped Cuirass":"Sleeveless Bodice",PrimitiveType.Sphere,new Vector3(0,woman?.29f:.2f,-.008f),new Vector3(woman?.43f:.49f,woman?.38f:.61f,.28f),garment);
            Part("Opaque Shorts",PrimitiveType.Cube,new Vector3(0,-.29f,0),new Vector3(.46f,.23f,.29f),robe?cloth:leather);
            Part("Belt",PrimitiveType.Cube,new Vector3(0,-.18f,0),new Vector3(.47f,.07f,.3f),leather);
            Part("Buckle",PrimitiveType.Cube,new Vector3(0,-.18f,-.17f),new Vector3(.09f,.07f,.03f),new Color(.7f,.57f,.3f));
            for(int side=-1;side<=1;side+=2) {
                float x=side*.3f;
                Part("Upper Arm",PrimitiveType.Capsule,new Vector3(x,.3f,0),new Vector3(.115f,.19f,.115f),skin);
                Part("Forearm",PrimitiveType.Capsule,new Vector3(x,.01f,0),new Vector3(.105f,.17f,.105f),skin);
                Part("Hand",PrimitiveType.Sphere,new Vector3(x,-.2f,0),new Vector3(.11f,.14f,.1f),skin);
                Part("Bracer",PrimitiveType.Cylinder,new Vector3(x,-.06f,0),new Vector3(.12f,.075f,.12f),armor?garment:leather);
                Part("Thigh",PrimitiveType.Capsule,new Vector3(side*.13f,-.49f,0),new Vector3(.16f,.18f,.16f),skin);
                Part("Lower Leg",PrimitiveType.Capsule,new Vector3(side*.13f,-.77f,0),new Vector3(.125f,.17f,.125f),skin);
                Part("Tall Boot",PrimitiveType.Cylinder,new Vector3(side*.13f,-.77f,0),new Vector3(.15f,.19f,.15f),leather);
                Part("Boot Foot",PrimitiveType.Cube,new Vector3(side*.13f,-.96f,-.035f),new Vector3(.16f,.07f,.27f),leather);
            }
            if(robe) {
                // Front/back panels leave side splits; opaque shorts cover the opening.
                Part("Short Robe Front",PrimitiveType.Cube,new Vector3(0,-.35f,-.17f),new Vector3(.38f,.37f,.045f),cloth);
                Part("Short Robe Back",PrimitiveType.Cube,new Vector3(0,-.35f,.17f),new Vector3(.38f,.37f,.045f),cloth);
            }
            if(armor) {
                Part("Breastplate Trim",PrimitiveType.Cube,new Vector3(0,.18f,-.15f),new Vector3(.23f,.06f,.035f),new Color(.7f,.57f,.3f));
                Part("Hip Plate Left",PrimitiveType.Cube,new Vector3(-.25f,-.29f,0),new Vector3(.055f,.23f,.27f),garment);
                Part("Hip Plate Right",PrimitiveType.Cube,new Vector3(.25f,-.29f,0),new Vector3(.055f,.23f,.27f),garment);
            }
            var hair=new Color(.12f,.08f,.06f);
            Part("Hair",PrimitiveType.Sphere,new Vector3(0,.85f,.035f),new Vector3(.285f,.18f,.265f),hair);
            if(woman) {
                Part("Hair Back",PrimitiveType.Cube,new Vector3(0,.64f,.12f),new Vector3(.28f,.32f,.08f),hair);
                Part("Hair Left",PrimitiveType.Cube,new Vector3(-.13f,.65f,.03f),new Vector3(.045f,.27f,.1f),hair);
            }
            Part("Nose",PrimitiveType.Sphere,new Vector3(0,.75f,-.13f),new Vector3(.035f,.045f,.045f),skin);
            for(int side=-1;side<=1;side+=2) Part("Eye",PrimitiveType.Sphere,new Vector3(side*.053f,.8f,-.118f),new Vector3(.015f,.012f,.013f),Color.black);
        }
        void OnDestroy() { foreach(var material in palette.Values) Destroy(material); }
    }
}
