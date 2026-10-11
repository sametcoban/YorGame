using System;
using Ashlight;

// Headless Auto Play uses the same planner and class defense rolls as the Unity UI.
// No perfect defenses, paid pulls, idle XP, talents, or equipment are assumed.
class BalanceChecks {
 const int Runs=200;
 static bool Fight(Battle b,Random rng) {
  for(int actions=0;actions<1000;actions++) {
   if(b.Current==Phase.Won) return true;
   if(b.Current==Phase.Lost) return false;
   if(b.Current==Phase.WaveCleared) { if(!b.AdvanceWave()) throw new Exception("Wave stalled"); continue; }
   if(b.Current==Phase.Player) {
    b.SelectEnemy(AutoBattlePlanner.ChooseTarget(b));
    int skill=AutoBattlePlanner.ChooseSkill(b);
    if(!(skill<0?b.Attack():b.UseAbility(skill))) throw new Exception("Auto action rejected");
   } else if(b.Current==Phase.EnemyWindup) {
    b.BeginStrike();
    if(b.IncomingSkill.Action==EnemyAction.Attack) {
     int target=b.ActiveIndex;
     for(int i=0;i<b.Party.Count;i++) {
      if(b.Party[i].Health<=0 || (!b.IncomingSkill.PartyWide && i!=target)) continue;
      var defense=AutoBattlePlanner.RollDefense(b.Party[i].Identity.Class,rng.NextDouble());
      if(defense!=DefenseOutcome.Miss) b.DefendHero(i,defense==DefenseOutcome.Parry,defense==DefenseOutcome.Parry?.12f:.26f);
     }
    }
    b.FinishStrike();
   } else throw new Exception("Unexpected phase: "+b.Current);
  }
  throw new Exception("Battle exceeded 1000 actions");
 }
 static void Campaign(bool healer) {
  int[] reached=new int[30],won=new int[30];
  for(int seed=0;seed<Runs;seed++) {
   var rng=new Random(seed);
   var b=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng);
   while(b.Encounter<30) {
    if(healer && b.Party.Count==5 && b.HeroProgress("Cleric_Common")!=null) b.EquipHeroAt("Cleric_Common",4);
    int stage=b.Encounter; reached[stage]++;
    if(!Fight(b,rng)) break;
    won[stage]++;
    if(!b.ContinueAfterVictory(.5,0)) throw new Exception("Claim rejected");
   }
  }
  if(won[0]<Runs*.95 || won[29]<Runs*.6) throw new Exception("Fresh campaign accessibility regressed");
  Console.WriteLine(healer?"CAMPAIGN WITH CLERIC":"CAMPAIGN DEFAULT TEAM");
  for(int i=0;i<30;i++) if(reached[i]>0) Console.WriteLine("{0}-{1}: reached {2}, won {3} ({4:F1}%)",i/5+1,i%5+1,reached[i],won[i],100.0*won[i]/reached[i]);
 }
 static void Stages(int level,HeroQuality quality,int first=0,int last=30) {
  Console.WriteLine("FIXED ROSTER: "+quality+" own level "+level+", no gear/talents");
  for(int stage=first;stage<last;stage++) {
   int wins=0;
   for(int seed=0;seed<Runs;seed++) {
    var rng=new Random(seed);
    var b=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng);
    var save=b.ExportProgress(); save.encounter=stage;
    var ids=new System.Collections.Generic.List<string>(); var loads=new System.Collections.Generic.List<HeroLoadoutData>();
    ids.Add("Knight_Common"); loads.Add(new HeroLoadoutData{id="Knight_Common",level=level,secondSkill=1});
    var team=new System.Collections.Generic.List<string>();
    foreach(var kind in new[]{HeroClass.Knight,HeroClass.Paladin,HeroClass.Sorceress,HeroClass.Ranger,HeroClass.Cleric}) {
     string id=kind+"_"+quality;
     if(!ids.Contains(id)) { ids.Add(id); loads.Add(new HeroLoadoutData{id=id,level=level,secondSkill=1}); }
     if(team.Count<Math.Min(5,stage/5+1)) team.Add(id);
    }
    save.recruited=ids.ToArray(); save.party=team.ToArray(); save.loadouts=loads.ToArray();
    if(!b.RestoreProgress(save)) throw new Exception("Roster restore rejected");
    if(Fight(b,rng)) wins++;
   }
   if(stage>=30 && level>=10 && quality==HeroQuality.Legendary && wins<Runs*.9) throw new Exception("Late stages block a developed team");
   if(stage==0 && wins<Runs*.95) throw new Exception("Opening stage too punishing");
   if(stage==29 && (level>=10 || quality==HeroQuality.Legendary) && wins<Runs*.9) throw new Exception("Upgraded teams hit a progression wall");
   if(stage==29 && level==1 && quality==HeroQuality.Common && wins>=Runs*.5) throw new Exception("Final boss does not reward progression");
   Console.WriteLine("{0}-{1}: {2}/{3} wins",stage/5+1,stage%5+1,wins,Runs);
  }
 }
 static void Main() { Campaign(false); Campaign(true); Stages(1,HeroQuality.Common); Stages(10,HeroQuality.Common); Stages(1,HeroQuality.Legendary); Stages(20,HeroQuality.Common,30,60); Stages(10,HeroQuality.Legendary,30,60); Console.WriteLine("PASS: 30,000 stage samples and 400 first-arc campaign runs"); }
}
