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
            int target = -1, bestPriority = -1;
            for (int i=0; i<battle.Enemies.Count; i++) {
                var foe=battle.Enemies[i];
                if(foe.Health==0) continue;
                int priority=foe.Charged?3:foe.Shield>0?0:(foe.Definition.Role==EnemyRole.Healer || foe.Definition.Role==EnemyRole.Support || (foe.Definition.Row==EnemyRow.Back && (battle.Party[battle.ActiveIndex].Identity.Class==HeroClass.Rogue || battle.Party[battle.ActiveIndex].Identity.Class==HeroClass.Ranger)))?2:1;
                if(target<0 || priority>bestPriority || (priority==bestPriority && foe.Health<battle.Enemies[target].Health)) {
                    target=i; bestPriority=priority;
                }
            }
            if(target<0) return battle.SelectedEnemyIndex;
            return target;
        }
        public static int ChooseSkill(Battle battle) {
            var hero=battle.Party[battle.ActiveIndex];
            bool weakened=hero.Status==Debuff.Chill || hero.Status==Debuff.Weaken;
            int choice=-1; float best=battle.Enemy.Damage(hero.Definition.AttackDamage,hero.Identity.Affinity);
            best=(float)System.Math.Ceiling(best*battle.PassiveDamageMultiplier(hero,battle.Enemy,hero.Identity.Affinity));
            best=(float)System.Math.Ceiling(best*hero.DamageBoost(hero.Identity.Affinity));
            if(weakened) best=(float)System.Math.Ceiling(best*.75f);
            if(battle.EnemyProtected(battle.SelectedEnemyIndex) && hero.Identity.Class!=HeroClass.Rogue) best=(float)System.Math.Ceiling(best*.8);
            for (int slot=0;slot<2;slot++) {
                if (hero.SkillCharges(slot)==0) continue;
                var skill=hero.Skill(slot);
                float score=battle.Enemy.Damage(skill.Damage,skill.Affinity ?? hero.Identity.Affinity);
                score=(float)System.Math.Ceiling(score*battle.PassiveDamageMultiplier(hero,battle.Enemy,skill.Affinity ?? hero.Identity.Affinity));
                score=(float)System.Math.Ceiling(score*hero.DamageBoost(skill.Affinity ?? hero.Identity.Affinity));
                if(weakened) score=(float)System.Math.Ceiling(score*.75f);
                if(battle.EnemyProtected(battle.SelectedEnemyIndex) && hero.Identity.Class!=HeroClass.Rogue) score=(float)System.Math.Ceiling(score*.8);
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
