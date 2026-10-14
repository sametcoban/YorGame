using System;
using System.Collections.Generic;
namespace Ashlight {
    public sealed partial class Battle {
        readonly HashSet<string> lockedGear=new HashSet<string>();
        readonly Dictionary<string,int> relicRefinements=new Dictionary<string,int>();
        long relicRaidDay; int relicRaidClaims,raidChapter=-1,objectiveClaims; long raidStartDay;
        bool objectiveKillSeen,objectiveFirstBack,restoredObjectiveOverride,restoredObjectiveMet;
        GearEnhancementData[] ExportRefinements() { var items=new List<GearEnhancementData>(); foreach(var pair in relicRefinements) items.Add(new GearEnhancementData{id=pair.Key,level=pair.Value}); return items.ToArray(); }
        public int RelicEssence { get; private set; }
        public bool IsRelicRaid { get { return raidChapter>=0; } }
        public int RaidChapter { get { return raidChapter; } }
        public bool GearLocked(string id) { return id!=null && lockedGear.Contains(id); }
        public bool SetGearLocked(string id,bool locked) { if(!campaign || IsDailyBoss || (!CanChangeParty && !CampaignComplete) || GearCount(id)==0) return false; return locked?lockedGear.Add(id):lockedGear.Remove(id); }
        public int RelicRefinement(string id) { int rank; return id!=null && relicRefinements.TryGetValue(id,out rank)?rank:0; }
        public int RefinementCost(string id) { var item=GearDefinition.Find(id); return item==null || item.RelicChapter<0 || RelicRefinement(id)>=5?0:5*(RelicRefinement(id)+1); }
        public bool CanRefineRelic(string id) { return campaign && !IsDailyBoss && (CanChangeParty || CampaignComplete) && GearCount(id)>0 && RefinementCost(id)>0 && RelicEssence>=RefinementCost(id); }
        public bool RefineRelic(string id) {
            if(!CanRefineRelic(id)) return false; RelicEssence-=RefinementCost(id); relicRefinements[id]=RelicRefinement(id)+1;
            foreach(var hero in recruits) { var member=HeroProgress(hero.Id); for(int slot=0;slot<3;slot++) if(member.Gear((GearSlot)slot)!=null && member.Gear((GearSlot)slot).Id==id) { member.SetRefinement((GearSlot)slot,RelicRefinement(id)); member.Restore(); } }
            return true;
        }
        void RefreshRelicDay() { long day=utcNow().Date.Ticks; if(day>relicRaidDay) { relicRaidDay=day; relicRaidClaims=0; } }
        public int RelicRaidRewardsLeft { get { RefreshRelicDay(); return 3-relicRaidClaims; } }
        public Battle StartRelicRaid(int chapter) {
            if(!CanStartDailyBoss || !ChapterCleared(chapter)) return null;
            RefreshRelicDay(); var run=StartDailyBoss(); run.raidChapter=chapter; run.raidStartDay=relicRaidDay; run.bossEncounter=chapter*5+4; run.bossHealth=Math.Min(1000000,Math.Max(1500,TeamPower*2)); run.Reset(); return run;
        }
        public bool RelicRaidGoalMet { get { return IsRelicRaid && (Current==Phase.Won || DailyBossRounds>=8 && party.Exists(hero=>hero.Health>0)); } }
        bool FinishRelicRaid(Battle run,out bool rewarded) {
            rewarded=false; if(run==null || run.bossOwner!=this || run.runClaimed || (run.Current!=Phase.Won && run.Current!=Phase.Lost)) return false;
            run.runClaimed=true; RefreshRelicDay();
            if(run.DailyBossScore>0) { RecordDaily(0); RecordBonds(run.Party); }
            if(run.DailyBossScore>0 && run.RelicRaidGoalMet && run.raidStartDay==relicRaidDay && relicRaidClaims<3) { relicRaidClaims++; RelicEssence=Math.Min(1000000,RelicEssence+5); ForgeMaterials=Math.Min(1000000,ForgeMaterials+75); rewarded=true; }
            return true;
        }
        public bool HasOptionalObjective { get { return campaign && !InTower && !IsDailyBoss && ChapterIndex>=18 && StageIndex==2; } }
        public string OptionalObjective { get { if(!HasOptionalObjective) return ""; return new[]{"Keep every hero alive","Defeat a rear enemy first in the final wave","Finish with every hero alive and above 25% HP"}[ChapterIndex%3]+" · Optional bonus: 5 relic essence"; } }
        public bool OptionalObjectiveMet { get {
            if(!HasOptionalObjective || Current!=Phase.Won) return false;
            if(restoredObjectiveOverride) return restoredObjectiveMet;
            if(ChapterIndex%3==1) return objectiveKillSeen && objectiveFirstBack;
            foreach(var hero in party) if(hero.Health<=0 || ChapterIndex%3==2 && hero.Health*4<hero.Definition.MaxHealth) return false;
            return true;
        } }
        void RecordObjectiveKill(int index) { if(HasOptionalObjective && WaveIndex==WaveCount-1 && !objectiveKillSeen) { objectiveKillSeen=true; objectiveFirstBack=enemies[index].Definition.Row==EnemyRow.Back; } }
        public bool ObjectiveClaimed(int chapter) { return chapter>=0 && chapter<24 && (objectiveClaims&(1<<chapter))!=0; }
        void ClaimOptionalObjective() { if(OptionalObjectiveMet && !ObjectiveClaimed(ChapterIndex)) { objectiveClaims|=1<<ChapterIndex; RelicEssence=Math.Min(1000000,RelicEssence+5); } }
        internal static int AllowedObjectives(int stage) { int chapters=stage/5; int mask=(1<<chapters)-1; if(stage%5>=3 && chapters<24) mask|=1<<chapters; return mask & ~((1<<18)-1); }
    }
}
