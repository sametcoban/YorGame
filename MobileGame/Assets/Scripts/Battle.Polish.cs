using System;
using System.Collections.Generic;
namespace Ashlight {
    public sealed partial class Battle {
        public static string PresetMode(int mode) { return new[]{"Campaign","Tower","Challenges"}[mode]; }
        public string PresetName(int index) { return index<0 || index>=9?"":string.IsNullOrEmpty(presetNames[index])?PresetMode(index/3)+" "+(index%3+1):presetNames[index]; }
        public bool RenamePreset(int index,string name) {
            if(!campaign || IsDailyBoss || index<0 || index>=9 || (!CanChangeParty && !CampaignComplete) || !ValidPresetName(name)) return false;
            presetNames[index]=name.Trim(); return true;
        }
        static bool ValidPresetName(string name) { if(name==null || name.Length>24) return false; foreach(char c in name) if(char.IsControl(c) || c=='<' || c=='>') return false; return true; }
        TeamPresetData[] ExportModePresets() { var result=new TeamPresetData[9]; for(int i=0;i<9;i++) result[i]=new TeamPresetData{name=presetNames[i]??"",heroes=TeamPreset(i)}; return result; }
        static bool ValidateModePresets(ProgressData data,Dictionary<string,PartyHero> heroes) {
            if(data.modePresets==null || data.modePresets.Length!=9) return false;
            foreach(var entry in data.modePresets) {
                if(entry==null || !ValidPresetName(entry.name) || entry.heroes==null || entry.heroes.Length>MaxPartySize) return false;
                var unique=new HashSet<string>(); foreach(var id in entry.heroes) if(id==null || !heroes.ContainsKey(id) || !unique.Add(id)) return false;
            }
            // Keep the compatibility bank identical to the campaign bank.
            if(data.presets==null || data.presets.Length!=3) return false;
            for(int i=0;i<3;i++) { if(data.presets[i]==null || data.presets[i].heroes==null || data.presets[i].heroes.Length!=data.modePresets[i].heroes.Length) return false; for(int j=0;j<data.presets[i].heroes.Length;j++) if(data.presets[i].heroes[j]!=data.modePresets[i].heroes[j]) return false; }
            return true;
        }
        public static string FactionIdentity(HeroFaction faction) {
            switch(faction) { case HeroFaction.DawnOrder:return "Dawn Order · +6 personal ward after first action each wave."; case HeroFaction.Wilderkin:return "Wilderkin · Recover up to 4 HP after first action each wave."; default:return "Veilborn · +4 energy after first action each wave."; }
        }
        void ApplyFactionIdentity() {
            if(!FactionsEnabled || IsFormationPuzzle || Active.FactionActionUsed) return;
            Active.FactionActionUsed=true;
            switch(Active.Identity.Faction) {
                case HeroFaction.DawnOrder: Active.Guard+=6; LastSkillGuard[ActiveIndex]+=6; break;
                case HeroFaction.Wilderkin: int healed=Math.Min(4,Active.Definition.MaxHealth-Active.Health); Active.Health+=healed; Active.EncounterHealing=Math.Min(1000000,Active.EncounterHealing+healed); break;
                default: Active.Energy=Math.Min(100,Active.Energy+4); break;
            }
        }
        void RecordActionSupport() {
            int ward=0,cleanse=0; for(int i=0;i<party.Count;i++) { ward+=LastSkillGuard[i]+LastUltimateGuard[i]; if(LastSkillCleanse[i]) cleanse++; }
            Active.WardGranted=Math.Min(1000000,Active.WardGranted+ward); Active.Cleanses=Math.Min(1000000,Active.Cleanses+cleanse);
        }
        int[] ExportSupport() { var result=new int[party.Count*5]; for(int i=0;i<party.Count;i++) { var hero=party[i]; result[i*5]=hero.DamageTaken; result[i*5+1]=hero.WardGranted; result[i*5+2]=hero.WardAbsorbed; result[i*5+3]=hero.Cleanses; result[i*5+4]=hero.Interrupts; } return result; }
        static bool ValidSupport(PendingResultsData result,int count) { if(result==null || result.support==null || result.support.Length!=count*5) return false; foreach(int value in result.support) if(value<0 || value>1000000) return false; return true; }
        void RestoreSupport(int[] values) { for(int i=0;i<party.Count;i++) { var hero=party[i]; hero.DamageTaken=values[i*5]; hero.WardGranted=values[i*5+1]; hero.WardAbsorbed=values[i*5+2]; hero.Cleanses=values[i*5+3]; hero.Interrupts=values[i*5+4]; } }
        public static string SupportReport(PartyHero hero) { return "Taken "+hero.DamageTaken+" · Ward given "+hero.WardGranted+" / absorbed "+hero.WardAbsorbed+" · Cleanses "+hero.Cleanses+" · Interrupts "+hero.Interrupts; }
        public string UpgradeBenefits(string id) {
            var hero=HeroProgress(id); if(hero==null) return "";
            var next=PreviewNextLevel(id); var ascended=PreviewAscension(id);
            string result="EXPECTED BENEFITS · Current equipped build";
            if(next!=null) result+="\nNext own level: "+Benefit(hero,next)+" · "+(hero.NextLevelXP-hero.Experience)+" XP remaining";
            if(hero.Level==20 && encounter>=60 && hero.MasteryRank<5) { var mastered=PreviewCopy(hero,hero.FactionBonus); mastered.MasteryRank++; mastered.RefreshStats(); result+="\nMastery: "+Benefit(hero,mastered)+" · "+MasteryCost(id)+" materials"; }
            if(ascended!=null) result+="\nAscend: "+Benefit(hero,ascended)+" · "+hero.UpgradeCost+" shards";
            for(int slot=0;slot<3;slot++) { var item=hero.EquipmentStats((GearSlot)slot); if(item==null || item.Enhancement>=10) continue; var preview=PreviewEnhancement(id,(GearSlot)slot); if(preview!=null) result+="\nForge "+item.Name+": "+Benefit(hero,preview)+" · "+item.EnhanceCost+" materials"; }
            return result+"\nChoose free talents and fill empty gear slots before spending scarce materials.";
        }
        static string Benefit(PartyHero before,PartyHero after) { return "Power "+Signed(after.Power-before.Power)+", HP "+Signed(after.Definition.MaxHealth-before.Definition.MaxHealth)+", attack "+Signed(after.Definition.AttackDamage-before.Definition.AttackDamage); }
        static string Signed(int value) { return value>=0?"+"+value:value.ToString(); }
        public string CompareHero(string id) {
            var hero=HeroProgress(id); if(hero==null) return "Select an owned hero.";
            string text=hero.Identity.Name+" · "+hero.Identity.Class+"\n"+hero.RarityLabel+" · Lv "+hero.CombatLevel+" · Power "+hero.Power+"\nHP "+hero.Definition.MaxHealth+" · Attack "+hero.Definition.AttackDamage+" · Armor "+hero.Protection+"\n"+hero.Identity.Affinity+" · "+hero.Identity.FactionName+"\n"+FactionIdentity(hero.Identity.Faction)+"\n\nSKILLS\n";
            for(int i=0;i<2;i++) { var skill=hero.Skill(i); text+=skill.Name+" · "+(skill.Affinity??hero.Identity.Affinity)+"\nDamage "+skill.Damage+" · Heal "+skill.Healing+" · Ward "+skill.Guard+"\n"; }
            text+="\nULTIMATE · "+hero.Ultimate.Name+"\n"+(hero.Ultimate.AreaDamage?"Area":"Single target")+" · Damage "+hero.Ultimate.Damage+" · Heal "+hero.Ultimate.Healing+" · Ward "+hero.Ultimate.Guard+"\n\n"+ClassDefinition.PassiveDescription(hero.Identity.Class)+"\n"+HeroSignature.For(hero.Identity).Description+"\n\nGEAR";
            for(int slot=0;slot<3;slot++) { var item=hero.EquipmentStats((GearSlot)slot); text+="\n"+(GearSlot)slot+": "+(item==null?"Empty":item.Name+" +"+item.Enhancement+" / R"+item.Refinement); }
            return text+"\nStats include current shared levels and formation bonuses.";
        }
        bool training; int trainingRule=-1,trainingPartyLimit;
        public bool IsTraining { get { return training; } }
        public int TrainingRule { get { return trainingRule; } }
        public int RotatingPracticeRule { get { return (int)(utcNow().Date.Ticks/TimeSpan.TicksPerDay%6); } }
        public static string PracticeName(int rule) { return rule<3?WeeklyName(rule):new[]{"Restless Grave","Chill Vigil","Iron Oath"}[rule-3]; }
        public static string PracticeRules(int rule) { return rule<3?WeeklyRules(rule):new[]{"Boss recovers up to 5% max HP after an attack that deals HP damage. Wards, dodge and interrupts prevent this recovery.","Unresisted connected attacks inflict Chill. Bring cleanse or protect vulnerable damage dealers.","Physical damage is reduced by 25%. Elemental skills and heroes keep their damage."}[rule-3]; }
        void ApplyPracticeRecovery(BattleEnemy attacker) {
            if(!IsTraining || trainingRule!=3 || attacker.Health<=0 || IncomingSkill.Action!=EnemyAction.Attack) return;
            bool connected=false; foreach(int damage in LastIncomingDamage) connected|=damage>0; if(!connected) return;
            int before=attacker.Health; attacker.Health=Math.Min(attacker.Definition.MaxHealth,attacker.Health+(attacker.Definition.MaxHealth+19)/20);
            LastEnemyHealing=attacker.Health-before; LastMechanicTarget=AttackingEnemyIndex;
        }
        public bool CanTrain { get { return campaign && !IsDailyBoss && !InTower && (CanChangeParty || CampaignComplete); } }
        public Battle StartTraining(int chapter,bool rotating) {
            if(!CanTrain || chapter<0 || chapter>=ChapterDefinition.Catalog.Count || chapter>Math.Min(ChapterIndex,ChapterDefinition.Catalog.Count-1)) return null;
            var run=new Battle(true,enableClassPassives:ClassPassivesEnabled,enableFactions:FactionsEnabled,utcNow:utcNow);
            if(!run.RestoreProgress(ExportProgress())) return null;
            run.bossOwner=this; run.training=true; run.trainingPartyLimit=PartyLimit; run.trainingRule=rotating?RotatingPracticeRule:-1;
            run.bossEncounter=chapter*5+4; run.bossHealth=Math.Min(1000000,Math.Max(500,TeamPower*2));
            if(rotating && run.trainingRule<3) { run.weeklyTrial=true; run.trialIndex=run.trainingRule; }
            run.Reset(); return run;
        }
    }
}
