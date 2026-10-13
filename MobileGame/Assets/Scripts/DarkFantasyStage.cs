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
        Mesh atmosphereMesh;
        Vector3[] motePositions, moteVertices;
        Camera stageCamera;
        ChapterStageDefinition theme;
        int chapter;
        static Color Tint(string hex) { Color color; ColorUtility.TryParseHtmlString(hex,out color); return color; }
        public static DarkFantasyStage Create(Transform parent, Camera camera, int chapter=0) {
            var root = new GameObject(ChapterStageDefinition.For(chapter).Name); root.transform.SetParent(parent, false);
            var stage=root.AddComponent<DarkFantasyStage>(); stage.chapter=chapter; stage.theme=ChapterStageDefinition.For(chapter); stage.Build(camera); return stage;
        }
        public static void Frame(Camera camera) {
            camera.transform.position = CameraPosition; camera.transform.LookAt(CameraTarget);
            camera.fieldOfView = 43; camera.nearClipPlane = .1f; camera.farClipPlane = 60;
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
            if (asset != null) {
                var themed=new Material(asset); ownedMaterials.Add(themed);
                if(name=="Stone") themed.color=Tint(theme.Stone);
                if(name=="PaleStone") themed.color=Tint(theme.Accent);
                if(name=="Ember") { themed.color=Tint(theme.Flame); themed.EnableKeyword("_EMISSION"); themed.SetColor("_EmissionColor",Tint(theme.Flame)*1.5f); }
                return themed;
            }
            Debug.LogWarning("Dark fantasy material missing: " + name + ". Run Ashlight > Prepare Mobile Project.");
            var fallback = new Material(Shader.Find("Standard")); fallback.color = name=="PaleStone"?Tint(theme.Accent):name=="Ember"?Tint(theme.Flame):Tint(theme.Stone);
            ownedMaterials.Add(fallback); return fallback;
        }
        void Build(Camera camera) {
            stageCamera=camera;
            Frame(camera); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Tint(theme.Sky);
            camera.allowHDR = false; camera.allowMSAA = true;
            // One shadow-casting sun plus two small, shadowless torch lights.
            QualitySettings.pixelLightCount = 3; QualitySettings.shadowDistance = 18;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(Tint(theme.Moon),Color.black,.64f);
            RenderSettings.ambientEquatorColor = new Color(.10f,.12f,.16f);
            RenderSettings.ambientGroundColor = new Color(.035f,.04f,.05f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = camera.backgroundColor; RenderSettings.fogDensity = theme.Fog;
            var sun = new GameObject("Cold moonlight").AddComponent<Light>(); sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional; sun.color = Tint(theme.Moon); sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .7f; sun.shadowBias = .045f;
            sun.transform.localRotation = Quaternion.Euler(40,-28,0);
            Stone(new Vector3(0,-.20f,0),new Vector3(14,.25f,11));
            // Deterministic flagstones: no random state shared with summon rolls.
            var random = new System.Random(2401+chapter*71);
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
                light.range = 6; light.color = Tint(theme.Flame); light.intensity = 2.7f; light.shadows = LightShadows.None;
                torches[i] = light;
            }
            ChapterLandmarks();
            Atmosphere();
            foreach (var batch in batches) {
                var mesh = new Mesh { name = "Courtyard " + batch.Key, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(batch.Value.ToArray(),true,true); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
                var obj = new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(transform,false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = Material(batch.Key);
                if (batch.Key == "Ember") obj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            batches.Clear();
        }
        void ChapterLandmarks() {
            // Landmarks stay at the perimeter, outside party/boss movement lanes.
            if(chapter==1) {
                for(int side=-1;side<=1;side+=2) {
                    for(int i=0;i<5;i++) {
                        Piece("PaleStone",PrimitiveType.Sphere,new Vector3(side*4.9f,.07f,-2+i*1.5f),new Vector3(1.4f,.35f,1.9f));
                        Stone(new Vector3(side*5.5f,1.2f+i*.14f,1+i*.7f),new Vector3(.5f,2.6f,.7f),true,new Vector3(0,25+i*11,side*18));
                    }
                }
            } else if(chapter==2) {
                for(int side=-1;side<=1;side+=2) {
                    Piece("Ember",PrimitiveType.Cube,new Vector3(side*4.8f,-.035f,0),new Vector3(.24f,.035f,7));
                    Stone(new Vector3(side*4.2f,2.5f,4.9f),new Vector3(1.2f,5.1f,1.2f));
                    for(int i=0;i<4;i++) Piece("Ember",PrimitiveType.Cube,new Vector3(side*4.2f,.6f+i*.85f,4.24f),new Vector3(.55f,.13f,.025f));
                }
            } else if(chapter==3) {
                for(int side=-1;side<=1;side+=2) for(int i=0;i<3;i++) {
                    Stone(new Vector3(side*(4.2f+i*.4f),1.8f+i*.6f,3.7f+i*.6f),new Vector3(.6f,3.6f+i*1.2f,.6f),true,new Vector3(0,45,0));
                    Piece("Ember",PrimitiveType.Sphere,new Vector3(side*(4.2f+i*.4f),3.8f+i*1.2f,3.7f+i*.6f),Vector3.one*.28f);
                }
            } else if(chapter==4) {
                for(int side=-1;side<=1;side+=2) for(int i=0;i<4;i++) {
                    Piece("Leather",PrimitiveType.Cylinder,new Vector3(side*(4.8f-i*.13f),.8f+i*.15f,-1+i*1.6f),new Vector3(.22f,1.25f,.22f),new Vector3(20,0,side*(22+i*5)));
                    for(int j=0;j<3;j++) Piece("Ember",PrimitiveType.Sphere,new Vector3(side*(4.6f+j*.25f),1.2f+j*.35f,-1+i*1.6f),new Vector3(.15f,.2f,.15f));
                    Piece("Wool",PrimitiveType.Sphere,new Vector3(side*4.7f,.2f,-1+i*1.6f),new Vector3(.9f,.3f,.8f));
                }
            } else if(chapter==5) {
                for(int i=0;i<4;i++) Stone(new Vector3(0,.13f+i*.12f,5.25f+i*.3f),new Vector3(3.1f-i*.4f,.24f,1.7f));
                Stone(new Vector3(0,1.15f,6.1f),new Vector3(1.25f,1.2f,.6f),true);
                Stone(new Vector3(0,2.35f,6.4f),new Vector3(1.5f,2.6f,.35f),true);
                for(int i=0;i<20;i++) {
                    float angle=i*Mathf.PI*2/20;
                    Piece("Ember",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*1.1f,4.45f+Mathf.Sin(angle)*1.1f,6.3f),new Vector3(.18f,.18f,.10f));
                }
            } else if(chapter>=6) {
                int motif=(chapter-6)%6;
                for(int side=-1;side<=1;side+=2) for(int i=0;i<3;i++) {
                    float x=side*(4.8f+i*.2f), z=-1+i*2.1f;
                    Stone(new Vector3(x,1.3f,z),new Vector3(.5f,2.6f+motif*.12f,.6f),true,new Vector3(0,motif*17,side*(motif==3?20:0)));
                    Piece("Ember",PrimitiveType.Sphere,new Vector3(x,2.8f+motif*.12f,z),Vector3.one*(motif==4?.16f:.25f));
                    if(motif==1 || motif==2) Stone(new Vector3(x-side*.55f,3.05f,z),new Vector3(1.6f,.35f,.55f));
                }
                if(motif==2 || motif==5) for(int i=0;i<16;i++) {
                    float angle=i*Mathf.PI*2/16;
                    Piece("Ember",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*1.6f,3.9f+Mathf.Sin(angle)*1.6f,6.1f),Vector3.one*.18f);
                }
            }
        }
        void Atmosphere() {
            var obj=new GameObject("Chapter atmosphere",typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(transform,false);
            int count=chapter==1?40:24;
            motePositions=new Vector3[count]; moteVertices=new Vector3[count*4];
            var triangles=new int[count*6]; var uv=new Vector2[count*4]; var colors=new Color[count*4];
            var random=new System.Random(6101+chapter);
            for(int i=0;i<count;i++) {
                motePositions[i]=new Vector3((float)random.NextDouble()*11-5.5f,(float)random.NextDouble()*4.5f,(float)random.NextDouble()*8-3);
                int v=i*4,t=i*6;
                triangles[t]=v; triangles[t+1]=v+1; triangles[t+2]=v+2; triangles[t+3]=v; triangles[t+4]=v+2; triangles[t+5]=v+3;
                uv[v]=Vector2.zero; uv[v+1]=Vector2.right; uv[v+2]=Vector2.one; uv[v+3]=Vector2.up;
                for(int j=0;j<4;j++) colors[v+j]=Color.white;
            }
            atmosphereMesh=new Mesh { name="Atmosphere motes" }; atmosphereMesh.MarkDynamic();
            atmosphereMesh.vertices=moteVertices; atmosphereMesh.triangles=triangles; atmosphereMesh.uv=uv; atmosphereMesh.colors=colors;
            atmosphereMesh.bounds=new Bounds(new Vector3(0,2.25f,1),new Vector3(12,6,9)); ownedMeshes.Add(atmosphereMesh);
            obj.GetComponent<MeshFilter>().sharedMesh=atmosphereMesh;
            var template=Resources.Load<Material>("DarkFantasy/Materials/Atmosphere");
            var material=template!=null?new Material(template):new Material(Shader.Find("Sprites/Default"));
            var color=chapter==1?Tint(theme.Accent):Tint(theme.Flame); color.a=.35f; material.color=color;
            ownedMaterials.Add(material); var renderer=obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            UpdateAtmosphere(0);
        }
        void UpdateAtmosphere(float delta) {
            if(atmosphereMesh==null || stageCamera==null) return;
            float size=chapter==1?.022f:.014f;
            Vector3 right=stageCamera.transform.right*size, up=stageCamera.transform.up*size;
            for(int i=0;i<motePositions.Length;i++) {
                var point=motePositions[i]; point.y+=delta*(chapter==1?-.35f:.09f);
                point.x+=delta*Mathf.Sin(Time.time*.5f+i)*.04f;
                if(point.y<0) point.y=4.5f; if(point.y>4.5f) point.y=0;
                motePositions[i]=point; int v=i*4;
                moteVertices[v]=point-right-up; moteVertices[v+1]=point+right-up; moteVertices[v+2]=point+right+up; moteVertices[v+3]=point-right+up;
            }
            atmosphereMesh.vertices=moteVertices;
        }
        void Update() {
            UpdateAtmosphere(Time.deltaTime);
            if (torches == null) return;
            for (int i = 0; i < torches.Length; i++) torches[i].intensity = 2.7f + Mathf.Sin(Time.time*3.1f+i*1.6f)*.08f;
        }
        void OnDestroy() {
            foreach (var mesh in ownedMeshes) Destroy(mesh);
            foreach (var material in ownedMaterials) Destroy(material);
        }
    }
}
