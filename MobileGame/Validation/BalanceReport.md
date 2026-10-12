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

## Enemy families and economy pass — October 12

Chapters 7–12 now have family-specific attacks and consistent role silhouettes. Mixed waves use a visible guardian, a ranged priest/hunter, and an optional beast. Crimson rear enemies heal, Glass front enemies ward, Drowned hunters chill rear targets, Rime casters use party-wide chill, and Abyss hunters/casters poison or weaken. Bosses gain related family patterns. These are evaluated by the existing production target and skill planner, including its healer/support and unshielded-target priorities.

Later regular story victories now award 80–105 crystals, and finales award 105–130. All 60 story victories supply 4,425 crystals; the initial wallet adds 300 (47.25 base-cost pulls before refunds, dailies, idle claims, or Tower). Earlier story rewards remain 50 per victory. Claimed XP and crystals come from the displayed prepared result, and crystals cap at 1,000,000.

The old idle currency budget was 720 crystals at a fresh save and 9,360 after stage 60 per capped 12 hours (7.2 and 93.6 base-cost pulls). It is now 72 and 936 respectively (0.72 and 9.36 pulls). At stage 30 the budget is 504 crystals and 5,760 XP per hero. Idle XP stays unchanged; fractional currency credits are carried in version-13 saves. Regression checks compare frequent claims/reloads against a single bulk claim, reject invalid fractions without mutation, and migrate version-12 saves without changing their wallet.

### Progression simulations

The runner adds two cohorts of 200 fresh-save, full twelve-chapter campaigns. Both cohorts use earned story recruits, earned loot equipped with the production gear recommendation, available talents/ascensions, class-aware roster upgrades, formation recommendations, and ordinary random defenses. One cohort uses no summons; the other spends earned crystals on at most three pulls between victories. Loot and summon rolls are seeded. Saves reload after each completed chapter. Each loss permits a 30-minute idle wait and retry, with at most four attempts per stage. Neither cohort injects levels, inventory, crystals, or legendary heroes.

Both cohorts completed **200/200** campaigns without retries or idle waits. The earned-summon cohort averaged **65 pulls**, including duplicate refunds. This shows that deliberate use of free story recruits, gear, and upgrades can clear this prototype; it does not establish that every player formation or casual play pattern will perform equally well. Since no retry was needed, these campaigns do not measure recovery after a loss; separate combat/save tests exercise idle rewards and the runner reports rather than assumes waiting time.

The 30,000 fixed-stage samples still pass their opening-accessibility and developed-team checks. Across chapters 7–12, level-10 Legendary teams with default skills and no gear/talents win at least 90% per stage and win **199/200 (99.5%)** at the final boss. Level-20 Common teams without gear/talents win **77/200 (38.5%)** at the final boss, so gear/quality still matter. The original 400 first-arc campaign runs remain 167/200 default and 156/200 with the Cleric replacement.

**Validation:** 30,046 combat/progression regression assertions, 30,000 seeded stage samples, 400 first-arc campaigns, 400 full progression campaigns, and explicit idle/story economy budgets. These are model simulations using Mono. The Unity serialization diagnostic gained a fractional-credit round-trip fixture but has not run in the cloud. No Unity animation playback, effects capture, device fight-duration measurement, long-term retention, or production monetization balance is claimed.
