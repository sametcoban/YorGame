using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum Phase { Player, EnemyWindup, EnemyStrike, Won, Lost, Complete }
    public sealed class PartyHero {
        public HeroDefinition Identity { get; private set; }
        public ClassDefinition Definition { get { return Identity.Stats; } }
        public int Health { get; internal set; }
        readonly int[] equipped = { 0, 1 };
        readonly int[] charges = { 2, 2 };
        public int Guard { get; internal set; }
        public int AbilityCharges { get { return charges[0]; } }
        public SkillDefinition Skill(int slot) { return Definition.Skills[equipped[slot]]; }
        public int SkillIndex(int slot) { return equipped[slot]; }
        public int SkillCharges(int slot) { return charges[slot]; }
        internal void Equip(int slot, int index) { equipped[slot] = index; }
        internal void Consume(int slot) { charges[slot]--; }
        public bool Acted { get; internal set; }
        public PartyHero(HeroClass kind) : this(HeroDefinition.Common(kind)) { }
        public PartyHero(HeroDefinition identity) { Identity = identity; Restore(); }
        internal void Restore() { Health = Definition.MaxHealth; charges[0] = charges[1] = 2; Guard = 0; Acted = false; }
    }
    public sealed class BattleEnemy {
        public EnemyDefinition Definition { get; private set; }
        public int Health { get; internal set; }
        public BattleEnemy(EnemyDefinition definition) { Definition = definition; Health = definition.MaxHealth; }
    }
    public sealed class Battle {
        readonly List<PartyHero> party = new List<PartyHero>();
        readonly bool campaign;
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
        public const int MaxPartySize = 3;
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
        public Battle() : this(HeroClass.Knight) { }
        public Battle(HeroClass kind) { unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset(); }
        public Battle(bool recruitmentCampaign) { campaign = recruitmentCampaign; Crystals = Summoning.StarterCrystals; unlocked.Add(HeroClass.Knight); recruits.Add(HeroDefinition.Common(HeroClass.Knight)); party.Add(new PartyHero(HeroClass.Knight)); Reset(); }
        public void SelectClass(HeroClass kind) {
            if (campaign) throw new InvalidOperationException("Campaign heroes are recruited, not replaced.");
            party.Clear(); recruits.Clear(); unlocked.Clear(); unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset();
        }
        public bool CanChangeParty {
            get {
                if (Current != Phase.Player) return false;
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
        public bool ContinueAfterVictory() {
            if (Current != Phase.Won) return false;
            if (campaign) {
                foreach(var candidate in HeroDefinition.Catalog) {
                    bool known=false;
                    foreach(var recruit in recruits) if(recruit.Id==candidate.Id) known=true;
                    if(!known) { AddRecruit(candidate,true); break; }
                }
                Crystals+=Summoning.VictoryCrystals;
                encounter++;
            }
            Reset(); return true;
        }
        // Replay the campaign with the existing roster, loadouts and summon economy.
        public bool RestartCampaign() {
            if (!CampaignComplete || Current != Phase.Complete) return false;
            encounter = 0;
            Reset(); return true;
        }
        void AddRecruit(HeroDefinition hero, bool addToParty) {
            recruits.Add(hero);
            if(!unlocked.Contains(hero.Class)) unlocked.Add(hero.Class);
            if(addToParty && party.Count<MaxPartySize) party.Add(new PartyHero(hero));
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
            return new SummonResult(candidate,duplicate,refund);
        }
        public ProgressData ExportProgress() {
            var data = new ProgressData { crystals = Crystals, rareMisses = RareMisses, legendaryMisses = LegendaryMisses, encounter = encounter, pendingVictory = Current == Phase.Won, activeIndex = ActiveIndex,
                recruited = new string[recruits.Count], party = new string[party.Count], loadouts = new HeroLoadoutData[recruits.Count] };
            for (int i = 0; i < party.Count; i++) data.party[i] = party[i].Identity.Id;
            for (int i = 0; i < recruits.Count; i++) {
                data.recruited[i] = recruits[i].Id;
                PartyHero member = null;
                foreach (var ally in party) if (ally.Identity.Id == recruits[i].Id) member = ally;
                if (member == null) savedHeroes.TryGetValue(recruits[i].Id, out member);
                data.loadouts[i] = new HeroLoadoutData { id = recruits[i].Id, firstSkill = member == null ? 0 : member.SkillIndex(0), secondSkill = member == null ? 1 : member.SkillIndex(1) };
            }
            return data;
        }
        // Validate the whole snapshot before mutating live progress. Loading starts a fresh encounter.
        public bool RestoreProgress(ProgressData data) {
            if (!campaign || data == null || (data.version != 1 && data.version != 2) || data.encounter < 0 || data.encounter > ChapterDefinition.TotalStages || (data.pendingVictory && data.encounter == ChapterDefinition.TotalStages) ||
                data.recruited == null || data.recruited.Length < 1 || data.recruited.Length > HeroDefinition.Catalog.Count ||
                data.party == null || data.party.Length < 1 || data.party.Length > MaxPartySize ||
                data.loadouts == null || data.loadouts.Length != data.recruited.Length ||
                data.activeIndex < 0 || data.activeIndex >= data.party.Length) return false;
            if(data.version==2 && (data.crystals<0 || data.crystals>1000000 || data.rareMisses<0 || data.rareMisses>=Summoning.RareGuarantee || data.legendaryMisses<0 || data.legendaryMisses>=Summoning.LegendaryGuarantee)) return false;
            var restored = new Dictionary<string, PartyHero>();
            for (int i = 0; i < data.recruited.Length; i++) {
                HeroDefinition identity=null;
                foreach(var known in HeroDefinition.Catalog) if(known.Id==data.recruited[i]) identity=known;
                if(identity==null || restored.ContainsKey(identity.Id) || data.loadouts[i]==null || data.loadouts[i].id!=identity.Id) return false;
                if(i==0 && identity.Id!=HeroDefinition.Common(HeroClass.Knight).Id) return false;
                var loadout = data.loadouts[i];
                if (loadout.firstSkill < 0 || loadout.firstSkill >= 6 || loadout.secondSkill < 0 || loadout.secondSkill >= 6 || loadout.firstSkill == loadout.secondSkill) return false;
                var member = new PartyHero(identity);
                member.Equip(0, loadout.firstSkill); member.Equip(1, loadout.secondSkill);
                restored.Add(member.Identity.Id, member);
            }
            var chosen = new HashSet<string>();
            foreach (var id in data.party) if (id == null || !restored.ContainsKey(id) || !chosen.Add(id)) return false;
            recruits.Clear(); unlocked.Clear(); party.Clear(); savedHeroes.Clear();
            for (int i = 0; i < data.recruited.Length; i++) {
                var identity = restored[data.recruited[i]].Identity; recruits.Add(identity);
                if (!unlocked.Contains(identity.Class)) unlocked.Add(identity.Class);
                savedHeroes.Add(identity.Id, restored[identity.Id]);
            }
            foreach (var id in data.party) party.Add(restored[id]);
            Crystals=data.version==1?Summoning.StarterCrystals:data.crystals;
            RareMisses=data.version==1?0:data.rareMisses; LegendaryMisses=data.version==1?0:data.legendaryMisses;
            encounter = data.encounter; Reset(); ActiveIndex = data.activeIndex;
            if (data.pendingVictory) { Current = Phase.Won; foreach (var enemy in enemies) enemy.Health = 0; }
            return true;
        }
        public void Reset() {
            foreach (var member in party) member.Restore();
            enemies.Clear();
            if (campaign) foreach (var definition in ChapterDefinition.EnemiesAt(Math.Min(encounter, ChapterDefinition.TotalStages - 1))) enemies.Add(new BattleEnemy(definition));
            else enemies.Add(new BattleEnemy(new EnemyDefinition("Training Warden", 100)));
            SelectedEnemyIndex = AttackingEnemyIndex = 0;
            ActiveIndex = 0; targetCursor = 0; LastDamage = 0; LastElementMultiplier = 1f; Current = CampaignComplete ? Phase.Complete : Phase.Player; Defended = false;
        }
        public bool Attack() {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0) return false;
            DealDamage(Hero.AttackDamage);
            CompleteAction(); return true;
        }
        public bool UseAbility(int slot = 0) {
            if (Current != Phase.Player || Active.Acted || Active.Health == 0 || slot < 0 || slot > 1 || Active.SkillCharges(slot) == 0) return false;
            var skill = Active.Skill(slot);
            Active.Consume(slot);
            if (skill.PartyHealing) {
                foreach (var member in party)
                    if (member.Health > 0) member.Health = Math.Min(member.Definition.MaxHealth, member.Health + skill.Healing);
            } else Active.Health = Math.Min(Hero.MaxHealth, Active.Health + skill.Healing);
            Active.Guard = Math.Max(Active.Guard, skill.Guard);
            DealDamage(skill.Damage, skill.Affinity);
            CompleteAction(); return true;
        }
        void DealDamage(int amount, Element? skillElement = null, int target = -1) {
            var foe = enemies[target < 0 ? SelectedEnemyIndex : target];
            var definition = foe.Definition;
            LastElement = skillElement ?? Active.Identity.Affinity;
            LastElementMultiplier = amount > 0 ? definition.Multiplier(LastElement) : 1f;
            int damage = definition.Damage(amount, LastElement);
            LastDamage = Math.Min(foe.Health, damage);
            foe.Health = Math.Max(0, foe.Health - damage);
            if (enemies[SelectedEnemyIndex].Health == 0)
                for (int i = 0; i < enemies.Count; i++) if (enemies[i].Health > 0) { SelectedEnemyIndex = i; break; }
        }
        void CompleteAction() {
            Active.Acted = true; Defended = false;
            if (AllEnemiesDefeated) { Current = Phase.Won; return; }
            for (int i = 0; i < party.Count; i++) {
                if (party[i].Health > 0 && !party[i].Acted) { ActiveIndex = i; return; }
            }
            for (int i = 0; i < enemies.Count; i++) if (enemies[i].Health > 0) { AttackingEnemyIndex = i; break; }
            PrepareEnemyStrike();
        }
        void PrepareEnemyStrike() {
            Defended = false;
            // Cycle targets across living allies for each surviving enemy's strike.
            for (int offset = 0; offset < party.Count; offset++) {
                int index = (targetCursor + offset) % party.Count;
                if (party[index].Health > 0) { ActiveIndex = index; targetCursor = (index + 1) % party.Count; break; }
            }
            Current = Phase.EnemyWindup;
        }
        public bool BeginStrike() {
            if (Current != Phase.EnemyWindup) return false;
            Current = Phase.EnemyStrike; return true;
        }
        public bool Defend(bool parry, float elapsed) {
            if (Current != Phase.EnemyStrike || Defended || elapsed < 0 || elapsed > (parry ? .18f : .4f)) return false;
            Defended = true;
            if (parry) DealDamage(10, null, AttackingEnemyIndex);
            if (AllEnemiesDefeated) Current = Phase.Won;
            return true;
        }
        public void FinishStrike() {
            if (Current != Phase.EnemyStrike) return;
            if (!Defended) {
                Active.Health = Math.Max(0, Active.Health - Math.Max(0, 35 - Active.Guard));
                Active.Guard = 0;
            }
            bool survivor = false;
            foreach (var member in party) if (member.Health > 0) survivor = true;
            if (!survivor) { Current = Phase.Lost; return; }
            for (int i = AttackingEnemyIndex + 1; i < enemies.Count; i++) if (enemies[i].Health > 0) {
                AttackingEnemyIndex = i; PrepareEnemyStrike(); return;
            }
            Current = Phase.Lost;
            for (int i = 0; i < party.Count; i++) {
                party[i].Acted = false;
                if (party[i].Health > 0 && Current == Phase.Lost) { ActiveIndex = i; Current = Phase.Player; }
            }
        }
    }
}
