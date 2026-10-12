namespace Ashlight {
    public sealed class UltimateSignature {
        public readonly int Points, Satellites;
        public readonly float Rotation, Pitch;
        public readonly HeroClass Class;
        UltimateSignature(HeroDefinition hero) {
            Class=hero.Class; int tier=(int)hero.Quality;
            Points=6+(int)hero.Class*2; Satellites=tier+3;
            Rotation=tier*18; Pitch=.86f+(int)hero.Class*.035f+tier*.02f;
        }
        public static UltimateSignature For(HeroDefinition hero) { return new UltimateSignature(hero); }
    }
    public enum SkillEffectStyle { Slash, HeavyStrike, Cleave, ShieldBash, Heal, Barrier, HolyStrike, Burst, Lance, Drain, Meteor, Arrow, Volley, Poison, Shadow, TwinStrike }
    // Authored mappings use stable skill names; presentation never changes damage or targets.
    public sealed class SkillPresentation {
        public readonly SkillEffectStyle Style;
        public readonly bool Casting;
        public readonly int Projectiles;
        static readonly ActionMotion RogueFinisher=new ActionMotion(12,26,1.35f);
        public SkillPresentation(SkillEffectStyle style,bool casting=false,int projectiles=1) { Style=style; Casting=casting; Projectiles=projectiles; }
        public ActionMotion Motion(HeroClass kind) { return kind==HeroClass.Rogue && Style==SkillEffectStyle.HeavyStrike?RogueFinisher:CombatMotion.For(kind,Casting); }
        public static SkillPresentation Basic(HeroClass kind) {
            return new SkillPresentation(kind==HeroClass.Ranger?SkillEffectStyle.Arrow:kind==HeroClass.Rogue?SkillEffectStyle.TwinStrike:kind==HeroClass.Sorceress?SkillEffectStyle.Burst:kind==HeroClass.Cleric?SkillEffectStyle.HolyStrike:SkillEffectStyle.Slash,kind==HeroClass.Sorceress || kind==HeroClass.Cleric);
        }
        public static SkillPresentation For(SkillDefinition skill,HeroClass kind) {
            switch(skill.Name) {
                case "Power Strike": case "Smite": case "Assassinate": return new SkillPresentation(SkillEffectStyle.HeavyStrike);
                case "Guarded Slash": case "Burning Resolve": return new SkillPresentation(SkillEffectStyle.Slash);
                case "Second Wind": case "Field Remedy": case "Restore": case "Healing Prayer": case "Renewal": case "Sanctuary": return new SkillPresentation(SkillEffectStyle.Heal,true);
                case "Shield Bash": return new SkillPresentation(SkillEffectStyle.ShieldBash);
                case "Flame Cleave": return new SkillPresentation(SkillEffectStyle.Cleave);
                case "Holy Light": case "Judgment": return new SkillPresentation(SkillEffectStyle.HolyStrike,true);
                case "Radiant Blade": case "Blessed Strike": return new SkillPresentation(SkillEffectStyle.HolyStrike);
                case "Aegis": case "Sacred Resolve": case "Frost Ward": case "Mana Barrier": case "Divine Ward": return new SkillPresentation(SkillEffectStyle.Barrier,true);
                case "Arcane Burst": return new SkillPresentation(SkillEffectStyle.Burst,true);
                case "Flame Lance": return new SkillPresentation(SkillEffectStyle.Lance,true);
                case "Frost Drain": return new SkillPresentation(SkillEffectStyle.Drain,true);
                case "Starfall": return new SkillPresentation(SkillEffectStyle.Meteor,true);
                case "Piercing Shot": case "Evasive Shot": case "Steady Aim": return new SkillPresentation(SkillEffectStyle.Arrow);
                case "Double Shot": return new SkillPresentation(SkillEffectStyle.Volley,false,2);
                case "Barrage": return new SkillPresentation(SkillEffectStyle.Volley,false,3);
                case "Backstab": case "Shadow Strike": return new SkillPresentation(SkillEffectStyle.Shadow);
                case "Twin Fangs": return new SkillPresentation(SkillEffectStyle.TwinStrike);
                case "Smoke Veil": return new SkillPresentation(SkillEffectStyle.Poison,true);
                case "Leeching Cut": return new SkillPresentation(SkillEffectStyle.Drain);
                default: return Basic(kind);
            }
        }
        public static SkillPresentation Ultimate(HeroClass kind,UltimateDefinition skill) {
            switch(kind) {
                case HeroClass.Knight: return new SkillPresentation(skill.AreaDamage?SkillEffectStyle.Cleave:SkillEffectStyle.HeavyStrike);
                case HeroClass.Paladin: return new SkillPresentation(SkillEffectStyle.HolyStrike,true);
                case HeroClass.Sorceress: return new SkillPresentation(skill.AreaDamage?SkillEffectStyle.Meteor:skill.Affinity==Element.Lightning?SkillEffectStyle.Burst:SkillEffectStyle.Lance,true);
                case HeroClass.Ranger: return new SkillPresentation(skill.AreaDamage?SkillEffectStyle.Volley:SkillEffectStyle.Arrow,false,skill.AreaDamage?3:1);
                case HeroClass.Rogue: return new SkillPresentation(skill.AreaDamage?SkillEffectStyle.Poison:SkillEffectStyle.Shadow,skill.AreaDamage);
                default: return new SkillPresentation(SkillEffectStyle.HolyStrike,true);
            }
        }
    }
}
