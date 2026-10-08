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
  // Finish the final chapter and confirm completion is permanent across saves.
  resumed.Reset();
  while(resumed.Current!=Phase.Won) {
   if(resumed.Current==Phase.Player) resumed.Attack();
   else { resumed.BeginStrike();resumed.Defend(false,.1f);resumed.FinishStrike(); }
  }
  Check(resumed.ContinueAfterVictory() && resumed.CampaignComplete && resumed.Current==Phase.Complete);
  Check(!resumed.Attack() && !resumed.UseAbility() && !resumed.ContinueAfterVictory());
  var completed=new Battle(true);
  Check(completed.RestoreProgress(resumed.ExportProgress()) && completed.CampaignComplete && completed.Encounter==30);
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
  Console.WriteLine("PASS: " + count + " checks: party/recruitment, 36 skills, loadouts, rarity scaling, elements, defense, and battle outcomes.");
 }
}
