using System;
namespace Ashlight {
    public sealed partial class Battle {
        int claimedRelics;
        public bool RelicClaimed(int chapter) { return chapter>=0 && chapter<ChapterDefinition.Catalog.Count && (claimedRelics&(1<<chapter))!=0; }
        public bool CanClaimBossRelic(int chapter) { return campaign && !InTower && !IsDailyBoss && (CanChangeParty || CampaignComplete) && ChapterCleared(chapter) && !RelicClaimed(chapter); }
        public bool ClaimBossRelic(int chapter) { if(!CanClaimBossRelic(chapter)) return false; var item=GearDefinition.BossRelic(chapter); inventory[item.Id]=Math.Min(1000000,GearCount(item.Id)+1); claimedRelics|=1<<chapter; return true; }
        public int MasteryCost(string id) { var hero=HeroProgress(id); return hero==null || hero.MasteryRank>=PartyHero.MaxMasteryRank?0:500+hero.MasteryRank*250; }
        public bool CanMasterHero(string id) { var hero=HeroProgress(id); return campaign && !IsDailyBoss && encounter>=60 && (CanChangeParty || CampaignComplete) && hero!=null && hero.Level==20 && hero.MasteryRank<PartyHero.MaxMasteryRank && ForgeMaterials>=MasteryCost(id); }
        public bool MasterHero(string id) { if(!CanMasterHero(id)) return false; var hero=HeroProgress(id); ForgeMaterials-=MasteryCost(id); hero.MasteryRank++; hero.RefreshStats(); hero.Restore(); return true; }
        bool PairInRows(HeroClass first,HeroClass second,bool front) {
            bool a=false,b=false; for(int i=0;i<party.Count;i++) if(IsFrontRow(i)==front) { a|=party[i].Identity.Class==first; b|=party[i].Identity.Class==second; } return a && b;
        }
        bool TechniquesUnlocked { get { return FactionsEnabled && encounter>=60 && !IsFormationPuzzle; } }
        public string FormationSynergySummary { get {
            if(!TechniquesUnlocked) return "FORMATION TECHNIQUES · Unlock at chapter 13";
            string text="";
            if(PairInRows(HeroClass.Knight,HeroClass.Paladin,true)) text+="BULWARK · Front Knight + Paladin: each starts with 25 guard.\n";
            if(PairInRows(HeroClass.Ranger,HeroClass.Rogue,false)) text+="AMBUSH · Rear Ranger + Rogue: each starts with 15 guard.\n";
            if(PairInRows(HeroClass.Sorceress,HeroClass.Cleric,false)) text+="CONDUIT · Rear Sorceress + Cleric: team starts with 10 energy.\n";
            return text==""?"No technique active · Front Knight/Paladin; rear Ranger/Rogue or Sorceress/Cleric.":text.TrimEnd();
        } }
        bool techniquesApplied;
        void PrepareFormationTechniques() { if(techniquesApplied || !TechniquesUnlocked || (!IsDailyBoss && !CanChangeParty)) return; techniquesApplied=true; ApplyFormationTechniques(); }
        void ApplyFormationTechniques() {
            if(!TechniquesUnlocked) return;
            if(PairInRows(HeroClass.Knight,HeroClass.Paladin,true)) for(int i=0;i<party.Count;i++) if(IsFrontRow(i)) party[i].Guard+=25;
            if(PairInRows(HeroClass.Ranger,HeroClass.Rogue,false)) for(int i=0;i<party.Count;i++) if(!IsFrontRow(i) && (party[i].Identity.Class==HeroClass.Ranger || party[i].Identity.Class==HeroClass.Rogue)) party[i].Guard+=15;
            if(PairInRows(HeroClass.Sorceress,HeroClass.Cleric,false)) foreach(var hero in party) hero.Energy=Math.Min(PartyHero.MaxEnergy,hero.Energy+10);
        }
    }
}
