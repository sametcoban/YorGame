using System;
namespace Ashlight {
    [Serializable]
    public sealed class HeroLoadoutData {
        public string id;
        public int firstSkill, secondSkill;
        public int level = 1;
        public int experience, upgradeRank, shards;
        public string[] gear;
    }
    [Serializable]
    public sealed class GearStackData { public string id; public int count; }
    [Serializable]
    public sealed class ProgressData {
        public int version = 5;
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
