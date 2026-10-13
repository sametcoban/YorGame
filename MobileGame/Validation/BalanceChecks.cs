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
   if(stage>=30 && level>=10 && quality==HeroQuality.Legendary && wins<Runs*.9) throw new Exception("Late stages block a developed team: "+(stage/5+1)+"-"+(stage%5+1)+" "+wins+"/"+Runs);
   if(stage==0 && wins<Runs*.95) throw new Exception("Opening stage too punishing");
   if(stage==29 && (level>=10 || quality==HeroQuality.Legendary) && wins<Runs*.9) throw new Exception("Upgraded teams hit a progression wall");
   if(stage==29 && level==1 && quality==HeroQuality.Common && wins>=Runs*.5) throw new Exception("Final boss does not reward progression");
   Console.WriteLine("{0}-{1}: {2}/{3} wins",stage/5+1,stage%5+1,wins,Runs);
  }
 }
 static void PrepareTeam(Battle b,bool recommendations=false) {
  b.FillAvailableSlots();
  foreach(var hero in b.Recruits) {
   while(b.UpgradeHero(hero.Id)) {}
   for(int tier=0;tier<3;tier++) b.ChooseTalent(hero.Id,tier,0);
  }
  var classes=new[]{HeroClass.Knight,HeroClass.Paladin,HeroClass.Sorceress,HeroClass.Ranger,HeroClass.Cleric};
  for(int slot=0;slot<b.Party.Count;slot++) {
   var candidates=b.Collection(classes[slot]);
   if(candidates.Count>0 && candidates[0].Power>b.Party[slot].Power) b.EquipHeroAt(candidates[0].Identity.Id,slot);
  }
  b.AutoArrangeFormation();
  foreach(var member in b.Party) b.EquipBestGear(member.Identity.Id);
  int goal=b.Encounter<15?3:b.Encounter<30?5:10;
  foreach(GearSlot slot in new[]{GearSlot.Weapon,GearSlot.Armor,GearSlot.Accessory}) foreach(var member in b.Party) { var gear=member.Gear(slot); if(gear!=null) while(b.EnhancementLevel(gear.Id)<goal && b.CanEnhanceGear(gear.Id)) b.EnhanceGear(gear.Id); }
  if(recommendations) { var team=b.RecommendTeam(); if(team==null || !b.ApplyRecommendation(team) || b.TeamPower!=team.Power) throw new Exception("Stage recommendation rejected or mismatched"); }
 }
 static void FullProgression(bool earnedSummons,bool wishlist=false,bool recommendations=false) {
  int complete=0, retries=0, waiting=0, summons=0; int[] failures=new int[ChapterDefinition.TotalStages];
  for(int seed=0;seed<Runs;seed++) {
   var rng=new Random(40000+seed); var now=new DateTime(2026,10,12,0,0,0,DateTimeKind.Utc);
   var b=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng,utcNow:()=>now);
   if(wishlist) foreach(var id in new[]{"Knight_Rare","Sorceress_Legendary","Paladin_Epic","Cleric_Legendary","Ranger_Epic","Knight_Legendary"}) if(!b.ToggleWishlist(id)) throw new Exception("Wishlist fixture rejected");
   while(!b.CampaignComplete) {
    // At most three earned pulls between wins; no real money or injected resources.
    for(int pull=0;earnedSummons && pull<3 && b.CanSummon;pull++) { if(b.Summon(rng.NextDouble(),rng.NextDouble())==null) throw new Exception("Earned summon rejected"); summons++; }
    PrepareTeam(b,recommendations); bool won=false;
    for(int attempt=0;attempt<4;attempt++) {
     if(Fight(b,rng)) { won=true; break; }
     retries++; b.Reset(); now=now.AddMinutes(30); waiting+=30;
     if(!b.ClaimIdleRewards()) throw new Exception("Retry idle claim rejected");
     PrepareTeam(b,recommendations);
    }
    if(!won) { failures[b.Encounter]++; break; }
    if(!b.ContinueAfterVictory(rng.NextDouble(),rng.Next(18))) throw new Exception("Progression claim rejected");
    // Prove actual earned/save state survives normal reloads after each completed chapter.
    if(b.Encounter%5==0) {
     if(!b.ClaimMilestone(b.Encounter/5-1)) throw new Exception("Earned chapter milestone rejected");
     var restored=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng,utcNow:()=>now);
     if(!restored.RestoreProgress(b.ExportProgress()) || (wishlist && restored.Wishlist.Length!=6)) throw new Exception("Progression save rejected"); b=restored;
    }
   }
   if(b.CampaignComplete) complete++;
  }
  Console.WriteLine("FULL PROGRESSION (summons {5}, wishlist {6}, recommendations {7}): {0}/{1} completed, {2} retries, {3:F1} mean idle hours, {4:F1} mean earned pulls",complete,Runs,retries,waiting/(60.0*Runs),summons/(double)Runs,earnedSummons,wishlist,recommendations);
  for(int stage=0;stage<failures.Length;stage++) if(failures[stage]>0) Console.WriteLine("Progression stopped at {0}-{1}: {2} runs",stage/5+1,stage%5+1,failures[stage]);
  if(complete<Runs*.95) throw new Exception("Developing teams cannot reliably complete eighteen chapters");
 }
 static void Economy() {
  int storyCrystals=0;
  var now=new DateTime(2026,10,12,0,0,0,DateTimeKind.Utc);
  var b=new Battle(true,utcNow:()=>now);
  for(int stage=0;stage<60;stage++) {
   var save=b.ExportProgress(); save.encounter=stage;
   if(!b.RestoreProgress(save)) throw new Exception("Economy stage restore rejected");
   storyCrystals+=b.CampaignVictoryCrystals;
  }
  if(storyCrystals!=4425) throw new Exception("Campaign reward budget changed unexpectedly");
  foreach(int stage in new[]{0,30,60}) {
   var rewards=new Battle(true,utcNow:()=>now); var save=rewards.ExportProgress(); save.encounter=stage;
   if(!rewards.RestoreProgress(save)) throw new Exception("Idle budget fixture rejected");
   now=now.AddHours(12); int amount=rewards.IdleCrystals, xp=rewards.IdleExperience;
   if(amount!=72*(1+stage/5) || xp!=720*(2+stage/5) || !rewards.ClaimIdleRewards() || rewards.Crystals!=300+amount || rewards.ClaimIdleRewards()) throw new Exception("Idle budget or single-claim semantics regressed");
   Console.WriteLine("ECONOMY stage {0}: capped idle {1} crystals ({2:F2} base-cost pulls), {3} XP per hero",stage,amount,amount/100.0,xp);
  }
  int milestones=0; for(int chapter=0;chapter<12;chapter++) milestones+=ChapterMilestone.For(chapter).Crystals;
  if(milestones!=2850) throw new Exception("Milestone reward budget changed unexpectedly");
  int expandedCrystals=0; for(int stage=0;stage<ChapterDefinition.TotalStages;stage++) { var save=b.ExportProgress(); save.encounter=stage; if(!b.RestoreProgress(save)) throw new Exception("Expanded economy fixture rejected"); expandedCrystals+=b.CampaignVictoryCrystals; }
  int expandedMilestones=0; for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) expandedMilestones+=ChapterMilestone.For(chapter).Crystals;
  if(expandedCrystals!=8250 || expandedMilestones!=5625) throw new Exception("Expanded reward budget mismatch");
  Console.WriteLine("EXPANDED ECONOMY: {0} story crystals + {1} milestones; three optional puzzle first clears add 150 crystals/materials total",expandedCrystals,expandedMilestones);
  Console.WriteLine("ECONOMY: {0} story crystals + {1} milestones + {2} starter; duplicate refunds, dailies and Tower excluded",storyCrystals,milestones,Summoning.StarterCrystals);
 }
 static void Main(string[] args) {
  if(args.Length>0 && args[0]=="late") { Stages(20,HeroQuality.Legendary,60,90); Console.WriteLine("PASS: 6,000 expanded-stage samples"); return; }
  if(args.Length>0 && args[0]=="economy") { Economy(); return; }
  if(args.Length>0 && args[0]=="recommendations") { FullProgression(true,true,true); Console.WriteLine("PASS: 200 recommendation progression campaigns"); return; }
  Economy(); FullProgression(false); FullProgression(true); FullProgression(true,true); FullProgression(true,true,true); Campaign(false); Campaign(true); Stages(1,HeroQuality.Common); Stages(10,HeroQuality.Common); Stages(1,HeroQuality.Legendary); Stages(20,HeroQuality.Common,30,60); Stages(10,HeroQuality.Legendary,30,60); Stages(20,HeroQuality.Legendary,60,90); Console.WriteLine("PASS: 36,000 stage samples, 400 first-arc campaigns, and 800 full progression campaigns");
 }
}
