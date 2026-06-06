# TRnK.Toolkit Editor Tools

Editor-only tools that integrate into the Unity Editor workflow.

---

## Toolbar Tools

Toolbar tools appear directly in the main Unity Editor toolbar for one-click access. They are active editor-wide and require no scene setup.

### Scene Switcher

A toolbar dropdown that lists all scenes registered in **Build Settings**, letting you open any scene without navigating to it in the Project window.

**Features:**

- Lists all Build Settings scenes by name; disambiguates duplicates with their paths
- Right-click a scene entry to **mark it as the Startup Scene** — entering Play mode automatically loads that scene first, then restores the original scene on exit
- Optional: **Activate Loaded Additive** — when enabled, selecting a scene that is already loaded additively makes it the active scene instead of opening it fresh
- Survives domain reloads via `SessionState`

**Settings** (`Edit > Project Settings > TRnK.Toolkit`):
| Setting | Description |
|---------|-------------|
| Startup Scene Path | The scene that loads automatically when entering Play mode |
| Activate Loaded Additive On Select | Activate an already-loaded additive scene instead of re-opening it |

---

### Time Scale Tool

A slider embedded in the toolbar for adjusting `Time.timeScale` in real time during Play mode.

**Features:**

- Slider range: `0` → `10` (upper bound configurable in Project Settings)
- Syncs bidirectionally with **Project Settings → Time Manager** — external changes are reflected in the slider
- Reset button snaps back to `1.0`
- Time scale is automatically restored to `1.0` when exiting Play mode

**Settings** (`Edit > Project Settings > TRnK.Toolkit`):
| Setting | Description |
|---------|-------------|
| Time Scale Max | Upper bound of the slider (default `10`) |

---

### Clear PlayerPrefs

A toolbar button that clears all `PlayerPrefs` with a confirmation dialog.

**Features:**

- Confirmation dialog prevents accidental deletion
- If triggered **during Play mode**: exits Play mode, clears PlayerPrefs, then optionally re-enters Play mode
- Re-enter Play mode after clear is configurable

**Settings** (`Edit > Project Settings > TRnK.Toolkit`):
| Setting | Description |
|---------|-------------|
| Auto Re-enter Play After Clear | Re-enter Play mode automatically after clearing PlayerPrefs |

---

## Editor Windows

### Game Screenshot Tool

`Tools > TRnK Framework > Screenshot > Open Settings`  
Shortcut: `Ctrl + Shift + K` (Quick Capture without opening the window)

Captures screenshots from the Unity Editor during Play mode.

**Capture Modes:**

| Mode             | Description                                                                                            |
| ---------------- | ------------------------------------------------------------------------------------------------------ |
| `GameView`       | Captures the Game View at its current resolution × supersize multiplier                                |
| `SpecificCamera` | Renders a specific camera offscreen at a chosen resolution — UI (Screen Space Overlay) will not appear |

**Supersize (GameView mode):**  
Multiplies the Game View resolution before capture. A `2×` supersize on a 1080p Game View produces a 2160p (4K) image.

| Supersize | 1080p output | 1440p output |
| --------- | ------------ | ------------ |
| 1×        | 1920 × 1080  | 2560 × 1440  |
| 2×        | 3840 × 2160  | 5120 × 2880  |
| 3×        | 5760 × 3240  | 7680 × 4320  |
| 4×        | 7680 × 4320  | 10240 × 5760 |

**Settings (persisted per-project as a ScriptableObject):**
| Setting | Description |
|---------|-------------|
| Capture Mode | `GameView` or `SpecificCamera` |
| Supersize | Multiplier 1–4 (GameView mode only) |
| Save Folder | Destination folder for captured files |
| Reveal On Save | Opens the folder in Explorer/Finder after capture |

**Output filename format:** `{SceneName}_{Width}x{Height}_{Timestamp}.png`

> Capture requires **Play mode**. Quick Capture shows a notification toast if called outside Play mode.

---

### Timer Tracker Window

`Tools > TRnK > Timer Tracker`

> Timer Tracker is part of the **TRnK Timer** package (`com.trnkdev.unitytimer`). It is documented here for discoverability but lives in `Assets/TRnK Timer/`.

Visualises all active **Countdowns** and **Stopwatches** created via the `TRnK.Timer` system while in Play mode.

**Features:**

- Two tabs: **Countdowns** and **Stopwatches**
- Displays source GameObject, component name, elapsed/remaining time, and a smooth progress bar per row
- Stats bar: **IsAlive** (allocated slots with a valid owner), **Capacity**, **Free Slots**, **Slot Memory** (exact struct sizes via `Unsafe.SizeOf`)
- Paginated at 20 items per page for large timer counts
- Clears automatically on exiting Play mode

> Timers are created via `Countdown.Create` / `Stopwatch.Create`, or the `Delay` / `Repeat` extension methods on `MonoBehaviour` — see the [TRnK Timer README](../../TRnK Timer/README.md).

---

## Project Settings

`Edit > Project Settings > TRnK.Toolkit`

Central configuration for TRnK.Toolkit editor tools, stored at `Assets/Plugins/TRnK.Toolkit/Editor/TRnKSettings.asset` (auto-created on first use).

| Setting                            | Default  | Description                                                 |
| ---------------------------------- | -------- | ----------------------------------------------------------- |
| Startup Scene Path                 | _(none)_ | Scene loaded automatically on entering Play mode            |
| Activate Loaded Additive On Select | `false`  | Scene Switcher behaviour for already-loaded additive scenes |
| Time Scale Max                     | `10`     | Upper limit of the Time Scale toolbar slider                |
| Hide Toolbar                       | `false`  | Hides all TRnK.Toolkit toolbar elements                          |
| Auto Re-enter Play After Clear     | `true`   | Re-enter Play mode after clearing PlayerPrefs               |

---

## Setup Window

`Window > TRnK Framework > Setup`

A one-time project setup wizard for new TRnK.Toolkit projects.

**Tabs:**

- **Folders** — Creates a standard project folder structure (configured in `SetupFoldersSettings`)
- **Packages** — Installs a pre-defined list of Unity packages (configured in `SetupPackagesSettings`)

Both settings assets are serialised ScriptableObjects that can be customised per team.
