using System;
namespace Ashlight {
    [Serializable]
    public sealed class HeroLoadoutData {
        public string id;
        public int firstSkill, secondSkill;
    }
    [Serializable]
    public sealed class ProgressData {
        public int version = 1;
        public int encounter;
        public bool pendingVictory;
        public string[] recruited;
        public string[] party;
        public HeroLoadoutData[] loadouts;
        public int activeIndex;
    }
}
