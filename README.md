# Neon Coast Racing — first playable

Unity 6000.3.24f1, Universal Render Pipeline 17.3.0, Windows desktop.

Controls:
- **Keyboard**: W/Up accelerate; S/Down brake/reverse; A/D or Left/Right steer; Space handbrake; Shift nitro; C camera; R recover; Esc pause.
- **Gamepad**: RT accelerate (analog); LT brake/reverse (analog); Left Stick steer (analog); X/Square handbrake; A/Cross or RB nitro; Y/Triangle camera; B/Circle recover; Start pause.

This milestone contains an original continuous coastal circuit with elevation, a covered section, a harbor service junction, collision boundaries, a custom sports car, wheel suspension, progressive steering, speed-dependent camera FOV, nitro, three-lap checkpoint timing, a HUD, and a night environment. The initial scene remains preserved as SampleScene.

All game geometry and procedural textures are custom-created. Dependencies are free official Unity packages: URP (including Shader Graph), Input System, Cinemachine, ProBuilder, uGUI/TextMeshPro. The Pipeline package is an editor connection tool, not a paid runtime dependency. TextMeshPro essential resources retain their supplied licenses. No paid assets or external asset-store models are used.

The current chase camera uses a dedicated damping script; Cinemachine is installed for later camera authoring. Reflection probes use baked cubemaps for lower runtime cost. Most scenery is marked static and materials support GPU instancing.

## Game Modes & Flow

- **Circuit Race**: 3-lap competitive race against 3 AI rivals with civilian traffic.
- **Time Trial**: Solo hotlapping with rivals and civilian traffic cleared from the circuit. Tracks and persists Personal Best Lap and Best Race time to `PlayerPrefs` (`NCR_BestLap`, `NCR_BestRace`), displaying badges and record splits on the HUD and results overlay.
- **Main Menu** (MainMenu.cs / MainMenuBuilder.cs): Title screen with neon styling, Start Race, Time Trial, and Quit buttons with keyboard/gamepad shortcuts. Generated via `Neon Coast/Build Main Menu Scene`.
- **Pause Menu** (PauseMenu.cs / GameUIBuilder.cs): In-game overlay with Resume, Restart, and Quit to Menu. Triggered by Esc or Start during gameplay.
- **Race Results** (RaceResults.cs / GameUIBuilder.cs): Post-session overlay showing final standing / record status, elapsed time, best lap, and record badges.

## Vehicle Selection & Garage

Players can select and customize vehicles directly from the Main Menu:
- **Aster GT** (Coastal Spec): Balanced all-rounder with sharp steering response and progressive grip.
- **Valkyrie Apex** (Hyper Interceptor): Raw brute acceleration and blistering top speed (+35% motor torque, 260 km/h top speed, heavier steering).
- **Shinobi R-Spec** (Drift Specialist): Ultra-nimble tuner with high steer angle (36°), loose drift friction, and +80% nitro recovery during drifts.
- **Custom Finishes**: Ion Cyan, Coral Neon, Solar Amber, and Phantom Violet. Selections are persisted to `PlayerPrefs` and automatically configure vehicle physics, body styling, and metallic paint when entering any session.

## High-Visibility HUD Overhaul

The dashboard UI was overhauled for legibility and aesthetic polish:
- **ScreenSpaceOverlay Rendering**: The HUD canvas bypasses camera post-processing so text is never blurred or blown out by bloom.
- **Frosted Obsidian Backing Cards**: High-contrast semi-translucent dark panels (`rgba(5, 10, 18, 0.90)`) with neon cyan/coral accent lines prevent text from blending with track and city lights.
- **Modular Dashboard Modules**:
  - **Top-Left Status Card**: Brand title, selected car name, position pill (`POS 1 / 4` or `TIME TRIAL`), lap count (`LAP 1 / 3`), and gate tracker.
  - **Top-Right Chrono Card**: Large digital stopwatch, personal record lap split, and animated glowing badges for `[NITRO]` and `[DRIFT]`.
  - **Bottom-Right Instrument Card**: Big crisp digital speedometer (78pt), KM/H unit, gear indicator (`GEAR 1-6`), dynamic tachometer speed fill bar, and glowing nitro gauge with boost pulse.
  - **Bottom-Left Controls Card**: High-contrast controls reference pill.
  - **Center Alert Banner**: High-priority notifications for Countdown ("3", "2", "1", "GO!"), "FINAL LAP!", and "WRONG WAY!" warning.

## Visual Effects (VFX)

- **Dynamic Brake Lights**: Taillights dynamically flare to intense glowing red emission when braking or handbraking, and dim to soft running lights when accelerating.
- **High-Speed Camera Shake**: `ChaseCamera` introduces subtle micro-shake when nitro is engaged or exceeding 140 km/h.
- **Skidmarks** (Skidmarks.cs): Dual TrailRenderers on rear wheels during drifts (12+ km/h) aligned to WheelCollider ground hits.
- **Collision Sparks** (CarEffects.cs): Dynamic high-speed particle sparks emitted at barrier contact points upon impact.
- **Nitro Exhaust**: Dual cyan particle cones emitted during boost.

## Audio System

All audio is procedurally synthesized at runtime — no external audio files needed. CarEffects (on each car) provides:
- **Engine**: 6-harmonic sine wave with gear-based pitch simulation (player + AI cars)
- **Tire screech**: Bandpass-filtered noise during drifts (player only)
- **Wind noise**: Low-frequency filtered noise, volume scales with speed (player only)
- **Nitro whoosh**: Low-frequency rumble + noise when boosting (player only)
- **Collision impact**: Exponential-decay thud on barrier hits (player only)

RaceAudio (scene-level) provides:
- **Countdown beeps**: 880 Hz ticks at 3, 2, 1 and 1760 Hz tone at GO
- **Finish fanfare**: Ascending C6–E6–G6 three-note jingle

## Build Order

To generate or update the full game, run editor menu items in order:
1. `Neon Coast/Build Initial Racing Scene` — generates circuit and player car
2. `Neon Coast/Apply Racing and Art Pass` — adds rivals, traffic, detail, and facades
3. `Neon Coast/Build Main Menu Scene` — generates the main menu with Vehicle Selection Showcase
4. `Neon Coast/Rebuild High-Visibility HUD` — installs the high-contrast ScreenSpaceOverlay dashboard in NeonCoast scene
5. `Neon Coast/Add Pause and Results UI` — adds pause menu and race results to NeonCoast scene
6. `Neon Coast/Add Race Audio` — adds countdown beeps and finish fanfare to NeonCoast scene
7. `Neon Coast/Restore Ultra-Crisp Visuals` — tunes camera SMAA, disables motion blur smearing, calibrates bloom threshold, and sharpens atmospheric contrast

This is an initial playable foundation, not the complete production game in the original brief. Competitive AI, traffic, garage/progression, multiple race modes, dynamic weather, detailed vehicle interiors, and final art/audio production are not complete. The road uses continuous 3D geometry, not a 2D RuleTile palette.

Editor tools live in Assets/Editor/NeonCoast. The builder preserves an existing NeonCoast scene; do not regenerate over authored work. DrivingValidation is a temporary runtime test component used only for validation; PaceDriver supplies a simple path-following driver for track tests.
