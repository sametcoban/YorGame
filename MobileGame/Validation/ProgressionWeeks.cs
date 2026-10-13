using System;
using Ashlight;
class ProgressionWeeks {
 static bool Fight(Battle b,Random rng) {
  for(int action=0;action<1000;action++) {
   if(b.Current==Phase.Won) return true; if(b.Current==Phase.Lost) return false;
   if(b.Current==Phase.WaveCleared) { b.AdvanceWave(); continue; }
   if(b.Current==Phase.Player) { b.SelectEnemy(AutoBattlePlanner.ChooseTarget(b)); int skill=AutoBattlePlanner.ChooseSkill(b); if(!(skill<0?b.Attack():b.UseAbility(skill))) throw new Exception("Action rejected"); }
   else if(b.Current==Phase.EnemyWindup) {
    b.BeginStrike(); if(b.IncomingSkill.Action==EnemyAction.Attack) for(int i=0;i<b.Party.Count;i++) if(b.Party[i].Health>0 && (b.IncomingSkill.PartyWide || i==b.ActiveIndex)) {
     var defense=AutoBattlePlanner.RollDefense(b.Party[i].Identity.Class,rng.NextDouble()); if(defense!=DefenseOutcome.Miss) b.DefendHero(i,defense==DefenseOutcome.Parry,defense==DefenseOutcome.Parry?.12f:.26f);
    }
    b.FinishStrike();
   } else throw new Exception("Unexpected phase "+b.Current);
  }
  throw new Exception("Battle action limit exceeded");
 }
 static void Prepare(Battle b,int day) {
  b.FillAvailableSlots(); foreach(var hero in b.Recruits) { while(b.UpgradeHero(hero.Id)) {} for(int tier=0;tier<3;tier++) b.ChooseTalent(hero.Id,tier,0); }
  if(!b.CampaignComplete) { var team=b.RecommendTeam(); if(team!=null) b.ApplyRecommendation(team); }
  foreach(var hero in b.Party) b.EquipBestGear(hero.Identity.Id);
  int goal=day<7?3:day<14?5:10;
  foreach(var hero in b.Party) foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot))) { var gear=hero.Gear(slot); if(gear!=null) while(b.EnhancementLevel(gear.Id)<goal && b.CanEnhanceGear(gear.Id)) b.EnhanceGear(gear.Id); }
 }
 static void Main() {
  const int seeds=24;
  foreach(int sessions in new[]{1,2}) {
   var stageTotals=new int[28]; var heroesTotals=new int[28]; var levelTotals=new int[28]; var powerTotals=new long[28]; var crystalTotals=new long[28];
   int completed=0,allDailyDays=0,pulls=0,levelCapDay=0,retries=0;
   for(int seed=0;seed<seeds;seed++) {
    var now=new DateTime(2026,10,13,0,0,0,DateTimeKind.Utc); var rng=new Random(71000+seed);
    var b=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng,utcNow:()=>now); bool capped=false;
    for(int day=1;day<=28;day++) {
     for(int login=0;login<sessions;login++) {
      if(day>1 || login>0) now=now.AddHours(24.0/sessions);
      b.ClaimIdleRewards(); Prepare(b,day);
      for(int stage=0;stage<3 && !b.CampaignComplete;stage++) {
       if(!Fight(b,rng)) { retries++; b.Reset(); break; }
       if(!b.ContinueAfterVictory(rng.NextDouble(),rng.Next(18))) throw new Exception("Reward rejected");
       for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) if(b.CanClaimMilestone(chapter)) b.ClaimMilestone(chapter);
       Prepare(b,day);
      }
      if(!b.CanChangeParty && !b.CampaignComplete) b.Reset();
      int dailyAttempts=b.CampaignComplete?3:1;
      for(int attempt=0;attempt<dailyAttempts && b.CanStartDailyBoss;attempt++) { var run=b.StartDailyBoss(); Fight(run,rng); bool reward; if(!b.FinishDailyBoss(run,out reward)) throw new Exception("Daily result rejected"); }
      int summonLimit=100; while(b.CanSummon && summonLimit-->0) { if(b.Summon(rng.NextDouble(),rng.NextDouble())==null) throw new Exception("Summon rejected"); pulls++; }
      for(int quest=0;quest<4;quest++) if(b.CanClaimDaily(quest)) b.ClaimDaily(quest);
      Prepare(b,day);
      var reload=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:rng,utcNow:()=>now);
      if(!reload.RestoreProgress(b.ExportProgress())) throw new Exception("Daily reload rejected"); b=reload;
     }
     if(b.DailyClaimed(3)) allDailyDays++;
     if(!capped && b.HeroProgress("Knight_Common").Level==20) { capped=true; levelCapDay+=day; }
     stageTotals[day-1]+=b.Encounter; heroesTotals[day-1]+=b.Recruits.Count; levelTotals[day-1]+=b.HeroProgress("Knight_Common").Level; powerTotals[day-1]+=b.TeamPower; crystalTotals[day-1]+=b.Crystals;
    }
    if(b.CampaignComplete) completed++;
   }
   foreach(int day in new[]{1,7,14,28}) Console.WriteLine("{0} logins/day · day {1}: mean stage {2:F1}, owned {3:F1}, Rowan level {4:F1}, team power {5:F0}, crystals {6:F0}",sessions,day,stageTotals[day-1]/(double)seeds,heroesTotals[day-1]/(double)seeds,levelTotals[day-1]/(double)seeds,powerTotals[day-1]/(double)seeds,crystalTotals[day-1]/(double)seeds);
   Console.WriteLine("SUMMARY {0} logins/day: {1}/{2} campaigns completed, {3:F1} pulls, {4:F1} all-daily days, Rowan cap mean day {5:F1}, {6} losses",sessions,completed,seeds,pulls/(double)seeds,allDailyDays/(double)seeds,levelCapDay/(double)seeds,retries);
   if(completed!=(sessions==1?0:seeds) || allDailyDays!=seeds*(sessions==1?27:28)) throw new Exception("Campaign completion or sustainable daily quests regressed");
  }
  Console.WriteLine("PASS: 48 twenty-eight-day profiles with earned rewards and save reloads");
 }
}
