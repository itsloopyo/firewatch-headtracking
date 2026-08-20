# Changelog

## [0.1.2] - 2026-08-18

### Added

- honor remote recenter trailer, recenter only on first connect

### Fixed

- show full control set in pixi install via shared -Controls
- follow core's per-connection smoothing split
- match stub member kinds to the shipped Unity assemblies

## [Unreleased]

### Changed

- Troubleshooting now points at `MelonLoader/Latest.log` and the `OpenTrack
  connected` line in it as the first check for "no tracking response", so a
  report can be answered from one file.
- The mod no longer keeps a centre of its own, and the recenter hotkey is gone
  (`Home` / `Ctrl+Shift+T`, plus the `RecenterKey` preference). Every tracker
  app centres itself, so a mod-side centre sat in series with the tracker's and
  the two drifted apart. Centre in your tracker app instead: OpenTrack's Center
  bind, or the CENTER button in a phone tracker app. The mod now applies the
  tracker pose as absolute.
- Smoothing is now two MelonPreferences entries instead of one:
  `LocalSmoothing` (default 0.0) applies when the tracker runs on this machine,
  `RemoteSmoothing` (default 0.15) applies when the tracker is a remote device
  on the network. The value is selected per connection from the packet source
  address and re-evaluated every frame, so switching trackers needs no restart.
- Removed `Smoothing` and `PositionSmoothing`. Both new entries cover rotation
  and position, so there is no separate position smoothing key.
- Removed the hidden 0.15 baseline smoothing floor. Local users now get
  zero-latency tracking by default instead of a silently enforced minimum.

## [0.1.1] - 2026-06-07

### Added

- add HeadTrackingSession and expand C++ core with RE Engine, Unreal, and tracking-session modules
- aim projection, reframework/unreal hooks, input/logging hardening, games
- live smoothing, position Z fixes, nightly release channel
- add Mass Effect Legendary Edition to games catalog
- expand games catalog, fix unicode games.json read, stage launcher manifest
- add Pacific Drive to games catalog
- add Homeworld: Remastered Collection to games catalog
- add manifest-mode installer validator and ASI loader subdir support
- authenticate GitHub API requests via env token when present
- migrate to launcher-manifest.json and game-free CI build
- add R.E.P.O. detection data

### Fixed

- fail fast in ASI dev-deploy when the game is running
- restore il2cpp camera position by undoing applied local delta
- set SO_REUSEADDR so the receiver reclaims its port on relaunch

### Other

- scripts: detect BepInEx 6 IL2CPP via BepInEx.Core.dll marker
- powershell: skip cameraunlock-core remote refresh in CI
- scripts: add UE4SS install template, fix delayed expansion in ASI body, expand games registry
- protocol: reject finite-but-out-of-float-range packet values
- data: add Subnautica 2 to games registry
- detection: add installer-registry game path lookup (Black & White GameDir)
- protocol: reorder tracking data member in udp_receiver
- data: fix Subnautica 2 Steam app id (3367150 -> 1962700)
- data: add Ni no Kuni Remastered and Yakuza 0; switch find-game output to UTF-8
- detection: add Xbox/GDK build support for Subnautica 2 (and any future GDK title)
- find-game: escape `&` in GAME_DISPLAY_NAME so echo doesn't split
- templates: add uninstall.ps1; data: add Deus Ex Mankind Divided
- powershell: add NightlyRelease module for Patreon-gated nightly builds
- protocol: disable SIO_UDP_CONNRESET and add one-shot receiver diagnostics; powershell: write nightly manifest.json without UTF-8 BOM; data: add Mixtape
- powershell: stop redirecting git stderr in Update-CameraUnlockCoreToRemoteTip
- powershell: publish dev builds as GitHub pre-releases
- protocol: disable SIO_UDP_CONNRESET and add one-shot receiver diagnostics
- data: add Mixtape
- powershell: stop redirecting git stderr in Update-CameraUnlockCoreToRemoteTip
- powershell: run gh under Continue so its stderr doesn't abort the dev-release publish
- reframework: strip VR runtime DLLs on install for flatscreen mode
- reframework: cache GetValue method and avoid per-call heap in ArrayGetValue; data: add BioShock Infinite
- uninstall: remove reframework_revision.txt marker dropped at game root
- install: render MOD_CONTROLS multi-line via percent expansion
- Add YAPYAP to games.json
- powershell: write state file BOM-less so Lopari JSON parser accepts it
- powershell: stop redirecting git stderr in Invoke-VersionCommit

## [0.1.0] - 2026-05-12

First release.
