using System;
namespace Ashlight {
    public enum Debuff { None, Burn, Poison, Chill, Shock, Weaken }
    public sealed class EnemySkill {
        public readonly string Name;
        public readonly int Damage;
        public readonly Element Element;
        public readonly bool PartyWide;
        public readonly Debuff Status;
        public EnemySkill(string name, int damage, Element element, bool partyWide=false, Debuff status=Debuff.None) {
            Name=name; Damage=damage; Element=element; PartyWide=partyWide; Status=status;
        }
    }
    public static class EnemySkills {
        public static EnemySkill For(int encounter, bool boss, int completedStrikes) {
            int chapter=encounter/5;
            var element=new[]{Element.Physical,Element.Cold,Element.Fire,Element.Lightning,Element.Poison,Element.Light}[Math.Min(5,Math.Max(0,chapter))];
            var status=new[]{Debuff.None,Debuff.Chill,Debuff.Burn,Debuff.Shock,Debuff.Poison,Debuff.Weaken}[Math.Min(5,Math.Max(0,chapter))];
            int pattern=completedStrikes%3;
            if(pattern==0) return new EnemySkill("Quick Strike",35,Element.Physical);
            if(pattern==1) return new EnemySkill(chapter==0?"Crushing Blow":element+" Hex",chapter==0?42:24,element,false,status);
            return boss ? new EnemySkill(chapter==0?"Sweeping Strike":element+" Tempest",22,element,true,status) :
                new EnemySkill("Heavy Strike",45,element);
        }
    }
}
