using System;
using System.Collections.Generic;
namespace Ashlight {
    [Serializable]
    public sealed class PendingResultsData {
        public string lootId;
        public bool objectiveMet;
        public int[] damage, healing;
        public int[] support;
    }
    public sealed class HeroBattleResult {
        public readonly string Name;
        public readonly int DamageTaken, WardGranted, WardAbsorbed, Cleanses, Interrupts;
        public readonly int Damage, Healing, LevelBefore, LevelAfter, ExperienceAfter;
        public HeroBattleResult(PartyHero member,int xp) {
            DamageTaken=member.DamageTaken; WardGranted=member.WardGranted; WardAbsorbed=member.WardAbsorbed; Cleanses=member.Cleanses; Interrupts=member.Interrupts;
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
        public readonly int Experience, Crystals, Materials, RecruitShards;
        public readonly bool RecruitDuplicate;
        public readonly string RecruitIntroduction;
        public readonly GearDefinition Loot;
        public GearDefinition BossRelic { get; internal set; }
        public readonly string RecruitName;
        public readonly IReadOnlyList<HeroBattleResult> Heroes;
        public BattleResults(int xp,int crystals,GearDefinition loot,string recruit,IReadOnlyList<PartyHero> party,int materials=0,string introduction=null,bool recruitDuplicate=false,int recruitShards=0) {
            Experience=xp; Crystals=crystals; Loot=loot; RecruitName=recruit; Materials=materials; RecruitIntroduction=introduction; RecruitDuplicate=recruitDuplicate; RecruitShards=recruitShards;
            var rows=new List<HeroBattleResult>(); foreach(var member in party) rows.Add(new HeroBattleResult(member,xp)); Heroes=rows.AsReadOnly();
        }
    }
}
