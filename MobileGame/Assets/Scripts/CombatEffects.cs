using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    // Reuses up to 48 simple effect objects. No downloaded art or particle dependencies.
    public sealed class CombatEffects : MonoBehaviour {
        enum Mode { Spark, Ring, GroundRing, Beam, Text, Slash, Charge, Projectile, Sigil, Shield, Cleave, Arrow, Meteor, Crest, Trail, Shard, Lightning }
        sealed class Effect {
            public GameObject Root;
            public Renderer Mesh;
            public LineRenderer Line;
            public TextMesh Text, Shadow;
            public Mode Kind;
            public Element Element;
            public float Age, Duration, Size;
            public Vector3 Start, Velocity, Destination;
            public UltimateSignature Signature;
        }
        readonly List<Effect> pool = new List<Effect>();
        readonly Dictionary<Element, Material> materials = new Dictionary<Element, Material>();
        readonly LineRenderer[] targetMarkers=new LineRenderer[Battle.MaxPartySize];
        Material targetMaterial;
        MaterialPropertyBlock meshTint;
        Camera view;
        Font font;
        Vector3 cameraOffset;
        float shakeLeft, focusLeft, focusDuration, focusStrength, restingFov;
        const int Limit = 48;
        public void Initialize(Camera camera) {
            view = camera; restingFov=camera.fieldOfView; meshTint=new MaterialPropertyBlock();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var template = Resources.Load<Material>("CombatEffects");
            var shader = Resources.Load<Shader>("CombatGlow");
            if(shader==null || !shader.isSupported) shader=template != null ? template.shader : Shader.Find("Unlit/Color");
            if (shader == null) { Debug.LogError("Combat effect shader is missing. Run Ashlight > Prepare Mobile Project before building."); enabled = false; return; }
            foreach (Element element in System.Enum.GetValues(typeof(Element))) {
                var material = new Material(shader); material.color = Tint(element); materials.Add(element, material);
            }
            // Warm a small reusable pool before the first multi-target attack.
            for(int i=0;i<12;i++) { var effect=Get(Mode.Text,Vector3.zero,Element.Physical,.1f,.1f); if(effect!=null) { effect.Age=0; } }
            foreach(var effect in pool) effect.Root.SetActive(false);
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
            if(AccessibilityOptions.ReducedEffects && (mode==Mode.Spark || mode==Mode.Slash || mode==Mode.Crest || mode==Mode.Meteor || mode==Mode.Trail || mode==Mode.Shard || mode==Mode.Lightning)) return null;
            Effect effect = null; int active=0;
            foreach (var item in pool) { if(item.Root.activeSelf) active++; else if(effect==null) effect=item; }
            // Reserve room for five-target damage and healing numbers before decorative effects.
            if(mode!=Mode.Text && active>=Limit-12) return null;
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
                var shadow=new GameObject("Text Outline"); shadow.transform.SetParent(text.transform,false); shadow.transform.localPosition=new Vector3(.018f,-.018f,.01f);
                effect.Shadow=shadow.AddComponent<TextMesh>(); effect.Shadow.font=font; effect.Shadow.fontSize=48; effect.Shadow.anchor=TextAnchor.MiddleCenter;
                shadow.GetComponent<Renderer>().sharedMaterial=font.material; effect.Shadow.color=Color.black;
                pool.Add(effect);
            }
            effect.Root.SetActive(true); effect.Root.transform.position = position; effect.Root.transform.localScale = Vector3.one;
            effect.Root.transform.rotation = Quaternion.identity;
            effect.Kind = mode; effect.Element = element; effect.Start = position; effect.Age = 0; effect.Duration = duration; effect.Size = size; effect.Velocity = Vector3.zero; effect.Destination=Vector3.zero;
            effect.Mesh.gameObject.SetActive(mode == Mode.Spark || mode == Mode.Projectile || mode == Mode.Arrow || mode == Mode.Meteor);
            effect.Text.gameObject.SetActive(mode == Mode.Text);
            effect.Line.enabled = mode == Mode.Trail || mode == Mode.Shard || mode == Mode.Lightning || mode == Mode.Crest || mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Beam || mode == Mode.Slash || mode == Mode.Charge || mode == Mode.Sigil || mode == Mode.Shield || mode == Mode.Cleave;
            effect.Mesh.sharedMaterial = materials[element]; effect.Line.sharedMaterial = materials[element]; effect.Text.color = Tint(element);
            effect.Line.loop = mode == Mode.Shard || mode == Mode.Crest || mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge || mode == Mode.Sigil;
            effect.Line.startWidth = size; effect.Line.endWidth = mode==Mode.Trail?size*.15f:size;
            effect.Line.startColor=effect.Line.endColor=Color.white;
            meshTint.SetFloat("_UseVertexColor",0); meshTint.SetColor("_Color",Tint(element)); effect.Mesh.SetPropertyBlock(meshTint);
            effect.Line.positionCount = mode==Mode.Trail?12:mode==Mode.Shard?4:mode==Mode.Lightning?7:(mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge || mode == Mode.Slash || mode == Mode.Shield || mode == Mode.Cleave) ? 24 : mode == Mode.Sigil ? 10 : 2;
            if(effect.Line.enabled) for(int i=0;i<effect.Line.positionCount;i++) effect.Line.SetPosition(i,Vector3.zero);
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
            var effect = Get(Mode.Text, position + Vector3.up * (1.2f + (lane%4)*.30f) + Vector3.right*((lane/4%2==0?1:-1)*.18f), element, 1.15f, 1);
            if (effect != null) { effect.Text.text = text; effect.Text.characterSize = .05f; effect.Shadow.text=text; effect.Shadow.characterSize=.05f; effect.Shadow.color=Color.black; effect.Velocity = Vector3.up * .65f; }
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
        public void BeginUltimate(Vector3 actor,Element element,UltimateSignature signature,bool autoPlay) {
            focusDuration=autoPlay?.5f:.8f; focusLeft=focusDuration; focusStrength=autoPlay?2f:4f;
            var crest=Get(Mode.Crest,actor+Vector3.up*.7f,element,focusDuration,.055f);
            if(crest!=null) { crest.Signature=signature; crest.Line.positionCount=signature.Points*2; }
        }
        public void SignatureImpact(Vector3 from,Vector3 to,Element element,UltimateSignature signature,bool area) {
            // Each class has a recognizable shape; named heroes vary crown count, angle and sound.
            int decorations=area?Mathf.Min(4,signature.Satellites):signature.Satellites;
            for(int i=0;i<decorations;i++) {
                float angle=(i*360f/decorations+signature.Rotation)*Mathf.Deg2Rad;
                Vector3 offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(area?.75f:.55f);
                switch(signature.Class) {
                    case HeroClass.Knight:
                        var cut=Get(Mode.Slash,to+offset*.5f,element,.32f,.10f); if(cut!=null) cut.Destination=new Vector3(0,0,signature.Rotation+i*25); break;
                    case HeroClass.Paladin: Get(Mode.Shield,to+offset*.3f,element,.45f,.055f); break;
                    case HeroClass.Sorceress: Spark(to+Vector3.up*.8f,element,offset*2+Vector3.up,.13f,.6f); break;
                    case HeroClass.Ranger:
                        var arrow=Get(Mode.Arrow,from+offset+Vector3.up*.3f,element,.18f+i*.025f,.07f); if(arrow!=null) arrow.Destination=to; break;
                    case HeroClass.Rogue: Get(Mode.Cleave,to+offset,element,.25f+i*.025f,.07f); break;
                    case HeroClass.Cleric: Get(Mode.Ring,to+Vector3.up*(i*.16f),element,.45f+i*.035f,.05f); break;
                }
            }
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
            if(profile.Casting) { Get(Mode.Charge,actor+Vector3.up*.7f,element,duration,.065f); if(!AccessibilityOptions.ReducedEffects) Get(Mode.GroundRing,new Vector3(actor.x,.065f,actor.z),element,duration,.045f); }
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
            ElementAccent(to+Vector3.up*.65f,element,ultimate?1.25f:1);
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
        void ElementAccent(Vector3 position,Element element,float scale) {
            if(AccessibilityOptions.ReducedEffects) return;
            if(element==Element.Cold) for(int i=0;i<3;i++) { var shard=Get(Mode.Shard,position+Vector3.right*(i-1)*.2f,element,.45f,.28f*scale); if(shard!=null) shard.Destination=new Vector3(0,0,(i-1)*24); }
            else if(element==Element.Lightning) { var bolt=Get(Mode.Lightning,position+Vector3.down*.55f,element,.28f,.2f*scale); if(bolt!=null) bolt.Destination=Vector3.up*1.3f; }
            else if(element==Element.Fire) for(int i=0;i<3;i++) Spark(position+Vector3.right*(i-1)*.1f,element,new Vector3((i-1)*.35f,1.5f,0),.12f*scale,.4f);
            else if(element==Element.Poison) Get(Mode.GroundRing,new Vector3(position.x,.07f,position.z),element,.8f,.08f*scale);
            else if(element==Element.Light) Get(Mode.Sigil,new Vector3(position.x,.075f,position.z),element,.5f,.055f*scale);
        }
        public void WeaponTrail(Vector3 from,Vector3 to,Element element,HeroClass kind) {
            int count=kind==HeroClass.Rogue?2:1;
            for(int i=0;i<count;i++) { var trail=Get(Mode.Trail,from+Vector3.up*(.7f+i*.15f),element,.22f,kind==HeroClass.Knight || kind==HeroClass.Paladin?.085f:.05f); if(trail!=null) trail.Destination=(to-from)*.8f; }
        }
        public void BossWarning(Vector3 position,BossCue cue,float duration) {
            var ground=new Vector3(position.x,.06f,position.z);
            Get(Mode.GroundRing,ground,cue.Element,duration,.075f);
            if(cue.Style==BossEffectStyle.Bell || cue.Style==BossEffectStyle.Eclipse) Get(Mode.Sigil,ground,cue.Element,duration,.07f);
            else if(cue.Style==BossEffectStyle.Forge) Get(Mode.Shield,position+Vector3.up*.5f,cue.Element,duration,.075f);
            else if(cue.Style==BossEffectStyle.Stars) Get(Mode.Charge,position+Vector3.up*.8f,cue.Element,duration,.08f);
            else for(int i=0;i<3;i++) Get(Mode.Ring,position+Vector3.up*(i*.25f),cue.Element,duration,.04f+i*.015f);
        }
        public void BossSupport(Vector3 actor,Vector3 recipient,BossCue cue,bool healing) {
            var beam=Get(Mode.Beam,actor+Vector3.up*.6f,healing?Element.Light:cue.Element,.45f,.045f);
            if(beam!=null) { beam.Line.SetPosition(0,Vector3.zero); beam.Line.SetPosition(1,recipient-actor); }
            Get(healing?Mode.Sigil:Mode.Shield,recipient,healing?Element.Light:cue.Element,.7f,.10f);
            Get(Mode.GroundRing,new Vector3(recipient.x,.06f,recipient.z),cue.Element,.7f,.09f);
        }
        public void BossSignatureImpact(Vector3 position,BossCue cue,bool area) {
            var ground=new Vector3(position.x,.065f,position.z);
            switch(cue.Style) {
                case BossEffectStyle.Bell: for(int i=0;i<3;i++) Get(Mode.Ring,position+Vector3.up*(i*.25f),cue.Element,.45f+i*.12f,.06f+i*.02f); break;
                case BossEffectStyle.Tide: for(int i=0;i<3;i++) Get(Mode.GroundRing,ground+Vector3.up*(i*.012f),cue.Element,.55f+i*.13f,.07f+i*.035f); break;
                case BossEffectStyle.Thorns: for(int i=0;i<5;i++) { float angle=i*Mathf.PI*2/5; Spark(ground,cue.Element,new Vector3(Mathf.Cos(angle)*.65f,1.8f,Mathf.Sin(angle)*.65f),.09f,.5f); } break;
                case BossEffectStyle.Stars: var meteor=Get(Mode.Meteor,position+Vector3.up*2,cue.Element,.4f,.08f); if(meteor!=null) meteor.Destination=position; Get(Mode.Sigil,ground,cue.Element,.6f,.10f); break;
                case BossEffectStyle.Forge: Get(Mode.Cleave,position,cue.Element,.35f,.13f); Get(Mode.GroundRing,ground,cue.Element,.6f,.12f); break;
                default: Get(Mode.Charge,position+Vector3.up*.5f,cue.Element,.6f,.09f); Get(Mode.Sigil,ground,cue.Element,.65f,.11f); break;
            }
            // Area effects are local to affected heroes; one bounded camera shake per volley.
            shakeLeft=Mathf.Max(shakeLeft,area?.14f:.18f);
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
                float alpha=1-Mathf.SmoothStep(0,1,progress);
                if(effect.Line.enabled) { effect.Line.startColor=new Color(1,1,1,alpha); effect.Line.endColor=new Color(1,1,1,alpha*(effect.Line.loop?1:.7f)); }
                if(effect.Mesh.gameObject.activeSelf) { var tint=Tint(effect.Element); tint.a=alpha; meshTint.SetFloat("_UseVertexColor",0); meshTint.SetColor("_Color",tint); effect.Mesh.SetPropertyBlock(meshTint); }
                if(effect.Kind==Mode.Trail) {
                    for(int i=0;i<12;i++) { float t=Mathf.Lerp(Mathf.Max(0,progress-.5f),progress,i/11f); effect.Line.SetPosition(i,effect.Destination*t+Vector3.up*Mathf.Sin(t*Mathf.PI)*.35f); }
                    effect.Line.startWidth=effect.Size*(1-progress); effect.Line.endWidth=effect.Line.startWidth*.12f;
                }
                if(effect.Kind==Mode.Shard) {
                    effect.Root.transform.rotation=view.transform.rotation*Quaternion.Euler(0,0,effect.Destination.z);
                    float height=effect.Size*(1+progress*.5f);
                    effect.Line.SetPosition(0,Vector3.up*height); effect.Line.SetPosition(1,Vector3.right*height*.3f); effect.Line.SetPosition(2,Vector3.down*height); effect.Line.SetPosition(3,Vector3.left*height*.3f);
                    effect.Line.startWidth=effect.Line.endWidth=.035f*(1-progress);
                }
                if(effect.Kind==Mode.Lightning) {
                    for(int i=0;i<7;i++) { float t=i/6f; float jitter=i==0 || i==6?0:Mathf.Sin(i*13+effect.Age*55)*effect.Size; effect.Line.SetPosition(i,effect.Destination*t+Vector3.right*jitter); }
                    effect.Line.startWidth=effect.Line.endWidth=.065f*(1-progress);
                }
                if (effect.Kind == Mode.Spark) effect.Mesh.transform.localScale = (effect.Element == Element.Cold ? new Vector3(.5f,1.8f,.5f) : Vector3.one) * effect.Size * (1-progress);
                if(effect.Kind==Mode.Arrow || effect.Kind==Mode.Meteor) {
                    effect.Root.transform.position=Vector3.Lerp(effect.Start,effect.Destination,progress);
                    if(effect.Kind==Mode.Arrow) {
                        var direction=effect.Destination-effect.Start;
                        if(direction.sqrMagnitude>.001f) effect.Root.transform.rotation=Quaternion.LookRotation(direction);
                        effect.Mesh.transform.localScale=new Vector3(effect.Size,effect.Size,effect.Size*5);
                    } else effect.Mesh.transform.localScale=Vector3.one*effect.Size*(1+progress*.4f);
                }
                if(effect.Kind==Mode.Crest) {
                    effect.Root.transform.rotation=view.transform.rotation;
                    var sig=effect.Signature; float radius=.55f+Mathf.Sin(progress*Mathf.PI)*.2f;
                    for(int i=0;i<sig.Points*2;i++) {
                        float angle=(i*180f/sig.Points+sig.Rotation+progress*30)*Mathf.Deg2Rad;
                        float r=radius*(i%2==0?1:.72f);
                        effect.Line.SetPosition(i,new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0));
                    }
                    effect.Line.startWidth=effect.Line.endWidth=effect.Size*(1-progress);
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
                    effect.Root.transform.rotation = view.transform.rotation*Quaternion.Euler(effect.Destination);
                    float radius = effect.Kind == Mode.Charge ? .65f*(1-progress)+.15f : effect.Kind==Mode.Shield?.8f:effect.Kind==Mode.Cleave?.7f+progress: .35f+progress*.7f;
                    for (int i=0;i<24;i++) {
                        float angle = effect.Kind == Mode.Charge ? i*Mathf.PI*2/24 : (-.65f+i/23f*1.3f+progress*.7f)*Mathf.PI;
                        effect.Line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
                    }
                    effect.Line.startWidth=effect.Line.endWidth=effect.Size*(1-progress*.8f);
                }
                if (effect.Kind == Mode.Text) {
                    effect.Root.transform.rotation = view.transform.rotation;
                    var color = effect.Text.color; color.a = progress < .55f ? 1 : (1-progress)/.45f; effect.Text.color = color; effect.Shadow.color=new Color(0,0,0,color.a);
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
            if(AccessibilityOptions.ReducedMotion) { focusLeft=0; shakeLeft=0; }
            if(focusLeft>0) { focusLeft=Mathf.Max(0,focusLeft-Time.deltaTime); view.fieldOfView=restingFov-Mathf.Sin(focusLeft/focusDuration*Mathf.PI)*focusStrength; }
            else view.fieldOfView=restingFov;
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
            cameraOffset = Vector3.zero; shakeLeft = 0; focusLeft=0; if(view!=null) view.fieldOfView=restingFov;
        }
        void OnDisable() { Clear(); }
        void OnDestroy() { foreach (var material in materials.Values) Destroy(material); if(targetMaterial!=null) Destroy(targetMaterial); }
    }
}
