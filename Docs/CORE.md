# TRnK.Toolkit Core

Core foundational systems: singletons, pooling, and color swatches.

## Singletons

```csharp
using TRnK.Singleton;
```

### PersistentSingleton

Survives scene changes. Perfect for game managers.

```csharp
public class GameManager : PersistentSingleton<GameManager>
{
    public int Score { get; set; }
}

// Usage
GameManager.Instance.Score += 100;
```

### Pooling

```csharp
using TRnK.Pooling;
```

A lightweight, deterministic prefab pool. Inherit from `PoolableObject` to make a MonoBehaviour poolable.

```csharp
using TRnK.Pooling;
using UnityEngine;

public sealed class Bullet : PoolableObject
{
    private void OnCollisionEnter(Collision _)
    {
        Release(); // return to pool, or Destroy() if not managed by a pool
    }
}

public sealed class BulletSpawner : MonoBehaviour
{
    [SerializeField] private Bullet _bulletPrefab;

    private Pool<Bullet> _pool;

    private void Awake()
    {
        // auto-creates a [Pool] Bullet root in the hierarchy
        _pool = new Pool<Bullet>(_bulletPrefab);

        // optional: with capacity hint, max size, and explicit root
        _pool = new Pool<Bullet>(_bulletPrefab, capacity: 32, maxSize: 256);
        _pool = new Pool<Bullet>(_bulletPrefab, capacity: 32, maxSize: 256, root: _poolRoot);
    }

    public void Fire(Vector3 position, Quaternion rotation)
    {
        var bullet = _pool.Get(position, rotation);
    }
}
```

#### Spawning

```csharp
var b = _pool.Get();                            // at Vector3.zero
var b = _pool.Get(position, rotation);          // at world position
var b = _pool.Get(position, rotation, parent);  // reparented
var b = _pool.Get(parent);                      // at zero, reparented
```

When `Get(position, rotation, ...)` is called, `OnEnable` on the returned instance always observes the supplied world pose. Velocity setters and other transform-reading initializers in `OnEnable` are safe.

#### Releasing

```csharp
_pool.Release(bullet);  // via pool reference
Release();              // self-release from inside PoolableObject (idempotent)
_pool.Clear();          // destroy all inactive instances and reclaim memory
```

#### Prewarm

```csharp
_pool.Prewarm(20);                              // instantiate 20 inactive instances up-front
```

Prewarm pays the `Instantiate` cost during loading so the first 20 `Get()` calls don't spike a gameplay frame. Prewarmed instances live under an inactive staging GameObject — **`Awake`, `OnEnable`, and `OnDisable` never fire on them** until they're first `Get()`-ed. This makes prewarm safe even for poolables whose `OnDisable` triggers gameplay (e.g. a bullet that spawns shrapnel when released).

`Prewarm(count)` clamps to the pool's `maxSize` and is additive — calling it multiple times fills toward the cap.

#### PoolableObject

Use Unity's own `gameObject.activeSelf` / `activeInHierarchy` to check whether an instance is in use — `PoolableObject` does not expose a public active flag.

```csharp
public sealed class Mine : PoolableObject
{
    private void OnTriggerEnter(Collider _)
    {
        Release(); // safe to call multiple times
    }
}
```

#### ReleaseAfterLifetime

Add the `ReleaseAfterLifetime` component to auto-return a pooled object after a fixed duration. Set `Lifetime` in the Inspector (values <= 0 release on the first Update after activation). Toggle `Use Unscaled Time` to ignore `Time.timeScale` — useful for UI overlays that should expire even while the game is paused. Default is scaled.

#### PoolableParticle

Inherit `PoolableParticle` for particle effects — it automatically calls `Release()` when the particle system stops.

```csharp
public sealed class HitEffect : PoolableParticle { }
```

### Color Swatch

```csharp
using TRnK.ColorPalette;
```

Pre-defined color constants for consistent theming.

```csharp
// Available colors
Color darkGray = Swatch.DG;
Color vibrantRed = Swatch.VR;

// Usage in debug messages
Debug.Log("Success!".Colorize(Swatch.DE));
Debug.LogError("Error!".Colorize(Swatch.VR));

// UI theming
button.color = Swatch.VC;
errorText.color = Swatch.VR;
```

### Log

```csharp
using TRnK.Logger;
```

Simple conditional logger. Methods are compiled only in the Editor, Development builds, or when `TRNK_LOG` is defined.

```csharp
using TRnK.Logger;

// Basic usage
Log.Info("Started");
Log.Warn("Potential issue");
Log.Error("Something went wrong");

// With context to ping an object in the Console
Log.Info("Found object", someGameObject);
```
