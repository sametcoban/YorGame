using System;
namespace Ashlight {
    public enum Debuff { None, Burn, Poison, Chill, Shock, Weaken }
    public enum EnemyAction { Attack, Charge, HealPartner, Shield }
    public sealed class EnemySkill {
        public readonly string Name;
        public readonly int Damage;
        public readonly Element Element;
        public readonly bool PartyWide, BackRow;
        public readonly Debuff Status;
        public readonly EnemyAction Action;
        public EnemySkill(string name, int damage, Element element, bool partyWide=false, Debuff status=Debuff.None, EnemyAction action=EnemyAction.Attack, bool backRow=false) {
            Action=action; BackRow=backRow; Name=name; Damage=damage; Element=element; PartyWide=partyWide; Status=status;
        }
    }
    public static class EnemySkills {
        public static EnemySkill ForRole(int encounter, EnemyDefinition enemy, int completedStrikes) {
            if(enemy.IsBoss) return For(encounter,true,completedStrikes);
            var basis=For(encounter,false,1);
            switch(enemy.Role) {
                case EnemyRole.Archer: return new EnemySkill("Rear Volley",32,basis.Element,backRow:true);
                case EnemyRole.Caster: return completedStrikes%3==2?new EnemySkill("Arcane Tempest",16,basis.Element,true,basis.Status):new EnemySkill("Hex Bolt",28,basis.Element,status:basis.Status);
                case EnemyRole.Healer: return completedStrikes%3==1?new EnemySkill("Dark Renewal",0,basis.Element,action:EnemyAction.HealPartner):new EnemySkill("Withering Touch",22,basis.Element);
                case EnemyRole.Support: return completedStrikes%3==1?new EnemySkill("Warding Sigil",0,basis.Element,action:EnemyAction.Shield):new EnemySkill("Shadow Bolt",24,basis.Element);
                case EnemyRole.Bruiser: return new EnemySkill("Rending Assault",40,Element.Physical);
                default: return new EnemySkill("Shield Bash",30,Element.Physical);
            }
        }
        public static EnemySkill For(int encounter, bool boss, int completedStrikes) {
            int chapter=encounter/5;
            var element=new[]{Element.Physical,Element.Cold,Element.Fire,Element.Lightning,Element.Poison,Element.Light,Element.Fire,Element.Cold,Element.Light,Element.Fire,Element.Cold,Element.Poison}[Math.Min(11,Math.Max(0,chapter))];
            var status=new[]{Debuff.None,Debuff.Chill,Debuff.Burn,Debuff.Shock,Debuff.Poison,Debuff.Weaken,Debuff.Burn,Debuff.Chill,Debuff.Weaken,Debuff.Burn,Debuff.Chill,Debuff.Poison}[Math.Min(11,Math.Max(0,chapter))];
            int pattern=completedStrikes%3;
            if(pattern==0) return new EnemySkill("Quick Strike",35,Element.Physical);
            if(pattern==1) return new EnemySkill(chapter==0?"Crushing Blow":element+" Hex",chapter==0?42:24,element,false,status);
            return boss ? new EnemySkill(chapter==0?"Sweeping Strike":element+" Tempest",22,element,true,status) :
                new EnemySkill("Back Row Strike",45,element,backRow:true);
        }
    }
}
