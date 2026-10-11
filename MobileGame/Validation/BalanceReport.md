# First campaign balance pass

Enemy base health previously stopped at 400 at stage 13, while the party expanded to five heroes. It now rises by 25 per stage through all 30 stages (100 to 825 before party-size scaling). Campaign attack damage is unchanged in chapter 1 and gains 8 percentage points per stage afterward, reaching 3× base at the final stage. Early-wave reduction still applies. Tower retains its separate attack scaling; its chapter-derived health also benefits from the removed ceiling. Stage power and previews reflect the campaign adjustment and exclude it for Tower.

## Auto Play evidence

200 seeds per setup. Every stage includes all three waves. The harness uses the production target/skill planner and class-specific random defense rolls. Fixed rosters use Knight, Paladin, Sorceress, Ranger, and Cleric up to the chapter slot limit, with default skills, no equipment, talents, ascension, or idle rewards. These are headless model simulations, not Unity playback or device tests.

| Fixed team | Chapter 5 boss before → after | Chapter 6 boss before → after |
|---|---|---|
| Common, level 1 | 100% → 24% | 100% → 10.5% |
| Common, level 10 | 100% → 100% | 100% → 100% |
| Legendary, level 1 | 100% → 100% | 100% → 100% |

Fresh campaigns start with the Knight and use only story recruits and earned battle XP. No retries, summons, equipped loot, or idle claims are used. The default roster completed all chapters in 167/200 runs (83.5%); replacing the fifth slot with Cleric when available completed in 156/200 (78%). Both previously completed in 196/200 (98%). The reserve Cleric has less earned battle XP than the replaced hero, so this does not compare classes at equal level.

The opening two stages still won 200/200 fresh runs. Chapter 1 stage 3 won 196/200, unchanged from baseline. Later regular stages remain forgiving; final bosses now reward leveling or other progression. Strong teams can still clear consistently, which is intentional for this first pass. The suite checks opening accessibility, at least 60% full-campaign completion without upgrades beyond earned XP, and a meaningful benefit from leveled or Legendary teams at the final boss.

## Validation

- 18,000 fixed-roster stage samples and 400 fresh campaign runs passed their balance checks.
- 18,412 existing combat regression checks passed, including campaign, save behavior, bosses, and all 100 Tower floors with controlled defenses.
- Tower Auto Play balance, alternate skill loadouts, individual hero rankings, equipment/idle pacing, and real-device fight duration still need further passes.

To reproduce from `MobileGame`, compile the pure C# script list documented in the README with `Validation/BalanceChecks.cs` instead of `Validation/BattleChecks.cs`, then run the executable with Mono. Seeds are fixed; the harness does not depend on Unity.

## Expansion to chapters 7–12

The runner now keeps the original 18,000 first-arc samples and 400 chapter-1–6 campaign runs, and adds 12,000 samples across every stage of chapters 7–12. The new cohorts are level-20 Common and level-10 Legendary teams, using the same default skills and no equipment/talents/ascension. Level-10 Legendary teams win 91–100% across the expansion (96% at the final boss); level-20 Common teams range from 19.5–100% (26% at the final boss). Developed teams can progress, and additional upgrades matter in the latest chapters. Natural fresh-save progression through all twelve chapters with summons, gear, ascension, idle rewards, and retries remains to be measured. The original first-arc report above is retained as a baseline.
