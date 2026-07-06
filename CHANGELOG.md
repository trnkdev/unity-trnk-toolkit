## [2.5.3] - 2026-06-01

### Breaking Changes: Services Migrated to TRnK Game System

`TimeService` and `NetworkService` have been removed from TRnK Toolkit and now live in dedicated packages inside **TRnK Game System**:

| Old | New package | New class |
|---|---|---|
| `TRnK.Services.TimeService` | `TRnK Game System / ServerTime` | `TRnK.ServerTime.ServerTimeService` |
| `TRnK.Services.NetworkService` | `TRnK Game System / Network` | `TRnK.Network.NetworkService` |

**Migration steps:**

1. Import the `ServerTime` and/or `Network` systems from **TRnK Game System**.
2. Replace `using TRnK.Services;` with `using TRnK.ServerTime;` and/or `using TRnK.Network;`.
3. Rename all `TimeService.*` call sites to `ServerTimeService.*`.
4. `NetworkService` API is unchanged — only the namespace changes to `TRnK.Network`.

**Also changed:**

- `TimeExtensions` methods that required `TimeService` (`TimeSince`, `SecondsSince`, `TimeFromNow`, `SecondsFromNow`, `IsToday`, and UTC variants) have moved to `ServerTimeExtensions` in the `TRnK.ServerTime` namespace. Add `using TRnK.ServerTime;` to restore them.
- `TimeExtensions` in Toolkit retains all pure formatting and manipulation methods (`ToClock`, `ToShortClock`, `ToReadableFormat`, `WithDate`, `WithTime`, `IsStartOfDay`, `IsStartOfWeek`, `IsStartOfMonth`) with no changes.
- Both services now load configuration from a `ScriptableObject` asset at `Resources/TRnK/` (urls, timeouts, poll interval).

---

## [2.4.2] - 2026-05-18

### Overhaul Extension Methods

- Add `BigNumberStyleExtensions` for formatting `TRnK.BigNum` values with configurable notation styles.
- Refactor `CollectionExtensions`: rename index-based `Swap(int, int)` to `SwapAt(int, int)` on arrays and lists to resolve overload ambiguity with value-based `Swap(T, T)`.
- Refactor `AnimatorExtensions`, `CameraExtensions`, `GameObjectExtensions`, `TransformExtensions`, `Vector2Extensions`, and `Vector3Extensions` with improved XML documentation and cleaner APIs.
- Refactor `NumberExtensions`, `StringExtensions`, `TextColorizeExtensions`, `TextFormatExtensions`, `TMPTextExtensions`, `TaskExtensions`, and `TimeExtensions` for consistency and correctness.
- Update `Docs/EXTENSIONS.md` and `TRnKDocDatabase` to reflect all API changes.
- Bump `package.json` version.


## [2.4.1] - 2026-05-17

### Refactor Pooling System

- Simplify Pool and PoolableObject APIs with clearer method names and XML documentation.
- Add ReleaseAfterLifetime component to auto-release poolable instances after a specified duration.
- Add PoolableParticle subclass for easy particle system pooling with auto-release on stop.


## [2.4.0] - 2026-05-11

### New Features and Improvements

- Add new extensions for Vector3 and Vector2.
- Simplify timer system.
- Refactor pooling system.

## [2.2.0] - 2026-03-05

### Major Update: Timer System Overhaul

- Rearchitect timer internals to a flat ECS-style slot array, replacing the old per-type pool model.
- Move all timer types and extensions into the `TRnK.Timer` namespace.
- Replace `InvokeAfterDelay`, `InvokeEvery`, and `InvokeEverySeconds` with `Delay` and `Repeat`.
- Replace `CancelHandler` with `TimerToken` readonly struct.
- Add non-capturing generic overloads to all fluent timer callbacks to reduce allocations.
- Remove `TimerUtils` prewarm helpers.
- Overhaul Timer Tracker window with stats bar, smooth progress bars, and editor debug API.

## [2.0.0] - 2026-02-05

### Major Update: NetworkService Overhaul

- Refactor NetworkService to improve reliability and performance.
- Introduce ConnectionStatus enum to represent network connectivity states.

## [1.9.22] - 2026-01-26

### Pooling Feature

- Add PrefabPool class to manage pooling of prefab instances.
- Add IPoolable interface for lifecycle hooks on pooled objects.
- Add PoolableBehaviour base class for pooled objects to self-manage their lifecycle without holding pool references.

## [1.9.16] - 2025-12-14

### TimeService Improvements

- Add fetching coroutine.
- Adjust TimeService time precision after fetching from online sources.

## [1.9.14] - 2025-12-01

### Timer Pooling Enhancements

- Add diagnostics to monitor timer pooling status in the editor.
- Overhaul timer system to using PlayerLoopDriver for better performance and management.
- Improve timer pooling mechanism to reduce garbage collection and enhance performance.

## [1.9.12] - 2025-11-10

### Major Timer Extension Updates

- Add InvokeEvery extension method to invoke an action repeatedly at a specified interval.
- Add InvokeEverySeconds extension method to invoke an action repeatedly at a specified interval in seconds.
- Add TickEverySeconds extension method to invoke an action repeatedly at a specified interval in seconds for a specified duration.
- Improve TimerTrackerWindow and Timer inspector window.

## [1.9.9] - 2025-11-02

### Introduces significant improvements and new features:

- New collection extensions
- Add option to hide toolbar tools
- Add more time scale max values

## [1.9.8] - 2025-10-13

### Add Debug Logger

- Add a simple debug logger to log messages, warnings, and errors with conditions.
- Replace all Unity default Debug.Log with TRnK.Logger.

## [1.9.7] - 2025-10-09

### Project Setup Window

- Add a setup window to guide users through initial project setup.
- Add a button to open the setup window under the tool section.

## [1.9.4] - 2025-09-25

### Overhaul Singleton Pattern

- Remove SceneSingleton due to its limited use cases and potential issues with scene management.
- Refactor PersistentSingleton to remove OnBeforeSceneLoad.

## [1.9.3] - 2025-09-24

### Toolbar Features

- Add a scene switcher dropdown to the toolbar for quick scene switching.
- Add a button to clear player prefs directly from the toolbar.
- Add a time scale progress bar to visualize the current time scale.

## [1.9.2] - 2025-09-21

### Scene Switcher Tool

- Fix some UI text capitalization for better readability.
- Add support for Unity 2020.1 and newer for UIElements in the SceneSwitcherTool and StartupSceneLoaderTool.

## [1.9.0] - 2025-09-15

### Rework Timer System

- Remove TimerManager singleton.
- Add TimerRegistry component to manage all timers in a single game object.

## [1.8.6] - 2025-09-14

### New Animator, Transform, and GameObject Extensions

- Add new extensions for animator to query animation state and progress.
- Add coroutine support for waiting on animation events.
- Add new extensions for transform and game object to query for child objects in specific layer masks.

## [1.8.5] - 2025-09-07

### Update Coroutine and Task Extensions

- Add more coroutine extensions.
- Add more task extensions.

## [1.8.4] - 2025-09-07

### Network & DateTime Services

- Convert DateTimeManager to TimeService with static API for easy access.
- Convert NetworkManager to NetworkService with static API for easy access.
- Add more debug logs for both services.

## [1.8.3] - 2025-09-03

### Coroutine Extensions

- Add null element check for collection extensions.
- Add simple Monobehavior coroutine extensions.

## [1.8.2] - 2025-09-02

### Improve Timer System

- New Timer Tracker Window.
- New utilities for countdown and stopwatch.
- New extensions for timer.

## [1.8.1] - 2025-09-02

### Scrolling Background Components (Parallax)

- Add ScrollingImage component for UI Image backgrounds.
- Add ScrollingRawImage component for UI RawImage backgrounds.
- Add ScrollingSprite component for SpriteRenderer backgrounds.
- Add ScrollingMesh component for MeshRenderer backgrounds.
- Fix document formatting.

## [1.8.0] - 2025-09-01

### Update Library Structure

- Move singleton, timer, and color swatch to Core.
- Update other related libraries.

## [1.7.4] - 2025-09-01

### Redesign Component Inspector

- Change designs for Timer, AutoDestroy and SpriteAnimators.
- Switch back to normal UnityEvent add/remove listeners for all components.

## [1.7.3] - 2025-09-01

### Simple sprite & UI animator

- Sprite animator to animate an array of sprites on a SpriteRenderer.
- UI sprite animator to animate an array of sprites on an Image.
- Refactor singleton pattern.

## [1.7.2] - 2025-08-27

### New Time Extensions

- Suppress warnings when getting time not from online sources.
- New clock formats.

## [1.7.1] - 2025-08-26

### Camera & GameObject Extensions

- Add game object extensions to add layer mask.
- Add camera extensions related to culling mask and FOV.

## [1.7.0] - 2025-08-24

### Refactor Singleton Pattern

- Change DateTimeManager, TimerManager, and NetworkManger from Lazy to Persistent Singleton.
- Rewrite documentations.

## [1.6.3] - 2025-08-23

### Color Utilities

- Rename color palette for shorter calls.

## [1.6.2] - 2025-08-23

### Refactor Utilities

- Add TimeSystem convenient class for shorter way to call for DateTimeManager.
- Reorganize util methods into different scripts.

## [1.6.1] - 2025-08-22

### More Components and Extensions

- Add AutoOrbitAround component.
- Adjust some text format extensions.
- Add some new draw gizmo functions.
- Add some new collection extensions.

## [1.6.0] - 2025-08-21

### Exception Handling Improvements

- Add new extensions for color, transform and game object.
- Add exception handling for some extensions.

## [1.5.3] - 2025-08-15

### Minor Improvements

- Add auto destroy component
- Add look at camera component

## [1.5.2] - 2025-08-15

### Additional Vector3 and Transform Extensions

- Add new Vector3 extensions
- Add new Transform extensions
- Fix minor bugs

## [1.5.1] - 2025-08-15

### Enum Utilities and Minor Fixes

- Remove some comments
- Add Enum utilities along with int and string extensions for Enum

## [1.5.0] - 2025-08-15

### Overhaul Time and Vector Extensions

- Add more helper methods to DateTimeManager
- Add more time extensions
- Add more vector extensions (bug fixes too)
- Remove long comments

## [1.4.0] - 2025-08-13

### Fix Object Detections

- New mouse detections for 2D/3D
- New object detections for 2D/3D
- Remove camera related extensions for vector2 and vector3

## [1.3.6] - 2025-08-12

### Minor Fixes

- Testing new Task extension

## [1.3.5] - 2025-08-12

### Breaking Changes

- **Remove UniTask dependency**: Convert all async operations from UniTask to standard C# Task
- NetworkManager and DateTimeManager now use System.Threading.Tasks instead of Cysharp.Threading.Tasks
- Update all async method signatures to use Task<T> instead of UniTask<T>
- Improve cancellation token handling with proper destroyCancellationToken integration
- No external dependencies required - library is now fully self-contained

## [1.3.4] - 2025-08-12

### Fix Import Errors

- Fix dependencies section for UniTask

## [1.3.3] - 2025-08-12

### Timer Component Enhancement

- Refactor timer component
- Add editor debug for timer component
- Add update event for timers

## [1.3.2] - 2025-08-11

### Refactor Timer API

- Manage every timer inside TimerManager
- Add extensions to create timers directly in Monobehavior
- Add a fluent builder pattern to create timers

## [1.3.1] - 2025-08-10

### Refactor Services

- Convert WorldTimeAPI into DateTimeManager

## [1.3.0] - 2025-08-10

### Refactor Services

- Improve and fix bugs for Singleton patterns
- Refactor NetworkManager to use UniTask instead of Coroutine

## [1.1.0] - 2025-06-20

### World API Request

- Request world time from WorldAPI
- Rework NetworkManager

## [1.0.2] - 2025-06-07

### Text Formatter

- Improve text colorization
- Add text formatting extensions (bold, italic, and underline)
- Add my own color palette
- Detect mouse over game object (2D and 3D)

## [1.0.1] - 2025-05-31

### Network Manager and Improvements

- Add a NetworkManager class to frequently check for internet reachability
- Add a YieldTask utility class to yield a task inside a coroutine
- Improve singletons

## [1.0.0] - 2025-05-07

### First Release

- Insert 3 useful Singleton components (scene, persistent, and lazy)
- Insert 2 class based timers and 1 monobehavior timer
- Some useful extensions
- Some useful utilities
