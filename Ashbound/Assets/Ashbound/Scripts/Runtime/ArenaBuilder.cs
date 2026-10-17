using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ashbound
{
    internal static class ArenaBuilder
    {
        internal static readonly Color Gold = new Color(0.93f, 0.66f, 0.3f);
        internal static readonly Color Soul = new Color(0.27f, 0.85f, 0.77f);
        private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        internal static Material Material(Color color, bool glow = false)
        {
            // Emissive variants use a separate key, keeping shared materials immutable.
            Color key = new Color(color.r, color.g, color.b, glow ? 0.5f : 1f);
            Material result;
            if (materials.TryGetValue(key, out result) && result != null) return result;
            var template = Resources.Load<Material>(glow ? "AshboundGlow" : "AshboundLit");
            result = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            result.color = color;
            result.SetFloat("_Glossiness", glow ? 0.1f : 0.28f);
            if (glow)
            {
                result.EnableKeyword("_EMISSION");
                result.SetColor("_EmissionColor", color * 1.8f);
            }
            materials[key] = result;
            return result;
        }

        internal static GameObject Shape(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Color color, bool glow = false)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var collider = obj.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(color, glow);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return obj;
        }

        internal static GameObject Box(string name, Transform parent, Vector3 position,
            Vector3 scale, Color color, bool glow = false)
        {
            return Shape(name, PrimitiveType.Cube, parent, position, scale, color, glow);
        }

        internal static Camera Build(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.3f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.065f, 0.085f);
            RenderSettings.fogDensity = 0.035f;

            var cameraObject = new GameObject("Arena camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 8f, -17f);
            camera.transform.LookAt(new Vector3(0f, 1f, 1f));

            var lightObject = new GameObject("Cold moonlight", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.57f, 0.72f, 0.88f);
            light.intensity = 1.3f;

            var stone = new Color(0.17f, 0.2f, 0.23f);
            var darkStone = new Color(0.09f, 0.12f, 0.15f);
            var ground = new Color(0.065f, 0.085f, 0.09f);
            Box("Earth", parent, new Vector3(0f, -0.35f, 5f), new Vector3(60f, 0.5f, 38f), ground);
            Box("Causeway", parent, new Vector3(0f, -0.08f, 0f), new Vector3(28f, 0.22f, 3.6f), darkStone);
            for (int x = -13; x <= 13; x++)
            for (int z = -1; z <= 1; z++)
                Box("Flagstone", parent, new Vector3(x, 0.05f, z * 1.05f),
                    new Vector3(0.95f, 0.12f, 0.98f), stone * (1f + ((x * x + z * z) % 3) * 0.06f));

            Shape("Oath circle", PrimitiveType.Cylinder, parent,
                new Vector3(0f, 0.1f, 0f), new Vector3(5.7f, 0.13f, 4.1f), darkStone);
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                var rune = Box("Oath rune", parent, new Vector3(Mathf.Cos(angle) * 2.5f, 0.245f,
                    Mathf.Sin(angle) * 1.65f), new Vector3(0.16f, 0.015f, 0.055f), Gold * 0.65f, true);
                rune.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }
            // The gold marks communicate the actual gameplay strike boundary.
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Strike threshold", parent, new Vector3(side * Core.CombatSession.StrikeRange, 0.14f, -0.5f),
                    new Vector3(0.07f, 0.025f, 2.5f), Gold, true);
                BuildBrazier(parent, new Vector3(side * 5f, 0f, 2.1f));
                BuildBrazier(parent, new Vector3(side * 10.7f, 0f, 2.4f));
                for (int i = 0; i < 9; i++)
                {
                    float x = side * (2.5f + i * 1.6f);
                    float z = 4.5f + (i % 3) * 1.5f;
                    var grave = Box("Gravestone", parent, new Vector3(x, 0.65f, z),
                        new Vector3(0.6f, 1.25f + (i % 2) * 0.2f, 0.27f), stone * 0.8f);
                    grave.transform.localRotation = Quaternion.Euler(0f, side * i * 7f, i % 2 == 0 ? -8f : 5f);
                    Box("Grave cross", grave.transform, new Vector3(0f, 0.12f, -0.53f),
                        new Vector3(0.075f, 0.45f, 0.05f), darkStone);
                    Box("Grave crossbar", grave.transform, new Vector3(0f, 0.18f, -0.53f),
                        new Vector3(0.42f, 0.075f, 0.05f), darkStone);
                }
            }

            // Ruined chapel silhouettes frame the arena without obscuring the lanes.
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 5.6f;
                Box("Chapel pier", parent, new Vector3(x, 4f, 11f), new Vector3(1.2f, 8f, 1.5f), darkStone);
                Box("Pier cap", parent, new Vector3(x, 8.1f, 11f), new Vector3(1.6f, 0.3f, 1.9f), stone);
                if (i == 2) continue;
                for (int wing = -1; wing <= 1; wing += 2)
                {
                    var arch = Box("Broken gothic arch", parent,
                        new Vector3(x + 2.8f + wing * 1.1f, 7.3f, 11f), new Vector3(0.55f, 3.3f, 1f), darkStone);
                    arch.transform.rotation = Quaternion.Euler(0f, 0f, wing * 43f);
                }
            }
            Shape("Veiled moon", PrimitiveType.Sphere, parent,
                new Vector3(7f, 12f, 24f), Vector3.one * 3.2f, new Color(0.38f, 0.51f, 0.55f), true);
            return camera;
        }

        private static void BuildBrazier(Transform parent, Vector3 position)
        {
            var root = new GameObject("Soul brazier").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            var iron = new Color(0.13f, 0.15f, 0.16f);
            Shape("Stem", PrimitiveType.Cylinder, root, new Vector3(0f, 0.6f, 0f),
                new Vector3(0.22f, 0.65f, 0.22f), iron);
            Shape("Bowl", PrimitiveType.Cylinder, root, new Vector3(0f, 1.2f, 0f),
                new Vector3(0.7f, 0.13f, 0.7f), iron);
            Shape("Soul flame", PrimitiveType.Sphere, root, new Vector3(0f, 1.65f, 0f),
                new Vector3(0.3f, 0.85f, 0.3f), Soul, true);
            var glow = new GameObject("Brazier glow", typeof(Light));
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = new Vector3(0f, 2f, -0.4f);
            var light = glow.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = Soul;
            light.range = 5f;
            light.intensity = 1.8f;
            light.shadows = LightShadows.None;
        }

        internal static void DisposeMaterials()
        {
            foreach (var material in materials.Values)
                if (material != null) Object.Destroy(material);
            materials.Clear();
        }
    }
}
