using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum Phase { Player, EnemyWindup, EnemyStrike, WaveCleared, Won, Lost, Complete }
    public sealed class PartyHero {
        public HeroDefinition Identity { get; private set; }
        ClassDefinition stats;
        public ClassDefinition Definition { get { return stats; } }
        public const int MaxLevel=20, MaxUpgradeRank=5;
        readonly int[] talents={-1,-1,-1};
        public int TalentChoice(int tier) { return talents[tier]; }
        public bool HasTalent(int tier,int choice) { return Level>=ClassTalents.UnlockLevel(tier) && talents[tier]==choice; }
        internal void SetTalent(int tier,int choice) { talents[tier]=choice; }
        public int EnergyGain { get { return ActionEnergy+(Identity.Class!=HeroClass.Knight && Identity.Class!=HeroClass.Paladin && HasTalent(1,0)?10:0); } }
        public double HealingMultiplier { get { return Identity.Class==HeroClass.Paladin && HasTalent(0,1)?1.2:1; } }
        public double CriticalChance { get { return .25+(HasTalent(0,0)?.10:0)+(HasTalent(2,0)?.15:0); } }
        public double CriticalMultiplier { get { return 1.5+(HasTalent(0,1)?.25:0)+(HasTalent(2,1)?.25:0); } }
        public int Level { get; internal set; } = 1;
        public int Experience { get; internal set; }
        public int UpgradeRank { get; internal set; }
        public int Shards { get; internal set; }
        public int NextLevelXP { get { return 100+(Level-1)*25; } }
        public int UpgradeCost { get { return 20*(UpgradeRank+1); } }
        public float Growth { get { return 1f+(Level-1)*.05f+UpgradeRank*.10f; } }
        readonly GearDefinition[] gear=new GearDefinition[3];
        public GearDefinition Gear(GearSlot slot) { return gear[(int)slot]; }
        internal void SetGear(GearSlot slot,GearDefinition item) { gear[(int)slot]=item; RefreshStats(); }
        public int Protection { get { var armor=Gear(GearSlot.Armor); return armor==null?0:armor.Protection; } }
        public float DamageBoost(Element element) { var accessory=Gear(GearSlot.Accessory); return accessory!=null && accessory.Element==element?1+accessory.ElementBoost:1; }
        public UltimateDefinition Ultimate { get { var weapon=Gear(GearSlot.Weapon); return Identity.Ultimate.Scaled(Growth).WithTalentDamage((Identity.Class==HeroClass.Sorceress || Identity.Class==HeroClass.Ranger) && HasTalent(2,1)?20:0).WithDamageBonus(weapon==null?0:weapon.SkillDamage); } }
        internal void RefreshStats() {
            var weapon=Gear(GearSlot.Weapon); var armor=Gear(GearSlot.Armor);
            bool tank=Identity.Class==HeroClass.Knight || Identity.Class==HeroClass.Paladin;
            bool health=HasTalent(1,tank?0:1);
            bool skill=HasTalent(tank?1:0,1) && (tank || Identity.Class==HeroClass.Sorceress || Identity.Class==HeroClass.Ranger);
            stats=Identity.Stats.Scaled(Growth).WithTalentBonuses(health?15:0,skill?10:0).WithEquipment(armor==null?0:armor.Health,weapon==null?0:weapon.Attack,weapon==null?0:weapon.SkillDamage);
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
        public int AbilityCharges { get { return charges[0]; } }
        public SkillDefinition Skill(int slot) { return Definition.Skills[equipped[slot]]; }
        public int SkillIndex(int slot) { return equipped[slot]; }
        public int SkillCharges(int slot) { return charges[slot]; }
        internal void Equip(int slot, int index) { equipped[slot] = index; }
        internal void Consume(int slot) { charges[slot]--; }
        public bool Acted { get; internal set; }
        public PartyHero(HeroClass kind) : this(HeroDefinition.Common(kind)) { }
        public PartyHero(HeroDefinition identity) { Identity = identity; RefreshStats(); Restore(); }
        internal void Restore() { EncounterDamage=EncounterHealing=0; Health = Definition.MaxHealth; charges[0] = charges[1] = 2; Guard = 0; Energy = 0; Status = Debuff.None; StatusRounds = 0; Acted = false; }
    }
    public sealed class BattleEnemy {
        public EnemyDefinition Definition { get; private set; }
        public int CompletedStrikes { get; internal set; }
        public bool Charged { get; internal set; }
        public int ChargeDamage { get; internal set; }
        public int Shield { get; internal set; }
        public int Health { get; internal set; }
        public BattleEnemy(EnemyDefinition definition) { Definition = definition; Health = definition.MaxHealth; }
    }
    public sealed class Battle {
        readonly List<PartyHero> party = new List<PartyHero>();
        readonly bool campaign;
        readonly bool waveBattles;
        public bool ClassPassivesEnabled { get; private set; }
        readonly Random passiveRandom;
        public string LastOffensivePassive { get; private set; }
        public readonly Debuff[] LastPassiveCleanse=new Debuff[MaxPartySize];
        public readonly int[] LastPassiveGuard=new int[MaxPartySize], LastPassiveHealing=new int[MaxPartySize], LastPassiveProtection=new int[MaxPartySize];
        public int WaveIndex { get; private set; }
        public int WaveCount { get { return campaign && waveBattles ? 3 : 1; } }
        public int Crystals { get; private set; }
        public int RareMisses { get; private set; }
        public int LegendaryMisses { get; private set; }
        public bool CampaignComplete { get { return campaign && encounter >= ChapterDefinition.TotalStages; } }
        public int ChapterIndex { get { return Math.Min(encounter / ChapterDefinition.StagesPerChapter, ChapterDefinition.Catalog.Count - 1); } }
        public int StageIndex { get { return encounter % ChapterDefinition.StagesPerChapter; } }
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
            return added;
        }
        public bool SwapFormation(int first,int second) {
            if(!CanChangeParty || first<0 || second<0 || first>=party.Count || second>=party.Count || first==second) return false;
            var hero=party[first]; party[first]=party[second]; party[second]=hero;
            if(ActiveIndex==first) ActiveIndex=second; else if(ActiveIndex==second) ActiveIndex=first;
            return true;
        }
        public int PartyLimit { get { return campaign?Math.Min(MaxPartySize,ChapterIndex+1):MaxPartySize; } }
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
        public readonly int[] LastUltimateDamage = new int[2];
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
        public Battle(bool recruitmentCampaign, bool enableWaves=false, bool enableClassPassives=false, Random criticalRandom=null) { campaign = recruitmentCampaign; waveBattles=enableWaves; ClassPassivesEnabled=enableClassPassives; passiveRandom=criticalRandom??new Random(); Crystals = Summoning.StarterCrystals; unlocked.Add(HeroClass.Knight); recruits.Add(HeroDefinition.Common(HeroClass.Knight)); party.Add(new PartyHero(HeroClass.Knight)); Reset(); }
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
        public bool EquipSkill(int slot, int skillIndex) {
            if (!CanChangeParty || slot < 0 || slot > 1 || skillIndex < 0 || skillIndex >= Hero.Skills.Count) return false;
            if (Active.SkillIndex(1 - slot) == skillIndex) return false;
            Active.Equip(slot, skillIndex); return true;
        }
        public bool ReplaceActiveHero(HeroClass kind) {
            if (!CanChangeParty || !unlocked.Contains(kind)) return false;
            foreach (var member in party) if (member.Definition.Kind == kind) return false;
            return EquipHero(HeroDefinition.Common(kind).Id);
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
                party[ActiveIndex] = replacement; return true;
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
        public GearDefinition LastLoot { get; private set; }
        public int GearCount(string id) { int count; return inventory.TryGetValue(id,out count)?count:0; }
        public BattleResults Results { get; private set; }
        void BuildResults(GearDefinition loot) {
            int xp=campaign?40+ChapterIndex*10+(enemies[0].Definition.IsBoss?20:0):0;
            string recruit=null;
            if(campaign) foreach(var candidate in HeroDefinition.Catalog) { bool known=false; foreach(var hero in recruits) if(hero.Id==candidate.Id) known=true; if(!known) { recruit=candidate.Name; break; } }
            Results=new BattleResults(xp,campaign?Summoning.VictoryCrystals:0,loot,recruit,Party);
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
            if (campaign) {
                LastLoot=loot;
                inventory[LastLoot.Id]=Math.Min(1000000,GearCount(LastLoot.Id)+1);
                int xp=40+ChapterIndex*10+(enemies[0].Definition.IsBoss?20:0);
                foreach(var member in party) member.GainExperience(xp);
                foreach(var candidate in HeroDefinition.Catalog) {
                    bool known=false;
                    foreach(var recruit in recruits) if(recruit.Id==candidate.Id) known=true;
                    if(!known) { AddRecruit(candidate,true); break; }
                }
                Crystals+=Summoning.VictoryCrystals;
                encounter++;
                foreach(var hero in recruits) {
                    if(party.Count>=PartyLimit) break;
                    bool active=false; foreach(var member in party) if(member.Identity.Id==hero.Id) active=true;
                    if(!active) party.Add(HeroProgress(hero.Id));
                }
            }
            Reset(); return true;
        }
        // Replay the campaign with the existing roster, loadouts and summon economy.
        public bool RestartCampaign() {
            if (!CampaignComplete || Current != Phase.Complete) return false;
            encounter = 0;
            while(party.Count>PartyLimit) party.RemoveAt(party.Count-1);
            Reset(); return true;
        }
        void AddRecruit(HeroDefinition hero, bool addToParty) {
            recruits.Add(hero);
            if(!unlocked.Contains(hero.Class)) unlocked.Add(hero.Class);
            var member=new PartyHero(hero); savedHeroes[hero.Id]=member;
            if(addToParty && party.Count<PartyLimit) party.Add(member);
        }
        public bool CanSummon { get { return campaign && (CanChangeParty || CampaignComplete) && Crystals>=Summoning.Cost; } }
        public SummonResult Summon(double rarityRoll, int classIndex) {
            if(!CanSummon || classIndex<0 || classIndex>=6 || double.IsNaN(rarityRoll) || double.IsInfinity(rarityRoll) || rarityRoll<0 || rarityRoll>=1) return null;
            var quality=Summoning.Quality(rarityRoll,RareMisses,LegendaryMisses);
            HeroDefinition candidate=null;
            foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==quality && (int)hero.Class==classIndex) candidate=hero;
            if(candidate==null) return null;
            bool duplicate=false;
            foreach(var known in recruits) if(known.Id==candidate.Id) duplicate=true;
            int refund=duplicate?Summoning.DuplicateRefund(quality):0;
            Crystals=Crystals-Summoning.Cost+refund;
            RareMisses=quality>=HeroQuality.Rare?0:RareMisses+1;
            LegendaryMisses=quality==HeroQuality.Legendary?0:LegendaryMisses+1;
            if(!duplicate) AddRecruit(candidate,false);
            else { var member=HeroProgress(candidate.Id); member.Shards=Math.Min(1000000,member.Shards+10+(int)quality*5); }
            return new SummonResult(candidate,duplicate,refund);
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
        public bool EquipGear(string heroId,GearSlot slot,string itemId) {
            if(!campaign || (!CanChangeParty && !CampaignComplete) || (int)slot<0 || (int)slot>2) return false;
            var member=HeroProgress(heroId); var item=itemId==null?null:GearDefinition.Find(itemId);
            if(member==null || (itemId!=null && (item==null || item.Slot!=slot))) return false;
            if(member.Gear(slot)==item) return false;
            if(item!=null) {
                int used=0; foreach(var hero in recruits) { var owner=HeroProgress(hero.Id); if(owner!=null && owner.Gear(slot)==item) used++; }
                if(used>=GearCount(item.Id)) return false;
            }
            member.SetGear(slot,item); member.Restore(); return true;
        }
        public ProgressData ExportProgress() {
            var data = new ProgressData { crystals = Crystals, rareMisses = RareMisses, legendaryMisses = LegendaryMisses, encounter = encounter, pendingVictory = Current == Phase.Won, activeIndex = ActiveIndex,
                recruited = new string[recruits.Count], party = new string[party.Count], loadouts = new HeroLoadoutData[recruits.Count] };
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
            if (!campaign || data == null || (data.version != 1 && data.version != 2 && data.version != 3 && data.version != 4 && data.version != 5 && data.version != 6) || data.encounter < 0 || data.encounter > ChapterDefinition.TotalStages || (data.pendingVictory && data.encounter == ChapterDefinition.TotalStages) ||
                data.recruited == null || data.recruited.Length < 1 || data.recruited.Length > HeroDefinition.Catalog.Count ||
                data.party == null || data.party.Length < 1 || data.party.Length > MaxPartySize ||
                data.loadouts == null || data.loadouts.Length != data.recruited.Length ||
                data.activeIndex < 0 || data.activeIndex >= data.party.Length) return false;
            if(data.version>=2 && (data.crystals<0 || data.crystals>1000000 || data.rareMisses<0 || data.rareMisses>=Summoning.RareGuarantee || data.legendaryMisses<0 || data.legendaryMisses>=Summoning.LegendaryGuarantee)) return false;
            var restoredInventory=new Dictionary<string,int>();
            if(data.version>=4) {
                if(data.inventory==null || data.inventory.Length>GearDefinition.Catalog.Count) return false;
                foreach(var item in data.inventory) {
                    if(item==null || GearDefinition.Find(item.id)==null || item.count<1 || item.count>1000000 || restoredInventory.ContainsKey(item.id)) return false;
                    restoredInventory.Add(item.id,item.count);
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
                    }
                }
                member.Equip(0, loadout.firstSkill); member.Equip(1, loadout.secondSkill);
                restored.Add(member.Identity.Id, member);
            }
            var chosen = new HashSet<string>();
            foreach (var id in data.party) if (id == null || !restored.ContainsKey(id) || !chosen.Add(id)) return false;
            int restoredLimit=Math.Min(MaxPartySize,Math.Min(data.encounter/ChapterDefinition.StagesPerChapter,ChapterDefinition.Catalog.Count-1)+1);
            if(data.version>=5 && data.party.Length>restoredLimit) return false;
            GearDefinition pendingLoot=null;
            if(data.version>=5) {
                if(data.pendingVictory) {
                    var result=data.results;
                    if(result==null || result.damage==null || result.healing==null || result.damage.Length!=data.party.Length || result.healing.Length!=data.party.Length) return false;
                    pendingLoot=GearDefinition.Find(result.lootId); if(pendingLoot==null || (ChapterDefinition.EnemiesAt(data.encounter)[0].IsBoss && pendingLoot.Quality<HeroQuality.Rare)) return false;
                    for(int i=0;i<data.party.Length;i++) if(result.damage[i]<0 || result.damage[i]>1000000 || result.healing[i]<0 || result.healing[i]>1000000) return false;
                } else if(data.results!=null && (!string.IsNullOrEmpty(data.results.lootId) ||
                    (data.results.damage!=null && data.results.damage.Length>0) ||
                    (data.results.healing!=null && data.results.healing.Length>0))) return false;
            }
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
            Results=null; LastEnemyHealing=0; LastMechanicTarget=-1; LastShieldAbsorbed=0;
            foreach (var member in party) member.Restore();
            Array.Clear(defendedHeroes,0,defendedHeroes.Length); Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length); Array.Clear(LastStatusDamage,0,LastStatusDamage.Length); IncomingSkill=null; LastUltimate=null; Array.Clear(LastUltimateDamage,0,LastUltimateDamage.Length);
            Array.Clear(LastUltimateHealing,0,LastUltimateHealing.Length); Array.Clear(LastUltimateGuard,0,LastUltimateGuard.Length);
            WaveIndex=0; LoadWaveEnemies();
            SelectedEnemyIndex = AttackingEnemyIndex = 0;
            ActiveIndex = 0; targetCursor = 0; LastDamage = 0; LastElementMultiplier = 1f; Current = CampaignComplete ? Phase.Complete : Phase.Player; Defended = false;
        }
        void LoadWaveEnemies() {
            enemies.Clear();
            if(!campaign) enemies.Add(new BattleEnemy(new EnemyDefinition("Training Warden",100)));
            else foreach(var definition in ChapterDefinition.WaveAt(Math.Min(encounter,ChapterDefinition.TotalStages-1),WaveIndex,WaveCount))
                enemies.Add(new BattleEnemy(new EnemyDefinition(definition.Name,
                    (int)Math.Ceiling(definition.MaxHealth*Math.Max(1,PartyLimit/3.0)),definition.Weakness,definition.Resistance,definition.IsBoss)));
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
        public bool Attack() {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0) return false;
            LastOffensivePassive=null;
            DealDamage(Hero.AttackDamage);
            CompleteAction(); return true;
        }
        public bool UseAbility(int slot = 0) {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0 || slot < 0 || slot > 1 || Active.SkillCharges(slot) == 0) return false;
            LastOffensivePassive=null;
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
            Active.Guard = Math.Max(Active.Guard, skill.Guard);
            DealDamage(skill.Damage, skill.Affinity);
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
            IncomingSkill = campaign ? EnemySkills.For(encounter,enemies[AttackingEnemyIndex].Definition.IsBoss,enemies[AttackingEnemyIndex].CompletedStrikes) : new EnemySkill("Quick Strike",35,Element.Physical);
            var attacker=enemies[AttackingEnemyIndex];
            if(attacker.Definition.Name=="Cinder Sovereign") {
                int move=attacker.CompletedStrikes%4;
                IncomingSkill=EnemySkills.For(encounter,true,move);
                if(move==2) IncomingSkill=new EnemySkill("Inferno Charge · deal 60 damage next round to interrupt",0,Element.Fire,action:EnemyAction.Charge);
                if(move==3) IncomingSkill=new EnemySkill(attacker.Charged?"Charged Inferno":"Disrupted Inferno",attacker.Charged?70:20,Element.Fire,status:attacker.Charged?Debuff.Burn:Debuff.None);
            }
            if(attacker.Definition.Name=="Thorn Queen" && attacker.CompletedStrikes%3==1)
                IncomingSkill=new EnemySkill("Root Communion · restores a living partner",0,Element.Poison,action:EnemyAction.HealPartner);
            if(attacker.Definition.Name=="Veiled Monarch" && attacker.CompletedStrikes%3==1)
                IncomingSkill=new EnemySkill("Eclipse Veil · shields one boss; target the other",0,Element.Light,action:EnemyAction.Shield);
            if(WaveIndex<WaveCount-1 && IncomingSkill.Action==EnemyAction.Attack)
                IncomingSkill=new EnemySkill(IncomingSkill.Name,Math.Max(1,(int)Math.Ceiling(IncomingSkill.Damage*.6)),IncomingSkill.Element,IncomingSkill.PartyWide,IncomingSkill.Status,backRow:IncomingSkill.BackRow);
            // Three targeting tickets per front hero and one per back hero.
            var targets=new List<int>();
            if(IncomingSkill.BackRow)
                for(int i=FrontSlots;i<party.Count;i++) if(party[i].Health>0) targets.Add(i);
            if(targets.Count==0) {
                for(int repeat=0;repeat<3;repeat++)
                    for(int i=0;i<Math.Min(FrontSlots,party.Count);i++) if(party[i].Health>0) targets.Add(i);
                for(int i=FrontSlots;i<party.Count;i++) if(party[i].Health>0) targets.Add(i);
            }
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
                DealDamage(10+(ClassPassivesEnabled && Active.Identity.Class==HeroClass.Knight?(Active.HasTalent(0,1)?5:0)+(Active.HasTalent(2,1)?10:0):0),null,AttackingEnemyIndex); ActiveIndex=previous;
            }
            if (AllEnemiesDefeated) Victory();
            return true;
        }
        public void FinishStrike() {
            if (Current != Phase.EnemyStrike) return;
            Array.Clear(LastIncomingDamage,0,LastIncomingDamage.Length);
            Array.Clear(LastStatusDamage,0,LastStatusDamage.Length);
            var attacker=enemies[AttackingEnemyIndex];
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
                enemies[target].Shield=60; LastMechanicTarget=target;
            }
            if(IncomingSkill.Action==EnemyAction.Attack && attacker.Definition.Name=="Cinder Sovereign" && attacker.CompletedStrikes%4==3) { attacker.Charged=false; attacker.ChargeDamage=0; }
            var livingKnights=new bool[FrontSlots];
            for(int i=0;i<Math.Min(FrontSlots,party.Count);i++) livingKnights[i]=party[i].Health>0 && party[i].Identity.Class==HeroClass.Knight;
            for(int i=0;i<party.Count;i++) {
                var member=party[i];
                if(IncomingSkill.Action!=EnemyAction.Attack || member.Health==0 || (!IncomingSkill.PartyWide && i!=ActiveIndex) || defendedHeroes[i]) continue;
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
            Current = Phase.Lost;
            for (int i = 0; i < party.Count; i++) {
                party[i].Acted = false;
                if (party[i].Health > 0 && Current == Phase.Lost) { ActiveIndex = i; Current = Phase.Player; }
            }
        }
    }
}
