using System;
namespace Ashlight {
    [Serializable]
    public sealed class HeroLoadoutData {
        public string id;
        public int firstSkill, secondSkill;
        public int level = 1;
        public int experience, upgradeRank, shards;
        public string[] gear;
        public int[] talents={-1,-1,-1};
    }
    [Serializable]
    public sealed class TeamPresetData { public string[] heroes=new string[0]; }
    [Serializable]
    public sealed class GearStackData { public string id; public int count; }
    [Serializable]
    public sealed class ProgressData {
        public int version = 12;
        public TeamPresetData[] presets={new TeamPresetData(),new TeamPresetData(),new TeamPresetData()};
        public bool inTower;
        public int towerFloor=1;
        public string[] towerCampaignParty=new string[0];
        public long dailyDay;
        public int[] dailyProgress=new int[3];
        public bool[] dailyClaimed=new bool[4];
        public string[] sharedHeroes=new string[0];
        public long idleClaimTicks;
        public int furthestStage;
        public PendingResultsData results;
        public GearStackData[] inventory = new GearStackData[0];
        public int crystals;
        public int rareMisses, legendaryMisses;
        public int encounter;
        public bool pendingVictory;
        public string[] recruited;
        public string[] party;
        public HeroLoadoutData[] loadouts;
        public int activeIndex;
    }
}
