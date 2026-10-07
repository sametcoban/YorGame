using System;
namespace Ashlight {
    public enum HeroClass { Knight, Paladin, Sorceress, Ranger, Rogue, Cleric }
    public sealed class ClassDefinition {
        public readonly HeroClass Kind;
        public readonly System.Collections.Generic.IReadOnlyList<SkillDefinition> Skills;
        public readonly string Name, AbilityName, Description;
        public readonly int MaxHealth, AttackDamage, AbilityDamage, AbilityHealing;
        ClassDefinition(HeroClass kind, string name, string ability, string description, int health, int attack, int damage, int healing, System.Collections.Generic.IReadOnlyList<SkillDefinition> skills = null) {
            Skills = skills ?? SkillDefinition.For(kind);
            Kind = kind; Name = name; AbilityName = ability; Description = description;
            MaxHealth = health; AttackDamage = attack; AbilityDamage = damage; AbilityHealing = healing;
        }
        public ClassDefinition Scaled(float multiplier) {
            var skills = new SkillDefinition[Skills.Count];
            for (int i = 0; i < skills.Length; i++) skills[i] = Skills[i].Scaled(multiplier);
            return new ClassDefinition(Kind, Name, AbilityName, Description, (int)Math.Round(MaxHealth * multiplier), (int)Math.Round(AttackDamage * multiplier), (int)Math.Round(AbilityDamage * multiplier), (int)Math.Round(AbilityHealing * multiplier), Array.AsReadOnly(skills));
        }
        public static ClassDefinition For(HeroClass kind) {
            switch (kind) {
                case HeroClass.Knight: return new ClassDefinition(kind, "Knight", "POWER STRIKE", "Balanced sword fighter", 100, 25, 40, 0);
                case HeroClass.Paladin: return new ClassDefinition(kind, "Paladin", "HOLY LIGHT", "Durable fighter with healing", 120, 20, 15, 30);
                case HeroClass.Sorceress: return new ClassDefinition(kind, "Sorceress", "ARCANE BURST", "Fragile caster with high damage", 80, 30, 45, 0);
                case HeroClass.Ranger: return new ClassDefinition(kind, "Ranger", "PIERCING SHOT", "Precise ranged fighter", 90, 28, 42, 0);
                case HeroClass.Rogue: return new ClassDefinition(kind, "Rogue", "BACKSTAB", "Fast burst damage fighter", 85, 32, 48, 0);
                case HeroClass.Cleric: return new ClassDefinition(kind, "Cleric", "RESTORE", "Healer who restores all allies", 95, 18, 10, 25);
                default: throw new ArgumentOutOfRangeException("kind");
            }
        }
    }
}
