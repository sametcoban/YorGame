using System;
using Ashlight;
class BattleChecks {
 static int count;
 static void Check(bool ok) { count++; if(!ok) throw new Exception("Failed check " + count); }
 static void Main() {
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
   Check(campaign.Party.Count == Math.Min(3, encounter + 2));
   Check(campaign.Recruits.Count == encounter + 2);
  }
  Check(campaign.Recruits.Count == 30 && campaign.Party.Count == 3);
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
    duel.BeginStrike(); duel.FinishStrike();
    Check(duel.HeroHealth == duel.Hero.MaxHealth - Math.Max(0,35-skills[index].Guard));
    Check(duel.Party[0].Guard == 0);
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
  for(int encounter=0;encounter<2;encounter++) {
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
  invalidSave=campaign.ExportProgress();invalidSave.party=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common"};
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
  Check(ChapterDefinition.Catalog.Count==6 && ChapterDefinition.TotalStages==30);
  var enemyNames=new System.Collections.Generic.HashSet<string>();
  for(int chapter=0;chapter<6;chapter++) for(int stage=0;stage<5;stage++) {
   var foe=ChapterDefinition.EnemyAt(chapter*5+stage);
   Check(enemyNames.Add(foe.Name));
   Check(foe.MaxHealth>0 && foe.Weakness!=foe.Resistance);
  }
  // Completion survives saves until the player explicitly restarts the campaign.
  resumed.Reset();
  while(resumed.Current!=Phase.Won) {
   if(resumed.Current==Phase.Player) resumed.Attack();
   else { resumed.BeginStrike();resumed.Defend(false,.1f);resumed.FinishStrike(); }
  }
  Check(resumed.ContinueAfterVictory() && resumed.CampaignComplete && resumed.Current==Phase.Complete);
  Check(!resumed.Attack() && !resumed.UseAbility() && !resumed.ContinueAfterVictory());
  var completed=new Battle(true);
  Check(completed.RestoreProgress(resumed.ExportProgress()) && completed.CampaignComplete && completed.Encounter==30);
  var retained=completed.ExportProgress();
  retained.crystals=913; retained.rareMisses=2; retained.legendaryMisses=9;
  foreach(var loadout in retained.loadouts) { loadout.firstSkill=4; loadout.secondSkill=5; }
  Check(completed.RestoreProgress(retained));
  Check(completed.RestartCampaign() && !completed.CampaignComplete && completed.Encounter==0 && completed.Current==Phase.Player && completed.CanChangeParty);
  var replaySave=completed.ExportProgress();
  Check(replaySave.crystals==retained.crystals && replaySave.rareMisses==retained.rareMisses && replaySave.legendaryMisses==retained.legendaryMisses);
  Check(string.Join(",",replaySave.recruited)==string.Join(",",retained.recruited) && string.Join(",",replaySave.party)==string.Join(",",retained.party));
  for(int i=0;i<retained.loadouts.Length;i++) Check(replaySave.loadouts[i].id==retained.loadouts[i].id && replaySave.loadouts[i].firstSkill==retained.loadouts[i].firstSkill && replaySave.loadouts[i].secondSkill==retained.loadouts[i].secondSkill);
  foreach(var ally in completed.Party) Check(ally.Health==ally.Definition.MaxHealth && ally.SkillCharges(0)==2 && ally.SkillCharges(1)==2 && !ally.Acted);
  Check(!completed.RestartCampaign() && completed.Encounter==0);
  Check(!new Battle().RestartCampaign());
  var replayReload=new Battle(true);
  Check(replayReload.RestoreProgress(replaySave) && !replayReload.CampaignComplete && replayReload.Encounter==0 && replayReload.Attack());
  while(completed.Current!=Phase.Won) {
   if(completed.Current==Phase.Player) completed.Attack();
   else {completed.BeginStrike();completed.Defend(false,.1f);completed.FinishStrike();}
  }
  Check(!completed.RestartCampaign() && completed.Current==Phase.Won);
  Check(completed.ContinueAfterVictory() && completed.Encounter==1 && completed.Crystals==retained.crystals+Summoning.VictoryCrystals && completed.Recruits.Count==retained.recruited.Length);
  var winner=new Battle(true);
  while(winner.Current!=Phase.Won) {
   if(winner.Current==Phase.Player) winner.Attack();
   else {winner.BeginStrike();winner.Defend(false,.1f);winner.FinishStrike();}
  }
  var wonSave=winner.ExportProgress();var wonReload=new Battle(true);
  Check(wonSave.pendingVictory && wonReload.RestoreProgress(wonSave) && wonReload.Current==Phase.Won);
  Check(wonReload.ContinueAfterVictory() && wonReload.Encounter==1 && wonReload.Recruits.Count==2);
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
  Check(migrated.RestoreProgress(legacy) && migrated.Crystals==300 && migrated.ExportProgress().version==2);
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
  for(int chapter=0;chapter<6;chapter++) for(int stage=0;stage<5;stage++) {
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
  paired.Reset();Check(paired.Enemies[0].Health==400 && paired.Enemies[1].Health==280 && paired.SelectedEnemyIndex==0 && paired.CanChangeParty);
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
  // Clear all four paired encounters through the public combat API.
  for(int chapter=2;chapter<6;chapter++) {
   var run=new Battle(true);var snapshot=run.ExportProgress();snapshot.encounter=chapter*5+4;
   Check(run.RestoreProgress(snapshot));int turns=0;
   while(run.Current!=Phase.Won && turns++<300) {
    if(run.Current==Phase.Player) Check(run.Attack());
    else { Check(run.BeginStrike() && run.Defend(false,.1f));run.FinishStrike(); }
   }
   Check(run.Current==Phase.Won && run.Enemies[0].Health==0 && run.Enemies[1].Health==0);
   Check(run.ContinueAfterVictory() && run.Encounter==(chapter+1)*5 && run.Crystals==350);
   Check(run.CampaignComplete==(chapter==5));
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
     if(outcome!=DefenseOutcome.Miss) Check(automated.Defend(outcome==DefenseOutcome.Parry,.1f));
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
  Check(skillBattle.Attack()); Check(skillBattle.IncomingSkill.Status==Debuff.Poison);
  skillBattle.BeginStrike(); skillBattle.FinishStrike();
  Check(skillBattle.Party[0].Status==Debuff.Poison && skillBattle.Party[0].StatusRounds==2);
  skillBattle.BeginStrike(); Check(skillBattle.Defend(false,.1f)); skillBattle.FinishStrike();
  Check(skillBattle.LastStatusDamage[0]==8 && skillBattle.Party[0].StatusRounds==1);
  skillBattle.Reset(); Check(skillBattle.Party[0].Status==Debuff.None && skillBattle.Enemies[0].CompletedStrikes==0);
  // Party-wide skills defend each living hero independently and consume guard only on a hit.
  var sweepBattle=new Battle(true); var groupSave=sweepBattle.ExportProgress(); groupSave.encounter=9;
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
  Check(sweepBattle.Party[1].Status==Debuff.Chill && sweepBattle.Party[1].StatusRounds==1);
  Check(sweepBattle.Party[2].Status==Debuff.None && sweepBattle.Party[2].Guard==0);
  int sweepHealthBefore=sweepBattle.EnemyHealth;
  Check(sweepBattle.SelectHero(1) && sweepBattle.Attack());
  Check(sweepHealthBefore-sweepBattle.EnemyHealth==(int)Math.Ceiling(sweepBattle.Party[1].Definition.AttackDamage*.75f));
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
  Console.WriteLine("PASS: " + count + " checks: party/recruitment, 36 skills, loadouts, rarity scaling, elements, defense, battle outcomes, enemy targeting, and simultaneous bosses.");
 }
}
