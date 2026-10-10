using System;
using System.Collections.Generic;
namespace Ashlight {
    [Serializable]
    public sealed class PendingResultsData {
        public string lootId;
        public int[] damage, healing;
    }
    public sealed class HeroBattleResult {
        public readonly string Name;
        public readonly int Damage, Healing, LevelBefore, LevelAfter, ExperienceAfter;
        public HeroBattleResult(PartyHero member,int xp) {
            Name=member.Identity.Name; Damage=member.EncounterDamage; Healing=member.EncounterHealing;
            LevelBefore=member.Level; int level=member.Level,experience=member.Experience;
            if(level<PartyHero.MaxLevel) {
                experience+=xp;
                while(level<PartyHero.MaxLevel && experience>=100+(level-1)*25) { experience-=100+(level-1)*25; level++; }
                if(level==PartyHero.MaxLevel) experience=0;
            }
            LevelAfter=level; ExperienceAfter=experience;
        }
    }
    public sealed class BattleResults {
        public readonly int Experience, Crystals;
        public readonly GearDefinition Loot;
        public readonly string RecruitName;
        public readonly IReadOnlyList<HeroBattleResult> Heroes;
        public BattleResults(int xp,int crystals,GearDefinition loot,string recruit,IReadOnlyList<PartyHero> party) {
            Experience=xp; Crystals=crystals; Loot=loot; RecruitName=recruit;
            var rows=new List<HeroBattleResult>(); foreach(var member in party) rows.Add(new HeroBattleResult(member,xp)); Heroes=rows.AsReadOnly();
        }
    }
}
