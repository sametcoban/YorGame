# Ashlight: mobile 3D combat starter

An original combat prototype inspired by reactive turn-based RPGs. Uses simple capsule characters and a generated arena; this is a foundation, not a finished game. No Clair Obscur assets, names, music, or story are included.

## Open and play

1. Install Unity Hub and Unity **6000.0.65f1** with **Web Build Support (WebGL)** for browser testing on iPhone from Windows. Android Build Support is optional for Android APKs.
2. Open this `MobileGame` directory as a Unity project. Allow package import to finish.
3. The editor helper creates `Assets/Scenes/Battle.unity` and registers it for builds. If necessary select **Ashlight > Prepare Mobile Project**. Open that scene, then press Play.
4. Use **Attack**, then watch **GET READY** followed by **PARRY OR DODGE!**. The shrinking defense meter is yellow during the 180 ms parry window, then blue for the remaining dodge window (400 ms total). Defense registers on button press, not release. Restart resets the battle. Mouse clicks work in the editor; buttons accept touch on devices.

The combat presentation includes health bars, attack lunges, enemy windup, dodge motion, and parry/counter feedback. These procedural movements use placeholder capsule characters. Safe-area bounds update when the window or phone orientation changes. New presentation and touch-down behavior require Unity/device validation.

The scene starts empty intentionally: `Prototype` generates the camera, lighting, arena, characters, event system, and HUD at runtime. Use the built-in rendering pipeline. The UI uses the legacy input module; keep Active Input Handling set to **Input Manager (Old)** or **Both**. Landscape orientation and safe-area bounds are configured for the prototype. Device cutouts, aspect ratios, timing feel, and actual rendering need device testing.

## iPhone browser testing from Windows

This route uses Safari, without Xcode or a native app installation. Unity's mobile Web support varies by browser/device; use a recent iPhone with updated Safari. Real device validation is required.

1. In Unity Hub > Installs > your editor > Add modules, install **Web Build Support** (sometimes labeled WebGL Build Support).
2. Stop Play mode. Select **Ashlight > Browser > Configure Browser**, wait for importing, then select **Ashlight > Browser > Build Browser Game**.
3. A successful build creates `Builds/Browser/index.html` plus the `Build` folder. The custom template supplies a touch-friendly launch screen, landscape reminder, safe-area margins, loading progress, and error messages. Builds disable compression so static hosting does not require special compression headers.
4. Host the **entire `Builds/Browser` folder** with a static website host, such as Netlify's manual deploy, then open its HTTPS URL in Safari on your phone. Hosting uploads your game publicly; review the files and choose your host before uploading. No site has been published automatically.
5. Rotate the iPhone sideways and tap Play. Try attacks, timed dodges/parries, victory/defeat, and restart. Also check Safari's toolbar, orientation changes, and returning from the background.

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

The engine-independent combat model passes 1,335 checks covering party turns, the three-hero cap, recruitment, all 36 skills, two-skill loadouts, healing/guard, rarity scaling, elemental overrides, defense, victory/defeat, and reset. To rerun in the prepared cloud machine:

```bash
source /workspace/.yorgame-tools/activate
mcs -out:/tmp/ashlight-checks.exe Assets/Scripts/Battle.cs Assets/Scripts/CharacterClass.cs Assets/Scripts/SkillDefinition.cs Assets/Scripts/HeroDefinition.cs Assets/Scripts/Elements.cs Validation/BattleChecks.cs
mono /tmp/ashlight-checks.exe
```

Unity is not installed in the onboarding machine. Unity script compilation, scene rendering, touch input, Android builds, and iOS builds have **not** been verified. Open in Unity to perform these checks before calling this a playable device build.

The existing Data.Layer project is separate and untouched. No backend or database is required. Exploration, character art/animation, audio, progression, save data, accessibility settings, and performance tuning are future work.

## Updating an existing local copy

Stop Play mode. Copy **all** `.cs` files from this branch's `Assets/Scripts` into your local project's matching folder, including `CharacterClass.cs`, `HeroDefinition.cs`, `SkillDefinition.cs`, `Elements.cs`, and `PressAction.cs`. Preserve your scene and settings. Unity will import the changes. Alternatively, extract the updated GitHub ZIP into a separate directory and open that `MobileGame` folder.

## Party, recruitment, and named heroes

Start with **Rowan**, a Common Knight. Each prototype victory recruits the next named hero when you press **Continue**. The first allies are Lucan (Paladin) and Elara (Sorceress). The active party never exceeds **three heroes**. Later recruits enter reserves; select a class button to open its named hero list. Selecting an active party member changes whose action you control. Selecting a recruited reserve replaces the currently selected party slot, before battle only. Two different named heroes of the same class can be in the party, but the same named hero cannot occupy two slots.

All living heroes receive one action before the enemy responds. The enemy cycles targets across living allies. Defeated allies cannot act. Party healing does not revive defeated allies. Full party defeat ends the battle.

The roster contains **30 named heroes**: five per class, across Common, Uncommon, Rare, Epic, and Legendary. Rarity scales base health, attack, skill damage, healing, and guard. This is a starting balance, not a final progression economy. See [HEROES.md](HEROES.md) for the complete roster and skill catalog.

Recruitment is currently a battle-victory placeholder, not world exploration. Progress exists only during the current play session; persistent saves and story encounters are not implemented. Restart restores health and skill uses while retaining the current roster and equipped skills. Reserve hero loadouts are retained within the session.

## Equip two of six skills

Select a party hero through its class/hero list before battle, then press **Skills**. Select **Slot 1** or **Slot 2**, then choose from that class's six skills. The two slots must contain different skills. Press **Done** to return. Skill selection locks after combat begins. Both skill buttons appear beside the basic attack button; each has two independent uses per battle. Skills consume that hero's action. Higher rarity strengthens the same class skills rather than adding equipped slots.

## Elements and enemies

Named hero affinities follow class themes: Knight Physical/Fire; Paladin Fire/Light; Sorceress Cold/Lightning/Fire; Rogue Physical/Poison; Ranger Physical/Cold; Cleric Light. Basic attacks and skills without an explicit override use hero affinity. All current class skills have explicit affinities, independent of the named hero's basic attack affinity: Flame Lance uses Fire, Frost Ward uses Cold, Arcane Burst and Starfall use Lightning, and Frost Drain uses Cold. Some weapon skills use Physical, which is neutral against the current enemies. Skill affinity is displayed in the loadout panel.

Enemies show their weakness and resistance before you attack. Weakness multiplies damage by **1.5**; resistance by **0.5**; other damage is neutral. Damage is rounded, with at least one damage for a positive attack. Healing and guard are unaffected. The encounter cycles among Lantern Warden, Ember Sentinel, Storm Revenant, Blight Guardian, and Dusk Shade with increasing health (capped at 400). These enemy values remain provisional.

**Poison is currently an elemental damage type**, not a damage-over-time status. Burn, freeze, poison ticks, stun, cleanse, and revival are future mechanics. Explicit skill affinity overrides preserve their element across all rarity tiers.

## Validate the updated Unity presentation

The combat/roster model has been exercised outside Unity. The new UI, 3D party props, and builds remain unverified until opened in Unity. Check:

- Start shows Rowan alone, with two equipped skills and locked unrecruited heroes.
- Win, Continue, and recruit Lucan, then Elara; three characters appear together.
- Each living hero acts once before the enemy attack; the targeted ally is identified.
- Skill selection rejects duplicates and cannot change during a fight.
- Recruit a reserve, select an active party slot, then replace it without exceeding three heroes.
- Named hero lists display rarity, affinity, scaled health/attack, and recruitment status.
- Weakness/resistance feedback agrees with the skill's shown element.
- Restart mid-attack cancels the old coroutine and preserves the roster/loadouts.

Characters still use procedural capsule bodies and simple class props rather than finished character art or rigged animation.
