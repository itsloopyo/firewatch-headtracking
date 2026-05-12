# melonloader (vendored)

This directory contains a bundled copy of the upstream mod loader. It is the install-time
source of truth: install.cmd extracts directly from here and never reaches out to the network.
Refresh manually with `pixi run update-deps`, then commit.

## Snapshot

- Asset: `MelonLoader.x64.zip`
- Tag: `v0.5.7`
- Commit: `d6c92646a8638462d0fbb3c96523728090eb8b6d`
- Upstream URL: https://github.com/LavaGang/MelonLoader/releases/download/v0.5.7/MelonLoader.x64.zip
- SHA-256: `147d79e2d61ffd4d1da2811162478d3e22764bc2c5c93fa2c7f0e47dc792b4d0`
- Fetched at: 2026-05-02T11:41:36.2655094+01:00
- Source: github

Do not edit this directory by hand. Run ``pixi run package`` (or CI release) to refresh.
