using System;
using Ashlight;
class BattleChecks {
 static int count;
 static void Check(bool ok) { count++; if(!ok) throw new Exception("Failed check " + count); }
 static Battle BossBattle(int encounter) {
  var battle=new Battle(true); var save=battle.ExportProgress(); save.encounter=encounter;
  Check(battle.RestoreProgress(save)); return battle;
 }
 static void ResolveEnemies(Battle battle) {
  while(battle.Current==Phase.EnemyWindup) {
   Check(battle.BeginStrike());
   if(battle.IncomingSkill.Action==EnemyAction.Attack) Check(battle.Defend(false,.1f));
   battle.FinishStrike();
  }
 }
 sealed class FixedRoll : Random {
  readonly double value; public FixedRoll(double roll) { value=roll; }
  public override double NextDouble() { return value; }
 }
 static Battle PassiveBattle(int encounter,string[] heroes,double roll=.99,bool waves=false) {
  var battle=new Battle(true,enableWaves:waves,enableClassPassives:true,criticalRandom:new FixedRoll(roll));
  var save=battle.ExportProgress(); save.encounter=encounter;
  var recruits=new System.Collections.Generic.List<string>(); recruits.Add("Knight_Common");
  foreach(var hero in heroes) if(!recruits.Contains(hero)) recruits.Add(hero);
  save.recruited=recruits.ToArray(); save.party=heroes; save.loadouts=new HeroLoadoutData[save.recruited.Length];
  for(int i=0;i<save.loadouts.Length;i++) save.loadouts[i]=new HeroLoadoutData{id=save.recruited[i],firstSkill=0,secondSkill=1};
  Check(battle.RestoreProgress(save)); return battle;
 }
 static Battle TalentBattle(string[] heroes,int level=15,int encounter=24,double roll=.99) {
  var battle=PassiveBattle(encounter,heroes,roll); var save=battle.ExportProgress();
  foreach(var loadout in save.loadouts) { loadout.level=level; loadout.experience=0; }
  Check(battle.RestoreProgress(save)); return battle;
 }
 static Battle FactionBattle(string[] heroes,int encounter=20) {
  var battle=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,criticalRandom:new FixedRoll(.99));
  var save=battle.ExportProgress(); save.encounter=encounter; save.party=heroes;
  var recruited=new System.Collections.Generic.List<string>(); var loadouts=new System.Collections.Generic.List<HeroLoadoutData>();
  foreach(var hero in HeroDefinition.Catalog) { recruited.Add(hero.Id); loadouts.Add(new HeroLoadoutData{id=hero.Id,secondSkill=1}); }
  save.recruited=recruited.ToArray(); save.loadouts=loadouts.ToArray(); Check(battle.RestoreProgress(save)); return battle;
 }
 static void StrategyAndForgeChecks() {
  var scheduled=new System.Collections.Generic.HashSet<string>();
  foreach(var arrival in StoryRecruit.Catalog) { Check(arrival.Hero.Id==arrival.HeroId && scheduled.Add(arrival.HeroId) && arrival.Introduction.Length>20); Check(StoryRecruit.At(arrival.Encounter)==arrival); }
  Check(StoryRecruit.At(0)==null && scheduled.Count==9);
  var recruitTest=BossBattle(4); recruitTest.Enemies[0].Health=1; Check(recruitTest.Attack());
  Check(recruitTest.Results.RecruitName=="Lucan" && !recruitTest.Results.RecruitDuplicate && recruitTest.Results.Materials==20);
  Check(recruitTest.ContinueAfterVictory(.5,0) && recruitTest.HeroProgress("Paladin_Common")!=null && recruitTest.Party.Count==2 && recruitTest.ForgeMaterials==20);
  Check(!recruitTest.ContinueAfterVictory() && recruitTest.ForgeMaterials==20);
  var duplicateRecruit=BossBattle(4); var duplicateSave=duplicateRecruit.ExportProgress();
  duplicateSave.recruited=new[]{"Knight_Common","Paladin_Common"}; duplicateSave.loadouts=new[]{duplicateSave.loadouts[0],new HeroLoadoutData{id="Paladin_Common",secondSkill=1,shards=7}};
  Check(duplicateRecruit.RestoreProgress(duplicateSave)); duplicateRecruit.Enemies[0].Health=1; Check(duplicateRecruit.Attack());
  Check(duplicateRecruit.Results.RecruitDuplicate && duplicateRecruit.Results.RecruitShards==20 && duplicateRecruit.ContinueAfterVictory(.5,0));
  Check(duplicateRecruit.Recruits.Count==2 && duplicateRecruit.HeroProgress("Paladin_Common").Shards==27);
  string[] team={"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"};
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass))) foreach(var skill in SkillDefinition.For(kind)) {
   Check(skill.Scaled(1.5f).Utility==skill.Utility && !string.IsNullOrEmpty(skill.Description));
   var classStats=ClassDefinition.For(kind).WithTalentBonuses(15,10).WithEquipment(20,6,3).WithFactionBonus(10);
   foreach(var enhancedSkill in classStats.Skills) if(enhancedSkill.Name==skill.Name) Check(enhancedSkill.Utility==skill.Utility);
  }
  var cleanse=FactionBattle(team,20); Check(cleanse.SelectHero(4) && cleanse.EquipSkill(0,4));
  for(int i=0;i<cleanse.Party.Count;i++) { cleanse.Party[i].Status=i%2==0?Debuff.Poison:Debuff.Chill; cleanse.Party[i].StatusRounds=2; cleanse.Party[i].Health-=5; }
  cleanse.Party[2].Health=0; Check(cleanse.UseAbility());
  for(int i=0;i<cleanse.Party.Count;i++) Check(i==2?cleanse.Party[i].Health==0 && !cleanse.LastSkillCleanse[i]:cleanse.Party[i].Status==Debuff.None && cleanse.Party[i].StatusRounds==0 && cleanse.LastSkillCleanse[i]);
  var ward=FactionBattle(team,20); Check(ward.SelectHero(4) && ward.EquipSkill(0,2)); ward.Party[0].Guard=50; ward.Party[2].Health=0;
  Check(ward.UseAbility() && ward.Party[0].Guard==50 && ward.LastSkillGuard[0]==0 && ward.Party[2].Guard==0);
  Check(ward.Party[1].Guard==35 && ward.Party[3].Guard==35 && ward.LastSkillGuard[3]==35);
  var expose=FactionBattle(team,20); int normal=expose.Party[0].Definition.AttackDamage; Check(expose.SelectHero(2) && expose.UseAbility());
  Check(expose.Enemies[0].ExposedRounds==1 && expose.SelectHero(0) && expose.Attack() && expose.LastDamage==(int)Math.Ceiling(normal*1.2));
  var dispel=FactionBattle(team,20); dispel.Enemies[0].Shield=100; Check(dispel.SelectHero(1) && dispel.EquipSkill(0,4) && dispel.UseAbility());
  Check(dispel.Enemies[0].Shield==0 && dispel.LastDamage>0 && dispel.LastUtilityText.Contains("WARD BROKEN"));
  var interrupt=PassiveBattle(29,team); Check(interrupt.SelectHero(3) && interrupt.UseAbility());
  Check(interrupt.Enemies[0].InterruptQueued);
  while(interrupt.Current==Phase.Player) Check(interrupt.Attack());
  Check(interrupt.IncomingSkill.Action==EnemyAction.Interrupted && interrupt.BeginStrike()); interrupt.FinishStrike();
  foreach(int damage in interrupt.LastIncomingDamage) Check(damage==0);
  ResolveEnemies(interrupt); Check(!interrupt.CanInterrupt(0) && interrupt.Enemies[0].InterruptRecovery==1);
  while(interrupt.Current==Phase.Player) Check(interrupt.Attack()); ResolveEnemies(interrupt); Check(interrupt.Enemies[0].InterruptRecovery==0);
  var charged=PassiveBattle(14,new[]{"Knight_Common","Paladin_Common","Ranger_Common"}); charged.Enemies[0].Charged=true; charged.Enemies[0].ChargeDamage=0;
  Check(charged.SelectHero(2) && charged.UseAbility() && !charged.Enemies[0].Charged && charged.Enemies[0].ChargeDamage>=60);
  var taunt=PassiveBattle(35,new[]{"Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common","Knight_Common"},waves:true);
  Check(taunt.SelectHero(4) && taunt.EquipSkill(1,2) && taunt.EquipSkill(0,1) && taunt.UseAbility());
  while(taunt.Current==Phase.Player) Check(taunt.Attack());
  Check(taunt.ActiveIndex==4 && !taunt.IncomingSkill.BackRow && taunt.BeginStrike() && taunt.Defend(false,.1f)); taunt.FinishStrike();
  Check(taunt.IncomingSkill.BackRow && taunt.ActiveIndex!=4); ResolveEnemies(taunt); Check(taunt.Party[4].TauntRounds==0);
  var tacticalReload=new Battle(true,enableWaves:true); Check(tacticalReload.RestoreProgress(taunt.ExportProgress()));
  foreach(var hero in tacticalReload.Party) Check(hero.TauntRounds==0);
  var forge=FactionBattle(team,20); var forgeSave=forge.ExportProgress(); string weapon="Weapon_Common_Physical";
  forgeSave.forgeMaterials=1000; forgeSave.inventory=new[]{new GearStackData{id=weapon,count=3},new GearStackData{id="Weapon_Uncommon_Fire",count=1}};
  foreach(var load in forgeSave.loadouts) if(load.id=="Knight_Common" || load.id=="Paladin_Common" || load.id=="Cleric_Uncommon") load.gear=new[]{weapon,null,null};
  Check(forge.RestoreProgress(forgeSave)); int beforeAttack=forge.Party[0].Definition.AttackDamage;
  var comparison=forge.PreviewEnhancement("Knight_Common",GearSlot.Weapon);
  Check(comparison!=null && comparison.Definition.AttackDamage>beforeAttack && forge.ForgeMaterials==1000 && forge.EnhancementLevel(weapon)==0);
  Check(forge.EnhanceGear(weapon) && forge.ForgeMaterials==990 && forge.EnhancementLevel(weapon)==1 && forge.Party[0].Definition.AttackDamage==comparison.Definition.AttackDamage);
  Check(forge.Party[1].GearEnhancement(GearSlot.Weapon)==1 && forge.HeroProgress("Cleric_Uncommon").GearEnhancement(GearSlot.Weapon)==1 && forge.GearCount(weapon)==3 && GearDefinition.Find(weapon).Attack==6);
  Check(!forge.EnhanceGear(null) && !forge.EnhanceGear("missing") && !forge.EnhanceGear("Armor_Common_Physical"));
  while(forge.CanEnhanceGear(weapon)) Check(forge.EnhanceGear(weapon));
  Check(forge.EnhancementLevel(weapon)==10 && forge.ForgeMaterials==675 && !forge.EnhanceGear(weapon) && forge.PreviewEnhancement("Knight_Common",GearSlot.Weapon)==null);
  Check(forge.EquipGear("Paladin_Common",GearSlot.Weapon,null) && forge.EquipGear("Paladin_Common",GearSlot.Weapon,weapon) && forge.Party[1].GearEnhancement(GearSlot.Weapon)==10);
  Check(!forge.EquipBestGear("Knight_Common") && forge.Party[0].Gear(GearSlot.Weapon).Id==weapon);
  var forgeReload=new Battle(true,enableWaves:true,enableFactions:true); Check(forgeReload.RestoreProgress(forge.ExportProgress()) && forgeReload.ForgeMaterials==675 && forgeReload.EnhancementLevel(weapon)==10 && forgeReload.Party[0].Definition.AttackDamage==forge.Party[0].Definition.AttackDamage);
  var badForge=forge.ExportProgress(); badForge.enhancements[0].level=11; Check(!forgeReload.RestoreProgress(badForge) && forgeReload.ForgeMaterials==675);
  badForge=forge.ExportProgress(); badForge.enhancements[0].id="Armor_Common_Physical"; Check(!forgeReload.RestoreProgress(badForge));
  badForge=forge.ExportProgress(); badForge.forgeMaterials=-1; Check(!forgeReload.RestoreProgress(badForge));
  badForge=forge.ExportProgress(); badForge.enhancements=new[]{badForge.enhancements[0],badForge.enhancements[0]}; Check(!forgeReload.RestoreProgress(badForge));
  var legacyForge=forge.ExportProgress(); legacyForge.version=14; legacyForge.enhancements=null; legacyForge.forgeMaterials=-1;
  Check(forgeReload.RestoreProgress(legacyForge) && forgeReload.ForgeMaterials==0 && forgeReload.EnhancementLevel(weapon)==0 && forgeReload.Party[0].Gear(GearSlot.Weapon).Id==weapon);
  Check(forge.Attack() && !forge.EnhanceGear("Weapon_Uncommon_Fire") && forge.ForgeMaterials==675);
  var clock=new DateTime(2026,10,13,0,0,0,DateTimeKind.Utc); var idleForge=new Battle(true,utcNow:()=>clock);
  clock=clock.AddHours(20); Check(idleForge.IdleForgeMaterials==720 && idleForge.ClaimIdleRewards() && idleForge.ForgeMaterials==720 && !idleForge.ClaimIdleRewards());
  var fullMaterials=idleForge.ExportProgress(); fullMaterials.forgeMaterials=999999; Check(idleForge.RestoreProgress(fullMaterials)); clock=clock.AddMinutes(10);
  Check(idleForge.IdleForgeMaterials==1 && idleForge.ClaimIdleRewards() && idleForge.ForgeMaterials==1000000);
 }
 static void WishlistChecks() {
  var b=new Battle(true); Check(b.Wishlist.Length==0 && !b.ToggleWishlist("Knight_Common") && !b.ToggleWishlist("missing"));
  Check(b.ToggleWishlist("Knight_Rare") && b.IsWishlisted("Knight_Rare"));
  Check(b.ToggleWishlist("Paladin_Rare") && b.WishlistCount(HeroFaction.Veilborn)==2);
  Check(!b.ToggleWishlist("Rogue_Epic") && b.Wishlist.Length==2);
  Check(b.ToggleWishlist("Paladin_Rare") && !b.IsWishlisted("Paladin_Rare"));
  var wishes=b.Wishlist; wishes[0]="bad"; Check(b.IsWishlisted("Knight_Rare"));
  var selected=HeroDefinition.Find("Knight_Rare"); var other=HeroDefinition.Find("Paladin_Rare");
  Check(Summoning.HeroChance(selected,b.Wishlist)==.375 && Summoning.HeroChance(other,b.Wishlist)==.125);
  foreach(HeroQuality quality in Enum.GetValues(typeof(HeroQuality))) {
   var counts=new int[6];
   for(int i=0;i<8000;i++) counts[(int)Summoning.Pick(quality,(i+.5)/8000,b.Wishlist).Class]++;
   foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==quality) {
    Check(Math.Abs(counts[(int)hero.Class]/8000.0-Summoning.HeroChance(hero,b.Wishlist))<.001);
   }
  }
  // An empty tier is uniform; selecting every candidate in a tier is also uniform.
  var allRare=new System.Collections.Generic.List<string>(); foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==HeroQuality.Rare) allRare.Add(hero.Id);
  foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==HeroQuality.Rare) Check(Math.Abs(Summoning.HeroChance(hero,allRare)-1.0/6)<.000001);
  int wallet=b.Crystals, pity=b.RareMisses; Check(b.Summon(.9,double.NaN)==null && b.Summon(.9,1.0)==null && b.Crystals==wallet && b.RareMisses==pity);
  var first=b.Summon(.9,0.0); Check(first.Hero.Id=="Knight_Rare" && !first.Duplicate && b.Crystals==wallet-100);
  var second=b.Summon(.9,.2); Check(second.Hero.Id=="Knight_Rare" && second.Duplicate && second.Shards==20 && second.Refund==50 && b.HeroProgress("Knight_Rare").Shards==20);
  var saved=b.ExportProgress(); saved.crystals=2000; saved.rareMisses=4; saved.legendaryMisses=14;
  var reload=new Battle(true); Check(reload.RestoreProgress(saved) && reload.IsWishlisted("Knight_Rare"));
  var guaranteed=reload.Summon(0.0,0.0); Check(guaranteed.Hero.Quality==HeroQuality.Legendary && reload.RareMisses==0 && reload.LegendaryMisses==0);
  saved=reload.ExportProgress(); saved.rareMisses=4; saved.legendaryMisses=0; Check(reload.RestoreProgress(saved));
  Check(reload.Summon(0.0,0.0).Hero.Id=="Knight_Rare");
  var tenSave=reload.ExportProgress(); tenSave.crystals=2000; Check(reload.RestoreProgress(tenSave));
  var rolls=new double[10]; var indexes=new int[10]; var heroes=new double[10];
  for(int i=0;i<10;i++) rolls[i]=.9;
  wallet=reload.Crystals; pity=reload.RareMisses; int beforeShards=reload.HeroProgress("Knight_Rare").Shards; heroes[9]=1;
  Check(reload.SummonTen(rolls,indexes,heroes)==null && reload.Crystals==wallet && reload.RareMisses==pity && reload.HeroProgress("Knight_Rare").Shards==beforeShards);
  heroes[9]=0; var batch=reload.SummonTen(rolls,heroes); Check(batch!=null && batch.Length==10);
  foreach(var pull in batch) Check(pull.Hero.Id=="Knight_Rare" || pull.Hero.Quality==HeroQuality.Legendary);
  var bad=b.ExportProgress(); wallet=reload.Crystals;
  foreach(var invalid in new[]{new[]{"missing"},new[]{"Knight_Common"},new[]{"Knight_Rare","Knight_Rare"},new[]{"Knight_Rare","Paladin_Rare","Rogue_Epic"},new string[]{null},null}) {
   bad.wishlist=invalid; Check(!reload.RestoreProgress(bad) && reload.Crystals==wallet && reload.IsWishlisted("Knight_Rare"));
  }
  var legacy=b.ExportProgress(); legacy.version=15; legacy.wishlist=null; Check(reload.RestoreProgress(legacy) && reload.Wishlist.Length==0 && reload.Crystals==b.Crystals);
  Check(b.Attack() && !b.ToggleWishlist("Knight_Rare") && b.IsWishlisted("Knight_Rare"));
  var full=new Battle(true); foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==HeroQuality.Rare) Check(full.ToggleWishlist(hero.Id));
  Check(full.Wishlist.Length==6 && !full.ToggleWishlist("Knight_Legendary") && full.Crystals==300 && full.RareMisses==0);
  Check(new Battle(true).RestoreProgress(full.ExportProgress()));
 }
 static void ActivitiesChecks() {
  var fresh=new Battle(true,enableWaves:true);
  Check(fresh.DiscoveredEnemies.Length==0 && !fresh.CanStartDailyBoss && fresh.StartDailyBoss()==null);
  string key=new EnemyGuideEntry(0,fresh.Enemy).Key;
  Check(EnemyGuideEntry.Find(key)!=null && fresh.Attack() && fresh.EnemyDiscovered(key));
  var discovered=new Battle(true,enableWaves:true); Check(discovered.RestoreProgress(fresh.ExportProgress()) && discovered.EnemyDiscovered(key));
  var keys=new System.Collections.Generic.HashSet<string>(); foreach(var entry in EnemyGuideEntry.Catalog) Check(keys.Add(entry.Key) && !string.IsNullOrEmpty(entry.Advice));
  for(int day=0;day<3;day++) {
   var now=new DateTime(2026,10,15+day,0,0,0,DateTimeKind.Utc);
   var owner=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true,utcNow:()=>now);
   var save=owner.ExportProgress(); save.encounter=10;
   save.recruited=save.party=new[]{"Knight_Common","Paladin_Common","Sorceress_Common"};
   save.loadouts=new[]{new HeroLoadoutData{id="Knight_Common",secondSkill=1},new HeroLoadoutData{id="Paladin_Common",secondSkill=1},new HeroLoadoutData{id="Sorceress_Common",secondSkill=1}};
   Check(owner.RestoreProgress(save) && owner.CanStartDailyBoss);
   int encounter=owner.Encounter, crystals=owner.Crystals, materials=owner.ForgeMaterials, power=owner.TeamPower, index=owner.DailyBossIndex;
   var run=owner.StartDailyBoss(); Check(run!=null && run.IsDailyBoss && run.Party.Count==3 && run.WaveCount==1 && run.DailyBossRounds==0 && run.Enemy.Name==owner.DailyBossName);
   Check(!run.CanChangeParty && !run.CanSummon && !run.ContinueAfterVictory());
   bool reward; Check(!owner.FinishDailyBoss(run,out reward));
   int actions=0;
   while(run.Current!=Phase.Won && run.Current!=Phase.Lost && actions++<200) {
    if(run.Current==Phase.Player) Check(run.Attack());
    else if(run.Current==Phase.EnemyWindup) {
     int rounds=run.DailyBossRounds; Check(run.BeginStrike());
     if(run.IncomingSkill.Action==EnemyAction.Attack) for(int i=0;i<run.Party.Count;i++) if(run.Party[i].Health>0 && (run.IncomingSkill.PartyWide || i==run.ActiveIndex)) Check(run.DefendHero(i,true,.1f));
     run.FinishStrike(); Check(run.DailyBossRounds==rounds+1);
    } else throw new Exception("Unexpected daily phase "+run.Current);
   }
   Check(run.Current==Phase.Lost && run.DailyBossRounds==8 && run.DailyBossScore>0 && run.Results==null);
   Check(owner.Encounter==encounter && owner.TeamPower==power && owner.Crystals==crystals);
   foreach(var hero in owner.Party) Check(hero.Health==hero.Definition.MaxHealth && hero.EncounterDamage==0 && hero.Energy==0);
   Check(owner.FinishDailyBoss(run,out reward) && reward && owner.Crystals==crystals+35 && owner.ForgeMaterials==materials+30 && owner.DailyBossBest(index)==run.DailyBossScore);
   Check(!owner.FinishDailyBoss(run,out reward) && !reward && !owner.DailyBossRewardAvailable);
   var replay=owner.StartDailyBoss(); Check(replay!=null);
   replay.Enemies[0].Health=1; Check(replay.Attack() && replay.Current==Phase.Won && replay.Results==null);
   int rewardedWallet=owner.Crystals;
   Check(owner.FinishDailyBoss(replay,out reward) && !reward && owner.Crystals==rewardedWallet && owner.Encounter==10 && owner.DailyBossBest(index)==run.DailyBossScore);
   var reload=new Battle(true,utcNow:()=>now); Check(reload.RestoreProgress(owner.ExportProgress()) && reload.DailyBossBest(index)==run.DailyBossScore && !reload.DailyBossRewardAvailable && reload.Encounter==10);
   int record=reload.DailyBossBest(index); now=now.AddDays(-1); Check(!reload.DailyBossRewardAvailable && reload.DailyBossIndex==index);
   now=now.AddDays(2); Check(reload.DailyBossRewardAvailable && reload.DailyBossIndex==(index+1)%3 && reload.DailyBossBest(index)==record);
   var invalid=owner.ExportProgress(); invalid.bossRecords=new[]{-1,0,0}; Check(!reload.RestoreProgress(invalid) && reload.DailyBossBest(index)==record);
   invalid=owner.ExportProgress(); invalid.bossClaimDay=invalid.bossClockDay+TimeSpan.TicksPerDay; Check(!reload.RestoreProgress(invalid));
   invalid=owner.ExportProgress(); invalid.bossClockDay++; Check(!reload.RestoreProgress(invalid));
   invalid=owner.ExportProgress(); invalid.discoveredEnemies=new[]{"unknown"}; Check(!reload.RestoreProgress(invalid));
   invalid=owner.ExportProgress(); if(invalid.discoveredEnemies.Length>0) { invalid.discoveredEnemies=new[]{invalid.discoveredEnemies[0],invalid.discoveredEnemies[0]}; Check(!reload.RestoreProgress(invalid)); }
   var legacy=owner.ExportProgress(); legacy.version=16; legacy.discoveredEnemies=null; legacy.bossRecords=null;
   Check(reload.RestoreProgress(legacy) && reload.DailyBossBest(index)==0 && reload.DiscoveredEnemies.Length>0 && reload.Encounter==10 && reload.Crystals==owner.Crystals);
   string advice=owner.UpgradeGuidance("Knight_Common"); Check(advice.Contains("100 XP") && advice.Contains("20 more shards") && advice.Contains("own Lv 5") && advice.Contains("Empty Weapon"));
   Check(owner.UpgradeGuidance("unknown")=="Select a recruited hero.");
   var ready=owner.ExportProgress(); ready.loadouts[0].level=5; ready.loadouts[0].shards=20;
   Check(owner.RestoreProgress(ready)); int upgradeWallet=owner.Crystals, upgradePower=owner.TeamPower;
   advice=owner.UpgradeGuidance("Knight_Common"); Check(advice.Contains("ASCENSION READY") && advice.Contains("TALENT READY") && owner.Crystals==upgradeWallet && owner.TeamPower==upgradePower && owner.HeroProgress("Knight_Common").Shards==20);
   for(int sample=0;sample<20;sample++) {
    var trial=owner.StartDailyBoss(); var rng=new Random(1000+day*20+sample); int turns=0;
    while(trial.Current!=Phase.Won && trial.Current!=Phase.Lost && turns++<300) {
     if(trial.Current==Phase.Player) { int skill=AutoBattlePlanner.ChooseSkill(trial); if(!(skill<0?trial.Attack():trial.UseAbility(skill))) throw new Exception("Daily auto action rejected"); }
     else if(trial.Current==Phase.EnemyWindup) {
      trial.BeginStrike(); if(trial.IncomingSkill.Action==EnemyAction.Attack) for(int i=0;i<trial.Party.Count;i++) if(trial.Party[i].Health>0 && (trial.IncomingSkill.PartyWide || i==trial.ActiveIndex)) {
       var defense=AutoBattlePlanner.RollDefense(trial.Party[i].Identity.Class,rng.NextDouble()); if(defense!=DefenseOutcome.Miss) trial.DefendHero(i,defense==DefenseOutcome.Parry,defense==DefenseOutcome.Parry?.12f:.26f);
      }
      trial.FinishStrike();
     } else throw new Exception("Unexpected daily auto phase");
    }
    Check((trial.Current==Phase.Won || trial.Current==Phase.Lost) && trial.DailyBossRounds<=8 && trial.DailyBossScore>0 && owner.Encounter==10 && owner.Crystals==upgradeWallet && owner.TeamPower==upgradePower);
   }
  }
 }
 static void Main() {
  ActivitiesChecks();
  WishlistChecks();
  var b = new Battle();
  Check(b.Current == Phase.Player && b.HeroHealth == 100);
  Check(!b.Defend(false, 0));
  Check(b.Attack() && b.EnemyHealth == 75);
  Check(!b.Attack() && !b.Defend(false, 0));
  Check(b.BeginStrike());
  Check(!b.Defend(true, .19f));
  Check(b.Defend(false, .4f));
  Check(!b.Defend(true, .1f));
  b.FinishStrike(); Check(b.HeroHealth == 100 && b.Current == Phase.Player);
  b.Reset();
  for(int i=0;i<3;i++) { Check(b.Attack()); b.BeginStrike(); b.FinishStrike(); }
  Check(b.Current == Phase.Lost && b.HeroHealth == 0 && !b.Attack());
  b.Reset();
  for(int i=0;i<3;i++) { b.Attack(); if(b.Current == Phase.Won) break; b.BeginStrike(); Check(b.Defend(true,.18f)); b.FinishStrike(); }
  Check(b.Current == Phase.Won && b.EnemyHealth == 0 && b.HeroHealth == 100);
  b.FinishStrike(); Check(b.Current == Phase.Won);
  b.Reset(); Check(b.Current == Phase.Player && b.EnemyHealth == 100 && !b.Defended);
  b.Attack(); b.BeginStrike(); Check(!b.Defend(false,-.01f) && !b.Defend(false,.41f));
  foreach (HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   var hero = ClassDefinition.For(kind);
   var duel = new Battle(kind);
   Check(duel.Hero.Kind == kind && duel.HeroHealth == hero.MaxHealth && duel.AbilityCharges == 2);
   Check(duel.Attack() && duel.EnemyHealth == 100 - hero.AttackDamage);
   Check(!duel.UseAbility() && duel.AbilityCharges == 2);
   duel.BeginStrike(); duel.FinishStrike();
   int health = duel.HeroHealth;
   Check(duel.UseAbility() && duel.AbilityCharges == 1);
   Check(duel.HeroHealth == Math.Min(hero.MaxHealth, health + hero.AbilityHealing));
   Check(duel.EnemyHealth == Math.Max(0, 100 - hero.AttackDamage - hero.AbilityDamage));
   duel.Reset();
   Check(duel.UseAbility() && duel.HeroHealth == hero.MaxHealth);
   duel.BeginStrike(); duel.Defend(false, .1f); duel.FinishStrike();
   Check(duel.UseAbility() && duel.AbilityCharges == 0);
   if (duel.Current == Phase.EnemyWindup) { duel.BeginStrike(); duel.Defend(false, .1f); duel.FinishStrike(); }
   Check(!duel.UseAbility() && duel.AbilityCharges == 0);
   duel.SelectClass(HeroClass.Paladin);
   Check(duel.HeroHealth == 120 && duel.EnemyHealth == 100 && duel.AbilityCharges == 2 && duel.Current == Phase.Player);
  }
  bool invalid = false;
  try { ClassDefinition.For((HeroClass)99); } catch (ArgumentOutOfRangeException) { invalid = true; }
  Check(invalid);
  var campaign = new Battle(true);
  Check(campaign.Party.Count == 1 && campaign.Party[0].Identity.Name == "Rowan");
  Check(!campaign.EquipHero("Paladin_Common") && !campaign.ContinueAfterVictory());
  for (int encounter = 0; encounter < 29; encounter++) {
   int safety = 0;
   while (campaign.Current != Phase.Won && safety++ < 500) {
    if (campaign.Current == Phase.Player) Check(campaign.Attack());
    else if (campaign.Current == Phase.EnemyWindup) { campaign.BeginStrike(); campaign.Defend(false,.1f); campaign.FinishStrike(); }
    else throw new Exception("Unexpected campaign phase");
   }
   Check(campaign.Current == Phase.Won);
   Check(campaign.ContinueAfterVictory());
   Check(campaign.Party.Count == Math.Min(Battle.MaxPartySize, (encounter+1)/5+1));
   int expectedRecruits=1; foreach(var arrival in StoryRecruit.Catalog) if(arrival.Encounter<=encounter) expectedRecruits++;
   Check(campaign.Recruits.Count==expectedRecruits);
  }
  Check(campaign.Recruits.Count==6 && campaign.Party.Count==5);
  // Retain a smaller party for the existing three-hero action and save regression cases.
  var trio=campaign.ExportProgress();
  // Explicit all-hero fixture keeps the later collection/save regressions independent of story grants.
  var allStoryIds=new System.Collections.Generic.List<string>(); var allStoryLoads=new System.Collections.Generic.List<HeroLoadoutData>();
  foreach(var hero in HeroDefinition.Catalog) { allStoryIds.Add(hero.Id); HeroLoadoutData load=null; foreach(var owned in trio.loadouts) if(owned.id==hero.Id) load=owned; allStoryLoads.Add(load??new HeroLoadoutData{id=hero.Id,secondSkill=1}); }
  trio.recruited=allStoryIds.ToArray(); trio.loadouts=allStoryLoads.ToArray(); trio.party=new[]{trio.party[0],trio.party[1],trio.party[2]};
  Check(campaign.RestoreProgress(trio));
  Check(campaign.SelectHero(2) && campaign.EquipHero("Cleric_Legendary"));
  Check(campaign.Party[2].Identity.Quality == HeroQuality.Legendary);
  Check(campaign.EquipSkill(0,4));
  Check(campaign.EquipHero("Rogue_Common"));
  Check(campaign.EquipHero("Cleric_Legendary") && campaign.Party[2].SkillIndex(0) == 4);
  Check(!campaign.EquipHero("Knight_Common")); // already in another party slot
  Check(campaign.SelectHero(0));
  Check(campaign.Attack() && campaign.Current == Phase.Player && campaign.Party[0].Acted);
  Check(!campaign.SelectHero(0) && !campaign.EquipSkill(0,3) && !campaign.EquipHero("Ranger_Common"));
  Check(campaign.Attack() && campaign.Current == Phase.Player);
  Check(campaign.Attack() && campaign.Current == Phase.EnemyWindup);
  Check(!campaign.SelectHero(2));
  int targetIndex = campaign.ActiveIndex;
  int targetHealth = campaign.Party[targetIndex].Health;
  campaign.BeginStrike(); campaign.FinishStrike();
  Check(campaign.Party[targetIndex].Health == targetHealth - 35);
  Check(campaign.Current == Phase.EnemyWindup && campaign.Party[0].Acted);
  Check(campaign.AttackingEnemyIndex == 1 && campaign.BeginStrike());
  campaign.FinishStrike();
  Check(campaign.Current == Phase.Player && !campaign.Party[0].Acted);
  campaign.Reset();
  campaign.Party[0].Health = 0; campaign.Party[1].Health = 40; campaign.Party[2].Health = 50;
  Check(campaign.SelectHero(2));
  Check(campaign.UseAbility(0));
  Check(campaign.Party[0].Health == 0 && campaign.Party[1].Health > 40 && campaign.Party[2].Health > 50);
  Check(!campaign.SelectHero(0));
  campaign.Reset();
  campaign.Party[0].Health = 0; campaign.Party[1].Health = 0;
  Check(campaign.SelectHero(2) && campaign.Attack() && campaign.Current == Phase.EnemyWindup);
  campaign.Party[2].Health = 1;
  campaign.BeginStrike(); campaign.FinishStrike();
  Check(campaign.Current == Phase.Lost);

  foreach (HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   var skills = SkillDefinition.For(kind);
   Check(skills.Count == 6);
   for(int index=0; index<6; index++) {
    var duel = new Battle(kind);
    if(index == 1) Check(duel.EquipSkill(1,2));
    Check(duel.EquipSkill(0,index));
    Check(!duel.EquipSkill(1,index));
    Check(duel.UseAbility(0));
    Check(duel.EnemyHealth == 100 - skills[index].Damage && duel.AbilityCharges == 1);
    Check(!duel.EquipSkill(0,5));
    bool interrupted=(skills[index].Utility&SkillUtility.Interrupt)!=0;
    Check(duel.BeginStrike() && (duel.IncomingSkill.Action==EnemyAction.Interrupted)==interrupted); duel.FinishStrike();
    Check(duel.HeroHealth==duel.Hero.MaxHealth-(interrupted?0:Math.Max(0,35-skills[index].Guard)));
    Check(duel.Party[0].Guard==(interrupted?skills[index].Guard:0));
    duel.Reset(); Check(duel.Party[0].SkillIndex(0) == index && duel.Party[0].SkillCharges(0) == 2);
   }
  }
  var gear = new Battle(HeroClass.Knight);
  Check(!gear.EquipSkill(-1,0) && !gear.EquipSkill(2,0) && !gear.EquipSkill(0,6));
  Check(!gear.UseAbility(-1) && !gear.UseAbility(2));
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   HeroDefinition previous = null;
   foreach(var hero in HeroDefinition.Catalog) if(hero.Class == kind) {
    if(previous != null) {
     Check(hero.Stats.MaxHealth > previous.Stats.MaxHealth && hero.Stats.AttackDamage > previous.Stats.AttackDamage);
     for(int skill=0;skill<6;skill++) {
      var before = previous.Stats.Skills[skill]; var after = hero.Stats.Skills[skill];
      Check(after.Damage >= before.Damage && after.Healing >= before.Healing && after.Guard >= before.Guard);
     }
    }
    previous = hero;
   }
  }
  var enemy = new EnemyDefinition("Element target",100,Element.Fire,Element.Cold);
  Check(enemy.Damage(20,Element.Fire)==30 && enemy.Damage(20,Element.Cold)==10);
  Check(enemy.Damage(20,Element.Poison)==20 && enemy.Damage(20,Element.Lightning)==20);
  Check(enemy.Damage(0,Element.Fire)==0 && enemy.Damage(1,Element.Cold)==1);
  var elemental = new Battle(true);
  Check(elemental.Attack() && elemental.LastDamage == 38 && elemental.LastElementMultiplier == 1.5f);
  var overrides = new Battle(true);
  for(int encounter=0;encounter<10;encounter++) {
   while(overrides.Current != Phase.Won) {
    if(overrides.Current == Phase.Player) overrides.Attack();
    else { overrides.BeginStrike(); overrides.Defend(false,.1f); overrides.FinishStrike(); }
   }
   overrides.ContinueAfterVictory();
  }
  Check(overrides.SelectHero(2));
  Check(overrides.UseAbility(0) && overrides.LastElement == Element.Lightning && overrides.LastElementMultiplier == 1f && overrides.LastDamage == 45);
  overrides.Reset(); Check(overrides.SelectHero(2));
  Check(overrides.Attack() && overrides.LastElement == Element.Cold && overrides.LastElementMultiplier == 1.5f && overrides.LastDamage == 45);
  foreach(var named in HeroDefinition.Catalog) {
   Check(named.Stats.Skills[1].Affinity == SkillDefinition.For(named.Class)[1].Affinity);
  }
  foreach(var named in HeroDefinition.Catalog) {
   var allowed = ClassElements.For(named.Class);
   bool heroAllowed = false;
   foreach(var element in allowed) if(element == named.Affinity) heroAllowed = true;
   Check(heroAllowed);
   foreach(var skill in named.Stats.Skills) {
    bool skillAllowed = false;
    foreach(var element in allowed) if(skill.Affinity == element) skillAllowed = true;
    Check(skillAllowed);
   }
  }
  var shade = EnemyDefinition.Encounter(4);
  Check(shade.Damage(20,Element.Light)==30 && shade.Damage(20,Element.Fire)==10);
  var progress = campaign.ExportProgress();
  var resumed = new Battle(true);
  Check(resumed.RestoreProgress(progress));
  Check(resumed.Encounter == campaign.Encounter && resumed.Recruits.Count == 30 && resumed.Party.Count == 3);
  for(int i=0;i<3;i++) {
   Check(resumed.Party[i].Identity.Id==campaign.Party[i].Identity.Id);
   Check(resumed.Party[i].SkillIndex(0)==campaign.Party[i].SkillIndex(0));
   Check(resumed.Party[i].Health==resumed.Party[i].Definition.MaxHealth);
  }
  Check(resumed.SelectHero(2) && resumed.Party[2].SkillIndex(0)==4);
  var invalidSave=campaign.ExportProgress(); invalidSave.version=99;
  Check(!resumed.RestoreProgress(invalidSave) && resumed.Recruits.Count==30);
  invalidSave=campaign.ExportProgress();invalidSave.party=new[]{"Knight_Common","Knight_Common"};
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.party=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common","Cleric_Common"};
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.loadouts[0].secondSkill=invalidSave.loadouts[0].firstSkill;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.loadouts[0].firstSkill=6;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.recruited[0]="missing";
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.loadouts=null;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.encounter=-1;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.encounter=ChapterDefinition.TotalStages+1;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.activeIndex=3;
  Check(!resumed.RestoreProgress(invalidSave));
  invalidSave=campaign.ExportProgress();invalidSave.crystals=-1;
  Check(!resumed.RestoreProgress(invalidSave));
  Check(!resumed.RestoreProgress(null));
  // Reloading midway through a fight restores the pre-encounter loadout, not health/turn state.
  var fresh=new Battle(true); fresh.EquipSkill(0,4); fresh.Attack(); fresh.BeginStrike(); fresh.FinishStrike();
  var checkpoint=fresh.ExportProgress(); var reload=new Battle(true);
  Check(reload.RestoreProgress(checkpoint) && reload.HeroHealth==100 && reload.Current==Phase.Player && reload.Party[0].SkillIndex(0)==4);
  Check(ChapterDefinition.Catalog.Count==12 && ChapterDefinition.TotalStages==60);
  var enemyNames=new System.Collections.Generic.HashSet<string>();
  for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) for(int stage=0;stage<5;stage++) {
   var foe=ChapterDefinition.EnemyAt(chapter*5+stage);
   Check(enemyNames.Add(foe.Name));
   Check(foe.MaxHealth>0 && foe.Weakness!=foe.Resistance);
  }
  // A save that completed the original six chapters continues directly into chapter seven.
  var oldComplete=campaign.ExportProgress(); oldComplete.encounter=30; oldComplete.pendingVictory=false; oldComplete.results=null;
  oldComplete.crystals=913; oldComplete.rareMisses=2; oldComplete.legendaryMisses=9;
  var extended=new Battle(true,enableWaves:true);
  Check(extended.RestoreProgress(oldComplete) && !extended.CampaignComplete && extended.ChapterIndex==6 && extended.Encounter==30 && extended.PartyLimit==5);
  var continuedSave=extended.ExportProgress();
  Check(continuedSave.crystals==913 && continuedSave.rareMisses==2 && continuedSave.legendaryMisses==9);
  Check(string.Join(",",continuedSave.recruited)==string.Join(",",oldComplete.recruited));
  foreach(var loadout in oldComplete.loadouts) Check(extended.HeroProgress(loadout.id).Level==loadout.level && extended.HeroProgress(loadout.id).Experience==loadout.experience && extended.HeroProgress(loadout.id).UpgradeRank==loadout.upgradeRank && extended.HeroProgress(loadout.id).Shards==loadout.shards && extended.HeroProgress(loadout.id).SkillIndex(0)==loadout.firstSkill && extended.HeroProgress(loadout.id).SkillIndex(1)==loadout.secondSkill);
  // Completion after chapter twelve remains complete; there is no campaign-reset API.
  var finalSave=campaign.ExportProgress(); finalSave.encounter=ChapterDefinition.TotalStages-1; finalSave.pendingVictory=false; finalSave.results=null;
  Check(resumed.RestoreProgress(finalSave)); foreach(var foe in resumed.Enemies) foe.Health=1;
  while(resumed.Current!=Phase.Won) {
   if(resumed.Current==Phase.Player) Check(resumed.Attack());
   else { Check(resumed.BeginStrike()); if(resumed.IncomingSkill.Action==EnemyAction.Attack) resumed.Defend(true,.1f); resumed.FinishStrike(); }
  }
  Check(resumed.ContinueAfterVictory() && resumed.CampaignComplete && resumed.Current==Phase.Complete);
  Check(!resumed.Attack() && !resumed.UseAbility() && !resumed.ContinueAfterVictory());
  var completed=new Battle(true);
  Check(completed.RestoreProgress(resumed.ExportProgress()) && completed.CampaignComplete && completed.Encounter==60);
  var winner=new Battle(true);
  while(winner.Current!=Phase.Won) {
   if(winner.Current==Phase.Player) winner.Attack();
   else {winner.BeginStrike();winner.Defend(false,.1f);winner.FinishStrike();}
  }
  var wonSave=winner.ExportProgress();var wonReload=new Battle(true);
  Check(wonSave.pendingVictory && wonReload.RestoreProgress(wonSave) && wonReload.Current==Phase.Won);
  Check(wonReload.ContinueAfterVictory() && wonReload.Encounter==1 && wonReload.Recruits.Count==1);
  Check(!wonReload.ContinueAfterVictory());
  Check(!wonReload.ExportProgress().pendingVictory);
  Check(Summoning.Quality(0,0,0)==HeroQuality.Common);
  Check(Summoning.Quality(.599999,0,0)==HeroQuality.Common);
  Check(Summoning.Quality(.60,0,0)==HeroQuality.Uncommon);
  Check(Summoning.Quality(.85,0,0)==HeroQuality.Rare);
  Check(Summoning.Quality(.95,0,0)==HeroQuality.Epic);
  Check(Summoning.Quality(.99,0,0)==HeroQuality.Legendary);
  Check(Summoning.Quality(0,4,0)==HeroQuality.Rare);
  Check(Summoning.Quality(.97,4,0)==HeroQuality.Epic);
  Check(Summoning.Quality(0,4,14)==HeroQuality.Legendary);
  int[] distribution=new int[5];
  for(int i=0;i<10000;i++) distribution[(int)Summoning.Quality((i+.5)/10000,0,0)]++;
  Check(distribution[0]==6000 && distribution[1]==2500 && distribution[2]==1000 && distribution[3]==400 && distribution[4]==100);
  var summonGame=new Battle(true);
  Check(summonGame.Crystals==300 && summonGame.CanSummon);
  Check(summonGame.Summon(double.NaN,0)==null && summonGame.Crystals==300);
  Check(summonGame.Summon(1,0)==null && summonGame.Summon(0,6)==null);
  var duplicate=summonGame.Summon(0,0);
  Check(duplicate.Duplicate && duplicate.Refund==20 && summonGame.Crystals==220 && summonGame.Recruits.Count==1);
  var rare=summonGame.Summon(.9,2);
  Check(!rare.Duplicate && rare.Hero.Id=="Sorceress_Rare" && summonGame.Recruits.Count==2 && summonGame.Party.Count==1);
  Check(summonGame.RareMisses==0 && summonGame.LegendaryMisses==2 && summonGame.Crystals==120);
  var legend=summonGame.Summon(.995,1);
  Check(legend.Hero.Quality==HeroQuality.Legendary && summonGame.LegendaryMisses==0 && summonGame.Crystals==20);
  Check(summonGame.Summon(0,0)==null && summonGame.Crystals==20);
  var summonSave=summonGame.ExportProgress();var summonReload=new Battle(true);
  Check(summonReload.RestoreProgress(summonSave) && summonReload.Crystals==20 && summonReload.Recruits[1].Id=="Sorceress_Rare");
  Check(summonReload.EquipHero("Paladin_Legendary"));
  Check(summonReload.Party.Count==1);
  var funded=new Battle(true);var funds=funded.ExportProgress();funds.crystals=5000;
  Check(funded.RestoreProgress(funds));
  for(int i=0;i<15;i++) {
   var result=funded.Summon(0,0);
   Check(result!=null);
   if(i==4 || i==9) Check(result.Hero.Quality>=HeroQuality.Rare);
   if(i==14) Check(result.Hero.Quality==HeroQuality.Legendary);
  }
  Check(funded.LegendaryMisses==0 && funded.RareMisses==0 && funded.Party.Count==1);
  funds=funded.ExportProgress();funds.rareMisses=5;
  Check(!funded.RestoreProgress(funds));
  funds=funded.ExportProgress();funds.legendaryMisses=15;
  Check(!funded.RestoreProgress(funds));
  funds=funded.ExportProgress();funds.recruited[1]=funds.recruited[0];
  Check(!funded.RestoreProgress(funds));
  var legacy=winner.ExportProgress();legacy.version=1;legacy.crystals=0;
  var migrated=new Battle(true);
  Check(migrated.RestoreProgress(legacy) && migrated.Crystals==300 && migrated.ExportProgress().version==17);
  var rewarded=new Battle(true);int beforeCurrency=rewarded.Crystals;
  while(rewarded.Current!=Phase.Won) {
   if(rewarded.Current==Phase.Player) rewarded.Attack();
   else {rewarded.BeginStrike();rewarded.Defend(false,.1f);rewarded.FinishStrike();}
  }
  Check(rewarded.ContinueAfterVictory() && rewarded.Crystals==beforeCurrency+50);
  Check(!rewarded.ContinueAfterVictory() && rewarded.Crystals==beforeCurrency+50);
  Check(rewarded.Attack() && !rewarded.CanSummon && rewarded.Summon(0,0)==null);
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   int women=0,men=0;
   foreach(var named in HeroDefinition.Catalog) if(named.Class==kind) {
    if(named.Gender==HeroGender.Woman) women++; else men++;
    var expectedStats=ClassDefinition.For(kind).Scaled(HeroDefinition.Multiplier(named.Quality));
    Check(named.Stats.MaxHealth==expectedStats.MaxHealth && named.Stats.AttackDamage==expectedStats.AttackDamage);
   }
   Check(women>0 && men>0);
  }
  foreach(var named in HeroDefinition.Catalog)
   if(named.Class==HeroClass.Sorceress) Check(named.DisplayClass==(named.Gender==HeroGender.Woman?"Sorceress":"Sorcerer"));
  for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) for(int stage=0;stage<5;stage++) {
   var group=ChapterDefinition.EnemiesAt(chapter*5+stage);
   Check(group.Count==(chapter>=2 && stage==4?2:1));
   foreach(var foe in group) Check(foe.IsBoss==(stage==4));
   if(group.Count==2) Check(group[1].Weakness==group[0].Resistance && group[1].Resistance==group[0].Weakness);
  }
  var paired=new Battle(true);var pairSave=paired.ExportProgress();pairSave.encounter=14;
  Check(paired.RestoreProgress(pairSave) && paired.Enemies.Count==2 && paired.CanChangeParty);
  Check(!paired.SelectEnemy(-1) && !paired.SelectEnemy(2));
  Check(paired.SelectEnemy(1) && paired.SelectedEnemyIndex==1);
  int firstBefore=paired.Enemies[0].Health,secondBefore=paired.Enemies[1].Health;
  Check(paired.Attack() && paired.Enemies[0].Health==firstBefore && paired.Enemies[1].Health<secondBefore);
  Check(!paired.CanChangeParty && !paired.SelectEnemy(0) && paired.AttackingEnemyIndex==0);
  Check(paired.BeginStrike());firstBefore=paired.Enemies[0].Health;secondBefore=paired.Enemies[1].Health;
  Check(paired.Defend(true,.1f));
  Check(paired.Enemies[0].Health<firstBefore && paired.Enemies[1].Health==secondBefore); // counter the attacker, not the selected target
  paired.FinishStrike();
  Check(paired.Current==Phase.EnemyWindup && paired.AttackingEnemyIndex==1 && !paired.Defended && paired.Party[0].Acted);
  Check(paired.BeginStrike() && paired.Defend(false,.3f));paired.FinishStrike();
  Check(paired.Current==Phase.Player && !paired.Party[0].Acted && paired.HeroHealth==100);
  paired.Enemies[0].Health=1;Check(paired.SelectEnemy(0) && paired.Attack());
  Check(paired.Enemies[0].Health==0 && paired.Current!=Phase.Won && paired.SelectedEnemyIndex==1 && paired.AttackingEnemyIndex==1);
  Check(paired.BeginStrike() && paired.Defend(false,.1f));paired.FinishStrike();
  Check(paired.Current==Phase.Player && !paired.SelectEnemy(0));
  var midPair=paired.ExportProgress();var midReload=new Battle(true);
  Check(midReload.RestoreProgress(midPair) && midReload.Enemies.Count==2 && midReload.Enemies[0].Health==midReload.Enemies[0].Definition.MaxHealth && midReload.Enemies[1].Health==midReload.Enemies[1].Definition.MaxHealth);
  paired.Enemies[1].Health=1;Check(paired.Attack() && paired.Current==Phase.Won);
  int pairCurrency=paired.Crystals;var pairWon=paired.ExportProgress();var pairReload=new Battle(true);
  Check(pairReload.RestoreProgress(pairWon) && pairReload.Current==Phase.Won && pairReload.Enemies[0].Health==0 && pairReload.Enemies[1].Health==0);
  Check(pairReload.ContinueAfterVictory() && pairReload.Encounter==15 && pairReload.Crystals==pairCurrency+50);
  Check(!pairReload.ContinueAfterVictory() && pairReload.Crystals==pairCurrency+50);
  Check(paired.RestoreProgress(pairSave));paired.Enemies[0].Health=1;
  Check(paired.SelectEnemy(1) && paired.Attack() && paired.BeginStrike() && paired.Defend(true,.1f));
  Check(paired.Enemies[0].Health==0 && paired.Current==Phase.EnemyStrike);
  paired.FinishStrike();Check(paired.Current==Phase.EnemyWindup && paired.AttackingEnemyIndex==1);
  Check(paired.BeginStrike());paired.FinishStrike();Check(paired.Current==Phase.Player && paired.HeroHealth==65);
  paired.Reset();Check(paired.Enemies[0].Health==450 && paired.Enemies[1].Health==315 && paired.SelectedEnemyIndex==0 && paired.CanChangeParty);
  paired.Attack();paired.BeginStrike();paired.Defend(false,.1f);paired.FinishStrike();
  paired.Party[0].Health=1;Check(paired.BeginStrike());paired.FinishStrike();
  Check(paired.Current==Phase.Lost && !paired.Attack() && !paired.SelectEnemy(0));
  // Defeating the second boss first removes its attack from the round.
  Check(paired.RestoreProgress(pairSave));paired.Enemies[1].Health=1;
  Check(paired.SelectEnemy(1) && paired.Attack() && paired.SelectedEnemyIndex==0 && paired.AttackingEnemyIndex==0);
  Check(paired.BeginStrike() && paired.Defend(false,.1f));paired.FinishStrike();
  Check(paired.Current==Phase.Player);
  // Enemy turns skip fallen allies, and a death during the first strike does not end a surviving party.
  var rotation=new Battle(true);var rotationSave=campaign.ExportProgress();rotationSave.encounter=14;rotationSave.pendingVictory=false;
  Check(rotation.RestoreProgress(rotationSave) && rotation.Party.Count==3);
  rotation.Party[0].Health=0;rotation.Party[1].Health=1;
  Check(rotation.SelectHero(1) && rotation.Attack() && rotation.Attack());
  Check(rotation.Current==Phase.EnemyWindup && rotation.ActiveIndex==1);
  Check(rotation.BeginStrike());rotation.FinishStrike();
  Check(rotation.Current==Phase.EnemyWindup && rotation.Party[1].Health==0 && rotation.ActiveIndex==2 && rotation.AttackingEnemyIndex==1);
  Check(rotation.BeginStrike() && rotation.Defend(false,.1f));rotation.FinishStrike();
  Check(rotation.Current==Phase.Player && rotation.ActiveIndex==2 && !rotation.Party[2].Acted);
  // Clear every paired chapter boss encounter through the public combat API.
  for(int chapter=2;chapter<ChapterDefinition.Catalog.Count;chapter++) {
   var run=new Battle(true);var snapshot=run.ExportProgress();snapshot.encounter=chapter*5+4;
   snapshot.recruited=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"};
   snapshot.party=new string[Math.Min(5,chapter+1)]; Array.Copy(snapshot.recruited,snapshot.party,snapshot.party.Length);
   snapshot.loadouts=new HeroLoadoutData[5];
   for(int i=0;i<5;i++) snapshot.loadouts[i]=new HeroLoadoutData{id=snapshot.recruited[i],secondSkill=1};
   Check(run.RestoreProgress(snapshot));int turns=0;
   while(run.Current!=Phase.Won && turns++<1000) {
    if(run.Current==Phase.Player) Check(run.Attack());
    else {
     Check(run.BeginStrike());
     if(run.IncomingSkill.Action==EnemyAction.Attack) for(int i=0;i<run.Party.Count;i++)
      if(run.Party[i].Health>0 && (run.IncomingSkill.PartyWide || i==run.ActiveIndex)) Check(run.DefendHero(i,false,.1f));
     run.FinishStrike();
    }
   }
   Check(run.Current==Phase.Won && run.Enemies[0].Health==0 && run.Enemies[1].Health==0);
   Check(run.ContinueAfterVictory() && run.Encounter==(chapter+1)*5 && run.Crystals==350+(chapter>=6?chapter*5+25:0));
   Check(run.CampaignComplete==(chapter==ChapterDefinition.Catalog.Count-1));
  }
  foreach(var visual in EnemyVisualDefinition.Catalog) {
   Check(visual.Height>0 && visual.Height<=2.8f && !string.IsNullOrEmpty(visual.SourceModel));
   Check(EnemyVisualDefinition.For(visual.EnemyName)==visual);
  }
  foreach(var foe in ChapterDefinition.EnemiesAt(14)) Check(EnemyVisualDefinition.For(foe.Name)!=null);
  Check(EnemyVisualDefinition.For("Training Warden")==null);
  foreach (HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   double dodge,parry; AutoBattlePlanner.DefenseChances(kind,out dodge,out parry);
   Check(dodge>0 && parry>0 && dodge+parry<1);
   Check(AutoBattlePlanner.RollDefense(kind,0)==DefenseOutcome.Parry);
   Check(AutoBattlePlanner.RollDefense(kind,parry)==DefenseOutcome.Dodge);
   Check(AutoBattlePlanner.RollDefense(kind,parry+dodge)==DefenseOutcome.Miss);
   var motion=CombatMotion.For(kind,false);
   Check(motion.Impact>0 && motion.Duration>motion.Impact);
   Check(CombatMotion.Advance(0,motion.Impact,motion.Duration)==0);
   Check(CombatMotion.Advance(motion.Impact,motion.Impact,motion.Duration)==1);
   Check(CombatMotion.Advance(motion.Duration,motion.Impact,motion.Duration)==0);
   var automated=new Battle(kind); var rng=new Random(71+(int)kind);
   int safety=0;
   while (automated.Current!=Phase.Won && automated.Current!=Phase.Lost && safety++<100) {
    if (automated.Current==Phase.Player) {
     Check(automated.SelectEnemy(AutoBattlePlanner.ChooseTarget(automated)));
     int slot=AutoBattlePlanner.ChooseSkill(automated);
     Check(slot<0?automated.Attack():automated.UseAbility(slot));
    } else {
     automated.BeginStrike();
     var outcome=AutoBattlePlanner.RollDefense(automated.Hero.Kind,rng.NextDouble());
     if(automated.IncomingSkill.Action==EnemyAction.Attack && outcome!=DefenseOutcome.Miss) Check(automated.Defend(outcome==DefenseOutcome.Parry,.1f));
     automated.FinishStrike();
    }
   }
   Check(safety<100 && (automated.Current==Phase.Won || automated.Current==Phase.Lost));
   Check(automated.Crystals==0);
  }
  Check(CombatMotion.For(HeroClass.Rogue,false).SecondImpact>CombatMotion.For(HeroClass.Rogue,false).Impact);
  Check(!CombatMotion.UsesCast(HeroClass.Knight,true) && CombatMotion.UsesCast(HeroClass.Sorceress,false));
  for(int chapter=0;chapter<6;chapter++) {
   var normal=EnemySkills.For(chapter*5,true,0);
   var special=EnemySkills.For(chapter*5,true,1);
   var sweep=EnemySkills.For(chapter*5,true,2);
   Check(normal.Damage==35 && !normal.PartyWide && normal.Status==Debuff.None);
   Check(sweep.PartyWide && sweep.Damage==22);
   Check(EnemySkills.For(chapter*5,false,2).Damage==45);
   Check(chapter==0 ? special.Status==Debuff.None : special.Status!=Debuff.None);
  }
  var skillBattle=new Battle(true);
  var skillSave=skillBattle.ExportProgress(); skillSave.encounter=24;
  Check(skillBattle.RestoreProgress(skillSave));
  Check(skillBattle.Attack()); skillBattle.BeginStrike();
  Check(skillBattle.Defend(false,.1f)); skillBattle.FinishStrike();
  skillBattle.BeginStrike(); Check(skillBattle.Defend(false,.1f)); skillBattle.FinishStrike();
  Check(skillBattle.Attack()); Check(skillBattle.IncomingSkill.Action==EnemyAction.HealPartner);
  skillBattle.BeginStrike(); skillBattle.FinishStrike();
  Check(skillBattle.IncomingSkill.Status==Debuff.Poison);
  skillBattle.BeginStrike(); skillBattle.FinishStrike();
  Check(skillBattle.Party[0].Status==Debuff.Poison);
  Check(skillBattle.LastStatusDamage[0]==8 && skillBattle.Party[0].StatusRounds==1);
  skillBattle.Reset(); Check(skillBattle.Party[0].Status==Debuff.None && skillBattle.Enemies[0].CompletedStrikes==0);
  // Party-wide skills defend each living hero independently and consume guard only on a hit.
  var sweepBattle=new Battle(true); var groupSave=sweepBattle.ExportProgress(); groupSave.encounter=19;
  groupSave.recruited=new[]{"Knight_Common","Rogue_Common","Sorceress_Common"};
  groupSave.party=(string[])groupSave.recruited.Clone();
  groupSave.loadouts=new[]{new HeroLoadoutData{id="Knight_Common",firstSkill=0,secondSkill=1},new HeroLoadoutData{id="Rogue_Common",firstSkill=0,secondSkill=1},new HeroLoadoutData{id="Sorceress_Common",firstSkill=0,secondSkill=1}};
  Check(sweepBattle.RestoreProgress(groupSave));
  sweepBattle.Enemies[0].CompletedStrikes=2;
  for(int i=0;i<3;i++) Check(sweepBattle.Attack());
  Check(sweepBattle.IncomingSkill.PartyWide); sweepBattle.BeginStrike();
  int knight=sweepBattle.Party[0].Health,rogue=sweepBattle.Party[1].Health;
  sweepBattle.Party[2].Guard=35;
  Check(sweepBattle.DefendHero(0,false,.1f));
  Check(!sweepBattle.DefendHero(0,true,.1f));
  Check(!sweepBattle.DefendHero(1,false,float.NaN));
  sweepBattle.FinishStrike();
  Check(sweepBattle.Party[0].Health==knight && sweepBattle.Party[1].Health==rogue-22);
  Check(sweepBattle.Party[1].Status==Debuff.Shock && sweepBattle.Party[1].StatusRounds==2);
  Check(sweepBattle.Party[2].Status==Debuff.None && sweepBattle.Party[2].Guard==0);
  sweepBattle.BeginStrike(); sweepBattle.Defend(false,.1f); sweepBattle.FinishStrike();
  int sweepHealthBefore=sweepBattle.EnemyHealth;
  Check(sweepBattle.SelectHero(1) && sweepBattle.Attack());
  Check(sweepHealthBefore-sweepBattle.EnemyHealth==sweepBattle.Party[1].Definition.AttackDamage);
  for(int chapter=1;chapter<6;chapter++) {
   var statusBattle=new Battle(true); var statusSave=statusBattle.ExportProgress(); statusSave.encounter=chapter*5;
   Check(statusBattle.RestoreProgress(statusSave)); statusBattle.Enemies[0].CompletedStrikes=1;
   Check(statusBattle.Attack()); statusBattle.BeginStrike();
   var incoming=statusBattle.IncomingSkill;
   statusBattle.FinishStrike();
   bool resist=statusBattle.Party[0].Identity.Affinity==incoming.Element;
   int dot=incoming.Status==Debuff.Burn?6:incoming.Status==Debuff.Poison?8:0;
   Check(statusBattle.HeroHealth==100-(resist?18:24+dot));
   Check(statusBattle.Party[0].Status==(resist?Debuff.None:incoming.Status));
   Check(statusBattle.Attack()); statusBattle.BeginStrike(); Check(statusBattle.Defend(false,.1f)); statusBattle.FinishStrike();
   Check(statusBattle.Party[0].Status==Debuff.None && statusBattle.Party[0].StatusRounds==0);
  }
  var dotDeath=new Battle(); dotDeath.Party[0].Health=1;
  dotDeath.Party[0].Status=Debuff.Poison; dotDeath.Party[0].StatusRounds=1;
  Check(dotDeath.Attack()); dotDeath.BeginStrike(); Check(dotDeath.Defend(false,.1f)); dotDeath.FinishStrike();
  Check(dotDeath.Current==Phase.Lost && dotDeath.HeroHealth==0 && dotDeath.LastStatusDamage[0]==1);
  var shockGuard=new Battle(); shockGuard.Party[0].Status=Debuff.Shock; shockGuard.Party[0].StatusRounds=2;
  shockGuard.Party[0].Guard=10; Check(shockGuard.Attack()); shockGuard.BeginStrike(); shockGuard.FinishStrike();
  Check(shockGuard.HeroHealth==70 && shockGuard.Party[0].Guard==0 && shockGuard.Party[0].StatusRounds==1);
  var ultimateNames=new System.Collections.Generic.HashSet<string>();
  foreach(var identity in HeroDefinition.Catalog) {
   Check(ultimateNames.Add(identity.Ultimate.Name));
   Check(identity.Ultimate.Affinity==identity.Affinity && identity.Ultimate.Damage>identity.Stats.AttackDamage);
   var ultimateBattle=new Battle(true); var ultimateSave=ultimateBattle.ExportProgress(); ultimateSave.encounter=14;
   ultimateSave.recruited=identity.Id=="Knight_Common"?new[]{identity.Id}:new[]{"Knight_Common",identity.Id};
   ultimateSave.party=new[]{identity.Id}; ultimateSave.loadouts=new HeroLoadoutData[ultimateSave.recruited.Length];
   for(int i=0;i<ultimateSave.recruited.Length;i++) ultimateSave.loadouts[i]=new HeroLoadoutData{id=ultimateSave.recruited[i],firstSkill=0,secondSkill=1};
   Check(ultimateBattle.RestoreProgress(ultimateSave) && ultimateBattle.SelectEnemy(1));
   var member=ultimateBattle.Party[0]; member.Energy=75; member.Health=identity.Stats.MaxHealth/2;
   int hp=member.Health;
   int primary=ultimateBattle.Enemies[0].Health,partner=ultimateBattle.Enemies[1].Health;
   int regular=ultimateBattle.Enemies[1].Definition.Damage(identity.Stats.AttackDamage,identity.Affinity);
   Check(ultimateBattle.Attack());
   Check(ultimateBattle.LastUltimate==identity.Ultimate && member.Energy==0 && member.SkillCharges(0)==2);
   Check(ultimateBattle.LastDamage==regular && ultimateBattle.UltimateTarget==1);
   int first=identity.Ultimate.AreaDamage?Math.Min(primary,ultimateBattle.Enemies[0].Definition.Damage(identity.Ultimate.Damage,identity.Affinity)):0;
   int second=Math.Min(partner-regular,ultimateBattle.Enemies[1].Definition.Damage(identity.Ultimate.Damage,identity.Affinity));
   Check(ultimateBattle.LastUltimateDamage[0]==first && ultimateBattle.LastUltimateDamage[1]==second);
   Check(ultimateBattle.Enemies[0].Health==primary-first && ultimateBattle.Enemies[1].Health==partner-regular-second);
   Check(member.Health==Math.Min(identity.Stats.MaxHealth,hp+identity.Ultimate.Healing));
   Check(member.Guard==identity.Ultimate.Guard);
   Check(member.EncounterDamage==regular+first+second && member.EncounterHealing==Math.Min(identity.Stats.MaxHealth-hp,identity.Ultimate.Healing));
   ultimateBattle.Reset(); Check(member.Energy==0 && ultimateBattle.LastUltimate==null && ultimateBattle.LastUltimateDamage[0]==0);
  }
  foreach(HeroQuality quality in Enum.GetValues(typeof(HeroQuality))) {
   int maxArea=0,minSingle=int.MaxValue;
   foreach(var identity in HeroDefinition.Catalog) if(identity.Quality==quality) {
    if(identity.Ultimate.AreaDamage) maxArea=Math.Max(maxArea,identity.Ultimate.Damage);
    else minSingle=Math.Min(minSingle,identity.Ultimate.Damage);
   }
   Check(minSingle>maxArea && maxArea>0);
  }
  var charging=new Battle(HeroClass.Cleric);
  for(int turn=1;turn<=4;turn++) {
   Check(charging.Attack());
   Check(turn<4?charging.Party[0].Energy==turn*25 && charging.LastUltimate==null:charging.Party[0].Energy==0 && charging.LastUltimate!=null);
   if(charging.Current==Phase.EnemyWindup) { charging.BeginStrike(); Check(charging.Defend(false,.1f)); charging.FinishStrike(); }
  }
  charging.Reset(); Check(charging.UseAbility() && charging.Party[0].Energy==25);
  Check(!charging.UseAbility() && charging.Party[0].Energy==25);
  charging.BeginStrike(); charging.Defend(true,.1f); charging.FinishStrike(); Check(charging.Party[0].Energy==25);
  var fallback=new Battle(true); var fallbackSave=fallback.ExportProgress(); fallbackSave.encounter=14;
  Check(fallback.RestoreProgress(fallbackSave)); fallback.Party[0].Energy=75; fallback.Enemies[0].Health=1;
  Check(fallback.Attack() && fallback.UltimateTarget==1 && fallback.LastUltimateDamage[0]==0 && fallback.LastUltimateDamage[1]>0);
  var energyParty=new Battle(true); Check(energyParty.RestoreProgress(groupSave));
  energyParty.Party[0].Energy=75; energyParty.Party[1].Energy=50; energyParty.Party[2].Energy=25;
  Check(energyParty.Attack() && energyParty.LastUltimate!=null);
  Check(energyParty.Party[0].Energy==0 && energyParty.Party[1].Energy==50 && energyParty.Party[2].Energy==25);
  Check(energyParty.SelectHero(1)); energyParty.Party[1].Energy=75;
  int autoSkill=AutoBattlePlanner.ChooseSkill(energyParty);
  Check(autoSkill<0?energyParty.Attack():energyParty.UseAbility(autoSkill));
  Check(energyParty.LastUltimate==energyParty.Party[1].Identity.Ultimate && energyParty.Party[1].Energy==0);
  var energyReload=new Battle(true); Check(energyReload.RestoreProgress(energyParty.ExportProgress()));
  foreach(var member in energyReload.Party) Check(member.Energy==0);
  var growthHero=new PartyHero(HeroClass.Knight);
  growthHero.GainExperience(99); Check(growthHero.Level==1 && growthHero.Experience==99);
  growthHero.GainExperience(1); Check(growthHero.Level==2 && growthHero.Experience==0 && growthHero.Definition.MaxHealth>100 && growthHero.Definition.AttackDamage>25);
  growthHero.GainExperience(275); Check(growthHero.Level==4 && growthHero.Experience==0);
  growthHero.GainExperience(100000); Check(growthHero.Level==20 && growthHero.Experience==0);
  int capAttack=growthHero.Definition.AttackDamage; growthHero.GainExperience(100); Check(growthHero.Experience==0 && growthHero.Definition.AttackDamage==capAttack);
  var upgradeBattle=new Battle(true);
  var dupOne=upgradeBattle.Summon(.1,0); Check(dupOne.Duplicate && dupOne.Refund==20 && upgradeBattle.HeroProgress("Knight_Common").Shards==10);
  Check(!upgradeBattle.UpgradeHero("Knight_Common") && !upgradeBattle.UpgradeHero("Rogue_Common"));
  upgradeBattle.Summon(.1,0); Check(upgradeBattle.HeroProgress("Knight_Common").Shards==20);
  Check(upgradeBattle.UpgradeHero("Knight_Common"));
  Check(upgradeBattle.Party[0].UpgradeRank==1 && upgradeBattle.Party[0].Shards==0 && upgradeBattle.Party[0].UpgradeCost==40);
  Check(upgradeBattle.Hero.AttackDamage>25 && upgradeBattle.Hero.MaxHealth>100 && upgradeBattle.Party[0].Ultimate.Damage>upgradeBattle.Party[0].Identity.Ultimate.Damage);
  Check(!upgradeBattle.UpgradeHero("Knight_Common"));
  upgradeBattle.Party[0].Shards=1000; Check(upgradeBattle.Attack());
  Check(!upgradeBattle.UpgradeHero("Knight_Common") && upgradeBattle.Party[0].Shards==1000);
  upgradeBattle.Reset();
  for(int rank=2;rank<=5;rank++) Check(upgradeBattle.UpgradeHero("Knight_Common") && upgradeBattle.Party[0].UpgradeRank==rank);
  int cappedShards=upgradeBattle.Party[0].Shards;
  Check(!upgradeBattle.UpgradeHero("Knight_Common") && upgradeBattle.Party[0].Shards==cappedShards);
  var growthSave=upgradeBattle.ExportProgress(); var growthReload=new Battle(true);
  Check(growthReload.RestoreProgress(growthSave) && growthReload.Party[0].UpgradeRank==5 && growthReload.Party[0].Shards==cappedShards);
  Check(growthReload.Hero.AttackDamage==upgradeBattle.Hero.AttackDamage);
  growthSave.loadouts[0].level=21;
  Check(!growthReload.RestoreProgress(growthSave) && growthReload.Party[0].UpgradeRank==5);
  growthSave.loadouts[0].level=1; growthSave.loadouts[0].experience=100;
  Check(!growthReload.RestoreProgress(growthSave));
  growthSave.version=2; Check(growthReload.RestoreProgress(growthSave) && growthReload.Party[0].Level==1 && growthReload.Party[0].UpgradeRank==0 && growthReload.Party[0].Shards==0);
  var xpBattle=new Battle(true); xpBattle.Enemies[0].Health=1; Check(xpBattle.Attack() && xpBattle.Current==Phase.Won);
  Check(xpBattle.Party[0].Experience==0);
  var pendingXP=xpBattle.ExportProgress(); var pendingReload=new Battle(true);
  Check(pendingReload.RestoreProgress(pendingXP) && pendingReload.ContinueAfterVictory());
  Check(pendingReload.HeroProgress("Knight_Common").Experience==40 && pendingReload.HeroProgress("Paladin_Common")==null);
  Check(!pendingReload.ContinueAfterVictory() && pendingReload.Party[0].Experience==40);
  var awardedReload=new Battle(true); Check(awardedReload.RestoreProgress(pendingReload.ExportProgress()) && awardedReload.Party[0].Experience==40);
  // Growth follows the named hero through reserve swaps.
  pendingReload.Party[0].GainExperience(100); pendingReload.Party[0].Shards=17;
  var reserveSave=pendingReload.ExportProgress(); reserveSave.recruited=new[]{"Knight_Common","Paladin_Common","Rogue_Common"};
  reserveSave.loadouts=new[]{reserveSave.loadouts[0],new HeroLoadoutData{id="Paladin_Common",secondSkill=1},new HeroLoadoutData{id="Rogue_Common",firstSkill=0,secondSkill=1}};
  Check(pendingReload.RestoreProgress(reserveSave) && pendingReload.EquipHero("Rogue_Common"));
  Check(pendingReload.HeroProgress("Knight_Common").Level==2 && pendingReload.HeroProgress("Knight_Common").Shards==17);
  Check(pendingReload.EquipHero("Knight_Common") && pendingReload.Party[0].Level==2 && pendingReload.Party[0].Experience==40);
  var replayGrowth=upgradeBattle.ExportProgress(); replayGrowth.encounter=30; replayGrowth.pendingVictory=false;
  Check(growthReload.RestoreProgress(replayGrowth) && growthReload.ChapterIndex==6);
  Check(growthReload.Party[0].UpgradeRank==5 && growthReload.Party[0].Shards==cappedShards);
  var bossXP=new Battle(true); var bossXPsave=bossXP.ExportProgress(); bossXPsave.encounter=14;
  Check(bossXP.RestoreProgress(bossXPsave)); bossXP.Enemies[0].Health=bossXP.Enemies[1].Health=1;
  bossXP.Attack(); bossXP.BeginStrike(); Check(bossXP.Defend(true,.1f) && bossXP.Current==Phase.Won);
  Check(bossXP.ContinueAfterVictory() && bossXP.HeroProgress("Knight_Common").Experience==80);
  Check(GearDefinition.Catalog.Count==90);
  Check(GearDefinition.Drop(true,0,0).Quality==HeroQuality.Rare && GearDefinition.Drop(true,.99,17).Quality==HeroQuality.Legendary);
  Check(GearDefinition.Drop(false,.54,0).Quality==HeroQuality.Common && GearDefinition.Drop(false,.995,0).Quality==HeroQuality.Legendary);
  var gearBattle=new Battle(true); gearBattle.Enemies[0].Health=1; gearBattle.Attack();
  Check(!gearBattle.ContinueAfterVictory(double.NaN,0));
  Check(gearBattle.ContinueAfterVictory(.1,0));
  string weaponId="Weapon_Common_Physical",armorId="Armor_Common_Physical",sigilId="Accessory_Common_Fire";
  Check(gearBattle.LastLoot.Id==weaponId && gearBattle.GearCount(weaponId)==1);
  Check(!gearBattle.ContinueAfterVictory(.1,0) && gearBattle.GearCount(weaponId)==1);
  Check(gearBattle.EquipGear("Knight_Common",GearSlot.Weapon,weaponId));
  Check(gearBattle.Party[0].Definition.AttackDamage==31 && gearBattle.Party[0].Skill(0).Damage==43 && gearBattle.Party[0].Ultimate.Damage==103);
  Check(!gearBattle.EquipGear("Paladin_Common",GearSlot.Weapon,weaponId));
  Check(!gearBattle.EquipGear("Knight_Common",GearSlot.Armor,weaponId));
  var gearSave=gearBattle.ExportProgress(); gearSave.recruited=new[]{"Knight_Common","Paladin_Common"}; gearSave.loadouts=new[]{gearSave.loadouts[0],new HeroLoadoutData{id="Paladin_Common",secondSkill=1}}; gearSave.inventory=new[]{new GearStackData{id=weaponId,count=1},new GearStackData{id=armorId,count=1},new GearStackData{id=sigilId,count=1}};
  Check(gearBattle.RestoreProgress(gearSave));
  Check(gearBattle.EquipGear("Knight_Common",GearSlot.Armor,armorId) && gearBattle.EquipGear("Knight_Common",GearSlot.Accessory,sigilId));
  Check(gearBattle.Party[0].Health==120 && gearBattle.Party[0].Protection==2 && Math.Abs(gearBattle.Party[0].DamageBoost(Element.Fire)-1.06f)<.001f);
  Check(gearBattle.Party[0].DamageBoost(Element.Cold)==1);
  int gearEnemyBefore=gearBattle.EnemyHealth; Check(gearBattle.Attack());
  Check(gearEnemyBefore-gearBattle.EnemyHealth==(int)Math.Ceiling(gearBattle.Enemy.Damage(31,Element.Fire)*1.06f));
  Check(!gearBattle.EquipGear("Knight_Common",GearSlot.Weapon,null));
  gearBattle.Attack(); gearBattle.BeginStrike(); gearBattle.FinishStrike();
  Check(gearBattle.LastIncomingDamage[0]==33);
  var gearLoaded=new Battle(true); Check(gearLoaded.RestoreProgress(gearBattle.ExportProgress()));
  Check(gearLoaded.Party[0].Gear(GearSlot.Armor).Id==armorId && gearLoaded.GearCount(weaponId)==1);
  Check(gearLoaded.EquipGear("Knight_Common",GearSlot.Weapon,null) && gearLoaded.Party[0].Definition.AttackDamage==25);
  Check(gearLoaded.EquipGear("Paladin_Common",GearSlot.Weapon,weaponId));
  var invalidGear=gearLoaded.ExportProgress(); invalidGear.loadouts[0].gear[0]=weaponId;
  Check(!gearLoaded.RestoreProgress(invalidGear) && gearLoaded.Party[0].Gear(GearSlot.Weapon)==null);
  invalidGear=gearLoaded.ExportProgress(); invalidGear.inventory[0].count=0; Check(!gearLoaded.RestoreProgress(invalidGear));
  var oldGearSave=gearLoaded.ExportProgress(); oldGearSave.version=3;
  var oldGearReload=new Battle(true); Check(oldGearReload.RestoreProgress(oldGearSave) && oldGearReload.GearCount(weaponId)==0 && oldGearReload.Party[0].Gear(GearSlot.Armor)==null);
  Check(bossXP.LastLoot!=null && bossXP.LastLoot.Quality>=HeroQuality.Rare && bossXP.GearCount(bossXP.LastLoot.Id)==1);
  var gearReplay=gearLoaded.ExportProgress(); gearReplay.encounter=30; gearReplay.pendingVictory=false;
  Check(gearLoaded.RestoreProgress(gearReplay) && gearLoaded.ChapterIndex==6);
  Check(gearLoaded.GearCount(weaponId)==1 && gearLoaded.HeroProgress("Paladin_Common").Gear(GearSlot.Weapon).Id==weaponId);
  // Results count actual damage/healing, preview levels without granting rewards, and survive reload.
  var resultBattle=new Battle(true); resultBattle.Party[0].GainExperience(90);
  resultBattle.Enemies[0].Health=1; Check(resultBattle.Attack());
  var preview=resultBattle.Results; Check(preview!=null && preview.Heroes.Count==1 && preview.Heroes[0].Damage==1 && preview.Experience==40);
  Check(preview.Heroes[0].LevelBefore==1 && preview.Heroes[0].LevelAfter==2 && preview.Heroes[0].ExperienceAfter==30);
  Check(resultBattle.Party[0].Level==1 && resultBattle.Crystals==300 && resultBattle.GearCount(preview.Loot.Id)==0);
  var resultReload=new Battle(true); Check(resultReload.RestoreProgress(resultBattle.ExportProgress()));
  Check(resultReload.Results.Loot.Id==preview.Loot.Id && resultReload.Results.Heroes[0].Damage==1);
  Check(resultReload.ContinueAfterVictory() && resultReload.LastLoot.Id==preview.Loot.Id && resultReload.GearCount(preview.Loot.Id)==1);
  Check(resultReload.Party[0].Level==preview.Heroes[0].LevelAfter && resultReload.Party[0].Experience==preview.Heroes[0].ExperienceAfter);
  Check(!resultReload.ContinueAfterVictory() && resultReload.Crystals==350 && resultReload.Results==null);
  var badResult=resultBattle.ExportProgress(); badResult.results.damage[0]=-1;
  Check(!resultReload.RestoreProgress(badResult) && resultReload.Encounter==1);
  var healStats=new Battle(HeroClass.Cleric); healStats.Party[0].Health=90;
  Check(healStats.UseAbility() && healStats.Party[0].EncounterHealing==5);
  healStats.BeginStrike(); healStats.Defend(true,.1f); healStats.FinishStrike();
  Check(healStats.Party[0].EncounterDamage==20);
  healStats.Reset(); Check(healStats.Party[0].EncounterDamage==0 && healStats.Party[0].EncounterHealing==0);
  var limits=new Battle(true);
  for(int encounter=0;encounter<10;encounter++) {
   Check(limits.PartyLimit==(encounter<5?1:2) && limits.Party.Count==limits.PartyLimit);
   // Finish via public attacks with every affected hero defending successfully.
   while(limits.Current!=Phase.Won) {
    if(limits.Current==Phase.Player) limits.Attack();
    else { limits.BeginStrike(); for(int heroIndex=0;heroIndex<limits.Party.Count;heroIndex++) limits.DefendHero(heroIndex,false,.1f); limits.FinishStrike(); }
   }
   Check(limits.ContinueAfterVictory());
  }
  Check(limits.PartyLimit==3 && limits.Party.Count==3 && limits.Recruits.Count==4);
  var limitedSave=limits.ExportProgress(); limitedSave.encounter=0;
  Check(!new Battle(true).RestoreProgress(limitedSave));
  limitedSave.version=4; var legacyLimits=new Battle(true);
  Check(legacyLimits.RestoreProgress(limitedSave) && legacyLimits.Party.Count==1 && legacyLimits.Recruits.Count==4);
  Check(legacyLimits.HeroProgress(limitedSave.party[1]).Identity.Id==limitedSave.party[1]);
  var visualCoverageNames=new System.Collections.Generic.HashSet<string>();
  var enemyPaths=new System.Collections.Generic.HashSet<string>();
  var sources=new System.Collections.Generic.HashSet<string>(new[]{"LanternWarden","AshHound","CinderScout","IronWatcher","Roadkeeper","HollowMatron"});
  Check(EnemyVisualDefinition.Catalog.Count==70 && ChapterStageDefinition.Catalog.Length==12);
  foreach(var visual in EnemyVisualDefinition.Catalog) Check(visualCoverageNames.Add(visual.EnemyName) && enemyPaths.Add(visual.ResourcePath) && sources.Contains(visual.SourceModel));
  for(int encounter=0;encounter<ChapterDefinition.TotalStages;encounter++) foreach(var campaignEnemy in ChapterDefinition.EnemiesAt(encounter)) {
   var visual=EnemyVisualDefinition.For(campaignEnemy.Name);
   Check(visual!=null && visual.IsBoss==campaignEnemy.IsBoss && visual.Height>0 && visual.Height<=2.8f);
  }
  var skies=new System.Collections.Generic.HashSet<string>();
  for(int chapter=0;chapter<ChapterStageDefinition.Catalog.Length;chapter++) {
   var stage=ChapterStageDefinition.For(chapter);
   Check(skies.Add(stage.Sky) && stage.Fog>0 && stage.Fog<=.04f && stage.Stone.StartsWith("#") && stage.Flame.StartsWith("#"));
  }
  // Unity's serializer can represent null string slots as empty strings and null inline objects as empty objects.
  var unityShape=new Battle(true).ExportProgress();
  foreach(var loadout in unityShape.loadouts) for(int slot=0;slot<3;slot++) if(loadout.gear[slot]==null) loadout.gear[slot]="";
  unityShape.results=new PendingResultsData {lootId="",damage=new int[0],healing=new int[0]};
  Check(new Battle(true).RestoreProgress(unityShape));
  var unityPending=resultBattle.ExportProgress();
  foreach(var loadout in unityPending.loadouts) for(int slot=0;slot<3;slot++) if(loadout.gear[slot]==null) loadout.gear[slot]="";
  var unityResume=new Battle(true); Check(unityResume.RestoreProgress(unityPending) && unityResume.Results.Loot.Id==resultBattle.Results.Loot.Id);
  Check(unityResume.ContinueAfterVictory() && unityResume.GearCount(resultBattle.Results.Loot.Id)==1);
  unityShape.results=null; Check(new Battle(true).RestoreProgress(unityShape));
  unityShape.results=new PendingResultsData(); Check(new Battle(true).RestoreProgress(unityShape));
  unityShape.results=new PendingResultsData{lootId="Weapon_Common_Physical"}; Check(!new Battle(true).RestoreProgress(unityShape));
  unityShape.results=new PendingResultsData{damage=new[]{1}}; Check(!new Battle(true).RestoreProgress(unityShape));
  unityShape.results=null; unityShape.loadouts[0].gear[0]="missing_item"; Check(!new Battle(true).RestoreProgress(unityShape));
  unityShape.loadouts[0].gear[0]=" "; Check(!new Battle(true).RestoreProgress(unityShape));
  // Boss support actions spend their turn without attacking or accepting defensive counters.
  var charge=BossBattle(14); charge.Enemies[0].CompletedStrikes=2;
  Check(charge.Attack() && charge.IncomingSkill.Action==EnemyAction.Charge);
  Check(charge.BeginStrike() && !charge.Defend(true,.1f)); charge.FinishStrike();
  Check(charge.Enemies[0].Charged && charge.LastMechanicTarget==0 && charge.LastIncomingDamage[0]==0);
  ResolveEnemies(charge);
  charge.Enemies[0].ChargeDamage=59;
  Check(AutoBattlePlanner.ChooseTarget(charge)==0 && charge.Attack());
  Check(!charge.Enemies[0].Charged && charge.IncomingSkill.Damage==20 && charge.IncomingSkill.Status==Debuff.None);
  ResolveEnemies(charge); charge.Reset();
  Check(!charge.Enemies[0].Charged && charge.Enemies[0].ChargeDamage==0);
  charge.Enemies[0].CompletedStrikes=2; charge.Attack(); ResolveEnemies(charge);
  Check(charge.Enemies[0].Charged);
  charge.SelectEnemy(1); charge.Attack();
  Check(charge.IncomingSkill.Damage==70 && charge.IncomingSkill.Status==Debuff.Burn);
  ResolveEnemies(charge); Check(!charge.Enemies[0].Charged);

  var healer=BossBattle(24); healer.Enemies[0].CompletedStrikes=1;
  healer.Enemies[1].Health-=10;
  Check(healer.Attack() && healer.IncomingSkill.Action==EnemyAction.HealPartner);
  healer.BeginStrike(); Check(!healer.Defend(false,.1f)); healer.FinishStrike();
  Check(healer.LastMechanicTarget==1 && healer.LastEnemyHealing==10 && healer.Enemies[1].Health==healer.Enemies[1].Definition.MaxHealth);
  Check(AutoBattlePlanner.ChooseTarget(healer)==0); ResolveEnemies(healer);
  healer.Enemies[1].Health=0; healer.Enemies[0].CompletedStrikes=4;
  healer.Attack(); healer.BeginStrike(); healer.FinishStrike();
  Check(healer.LastMechanicTarget==0 && healer.LastEnemyHealing>0 && healer.Enemies[1].Health==0);

  var shield=BossBattle(29); shield.Enemies[0].CompletedStrikes=1;
  shield.Attack(); shield.BeginStrike(); shield.FinishStrike();
  Check(shield.LastMechanicTarget==1 && shield.Enemies[1].Shield==60 && shield.Enemies[0].Shield==0);
  Check(AutoBattlePlanner.ChooseTarget(shield)==0); ResolveEnemies(shield);
  shield.SelectEnemy(1); int shieldHealth=shield.Enemies[1].Health;
  int recorded=shield.Party[0].EncounterDamage;
  shield.Attack(); Check(shield.Enemies[1].Health==shieldHealth && shield.Enemies[1].Shield<60);
  Check(shield.LastDamage==0 && shield.LastShieldAbsorbed>0 && shield.Party[0].EncounterDamage==recorded);
  ResolveEnemies(shield); shield.Enemies[0].CompletedStrikes=4;
  shield.Attack(); shield.BeginStrike(); shield.FinishStrike();
  Check(shield.LastMechanicTarget==0 && shield.Enemies[0].Shield==60 && shield.Enemies[1].Shield==0);
  Check(AutoBattlePlanner.ChooseTarget(shield)==1); ResolveEnemies(shield);
  shield.Reset(); Check(shield.Enemies[0].Shield==0 && shield.Enemies[1].Shield==0);
  // A real three-hero turn accumulates damage across actions to interrupt the charge.
  var teamwork=new Battle(true); var teamSave=teamwork.ExportProgress(); teamSave.encounter=14;
  teamSave.recruited=new[]{"Knight_Common","Rogue_Common","Sorceress_Common"}; teamSave.party=teamSave.recruited;
  teamSave.loadouts=groupSave.loadouts;
  Check(teamwork.RestoreProgress(teamSave)); teamwork.Enemies[0].CompletedStrikes=2;
  for(int i=0;i<3;i++) Check(teamwork.Attack()); ResolveEnemies(teamwork);
  Check(teamwork.Enemies[0].Charged && teamwork.Enemies[0].ChargeDamage==0);
  for(int i=0;i<3;i++) Check(teamwork.Attack());
  Check(teamwork.Enemies[0].ChargeDamage>=60 && !teamwork.Enemies[0].Charged && teamwork.IncomingSkill.Damage==20);
  // Ultimates absorb the remaining shield before health damage, and caster death breaks the veil.
  shield.Enemies[1].Shield=60; shield.SelectEnemy(1); shield.Party[0].Energy=75;
  shieldHealth=shield.Enemies[1].Health; recorded=shield.Party[0].EncounterDamage;
  Check(shield.Attack() && shield.LastUltimate!=null && shield.Enemies[1].Shield==0);
  Check(shieldHealth-shield.Enemies[1].Health==shield.Party[0].EncounterDamage-recorded);
  ResolveEnemies(shield); shield.Enemies[1].Shield=60; shield.Enemies[0].Health=1;
  shield.SelectEnemy(0); Check(shield.Attack() && shield.Enemies[0].Health==0 && shield.Enemies[1].Shield==0);
  // The game opts into waves; single-wave battles above retain coverage of the original combat rules.
  var waves=new Battle(true,enableWaves:true);
  Check(waves.WaveCount==3 && waves.WaveIndex==0 && !waves.AdvanceWave());
  waves.Party[0].Health=77; waves.Party[0].Energy=25; waves.Enemies[0].Health=1;
  Check(waves.UseAbility(0) && waves.Current==Phase.WaveCleared);
  int carriedHealth=waves.HeroHealth,carriedEnergy=waves.Party[0].Energy,carriedCharges=waves.Party[0].SkillCharges(0);
  int carriedDamage=waves.Party[0].EncounterDamage; waves.Party[0].Guard=9;
  waves.Party[0].Status=Debuff.Shock; waves.Party[0].StatusRounds=2;
  Check(waves.Results==null && waves.Crystals==Summoning.StarterCrystals && !waves.ContinueAfterVictory());
  Check(!waves.Attack() && !waves.UseAbility(1) && !waves.SelectHero(0));
  var betweenWaves=waves.ExportProgress();
  Check(!betweenWaves.pendingVictory && betweenWaves.results==null);
  Check(waves.AdvanceWave() && waves.WaveIndex==1 && waves.Current==Phase.Player && !waves.CanChangeParty);
  Check(waves.HeroHealth==carriedHealth && waves.Party[0].Energy==carriedEnergy && waves.Party[0].SkillCharges(0)==carriedCharges);
  Check(waves.Party[0].Guard==9 && waves.Party[0].Status==Debuff.Shock && waves.Party[0].StatusRounds==2);
  Check(waves.Party[0].EncounterDamage==carriedDamage && !waves.Party[0].Acted && !waves.AdvanceWave());
  var waveReload=new Battle(true,enableWaves:true);
  Check(waveReload.RestoreProgress(betweenWaves) && waveReload.WaveIndex==0 && waveReload.HeroHealth==waveReload.Hero.MaxHealth);
  waves.Reset(); Check(waves.WaveIndex==0 && waves.CanChangeParty && waves.Party[0].Energy==0);
  var fallenWave=new Battle(true,enableWaves:true); var fallenSave=fallenWave.ExportProgress();
  fallenSave.encounter=14; fallenSave.recruited=teamSave.recruited; fallenSave.party=teamSave.party; fallenSave.loadouts=teamSave.loadouts;
  Check(fallenWave.RestoreProgress(fallenSave)); fallenWave.Party[0].Health=0;
  Check(fallenWave.SelectHero(1)); fallenWave.Party[1].Energy=75; fallenWave.Enemies[0].Health=1;
  Check(fallenWave.Attack() && fallenWave.Current==Phase.WaveCleared && fallenWave.Party[1].Energy==100);
  Check(fallenWave.AdvanceWave() && fallenWave.Party[0].Health==0 && fallenWave.ActiveIndex==1);
  Check(fallenWave.Party[1].Energy==100 && fallenWave.Attack() && fallenWave.LastUltimate!=null && fallenWave.Party[1].Energy==0);
  // Complete all 180 waves with public actions and defend every affected living hero.
  var waveCampaign=new Battle(true,enableWaves:true); int clearedWaves=0,steps=0;
  while(!waveCampaign.CampaignComplete && steps++<6000) {
   if(waveCampaign.Current==Phase.Player) {
    waveCampaign.SelectEnemy(AutoBattlePlanner.ChooseTarget(waveCampaign));
    int planned=AutoBattlePlanner.ChooseSkill(waveCampaign);
    Check(planned<0?waveCampaign.Attack():waveCampaign.UseAbility(planned));
   } else if(waveCampaign.Current==Phase.EnemyWindup) {
    Check(waveCampaign.BeginStrike());
    if(waveCampaign.IncomingSkill.Action==EnemyAction.Attack)
     for(int member=0;member<waveCampaign.Party.Count;member++)
      if(waveCampaign.Party[member].Health>0 && (waveCampaign.IncomingSkill.PartyWide || member==waveCampaign.ActiveIndex))
       Check(waveCampaign.DefendHero(member,false,.1f));
    waveCampaign.FinishStrike();
   } else if(waveCampaign.Current==Phase.WaveCleared) {
    clearedWaves++; Check(waveCampaign.Results==null && waveCampaign.AdvanceWave());
   } else if(waveCampaign.Current==Phase.Won) {
    clearedWaves++; Check(waveCampaign.WaveIndex==2 && waveCampaign.Results!=null);
    if(waveCampaign.StageIndex==4) Check(waveCampaign.Enemies[0].Definition.IsBoss);
    var savedWaveVictory=waveCampaign.ExportProgress(); var pendingWave=new Battle(true,enableWaves:true);
    Check(pendingWave.RestoreProgress(savedWaveVictory) && pendingWave.Current==Phase.Won && pendingWave.WaveIndex==2);
    Check(pendingWave.Results.Loot.Id==waveCampaign.Results.Loot.Id);
    int previousCrystals=pendingWave.Crystals;
    Check(pendingWave.ContinueAfterVictory() && pendingWave.Encounter==savedWaveVictory.encounter+1 && pendingWave.Crystals==previousCrystals+(savedWaveVictory.encounter/5>=6?50+savedWaveVictory.encounter/5*5+(savedWaveVictory.encounter%5==4?25:0):50));
    Check(!pendingWave.ContinueAfterVictory());
    Check(waveCampaign.ContinueAfterVictory());
   } else throw new Exception("Unexpected wave campaign phase: "+waveCampaign.Current);
  }
  Check(waveCampaign.CampaignComplete && clearedWaves==ChapterDefinition.TotalStages*3 && waveCampaign.Recruits.Count==10);
  for(int stage=0;stage<ChapterDefinition.TotalStages;stage++) {
   for(int wave=0;wave<3;wave++) {
    var group=ChapterDefinition.WaveAt(stage,wave,3);
    Check(group.Count>=1 && group.Count<=Battle.MaxEnemies);
    foreach(var member in group) Check(EnemyVisualDefinition.For(member.Name)!=null && (wave==2 || !member.IsBoss));
   }
  }
  // Five-hero formations persist through saves without changing named hero progression.
  var formation=new Battle(true); var formationSave=formation.ExportProgress(); formationSave.encounter=24;
  formationSave.recruited=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"};
  formationSave.party=(string[])formationSave.recruited.Clone(); formationSave.loadouts=new HeroLoadoutData[5];
  for(int i=0;i<5;i++) formationSave.loadouts[i]=new HeroLoadoutData{id=formationSave.recruited[i],firstSkill=0,secondSkill=1};
  Check(formation.RestoreProgress(formationSave) && formation.PartyLimit==5 && formation.Party.Count==5);
  Check(formation.IsFrontRow(0) && formation.IsFrontRow(1) && !formation.IsFrontRow(2) && !formation.IsFrontRow(4) && !formation.IsFrontRow(-1));
  Check(!formation.SwapFormation(-1,0) && !formation.SwapFormation(0,5) && !formation.SwapFormation(0,0));
  Check(formation.SelectHero(0) && formation.SwapFormation(0,4));
  Check(formation.ActiveIndex==4 && formation.Party[4].Identity.Id=="Knight_Common" && formation.Party[0].Identity.Id=="Cleric_Common");
  var formationReload=new Battle(true); Check(formationReload.RestoreProgress(formation.ExportProgress()));
  for(int i=0;i<5;i++) Check(formationReload.Party[i].Identity.Id==formation.Party[i].Identity.Id);
  Check(formation.Enemies[0].Definition.MaxHealth==1167 && formation.Enemies[1].Definition.MaxHealth==817);
  var fourHeroes=BossBattle(19);
  Check(fourHeroes.PartyLimit==4 && fourHeroes.Enemies[0].Definition.MaxHealth==767 && fourHeroes.Enemies[1].Definition.MaxHealth==536);
  // Older three-hero saves can fill newly unlocked slots from reserves before combat.
  var oldTeam=formationSave; oldTeam.party=new[]{"Knight_Common","Paladin_Common","Sorceress_Common"};
  Check(formationReload.RestoreProgress(oldTeam) && formationReload.Party.Count==3);
  Check(formationReload.FillAvailableSlots() && formationReload.Party.Count==5 && !formationReload.FillAvailableSlots());
  Check(formationReload.Attack() && !formationReload.SwapFormation(0,4) && !formationReload.FillAvailableSlots());
  // A normal strike cycle allocates three tickets per front hero, one per back hero.
  int frontTargets=0,backTargets=0;
  for(int round=0;round<9;round++) {
   formation.Enemies[0].CompletedStrikes=0; formation.Enemies[1].CompletedStrikes=0;
   foreach(var foe in formation.Enemies) foe.Health=foe.Definition.MaxHealth;
   for(int action=0;action<5;action++) Check(formation.Attack());
   for(int foeIndex=0;foeIndex<2;foeIndex++) {
    if(formation.IsFrontRow(formation.ActiveIndex)) frontTargets++; else backTargets++;
    Check(formation.BeginStrike() && formation.Defend(false,.1f)); formation.FinishStrike();
   }
  }
  Check(frontTargets==12 && backTargets==6 && formation.Party[4].Energy>=0);
  // Heavy attacks reach the back row, then fall back to living front heroes.
  var backStrike=new Battle(true); var backSave=formationSave; backSave.party=(string[])backSave.recruited.Clone(); backSave.encounter=20;
  Check(backStrike.RestoreProgress(backSave)); backStrike.Enemies[0].CompletedStrikes=2;
  for(int i=0;i<5;i++) Check(backStrike.Attack());
  Check(backStrike.IncomingSkill.BackRow && backStrike.ActiveIndex>=Battle.FrontSlots);
  Check(backStrike.BeginStrike() && backStrike.Defend(false,.1f)); backStrike.FinishStrike();
  for(int i=2;i<5;i++) backStrike.Party[i].Health=0;
  backStrike.Enemies[0].CompletedStrikes=2; Check(backStrike.Attack() && backStrike.Attack());
  Check(backStrike.IncomingSkill.BackRow && backStrike.IsFrontRow(backStrike.ActiveIndex));
  // Party-wide damage, support ultimates and results include the fourth and fifth heroes.
  formation.Reset(); formation.Enemies[0].CompletedStrikes=2; formation.Enemies[1].CompletedStrikes=2;
  for(int i=0;i<5;i++) Check(formation.Attack());
  Check(formation.IncomingSkill.PartyWide && formation.BeginStrike());
  int fourthHealth=formation.Party[3].Health,fifthHealth=formation.Party[4].Health;
  Check(formation.DefendHero(4,false,.1f)); formation.FinishStrike();
  Check(formation.Party[3].Health<fourthHealth && formation.Party[4].Health==fifthHealth);
  ResolveEnemies(formation); formation.Reset();
  formation.Party[0].Health-=30; formation.Party[4].Health-=30; formation.Party[0].Energy=75;
  Check(formation.Attack() && formation.LastUltimate!=null && formation.LastUltimateHealing[4]>0);
  Check(formation.Party[4].Health>formation.Party[4].Definition.MaxHealth-30);
  formation.Reset(); foreach(var foe in formation.Enemies) foe.Health=1;
  Check(formation.SelectEnemy(0) && formation.Attack());
  Check(formation.SelectEnemy(1) && formation.Attack() && formation.Current==Phase.Won && formation.Results.Heroes.Count==5);
  var fiveResults=new Battle(true); Check(fiveResults.RestoreProgress(formation.ExportProgress()) && fiveResults.Results.Heroes.Count==5);
  Check(fiveResults.ContinueAfterVictory() && fiveResults.Party.Count==5);
  // Offensive passives are player-action bonuses; counters retain their original damage.
  var arcane=PassiveBattle(10,new[]{"Sorceress_Common"});
  double arcaneBonus=arcane.PassiveDamageMultiplier(arcane.Party[0],arcane.Enemy,arcane.Party[0].Identity.Affinity);
  int arcaneExpected=(int)Math.Ceiling(arcane.Enemy.Damage(arcane.Hero.AttackDamage,arcane.Party[0].Identity.Affinity)*arcaneBonus);
  Check(arcaneBonus==1.2 && arcane.Attack() && arcane.LastDamage==54 && arcane.LastDamage==arcaneExpected && arcane.LastOffensivePassive=="ARCANE EXPOSURE");
  arcane.Reset(); arcane.EquipSkill(0,4); var elementalSkill=arcane.Party[0].Skill(0);
  Element skillAffinity=elementalSkill.Affinity ?? arcane.Party[0].Identity.Affinity;
  arcaneExpected=(int)Math.Ceiling(arcane.Enemy.Damage(elementalSkill.Damage,skillAffinity)*arcane.PassiveDamageMultiplier(arcane.Party[0],arcane.Enemy,skillAffinity));
  Check(arcane.UseAbility() && arcane.LastDamage==arcaneExpected);
  Check(arcane.PassiveDamageMultiplier(arcane.Party[0],new EnemyDefinition("Test",100,Element.Fire,Element.Cold),Element.Fire)==1.2);
  Check(arcane.PassiveDamageMultiplier(arcane.Party[0],new EnemyDefinition("Test",100,Element.Fire,Element.Cold),Element.Cold)==1);
  var hunter=PassiveBattle(2,new[]{"Ranger_Common"});
  int hunterBase=hunter.Enemy.Damage(hunter.Hero.AttackDamage,hunter.Party[0].Identity.Affinity);
  Check(hunter.Attack() && hunter.LastDamage==(int)Math.Ceiling(hunterBase*1.2) && hunter.LastOffensivePassive=="HUNTER'S MARK");
  var hunterMelee=PassiveBattle(0,new[]{"Ranger_Common"});
  hunterBase=hunterMelee.Enemy.Damage(hunterMelee.Hero.AttackDamage,hunterMelee.Party[0].Identity.Affinity);
  Check(hunterMelee.Attack() && hunterMelee.LastDamage==hunterBase && hunterMelee.LastOffensivePassive==null);
  var critical=PassiveBattle(5,new[]{"Rogue_Common"},.249);
  int criticalBase=critical.Enemy.Damage(critical.Hero.AttackDamage,critical.Party[0].Identity.Affinity);
  Check(critical.Attack() && critical.LastDamage==(int)Math.Ceiling(criticalBase*1.5) && critical.LastOffensivePassive=="CRITICAL STRIKE");
  critical.BeginStrike(); Check(critical.Defend(true,.1f));
  Check(critical.LastDamage==critical.Enemy.Damage(10,critical.Party[0].Identity.Affinity)); critical.FinishStrike();
  var ordinary=PassiveBattle(5,new[]{"Rogue_Common"},.25);
  Check(ordinary.Attack() && ordinary.LastDamage==criticalBase && ordinary.LastOffensivePassive==null);
  critical.Reset(); critical.Party[0].Energy=75;
  Check(critical.Attack() && critical.LastUltimate!=null && critical.LastUltimateDamage[0]>0 && critical.Party[0].EncounterDamage==critical.LastDamage+critical.LastUltimateDamage[0]);
  // Knight protection works only from the front, and never stacks or protects itself.
  var vanguard=PassiveBattle(14,new[]{"Knight_Common","Sorceress_Common","Cleric_Common"});
  for(int i=0;i<3;i++) Check(vanguard.Attack());
  Check(vanguard.BeginStrike()); vanguard.FinishStrike();
  Check(vanguard.LastIncomingDamage[0]==35 && vanguard.LastPassiveProtection[0]==0);
  Check(vanguard.BeginStrike()); vanguard.FinishStrike();
  Check(vanguard.LastIncomingDamage[1]==28 && vanguard.LastPassiveProtection[1]==7);
  vanguard.Reset(); Check(vanguard.SwapFormation(0,2));
  for(int i=0;i<3;i++) Check(vanguard.Attack()); vanguard.BeginStrike(); vanguard.FinishStrike();
  vanguard.BeginStrike(); vanguard.FinishStrike(); Check(vanguard.LastIncomingDamage[1]==35 && vanguard.LastPassiveProtection[1]==0);
  // Paladins raise Guard to eight once per enemy round and preserve stronger skill guards.
  var aegis=PassiveBattle(14,new[]{"Knight_Common","Paladin_Common","Cleric_Common"}); aegis.Party[0].Guard=20;
  for(int i=0;i<3;i++) Check(aegis.Attack());
  Check(aegis.Party[0].Guard==20 && aegis.Party[1].Guard==8 && aegis.Party[2].Guard==8);
  Check(aegis.LastPassiveGuard[0]==0 && aegis.LastPassiveGuard[2]==8);
  aegis.BeginStrike(); aegis.FinishStrike(); Check(aegis.Party[0].Guard==0);
  aegis.BeginStrike(); aegis.FinishStrike(); Check(aegis.Party[0].Guard==0);
  aegis.Reset(); aegis.Party[1].Health=0; Check(aegis.Attack() && aegis.Attack());
  Check(aegis.Party[0].Guard==0 && aegis.LastPassiveGuard[0]==0);
  // Clerics heal by health percentage, cap actual healing and never revive fallen allies.
  var prayer=PassiveBattle(14,new[]{"Knight_Common","Paladin_Common","Cleric_Common"});
  prayer.Party[0].Health=50; prayer.Party[1].Health=50;
  for(int i=0;i<3;i++) Check(prayer.Attack());
  prayer.BeginStrike(); prayer.Defend(false,.1f); prayer.FinishStrike();
  Check(prayer.LastPassiveHealing[1]==0); prayer.BeginStrike(); prayer.Defend(false,.1f); prayer.FinishStrike();
  Check(prayer.LastPassiveHealing[1]==10 && prayer.Party[1].Health==60 && prayer.Party[0].Health==50 && prayer.Party[2].EncounterHealing==10);
  prayer.Reset(); prayer.Party[0].Health=0; prayer.Party[1].Health=prayer.Party[1].Definition.MaxHealth-3;
  Check(prayer.SelectHero(1) && prayer.Attack() && prayer.Attack()); ResolveEnemies(prayer);
  Check(prayer.Party[0].Health==0 && prayer.LastPassiveHealing[1]==3 && prayer.Party[2].EncounterHealing==3);
  prayer.Reset(); prayer.Party[2].Health=0; prayer.Party[0].Health=20;
  Check(prayer.Attack() && prayer.Attack()); ResolveEnemies(prayer);
  Check(prayer.Party[0].Health==20 && prayer.LastPassiveHealing[0]==0);
  // A lethal area hit snapshots Knight protection before resolving all hero damage.
  vanguard=PassiveBattle(14,new[]{"Knight_Common","Sorceress_Common","Cleric_Common"}); vanguard.Enemies[0].Health=0; vanguard.Enemies[1].CompletedStrikes=2; vanguard.Party[0].Health=1;
  for(int i=0;i<3;i++) Check(vanguard.Attack());
  Check(vanguard.IncomingSkill.PartyWide && vanguard.BeginStrike()); vanguard.FinishStrike();
  Check(vanguard.Party[0].Health==0 && vanguard.LastIncomingDamage[1]==18 && vanguard.LastPassiveProtection[1]==4);
  var duplicateAegis=PassiveBattle(24,new[]{"Knight_Common","Paladin_Common","Paladin_Uncommon"});
  for(int i=0;i<3;i++) Check(duplicateAegis.Attack());
  Check(duplicateAegis.Party[0].Guard==8 && duplicateAegis.Party[1].Guard==8 && duplicateAegis.Party[2].Guard==8);
  // With passives enabled, public actions still finish the entire 90-wave campaign.
  var passiveCampaign=new Battle(true,enableWaves:true,enableClassPassives:true,criticalRandom:new Random(123)); int passiveSteps=0,passiveWaves=0;
  while(!passiveCampaign.CampaignComplete && passiveSteps++<6000) {
   if(passiveCampaign.Current==Phase.Player) {
    passiveCampaign.SelectEnemy(AutoBattlePlanner.ChooseTarget(passiveCampaign));
    int pick=AutoBattlePlanner.ChooseSkill(passiveCampaign); Check(pick<0?passiveCampaign.Attack():passiveCampaign.UseAbility(pick));
   } else if(passiveCampaign.Current==Phase.EnemyWindup) {
    passiveCampaign.BeginStrike();
    if(passiveCampaign.IncomingSkill.Action==EnemyAction.Attack)
     for(int i=0;i<passiveCampaign.Party.Count;i++) if(passiveCampaign.Party[i].Health>0 && (passiveCampaign.IncomingSkill.PartyWide || i==passiveCampaign.ActiveIndex)) Check(passiveCampaign.DefendHero(i,false,.1f));
    passiveCampaign.FinishStrike();
   } else if(passiveCampaign.Current==Phase.WaveCleared) { passiveWaves++; Check(passiveCampaign.AdvanceWave()); }
   else if(passiveCampaign.Current==Phase.Won) { passiveWaves++; Check(passiveCampaign.ContinueAfterVictory()); }
   else throw new Exception("Unexpected passive campaign phase: "+passiveCampaign.Current);
  }
  Check(passiveCampaign.CampaignComplete && passiveWaves==ChapterDefinition.TotalStages*3 && passiveCampaign.Party.Count==5);
  // Every campaign enemy has a role; mixed waves always pair a defender and rear threat.
  foreach(var visualRole in EnemyVisualDefinition.Catalog) {
   var roleDefinition=new EnemyDefinition(visualRole.EnemyName,100);
   Check(roleDefinition.Row==EnemyRoles.Row(roleDefinition.Role));
   Check(roleDefinition.Row==EnemyRow.Back || roleDefinition.Role==EnemyRole.Defender || roleDefinition.Role==EnemyRole.Bruiser);
  }
  for(int stage=5;stage<ChapterDefinition.TotalStages;stage++) {
   var mixed=ChapterDefinition.WaveAt(stage,1,3);
   Check(mixed.Count>=2 && mixed.Count<=3 && mixed[0].Role==EnemyRole.Defender && mixed[0].Row==EnemyRow.Front && mixed[1].Row==EnemyRow.Back && mixed[0].Name!=mixed[1].Name);
  }
  Check(ChapterDefinition.EnemiesAt(24)[0].Role==EnemyRole.Healer && ChapterDefinition.EnemiesAt(24)[1].Role==EnemyRole.Defender);
  Check(ChapterDefinition.EnemiesAt(29)[0].Role==EnemyRole.Support && ChapterDefinition.EnemiesAt(29)[1].Role==EnemyRole.Defender);
  var guardedBoss=PassiveBattle(24,new[]{"Knight_Common"});
  Check(guardedBoss.EnemyProtected(0) && !guardedBoss.EnemyProtected(1) && !guardedBoss.EnemyProtected(-1));
  int guardedBase=guardedBoss.Enemy.Damage(guardedBoss.Hero.AttackDamage,guardedBoss.Party[0].Identity.Affinity);
  Check(guardedBoss.Attack() && guardedBoss.LastDamage==(int)Math.Ceiling(guardedBase*.8));
  guardedBoss.BeginStrike(); Check(guardedBoss.Defend(true,.1f) && guardedBoss.LastDamage==8); guardedBoss.FinishStrike(); ResolveEnemies(guardedBoss);
  Check(guardedBoss.SelectEnemy(1)); guardedBoss.Enemies[1].Health=1; Check(guardedBoss.Attack());
  Check(!guardedBoss.EnemyProtected(0)); ResolveEnemies(guardedBoss);
  guardedBoss.SelectEnemy(0); Check(guardedBoss.Attack() && guardedBoss.LastDamage==guardedBase);
  var infiltrator=PassiveBattle(24,new[]{"Rogue_Common"},.99);
  int bypassBase=infiltrator.Enemy.Damage(infiltrator.Hero.AttackDamage,infiltrator.Party[0].Identity.Affinity);
  Check(infiltrator.Attack() && infiltrator.LastDamage==bypassBase && infiltrator.LastOffensivePassive=="BYPASS GUARD");
  var rearHunter=PassiveBattle(24,new[]{"Ranger_Common"});
  int rearBase=rearHunter.Enemy.Damage(rearHunter.Hero.AttackDamage,rearHunter.Party[0].Identity.Affinity);
  Check(rearHunter.PassiveDamageMultiplier(rearHunter.Party[0],rearHunter.Enemies[0].Definition,rearHunter.Party[0].Identity.Affinity)==1.2);
  Check(rearHunter.PassiveDamageMultiplier(rearHunter.Party[0],rearHunter.Enemies[1].Definition,rearHunter.Party[0].Identity.Affinity)==1);
  Check(rearHunter.Attack() && rearHunter.LastDamage==(int)Math.Ceiling(Math.Ceiling(rearBase*1.2)*.8));
  var areaBypass=PassiveBattle(24,new[]{"Paladin_Uncommon"}); areaBypass.Party[0].Energy=75;
  int areaExpected=areaBypass.Enemy.Damage(areaBypass.Party[0].Ultimate.Damage,areaBypass.Party[0].Ultimate.Affinity);
  Check(areaBypass.Attack() && areaBypass.LastUltimate.AreaDamage && areaBypass.LastUltimateDamage[0]==areaExpected);
  var singleGuard=PassiveBattle(24,new[]{"Knight_Common"}); singleGuard.Party[0].Energy=75;
  int singleExpected=(int)Math.Ceiling(singleGuard.Enemy.Damage(singleGuard.Party[0].Ultimate.Damage,singleGuard.Party[0].Ultimate.Affinity)*.8);
  Check(singleGuard.Attack() && !singleGuard.LastUltimate.AreaDamage && singleGuard.LastUltimateDamage[0]==singleExpected);
  var shieldAndGuard=PassiveBattle(29,new[]{"Knight_Common"}); shieldAndGuard.Enemies[0].Shield=60;
  int protectedHealth=shieldAndGuard.Enemies[0].Health; Check(shieldAndGuard.Attack());
  Check(shieldAndGuard.Enemies[0].Health==protectedHealth && shieldAndGuard.LastShieldAbsorbed==11 && shieldAndGuard.Enemies[0].Shield==49);
  var roleReload=new Battle(true); Check(roleReload.RestoreProgress(singleGuard.ExportProgress()) && roleReload.EnemyProtected(0));
  Check(roleReload.Enemies[0].Definition.Role==EnemyRole.Healer && roleReload.Enemies[0].Definition.Row==EnemyRow.Back);
  // All 36 upgrade choices are gated by level, exclusive within a tier and free to reset.
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass))) {
   string talentHero=HeroDefinition.Common(kind).Id;
   var locked=TalentBattle(new[]{talentHero},4);
   Check(!locked.ChooseTalent(talentHero,0,0) && !locked.ChooseTalent(talentHero,1,0) && !locked.ChooseTalent(talentHero,2,0));
   for(int tier=0;tier<3;tier++) {
    var atThreshold=TalentBattle(new[]{talentHero},ClassTalents.UnlockLevel(tier)); int currency=atThreshold.Crystals;
    for(int choice=0;choice<2;choice++) {
     Check(!string.IsNullOrEmpty(ClassTalents.Name(kind,tier,choice)) && !string.IsNullOrEmpty(ClassTalents.Description(kind,tier,choice)));
     Check(atThreshold.ChooseTalent(talentHero,tier,choice) && atThreshold.Party[0].TalentChoice(tier)==choice && atThreshold.Party[0].HasTalent(tier,choice));
     Check(!atThreshold.Party[0].HasTalent(tier,1-choice) && !atThreshold.ChooseTalent(talentHero,tier,choice));
    }
    Check(atThreshold.ResetTalents(talentHero) && atThreshold.Party[0].TalentChoice(tier)==-1 && atThreshold.Crystals==currency && !atThreshold.ResetTalents(talentHero));
   }
  }
  var talentedKnight=TalentBattle(new[]{"Knight_Common"});
  Check(talentedKnight.ChooseTalent("Knight_Common",0,1) && talentedKnight.ChooseTalent("Knight_Common",2,1));
  Check(talentedKnight.Attack() && !talentedKnight.ResetTalents("Knight_Common") && !talentedKnight.ChooseTalent("Knight_Common",1,0));
  talentedKnight.BeginStrike(); Check(talentedKnight.Defend(true,.1f) && talentedKnight.LastDamage==20); // 25 Fire counter reduced by rear Defender.
  talentedKnight.FinishStrike(); ResolveEnemies(talentedKnight); talentedKnight.Reset();
  Check(!talentedKnight.ChooseTalent("unknown",0,0) && !talentedKnight.ChooseTalent("Knight_Common",-1,0) && !talentedKnight.ChooseTalent("Knight_Common",0,2));
  var enhancedGuard=TalentBattle(new[]{"Knight_Common","Paladin_Common","Cleric_Common"});
  Check(enhancedGuard.ChooseTalent("Paladin_Common",0,0) && enhancedGuard.ChooseTalent("Paladin_Common",2,0));
  for(int i=0;i<3;i++) Check(enhancedGuard.Attack());
  Check(enhancedGuard.Party[0].Guard==20 && enhancedGuard.LastPassiveGuard[2]==20);
  var fastEnergy=TalentBattle(new[]{"Sorceress_Common"},10,10);
  Check(fastEnergy.ChooseTalent("Sorceress_Common",1,0) && fastEnergy.Party[0].EnergyGain==35);
  for(int i=0;i<3;i++) { fastEnergy.Enemies[0].Health=fastEnergy.Enemies[0].Definition.MaxHealth; Check(fastEnergy.Attack()); if(i<2) ResolveEnemies(fastEnergy); }
  Check(fastEnergy.LastUltimate!=null && fastEnergy.Party[0].Energy==0);
  var ultimateTalent=TalentBattle(new[]{"Ranger_Common"}); int baseUltimate=ultimateTalent.Party[0].Ultimate.Damage;
  Check(ultimateTalent.ChooseTalent("Ranger_Common",2,1) && ultimateTalent.Party[0].Ultimate.Damage==(baseUltimate*120+99)/100);
  Check(ultimateTalent.ChooseTalent("Ranger_Common",0,0) && ultimateTalent.ChooseTalent("Ranger_Common",2,0));
  Check(ultimateTalent.PassiveDamageMultiplier(ultimateTalent.Party[0],ultimateTalent.Enemy,ultimateTalent.Party[0].Identity.Affinity)==1.5);
  var healingTalent=TalentBattle(new[]{"Knight_Common","Paladin_Common","Cleric_Common"});
  Check(healingTalent.ChooseTalent("Cleric_Common",0,1) && healingTalent.ChooseTalent("Cleric_Common",2,1));
  healingTalent.Party[0].Health=100; healingTalent.Party[0].Status=Debuff.Poison; healingTalent.Party[0].StatusRounds=2;
  healingTalent.Party[1].Status=Debuff.Burn; healingTalent.Party[1].StatusRounds=2;
  for(int i=0;i<3;i++) Check(healingTalent.Attack()); ResolveEnemies(healingTalent);
  Check(healingTalent.Party[0].Status==Debuff.None && healingTalent.Party[1].Status==Debuff.None && healingTalent.LastPassiveCleanse[0]==Debuff.Poison && healingTalent.LastPassiveCleanse[1]==Debuff.Burn);
  Check(healingTalent.Party[0].Health>100-8 && healingTalent.Party[2].EncounterHealing>0);
  healingTalent.Reset(); Check(healingTalent.LastPassiveCleanse[0]==Debuff.None);
  var auraTalent=TalentBattle(new[]{"Knight_Common","Paladin_Common"});
  Check(auraTalent.ChooseTalent("Paladin_Common",2,1)); auraTalent.Party[0].Health-=20;
  Check(auraTalent.Attack() && auraTalent.Attack()); ResolveEnemies(auraTalent);
  Check(auraTalent.LastPassiveHealing[0]>0 && auraTalent.Party[1].EncounterHealing==auraTalent.LastPassiveHealing[0]);
  var criticalTalent=TalentBattle(new[]{"Rogue_Common"},15,20,.4);
  Check(criticalTalent.ChooseTalent("Rogue_Common",0,0) && criticalTalent.ChooseTalent("Rogue_Common",2,0));
  Check(criticalTalent.Party[0].CriticalChance==.5 && criticalTalent.Attack() && criticalTalent.LastOffensivePassive=="CRITICAL STRIKE");
  criticalTalent.Reset(); Check(criticalTalent.ChooseTalent("Rogue_Common",0,1) && criticalTalent.ChooseTalent("Rogue_Common",2,1));
  Check(criticalTalent.Party[0].CriticalMultiplier==2 && criticalTalent.Party[0].CriticalChance==.25);
  // Version 6 saves talents per named hero; v1–5 migrate with all choices unset.
  var talentSave=talentedKnight.ExportProgress(); var talentLoad=new Battle(true,enableClassPassives:true);
  Check(talentSave.version==17 && talentLoad.RestoreProgress(talentSave) && talentLoad.Party[0].TalentChoice(0)==1 && talentLoad.Party[0].TalentChoice(2)==1);
  talentSave.loadouts[0].talents[1]=0; talentSave.loadouts[0].level=5;
  Check(!talentLoad.RestoreProgress(talentSave) && talentLoad.Party[0].TalentChoice(2)==1);
  talentSave=talentedKnight.ExportProgress(); talentSave.loadouts[0].talents[0]=2; Check(!talentLoad.RestoreProgress(talentSave));
  talentSave=talentedKnight.ExportProgress(); talentSave.loadouts[0].talents=null; Check(!talentLoad.RestoreProgress(talentSave));
  talentSave.version=5; Check(talentLoad.RestoreProgress(talentSave) && talentLoad.Party[0].TalentChoice(0)==-1 && talentLoad.Party[0].TalentChoice(2)==-1 && talentLoad.Party[0].Level==15);
  var reserveTalent=TalentBattle(new[]{"Paladin_Common"});
  Check(reserveTalent.ChooseTalent("Knight_Common",0,0));
  var reserveLoad=new Battle(true); Check(reserveLoad.RestoreProgress(reserveTalent.ExportProgress()) && reserveLoad.HeroProgress("Knight_Common").TalentChoice(0)==0);
  var wallTalent=TalentBattle(new[]{"Knight_Common","Sorceress_Common","Cleric_Common"});
  Check(wallTalent.ChooseTalent("Knight_Common",0,0) && wallTalent.ChooseTalent("Knight_Common",2,0));
  for(int i=0;i<3;i++) wallTalent.Attack(); wallTalent.BeginStrike(); wallTalent.Defend(false,.1f); wallTalent.FinishStrike();
  wallTalent.BeginStrike(); wallTalent.FinishStrike(); Check(wallTalent.LastIncomingDamage[1]==21 && wallTalent.LastPassiveProtection[1]==14);
  var strongPrayer=TalentBattle(new[]{"Knight_Common","Cleric_Common"});
  Check(strongPrayer.ChooseTalent("Cleric_Common",0,0) && strongPrayer.ChooseTalent("Cleric_Common",2,0));
  strongPrayer.Party[0].Health=100; Check(strongPrayer.Attack() && strongPrayer.Attack()); ResolveEnemies(strongPrayer);
  Check(strongPrayer.LastPassiveHealing[0]==41 && strongPrayer.Party[1].EncounterHealing==41);
  var merciful=TalentBattle(new[]{"Paladin_Common"},5);
  Check(merciful.ChooseTalent("Paladin_Common",0,1)); merciful.Party[0].Health=50;
  Check(merciful.UseAbility() && merciful.HeroHealth==94 && merciful.Party[0].EncounterHealing==44);
  var gearTalent=TalentBattle(new[]{"Knight_Common"},10); var gearTalentSave=gearTalent.ExportProgress();
  gearTalentSave.inventory=new[]{new GearStackData{id="Weapon_Common_Physical",count=1}};
  gearTalentSave.loadouts[0].gear=new[]{"Weapon_Common_Physical",null,null};
  Check(gearTalent.RestoreProgress(gearTalentSave));
  Check(gearTalent.ChooseTalent("Knight_Common",1,1) && gearTalent.Party[0].Skill(0).Damage==67);
  Check(gearTalent.ChooseTalent("Knight_Common",1,0) && gearTalent.Party[0].Definition.MaxHealth==167 && gearTalent.Party[0].Skill(0).Damage==61);
  Check(gearTalent.ResetTalents("Knight_Common") && gearTalent.Party[0].Definition.MaxHealth==145 && gearTalent.Party[0].Gear(GearSlot.Weapon).Id=="Weapon_Common_Physical");
  var malformedTalent=talentedKnight.ExportProgress(); malformedTalent.loadouts[0].talents=new[]{-1,-1}; Check(!talentLoad.RestoreProgress(malformedTalent));
  malformedTalent=talentedKnight.ExportProgress(); malformedTalent.loadouts[0].talents[0]=-2; Check(!talentLoad.RestoreProgress(malformedTalent));
  var chosenCampaign=new Battle(true,enableWaves:true,enableClassPassives:true,criticalRandom:new Random(321),enableFactions:true); int chosenSteps=0,chosenCount=0;
  while(!chosenCampaign.CampaignComplete && chosenSteps++<6000) {
   if(chosenCampaign.Current==Phase.Player) {
    if(chosenCampaign.CanChangeParty) foreach(var recruit in chosenCampaign.Recruits)
     if(chosenCampaign.SharedCount<Battle.SharedSlots && !chosenCampaign.IsShared(recruit.Id) && chosenCampaign.HeroProgress(recruit.Id).Level<chosenCampaign.CrystalLevel) Check(chosenCampaign.SetShared(recruit.Id,true));
    if(chosenCampaign.CanChangeParty) foreach(var hero in chosenCampaign.Party)
     for(int tier=0;tier<3;tier++) if(hero.Level>=ClassTalents.UnlockLevel(tier) && hero.TalentChoice(tier)<0) {
      Check(chosenCampaign.ChooseTalent(hero.Identity.Id,tier,((int)hero.Identity.Class+tier)%2)); chosenCount++;
     }
    chosenCampaign.SelectEnemy(AutoBattlePlanner.ChooseTarget(chosenCampaign)); int skill=AutoBattlePlanner.ChooseSkill(chosenCampaign);
    Check(skill<0?chosenCampaign.Attack():chosenCampaign.UseAbility(skill));
   } else if(chosenCampaign.Current==Phase.EnemyWindup) {
    chosenCampaign.BeginStrike(); if(chosenCampaign.IncomingSkill.Action==EnemyAction.Attack)
     for(int i=0;i<chosenCampaign.Party.Count;i++) if(chosenCampaign.Party[i].Health>0 && (chosenCampaign.IncomingSkill.PartyWide || i==chosenCampaign.ActiveIndex)) Check(chosenCampaign.DefendHero(i,false,.1f));
    chosenCampaign.FinishStrike();
   } else if(chosenCampaign.Current==Phase.WaveCleared) Check(chosenCampaign.AdvanceWave());
   else if(chosenCampaign.Current==Phase.Won) {
    var selectedSave=chosenCampaign.ExportProgress(); var selectedReload=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true);
    Check(selectedReload.RestoreProgress(selectedSave) && selectedReload.Results.Loot.Id==chosenCampaign.Results.Loot.Id && selectedReload.SharedCount==chosenCampaign.SharedCount);
    foreach(var hero in chosenCampaign.Party) for(int tier=0;tier<3;tier++) Check(selectedReload.HeroProgress(hero.Identity.Id).TalentChoice(tier)==hero.TalentChoice(tier));
    Check(chosenCampaign.ContinueAfterVictory());
   } else throw new Exception("Unexpected talent campaign phase: "+chosenCampaign.Current);
  }
  Check(chosenCampaign.CampaignComplete && chosenCount>0);
  // Stage previews describe the exact wave definitions; roleEnemy roles drive real actions.
  for(int stage=0;stage<ChapterDefinition.TotalStages;stage++) {
   var stagePreview=ChapterDefinition.Preview(stage);
   for(int wave=0;wave<3;wave++) foreach(var roleEnemy in ChapterDefinition.WaveAt(stage,wave,3))
    Check(stagePreview.Contains(roleEnemy.Name+" · "+roleEnemy.Role+" / "+roleEnemy.Row+" · Weak: "+roleEnemy.Weakness));
  }
  Check(ChapterDefinition.WaveAt(11,0,3).Count==2 && ChapterDefinition.WaveAt(10,0,3).Count==1);
  foreach(var role in new[]{EnemyRole.Archer,EnemyRole.Caster,EnemyRole.Healer,EnemyRole.Support}) {
   var roleEnemy=new EnemyDefinition("Glacier Spirit",100,role:role);
   Check(roleEnemy.Role==role && roleEnemy.Row==EnemyRow.Back);
   var move=EnemySkills.ForRole(7,roleEnemy,role==EnemyRole.Caster?2:1);
   Check(role==EnemyRole.Archer?move.BackRow:role==EnemyRole.Caster?move.PartyWide:role==EnemyRole.Healer?move.Action==EnemyAction.HealPartner:move.Action==EnemyAction.Shield);
  }
  for(int stage=7;stage<=8;stage++) {
   var team=PassiveBattle(stage,new[]{"Knight_Common"},waves:true);
   team.Enemies[0].Health=1; Check(team.Attack() && team.Current==Phase.WaveCleared && team.AdvanceWave());
   Check(team.Enemies[1].Definition.Role==(stage==7?EnemyRole.Healer:EnemyRole.Support));
   team.Enemies[1].CompletedStrikes=1; team.Enemies[0].Health-=10;
   Check(team.Attack()); team.BeginStrike(); team.FinishStrike();
   Check(team.Current==Phase.EnemyWindup && team.AttackingEnemyIndex==1);
   Check(team.IncomingSkill.Action==(stage==7?EnemyAction.HealPartner:EnemyAction.Shield));
   team.BeginStrike(); team.FinishStrike();
   Check(stage==7?team.LastEnemyHealing>0 && team.LastMechanicTarget==0:team.Enemies[0].Shield==25);
   var restoredTeam=new Battle(true,enableWaves:true); Check(restoredTeam.RestoreProgress(team.ExportProgress()));
   // Unfinished-stage reload starts wave one; stagePreview remains deterministic.
   Check(restoredTeam.WaveIndex==0 && ChapterDefinition.Preview(restoredTeam.Encounter)==ChapterDefinition.Preview(stage));
  }
  // Offline rewards use an injected UTC clock, survive reloads, and consume time only once.
  var idleNow=new DateTime(2026,10,11,12,0,0,DateTimeKind.Utc);
  var idle=new Battle(true,utcNow:()=>idleNow);
  Check(idle.IdleMinutes==0 && !idle.ClaimIdleRewards());
  idleNow=idleNow.AddSeconds(59); Check(idle.IdleMinutes==0);
  idleNow=idleNow.AddSeconds(2); Check(idle.IdleMinutes==1 && idle.IdleCrystals==0 && idle.IdleExperience==2);
  int idleCurrency=idle.Crystals; Check(idle.ClaimIdleRewards() && idle.Crystals==idleCurrency && idle.Party[0].Experience==2 && idle.ExportProgress().idleCrystalRemainder==1);
  Check(!idle.ClaimIdleRewards()); idleNow=idleNow.AddSeconds(59); Check(idle.IdleMinutes==1); // retained fractional minute
  var idleSave=idle.ExportProgress(); var idleReload=new Battle(true,utcNow:()=>idleNow);
  Check(idleReload.RestoreProgress(idleSave) && idleReload.IdleMinutes==1 && idleReload.ClaimIdleRewards());
  var afterClaim=idleReload.ExportProgress(); Check(idle.RestoreProgress(afterClaim) && !idle.ClaimIdleRewards());
  idleNow=idleNow.AddDays(2); Check(idle.IdleMinutes==720 && idle.IdleCrystals==72 && idle.IdleExperience==1440);
  Check(idle.ClaimIdleRewards() && idle.IdleMinutes==0);
  long idleAnchor=idle.ExportProgress().idleClaimTicks; idleNow=idleNow.AddDays(-3);
  Check(idle.IdleMinutes==0 && !idle.ClaimIdleRewards() && idle.ExportProgress().idleClaimTicks==idleAnchor);
  idleNow=new DateTime(idleAnchor,DateTimeKind.Utc).AddMinutes(1); Check(idle.IdleMinutes==1);
  var progressedIdle=idle.ExportProgress(); progressedIdle.encounter=10;
  progressedIdle.recruited=new[]{"Knight_Common","Paladin_Common"}; progressedIdle.party=new[]{"Knight_Common"};
  progressedIdle.loadouts=new[]{new HeroLoadoutData{id="Knight_Common",secondSkill=1},new HeroLoadoutData{id="Paladin_Common",secondSkill=1}};
  Check(idle.RestoreProgress(progressedIdle) && idle.IdleCrystalRate==3 && idle.IdleExperienceRate==4);
  idleCurrency=idle.Crystals; Check(idle.ClaimIdleRewards() && idle.Crystals==idleCurrency && idle.ExportProgress().idleCrystalRemainder==5 && idle.HeroProgress("Paladin_Common").Experience==4);
  idleNow=idleNow.AddMinutes(10); idle.Attack(); Check(!idle.ClaimIdleRewards()); idle.Reset(); Check(idle.ClaimIdleRewards());
  progressedIdle=idle.ExportProgress(); progressedIdle.encounter=30; Check(idle.RestoreProgress(progressedIdle) && idle.ChapterIndex==6);
  Check(idle.FurthestStage==30 && idle.IdleCrystalRate==7 && idle.IdleExperienceRate==8);
  var replayIdle=new Battle(true,utcNow:()=>idleNow); Check(replayIdle.RestoreProgress(idle.ExportProgress()) && replayIdle.FurthestStage==30);
  var oldIdle=idle.ExportProgress(); oldIdle.version=6; oldIdle.idleClaimTicks=0; oldIdle.furthestStage=0;
  Check(replayIdle.RestoreProgress(oldIdle) && replayIdle.IdleMinutes==0 && replayIdle.ExportProgress().version==17);
  var badIdle=idle.ExportProgress(); int preservedIdleCrystals=replayIdle.Crystals;
  foreach(long invalidTicks in new[]{0L,-1L,DateTime.MaxValue.Ticks+1}) { badIdle.idleClaimTicks=invalidTicks; Check(!replayIdle.RestoreProgress(badIdle) && replayIdle.Crystals==preservedIdleCrystals); }
  badIdle=idle.ExportProgress(); badIdle.furthestStage=ChapterDefinition.TotalStages+1; Check(!replayIdle.RestoreProgress(badIdle));
  badIdle.furthestStage=-1; Check(!replayIdle.RestoreProgress(badIdle));
  var fullWallet=idle.ExportProgress(); fullWallet.crystals=999999; Check(idle.RestoreProgress(fullWallet));
  idleNow=idleNow.AddMinutes(10); Check(idle.IdleCrystals==1 && idle.ClaimIdleRewards() && idle.Crystals==1000000);
  Check(new Battle(true).RestoreProgress(idle.ExportProgress()));
  var practiceIdle=new Battle(false,utcNow:()=>idleNow); idleNow=idleNow.AddHours(1); Check(!practiceIdle.ClaimIdleRewards());
  // Factions span classes and rarities, and boost the whole active team at exact thresholds.
  foreach(HeroFaction faction in Enum.GetValues(typeof(HeroFaction))) {
   int total=0; var kinds=new System.Collections.Generic.HashSet<HeroClass>(); var qualities=new System.Collections.Generic.HashSet<HeroQuality>();
   foreach(var definition in HeroDefinition.Catalog) if(definition.Faction==faction) { total++; kinds.Add(definition.Class); qualities.Add(definition.Quality); Check(!string.IsNullOrEmpty(definition.FactionName)); }
   Check(total==10 && kinds.Count==6 && qualities.Count==5);
  }
  var noSynergy=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common"});
  Check(noSynergy.TeamFactionBonus==0 && noSynergy.Party[0].Definition.MaxHealth==100);
  var threeFaction=FactionBattle(new[]{"Knight_Common","Paladin_Common","Rogue_Uncommon","Sorceress_Common","Ranger_Common"});
  Check(threeFaction.TeamFactionBonus==10 && threeFaction.FactionCount(HeroFaction.DawnOrder)==3);
  foreach(var member in threeFaction.Party) {
   Check(member.Definition.MaxHealth==(member.Identity.Stats.MaxHealth*110+99)/100);
   Check(member.Definition.AttackDamage==(member.Identity.Stats.AttackDamage*110+99)/100);
   Check(member.Skill(0).Damage==(member.Identity.Stats.Skills[0].Damage*110+99)/100);
   Check(member.Ultimate.Damage==(member.Identity.Ultimate.Damage*110+99)/100);
   Check(member.Skill(0).Healing==member.Identity.Stats.Skills[0].Healing);
  }
  threeFaction.Party[2].Health=0; Check(threeFaction.TeamFactionBonus==10);
  Check(!threeFaction.SwapFormation(0,1) && threeFaction.TeamFactionBonus==10 && threeFaction.Party[2].Health==0);
  threeFaction.Reset(); Check(threeFaction.SwapFormation(0,1) && threeFaction.TeamFactionBonus==10);
  threeFaction.Reset(); Check(threeFaction.SelectHero(2) && threeFaction.EquipHero("Cleric_Common") && threeFaction.TeamFactionBonus==0);
  foreach(var member in threeFaction.Party) Check(member.Definition.MaxHealth==member.Identity.Stats.MaxHealth);
  Check(threeFaction.HeroProgress("Rogue_Uncommon").Definition.MaxHealth==threeFaction.HeroProgress("Rogue_Uncommon").Identity.Stats.MaxHealth);
  var fourFaction=FactionBattle(new[]{"Knight_Common","Paladin_Common","Rogue_Uncommon","Cleric_Uncommon","Sorceress_Common"});
  Check(fourFaction.TeamFactionBonus==15);
  foreach(var member in fourFaction.Party) {
   Check(member.Definition.MaxHealth==(member.Identity.Stats.MaxHealth*115+99)/100);
   Check(member.Definition.AttackDamage==(member.Identity.Stats.AttackDamage*115+99)/100);
   Check(member.Skill(0).Damage==(member.Identity.Stats.Skills[0].Damage*115+99)/100);
   Check(member.Ultimate.Damage==(member.Identity.Ultimate.Damage*115+99)/100);
  }
  Check(fourFaction.FactionProgress.Contains("1 more matching hero for +20%"));
  var fourSave=fourFaction.ExportProgress();
  var fourReload=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true);
  Check(fourReload.RestoreProgress(fourSave) && fourReload.TeamFactionBonus==15);
  int beforePower=fourFaction.TeamPower, beforeHealth=fourFaction.Party[0].Health;
  var gainPreview=fourFaction.PreviewReplacement("Sorceress_Rare",4);
  Check(gainPreview!=null && gainPreview.FactionBonus==20 && fourFaction.TeamFactionBonus==15 && fourFaction.TeamPower==beforePower && fourFaction.Party[0].Health==beforeHealth);
  Check(fourFaction.EquipHeroAt("Sorceress_Rare",4) && fourFaction.TeamFactionBonus==20 && fourFaction.TeamPower==gainPreview.TeamPower);
  var lossPreview=fourFaction.PreviewReplacement("Sorceress_Common",4);
  Check(lossPreview.FactionBonus==15 && fourFaction.TeamFactionBonus==20);
  Check(fourFaction.EquipHeroAt("Sorceress_Common",4) && fourFaction.TeamFactionBonus==15 && fourFaction.TeamPower==lossPreview.TeamPower);
  var dropPreview=fourFaction.PreviewReplacement("Ranger_Common",2);
  Check(dropPreview.FactionBonus==10);
  Check(fourFaction.EquipHeroAt("Ranger_Common",2) && fourFaction.TeamFactionBonus==10 && fourFaction.TeamPower==dropPreview.TeamPower);
  Check(FactionRules.Bonus(0)==0 && FactionRules.Bonus(2)==0 && FactionRules.Bonus(3)==10 && FactionRules.Bonus(4)==15 && FactionRules.Bonus(5)==20);
  Check(new Battle(true,enableFactions:true).FactionProgress.Contains("Unlock more party slots"));
  Check(new Battle(true).FactionSummary.Contains("SYNERGY OFF"));
  var fiveFaction=FactionBattle(new[]{"Knight_Common","Paladin_Common","Rogue_Uncommon","Cleric_Uncommon","Sorceress_Rare"});
  Check(fiveFaction.TeamFactionBonus==20 && fiveFaction.Party[0].Definition.MaxHealth==120 && fiveFaction.Party[0].Definition.AttackDamage==30);
  Check(fiveFaction.HeroProgress("Ranger_Rare").FactionBonus==0);
  var factionReload=new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true);
  Check(factionReload.RestoreProgress(fiveFaction.ExportProgress()) && factionReload.TeamFactionBonus==20 && factionReload.Party[0].Definition.MaxHealth==120);
  Check(fiveFaction.Attack() && fiveFaction.LastDamage==30); // Boosted attack, neutral Fire matchup.
  Check(!fiveFaction.EquipHero("Sorceress_Common"));
  var factionGrowth=factionReload.ExportProgress(); factionGrowth.loadouts[0].level=10; factionGrowth.loadouts[0].talents=new[]{-1,0,-1};
  factionGrowth.inventory=new[]{new GearStackData{id="Armor_Common_Physical",count=1}}; factionGrowth.loadouts[0].gear=new[]{null,"Armor_Common_Physical",null};
  Check(factionReload.RestoreProgress(factionGrowth));
  int expectedFactionHealth=(factionReload.Party[0].Identity.Stats.Scaled(factionReload.Party[0].Growth).WithTalentBonuses(15,0).MaxHealth+GearDefinition.Find("Armor_Common_Physical").Health)*120;
  Check(factionReload.Party[0].Definition.MaxHealth==(expectedFactionHealth+99)/100);
  var factionReplay=fiveFaction.ExportProgress(); factionReplay.encounter=30; factionReplay.pendingVictory=false; factionReplay.results=null;
  Check(factionReload.RestoreProgress(factionReplay) && factionReload.ChapterIndex==6 && factionReload.TeamFactionBonus==20 && factionReload.Party[0].Definition.MaxHealth==120);
  Check(factionReload.HeroProgress("Sorceress_Rare").FactionBonus==20);
  // Ascension raises effective rarity while preserving every named hero and their loadout.
  var ascension=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common"});
  var ascensionData=ascension.ExportProgress(); foreach(var loadout in ascensionData.loadouts) loadout.shards=1000;
  Check(ascension.RestoreProgress(ascensionData));
  foreach(var definition in HeroDefinition.Catalog) {
   var member=ascension.HeroProgress(definition.Id); var originalUlt=member.Ultimate.Name; var originalAffinity=member.Identity.Affinity; int previousHealth=member.Definition.MaxHealth;
   for(int step=1;step<=5;step++) {
    int previousShards=member.Shards, cost=member.UpgradeCost;
    Check(ascension.UpgradeHero(definition.Id) && member.Shards==previousShards-cost && member.UpgradeRank==step);
    Check(member.Quality==(HeroQuality)Math.Min(4,(int)definition.Quality+step) && member.AscensionStars==Math.Max(0,(int)definition.Quality+step-4));
    Check(member.Identity==definition && member.Identity.Affinity==originalAffinity && member.Ultimate.Name==originalUlt && member.SkillIndex(0)==0 && member.SkillIndex(1)==1);
    Check(member.Definition.MaxHealth>=previousHealth && member.Health==member.Definition.MaxHealth); previousHealth=member.Health;
   }
   Check(!ascension.UpgradeHero(definition.Id));
  }
  var ascensionReload=new Battle(true,enableFactions:true); Check(ascensionReload.RestoreProgress(ascension.ExportProgress()));
  foreach(var definition in HeroDefinition.Catalog) Check(ascensionReload.HeroProgress(definition.Id).RarityLabel==ascension.HeroProgress(definition.Id).RarityLabel);
  var legacyAscension=ascension.ExportProgress(); legacyAscension.version=7;
  Check(ascensionReload.RestoreProgress(legacyAscension) && ascensionReload.HeroProgress("Knight_Common").UpgradeRank==5 && ascensionReload.HeroProgress("Knight_Common").Quality==HeroQuality.Legendary);
  Check(ascensionReload.HeroProgress("Knight_Common").AscensionStars==1 && ascensionReload.HeroProgress("Knight_Legendary").AscensionStars==5);
  // Existing purchased ranks retain at least their previous base-stat strength at every level.
  foreach(var definition in HeroDefinition.Catalog) for(int rank=0;rank<=5;rank++) for(int level=1;level<=20;level++) {
   var grown=new PartyHero(definition); grown.UpgradeRank=rank; grown.Level=level; grown.RefreshStats();
   Check(grown.Definition.MaxHealth>=definition.Stats.Scaled(1f+(level-1)*.05f+rank*.1f).MaxHealth);
  }
  // Shared combat levels derive exclusively from earned levels, never other linked boosts.
  var crystal=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common"});
  var crystalSave=crystal.ExportProgress(); int[] leaderLevels={20,18,15,12,10};
  for(int leader=0;leader<5;leader++) crystalSave.loadouts[leader].level=leaderLevels[leader];
  Check(crystal.RestoreProgress(crystalSave) && crystal.CrystalUnlocked && crystal.CrystalLevel==10);
  string linkedId=HeroDefinition.Catalog[5].Id; var linkedHero=crystal.HeroProgress(linkedId); string linkedUltimate=linkedHero.Ultimate.Name;
  Check(crystal.SetShared(linkedId,true) && crystal.SharedCount==1 && linkedHero.Level==1 && linkedHero.CombatLevel==10);
  Check(linkedHero.Definition.MaxHealth==linkedHero.Identity.Stats.Scaled(1.45f).MaxHealth && linkedHero.Experience==0 && linkedHero.Ultimate.Name==linkedUltimate);
  Check(!crystal.ChooseTalent(linkedId,0,0)); // Talent unlocks continue to use personally earned levels.
  Check(!crystal.SetShared(linkedId,true) && !crystal.SetShared("missing",true) && !crystal.SetShared("Knight_Common",true));
  for(int entry=6;entry<10;entry++) Check(crystal.SetShared(HeroDefinition.Catalog[entry].Id,true));
  Check(crystal.SharedCount==5 && crystal.CrystalLevel==10 && !crystal.SetShared(HeroDefinition.Catalog[10].Id,true));
  var crystalReload=new Battle(true,enableFactions:true); Check(crystalReload.RestoreProgress(crystal.ExportProgress()));
  Check(crystalReload.SharedCount==5 && crystalReload.HeroProgress(linkedId).Level==1 && crystalReload.HeroProgress(linkedId).CombatLevel==10);
  Check(crystalReload.SetShared(linkedId,false) && crystalReload.HeroProgress(linkedId).CombatLevel==1 && crystalReload.HeroProgress(linkedId).Experience==0);
  Check(crystal.SelectHero(4) && crystal.EquipHero(linkedId) && crystal.HeroProgress(linkedId).CombatLevel==10);
  crystal.Attack(); Check(!crystal.SetShared(linkedId,false)); crystal.Reset();
  var improvedCrystal=crystal.ExportProgress(); improvedCrystal.loadouts[4].level=11; Check(crystal.RestoreProgress(improvedCrystal) && crystal.CrystalLevel==11 && crystal.HeroProgress(linkedId).CombatLevel==11);
  Check(crystal.HeroProgress(linkedId).Level==1 && crystal.SharedCount==5);
  var migratedCrystal=crystal.ExportProgress(); migratedCrystal.version=8; Check(crystalReload.RestoreProgress(migratedCrystal) && crystalReload.SharedCount==0 && crystalReload.HeroProgress(linkedId).CombatLevel==1);
  var badCrystal=crystal.ExportProgress(); int originalCrystalLevel=crystalReload.CrystalLevel;
  foreach(var invalidLinks in new[]{new[]{linkedId,linkedId},new[]{"unknown"},new string[]{null},new[]{linkedId,HeroDefinition.Catalog[6].Id,HeroDefinition.Catalog[7].Id,HeroDefinition.Catalog[8].Id,HeroDefinition.Catalog[9].Id,HeroDefinition.Catalog[10].Id}}) {
   badCrystal.sharedHeroes=invalidLinks; Check(!crystalReload.RestoreProgress(badCrystal) && crystalReload.CrystalLevel==originalCrystalLevel);
  }
  badCrystal.sharedHeroes=null; Check(!crystalReload.RestoreProgress(badCrystal));
  var starterCrystal=new Battle(true); Check(!starterCrystal.CrystalUnlocked && starterCrystal.CrystalLevel==1 && !starterCrystal.SetShared("Knight_Common",true));
  var replayCrystal=crystal.ExportProgress(); replayCrystal.encounter=30;
  Check(crystalReload.RestoreProgress(replayCrystal) && crystalReload.ChapterIndex==6 && crystalReload.SharedCount==5 && crystalReload.CrystalLevel==11);
  Check(crystalReload.HeroProgress(linkedId).CombatLevel==11);
  var sharedClock=new DateTime(2026,10,11,12,0,0,DateTimeKind.Utc); var idleCrystal=new Battle(true,utcNow:()=>sharedClock);
  var idleCrystalSave=crystal.ExportProgress(); idleCrystalSave.idleClaimTicks=sharedClock.Ticks;
  Check(idleCrystal.RestoreProgress(idleCrystalSave)); sharedClock=sharedClock.AddHours(1);
  Check(idleCrystal.ClaimIdleRewards() && idleCrystal.HeroProgress(linkedId).Level>1 && idleCrystal.HeroProgress(linkedId).CombatLevel>=idleCrystal.HeroProgress(linkedId).Level);
  Check(idleCrystal.SetShared(linkedId,false) && idleCrystal.HeroProgress(linkedId).CombatLevel==idleCrystal.HeroProgress(linkedId).Level);
  // Power estimates are stable preparation ratings, independent from transient combat damage.
  foreach(var definition in HeroDefinition.Catalog) {
   var rated=new PartyHero(definition); int basePower=rated.Power; Check(basePower>0);
   rated.Health=1; rated.Energy=100; rated.Status=Debuff.Burn; Check(rated.Power==basePower);
   rated.Level=10; rated.RefreshStats(); Check(rated.Power>basePower);
   int levelPower=rated.Power; rated.UpgradeRank=1; rated.RefreshStats(); Check(rated.Power>levelPower);
  }
  Check(ChapterDefinition.Difficulty(125,100)=="EASY" && ChapterDefinition.Difficulty(124,100)=="EVEN");
  Check(ChapterDefinition.Difficulty(85,100)=="HARD" && ChapterDefinition.Difficulty(86,100)=="EVEN");
  Check(ChapterDefinition.Difficulty(0,100)=="HARD");
  for(int stage=0;stage<ChapterDefinition.TotalStages;stage++) {
   int peakPower=0;
   for(int wave=0;wave<3;wave++) { int wavePower=ChapterDefinition.WavePower(stage,wave); Check(wavePower>0 && ChapterDefinition.Preview(stage).Contains("Power "+wavePower)); peakPower=Math.Max(peakPower,wavePower); }
   Check(ChapterDefinition.StagePower(stage)>=peakPower && ChapterDefinition.StagePower(stage,1)==ChapterDefinition.WavePower(stage,0,1));
  }
  var ratedTeam=FactionBattle(new[]{"Knight_Common","Paladin_Common","Rogue_Uncommon","Cleric_Uncommon","Sorceress_Rare"});
  int ratedTotal=0; foreach(var member in ratedTeam.Party) ratedTotal+=member.Power; Check(ratedTotal==ratedTeam.TeamPower);
  var ratedTeamSave=ratedTeam.ExportProgress(); var noFactionRating=new Battle(true,enableWaves:true);
  Check(noFactionRating.RestoreProgress(ratedTeamSave) && ratedTeam.TeamPower>noFactionRating.TeamPower);
  var ratingReload=new Battle(true,enableWaves:true,enableFactions:true);
  Check(ratingReload.RestoreProgress(ratedTeamSave) && ratingReload.TeamPower==ratedTeam.TeamPower && ratingReload.StagePower==ratedTeam.StagePower && ratingReload.Difficulty==ratedTeam.Difficulty);
  int beforeRatingAttack=ratedTeam.TeamPower; ratedTeam.Attack(); Check(ratedTeam.TeamPower==beforeRatingAttack);
  var gearRating=FactionBattle(new[]{"Knight_Common"}); int beforeGear=gearRating.TeamPower;
  var gearRatingSave=gearRating.ExportProgress(); gearRatingSave.inventory=new[]{new GearStackData{id="Weapon_Common_Physical",count=1},new GearStackData{id="Armor_Common_Physical",count=1}};
  Check(gearRating.RestoreProgress(gearRatingSave) && gearRating.EquipGear("Knight_Common",GearSlot.Weapon,"Weapon_Common_Physical") && gearRating.TeamPower>beforeGear);
  beforeGear=gearRating.TeamPower; Check(gearRating.EquipGear("Knight_Common",GearSlot.Armor,"Armor_Common_Physical") && gearRating.TeamPower>beforeGear);
  var sharedRating=FactionBattle(new[]{"Knight_Common"}); var sharedRatingSave=sharedRating.ExportProgress();
  for(int leader=0;leader<5;leader++) sharedRatingSave.loadouts[leader].level=15;
  Check(sharedRating.RestoreProgress(sharedRatingSave)); int beforeLinkedRating=sharedRating.HeroProgress("Cleric_Common").Power;
  Check(sharedRating.SetShared("Cleric_Common",true) && sharedRating.HeroProgress("Cleric_Common").Power>beforeLinkedRating);
  Check(sharedRating.SetShared("Cleric_Common",false) && sharedRating.HeroProgress("Cleric_Common").Power==beforeLinkedRating);
  // Daily quests count successful actions and claimed stage wins, once per UTC day.
  var questNow=new DateTime(2026,10,11,12,0,0,DateTimeKind.Utc); var quests=new Battle(true,utcNow:()=>questNow);
  Check(!quests.ClaimDaily(0) && !quests.ClaimDaily(3) && !quests.ClaimDaily(-1) && !quests.ClaimDaily(4));
  Check(!quests.ClaimIdleRewards() && quests.DailyProgress(1)==0);
  Check(quests.Summon(double.NaN,0)==null && quests.DailyProgress(2)==0);
  for(int stage=0;stage<3;stage++) {
   int moves=0;
   while(quests.Current!=Phase.Won && moves++<100) {
    if(quests.Current==Phase.Player) Check(quests.Attack());
    else if(quests.Current==Phase.EnemyWindup) { quests.BeginStrike(); quests.Defend(false,.1f); quests.FinishStrike(); }
    else throw new Exception("Unexpected daily quest battle phase.");
   }
   Check(quests.Current==Phase.Won && quests.DailyProgress(0)==stage && !quests.ClaimDaily(0));
   Check(quests.ContinueAfterVictory() && quests.DailyProgress(0)==stage+1 && !quests.ContinueAfterVictory());
  }
  questNow=questNow.AddMinutes(1); Check(quests.ClaimIdleRewards() && quests.DailyProgress(1)==1 && !quests.ClaimIdleRewards());
  Check(quests.Summon(0,0)!=null && quests.DailyProgress(2)==1 && !quests.CanClaimDaily(3));
  int preQuestCrystals=quests.Crystals, preQuestShards=quests.HeroProgress("Knight_Common").Shards;
  Check(quests.ClaimDaily(0) && quests.ClaimDaily(1) && quests.ClaimDaily(2));
  Check(quests.Crystals==preQuestCrystals+90 && quests.HeroProgress("Knight_Common").Shards==preQuestShards+25);
  Check(quests.ClaimDaily(3) && quests.Crystals==preQuestCrystals+190 && quests.HeroProgress("Knight_Common").Shards==preQuestShards+35);
  foreach(var recruit in quests.Recruits) if(recruit.Id!="Knight_Common") Check(quests.HeroProgress(recruit.Id).Shards==10);
  var questReload=new Battle(true,utcNow:()=>questNow); Check(questReload.RestoreProgress(quests.ExportProgress()));
  for(int q=0;q<4;q++) Check(questReload.DailyClaimed(q) && !questReload.ClaimDaily(q));
  questNow=questNow.AddHours(-1); Check(questReload.DailyClaimed(3) && !questReload.ClaimDaily(3));
  questNow=new DateTime(2026,10,12,0,0,0,DateTimeKind.Utc);
  for(int q=0;q<4;q++) Check(!questReload.DailyClaimed(q) && !questReload.CanClaimDaily(q));
  for(int q=0;q<3;q++) Check(questReload.DailyProgress(q)==0);
  var questInvalid=questReload.ExportProgress(); questInvalid.dailyProgress=new[]{4,0,0}; Check(!questReload.RestoreProgress(questInvalid));
  questInvalid=questReload.ExportProgress(); questInvalid.dailyClaimed[3]=true; Check(!questReload.RestoreProgress(questInvalid));
  questInvalid=questReload.ExportProgress(); questInvalid.dailyClaimed[0]=true; Check(!questReload.RestoreProgress(questInvalid));
  questInvalid=questReload.ExportProgress(); questInvalid.dailyDay++; Check(!questReload.RestoreProgress(questInvalid));
  questInvalid=questReload.ExportProgress(); questInvalid.dailyProgress=null; Check(!questReload.RestoreProgress(questInvalid));
  questInvalid=questReload.ExportProgress(); questInvalid.dailyClaimed=new bool[3]; Check(!questReload.RestoreProgress(questInvalid));
  var questLegacy=quests.ExportProgress(); questLegacy.version=9; questLegacy.dailyProgress=null;
  Check(questReload.RestoreProgress(questLegacy) && questReload.DailyProgress(0)==0 && !questReload.DailyClaimed(3));
  // Ten pulls are atomic on invalid input and match ten sequential single pulls exactly.
  var tenSummon=new Battle(true); var tenSave=tenSummon.ExportProgress(); tenSave.crystals=1000; Check(tenSummon.RestoreProgress(tenSave));
  var singlesSummon=new Battle(true); Check(singlesSummon.RestoreProgress(tenSave));
  var tenRolls=new double[10]; var tenClasses=new int[10]; for(int pull=0;pull<10;pull++) tenClasses[pull]=1;
  var batchResults=tenSummon.SummonTen(tenRolls,tenClasses); Check(batchResults!=null && batchResults.Length==10);
  Check(!batchResults[0].Duplicate && batchResults[1].Duplicate && batchResults[1].Shards==10);
  Check(batchResults[4].Hero.Quality>=HeroQuality.Rare && batchResults[9].Hero.Quality>=HeroQuality.Rare && batchResults[9].Duplicate);
  for(int pull=0;pull<10;pull++) {
   var singleResult=singlesSummon.Summon(tenRolls[pull],tenClasses[pull]);
   Check(singleResult.Hero.Id==batchResults[pull].Hero.Id && singleResult.Refund==batchResults[pull].Refund && singleResult.Shards==batchResults[pull].Shards && singleResult.Duplicate==batchResults[pull].Duplicate);
  }
  Check(tenSummon.Crystals==singlesSummon.Crystals && tenSummon.RareMisses==singlesSummon.RareMisses && tenSummon.LegendaryMisses==singlesSummon.LegendaryMisses && tenSummon.DailyProgress(2)==1);
  foreach(var recruit in tenSummon.Recruits) Check(tenSummon.HeroProgress(recruit.Id).Shards==singlesSummon.HeroProgress(recruit.Id).Shards);
  var tenReload=new Battle(true); Check(tenReload.RestoreProgress(tenSummon.ExportProgress()) && tenReload.Crystals==tenSummon.Crystals && tenReload.RareMisses==tenSummon.RareMisses);
  var invalidBatch=new Battle(true); Check(invalidBatch.RestoreProgress(tenSave));
  tenRolls[9]=double.NaN; Check(invalidBatch.SummonTen(tenRolls,tenClasses)==null && invalidBatch.Crystals==1000 && invalidBatch.Recruits.Count==1 && invalidBatch.DailyProgress(2)==0);
  tenRolls[9]=0; tenClasses[9]=6; Check(invalidBatch.SummonTen(tenRolls,tenClasses)==null && invalidBatch.Crystals==1000 && invalidBatch.LegendaryMisses==0);
  tenClasses[9]=1; Check(invalidBatch.SummonTen(null,tenClasses)==null && invalidBatch.SummonTen(new double[9],tenClasses)==null);
  var lowBatch=tenSave; lowBatch.crystals=999; Check(invalidBatch.RestoreProgress(lowBatch) && !invalidBatch.CanSummonTen && invalidBatch.SummonTen(tenRolls,tenClasses)==null && invalidBatch.Crystals==999);
  var pityBatch=invalidBatch.ExportProgress(); pityBatch.crystals=1000; pityBatch.legendaryMisses=14;
  Check(invalidBatch.RestoreProgress(pityBatch)); var pityResults=invalidBatch.SummonTen(tenRolls,tenClasses);
  Check(pityResults[0].Hero.Quality==HeroQuality.Legendary && invalidBatch.LegendaryMisses==9);
  var capBatch=new Battle(true); var capBatchSave=capBatch.ExportProgress(); capBatchSave.loadouts[0].shards=999999;
  Check(capBatch.RestoreProgress(capBatchSave)); var cappedDuplicate=capBatch.Summon(0,0); Check(cappedDuplicate.Shards==1 && capBatch.HeroProgress("Knight_Common").Shards==1000000);
  var lockedBatch=new Battle(true); var lockedBatchSave=lockedBatch.ExportProgress(); lockedBatchSave.crystals=1000; Check(lockedBatch.RestoreProgress(lockedBatchSave));
  lockedBatch.Attack(); Check(!lockedBatch.CanSummonTen && lockedBatch.SummonTen(tenRolls,tenClasses)==null);
  // Tower fights preserve campaign position and lineup, and claim each floor exactly once.
  var tower=FactionBattle(new[]{"Knight_Common"},0); int towerCampaignPosition=tower.Encounter;
  Check(tower.EnterTower() && tower.InTower && tower.Party.Count==5 && tower.PartyLimit==5 && tower.WaveCount==3 && tower.TowerFloor==1);
  Check(tower.Enemies[0].Definition.MaxHealth==50); // 30 early-wave HP, scaled for five party slots.
  var towerSave=tower.ExportProgress(); var towerReload=new Battle(true,enableWaves:true,enableFactions:true);
  Check(towerReload.RestoreProgress(towerSave) && towerReload.InTower && towerReload.Party.Count==5 && towerReload.Encounter==towerCampaignPosition);
  Check(towerReload.LeaveTower() && towerReload.Party.Count==1 && towerReload.Party[0].Identity.Id=="Knight_Common" && towerReload.Encounter==0);
  var invalidTower=tower.ExportProgress(); invalidTower.towerCampaignParty=new[]{"unknown"}; Check(!towerReload.RestoreProgress(invalidTower));
  invalidTower=tower.ExportProgress(); invalidTower.towerFloor=0; Check(!towerReload.RestoreProgress(invalidTower));
  invalidTower=tower.ExportProgress(); invalidTower.towerCampaignParty=new[]{"Knight_Common","Knight_Common"}; Check(!towerReload.RestoreProgress(invalidTower));
  Check(!new Battle(true).EnterTower());
  var towerStrong=tower.ExportProgress(); foreach(var loadout in towerStrong.loadouts) { loadout.level=20; loadout.experience=0; loadout.upgradeRank=5; }
  Check(tower.RestoreProgress(towerStrong)); int towerClaimed=0,towerMoves=0;
  while(!tower.TowerComplete && towerMoves++<100000) {
   if(tower.Current==Phase.Player) { tower.SelectEnemy(AutoBattlePlanner.ChooseTarget(tower)); int selectedTowerSkill=AutoBattlePlanner.ChooseSkill(tower); Check(selectedTowerSkill<0?tower.Attack():tower.UseAbility(selectedTowerSkill)); }
   else if(tower.Current==Phase.EnemyWindup) {
    tower.BeginStrike(); if(tower.IncomingSkill.Action==EnemyAction.Attack) for(int member=0;member<tower.Party.Count;member++) if(tower.Party[member].Health>0 && (tower.IncomingSkill.PartyWide || member==tower.ActiveIndex)) Check(tower.DefendHero(member,false,.1f)); tower.FinishStrike();
   } else if(tower.Current==Phase.WaveCleared) Check(tower.AdvanceWave());
   else if(tower.Current==Phase.Won) {
    int floor=tower.TowerFloor, crystals=tower.Crystals, shards=tower.Party[0].Shards, participants=tower.Party.Count; int shardReward=tower.TowerShards;
    Check(tower.Results.Crystals==80+floor*5 && tower.Results.Experience==60+floor*2 && tower.Results.RecruitName==null);
    var pendingTower=new Battle(true,enableWaves:true,enableFactions:true); Check(pendingTower.RestoreProgress(tower.ExportProgress()) && pendingTower.Current==Phase.Won && pendingTower.InTower && pendingTower.Results.Loot.Id==tower.Results.Loot.Id);
    Check(!pendingTower.LeaveTower());
    if(floor==1) { var endStoryTower=tower.ExportProgress(); endStoryTower.encounter=ChapterDefinition.TotalStages; var endStoryReload=new Battle(true); Check(endStoryReload.RestoreProgress(endStoryTower) && endStoryReload.Current==Phase.Won && endStoryReload.ContinueAfterVictory() && endStoryReload.LeaveTower() && endStoryReload.CampaignComplete); }
    Check(tower.ContinueAfterVictory() && tower.TowerFloor==floor+1 && tower.Crystals==crystals+80+floor*5 && tower.Party[0].Shards==shards+shardReward && tower.Encounter==0);
    Check(!tower.ContinueAfterVictory() && tower.Party.Count==participants); towerClaimed++;
   } else throw new Exception("Unexpected tower phase: "+tower.Current);
  }
  Check(tower.TowerComplete && towerClaimed==100 && tower.Current==Phase.Complete && tower.FurthestStage==0);
  var completedTower=new Battle(true,enableWaves:true); Check(completedTower.RestoreProgress(tower.ExportProgress()) && completedTower.TowerComplete && completedTower.LeaveTower() && !completedTower.CanEnterTower);
  Check(tower.LeaveTower() && tower.Party.Count==1 && tower.Encounter==0);
  var legacyTower=tower.ExportProgress(); legacyTower.version=10; Check(towerReload.RestoreProgress(legacyTower) && towerReload.TowerFloor==1 && !towerReload.InTower);
  var afterStory=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common"},ChapterDefinition.TotalStages);
  Check(afterStory.CampaignComplete && afterStory.EnterTower() && !afterStory.CampaignComplete && afterStory.LeaveTower() && afterStory.CampaignComplete);
  // Collection filters use current ascended rarity and deterministic sorting.
  var filteredCollection=FactionBattle(new[]{"Knight_Common","Paladin_Common","Rogue_Uncommon","Cleric_Uncommon","Sorceress_Rare"});
  Check(filteredCollection.Collection().Count==30 && new Battle(true).Collection().Count==1);
  foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass))) { var matches=filteredCollection.Collection(kind:kind); Check(matches.Count==5); foreach(var member in matches) Check(member.Identity.Class==kind); }
  foreach(HeroFaction faction in Enum.GetValues(typeof(HeroFaction))) { var matches=filteredCollection.Collection(faction:faction); Check(matches.Count==10); foreach(var member in matches) Check(member.Identity.Faction==faction); }
  foreach(HeroQuality quality in Enum.GetValues(typeof(HeroQuality))) { var matches=filteredCollection.Collection(quality:quality); Check(matches.Count==6); foreach(var member in matches) Check(member.Quality==quality); }
  foreach(CollectionSort sorting in Enum.GetValues(typeof(CollectionSort))) { var matches=filteredCollection.Collection(sort:sorting); var repeat=filteredCollection.Collection(sort:sorting); for(int index=0;index<matches.Count;index++) { Check(matches[index].Identity.Id==repeat[index].Identity.Id); if(index>0 && sorting==CollectionSort.Power) Check(matches[index-1].Power>=matches[index].Power); } }
  var collectionData=filteredCollection.ExportProgress(); collectionData.loadouts[0].shards=20; collectionData.loadouts[6].level=20;
  Check(filteredCollection.RestoreProgress(collectionData) && filteredCollection.Collection(sort:CollectionSort.Ascension)[0].Identity.Id=="Knight_Common" && filteredCollection.Collection(sort:CollectionSort.Level)[0].Identity.Id==collectionData.loadouts[6].id);
  Check(filteredCollection.UpgradeHero("Knight_Common")); Check(filteredCollection.Collection(quality:HeroQuality.Common).Count==5 && filteredCollection.Collection(quality:HeroQuality.Uncommon).Count==7);
  // Replacement previews predict all faction changes without mutating live heroes.
  string reserveForPreview="Sorceress_Common"; int beforePreviewPower=filteredCollection.TeamPower, beforePreviewActive=filteredCollection.ActiveIndex;
  int reserveHp=filteredCollection.HeroProgress(reserveForPreview).Definition.MaxHealth, originalSlotHp=filteredCollection.Party[0].Definition.MaxHealth;
  var swapPreview=filteredCollection.PreviewReplacement(reserveForPreview,0);
  Check(swapPreview!=null && swapPreview.TeamPower<beforePreviewPower && filteredCollection.TeamPower==beforePreviewPower && filteredCollection.ActiveIndex==beforePreviewActive && filteredCollection.TeamFactionBonus==20);
  Check(filteredCollection.HeroProgress(reserveForPreview).Definition.MaxHealth==reserveHp && filteredCollection.Party[0].Definition.MaxHealth==originalSlotHp);
  Check(filteredCollection.EquipHeroAt(reserveForPreview,0) && filteredCollection.TeamPower==swapPreview.TeamPower && filteredCollection.Party[0].Definition.MaxHealth==swapPreview.Hero.Definition.MaxHealth && filteredCollection.Party[0].Definition.AttackDamage==swapPreview.Hero.Definition.AttackDamage);
  Check(filteredCollection.TeamFactionBonus==15 && filteredCollection.PreviewReplacement(reserveForPreview,1)==null && !filteredCollection.EquipHeroAt(reserveForPreview,1));
  Check(filteredCollection.PreviewReplacement("missing",0)==null && filteredCollection.PreviewReplacement("Knight_Common",-1)==null && !filteredCollection.EquipHeroAt("Knight_Common",5));
  filteredCollection.Attack(); Check(!filteredCollection.EquipHeroAt("Knight_Common",0));
  var slotLimited=new Battle(true); Check(!slotLimited.EquipHeroAt("Paladin_Common",0) && !slotLimited.EquipHeroAt("Knight_Common",1));
  var collectionReload=new Battle(true,enableFactions:true); Check(collectionReload.RestoreProgress(filteredCollection.ExportProgress()) && collectionReload.Party[0].Identity.Id==reserveForPreview);
  // Saved team presets preserve named heroes and formation order across caps and reloads.
  var presets=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Rare","Rogue_Uncommon","Cleric_Uncommon"});
  var presetGrowth=presets.ExportProgress(); presetGrowth.loadouts[0].level=7; presetGrowth.loadouts[0].upgradeRank=2; presetGrowth.loadouts[0].shards=55; presetGrowth.loadouts[0].firstSkill=2; presetGrowth.loadouts[0].secondSkill=3;
  presetGrowth.inventory=new[]{new GearStackData{id="Weapon_Common_Physical",count=1}}; presetGrowth.loadouts[0].gear=new[]{"Weapon_Common_Physical",null,null}; Check(presets.RestoreProgress(presetGrowth));
  Check(presets.SaveTeamPreset(0)); var originalPreset=presets.TeamPreset(0); Check(originalPreset.Length==5 && originalPreset[0]=="Knight_Common" && originalPreset[4]=="Cleric_Uncommon");
  originalPreset[0]="invalid"; Check(presets.TeamPreset(0)[0]=="Knight_Common");
  Check(presets.SwapFormation(0,4) && presets.SaveTeamPreset(1)); Check(presets.TeamPreset(1)[0]=="Cleric_Uncommon");
  Check(presets.LoadTeamPreset(0) && presets.Party[0].Identity.Id=="Knight_Common" && presets.Party[4].Identity.Id=="Cleric_Uncommon" && presets.TeamFactionBonus==20);
  Check(presets.Party[0].Level==7 && presets.Party[0].UpgradeRank==2 && presets.Party[0].Shards==55 && presets.Party[0].SkillIndex(0)==2 && presets.Party[0].Gear(GearSlot.Weapon).Id=="Weapon_Common_Physical");
  var presetSave=presets.ExportProgress(); var presetReload=new Battle(true,enableWaves:true,enableFactions:true); Check(presetReload.RestoreProgress(presetSave));
  Check(presetReload.LoadTeamPreset(1) && presetReload.Party[0].Identity.Id=="Cleric_Uncommon" && presetReload.TeamPreset(0)[0]=="Knight_Common");
  Check(presetReload.ClearTeamPreset(1) && !presetReload.LoadTeamPreset(1) && !presetReload.ClearTeamPreset(1));
  Check(!presetReload.SaveTeamPreset(-1) && !presetReload.SaveTeamPreset(3) && !presetReload.LoadTeamPreset(3));
  presets.Attack(); Check(!presets.LoadTeamPreset(1) && !presets.SaveTeamPreset(2) && !presets.ClearTeamPreset(0));
  presetSave=presets.ExportProgress(); presetSave.encounter=0; presetSave.party=new[]{"Knight_Common"}; presetSave.activeIndex=0;
  Check(presetReload.RestoreProgress(presetSave) && presetReload.LoadTeamPreset(1) && presetReload.Party.Count==1 && presetReload.Party[0].Identity.Id=="Cleric_Uncommon" && presetReload.TeamPreset(1).Length==5);
  Check(presetReload.EnterTower() && presetReload.LoadTeamPreset(0) && presetReload.Party.Count==5 && presetReload.Party[0].Identity.Id=="Knight_Common" && presetReload.LeaveTower() && presetReload.Party.Count==1 && presetReload.Party[0].Identity.Id=="Cleric_Uncommon");
  var oneHeroPreset=presetReload.ExportProgress(); oneHeroPreset.encounter=20; Check(presetReload.RestoreProgress(oneHeroPreset) && presetReload.SaveTeamPreset(2) && presetReload.LoadTeamPreset(2) && presetReload.Party.Count==5 && presetReload.Party[0].Identity.Id=="Cleric_Uncommon");
  Check(presetReload.TeamPreset(2).Length==1);
  var presetLegacy=presetReload.ExportProgress(); presetLegacy.version=11; Check(presetReload.RestoreProgress(presetLegacy) && presetReload.TeamPreset(0).Length==0);
  var invalidPresets=presets.ExportProgress(); invalidPresets.presets[0].heroes=new[]{"Knight_Common","Knight_Common"}; Check(!presetReload.RestoreProgress(invalidPresets) && presetReload.TeamPreset(0).Length==0);
  invalidPresets=presets.ExportProgress(); invalidPresets.presets[0].heroes=new[]{"unknown"}; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets[0].heroes=new string[]{null}; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets[0].heroes=new string[6]; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets=null; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets=new TeamPresetData[2]; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets[1]=null; Check(!presetReload.RestoreProgress(invalidPresets));
  invalidPresets=presets.ExportProgress(); invalidPresets.presets[1].heroes=null; Check(!presetReload.RestoreProgress(invalidPresets));
  // Late campaign waves have simultaneous groups, including three independent enemy turns.
  for(int stage=30;stage<ChapterDefinition.TotalStages;stage++) for(int wave=0;wave<3;wave++) {
   var group=ChapterDefinition.WaveAt(stage,wave,3);
   Check(group.Count>=2 && group.Count<=3);
   var names=new System.Collections.Generic.HashSet<string>();
   foreach(var foe in group) Check(names.Add(foe.Name) && EnemyVisualDefinition.For(foe.Name)!=null);
   if(wave==2 && stage%5==4) Check(group.Count==2 && group[0].IsBoss && group[1].IsBoss);
   else { Check(group[0].Role==EnemyRole.Defender && group[1].Row==EnemyRow.Back); if(group.Count==3) Check(group[2].Role==EnemyRole.Bruiser); }
  }
  var lateTrio=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Rare","Ranger_Common","Cleric_Common"},31);
  Check(lateTrio.Enemies.Count==3 && lateTrio.SelectEnemy(2));
  int[] beforeTrio=new int[3]; for(int i=0;i<3;i++) { lateTrio.Enemies[i].Health=10000; beforeTrio[i]=lateTrio.Enemies[i].Health; }
  foreach(var hero in lateTrio.Party) hero.Acted=true; lateTrio.Party[0].Acted=false;
  Check(lateTrio.Attack() && lateTrio.Enemies[2].Health<beforeTrio[2] && lateTrio.Enemies[0].Health==beforeTrio[0] && lateTrio.Enemies[1].Health==beforeTrio[1]);
  for(int foeIndex=0;foeIndex<3;foeIndex++) {
   Check(lateTrio.Current==Phase.EnemyWindup && lateTrio.AttackingEnemyIndex==foeIndex && lateTrio.BeginStrike());
   if(lateTrio.IncomingSkill.Action==EnemyAction.Attack) for(int hero=0;hero<lateTrio.Party.Count;hero++)
    if(lateTrio.Party[hero].Health>0 && (lateTrio.IncomingSkill.PartyWide || hero==lateTrio.ActiveIndex)) Check(lateTrio.DefendHero(hero,false,.1f));
   lateTrio.FinishStrike(); Check(lateTrio.Enemies[foeIndex].CompletedStrikes==1);
  }
  Check(lateTrio.Current==Phase.Player && lateTrio.SelectHero(2)); lateTrio.Party[2].Energy=100;
  Check(lateTrio.Attack() && lateTrio.LastUltimate!=null && lateTrio.LastUltimate.AreaDamage && lateTrio.LastUltimateDamage.Length==3);
  for(int i=0;i<3;i++) Check(lateTrio.LastUltimateDamage[i]>0);
  var soloUltimate=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Rare","Ranger_Common","Cleric_Common"},31);
  soloUltimate.Party[0].Energy=100; Check(soloUltimate.SelectEnemy(2) && soloUltimate.Attack());
  Check(soloUltimate.LastUltimate!=null && !soloUltimate.LastUltimate.AreaDamage && soloUltimate.LastUltimateDamage[2]>0 && soloUltimate.LastUltimateDamage[0]==0 && soloUltimate.LastUltimateDamage[1]==0);
  var thirdTargetSave=soloUltimate.ExportProgress(); var thirdReload=new Battle(true,enableWaves:true,enableFactions:true);
  Check(thirdReload.RestoreProgress(thirdTargetSave) && thirdReload.Enemies.Count==3 && thirdReload.WaveIndex==0 && thirdReload.Encounter==31);
  // Equipment previews are isolated, and automatic gearing respects reservations and useful elements.
  var bestGear=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"},20);
  var bestGearSave=bestGear.ExportProgress();
  bestGearSave.inventory=new[]{
   new GearStackData{id="Weapon_Rare_Physical",count=1},new GearStackData{id="Weapon_Epic_Fire",count=1},
   new GearStackData{id="Armor_Legendary_Cold",count=1},new GearStackData{id="Armor_Common_Physical",count=1},
   new GearStackData{id="Accessory_Legendary_Cold",count=1},new GearStackData{id="Accessory_Common_Fire",count=1},new GearStackData{id="Accessory_Epic_Fire",count=1}
  };
  foreach(var loadout in bestGearSave.loadouts) {
   if(loadout.id=="Knight_Common") loadout.gear=new[]{"Weapon_Rare_Physical",null,"Accessory_Legendary_Cold"};
   if(loadout.id=="Sorceress_Legendary") loadout.gear=new[]{null,"Armor_Legendary_Cold",null};
  }
  Check(bestGear.RestoreProgress(bestGearSave));
  Check(bestGear.AvailableGear("Armor_Legendary_Cold")==0 && bestGear.AvailableGear("Weapon_Epic_Fire")==1 && bestGear.AvailableGear("unknown")==0 && bestGear.AvailableGear(null)==0);
  bestGear.Party[0].Health=42; bestGear.Party[0].Energy=50; bestGear.Party[0].Guard=9;
  int oldGearPower=bestGear.Party[0].Power;
  var armorPreview=bestGear.PreviewGear("Knight_Common",GearSlot.Armor,"Armor_Legendary_Cold");
  Check(armorPreview!=null && armorPreview.Definition.MaxHealth>bestGear.Party[0].Definition.MaxHealth && armorPreview.Protection==10);
  Check(bestGear.Party[0].Health==42 && bestGear.Party[0].Energy==50 && bestGear.Party[0].Guard==9 && bestGear.Party[0].Power==oldGearPower && bestGear.Party[0].Gear(GearSlot.Armor)==null);
  Check(bestGear.PreviewGear(null,GearSlot.Armor,null)==null && bestGear.PreviewGear("missing",GearSlot.Armor,null)==null && bestGear.PreviewGear("Knight_Common",(GearSlot)99,null)==null);
  Check(bestGear.PreviewGear("Knight_Common",GearSlot.Armor,"Weapon_Epic_Fire")==null && bestGear.PreviewGear("Knight_Common",GearSlot.Armor,"Armor_Rare_Fire")==null);
  Check(!bestGear.EquipBestGear("Knight_Common")); bestGear.Reset();
  Check(bestGear.EquipBestGear("Knight_Common"));
  Check(bestGear.Party[0].Gear(GearSlot.Weapon).Id=="Weapon_Epic_Fire" && bestGear.Party[0].Gear(GearSlot.Armor).Id=="Armor_Common_Physical" && bestGear.Party[0].Gear(GearSlot.Accessory).Id=="Accessory_Epic_Fire");
  Check(bestGear.HeroProgress("Sorceress_Legendary").Gear(GearSlot.Armor).Id=="Armor_Legendary_Cold" && bestGear.AvailableGear("Armor_Legendary_Cold")==0);
  Check(bestGear.AvailableGear("Weapon_Rare_Physical")==1 && bestGear.AvailableGear("Accessory_Legendary_Cold")==1 && bestGear.AvailableGear("Accessory_Epic_Fire")==0);
  Check(!bestGear.EquipBestGear("Knight_Common") && !bestGear.EquipBestGear(null) && !bestGear.EquipBestGear("missing"));
  Check(bestGear.EquipBestGear("Cleric_Common") && bestGear.HeroProgress("Cleric_Common").Gear(GearSlot.Accessory)==null);
  var bestReload=new Battle(true,enableWaves:true,enableFactions:true);
  Check(bestReload.RestoreProgress(bestGear.ExportProgress()) && bestReload.Party[0].Gear(GearSlot.Accessory).Id=="Accessory_Epic_Fire" && bestReload.AvailableGear("Weapon_Epic_Fire")==0);
  Check(bestGear.Attack() && !bestGear.EquipBestGear("Knight_Common") && bestGear.Party[0].Gear(GearSlot.Accessory).Id=="Accessory_Epic_Fire");
  var tiedGearSave=bestReload.ExportProgress();
  var tiedInventory=new System.Collections.Generic.List<GearStackData>(tiedGearSave.inventory); tiedInventory.Add(new GearStackData{id="Weapon_Epic_Poison",count=1}); tiedGearSave.inventory=tiedInventory.ToArray();
  foreach(var loadout in tiedGearSave.loadouts) if(loadout.id=="Knight_Common") loadout.gear[0]="Weapon_Epic_Poison";
  Check(bestReload.RestoreProgress(tiedGearSave) && !bestReload.EquipBestGear("Knight_Common") && bestReload.Party[0].Gear(GearSlot.Weapon).Id=="Weapon_Epic_Poison");
  var elementGear=PassiveBattle(10,new[]{"Sorceress_Common"}); var elementGearSave=elementGear.ExportProgress();
  foreach(var loadout in elementGearSave.loadouts) if(loadout.id=="Sorceress_Common") { loadout.firstSkill=2; loadout.secondSkill=3; }
  elementGearSave.inventory=new[]{new GearStackData{id="Accessory_Common_Cold",count=1},new GearStackData{id="Accessory_Legendary_Lightning",count=1}};
  Check(elementGear.RestoreProgress(elementGearSave) && elementGear.EquipBestGear("Sorceress_Common") && elementGear.Party[0].Gear(GearSlot.Accessory).Id=="Accessory_Common_Cold");
  Check(elementGear.EquipSkill(0,0) && elementGear.EquipBestGear("Sorceress_Common") && elementGear.Party[0].Gear(GearSlot.Accessory).Id=="Accessory_Legendary_Lightning");
  // Formation recommendations reorder only current heroes and preserve resources and active identity.
  var autoFormation=FactionBattle(new[]{"Cleric_Common","Sorceress_Common","Ranger_Common","Knight_Common","Paladin_Common"},20);
  var autoFormationSave=autoFormation.ExportProgress();
  autoFormationSave.inventory=new[]{new GearStackData{id="Weapon_Rare_Physical",count=1}};
  foreach(var loadout in autoFormationSave.loadouts) {
   if(loadout.id=="Knight_Common") { loadout.gear=new[]{"Weapon_Rare_Physical",null,null}; loadout.firstSkill=4; loadout.secondSkill=5; }
   if(loadout.id=="Cleric_Common") { loadout.experience=10; loadout.shards=12; }
  }
  Check(autoFormation.RestoreProgress(autoFormationSave));
  var recommended=autoFormation.RecommendedFormation();
  Check(string.Join(",",recommended)=="Paladin_Common,Knight_Common,Cleric_Common,Sorceress_Common,Ranger_Common");
  recommended[0]="unknown"; Check(autoFormation.RecommendedFormation()[0]=="Paladin_Common");
  autoFormation.Party[0].Energy=50; autoFormation.Party[0].Guard=11;
  int formationPowerBefore=autoFormation.TeamPower,formationCrystalsBefore=autoFormation.Crystals,formationRecruitsBefore=autoFormation.Recruits.Count;
  var originalActive=autoFormation.Party[autoFormation.ActiveIndex];
  Check(autoFormation.AutoArrangeFormation() && object.ReferenceEquals(originalActive,autoFormation.Party[autoFormation.ActiveIndex]) && autoFormation.ActiveIndex==2);
  Check(autoFormation.Party[0].Identity.Id=="Paladin_Common" && autoFormation.Party[1].Identity.Id=="Knight_Common" && autoFormation.Party[4].Identity.Id=="Ranger_Common");
  Check(autoFormation.TeamPower==formationPowerBefore && autoFormation.Crystals==formationCrystalsBefore && autoFormation.Recruits.Count==formationRecruitsBefore);
  Check(autoFormation.HeroProgress("Cleric_Common").Energy==50 && autoFormation.HeroProgress("Cleric_Common").Guard==11 && autoFormation.HeroProgress("Cleric_Common").Experience==10 && autoFormation.HeroProgress("Cleric_Common").Shards==12);
  Check(autoFormation.HeroProgress("Knight_Common").Gear(GearSlot.Weapon).Id=="Weapon_Rare_Physical" && autoFormation.HeroProgress("Knight_Common").SkillIndex(0)==4 && autoFormation.HeroProgress("Knight_Common").SkillIndex(1)==5);
  Check(!autoFormation.AutoArrangeFormation());
  var arrangedReload=new Battle(true,enableWaves:true,enableFactions:true);
  Check(arrangedReload.RestoreProgress(autoFormation.ExportProgress()) && arrangedReload.ActiveIndex==2 && arrangedReload.Party[0].Identity.Id=="Paladin_Common" && arrangedReload.Party[1].Identity.Id=="Knight_Common");
  Check(autoFormation.Attack()); var lockedFormation=autoFormation.ExportProgress();
  Check(!autoFormation.AutoArrangeFormation() && string.Join(",",autoFormation.ExportProgress().party)==string.Join(",",lockedFormation.party));
  var soloFormation=new Battle(true); Check(soloFormation.RecommendedFormation().Length==1 && !soloFormation.AutoArrangeFormation());
  var nonTankFormation=FactionBattle(new[]{"Cleric_Common","Sorceress_Common","Ranger_Common","Rogue_Common","Cleric_Uncommon"},20);
  Check(nonTankFormation.AutoArrangeFormation() && nonTankFormation.Party[0].Identity.Class==HeroClass.Rogue && nonTankFormation.Party[1].Identity.Class==HeroClass.Ranger);
  // Frequent idle claims give exactly the same crystals/XP as one claim; remainders migrate safely.
  var rewardClock=new DateTime(2026,10,12,0,0,0,DateTimeKind.Utc);
  var frequentIdle=new Battle(true,utcNow:()=>rewardClock); var bulkIdle=new Battle(true,utcNow:()=>rewardClock);
  for(int minute=0;minute<19;minute++) {
   rewardClock=rewardClock.AddMinutes(1); Check(frequentIdle.ClaimIdleRewards());
   var reloadedIdle=new Battle(true,utcNow:()=>rewardClock); Check(reloadedIdle.RestoreProgress(frequentIdle.ExportProgress())); frequentIdle=reloadedIdle;
  }
  Check(bulkIdle.ClaimIdleRewards() && frequentIdle.Crystals==bulkIdle.Crystals && frequentIdle.Party[0].Experience==bulkIdle.Party[0].Experience);
  Check(frequentIdle.Crystals==301 && frequentIdle.ExportProgress().idleCrystalRemainder==9);
  rewardClock=rewardClock.AddMinutes(1); Check(frequentIdle.IdleCrystals==1 && frequentIdle.ClaimIdleRewards() && frequentIdle.Crystals==302 && frequentIdle.ExportProgress().idleCrystalRemainder==0);
  var oldEconomy=frequentIdle.ExportProgress(); oldEconomy.version=12; oldEconomy.idleCrystalRemainder=99;
  Check(frequentIdle.RestoreProgress(oldEconomy) && frequentIdle.Crystals==302 && frequentIdle.ExportProgress().idleCrystalRemainder==0);
  var invalidEconomy=frequentIdle.ExportProgress(); invalidEconomy.idleCrystalRemainder=10;
  Check(!frequentIdle.RestoreProgress(invalidEconomy) && frequentIdle.Crystals==302);
  invalidEconomy.idleCrystalRemainder=-1; Check(!frequentIdle.RestoreProgress(invalidEconomy));
  // Presentation envelopes must settle at anchors without changing action contact times.
  Check(CombatMotion.Recoil(-1)==0 && CombatMotion.Recoil(0)==0 && CombatMotion.Recoil(.34f)==0);
  Check(CombatMotion.Recoil(.068f)>.99f && CombatMotion.Recoil(.2f)>0 && CombatMotion.Recoil(.2f)<1);
  Check(CombatMotion.Stride(0,1,1)==0 && CombatMotion.Stride(1,1,1)==0 && CombatMotion.Stride(.25f,1,0)==0);
  var signatures=new System.Collections.Generic.HashSet<string>();
  foreach(var identity in HeroDefinition.Catalog) {
   var sig=UltimateSignature.For(identity);
   Check(sig.Points>=6 && sig.Points<=16 && sig.Satellites>=3 && sig.Satellites<=7 && sig.Pitch>=.8f && sig.Pitch<=1.2f);
   Check(signatures.Add(sig.Points+":"+sig.Satellites+":"+sig.Rotation));
  }
  Check(signatures.Count==30);
  // Family traits stay tied to visible roles and actual encounter skills.
  var pyre=EnemySkills.ForRole(30,new EnemyDefinition("Ashbound Sentinel",200,role:EnemyRole.Defender),2);
  Check(pyre.Element==Element.Fire && pyre.Status==Debuff.Burn && !pyre.PartyWide);
  var drowned=EnemySkills.ForRole(35,new EnemyDefinition("Grave Archer",200,role:EnemyRole.Archer),2);
  Check(drowned.BackRow && drowned.Status==Debuff.Chill && drowned.Element==Element.Cold);
  var crimson=EnemySkills.ForRole(40,new EnemyDefinition("Scarlet Priest",200,role:EnemyRole.Healer),1);
  Check(crimson.Action==EnemyAction.HealPartner && crimson.Damage==0);
  var glass=EnemySkills.ForRole(45,new EnemyDefinition("Obsidian Guard",200,role:EnemyRole.Defender),1);
  Check(glass.Action==EnemyAction.Shield && glass.Damage==0);
  var rime=EnemySkills.ForRole(50,new EnemyDefinition("Winter Seer",200,role:EnemyRole.Caster),2);
  Check(rime.PartyWide && rime.Status==Debuff.Chill);
  var abyss=EnemySkills.ForRole(55,new EnemyDefinition("Dread Archer",200,role:EnemyRole.Archer),2);
  Check(abyss.BackRow && abyss.Status==Debuff.Poison);
  var abyssHex=EnemySkills.ForRole(55,new EnemyDefinition("Night Prophet",200,role:EnemyRole.Caster),2);
  Check(abyssHex.PartyWide && abyssHex.Status==Debuff.Weaken);
  Check(EnemyRoles.For("Scarlet Priest")==EnemyRole.Healer && EnemyRoles.For("Grave Archer")==EnemyRole.Archer && EnemyRoles.For("Glass Warden")==EnemyRole.Support);
  for(int encounter=30;encounter<60;encounter++) for(int wave=0;wave<3;wave++) {
   var foes=ChapterDefinition.WaveAt(encounter,wave,3);
   if(wave==2 && encounter%5==4) continue;
   Check(foes[0].Role==EnemyRole.Defender && EnemyVisualDefinition.For(foes[0].Name).SourceModel=="LanternWarden");
   Check(foes[1].Row==EnemyRow.Back && EnemyVisualDefinition.For(foes[1].Name).IsRanged);
   if(foes.Count==3) Check(foes[2].Role==EnemyRole.Bruiser && EnemyVisualDefinition.For(foes[2].Name).SourceModel=="AshHound");
  }
  var lateRewards=FactionBattle(new[]{"Knight_Legendary","Paladin_Legendary","Sorceress_Legendary","Ranger_Legendary","Cleric_Legendary"},34);
  var lateRewardSave=lateRewards.ExportProgress(); lateRewardSave.crystals=999999;
  Check(lateRewards.RestoreProgress(lateRewardSave));
  int rewardSafety=0;
  while(lateRewards.Current!=Phase.Won && rewardSafety++<1000) {
   if(lateRewards.Current==Phase.Player) lateRewards.Attack();
   else if(lateRewards.Current==Phase.WaveCleared) lateRewards.AdvanceWave();
   else {
    lateRewards.BeginStrike();
    if(lateRewards.IncomingSkill.Action==EnemyAction.Attack) for(int slot=0;slot<lateRewards.Party.Count;slot++) if(lateRewards.Party[slot].Health>0) lateRewards.DefendHero(slot,false,.1f);
    lateRewards.FinishStrike();
   }
  }
  Check(rewardSafety<1000 && lateRewards.Results.Crystals==105 && lateRewards.Results.Experience==120);
  Check(lateRewards.ContinueAfterVictory(.5,0) && lateRewards.Crystals==1000000);
  Check(new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true).RestoreProgress(lateRewards.ExportProgress()));
  // One-time chapter rewards unlock only after the finale is claimed, never from Tower.
  var chapterRewards=new Battle(true,enableWaves:true); var chapterRewardSave=chapterRewards.ExportProgress();
  Check(!chapterRewards.CanClaimMilestone(0) && !chapterRewards.ClaimMilestone(0) && !chapterRewards.ClaimMilestone(-1) && !chapterRewards.ClaimMilestone(12));
  chapterRewardSave.encounter=5; chapterRewardSave.recruited=new[]{"Knight_Common","Paladin_Common","Cleric_Common"};
  chapterRewardSave.party=new[]{"Knight_Common","Paladin_Common"}; chapterRewardSave.loadouts=new[]{new HeroLoadoutData{id="Knight_Common",secondSkill=1},new HeroLoadoutData{id="Paladin_Common",secondSkill=1},new HeroLoadoutData{id="Cleric_Common",secondSkill=1}};
  Check(chapterRewards.RestoreProgress(chapterRewardSave) && chapterRewards.ReadyMilestones==1 && chapterRewards.ChapterCleared(0) && !chapterRewards.ChapterCleared(1));
  Check(chapterRewards.ClaimMilestone(0) && chapterRewards.Crystals==400 && chapterRewards.ReadyMilestones==0 && chapterRewards.MilestoneClaimed(0));
  foreach(var hero in chapterRewards.Recruits) Check(chapterRewards.HeroProgress(hero.Id).Level==2 && chapterRewards.HeroProgress(hero.Id).Shards==5);
  Check(!chapterRewards.ClaimMilestone(0) && chapterRewards.Crystals==400 && chapterRewards.ExportProgress().claimedMilestones==1);
  var chapterRewardReload=new Battle(true,enableWaves:true); Check(chapterRewardReload.RestoreProgress(chapterRewards.ExportProgress()) && !chapterRewardReload.ClaimMilestone(0));
  var invalidMilestones=chapterRewards.ExportProgress(); invalidMilestones.claimedMilestones=2;
  Check(!chapterRewardReload.RestoreProgress(invalidMilestones) && chapterRewardReload.Crystals==400 && chapterRewardReload.MilestoneClaimed(0));
  invalidMilestones.claimedMilestones=-1; Check(!chapterRewardReload.RestoreProgress(invalidMilestones));
  invalidMilestones.claimedMilestones=4096; Check(!chapterRewardReload.RestoreProgress(invalidMilestones));
  var previousMilestones=chapterRewards.ExportProgress(); previousMilestones.version=13; previousMilestones.claimedMilestones=4096;
  Check(chapterRewardReload.RestoreProgress(previousMilestones) && chapterRewardReload.ExportProgress().version==17 && chapterRewardReload.CanClaimMilestone(0) && chapterRewardReload.Crystals==400);
  Check(chapterRewardReload.Attack() && !chapterRewardReload.ClaimMilestone(0));
  var completeRewards=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"},59);
  var completeRewardSave=completeRewards.ExportProgress(); completeRewardSave.encounter=60; completeRewardSave.crystals=999999;
  Check(completeRewards.RestoreProgress(completeRewardSave) && completeRewards.ReadyMilestones==12 && completeRewards.EnterTower());
  Check(!completeRewards.ClaimMilestone(0) && completeRewards.ReadyMilestones==0 && completeRewards.LeaveTower());
  Check(completeRewards.ClaimMilestone(11) && completeRewards.Crystals==1000000 && completeRewards.MilestoneClaimed(11));
  var lastChapterReload=new Battle(true,enableWaves:true,enableFactions:true); Check(lastChapterReload.RestoreProgress(completeRewards.ExportProgress()) && lastChapterReload.MilestoneClaimed(11) && lastChapterReload.ReadyMilestones==11);
  int milestoneBudget=0;
  for(int chapter=0;chapter<12;chapter++) { var reward=ChapterMilestone.For(chapter); milestoneBudget+=reward.Crystals; Check(reward.Crystals==100+chapter*25 && reward.Experience==100+chapter*25 && reward.Shards==5 && !string.IsNullOrEmpty(reward.Story)); }
  Check(milestoneBudget==2850);
  // Reserve loadouts can be managed without replacing a hero or changing the active slot.
  var reserveLoadout=new Battle(true,enableWaves:true); var reserveLoadoutSave=chapterRewardSave;
  Check(reserveLoadout.RestoreProgress(reserveLoadoutSave)); int reserveActive=reserveLoadout.ActiveIndex;
  Check(reserveLoadout.EquipHeroSkill("Cleric_Common",0,2) && reserveLoadout.HeroProgress("Cleric_Common").SkillIndex(0)==2 && reserveLoadout.Party.Count==2 && reserveLoadout.ActiveIndex==reserveActive);
  Check(!reserveLoadout.EquipHeroSkill("missing",0,2) && !reserveLoadout.EquipHeroSkill(null,0,2) && !reserveLoadout.EquipHeroSkill("Cleric_Common",-1,2) && !reserveLoadout.EquipHeroSkill("Cleric_Common",0,6) && !reserveLoadout.EquipHeroSkill("Cleric_Common",0,1));
  var reserveReload=new Battle(true,enableWaves:true); Check(reserveReload.RestoreProgress(reserveLoadout.ExportProgress()) && reserveReload.HeroProgress("Cleric_Common").SkillIndex(0)==2);
  Check(reserveLoadout.Attack() && !reserveLoadout.EquipHeroSkill("Cleric_Common",0,3));
  Check(completeRewards.EquipHeroSkill("Knight_Common",0,2));
  // Upgrade comparisons preserve all live resources and match the stats actually awarded.
  var upgradePreview=FactionBattle(new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"},20);
  var upgradePreviewSave=upgradePreview.ExportProgress(); upgradePreviewSave.inventory=new[]{new GearStackData{id="Weapon_Rare_Physical",count=1}};
  foreach(var loadout in upgradePreviewSave.loadouts) if(loadout.id=="Knight_Common") { loadout.level=5; loadout.experience=15; loadout.shards=20; loadout.gear=new[]{"Weapon_Rare_Physical",null,null}; loadout.talents=new[]{0,-1,-1}; }
  Check(upgradePreview.RestoreProgress(upgradePreviewSave)); var previewMember=upgradePreview.HeroProgress("Knight_Common");
  previewMember.Health=42; previewMember.Energy=50; previewMember.Guard=9;
  int previewMemberPower=previewMember.Power; var higherQuality=upgradePreview.PreviewAscension("Knight_Common"); var higherLevel=upgradePreview.PreviewNextLevel("Knight_Common");
  Check(higherQuality!=null && higherQuality.Quality==HeroQuality.Uncommon && higherQuality.Power>previewMemberPower && higherQuality.Shards==20);
  Check(higherLevel!=null && higherLevel.Level==6 && higherLevel.Power>previewMemberPower && higherLevel.TalentChoice(0)==0 && higherLevel.Gear(GearSlot.Weapon).Id=="Weapon_Rare_Physical");
  Check(previewMember.Health==42 && previewMember.Energy==50 && previewMember.Guard==9 && previewMember.Level==5 && previewMember.Experience==15 && previewMember.UpgradeRank==0 && previewMember.Shards==20 && previewMember.Power==previewMemberPower);
  Check(upgradePreview.PreviewAscension(null)==null && upgradePreview.PreviewAscension("missing")==null && upgradePreview.PreviewNextLevel(null)==null);
  upgradePreview.Reset(); Check(upgradePreview.UpgradeHero("Knight_Common") && previewMember.Power==higherQuality.Power && previewMember.Definition.MaxHealth==higherQuality.Definition.MaxHealth && previewMember.Ultimate.Damage==higherQuality.Ultimate.Damage);
  var maxPreviewSave=upgradePreview.ExportProgress(); foreach(var loadout in maxPreviewSave.loadouts) if(loadout.id=="Knight_Common") { loadout.level=20; loadout.experience=0; loadout.upgradeRank=5; }
  Check(upgradePreview.RestoreProgress(maxPreviewSave) && upgradePreview.PreviewAscension("Knight_Common")==null && upgradePreview.PreviewNextLevel("Knight_Common")==null);
  // A boss killed by a counter cannot finish its party-wide strike against other heroes.
  var counterCancel=PassiveBattle(44,new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"});
  counterCancel.Enemies[0].Health=5; counterCancel.Enemies[0].CompletedStrikes=2; Check(counterCancel.SelectEnemy(1));
  int counterSafety=0; while(counterCancel.Current==Phase.Player && counterSafety++<10) Check(counterCancel.Attack());
  Check(counterSafety<10 && counterCancel.BeginStrike() && counterCancel.IncomingSkill.PartyWide && counterCancel.DefendHero(0,true,.12f) && counterCancel.Enemies[0].Health==0);
  int otherHeroHealth=counterCancel.Party[2].Health; counterCancel.FinishStrike(); Check(counterCancel.Party[2].Health==otherHeroHealth);
  foreach(int amount in counterCancel.LastIncomingDamage) Check(amount==0);
  foreach(var hero in HeroDefinition.Catalog) if(hero.Class==HeroClass.Rogue && !hero.Ultimate.AreaDamage) Check(SkillPresentation.Ultimate(hero.Class,hero.Ultimate).Motion(hero.Class).SecondImpact==0);
  StrategyAndForgeChecks();
  Console.WriteLine("PASS: " + count + " checks: party/recruitment, 36 skills, loadouts, rarity scaling, elements, defense, battle outcomes, enemy targeting, and simultaneous bosses.");
 }
}
