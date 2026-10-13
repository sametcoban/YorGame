using System;
namespace Ashlight {
    public enum Debuff { None, Burn, Poison, Chill, Shock, Weaken }
    public enum EnemyAction { Attack, Charge, HealPartner, Shield, Interrupted }
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
        public static string Family(int encounter) {
            return new[]{"Road Wardens","Frostbound","Ember Legion","Stormborn","Blight Court","Eclipse Host","Pyre Legion","Drowned Court","Crimson Covenant","Glass Legion","Rime Court","Abyssal Host"}[Math.Min(11,Math.Max(0,encounter/5))];
        }
        public static string Tactics(int encounter) {
            switch(encounter/5) {
                case 6: return "Pyre: burning third strikes · bring Fire-aligned defenders.";
                case 7: return "Drowned: chilling rear strikes · protect your back row.";
                case 8: return "Crimson: priests renew allies · focus the healer first.";
                case 9: return "Glass: guards ward allies · strike an unshielded target.";
                case 10: return "Rime: every third spell hits the party · spread Cold affinity.";
                case 11: return "Abyss: poison volleys and weakening hexes · bring healing.";
                default: return "Defenders protect rear enemies · Rogues bypass that protection.";
            }
        }
        static EnemySkill FamilyMove(int encounter,EnemyDefinition enemy,int completedStrikes) {
            int chapter=encounter/5, move=completedStrikes%3;
            if(chapter<6) return null;
            if(enemy.IsBoss) {
                if(move==1 && chapter==8) return new EnemySkill("Blood Communion",0,Element.Light,action:EnemyAction.HealPartner);
                if(move==1 && chapter==9) return new EnemySkill("Obsidian Aegis",0,Element.Fire,action:EnemyAction.Shield);
                if(move==2) return new EnemySkill(Family(encounter)+" Cataclysm",chapter==11?18:20,chapter==7 || chapter==10?Element.Cold:chapter==8?Element.Light:chapter==11?Element.Poison:Element.Fire,true,chapter==11?Debuff.Poison:chapter==7 || chapter==10?Debuff.Chill:chapter==8?Debuff.Weaken:Debuff.Burn);
                return null;
            }
            switch(chapter) {
                case 6:
                    if(move==2 && (enemy.Role==EnemyRole.Defender || enemy.Role==EnemyRole.Bruiser)) return new EnemySkill("Branding Strike",34,Element.Fire,status:Debuff.Burn);
                    break;
                case 7:
                    if(enemy.Role==EnemyRole.Archer) return new EnemySkill("Drowned Harpoon",28,Element.Cold,status:move==2?Debuff.Chill:Debuff.None,backRow:true);
                    break;
                case 8:
                    if(enemy.Role==EnemyRole.Healer) return move==1?new EnemySkill("Crimson Renewal",0,Element.Light,action:EnemyAction.HealPartner):new EnemySkill("Blood Hex",22,Element.Light,status:move==2?Debuff.Weaken:Debuff.None);
                    break;
                case 9:
                    if(enemy.Role==EnemyRole.Defender && move==1) return new EnemySkill("Glass Bulwark",0,Element.Fire,action:EnemyAction.Shield);
                    break;
                case 10:
                    if(enemy.Role==EnemyRole.Caster) return move==2?new EnemySkill("Whiteout",16,Element.Cold,true,Debuff.Chill):new EnemySkill("Rime Spear",26,Element.Cold);
                    break;
                case 11:
                    if(enemy.Role==EnemyRole.Archer) return new EnemySkill("Venom Volley",28,Element.Poison,status:move==2?Debuff.Poison:Debuff.None,backRow:true);
                    if(enemy.Role==EnemyRole.Caster) return move==2?new EnemySkill("Nightfall",12,Element.Poison,true,Debuff.Weaken):new EnemySkill("Void Hex",26,Element.Poison);
                    break;
            }
            return null;
        }
        public static EnemySkill ForRole(int encounter, EnemyDefinition enemy, int completedStrikes) {
            var familyMove=FamilyMove(encounter,enemy,completedStrikes); if(familyMove!=null) return familyMove;
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
