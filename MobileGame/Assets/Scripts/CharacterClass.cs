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
        public ClassDefinition WithTalentBonuses(int health,int skillsDamage) {
            if(health==0 && skillsDamage==0) return this;
            var skills=new SkillDefinition[Skills.Count];
            for(int i=0;i<skills.Length;i++) { var skill=Skills[i]; skills[i]=new SkillDefinition(skill.Name,(skill.Damage*(100+skillsDamage)+99)/100,skill.Healing,skill.Guard,skill.PartyHealing,skill.Affinity); }
            return new ClassDefinition(Kind,Name,AbilityName,Description,(MaxHealth*(100+health)+99)/100,AttackDamage,(AbilityDamage*(100+skillsDamage)+99)/100,AbilityHealing,Array.AsReadOnly(skills));
        }
        public ClassDefinition WithFactionBonus(int percent) {
            if(percent==0) return this;
            var boosted=WithTalentBonuses(percent,percent);
            return new ClassDefinition(Kind,Name,AbilityName,Description,boosted.MaxHealth,(AttackDamage*(100+percent)+99)/100,boosted.AbilityDamage,AbilityHealing,boosted.Skills);
        }
        public ClassDefinition WithEquipment(int health,int attack,int damage) {
            if(health==0 && attack==0 && damage==0) return this;
            var skills=new SkillDefinition[Skills.Count];
            for(int i=0;i<skills.Length;i++) { var s=Skills[i]; skills[i]=new SkillDefinition(s.Name,s.Damage>0?s.Damage+damage:0,s.Healing,s.Guard,s.PartyHealing,s.Affinity); }
            return new ClassDefinition(Kind,Name,AbilityName,Description,MaxHealth+health,AttackDamage+attack,AbilityDamage>0?AbilityDamage+damage:0,AbilityHealing,Array.AsReadOnly(skills));
        }
        public static string PassiveDescription(HeroClass kind) {
            switch(kind) {
                case HeroClass.Knight: return "Vanguard · while in front, reduces direct damage to the other front hero by 20%.";
                case HeroClass.Paladin: return "Sacred Aegis · grants at least 8 Guard to living allies before each enemy round.";
                case HeroClass.Sorceress: return "Arcane Exposure · +20% damage when exploiting an elemental weakness.";
                case HeroClass.Ranger: return "Hunter's Mark · +20% damage against back-row enemies.";
                case HeroClass.Rogue: return "Deadly Precision · 25% chance to deal 1.5× damage on a damaging hit; ignores enemy defender protection.";
                default: return "Mending Prayer · after each enemy round, heals the living ally with the lowest health percentage for 10% of the Cleric's max HP.";
            }
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
    public static class ClassTalents {
        public static int UnlockLevel(int tier) { return 5+tier*5; }
        static readonly string[,] Names={
            {"Shield Wall","Riposte","Iron Resolve","Blade Mastery","Unbroken Line","Vengeful Steel"},
            {"Blessed Aegis","Merciful Light","Sacred Resolve","Holy Warfare","Radiant Bulwark","Renewing Aura"},
            {"Elemental Insight","Spell Mastery","Arcane Flow","Warded Soul","Elemental Dominion","Astral Power"},
            {"Rear Hunter","Bow Mastery","Rapid Focus","Trail Survivor","Hunter's Dominance","Deadeye"},
            {"Keen Edge","Deep Cuts","Shadow Flow","Elusive Survivor","Assassin's Luck","Fatal Wounds"},
            {"Greater Prayer","Purifying Prayer","Devout Flow","Blessed Vitality","Miracle Worker","Mass Purification"}
        };
        static readonly string[,] Descriptions={
            {"Front-row protection rises from 20% to 30%.","Counterattacks deal +5 base damage.","+15% maximum health before armor.","+10% damaging skill power before weapon bonuses.","Front-row protection gains another 10 percentage points.","Counterattacks deal another +10 base damage."},
            {"Sacred Aegis grants at least 12 Guard.","+20% healing from your skills.","+15% maximum health before armor.","+10% damaging skill power before weapon bonuses.","Sacred Aegis gains another 8 Guard.","After each enemy round, heal living allies for 3% of your max HP."},
            {"Weakness damage bonus rises from 20% to 30%.","+10% damaging skill power before weapon bonuses.","Gain 10 extra energy per attack or skill.","+15% maximum health before armor.","Weakness damage bonus gains another 20 percentage points.","+20% ultimate damage before weapon bonuses."},
            {"Back-row damage bonus rises from 20% to 30%.","+10% damaging skill power before weapon bonuses.","Gain 10 extra energy per attack or skill.","+15% maximum health before armor.","Back-row damage bonus gains another 20 percentage points.","+20% ultimate damage before weapon bonuses."},
            {"Critical chance rises from 25% to 35%.","Critical hits rise from 1.5× to 1.75× damage.","Gain 10 extra energy per attack or skill.","+15% maximum health before armor.","Critical chance gains another 15 percentage points.","Critical damage gains another 0.25× multiplier."},
            {"Mending Prayer heals for 15% of your max HP.","Mending Prayer removes burn or poison from its target.","Gain 10 extra energy per attack or skill.","+15% maximum health before armor.","Mending Prayer gains another 10% of your max HP.","After each enemy round, cleanse burn and poison from all living allies."}
        };
        public static string Name(HeroClass kind,int tier,int choice) { return Names[(int)kind,tier*2+choice]; }
        public static string Description(HeroClass kind,int tier,int choice) { return Descriptions[(int)kind,tier*2+choice]; }
    }

}
