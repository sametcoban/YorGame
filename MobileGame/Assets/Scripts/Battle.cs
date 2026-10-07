namespace Ashlight {
    public enum Phase { Player, EnemyWindup, EnemyStrike, Won, Lost }
    public sealed class Battle {
        public int HeroHealth { get; private set; }
        public int EnemyHealth { get; private set; }
        public Phase Current { get; private set; }
        public bool Defended { get; private set; }
        public Battle() { Reset(); }
        public void Reset() { HeroHealth = 100; EnemyHealth = 100; Current = Phase.Player; Defended = false; }
        public bool Attack() {
            if (Current != Phase.Player) return false;
            EnemyHealth = System.Math.Max(0, EnemyHealth - 25);
            Current = EnemyHealth == 0 ? Phase.Won : Phase.EnemyWindup;
            Defended = false;
            return true;
        }
        public bool BeginStrike() {
            if (Current != Phase.EnemyWindup) return false;
            Current = Phase.EnemyStrike; return true;
        }
        public bool Defend(bool parry, float elapsed) {
            if (Current != Phase.EnemyStrike || Defended || elapsed < 0 || elapsed > (parry ? 0.18f : 0.4f)) return false;
            Defended = true;
            if (parry) EnemyHealth = System.Math.Max(0, EnemyHealth - 10);
            if (EnemyHealth == 0) Current = Phase.Won;
            return true;
        }
        public void FinishStrike() {
            if (Current != Phase.EnemyStrike) return;
            if (!Defended) HeroHealth = System.Math.Max(0, HeroHealth - 35);
            Current = HeroHealth == 0 ? Phase.Lost : Phase.Player;
        }
    }
}
