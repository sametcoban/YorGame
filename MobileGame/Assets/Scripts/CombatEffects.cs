using System.Collections.Generic;
using UnityEngine;
namespace Ashlight {
    // Reuses up to 48 simple effect objects. No downloaded art or particle dependencies.
    public sealed class CombatEffects : MonoBehaviour {
        enum Mode { Spark, Ring, Beam, Text }
        sealed class Effect {
            public GameObject Root;
            public Renderer Mesh;
            public LineRenderer Line;
            public TextMesh Text;
            public Mode Kind;
            public Element Element;
            public float Age, Duration, Size;
            public Vector3 Start, Velocity;
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
            effect.Mesh.gameObject.SetActive(mode == Mode.Spark);
            effect.Text.gameObject.SetActive(mode == Mode.Text);
            effect.Line.enabled = mode == Mode.Ring || mode == Mode.Beam;
            effect.Mesh.sharedMaterial = materials[element]; effect.Line.sharedMaterial = materials[element]; effect.Text.color = Tint(element);
            effect.Line.loop = mode == Mode.Ring;
            effect.Line.startWidth = effect.Line.endWidth = size;
            effect.Line.positionCount = mode == Mode.Ring ? 24 : 2;
            if (mode == Mode.Spark) effect.Mesh.transform.localScale = Vector3.one * size;
            return effect;
        }
        void Spark(Vector3 position, Element element, Vector3 velocity, float size, float duration) {
            var effect = Get(Mode.Spark, position, element, duration, size); if (effect != null) effect.Velocity = velocity;
        }
        public void Floating(Vector3 position, string text, Element element) {
            var effect = Get(Mode.Text, position + Vector3.up * 1.2f, element, .9f, 1);
            if (effect != null) { effect.Text.text = text; effect.Velocity = Vector3.up * .8f; }
        }
        public void Ring(Vector3 position, Element element, float size = .06f) { Get(Mode.Ring, position, element, .45f, size); }
        public void Attack(Vector3 from, Vector3 to, Element element, int damage, float multiplier) {
            if (damage <= 0) return;
            var beam = Get(Mode.Beam, from, element, .18f, element == Element.Lightning ? .09f : .05f);
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
                if (effect.Kind == Mode.Text) {
                    effect.Root.transform.rotation = view.transform.rotation;
                    var color = effect.Text.color; color.a = 1-progress; effect.Text.color = color;
                }
                if (effect.Kind == Mode.Ring) {
                    float radius = .15f + progress * .85f;
                    for (int i = 0; i < 24; i++) {
                        float angle = i*Mathf.PI*2/24;
                        effect.Line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
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
