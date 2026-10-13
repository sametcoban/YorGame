using System;
using Ashlight;
using UnityEditor;
using UnityEngine;
public static class SaveSerializationChecks {
    static Battle RoundTrip(Battle battle,bool waves=false) {
        var restored=new Battle(true,enableWaves:waves,enableFactions:true);
        if(!restored.RestoreProgress(JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(battle.ExportProgress()))))
            throw new InvalidOperationException("Unity save round-trip rejected: "+battle.Current);
        return restored;
    }
    [MenuItem("Ashlight/Diagnostics/Check Save Serialization")]
    public static void Run() {
        // Isolated test battles: never read or overwrite the player's PlayerPrefs.
        var battle=new Battle(true); var fresh=RoundTrip(battle);
        if(fresh.ExportProgress().idleClaimTicks!=battle.ExportProgress().idleClaimTicks || fresh.FurthestStage!=battle.FurthestStage || fresh.ExportProgress().idleCrystalRemainder!=battle.ExportProgress().idleCrystalRemainder)
            throw new InvalidOperationException("Idle reward timestamp changed during Unity serialization.");
        var fractionClock=new DateTime(2026,10,12,0,0,0,DateTimeKind.Utc);
        var fractionTest=new Battle(true,utcNow:()=>fractionClock); fractionClock=fractionClock.AddMinutes(9);
        if(!fractionTest.ClaimIdleRewards()) throw new InvalidOperationException("Could not prepare fractional idle rewards.");
        var fractionReload=new Battle(true,utcNow:()=>fractionClock);
        if(!fractionReload.RestoreProgress(JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(fractionTest.ExportProgress()))) || fractionReload.ExportProgress().idleCrystalRemainder!=9)
            throw new InvalidOperationException("Fractional idle rewards changed during serialization.");
        fractionClock=fractionClock.AddMinutes(1);
        if(!fractionReload.ClaimIdleRewards() || fractionReload.Crystals!=301 || fractionReload.ExportProgress().idleCrystalRemainder!=0)
            throw new InvalidOperationException("Reload lost fractional idle crystals.");
        var milestoneTest=new Battle(true); var milestoneSave=milestoneTest.ExportProgress(); milestoneSave.encounter=5;
        if(!milestoneTest.RestoreProgress(milestoneSave) || !milestoneTest.ClaimMilestone(0)) throw new InvalidOperationException("Could not prepare chapter milestone fixture.");
        var milestoneReload=RoundTrip(milestoneTest);
        if(!milestoneReload.MilestoneClaimed(0) || milestoneReload.ClaimMilestone(0) || milestoneReload.Crystals!=milestoneTest.Crystals || milestoneReload.HeroProgress("Knight_Common").Shards!=5)
            throw new InvalidOperationException("Chapter rewards changed or became claimable twice during serialization.");
        var forgeTest=new Battle(true); var forgeSave=forgeTest.ExportProgress(); forgeSave.forgeMaterials=1000;
        forgeSave.inventory=new[]{new GearStackData{id="Weapon_Common_Physical",count=1}};
        forgeSave.loadouts[0].gear=new[]{"Weapon_Common_Physical",null,null};
        if(!forgeTest.RestoreProgress(forgeSave) || !forgeTest.EnhanceGear("Weapon_Common_Physical")) throw new InvalidOperationException("Could not prepare forged gear fixture.");
        var forgeReload=RoundTrip(forgeTest);
        if(forgeReload.ForgeMaterials!=990 || forgeReload.EnhancementLevel("Weapon_Common_Physical")!=1 || forgeReload.Party[0].Definition.AttackDamage!=forgeTest.Party[0].Definition.AttackDamage)
            throw new InvalidOperationException("Forged equipment or materials changed during serialization.");
        var storyTest=new Battle(true); var storySave=storyTest.ExportProgress(); storySave.encounter=4;
        storySave.recruited=new[]{"Knight_Common","Paladin_Common"}; storySave.loadouts=new[]{storySave.loadouts[0],new HeroLoadoutData{id="Paladin_Common",secondSkill=1}};
        if(!storyTest.RestoreProgress(storySave)) throw new InvalidOperationException("Could not prepare story arrival fixture.");
        int storyTurns=0;
        while(storyTest.Current!=Phase.Won && storyTurns++<100) {
            if(storyTest.Current==Phase.Player) storyTest.Attack();
            else if(storyTest.Current==Phase.EnemyWindup) { storyTest.BeginStrike(); storyTest.Defend(false,.1f); storyTest.FinishStrike(); }
            else throw new InvalidOperationException("Unexpected story fixture phase: "+storyTest.Current);
        }
        if(storyTest.Current!=Phase.Won) throw new InvalidOperationException("Story fixture battle failed to finish.");
        var storyReload=RoundTrip(storyTest);
        if(!storyReload.Results.RecruitDuplicate || storyReload.Results.RecruitShards!=20 || !storyReload.ContinueAfterVictory() || storyReload.HeroProgress("Paladin_Common").Shards!=20 || storyReload.ContinueAfterVictory())
            throw new InvalidOperationException("Pending story duplicate reward changed or repeated during serialization.");
        var sharedTest=new Battle(true); var sharedSave=sharedTest.ExportProgress();
        sharedSave.recruited=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Rogue_Common","Cleric_Common"};
        sharedSave.loadouts=new HeroLoadoutData[6];
        for(int i=0;i<6;i++) sharedSave.loadouts[i]=new HeroLoadoutData{id=sharedSave.recruited[i],secondSkill=1,level=i<5?15:1};
        if(!sharedTest.RestoreProgress(sharedSave) || !sharedTest.SetShared("Cleric_Common",true)) throw new InvalidOperationException("Could not prepare shared level fixture.");
        if(!sharedTest.EquipHeroSkill("Cleric_Common",0,2)) throw new InvalidOperationException("Could not prepare reserve skill fixture.");
        var sharedRoundTrip=RoundTrip(sharedTest);
        if(sharedRoundTrip.SharedCount!=1 || sharedRoundTrip.HeroProgress("Cleric_Common").Level!=1 || sharedRoundTrip.HeroProgress("Cleric_Common").CombatLevel!=15 || sharedRoundTrip.HeroProgress("Cleric_Common").SkillIndex(0)!=2) throw new InvalidOperationException("Shared levels changed during Unity serialization.");
        var dailyClock=new DateTime(2026,10,11,12,0,0,DateTimeKind.Utc); var dailyTest=new Battle(true,utcNow:()=>dailyClock);
        var dailySave=dailyTest.ExportProgress(); dailySave.dailyProgress=new[]{3,1,1};
        if(!dailyTest.RestoreProgress(dailySave) || !dailyTest.ClaimDaily(0)) throw new InvalidOperationException("Could not prepare daily quest fixture.");
        var dailyRoundTrip=new Battle(true,utcNow:()=>dailyClock);
        if(!dailyRoundTrip.RestoreProgress(JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(dailyTest.ExportProgress()))) || !dailyRoundTrip.DailyClaimed(0) || dailyRoundTrip.ClaimDaily(0) || dailyRoundTrip.DailyProgress(1)!=1) throw new InvalidOperationException("Daily quest claims changed during Unity serialization.");
        var tenTest=new Battle(true); var tenSave=tenTest.ExportProgress(); tenSave.crystals=1000;
        if(!tenTest.RestoreProgress(tenSave) || tenTest.SummonTen(new double[10],new int[10])==null) throw new InvalidOperationException("Could not prepare ten-pull fixture.");
        var tenRoundTrip=RoundTrip(tenTest);
        if(tenRoundTrip.Crystals!=tenTest.Crystals || tenRoundTrip.Recruits.Count!=tenTest.Recruits.Count || tenRoundTrip.RareMisses!=tenTest.RareMisses || tenRoundTrip.LegendaryMisses!=tenTest.LegendaryMisses || tenRoundTrip.HeroProgress("Knight_Common").Shards!=tenTest.HeroProgress("Knight_Common").Shards) throw new InvalidOperationException("Ten-pull rewards changed during Unity serialization.");
        var towerTest=new Battle(true); var towerSave=sharedTest.ExportProgress();
        if(!towerTest.RestoreProgress(towerSave) || !towerTest.EnterTower()) throw new InvalidOperationException("Could not prepare Tower fixture.");
        var towerRoundTrip=RoundTrip(towerTest);
        if(!towerRoundTrip.InTower || towerRoundTrip.TowerFloor!=1 || towerRoundTrip.Party.Count!=5 || !towerRoundTrip.LeaveTower() || towerRoundTrip.Party.Count!=1 || towerRoundTrip.Encounter!=0) throw new InvalidOperationException("Tower state changed during Unity serialization.");
        battle.Attack(); RoundTrip(battle);
        battle.Reset(); var progress=battle.ExportProgress(); progress.loadouts[0].experience=90;
        if(!battle.RestoreProgress(progress)) throw new InvalidOperationException("Could not prepare serialization test battle.");
        int turns=0;
        while(battle.Current!=Phase.Won && turns++<100) {
            if(battle.Current==Phase.Player) battle.Attack();
            else if(battle.Current==Phase.EnemyWindup) { battle.BeginStrike(); battle.Defend(false,.1f); battle.FinishStrike(); }
            else throw new InvalidOperationException("Unexpected serialization test battle phase: "+battle.Current);
        }
        if(battle.Current!=Phase.Won) throw new InvalidOperationException("Serialization test battle failed to finish.");
        var restored=RoundTrip(battle);
        if(restored.Results==null || restored.Results.Loot.Id!=battle.Results.Loot.Id || restored.Results.Heroes[0].Damage!=battle.Results.Heroes[0].Damage)
            throw new InvalidOperationException("Pending victory results changed during Unity serialization.");
        string item=restored.Results.Loot.Id;
        if(!restored.ContinueAfterVictory() || restored.GearCount(item)!=1 || restored.Party[0].Level!=2)
            throw new InvalidOperationException("Reloaded victory rewards were not claimed correctly.");
        restored=RoundTrip(restored);
        var gear=GearDefinition.Find(item);
        if(!restored.EquipGear(restored.Party[0].Identity.Id,gear.Slot,item))
            throw new InvalidOperationException("Claimed gear could not be equipped.");
        var equipped=RoundTrip(restored);
        if(equipped.Party[0].Gear(gear.Slot)==null || equipped.Party[0].Gear(gear.Slot).Id!=item)
            throw new InvalidOperationException("Equipped gear changed during Unity serialization.");
        var team=new Battle(true,enableFactions:true); var teamSave=team.ExportProgress(); teamSave.encounter=24;
        teamSave.recruited=new[]{"Knight_Common","Paladin_Common","Sorceress_Rare","Rogue_Uncommon","Cleric_Uncommon"};
        teamSave.party=(string[])teamSave.recruited.Clone(); teamSave.loadouts=new HeroLoadoutData[teamSave.recruited.Length];
        for(int i=0;i<teamSave.loadouts.Length;i++) teamSave.loadouts[i]=new HeroLoadoutData{id=teamSave.recruited[i],firstSkill=0,secondSkill=1,level=15,upgradeRank=1};
        if(!team.RestoreProgress(teamSave) || !team.SwapFormation(0,4)) throw new InvalidOperationException("Could not prepare five-hero formation.");
        foreach(var hero in team.Party) for(int tier=0;tier<3;tier++) if(!team.ChooseTalent(hero.Identity.Id,tier,tier%2)) throw new InvalidOperationException("Could not prepare saved progression choices.");
        if(!team.SaveTeamPreset(0)) throw new InvalidOperationException("Could not prepare team preset fixture.");
        var teamRoundTrip=RoundTrip(team);
        for(int i=0;i<5;i++) if(teamRoundTrip.TeamPreset(0)[i]!=team.Party[i].Identity.Id) throw new InvalidOperationException("Team preset order changed during Unity serialization.");
        foreach(var hero in team.Party) if(teamRoundTrip.HeroProgress(hero.Identity.Id).RarityLabel!=hero.RarityLabel) throw new InvalidOperationException("Ascension rarity changed during Unity serialization.");
        if(team.TeamFactionBonus!=20 || teamRoundTrip.TeamFactionBonus!=20 || teamRoundTrip.TeamPower!=team.TeamPower || team.Party[0].Definition.MaxHealth!=teamRoundTrip.Party[0].Definition.MaxHealth) throw new InvalidOperationException("Faction bonus changed during Unity serialization.");
        foreach(var hero in team.Party) for(int tier=0;tier<3;tier++) if(teamRoundTrip.HeroProgress(hero.Identity.Id).TalentChoice(tier)!=hero.TalentChoice(tier)) throw new InvalidOperationException("Progression choices changed during Unity serialization.");
        for(int i=0;i<team.Party.Count;i++) if(team.Party[i].Identity.Id!=teamRoundTrip.Party[i].Identity.Id)
            throw new InvalidOperationException("Formation changed during Unity serialization.");
        turns=0;
        while(team.Current!=Phase.Won && turns++<500) {
            if(team.Current==Phase.Player) {
                team.SelectEnemy(AutoBattlePlanner.ChooseTarget(team));
                int skill=AutoBattlePlanner.ChooseSkill(team);
                if(skill<0) team.Attack(); else team.UseAbility(skill);
            } else if(team.Current==Phase.EnemyWindup) {
                team.BeginStrike();
                if(team.IncomingSkill.Action==EnemyAction.Attack)
                    for(int i=0;i<team.Party.Count;i++) if(team.Party[i].Health>0 && (team.IncomingSkill.PartyWide || i==team.ActiveIndex)) team.DefendHero(i,false,.1f);
                team.FinishStrike();
            } else throw new InvalidOperationException("Unexpected five-hero battle phase: "+team.Current);
        }
        if(team.Current!=Phase.Won) throw new InvalidOperationException("Five-hero serialization battle failed to finish.");
        var teamResults=RoundTrip(team);
        if(teamResults.Results==null || teamResults.Results.Heroes.Count!=5 || teamResults.Results.Loot.Id!=team.Results.Loot.Id)
            throw new InvalidOperationException("Five-hero results changed during Unity serialization.");
        for(int i=0;i<5;i++) if(teamResults.Results.Heroes[i].Damage!=team.Results.Heroes[i].Damage || teamResults.Results.Heroes[i].Healing!=team.Results.Heroes[i].Healing)
            throw new InvalidOperationException("Five-hero contributions changed during Unity serialization.");
        var continued=team.ExportProgress(); continued.encounter=30; continued.pendingVictory=false; continued.results=null;
        var continuation=new Battle(true,enableWaves:true,enableFactions:true);
        if(!continuation.RestoreProgress(continued)) throw new InvalidOperationException("Could not prepare chapter seven continuation.");
        var resumedChapter=RoundTrip(continuation,true);
        if(resumedChapter.CampaignComplete || resumedChapter.ChapterIndex!=6 || resumedChapter.Crystals!=continued.crystals || resumedChapter.Party.Count!=5)
            throw new InvalidOperationException("Original campaign completion did not continue into chapter seven.");
        continued.encounter=31;
        if(!continuation.RestoreProgress(continued) || continuation.Enemies.Count!=3 || !continuation.SelectEnemy(2))
            throw new InvalidOperationException("Could not prepare three-enemy serialization fixture.");
        var tripleRoundTrip=RoundTrip(continuation,true);
        if(tripleRoundTrip.Enemies.Count!=3 || tripleRoundTrip.Encounter!=31 || !tripleRoundTrip.SelectEnemy(2))
            throw new InvalidOperationException("Three-enemy stage changed during Unity serialization.");
        Debug.Log("PASS: Unity save serialization for fresh progress, combat, pending results, reward claims, equipped gear and five-hero formations/results, progression choices, chapter-seven continuation and three-enemy stages. Player saves untouched.");
    }
}
