using System;
using Ashlight;
using UnityEditor;
using UnityEngine;
public static class SaveSerializationChecks {
    static Battle RoundTrip(Battle battle) {
        var restored=new Battle(true);
        if(!restored.RestoreProgress(JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(battle.ExportProgress()))))
            throw new InvalidOperationException("Unity save round-trip rejected: "+battle.Current);
        return restored;
    }
    [MenuItem("Ashlight/Diagnostics/Check Save Serialization")]
    public static void Run() {
        // Isolated test battles: never read or overwrite the player's PlayerPrefs.
        var battle=new Battle(true); RoundTrip(battle);
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
        var team=new Battle(true); var teamSave=team.ExportProgress(); teamSave.encounter=24;
        teamSave.recruited=new[]{"Knight_Common","Paladin_Common","Sorceress_Common","Ranger_Common","Cleric_Common"};
        teamSave.party=(string[])teamSave.recruited.Clone(); teamSave.loadouts=new HeroLoadoutData[teamSave.recruited.Length];
        for(int i=0;i<teamSave.loadouts.Length;i++) teamSave.loadouts[i]=new HeroLoadoutData{id=teamSave.recruited[i],firstSkill=0,secondSkill=1};
        if(!team.RestoreProgress(teamSave) || !team.SwapFormation(0,4)) throw new InvalidOperationException("Could not prepare five-hero formation.");
        var teamRoundTrip=RoundTrip(team);
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
        Debug.Log("PASS: Unity save serialization for fresh progress, combat, pending results, reward claims, equipped gear and five-hero formations/results. Player saves untouched.");
    }
}
