using System;
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
        Run("Close strike awards the perfect bonus", Perfect);
        Run("Wrong-side strike misses and breaks the chain", WrongSide);
        Run("Cooldown rejects repeated attacks without extra events", Cooldown);
        Run("Three wounds end the run exactly once", Defeat);
        Run("Reset clears the previous run and restores health", Reset);
        Run("Equal seeds produce equal encounters", Deterministic);
        Run("Wraiths unlock after the opening phase", Wraiths);
        Run("Long-run combat scales the multiplier and remains survivable", LongRun);
        Run("A large tick agrees with normal frame updates", FrameHitch);
        Run("Invalid time and attack input are rejected", InvalidInput);
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

    private static Enemy Approach(CombatSession game, float distance)
    {
        for (int frame = 0; frame < 2000; frame++)
        {
            game.Tick(1f / 120f);
            if (game.Enemies.Count > 0 && game.Enemies[0].Distance <= distance)
                return game.Enemies[0];
        }
        throw new Exception("No enemy reached the requested distance.");
    }

    private static void OpeningGrace()
    {
        var game = new CombatSession(1);
        game.Tick(0.6f);
        Check(game.Enemies.Count == 0, "Enemy spawned too early.");
        game.Tick(0.1f);
        Check(game.Enemies.Count == 1, "Expected one opening enemy.");
        Check(game.Health == 3 && game.Score == 0, "Incorrect initial state.");
    }

    private static void Hit()
    {
        var game = new CombatSession(2);
        Enemy target = Approach(game, 2.4f);
        int count = game.Enemies.Count;
        game.DrainEvents();
        Check(game.Attack(target.Side), "Strike was rejected.");
        Check(game.Enemies.Count == count - 1, "Strike did not remove exactly one enemy.");
        Check(game.Score == 100 && game.Kills == 1 && game.Combo == 1, "Incorrect hit rewards.");
        var events = game.DrainEvents();
        Check(events.Length == 2 && events[0].Kind == CombatEventKind.Strike &&
            events[1].Kind == CombatEventKind.Kill && events[1].EnemyId == target.Id,
            "Presentation did not receive the correct strike and kill.");
    }

    private static void Perfect()
    {
        var game = new CombatSession(3);
        Enemy target = Approach(game, 1.5f);
        game.DrainEvents();
        game.Attack(target.Side);
        Check(game.Score == 150, "Perfect strike should award 150 souls.");
        var events = game.DrainEvents();
        Check(events[events.Length - 1].Perfect, "Perfect flag missing.");
        Check(game.Health == 3, "Enemy wounded player before contact.");
    }

    private static void WrongSide()
    {
        var game = new CombatSession(4);
        var target = Approach(game, 2.4f);
        game.Attack(target.Side);
        game.Tick(0.2f);
        int count = game.Enemies.Count;
        game.DrainEvents();
        game.Attack(target.Side == Side.Left ? Side.Right : Side.Left);
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

    private static void FrameHitch()
    {
        var a = new CombatSession(18);
        var b = new CombatSession(18);
        a.Tick(5f);
        for (int i = 0; i < 300; i++) b.Tick(1f / 60f);
        Check(a.Enemies.Count == b.Enemies.Count, "Large tick dropped spawns.");
        for (int i = 0; i < a.Enemies.Count; i++)
            Check(Math.Abs(a.Enemies[i].Distance - b.Enemies[i].Distance) < 0.04f, "Large tick changed enemy movement.");
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
}
