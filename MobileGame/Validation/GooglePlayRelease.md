# Google Play release preparation

Status: release build tooling is prepared. **No Android build has been generated, uploaded or published.** The cloud workspace has no Unity Editor or Play Console connection. Native compilation, signing and Android testing remain pending.

## Build the candidate on Windows

1. Use Unity 6000.0.65f1 with Android Build Support, SDK/NDK and OpenJDK installed through Unity Hub. Import the updated Scripts/Editor folders and Resources/CombatGlow.shader.
2. Run Ashlight → Prepare Mobile Project, Check Combat Assets and Check Save Serialization. Resolve native import/compile/shader errors before building.
3. Open Player Settings for Android. Choose the final package identifier (the project's initial example is com.ashlightstudio.ashlight), app version and positive bundle version code. Package identity cannot be changed for the same listing after publication; increment the version code on each upload.
4. In Publishing Settings use Keystore Manager to create/select a private **upload key**, select its alias and enter passwords locally. Back up the key and passwords privately. Do not upload keys/passwords to GitHub or chat. The project ignores .keystore/.jks files; arbitrary differently named signing files still require care.
5. Choose **Ashlight → Android → Build Google Play Bundle**. It uses IL2CPP, ARM64 and a non-development build, writing **Builds/Android/Ashlight.aab**. It validates signing configuration and restores the prior app-bundle toggle. This action builds locally; it does not upload or publish.
6. The target SDK is the newest installed SDK, not a promise of current Play compliance. Install the SDK required by your current Console policy and inspect the completed bundle for native-library/page-size and other compatibility requirements. Follow Console warnings for the actual candidate.

## Submit through Play Console

Use your Google Play developer account at https://play.google.com/console. Account registration/identity verification and any applicable testing requirements must be completed by the account owner. Exact production-access requirements depend on the account and current Console rules; follow the requirements displayed there.

Create the app listing, enable Play App Signing, and upload the AAB to an **internal testing** release first. Google keeps the app signing key; your private upload key authenticates uploads. Save the revision/build tag, package ID, app version/version code and candidate filename. Review pre-launch results and test actual Android behavior with an ARM64-compatible device/emulator or an Android tester. Many Windows emulators use x86 and cannot run an ARM64-only build; check ABI compatibility before using one. Play pre-launch testing can also provide device results. Neither an internal-test upload nor a passing cloud suite establishes production readiness.

Prepare final icon, screenshots, descriptions, support contact and privacy policy. Complete Data safety, content rating, target audience, ads/app-access and other Console declarations based on the actual shipping app and included services. The current prototype has earned-currency summons and local progress; no real-money purchase integration is implemented. Do not declare paid features that are not present, or assume future analytics/SDK data collection is covered by today's declarations.

Use ClosedBetaChecklist.md and PlaytestChecklist.md for save, battle, audio, overlay and performance checks. Then complete the account's required testing/production-access process and submit a production release for Google's review. A production listing is live only after its release is approved and rolled out; the current source ZIP is not a playable store release.
