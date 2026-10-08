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
    public sealed class Battle {
        readonly List<PartyHero> party = new List<PartyHero>();
        readonly bool campaign;
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
        public EnemyDefinition Enemy { get; private set; }
        public int LastDamage { get; private set; }
        public float LastElementMultiplier { get; private set; }
        public Element LastElement { get; private set; }
        public IReadOnlyList<PartyHero> Party { get { return party.AsReadOnly(); } }
        public int ActiveIndex { get; private set; }
        PartyHero Active { get { return party[ActiveIndex]; } }
        public ClassDefinition Hero { get { return Active.Definition; } }
        public int AbilityCharges { get { return Active.AbilityCharges; } }
        public int HeroHealth { get { return Active.Health; } }
        public int EnemyHealth { get; private set; }
        public Phase Current { get; private set; }
        public bool Defended { get; private set; }
        public Battle() : this(HeroClass.Knight) { }
        public Battle(HeroClass kind) { unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset(); }
        public Battle(bool recruitmentCampaign) { campaign = recruitmentCampaign; unlocked.Add(HeroClass.Knight); recruits.Add(HeroDefinition.Common(HeroClass.Knight)); party.Add(new PartyHero(HeroClass.Knight)); Reset(); }
        public void SelectClass(HeroClass kind) {
            if (campaign) throw new InvalidOperationException("Campaign heroes are recruited, not replaced.");
            party.Clear(); recruits.Clear(); unlocked.Clear(); unlocked.Add(kind); recruits.Add(HeroDefinition.Common(kind)); party.Add(new PartyHero(kind)); Reset();
        }
        public bool CanChangeParty {
            get {
                if (Current != Phase.Player || EnemyHealth != Enemy.MaxHealth) return false;
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
            if (campaign && recruits.Count < HeroDefinition.Catalog.Count) {
                var recruit = HeroDefinition.Catalog[recruits.Count];
                recruits.Add(recruit);
                if (!unlocked.Contains(recruit.Class)) unlocked.Add(recruit.Class);
                if (party.Count < MaxPartySize) party.Add(new PartyHero(recruit));
            }
            if (campaign) encounter++;
            Reset(); return true;
        }
        public ProgressData ExportProgress() {
            var data = new ProgressData { encounter = encounter, pendingVictory = Current == Phase.Won, activeIndex = ActiveIndex,
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
            if (!campaign || data == null || data.version != 1 || data.encounter < 0 || data.encounter > ChapterDefinition.TotalStages || (data.pendingVictory && data.encounter == ChapterDefinition.TotalStages) ||
                data.recruited == null || data.recruited.Length < 1 || data.recruited.Length > HeroDefinition.Catalog.Count || data.recruited.Length > data.encounter + 1 ||
                data.party == null || data.party.Length < 1 || data.party.Length > MaxPartySize ||
                data.loadouts == null || data.loadouts.Length != data.recruited.Length ||
                data.activeIndex < 0 || data.activeIndex >= data.party.Length) return false;
            var restored = new Dictionary<string, PartyHero>();
            for (int i = 0; i < data.recruited.Length; i++) {
                if (data.recruited[i] != HeroDefinition.Catalog[i].Id || data.loadouts[i] == null || data.loadouts[i].id != data.recruited[i]) return false;
                var loadout = data.loadouts[i];
                if (loadout.firstSkill < 0 || loadout.firstSkill >= 6 || loadout.secondSkill < 0 || loadout.secondSkill >= 6 || loadout.firstSkill == loadout.secondSkill) return false;
                var member = new PartyHero(HeroDefinition.Catalog[i]);
                member.Equip(0, loadout.firstSkill); member.Equip(1, loadout.secondSkill);
                restored.Add(member.Identity.Id, member);
            }
            var chosen = new HashSet<string>();
            foreach (var id in data.party) if (id == null || !restored.ContainsKey(id) || !chosen.Add(id)) return false;
            recruits.Clear(); unlocked.Clear(); party.Clear(); savedHeroes.Clear();
            for (int i = 0; i < data.recruited.Length; i++) {
                var identity = HeroDefinition.Catalog[i]; recruits.Add(identity);
                if (!unlocked.Contains(identity.Class)) unlocked.Add(identity.Class);
                savedHeroes.Add(identity.Id, restored[identity.Id]);
            }
            foreach (var id in data.party) party.Add(restored[id]);
            encounter = data.encounter; Reset(); ActiveIndex = data.activeIndex;
            if (data.pendingVictory) { Current = Phase.Won; EnemyHealth = 0; }
            return true;
        }
        public void Reset() {
            foreach (var member in party) member.Restore();
            Enemy = campaign ? ChapterDefinition.EnemyAt(Math.Min(encounter, ChapterDefinition.TotalStages - 1)) : new EnemyDefinition("Training Warden", 100);
            ActiveIndex = 0; targetCursor = 0; EnemyHealth = Enemy.MaxHealth; LastDamage = 0; LastElementMultiplier = 1f; Current = CampaignComplete ? Phase.Complete : Phase.Player; Defended = false;
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
        void DealDamage(int amount, Element? skillElement = null) {
            LastElement = skillElement ?? Active.Identity.Affinity;
            LastElementMultiplier = amount > 0 ? Enemy.Multiplier(LastElement) : 1f;
            int damage = Enemy.Damage(amount, LastElement);
            LastDamage = Math.Min(EnemyHealth, damage);
            EnemyHealth = Math.Max(0, EnemyHealth - damage);
        }
        void CompleteAction() {
            Active.Acted = true; Defended = false;
            if (EnemyHealth == 0) { Current = Phase.Won; return; }
            for (int i = 0; i < party.Count; i++) {
                if (party[i].Health > 0 && !party[i].Acted) { ActiveIndex = i; return; }
            }
            // Cycle enemy targets across living allies each round; display the target before the strike.
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
            if (parry) DealDamage(10);
            if (EnemyHealth == 0) Current = Phase.Won;
            return true;
        }
        public void FinishStrike() {
            if (Current != Phase.EnemyStrike) return;
            if (!Defended) {
                Active.Health = Math.Max(0, Active.Health - Math.Max(0, 35 - Active.Guard));
                Active.Guard = 0;
            }
            Current = Phase.Lost;
            for (int i = 0; i < party.Count; i++) {
                party[i].Acted = false;
                if (party[i].Health > 0 && Current == Phase.Lost) { ActiveIndex = i; Current = Phase.Player; }
            }
        }
    }
}
