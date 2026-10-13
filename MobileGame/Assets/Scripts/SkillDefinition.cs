using System;
using System.Collections.Generic;
namespace Ashlight {
    [Flags] public enum SkillUtility { None=0, Taunt=1, Cleanse=2, PartyShield=4, Interrupt=8, Dispel=16, Expose=32 }
    public sealed class SkillDefinition {
        public readonly string Name;
        public readonly int Damage, Healing, Guard;
        public readonly bool PartyHealing;
        public readonly SkillUtility Utility;
        // Null follows the hero affinity; an explicit value overrides it for this skill.
        public readonly Element? Affinity;
        public SkillDefinition(string name, int damage, int healing = 0, int guard = 0, bool partyHealing = false, Element? affinity = null, SkillUtility utility=SkillUtility.None) {
            Name = name; Damage = damage; Healing = healing; Guard = guard; PartyHealing = partyHealing; Affinity = affinity; Utility=utility;
        }
        public SkillDefinition Scaled(float multiplier) {
            return new SkillDefinition(Name, (int)Math.Round(Damage * multiplier), (int)Math.Round(Healing * multiplier), (int)Math.Round(Guard * multiplier), PartyHealing, Affinity, Utility);
        }
        public string Description {
            get {
                string text = Damage > 0 ? Damage + " damage. " : "";
                if (Healing > 0) text += "Heal " + Healing + (PartyHealing ? " to living allies. " : " to self. ");
                if (Guard > 0) text += "Block " + Guard + " from the next hit "+((Utility&SkillUtility.PartyShield)!=0?"on each living ally. ":"on this hero. ");
                if((Utility&SkillUtility.Taunt)!=0) text+="Taunt normal attacks for one enemy round; rear strikes and area attacks ignore taunts. ";
                if((Utility&SkillUtility.Cleanse)!=0) text+="Remove debuffs "+(PartyHealing?"from living allies. ":"from self. ");
                if((Utility&SkillUtility.Interrupt)!=0) text+="Interrupt the target's next action or disrupt its charged attack. Bosses resist consecutive interrupts. ";
                if((Utility&SkillUtility.Dispel)!=0) text+="Break the target's shield before damage. ";
                if((Utility&SkillUtility.Expose)!=0) text+="Expose the target: allies deal +20% damage this round. ";
                return text + "2 uses per battle.";
            }
        }
        public static IReadOnlyList<SkillDefinition> For(HeroClass kind) {
            SkillDefinition[] skills;
            switch (kind) {
                case HeroClass.Knight: skills = new[] {
                    new SkillDefinition("Power Strike",40), new SkillDefinition("Guarded Slash",20,0,20),
                    new SkillDefinition("Second Wind",0,30), new SkillDefinition("Shield Bash",30,0,10),
                    new SkillDefinition("Flame Cleave",45), new SkillDefinition("Burning Resolve",25,15,10) }; break;
                case HeroClass.Paladin: skills = new[] {
                    new SkillDefinition("Holy Light",15,30), new SkillDefinition("Smite",35),
                    new SkillDefinition("Sanctuary",0,20,0,true), new SkillDefinition("Aegis",10,0,35),
                    new SkillDefinition("Radiant Blade",25,15), new SkillDefinition("Sacred Resolve",0,35,15) }; break;
                case HeroClass.Sorceress: skills = new[] {
                    new SkillDefinition("Arcane Burst",45), new SkillDefinition("Flame Lance",50),
                    new SkillDefinition("Frost Ward",25,0,20), new SkillDefinition("Frost Drain",25,25),
                    new SkillDefinition("Mana Barrier",0,0,35), new SkillDefinition("Starfall",55) }; break;
                case HeroClass.Ranger: skills = new[] {
                    new SkillDefinition("Piercing Shot",42), new SkillDefinition("Double Shot",40),
                    new SkillDefinition("Evasive Shot",25,0,20), new SkillDefinition("Field Remedy",0,30),
                    new SkillDefinition("Barrage",48), new SkillDefinition("Steady Aim",35,0,10) }; break;
                case HeroClass.Rogue: skills = new[] {
                    new SkillDefinition("Backstab",48), new SkillDefinition("Twin Fangs",42),
                    new SkillDefinition("Smoke Veil",10,0,30), new SkillDefinition("Leeching Cut",25,20),
                    new SkillDefinition("Shadow Strike",35,0,15), new SkillDefinition("Assassinate",55) }; break;
                case HeroClass.Cleric: skills = new[] {
                    new SkillDefinition("Restore",10,25,0,true), new SkillDefinition("Healing Prayer",0,40),
                    new SkillDefinition("Divine Ward",0,0,35), new SkillDefinition("Judgment",35),
                    new SkillDefinition("Renewal",0,30,0,true), new SkillDefinition("Blessed Strike",20,15,0,true) }; break;
                default: throw new ArgumentOutOfRangeException("kind");
            }
            Element[] affinities;
            switch (kind) {
                case HeroClass.Knight: affinities = new[] { Element.Physical, Element.Physical, Element.Physical, Element.Physical, Element.Fire, Element.Fire }; break;
                case HeroClass.Paladin: affinities = new[] { Element.Light, Element.Light, Element.Light, Element.Light, Element.Fire, Element.Fire }; break;
                case HeroClass.Sorceress: affinities = new[] { Element.Lightning, Element.Fire, Element.Cold, Element.Cold, Element.Cold, Element.Lightning }; break;
                case HeroClass.Ranger: affinities = new[] { Element.Physical, Element.Physical, Element.Cold, Element.Physical, Element.Cold, Element.Physical }; break;
                case HeroClass.Rogue: affinities = new[] { Element.Physical, Element.Poison, Element.Poison, Element.Poison, Element.Physical, Element.Poison }; break;
                default: affinities = new[] { Element.Light, Element.Light, Element.Light, Element.Light, Element.Light, Element.Light }; break;
            }
            for (int i = 0; i < skills.Length; i++) {
                var skill = skills[i];
                var utility=skill.Name=="Guarded Slash"?SkillUtility.Taunt:
                    skill.Name=="Sanctuary" || skill.Name=="Renewal" || skill.Name=="Field Remedy" || skill.Name=="Sacred Resolve" || skill.Name=="Smoke Veil"?SkillUtility.Cleanse:
                    skill.Name=="Aegis" || skill.Name=="Divine Ward"?SkillUtility.PartyShield:
                    skill.Name=="Shield Bash" || skill.Name=="Piercing Shot" || skill.Name=="Shadow Strike"?SkillUtility.Interrupt:
                    skill.Name=="Flame Lance" || skill.Name=="Radiant Blade"?SkillUtility.Dispel:
                    skill.Name=="Arcane Burst"?SkillUtility.Expose:SkillUtility.None;
                skills[i] = new SkillDefinition(skill.Name, skill.Damage, skill.Healing, skill.Guard, skill.PartyHealing, affinities[i],utility);
            }
            return Array.AsReadOnly(skills);
        }
    }
}
