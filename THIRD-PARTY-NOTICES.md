# Third-Party Notices

This mod includes or links against the following third-party software. Each
component remains under its own license, reproduced or referenced below.

## MelonLoader

- **Version:** 0.5.7
- **License:** Apache-2.0
- **Upstream:** https://github.com/LavaGang/MelonLoader
- **Usage:** Unity mod loader that loads `FirewatchHeadTracking.dll` at runtime. Pinned to 0.5.7 because 0.6.x crashes on Firewatch's Unity 2017 Mono runtime.
- **Bundled:** yes. Shipped in the GitHub installer ZIP at `vendor/melonloader/MelonLoader.x64.zip` and used as the install-time source.

---

## HarmonyX (0Harmony)

- **Version:** ships with MelonLoader 0.5.7
- **License:** MIT
- **Upstream:** https://github.com/BepInEx/HarmonyX
- **Usage:** Runtime patching library used to patch `vgCameraController.LateUpdate` for look/aim decoupling. Linked at build time against `0Harmony.dll`; loaded at runtime via MelonLoader.
- **Bundled:** no. Provided by MelonLoader at runtime.

---

## OpenTrack

- **Version:** protocol only
- **License:** ISC
- **Upstream:** https://github.com/opentrack/opentrack
- **Usage:** Head pose source. The mod listens for OpenTrack's UDP packet format on port 4242. No OpenTrack code is bundled.
- **Bundled:** no.
