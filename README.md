# Firewatch Head Tracking

An unofficial MelonLoader mod that adds OpenTrack-compatible head tracking to Firewatch: look around by moving your head while your mouse still controls aim.

![Mod GIF](https://raw.githubusercontent.com/itsloopyo/firewatch-headtracking/main/assets/readme-clip.gif)

## Features

- **Decoupled look and aim** - head tracking moves the camera; aim stays on your mouse/controller
- **6DOF positional tracking** - lean and peek with head position

## Requirements

- [Firewatch](https://store.steampowered.com/app/383870/Firewatch/) (Steam or Xbox/MS Store)
- [OpenTrack](https://github.com/opentrack/opentrack) or a compatible head tracking app (smartphone, webcam, or dedicated hardware)
- Windows 10/11 (64-bit)

## Installation

1. Download the latest release from the [Releases page](https://github.com/itsloopyo/firewatch-headtracking/releases)
2. Extract the ZIP anywhere
3. Double-click `install.cmd`
4. Configure OpenTrack to output UDP to `127.0.0.1:4242`
5. Launch the game

The installer automatically finds your game via Steam registry lookup and Xbox/MS Store paths. If it can't find the game:
- Set the `FIREWATCH_PATH` environment variable to your game folder, or
- Run from command prompt: `install.cmd "D:\Games\Firewatch"`

### Manual Installation

If you prefer to install manually or the installer doesn't work for you:

1. Download [MelonLoader v0.5.7](https://github.com/LavaGang/MelonLoader/releases/tag/v0.5.7) (v0.6.x is not compatible with Firewatch's Unity 2017 Mono runtime)
2. Extract the MelonLoader zip into your Firewatch game folder (next to `Firewatch.exe`)
3. Launch the game once to initialize MelonLoader, then close it
4. Download the Nexus release ZIP (the one ending in `-nexus.zip`)
5. Extract it into your game folder - `FirewatchHeadTracking.dll` lands in `Mods/` and the `CameraUnlock.Core*.dll` files land in `UserLibs/`
6. Configure your tracker to output UDP to `127.0.0.1:4242`
7. Launch the game

## Setting Up OpenTrack

1. Download and install [OpenTrack](https://github.com/opentrack/opentrack/releases)
2. Configure your tracker as input
3. Set output to **UDP over network**
4. Host: `127.0.0.1`, Port: `4242`
5. Start tracking before launching the game

### Webcam Setup

No special hardware needed - OpenTrack's built-in **neuralnet tracker** uses any webcam for 6DOF face tracking.

1. In OpenTrack, set the input to **neuralnet tracker**
2. Select your webcam in the tracker settings
3. Set output to **UDP over network** (`127.0.0.1:4242`)
4. Start tracking before launching the game
5. Recenter in OpenTrack via its hotkey, and press **Home** in-game to recenter the mod as needed

### Phone App Setup

This mod includes built-in smoothing for network jitter, so if your tracking app already provides a filtered signal you can send directly from your phone to the mod on port 4242 without needing OpenTrack on PC.

1. Install an OpenTrack-compatible head tracking app
2. Configure it to send to your PC's IP on port 4242 (run `ipconfig` to find it)
3. Set the protocol to OpenTrack/UDP

**With OpenTrack (optional):** If you want curve mapping, a visual preview, or extra filtering, route through OpenTrack. Since the mod already listens on 4242, OpenTrack's input needs a different port. Set OpenTrack's input to "UDP over network" on a different port (e.g. 5252), set its output to `127.0.0.1:4242`, and point your phone app at port 5252. Make sure your firewall allows incoming UDP on the input port.

## Controls

Two equivalent binding sets - use whichever your keyboard has.

| Action                | Nav-cluster | Chord          |
|-----------------------|-------------|----------------|
| Recenter              | `Home`      | `Ctrl+Shift+T` |
| Toggle tracking       | `End`       | `Ctrl+Shift+Y` |
| Cycle tracking mode   | `Page Up`   | `Ctrl+Shift+G` |
| Toggle yaw mode       | `Page Down` | `Ctrl+Shift+H` |
| Toggle reticle follow | `Insert`    | `Ctrl+Shift+U` |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Normal head-tracked gameplay
2. Positional tracking disabled, rotational tracking enabled
3. Rotational tracking disabled, positional tracking enabled
4. Back to normal

`Page Down` / `Ctrl+Shift+H` toggles yaw mode:

- **World-space yaw (default):** yaw rotates around the world up-axis. Looking down at your feet and moving your head left/right still pans across the floor like a compass turn - "up" stays a constant.
- **Camera-local yaw:** yaw rotates around the camera's current up-axis. At extreme pitch this rolls the view, which some players prefer for aerial / zero-G content.

The `Ctrl+Shift+<letter>` chords are provided for keyboards without a navigation cluster (laptops, 60% / TKL layouts). Both bindings fire the same action.

## Configuration

Settings are stored in `UserData/MelonPreferences.cfg` under the `[HeadTracking]` section. The file is created on first run with sensible defaults.

```ini
[HeadTracking]
UdpPort = 4242

# Keybindings (Unity KeyCode names)
# See https://docs.unity3d.com/ScriptReference/KeyCode.html
RecenterKey = Home
ToggleKey = End
TrackingModeKey = PageUp
YawModeKey = PageDown
ReticleToggleKey = Insert

# Yaw mode: true = horizon-locked yaw (default), false = camera-local
WorldSpaceYaw = true

# Sensitivity (multipliers, 0.1-3.0)
YawSensitivity = 1.0
PitchSensitivity = 1.0
RollSensitivity = 1.0
InvertPitch = false

# Smoothing (0.0-1.0, remote connections add a minimum of 0.15)
Smoothing = 0.0

# Position tracking (meters)
PositionEnabled = true
PositionSensitivityX = 1.0
PositionSensitivityY = 1.0
PositionSensitivityZ = 1.0
PositionLimitX = 0.30         # side-to-side
PositionLimitY = 0.20         # up/down
PositionLimitZ = 0.40         # forward lean
PositionLimitZBack = 0.10     # backward lean (smaller to prevent clipping through player model)
PositionSmoothing = 0.15
InvertPositionX = false
InvertPositionY = false
InvertPositionZ = false

# Reticle
ShowReticle = true            # repositions the game reticle to follow the mouse aim point
```

## Troubleshooting

**Mod not loading:**
- Make sure you ran the game once after installing MelonLoader so it could initialize
- Verify `version.dll` exists in the game folder
- Verify `FirewatchHeadTracking.dll` is in `Mods/` and both `CameraUnlock.Core*.dll` files are in `UserLibs/`
- Check `MelonLoader/Latest.log` for errors

**No tracking response:**
- Verify your tracker is running and sending to `127.0.0.1:4242`
- Press **End** to make sure tracking is enabled
- Check the MelonLoader console for error messages
- Check that your firewall isn't blocking UDP port 4242

**Jittery / unstable tracking:**
- Increase the `Smoothing` value (e.g. 0.1 to 0.3)
- Reduce sensitivity in your tracking software
- Ensure stable lighting if using face tracking

**Wrong rotation axis:**
- Toggle `InvertPitch` in the config if vertical look is reversed
- Adjust the per-axis sensitivity multipliers in `MelonPreferences.cfg`

**Yaw feels wrong when looking up or down at extreme angles:**
- Try toggling between world-locked and camera-local yaw with `Page Down` (or `Ctrl+Shift+H`). World-locked (default) is horizon-stable; camera-local follows the camera's current up-axis.

## Updating

Download the new release and run `install.cmd` again. Your config is preserved.

## Uninstalling

Run `uninstall.cmd` from the release folder. This removes the mod DLLs. MelonLoader is only removed if the installer put it there. To force-remove MelonLoader anyway:

```
uninstall.cmd /force
```

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (any recent version)
- [pixi](https://pixi.sh/) task runner
- Firewatch installed (for game assembly references)

### Build

```bash
git clone --recurse-submodules https://github.com/itsloopyo/firewatch-headtracking.git
cd firewatch-headtracking

# Build and install to game
pixi run install

# Build only
pixi run build

# Package for release
pixi run package
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- [Campo Santo](https://www.camposanto.com/) / [Panic](https://panic.com/) - Firewatch
- [MelonLoader](https://melonwiki.xyz/) - Mod framework
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching library
- [OpenTrack](https://github.com/opentrack/opentrack) - Head tracking software

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by Campo Santo or Panic. "Firewatch" is a trademark of Campo Santo / Panic. Use this mod at your own risk; no warranty is provided.
