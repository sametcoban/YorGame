using System;
using System.Collections.Generic;
namespace Ashlight {
    public sealed class EnemyGuideEntry {
        public readonly string Key;
        public readonly int Encounter;
        public readonly EnemyDefinition Enemy;
        public EnemyGuideEntry(int encounter,EnemyDefinition enemy) { Encounter=encounter; Enemy=enemy; Key=(encounter/5)+"|"+enemy.Name+"|"+enemy.Role+"|"+enemy.Weakness+"|"+enemy.Resistance; }
        public string Advice {
            get {
                if(Enemy.Name=="Cinder Sovereign") return "Interrupt the charge, or deal 60 damage before its next strike to weaken the inferno.";
                switch(Enemy.Role) {
                    case EnemyRole.Healer:return "Focus or interrupt healing. Cleanse removes harmful effects from your allies.";
                    case EnemyRole.Support:return "Interrupt wards or dispel shields before dealing damage.";
                    case EnemyRole.Defender:return "Guards protect rear enemies. Rogues bypass protection; expose the guard.";
                    case EnemyRole.Archer:return "Rear strikes ignore taunt. Equip armor on back-row heroes.";
                    case EnemyRole.Caster:return "Party shields help against area spells. Cleanse clears harmful effects.";
                    default:return "Taunt with a healthy defender; armor and shields reduce direct hits.";
                }
            }
        }
        static readonly IReadOnlyList<EnemyGuideEntry> catalog=Build();
        public static IReadOnlyList<EnemyGuideEntry> Catalog { get { return catalog; } }
        static IReadOnlyList<EnemyGuideEntry> Build() {
            var entries=new List<EnemyGuideEntry>(); var keys=new HashSet<string>();
            for(int stage=0;stage<ChapterDefinition.TotalStages;stage++) for(int wave=0;wave<3;wave++)
                foreach(var enemy in ChapterDefinition.WaveAt(stage,wave,3)) { var entry=new EnemyGuideEntry(stage,enemy); if(keys.Add(entry.Key)) entries.Add(entry); }
            return entries.AsReadOnly();
        }
        public static EnemyGuideEntry Find(string key) { foreach(var entry in Catalog) if(entry.Key==key) return entry; return null; }
    }
    public sealed partial class Battle {
        readonly HashSet<string> discoveredEnemies=new HashSet<string>();
        public string[] DiscoveredEnemies { get { var entries=new List<string>(discoveredEnemies); entries.Sort(StringComparer.Ordinal); return entries.ToArray(); } }
        public bool EnemyDiscovered(string key) { return discoveredEnemies.Contains(key); }
        void DiscoverEnemies() { if(!campaign || IsDailyBoss) return; foreach(var enemy in enemies) { var entry=new EnemyGuideEntry(EnemyEncounter,enemy.Definition); if(!discoveredEnemies.Contains(entry.Key) && EnemyGuideEntry.Find(entry.Key)!=null) discoveredEnemies.Add(entry.Key); } }
        public string UpgradeGuidance(string id) {
            var hero=HeroProgress(id); if(hero==null) return "Select a recruited hero.";
            string level=hero.Level==PartyHero.MaxLevel?"LEVEL · Maximum own level reached.":"LEVEL · "+(hero.NextLevelXP-hero.Experience)+" XP to own Lv "+(hero.Level+1)+". Earn from victories and Idle Rewards.";
            if(IsShared(id)) level+=" Shared level comes from your five strongest own levels.";
            string ascension=hero.UpgradeRank==PartyHero.MaxUpgradeRank?"ASCENSION · Maximum rank reached.":hero.Shards>=hero.UpgradeCost?"ASCENSION READY · Spend "+hero.UpgradeCost+" shards for "+hero.NextAscension+".":"ASCENSION · Need "+(hero.UpgradeCost-hero.Shards)+" more shards. Duplicate summons, story copies and chapter milestones grant shards; Tower rewards its active team.";
            string talents="TALENTS · All unlocked choices are assigned.";
            for(int tier=0;tier<3;tier++) if(hero.TalentChoice(tier)<0) { int unlock=ClassTalents.UnlockLevel(tier); talents=hero.Level>=unlock?"TALENT READY · Tier "+(tier+1)+" is a free choice in Talents.":"TALENTS · Next tier unlocks at own Lv "+unlock+" (shared levels do not unlock talents)."; break; }
            string gear="GEAR · All equipped types are +10. Compare stronger loot in Equipment.";
            for(int slot=0;slot<3;slot++) { var item=hero.EquipmentStats((GearSlot)slot); if(item==null) { gear="GEAR · Empty "+(GearSlot)slot+" slot. Equip owned loot, or earn gear from victories."; break; } if(item.Enhancement<10) { gear="FORGE · "+item.Name+" → +"+(item.Enhancement+1)+" costs "+item.EnhanceCost+" materials (you have "+ForgeMaterials+"). Earn materials from victories and Idle Rewards."; break; } }
            return level+"\n\n"+ascension+"\n\n"+talents+"\n\n"+gear;
        }
        public const int DailyBossRoundLimit=8, DailyBossCrystals=35, DailyBossMaterials=30;
        long bossClockDay,bossClaimDay;
        readonly int[] bossRecords=new int[3];
        Battle bossOwner;
        int bossIndex,bossEncounter,bossHealth;
        long runDay;
        bool runClaimed;
        public bool IsDailyBoss { get { return bossOwner!=null; } }
        public int RunBossIndex { get { return bossIndex; } }
        public int DailyBossRounds { get; private set; }
        public int DailyBossScore { get { int score=0; foreach(var member in party) score+=member.EncounterDamage; return Math.Min(1000000,score); } }
        void RefreshBossDay() { bossClockDay=Math.Max(bossClockDay,utcNow().Date.Ticks); }
        public int DailyBossIndex { get { RefreshBossDay(); return (int)(bossClockDay/TimeSpan.TicksPerDay%3); } }
        public static int BossEncounter(int index) { return new[]{4,14,29}[index]; }
        public string DailyBossName { get { return ChapterDefinition.Catalog[BossEncounter(DailyBossIndex)/5].Enemies[4].Name; } }
        public int DailyBossBest(int index) { return bossRecords[index]; }
        public bool DailyBossRewardAvailable { get { RefreshBossDay(); return bossClaimDay<bossClockDay; } }
        public bool CanStartDailyBoss { get { return campaign && !IsDailyBoss && !InTower && encounter>=10 && (CanChangeParty || CampaignComplete); } }
        public Battle StartDailyBoss() {
            if(!CanStartDailyBoss) return null;
            int index=DailyBossIndex;
            var run=new Battle(true,enableClassPassives:ClassPassivesEnabled,enableFactions:FactionsEnabled,utcNow:utcNow);
            if(!run.RestoreProgress(ExportProgress())) return null;
            run.bossOwner=this; run.bossIndex=index; run.bossEncounter=BossEncounter(index); run.runDay=bossClockDay;
            run.bossHealth=Math.Min(1000000,Math.Max(1500,TeamPower*3)); run.Reset(); return run;
        }
        public bool FinishDailyBoss(Battle run,out bool rewarded) {
            rewarded=false;
            if(run!=null && run.IsWeeklyTrial) return FinishWeeklyTrial(run,out rewarded);
            if(run==null || run.bossOwner!=this || run.runClaimed || (run.Current!=Phase.Lost && run.Current!=Phase.Won)) return false;
            run.runClaimed=true;
            if(run.DailyBossScore>0) { RecordDaily(0); RecordBonds(run.Party); }
            foreach(var key in run.discoveredEnemies) discoveredEnemies.Add(key);
            var fought=new EnemyGuideEntry(run.bossEncounter,run.Enemy); if(EnemyGuideEntry.Find(fought.Key)!=null) discoveredEnemies.Add(fought.Key);
            bossRecords[run.bossIndex]=Math.Max(bossRecords[run.bossIndex],run.DailyBossScore);
            if(run.DailyBossScore>0 && run.runDay>bossClaimDay) {
                bossClaimDay=run.runDay; Crystals=Math.Min(1000000,Crystals+DailyBossCrystals); ForgeMaterials=Math.Min(1000000,ForgeMaterials+DailyBossMaterials); rewarded=true;
            }
            return true;
        }
    }
}
