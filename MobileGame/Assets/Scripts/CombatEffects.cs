using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    // Reuses up to 48 simple effect objects. No downloaded art or particle dependencies.
    public sealed class CombatEffects : MonoBehaviour {
        enum Mode { Spark, Ring, GroundRing, Beam, Text, Slash, Charge, Projectile, Sigil, Shield, Cleave, Arrow, Meteor }
        sealed class Effect {
            public GameObject Root;
            public Renderer Mesh;
            public LineRenderer Line;
            public TextMesh Text;
            public Mode Kind;
            public Element Element;
            public float Age, Duration, Size;
            public Vector3 Start, Velocity, Destination;
        }
        readonly List<Effect> pool = new List<Effect>();
        readonly Dictionary<Element, Material> materials = new Dictionary<Element, Material>();
        readonly LineRenderer[] targetMarkers=new LineRenderer[Battle.MaxPartySize];
        Material targetMaterial;
        Camera view;
        Font font;
        Vector3 cameraOffset;
        float shakeLeft;
        const int Limit = 48;
        public void Initialize(Camera camera) {
            view = camera;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var template = Resources.Load<Material>("CombatEffects");
            var shader = template != null ? template.shader : Shader.Find("Unlit/Color");
            if (shader == null) { Debug.LogError("Combat effect shader is missing. Run Ashlight > Prepare Mobile Project before building."); enabled = false; return; }
            foreach (Element element in System.Enum.GetValues(typeof(Element))) {
                var material = new Material(shader); material.color = Tint(element); materials.Add(element, material);
            }
            targetMaterial=new Material(shader); targetMaterial.color=new Color(1,.12f,.08f);
            for(int i=0;i<targetMarkers.Length;i++) {
                var marker=new GameObject("Enemy Target "+i); marker.transform.SetParent(transform,false);
                var line=marker.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.positionCount=32;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows=false;
                targetMarkers[i]=line; marker.SetActive(false);
            }
        }
        public static Color Tint(Element element) {
            switch (element) {
                case Element.Fire: return new Color(1,.35f,.06f);
                case Element.Cold: return new Color(.35f,.85f,1);
                case Element.Poison: return new Color(.45f,1,.2f);
                case Element.Lightning: return new Color(.8f,.5f,1);
                case Element.Light: return new Color(1,.9f,.4f);
                default: return new Color(.9f,.95f,1);
            }
        }
        Effect Get(Mode mode, Vector3 position, Element element, float duration, float size) {
            if (!enabled || !materials.ContainsKey(element)) return null;
            Effect effect = null;
            foreach (var item in pool) if (!item.Root.activeSelf) { effect = item; break; }
            if(effect==null && pool.Count>=Limit) {
                if(mode!=Mode.Text) return null;
                // Keep damage/healing readable when several area effects fill the pool.
                foreach(var item in pool) if(item.Kind==Mode.Spark && (effect==null || item.Age/item.Duration>effect.Age/effect.Duration)) effect=item;
                if(effect==null) return null;
            }
            if (effect == null) {
                effect = new Effect();
                effect.Root = new GameObject("Combat Effect"); effect.Root.transform.SetParent(transform, false);
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere); mesh.transform.SetParent(effect.Root.transform, false);
                var collider = mesh.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
                effect.Mesh = mesh.GetComponent<Renderer>();
                effect.Mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; effect.Mesh.receiveShadows = false;
                effect.Line = effect.Root.AddComponent<LineRenderer>();
                effect.Line.useWorldSpace = false; effect.Line.numCapVertices = 2;
                var text = new GameObject("Floating Text"); text.transform.SetParent(effect.Root.transform, false);
                effect.Text = text.AddComponent<TextMesh>(); effect.Text.font = font;
                effect.Text.fontSize = 48; effect.Text.characterSize = .04f; effect.Text.anchor = TextAnchor.MiddleCenter;
                text.GetComponent<Renderer>().sharedMaterial = font.material;
                pool.Add(effect);
            }
            effect.Root.SetActive(true); effect.Root.transform.position = position; effect.Root.transform.localScale = Vector3.one;
            effect.Root.transform.rotation = Quaternion.identity;
            effect.Kind = mode; effect.Element = element; effect.Start = position; effect.Age = 0; effect.Duration = duration; effect.Size = size; effect.Velocity = Vector3.zero;
            effect.Mesh.gameObject.SetActive(mode == Mode.Spark || mode == Mode.Projectile || mode == Mode.Arrow || mode == Mode.Meteor);
            effect.Text.gameObject.SetActive(mode == Mode.Text);
            effect.Line.enabled = mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Beam || mode == Mode.Slash || mode == Mode.Charge || mode == Mode.Sigil || mode == Mode.Shield || mode == Mode.Cleave;
            effect.Mesh.sharedMaterial = materials[element]; effect.Line.sharedMaterial = materials[element]; effect.Text.color = Tint(element);
            effect.Line.loop = mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge || mode == Mode.Sigil;
            effect.Line.startWidth = effect.Line.endWidth = size;
            effect.Line.positionCount = (mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge || mode == Mode.Slash || mode == Mode.Shield || mode == Mode.Cleave) ? 24 : mode == Mode.Sigil ? 10 : 2;
            if (mode == Mode.Spark || mode == Mode.Projectile || mode == Mode.Arrow || mode == Mode.Meteor) effect.Mesh.transform.localScale = Vector3.one * size;
            return effect;
        }
        void Spark(Vector3 position, Element element, Vector3 velocity, float size, float duration) {
            var effect = Get(Mode.Spark, position, element, duration, size); if (effect != null) effect.Velocity = velocity;
        }
        public void TargetWarning(int heroIndex,Vector3 position,bool active,bool actualTarget) {
            if(heroIndex<0 || heroIndex>=targetMarkers.Length || targetMarkers[heroIndex]==null) return;
            var line=targetMarkers[heroIndex]; line.gameObject.SetActive(active); if(!active) return;
            line.transform.position=new Vector3(position.x,.055f,position.z);
            line.sharedMaterial=actualTarget?targetMaterial:materials[Element.Fire];
            float pulse=(Mathf.Sin(Time.time*7)+1)*.5f;
            float radius=(actualTarget?.62f:.5f)+pulse*.06f;
            line.startWidth=line.endWidth=actualTarget?.07f+pulse*.025f:.025f;
            for(int i=0;i<32;i++) {
                float angle=i*Mathf.PI*2/32; line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
            }
        }
        public void Floating(Vector3 position, string text, Element element) {
            // Give simultaneous damage, status and defense messages their own vertical lanes.
            int lane = 0;
            foreach (var item in pool)
                if (item.Root.activeSelf && item.Kind == Mode.Text && item.Age < .7f &&
                    (new Vector2(item.Start.x-position.x,item.Start.z-position.z)).sqrMagnitude < .6f) lane++;
            var effect = Get(Mode.Text, position + Vector3.up * (1.2f + lane*.38f), element, 1.15f, 1);
            if (effect != null) { effect.Text.text = text; effect.Text.characterSize = .045f; effect.Velocity = Vector3.up * .65f; }
        }
        public void Ring(Vector3 position, Element element, float size = .06f) { Get(Mode.Ring, position, element, .45f, size); }
        public void Attack(Vector3 from, Vector3 to, Element element, int damage, float multiplier) {
            if (damage <= 0) return;
            var beam = element == Element.Physical ? null : Get(Mode.Beam, from, element, .18f, element == Element.Lightning ? .09f : .05f);
            if (element == Element.Physical) Get(Mode.Slash,to,element,.25f,.09f);
            if (beam != null) {
                beam.Line.SetPosition(0, Vector3.zero); beam.Line.SetPosition(1, to - from);
                if (element == Element.Lightning) {
                    beam.Line.positionCount = 7;
                    for (int i = 0; i < 7; i++) beam.Line.SetPosition(i, Vector3.Lerp(Vector3.zero,to-from,i/6f) + (i == 0 || i == 6 ? Vector3.zero : Vector3.up * (i%2==0?.18f:-.18f)));
                }
            }
            Ring(to, element);
            int count = element == Element.Fire ? 10 : 7;
            for (int i = 0; i < count; i++) {
                float angle = i * Mathf.PI * 2 / count;
                var velocity = new Vector3(Mathf.Cos(angle), element == Element.Poison ? .4f : 1, Mathf.Sin(angle)) * 1.8f;
                Spark(to, element, velocity, element == Element.Cold ? .13f : .09f, .5f);
            }
            Floating(to, "−" + damage + (multiplier > 1 ? " WEAK!" : multiplier < 1 ? " RESIST" : ""), element);
            shakeLeft = Mathf.Max(shakeLeft,.12f);
        }
        public void Windup(Vector3 from, Vector3 to, Element element, float duration, bool ranged) {
            if (ranged) {
                var shot = Get(Mode.Projectile,from,element,Mathf.Max(.05f,duration),.14f);
                if (shot != null) shot.Destination = to;
            } else Get(Mode.Charge,from,element,Mathf.Max(.05f,duration),.055f);
        }
        public void UltimateCharge(Vector3 position, Element element, float duration) {
            Get(Mode.Charge,position,element,Mathf.Max(.05f,duration),.12f);
            Get(Mode.GroundRing,new Vector3(position.x,.055f,position.z),element,duration,.12f);
        }
        public void UltimateImpact(Vector3 position, Element element, bool area) {
            Get(Mode.GroundRing,new Vector3(position.x,.055f,position.z),element,.7f,area?.14f:.09f);
            Get(Mode.Ring,position,element,.55f,.15f);
            if (!area) Get(Mode.Slash,position,element,.35f,.16f);
            shakeLeft = Mathf.Max(shakeLeft,area?.20f:.24f);
        }
        public void SkillWindup(SkillPresentation profile,Vector3 actor,Element element,float duration) {
            if(profile.Casting) Get(Mode.Charge,actor,element,duration,.065f);
            if(profile.Style==SkillEffectStyle.Heal || profile.Style==SkillEffectStyle.HolyStrike)
                Get(Mode.Sigil,new Vector3(actor.x,.06f,actor.z),Element.Light,duration,.05f);
            if(profile.Style==SkillEffectStyle.Barrier) Get(Mode.Shield,actor,element,duration,.08f);
            if(profile.Style==SkillEffectStyle.Poison) {
                for(int i=0;i<4;i++) Spark(actor,element,new Vector3((i-1.5f)*.3f,.7f,0),.2f,duration);
            }
        }
        public void SkillRelease(SkillPresentation profile,Vector3 from,Vector3 to,Element element,float duration) {
            var style=profile.Style;
            if(style!=SkillEffectStyle.Arrow && style!=SkillEffectStyle.Volley && style!=SkillEffectStyle.Lance && style!=SkillEffectStyle.Meteor) return;
            int count=style==SkillEffectStyle.Volley?profile.Projectiles:1;
            for(int i=0;i<count;i++) {
                var mode=style==SkillEffectStyle.Meteor?Mode.Meteor:Mode.Arrow;
                Vector3 offset=Vector3.forward*((i-(count-1)*.5f)*.18f);
                var shot=Get(mode,style==SkillEffectStyle.Meteor?to+Vector3.up*2.5f:from+offset,element,duration,style==SkillEffectStyle.Lance?.16f:style==SkillEffectStyle.Meteor?.3f:.08f);
                if(shot!=null) shot.Destination=to+offset;
            }
        }
        public void SkillSupport(SkillEffectStyle style,Vector3 position,Element element,bool healing) {
            if(healing) Get(Mode.Sigil,new Vector3(position.x,.06f,position.z),Element.Light,.7f,.07f);
            else Get(Mode.Shield,position,element,.6f,.08f);
        }
        public void SkillImpact(SkillEffectStyle style,Vector3 from,Vector3 to,Element element,bool ultimate=false) {
            float size=ultimate?.14f:.08f;
            switch(style) {
                case SkillEffectStyle.Cleave: Get(Mode.Cleave,to,element,.4f,size); break;
                case SkillEffectStyle.HeavyStrike: case SkillEffectStyle.ShieldBash:
                    Get(Mode.GroundRing,new Vector3(to.x,.06f,to.z),element,.5f,size); shakeLeft=Mathf.Max(shakeLeft,.16f); break;
                case SkillEffectStyle.HolyStrike:
                    Get(Mode.Sigil,new Vector3(to.x,.06f,to.z),element,.55f,size);
                    var pillar=Get(Mode.Beam,to,element,.35f,size);
                    if(pillar!=null) { pillar.Line.SetPosition(0,Vector3.down*.8f); pillar.Line.SetPosition(1,Vector3.up*1.7f); } break;
                case SkillEffectStyle.Burst: case SkillEffectStyle.Meteor:
                    Get(Mode.Ring,to,element,.55f,size); Get(Mode.GroundRing,new Vector3(to.x,.06f,to.z),element,.55f,size); break;
                case SkillEffectStyle.Drain:
                    var drain=Get(Mode.Beam,to,element,.4f,.06f);
                    if(drain!=null) { drain.Line.SetPosition(0,Vector3.zero); drain.Line.SetPosition(1,from-to); }
                    for(int i=0;i<3;i++) Spark(to+Vector3.up*i*.15f,element,(from-to)*2,.08f,.4f); break;
                case SkillEffectStyle.Shadow: Get(Mode.Slash,to,element,.3f,size); Get(Mode.Slash,to+Vector3.up*.25f,element,.3f,size*.6f); break;
                case SkillEffectStyle.Poison:
                    for(int i=0;i<3;i++) Spark(to,element,new Vector3(i-1,.45f,0),.24f,.65f); break;
            }
        }
        public void BossImpact(Vector3 position, Element element) {
            var ground = new Vector3(position.x,.055f,position.z);
            Get(Mode.GroundRing,ground,element,.6f,.09f);
            for (int i = 0; i < 6; i++) {
                float angle = i*Mathf.PI/3;
                Spark(ground,element,new Vector3(Mathf.Cos(angle),.7f,Mathf.Sin(angle))*1.8f,.11f,.55f);
            }
            shakeLeft = Mathf.Max(shakeLeft,.18f);
        }
        public void Heal(Vector3 position, int amount) {
            Ring(position, Element.Light);
            Floating(position, "+" + amount, Element.Light);
            for (int i = 0; i < 5; i++) Spark(position + Vector3.right*(i-2)*.15f,Element.Light,Vector3.up*1.8f,.09f,.6f);
        }
        public void Defense(Vector3 position, bool parry) {
            Ring(position, parry ? Element.Light : Element.Cold, .08f);
            Floating(position, parry ? "PARRY" : "DODGE", parry ? Element.Light : Element.Cold);
        }
        public void Guard(Vector3 position) { Ring(position, Element.Light, .12f); Floating(position,"GUARD",Element.Light); }
        void Update() {
            foreach (var effect in pool) {
                if (!effect.Root.activeSelf) continue;
                effect.Age += Time.deltaTime;
                float progress = effect.Age / effect.Duration;
                if (progress >= 1) { effect.Root.SetActive(false); continue; }
                effect.Root.transform.position = effect.Start + effect.Velocity * effect.Age;
                if (effect.Kind == Mode.Spark) effect.Mesh.transform.localScale = (effect.Element == Element.Cold ? new Vector3(.5f,1.8f,.5f) : Vector3.one) * effect.Size * (1-progress);
                if(effect.Kind==Mode.Arrow || effect.Kind==Mode.Meteor) {
                    effect.Root.transform.position=Vector3.Lerp(effect.Start,effect.Destination,progress);
                    if(effect.Kind==Mode.Arrow) {
                        var direction=effect.Destination-effect.Start;
                        if(direction.sqrMagnitude>.001f) effect.Root.transform.rotation=Quaternion.LookRotation(direction);
                        effect.Mesh.transform.localScale=new Vector3(effect.Size,effect.Size,effect.Size*5);
                    } else effect.Mesh.transform.localScale=Vector3.one*effect.Size*(1+progress*.4f);
                }
                if(effect.Kind==Mode.Sigil) {
                    float radius=.35f+progress*.65f;
                    for(int i=0;i<10;i++) { float angle=i*Mathf.PI*2/10; float r=radius*(i%2==0?1:.45f); effect.Line.SetPosition(i,new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r)); }
                    effect.Line.startWidth=effect.Line.endWidth=effect.Size*(1-progress);
                }
                if (effect.Kind == Mode.Projectile) {
                    effect.Root.transform.position = Vector3.Lerp(effect.Start,effect.Destination,progress) + Vector3.up*Mathf.Sin(progress*Mathf.PI)*.3f;
                }
                if (effect.Kind == Mode.Slash || effect.Kind == Mode.Charge || effect.Kind == Mode.Shield || effect.Kind == Mode.Cleave) {
                    effect.Root.transform.rotation = view.transform.rotation;
                    float radius = effect.Kind == Mode.Charge ? .65f*(1-progress)+.15f : effect.Kind==Mode.Shield?.8f:effect.Kind==Mode.Cleave?.7f+progress: .35f+progress*.7f;
                    for (int i=0;i<24;i++) {
                        float angle = effect.Kind == Mode.Charge ? i*Mathf.PI*2/24 : (-.65f+i/23f*1.3f+progress*.7f)*Mathf.PI;
                        effect.Line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
                    }
                    effect.Line.startWidth=effect.Line.endWidth=effect.Size*(1-progress*.8f);
                }
                if (effect.Kind == Mode.Text) {
                    effect.Root.transform.rotation = view.transform.rotation;
                    var color = effect.Text.color; color.a = progress < .55f ? 1 : (1-progress)/.45f; effect.Text.color = color;
                }
                if (effect.Kind == Mode.Ring || effect.Kind == Mode.GroundRing) {
                    float radius = .15f + progress * .85f;
                    for (int i = 0; i < 24; i++) {
                        float angle = i*Mathf.PI*2/24;
                        effect.Line.SetPosition(i,effect.Kind == Mode.GroundRing ? new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius) : new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
                    }
                    effect.Line.startWidth = effect.Line.endWidth = effect.Size * (1-progress);
                }
            }
        }
        void LateUpdate() {
            if (view == null) return;
            view.transform.position -= cameraOffset; cameraOffset = Vector3.zero;
            if (shakeLeft > 0) {
                shakeLeft = Mathf.Max(0,shakeLeft-Time.deltaTime);
                cameraOffset = Vector3.up*Mathf.Sin(Time.time*80)*.035f*(shakeLeft/.12f);
                view.transform.position += cameraOffset;
            }
        }
        public void Clear() {
            foreach (var effect in pool) effect.Root.SetActive(false);
            foreach(var marker in targetMarkers) if(marker!=null) marker.gameObject.SetActive(false);
            if (view != null) view.transform.position -= cameraOffset;
            cameraOffset = Vector3.zero; shakeLeft = 0;
        }
        void OnDisable() { Clear(); }
        void OnDestroy() { foreach (var material in materials.Values) Destroy(material); if(targetMaterial!=null) Destroy(targetMaterial); }
    }
}
