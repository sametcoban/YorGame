# Ashlight: mobile 3D combat starter

An original combat prototype inspired by reactive turn-based RPGs. Uses simple capsule characters and a generated arena; this is a foundation, not a finished game. No Clair Obscur assets, names, music, or story are included.

## Open and play

1. Install Unity Hub and Unity **6000.0.65f1** with Android Build Support (SDK, NDK, OpenJDK). iOS is deferred until the Android prototype is validated.
2. Open this `MobileGame` directory as a Unity project. Allow package import to finish.
3. The editor helper creates `Assets/Scenes/Battle.unity` and registers it for builds. If necessary select **Ashlight > Prepare Mobile Project**. Open that scene, then press Play.
4. Use **Attack**, then wait for the yellow **FLASH**. Dodge within 400 ms, or parry within 180 ms to deal counter damage. Restart resets the battle. Mouse clicks work in the editor; buttons accept touch on devices.

The scene starts empty intentionally: `Prototype` generates the camera, lighting, arena, characters, event system, and HUD at runtime. Use the built-in rendering pipeline. The UI uses the legacy input module; keep Active Input Handling set to **Input Manager (Old)** or **Both**. Landscape orientation and safe-area bounds are configured for the prototype. Device cutouts, aspect ratios, timing feel, and actual rendering need device testing.

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
- Attack reduces enemy health by 25 and cannot be repeated during the enemy turn.
- Dodge during FLASH prevents damage; a prompt parry prevents damage and counters for 10. Missing defense costs 35 health.
- Win by defeating the enemy; lose by missing three defenses. Restart resets health and cancels a pending enemy turn.
- Test repeated restarts, background/resume, and screen locking. Timing feel, frame rate, and UI layout remain unverified on devices.

No APK has been built in this cloud workspace: Unity, its Android modules, and editor activation are unavailable here. The scripts prepare the build path but require an actual Unity build and phone test.

## iPhone

On **macOS**, switch the build profile to iOS and export an Xcode project. Open it in Xcode, select your signing team and unique bundle identifier, and run on a connected iPhone. App Store distribution requires Apple Developer membership. This Linux cloud machine cannot build/sign the iOS app.

## Validation and current limits

The engine-independent combat model passed 20 checks: turn gating, damage, dodge/parry timing, repeated defense rejection, counter damage, victory, defeat, and reset. To rerun in the prepared cloud machine:

```bash
source /workspace/.yorgame-tools/activate
mcs -out:/tmp/ashlight-checks.exe Assets/Scripts/Battle.cs Validation/BattleChecks.cs
mono /tmp/ashlight-checks.exe
```

Unity is not installed in the onboarding machine. Unity script compilation, scene rendering, touch input, Android builds, and iOS builds have **not** been verified. Open in Unity to perform these checks before calling this a playable device build.

The existing Data.Layer project is separate and untouched. No backend or database is required. Exploration, character art/animation, audio, progression, save data, accessibility settings, and performance tuning are future work.
