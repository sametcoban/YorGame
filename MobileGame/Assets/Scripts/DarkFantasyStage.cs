using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ashlight {
    public sealed class DarkFantasyStage : MonoBehaviour {
        public static readonly Vector3 CameraPosition = new Vector3(.3f, 3.4f, -9.6f);
        public static readonly Vector3 CameraTarget = new Vector3(0, 1.1f, 0);
        readonly Dictionary<string, List<CombineInstance>> batches = new Dictionary<string, List<CombineInstance>>();
        readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        readonly List<Material> ownedMaterials = new List<Material>();
        Light[] torches;
        public static void Create(Transform parent, Camera camera) {
            var root = new GameObject("The Lantern Courtyard"); root.transform.SetParent(parent, false);
            root.AddComponent<DarkFantasyStage>().Build(camera);
        }
        public static void Frame(Camera camera) {
            camera.transform.position = CameraPosition; camera.transform.LookAt(CameraTarget);
            camera.fieldOfView = 39; camera.nearClipPlane = .1f; camera.farClipPlane = 60;
        }
        Mesh Primitive(PrimitiveType kind) {
            Mesh mesh;
            if (primitives.TryGetValue(kind, out mesh)) return mesh;
            var temporary = GameObject.CreatePrimitive(kind);
            temporary.SetActive(false); mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            primitives.Add(kind, mesh); Destroy(temporary); return mesh;
        }
        void Piece(string material, PrimitiveType kind, Vector3 position, Vector3 scale, Vector3 rotation = default(Vector3)) {
            List<CombineInstance> batch;
            if (!batches.TryGetValue(material, out batch)) { batch = new List<CombineInstance>(); batches.Add(material, batch); }
            batch.Add(new CombineInstance { mesh = Primitive(kind), transform = Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale) });
        }
        void Stone(Vector3 position, Vector3 size, bool pale = false, Vector3 rotation = default(Vector3)) {
            Piece(pale ? "PaleStone" : "Stone", PrimitiveType.Cube, position, size, rotation);
        }
        Material Material(string name) {
            var asset = Resources.Load<Material>("DarkFantasy/Materials/" + name);
            if (asset != null) return asset;
            Debug.LogWarning("Dark fantasy material missing: " + name + ". Run Ashlight > Prepare Mobile Project.");
            var fallback = new Material(Shader.Find("Standard")); fallback.color = new Color(.14f,.16f,.19f);
            ownedMaterials.Add(fallback); return fallback;
        }
        void Build(Camera camera) {
            Frame(camera); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.052f);
            camera.allowHDR = false; camera.allowMSAA = true;
            // One shadow-casting sun plus two small, shadowless torch lights.
            QualitySettings.pixelLightCount = 3; QualitySettings.shadowDistance = 18;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.24f,.29f,.38f);
            RenderSettings.ambientEquatorColor = new Color(.10f,.12f,.16f);
            RenderSettings.ambientGroundColor = new Color(.035f,.04f,.05f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = camera.backgroundColor; RenderSettings.fogDensity = .024f;
            var sun = new GameObject("Cold moonlight").AddComponent<Light>(); sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional; sun.color = new Color(.70f,.79f,.94f); sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .7f; sun.shadowBias = .045f;
            sun.transform.localRotation = Quaternion.Euler(40,-28,0);
            Stone(new Vector3(0,-.20f,0),new Vector3(14,.25f,11));
            // Deterministic flagstones: no random state shared with summon rolls.
            var random = new System.Random(2401);
            for (int x = 0; x < 12; x++) for (int z = 0; z < 10; z++) {
                float yaw = (float)(random.NextDouble()-.5)*2;
                Stone(new Vector3((x-5.5f)*.86f,-.07f,(z-4.5f)*.86f),new Vector3(.82f,.14f,.82f), (x+z)%4==0, new Vector3(0,yaw,0));
            }
            // Broken side walls leave the actors and camera unobstructed.
            for (int side = -1; side <= 1; side += 2) {
                for (int z = 0; z < 7; z++) for (int row = 0; row < (z < 3 ? 2 : 4); row++)
                    Stone(new Vector3(side*5.35f,.25f+row*.52f,-1.1f+z*.84f),new Vector3(.5f,.48f,.8f),(z+row)%3==0);
                for (int row = 0; row < 7; row++)
                    Stone(new Vector3(side*1.75f,.25f+row*.48f,4.25f),new Vector3(.6f,.44f,.7f),row%3==0);
                Stone(new Vector3(side*1.75f,.12f,4.25f),new Vector3(.95f,.24f,.95f),true);
                Stone(new Vector3(side*1.75f,3.6f,4.25f),new Vector3(.8f,.2f,.8f),true);
            }
            for (int i = 0; i < 13; i++) {
                float angle = i*Mathf.PI/12;
                Stone(new Vector3(Mathf.Cos(angle)*1.55f,2.45f+Mathf.Sin(angle)*1.55f,4.25f),new Vector3(.42f,.48f,.7f),i%3==0,new Vector3(0,0,angle*Mathf.Rad2Deg-90));
            }
            for (int i = -5; i <= 5; i++) Piece("Iron",PrimitiveType.Cube,new Vector3(i*.24f,1.55f,4.48f),new Vector3(.035f,2.8f,.05f));
            Piece("Iron",PrimitiveType.Cube,new Vector3(0,1.5f,4.48f),new Vector3(2.65f,.05f,.07f));
            // Rubble around the perimeter, outside the battle movement area.
            for (int i = 0; i < 22; i++) {
                int side = i%2==0 ? -1 : 1;
                Stone(new Vector3(side*(4.3f+(float)random.NextDouble()),.1f,-2.3f+(float)random.NextDouble()*5.5f),
                    new Vector3(.32f,.22f,.42f),i%3==0,new Vector3(0,(float)random.NextDouble()*90,12));
            }
            torches = new Light[2];
            for (int i = 0; i < 2; i++) {
                float x = i==0 ? -3.6f : 3.6f;
                Stone(new Vector3(x,.25f,3.2f),new Vector3(.5f,.5f,.5f),true);
                Piece("Iron",PrimitiveType.Cylinder,new Vector3(x,1.22f,3.2f),new Vector3(.09f,.72f,.09f));
                Piece("Brass",PrimitiveType.Cylinder,new Vector3(x,1.92f,3.2f),new Vector3(.36f,.08f,.36f));
                Piece("Ember",PrimitiveType.Sphere,new Vector3(x,2.12f,3.2f),new Vector3(.2f,.42f,.2f));
                var light = new GameObject("Torch " + (i+1)).AddComponent<Light>(); light.transform.SetParent(transform,false);
                light.transform.localPosition = new Vector3(x,2.2f,3); light.type = LightType.Point;
                light.range = 6; light.color = new Color(1,.53f,.22f); light.intensity = 2.7f; light.shadows = LightShadows.None;
                torches[i] = light;
            }
            foreach (var batch in batches) {
                var mesh = new Mesh { name = "Courtyard " + batch.Key, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(batch.Value.ToArray(),true,true); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
                var obj = new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(transform,false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = Material(batch.Key);
                if (batch.Key == "Ember") obj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            batches.Clear();
        }
        void Update() {
            if (torches == null) return;
            for (int i = 0; i < torches.Length; i++) torches[i].intensity = 2.7f + Mathf.Sin(Time.time*3.1f+i*1.6f)*.08f;
        }
        void OnDestroy() {
            foreach (var mesh in ownedMeshes) Destroy(mesh);
            foreach (var material in ownedMaterials) Destroy(material);
        }
    }
}
