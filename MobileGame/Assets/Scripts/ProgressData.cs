using System;
namespace Ashlight {
    [Serializable]
    public sealed class HeroLoadoutData {
        public string id;
        public int firstSkill, secondSkill;
        public int level = 1;
        public int experience, upgradeRank, shards;
    }
    [Serializable]
    public sealed class ProgressData {
        public int version = 3;
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
