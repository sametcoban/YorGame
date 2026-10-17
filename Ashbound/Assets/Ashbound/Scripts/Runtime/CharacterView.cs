using UnityEngine;
using Ashbound.Core;

namespace Ashbound
{
    internal sealed class CharacterView
    {
        internal readonly Transform Root;
        private readonly Transform body;
        private readonly Transform weapon;
        private readonly Transform leftLeg;
        private readonly Transform rightLeg;
        private readonly bool knight;
        private readonly bool wraith;
        private float strikeAge = 10f;
        private float facing = 1f;

        internal CharacterView(Transform parent, bool isKnight, bool isWraith = false)
        {
            knight = isKnight;
            wraith = isWraith;
            Root = new GameObject(isKnight ? "The oathkeeper" : isWraith ? "Wraith" : "Skeleton").transform;
            Root.SetParent(parent, false);
            body = new GameObject("Body").transform;
            body.SetParent(Root, false);
            weapon = new GameObject("Weapon pivot").transform;
            weapon.SetParent(body, false);
            weapon.localPosition = new Vector3(0.45f, 1.3f, -0.2f);
            var bone = new Color(0.71f, 0.72f, 0.62f);
            var steel = new Color(0.29f, 0.34f, 0.39f);
            var black = new Color(0.035f, 0.045f, 0.055f);
            var robe = new Color(0.08f, 0.19f, 0.2f);

            if (knight)
            {
                ArenaBuilder.Box("Breastplate", body, new Vector3(0f, 1.2f, 0f), new Vector3(0.78f, 0.85f, 0.53f), steel);
                ArenaBuilder.Box("Oath sigil", body, new Vector3(0f, 1.3f, -0.29f), new Vector3(0.08f, 0.47f, 0.04f), ArenaBuilder.Gold, true);
                ArenaBuilder.Box("Oath sigil cross", body, new Vector3(0f, 1.4f, -0.29f), new Vector3(0.35f, 0.07f, 0.04f), ArenaBuilder.Gold, true);
                ArenaBuilder.Box("Helm", body, new Vector3(0f, 1.97f, 0f), new Vector3(0.66f, 0.65f, 0.58f), steel * 1.2f);
                ArenaBuilder.Box("Visor", body, new Vector3(0f, 2f, -0.31f), new Vector3(0.54f, 0.12f, 0.05f), black);
                ArenaBuilder.Box("Ember gaze", body, new Vector3(0f, 2f, -0.34f), new Vector3(0.4f, 0.035f, 0.02f), ArenaBuilder.Gold, true);
                var cape = ArenaBuilder.Box("Tattered crimson cloak", body, new Vector3(0f, 1.12f, 0.37f),
                    new Vector3(1.05f, 1.65f, 0.12f), new Color(0.31f, 0.045f, 0.065f));
                cape.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                for (int side = -1; side <= 1; side += 2)
                    ArenaBuilder.Box("Pauldron", body, new Vector3(side * 0.51f, 1.52f, 0f), new Vector3(0.42f, 0.35f, 0.62f), steel);
                ArenaBuilder.Box("Sword hilt", weapon, new Vector3(0.25f, 0f, 0f), new Vector3(0.5f, 0.12f, 0.12f), black);
                ArenaBuilder.Box("Sword guard", weapon, new Vector3(0.48f, 0f, 0f), new Vector3(0.1f, 0.6f, 0.16f), ArenaBuilder.Gold);
                ArenaBuilder.Box("Ash blade", weapon, new Vector3(1.12f, 0f, 0f), new Vector3(1.3f, 0.16f, 0.08f), new Color(0.77f, 0.82f, 0.81f));
                ArenaBuilder.Box("Blade rune", weapon, new Vector3(1.08f, 0f, -0.05f), new Vector3(0.9f, 0.045f, 0.02f), ArenaBuilder.Gold, true);
            }
            else if (wraith)
            {
                ArenaBuilder.Shape("Shroud", PrimitiveType.Capsule, body, new Vector3(0f, 1.15f, 0f), new Vector3(0.82f, 0.9f, 0.7f), robe);
                ArenaBuilder.Shape("Hood", PrimitiveType.Sphere, body, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.7f, black);
                for (int i = -1; i <= 1; i++)
                    ArenaBuilder.Box("Ragged shroud", body, new Vector3(i * 0.26f, 0.47f + i * 0.1f, 0.1f), new Vector3(0.2f, 0.8f, 0.3f), robe);
                for (int side = -1; side <= 1; side += 2)
                    ArenaBuilder.Box("Spectral eye", body, new Vector3(side * 0.14f, 1.98f, -0.34f), new Vector3(0.11f, 0.06f, 0.05f), ArenaBuilder.Soul, true);
                ArenaBuilder.Box("Scythe haft", weapon, new Vector3(0f, 0.05f, 0f), new Vector3(0.09f, 2.2f, 0.09f), black);
                ArenaBuilder.Box("Scythe blade", weapon, new Vector3(0.35f, 1.06f, 0f), new Vector3(0.9f, 0.15f, 0.08f), ArenaBuilder.Soul, true);
            }
            else
            {
                ArenaBuilder.Box("Spine", body, new Vector3(0f, 1.15f, 0f), new Vector3(0.13f, 0.8f, 0.17f), bone);
                for (int i = 0; i < 4; i++)
                    ArenaBuilder.Box("Rib", body, new Vector3(0f, 1.03f + i * 0.14f, 0f), new Vector3(0.61f - i * 0.06f, 0.075f, 0.36f), bone);
                ArenaBuilder.Shape("Skull", PrimitiveType.Sphere, body, new Vector3(0f, 1.92f, 0f), new Vector3(0.62f, 0.64f, 0.52f), bone);
                ArenaBuilder.Box("Jaw", body, new Vector3(0f, 1.68f, -0.1f), new Vector3(0.36f, 0.16f, 0.35f), bone);
                for (int side = -1; side <= 1; side += 2)
                {
                    ArenaBuilder.Box("Eye socket", body, new Vector3(side * 0.15f, 1.97f, -0.245f), new Vector3(0.17f, 0.16f, 0.07f), black);
                    ArenaBuilder.Box("Grave light", body, new Vector3(side * 0.15f, 1.97f, -0.285f), new Vector3(0.07f, 0.055f, 0.02f), ArenaBuilder.Soul, true);
                    var arm = ArenaBuilder.Box("Arm", body, new Vector3(side * 0.42f, 1.14f, 0f), new Vector3(0.11f, 0.75f, 0.13f), bone);
                    arm.transform.localRotation = Quaternion.Euler(0f, 0f, side * 18f);
                }
                ArenaBuilder.Box("Rusty blade", weapon, new Vector3(0.48f, 0f, 0f), new Vector3(0.9f, 0.1f, 0.08f), new Color(0.39f, 0.27f, 0.18f));
            }

            var legColor = knight ? steel * 0.75f : bone;
            leftLeg = ArenaBuilder.Box("Left leg", body, new Vector3(-0.2f, 0.45f, 0f), new Vector3(knight ? 0.25f : 0.12f, 0.8f, 0.22f), legColor).transform;
            rightLeg = ArenaBuilder.Box("Right leg", body, new Vector3(0.2f, 0.45f, 0f), new Vector3(knight ? 0.25f : 0.12f, 0.8f, 0.22f), legColor).transform;
            if (wraith) { leftLeg.gameObject.SetActive(false); rightLeg.gameObject.SetActive(false); }
            ArenaBuilder.Shape("Contact shadow", PrimitiveType.Cylinder, Root,
                new Vector3(0f, 0.08f, 0f), new Vector3(1.05f, 0.008f, 0.6f), black);
        }

        internal void Strike(Side side)
        {
            facing = (int)side;
            strikeAge = 0f;
        }

        internal void Animate(float time, float dt, float x, int direction, bool walking)
        {
            strikeAge += dt;
            if (!knight) facing = direction;
            Root.localPosition = new Vector3(x, knight ? 0.25f : 0.13f, 0f);
            Root.localScale = new Vector3(facing, 1f, 1f);
            float stride = Mathf.Sin(time * 9f + Mathf.Abs(x));
            body.localPosition = new Vector3(0f, wraith ? Mathf.Sin(time * 3f) * 0.12f : walking ? Mathf.Abs(stride) * 0.06f : Mathf.Sin(time * 2f) * 0.025f, 0f);
            leftLeg.localRotation = Quaternion.Euler(0f, 0f, walking ? stride * 22f : -4f);
            rightLeg.localRotation = Quaternion.Euler(0f, 0f, walking ? -stride * 22f : 4f);
            float angle = knight ? (strikeAge < 0.24f ? Mathf.Lerp(65f, -38f, strikeAge / 0.24f) : -28f) : -35f;
            weapon.localRotation = Quaternion.Euler(0f, 0f, angle);
            body.localRotation = Quaternion.Euler(0f, 0f, knight && strikeAge < 0.2f ? -8f : 0f);
        }

        internal void Destroy() { Object.Destroy(Root.gameObject); }
    }
}
