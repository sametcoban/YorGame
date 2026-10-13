using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum Phase { Player, EnemyWindup, EnemyStrike, WaveCleared, Won, Lost, Complete }
    public sealed class PartyHero {
        public HeroDefinition Identity { get; private set; }
        ClassDefinition stats;
        public ClassDefinition Definition { get { return stats; } }
        public const int MaxLevel=20, MaxUpgradeRank=5;
        internal int FactionBonus;
        readonly int[] talents={-1,-1,-1};
        public int TalentChoice(int tier) { return talents[tier]; }
        public bool HasTalent(int tier,int choice) { return Level>=ClassTalents.UnlockLevel(tier) && talents[tier]==choice; }
        internal void SetTalent(int tier,int choice) { talents[tier]=choice; }
        public int EnergyGain { get { return ActionEnergy+(Identity.Class!=HeroClass.Knight && Identity.Class!=HeroClass.Paladin && HasTalent(1,0)?10:0); } }
        public double HealingMultiplier { get { return Identity.Class==HeroClass.Paladin && HasTalent(0,1)?1.2:1; } }
        public double CriticalChance { get { return .25+(HasTalent(0,0)?.10:0)+(HasTalent(2,0)?.15:0); } }
        public double CriticalMultiplier { get { return 1.5+(HasTalent(0,1)?.25:0)+(HasTalent(2,1)?.25:0); } }
        public int Level { get; internal set; } = 1;
        internal int SharedLevel;
        public int CombatLevel { get { return Math.Max(Level,SharedLevel); } }
        public string LevelLabel { get { return CombatLevel>Level?CombatLevel+" (own "+Level+")":Level.ToString(); } }
        public int Power {
            get {
                double critical=Identity.Class==HeroClass.Rogue?1+CriticalChance*(CriticalMultiplier-1):1;
                double value=Definition.MaxHealth+Definition.AttackDamage*4*critical+Protection*10;
                for(int slot=0;slot<2;slot++) { var skill=Skill(slot); value+=skill.Damage*critical+skill.Healing*HealingMultiplier*(skill.PartyHealing?2:1)+skill.Guard; }
                var ultimate=Ultimate; value+=(ultimate.Damage*(ultimate.AreaDamage?1.5:1)*critical+ultimate.Healing*2+ultimate.Guard*2)*EnergyGain/100.0;
                foreach(Element element in Enum.GetValues(typeof(Element))) value+=(DamageBoost(element)-1)*Definition.AttackDamage*2;
                for(int tier=0;tier<3;tier++) if(TalentChoice(tier)>=0 && Level>=ClassTalents.UnlockLevel(tier)) value+=15;
                return Math.Max(1,(int)Math.Ceiling(value));
            }
        }
        public int Experience { get; internal set; }
        public int UpgradeRank { get; internal set; }
        public int Shards { get; internal set; }
        public int NextLevelXP { get { return 100+(Level-1)*25; } }
        public int UpgradeCost { get { return 20*(UpgradeRank+1); } }
        public HeroQuality Quality { get { return (HeroQuality)Math.Min(4,(int)Identity.Quality+UpgradeRank); } }
        public int AscensionStars { get { return Math.Max(0,(int)Identity.Quality+UpgradeRank-4); } }
        public string RarityLabel { get { return Quality+(AscensionStars>0?" ★"+AscensionStars:""); } }
        public string NextAscension { get { int rank=UpgradeRank+1; int stars=Math.Max(0,(int)Identity.Quality+rank-4); return ((HeroQuality)Math.Min(4,(int)Identity.Quality+rank))+(stars>0?" ★"+stars:""); } }
        public float Growth { get { return (1f+(CombatLevel-1)*.05f)*HeroDefinition.Multiplier(Quality)/HeroDefinition.Multiplier(Identity.Quality)*(1f+AscensionStars*.10f); } }
        readonly GearDefinition[] gear=new GearDefinition[3];
        readonly int[] gearEnhancements=new int[3];
        public GearDefinition Gear(GearSlot slot) { return gear[(int)slot]; }
        public int GearEnhancement(GearSlot slot) { return gearEnhancements[(int)slot]; }
        public GearDefinition EquipmentStats(GearSlot slot) { var item=Gear(slot); return item==null?null:item.Enhanced(GearEnhancement(slot)); }
        internal void SetGearEnhancement(GearSlot slot,int level) { gearEnhancements[(int)slot]=level; RefreshStats(); }
        internal void SetGear(GearSlot slot,GearDefinition item) { gear[(int)slot]=item; gearEnhancements[(int)slot]=0; RefreshStats(); }
        public int Protection { get { var armor=EquipmentStats(GearSlot.Armor); return armor==null?0:armor.Protection; } }
        public float DamageBoost(Element element) { var accessory=EquipmentStats(GearSlot.Accessory); return accessory!=null && accessory.Element==element?1+accessory.ElementBoost:1; }
        public UltimateDefinition Ultimate { get { var weapon=EquipmentStats(GearSlot.Weapon); return Identity.Ultimate.Scaled(Growth).WithTalentDamage((Identity.Class==HeroClass.Sorceress || Identity.Class==HeroClass.Ranger) && HasTalent(2,1)?20:0).WithDamageBonus(weapon==null?0:weapon.SkillDamage).WithTalentDamage(FactionBonus); } }
        internal void RefreshStats() {
            var weapon=EquipmentStats(GearSlot.Weapon); var armor=EquipmentStats(GearSlot.Armor);
            bool tank=Identity.Class==HeroClass.Knight || Identity.Class==HeroClass.Paladin;
            bool health=HasTalent(1,tank?0:1);
            bool skill=HasTalent(tank?1:0,1) && (tank || Identity.Class==HeroClass.Sorceress || Identity.Class==HeroClass.Ranger);
            stats=Identity.Stats.Scaled(Growth).WithTalentBonuses(health?15:0,skill?10:0).WithEquipment(armor==null?0:armor.Health,weapon==null?0:weapon.Attack,weapon==null?0:weapon.SkillDamage).WithFactionBonus(FactionBonus);
        }
        internal void GainExperience(int amount) {
            if(Level==MaxLevel) return;
            Experience+=amount;
            while(Level<MaxLevel && Experience>=NextLevelXP) { Experience-=NextLevelXP; Level++; }
            if(Level==MaxLevel) Experience=0;
            RefreshStats();
        }
        public int Health { get; internal set; }
        public int EncounterDamage { get; internal set; }
        public int EncounterHealing { get; internal set; }
        readonly int[] equipped = { 0, 1 };
        readonly int[] charges = { 2, 2 };
        public int Guard { get; internal set; }
        public const int MaxEnergy=100, ActionEnergy=25;
        public int Energy { get; internal set; }
        public Debuff Status { get; internal set; }
        public int StatusRounds { get; internal set; }
        public int TauntRounds { get; internal set; }
        public int AbilityCharges { get { return charges[0]; } }
        public SkillDefinition Skill(int slot) { return Definition.Skills[equipped[slot]]; }
        public int SkillIndex(int slot) { return equipped[slot]; }
        public int SkillCharges(int slot) { return charges[slot]; }
        internal void Equip(int slot, int index) { equipped[slot] = index; }
        internal void Consume(int slot) { charges[slot]--; }
        public bool Acted { get; internal set; }
        public PartyHero(HeroClass kind) : this(HeroDefinition.Common(kind)) { }
        public PartyHero(HeroDefinition identity) { Identity = identity; RefreshStats(); Restore(); }
        internal void Restore() { EncounterDamage=EncounterHealing=0; Health = Definition.MaxHealth; charges[0] = charges[1] = 2; Guard = 0; Energy = 0; Status = Debuff.None; StatusRounds = 0; TauntRounds=0; Acted = false; }
    }
    public sealed class BattleEnemy {
        public EnemyDefinition Definition { get; private set; }
        public int CompletedStrikes { get; internal set; }
        public bool Charged { get; internal set; }
        public int ChargeDamage { get; internal set; }
        public int Shield { get; internal set; }
        public bool InterruptQueued { get; internal set; }
        public int InterruptRecovery { get; internal set; }
        public int ExposedRounds { get; internal set; }
        public int Health { get; internal set; }
        public BattleEnemy(EnemyDefinition definition) { Definition = definition; Health = definition.MaxHealth; }
    }
    public enum CollectionSort { Power, Level, Ascension }
    public sealed class HeroReplacement {
        public readonly PartyHero Hero;
        public readonly int TeamPower, HealthDelta, AttackDelta, PowerDelta;
        public readonly int FactionBonus;
        public HeroReplacement(PartyHero hero,int teamPower,PartyHero previous,int previousTeamPower) { Hero=hero; TeamPower=teamPower; FactionBonus=hero.FactionBonus; HealthDelta=hero.Definition.MaxHealth-previous.Definition.MaxHealth; AttackDelta=hero.Definition.AttackDamage-previous.Definition.AttackDamage; PowerDelta=teamPower-previousTeamPower; }
    }
    public sealed class Battle {
        readonly List<PartyHero> party = new List<PartyHero>();
        readonly bool campaign;
        readonly bool waveBattles;
        public bool ClassPassivesEnabled { get; private set; }
        public bool FactionsEnabled { get; private set; }
        public int FactionCount(HeroFaction faction) { int count=0; foreach(var member in party) if(member.Identity.Faction==faction) count++; return count; }
        public int TeamFactionBonus { get { if(!FactionsEnabled) return 0; int best=0; foreach(HeroFaction faction in Enum.GetValues(typeof(HeroFaction))) best=Math.Max(best,FactionCount(faction)); return FactionRules.Bonus(best); } }
        public string FactionSummary { get { return "Dawn "+FactionCount(HeroFaction.DawnOrder)+" · Wilderkin "+FactionCount(HeroFaction.Wilderkin)+" · Veilborn "+FactionCount(HeroFaction.Veilborn)+" | "+(!FactionsEnabled?"SYNERGY OFF":TeamFactionBonus==0?"NO ACTIVE BONUS": "TEAM +"+TeamFactionBonus+"% HP & damage"); } }
        public string FactionProgress {
            get {
                if(!FactionsEnabled) return "Faction bonuses are disabled.";
                int best=0; foreach(HeroFaction faction in Enum.GetValues(typeof(HeroFaction))) best=Math.Max(best,FactionCount(faction));
                if(best>=5) return "Maximum faction bonus active · +20% HP & damage.";
                int target=best<3?3:best+1;
                return "NEXT · "+(target-best)+" more matching "+(target-best==1?"hero":"heroes")+" for +"+FactionRules.Bonus(target)+"%"+(target>PartyLimit?" · Unlock more party slots in later chapters.":"");
            }
        }
        void RefreshFactionStats() {
            int bonus=TeamFactionBonus;
            foreach(var member in savedHeroes.Values) { member.FactionBonus=party.Contains(member)?bonus:0; member.RefreshStats(); }
            foreach(var member in party) { member.FactionBonus=bonus; member.RefreshStats(); member.Restore(); }
        }
        readonly Random passiveRandom;
        public string LastOffensivePassive { get; private set; }
        public readonly Debuff[] LastPassiveCleanse=new Debuff[MaxPartySize];
        public readonly int[] LastPassiveGuard=new int[MaxPartySize], LastPassiveHealing=new int[MaxPartySize], LastPassiveProtection=new int[MaxPartySize];
        public int WaveIndex { get; private set; }
        readonly string[][] teamPresets={new string[0],new string[0],new string[0]};
        public string[] TeamPreset(int index) { return index<0 || index>=3?new string[0]:(string[])teamPresets[index].Clone(); }
        public bool SaveTeamPreset(int index) {
            if(!campaign || index<0 || index>=3 || (!CanChangeParty && !CampaignComplete)) return false;
            var ids=new string[party.Count]; for(int i=0;i<party.Count;i++) ids[i]=party[i].Identity.Id; teamPresets[index]=ids; return true;
        }
        public bool ClearTeamPreset(int index) {
            if(!campaign || index<0 || index>=3 || teamPresets[index].Length==0 || (!CanChangeParty && !CampaignComplete)) return false;
            teamPresets[index]=new string[0]; return true;
        }
        public bool LoadTeamPreset(int index) {
            if(!campaign || !CanChangeParty || index<0 || index>=3 || teamPresets[index].Length==0) return false;
            var lineup=new List<PartyHero>();
            foreach(var id in teamPresets[index]) { if(lineup.Count>=PartyLimit) break; var member=HeroProgress(id); if(member==null) return false; lineup.Add(member); }
            foreach(var member in party) if(lineup.Count<PartyLimit && !lineup.Contains(member)) lineup.Add(member);
            foreach(var recruit in recruits) { var member=HeroProgress(recruit.Id); if(lineup.Count<PartyLimit && !lineup.Contains(member)) lineup.Add(member); }
            foreach(var member in party) savedHeroes[member.Identity.Id]=member;
            party.Clear(); party.AddRange(lineup); ActiveIndex=0; RefreshSharedLevels(); RefreshFactionStats(); return true;
        }
        public bool InTower { get; private set; }
        public int TowerFloor { get; private set; } = 1;
        public const int TowerFloors=100;
        // Keep existing Tower floors stable as story chapters are appended.
        public const int TowerCycleStages=30;
        string[] towerCampaignParty=new string[0];
        public bool TowerComplete { get { return TowerFloor>TowerFloors; } }
        public int EnemyEncounter { get { return InTower?(Math.Min(TowerFloor,TowerFloors)-1)%Math.Min(TowerCycleStages,ChapterDefinition.TotalStages):Math.Min(encounter,ChapterDefinition.TotalStages-1); } }
        public double TowerHealthScale { get { return InTower?1+(TowerFloor-1)*.08:1; } }
        public int TowerShards { get { return TowerFloor%5==0?10:5; } }
        public bool CanEnterTower { get { return campaign && !InTower && !TowerComplete && recruits.Count>=5 && (CanChangeParty || CampaignComplete); } }
        public bool EnterTower() {
            if(!CanEnterTower) return false;
            towerCampaignParty=new string[party.Count]; for(int i=0;i<party.Count;i++) towerCampaignParty[i]=party[i].Identity.Id;
            InTower=true; Reset(); FillAvailableSlots(); Reset(); return true;
        }
        public bool LeaveTower() {
            if(!InTower || (Current!=Phase.Lost && Current!=Phase.Complete && !CanChangeParty)) return false;
            var restoredParty=new List<PartyHero>(); foreach(var id in towerCampaignParty) restoredParty.Add(HeroProgress(id));
            InTower=false; party.Clear(); party.AddRange(restoredParty); towerCampaignParty=new string[0]; Reset(); return true;
        }
        public int WaveCount { get { return campaign && (waveBattles || InTower) ? 3 : 1; } }
        long dailyDay;
        readonly int[] dailyProgress=new int[3];
        readonly bool[] dailyClaimed=new bool[4];
        void RefreshDaily() {
            long today=(utcNow==null?DateTime.UtcNow:utcNow()).Date.Ticks;
            if(today>dailyDay) { dailyDay=today; Array.Clear(dailyProgress,0,3); Array.Clear(dailyClaimed,0,4); }
        }
        void RecordDaily(int quest) { RefreshDaily(); dailyProgress[quest]=Math.Min(quest==0?3:1,dailyProgress[quest]+1); }
        public int DailyProgress(int quest) { RefreshDaily(); return dailyProgress[quest]; }
        public bool DailyClaimed(int quest) { RefreshDaily(); return dailyClaimed[quest]; }
        public bool CanClaimDaily(int quest) {
            RefreshDaily(); if(!campaign || (!CanChangeParty && !CampaignComplete) || quest<0 || quest>3 || dailyClaimed[quest]) return false;
            return quest==3?dailyClaimed[0] && dailyClaimed[1] && dailyClaimed[2]:dailyProgress[quest]>=(quest==0?3:1);
        }
        public bool ClaimDaily(int quest) {
            if(!CanClaimDaily(quest)) return false;
            Crystals=Math.Min(1000000,Crystals+new[]{40,20,30,100}[quest]);
            if(quest==3) foreach(var recruit in recruits) { var hero=HeroProgress(recruit.Id); hero.Shards=Math.Min(1000000,hero.Shards+10); }
            else { var hero=HeroProgress("Knight_Common"); hero.Shards=Math.Min(1000000,hero.Shards+(quest==1?5:10)); }
            dailyClaimed[quest]=true; return true;
        }
        readonly List<string> sharedHeroes=new List<string>();
        public const int SharedSlots=5;
        public bool CrystalUnlocked { get { return recruits.Count>=5; } }
        public int SharedCount { get { return sharedHeroes.Count; } }
        public bool IsShared(string id) { return sharedHeroes.Contains(id); }
        public int CrystalLevel { get { if(!CrystalUnlocked) return 1; var levels=new List<int>(); foreach(var recruit in recruits) levels.Add(HeroProgress(recruit.Id).Level); levels.Sort((a,b)=>b.CompareTo(a)); return levels[4]; } }
        void RefreshSharedLevels() {
            int level=CrystalLevel;
            foreach(var recruit in recruits) { var member=HeroProgress(recruit.Id); member.SharedLevel=IsShared(recruit.Id)?level:0; member.RefreshStats(); }
        }
        public bool SetShared(string id,bool enabled) {
            if(!campaign || (!CanChangeParty && !CampaignComplete)) return false;
            var member=HeroProgress(id); if(member==null || IsShared(id)==enabled) return false;
            if(enabled) { if(!CrystalUnlocked || SharedCount>=SharedSlots || member.Level>=CrystalLevel) return false; sharedHeroes.Add(id); }
            else sharedHeroes.Remove(id);
            RefreshSharedLevels(); foreach(var recruit in recruits) HeroProgress(recruit.Id).Restore(); return true;
        }
        readonly Func<DateTime> utcNow;
        long idleClaimTicks;
        int furthestStage, idleCrystalRemainder;
        public int FurthestStage { get { return Math.Max(furthestStage,encounter); } }
        public const int IdleCapMinutes=720, IdleCrystalInterval=10;
        // Crystal credits per ten minutes; fractional credits survive every claim/reload.
        public int IdleCrystalRate { get { return 1+FurthestStage/5; } }
        public int IdleExperienceRate { get { return 2+FurthestStage/5; } }
        public int IdleMinutes { get { return (int)Math.Min(IdleCapMinutes,Math.Max(0,(utcNow().Ticks-idleClaimTicks)/TimeSpan.TicksPerMinute)); } }
        public int IdleCrystals { get { return Math.Min(1000000-Crystals,(IdleMinutes*IdleCrystalRate+idleCrystalRemainder)/IdleCrystalInterval); } }
        public int IdleExperience { get { return IdleMinutes*IdleExperienceRate; } }
        public bool ClaimIdleRewards() {
            if(!campaign || (!CanChangeParty && !CampaignComplete)) return false;
            long now=utcNow().Ticks;
            long elapsed=now-idleClaimTicks;
            int minutes=(int)Math.Min(IdleCapMinutes,Math.Max(0,elapsed/TimeSpan.TicksPerMinute));
            if(minutes==0) return false;
            ForgeMaterials=Math.Min(1000000,ForgeMaterials+minutes);
            int crystalCredits=minutes*IdleCrystalRate+idleCrystalRemainder;
            Crystals=Math.Min(1000000,Crystals+crystalCredits/IdleCrystalInterval);
            idleCrystalRemainder=crystalCredits%IdleCrystalInterval;
            foreach(var recruit in recruits) { var member=HeroProgress(recruit.Id); member.GainExperience(minutes*IdleExperienceRate); member.Restore(); }
            RefreshSharedLevels(); foreach(var recruit in recruits) HeroProgress(recruit.Id).Restore();
            RecordDaily(1);
            idleClaimTicks=now-elapsed%TimeSpan.TicksPerMinute;
            return true;
        }
        public int Crystals { get; private set; }
        public int RareMisses { get; private set; }
        public int LegendaryMisses { get; private set; }
        public bool CampaignComplete { get { return campaign && !InTower && encounter >= ChapterDefinition.TotalStages; } }
        public int ChapterIndex { get { return Math.Min(EnemyEncounter / ChapterDefinition.StagesPerChapter, ChapterDefinition.Catalog.Count - 1); } }
        public int StageIndex { get { return EnemyEncounter % ChapterDefinition.StagesPerChapter; } }
        public int Encounter { get { return encounter; } }
        readonly Dictionary<string, PartyHero> savedHeroes = new Dictionary<string, PartyHero>();
        readonly List<HeroDefinition> recruits = new List<HeroDefinition>();
        public IReadOnlyList<HeroDefinition> Recruits { get { return recruits.AsReadOnly(); } }
        readonly List<HeroClass> unlocked = new List<HeroClass>();
        public const int MaxPartySize = 5;
        public const int FrontSlots = 2;
        public bool IsFrontRow(int index) { return index>=0 && index<Math.Min(FrontSlots,party.Count); }
        public bool FillAvailableSlots() {
            if(!CanChangeParty) return false;
            bool added=false;
            foreach(var recruit in recruits) {
                if(party.Count>=PartyLimit) break;
                bool active=false; foreach(var member in party) if(member.Identity.Id==recruit.Id) active=true;
                if(!active) { var member=HeroProgress(recruit.Id); member.Restore(); party.Add(member); added=true; }
            }
            if(added) RefreshFactionStats();
            return added;
        }
        public bool SwapFormation(int first,int second) {
            if(!CanChangeParty || first<0 || second<0 || first>=party.Count || second>=party.Count || first==second) return false;
            var hero=party[first]; party[first]=party[second]; party[second]=hero;
            if(ActiveIndex==first) ActiveIndex=second; else if(ActiveIndex==second) ActiveIndex=first;
            return true;
        }
        static double FrontlineScore(PartyHero hero) {
            double dodge,parry; AutoBattlePlanner.DefenseChances(hero.Identity.Class,out dodge,out parry);
            double score=hero.Definition.MaxHealth/(1-dodge-parry)+hero.Protection*12;
            if(hero.Identity.Class==HeroClass.Knight || hero.Identity.Class==HeroClass.Paladin) score+=100;
            if(hero.Identity.Class==HeroClass.Cleric) score-=60;
            return score;
        }
        public string[] RecommendedFormation() {
            var ranked=new List<int>(); for(int i=0;i<party.Count;i++) ranked.Add(i);
            ranked.Sort((a,b)=> { int order=FrontlineScore(party[b]).CompareTo(FrontlineScore(party[a])); return order!=0?order:a.CompareTo(b); });
            var orderIds=new List<string>();
            for(int i=0;i<Math.Min(FrontSlots,ranked.Count);i++) orderIds.Add(party[ranked[i]].Identity.Id);
            // Preserve the original relative order of heroes left in back.
            foreach(var hero in party) if(!orderIds.Contains(hero.Identity.Id)) orderIds.Add(hero.Identity.Id);
            return orderIds.ToArray();
        }
        public bool AutoArrangeFormation() {
            if(!CanChangeParty || party.Count<2) return false;
            var ids=RecommendedFormation(); bool changed=false;
            for(int i=0;i<ids.Length;i++) if(party[i].Identity.Id!=ids[i]) changed=true;
            if(!changed) return false;
            var active=party[ActiveIndex]; var arranged=new List<PartyHero>();
            foreach(var id in ids) foreach(var hero in party) if(hero.Identity.Id==id) { arranged.Add(hero); break; }
            party.Clear(); party.AddRange(arranged); ActiveIndex=party.IndexOf(active); return true;
        }
        public int TeamPower { get { int total=0; foreach(var member in party) total+=member.Power; return total; } }
        public double TowerPowerScale { get { return TowerHealthScale*(InTower?Math.Max(1,PartyLimit/3.0)/Math.Max(1,Math.Min(5,EnemyEncounter/5+1)/3.0):1); } }
        public int StagePower { get { return (int)Math.Ceiling(ChapterDefinition.StagePower(EnemyEncounter,WaveCount,!InTower)*TowerPowerScale); } }
        public string Difficulty { get { return ChapterDefinition.Difficulty(TeamPower,StagePower); } }
        public int PartyLimit { get { return InTower?MaxPartySize:campaign?Math.Min(MaxPartySize,ChapterIndex+1):MaxPartySize; } }
        public IReadOnlyList<HeroClass> UnlockedClasses { get { return unlocked.AsReadOnly(); } }
        int targetCursor, encounter;
        readonly List<BattleEnemy> enemies = new List<BattleEnemy>();
        public IReadOnlyList<BattleEnemy> Enemies { get { return enemies.AsReadOnly(); } }
        public int SelectedEnemyIndex { get; private set; }
        public int AttackingEnemyIndex { get; private set; }
        public EnemyDefinition Enemy { get { return enemies[SelectedEnemyIndex].Definition; } }
        bool AllEnemiesDefeated { get { foreach (var enemy in enemies) if (enemy.Health > 0) return false; return true; } }
        public bool SelectEnemy(int index) {
            if (Current != Phase.Player || index < 0 || index >= enemies.Count || enemies[index].Health <= 0) return false;
            SelectedEnemyIndex = index; return true;
        }
        public int LastDamage { get; private set; }
        public UltimateDefinition LastUltimate { get; private set; }
        public int UltimateTarget { get; private set; }
        public const int MaxEnemies=3;
        public readonly int[] LastUltimateDamage = new int[MaxEnemies];
        public readonly int[] LastUltimateHealing = new int[MaxPartySize], LastUltimateGuard = new int[MaxPartySize];
        public float LastElementMultiplier { get; private set; }
        public Element LastElement { get; private set; }
        public IReadOnlyList<PartyHero> Party { get { return party.AsReadOnly(); } }
        public int ActiveIndex { get; private set; }
        PartyHero Active { get { return party[ActiveIndex]; } }
        public ClassDefinition Hero { get { return Active.Definition; } }
        public int AbilityCharges { get { return Active.AbilityCharges; } }
        public int HeroHealth { get { return Active.Health; } }
        public int EnemyHealth { get { return enemies[SelectedEnemyIndex].Health; } }
        public Phase Current { get; private set; }
        public bool Defended { get; private set; }
        readonly bool[] defendedHeroes = new bool[MaxPartySize];
        public EnemySkill IncomingSkill { get; private set; }
        public int LastMechanicTarget { get; private set; } = -1;
        public int LastEnemyHealing { get; private set; }
        public int LastShieldAbsorbed { get; private set; }
        public readonly int[] LastIncomingDamage = new int[MaxPartySize];
        public readonly int[] LastStatusDamage = new int[MaxPartySize];
        public bool HeroDefended(int index) { return defendedHeroes[index]; }
        public Battle() : this(HeroClass.Knight) { }
        public Battle(HeroClass kind) { unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset(); }
        public Battle(bool recruitmentCampaign, bool enableWaves=false, bool enableClassPassives=false, Random criticalRandom=null, Func<DateTime> utcNow=null, bool enableFactions=false) { FactionsEnabled=enableFactions; this.utcNow=utcNow??(()=>DateTime.UtcNow); idleClaimTicks=this.utcNow().Ticks; campaign = recruitmentCampaign; waveBattles=enableWaves; ClassPassivesEnabled=enableClassPassives; passiveRandom=criticalRandom??new Random(); Crystals = Summoning.StarterCrystals; unlocked.Add(HeroClass.Knight); recruits.Add(HeroDefinition.Common(HeroClass.Knight)); party.Add(new PartyHero(HeroClass.Knight)); Reset(); }
        public void SelectClass(HeroClass kind) {
            if (campaign) throw new InvalidOperationException("Campaign heroes are recruited, not replaced.");
            party.Clear(); recruits.Clear(); unlocked.Clear(); unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset();
        }
        public bool CanChangeParty {
            get {
                if (Current != Phase.Player || WaveIndex>0) return false;
                foreach (var enemy in enemies) if (enemy.Health != enemy.Definition.MaxHealth) return false;
                foreach (var member in party)
                    if (member.Acted || member.Health != member.Definition.MaxHealth || member.SkillCharges(0) != 2 || member.SkillCharges(1) != 2) return false;
                return true;
            }
        }
        public bool EquipSkill(int slot,int skillIndex) {
            if(!CanChangeParty) return false;
            return EquipHeroSkill(Active.Identity.Id,slot,skillIndex);
        }
        public bool EquipHeroSkill(string id,int slot,int skillIndex) {
            if(id==null || (!campaign && id!=Active.Identity.Id) || (!CanChangeParty && !CampaignComplete) || slot<0 || slot>1) return false;
            var member=campaign?HeroProgress(id):Active;
            if(member==null || skillIndex<0 || skillIndex>=member.Definition.Skills.Count || member.SkillIndex(1-slot)==skillIndex) return false;
            member.Equip(slot,skillIndex); return true;
        }
        public bool ReplaceActiveHero(HeroClass kind) {
            if (!CanChangeParty || !unlocked.Contains(kind)) return false;
            foreach (var member in party) if (member.Definition.Kind == kind) return false;
            return EquipHero(HeroDefinition.Common(kind).Id);
        }
        public List<PartyHero> Collection(HeroClass? kind=null,HeroFaction? faction=null,HeroQuality? quality=null,CollectionSort sort=CollectionSort.Power) {
            var list=new List<PartyHero>();
            foreach(var recruit in recruits) { var hero=HeroProgress(recruit.Id); if((!kind.HasValue || recruit.Class==kind.Value) && (!faction.HasValue || recruit.Faction==faction.Value) && (!quality.HasValue || hero.Quality==quality.Value)) list.Add(hero); }
            list.Sort((a,b)=> { int order=sort==CollectionSort.Level?b.CombatLevel.CompareTo(a.CombatLevel):sort==CollectionSort.Ascension?(b.UpgradeRank<5 && b.Shards>=b.UpgradeCost).CompareTo(a.UpgradeRank<5 && a.Shards>=a.UpgradeCost):b.Power.CompareTo(a.Power); if(order==0) order=b.Power.CompareTo(a.Power); if(order==0) order=string.CompareOrdinal(a.Identity.Name,b.Identity.Name); return order; }); return list;
        }
        static PartyHero PreviewCopy(PartyHero original,int factionBonus) {
            var clone=new PartyHero(original.Identity); clone.Level=original.Level; clone.Experience=original.Experience; clone.Shards=original.Shards; clone.UpgradeRank=original.UpgradeRank; clone.SharedLevel=original.SharedLevel; clone.FactionBonus=factionBonus;
            for(int tier=0;tier<3;tier++) clone.SetTalent(tier,original.TalentChoice(tier));
            for(int slot=0;slot<3;slot++) { clone.SetGear((GearSlot)slot,original.Gear((GearSlot)slot)); clone.SetGearEnhancement((GearSlot)slot,original.GearEnhancement((GearSlot)slot)); }
            for(int slot=0;slot<2;slot++) clone.Equip(slot,original.SkillIndex(slot)); clone.RefreshStats(); clone.Restore(); return clone;
        }
        public HeroReplacement PreviewReplacement(string id,int slot) {
            var candidate=HeroProgress(id); if(candidate==null || slot<0 || slot>=party.Count || party.Contains(candidate)) return null;
            var counts=new int[3]; for(int i=0;i<party.Count;i++) counts[(int)(i==slot?candidate:party[i]).Identity.Faction]++;
            int strongest=Math.Max(counts[0],Math.Max(counts[1],counts[2])); int bonus=FactionsEnabled?FactionRules.Bonus(strongest):0;
            int total=0; PartyHero replacement=null;
            for(int i=0;i<party.Count;i++) { var cloned=PreviewCopy(i==slot?candidate:party[i],bonus); total+=cloned.Power; if(i==slot) replacement=cloned; }
            return new HeroReplacement(replacement,total,party[slot],TeamPower);
        }
        public bool EquipHeroAt(string id,int slot) {
            if(!CanChangeParty || slot<0 || slot>=party.Count || HeroProgress(id)==null) return false;
            foreach(var member in party) if(member.Identity.Id==id) return false;
            int previous=ActiveIndex; ActiveIndex=slot; if(EquipHero(id)) return true; ActiveIndex=previous; return false;
        }
        public bool EquipHero(string id) {
            if (!CanChangeParty) return false;
            foreach (var member in party) if (member.Identity.Id == id) return false;
            foreach (var recruit in recruits) if (recruit.Id == id) {
                var previous = party[ActiveIndex];
                savedHeroes[previous.Identity.Id] = previous;
                PartyHero replacement;
                if (!savedHeroes.TryGetValue(id, out replacement)) replacement = new PartyHero(recruit);
                replacement.Restore();
                party[ActiveIndex] = replacement; RefreshFactionStats(); return true;
            }
            return false;
        }
        public bool SelectHero(int index) {
            if (Current != Phase.Player || index < 0 || index >= party.Count || party[index].Health == 0 || party[index].Acted) return false;
            ActiveIndex = index; return true;
        }
        // Prototype story checkpoints: victory unlocks an ally for the next encounter.
        readonly Random lootRandom=new Random();
        readonly Dictionary<string,int> inventory=new Dictionary<string,int>();
        readonly Dictionary<string,int> enhancements=new Dictionary<string,int>();
        public int ForgeMaterials { get; private set; }
        public int EnhancementLevel(string id) { int level; return id!=null && enhancements.TryGetValue(id,out level)?level:0; }
        public GearDefinition EnhancedGear(string id) { var item=GearDefinition.Find(id); return item==null?null:item.Enhanced(EnhancementLevel(id)); }
        public int IdleForgeMaterials { get { return Math.Min(1000000-ForgeMaterials,IdleMinutes); } }
        public bool CanEnhanceGear(string id) {
            var item=EnhancedGear(id); return campaign && (CanChangeParty || CampaignComplete) && item!=null && GearCount(id)>0 && item.Enhancement<GearDefinition.MaxEnhancement && ForgeMaterials>=item.EnhanceCost;
        }
        public bool EnhanceGear(string id) {
            if(!CanEnhanceGear(id)) return false;
            var item=EnhancedGear(id); ForgeMaterials-=item.EnhanceCost; enhancements[id]=item.Enhancement+1;
            foreach(var recruit in recruits) { var hero=HeroProgress(recruit.Id); if(hero.Gear(item.Slot)!=null && hero.Gear(item.Slot).Id==id) { hero.SetGearEnhancement(item.Slot,item.Enhancement+1); hero.Restore(); } }
            return true;
        }
        public PartyHero PreviewEnhancement(string heroId,GearSlot slot,string itemId=null) {
            var hero=heroId==null?null:HeroProgress(heroId); if(hero==null || (int)slot<0 || (int)slot>2) return null;
            var gear=itemId==null?hero.Gear(slot):GearDefinition.Find(itemId); if(gear==null || gear.Slot!=slot || GearCount(gear.Id)==0 || EnhancementLevel(gear.Id)>=GearDefinition.MaxEnhancement) return null;
            var clone=PreviewGear(heroId,slot,gear.Id); clone.SetGearEnhancement(slot,EnhancementLevel(gear.Id)+1); return clone;
        }
        public GearDefinition LastLoot { get; private set; }
        public int GearCount(string id) { int count; return inventory.TryGetValue(id,out count)?count:0; }
        public BattleResults Results { get; private set; }
        public int CampaignVictoryCrystals { get { return Summoning.VictoryCrystals+(ChapterIndex>=6?ChapterIndex*5+(StageIndex==4?25:0):0); } }
        void BuildResults(GearDefinition loot) {
            int xp=InTower?60+TowerFloor*2:campaign?40+ChapterIndex*10+(enemies[0].Definition.IsBoss?20:0):0;
            string recruit=null;
            var story=campaign && !InTower?StoryRecruit.At(encounter):null;
            if(story!=null) recruit=story.Hero.Name;
            int materials=!campaign?0:InTower?10+TowerFloor/5:10+ChapterIndex*2+(StageIndex==4?10:0);
            bool duplicate=story!=null && HeroProgress(story.HeroId)!=null;
            Results=new BattleResults(xp,InTower?80+TowerFloor*5:campaign?CampaignVictoryCrystals:0,loot,recruit,Party,materials,story==null?null:story.Introduction,duplicate,duplicate?20:0);
        }
        void Victory() {
            if(WaveIndex<WaveCount-1) { Current=Phase.WaveCleared; return; }
            Current=Phase.Won;
            if(Results==null) BuildResults(campaign?GearDefinition.Drop(enemies[0].Definition.IsBoss,lootRandom.NextDouble(),lootRandom.Next(18)):null);
        }
        public bool ContinueAfterVictory() {
            if(Current!=Phase.Won || Results==null) return false;
            return ClaimVictory(Results.Loot);
        }
        // Explicit rolls support deterministic simulations; the UI claims the prepared result.
        public bool ContinueAfterVictory(double lootRoll,int variant) {
            if (Current != Phase.Won || double.IsNaN(lootRoll) || lootRoll<0 || lootRoll>=1 || variant<0 || variant>=18) return false;
            return ClaimVictory(campaign?GearDefinition.Drop(enemies[0].Definition.IsBoss,lootRoll,variant):null);
        }
        bool ClaimVictory(GearDefinition loot) {
            if(campaign) ForgeMaterials=Math.Min(1000000,ForgeMaterials+Results.Materials);
            if(InTower) {
                RecordDaily(0); LastLoot=loot; inventory[loot.Id]=Math.Min(1000000,GearCount(loot.Id)+1);
                foreach(var member in party) { member.GainExperience(Results.Experience); member.Shards=Math.Min(1000000,member.Shards+TowerShards); }
                Crystals=Math.Min(1000000,Crystals+Results.Crystals); TowerFloor++; Reset(); return true;
            }
            if (campaign) {
                RecordDaily(0);
                LastLoot=loot;
                inventory[LastLoot.Id]=Math.Min(1000000,GearCount(LastLoot.Id)+1);
                foreach(var member in party) member.GainExperience(Results.Experience);
                var story=StoryRecruit.At(encounter);
                if(story!=null) {
                    var known=HeroProgress(story.HeroId);
                    if(known==null) AddRecruit(story.Hero,false);
                    else known.Shards=Math.Min(1000000,known.Shards+20);
                }
                Crystals=Math.Min(1000000,Crystals+Results.Crystals);
                encounter++; furthestStage=Math.Max(furthestStage,encounter);
                foreach(var hero in recruits) {
                    if(party.Count>=PartyLimit) break;
                    bool active=false; foreach(var member in party) if(member.Identity.Id==hero.Id) active=true;
                    if(!active) party.Add(HeroProgress(hero.Id));
                }
            }
            Reset(); return true;
        }
        void AddRecruit(HeroDefinition hero, bool addToParty) {
            recruits.Add(hero);
            if(!unlocked.Contains(hero.Class)) unlocked.Add(hero.Class);
            var member=new PartyHero(hero); savedHeroes[hero.Id]=member;
            if(addToParty && party.Count<PartyLimit) party.Add(member);
        }
        public bool CanSummon { get { return campaign && (CanChangeParty || CampaignComplete) && Crystals>=Summoning.Cost; } }
        public bool CanSummonTen { get { return CanSummon && Crystals>=Summoning.Cost*10; } }
        readonly List<string> wishlist=new List<string>();
        public string[] Wishlist { get { return wishlist.ToArray(); } }
        public bool IsWishlisted(string id) { return wishlist.Contains(id); }
        public int WishlistCount(HeroFaction faction) { int count=0; foreach(var id in wishlist) if(HeroDefinition.Find(id).Faction==faction) count++; return count; }
        public bool ToggleWishlist(string id) {
            if(!campaign || (!CanChangeParty && !CampaignComplete)) return false;
            var hero=HeroDefinition.Find(id); if(hero==null || hero.Quality<HeroQuality.Rare) return false;
            if(wishlist.Contains(id)) { wishlist.Remove(id); return true; }
            if(WishlistCount(hero.Faction)>=Summoning.WishlistPerFaction) return false;
            wishlist.Add(id); return true;
        }
        public SummonResult[] SummonTen(double[] rolls,int[] classIndexes,double[] heroRolls=null) {
            if(!CanSummonTen || rolls==null || classIndexes==null || rolls.Length!=10 || classIndexes.Length!=10) return null;
            if(heroRolls!=null && heroRolls.Length!=10) return null;
            for(int i=0;i<10;i++) if(double.IsNaN(rolls[i]) || double.IsInfinity(rolls[i]) || rolls[i]<0 || rolls[i]>=1 || classIndexes[i]<0 || classIndexes[i]>=6 || (heroRolls!=null && (double.IsNaN(heroRolls[i]) || heroRolls[i]<0 || heroRolls[i]>=1))) return null;
            var results=new SummonResult[10];
            for(int i=0;i<10;i++) results[i]=Summon(rolls[i],heroRolls==null?(classIndexes[i]+.5)/6.0:heroRolls[i]);
            return results;
        }
        public SummonResult[] SummonTen(double[] rolls,double[] heroRolls) {
            return heroRolls==null?null:SummonTen(rolls,new int[10],heroRolls);
        }
        public SummonResult Summon(double rarityRoll, int classIndex) {
            return classIndex<0 || classIndex>=6?null:Summon(rarityRoll,(classIndex+.5)/6.0);
        }
        public SummonResult Summon(double rarityRoll, double heroRoll) {
            if(!CanSummon || double.IsNaN(heroRoll) || heroRoll<0 || heroRoll>=1 || double.IsNaN(rarityRoll) || double.IsInfinity(rarityRoll) || rarityRoll<0 || rarityRoll>=1) return null;
            var quality=Summoning.Quality(rarityRoll,RareMisses,LegendaryMisses);
            var candidate=Summoning.Pick(quality,heroRoll,wishlist);
            bool duplicate=false;
            foreach(var known in recruits) if(known.Id==candidate.Id) duplicate=true;
            int refund=duplicate?Summoning.DuplicateRefund(quality):0;
            Crystals=Crystals-Summoning.Cost+refund;
            RareMisses=quality>=HeroQuality.Rare?0:RareMisses+1;
            LegendaryMisses=quality==HeroQuality.Legendary?0:LegendaryMisses+1;
            int shards=0;
            if(!duplicate) AddRecruit(candidate,false);
            else { var member=HeroProgress(candidate.Id); int before=member.Shards; member.Shards=Math.Min(1000000,member.Shards+10+(int)quality*5); shards=member.Shards-before; }
            RecordDaily(2);
            return new SummonResult(candidate,duplicate,refund,shards);
        }
        public PartyHero HeroProgress(string id) {
            foreach(var member in party) if(member.Identity.Id==id) return member;
            PartyHero reserve;
            return savedHeroes.TryGetValue(id,out reserve)?reserve:null;
        }
        public bool UpgradeHero(string id) {
            if(!campaign || (!CanChangeParty && !CampaignComplete)) return false;
            var member=HeroProgress(id);
            if(member==null || member.UpgradeRank>=PartyHero.MaxUpgradeRank || member.Shards<member.UpgradeCost) return false;
            member.Shards-=member.UpgradeCost; member.UpgradeRank++; member.RefreshStats(); member.Restore(); return true;
        }
        public bool ChooseTalent(string id,int tier,int choice) {
            if(!campaign || (!CanChangeParty && !CampaignComplete) || tier<0 || tier>2 || choice<0 || choice>1) return false;
            var hero=HeroProgress(id);
            if(hero==null || hero.Level<ClassTalents.UnlockLevel(tier) || hero.TalentChoice(tier)==choice) return false;
            hero.SetTalent(tier,choice); hero.RefreshStats(); hero.Restore(); return true;
        }
        public bool ResetTalents(string id) {
            if(!campaign || (!CanChangeParty && !CampaignComplete)) return false;
            var hero=HeroProgress(id); if(hero==null) return false; bool changed=false;
            for(int i=0;i<3;i++) if(hero.TalentChoice(i)>=0) { hero.SetTalent(i,-1); changed=true; }
            if(!changed) return false;
            hero.RefreshStats(); hero.Restore(); return true;
        }
        public int AvailableGear(string itemId) {
            var item=GearDefinition.Find(itemId); if(item==null) return 0;
            int used=0; foreach(var recruit in recruits) { var owner=HeroProgress(recruit.Id); if(owner!=null && owner.Gear(item.Slot)==item) used++; }
            return Math.Max(0,GearCount(itemId)-used);
        }
        public PartyHero PreviewAscension(string id) {
            var member=id==null?null:HeroProgress(id); if(member==null || member.UpgradeRank>=PartyHero.MaxUpgradeRank) return null;
            var preview=PreviewCopy(member,member.FactionBonus); preview.UpgradeRank++; preview.RefreshStats(); return preview;
        }
        public PartyHero PreviewNextLevel(string id) {
            var member=id==null?null:HeroProgress(id); if(member==null || member.Level>=PartyHero.MaxLevel) return null;
            var preview=PreviewCopy(member,member.FactionBonus); preview.Level++; preview.Experience=0; preview.RefreshStats(); return preview;
        }
        int claimedMilestones;
        public bool ChapterCleared(int chapter) { return chapter>=0 && chapter<ChapterDefinition.Catalog.Count && encounter>=(chapter+1)*ChapterDefinition.StagesPerChapter; }
        public bool MilestoneClaimed(int chapter) { return chapter>=0 && chapter<ChapterDefinition.Catalog.Count && (claimedMilestones&(1<<chapter))!=0; }
        public bool CanClaimMilestone(int chapter) { return campaign && !InTower && (CanChangeParty || CampaignComplete) && ChapterCleared(chapter) && !MilestoneClaimed(chapter); }
        public int ReadyMilestones { get { int count=0; for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) if(CanClaimMilestone(chapter)) count++; return count; } }
        public bool ClaimMilestone(int chapter) {
            if(!CanClaimMilestone(chapter)) return false;
            var reward=ChapterMilestone.For(chapter); claimedMilestones|=1<<chapter;
            Crystals=Math.Min(1000000,Crystals+reward.Crystals);
            foreach(var recruit in recruits) { var member=HeroProgress(recruit.Id); member.GainExperience(reward.Experience); member.Shards=Math.Min(1000000,member.Shards+reward.Shards); }
            RefreshSharedLevels(); foreach(var recruit in recruits) HeroProgress(recruit.Id).Restore(); return true;
        }
        public PartyHero PreviewGear(string heroId,GearSlot slot,string itemId) {
            if(heroId==null) return null;
            var member=HeroProgress(heroId);
            if(member==null || (int)slot<0 || (int)slot>2) return null;
            var item=itemId==null?null:GearDefinition.Find(itemId);
            if(itemId!=null && (item==null || item.Slot!=slot || GearCount(itemId)==0)) return null;
            var clone=PreviewCopy(member,member.FactionBonus); clone.SetGear(slot,item); clone.SetGearEnhancement(slot,item==null?0:EnhancementLevel(item.Id)); return clone;
        }
        double EquipmentScore(PartyHero member,GearSlot slot,GearDefinition item) {
            if(slot!=GearSlot.Accessory) {
                var clone=PreviewCopy(member,member.FactionBonus); clone.SetGear(slot,item); clone.SetGearEnhancement(slot,item==null?0:EnhancementLevel(item.Id)); return clone.Power;
            }
            if(item==null) return 0;
            double damage=member.Identity.Affinity==item.Element?member.Definition.AttackDamage:0;
            for(int skill=0;skill<2;skill++) if((member.Skill(skill).Affinity??member.Identity.Affinity)==item.Element) damage+=member.Skill(skill).Damage;
            if(member.Ultimate.Affinity==item.Element) damage+=member.Ultimate.Damage*member.EnergyGain/100.0*(member.Ultimate.AreaDamage?1.5:1);
            return damage*item.Enhanced(EnhancementLevel(item.Id)).ElementBoost;
        }
        public bool EquipBestGear(string heroId) {
            if(heroId==null || !campaign || (!CanChangeParty && !CampaignComplete)) return false;
            var member=HeroProgress(heroId); if(member==null) return false;
            bool changed=false;
            for(int index=0;index<3;index++) {
                var slot=(GearSlot)index; var current=member.Gear(slot); var best=current;
                double score=EquipmentScore(member,slot,current);
                foreach(var item in GearDefinition.Catalog) {
                    if(item.Slot!=slot || item==current || AvailableGear(item.Id)==0) continue;
                    double candidate=EquipmentScore(member,slot,item);
                    if(candidate>score) { best=item; score=candidate; }
                }
                if(best!=current && EquipGear(heroId,slot,best.Id)) changed=true;
            }
            return changed;
        }
        public bool EquipGear(string heroId,GearSlot slot,string itemId) {
            if(!campaign || (!CanChangeParty && !CampaignComplete) || (int)slot<0 || (int)slot>2) return false;
            var member=HeroProgress(heroId); var item=itemId==null?null:GearDefinition.Find(itemId);
            if(member==null || (itemId!=null && (item==null || item.Slot!=slot))) return false;
            if(member.Gear(slot)==item) return false;
            if(item!=null) {
                int used=0; foreach(var hero in recruits) { var owner=HeroProgress(hero.Id); if(owner!=null && owner.Gear(slot)==item) used++; }
                if(used>=GearCount(item.Id)) return false;
            }
            member.SetGear(slot,item); member.SetGearEnhancement(slot,item==null?0:EnhancementLevel(item.Id)); member.Restore(); return true;
        }
        public ProgressData ExportProgress() {
            RefreshDaily();
            var data = new ProgressData { wishlist=Wishlist, presets=new[]{new TeamPresetData{heroes=TeamPreset(0)},new TeamPresetData{heroes=TeamPreset(1)},new TeamPresetData{heroes=TeamPreset(2)}}, inTower=InTower, towerFloor=TowerFloor, towerCampaignParty=(string[])towerCampaignParty.Clone(), dailyDay=dailyDay, dailyProgress=(int[])dailyProgress.Clone(), dailyClaimed=(bool[])dailyClaimed.Clone(), sharedHeroes=sharedHeroes.ToArray(), idleClaimTicks=idleClaimTicks, idleCrystalRemainder=idleCrystalRemainder, claimedMilestones=claimedMilestones, forgeMaterials=ForgeMaterials, furthestStage=FurthestStage, crystals = Crystals, rareMisses = RareMisses, legendaryMisses = LegendaryMisses, encounter = encounter, pendingVictory = Current == Phase.Won, activeIndex = ActiveIndex,
                recruited = new string[recruits.Count], party = new string[party.Count], loadouts = new HeroLoadoutData[recruits.Count] };
            data.enhancements=new GearEnhancementData[enhancements.Count]; int improvement=0; foreach(var entry in enhancements) data.enhancements[improvement++]=new GearEnhancementData{id=entry.Key,level=entry.Value};
            data.inventory=new GearStackData[inventory.Count]; int stack=0; foreach(var item in inventory) data.inventory[stack++]=new GearStackData{id=item.Key,count=item.Value};
            for (int i = 0; i < party.Count; i++) data.party[i] = party[i].Identity.Id;
            for (int i = 0; i < recruits.Count; i++) {
                data.recruited[i] = recruits[i].Id;
                PartyHero member = null;
                foreach (var ally in party) if (ally.Identity.Id == recruits[i].Id) member = ally;
                if (member == null) savedHeroes.TryGetValue(recruits[i].Id, out member);
                data.loadouts[i] = new HeroLoadoutData { id = recruits[i].Id, firstSkill = member == null ? 0 : member.SkillIndex(0), secondSkill = member == null ? 1 : member.SkillIndex(1), level=member==null?1:member.Level, experience=member==null?0:member.Experience, upgradeRank=member==null?0:member.UpgradeRank, shards=member==null?0:member.Shards, talents=member==null?new[]{-1,-1,-1}:new[]{member.TalentChoice(0),member.TalentChoice(1),member.TalentChoice(2)}, gear=new[]{member==null || member.Gear(GearSlot.Weapon)==null?null:member.Gear(GearSlot.Weapon).Id,member==null || member.Gear(GearSlot.Armor)==null?null:member.Gear(GearSlot.Armor).Id,member==null || member.Gear(GearSlot.Accessory)==null?null:member.Gear(GearSlot.Accessory).Id} };
            }
            if(Current==Phase.Won && Results!=null) {
                data.results=new PendingResultsData { lootId=Results.Loot==null?null:Results.Loot.Id,damage=new int[party.Count],healing=new int[party.Count] };
                for(int i=0;i<party.Count;i++) { data.results.damage[i]=party[i].EncounterDamage; data.results.healing[i]=party[i].EncounterHealing; }
            }
            return data;
        }
        // Validate the whole snapshot before mutating live progress. Loading starts a fresh encounter.
        public bool RestoreProgress(ProgressData data) {
            if (!campaign || data == null || (data.version != 1 && data.version != 2 && data.version != 3 && data.version != 4 && data.version != 5 && data.version != 6 && data.version != 7 && data.version != 8 && data.version != 9 && data.version != 10 && data.version != 11 && data.version != 12 && data.version != 13 && data.version != 14 && data.version != 15 && data.version != 16) || data.encounter < 0 || data.encounter > ChapterDefinition.TotalStages || (data.pendingVictory && data.encounter == ChapterDefinition.TotalStages && !(data.version>=11 && data.inTower)) ||
                data.recruited == null || data.recruited.Length < 1 || data.recruited.Length > HeroDefinition.Catalog.Count ||
                data.party == null || data.party.Length < 1 || data.party.Length > MaxPartySize ||
                data.loadouts == null || data.loadouts.Length != data.recruited.Length ||
                data.activeIndex < 0 || data.activeIndex >= data.party.Length) return false;
            if(data.version>=14) {
                int earned=(1<<(data.encounter/ChapterDefinition.StagesPerChapter))-1;
                if(data.claimedMilestones<0 || (data.claimedMilestones&~earned)!=0) return false;
            }
            if(data.version>=13 && (data.idleCrystalRemainder<0 || data.idleCrystalRemainder>=IdleCrystalInterval)) return false;
            if(data.version>=7 && (data.idleClaimTicks<=0 || data.idleClaimTicks>DateTime.MaxValue.Ticks || data.furthestStage<0 || data.furthestStage>ChapterDefinition.TotalStages)) return false;
            if(data.version>=2 && (data.crystals<0 || data.crystals>1000000 || data.rareMisses<0 || data.rareMisses>=Summoning.RareGuarantee || data.legendaryMisses<0 || data.legendaryMisses>=Summoning.LegendaryGuarantee)) return false;
            if(data.version>=10) {
                if(data.dailyDay<0 || data.dailyDay>DateTime.MaxValue.Ticks || data.dailyDay%TimeSpan.TicksPerDay!=0 || data.dailyProgress==null || data.dailyProgress.Length!=3 || data.dailyClaimed==null || data.dailyClaimed.Length!=4) return false;
                for(int q=0;q<3;q++) if(data.dailyProgress[q]<0 || data.dailyProgress[q]>(q==0?3:1) || (data.dailyClaimed[q] && data.dailyProgress[q]<(q==0?3:1))) return false;
                if(data.dailyClaimed[3] && (!data.dailyClaimed[0] || !data.dailyClaimed[1] || !data.dailyClaimed[2])) return false;
            }
            var restoredWishlist=new List<string>();
            if(data.version>=16) {
                if(data.wishlist==null || data.wishlist.Length>6) return false;
                var factionSlots=new int[3];
                foreach(var id in data.wishlist) {
                    var hero=HeroDefinition.Find(id);
                    if(hero==null || hero.Quality<HeroQuality.Rare || restoredWishlist.Contains(id) || ++factionSlots[(int)hero.Faction]>Summoning.WishlistPerFaction) return false;
                    restoredWishlist.Add(id);
                }
            }
            var restoredInventory=new Dictionary<string,int>();
            if(data.version>=4) {
                if(data.inventory==null || data.inventory.Length>GearDefinition.Catalog.Count) return false;
                foreach(var item in data.inventory) {
                    if(item==null || GearDefinition.Find(item.id)==null || item.count<1 || item.count>1000000 || restoredInventory.ContainsKey(item.id)) return false;
                    restoredInventory.Add(item.id,item.count);
                }
            }
            var restoredEnhancements=new Dictionary<string,int>();
            if(data.version>=15) {
                if(data.forgeMaterials<0 || data.forgeMaterials>1000000 || data.enhancements==null || data.enhancements.Length>GearDefinition.Catalog.Count) return false;
                foreach(var entry in data.enhancements) {
                    if(entry==null || entry.id==null || !restoredInventory.ContainsKey(entry.id) || entry.level<1 || entry.level>GearDefinition.MaxEnhancement || restoredEnhancements.ContainsKey(entry.id)) return false;
                    restoredEnhancements.Add(entry.id,entry.level);
                }
            }
            var equippedCounts=new Dictionary<string,int>();
            var restored = new Dictionary<string, PartyHero>();
            for (int i = 0; i < data.recruited.Length; i++) {
                HeroDefinition identity=null;
                foreach(var known in HeroDefinition.Catalog) if(known.Id==data.recruited[i]) identity=known;
                if(identity==null || restored.ContainsKey(identity.Id) || data.loadouts[i]==null || data.loadouts[i].id!=identity.Id) return false;
                if(i==0 && identity.Id!=HeroDefinition.Common(HeroClass.Knight).Id) return false;
                var loadout = data.loadouts[i];
                if (loadout.firstSkill < 0 || loadout.firstSkill >= 6 || loadout.secondSkill < 0 || loadout.secondSkill >= 6 || loadout.firstSkill == loadout.secondSkill) return false;
                var member = new PartyHero(identity);
                if(data.version>=3) {
                    if(loadout.level<1 || loadout.level>PartyHero.MaxLevel || loadout.upgradeRank<0 || loadout.upgradeRank>PartyHero.MaxUpgradeRank || loadout.shards<0 || loadout.shards>1000000 || loadout.experience<0 || loadout.experience>=100+(loadout.level-1)*25 || (loadout.level==PartyHero.MaxLevel && loadout.experience!=0)) return false;
                    member.Level=loadout.level; member.Experience=loadout.experience; member.UpgradeRank=loadout.upgradeRank; member.Shards=loadout.shards; member.RefreshStats();
                }
                if(data.version>=6) {
                    if(loadout.talents==null || loadout.talents.Length!=3) return false;
                    for(int tier=0;tier<3;tier++) {
                        int choice=loadout.talents[tier];
                        if(choice< -1 || choice>1 || (choice>=0 && member.Level<ClassTalents.UnlockLevel(tier))) return false;
                        member.SetTalent(tier,choice);
                    }
                    member.RefreshStats();
                }
                if(data.version>=4 && loadout.gear!=null) {
                    if(loadout.gear.Length!=3) return false;
                    for(int slot=0;slot<3;slot++) if(!string.IsNullOrEmpty(loadout.gear[slot])) {
                        var item=GearDefinition.Find(loadout.gear[slot]); int owned,used;
                        if(item==null || (int)item.Slot!=slot || !restoredInventory.TryGetValue(item.Id,out owned)) return false;
                        equippedCounts.TryGetValue(item.Id,out used); if(used>=owned) return false;
                        equippedCounts[item.Id]=used+1; member.SetGear((GearSlot)slot,item);
                        int rank; restoredEnhancements.TryGetValue(item.Id,out rank); member.SetGearEnhancement((GearSlot)slot,rank);
                    }
                }
                member.Equip(0, loadout.firstSkill); member.Equip(1, loadout.secondSkill);
                restored.Add(member.Identity.Id, member);
            }
            var restoredPresets=new string[][]{new string[0],new string[0],new string[0]};
            if(data.version>=12) {
                if(data.presets==null || data.presets.Length!=3) return false;
                for(int preset=0;preset<3;preset++) {
                    var entry=data.presets[preset]; if(entry==null || entry.heroes==null || entry.heroes.Length>MaxPartySize) return false;
                    var names=new HashSet<string>(); foreach(var id in entry.heroes) if(id==null || !restored.ContainsKey(id) || !names.Add(id)) return false;
                    restoredPresets[preset]=(string[])entry.heroes.Clone();
                }
            }
            var restoredShared=new HashSet<string>();
            if(data.version>=9) {
                if(data.sharedHeroes==null || data.sharedHeroes.Length>SharedSlots || (data.sharedHeroes.Length>0 && restored.Count<5)) return false;
                foreach(var id in data.sharedHeroes) if(id==null || !restored.ContainsKey(id) || !restoredShared.Add(id)) return false;
            }
            bool restoringTower=data.version>=11 && data.inTower;
            var campaignParty=new HashSet<string>();
            if(data.version>=11) {
                if(data.towerFloor<1 || data.towerFloor>TowerFloors+1 || data.towerCampaignParty==null || (!data.inTower && data.towerCampaignParty.Length!=0)) return false;
                if(data.inTower) {
                    if((data.towerFloor>TowerFloors && data.pendingVictory) || restored.Count<5 || data.towerCampaignParty.Length<1 || data.towerCampaignParty.Length>Math.Min(5,data.encounter/5+1)) return false;
                    foreach(var id in data.towerCampaignParty) if(id==null || !restored.ContainsKey(id) || !campaignParty.Add(id)) return false;
                }
            }
            var chosen = new HashSet<string>();
            foreach (var id in data.party) if (id == null || !restored.ContainsKey(id) || !chosen.Add(id)) return false;
            int restoredLimit=restoringTower?MaxPartySize:Math.Min(MaxPartySize,Math.Min(data.encounter/ChapterDefinition.StagesPerChapter,ChapterDefinition.Catalog.Count-1)+1);
            if(data.version>=5 && data.party.Length>restoredLimit) return false;
            GearDefinition pendingLoot=null;
            if(data.version>=5) {
                if(data.pendingVictory) {
                    var result=data.results;
                    if(result==null || result.damage==null || result.healing==null || result.damage.Length!=data.party.Length || result.healing.Length!=data.party.Length) return false;
                    pendingLoot=GearDefinition.Find(result.lootId); if(pendingLoot==null || (ChapterDefinition.EnemiesAt(restoringTower?(data.towerFloor-1)%TowerCycleStages:data.encounter)[0].IsBoss && pendingLoot.Quality<HeroQuality.Rare)) return false;
                    for(int i=0;i<data.party.Length;i++) if(result.damage[i]<0 || result.damage[i]>1000000 || result.healing[i]<0 || result.healing[i]>1000000) return false;
                } else if(data.results!=null && (!string.IsNullOrEmpty(data.results.lootId) ||
                    (data.results.damage!=null && data.results.damage.Length>0) ||
                    (data.results.healing!=null && data.results.healing.Length>0))) return false;
            }
            for(int preset=0;preset<3;preset++) teamPresets[preset]=restoredPresets[preset];
            InTower=restoringTower; TowerFloor=data.version>=11?data.towerFloor:1; towerCampaignParty=restoringTower?(string[])data.towerCampaignParty.Clone():new string[0];
            dailyDay=data.version>=10?data.dailyDay:0; Array.Clear(dailyProgress,0,3); Array.Clear(dailyClaimed,0,4);
            if(data.version>=10) { Array.Copy(data.dailyProgress,dailyProgress,3); Array.Copy(data.dailyClaimed,dailyClaimed,4); } RefreshDaily();
            sharedHeroes.Clear(); foreach(var id in restoredShared) sharedHeroes.Add(id);
            wishlist.Clear(); wishlist.AddRange(restoredWishlist);
            enhancements.Clear(); foreach(var entry in restoredEnhancements) enhancements.Add(entry.Key,entry.Value); ForgeMaterials=data.version>=15?data.forgeMaterials:0;
            inventory.Clear(); foreach(var item in restoredInventory) inventory.Add(item.Key,item.Value); LastLoot=null;
            recruits.Clear(); unlocked.Clear(); party.Clear(); savedHeroes.Clear();
            for (int i = 0; i < data.recruited.Length; i++) {
                var identity = restored[data.recruited[i]].Identity; recruits.Add(identity);
                if (!unlocked.Contains(identity.Class)) unlocked.Add(identity.Class);
                savedHeroes.Add(identity.Id, restored[identity.Id]);
            }
            foreach (var id in data.party) { if(party.Count<restoredLimit) party.Add(restored[id]); }
            Crystals=data.version==1?Summoning.StarterCrystals:data.crystals;
            RareMisses=data.version==1?0:data.rareMisses; LegendaryMisses=data.version==1?0:data.legendaryMisses;
            idleClaimTicks=data.version>=7?data.idleClaimTicks:utcNow().Ticks;
            idleCrystalRemainder=data.version>=13?data.idleCrystalRemainder:0;
            claimedMilestones=data.version>=14?data.claimedMilestones:0;
            furthestStage=data.version>=7?Math.Max(data.furthestStage,data.encounter):data.encounter;
            encounter = data.encounter; Reset(); ActiveIndex = Math.Min(data.activeIndex,party.Count-1);
            if (data.pendingVictory) {
                WaveIndex=WaveCount-1; LoadWaveEnemies();
                foreach (var enemy in enemies) enemy.Health = 0;
                if(data.version>=5) {
                    for(int i=0;i<party.Count;i++) { party[i].EncounterDamage=data.results.damage[i]; party[i].EncounterHealing=data.results.healing[i]; }
                    Current=Phase.Won; BuildResults(pendingLoot);
                } else Victory();
            }
            return true;
        }
        public void Reset() {
            LastOffensivePassive=null; ClearPassiveReports();
            RefreshSharedLevels(); RefreshFactionStats();
            Results=null; LastEnemyHealing=0; LastMechanicTarget=-1; LastShieldAbsorbed=0;
            foreach (var member in party) member.Restore();
            Array.Clear(defendedHeroes,0,defendedHeroes.Length); Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length); Array.Clear(LastStatusDamage,0,LastStatusDamage.Length); IncomingSkill=null; LastUltimate=null; Array.Clear(LastUltimateDamage,0,LastUltimateDamage.Length);
            Array.Clear(LastUltimateHealing,0,LastUltimateHealing.Length); Array.Clear(LastUltimateGuard,0,LastUltimateGuard.Length);
            WaveIndex=0; LoadWaveEnemies();
            SelectedEnemyIndex = AttackingEnemyIndex = 0;
            ClearSkillReports(); ActiveIndex = 0; targetCursor = 0; LastDamage = 0; LastElementMultiplier = 1f; Current = CampaignComplete || (InTower && TowerComplete) ? Phase.Complete : Phase.Player; Defended = false;
        }
        void LoadWaveEnemies() {
            enemies.Clear();
            if(!campaign) enemies.Add(new BattleEnemy(new EnemyDefinition("Training Warden",100)));
            else foreach(var definition in ChapterDefinition.WaveAt(EnemyEncounter,WaveIndex,WaveCount))
                enemies.Add(new BattleEnemy(new EnemyDefinition(definition.Name,
                    (int)Math.Ceiling(definition.MaxHealth*Math.Max(1,PartyLimit/3.0)*TowerHealthScale),definition.Weakness,definition.Resistance,definition.IsBoss,definition.Role)));
        }
        public bool AdvanceWave() {
            if(Current!=Phase.WaveCleared || WaveIndex>=WaveCount-1) return false;
            ClearPassiveReports(); LastOffensivePassive=null;
            WaveIndex++; LoadWaveEnemies();
            foreach(var member in party) member.Acted=false;
            for(int i=0;i<party.Count;i++) if(party[i].Health>0) { ActiveIndex=i; break; }
            SelectedEnemyIndex=AttackingEnemyIndex=targetCursor=0;
            Defended=false; IncomingSkill=null; LastUltimate=null;
            LastMechanicTarget=-1; LastEnemyHealing=LastShieldAbsorbed=0;
            Array.Clear(defendedHeroes,0,defendedHeroes.Length);
            Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length);
            Array.Clear(LastStatusDamage,0,LastStatusDamage.Length);
            Array.Clear(LastUltimateDamage,0,LastUltimateDamage.Length);
            Current=Phase.Player; return true;
        }
        public readonly int[] LastSkillGuard=new int[MaxPartySize];
        public readonly bool[] LastSkillCleanse=new bool[MaxPartySize];
        public string LastUtilityText { get; private set; }
        public int LastUtilityTarget { get; private set; }
        void ClearSkillReports() { Array.Clear(LastSkillGuard,0,MaxPartySize); Array.Clear(LastSkillCleanse,0,MaxPartySize); LastUtilityText=null; LastUtilityTarget=-1; }
        public bool CanInterrupt(int target) {
            return target>=0 && target<enemies.Count && enemies[target].Health>0 && !enemies[target].InterruptQueued && (!enemies[target].Definition.IsBoss || enemies[target].InterruptRecovery==0);
        }
        public bool Attack() {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0) return false;
            LastOffensivePassive=null; ClearSkillReports();
            DealDamage(Hero.AttackDamage);
            CompleteAction(); return true;
        }
        public bool UseAbility(int slot = 0) {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0 || slot < 0 || slot > 1 || Active.SkillCharges(slot) == 0) return false;
            LastOffensivePassive=null; ClearSkillReports();
            int target=SelectedEnemyIndex; var utility=new List<string>();
            var skill = Active.Skill(slot);
            int skillHealing=(int)Math.Ceiling(skill.Healing*Active.HealingMultiplier);
            int healingBefore=0; foreach(var member in party) healingBefore+=member.Health;
            Active.Consume(slot);
            if (skill.PartyHealing) {
                foreach (var member in party)
                    if (member.Health > 0) member.Health = Math.Min(member.Definition.MaxHealth, member.Health + skillHealing);
            } else Active.Health = Math.Min(Hero.MaxHealth, Active.Health + skillHealing);
            int healingAfter=0; foreach(var member in party) healingAfter+=member.Health;
            Active.EncounterHealing=Math.Min(1000000,Active.EncounterHealing+healingAfter-healingBefore);
            for(int i=0;i<party.Count;i++) {
                var member=party[i]; if(member.Health<=0) continue;
                if(i==ActiveIndex || (skill.Utility&SkillUtility.PartyShield)!=0) { int before=member.Guard; member.Guard=Math.Max(member.Guard,skill.Guard); LastSkillGuard[i]=member.Guard-before; }
                if((skill.Utility&SkillUtility.Cleanse)!=0 && (skill.PartyHealing || i==ActiveIndex) && member.Status!=Debuff.None) { member.Status=Debuff.None; member.StatusRounds=0; LastSkillCleanse[i]=true; }
            }
            if((skill.Utility&SkillUtility.Taunt)!=0) { Active.TauntRounds=1; utility.Add("TAUNT"); }
            if((skill.Utility&SkillUtility.Dispel)!=0 && enemies[target].Shield>0) { enemies[target].Shield=0; utility.Add("WARD BROKEN"); }
            DealDamage(skill.Damage, skill.Affinity);
            var foe=enemies[target];
            if(foe.Health>0 && (skill.Utility&SkillUtility.Expose)!=0) { foe.ExposedRounds=1; utility.Add("EXPOSED +20%"); }
            if(foe.Health>0 && (skill.Utility&SkillUtility.Interrupt)!=0) {
                if(CanInterrupt(target)) {
                    if(foe.Charged) { foe.Charged=false; foe.ChargeDamage=60; foe.InterruptRecovery=foe.Definition.IsBoss?1:0; }
                    else foe.InterruptQueued=true;
                    utility.Add("INTERRUPTED");
                } else if(foe.Definition.IsBoss && foe.InterruptRecovery>0) utility.Add("INTERRUPT RESISTED");
            }
            LastUtilityText=string.Join(" · ",utility.ToArray()); LastUtilityTarget=target;
            CompleteAction(); return true;
        }
        public bool EnemyProtected(int index) {
            if(index<0 || index>=enemies.Count || enemies[index].Health==0 || enemies[index].Definition.Row!=EnemyRow.Back) return false;
            foreach(var protector in enemies) if(protector.Health>0 && protector.Definition.Row==EnemyRow.Front && protector.Definition.Role==EnemyRole.Defender) return true;
            return false;
        }
        void DealDamage(int amount, Element? skillElement = null, int target = -1, bool areaDamage=false) {
            var foe = enemies[target < 0 ? SelectedEnemyIndex : target];
            var definition = foe.Definition;
            LastElement = skillElement ?? Active.Identity.Affinity;
            LastElementMultiplier = amount > 0 ? definition.Multiplier(LastElement) : 1f;
            int damage = definition.Damage(amount, LastElement);
            if(ClassPassivesEnabled && Current==Phase.Player && damage>0) {
                double bonus=PassiveDamageMultiplier(Active,definition,LastElement);
                if(bonus>1) { damage=(int)Math.Ceiling(damage*bonus); LastOffensivePassive=Active.Identity.Class==HeroClass.Sorceress?"ARCANE EXPOSURE":"HUNTER'S MARK"; }
                if(Active.Identity.Class==HeroClass.Rogue && passiveRandom.NextDouble()<Active.CriticalChance) { damage=(int)Math.Ceiling(damage*Active.CriticalMultiplier); LastOffensivePassive="CRITICAL STRIKE"; }
            }
            damage=(int)Math.Ceiling(damage*Active.DamageBoost(LastElement));
            if(foe.ExposedRounds>0 && Current==Phase.Player) damage=(int)Math.Ceiling(damage*1.2);
            if (Active.Status == Debuff.Chill || Active.Status == Debuff.Weaken) damage = (int)Math.Ceiling(damage*.75f);
            if(damage>0 && !areaDamage && EnemyProtected(target<0?SelectedEnemyIndex:target)) {
                if(Active.Identity.Class!=HeroClass.Rogue) damage=(int)Math.Ceiling(damage*.8);
                else if(Current==Phase.Player && string.IsNullOrEmpty(LastOffensivePassive)) LastOffensivePassive="BYPASS GUARD";
            }
            LastShieldAbsorbed=Math.Min(foe.Shield,damage); foe.Shield-=LastShieldAbsorbed; damage-=LastShieldAbsorbed;
            LastDamage = Math.Min(foe.Health, damage);
            if(foe.Charged && Current==Phase.Player) {
                foe.ChargeDamage+=LastDamage;
                if(foe.ChargeDamage>=60) foe.Charged=false;
            }
            Active.EncounterDamage=Math.Min(1000000,Active.EncounterDamage+LastDamage);
            foe.Health = Math.Max(0, foe.Health - damage);
            if(foe.Health==0 && foe.Definition.Name=="Veiled Monarch") foreach(var enemy in enemies) enemy.Shield=0;
            if (enemies[SelectedEnemyIndex].Health == 0)
                for (int i = 0; i < enemies.Count; i++) if (enemies[i].Health > 0) { SelectedEnemyIndex = i; break; }
        }
        void CompleteAction() {
            LastUltimate=null; Array.Clear(LastUltimateDamage,0,LastUltimateDamage.Length);
            Array.Clear(LastUltimateHealing,0,LastUltimateHealing.Length); Array.Clear(LastUltimateGuard,0,LastUltimateGuard.Length);
            Active.Energy=Math.Min(PartyHero.MaxEnergy,Active.Energy+Active.EnergyGain);
            if(Active.Energy==PartyHero.MaxEnergy && !AllEnemiesDefeated) {
                LastUltimate=Active.Ultimate; UltimateTarget=SelectedEnemyIndex;
                int normalDamage=LastDamage; Element normalElement=LastElement; float normalMultiplier=LastElementMultiplier;
                for(int i=0;i<enemies.Count;i++) {
                    if(enemies[i].Health==0 || (!LastUltimate.AreaDamage && i!=UltimateTarget)) continue;
                    DealDamage(LastUltimate.Damage,LastUltimate.Affinity,i,LastUltimate.AreaDamage); LastUltimateDamage[i]=LastDamage;
                }
                for(int i=0;i<party.Count;i++) if(party[i].Health>0) {
                    var member=party[i]; int hp=member.Health,guard=member.Guard;
                    member.Health=Math.Min(member.Definition.MaxHealth,member.Health+LastUltimate.Healing);
                    member.Guard=Math.Max(member.Guard,LastUltimate.Guard);
                    LastUltimateHealing[i]=member.Health-hp; LastUltimateGuard[i]=member.Guard-guard;
                    Active.EncounterHealing=Math.Min(1000000,Active.EncounterHealing+LastUltimateHealing[i]);
                }
                Active.Energy=0;
                LastDamage=normalDamage; LastElement=normalElement; LastElementMultiplier=normalMultiplier;
            }
            Active.Acted = true; Defended = false;
            if (AllEnemiesDefeated) { Victory(); return; }
            for (int i = 0; i < party.Count; i++) {
                if (party[i].Health > 0 && !party[i].Acted) { ActiveIndex = i; return; }
            }
            ApplyPaladinGuard();
            for (int i = 0; i < enemies.Count; i++) if (enemies[i].Health > 0) { AttackingEnemyIndex = i; break; }
            PrepareEnemyStrike();
        }
        void ClearPassiveReports() {
            Array.Clear(LastPassiveGuard,0,LastPassiveGuard.Length);
            Array.Clear(LastPassiveCleanse,0,LastPassiveCleanse.Length);
            Array.Clear(LastPassiveHealing,0,LastPassiveHealing.Length);
            Array.Clear(LastPassiveProtection,0,LastPassiveProtection.Length);
        }
        public double PassiveDamageMultiplier(PartyHero hero,EnemyDefinition foe,Element element) {
            if(!ClassPassivesEnabled) return 1;
            if(hero.Identity.Class==HeroClass.Sorceress && foe.Multiplier(element)>1) return 1.2+(hero.HasTalent(0,0)?.1:0)+(hero.HasTalent(2,0)?.2:0);
            if(hero.Identity.Class==HeroClass.Ranger && foe.Row==EnemyRow.Back) return 1.2+(hero.HasTalent(0,0)?.1:0)+(hero.HasTalent(2,0)?.2:0);
            return 1;
        }
        void ApplyPaladinGuard() {
            Array.Clear(LastPassiveGuard,0,LastPassiveGuard.Length);
            if(!ClassPassivesEnabled) return;
            int guard=0;
            foreach(var member in party) if(member.Health>0 && member.Identity.Class==HeroClass.Paladin) guard=Math.Max(guard,8+(member.HasTalent(0,0)?4:0)+(member.HasTalent(2,0)?8:0));
            if(guard==0) return;
            for(int i=0;i<party.Count;i++) if(party[i].Health>0) {
                int before=party[i].Guard; party[i].Guard=Math.Max(before,guard); LastPassiveGuard[i]=party[i].Guard-before;
            }
        }
        void CleansePassive(int target) {
            var ally=party[target];
            if(ally.Health>0 && (ally.Status==Debuff.Burn || ally.Status==Debuff.Poison)) {
                LastPassiveCleanse[target]=ally.Status; ally.Status=Debuff.None; ally.StatusRounds=0;
            }
        }
        void ApplyClericHealing() {
            if(!ClassPassivesEnabled) return;
            foreach(var healer in party) if(healer.Health>0) {
                if(healer.Identity.Class==HeroClass.Paladin && healer.HasTalent(2,1)) {
                    for(int i=0;i<party.Count;i++) if(party[i].Health>0) {
                        int healed=Math.Min(party[i].Definition.MaxHealth-party[i].Health,(healer.Definition.MaxHealth*3+99)/100);
                        party[i].Health+=healed; LastPassiveHealing[i]+=healed; healer.EncounterHealing=Math.Min(1000000,healer.EncounterHealing+healed);
                    }
                }
                if(healer.Identity.Class!=HeroClass.Cleric) continue;
                int target=-1;
                for(int i=0;i<party.Count;i++) if(party[i].Health>0 && party[i].Health<party[i].Definition.MaxHealth &&
                    (target<0 || (long)party[i].Health*party[target].Definition.MaxHealth<(long)party[target].Health*party[i].Definition.MaxHealth)) target=i;
                if(target>=0) {
                    int percentage=10+(healer.HasTalent(0,0)?5:0)+(healer.HasTalent(2,0)?10:0);
                    int healed=Math.Min(party[target].Definition.MaxHealth-party[target].Health,(healer.Definition.MaxHealth*percentage+99)/100);
                    party[target].Health+=healed; LastPassiveHealing[target]+=healed;
                    healer.EncounterHealing=Math.Min(1000000,healer.EncounterHealing+healed);
                    if(healer.HasTalent(0,1)) CleansePassive(target);
                }
                if(healer.HasTalent(2,1)) for(int i=0;i<party.Count;i++) CleansePassive(i);
            }
        }
        void PrepareEnemyStrike() {
            Defended = false; Array.Clear(defendedHeroes,0,defendedHeroes.Length);
            IncomingSkill = campaign ? ((waveBattles || InTower)?EnemySkills.ForRole(EnemyEncounter,enemies[AttackingEnemyIndex].Definition,enemies[AttackingEnemyIndex].CompletedStrikes):EnemySkills.For(EnemyEncounter,enemies[AttackingEnemyIndex].Definition.IsBoss,enemies[AttackingEnemyIndex].CompletedStrikes)) : new EnemySkill("Quick Strike",35,Element.Physical);
            var attacker=enemies[AttackingEnemyIndex];
            if(attacker.Definition.Name=="Cinder Sovereign") {
                int move=attacker.CompletedStrikes%4;
                IncomingSkill=EnemySkills.For(EnemyEncounter,true,move);
                if(move==2) IncomingSkill=new EnemySkill("Inferno Charge · deal 60 damage next round to interrupt",0,Element.Fire,action:EnemyAction.Charge);
                if(move==3) IncomingSkill=new EnemySkill(attacker.Charged?"Charged Inferno":"Disrupted Inferno",attacker.Charged?70:20,Element.Fire,status:attacker.Charged?Debuff.Burn:Debuff.None);
            }
            if(attacker.Definition.Name=="Thorn Queen" && attacker.CompletedStrikes%3==1)
                IncomingSkill=new EnemySkill("Root Communion · restores a living partner",0,Element.Poison,action:EnemyAction.HealPartner);
            if(attacker.Definition.Name=="Veiled Monarch" && attacker.CompletedStrikes%3==1)
                IncomingSkill=new EnemySkill("Eclipse Veil · shields one boss; target the other",0,Element.Light,action:EnemyAction.Shield);
            if(attacker.InterruptQueued) {
                attacker.InterruptQueued=false; attacker.InterruptRecovery=attacker.Definition.IsBoss?1:0;
                IncomingSkill=new EnemySkill("Interrupted · action cancelled",0,Element.Light,action:EnemyAction.Interrupted);
            }
            if(WaveIndex<WaveCount-1 && IncomingSkill.Action==EnemyAction.Attack)
                IncomingSkill=new EnemySkill(IncomingSkill.Name,Math.Max(1,(int)Math.Ceiling(IncomingSkill.Damage*.6)),IncomingSkill.Element,IncomingSkill.PartyWide,IncomingSkill.Status,backRow:IncomingSkill.BackRow);
            if(campaign && waveBattles && !InTower && IncomingSkill.Action==EnemyAction.Attack)
                IncomingSkill=new EnemySkill(IncomingSkill.Name,(int)Math.Ceiling(IncomingSkill.Damage*ChapterDefinition.DamageScale(EnemyEncounter)),IncomingSkill.Element,IncomingSkill.PartyWide,IncomingSkill.Status,backRow:IncomingSkill.BackRow);
            if(InTower && IncomingSkill.Action==EnemyAction.Attack) IncomingSkill=new EnemySkill(IncomingSkill.Name,(int)Math.Ceiling(IncomingSkill.Damage*(1+(TowerFloor-1)*.03)),IncomingSkill.Element,IncomingSkill.PartyWide,IncomingSkill.Status,backRow:IncomingSkill.BackRow);
            // Three targeting tickets per front hero and one per back hero.
            var targets=new List<int>();
            if(IncomingSkill.BackRow)
                for(int i=FrontSlots;i<party.Count;i++) if(party[i].Health>0) targets.Add(i);
            if(targets.Count==0) {
                for(int repeat=0;repeat<3;repeat++)
                    for(int i=0;i<Math.Min(FrontSlots,party.Count);i++) if(party[i].Health>0) targets.Add(i);
                for(int i=FrontSlots;i<party.Count;i++) if(party[i].Health>0) targets.Add(i);
            }
            if(!IncomingSkill.PartyWide && !IncomingSkill.BackRow && IncomingSkill.Action==EnemyAction.Attack)
                for(int i=0;i<party.Count;i++) if(party[i].Health>0 && party[i].TauntRounds>0) { targets.Clear(); targets.Add(i); break; }
            if(targets.Count>0) { ActiveIndex=targets[targetCursor%targets.Count]; targetCursor=(targetCursor+1)%1000000; }
            Current = Phase.EnemyWindup;
        }
        public bool BeginStrike() {
            if (Current != Phase.EnemyWindup) return false;
            Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length); Array.Clear(LastStatusDamage,0,LastStatusDamage.Length);
            LastEnemyHealing=0; LastMechanicTarget=-1; LastShieldAbsorbed=0;
            Array.Clear(LastPassiveCleanse,0,LastPassiveCleanse.Length);
            Array.Clear(LastPassiveHealing,0,LastPassiveHealing.Length); Array.Clear(LastPassiveProtection,0,LastPassiveProtection.Length);
            Current = Phase.EnemyStrike; return true;
        }
        public bool Defend(bool parry, float elapsed) { return DefendHero(ActiveIndex,parry,elapsed); }
        public bool DefendHero(int index, bool parry, float elapsed) {
            if (Current != Phase.EnemyStrike || IncomingSkill.Action!=EnemyAction.Attack || index<0 || index>=party.Count || party[index].Health==0 ||
                (!IncomingSkill.PartyWide && index!=ActiveIndex) || defendedHeroes[index] || float.IsNaN(elapsed) || elapsed < 0 || elapsed > (parry ? .18f : .4f)) return false;
            defendedHeroes[index] = true;
            if(index==ActiveIndex) Defended=true;
            if (parry) {
                int previous=ActiveIndex; ActiveIndex=index;
                int counter=10+(ClassPassivesEnabled && Active.Identity.Class==HeroClass.Knight?(Active.HasTalent(0,1)?5:0)+(Active.HasTalent(2,1)?10:0):0);
                DealDamage((counter*(100+Active.FactionBonus)+99)/100,null,AttackingEnemyIndex); ActiveIndex=previous;
            }
            if (AllEnemiesDefeated) Victory();
            return true;
        }
        public void FinishStrike() {
            if (Current != Phase.EnemyStrike) return;
            Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length);
            Array.Clear(LastStatusDamage,0,LastStatusDamage.Length);
            var attacker=enemies[AttackingEnemyIndex];
            if(IncomingSkill.Action==EnemyAction.Interrupted) LastMechanicTarget=AttackingEnemyIndex;
            else if(attacker.InterruptRecovery>0) attacker.InterruptRecovery--;
            if(IncomingSkill.Action==EnemyAction.Charge) { attacker.Charged=true; attacker.ChargeDamage=0; LastMechanicTarget=AttackingEnemyIndex; }
            if(IncomingSkill.Action==EnemyAction.HealPartner) {
                int target=AttackingEnemyIndex;
                for(int i=0;i<enemies.Count;i++) if(i!=AttackingEnemyIndex && enemies[i].Health>0) { target=i; break; }
                var healed=enemies[target]; int before=healed.Health;
                healed.Health=Math.Min(healed.Definition.MaxHealth,healed.Health+healed.Definition.MaxHealth/(target==AttackingEnemyIndex?10:5));
                LastMechanicTarget=target; LastEnemyHealing=healed.Health-before;
            }
            if(IncomingSkill.Action==EnemyAction.Shield) {
                foreach(var enemy in enemies) enemy.Shield=0;
                int target=AttackingEnemyIndex;
                if((attacker.CompletedStrikes/3)%2==0)
                    for(int i=0;i<enemies.Count;i++) if(i!=AttackingEnemyIndex && enemies[i].Health>0) { target=i; break; }
                enemies[target].Shield=attacker.Definition.IsBoss?60:25; LastMechanicTarget=target;
            }
            if(IncomingSkill.Action==EnemyAction.Attack && attacker.Definition.Name=="Cinder Sovereign" && attacker.CompletedStrikes%4==3) { attacker.Charged=false; attacker.ChargeDamage=0; }
            var livingKnights=new bool[FrontSlots];
            for(int i=0;i<Math.Min(FrontSlots,party.Count);i++) livingKnights[i]=party[i].Health>0 && party[i].Identity.Class==HeroClass.Knight;
            for(int i=0;i<party.Count;i++) {
                var member=party[i];
                if(attacker.Health==0 || IncomingSkill.Action!=EnemyAction.Attack || member.Health==0 || (!IncomingSkill.PartyWide && i!=ActiveIndex) || defendedHeroes[i]) continue;
                int damage=IncomingSkill.Damage+(member.Status==Debuff.Shock?5:0);
                bool resistant=IncomingSkill.Element!=Element.Physical && member.Identity.Affinity==IncomingSkill.Element;
                if(resistant) damage=(int)Math.Ceiling(damage*.75f);
                if(ClassPassivesEnabled && IsFrontRow(i))
                    for(int protector=0;protector<Math.Min(FrontSlots,party.Count);protector++)
                        if(protector!=i && livingKnights[protector]) {
                            int reduction=20+(party[protector].HasTalent(0,0)?10:0)+(party[protector].HasTalent(2,0)?10:0);
                            int reduced=(int)Math.Ceiling(damage*(100-reduction)/100.0); LastPassiveProtection[i]=damage-reduced; damage=reduced; break;
                        }
                damage=Math.Max(0,damage-member.Guard-member.Protection); member.Guard=0;
                LastIncomingDamage[i]=Math.Min(member.Health,damage);
                member.Health=Math.Max(0,member.Health-damage);
                if(damage>0 && member.Health>0 && !resistant && IncomingSkill.Status!=Debuff.None) {
                    member.Status=IncomingSkill.Status; member.StatusRounds=2;
                }
            }
            enemies[AttackingEnemyIndex].CompletedStrikes++;
            bool survivor = false;
            foreach (var member in party) if (member.Health > 0) survivor = true;
            if (!survivor) { Current = Phase.Lost; return; }
            for (int i = AttackingEnemyIndex + 1; i < enemies.Count; i++) if (enemies[i].Health > 0) {
                AttackingEnemyIndex = i; PrepareEnemyStrike(); return;
            }
            foreach(var member in party) {
                if(member.Health>0 && member.StatusRounds>0) {
                    int tick=member.Status==Debuff.Burn?6:member.Status==Debuff.Poison?8:0;
                    int index=party.IndexOf(member); LastStatusDamage[index]=Math.Min(member.Health,tick);
                    member.Health=Math.Max(0,member.Health-tick);
                    if(--member.StatusRounds==0) member.Status=Debuff.None;
                }
            }
            ApplyClericHealing();
            foreach(var member in party) if(member.TauntRounds>0) member.TauntRounds--;
            foreach(var foe in enemies) if(foe.ExposedRounds>0) foe.ExposedRounds--;
            Current = Phase.Lost;
            for (int i = 0; i < party.Count; i++) {
                party[i].Acted = false;
                if (party[i].Health > 0 && Current == Phase.Lost) { ActiveIndex = i; Current = Phase.Player; }
            }
        }
    }
}
