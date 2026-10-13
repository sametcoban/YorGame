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

## Home and chapter milestones follow-up

Twelve optional, one-time chapter milestones add **2,850 crystals**, **2,850 XP for each hero who is recruited when the reward is claimed**, and five shards per recruited hero per chapter (60 for a hero present for all twelve claims). The story budget is now 4,425 victory crystals + 2,850 milestone crystals + 300 starter crystals = **7,575 crystals / 75.75 base-cost pulls**, before duplicate refunds, idle income, dailies and Tower. Milestones unlock after claiming a chapter finale's battle result and do not gate the next chapter. Version-14 saves persist the claimed-chapter bitmask, reject claims for unearned chapters, and migrate versions 1–13 with past cleared chapters eligible once.

The full progression harness now claims the earned milestone after each chapter before reloading. Both the no-summon and earned-summon cohorts completed **200/200** full campaigns without retries/idle waits. The earned-summon cohort averaged **101.8 pulls**, including refunds and at most three pulls between stage victories. These cohorts deliberately choose gear/talents/rosters; they are not casual-player observations. Fixed-stage samples retain the prior no-gear/no-talent assumptions and the opening/late-stage pass thresholds.

**Validation:** 30,108 regression assertions, including reserve skill changes and battle locks, non-mutating level/ascension comparisons that match awarded stats, duplicate/future/invalid milestone claim rejection, wallet caps, backward migration and save reloads. A regression verifies that a boss killed by a counter cannot complete its party-wide damage against other heroes. The balance runner passes 30,000 fixed-stage samples, 400 first-arc campaign runs and 400 complete progression runs. The isolated Unity JSON diagnostic also gained milestone/reserve-skill fixtures, but Unity is absent in the cloud and those fixtures have not executed here. See the playtest checklist for the visual, input, native serialization and phone-performance checks still needed locally.
## Story, tactical skills and Forge follow-up

Story recruitment now awards nine scheduled allies across twelve chapters, plus Rowan as the starter. All six classes become available by chapter 3. Already-owned story allies award 20 shards. Tactical skills add taunt, cleanse, party shielding, interrupt, dispel and expose; Auto Play evaluates these effects. Nightfall's base damage is reduced from 16 to 12 to keep its party-wide Weaken pressure within the existing developed-team completion threshold.

Equipment types can be upgraded to +10 using materials from claimed victories and idle rewards. Upgrades apply to every owned and future copy of that exact gear type. Story victories provide 1,380 materials across 60 stages; capped twelve-hour idle rewards provide 720. A complete Common gear-type upgrade costs 325 materials, and a Legendary upgrade costs 1,625. Version-15 saves preserve the wallet and upgrades; older saves retain their heroes and inventory and start with no upgrades.

The full-progression harness spends earned materials on equipped gear, targeting +3 early, +5 midway and +10 later. Both no-summon and earned-summon cohorts completed **200/200** campaigns without retries or idle waits. The earned-summon cohort averaged **90.9 pulls**. These simulations deliberately manage formations, gear and upgrades; they do not represent every player strategy. Fixed-stage samples continue without gear or talents: developed Legendary teams won **186/200** at 12-4 and **200/200** at 12-5, meeting the existing 90% threshold.

**Validation:** **30,629 regression assertions**, **30,000 seeded stage samples**, **400 first-arc campaigns**, and **400 full-progression campaigns** passed using Mono. Coverage includes scheduled/duplicate recruitment, tactical effects and boss interrupt resistance, non-mutating upgrade previews, shared-copy upgrades, material spending, save migration and invalid-save rejection. Unity JSON fixtures were extended but have not run here. Unity import, UI layout, animation playback and device performance still require local verification.

## Four-hero faction tier and formation previews

Faction synergy now grants +10% at three matching heroes, +15% at four and +20% at five. Current and preview stats use one shared threshold calculation. Formation explains the next threshold and chapter slot restrictions; Collection shows the exact bonus gained or lost before assignment. Save data remains version 15 because the bonus derives from the saved formation.

**Validation:** 30,660 regression assertions passed, including all four-hero HP/attack/skill/ultimate stats, bonus increases/decreases, preview non-mutation, preview versus applied team power, and save reloads. The balance runner passed 30,000 stage samples, 400 first-arc campaigns and 400 full-progression campaigns. Both full-progression cohorts completed 200/200 without retries or idle waits; earned summons averaged 90.9 pulls. An additional separate-assembly compile check for SaveSerializationChecks passed with minimal Unity API stubs; this checks runtime/editor access boundaries, not native Unity serialization or import. UI readability and Unity playback still need local verification.

## Saved summon Wishlist

Players can select up to two named Rare+ heroes per faction. Rarity odds and guarantee counters are unchanged; within the rolled rarity, selected heroes have weight 3 and others weight 1. One selected hero among six has a 37.5% conditional probability, with each other hero at 12.5%; two selected heroes each have 30%, with the others at 10%. The screen reports conditional percentages, not overall chances per pull. Common/Uncommon and tiers with no selections remain uniform.

**Validation:** 30,739 regression assertions passed. A deterministic sweep of 8,000 selection rolls per rarity (40,000 total draws) agrees with displayed normalized probabilities. Additional checks cover full/per-faction limits, removing choices, unowned heroes, protected copies of saved selections, exact duplicate shards/refunds, Rare/Legendary guarantee priority, malformed ten-pull rejection without partial spending, invalid save rejection without mutation, version-15 migration and battle edit locks.

The balance harness now uses continuous hero-selection rolls like the Unity summon UI and adds 200 complete campaigns with six authored Wishlist selections, using earned crystals and ordinary production upgrades. All three full-progression cohorts completed 200/200 without retries or idle waits; the earned-summon cohort averaged 90.9 pulls without Wishlist and 91.4 with Wishlist. The difference includes duplicate refunds and is specific to the chosen list/seeded strategy. The runner passed 30,000 fixed-stage samples, 400 first-arc campaigns and 600 full-progression campaigns.

The separate-assembly editor diagnostic compile check passed using minimal Unity API stubs. The Wishlist JSON round-trip fixture was added but requires Unity to execute. No cloud Unity import, UI rendering or phone performance check is claimed.
