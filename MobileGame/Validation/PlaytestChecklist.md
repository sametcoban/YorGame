# Unity and mobile playtest

The cloud runs combat, progression, save-validation and economy simulations. Unity animation playback, native JsonUtility serialization, rendering, build output and real-device performance must be checked in Unity and on a device. The following diagnostics are read-only and do not replace those checks.

## Check the update in Unity

1. Copy the entire `Assets/Scripts` and `Assets/Editor` folders from this update. Keep the art imported by the previous updates. If portraits/models are missing, run **Ashlight > Art > Build All Named Heroes** and **Build Enemy Roster** outside Play Mode.
2. Run **Ashlight > Diagnostics > Check Combat Assets** to verify the 30 named hero prefabs, portraits, seven hero animation states, enemy windups, and 21 sounds. Run **Check Save Serialization** to test isolated JSON fixtures, including reserve skills and one-time chapter rewards. Neither diagnostic changes your PlayerPrefs.
3. Press Play. The default destination is Home. Check Campaign, Heroes, Summoning, Idle Rewards, Tower, Daily Quests and Milestones, then return from each. Portrait shortcuts should return to Home; heroes opened through Collection should return there. Summoning opens its lobby before spending crystals; Back spends nothing.
4. In Game view, test landscape sizes **1280×720**, **844×390**, and **1024×768**. Open each screen and run **Ashlight > Diagnostics > Check Current Screen**. It checks the topmost panel's button bounds/overlap and exactly one active audio listener. Visually check text clipping, readable fonts, unobstructed XP/health/energy bars and comfortable touch targets. The diagnostic does not automatically assess readability or touch comfort.
5. Open a reserve hero. Change its two equipped skills, equipment, and talents without assigning it to the party. Return and check the correct hero retained these changes. In Upgrades, compare level/ascension HP, ATK, ultimate damage and power. Ascend only after confirming the preview; verify the displayed shard cost was deducted once.
6. Clear a chapter finale, claim the battle result, and read the chapter story. Claim the milestone, close the game, and reopen. The milestone must remain claimed. The next chapter remains unlocked even if the milestone is left unclaimed. Existing saves can claim rewards for already-cleared chapters once.
7. Start a battle and enable Auto Play. Management panels must disappear; Home panels must stay closed. Check all six classes, 36 selectable skills and 30 hero ultimates as they become available. Watch target-facing, melee advances, Ranger release recoil, Rogue finishers, hit reactions and deaths. Check ultimates at 1× and 3×: short camera emphasis should settle back to the normal frame, and healing/guard effects should match the recipients.
8. In chapters 7–12, check family tactics and 2–3 enemy waves: Pyre burns, Drowned rear shots, Crimson renewal, Glass shields, Rime party attacks, Abyss poison/weakening. When a parry kills an enemy, that enemy must not finish damaging other heroes.
9. Use **Capture Current Screen** for screenshots in `Builds/Playtest`. **Measure Editor Playback (10 seconds)** reports Editor frame rate and managed heap, not phone performance. For memory/CPU/GPU diagnosis, use Unity's Profiler.

## Test on your iPhone from Windows

Use the browser route already included in the project:

1. In Unity Hub, install **Web Build Support (WebGL)** for Unity **6000.0.65f1**.
2. In Unity, run **Ashlight > Browser > Build Browser Game**. The build is written to `Builds/Browser` only after Unity reports success.
3. Serve the entire folder through an HTTP(S) static host, or a server on your Windows machine accessible to your phone on the same Wi-Fi. Use the computer's LAN address from the phone; a phone's `localhost` points to the phone itself. Do not open `index.html` directly as a file.
4. Open the served address in Safari, turn the phone sideways, and tap Play. Keep the site address stable when testing saved progress. Private browsing and browser storage cleanup can remove saves.
5. Tap all Home destinations, start a five-hero battle, use Auto Play at 3×, and test 2–3-enemy waves and area ultimates. Play at least 15 minutes. Check frame rate/heat, text, taps, notch/home-indicator clearance, audio after switching apps, and rotation back to landscape.
6. Claim battle, idle and milestone rewards. Reload Safari, close/reopen it, and background the game for several minutes. Check that heroes, gear, crystals, fractional idle credits, milestone claims and chapter progress survive without duplicate payouts.

Safari is an initial iPhone browser test. It does not validate a native iOS app; that later requires an iOS Unity export, a Mac/Xcode/signing, and a device or TestFlight build. Android testing uses **Ashlight > Android > Build Test APK** with Android Build Support/SDK/NDK/OpenJDK and an Android device or emulator. No cloud/device build or store-ready performance result is claimed by this update.

## Story, tactics and Forge update

- Check story introductions after 1-5, 2-2, 2-5, 3-3 and 3-5. All six classes should be available by the end of chapter 3 without summoning. Later Rare arrivals are 6-5, 8-5, 10-5 and 12-5. An already-owned story ally grants 20 shards rather than an unrelated recruit; reload a pending victory and verify that claiming still pays only once.
- Equip Guarded Slash and check normal enemy attacks follow the taunt while rear-row and area attacks keep their targeting. Use Sanctuary/Renewal against debuffs and Aegis/Divine Ward before area attacks. Watch effects on all living recipients; dead heroes must stay dead.
- Use an interrupt before a heal/shield/attack or during Cinder's charge. A cancelled action should have no attack animation/projectile/damage. Bosses show STEADY until completing an ordinary action. Use Flame Lance/Radiant Blade to break an enemy ward and Arcane Burst to expose a target before the other heroes attack.
- Earn materials from a victory and Idle Rewards. Select gear in Equipment, open Forge, compare +0/+1, then upgrade once. Check the exact material cost, all copies of that gear type (including reserve heroes), and re-equipping after an upgrade. Test +10, insufficient materials, and saving/restarting. Other gear types must retain their own ranks. Forging and loadout changes must remain locked during a fight.
- Run the isolated **Check Save Serialization** diagnostic for pending story duplicates and forged gear/materials. The diagnostic fixtures have not run in the cloud; they require Unity.

## Faction formation follow-up

- In Formation, check mixed teams and teams with 3, 4 and 5 matching heroes. Expect no bonus, +10%, +15% and +20% respectively. Verify the counts and next-threshold text, including early chapters with locked slots. Check that the four-line formation summary stays readable on the phone-sized window.
- In Collection, preview swaps from 3 to 4, 4 to 5 and back again. Verify team bonus and power match the applied swap; previewing must leave the roster, health and wallet unchanged. Check the three-line comparison fits its panel.
- Save/reopen with four matching heroes and confirm +15% persists. Replacing a matching hero should update all active heroes and remove the bonus from the outgoing reserve hero. Formation changes remain locked during combat.

## Summon Wishlist

- Open Home → Summoning → Wishlist. Switch all three factions; check six cards per faction, portraits, rarity/class/element labels and displayed conditional percentages. Select two heroes in one faction; further choices should disable until you remove one. Other faction tabs should keep their own two-slot limits. Test the empty list and all six slots.
- Check one selected hero in a rarity displays 37.5%, while other heroes of that rarity show 12.5%. Two selected heroes of the same rarity display 30% each and the others 10%. A choice in a different rarity should leave those percentages unchanged. The screen must clearly distinguish within-rarity percentages from base summon odds.
- Save/reopen and confirm all selections persist without spending crystals. Single and ten-pulls should respect rarity guarantees and keep duplicate shard/refund behavior. Skip/reopen results must not reroll or award again. Verify edits are unavailable during combat.
- Check the Wishlist rules, faction tabs, cards and Back button for overlap at phone-sized landscape resolutions. Run **Ashlight → Diagnostics → Check Save Serialization** for the new isolated Wishlist JSON fixture in Unity; it has not run in the cloud.

## Battle clarity and Activities

1. Play a crowded later-stage fight at normal and fast speeds. Check the top action banner, ultimate color cue, gold energy pulse, colored debuff/ward/taunt badges and larger shadowed damage numbers. Confirm badges do not cover action buttons or energy bars, and floating text stays within the camera. Open every preparation menu and finish a fight; HUD additions must stay beneath overlays.
2. Open a recruited hero → Next Upgrades. Check remaining XP/shards, a ready talent, shared-level talent restrictions, empty gear and Forge costs. Follow each shortcut and return. Guidance must not spend resources or alter a hero.
3. Open Home → Activities → Enemy Guide before and after encountering a new enemy. Check discovery persistence, unknown cards, page boundaries, known role/element variants, ability sequences and advice. Test Cinder's charge, Thorn Queen's communion and Veiled Monarch's ward descriptions.
4. Reach chapter 3 and enter the Daily Boss with your chosen formation. Confirm one boss/one wave, eight-round limit, Auto Play, random defenses and a result with total/per-hero damage and healing. Return home; campaign stage, heroes, HP, energy, formation and equipment must retain their pre-challenge state.
5. Complete an attempt and check +35 crystals/+30 materials. Replay the same day: score may improve, rewards must not repeat. Reload and check the best score and claim status. Check the next UTC day's rotation/reward and midnight during a fight. A backward clock change must not reopen a claimed day. Closing during an unfinished attempt should reload the preserved campaign, without granting an unfinished reward.
6. Run **Ashlight → Diagnostics → Check Save Serialization** in Unity. The new guide/boss record/claim fixture is isolated from PlayerPrefs. This native JSON diagnostic has not run in the cloud.

## Suggestions, scenes, summon polish and beta tools

- Before combat, open Enemy Preview → Team Suggestion. Compare a mixed roster against elemental enemies, rear threats, healers, wards and Cinder's charge. Review the proposed skills, positions and power; apply once. Wallet, materials, XP, shards, gear, enhancements and talents must remain unchanged. Confirm no recommendation controls or Auto Play actions work during combat or over a modal screen.
- Enter chapter openings and finales. Read, Next, Skip and restart; seen scenes should stay skipped on later visits. Check paired boss names from chapter 3 onward. Replay the current chapter from Activities. Verify the preview resumes after the scene and that scenes/recommendations sit above HUD labels.
- Try single and ten-pulls, including duplicate Rare+, Epic and Legendary results. Check portraits stay hidden until revealed; full batch rewards must be resolved before animation. Inspect net cost, refunds, awarded shards and current ascension progress. Skip/repeat/close/reload must not reroll or award a second time. Check single-card and ten-card text/portrait layouts locally.
- Open Activities → First Journey and replay Tutorial. Confirm the guided opening uses free chapter recruitment and marks achieved milestones. Complete chapter 1 without summoning and check the last introductory card; manual Tutorial replay should step through all five cards. The Tutorial and Activities buttons must not overlap on Home.
- Open Feedback, select each category and type observations. Build/copy a report; check stage, formation, skills, gear and version match the game. The report should stay local. Reopen after editing a draft, and verify notes persist. Test read-only selection/manual copying when clipboard access is unavailable.
- Run the isolated Save Serialization diagnostic for version-18 scene flags. Native Unity JSON, UGUI input fields/clipboard, visual layout and phone performance have not been exercised in the cloud. See ClosedBetaChecklist.md for later release gates.

## Sets, bonds, weekly challenges and styling

- Equip one, two and three matching elements, including mixed rarities/forged copies. Compare HP, armor, elemental damage, energy and healing with Set Builds. Removing a piece must remove its tier effect. Check that previews match applied stats, keep resources unchanged and account for set bonuses when choosing gear. Inspect the expanded equipment/hero labels for wrapping.
- With Tempest's three pieces, check +10 energy per action and earlier ultimates for ordinary 25-energy heroes. With Radiance, check +10% skill healing while ultimates remain unchanged. Venom's critical bonus applies to Rogues.
- Open Bonds for different named heroes and all three tabs. Check unique Origin memories, the three/ten-participation thresholds, companion ownership/participation requirements and persistence after reload. Reading spends nothing and changes no combat stats. A claimed result counts once; merely preparing or abandoning a challenge must not increment bonds.
- Enter each Weekly modifier with the selected team. Check poison tick damage, halved skill healing, initial/renewed shields, normal automatic defenses and the eight-round ending. A dead party at round eight must not count as surviving. A clear grants 150 crystals/150 materials/five participant shards once; replays do not repeat it. Daily claims must remain independent.
- Reload records/claims, cross a UTC Monday, and move the clock backward in an isolated test environment. Check the new weekly rotation, frozen starting week during an attempt, and stale lobby review. Verify campaign stage, equipment, hero HP and energy are preserved when returning.
- After campaign completion, complete three positive-score boss attempts, claim Idle Rewards, summon with earned crystals and finish the daily chain. Confirm failed/abandoned zero-score attempts do not count.
- Review every menu, hero portrait, summon reveal and affinity badge for consistent surfaces/text/rarity colors. Test the new header buttons for overlap and overlays for blocked combat actions. Native Unity JSON diagnostics now include weekly records and bond counters; they have not executed in the cloud.

### Journey expansion (`ashlight-beta-prep-3`)

- Update all Scripts, Editor and Resources/Audio/Combat files; rebuild the enemy roster. Run Check Combat Assets and native serialization checks in Unity.
- Load a save that finished chapter 12: it should open chapter 13, retaining heroes, loadouts, Forge levels, shards, resources and the original 24 story-scene flags. Check later intro and boss scenes remain seen after restarting.
- Check the three new chapter pages, milestones through chapter 18, two/three-enemy waves and paired finales. Target every enemy. Inspect the six new environment palettes and animated enemy variants.
- Home → Activities → Puzzles: swap heroes, cycle both skills without duplicates, and try healer targeting, back-row protection and shield dispelling. Loan heroes use fixed Rare level-ten stats. First clear gives exactly 50 crystals/materials; replay, defeat, abandonment and app restart must not pay again or change your campaign team. Twelve enemy actions is the puzzle limit; Daily/Weekly still use eight.
- Home → Activities → Goals: page through missing heroes and companions; compare gear collection against owned inventory and active sets. Check text and four Activities shortcuts at narrow portrait aspect ratios.
- Compare manual and Auto Play timing, especially Twin Fangs, Rogue basics, multi-target ultimates, interruptions and the transition into an enemy round. Ordinary recovery should be shorter in Auto; impacts and full ultimate/boss cues must remain readable.
- Listen for three alternating takes per supported weapon/element, menu ticks and frost/arcane boss impacts. Toggle mute and check voice overlap, pauses and background/resume behavior.

### Boss relics and advanced progression (`ashlight-beta-prep-4`)

- Copy all Scripts/Editor files and run native serialization diagnostics when Unity testing is available. Load version-20 saves without losing progress, heroes, gear or currencies.
- Chapter finales: normal loot plus named relic appear before Continue; neither is granted early. Reload pending victory and claim once. Cleared legacy chapters offer a separate Claim Relic button in Milestones even when milestone currency was already claimed.
- Equip and Forge relics; check stable IDs, set counting, gear comparison and material costs. New relics do not replace higher-rarity loot automatically without a comparison.
- Own Lv 20 + chapter 13 enables Mastery. Check five costs, +2 combat levels per rank, gray unaffordable/max controls, reserve heroes, shared heroes, save/reload, and blocked spending during combat. Own level/rarity/XP do not increase with mastery.
- Formation techniques: swap class positions before attacking; check final rows determine bonuses. Verify one application per battle, carried energy/guard between waves, no repeated grants on later actions, and no bonuses in loan puzzles. Check normal faction bonuses still work.
- Inspect route-strip spacing, long relic names, chapter-clear headings, both reward paragraphs and the wave banner on narrow screens. Return Home/restart during transitions; transient banners should clear.
- Device/playback/performance checks are intentionally pending until the user can playtest.

### Tactical/content expansion (`ashlight-beta-prep-5`)

- Import all Scripts/Editor files and Audio/Chapters, rebuild the enemy roster, and run asset/serialization diagnostics when Unity testing is possible.
- Load a version-21 chapter-18 completion save: chapter 19 opens, mastery/resources/gear survive, and chapter 13–18 boss-scene flags remain seen. Chapter 19–24 scenes/milestones/relics appear on the fourth campaign page.
- Check five enemy buttons and bars within the safe area; two front and three rear actors must remain individually targetable. Check fourth/fifth hits, area ultimates, deaths, shields and interrupt animations. Finales retain two bosses.
- Inspect Auto Play focus on paired bosses, interruptible charges, healer/support threats and shields. On approach waves, basic attacks should preserve ordinary damage-only skill charges when they can kill through the shield.
- Upgrade each class to Mastery 1 and check its named signature once per wave, exact guard/heal/energy/cleanse/expose effect, the next-wave reset and exclusion of fallen allies. Class passives and signature labels should remain readable.
- Lock a chosen equipped type and use Equip Best; it must remain equipped. Manually switch it, cycle all three sorts, compare refined stats and verify locks after restart.
- Relic raids: clear three times for 15 essence/225 materials total, then replay without further daily reward. Failed/abandoned attempts give no essence/materials. Check midnight, app close and quota reload. Eight is the count of individual boss actions; no campaign/Tower advancement or regular loot occurs.
- Refine relics through five essence costs; check Forge levels/refinement coexist, every equipped copy updates and previews match actual stats. Gray max/insufficient controls must explain cost/owned essence.
- Complete and miss each stage-3 objective; both victories advance campaign. Reload pending victory for both cases: only the actual completed objective pays once, including rear-target-first tracking.
- Inspect role crests on male/female rigs, melee trails, debuff colors, all six audio themes, mute, app pause/resume and a single AudioListener. Music should remain quieter than action cues.
- Phone/browser layout, animation playback and performance remain pending until the user can playtest.

## Beta prep 6: company tools and accessibility (pending Unity/device execution)

- Import all Scripts/Editor files into the existing project. Confirm compilation without adding Particle System or an AudioListener.
- Load a version-22 account: original presets occupy Campaign slots; Tower/Challenge slots start empty. Test nine independent names/order lists, invalid names, save/reload, early party caps and five-slot Tower teams. Modes require explicit Load; no automatic team switching.
- Compare two owned heroes; scroll each column independently. Check equipment, own/shared/mastery stats, utilities and faction/named passive text. Preview upgrades and compare actual stat changes after a purchase made outside practice.
- Train against an early and a late reached boss. Swap heroes/front/back and two skills before attacking; verify campaign formation and skills return unchanged afterward. Test victory, defeat, early exit and application pause. No resources, quests, bonds, rewards or campaign progress should change. Test training after all 24 chapters are completed.
- Check all six daily practice modifiers with a controlled UTC clock. Weekly rewards and relic raid quotas must remain independent. Confirm standard training has no modifier.
- Compare support reports against observable health loss, ward increases/absorption, status removal and successful interrupts. Inspect victory, defeat and challenges. Save a pending victory, restart and verify support counters; legacy pending victories show zero new counters.
- Toggle large text repeatedly; ensure sizes do not compound, scroll readers remain legible and bottom buttons fit at phone aspect ratios. Check preset rename fields and all dynamically refreshed combat labels. The cloud cannot verify clipping or touch targets.
- Toggle reduced effects and still camera mid-session. Damage/target/warning text remains; debris, atmospheric motes, warning/torch pulses, shake, zoom and entrance movement follow the appropriate setting. Restart and verify settings persist without affecting account saves.
- View paired-boss entrances in final waves and chapter monuments from both phone orientations. Verify entrance text disappears, does not hide the next wave banner, and cannot delay or desynchronise attacks. Check animation origins for all 148 enemy variants.
- Inspect first multi-target attack and steady combat performance with the warmed 12-object/48-cap effect pool. No allocation/performance claim is established until profiling on target hardware.

## Beta prep 7: inventory and profiles (pending Unity/device execution)

- Open Home → Tools → Inventory on empty, small and large accounts. Check all four filters, rarity ordering, pagination, selected stats and off-party equipped counts. Large text must remain readable on cards and scrollable detail text.
- Lock a type, equip a copy on a reserve hero, and upgrade a type. Verify protected copies/types cannot be recycled. Boss relics are always protected, including unupgraded/unequipped relics. Common through Legendary ordinary returns are 5/10/20/40/80 materials per copy.
- Test quantity +/-/Max, Cancel, Confirm and repeated Confirm. Cancellation changes nothing; confirmation converts only free copies and updates counts/materials once. At material cap, return value must not be discarded. Leave/reopen menus and restart to verify converted quantities persist; save failures must show their notice.
- For one hero, save an armed build and a build with three empty gear slots and different skills. Name all three slots and reload after restart. Profiles must use current Forge/refinement levels, preserve talents and progression, and support normal manual replacement of locked gear.
- Give one required item to another hero and attempt to load the saved build: no skill or equipment slot may change. Free that copy and load again. Recycle a referenced ordinary copy and verify the template remains saved but unavailable; acquire another copy and verify it becomes usable again.
- Load builds for reserve heroes, after campaign completion and before practice. Practice changes stay on the clone; profile saving/clearing and recycling stay disabled there. All new screens and buttons must remain inaccessible during actual combat, with no obstruction to energy bars or skill controls.
- Run Check Save Serialization: named profiles, null/empty gear slots, protected recycling, copy counts and materials must survive actual JsonUtility round trips. Import a version-23 save and verify old teams, currencies, hero gear and chapter progress remain intact.
