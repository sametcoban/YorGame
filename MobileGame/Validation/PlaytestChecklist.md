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
