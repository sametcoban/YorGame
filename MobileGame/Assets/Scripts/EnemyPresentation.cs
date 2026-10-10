using UnityEngine;
namespace Ashlight {
    public sealed class EnemyPresentation : MonoBehaviour {
        Renderer[] meshes;
        MaterialPropertyBlock block;
        void Awake() { meshes = GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock(); }
        public void Warning(bool active) {
            if (meshes == null) return;
            foreach (var renderer in meshes) {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) {
                    var material = materials[i];
                    if (material == null) continue;
                    renderer.GetPropertyBlock(block,i);
                    if (material.HasProperty("_EmissionColor"))
                        block.SetColor("_EmissionColor",active ? new Color(1,.55f,.08f)*.65f : material.GetColor("_EmissionColor"));
                    renderer.SetPropertyBlock(block,i); block.Clear();
                }
            }
        }
    }
}
