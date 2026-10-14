using System;
namespace Ashlight {
    public enum SignatureEffect { SelfGuard, FrontGuard, SelfHeal, SelfEnergy, PartyGuard, LowestHeal, LowestGuard, PartyHeal, SelfCleanse, Expose, PartyEnergy, BackGuard, BackCleanse }
    public sealed class HeroSignature {
        public readonly string Name;
        public readonly SignatureEffect Effect;
        public readonly int Amount;
        HeroSignature(string name,SignatureEffect effect,int amount) { Name=name; Effect=effect; Amount=amount; }
        static readonly string[] Names={"Lantern Vigil","Stag's Shelter","Iron Remembrance","Ashen Momentum","Ember Oath","Dawn's Mercy","Grove Covenant","Grave Oath","Sunlit Resolve","Solar Sanctuary","Spark Memory","Nightglass Mark","Winter Clarity","Veil Current","Starward Company","Trail Instinct","Leafward Shelter","Hunter's Remedy","Moonlit Quarry","Horizon Signal","Last Shadow","Stolen Vitality","Silent Momentum","Crow's Mark","Night's Escape","Roadside Mercy","Quiet Absolution","Rootward Prayer","Dawn Renewal","Last Benediction"};
        static readonly SignatureEffect[] Effects={SignatureEffect.SelfGuard,SignatureEffect.FrontGuard,SignatureEffect.SelfHeal,SignatureEffect.SelfEnergy,SignatureEffect.PartyGuard,SignatureEffect.LowestHeal,SignatureEffect.LowestGuard,SignatureEffect.PartyHeal,SignatureEffect.SelfCleanse,SignatureEffect.FrontGuard,SignatureEffect.SelfEnergy,SignatureEffect.Expose,SignatureEffect.SelfCleanse,SignatureEffect.PartyEnergy,SignatureEffect.PartyGuard,SignatureEffect.SelfEnergy,SignatureEffect.BackGuard,SignatureEffect.SelfHeal,SignatureEffect.Expose,SignatureEffect.PartyEnergy,SignatureEffect.SelfGuard,SignatureEffect.SelfHeal,SignatureEffect.SelfEnergy,SignatureEffect.Expose,SignatureEffect.BackCleanse,SignatureEffect.LowestHeal,SignatureEffect.SelfCleanse,SignatureEffect.PartyGuard,SignatureEffect.PartyHeal,SignatureEffect.PartyEnergy};
        public static HeroSignature For(HeroDefinition hero) { if(hero==null) throw new ArgumentNullException("hero"); int index=(int)hero.Class*5+(int)hero.Quality; return new HeroSignature(Names[index],Effects[index],5+index); }
        public string Description { get {
            string benefit=Effect==SignatureEffect.SelfGuard?"Gain "+Amount+" guard":Effect==SignatureEffect.FrontGuard?"Front allies gain "+Amount+" guard":Effect==SignatureEffect.PartyGuard?"Living allies gain "+Amount+" guard":Effect==SignatureEffect.BackGuard?"Rear allies gain "+Amount+" guard":Effect==SignatureEffect.SelfHeal?"Recover "+Amount+" HP":Effect==SignatureEffect.LowestHeal?"Heal the most wounded living ally for "+Amount+" HP":Effect==SignatureEffect.PartyHeal?"Heal living allies for "+Amount+" HP":Effect==SignatureEffect.LowestGuard?"Most wounded living ally gains "+Amount+" guard":Effect==SignatureEffect.SelfEnergy?"Gain "+Amount+" energy":Effect==SignatureEffect.PartyEnergy?"Living allies gain "+Amount+" energy":Effect==SignatureEffect.SelfCleanse?"Cleanse yourself and gain "+Amount+" guard":Effect==SignatureEffect.BackCleanse?"Cleanse rear allies and give each "+Amount+" guard":"Expose the target for one round and gain "+Amount+" guard";
            return Name+" · "+benefit+". Once per wave after your first action; requires Mastery 1.";
        } }
    }
    public sealed partial class Battle {
        void ApplySignature() {
            if(!ClassPassivesEnabled || Active.MasteryRank<1 || Active.SignatureUsed) return;
            Active.SignatureUsed=true; var sig=HeroSignature.For(Active.Identity);
            LastOffensivePassive=(string.IsNullOrEmpty(LastOffensivePassive)?"":LastOffensivePassive+" · ")+sig.Name;
            int lowest=ActiveIndex;
            for(int i=0;i<party.Count;i++) if(party[i].Health>0 && (long)party[i].Health*party[lowest].Definition.MaxHealth<(long)party[lowest].Health*party[i].Definition.MaxHealth) lowest=i;
            for(int i=0;i<party.Count;i++) {
                var hero=party[i]; if(hero.Health<=0) continue;
                bool self=i==ActiveIndex,front=IsFrontRow(i);
                bool guard=sig.Effect==SignatureEffect.PartyGuard || sig.Effect==SignatureEffect.SelfGuard && self || sig.Effect==SignatureEffect.FrontGuard && front || sig.Effect==SignatureEffect.BackGuard && !front || sig.Effect==SignatureEffect.LowestGuard && i==lowest;
                if(guard) { hero.Guard+=sig.Amount; LastSkillGuard[i]+=sig.Amount; }
                bool heal=sig.Effect==SignatureEffect.PartyHeal || sig.Effect==SignatureEffect.SelfHeal && self || sig.Effect==SignatureEffect.LowestHeal && i==lowest;
                if(heal) { int amount=Math.Min(sig.Amount,hero.Definition.MaxHealth-hero.Health); hero.Health+=amount; Active.EncounterHealing=Math.Min(1000000,Active.EncounterHealing+amount); }
                if(sig.Effect==SignatureEffect.SelfEnergy && self || sig.Effect==SignatureEffect.PartyEnergy) hero.Energy=Math.Min(PartyHero.MaxEnergy,hero.Energy+sig.Amount);
                if(sig.Effect==SignatureEffect.SelfCleanse && self || sig.Effect==SignatureEffect.BackCleanse && !front) { hero.Status=Debuff.None; hero.StatusRounds=0; LastSkillCleanse[i]=true; hero.Guard+=sig.Amount; LastSkillGuard[i]+=sig.Amount; }
            }
            if(sig.Effect==SignatureEffect.Expose && enemies[SelectedEnemyIndex].Health>0) enemies[SelectedEnemyIndex].ExposedRounds=1;
            if(sig.Effect==SignatureEffect.Expose) { Active.Guard+=sig.Amount; LastSkillGuard[ActiveIndex]+=sig.Amount; }
        }
    }
}
