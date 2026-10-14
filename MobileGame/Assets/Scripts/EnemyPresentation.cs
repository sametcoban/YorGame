using UnityEngine;
namespace Ashlight {
    public sealed class EnemyPresentation : MonoBehaviour {
        Renderer[] meshes;
        MaterialPropertyBlock block;
        bool warning; Element warningElement; float nextPulse;
        void Update() { if(warning && Time.time>=nextPulse) { nextPulse=Time.time+.08f; ApplyWarning(true); } }
        void OnDisable() { warning=false; ApplyWarning(false); }
        void Awake() { meshes = GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock(); }
        public void Warning(bool active) { Warning(active,Element.Fire); }
        public void Warning(bool active,Element element) { warning=active; warningElement=element; ApplyWarning(active); }
        void ApplyWarning(bool active) {
            if (meshes == null) return;
            foreach (var renderer in meshes) {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) {
                    var material = materials[i];
                    if (material == null) continue;
                    renderer.GetPropertyBlock(block,i);
                    if (material.HasProperty("_EmissionColor"))
                        block.SetColor("_EmissionColor",active ? CombatEffects.Tint(warningElement)*(.4f+.25f*Mathf.Sin(Time.time*7)) : material.GetColor("_EmissionColor"));
                    renderer.SetPropertyBlock(block,i); block.Clear();
                }
            }
        }
    }
}
