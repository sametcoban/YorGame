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
  Check(overrides.UseAbility(0) && overrides.LastElement == Element.Lightning && overrides.LastElementMultiplier == .5f && overrides.LastDamage == 23);
  overrides.Reset(); Check(overrides.SelectHero(2));
  Check(overrides.Attack() && overrides.LastElement == Element.Cold && overrides.LastElementMultiplier == 1f && overrides.LastDamage == 30);
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
  Console.WriteLine("PASS: " + count + " checks: party/recruitment, 36 skills, loadouts, rarity scaling, elements, defense, and battle outcomes.");
 }
}
