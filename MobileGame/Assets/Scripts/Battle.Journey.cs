using System;
using System.Collections.Generic;
using System.Text;
namespace Ashlight {
    public static class FormationPuzzles {
        public const int Count=3, ActionLimit=12;
        public static string Name(int index) { return new[]{"Silence the Choir","Protect the Lantern","Break the Glass"}[index]; }
        public static string Hint(int index) { return new[]{"A defender protects a healer. Put tanks in front; a Rogue can bypass the defender. Interrupt or focus the healer.","Rear hunters threaten your healers. Protect the back row with wards and cleanse, then focus the hunters.","The support renews shields. Equip a dispel, focus the support, and exploit Cold weakness."}[index]; }
        public static string[] Heroes() { return new[]{"Knight_Rare","Paladin_Rare","Rogue_Rare","Sorceress_Rare","Cleric_Rare"}; }
        public static int Encounter(int index) { return new[]{40,35,45}[index]; }
        public static IReadOnlyList<EnemyDefinition> Enemies(int index) {
            var roster=ChapterDefinition.Catalog[Encounter(index)/5].Enemies;
            return Array.AsReadOnly(new[]{new EnemyDefinition(roster[0].Name,650,roster[0].Weakness,roster[0].Resistance,role:EnemyRole.Defender),new EnemyDefinition(roster[index==1?3:2].Name,420,roster[2].Weakness,roster[2].Resistance,role:index==0?EnemyRole.Healer:index==1?EnemyRole.Archer:EnemyRole.Support),new EnemyDefinition(roster[1].Name,index==2?450:500,roster[1].Weakness,roster[1].Resistance,role:EnemyRole.Bruiser)});
        }
    }
    public sealed partial class Battle {
        int puzzleIndex=-1, puzzleClears;
        public bool IsFormationPuzzle { get { return puzzleIndex>=0; } }
        public int PuzzleIndex { get { return puzzleIndex; } }
        public bool PuzzleCleared(int index) { return index>=0 && index<FormationPuzzles.Count && (puzzleClears&(1<<index))!=0; }
        public bool CanStartPuzzle { get { return CanStartDailyBoss; } }
        public Battle StartFormationPuzzle(int index,string[] formation,int[,] skills) {
            if(!CanStartPuzzle || index<0 || index>=FormationPuzzles.Count || formation==null || formation.Length!=5 || skills==null || skills.GetLength(0)!=5 || skills.GetLength(1)!=2) return null;
            var available=new HashSet<string>(FormationPuzzles.Heroes());
            for(int i=0;i<5;i++) if(!available.Remove(formation[i]) || skills[i,0]<0 || skills[i,0]>=6 || skills[i,1]<0 || skills[i,1]>=6 || skills[i,0]==skills[i,1]) return null;
            var run=new Battle(true,enableClassPassives:true,enableFactions:true,criticalRandom:new Random(8300+index),utcNow:utcNow);
            run.encounter=10; run.bossOwner=this; run.bossEncounter=FormationPuzzles.Encounter(index); run.puzzleIndex=index;
            run.party.Clear(); run.recruits.Clear(); run.savedHeroes.Clear(); run.unlocked.Clear();
            for(int i=0;i<5;i++) {
                var identity=HeroDefinition.Find(formation[i]); var hero=new PartyHero(identity); hero.Level=10; hero.RefreshStats(); hero.Equip(0,skills[i,0]); hero.Equip(1,skills[i,1]);
                run.party.Add(hero); run.recruits.Add(identity); run.savedHeroes.Add(identity.Id,hero); if(!run.unlocked.Contains(identity.Class)) run.unlocked.Add(identity.Class);
            }
            run.Reset(); return run;
        }
        bool FinishFormationPuzzle(Battle run,out bool rewarded) {
            rewarded=false;
            if(run==null || !run.IsFormationPuzzle || run.bossOwner!=this || run.runClaimed || (run.Current!=Phase.Won && run.Current!=Phase.Lost)) return false;
            run.runClaimed=true;
            if(run.Current==Phase.Won && !PuzzleCleared(run.puzzleIndex)) { puzzleClears|=1<<run.puzzleIndex; Crystals=Math.Min(1000000,Crystals+50); ForgeMaterials=Math.Min(1000000,ForgeMaterials+50); rewarded=true; }
            return true;
        }
        public string CollectionGoals(int page) {
            var text=new StringBuilder();
            if(page==0) {
                text.AppendLine("MISSING HEROES · "+(HeroDefinition.Catalog.Count-recruits.Count)+" remaining");
                foreach(var hero in HeroDefinition.Catalog) if(HeroProgress(hero.Id)==null) text.AppendLine(hero.Name+" · "+hero.DisplayClass+" · "+hero.Quality+" · "+hero.FactionName);
                if(recruits.Count==HeroDefinition.Catalog.Count) text.AppendLine("Every named hero recruited.");
                text.AppendLine("Recruit through story and summons. Rare+ heroes can be added to the Wishlist; base rarity odds stay the same.");
            } else if(page==1) {
                text.AppendLine("BOND COMPANIONS · Your recruited heroes");
                foreach(var hero in recruits) { var partner=HeroBonds.Companion(hero); var companion=HeroProgress(partner.Id); var own=HeroProgress(hero.Id); text.AppendLine(hero.Name+" → "+partner.Name+" · "+(BondUnlocked(hero.Id,2)?"STORY READY":companion==null?"RECRUIT COMPANION":own.BondBattles<10?"Own trust "+own.BondBattles+"/10":"Companion trust "+companion.BondBattles+"/3")); }
            } else if(page==2) {
                text.AppendLine("GEAR SET COLLECTION · At least one owned type per slot");
                foreach(Element element in Enum.GetValues(typeof(Element))) { int slots=0; var missing=new List<string>(); foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot))) { bool owned=false; foreach(var item in GearDefinition.Catalog) if(item.Element==element && item.Slot==slot && GearCount(item.Id)>0) { owned=true; break; } if(owned) slots++; else missing.Add(slot.ToString()); } text.AppendLine(GearSets.Name(element)+" · "+slots+"/3 · "+(slots==3?"COLLECTED":"Missing "+string.Join(", ",missing.ToArray()))); }
                text.AppendLine("Collected pieces may be equipped on different heroes. Equipment shows each hero's active set; rarity and Forge level do not change the set.");
            } else throw new ArgumentOutOfRangeException("page");
            return text.ToString();
        }
    }
}
