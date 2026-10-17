using System;
using System.Collections.Generic;
using Ashbound.Core;

// Dependency-free console regression suite. Kept outside Assets so it is never
// shipped with the game. The runner compiles this against the actual combat model.
internal static class CombatSessionTests
{
    private static int passed;
    private static int failed;

    public static int Main()
    {
        Run("First enemy waits for the opening grace period", OpeningGrace);
        Run("In-range strike kills exactly one enemy and awards souls", Hit);
        Run("Quick response after an advance awards the perfect bonus", Perfect);
        Run("Wrong-side strike misses and breaks the chain", WrongSide);
        Run("Cooldown rejects repeated attacks without extra events", Cooldown);
        Run("Three wounds end the run exactly once", Defeat);
        Run("Reset clears the previous run and restores health", Reset);
        Run("Equal seeds produce equal encounters", Deterministic);
        Run("Wraiths unlock after the opening phase", Wraiths);
        Run("Long-run combat scales the multiplier and remains survivable", LongRun);
        Run("Kill-driven reinforcements leave no gaps between targets", EncounterPacing);
        Run("A large tick agrees with normal frame updates", FrameHitch);
        Run("Invalid time and attack input are rejected", InvalidInput);
        Run("Every kill grants exactly 2.5 ultimate charge and forty fill it", UltimateCharge);
        Run("Perfect strikes grant the same ultimate charge as normal kills", PerfectCharge);
        Run("Misses and timeouts preserve charge without awarding kills", PreserveCharge);
        Run("Ultimate is unavailable below 100 and after defeat", UltimateUnavailable);
        Run("Left wave clears every left enemy at any distance", UltimateLeft);
        Run("Right wave clears every right enemy at any distance", UltimateRight);
        Run("Ultimate can interrupt a normal attack and stops immediate repeats", UltimateInterrupt);
        Run("An empty-side ultimate spends its charge and still emits a wave", EmptyUltimate);
        Run("Restart clears accumulated ultimate charge", UltimateReset);
        Run("Normal attacks cycle all four styles in either direction", AttackStyleCycle);
        Run("Rejected cooldown inputs do not skip attack styles", AttackStyleCooldown);
        Run("Restart returns the attack sequence to cleave", AttackStyleReset);
        Run("Ultimate has its own animation without advancing normal styles", AttackStyleUltimate);
        Run("Enemies hold their positions while no kill occurs", StationaryQueue);
        Run("Each normal kill advances survivors exactly one step", KillAdvancesQueue);
        Run("Wrong-side attacks do not move the queue or reset its deadline", MissHoldsQueue);
        Run("Timeout wounds once and gives the next enemy a fresh deadline", TimeoutAdvance);
        Run("Ultimate compacts survivors safely and the next kill replenishes them", UltimateReplenishment);
        Run("Clearing the entire queue cannot stall the run", EmptyQueueRecovery);
        Console.WriteLine("\n" + passed + " passed; " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static Enemy OpenQueue(CombatSession game)
    {
        game.Tick(0.7f);
        Check(game.Enemies.Count == 6 && game.Health == 3, "Opening queue was not ready.");
        return game.Enemies[0];
    }

    private static void OpeningGrace()
    {
        var game = new CombatSession(1);
        game.Tick(0.6f);
        Check(game.Enemies.Count == 0, "Enemy spawned too early.");
        game.Tick(0.1f);
        Check(game.Enemies.Count == 6 && game.Enemies[0].Distance <= CombatSession.StrikeRange,
            "Opening must provide a full queue with its first enemy already in range.");
        Check(game.Health == 3 && game.Score == 0, "Incorrect initial state.");
    }

    private static void Hit()
    {
        var game = new CombatSession(2);
        Enemy target = OpenQueue(game);
        game.Tick(0.35f); // A valid slower strike earns the base reward.
        int count = game.Enemies.Count;
        game.DrainEvents();
        Check(game.Attack(target.Side), "Strike was rejected.");
        Check(game.Enemies.Count == count && !new List<Enemy>(game.Enemies).Contains(target),
            "Strike did not replace exactly the defeated enemy.");
        Check(game.Score == 100 && game.Kills == 1 && game.Combo == 1, "Incorrect hit rewards.");
        var events = game.DrainEvents();
        Check(events.Length == 3 && events[0].Kind == CombatEventKind.Strike &&
            events[1].Kind == CombatEventKind.Kill && events[1].EnemyId == target.Id &&
            events[2].Kind == CombatEventKind.Spawn, "Presentation did not receive the strike, kill, and reinforcement.");
    }

    private static void Perfect()
    {
        var game = new CombatSession(3);
        Enemy target = OpenQueue(game);
        game.DrainEvents();
        game.Attack(target.Side);
        Check(game.Score == 150, "Perfect strike should award 150 souls.");
        var events = game.DrainEvents();
        Check(events[1].Kind == CombatEventKind.Kill && events[1].Perfect, "Perfect flag missing.");
        Check(game.Health == 3, "Enemy wounded player before contact.");
    }

    private static void WrongSide()
    {
        var game = new CombatSession(4);
        var target = OpenQueue(game);
        game.Tick(0.35f);
        game.Attack(target.Side);
        game.Tick(0.2f);
        int count = game.Enemies.Count;
        game.DrainEvents();
        game.Attack(game.Enemies[0].Side == Side.Left ? Side.Right : Side.Left);
        Check(game.Combo == 0 && game.BestCombo == 1, "Miss must break current chain and retain best.");
        Check(game.Enemies.Count == count && game.Score == 100, "Out-of-range strike killed an enemy.");
        var events = game.DrainEvents();
        Check(events[events.Length - 1].Kind == CombatEventKind.Miss, "Miss event missing.");
    }

    private static void Cooldown()
    {
        var game = new CombatSession(1);
        Check(game.Attack(Side.Left), "First strike rejected.");
        game.DrainEvents();
        game.Tick(0.05f);
        Check(!game.Attack(Side.Right), "Cooldown allowed a second strike.");
        Check(game.DrainEvents().Length == 0, "Rejected strike generated events.");
        game.Tick(0.14f);
        Check(game.Attack(Side.Right), "Strike rejected after cooldown expired.");
    }

    private static void Defeat()
    {
        var game = new CombatSession(5);
        game.Tick(60f);
        Check(game.IsOver && game.Health == 0, "Idle run should end after three wounds.");
        int wounds = 0, ends = 0;
        foreach (var e in game.DrainEvents())
        {
            if (e.Kind == CombatEventKind.Hurt) wounds++;
            if (e.Kind == CombatEventKind.GameOver) ends++;
        }
        Check(wounds == 3 && ends == 1, "Incorrect wound/game-over event count.");
        float elapsed = game.Elapsed;
        game.Tick(20f);
        Check(!game.Attack(Side.Left), "Dead player can attack.");
        Check(game.Elapsed == elapsed && game.DrainEvents().Length == 0, "Dead run kept advancing.");
    }

    private static void Reset()
    {
        var game = new CombatSession(7);
        game.Tick(60f);
        game.Reset(7);
        Check(!game.IsOver && game.Health == 3 && game.Score == 0 && game.Combo == 0 &&
            game.BestCombo == 0 && game.Kills == 0 && game.Elapsed == 0f && game.Wave == 1,
            "Reset left stale run state.");
        Check(game.Enemies.Count == 0 && game.DrainEvents().Length == 0, "Reset left stale enemies/events.");
        var fresh = new CombatSession(7);
        game.Tick(1f); fresh.Tick(1f);
        Check(game.Enemies[0].Id == 1 && game.Enemies[0].Side == fresh.Enemies[0].Side,
            "Reset did not reset ID or encounter seed.");
    }

    private static void Deterministic()
    {
        var a = new CombatSession(23);
        var b = new CombatSession(23);
        for (int i = 0; i < 600; i++) { a.Tick(1f / 120f); b.Tick(1f / 120f); }
        Check(a.Enemies.Count == b.Enemies.Count, "Encounter counts differ.");
        for (int i = 0; i < a.Enemies.Count; i++)
            Check(a.Enemies[i].Side == b.Enemies[i].Side && a.Enemies[i].Distance == b.Enemies[i].Distance,
                "Equal seeds yielded different encounters.");
    }

    private static void Wraiths()
    {
        var game = new CombatSession(11);
        int wraiths = 0;
        for (int i = 0; i < 120 * 120; i++)
        {
            game.Tick(1f / 120f);
            AttackNearest(game);
            foreach (var e in game.DrainEvents())
                if (e.Kind == CombatEventKind.Spawn && e.EnemyKind == EnemyKind.Wraith)
                {
                    Check(game.Elapsed >= 22f, "Wraith arrived before its introduction.");
                    wraiths++;
                }
        }
        Check(wraiths > 0, "No wraiths appeared during two minutes of play.");
        Check(!game.IsOver, "Automated player could not survive the encounters.");
    }

    private static void LongRun()
    {
        var game = new CombatSession(31);
        int kills = 0, score = 0;
        for (int i = 0; i < 180 * 120; i++)
        {
            game.Tick(1f / 120f);
            AttackNearest(game);
            foreach (var e in game.DrainEvents())
            {
                if (e.Kind != CombatEventKind.Kill) continue;
                kills++;
                score += (e.Perfect ? 150 : 100) * (1 + Math.Min(4, kills / 10));
            }
            Check(game.Score == score, "Score diverged from awarded kills.");
            Check(game.Health == 3, "Fair timing player took a wound at " + game.Elapsed + " seconds.");
        }
        Check(kills >= 80 && game.Multiplier == 5 && game.Combo == kills,
            "Long run did not exercise the capped multiplier.");
        Check(game.Wave >= 9, "Nights did not advance.");
    }

    private static void AttackNearest(CombatSession game)
    {
        Enemy nearest = null;
        foreach (var enemy in game.Enemies)
            if (nearest == null || enemy.Distance < nearest.Distance) nearest = enemy;
        if (nearest != null && nearest.Distance <= 2.3f) game.Attack(nearest.Side);
    }

    private static void EncounterPacing()
    {
        var game = new CombatSession(31);
        int openingKills = 0, lateKills = 0;
        float previousKill = -1f, longestGap = 0f;
        for (int frame = 0; frame < 90 * 120; frame++)
        {
            game.Tick(1f / 120f);
            AttackNearest(game);
            foreach (var e in game.DrainEvents())
            {
                if (e.Kind != CombatEventKind.Kill) continue;
                if (game.Elapsed < 20f) openingKills++;
                if (game.Elapsed >= 60f && game.Elapsed < 80f) lateKills++;
                if (previousKill >= 10f) longestGap = Math.Max(longestGap, game.Elapsed - previousKill);
                previousKill = game.Elapsed;
            }
        }
        Check(openingKills >= 100, "Opening queue left too many gaps between attacks: " + openingKills);
        Check(lateKills >= 100, "Reinforcements did not sustain a fast combo: " + lateKills);
        Check(longestGap <= 0.2f, "A kill failed to immediately bring the next target into range: " + longestGap + " seconds.");
        Check(game.Health == 3, "Denser waves became unavoidable for a correctly timed player.");
        Console.WriteLine("  Pacing: " + openingKills + " kills in first 20s; " + lateKills +
            " kills from 60-80s; longest gap after 10s " + longestGap.ToString("0.00") + "s.");
    }

    private static void FrameHitch()
    {
        var a = new CombatSession(18);
        var b = new CombatSession(18);
        a.Tick(5f);
        for (int i = 0; i < 300; i++) b.Tick(1f / 60f);
        Check(a.Enemies.Count == b.Enemies.Count, "Large tick dropped spawns.");
        for (int i = 0; i < a.Enemies.Count; i++)
            Check(a.Enemies[i].Id == b.Enemies[i].Id && a.Enemies[i].Distance == b.Enemies[i].Distance,
                "Large tick changed turn order or step positions.");
        Check(a.Health == b.Health && Math.Abs(a.TimeToStrike - b.TimeToStrike) < 0.02f,
            "Large tick changed timeout damage or the response deadline.");
    }

    private static void InvalidInput()
    {
        var game = new CombatSession(1);
        foreach (float dt in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool threw = false;
            try { game.Tick(dt); } catch (ArgumentOutOfRangeException) { threw = true; }
            Check(threw, "Invalid delta was accepted.");
        }
        bool badSide = false;
        try { game.Attack((Side)0); } catch (ArgumentOutOfRangeException) { badSide = true; }
        Check(badSide, "Invalid side was accepted.");
        game.Tick(0f);
        Check(game.Elapsed == 0f, "Zero delta advanced time.");
    }

    private static CombatSession ChargeTo(int kills, int seed = 31)
    {
        var game = new CombatSession(seed);
        for (int frame = 0; frame < 240 * 120 && game.Kills < kills && !game.IsOver; frame++)
        {
            game.Tick(1f / 120f);
            AttackNearest(game);
            game.DrainEvents();
        }
        Check(!game.IsOver && game.Kills == kills, "Could not prepare a charged live run.");
        return game;
    }

    private static void UltimateCharge()
    {
        var game = new CombatSession(31);
        Check(game.UltimateCharge == 0f && !game.IsUltimateReady, "New run starts charged.");
        for (int i = 0; i < 120 * 120 && game.Kills < 45; i++)
        {
            game.Tick(1f / 120f);
            AttackNearest(game);
            game.DrainEvents();
            Check(game.UltimateCharge == Math.Min(100f, game.Kills * 2.5f), "Wrong per-kill charge or charge exceeded 100.");
            Check(game.IsUltimateReady == (game.Kills >= 40), "Readiness did not change at exactly forty kills.");
        }
        Check(game.Kills == 45 && game.UltimateCharge == 100f, "Did not exercise the full charge cap.");
    }

    private static void PerfectCharge()
    {
        var game = new CombatSession(3);
        Enemy target = OpenQueue(game);
        game.Attack(target.Side);
        Check(game.Score == 150 && game.UltimateCharge == 2.5f, "Perfect kill changed charge reward.");
    }

    private static void PreserveCharge()
    {
        var game = ChargeTo(1);
        game.Tick(0.2f);
        game.Attack(game.Enemies[0].Side == Side.Left ? Side.Right : Side.Left);
        Check(game.UltimateCharge == 2.5f, "Miss changed charge.");
        for (int i = 0; i < 2000 && game.Health == 3; i++) game.Tick(1f / 120f);
        Check(game.Health < 3 && game.UltimateCharge == 2.5f, "Timeout granted charge or wound drained it.");
    }

    private static void UltimateUnavailable()
    {
        var game = ChargeTo(39);
        int count = game.Enemies.Count;
        int score = game.Score;
        Check(!game.ActivateUltimate(Side.Left) && !game.ActivateUltimate(Side.Right), "Ultimate fired at 97.5.");
        Check(game.UltimateCharge == 97.5f && game.Enemies.Count == count && game.Score == score &&
            game.DrainEvents().Length == 0, "Rejected ultimate changed combat state.");
        game = ChargeTo(40);
        game.Tick(60f);
        game.DrainEvents();
        Check(game.IsOver && !game.ActivateUltimate(Side.Left) && game.UltimateCharge == 100f &&
            game.DrainEvents().Length == 0, "Ultimate fired after defeat.");
        bool invalid = false;
        try { game.ActivateUltimate((Side)0); } catch (ArgumentOutOfRangeException) { invalid = true; }
        Check(invalid, "Ultimate accepted an invalid direction.");
    }

    private static void UltimateLeft() { ClearSide(Side.Left); }
    private static void UltimateRight() { ClearSide(Side.Right); }

    private static void ClearSide(Side side)
    {
        var game = ChargeTo(40);
        var selected = new List<Enemy>();
        var opposite = new List<Enemy>();
        bool far = false;
        foreach (var enemy in game.Enemies)
        {
            if (enemy.Side == side) { selected.Add(enemy); far |= enemy.Distance > CombatSession.StrikeRange; }
            else opposite.Add(enemy);
        }
        Check(!game.IsOver && selected.Count >= 2 && opposite.Count > 0 && far, "Could not prepare both populated lanes and a distant target.");
        int kills = game.Kills, combo = game.Combo, score = game.Score;
        int expectedReward = 0;
        for (int i = 1; i <= selected.Count; i++) expectedReward += 100 * (1 + Math.Min(4, (combo + i) / 10));
        game.DrainEvents();
        Check(game.ActivateUltimate(side), "Charged ultimate was rejected.");
        Check(game.Enemies.Count == opposite.Count, "Wave failed to clear only the chosen lane.");
        foreach (var enemy in game.Enemies)
            Check(opposite.Contains(enemy), "Wave damaged or replaced an opposite-side enemy.");
        Check(game.Kills == kills + selected.Count && game.Combo == combo + selected.Count &&
            game.Score == score + expectedReward, "Wave kills did not award normal kill rewards.");
        Check(game.UltimateCharge == selected.Count * 2.5f, "Charge was not consumed before awarding wave kills.");
        int killEvents = 0, waveEvents = 0;
        foreach (var e in game.DrainEvents())
        {
            if (e.Kind == CombatEventKind.Ultimate)
            {
                waveEvents++;
                Check(e.Side == side && e.IsUltimate, "Wave effect received the wrong direction.");
            }
            if (e.Kind == CombatEventKind.Kill)
            {
                killEvents++;
                Check(e.Side == side && e.IsUltimate && !e.Perfect, "Invalid wave kill feedback.");
            }
        }
        Check(killEvents == selected.Count && waveEvents == 1, "Missing wave/kill events.");
    }

    private static void UltimateInterrupt()
    {
        var game = ChargeTo(40); // The last regular strike is still cooling down.
        Check(!game.Attack(Side.Left), "Test did not retain the active attack cooldown.");
        Check(game.ActivateUltimate(Side.Right), "Regular cooldown blocked the ultimate.");
        game.DrainEvents();
        Check(!game.ActivateUltimate(Side.Left) && game.DrainEvents().Length == 0, "Ultimate immediately repeated without recharging.");
    }

    private static void EmptyUltimate()
    {
        var game = ChargeTo(40);
        // Arrange the empty-left-lane edge case explicitly. The denser encounter
        // stream intentionally keeps both lanes populated during normal play.
        foreach (var enemy in game.Enemies) enemy.Side = Side.Right;
        int enemyCount = game.Enemies.Count;
        int kills = game.Kills, score = game.Score;
        float deadline = game.TimeToStrike;
        game.DrainEvents();
        Check(game.ActivateUltimate(Side.Left) && game.UltimateCharge == 0f, "Empty cast failed to spend its charge.");
        var events = game.DrainEvents();
        Check(game.TimeToStrike == deadline && game.Enemies.Count == enemyCount && game.Kills == kills && game.Score == score && events.Length == 1 &&
            events[0].Kind == CombatEventKind.Ultimate, "Empty cast awarded kills or lost its wave effect.");
    }

    private static void UltimateReset()
    {
        var game = ChargeTo(40);
        game.Reset(1);
        Check(game.UltimateCharge == 0f && !game.IsUltimateReady && !game.ActivateUltimate(Side.Left),
            "Restart retained a charged ultimate.");
    }

    private static AttackStyle StrikeStyle(CombatSession game, Side side)
    {
        Check(game.Attack(side), "Expected a normal attack to be accepted.");
        foreach (var e in game.DrainEvents())
            if (e.Kind == CombatEventKind.Strike)
            {
                Check(e.Side == side, "Animation direction did not match input.");
                return e.Style;
            }
        throw new Exception("Normal attack did not send an animation event.");
    }

    private static void AttackStyleCycle()
    {
        var game = new CombatSession(1);
        var expected = new[] { AttackStyle.Cleave, AttackStyle.RisingCut, AttackStyle.Thrust, AttackStyle.Spin };
        for (int i = 0; i < 8; i++)
        {
            Check(StrikeStyle(game, i % 2 == 0 ? Side.Left : Side.Right) == expected[i % 4],
                "Four-strike animation cycle was out of order.");
            game.Tick(0.2f);
        }
    }

    private static void AttackStyleCooldown()
    {
        var game = new CombatSession(1);
        Check(StrikeStyle(game, Side.Left) == AttackStyle.Cleave, "First attack was not cleave.");
        Check(!game.Attack(Side.Right) && game.DrainEvents().Length == 0, "Cooldown input produced an animation.");
        game.Tick(0.2f);
        Check(StrikeStyle(game, Side.Right) == AttackStyle.RisingCut, "Rejected input skipped an animation style.");
    }

    private static void AttackStyleReset()
    {
        var game = new CombatSession(1);
        StrikeStyle(game, Side.Left);
        game.Tick(0.2f);
        StrikeStyle(game, Side.Right);
        game.Reset(1);
        Check(StrikeStyle(game, Side.Left) == AttackStyle.Cleave, "Restart retained the previous animation sequence.");
    }

    private static void AttackStyleUltimate()
    {
        var game = ChargeTo(40);
        Check(game.ActivateUltimate(Side.Left), "Could not prepare ultimate animation.");
        var events = game.DrainEvents();
        Check(events[0].Kind == CombatEventKind.Ultimate && events[0].IsUltimate, "Ultimate did not request its own presentation.");
        game.Tick(0.2f);
        Check(StrikeStyle(game, Side.Right) == AttackStyle.Cleave, "Ultimate advanced the normal four-strike sequence.");
    }

    private static Dictionary<int, float> Positions(CombatSession game)
    {
        var result = new Dictionary<int, float>();
        foreach (var enemy in game.Enemies) result.Add(enemy.Id, enemy.Distance);
        return result;
    }

    private static void StationaryQueue()
    {
        var game = new CombatSession(31);
        OpenQueue(game);
        var before = Positions(game);
        float deadline = game.TimeToStrike;
        game.DrainEvents();
        game.Tick(0.4f);
        Check(game.Enemies.Count == before.Count && game.Kills == 0 && game.Health == 3, "Idle queue changed before its deadline.");
        foreach (var enemy in game.Enemies)
            Check(before[enemy.Id] == enemy.Distance, "Enemy kept walking without a kill.");
        Check(game.TimeToStrike < deadline && game.DrainEvents().Length == 0, "Idle time should only run down the response clock.");
    }

    private static void KillAdvancesQueue()
    {
        var game = new CombatSession(31);
        var target = OpenQueue(game);
        var before = Positions(game);
        int lastId = game.Enemies[game.Enemies.Count - 1].Id;
        game.Attack(target.Side);
        int reinforcements = 0;
        foreach (var enemy in game.Enemies)
        {
            if (enemy.Id > lastId) { reinforcements++; continue; }
            Check(Math.Abs(enemy.Distance - (before[enemy.Id] - 1.65f)) < 0.001f,
                "A surviving enemy did not advance exactly one step after a kill.");
        }
        Check(game.Enemies.Count == 6 && reinforcements == 1 && game.Enemies[0].Distance <= CombatSession.StrikeRange,
            "Kill did not put a new front enemy in range and replace the back of the queue.");
        Check(game.Health == 3, "Advancing the opposite lane caused unavoidable damage.");
    }

    private static void MissHoldsQueue()
    {
        var game = new CombatSession(31);
        var target = OpenQueue(game);
        var before = Positions(game);
        float deadline = game.TimeToStrike;
        game.DrainEvents();
        game.Attack(target.Side == Side.Left ? Side.Right : Side.Left);
        Check(game.Kills == 0 && game.TimeToStrike == deadline, "A miss moved combat forward or extended the response deadline.");
        foreach (var enemy in game.Enemies)
            Check(enemy.Distance == before[enemy.Id], "A miss moved an enemy.");
        var events = game.DrainEvents();
        Check(events.Length == 2 && events[1].Kind == CombatEventKind.Miss, "Miss did not retain normal animation feedback.");
    }

    private static void TimeoutAdvance()
    {
        var game = new CombatSession(31);
        var attacker = OpenQueue(game);
        game.DrainEvents();
        game.Tick(game.TimeToStrike + 0.01f);
        int wounds = 0;
        foreach (var e in game.DrainEvents())
            if (e.Kind == CombatEventKind.Hurt)
            {
                wounds++;
                Check(e.EnemyId == attacker.Id, "Timeout used the wrong attacking enemy.");
            }
        Check(wounds == 1 && game.Health == 2 && game.Kills == 0 && game.UltimateCharge == 0f,
            "Timeout did not deal exactly one uncredited wound.");
        Check(game.Enemies.Count == 6 && game.Enemies[0].Id != attacker.Id &&
            game.Enemies[0].Distance <= CombatSession.StrikeRange && game.TimeToStrike > game.StrikeTimeLimit - 0.04f,
            "Timeout did not prepare the next turn with a fresh response window.");
    }

    private static void UltimateReplenishment()
    {
        var game = ChargeTo(40);
        game.ActivateUltimate(Side.Left);
        int waveKills = game.Kills - 40;
        Check(waveKills > 0 && game.Enemies.Count > 0, "Ultimate fixture did not leave survivors.");
        for (int i = 0; i < game.Enemies.Count; i++)
            Check(game.Enemies[i].Side == Side.Right && Math.Abs(game.Enemies[i].Distance - (2.25f + i * 1.65f)) < 0.001f,
                "Ultimate left gaps, crossed the hero, or repopulated its cleared side immediately.");
        game.Tick(0.2f);
        game.Attack(game.Enemies[0].Side);
        Check(game.Enemies.Count == 6 && game.Health == 3 && game.Kills == 41 + waveKills &&
            game.UltimateCharge == (waveKills + 1) * 2.5f, "Combat did not resume correctly after an ultimate.");
    }

    private static void EmptyQueueRecovery()
    {
        var game = ChargeTo(40);
        foreach (var enemy in game.Enemies) enemy.Side = Side.Right;
        game.ActivateUltimate(Side.Right);
        Check(game.Enemies.Count == 0 && game.Health == 3, "Fixture did not clear the entire queue.");
        game.Tick(0.01f);
        Check(game.Enemies.Count == 6 && game.Enemies[0].Distance <= CombatSession.StrikeRange && game.TimeToStrike > 0f,
            "Cleared queue did not recover with a target and a response deadline.");
    }
}
