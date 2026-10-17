using System;
using System.Collections.Generic;

namespace Ashbound.Core
{
    public enum Side { Left = -1, Right = 1 }
    public enum EnemyKind { Skeleton, Wraith }
    public enum AttackStyle { Cleave, RisingCut, Thrust, Spin }
    public enum CombatEventKind { Spawn, Strike, Kill, Miss, Hurt, GameOver, Ultimate }

    public sealed class Enemy
    {
        public int Id { get; internal set; }
        public Side Side { get; internal set; }
        public EnemyKind Kind { get; internal set; }
        public float Distance { get; internal set; }
    }

    public struct CombatEvent
    {
        public CombatEventKind Kind;
        public int EnemyId;
        public Side Side;
        public EnemyKind EnemyKind;
        public float Distance;
        public bool Perfect;
        public bool IsUltimate;
        public AttackStyle Style;
    }

    // This model has no Unity dependencies so the rules can be tested separately.
    // The presentation owns input, pausing, sound, and animation.
    public sealed class CombatSession
    {
        public const float SpawnDistance = 10.5f;
        public const float StrikeRange = 2.65f;
        public const float FrontDistance = 2.25f;
        public const float AdvanceDistance = 1.65f;
        public const int QueueSize = 6;
        public const float PerfectWindow = 0.3f;
        public const float AttackCooldown = 0.17f;
        public const int MaxHealth = 3;
        public const float UltimateMaxCharge = 100f;
        public const float UltimateChargePerKill = 2.5f;

        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<CombatEvent> events = new List<CombatEvent>();
        private static readonly CombatEvent[] noEvents = new CombatEvent[0];
        private Random random;
        private float openingTimer;
        private float attackTimer;
        private bool queueStarted;
        private int nextId;
        private int nextAttackStyle;
        private Side lastSpawn;
        private int sameSideSpawns;

        public IReadOnlyList<Enemy> Enemies { get { return enemies; } }
        public int Health { get; private set; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Kills { get; private set; }
        public float UltimateCharge { get; private set; }
        public bool IsUltimateReady { get { return UltimateCharge >= UltimateMaxCharge; } }
        public float Elapsed { get; private set; }
        public float TimeToStrike { get; private set; }
        public float StrikeTimeLimit { get; private set; }
        public bool IsOver { get { return Health == 0; } }
        public int Wave { get { return 1 + (int)(Elapsed / 20f); } }
        public int Multiplier { get { return 1 + Math.Min(4, Combo / 10); } }

        public CombatSession(int seed) { Reset(seed); }

        public void Reset(int seed)
        {
            random = new Random(seed);
            enemies.Clear();
            events.Clear();
            Health = MaxHealth;
            Score = Combo = BestCombo = Kills = nextId = sameSideSpawns = 0;
            nextAttackStyle = 0;
            Elapsed = attackTimer = TimeToStrike = StrikeTimeLimit = 0f;
            UltimateCharge = 0f;
            openingTimer = 0.65f;
            queueStarted = false;
            lastSpawn = Side.Left;
        }

        public CombatEvent[] DrainEvents()
        {
            if (events.Count == 0) return noEvents;
            var result = events.ToArray();
            events.Clear();
            return result;
        }

        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException("seconds");
            // Fixed steps keep the response deadline consistent after frame hitches.
            while (seconds > 0f && !IsOver)
            {
                float step = Math.Min(seconds, 1f / 60f);
                Step(step);
                seconds -= step;
            }
        }

        private void Step(float dt)
        {
            Elapsed += dt;
            attackTimer = Math.Max(0f, attackTimer - dt);
            if (!queueStarted)
            {
                openingTimer -= dt;
                if (openingTimer > 0f) return;
                queueStarted = true;
                FillQueue();
                ResetStrikeClock();
                return;
            }
            if (enemies.Count == 0)
            {
                FillQueue();
                ResetStrikeClock();
                return;
            }
            // Enemies hold their positions until a kill. If the response window
            // expires, the front enemy strikes once and the next takes its place.
            TimeToStrike = Math.Max(0f, TimeToStrike - dt);
            if (TimeToStrike > 0f) return;
            var attacker = enemies[0];
            enemies.RemoveAt(0);
            Health--;
            Combo = 0;
            events.Add(Event(CombatEventKind.Hurt, attacker));
            if (IsOver) events.Add(Event(CombatEventKind.GameOver, attacker));
            else
            {
                AdvanceQueue();
                FillQueue();
                ResetStrikeClock();
            }
        }

        private void ResetStrikeClock()
        {
            StrikeTimeLimit = Math.Max(0.55f, 1.6f - Elapsed * 0.012f);
            TimeToStrike = StrikeTimeLimit;
        }

        private void AdvanceQueue()
        {
            // Insertion order is also approach order. Compact gaps once after a
            // kill or ultimate so nobody crosses the hero or overtakes a survivor.
            for (int i = 0; i < enemies.Count; i++)
                enemies[i].Distance = FrontDistance + i * AdvanceDistance;
        }

        private void FillQueue()
        {
            while (enemies.Count < QueueSize) Spawn();
        }

        private void Spawn()
        {
            Side side = random.Next(2) == 0 ? Side.Left : Side.Right;
            if (sameSideSpawns >= 2 && side == lastSpawn)
                side = side == Side.Left ? Side.Right : Side.Left;
            sameSideSpawns = side == lastSpawn ? sameSideSpawns + 1 : 1;
            lastSpawn = side;

            EnemyKind kind = Elapsed >= 22f && random.Next(4) == 0
                ? EnemyKind.Wraith : EnemyKind.Skeleton;
            var enemy = new Enemy {
                Id = ++nextId, Side = side, Kind = kind,
                Distance = FrontDistance + enemies.Count * AdvanceDistance
            };
            enemies.Add(enemy);
            events.Add(Event(CombatEventKind.Spawn, enemy));
        }

        public bool Attack(Side side)
        {
            if (side != Side.Left && side != Side.Right)
                throw new ArgumentOutOfRangeException("side");
            if (IsOver || attackTimer > 0f) return false;
            attackTimer = AttackCooldown;
            events.Add(new CombatEvent { Kind = CombatEventKind.Strike, Side = side, Style = (AttackStyle)nextAttackStyle });
            nextAttackStyle = (nextAttackStyle + 1) % 4;

            Enemy target = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var candidate = enemies[i];
                if (candidate.Side == side && candidate.Distance <= StrikeRange &&
                    (target == null || candidate.Distance < target.Distance))
                    target = candidate;
            }
            if (target == null)
            {
                Combo = 0;
                events.Add(new CombatEvent { Kind = CombatEventKind.Miss, Side = side });
                return true;
            }

            DefeatEnemy(target, StrikeTimeLimit - TimeToStrike <= PerfectWindow, false);
            AdvanceQueue();
            FillQueue();
            ResetStrikeClock();
            return true;
        }

        public bool ActivateUltimate(Side side)
        {
            if (side != Side.Left && side != Side.Right)
                throw new ArgumentOutOfRangeException("side");
            if (IsOver || !IsUltimateReady) return false;

            // The ultimate can interrupt a normal swing. Spend its charge before
            // awarding kills so every enemy, including wave kills, gives 2.5.
            UltimateCharge = 0f;
            attackTimer = AttackCooldown;
            events.Add(new CombatEvent { Kind = CombatEventKind.Ultimate, Side = side, IsUltimate = true });
            int previousCount = enemies.Count;
            for (int i = enemies.Count - 1; i >= 0; i--)
                if (enemies[i].Side == side)
                    DefeatEnemy(enemies[i], false, true);
            if (enemies.Count != previousCount)
            {
                AdvanceQueue();
                ResetStrikeClock();
                // Keep the cleared side empty until the next normal kill refills
                // the queue; the ultimate's wave should visibly clear its lane.
            }
            return true;
        }

        private void DefeatEnemy(Enemy target, bool perfect, bool ultimate)
        {
            enemies.Remove(target);
            Combo++;
            BestCombo = Math.Max(BestCombo, Combo);
            Kills++;
            UltimateCharge = Math.Min(UltimateMaxCharge, UltimateCharge + UltimateChargePerKill);
            Score += (perfect ? 150 : 100) * Multiplier;
            var killed = Event(CombatEventKind.Kill, target);
            killed.Perfect = perfect;
            killed.IsUltimate = ultimate;
            events.Add(killed);
        }

        private static CombatEvent Event(CombatEventKind kind, Enemy enemy)
        {
            return new CombatEvent {
                Kind = kind, EnemyId = enemy.Id, Side = enemy.Side,
                EnemyKind = enemy.Kind, Distance = enemy.Distance
            };
        }
    }
}
