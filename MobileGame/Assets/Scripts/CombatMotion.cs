using System;
namespace Ashlight {
    public sealed class ActionMotion {
        public readonly float Impact, SecondImpact, Duration, Advance;
        public ActionMotion(int impactFrame, int finalFrame, float advance, int secondFrame = 0) {
            Impact = (impactFrame-1)/30f; Duration = (finalFrame-1)/30f;
            SecondImpact = secondFrame == 0 ? 0 : (secondFrame-1)/30f; Advance = advance;
        }
    }
    // Seconds correspond to the authored FBX keyframes at 30 fps. No gameplay clock changes.
    public static class CombatMotion {
        static readonly ActionMotion Knight = new ActionMotion(8,25,1.05f);
        static readonly ActionMotion Paladin = new ActionMotion(10,31,1f);
        static readonly ActionMotion Caster = new ActionMotion(11,31,0);
        static readonly ActionMotion Ranger = new ActionMotion(9,27,0);
        static readonly ActionMotion Rogue = new ActionMotion(7,26,1.25f,12);
        public static bool UsesCast(HeroClass kind, bool elementalSkill) {
            return kind == HeroClass.Sorceress || kind == HeroClass.Cleric || kind == HeroClass.Paladin && elementalSkill;
        }
        public static ActionMotion For(HeroClass kind, bool casting) {
            if (casting) return Caster;
            switch (kind) {
                case HeroClass.Paladin: return Paladin;
                case HeroClass.Ranger: return Ranger;
                case HeroClass.Rogue: return Rogue;
                case HeroClass.Sorceress: case HeroClass.Cleric: return Caster;
                default: return Knight;
            }
        }
        static float Smooth(float value) { value = Math.Max(0,Math.Min(1,value)); return value*value*(3-2*value); }
        public static float Advance(float elapsed, float contact, float recovery) {
            if (elapsed < 0 || elapsed >= recovery) return 0;
            if (elapsed <= contact) return Smooth(elapsed/contact);
            return 1-Smooth((elapsed-contact)/(recovery-contact));
        }
        public static float EnemyAdvance(float elapsed) { return Advance(elapsed,.45f,.85f); }
        public static float Dodge(float elapsed) { return Advance(elapsed,.12f,.55f); }
    }
}
