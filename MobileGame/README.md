# Ashlight: mobile 3D combat starter

An original combat prototype inspired by reactive turn-based RPGs. All six hero classes now use stylized dark fantasy armor and a generated ruined courtyard. All campaign enemies use themed enemy models. No Clair Obscur assets, names, music, or story are included.

## Open and play

1. Install Unity Hub and Unity **6000.0.65f1** with **Web Build Support (WebGL)** for browser testing on iPhone from Windows. Android Build Support is optional for Android APKs.
2. Open this `MobileGame` directory as a Unity project. Allow package import to finish.
3. The editor helper creates `Assets/Scenes/Battle.unity` and registers it for builds. If necessary select **Ashlight > Prepare Mobile Project**. Open that scene, then press Play.
4. The game opens on the **Chapters** menu. Select the unlocked chapter, then use **Attack**, either equipped skill, or **Auto Play**. Defense rolls automatically using each class’s dodge and parry chances. Mouse clicks work in the editor; buttons accept touch on devices.

The combat presentation includes health bars, attack lunges, enemy windup, dodge motion, and parry/counter feedback. Rowan and the Lantern Warden now use armored models with basic combat animation; all campaign encounters have enemy visuals. Safe-area bounds update when the window or phone orientation changes. New presentation and touch-down behavior require Unity/device validation.

The scene starts empty intentionally: `Prototype` generates the camera, lighting, arena, characters, event system, and HUD at runtime. Use the built-in rendering pipeline. The UI uses the legacy input module; keep Active Input Handling set to **Input Manager (Old)** or **Both**. Landscape orientation and safe-area bounds are configured for the prototype. Device cutouts, aspect ratios, timing feel, and actual rendering need device testing.

## Stylized dark fantasy reference

Rowan (Common Knight) and the first enemy, Lantern Warden, wear original angular armor, closed helmets, tattered capes, and muted metal. The Warden carries a halberd and glowing lantern. A torchlit ruined courtyard, lower camera, staggered party positions, charcoal HUD and compact health bars establish the new direction. All six hero classes now have male and female dark fantasy variants. All five chapter-one enemies and the paired bosses from chapter three onward now have modeled visuals; all campaign enemies have modeled visuals. The built-in rendering pipeline is retained.

To update an existing Unity project:

1. Copy **Assets/Art**, the updated **Assets/Editor** and **Assets/Scripts** from this version. Keep your existing project settings and saves.
2. Wait for importing, then run **Ashlight > Prepare Mobile Project**.
3. Run **Ashlight > Art > Build Dark Fantasy Roster** to refresh all twelve class/gender models. Use **Build Dark Fantasy Reference** only if Rowan or the Warden also need refreshing. Automatic preparation preserves existing generated prefabs.
4. Open the Battle scene and press Play. Select Rowan in your party and start the first chapter. If you already completed the campaign, use **Restart Campaign** to replay it while preserving your collection and currency.

![Blender character art preview](ArtPreview/DarkFantasyReference.png)

The class roster uses the existing hero gender assignments (18 women and 12 men). Paladins wear winged helmets and wine-colored capes; Sorceress/Sorcerer carry crystal staves and crowned cowls; Rangers carry recurve bows, arrows and quivers; Rogues carry twin leaf daggers and poison vials; Clerics carry halo staves, prayer books and ash-colored split vestments. Knights retain the sword and kite shield. Classes share basic armor construction; individual named heroes do not yet have unique art or rarity-specific outfits.

![Male class roster, Blender preview](ArtPreview/DarkFantasyRosterMan.png)
![Female class roster, Blender preview](ArtPreview/DarkFantasyRosterWoman.png)

Named hero prefabs still take priority. Otherwise, hero loading now uses `Resources/Heroes/DarkFantasy/<class>_<gender>`, then the legacy class/gender prefab as fallback. The new generated folder preserves existing custom and legacy prefabs. Characters still use basic animation; Ranger and Rogue attacks and caster staff motions have distinct poses.

These are Blender character art previews, not Unity screenshots; they do not show the courtyard or HUD. The free CC0 source and original asset details are in [DarkFantasy/CREDITS.md](Assets/Art/DarkFantasy/CREDITS.md).

Validation: **2,100 combat checks passed**. The twelve new class FBX files independently reimported with weighted meshes, UVs, the 53-bone skeleton and seven animation states; each is below 18,000 triangles and uses at most seven material slots. All three textures are 512×512 and their checksums match. Unity is unavailable in this environment, so Unity compilation, import, Play mode, clipping, camera framing and mobile performance require local verification.

## Enemy art and paired bosses

Chapter one keeps its five encounters: **Lantern Warden**, **Ash Hound**, **Cinder Scout**, **Iron Watcher**, and **Roadkeeper**. The Warden sweeps its halberd; the Hound is an original skeletal quadruped with a jaw, articulated legs and tail; the Scout wears a shadowed cowl and casts a frost effect from range; the Watcher has rusted spiked armor and a longer sword; the Roadkeeper has a crown, larger mantle and a heavy slam. Each model includes a separate Windup pose before the timed defense flash. Boss strikes add a pooled floor shockwave and sparks.

![Chapter-one enemy lineup, Blender art preview](ArtPreview/ChapterOneEnemies.png)

The image is a Blender character preview, not a Unity gameplay screenshot. Six source models are included (five chapter-one models plus a female hooded caster base). Later boss prefabs reuse these sources with chapter-specific cloaks and effects; they do not yet have eight unique meshes.

Starting with **chapter 3**, each chapter's fifth encounter has **two bosses on the field together**:

| Chapter | Boss pair |
| --- | --- |
| 3 — The Ember Citadel | Cinder Sovereign + Ashen Marshal |
| 4 — The Storm Spire | Storm Regent + Stormbound Executioner |
| 5 — The Blighted Garden | Thorn Queen + Rotbound Consort |
| 6 — The Eclipse Throne | Veiled Monarch + Eclipse Harbinger |

Both bosses have separate health and elemental weaknesses. Tap a boss's name in the compact header to choose the target during your turn. A defeated target automatically switches to a surviving boss. Each living boss attacks once per enemy round, with its own windup and dodge/parry window; parries counter the attacking boss regardless of your selected target. Victory and the usual 50-crystal reward occur once, after both bosses fall. The companion boss has 70% of the primary boss's maximum health and swaps its weakness/resistance, making target choice useful. Boss attack effects are presentation; damage and hero defense rules remain the existing fixed strike/guard system.

Chapters 1–2 keep one boss in the final encounter. There are still 30 encounters and at most five party heroes. Save format and encounter indices are unchanged: existing progress, recruits, loadouts and currency remain compatible. Reloading a fight starts its full encounter; a saved pending victory remains cleared. Other encounters outside this art pass retain their previous placeholder models.

To update:

1. Stop Play mode and copy **Assets/Art**, **Assets/Editor**, and **Assets/Scripts** from the updated branch. Copy all script files, including `EnemyVisualDefinition.cs`.
2. Wait for Unity to compile/import, then run **Ashlight > Prepare Mobile Project** followed by **Ashlight > Art > Build Enemy Roster**. This explicitly refreshes enemy prefabs/controllers, including the Warden's new Windup animation. Existing custom hero prefabs are preserved. The older **Build Chapter One Enemies** menu is an alias for the same operation.
3. Press Play and try chapter one. In chapter three's final encounter, select each boss, damage one, and check both attack turns. Also check that killing one leaves the other alive and that each parry counters the flashing attacker. Completed campaigns can use **Restart Campaign**.

**Verified here:** 2,100 combat checks, including all four paired encounters through the public combat API, defeat order, independent defense windows, counters, fallen allies, reset, save/reload and one reward per encounter. The six enemy source FBX files independently reimported in Blender with fully weighted meshes, UVs, eight animation states and evaluated finite pose bounds. Each has fewer than 18,000 triangles and at most seven material slots. Source reports and checksums are in [DarkFantasy/CREDITS.md](Assets/Art/DarkFantasy/CREDITS.md). **Unity compilation/import, gameplay rendering, two-boss camera framing, clipping and device performance remain unverified in this cloud environment.**

## iPhone browser testing from Windows

This route uses Safari, without Xcode or a native app installation. Unity's mobile Web support varies by browser/device; use a recent iPhone with updated Safari. Real device validation is required.

1. In Unity Hub > Installs > your editor > Add modules, install **Web Build Support** (sometimes labeled WebGL Build Support).
2. Stop Play mode. Select **Ashlight > Browser > Configure Browser**, wait for importing, then select **Ashlight > Browser > Build Browser Game**.
3. A successful build creates `Builds/Browser/index.html` plus the `Build` folder. The custom template supplies a touch-friendly launch screen, landscape reminder, safe-area margins, loading progress, and error messages. Builds disable compression so static hosting does not require special compression headers.
4. Host the **entire `Builds/Browser` folder** with a static website host, such as Netlify's manual deploy, then open its HTTPS URL in Safari on your phone. Hosting uploads your game publicly; review the files and choose your host before uploading. No site has been published automatically.
5. Rotate the iPhone sideways and tap Play. Choose the unlocked chapter to enter the next encounter. Try attacks, automatic defense, Auto Play, victory/defeat, and restart. Also check Safari's toolbar, orientation changes, and returning from the background.

For local testing instead of public hosting, install Python 3 on Windows, open a terminal in `Builds/Browser`, and run:

```powershell
py -m http.server 8000 --bind 0.0.0.0
```

Keep PC and iPhone on the same trusted Wi-Fi network. Run `ipconfig` on Windows to find the PC's Wi-Fi IPv4 address; open `http://YOUR-PC-IP:8000` in Safari. If Windows Firewall asks, allow Python on **Private networks only**. `localhost` on the phone refers to the phone, not the PC. Stop the server with Ctrl+C when finished. Guest Wi-Fi/client isolation may prevent access. Do not open `index.html` directly with a file URL.

Existing ZIP users: download the updated GitHub branch into a **separate directory**, then open its `MobileGame` folder through Unity Hub. Preserve your existing local scene/settings. To update an existing project without replacing it, copy `Assets/Editor/BrowserBuild.cs` and the whole `Assets/WebGLTemplates/MobileSafari` directory into the same paths in your local project; allow Unity to import them.

The build helper and template are source changes only: Unity Web compilation and iPhone Safari behavior have not been verified in this cloud machine. Combat logic checks still pass; your existing editor play test does not validate the Web build.

## Android first: build and test

1. In Unity Hub, add **Android Build Support**, including **Android SDK & NDK Tools** and **OpenJDK**, to the pinned editor. Use Unity's bundled tools rather than the cloud machine's Java 21.
2. In the editor select **Ashlight > Android > Configure Android**. Allow the platform switch/import to finish; run it again if Unity requests an import restart.
3. Select **Ashlight > Android > Build Test APK**. The helper explicitly includes the Battle scene and builds `Builds/Android/Ashlight.apk`. It reports failed builds as errors. Configuration uses IL2CPP, ARM64, Android 8.0 or later, landscape, and legacy touch input.
4. On an ARM64 Android phone, enable Developer options and USB debugging. Connect the phone, approve its debugging prompt, and run these commands using the `adb` shipped with Unity's Android SDK:

```bash
adb devices
adb install -r Builds/Android/Ashlight.apk
adb shell monkey -p com.ashlightstudio.ashlight -c android.intent.category.LAUNCHER 1
```

Alternatively, use Unity's Android Build And Run with the connected phone selected. These are development builds; publishing requires a unique application identifier, release settings, your own signing keystore, and store setup. `com.ashlightstudio.ashlight` is a placeholder.

For an activated Unity editor with Android modules installed, build from a terminal:

```bash
/path/to/Unity -batchmode -quit -projectPath "$PWD" -buildTarget Android -executeMethod AndroidBuild.BuildTestApk -logFile android-build.log
```

### First device acceptance check

- Arena, hero, enemy, health, and buttons appear in landscape without overlapping the phone cutout.
- Each living hero acts once per round. Basic attack damage depends on hero stats and the enemy elemental matchup. No hero can act during the enemy turn.
- Dodge during FLASH prevents damage; a prompt parry prevents damage and counters with elemental damage. Missing defense costs up to 35 health; guard reduces it.
- Win by defeating the enemy; lose when all active heroes are defeated. Restart cancels a pending enemy turn. Continue after victory advances recruitment.
- Test repeated restarts, background/resume, and screen locking. Timing feel, frame rate, and UI layout remain unverified on devices.

No APK has been built in this cloud workspace: Unity, its Android modules, and editor activation are unavailable here. The scripts prepare the build path but require an actual Unity build and phone test.

## iPhone

On **macOS**, switch the build profile to iOS and export an Xcode project. Open it in Xcode, select your signing team and unique bundle identifier, and run on a connected iPhone. App Store distribution requires Apple Developer membership. This Linux cloud machine cannot build/sign the iOS app.

## Validation and current limits

The engine-independent combat model passes 1,513 checks covering party turns, the five-hero cap, recruitment, all 36 skills, two-skill loadouts, healing/guard, rarity scaling, elemental overrides, defense, victory/defeat, save restoration, chapter progression, and reset. To rerun in the prepared cloud machine:

```bash
source /workspace/.yorgame-tools/activate
mcs -out:/tmp/ashlight-checks.exe Assets/Scripts/Battle.cs Assets/Scripts/CharacterClass.cs Assets/Scripts/SkillDefinition.cs Assets/Scripts/HeroDefinition.cs Assets/Scripts/Elements.cs Assets/Scripts/ChapterDefinition.cs Assets/Scripts/EnemyVisualDefinition.cs Assets/Scripts/ProgressData.cs Assets/Scripts/Summoning.cs Assets/Scripts/CombatMotion.cs Assets/Scripts/AutoBattlePlanner.cs Assets/Scripts/EnemySkills.cs Assets/Scripts/UltimateDefinition.cs Assets/Scripts/GearDefinition.cs Assets/Scripts/BattleResults.cs Assets/Scripts/ChapterStageDefinition.cs Validation/BattleChecks.cs
mono /tmp/ashlight-checks.exe
```

Unity is not installed in the onboarding machine. Unity script compilation, scene rendering, touch input, Android builds, and iOS builds have **not** been verified. Open in Unity to perform these checks before calling this a playable device build.

The existing Data.Layer project is separate and untouched. No backend or database is required. Further character art/animation, expanded audio, cloud save sync, accessibility settings, and performance tuning are future work. The development direction uses chapters rather than exploration.

## Updating an existing local copy

Stop Play mode. Copy **all** `.cs` files from this branch's `Assets/Scripts` into your local project's matching folder, including `CharacterClass.cs`, `HeroDefinition.cs`, `SkillDefinition.cs`, `Elements.cs`, `ChapterDefinition.cs`, `ProgressData.cs`, `ProgressStore.cs`, `Summoning.cs`, `CombatEffects.cs`, and `PressAction.cs`. Also copy the updated `Packages/manifest.json` so the JSON serialization module is enabled. Preserve your scene and settings. Unity will import the changes. Alternatively, extract the updated GitHub ZIP into a separate directory and open that `MobileGame` folder.

## Party, recruitment, and named heroes

Start with **Rowan**, a Common Knight. Each prototype victory recruits the next named hero when you press **Continue**. The first allies are Lucan (Paladin) and Elara (Sorceress). The active party never exceeds **five heroes**. Later recruits enter reserves; select a class button to open its named hero list. Selecting an active party member changes whose action you control. Selecting a recruited reserve replaces the currently selected party slot, before battle only. Two different named heroes of the same class can be in the party, but the same named hero cannot occupy two slots.

All living heroes receive one action before the enemy responds. Normal attacks favor living front-row allies, back-row strikes seek living back-row allies, and party-wide attacks hit everyone. Defeated allies cannot act. Party healing does not revive defeated allies. Full party defeat ends the battle.

The roster contains **30 named heroes**: five per class, across Common, Uncommon, Rare, Epic, and Legendary. Rarity scales base health, attack, skill damage, healing, and guard. This is a starting balance, not a final progression economy. See [HEROES.md](HEROES.md) for the complete roster and skill catalog.

Recruitment currently uses deterministic chapter victory rewards. There is no exploration area. Chapter progress, recruited named heroes, party slots, and active/reserve skill loadouts persist locally. Restart restores health and skill uses while retaining the roster and loadouts. A mid-battle reload starts that encounter fresh; a saved victory is processed once on the chapter menu, so its reward is retained.

## Equip two of six skills

Select a party hero through its class/hero list before battle, then press **Skills**. Select **Slot 1** or **Slot 2**, then choose from that class's six skills. The two slots must contain different skills. Press **Done** to return. Skill selection locks after combat begins. Both skill buttons appear beside the basic attack button; each has two independent uses per battle. Skills consume that hero's action. Higher rarity strengthens the same class skills rather than adding equipped slots.

## Elements and enemies

Named hero affinities follow class themes: Knight Physical/Fire; Paladin Fire/Light; Sorceress Cold/Lightning/Fire; Rogue Physical/Poison; Ranger Physical/Cold; Cleric Light. Basic attacks and skills without an explicit override use hero affinity. All current class skills have explicit affinities, independent of the named hero's basic attack affinity: Flame Lance uses Fire, Frost Ward uses Cold, Arcane Burst and Starfall use Lightning, and Frost Drain uses Cold. Some weapon skills use Physical, which is neutral against the current enemies. Skill affinity is displayed in the loadout panel.

Enemies show their weakness and resistance before you attack. Weakness multiplies damage by **1.5**; resistance by **0.5**; other damage is neutral. Damage is rounded, with at least one damage for a positive attack. Healing and guard are unaffected. Each chapter contains five uniquely named enemies with themed weaknesses/resistances; stage three reverses the usual matchup to encourage changing skill choices. Base health increases through the campaign (capped at 400), then scales to 1.33× in chapter 4 and 1.67× from chapter 5 onward. Stage five is a larger placeholder boss. These balance values remain provisional.

**Poison is currently an elemental damage type**, not a damage-over-time status. Burn, freeze, poison ticks, stun, cleanse, and revival are future mechanics. Explicit skill affinity overrides preserve their element across all rarity tiers.

## Validate the updated Unity presentation

The combat/roster model has been exercised outside Unity. The new UI, 3D party props, and builds remain unverified until opened in Unity. Check:

- Start shows the chapter menu, with Chapter 1 unlocked and later chapters locked. Choose Chapter 1; Rowan starts alone with two equipped skills.
- Win, Continue, and recruit Lucan, then Elara into reserves; additional active party slots unlock at chapters 2–5. Completing five stages unlocks Chapter 2.
- Each living hero acts once before the enemy attack; the targeted ally is identified.
- Skill selection rejects duplicates and cannot change during a fight.
- Recruit a reserve, select an active party slot, then replace it without exceeding five heroes.
- Named hero lists display rarity, affinity, scaled health/attack, and recruitment status.
- Weakness/resistance feedback agrees with the skill's shown element.
- Restart mid-attack cancels the old coroutine and preserves the roster/loadouts.

## Chapters and local saves

The six chapters are **The Lantern Road**, **The Frozen Pass**, **The Ember Citadel**, **The Storm Spire**, **The Blighted Garden**, and **The Eclipse Throne**. Each contains five distinct encounters. Only the current chapter is playable; previously cleared chapters show Completed and later chapters show Locked. After all 30 stages, the menu shows **Restart Campaign**. It returns you to Chapter 1, stage 1 while preserving recruited heroes, party composition, active/reserve skill loadouts, crystals and summon pity counters. Health and skill charges refill. The restart saves immediately and also works for previously completed saves. This button is only available after campaign completion; individual chapter selection/replay is not implemented. See [CHAPTERS.md](CHAPTERS.md) for the enemy list.

Progress uses Unity `PlayerPrefs` with versioned JSON and a previous valid snapshot as backup. Saves happen after summons, party/skill changes, victory, Continue, and on pause/focus loss/exit. Loading validates IDs, party size, unique members, recruited roster, skill choices, version, and stage bounds before applying anything. A valid backup is attempted if the primary save is unreadable. Each loaded encounter restores full health and skill uses. Unknown/incompatible snapshots are rejected rather than partially applied.

On Windows, saves are local to Unity's company/product preferences. On Web builds, they use browser storage for that website; clearing site data removes saves, private browsing may not retain them, and changing hosts does not transfer saves. This is **local saving, not cloud sync**. Unity storage behavior and the updated chapter UI require local verification; only the engine-independent snapshot/progression logic has been tested here.

Save keys are `Ashlight.Progress.v1` and `Ashlight.Progress.v1.backup`. For developers testing from a clean state, remove both keys in Unity deliberately; no automatic save deletion is performed during project updates.

All six classes use the included rigged male/female human meshes after project preparation. See the human character setup below.

## Free hero summons

The chapter menu has a **Summon — 100** button. New profiles start with **300 crystals**; each first stage clear grants **50 crystals** when you Continue. There are no purchases, ads, or real-money payments in this prototype.

| Rarity | Base chance | Duplicate crystal refund |
| --- | ---: | ---: |
| Common | 60% | 20 |
| Uncommon | 25% | 30 |
| Rare | 10% | 50 |
| Epic | 4% | 70 |
| Legendary | 1% | 90 |

Each rarity has six named heroes with equal conditional odds. Every fifth consecutive pull without Rare-or-better guarantees Rare-or-better; every fifteenth consecutive pull without Legendary guarantees Legendary. A naturally rolled better rarity is retained. Legendary pity takes priority. Any Rare/Epic/Legendary resets the Rare+ counter, and any Legendary resets the Legendary counter, including duplicates. The displayed numbers show how many pulls remain until each guarantee; pity means actual odds differ from the base rates near the thresholds.

Summons are available between encounters and after campaign completion, never during an active fight. Newly summoned heroes go to reserves, preserving the active party and its chapter-based five-hero cap. Duplicate heroes return the listed crystals without adding another copy. The result shows name, class, quality, affinity, and whether it is new or a duplicate. The hero pool remains fixed across chapters. Deterministic chapter rewards now award the first hero you do not own so a summon cannot cause a duplicated story reward.

Currency, recruits, and both pity counters save immediately. The version-2 snapshot accepts existing version-1 saves and gives those profiles the one-time 300-crystal starter balance on migration, preserving chapters/roster/loadouts. Save keys remain unchanged. Each cleared encounter awards crystals once when you Continue. After restarting a completed campaign, encounters award crystals again; this deliberately allows repeat play and earning more summons in the free prototype. Existing heroes are never duplicated by story rewards; any missing hero can still be recruited on a replay. Restarting does not grant starter crystals again.

Validation checks all probability boundaries, exact base-rate partition, both guarantees, duplicate refunds, invalid rolls, insufficient funds, rewards claimed once, save round trips, arbitrary summoned roster order, and legacy migration. The updated Unity summon screen and browser persistence still require local testing.

This is a local prototype, not a payment-ready economy. Real-money purchases would require store integrations and server-authoritative receipts, balances, hero ownership, and summon results.

## Combat effects

Attacks now render lightweight element-colored trails, impact bursts, and floating damage text (including Weak/Resist feedback). Fire creates orange embers; Cold creates stretched cyan shards; Poison uses green bubbles; Lightning draws a zigzag bolt; Light uses gold rings; Physical uses white trails and sparks. These are procedural prototype effects, not final authored assets.

Healing displays rising gold motes and the actual restored amount. Guard shows a ring and Guard label; a fully absorbed enemy hit shows Blocked. Successful dodge/parry has its own ring and label; parries also show the elemental counter hit. Impacts add a subtle, short vertical camera movement. None of these change battle damage or timing rules.

The effect pool is capped at 48 reusable objects with six shared colored materials. Restarts and chapter transitions clear effects and camera offsets. Preparation generates `Assets/Resources/CombatEffects.mat` referencing the built-in shader so Web/Android builds retain it. To update an existing project, copy **all scripts**, including the new `CombatEffects.cs`, and the updated **`Assets/Editor/ProjectSetup.cs`**, then choose **Ashlight > Prepare Mobile Project** before building.

The 1,513 engine-independent checks remain passing. Unity effect rendering has not been executed in this cloud environment. In local Play mode, verify each element, successful dodge/parry, healing, guard, restart while effects are active, and changing chapters. Phone/browser performance and shader inclusion still require a real build test.

## Battle HUD visibility

Hero selection, Skills, and Chapters appear only before the first action. Once combat starts, those controls disappear and heroes act in automatic party order. Restart is hidden throughout combat and returns after defeat; Continue appears after victory. Chapters also returns on the result screen. The selection overlay is closed whenever preparation ends. Attack, equipped skills, Auto Play, health, and automatic defense feedback remain on the battle HUD.

## Human character models

The project includes two adult human base models derived from MakeHuman/MPFB's CC0 anatomical mesh and skeleton, with original fitted clothing and basic animation. These replace the rejected procedural outfit figures. Preparation generates **12 class/gender prefabs**: Knight, Paladin, Sorceress, Ranger, Rogue, and Cleric, each with a male and female visual. Women have cropped fitted bodices/armor, shorts, and tall boots; men have broader shoulders and jawlines, short cropped hair, facial hair, higher-neck sleeveless cuirasses, shorts, and tall leg coverings. Sorceress/Cleric add split robe panels. Class colors and small bone-attached weapon props distinguish the six classes.

1. Download the updated project separately, or copy **Assets/ThirdParty/MakeHuman**, the updated **Assets/Editor** and **Assets/Scripts** folders into your current Unity project. Preserve your local scenes and settings. Delete the old `TemporaryOutfit.cs` script if updating files manually; it is no longer used.
2. Wait for Unity to finish importing the FBX files, then choose **Ashlight > Prepare Mobile Project**. The helper generates matching prefabs, materials, and two animation controllers in `Assets/Resources/Heroes`.
3. Open the battle scene and press Play. Rowan should now have an actual human mesh. Recruit/select a woman to see the female model. All genders use the same stats, skills, rarity rules, and progression.
4. **When updating from the earlier human models, choose Ashlight > Characters > Build Realistic Humans after the FBX import.** This refreshes the generated material slots, including facial hair. If a human prefab is missing, choose **Ashlight > Characters > Build Realistic Humans**. This explicitly rebuilds class/gender prefabs; automatic preparation preserves existing prefabs. A missing model shows a neutral capsule and a Console warning rather than the old outfit figures.

Models have faces, fingers, natural proportions, a 53-bone skeleton, opaque mesh clothing, and seven basic states: Idle, Attack, Cast, Dodge, Parry, Hit, Death. Root motion is disabled. A named prefab at `Resources/Heroes/Named/<hero-id>.prefab` still takes priority over a class/gender prefab. These older base models are retained as fallback assets. Dark fantasy class/gender models now take priority for named heroes without custom prefabs; unique faces, detailed skin/hair textures, polished animation and weapon meshes remain future art work. These are simple human models, not film-quality or photorealistic characters.

The 30 authored hero profiles include 18 women and 12 men, all adults ages 24–52. Stable IDs and gameplay are unchanged. Men in the Sorceress class display as Sorcerer; its class-selection button reads Sorcery.

**Verified here:** both FBX exports were independently reimported in Blender to check mesh budget, skeleton/hand attachments, weighted meshes, material roles, and all seven animation states. A Blender render was inspected for proportions and wardrobe fit. The asset report, source revision, license and checksums are in [Assets/ThirdParty/MakeHuman/CREDITS.md](Assets/ThirdParty/MakeHuman/CREDITS.md). Regeneration and validation tools are in `Tools/`. **Unity compilation/import, animation playback, battle appearance and phone/browser performance remain unverified here.** Check floor contact, facing, weapon attachment, clothing clipping during each action, and returning to Idle in Play mode.

The older licensed KayKit cartoon asset remains archived in `Assets/ThirdParty/KayKit`, with its optional build menu, but is not selected by gameplay. Previously generated `Resources/Heroes/Knight.prefab` and its materials/controller are unused; remove them deliberately if you do not want them included in Resources builds. No user assets are deleted automatically.

Additional free garment candidates remain documented in [WARDROBE.md](WARDROBE.md). None of those downloaded community clothing packs is bundled in this version.

## Updating the campaign restart button

Copy the updated `Assets/Scripts/Battle.cs` and `Assets/Scripts/Prototype.cs` into your current project, then press Play. Existing completed saves show the new button on the chapter menu automatically. No save deletion or project preparation is needed. The engine-independent checks cover restarting a completed save, preserving roster/party/loadouts/currency/pity, restoring battle readiness, reloading the restarted campaign, repeat rewards, and rejecting restart during unfinished encounters. Unity layout and button interaction still need local testing.


## Combat motion, audio and Auto Play

Copy the updated `Assets/Scripts`, `Assets/Editor`, `Assets/Art` and **`Assets/Resources/Audio`** into your Unity project. Wait for import/compilation, then run **Ashlight > Art > Refresh Combat Presentation** and press Play. This rebuilds the generated class/gender, Rowan and enemy controllers/prefabs; other custom named hero prefabs stay intact.

Class attacks now have anticipation, contact, follow-through and recovery: sword swings, heavier Paladin attacks, Rogue double strikes, Ranger bow release and caster charge/release. Enemy windup/strike poses, recoil, dodge, parry and death clips are updated. Impacts and 21 original free sound effects follow the contact frames; brief animator-only contact pauses do not stop the battle clock. Sound On/Off is on the chapter screen and persists across sessions.

Dodge and Parry buttons are removed. Defense automatically rolls once for each incoming strike, in manual and Auto Play modes. “Miss” means failed defense and normal incoming damage; Guard still reduces it. Current prototype probabilities (editable in `AutoBattlePlanner.DefenseChances`):

| Class | Dodge | Parry | Failed defense |
| --- | ---: | ---: | ---: |
| Knight | 15% | 45% | 40% |
| Paladin | 10% | 50% | 40% |
| Sorceress | 35% | 5% | 60% |
| Ranger | 50% | 10% | 40% |
| Rogue | 55% | 15% | 30% |
| Cleric | 25% | 15% | 60% |

**Auto Play: On/Off** chooses a living target, evaluates the two equipped skills against attack damage, missing health and guard, and respects skill charges. It can be toggled during an animation; the current action completes. It stops at victory/defeat and does not advance chapters or spend crystals. Manual action and target controls remain usable.

Validated here: **2,209 engine-independent combat/planner/motion checks**, independent Blender reimports of all 19 FBX exports, and all 21 WAV files' checksums, mono 22,050 Hz PCM format, amplitude and endpoints. Unity compilation, imported clip playback, audio listening, clipping and phone performance still require local testing. Check each class, support skills, automatic counters, both boss turns, Auto Play toggling and chapter/restart transitions in Play mode.

Regenerate motion after the existing geometry generators:

```sh
blender -b --factory-startup --python-exit-code 1 --python Tools/polish_combat_motion.py -- /tmp/ashlight-dark-fantasy /tmp/ashlight-dark-roster /tmp/ashlight-enemies /tmp/ashlight-polished-motion
python Tools/build_combat_audio.py Assets/Resources/Audio/Combat
```

Copy the polished FBX outputs to `Assets/Art/DarkFantasy/Models`. Contact frames and clip lengths are recorded in `MOTION_REPORT.json`; `CombatMotion.cs` contains the matching presentation timing.


## Enemy skills and elemental debuffs

Enemies cycle through Quick Strike (35 physical damage), a special and a finisher. Chapter one uses Crushing Blow (42 physical damage). Later chapter specials are Cold Hex, Fire Hex, Lightning Hex, Poison Hex and Light Hex (24 damage). Normal enemies finish with Heavy Strike (45 damage); bosses use a party-wide Sweeping Strike or elemental Tempest (22 damage to each living hero). Each boss tracks its own cycle. The HUD warns with the skill, damage, element, debuff and target before it strikes.

Each affected hero rolls class-based defense independently. A successful dodge/parry prevents that hero's damage and debuff; parries counter the attacker. Guard reduces incoming damage and is consumed on an undefended hit. Matching a hero's affinity to an elemental attack reduces its damage by 25% (rounded up) and prevents the associated debuff. Physical damage receives no affinity resistance.

| Debuff | Effect |
| --- | --- |
| Burn | 6 damage at the end of the enemy round |
| Poison | 8 damage at the end of the enemy round |
| Chill | 25% less outgoing damage |
| Shock | 5 extra damage on an undefended incoming hit, before resistance and guard |
| Weaken | 25% less outgoing damage |

Debuffs occupy one slot per hero, refresh/replace on a new application, and expire after two enemy-round endings, including the round of application. Both bosses finish before statuses tick. Damage over time can defeat heroes and ignores guard. Reset, chapter continuation and loading clear encounter statuses and enemy attack cycles. Party health labels show active statuses; impacts display debuff names and ticking damage. Auto Play uses the same combat rules.

Copy updated `Assets/Scripts` into Unity and wait for compilation. Test the second and third enemy attacks, a boss's party-wide attack, affinity resistance, each debuff, two bosses and Auto Play. Unity compilation/playback remain unverified in the cloud environment.

Validated here: **2,297 engine-independent checks**, including all five debuffs, affinity resistance, independent party defense, guards, two-boss round timing, reset and lethal damage over time.


## Hero energy and unique ultimates

Every party hero has a labeled energy bar above the action buttons. A successful attack or equipped skill grants **25 energy**, up to **100**. Invalid actions, enemy attacks and parry counters do not generate energy. At full energy, the hero automatically follows their normal action with their unique ultimate, then resets to zero. This works in both manual and Auto Play modes and costs no skill charge or extra turn. If the normal action wins the battle, no ultimate is needed. Energy resets for a new encounter, restart or loaded encounter.

All 30 named heroes have authored ultimate names and target patterns in `UltimateDefinition.cs`. Single-target base damage is 100–119; area base damage is 60–74 **per living enemy**, before rarity scaling, elemental weakness/resistance and damage debuffs. At every rarity, each single-target ultimate deals more base damage per enemy than any area ultimate. Area attacks can deal more combined damage against two enemies. Ultimates use the hero's affinity. A single-target ultimate follows the selected enemy, falling back to a living target if the normal action killed it.

Examples: Rowan’s **Last Ember** hits one enemy; Brenna’s **Cinderfall** hits all enemies; Wren’s **Widow’s Kiss** hits one enemy; Kestrel’s **Viper Dance** hits all enemies. Cleric ultimates also heal all living allies; Paladin ultimates grant guard to all living allies. Ultimate details appear in hero selection. During the follow-up animation, the bar turns gold at full energy and drains on impact; the skill name and elemental effects identify the ultimate. Existing class attack/cast clips are reused for the follow-up.

Update `Assets/Scripts` in Unity, wait for compilation and press Play. Test filling a hero's bar through attacks and skills, manual and Auto Play triggers, area damage against two bosses, target fallback, support effects and resets. **2,363 engine-independent checks passed**, including execution of all 30 ultimates, unique names, damage ordering at every rarity, charge accounting, target fallback and energy reset. Unity compilation, layout and playback still require local testing.


## Hero levels and shard upgrades

Victory awards each participating party hero **40 + 10 × chapter index XP**, plus **20 XP** for a boss encounter. Both bosses count as one encounter/reward. Claim XP by pressing Continue after victory; newly recruited heroes and reserves receive no XP for that fight. Defeated participating heroes still receive XP. The pending-victory save resumes the same one-time reward claim; losses and restarts grant no XP.

Heroes start at level 1 and cap at **level 20**. The next level costs `100 + 25 × (level − 1)` XP; excess XP carries over, and XP stops accumulating at the cap. Each level adds 5% of base stats. Duplicate summons retain the existing crystal refund and add **10/15/20/25/30 named-hero shards** for Common/Uncommon/Rare/Epic/Legendary duplicates.

Before battle, open hero selection or Skills and select **Upgrade [active hero]**. Ranks cap at **+5**, cost **20/40/60/80/100 shards**, and each rank adds 10% of base stats. Shards are specific to that named hero. Levels and upgrade ranks increase health, attack, equipped skills' damage/healing/guard, and ultimate damage/support. This growth is applied on top of rarity stats. Hero cards and battle labels show levels; selection shows XP, rank, shards and upgrade cost. Upgrades lock during combat.

Progress now uses save version 3. Existing version 1/2 saves migrate with heroes at level 1, rank 0 and zero XP/shards, preserving roster, loadouts, chapters and the existing economy. Levels and shards survive reserve swaps, reloads and campaign replay. Invalid progression data is rejected before modifying live progress. Copy updated `Assets/Scripts` into Unity and test Continue rewards, leveling, duplicates, the Upgrade button and reload. Unity compilation/UI playback still require local validation.

Validated here: **2,377 engine-independent checks**, including XP caps, duplicate shards, upgrade costs/caps, combat locks, save migration, invalid save rejection, reserve persistence, campaign replay and one-time boss/victory rewards.


## Equipment and victory loot

Each named hero has **Weapon, Armor and Accessory** slots. Before attacking, open **Equipment**, choose a party hero, then a slot and an owned item. Use Previous/Next to browse or Unequip to release the item. Equipment controls disappear during combat. To remove gear from a reserve, put that hero in the party before battle and open Equipment.

Weapons add `6 × rarity tier` attack and `3 × tier` damage to damaging skills and ultimates. Armor adds `20 × tier` health and `2 × tier` protection against direct incoming hits; protection does not reduce damage over time. Accessories add `6% × tier` damage to their matching element, including attacks, skills, ultimates and counters. Gear bonuses apply on top of level/upgrade stats; Auto Play evaluates the equipped damage bonuses. Common through Legendary tiers are 1–5. All classes can equip all gear.

Press Continue after victory to claim **one gear item per encounter**, along with XP and crystals. A two-boss battle drops one item after both bosses die. Ordinary drop rarity odds: Common 55%, Uncommon 30%, Rare 12%, Epic 2.5%, Legendary 0.5%. Boss drops guarantee Rare or better: Rare 55%, Epic 35%, Legendary 10%. The three slots and six elemental themes have equal odds. No drops are awarded for defeat or restart.

The 90 gear definitions stack in shared inventory. Every equipped copy is reserved for one named hero, including heroes in reserve; unequipping releases it. Loot, inventory and equipment survive reloads, party swaps and campaign replay. Save version 4 migrates versions 1–3 while preserving existing hero growth and adds an empty equipment inventory. Invalid item IDs, wrong slots, invalid stack counts and equipping more copies than owned are rejected before changing live progress.

Copy the updated `Assets/Scripts` into Unity. Win a battle, press Continue, enter the next chapter and open Equipment to equip your drop. **2,406 engine-independent checks passed**, including loot rewards, boss rarity floors, stat/element bonuses, equipment ownership, combat locks and save migration. Unity compilation, UI layout and playback still require local testing.


## Battle results and chapter party limits

After victory and the final action animation, a results panel displays XP per participating hero, crystals, the exact gear drop with rarity/stats, the next recruit, and each hero's **actual damage dealt and healing restored**. Damage includes normal actions, ultimates and parry counters; healing excludes overhealing. Each hero row previews the resulting level and XP, highlighting level-ups. **Continue · Claim Rewards** grants the displayed rewards once and returns to chapters. Auto Play stops on the results screen.

Rewards are prepared at victory and saved before claiming. Version 5 saves preserve the pending loot and contribution totals, so restarting Unity resumes the same results screen. It does not automatically claim rewards or reroll the drop. Existing version 1–4 saves migrate; older pending victories receive a new reward preview because those versions did not save result statistics.

Party capacity is **1 hero in chapter 1, 2 in chapter 2 and 3 in chapters 3–6**. Story recruits remain in reserve until a slot opens. Entering a new chapter automatically fills the new slot from available recruits, and you can change those heroes before attacking. Summoning does not bypass the cap. Campaign replay starts again with one active hero; all other heroes, progression and equipped gear remain in reserve. Older saves with excess active heroes move those heroes to reserve; new saves enforce the chapter cap. Party HUD and chapter cards show the capacity.

Copy updated `Assets/Scripts` into Unity and press Play. Check results after a normal attack, ultimate and counter; Continue claiming; restarting Unity with rewards unclaimed; and party expansion at chapters 2 and 3. Unity compilation, screen layout and playback still require local validation.

Validated here: **2,453 engine-independent checks**, including reward previews/claims, pending-result reloads, damage/healing attribution across all 30 ultimates, chapter party caps, chapter transitions and legacy roster migration.


## Chapter environments and complete enemy visuals

All six chapters now select their own environment at runtime: lantern-lit ruins; a blue frozen pass with snowdrifts, ice formations and snowfall; an ember citadel with furnace towers and glowing channels; a violet storm spire with charged obelisks; a green blighted garden with twisted roots and glowing growths; and an eclipse throne with steps, a throne and a luminous halo. Each has its own stone palette, sky, fog, directional light and torch glow. Atmosphere is capped at 48 particles, geometry is combined by material, and lighting keeps one shadow-casting directional light plus two shadowless points. Chapter transitions deactivate the old stage immediately and dispose its generated meshes/materials.

The missing visuals were unassigned chapter 2–6 enemies. The visual catalog now covers **all 34 named campaign enemies**, including the four companion bosses. Enemy prefabs use the existing six animated source models with matching palette, glow and size variants. These are shared humanoid/canine silhouettes; creatures such as Flame Drake, Thunder Hawk and Venom Bloom currently use those animated proxies rather than bespoke dragon, bird or plant meshes. Missing generated variants prefer an existing animated source/reference prefab before using a primitive fallback.

Update **`Assets/Scripts` and `Assets/Editor`**, keeping `Assets/Art/DarkFantasy` present. Wait for Unity import/compilation, then run **Ashlight > Art > Build Enemy Roster**. This rebuilds enemy prefabs/controllers for every chapter and creates the atmosphere material so its shader is included in builds. Then press Play. Existing cubes/placeholders at the generated enemy paths are replaced by that rebuild. If source models are missing locally, copy `Assets/Art` from the repository first.

Validated here: **2,570 engine-independent checks**, complete encounter-to-visual coverage, unique resource paths, all six referenced FBX files present, and six distinct stage themes. Unity compilation/import, chapter appearance, camera framing, animation playback and mobile performance require local testing. Test each chapter, both bosses, chapter transitions, replay returning to chapter one and background/resume.

Atmosphere compatibility fix: snow and motes now use one animated billboard mesh (40 snowflakes or 24 motes), with no Particle System module dependency. Update both `Assets/Scripts/DarkFantasyStage.cs` and `Assets/Editor/DarkFantasySetup.cs`, then run **Ashlight > Art > Build Enemy Roster** to refresh the atmosphere material.

## Unity save serialization compatibility

Empty equipment strings are accepted as unequipped slots, and empty inline results objects are accepted when no victory is pending. Unknown gear IDs, malformed inventories, nonempty unclaimed-result payloads on a non-victory save, and invalid reward data still fail validation. Existing progress and backups are retained. **2,579 engine-independent checks passed**, including fixtures representing Unity's empty-string/empty-object serialization and reward reload/claim checks.

Update `Assets/Scripts` and `Assets/Editor`. For a real Unity JSON round-trip check, run **Ashlight > Diagnostics > Check Save Serialization**. It tests fresh progress, combat, pending rewards, claims and equipped gear in isolated battles without reading or writing PlayerPrefs. This editor check has not been executed in the cloud environment, where Unity is unavailable.


## Boss mechanics

Chapter 3’s Cinder Sovereign spends his third action charging Inferno. Deal 60 total health damage to him during the next party turn to interrupt: the following attack drops from 70 Fire damage with Burn to 20 without Burn. Shield absorption and enemy-turn counters do not count toward interruption.

Chapter 5’s Thorn Queen replaces every second action in her three-action cycle with Root Communion: heal her living partner by 20% of their maximum HP, capped at maximum. Alone, she heals herself by 10%. Fallen partners stay defeated.

Chapter 6’s Veiled Monarch replaces every second action in his three-action cycle with Eclipse Veil. A 60-point damage shield alternates between his partner and himself; only one boss is shielded. Normal attacks, skills, counters and ultimates consume the shield before dealing health damage. Defeating the Monarch breaks the veil.

Charge progress and remaining shields appear in the enemy target labels, with casting and healing effects. Auto Play prioritizes charged bosses, then unshielded healers, then other unshielded enemies. These mechanics spend an enemy action and never trigger dodge or parry rolls. Restarting or loading a fight resets its temporary mechanics.

To test this update in an existing project, copy the updated **Assets/Scripts**, let Unity compile, then replay the final encounters of chapters 3, 5 and 6. No new art import is required. Pure C# combat checks run in the cloud; Unity playback and UI still need editor validation.


## Three-wave stages

Every stage in the game now has three waves. Wave 1 contains a chapter enemy; wave 2 contains another enemy (two together from chapter 2 onward). These enemies have 30% of their normal health and deal 60% of normal damage. Wave 3 is the original stage encounter: chapter finales still end with their boss, including two bosses together from chapter 3 onward.

A 1.2-second transition replaces defeated enemies and shows the next wave number. Living heroes keep health, energy, remaining skill charges, guard and status effects; fallen heroes stay down. Each new wave begins a fresh party turn. Party and equipment changes stay locked after combat starts. Auto Play continues through transitions and stops at the final results screen.

XP, crystals, recruitment and gear are granted once, after all three waves and pressing Continue. Existing saves remain compatible. Loading or restarting an unfinished stage begins wave 1 with a restored party, matching the existing fresh-encounter save behavior; pending final victories still reopen their saved results.

Copy the updated **Assets/Scripts** into your existing Unity project to test. No new assets are required. The pure C# checks cover all 90 campaign waves, resource carryover, transitions, reward timing and pending-victory save compatibility. Unity animations, layout and difficulty still need playtesting.


## Battle speed

The **SPEED** button beside Auto Play cycles **1× → 2× → 3× → 1×**. It works during manual play and Auto Play, including attacks, ultimates, enemy windups and wave transitions. The choice is remembered as a local preference. All controls stay below the hero energy bars.

Menus and results run at normal speed; stopping Play or disabling the prototype restores normal time. Damage, energy gain, skill charges and class defense rolls are unchanged. Sound pitch remains normal. To verify in Unity, switch speed mid-action, try Auto Play across waves, open the results and chapter menus, and stop Play; confirm animations accelerate and menus return to normal.

Copy updated **Assets/Scripts** to test this update. Unity playback is required to verify animation timing and the compact mobile button layout.


## Five-hero formations

The active party unlocks one slot per chapter: **1 / 2 / 3 / 4 / 5 heroes** in chapters 1 / 2 / 3 / 4 / 5+, respectively. Slots 1–2 are the front row; slots 3–5 are the back row. Before the first action, press **Formation**, tap one hero, then another to swap them. Formation changes preserve each named hero’s level, skills and equipment and save the party order. Formation controls disappear during combat and remain locked between waves.

Normal single-target strikes cycle through a weighted list with three entries per living front hero and one per living back hero. With a full party, front heroes take two-thirds of normal target selections. Normal enemies’ third action is a **Back Row Strike**, which targets a living back hero and falls back to a living front hero when necessary. Boss party-wide attacks still hit every living hero. Class-based dodge/parry chances are unchanged.

Enemy maximum health scales with unlocked party size: chapters 1–3 retain existing health, chapter 4 multiplies it by **4/3**, and chapters 5–6 multiply it by **5/3**, rounded up. The increase applies to every wave and both bosses, independent of how many heroes survive. For example, a 400-HP boss becomes **534 HP** in chapter 4 and **667 HP** in chapters 5–6. Damage is unchanged; these health values are provisional for playtesting.

All five heroes have health/energy labels and energy bars. Equipment selection and result contributions support five heroes. The arena displays two front models and three back models with a wider camera view. Existing saves remain compatible; unlocked empty party slots fill from recruited reserves when opening the game before combat. Saved victories keep their original participating heroes until Continue claims rewards and expands the party. Campaign restart returns to one active slot while retaining the collection.

Copy the updated **Assets/Scripts** and **Assets/Editor** into Unity. Test swapping Knight/Paladin to the front and ranged/healing heroes to the back, wave resource carryover, five-hero ultimates and boss results. **Ashlight > Diagnostics > Check Save Serialization** now also checks five-hero formation order and pending results using Unity’s JSON serializer without modifying player saves. Cloud checks cover combat and save-data validation; Unity compilation, serialization diagnostics, mobile layout and animation playback still need editor testing.


## Automatic class passives

Passives are enabled in the game for every named hero, alongside the two equipped skills and unique ultimate. Select a hero and open **Skills** to read its passive.

| Class | Passive |
| --- | --- |
| Knight | **Vanguard:** while alive in a front slot, reduces direct damage to the other front hero by 20%, rounded up after reduction. Does not protect itself or reduce damage over time; multiple Knights do not stack the reduction. |
| Paladin | **Sacred Aegis:** before each enemy round, raises each living ally’s Guard to at least 8. Existing stronger Guard stays intact. Applies once per round, including paired bosses; multiple Paladins do not stack it. |
| Sorceress / Sorcerer | **Arcane Exposure:** 20% extra damage when that hit’s element exploits the target’s weakness. |
| Ranger | **Hunter’s Mark:** 20% extra damage against back-row enemies. |
| Rogue | **Deadly Precision:** each damaging player attack, skill hit or ultimate hit has a 25% chance to deal 1.5× damage. Healing, Guard and enemy-turn counters do not roll critical hits. Rogues also bypass enemy Defender protection. |
| Cleric | **Mending Prayer:** after the complete enemy round and damage-over-time ticks, heals the living ally with the lowest health percentage for 10% of the Cleric’s maximum HP, capped at the ally’s maximum. Fallen Clerics do not heal and fallen allies stay defeated. Each living Cleric heals once. |

Offensive bonuses apply before equipped elemental boosts, weakened/chilled penalties and enemy shield absorption. Result contributions count actual health damage and healing; passive healing belongs to its Cleric. Auto Play evaluates the Sorceress and Ranger bonuses when choosing a skill. Floating labels identify passive damage, protection, Guard and healing effects.

Existing progress and formations remain compatible. Copy the updated **Assets/Scripts** into Unity, then try different front heroes, ranged enemies, wounded allies and Rogue ultimates. Pure C# checks cover the original combat rules and a complete 90-wave campaign with passives enabled. Unity compilation, floating labels and battle balance still require local playtesting.


## Enemy roles and formations

Enemies now have **Defender, Bruiser, Archer, Caster, Healer or Support** roles. Defenders and bruisers stand in front; archers, casters, healers and support enemies stand in back. Their role and row appear beside their name and in target buttons. A back enemy can stand alone; defeat of its front partner removes protection without changing its row.

From chapter 2 onward, wave 2 pairs a chapter Defender with a back-row threat. Final stages retain their existing boss pairs. Thorn Queen is a Healer behind Rotbound Consort; Veiled Monarch is Support behind Eclipse Harbinger. Their existing healing and shield mechanics still work.

A living front Defender reduces damage to back enemies by **20%** (remaining damage rounded up), without stacking. **Rogues bypass this protection**; **area ultimates bypass it** for every class. Basic attacks, single-target skills/ultimates and other heroes’ counters are reduced. Enemy shields then absorb the remaining damage as before. Defeating the Defender removes its protection immediately. Protected enemies show **GUARDED**.

The Ranger’s **Hunter’s Mark** now grants +20% damage to actual back-row enemies. Auto Play prioritizes healers and support enemies, gives Rangers and Rogues priority on other rear threats, and accounts for Defender protection when choosing a skill. Formation and roles derive from the existing enemy roster, so saves remain compatible and no art imports are required.

Copy updated **Assets/Scripts** into Unity. Test chapter 2 wave 2, then the chapter 5/6 boss pairs: attack a protected rear enemy, defeat its Defender, compare Ranger/Rogue damage and try an area ultimate. Pure C# checks cover all enemy roles, mixed waves, protection removal, shield interaction, single/area ultimates and the full campaign. Unity rendering, target-button readability and difficulty still require playtesting.

## Hero progression choices

Before combat, open **Skills > Progression** and select a hero. Every class has two choices at each of **levels 5, 10 and 15**; select one per tier. Choices belong to each named hero, including reserves, and survive reloads and campaign replay. Changing choices and **Free Reset** cost nothing while testing. Choices lock once fighting begins.

| Class | Level 5 | Level 10 | Level 15 |
| --- | --- | --- | --- |
| Knight | Stronger front protection / stronger counters | More health / stronger damaging skills | More protection / stronger counters |
| Paladin | More party Guard / stronger skill healing | More health / stronger damaging skills | More Guard / party healing after enemy rounds |
| Sorceress | Stronger weakness bonus / stronger damaging skills | Faster energy / more health | Stronger weakness bonus / stronger ultimate |
| Ranger | Stronger rear damage / stronger damaging skills | Faster energy / more health | Stronger rear damage / stronger ultimate |
| Rogue | Higher critical chance / stronger critical hits | Faster energy / more health | Higher critical chance / stronger critical hits |
| Cleric | Stronger prayer / prayer cleanses burn or poison | Faster energy / more health | Stronger prayer / party burn and poison cleanse |

The screen shows exact bonuses and level locks. Faster energy gives 35 energy per attack or skill, triggering an ultimate after three actions. Healing and cleansing happen after enemy actions and damage-over-time ticks; fallen heroes stay defeated. Bonuses work with equipment, class passives and Auto Play.

Save version **6** migrates existing saves with no choices selected, preserving hero growth, equipment, formations and pending victory rewards. Invalid choices or choices below their unlock level are rejected before changing progress.

Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **6,316 engine-independent checks passed**, including all 36 choices, free resets, save migration, combat interactions and the complete 90-wave campaign. **Ashlight > Diagnostics > Check Save Serialization** now also checks progression choices. Unity compilation, serialization diagnostics and progression screen layout still need local testing.


## Varied enemy teams and stage preview

Starting a stage now opens **Upcoming Enemies**, showing all three waves, roles, rows, elemental weaknesses and resistances. Choose **Prepare Party** to adjust heroes, formation, equipment or skills; **Enemy Preview** reopens it before the first action. The preview pauses Auto Play and closes when preparation ends.

Chapter 1 retains single-enemy introductory waves. From chapter 2, wave 2 varies its rear opponent by stage: caster, archer, healer, support, then caster. From chapter 3, stages 2 and 4 also open with a two-enemy ambush. All enemies use existing chapter models, and final encounters keep their original boss pairs.

Enemy roles now drive actions in wave battles: archers target rear heroes, casters apply elemental hexes and a party attack every third action, healers restore a living ally every third action, and support enemies grant a non-stacking 25-point shield every third action. Defenders use Shield Bash and bruisers use heavier physical attacks. Early-wave attack damage retains its 60% scaling; existing boss skills, protection, healing caps and no-revival rules remain intact.

Existing saves remain compatible and unfinished stages restart from wave one. Copy updated **Assets/Scripts** into Unity. **6,706 engine-independent checks passed**, including previews, role-specific skills, actual healer/support actions, save reloads and full campaign runs. Unity compilation, UI layout, animation playback and balance need local testing.

The intended direction is now an AFK Arena-inspired team-building RPG: five heroes, front/back formation, automatic combat and energy ultimates, with our own roster and visual style. Idle rewards and faction synergies are planned follow-ups, not implemented in this update.


## Idle rewards

The chapter screen now has an **Idle** reward button showing stored minutes, crystals and XP per hero. Rewards accumulate while the game is open or closed, up to **12 hours**. Claiming grants XP to every currently recruited named hero, including reserves, and adds crystals to the shared wallet. Claims are available before combat and after campaign completion; they do not interfere with unclaimed victory results.

Each full minute grants **1 + floor(furthest cleared stage / 5) crystals** and **2 + floor(furthest cleared stage / 5) XP per hero**. Finishing more stages raises the rate; restarting the campaign preserves the highest stage reached. Fractional minutes carry forward after a claim. Time above the 12-hour cap is discarded when claimed. Currency stays within the existing one-million limit, and heroes retain their level-20 cap.

Save version **7** preserves the claim timestamp and furthest stage. Versions 1–6 migrate with a fresh timer when first loaded; they receive no retroactive rewards for time before this feature. Loading a current save retains accumulated time and cannot duplicate an already saved claim. A backward clock change gives no rewards until time catches up. This prototype uses the device UTC clock and local storage; server-authoritative timing would be needed for an online economy.

Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **6,735 engine-independent checks passed**, covering elapsed time boundaries, the storage cap, fractional carryover, repeat claims, reloads, migration, reserve XP, campaign replay, invalid timestamps and wallet limits. The Unity serialization diagnostic checks timestamp preservation, but Unity compilation, actual offline/resume behavior and chapter-button layout still require local testing.


## Hero factions and team bonuses

Every named hero now belongs to **Dawn Order, Wilderkin or Veilborn**. Each faction has ten heroes covering all six classes and five rarities; faction identity is authored independently from elemental affinity. Factions appear on hero cards, skill selection and formation slots. The formation screen and battle HUD show faction counts and the current team bonus.

**Three or four matching active heroes** grant the whole party **+10% maximum HP and damage**. **Five matching heroes** grant **+20%**. Only the strongest threshold applies, with no stacking. All active heroes receive the bonus, including heroes from other factions. Reserve heroes do not contribute. Defeated heroes remain part of the lineup, so the bonus stays stable throughout the fight and between waves.

HP, attacks, damaging skills and ultimates receive the bonus after level/rank growth, talent upgrades and equipment; counters receive it before elemental and enemy protection adjustments. Increases round up. Direct skill healing and Guard values do not increase; support passives based on maximum HP naturally benefit from the HP bonus. Replacing heroes before combat recalculates the whole party and removes bonuses from reserves. Formation swaps preserve the bonus. Chapter party limits remain unchanged: a three-hero bonus first becomes possible in chapter 3, and five matching heroes become possible in chapter 5.

Faction assignments and bonuses derive from the saved named heroes and party; no new save format is required. Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **6,819 engine-independent checks passed**, including thresholds, every faction's roster, bonuses across the whole party, reserve exclusions, equipment/talent stacking, replacement, fallen heroes, replay, reloads and a full campaign with factions enabled. The Unity serialization diagnostic also checks a five-matching-hero bonus. Unity compilation, diagnostic execution, UI readability and difficulty still require local playtesting.


## Ascension and hero collection

Open **Collection** on the chapter screen to browse your recruited heroes in pages of six. Cards show current rarity, faction, class, level, HP, attack, party/reserve status and **Can Ascend**. Select a card to inspect its ultimate and shard requirement, then press **Ascend**. Reserve heroes and heroes after campaign completion can ascend; combat and pending victory rewards lock upgrades.

Ascension replaces the former shard-rank upgrade presentation. Each named hero has five purchased ascension steps costing **20, 40, 60, 80 and 100 hero-specific shards**. Steps raise rarity until Legendary, then add Legendary stars; each star adds 10% rarity-based stats. A Common hero finishes at Legendary ★1, while a hero recruited as Legendary can reach ★5. Levels multiply rarity and star growth. Names, factions, affinities, equipped skills, talents, gear and unique ultimates stay attached to the same hero. Summon odds and duplicate shard awards continue to use the original roster entry, so ascension does not alter banners or duplicate identity.

Save version **8** retains the existing rank field as ascension steps. Older purchased ranks and remaining shards carry over automatically; the new growth formula preserves or improves old maximum HP at every level and rank. No hero or gear sacrifice is required. The Skills screen now offers the same Ascend action rather than a second shard upgrade system. Collection stats include faction bonuses for active heroes; reserves show their own stats.

Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **11,084 engine-independent checks passed**, covering all 30 heroes and 150 ascensions, costs, caps, identity preservation, reserve upgrades, migration, reloads and all previous-level/rank HP comparisons. The Unity serialization diagnostic now checks ascended rarity too. Unity compilation, diagnostic execution, collection layout and progression balance still require local testing.

## Shared hero levels

Once you own **five heroes**, the Collection screen unlocks five shared-level slots. The shared crystal level is the **lowest personally earned level among your five highest-level recruited heroes**, whether those heroes are active or in reserve. Select a lower-level hero and press **Link · Lv N**; press **Unlink** to release its slot. Linking, unlinking and changing slots are free before combat and after campaign completion.

Linked heroes fight at the higher of their own level and the shared level. Health, attacks, skills and ultimate growth use this combat level, alongside their existing rarity, ascension, gear and faction bonuses. Collection, selection and the battle roster show boosted levels as `10 (own 1)`. A linked hero keeps the boost when placed in the party. Linked heroes' boosts never contribute to calculating the shared level, preventing feedback loops.

Each hero still earns their own XP from battles and idle rewards. Removing a link retains earned XP, levels, equipment, ascension and skill choices; talent unlocks continue to require the hero's own level. Slots stay linked as heroes catch up and can be released manually. Higher mentor levels automatically update the crystal after rewards are claimed. Campaign replay preserves mentors and links. Linking locks once combat starts and cannot be used to refill health mid-fight.

Save version **9** stores the five linked hero IDs; saves from versions 1–8 migrate with empty slots and preserve all previous progress. Duplicate, unknown, excessive or malformed link data is rejected before modifying progress.

Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **11,119 engine-independent checks passed**, including the five-leader calculation, slot capacity, stats, own XP preservation, party swaps, unlinking, mentor growth, idle rewards, migration, invalid saves and a full 90-wave campaign using shared slots. The Unity serialization diagnostic includes a level-1 reserve fighting at shared level 15. Unity compilation, diagnostics, Collection layout and live balance still require local testing.


## Battle power and stage difficulty

Hero power now appears in **Collection** cards and selected-hero details. **Formation** and the battle HUD show total active-team power. **Enemy Preview** compares team power with the stage rating, gives an **Easy / Even / Hard** estimate, and shows individual wave ratings alongside the existing roles and elements.

Hero ratings use maximum health, attack, the two equipped skills, healing/Guard support, ultimate damage and support, energy gain, critical bonuses, armor protection, elemental accessories and unlocked talents. Level growth, ascension, shared levels and faction buffs already feed those stats. Team power sums active heroes; reserves do not count. Current HP, energy, spent charges and debuffs do not change this preparation rating.

Enemy ratings include the actual chapter-scaled HP, role-specific attacks, early-wave damage scaling, party-wide targeting, healing/shields and unique boss threats. Stage power combines the strongest wave with 25% of the other waves to represent resource carryover. Boss pairs include both enemies. **Easy** means team power is at least 125% of stage power; **Hard** means at most 85%; otherwise it is **Even**. These are initial tuning estimates, not win probabilities or combat modifiers: elemental matchups, formation, enemy protection and targeting still matter.

Scores derive from existing progress, so the save format stays at version 9. Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **11,374 engine-independent checks passed**, including all hero growth paths, equipment/faction/shared-level changes, steady ratings during combat, all stage previews, difficulty thresholds and reload consistency. Unity compilation, UI readability, serialization diagnostics and difficulty calibration still require local playtesting.


## Daily quests

Open **Daily Quests** on the chapter screen. Quests reset at **00:00 UTC** and have explicit reward-claim buttons:

| Quest | Reward |
| --- | --- |
| Win three stages and claim their victory results | 40 crystals + 10 Rowan shards |
| Successfully claim idle rewards | 20 crystals + 5 Rowan shards |
| Successfully summon a hero, including a duplicate | 30 crystals + 10 Rowan shards |
| Claim all three quest rewards | 100 crystals + 10 shards for every hero currently recruited |

Failed actions and individual waves do not count. Stage wins count when Continue claims the result, avoiding unclaimed or repeated-victory credit. Completed quests cap their counters and each reward can be claimed once per UTC day. Claiming quests locks during combat and pending victory results. Unclaimed rewards expire on the next day; campaign replay preserves that day's quest state. Moving the clock backward does not reset the day or previously claimed rewards. The prototype uses the device clock and local storage, like idle rewards.

Save version **10** stores the day, counters and claim flags. Older saves migrate to a fresh current-day quest set without retroactive credit. Invalid counters, impossible claims and malformed data are rejected before changing progress. Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **11,426 engine-independent checks passed**, including real stage wins, successful/failed actions, exact reward amounts, repeat claims, reloads, UTC midnight, clock rollback and migration. The Unity serialization diagnostic includes daily claim state. Unity compilation, diagnostic execution and daily-screen layout still require local testing.


## Ten-pull summons and results

The chapter screen now offers **Summon x1 · 100** and **Summon x10 · 1,000**. Ten pulls require the full 1,000 crystals upfront; duplicate refunds are credited during the batch. Every pull uses the original odds, Rare+ guarantee within five pulls and Legendary guarantee within fifteen. Guarantees and counters carry between single pulls and batches, with no extra batch-only rarity rule.

Both buttons open a dedicated results screen. Single pulls show a large card; ten pulls show ten cards with rarity colors, names, classes, factions, new/reserve status, duplicate crystal refunds and actual shards granted. The header shows spending, total refunds, new heroes, balance and remaining pity counts. Repeat buttons disable if you cannot afford their cost. New heroes join reserves; repeated copies in the same batch correctly grant duplicates. Summons still count toward the daily summon quest.

Batch inputs are fully validated before any mutation. A valid batch matches ten sequential single pulls; insufficient funds or invalid rolls cannot partially spend currency. All rewards save before the results screen opens. Reloading retains heroes, shards, currency and pity, while the temporary results screen closes. The existing version-10 save remains compatible.

Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **11,457 engine-independent checks passed**, including sequential equivalence, multiple pity triggers, within-batch duplicates, actual shard caps, invalid last rolls, upfront affordability, combat locks and reloads. The Unity serialization diagnostic includes ten-pull rewards. Unity compilation, diagnostic execution and single/ten-card layouts still require local testing.


## Tower mode

The chapter screen now offers **Tower**, unlocked once you own five heroes. It has **100 floors**, each with three waves and room for five heroes. Floors cycle through existing chapter enemy teams, elements, scenery and boss patterns; every fifth floor ends with a boss encounter. Later paired-boss patterns retain both bosses and their unique mechanics. Enemy HP multipliers rise by 8 percentage points per floor, and attack multipliers by 3, on top of the existing wave and party scaling. Team previews show Tower floor numbers and scaled power estimates.

Floor victory rewards are **80 + 5 × floor crystals**, **60 + 2 × floor XP per participant**, an equipment drop, and **five hero-specific shards per participant** (ten on every fifth floor). Continue claims them once and unlocks the next floor, then returns to the chapter menu. Failed floors can be retried freely. All 100 cleared floors finish the current Tower. Tower victories also count toward daily battle quests.

Tower progress is independent from chapter progress. Tower clears do not recruit story heroes or increase the idle-reward campaign rate. Entering stores your campaign lineup, fills Tower slots from your recruited reserves and permits preparation as usual. Leaving restores the previous campaign lineup and chapter slot limit; your earned levels, equipment, shards and other hero progression remain. A pending victory must be claimed before leaving. Unfinished floor reloads restart at wave one, and pending Tower victories preserve their prepared loot and rewards.

Save version **11** stores Tower mode, next floor and the previous campaign lineup. Older saves migrate with Tower floor one and retain all other progress. Copy updated **Assets/Scripts** and **Assets/Editor** into Unity. **15,815 engine-independent checks passed**, including all 100 floors, once-only rewards, pending-result reloads, original lineup restoration, invalid saves and Tower access after campaign completion. These simulations use successful defenses and do not establish live balance. Unity compilation, serialization diagnostics, menu layout and automatic-combat difficulty still require local testing.
