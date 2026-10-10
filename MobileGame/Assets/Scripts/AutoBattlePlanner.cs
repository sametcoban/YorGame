namespace Ashlight {
    public enum DefenseOutcome { Miss, Dodge, Parry }
    // Prototype balance: one independent roll per incoming strike, including each boss.
    public static class AutoBattlePlanner {
        public static void DefenseChances(HeroClass kind, out double dodge, out double parry) {
            switch (kind) {
                case HeroClass.Knight: dodge=.15; parry=.45; break;
                case HeroClass.Paladin: dodge=.10; parry=.50; break;
                case HeroClass.Rogue: dodge=.55; parry=.15; break;
                case HeroClass.Ranger: dodge=.50; parry=.10; break;
                case HeroClass.Sorceress: dodge=.35; parry=.05; break;
                default: dodge=.25; parry=.15; break;
            }
        }
        public static DefenseOutcome RollDefense(HeroClass kind, double roll) {
            if (double.IsNaN(roll) || roll < 0 || roll >= 1) throw new System.ArgumentOutOfRangeException("roll");
            double dodge, parry; DefenseChances(kind,out dodge,out parry);
            return roll < parry ? DefenseOutcome.Parry : roll < parry+dodge ? DefenseOutcome.Dodge : DefenseOutcome.Miss;
        }
        public static int ChooseTarget(Battle battle) {
            int target = battle.SelectedEnemyIndex;
            for (int i=0; i<battle.Enemies.Count; i++)
                if (battle.Enemies[i].Health > 0 && (battle.Enemies[target].Health == 0 || battle.Enemies[i].Health < battle.Enemies[target].Health)) target=i;
            return target;
        }
        public static int ChooseSkill(Battle battle) {
            var hero=battle.Party[battle.ActiveIndex];
            bool weakened=hero.Status==Debuff.Chill || hero.Status==Debuff.Weaken;
            int choice=-1; float best=battle.Enemy.Damage(hero.Definition.AttackDamage,hero.Identity.Affinity);
            if(weakened) best=(float)System.Math.Ceiling(best*.75f);
            for (int slot=0;slot<2;slot++) {
                if (hero.SkillCharges(slot)==0) continue;
                var skill=hero.Skill(slot);
                float score=battle.Enemy.Damage(skill.Damage,skill.Affinity ?? hero.Identity.Affinity);
                if(weakened) score=(float)System.Math.Ceiling(score*.75f);
                if (skill.PartyHealing) {
                    foreach (var ally in battle.Party) if (ally.Health>0) score+=System.Math.Min(skill.Healing,ally.Definition.MaxHealth-ally.Health);
                } else score+=System.Math.Min(skill.Healing,hero.Definition.MaxHealth-hero.Health);
                score+=System.Math.Max(0,skill.Guard-hero.Guard)*.5f;
                if (score>best) { best=score; choice=slot; }
            }
            return choice;
        }
    }
}
