using System;
using System.Collections.Generic;
namespace Ashlight {
    public static class HeroBonds {
        static readonly string[] Memories={
            "Rowan carries a lantern repaired with wire from a ruined village. Its last keeper asked for a safe road, not revenge.","Lucan's shield still bears the dent from a gate held open for strangers.","Elara learned her first spell thawing a river so her family could cross.","Finn keeps a map with one unmarked path: the way home.","Wren leaves a copper coin wherever an old debt is finally paid.","Tessa's travelling kit includes letters she could never deliver.",
            "Aldric remembers every recruit who stood beside him at the broken wall.","Mira kept her oath after the order that wrote it abandoned her.","Nyra hears echoes in crystals that other mages discard as flawed.","Sylva plants seeds beside roads that armies have burned.","Kestrel once stole a noble's seal to free prisoners whose names were missing.","Jonas learned that listening can save a life before any prayer is spoken.",
            "Brenna's armor was forged from the tools of her lost workshop.","Garrick carries the bell from a chapel whose roof he could not save.","Selene charts the nights when the stars vanish above the old throne.","Tarin marks arrows with the names of places he means to protect.","Silas keeps a broken lock as a reminder that every prison has a weakness.","Amara studies the scars left by healing magic as carefully as wounds.",
            "Kaelan's blade is plain. He gave its silver ornaments to a starving town.","Aurelia refuses to polish the scorch marks that proved her first rescue.","Vesper collects songs from cities that no longer appear on maps.","Liora follows migrating birds through lands that soldiers call impassable.","Raven hides the names of informants in a book of nursery rhymes.","Elyse lights candles for people whose faith has failed them.",
            "Seraphine earned her command by bringing survivors home, not by taking a throne.","Solenne carries a sunrise banner through places that have forgotten its colors.","Astra believes every fallen star remembers the sky it came from.","Cael can find a trail in ash, but still asks his companions which road to take.","Nyx broke the last contract that asked her to silence a child.","Ilyra treats an enemy's wounds before asking who ordered the battle."
        };
        public static HeroDefinition Companion(HeroDefinition hero) {
            var faction=new List<HeroDefinition>(); foreach(var candidate in HeroDefinition.Catalog) if(candidate.Faction==hero.Faction) faction.Add(candidate);
            for(int i=0;i<faction.Count;i++) if(faction[i].Id==hero.Id) return faction[(i+1)%faction.Count]; return null;
        }
        public static string Story(HeroDefinition hero,int chapter) {
            if(hero==null || chapter<0 || chapter>2) throw new ArgumentOutOfRangeException("bond chapter");
            if(chapter==0) return Memories[(int)hero.Quality*6+(int)hero.Class];
            if(chapter==1) return hero.Name+" begins to trust the company after sharing its dangers. During a quiet watch, the story behind "+hero.IdentityKeepsake()+" is finally told. The next journey feels less like a solitary duty.";
            var partner=Companion(hero); return hero.Name+" and "+partner.Name+" meet beside the campfire. Their paths through the "+hero.FactionName+" were different, but both remember people they could not save. They promise to judge the next road by who returns from it, not who wins its battles.";
        }
        static string IdentityKeepsake(this HeroDefinition hero) { return hero.Class==HeroClass.Knight?"the worn armor":hero.Class==HeroClass.Paladin?"the old oath":hero.Class==HeroClass.Sorceress?"the spell journal":hero.Class==HeroClass.Ranger?"the folded map":hero.Class==HeroClass.Rogue?"the broken seal":"the travelling medicine kit"; }
    }
    public sealed partial class Battle {
        long weeklyClock,weeklyClaim;
        readonly int[] weeklyRecords=new int[3];
        bool weeklyTrial; int trialIndex; long trialWeek;
        public bool IsWeeklyTrial { get { return weeklyTrial; } }
        public int TrialIndex { get { return trialIndex; } }
        public bool WeeklyGoalMet { get { return IsWeeklyTrial && (Current==Phase.Won || (DailyBossRounds>=8 && party.Exists(hero=>hero.Health>0))); } }
        public double SkillHealingRule { get { return IsWeeklyTrial && trialIndex==1?.5:1; } }
        void RefreshWeek() { var date=utcNow().Date; int offset=((int)date.DayOfWeek+6)%7; weeklyClock=Math.Max(weeklyClock,date.AddDays(-offset).Ticks); }
        public int WeeklyIndex { get { RefreshWeek(); return (int)(weeklyClock/TimeSpan.TicksPerDay/7%3); } }
        public static string WeeklyName(int index) { return new[]{"Venom Communion","Fading Restoration","Iron Eclipse"}[index]; }
        public static EnemyDefinition WeeklyEnemy(int index) { return ChapterDefinition.Catalog[new[]{24,14,29}[index]/5].Enemies[4]; }
        public static string WeeklyRules(int index) { return new[]{"Connected attacks inflict Poison; Poison ticks deal 12 damage. Cleanse helps.","Skill healing is halved. Ultimates and class passives retain their healing.","Boss starts with 80 shield and renews 20 after an attack (cap 80). Dispel or interrupt wards."}[index]; }
        public int WeeklyBest(int index) { return weeklyRecords[index]; }
        public bool WeeklyRewardAvailable { get { RefreshWeek(); return weeklyClaim<weeklyClock; } }
        public bool CanStartWeekly { get { return CanStartDailyBoss && encounter>=15; } }
        public Battle StartWeeklyTrial() {
            if(!CanStartWeekly) return null; int index=WeeklyIndex;
            var run=StartDailyBoss(); run.weeklyTrial=true; run.trialIndex=index; run.trialWeek=weeklyClock; run.bossEncounter=new[]{24,14,29}[index]; run.Reset(); return run;
        }
        bool FinishWeeklyTrial(Battle run,out bool rewarded) {
            rewarded=false;
            if(run.bossOwner!=this || run.runClaimed || (run.Current!=Phase.Won && run.Current!=Phase.Lost)) return false;
            run.runClaimed=true; weeklyRecords[run.trialIndex]=Math.Max(weeklyRecords[run.trialIndex],run.DailyBossScore);
            var fought=new EnemyGuideEntry(run.bossEncounter,run.Enemy); if(EnemyGuideEntry.Find(fought.Key)!=null) discoveredEnemies.Add(fought.Key);
            if(run.DailyBossScore>0) { RecordDaily(0); RecordBonds(run.Party); }
            if(run.DailyBossScore>0 && run.WeeklyGoalMet && run.trialWeek>weeklyClaim) {
                weeklyClaim=run.trialWeek; Crystals=Math.Min(1000000,Crystals+150); ForgeMaterials=Math.Min(1000000,ForgeMaterials+150);
                foreach(var hero in run.Party) { var owned=HeroProgress(hero.Identity.Id); owned.Shards=Math.Min(1000000,owned.Shards+5); } rewarded=true;
            }
            return true;
        }
        void RecordBonds(IReadOnlyList<PartyHero> participants) { foreach(var hero in participants) { var owned=HeroProgress(hero.Identity.Id); if(owned!=null) owned.BondBattles=Math.Min(1000000,owned.BondBattles+1); } }
        public bool BondUnlocked(string id,int chapter) {
            var hero=HeroProgress(id); if(hero==null || chapter<0 || chapter>2) return false;
            if(chapter==0) return true; if(chapter==1) return hero.BondBattles>=3;
            var partner=HeroProgress(HeroBonds.Companion(hero.Identity).Id); return hero.BondBattles>=10 && partner!=null && partner.BondBattles>=3;
        }
    }
}
