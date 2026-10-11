using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    // Reuses up to 48 simple effect objects. No downloaded art or particle dependencies.
    public sealed class CombatEffects : MonoBehaviour {
        enum Mode { Spark, Ring, GroundRing, Beam, Text, Slash, Charge, Projectile }
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
            if (effect == null) {
                if (pool.Count >= Limit) return null;
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
            effect.Mesh.gameObject.SetActive(mode == Mode.Spark || mode == Mode.Projectile);
            effect.Text.gameObject.SetActive(mode == Mode.Text);
            effect.Line.enabled = mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Beam || mode == Mode.Slash || mode == Mode.Charge;
            effect.Mesh.sharedMaterial = materials[element]; effect.Line.sharedMaterial = materials[element]; effect.Text.color = Tint(element);
            effect.Line.loop = mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge;
            effect.Line.startWidth = effect.Line.endWidth = size;
            effect.Line.positionCount = (mode == Mode.Ring || mode == Mode.GroundRing || mode == Mode.Charge || mode == Mode.Slash) ? 24 : 2;
            if (mode == Mode.Spark || mode == Mode.Projectile) effect.Mesh.transform.localScale = Vector3.one * size;
            return effect;
        }
        void Spark(Vector3 position, Element element, Vector3 velocity, float size, float duration) {
            var effect = Get(Mode.Spark, position, element, duration, size); if (effect != null) effect.Velocity = velocity;
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
                if (effect.Kind == Mode.Projectile) {
                    effect.Root.transform.position = Vector3.Lerp(effect.Start,effect.Destination,progress) + Vector3.up*Mathf.Sin(progress*Mathf.PI)*.3f;
                }
                if (effect.Kind == Mode.Slash || effect.Kind == Mode.Charge) {
                    effect.Root.transform.rotation = view.transform.rotation;
                    float radius = effect.Kind == Mode.Charge ? .65f*(1-progress)+.15f : .35f+progress*.7f;
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
            if (view != null) view.transform.position -= cameraOffset;
            cameraOffset = Vector3.zero; shakeLeft = 0;
        }
        void OnDisable() { Clear(); }
        void OnDestroy() { foreach (var material in materials.Values) Destroy(material); }
    }
}
