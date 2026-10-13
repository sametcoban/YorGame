# Closed beta preparation

Status: opening guidance and local feedback tools are implemented. Phone testing is pending. No build has been distributed or approved for a store release by this update.

## Candidate record

- Record the Git revision, `BetaSupport.BuildTag`, Unity version, platform and build filename. Bump the build tag for the next externally tested candidate.
- Use Unity 6000.0.65f1 and the repository's Browser/Android build menus. Check the console has no compile errors and the scene has one audio listener.
- Update the entire Assets/Scripts and Assets/Editor folders; reuse the existing art and portraits. Preserve the player's saves.
- Run Ashlight → Diagnostics → Check Combat Assets and Check Save Serialization in Unity. Capture failures alongside the candidate revision.

## Opening session for testers

1. Start with Rowan, open Campaign and read or skip the first chapter scene.
2. Open Enemy Preview and Team Suggestion. Inspect the two suggested skills before applying or keeping your own choices.
3. Complete a stage manually or with Auto Play; observe energy, the unique ultimate and automatic defenses. Claim its result once.
4. Clear chapter 1, meet Lucan and prepare the two-hero chapter 2 team. Use Heroes → Next Upgrades to inspect XP, shards, talents and gear requirements.
5. Claim Idle Rewards and an earned chapter milestone. A summon or Wishlist choice is optional; no purchases are needed for the route.
6. At own level 5, select a free talent. Reach chapter 3 to try the daily boss and enemy guide.
7. Open Activities → First Journey to review progress, then use Feedback to record anything unclear or broken.

## Feedback procedure

Choose Bug, Balance, Confusing screen or General feedback. Describe what happened, what you expected and the steps to repeat it. Build the report, copy it, and share it with the developer only when you choose. Include a screenshot or recording if it helps explain a visual issue. The game does not send reports automatically.

The report contains the build tag, runtime/device model/OS, screen resolution, session-average FPS, save status, campaign/Tower state, current team/loadouts, gear upgrades, wallet and summon guarantee counters. It excludes device identifiers and raw PlayerPrefs. Notes stay as a local draft; remove their text to clear the draft. Clipboard support must be checked per target; manual selection/copy is the fallback.

## Gates before distributing a candidate

- [ ] Native Unity compilation, import and JSON diagnostics pass.
- [ ] Opening route and both manual/Auto Play battles work in the candidate build.
- [ ] Story, suggestions, portraits, summon summaries, feedback fields and overlays fit the target screen.
- [ ] Story and modal screens block combat actions; recommendation previews spend nothing.
- [ ] Pending results, summon batches, milestones and daily boss rewards survive reload without duplicate claims.
- [ ] An existing version-18 save migrates without losing heroes, Wishlist, gear upgrades, records or progress.
- [ ] iPhone Safari or the target browser has working controls, saving, clipboard fallback, audio after backgrounding and acceptable sustained battle performance.
- [ ] Android candidate is checked on a device or emulator before Android distribution.
- [ ] Record and resolve blocking crashes, lost saves and unreadable controls; list remaining known issues with the candidate.

The cloud model checks support development, but do not replace these build/device gates. Testing and distribution remain pending until those capabilities are available.

The second preparation update adds Sets, Bonds and Weekly Challenges. Include their playtest checklist before distributing `ashlight-beta-prep-2`; device gates remain pending.
