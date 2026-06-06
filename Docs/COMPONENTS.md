# TRnK.Toolkit Components

MonoBehaviour components for common game functionality.

```csharp
using TRnK.Components;
```

---

## Sprite Animation

### SpriteAnimator

Frame-based sprite animation for `SpriteRenderer`. Pauses automatically when the renderer is disabled.

```csharp
var animator = GetComponent<SpriteAnimator>();
animator.Play();
animator.PlayOneShot();          // Once mode, then stops
animator.Restart();
animator.Stop();
animator.SetFrameRate(24f);
animator.SetSpeedMultiplier(2f); // Double speed
animator.GoToFrame(5);
animator.SetFrameRate(0f);       // Freezes on current frame

bool playing = animator.IsPlaying;
int frame    = animator.CurrentFrame;
int total    = animator.FrameCount;
```

### UISpriteAnimator

Same as `SpriteAnimator` but targets a UI `Image` component. Pauses automatically when the Image or any assigned `CanvasGroup` has alpha ≤ 0.

```csharp
// Requires Image component on the same GameObject
var animator = GetComponent<UISpriteAnimator>();
animator.Play();
animator.Stop();
// Assign CanvasGroups in the inspector for auto-pause on invisible UI
```

#### Loop Modes

| Mode       | Behaviour                                    |
| ---------- | -------------------------------------------- |
| `Once`     | Plays once and stops on the last frame       |
| `Loop`     | Loops back to the first frame after the last |
| `PingPong` | Plays forward then reverses, repeating       |

#### Frame Events

Per-frame `UnityEvent` callbacks, configured in the **Frame Events** tab of the inspector.

```csharp
// Fires whenever a cycle boundary is reached:
// - Once: fires once when the animation completes
// - Loop: fires at the end of each loop
// - PingPong: fires at each reversal point
animator.OnCycleComplete.AddListener(() => Debug.Log("Cycle done"));
```

---

## Background Scrolling

A family of components that continuously scroll a texture offset to create parallax or looping background effects. All share the same API through `ScrollingBackgroundBase`.

| Component                 | Requires                      | Notes                                                   |
| ------------------------- | ----------------------------- | ------------------------------------------------------- |
| `ScrollingSpriteRenderer` | `SpriteRenderer`              | Instantiates material to avoid shared-material mutation |
| `ScrollingImage`          | UI `Image`                    | Instantiates material                                   |
| `ScrollingRawImage`       | UI `RawImage`                 | Scrolls `uvRect` — no material instantiation needed     |
| `ScrollingMeshRenderer`   | `MeshRenderer` + `MeshFilter` | Auto-disables shadows/probes in `OnValidate`            |

```csharp
var scroller = GetComponent<ScrollingSpriteRenderer>(); // or any variant

// Playback control
scroller.Play();    // resets offset and starts scrolling
scroller.Pause();
scroller.Resume();
scroller.Stop();    // resets offset

// Speed control
scroller.SetSpeed(new Vector2(0.2f, 0f)); // UV units per second
scroller.SetSpeed(x: 0.2f, y: 0f);
scroller.SetSpeedX(0.2f);
scroller.SetSpeedY(0f);

bool playing  = scroller.IsPlaying;
Vector2 speed = scroller.Speed;
```

Configure `Speed`, `Auto Play`, and `Use Unscaled Time` in the inspector.

---

## Utility Components

### AutoDestroy

Destroys the GameObject after a configurable delay. Fires an event just before destruction.

```csharp
// Inspector: set _destroyAfter (seconds, default 5)
// Fires event then destroys — no code required for basic use

var ad = GetComponent<AutoDestroy>();
ad.Bind(() => Debug.Log("Goodbye!"));
```

### LookAtCamera

Makes a GameObject face the camera every frame. Four modes selectable in the inspector:

| Mode                    | Behaviour                               |
| ----------------------- | --------------------------------------- |
| `LookAt`                | Rotates to face the camera's position        |
| `LookAtInverted`        | Rotates to face away from the camera         |
| `CameraForward`         | Matches the camera's forward (billboard-parallel) |
| `CameraForwardInverted` | Matches the camera's backward direction      |

Optionally override the camera via `Use Custom Camera` + `Camera To Look At` in the inspector.

```csharp
// No code required — configure mode in inspector
// Uses Camera.main by default
```

### AutoOrbitAround

Continuously orbits around a target transform using a single unified orbit axis driven by two angles. Draws a gizmo arc and a start-angle tick mark in the editor.

| Inspector Field | Description                                                                                   |
| --------------- | --------------------------------------------------------------------------------------------- |
| Target          | The transform to orbit around                                                                 |
| Distance        | Orbit radius in world units                                                                   |
| Speed           | Degrees per second; negative values reverse direction                                         |
| Start Angle     | Initial angle offset in degrees — use this to evenly space multiple orbiters                  |
| Elevation Angle | Tilts the orbit plane up/down: `0` = flat horizontal ring, `90` = vertical loop               |
| Bearing Angle   | Rotates the tilt direction around world Y; only meaningful when elevation is between 0 and 90 |
| Facing          | How the orbiting object orients itself (see table below)                                      |

#### Facing Modes

| Mode                  | Behaviour                                            |
| --------------------- | ---------------------------------------------------- |
| `FaceTarget`          | Always looks at the target (default)                 |
| `FaceAwayFromTarget`  | Looks directly away from the target                  |
| `FaceHeading`         | Faces the direction of travel (tangent of the orbit) |
| `FaceOppositeHeading` | Faces opposite to the direction of travel            |
| `None`                | Rotation is not modified                             |

```csharp
// No runtime code required — configure everything in the inspector.
// The component manages its own angle state internally.
// Use Start Angle to stagger multiple orbiters around the same target:
//   orbiter1._startAngle = 0f;
//   orbiter2._startAngle = 120f;
//   orbiter3._startAngle = 240f;
```
