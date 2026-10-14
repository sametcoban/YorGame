using System;
namespace Ashlight {
    public enum BossEffectStyle { Bell, Tide, Thorns, Stars, Forge, Eclipse }
    // Presentation only: never changes targets, defense rolls, damage or enemy turn order.
    public sealed class BossCue {
        public readonly BossEffectStyle Style;
        public readonly string Heading, Counter;
        public readonly Element Element;
        public readonly bool Casting;
        public const float WarningSeconds=1.4f;
        BossCue(BossEffectStyle style,string heading,string counter,Element element,bool casting) { Style=style; Heading=heading; Counter=counter; Element=element; Casting=casting; }
        public static BossCue For(int encounter,EnemyDefinition enemy,EnemySkill skill) {
            if(enemy==null || skill==null || !enemy.IsBoss) return null;
            if(encounter<0 || encounter>=ChapterDefinition.TotalStages) throw new ArgumentOutOfRangeException("encounter");
            int chapter=encounter/5;
            var style=chapter>=12?(BossEffectStyle)(chapter-12):new[]{BossEffectStyle.Bell,BossEffectStyle.Tide,BossEffectStyle.Forge,BossEffectStyle.Stars,BossEffectStyle.Thorns,BossEffectStyle.Eclipse}[chapter%6];
            string label,counter;
            switch(skill.Action) {
                case EnemyAction.HealPartner: label="ALLY RESTORATION"; counter="Focus the healer or interrupt its next action."; break;
                case EnemyAction.Shield: label="SHIELD RENEWAL"; counter="Dispel the shield or attack another enemy."; break;
                case EnemyAction.Charge: label="CHARGED ATTACK"; counter="Deal 60 damage before the next attack to disrupt Inferno."; break;
                case EnemyAction.Interrupted: label="INTERRUPTED"; counter="The boss loses this action."; break;
                default: label=skill.PartyWide?"PARTY ATTACK":skill.BackRow?"REAR-ROW ATTACK":"SINGLE TARGET";
                    counter=skill.PartyWide?"Party wards and healing protect against area attacks.":skill.BackRow?"Rear attacks ignore taunts; protect your back row.":"Taunts can redirect normal attacks; defense rolls automatically.";
                    if(skill.Status!=Debuff.None) counter+=" Applies "+skill.Status+" · Cleanse removes debuffs.";
                    break;
            }
            return new BossCue(style,enemy.Name+" · "+label+"\n"+skill.Name,counter,skill.Element,skill.Action!=EnemyAction.Attack || skill.PartyWide || skill.Element!=Element.Physical);
        }
    }
}
