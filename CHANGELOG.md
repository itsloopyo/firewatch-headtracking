# Changelog

## [0.4.0] - 2026-08-20

### Changed

- Maintenance release (no user-facing changes).

## [0.3.0] - 2026-08-20

### Changed

- Maintenance release (no user-facing changes).

## [0.2.0] - 2026-08-20

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

### Fixed

- restore the forward lean budget; InvertPositionZ becomes InvertTrackerZ

## [0.1.2] - 2026-08-18

### Added

- honor remote recenter trailer, recenter only on first connect

### Fixed

- show full control set in pixi install via shared -Controls
- follow core's per-connection smoothing split
- match stub member kinds to the shipped Unity assemblies

## [0.1.1] - 2026-06-07

### Changed

- Updated the shared cameraunlock-core submodule.

## [0.1.0] - 2026-05-12

First release.
