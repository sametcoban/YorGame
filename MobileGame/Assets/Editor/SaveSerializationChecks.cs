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
        Debug.Log("PASS: Unity save serialization for fresh progress, combat, pending results, reward claims and equipped gear. Player saves untouched.");
    }
}
