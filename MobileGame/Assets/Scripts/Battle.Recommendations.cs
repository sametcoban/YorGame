using System;
using System.Collections.Generic;
namespace Ashlight {
    public sealed class RecommendedHero {
        public readonly string Id, Reason;
        public readonly int FirstSkill, SecondSkill;
        internal readonly PartyHero Preview;
        internal RecommendedHero(PartyHero hero,int first,int second,string reason) { Id=hero.Identity.Id; FirstSkill=first; SecondSkill=second; Reason=reason; Preview=hero; }
    }
    public sealed class TeamRecommendation {
        public readonly int Encounter, Power, FactionBonus, Floor;
        public readonly bool Tower;
        public readonly IReadOnlyList<RecommendedHero> Heroes;
        internal TeamRecommendation(int encounter,bool tower,int floor,int power,int bonus,List<RecommendedHero> heroes) { Encounter=encounter; Tower=tower; Floor=floor; Power=power; FactionBonus=bonus; Heroes=heroes.AsReadOnly(); }
    }
    public sealed partial class Battle {
        double RecommendationSkillScore(PartyHero hero,int index,List<EnemyDefinition> threats,bool harmful,bool area,bool wards,bool charge) {
            var skill=hero.Definition.Skills[index]; double multiplier=0;
            foreach(var foe in threats) multiplier+=foe.Multiplier(skill.Affinity??hero.Identity.Affinity);
            multiplier/=Math.Max(1,threats.Count);
            double score=skill.Damage*multiplier*hero.DamageBoost(skill.Affinity??hero.Identity.Affinity)+skill.Healing*(skill.PartyHealing?Math.Min(3,PartyLimit):1)*.65+skill.Guard*.45;
            if((skill.Utility&SkillUtility.Cleanse)!=0 && harmful) score+=skill.PartyHealing?35:18;
            if((skill.Utility&SkillUtility.PartyShield)!=0 && area) score+=35;
            if((skill.Utility&SkillUtility.Interrupt)!=0 && (charge || wards || threats.Exists(e=>e.Role==EnemyRole.Healer || e.Role==EnemyRole.Caster))) score+=30;
            if((skill.Utility&SkillUtility.Dispel)!=0 && wards) score+=30;
            if((skill.Utility&SkillUtility.Expose)!=0 && PartyLimit>=3) score+=20;
            return score;
        }
        public TeamRecommendation RecommendTeam() {
            if(!campaign || IsDailyBoss || (!CanChangeParty && !CampaignComplete)) return null;
            var threats=new List<EnemyDefinition>(); bool harmful=false,area=false,wards=false,charge=false;
            for(int wave=0;wave<WaveCount;wave++) foreach(var enemy in ChapterDefinition.WaveAt(EnemyEncounter,wave,WaveCount)) {
                threats.Add(enemy); charge|=enemy.Name=="Cinder Sovereign";
                wards|=enemy.Role==EnemyRole.Support || enemy.Name=="Veiled Monarch";
                for(int round=0;round<3;round++) { var move=EnemySkills.ForRole(EnemyEncounter,enemy,round); harmful|=move.Status!=Debuff.None; area|=move.PartyWide; wards|=move.Action==EnemyAction.Shield; }
            }
            var pool=new List<RecommendedHero>();
            foreach(var recruit in recruits) {
                var hero=PreviewCopy(HeroProgress(recruit.Id),0); int first=0,second=1;
                double best=-1,next=-1;
                for(int i=0;i<6;i++) { double score=RecommendationSkillScore(hero,i,threats,harmful,area,wards,charge);
                    if(score>best) { second=first; next=best; first=i; best=score; } else if(score>next) { second=i; next=score; }
                }
                hero.Equip(0,first); hero.Equip(1,second);
                string reason=hero.Identity.Class==HeroClass.Cleric?"Team healing and recovery":hero.Identity.Class==HeroClass.Knight || hero.Identity.Class==HeroClass.Paladin?"Durable front-line option":hero.Identity.Class==HeroClass.Rogue && threats.Exists(e=>e.Role==EnemyRole.Defender)?"Bypasses defenders protecting the rear":"Damage and elemental coverage";
                if((hero.Skill(0).Utility&SkillUtility.Interrupt)!=0 || (hero.Skill(1).Utility&SkillUtility.Interrupt)!=0) reason+=" · interrupt";
                var utilities=hero.Skill(0).Utility|hero.Skill(1).Utility;
                if(harmful && (utilities&SkillUtility.Cleanse)!=0) reason+=" · cleanse";
                if(area && (utilities&SkillUtility.PartyShield)!=0) reason+=" · party shield";
                if(wards && (utilities&SkillUtility.Dispel)!=0) reason+=" · dispel";
                pool.Add(new RecommendedHero(hero,first,second,reason));
            }
            var chosen=new List<RecommendedHero>(); var counts=new int[3];
            while(chosen.Count<Math.Min(PartyLimit,pool.Count)) {
                RecommendedHero pick=null; double best=double.MinValue;
                foreach(var item in pool) if(!chosen.Contains(item)) {
                    var hero=item.Preview; double elemental=0; foreach(var foe in threats) elemental+=foe.Multiplier(hero.Identity.Affinity); elemental/=Math.Max(1,threats.Count);
                    double score=hero.Power*.7+hero.Definition.AttackDamage*elemental*2;
                    bool healer=chosen.Exists(h=>h.Preview.Identity.Class==HeroClass.Cleric);
                    bool tank=chosen.Exists(h=>h.Preview.Identity.Class==HeroClass.Knight || h.Preview.Identity.Class==HeroClass.Paladin);
                    if(PartyLimit>1 && hero.Identity.Class==HeroClass.Cleric) score+=healer?-80:90;
                    if(PartyLimit>1 && (hero.Identity.Class==HeroClass.Knight || hero.Identity.Class==HeroClass.Paladin)) score+=tank?0:60;
                    if(FactionsEnabled) score+=FactionRules.Bonus(counts[(int)hero.Identity.Faction]+1)*4;
                    if(score>best || (score==best && (pick==null || string.CompareOrdinal(item.Id,pick.Id)<0))) { best=score; pick=item; }
                }
                if(pick==null) break; chosen.Add(pick); counts[(int)pick.Preview.Identity.Faction]++;
            }
            chosen.Sort((a,b)=> { int score=FrontlineScore(b.Preview).CompareTo(FrontlineScore(a.Preview)); return score!=0?score:string.CompareOrdinal(a.Id,b.Id); });
            int bonus=FactionsEnabled?FactionRules.Bonus(Math.Max(counts[0],Math.Max(counts[1],counts[2]))):0,power=0;
            foreach(var hero in chosen) { var preview=PreviewCopy(hero.Preview,bonus); power+=preview.Power; }
            return new TeamRecommendation(EnemyEncounter,InTower,TowerFloor,power,bonus,chosen);
        }
        public bool ApplyRecommendation(TeamRecommendation recommendation) {
            if(!CanChangeParty || !campaign || IsDailyBoss || recommendation==null || recommendation.Encounter!=EnemyEncounter || recommendation.Tower!=InTower || (InTower && recommendation.Floor!=TowerFloor) || recommendation.Heroes.Count<1 || recommendation.Heroes.Count>PartyLimit) return false;
            var ids=new HashSet<string>(); var lineup=new List<PartyHero>();
            foreach(var row in recommendation.Heroes) {
                var hero=HeroProgress(row.Id); if(hero==null || !ids.Add(row.Id) || row.FirstSkill<0 || row.FirstSkill>=6 || row.SecondSkill<0 || row.SecondSkill>=6 || row.FirstSkill==row.SecondSkill) return false;
                lineup.Add(hero);
            }
            foreach(var hero in party) savedHeroes[hero.Identity.Id]=hero;
            party.Clear(); party.AddRange(lineup);
            for(int i=0;i<lineup.Count;i++) { lineup[i].Equip(0,recommendation.Heroes[i].FirstSkill); lineup[i].Equip(1,recommendation.Heroes[i].SecondSkill); }
            ActiveIndex=0; RefreshSharedLevels(); RefreshFactionStats(); return true;
        }
    }
}
