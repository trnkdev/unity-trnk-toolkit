#if UNITY_EDITOR
using System.Collections.Generic;

namespace TRnK.Toolkit
{
    internal static class TRnKDocDatabase
    {
        public static List<TRnKDocEntry> GetEntries()
        {
            return new List<TRnKDocEntry>
            {
                new TRnKDocEntry
                {
                    Title = "PersistentSingleton<T>",
                    Namespace = "TRnK.Singleton",
                    Summary = "A MonoBehaviour singleton that survives scene changes via DontDestroyOnLoad.",
                    Description = "Inherit from PersistentSingleton<T> to create a manager that lives for the entire game session. Only one instance is ever allowed; duplicates are automatically destroyed.",
                    Code =
@"public class GameManager : PersistentSingleton<GameManager>
{
    public int Score { get; set; }
}

// Usage
GameManager.Instance.Score += 100;",
                    Tags = new[] { "Singleton", "Lifecycle", "Scene" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "Instance",
                            Summary = "Returns the single active instance, or null if none exists yet.",
                            Code =
@"if (GameManager.Instance != null)
    GameManager.Instance.Score += 100;"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "HasInstance",
                            Summary = "True when a live instance exists. Safe to poll without creating one.",
                            Code =
@"if (PersistentSingleton<GameManager>.HasInstance)
    GameManager.Instance.Score += 10;"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Countdown",
                    Namespace = "TRnK.Timer",
                    Summary = "PlayerLoop-driven countdown timer handle. No coroutines. Supports loops, callbacks, and conditional ticking.",
                    Description = "Countdown is a lightweight readonly struct handle. Create with Countdown.Create(owner, duration), chain builder methods, then call .Start(). Ticks via Unity's PlayerLoop — no MonoBehaviour Update overhead.\n\nKey state: IsAlive, IsRunning, IsPaused, RemainingTime, TotalTime, CurrentLoopIteration.\nKey controls: Start(), Pause(), Resume(), Cancel() (silent — no callbacks), AddTime(), ReduceTime().",
                    Code =
@"var countdown = Countdown.Create(this, 10f)
    .SetLoop(3)
    .OnUpdateWhen(() => !isPaused)
    .OnUpdate(t => Debug.Log($""Remaining: {t:F2}s""))
    .OnComplete(() => Debug.Log(""Period elapsed""));

countdown.Start();

// Control
countdown.Pause();
countdown.Resume();
countdown.Cancel();      // silent teardown — no callbacks
countdown.AddTime(5f);
countdown.ReduceTime(2f);

bool  alive = countdown.IsAlive;
float left  = countdown.RemainingTime;",
                    Tags = new[] { "Timer", "PlayerLoop", "Countdown" },
                    Category = DocCategory.TRnKTimer,
                    Members = new[]
                    {
                        // Properties
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsAlive",
                            Summary = "True while the timer is registered and ticking (not yet stopped or cancelled).",
                            Code =
@"var cd = Countdown.Create(this, 5f);
cd.Start();
if (cd.IsAlive)
    Debug.Log(""Still running"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsRunning",
                            Summary = "True when alive and not paused.",
                            Code =
@"if (cd.IsRunning)
    UpdateUI(cd.RemainingTime);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsPaused",
                            Summary = "True when the timer is alive but paused.",
                            Code =
@"pauseIcon.SetActive(cd.IsPaused);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "RemainingTime",
                            Summary = "Seconds left in the current loop iteration.",
                            Code =
@"timerLabel.text = cd.RemainingTime.ToClock();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "TotalTime",
                            Summary = "The total duration this countdown was created with.",
                            Code =
@"float pct = 1f - (cd.RemainingTime / cd.TotalTime);
progressBar.fillAmount = pct;"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "CurrentLoopIteration",
                            Summary = "Number of iterations that have completed and fired OnComplete (1 after the first iteration ends, N after the last for SetLoop(N)).",
                            Code =
@"Debug.Log($""Iteration {cd.CurrentLoopIteration} of {loopCount}"");"
                        },
                        // Methods
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Create(MonoBehaviour owner, float duration)",
                            Summary = "Creates a new countdown attached to a MonoBehaviour owner.",
                            Code =
@"var cd = Countdown.Create(this, 10f)
    .OnComplete(() => OnTimerDone());
cd.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Start()",
                            Summary = "Starts the countdown from TotalTime. No-op if already running.",
                            Code =
@"var cd = Countdown.Create(this, 5f).OnComplete(OnDone);
cd.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Cancel()",
                            Summary = "Cancels the timer silently — no callbacks fire.",
                            Code =
@"cd.Cancel(); // silent teardown — no callbacks"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Pause()",
                            Summary = "Freezes ticking. RemainingTime holds its value.",
                            Code =
@"cd.Pause();
pausePanel.SetActive(true);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Resume()",
                            Summary = "Resumes ticking from where it was paused.",
                            Code =
@"cd.Resume();
pausePanel.SetActive(false);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "AddTime(float seconds)",
                            Summary = "Adds seconds to the remaining time (e.g. bonus time power-up).",
                            Code =
@"// Power-up: +5s bonus
cd.AddTime(5f);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "ReduceTime(float seconds)",
                            Summary = "Subtracts seconds from the remaining time. Clamps to zero on overshoot — no carry-over into subsequent loop iterations. Triggers OnComplete when reaching zero. NaN and negative values are ignored.",
                            Code =
@"// Penalty: -3s
cd.ReduceTime(3f);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetLoop(int count)",
                            Summary = "Sets how many times the countdown fires OnComplete. Use -1 for infinite, 0 (default) for one-shot.",
                            Code =
@"// SpawnWave 3 times, then GameOver after the 3rd
var waveIndex = 0;
var cd = Countdown.Create(this, 2f)
    .SetLoop(3)
    .OnComplete(() => {
        SpawnWave();
        if (++waveIndex == 3) GameOver();
    });
cd.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetUnscaledTime(bool value)",
                            Summary = "When true the timer ticks using unscaled time (ignores Time.timeScale).",
                            Code =
@"// Pause menu timer — unaffected by slow-motion
var cd = Countdown.Create(this, 30f)
    .SetUnscaledTime(true)
    .OnComplete(OnTimeout);
cd.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "AsTimerToken()",
                            Summary = "Returns a TimerToken that can only cancel this countdown. Use this to pass a timer to code that should not have full control (Pause/Resume/AddTime etc.).",
                            Code =
@"// Give a cancel-only handle to a UI element
TimerToken token = cd.AsTimerToken();
cancelButton.onClick.AddListener(() => token.Cancel());"
                        },
                        // Callbacks
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnUpdate(Action<float>)",
                            Summary = "Fires every tick with the current remaining time in seconds.",
                            Code =
@"Countdown.Create(this, 10f)
    .OnUpdate(t => timerLabel.text = t.ToClock())
    .Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnUpdateWhen(Func<bool>)",
                            Summary = "Conditionally gate ticking. The timer only ticks while the predicate returns true.",
                            Code =
@"Countdown.Create(this, 30f)
    .OnUpdateWhen(() => !isPaused && isInRound)
    .OnComplete(OnRoundEnd)
    .Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnComplete(Action)",
                            Summary = "Fires when the period elapses — once at zero for one-shot, once per iteration including the final one for SetLoop(N), every iteration forever for SetLoop(-1). Not fired on Cancel() or owner-MonoBehaviour destruction.",
                            Code =
@"// Infinite spawner — fires every 5 seconds until cancelled
Countdown.Create(this, 5f)
    .SetLoop(-1)
    .OnComplete(() => SpawnEnemy())
    .Start();

// One-shot — fires once at zero
Countdown.Create(this, 10f)
    .OnComplete(() => ShowTimeUpScreen())
    .Start();"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Stopwatch",
                    Namespace = "TRnK.Timer",
                    Summary = "Counts elapsed time upward. Auto-fires OnComplete when a stop predicate becomes true, or stays alive until Cancel() is called.",
                    Description = "Mirrors Countdown's builder API but measures elapsed time rather than remaining time. Use .SetStopWhen(predicate) to auto-fire OnComplete on a condition.",
                    Code =
@"var stopwatch = Stopwatch.Create(this)
    .SetStopWhen(() => gameIsOver)
    .OnUpdateWhen(() => isActiveState)
    .OnUpdate(elapsed => Debug.Log($""Elapsed: {elapsed:F2}s""))
    .OnComplete(() => Debug.Log(""Stop predicate triggered""));

stopwatch.Start();

float elapsed = stopwatch.ElapsedTime;",
                    Tags = new[] { "Timer", "Stopwatch", "Elapsed" },
                    Category = DocCategory.TRnKTimer,
                    Members = new[]
                    {
                        // Properties
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "ElapsedTime",
                            Summary = "Seconds elapsed since Start() was called.",
                            Code =
@"timerLabel.text = stopwatch.ElapsedTime.ToClock();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsAlive",
                            Summary = "True while the stopwatch is registered and has not been stopped or cancelled.",
                            Code =
@"if (stopwatch.IsAlive)
    UpdateElapsedDisplay();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsRunning",
                            Summary = "True when alive and not paused.",
                            Code =
@"stopButton.interactable = stopwatch.IsRunning;"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsPaused",
                            Summary = "True when the stopwatch is alive but paused.",
                            Code =
@"pauseIcon.SetActive(stopwatch.IsPaused);"
                        },
                        // Methods
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Create(MonoBehaviour owner)",
                            Summary = "Creates a new stopwatch attached to a MonoBehaviour owner.",
                            Code =
@"var sw = Stopwatch.Create(this)
    .OnUpdate(t => label.text = t.ToClock())
    .SetStopWhen(() => roundEnded)
    .OnComplete(OnRoundEnd);
sw.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Start()",
                            Summary = "Starts the stopwatch. No-op if already running.",
                            Code =
@"stopwatch.Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Cancel()",
                            Summary = "Cancels the stopwatch silently — no callbacks fire.",
                            Code =
@"stopwatch.Cancel();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Pause() / Resume()",
                            Summary = "Freezes or resumes elapsed time accumulation.",
                            Code =
@"stopwatch.Pause();
// ... show pause menu ...
stopwatch.Resume();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetStopWhen(Func<bool>)",
                            Summary = "Auto-fires OnComplete and stops the stopwatch when the predicate returns true.",
                            Code =
@"Stopwatch.Create(this)
    .SetStopWhen(() => playerDead)
    .OnComplete(ShowDeathScreen)
    .Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetUnscaledTime(bool value)",
                            Summary = "When true the stopwatch ticks using unscaled time (ignores Time.timeScale).",
                            Code =
@"Stopwatch.Create(this)
    .SetUnscaledTime(true)
    .OnUpdate(t => realTimeLabel.text = t.ToClock())
    .Start();"
                        },
                        // Callbacks
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnUpdate(Action<float>)",
                            Summary = "Fires every tick with the current elapsed time in seconds.",
                            Code =
@"Stopwatch.Create(this)
    .OnUpdate(t => timerLabel.text = t.ToClock())
    .Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnUpdateWhen(Func<bool>)",
                            Summary = "Conditionally gate ticking. Stopwatch only ticks while the predicate returns true.",
                            Code =
@"Stopwatch.Create(this)
    .OnUpdateWhen(() => roundActive && !isPaused)
    .SetStopWhen(() => roundEnded)
    .OnComplete(OnRoundEnd)
    .Start();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnComplete(Action)",
                            Summary = "Fires when SetStopWhen predicate becomes true. Never fires without a predicate. Not fired on Cancel() or owner-MonoBehaviour destruction.",
                            Code =
@"Stopwatch.Create(this)
    .SetStopWhen(() => reachedGoal)
    .OnComplete(() => SaveBestTime(stopwatch.ElapsedTime))
    .Start();"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Delay / Repeat",
                    Namespace = "TRnK.Timer",
                    Summary = "MonoBehaviour extension helpers for fire-once or repeating timer calls without coroutines.",
                    Description = "Delay and Repeat are extension methods on MonoBehaviour. They return a TimerToken which can be cancelled silently before firing. Both support an optional useUnscaledTime parameter.",
                    Code =
@"// One-shot after delay
TimerToken token    = this.Delay(2f, () => Debug.Log(""Fired!""));
TimerToken unscaled = this.Delay(2f, () => Debug.Log(""Unscaled""),
    useUnscaledTime: true);
token.Cancel();   // cancel before it fires — no callbacks

// Repeating tick
TimerToken ticker = this.Repeat(1f, () => Debug.Log(""Tick""));
ticker.Cancel();  // stop the loop",
                    Tags = new[] { "Timer", "Invoke", "Repeat" },
                    Category = DocCategory.TRnKTimer,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Delay(float delay, Action action)",
                            Summary = "Fires action once after delay seconds. Returns a token to cancel before firing. If delay \u2264 0, the action fires immediately and the returned token is default (already expired).",
                            Code =
@"TimerToken token = this.Delay(3f, () => SpawnBoss());
// Cancel if the round ends early
void OnRoundEnd() => token.Cancel();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Delay(float delay, Action action, bool useUnscaledTime)",
                            Summary = "Same as Delay but ticks with unscaled time when useUnscaledTime is true.",
                            Code =
@"// Fires after 2 real-world seconds regardless of Time.timeScale
TimerToken t = this.Delay(2f, ShowTip, useUnscaledTime: true);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Repeat(float interval, Action action)",
                            Summary = "Fires action repeatedly every interval seconds. Returns a token to stop the loop. Throws ArgumentException if interval \u2264 0.",
                            Code =
@"TimerToken ticker = this.Repeat(1f, () => IncrementScore(1));
// Stop when player dies
void OnPlayerDied() => ticker.Cancel();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Repeat(float interval, Action action, bool useUnscaledTime)",
                            Summary = "Same as Repeat but ticks with unscaled time when useUnscaledTime is true.",
                            Code =
@"// UI heartbeat — unaffected by slow-motion
TimerToken hb = this.Repeat(0.5f, PulseHeartIcon, useUnscaledTime: true);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "TimerToken.IsAlive",
                            Summary = "True while the associated timer is still active (not yet fired or cancelled).",
                            Code =
@"if (spawnToken.IsAlive)
    cancelButton.interactable = true;"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "TimerToken.Cancel()",
                            Summary = "Cancels the timer silently. No callback is fired.",
                            Code =
@"spawnToken.Cancel(); // stops fire-once or repeating loop"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Pool<T>",
                    Namespace = "TRnK.Pooling",
                    Summary = "Deterministic prefab pool. Inherit PoolableObject and call Get() / Release() instead of Instantiate / Destroy.",
                    Description = "Pool<T> manages a stack of inactive instances under an auto-created root transform. New and prewarmed instances live in an inactive staging GameObject until first Get() — so Awake, OnEnable, and OnDisable never fire on dormant pool items. Prewarm(N) pays Instantiate cost up-front to avoid first-spawn spikes. Get() applies the requested pose before activation so OnEnable observes the correct transform. Use Clear() for explicit cleanup; scene unload handles the rest.",
                    Code =
@"// Setup — auto-creates a [Pool] Bullet root
_pool = new Pool<Bullet>(_bulletPrefab);
_pool = new Pool<Bullet>(_bulletPrefab, capacity: 32, maxSize: 256);
_pool = new Pool<Bullet>(_bulletPrefab, capacity: 32, maxSize: 256, root: _poolRoot);

// Prewarm at boot — no lifecycle callbacks fire on prewarmed instances
_pool.Prewarm(32);

// Runtime — OnEnable observes the supplied pose
Bullet b = _pool.Get(position, rotation);
Bullet b = _pool.Get(position, rotation, parent);
Bullet b = _pool.Get();          // at Vector3.zero
Bullet b = _pool.Get(parent);    // at zero, reparented
_pool.Release(b);
_pool.Clear();",
                    Tags = new[] { "Pool", "Get", "Release", "Prewarm", "Performance" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Get()",
                            Summary = "Gets an instance from the pool at Vector3.zero with no rotation.",
                            Code =
@"var bullet = _pool.Get();
bullet.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Get(Vector3 position, Quaternion rotation)",
                            Summary = "Gets an instance placed at the given world position and rotation.",
                            Code =
@"Bullet b = _pool.Get(firePoint.position, firePoint.rotation);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Get(Vector3 position, Quaternion rotation, Transform parent)",
                            Summary = "Gets an instance placed at position/rotation and reparented under parent.",
                            Code =
@"var effect = _pool.Get(hit.point, Quaternion.identity, hitParent);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Get(Transform parent)",
                            Summary = "Gets an instance at Vector3.zero reparented under parent.",
                            Code =
@"var icon = _pool.Get(canvasRoot);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Prewarm(int count)",
                            Summary = "Instantiates count instances up-front into inactive staging. Clamps to maxSize. Awake, OnEnable, and OnDisable do NOT fire on prewarmed instances until first Get().",
                            Code =
@"private void Awake()
{
    _pool = new Pool<Bullet>(_bulletPrefab, capacity: 32, maxSize: 256);
    _pool.Prewarm(32); // amortize Instantiate cost away from gameplay frames
}"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Release(T instance)",
                            Summary = "Returns the instance to the pool. Destroys it if the pool is at capacity.",
                            Code =
@"_pool.Release(bullet);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Clear()",
                            Summary = "Destroys all inactive instances and reclaims the stack's backing memory.",
                            Code =
@"void OnLevelUnload() => _pool.Clear();"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "PoolableObject",
                    Namespace = "TRnK.Pooling",
                    Summary = "Abstract base for poolable MonoBehaviours. Provides self-release and active-state tracking.",
                    Description = "Inherit from PoolableObject to make a MonoBehaviour usable with Pool<T>. Call Release() to return the instance to its pool, or Destroy it if not managed by a pool. Release() is idempotent — safe to call multiple times.",
                    Code =
@"public sealed class EnemyProjectile : PoolableObject
{
    private void OnCollisionEnter(Collision _)
    {
        Release(); // returns to pool, or Destroys if unmanaged
    }
}",
                    Tags = new[] { "Pool", "Self-release" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Release()",
                            Summary = "Returns this object to its pool. Falls back to Destroy() if not managed by a pool. Idempotent.",
                            Code =
@"private void OnBecameInvisible()
{
    Release();
}"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "PoolableParticle",
                    Namespace = "TRnK.Pooling",
                    Summary = "Inherit to make a ParticleSystem poolable. Auto-releases back to pool when the particle stops.",
                    Description = "Requires a ParticleSystem on the same GameObject. The stop action is forced to Callback and playOnAwake is disabled automatically. Override OnParticleSystemStopped() for custom teardown logic. Call Play() to start playback.",
                    Code =
@"public sealed class HitEffect : PoolableParticle { }

// Usage — call Play() after Get() from the pool
var effect = _hitPool.Get(hit.point, Quaternion.identity);
effect.Play();
// Auto-releases when the particle system stops",
                    Tags = new[] { "Pool", "Particle", "VFX" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Play()",
                            Summary = "Starts playback if the particle system is not already playing.",
                            Code =
@"var effect = _explosionPool.Get(transform.position, Quaternion.identity);
effect.Play();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "ReleaseAfterLifetime",
                    Namespace = "TRnK.Pooling",
                    Summary = "Add to any poolable GameObject to auto-release it after a fixed duration. Configurable in the inspector.",
                    Description = "Requires a PoolableObject on the same GameObject. Lifetime is reset on each OnEnable (i.e. each pool Get()). Supports scaled and unscaled time. Values <= 0 release on the next Update after activation.",
                    Code =
@"// Inspector-only setup is the common case.
// Override at runtime when needed:
var ral = bullet.GetComponent<ReleaseAfterLifetime>();
ral.Lifetime = 2.5f;
ral.UseUnscaledTime = false;",
                    Tags = new[] { "Pool", "Lifetime", "Auto-release" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "Lifetime",
                            Summary = "Duration in seconds before the object is released. Clamped to >= 0. Reset on each OnEnable.",
                            Code = @"ral.Lifetime = 3f;" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "UseUnscaledTime",
                            Summary = "When true, the lifetime ticks with unscaled time — unaffected by Time.timeScale.",
                            Code = @"ral.UseUnscaledTime = true; // survives pause" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Swatch (Color Palette)",
                    Namespace = "TRnK.ColorPalette",
                    Summary = "Pre-defined color constants for consistent in-editor and in-game theming.",
                    Description = "Use Swatch constants for consistent Debug log coloring and runtime UI theming. Common swatches: VR (Vibrant Red), VC (Vibrant Cyan), DE (Debug Emphasis), DG (Dark Gray).",
                    Code =
@"Debug.Log(""Success!"".Colorize(Swatch.DE));
Debug.LogError(""Error!"".Colorize(Swatch.VR));

button.color    = Swatch.VC;
errorText.color = Swatch.VR;
Color dark      = Swatch.DG;",
                    Tags = new[] { "Color", "Debug", "UI" },
                    Category = DocCategory.Core
                },
                new TRnKDocEntry
                {
                    Title = "Log",
                    Namespace = "TRnK.Logger",
                    Summary = "Conditional logger — Info, Warn, and Assert are stripped in release builds. Error and Exception always fire so crash reporters receive them.",
                    Description = "Log.Info and Log.Warn are compiled only in the Editor, Development builds, or when TRNK_LOG is defined. Log.Error and Log.Exception always fire in all builds so crash reporters (e.g. Firebase Crashlytics, Sentry) capture them via Application.logMessageReceived. Supports an optional context object to ping it in the Console.",
                    Code =
@"using TRnK.Logger;

Log.Info(""System started"");
Log.Warn(""Low memory"");
Log.Error(""Something broke"");

// With context — click the log entry to ping the object
Log.Info(""Found target"", enemyGameObject);",
                    Tags = new[] { "Logging", "Debug", "Conditional" },
                    Category = DocCategory.Core,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Info(string message)",
                            Summary = "Logs an informational message (white). Stripped in non-development builds.",
                            Code =
@"Log.Info($""Player spawned at {transform.position}"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Info(string message, Object context)",
                            Summary = "Logs with a context object — click the Console entry to ping it in the Hierarchy.",
                            Code =
@"Log.Info(""Target acquired"", enemy.gameObject);"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Warn(string message)",
                            Summary = "Logs a warning (yellow). Stripped in non-development builds.",
                            Code =
@"Log.Warn(""Pool capacity exceeded — consider increasing maxSize"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Error(string message)",
                            Summary = "Logs an error (red). Always fires in all builds so crash reporters can capture it.",
                            Code =
@"Log.Error($""Failed to load asset: {assetName}"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Assert(bool condition, string message)",
                            Summary = "Logs an error and breaks if condition is false. No-op in release builds.",
                            Code =
@"Log.Assert(_pool != null, ""Bullet pool must be assigned"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Exception(Exception exception)",
                            Summary = "Logs an exception with full stack trace. Always fires in all builds so crash reporters can capture it.",
                            Code =
@"try { RiskyOperation(); }
catch (Exception e) { Log.Exception(e); }"
                        },
                    }
                },

                // ── Components ──────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "SpriteAnimator",
                    Namespace = "TRnK.Components",
                    Summary = "Frame-based sprite animation for SpriteRenderer. Auto-pauses when renderer is disabled.",
                    Description = "Loop modes: Once (stops on last frame), Loop (wraps back), PingPong (reverses). Add per-frame UnityEvent callbacks via the Frame Events inspector tab. OnCycleComplete fires at each cycle boundary.",
                    Code =
@"var anim = GetComponent<SpriteAnimator>();
anim.Play();                                       // Once mode (default)
anim.Play(SpriteAnimatorLoopMode.Loop);            // explicit loop
anim.Restart();
anim.Stop();
anim.SetFrameRate(24f);
anim.SetSpeedMultiplier(2f);
anim.GoToFrame(5);
anim.SetFrameRate(0f);    // freeze on current frame

bool playing = anim.IsPlaying;
int  frame   = anim.CurrentFrame;
int  total   = anim.FrameCount;

anim.OnCycleComplete.AddListener(() => Debug.Log(""Cycle done!""));",
                    Tags = new[] { "Animation", "Sprite", "SpriteRenderer" },
                    Category = DocCategory.Components,
                    Members = new[]
                    {
                        // Properties
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "IsPlaying",
                            Summary = "True while the animation is actively playing (not stopped or paused).",
                            Code =
@"if (anim.IsPlaying)
    anim.Stop();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "CurrentFrame",
                            Summary = "Zero-based index of the currently displayed frame.",
                            Code =
@"Debug.Log($""Frame {anim.CurrentFrame} / {anim.FrameCount - 1}"");"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Property,
                            Signature = "FrameCount",
                            Summary = "Total number of frames in the assigned sprite array.",
                            Code =
@"progressBar.fillAmount = (float)anim.CurrentFrame / anim.FrameCount;"
                        },
                        // Methods
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Play(SpriteAnimatorLoopMode loopMode = Once)",
                            Summary = "Starts playback. Sets and applies the given loop mode immediately (like Rigidbody.isKinematic). Defaults to Once.",
                            Code =
@"anim.Play();                                    // Once — stops at last frame
anim.Play(SpriteAnimatorLoopMode.Loop);         // continuous loop
anim.Play(SpriteAnimatorLoopMode.PingPong);     // bounces back and forth"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Stop()",
                            Summary = "Stops playback and resets to the first frame.",
                            Code =
@"anim.Stop();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Restart()",
                            Summary = "Stops and immediately starts from frame 0.",
                            Code =
@"anim.Restart();"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetFrameRate(float fps)",
                            Summary = "Changes the playback speed in frames per second. Pass 0 to freeze.",
                            Code =
@"anim.SetFrameRate(12f);   // slow cinematic
anim.SetFrameRate(0f);    // freeze on current frame"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "SetSpeedMultiplier(float multiplier)",
                            Summary = "Scales the frame rate by a multiplier. 1 = normal speed, 2 = double, 0 = freeze.",
                            Code =
@"anim.SetSpeedMultiplier(2f);  // double speed
anim.SetSpeedMultiplier(0.5f); // half speed
anim.SetSpeedMultiplier(0f);   // freeze"
                        },
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "GoToFrame(int index)",
                            Summary = "Jumps to a specific frame index without changing play state.",
                            Code =
@"anim.GoToFrame(0); // reset to first frame"
                        },
                        // Callbacks
                        new DocMember
                        {
                            Kind = DocMemberKind.Callback,
                            Signature = "OnCycleComplete",
                            Summary = "UnityEvent fired at the end of each animation cycle (loop wrap or ping-pong reversal).",
                            Code =
@"anim.OnCycleComplete.AddListener(() =>
{
    Debug.Log(""Cycle finished!"");
    SpawnTrailEffect();
});"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "UISpriteAnimator",
                    Namespace = "TRnK.Components",
                    Summary = "Same as SpriteAnimator but targets a UI Image. Auto-pauses when Image or CanvasGroups have alpha ≤ 0.",
                    Description = "Assign CanvasGroups in the inspector — animation auto-pauses when any group's alpha is zero. Shares the same play/stop API as SpriteAnimator.",
                    Code =
@"// Requires Image component on the same GameObject
var anim = GetComponent<UISpriteAnimator>();
anim.Play();
anim.Stop();
// Assign CanvasGroups in inspector for auto-pause on invisible UI",
                    Tags = new[] { "Animation", "UI", "Image" },
                    Category = DocCategory.Components,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "IsPlaying",
                            Summary = "True while the animation is actively playing.",
                            Code = @"if (anim.IsPlaying) anim.Stop();" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "CurrentFrame / FrameCount",
                            Summary = "Zero-based current frame index and total number of frames.",
                            Code = @"progressBar.fillAmount = (float)anim.CurrentFrame / anim.FrameCount;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Play() / Stop() / Restart()",
                            Summary = "Same playback control API as SpriteAnimator.",
                            Code =
@"anim.Play();
anim.Play(SpriteAnimatorLoopMode.Loop);
anim.Stop();
anim.Restart();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetFrameRate(float fps) / SetSpeedMultiplier(float) / GoToFrame(int index)",
                            Summary = "Change playback speed, scale it by a multiplier, or jump to a specific frame.",
                            Code =
@"anim.SetFrameRate(12f);
anim.SetSpeedMultiplier(2f);
anim.GoToFrame(0);" },
                        new DocMember { Kind = DocMemberKind.Callback, Signature = "OnCycleComplete",
                            Summary = "UnityEvent fired at the end of each animation cycle.",
                            Code =
@"anim.OnCycleComplete.AddListener(() => PlaySoundEffect(cycleClip));" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "ScrollingBackground",
                    Namespace = "TRnK.Components",
                    Summary = "Continuously scrolls a texture offset for parallax/looping background effects. Four renderer variants.",
                    Description = "Variants: ScrollingSpriteRenderer, ScrollingImage, ScrollingRawImage (scrolls uvRect, no material copy), ScrollingMeshRenderer. All share the same API. Configure Speed, Auto Play, and Use Unscaled Time in the inspector.",
                    Code =
@"var scroller = GetComponent<ScrollingSpriteRenderer>();
scroller.Play();     // reset offset and start
scroller.Pause();
scroller.Resume();
scroller.Stop();     // reset offset

scroller.SetSpeed(new Vector2(0.2f, 0f));
scroller.SetSpeedX(0.2f);
scroller.SetSpeedY(0f);

bool    playing = scroller.IsPlaying;
Vector2 speed   = scroller.Speed;",
                    Tags = new[] { "Background", "Scroll", "Parallax" },
                    Category = DocCategory.Components,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "IsPlaying",
                            Summary = "True while the scroller is actively scrolling.",
                            Code = @"pauseButton.interactable = scroller.IsPlaying;" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "Speed",
                            Summary = "Current scroll speed as a Vector2 (x = horizontal, y = vertical).",
                            Code = @"scroller.Speed = new Vector2(0.3f, 0f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Play()",
                            Summary = "Resets the offset to zero and starts scrolling.",
                            Code = @"scroller.Play();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Pause() / Resume()",
                            Summary = "Pauses or resumes scrolling without resetting the offset.",
                            Code =
@"scroller.Pause();
// ... player pauses game ...
scroller.Resume();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Stop()",
                            Summary = "Stops scrolling and resets the texture offset to zero.",
                            Code = @"scroller.Stop();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetSpeed(Vector2 speed) / SetSpeedX(float x) / SetSpeedY(float y)",
                            Summary = "Changes the scroll speed at runtime.",
                            Code =
@"scroller.SetSpeed(new Vector2(0.2f, 0f));
scroller.SetSpeedX(0.5f);  // only change X
scroller.SetSpeedY(0f);    // stop vertical" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "AutoDestroy",
                    Namespace = "TRnK.Components",
                    Summary = "Destroys the GameObject after a configurable delay. Optionally fires a callback just before destruction.",
                    Description = "Set _destroyAfter (seconds, default 5) in the inspector. Wire the UnityEvent in the inspector, or call Bind() in code to subscribe a runtime callback.",
                    Code =
@"// Wire the event in the inspector, or bind at runtime:
var ad = GetComponent<AutoDestroy>();
ad.Bind(() => Debug.Log(""Goodbye!""));",
                    Tags = new[] { "Lifecycle", "Destroy" },
                    Category = DocCategory.Components,
                    Members = new[]
                    {
                        new DocMember
                        {
                            Kind = DocMemberKind.Method,
                            Signature = "Bind(UnityAction action)",
                            Summary = "Adds a runtime callback to be invoked just before the GameObject is destroyed.",
                            Code =
@"var ad = GetComponent<AutoDestroy>();
ad.Bind(() =>
{
    particleSystem.Stop();
    AudioManager.Instance.PlayOneShot(destroyClip);
});"
                        },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "LookAtCamera",
                    Namespace = "TRnK.Components",
                    Summary = "Makes a GameObject face the camera every frame. Four facing modes selectable in the inspector.",
                    Description = "Modes: LookAt (face camera position), LookAtInverted (face away), CameraForward (match camera forward, billboard-parallel), CameraForwardInverted (match camera backward). Uses Camera.main by default. Override via inspector.",
                    Code =
@"// Pure inspector setup — no code required.
// Use Custom Camera + Camera To Look At to override Camera.main.",
                    Tags = new[] { "Billboard", "Camera" },
                    Category = DocCategory.Components
                },
                new TRnKDocEntry
                {
                    Title = "AutoOrbitAround",
                    Namespace = "TRnK.Components",
                    Summary = "Continuously orbits around a target transform. Configurable speed, elevation, bearing, and facing mode.",
                    Description = "Inspector fields: Target, Distance, Speed (deg/s; negative = reverse), StartAngle (stagger multiple orbiters), ElevationAngle (0 = flat ring, 90 = vertical loop), BearingAngle, Facing mode. Draws a gizmo arc in the Scene view.",
                    Code =
@"// Pure inspector setup. To evenly space three orbiters:
// orbiter1._startAngle = 0f;
// orbiter2._startAngle = 120f;
// orbiter3._startAngle = 240f;",
                    Tags = new[] { "Orbit", "Transform", "Gizmo" },
                    Category = DocCategory.Components
                },

                // ── Extensions ──────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "GameObjectExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "GetOrAdd<T>, layer management, child queries.",
                    Description = "GetOrAdd<T> avoids repeated GetComponent + AddComponent boilerplate. ClearChildTransforms() destroys all children. GetChildrenInLayer/Recursive filter by LayerMask.",
                    Code =
@"AudioSource audio = gameObject.GetOrAdd<AudioSource>();

bool inLayer = gameObject.IsInLayer(LayerMask.GetMask(""Enemy""));
gameObject.SetLayer(""Player"");
gameObject.SetLayer(8);

gameObject.ClearChildTransforms();

GameObject[] enemies =
    gameObject.GetChildrenInLayerRecursive(LayerMask.GetMask(""Enemy""));",
                    Tags = new[] { "GameObject", "Component", "Layer" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetOrAdd<T>()",
                            Summary = "Returns the component if it exists, otherwise adds and returns a new one.",
                            Code = @"var rb = gameObject.GetOrAdd<Rigidbody2D>();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInLayer(LayerMask mask)",
                            Summary = "Returns true if the GameObject's layer is included in the LayerMask.",
                            Code =
@"if (gameObject.IsInLayer(LayerMask.GetMask(""Enemy"")))
    TakeDamage();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetLayer(string name) / SetLayer(int index)",
                            Summary = "Sets the layer by name string or integer index.",
                            Code =
@"gameObject.SetLayer(""Ignore Raycast"");
gameObject.SetLayer(2);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ClearChildTransforms()",
                            Summary = "Destroys all immediate child GameObjects.",
                            Code = @"contentContainer.ClearChildTransforms(); // clear spawned list items" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetChildrenInLayer(LayerMask mask, bool includeInactive = false)",
                            Summary = "Returns immediate children whose layer matches the given LayerMask. Pass includeInactive: true to include disabled objects.",
                            Code = @"GameObject[] walls = room.GetChildrenInLayer(LayerMask.GetMask(""Wall""));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetChildrenInLayerRecursive(LayerMask mask, bool includeInactive = false)",
                            Summary = "Returns all descendants (any depth) whose layer matches the LayerMask. Pass includeInactive: true to include disabled objects.",
                            Code = @"GameObject[] all = root.GetChildrenInLayerRecursive(LayerMask.GetMask(""Enemy""), includeInactive: true);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TransformExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Clear children, LookAt2D, distance/direction helpers, transform resets.",
                    Description = "Clear() destroys all children. GetChildren() collects them. LookAt2D supports an optional angle offset. DistanceTo/DirectionTo/InRangeOf are readable alternatives to Vector3.Distance. ResetTransform / ResetLocalTransform zero world or local TRS.",
                    Code =
@"transform.Clear();
Transform[] kids = transform.GetChildren(includeInactive: false);

transform.LookAt2D(targetPos);
transform.LookAt2D(targetTr, angleOffset: 90f);

float   dist    = transform.DistanceTo(other);
Vector3 dir     = transform.DirectionTo(other);
bool    inRange = transform.InRangeOf(other, 5f);

transform.ResetTransform();       // world TRS -> identity
transform.ResetLocalTransform();  // local TRS -> identity",
                    Tags = new[] { "Transform", "Distance", "Children" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Clear()",
                            Summary = "Destroys all immediate child GameObjects.",
                            Code = @"poolContainer.Clear(); // destroy all spawned children" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetChildren(bool includeInactive)",
                            Summary = "Returns all immediate children as a Transform array. Pass true to include inactive ones.",
                            Code =
@"Transform[] active = transform.GetChildren();
Transform[] all    = transform.GetChildren(includeInactive: true);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "LookAt2D(Transform target)",
                            Summary = "Rotates this transform to face a 2D position or transform. Optional angle offset.",
                            Code =
@"transform.LookAt2D(enemy.position);
transform.LookAt2D(enemy, angleOffset: 90f); // offset for sprites facing right" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DistanceTo(Transform other)",
                            Summary = "Returns the world-space distance to another Transform or Vector3.",
                            Code =
@"float dist = transform.DistanceTo(target);
if (dist < attackRange) Attack();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DirectionTo(Transform other)",
                            Summary = "Returns the normalized direction vector toward another Transform or Vector3.",
                            Code = @"Vector3 dir = transform.DirectionTo(target);
rb.velocity = dir * speed;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "InRangeOf(Transform other, float range)",
                            Summary = "Returns true when the distance to other is less than or equal to range.",
                            Code =
@"if (transform.InRangeOf(player.transform, detectionRadius))
    ChasePlayer();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ResetTransform()",
                            Summary = "Resets world position to zero, rotation to identity, and scale to one.",
                            Code = @"spawnPoint.ResetTransform();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ResetLocalTransform()",
                            Summary = "Resets local position, rotation, and scale to their identity values.",
                            Code = @"uiPanel.transform.ResetLocalTransform();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "ColorExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Fluent Color modifications: alpha, channels, multiply, add, invert, grayscale, luminance, hex.",
                    Description = "All channel methods return a new Color — the original is unchanged. ToHex produces #RRGGBBAA. ToColor parses hex strings.",
                    Code =
@"Color faded   = color.WithAlpha(0.5f);
Color redded  = color.WithRed(1f);
Color bright  = color.MultiplyRGB(1.5f);
Color lighter = color.AddRGB(0.1f);
Color inv     = color.Invert();
Color grey    = color.ToGrayscale();
float lum     = color.GetLuminance();

string hex   = color.ToHex();           // ""#RRGGBBAA""
Color parsed = ""#FF0000FF"".ToColor();",
                    Tags = new[] { "Color", "Fluent", "Hex" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithAlpha(float a)",
                            Summary = "Returns a copy of the color with the alpha channel set to a.",
                            Code = @"image.color = baseColor.WithAlpha(0.5f); // 50% transparent" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithRed(float r) / WithGreen(float g) / WithBlue(float b)",
                            Summary = "Returns a copy with a single channel replaced. Original unchanged.",
                            Code = @"Color warning = color.WithRed(1f).WithBlue(0f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "MultiplyRGB(float factor)",
                            Summary = "Multiplies all RGB channels by factor. Useful for brightening or darkening.",
                            Code = @"Color highlight = baseColor.MultiplyRGB(1.4f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AddRGB(float value)",
                            Summary = "Adds value to all RGB channels. Clamps to [0, 1].",
                            Code = @"Color lighter = color.AddRGB(0.15f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Invert()",
                            Summary = "Returns a color with each RGB channel inverted (1 - channel). Alpha unchanged.",
                            Code = @"Color inv = accentColor.Invert();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToGrayscale()",
                            Summary = "Converts to grayscale using luminance weights.",
                            Code = @"Color grey = spriteColor.ToGrayscale();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetLuminance()",
                            Summary = "Returns the perceptual luminance (0–1) of the color.",
                            Code = @"bool isDark = color.GetLuminance() < 0.5f;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToHex()",
                            Summary = "Returns the color as a #RRGGBBAA hex string.",
                            Code = @"string hex = selectedColor.ToHex(); // ""#FF6600FF""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToColor()  (string ext)",
                            Summary = "Parses a #RRGGBB or #RRGGBBAA hex string into a Color.",
                            Code = @"Color c = ""#FF6600"".ToColor();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Vector2 / Vector3 Extensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Fluent component modification, distance/direction, range checks, perpendicular, rotate, and random circle/disk/annulus.",
                    Description = "With/Add/Subtract/Multiply/Divide allow per-component modification without allocating. InRangeOf/DirectionTo/DistanceTo simplify spatial queries. Perpendicular/PerpendicularClockwise, Rotate(). IsInsideCircle/Rect/Sphere/Box for hit checks. RandomPointOnCircle (edge), RandomPointInDisk (filled, uses insideUnitCircle), RandomPointInAnnulus (ring) for spawn scatter.",
                    Code =
@"// Vector2
Vector2 v   = vector.With(x: 5f).Add(y: 2f);
bool near   = pos.InRangeOf(target, 5f);
Vector2 dir = from.DirectionTo(to);
Vector2 per = vector.Perpendicular();
Vector2 rot = vector.Rotate(45f);
Vector2 onEdge = origin.RandomPointOnCircle(5f);
Vector2 inDisk = origin.RandomPointInDisk(5f);
Vector2 inRing = origin.RandomPointInAnnulus(2f, 8f);

// Vector3
Vector3 w     = vector.With(y: 10f).RotateY(90f);
bool inSphere = point.IsInsideSphere(center, 5f);
bool inBox    = point.IsInsideBox(center, size);
Vector3 onEdge3 = origin.RandomPointOnCircle(5f, Plane2D.XZ);
Vector3 inDisk3 = origin.RandomPointInDisk(5f, Plane2D.XZ);
Vector3 inRing3 = origin.RandomPointInAnnulus(2f, 8f, Plane2D.XZ);",
                    Tags = new[] { "Vector", "Math", "Spatial" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "With(float? x, float? y) / With(float? x, float? y, float? z)",
                            Summary = "Returns a copy with specified components replaced. Unspecified components are preserved.",
                            Code =
@"Vector2 up    = vel.With(y: 0f);      // zero out Y
Vector3 flat  = pos.With(y: 0f);      // flatten to XZ plane" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Add / Subtract / Multiply / Divide",
                            Summary = "Fluent per-component arithmetic. All return a new vector.",
                            Code =
@"Vector2 shifted = pos.Add(x: 2f).Subtract(y: 1f);
Vector3 scaled  = size.Multiply(z: 2f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "InRangeOf(Vector2 target, float range)",
                            Summary = "Returns true when this vector is within range distance of target.",
                            Code =
@"if (transform.position.InRangeOf(target.position, 5f))
    Attack();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DirectionTo(Vector2 target)",
                            Summary = "Returns a normalized direction vector from this point to target.",
                            Code = @"Vector2 dir = origin.DirectionTo(target);
rb.velocity = dir * speed;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DistanceTo(Vector2 target)",
                            Summary = "Returns the distance from this vector to target.",
                            Code = @"float d = pos.DistanceTo(enemy.position);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Perpendicular() / PerpendicularClockwise()",
                            Summary = "Returns a vector perpendicular to this one (counter-clockwise or clockwise).",
                            Code = @"Vector2 normal = moveDir.Perpendicular();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Rotate(float degrees)  (Vector2 ext)",
                            Summary = "Rotates the Vector2 by the given degrees counter-clockwise.",
                            Code = @"Vector2 rotated = forward.Rotate(45f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointOnCircle(float radius)",
                            Summary = "Returns a random point on the circumference of a circle. Uses trig (cos/sin).",
                            Code = @"Vector2 spawnPos = origin.RandomPointOnCircle(5f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInDisk(float radius)",
                            Summary = "Returns a random point inside a filled circle. Uses Random.insideUnitCircle — fastest option.",
                            Code = @"Vector2 scatter = origin.RandomPointInDisk(5f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInAnnulus(float min, float max)",
                            Summary = "Returns a random point within a ring defined by min and max radius. Uses sqrt for uniform distribution.",
                            Code = @"Vector2 spawnPos = origin.RandomPointInAnnulus(3f, 8f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RotateX(float deg) / RotateY(float deg) / RotateZ(float deg)  (Vector3 ext)",
                            Summary = "Rotates the Vector3 around the specified axis by degrees.",
                            Code = @"Vector3 orbiting = basePos.RotateY(angle);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideSphere(Vector3 center, float radius)",
                            Summary = "Returns true when this point is inside the sphere.",
                            Code = @"if (point.IsInsideSphere(explosionCenter, blastRadius)) ApplyDamage();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideBox(Vector3 center, Vector3 size)",
                            Summary = "Returns true when this point is inside the axis-aligned box.",
                            Code = @"if (pos.IsInsideBox(roomCenter, roomSize)) TriggerRoomEvent();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "StringExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Comma-float parsing, SplitCamelCase, WithoutSpaces, percent formatting, enum conversion.",
                    Description = "Parsing/format helpers for strings and primitive values. Number-display formatting lives in BigNumberFormatExtensions. AsPercent/AsExactPercent live on float, not string.",
                    Code =
@"float  val   = ""3,14"".ParseFloatWithComma();    // 3.14f
bool   ok    = ""3,14"".TryParseFloatWithComma(out float v);
string split = ""MyVarName"".SplitCamelCase();    // ""My Var Name""
string clean = ""hello world"".WithoutSpaces();   // ""helloworld""

string pct   = 0.25f.AsPercent();                 // ""25%""
string exact = 25f.AsExactPercent();              // ""25%""

MyEnum e  = ""EnumValue"".ToEnum<MyEnum>();
MyEnum e2 = ""bad"".ToEnumOrDefault(MyEnum.Default);",
                    Tags = new[] { "String", "Format" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ParseFloatWithComma()",
                            Summary = "Parses a float string that uses a comma as the decimal separator. Throws on failure.",
                            Code = @"float f = ""3,14"".ParseFloatWithComma(); // 3.14f" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "TryParseFloatWithComma(out float result)",
                            Summary = "Non-throwing variant of ParseFloatWithComma. Returns false on parse failure.",
                            Code =
@"if (input.TryParseFloatWithComma(out float f))
    ApplyValue(f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SplitCamelCase()",
                            Summary = "Inserts spaces before capital letters. Useful for displaying field names as labels.",
                            Code = @"string label = nameof(playerHealth).SplitCamelCase(); // ""Player Health""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithoutSpaces()",
                            Summary = "Removes all spaces from the string. Null-safe — returns empty string for null/empty input.",
                            Code = @"string key = displayName.WithoutSpaces(); // ""PlayerOne""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AsPercent(int decimalPlaces = 0)  (float ext)",
                            Summary = "Multiplies the float by 100 and appends %. 0.25f → \"25%\".",
                            Code = @"winRateLabel.text = winRate.AsPercent(); // ""72%""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AsExactPercent(int decimalPlaces = 0)  (float ext)",
                            Summary = "Appends % to the value as-is, without multiplying. 25f → \"25%\".",
                            Code = @"label.text = healthPercent.AsExactPercent();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToEnum<T>()",
                            Summary = "Parses the string as an enum value of type T. Throws if the value is not defined.",
                            Code = @"WeaponType wt = ""Sword"".ToEnum<WeaponType>();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToEnumOrDefault<T>(T defaultValue = default)",
                            Summary = "Parses the string as enum T. Returns defaultValue (with a warning) if parsing fails or input is null/empty.",
                            Code = @"MyEnum e = userInput.ToEnumOrDefault(MyEnum.None);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "BigNumberStyleExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Format int/long/decimal values as big-number display strings. Two styles: Compact (K/M/B/T → aa, bb, ...) and Alphabetical (a, b, c, ...).",
                    Description = "A mini number-formatter — no dependency on the TRnK.BigNum package. Use when a project wants idle/incremental-style number display without the full big-number arithmetic. Overloads for int, long, and decimal.",
                    Code =
@"decimal n = 1_500_000m;
string compact = n.ToBigNumber(BigNumberStyle.Compact);          // ""1.5M""
string alpha   = n.ToBigNumber(BigNumberStyle.Alphabetical);     // ""1.5b""

// Beyond T:
string big = 1_000_000_000_000_000m.ToBigNumber(BigNumberStyle.Compact); // ""1aa""

// Custom precision (trailing zeros always trimmed):
string p2 = 1230m.ToBigNumber(BigNumberStyle.Compact, 2); // ""1.23K""

// Negatives preserved:
string neg = (-1500m).ToBigNumber(BigNumberStyle.Compact); // ""-1.5K""",
                    Tags = new[] { "Number", "Format", "BigNumber", "Idle" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToBigNumber(BigNumberStyle style, int decimalPlaces = 1)  (decimal ext)",
                            Summary = "Formats the value using the given BigNumberStyle. Sub-1000 values written as-is. Trailing zeros always trimmed.",
                            Code =
@"goldLabel.text = playerGold.ToBigNumber(BigNumberStyle.Compact);
hpLabel.text   = currentHP.ToBigNumber(BigNumberStyle.Alphabetical, decimalPlaces: 2);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "CollectionExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Rand, Shuffle, Swap, RandWeighted, RandMultiple, Slice, ToLiteral, and more.",
                    Description = "All helpers are generic. RandWeighted accepts a weight selector func. ToLiteral gives human-readable string representations useful for debug logging. Null-safety: IsNullOrEmpty, ContainsNull.",
                    Code =
@"T    item  = array.Rand();
int  idx   = array.RandIndex();
T[]  sh    = array.Shuffle();
T[]  sl    = array.Slice(2, 5);
T[]  multi = array.RandMultiple(3);
T    wgt   = array.RandWeighted(x => x.weight);
string lit = array.ToLiteral();   // ""[a, b, c]""

bool empty = list.IsNullOrEmpty();

V      rv   = dict.RandV();
string dlit = dict.ToLiteral();  // ""{k: v, ...}""",
                    Tags = new[] { "Array", "List", "Dictionary", "Random" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Rand()",
                            Summary = "Returns a random element. Works on T[], List<T>. Throws if null or empty.",
                            Code = @"string name = namePool.Rand();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandIndex()",
                            Summary = "Returns a random valid index into the array or list.",
                            Code = @"int i = slots.RandIndex();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Shuffle()",
                            Summary = "Returns a new shuffled copy without modifying the original.",
                            Code =
@"T[] deck = cards.Shuffle();
foreach (var card in deck) Deal(card);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandMultiple(int count)",
                            Summary = "Returns count unique random elements without replacement.",
                            Code = @"Item[] loot = dropTable.RandMultiple(3);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandWeighted(Func<T, float> weightSelector)",
                            Summary = "Picks one element using weighted probability. Higher weight = more likely.",
                            Code =
@"Enemy e = spawnTable.RandWeighted(x => x.spawnWeight);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SwapAt(int aIndex, int bIndex) / Swap(T a, T b)",
                            Summary = "Swaps two elements in-place (void). SwapAt uses indices; Swap uses values (first occurrence).",
                            Code =
@"order.SwapAt(0, 2);        // by index, in-place
names.Swap(""Alice"", ""Bob""); // by value, in-place" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Slice(int startIndex, int length)",
                            Summary = "Returns a sub-array from startIndex with the given length.",
                            Code = @"T[] page = allItems.Slice(pageIndex * 10, 10);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsNullOrEmpty()",
                            Summary = "Returns true if the collection is null or contains no elements. Works on T[], List<T>, ICollection<T>.",
                            Code =
@"if (inventory.IsNullOrEmpty())
    ShowEmptyState();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ContainsNull()",
                            Summary = "Returns true if any element in the array or list is null.",
                            Code = @"Log.Assert(!slots.ContainsNull(), ""Null slot detected"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToLiteral()",
                            Summary = "Returns a human-readable string of the collection contents for debug logging.",
                            Code =
@"Debug.Log(inventory.ToLiteral());   // ""[Sword, Shield, Potion]""
Debug.Log(stats.ToLiteral());       // ""{hp: 100, mp: 50}""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandV() / RandK()  (Dictionary)",
                            Summary = "Returns a random value or key from a Dictionary.",
                            Code =
@"string key = dialogueMap.RandK();
string val = dialogueMap.RandV();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "NumberExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "PercentageOf, IsSuccessfulRoll probability, ToEnum, ToEnumOrDefault.",
                    Description = "IsSuccessfulRoll(float) uses a [min, max] range (default 0–1). The int overload compares against [min, max) via Random.Range. Both throw ArgumentException if the value is outside the range.",
                    Code =
@"float pct  = current.PercentageOf(total);

bool hit  = 0.75f.IsSuccessfulRoll();           // 75% chance (range 0–1)
bool hit2 = 25.IsSuccessfulRoll(0, 100);        // 25 out of [0, 100)

MyEnum e  = 1.ToEnum<MyEnum>();
MyEnum e2 = 99.ToEnumOrDefault(MyEnum.None);",
                    Tags = new[] { "Number", "Math", "Random" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "PercentageOf(float total) / PercentageOf(int total)",
                            Summary = "Returns this value as a 0–1 fraction of total. Returns 0 if total is 0.",
                            Code =
@"float fill = currentHealth.PercentageOf(maxHealth);
healthBar.fillAmount = fill;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsSuccessfulRoll(float min = 0f, float max = 1f)  (float)",
                            Summary = "Returns true with probability equal to the value within [min, max]. Boundary-exact: value <= min is always false, value >= max is always true.",
                            Code =
@"if (0.30f.IsSuccessfulRoll()) SpawnLoot();        // 30% chance
if (0.75f.IsSuccessfulRoll(0f, 1f)) CriticalHit(); // 75% chance" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsSuccessfulRoll(int min = 0, int max = 100)  (int)",
                            Summary = "Returns true if Random.Range(min, max) < value. Uses [min, max) semantics: value == max is always true, value == min is always false.",
                            Code = @"if (25.IsSuccessfulRoll(0, 100)) TriggerCriticalHit(); // 25% chance" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToEnum<T>()",
                            Summary = "Converts the integer to enum T. Throws ArgumentException if the value is not a defined member.",
                            Code = @"GameState state = savedValue.ToEnum<GameState>();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToEnumOrDefault<T>(T defaultValue = default)",
                            Summary = "Converts the integer to enum T, returning defaultValue (with a warning log) if not defined.",
                            Code = @"MyEnum e = rawInt.ToEnumOrDefault(MyEnum.None);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TimeExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "ToClock, ToShortClock, ToReadableFormat (float/double/int/TimeSpan), WithDate, WithTime, and period-check helpers.",
                    Description = "All formatting methods have float, double, int, and TimeSpan overloads. Float/double overloads accept useCeiling (default true). ToReadableFormat omits zero units and auto-selects granularity. WithDate/WithTime produce new DateTimes with modified components. IsStartOfDay/Week/Month are period-boundary checks.",
                    Code =
@"string clock = 3661f.ToClock();                    // ""01:01:01""
string sh    = 125f.ToShortClock();                // ""02:05""
string rdbl  = 93784f.ToReadableFormat();          // ""1d2h3m""
string spaced = 93784f.ToReadableFormat(useSpacing: true); // ""1d 2h 3m""

// TimeSpan overload
string ts = TimeSpan.FromHours(2.5).ToReadableFormat(); // ""2h30m""

// DateTime helpers
DateTime dt = DateTime.UtcNow.WithTime(hour: 0, minute: 0, second: 0);
bool isMonday = DateTime.UtcNow.IsStartOfWeek(DayOfWeek.Monday);",
                    Tags = new[] { "Time", "Format", "Clock", "DateTime" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToClock(bool useCeiling = true)  (float / double / int ext)",
                            Summary = "Formats seconds as HH:MM:SS. Rounds up by default for float/double.",
                            Code = @"timerLabel.text = remainingSeconds.ToClock(); // ""01:30:00""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToShortClock(bool useCeiling = true)  (float / double / int ext)",
                            Summary = "Formats seconds as MM:SS. Rounds up by default for float/double.",
                            Code = @"timerLabel.text = remainingSeconds.ToShortClock(); // ""02:45""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ToReadableFormat(bool useSpacing = true, bool useCeiling = true)  (float / double / int / TimeSpan ext)",
                            Summary = "Formats duration into \"1d 2h 3m\" style, omitting zero units. useSpacing adds spaces between units.",
                            Code =
@"cooldownLabel.text = timeLeft.ToReadableFormat();           // ""3h 20m""
cooldownLabel.text = timeLeft.ToReadableFormat(false);      // ""3h20m""
cooldownLabel.text = TimeSpan.FromMinutes(90).ToReadableFormat(); // ""1h 30m""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithDate(int? year, int? month, int? day)  (DateTime ext)",
                            Summary = "Returns a new DateTime with modified date components. Day clamps to the new month's max.",
                            Code =
@"DateTime firstOfMonth = DateTime.UtcNow.WithDate(day: 1);
DateTime nextYear = DateTime.UtcNow.WithDate(year: DateTime.UtcNow.Year + 1);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithTime(int? hour, int? minute, int? second, int? millisecond)  (DateTime ext)",
                            Summary = "Returns a new DateTime with modified time components.",
                            Code =
@"DateTime midnight = DateTime.UtcNow.WithTime(hour: 0, minute: 0, second: 0);
DateTime noon     = DateTime.UtcNow.WithTime(hour: 12);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsStartOfDay()  (DateTime ext)",
                            Summary = "Returns true when the DateTime's time-of-day is exactly 00:00:00.",
                            Code = @"if (serverTime.IsStartOfDay()) ResetDailyQuests();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsStartOfWeek(DayOfWeek firstDay = Monday)  (DateTime ext)",
                            Summary = "Returns true when the DateTime falls on the first day of the week at 00:00:00.",
                            Code = @"if (serverTime.IsStartOfWeek()) ResetWeeklyRewards();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsStartOfMonth()  (DateTime ext)",
                            Summary = "Returns true when the DateTime is the 1st day of the month at 00:00:00.",
                            Code = @"if (serverTime.IsStartOfMonth()) ResetMonthlyLeaderboard();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TMPTextExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "TMP_Text helpers: SetClock, SetShortClock, SetReadableTime — zero-alloc via TMP's SetText.",
                    Description = "All methods use TMP_Text.SetText() internally — no string allocations. Float overloads accept a useCeiling parameter (default true) to control rounding. SetReadableTime omits zero units and adjusts granularity automatically (e.g. shows 'd h m' when >= 1 day).",
                    Code =
@"timerLabel.SetClock(remainingSeconds);        // ""01:30:00""
timerLabel.SetShortClock(remainingSeconds);   // ""01:30""
timerLabel.SetReadableTime(remainingSeconds); // ""1h 30m 0s""

// Float overloads — ceiling by default
timerLabel.SetClock(3661.7f);                 // ""01:01:02""
timerLabel.SetClock(3661.7f, useCeiling: false); // ""01:01:01""",
                    Tags = new[] { "TMP", "TextMeshPro", "Time", "Format" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetClock(int totalSeconds)",
                            Summary = "Sets TMP_Text to \"HH:MM:SS\" format. Zero-alloc.",
                            Code = @"timerLabel.SetClock(Mathf.CeilToInt(remaining));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetClock(float totalSeconds, bool useCeiling = true)",
                            Summary = "Float overload — rounds up by default. Pass useCeiling: false to floor instead.",
                            Code =
@"// Update each frame — no string allocation
timerLabel.SetClock(countdown.RemainingTime);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetShortClock(int totalSeconds)",
                            Summary = "Sets TMP_Text to \"MM:SS\" format. Zero-alloc.",
                            Code = @"timerLabel.SetShortClock(125); // ""02:05""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetShortClock(float totalSeconds, bool useCeiling = true)",
                            Summary = "Float overload of SetShortClock.",
                            Code = @"timerLabel.SetShortClock(countdown.RemainingTime);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetReadableTime(int totalSeconds, bool useSpacing = true)",
                            Summary = "Sets TMP_Text to a human-readable duration like \"1d 2h 3m\" or \"45m 10s\". Omits zero units. useSpacing controls spaces between units.",
                            Code =
@"cooldownLabel.SetReadableTime(93784);          // ""1d 2h 3m""
cooldownLabel.SetReadableTime(93784, false);   // ""1d2h3m""" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetReadableTime(float totalSeconds, bool useSpacing = true, bool useCeiling = true)",
                            Summary = "Float overload of SetReadableTime.",
                            Code = @"cooldownLabel.SetReadableTime(timeLeft);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TextColorizeExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Unity rich-text color tags: whole string, char, word, words, params variants, predicate, and hex string.",
                    Description = "All overloads produce Unity rich-text <color> tags. char ext operates on a single character. params string[]/char[] overloads color multiple targets. Func<string,bool> overload colorizes matching words. hex string overload validates with ColorUtility and returns text unchanged on bad input.",
                    Code =
@"string s1 = ""Hello"".Colorize(Color.red);              // whole string
string s2 = 'H'.Colorize(Color.red);                   // char ext
string s3 = ""Hello World"".Colorize(Color.yellow, ""Hello""); // word
string s4 = ""Hello World"".Colorize(Color.cyan, 'o');   // char in string
string s5 = ""a b c"".Colorize(Color.green, ""a"", ""c"");  // params words
string s6 = ""abc"".Colorize(Color.green, 'a', 'c');     // params chars
string s7 = ""Error ok"".Colorize(Color.red, w => w == ""Error""); // predicate
string s8 = ""#FF0000"".Colorize(""#FF0000"");             // hex overload",
                    Tags = new[] { "RichText", "Color", "Colorize" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color)  (string ext)",
                            Summary = "Wraps the entire string in a Unity rich-text color tag.",
                            Code = @"debugLabel.text = message.Colorize(Color.yellow);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color)  (char ext)",
                            Summary = "Wraps a single character in a color tag and returns it as a string.",
                            Code = @"string tagged = 'A'.Colorize(Color.red);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color, string word)",
                            Summary = "Colors every word-boundary match of the given word within the string.",
                            Code = @"string s = ""Error: file not found"".Colorize(Color.red, ""Error"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color, char character)",
                            Summary = "Colors every occurrence of the given character within the string.",
                            Code = @"string s = ""Hello World"".Colorize(Color.red, 'o');" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color, params string[] words)",
                            Summary = "Colors every word-boundary match of any of the supplied words.",
                            Code = @"string s = ""Coin Ring Star"".Colorize(Color.yellow, ""Coin"", ""Star"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color, params char[] characters)",
                            Summary = "Colors every occurrence of any of the supplied characters.",
                            Code = @"string s = ""abc"".Colorize(Color.cyan, 'a', 'c');" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(Color color, Func<string, bool> predicate)",
                            Summary = "Colors every word for which the predicate returns true.",
                            Code = @"string s = msg.Colorize(Color.red, w => w == ""Error"" || w == ""Fatal"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Colorize(string hexColorCode)  (string ext)",
                            Summary = "Wraps the string in a color tag using a hex string (#RRGGBB). Returns text unchanged if the hex is invalid.",
                            Code = @"string s = ""Hello"".Colorize(""#FF4444"");" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TextFormatExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Unity rich-text Bold, Italic, Underline, Size — whole string, word, words, predicate, char overloads.",
                    Description = "Every method has: no-arg (whole string), string word, params string[] words, Func<string,bool> predicate, and char overloads. Bold/Italic/Underline use word-boundary matching; char overloads use string.Replace. Size overloads throw on non-positive values. All are chainable.",
                    Code =
@"string bold      = ""Important"".Bold();
string italic    = ""Emphasis"".Italic();
string underline = ""Link"".Underline();
string sized     = ""Big"".Size(24f);

string result = ""Important Warning""
    .Bold(""Important"")
    .Italic(""Warning"")
    .Underline(w => w == ""Warning"")
    .Size(18f, ""Warning"");",
                    Tags = new[] { "RichText", "Bold", "Italic", "Underline", "Size", "Format" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Bold() / Bold(string) / Bold(params string[]) / Bold(Func<string,bool>) / Bold(char)",
                            Summary = "Wraps matches in <b> tags. Char overload uses string.Replace (not word-boundary).",
                            Code =
@"label.text = ""Press Space"".Bold(""Space"");
label.text = msg.Bold(w => w == ""ERROR"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Italic() / Italic(string) / Italic(params string[]) / Italic(Func<string,bool>) / Italic(char)",
                            Summary = "Wraps matches in <i> tags. Char overload uses string.Replace.",
                            Code = @"label.text = caption.Italic();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Underline() / Underline(string) / Underline(params string[]) / Underline(Func<string,bool>) / Underline(char)",
                            Summary = "Wraps matches in <u> tags. Char overload uses string.Replace.",
                            Code =
@"label.text = ""Click here"".Underline(""here"");
label.text = link.Underline();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Size(float size) / Size(float, string) / Size(float, params string[]) / Size(float, Func<string,bool>)",
                            Summary = "Wraps matches in <size> tags. Throws ArgumentOutOfRangeException if size <= 0.",
                            Code = @"label.text = title.Size(28f).Bold();" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "CoroutineExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Sequential, parallel, delayed, conditional coroutine starters. Coroutine↔Task bridge.",
                    Description = "StartCoroutineSequence runs coroutines one after another. StartCoroutineParallel runs them concurrently. StartCoroutineDelayed adds a wait. StartCoroutineWhen waits for a condition. AsTask, WhenAll, WhenAny bridge into async/await.",
                    Code =
@"this.StartCoroutineSequence(corA, corB, corC);
this.StartCoroutineParallel(corA, corB);
this.StartCoroutineDelayed(myCor, 2f);
this.StartCoroutineWhen(myCor, () => isReady);

// Async bridge
Task t = StartCoroutine(myCor).AsTask(this);
await t;
await this.WhenAll(corA, corB, corC);",
                    Tags = new[] { "Coroutine", "Async", "Task" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartCoroutineSequence(params IEnumerator[])",
                            Summary = "Runs coroutines one after another, waiting for each to finish before starting the next.",
                            Code =
@"this.StartCoroutineSequence(
    FadeOut(),
    LoadScene(),
    FadeIn());" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartCoroutineParallel(params IEnumerator[])",
                            Summary = "Starts all coroutines simultaneously on the same MonoBehaviour.",
                            Code = @"this.StartCoroutineParallel(PlayMusic(), AnimateUI(), SpawnEnemies());" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartCoroutineDelayed(IEnumerator coroutine, float delay)",
                            Summary = "Waits delay seconds before starting the coroutine.",
                            Code = @"this.StartCoroutineDelayed(SpawnBoss(), 3f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartCoroutineWhen(IEnumerator coroutine, Func<bool> predicate)",
                            Summary = "Polls the predicate each frame and starts the coroutine once it returns true.",
                            Code = @"this.StartCoroutineWhen(BeginCutscene(), () => assetsLoaded);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AsTask(MonoBehaviour owner)  (Coroutine ext)",
                            Summary = "Converts a running Coroutine handle to a Task that completes when the coroutine ends. Respects destroyCancellationToken.",
                            Code =
@"Task t = StartCoroutine(MyCoroutine()).AsTask(this);
await t;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AsTask(MonoBehaviour owner)  (IEnumerator ext)",
                            Summary = "Converts an IEnumerator directly to a Task without needing StartCoroutine. Propagates exceptions.",
                            Code = @"await MyCoroutine().AsTask(this);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WhenAll(params IEnumerator[])  (MonoBehaviour ext)",
                            Summary = "Runs all coroutines in parallel and returns a Task that completes when all finish.",
                            Code = @"await this.WhenAll(LoadAudio(), LoadTextures(), LoadLevel());" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WhenAny(params IEnumerator[])  (MonoBehaviour ext)",
                            Summary = "Runs all coroutines and returns a Task<Task> that completes when the first one finishes.",
                            Code = @"await this.WhenAny(WaitForInput(), TimeoutCoroutine(5f));" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TaskExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Forget() for fire-and-forget tasks, YieldTask for awaiting Tasks inside coroutines.",
                    Description = "Forget() swallows exceptions unless you supply an error handler. YieldTask wraps a Task in a CustomYieldInstruction. AsCoroutine() converts an async Task to a yield instruction.",
                    Code =
@"myTask.Forget();
myTask.Forget(ex => Log.Error($""Failed: {ex}""));

IEnumerator Example()
{
    Task<string> webTask = FetchAsync();
    yield return new YieldTask(webTask);
    Log.Info(webTask.Result);

    yield return AnotherAsync().AsCoroutine();
}",
                    Tags = new[] { "Task", "Async", "Coroutine" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Forget()",
                            Summary = "Fire-and-forget a Task. Silently swallows any exception.",
                            Code = @"SaveDataAsync().Forget();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Forget(Action<Exception> errorHandler)",
                            Summary = "Fire-and-forget with an error handler invoked if the Task faults.",
                            Code = @"FetchLeaderboardAsync().Forget(ex => Log.Error($""Fetch failed: {ex.Message}""));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AsCoroutine()",
                            Summary = "Wraps a Task as a yield instruction so it can be awaited inside a coroutine.",
                            Code =
@"IEnumerator LoadSequence()
{
    yield return FetchServerTimeAsync().AsCoroutine();
    Debug.Log(""Time fetched, continuing..."");
}" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "new YieldTask(Task task)",
                            Summary = "Wraps a non-generic Task as a CustomYieldInstruction. Hold a reference to the original Task to read results — YieldTask has no .Result property.",
                            Code =
@"IEnumerator Fetch()
{
    Task<string> t = DownloadAsync();
    yield return new YieldTask(t);
    Debug.Log(t.Result); // read result from the original Task
}" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "AnimatorExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Clip length query and current-state checks (string + hash overloads).",
                    Description = "GetAnimationLength iterates runtimeAnimatorController.animationClips by name and returns 0f if not found or if the controller is null. IsPlayingAnimation has two overloads: string checks the state name via IsName(); int checks shortNameHash directly.",
                    Code =
@"float len = animator.GetAnimationLength(""Jump"");
bool  ok  = animator.IsPlayingAnimation(""Jump"");
bool  ok2 = animator.IsPlayingAnimation(IdleStateHash, layerIndex: 1);",
                    Tags = new[] { "Animator", "Animation" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetAnimationLength(string animName)",
                            Summary = "Returns the clip length in seconds by matching the clip asset's name. Returns 0f if not found.",
                            Code =
@"float attackDur = animator.GetAnimationLength(""Attack"");
Invoke(nameof(OnAttackEnd), attackDur);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsPlayingAnimation(string animName, int layerIndex = 0)",
                            Summary = "Returns true if the animator's current state on the given layer matches the name.",
                            Code = @"if (!animator.IsPlayingAnimation(""Idle"")) return;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsPlayingAnimation(int stateNameHash, int layerIndex = 0)",
                            Summary = "Returns true if the current state's shortNameHash matches the given hash.",
                            Code = @"if (animator.IsPlayingAnimation(Animator.StringToHash(""Idle""))) return;" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "CameraExtensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Culling mask management, FOV control, orthographic size, FitBoundsInView, screen size query.",
                    Description = "Culling helpers: IsLayerInCullingMask, AddToCullingMask, RemoveFromCullingMask, SetCullingMask. FOV: SetFOV. Orthographic: SetOrthographicSize, FitBoundsInView.",
                    Code =
@"camera.AddToCullingMask(LayerMask.GetMask(""UI""));
camera.RemoveFromCullingMask(LayerMask.GetMask(""UI""));

camera.SetFOV(60f);
camera.SetOrthographicSize(5f);
camera.FitBoundsInView(bounds);

Vector2 screen = camera.GetScreenSize();",
                    Tags = new[] { "Camera", "Culling", "FOV" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AddToCullingMask(LayerMask mask) / RemoveFromCullingMask(LayerMask mask)",
                            Summary = "Adds or removes layers from the camera's culling mask without touching other layers.",
                            Code =
@"cam.AddToCullingMask(LayerMask.GetMask(""UI""));
cam.RemoveFromCullingMask(LayerMask.GetMask(""HUD""));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsLayerInCullingMask(LayerMask layerMask)",
                            Summary = "Returns true if any layer in the LayerMask is currently visible to this camera.",
                            Code = @"if (!cam.IsLayerInCullingMask(LayerMask.GetMask(""UI""))) cam.AddToCullingMask(LayerMask.GetMask(""UI""));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetCullingMask(LayerMask mask)",
                            Summary = "Replaces the culling mask outright. Pass 0 to render nothing, -1 to render everything.",
                            Code = @"screenshotCam.SetCullingMask(LayerMask.GetMask(""Game""));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetFOV(float fov)",
                            Summary = "Sets the perspective camera field of view, clamped to [MinFOV, MaxFOV].",
                            Code = @"cam.SetFOV(60f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetOrthographicSize(float size)",
                            Summary = "Sets the orthographic camera's half-height size.",
                            Code = @"cam.SetOrthographicSize(5f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "FitBoundsInView(Bounds bounds)",
                            Summary = "Adjusts orthographic size or FOV so the given Bounds fits entirely in view.",
                            Code = @"cam.FitBoundsInView(levelBounds);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetScreenSize()",
                            Summary = "Returns the screen dimensions as a Vector2 (width, height) in pixels.",
                            Code = @"Vector2 screen = cam.GetScreenSize();" },
                    }
                },

                new TRnKDocEntry
                {
                    Title = "Vector2Extensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Fluent Vector2 arithmetic (With/Add/Subtract/Multiply/Divide), spatial queries, random point generation.",
                    Description = "All arithmetic methods accept nullable float components — omit any axis to leave it unchanged. InRangeOf uses sqrMagnitude for performance. RandomPointOnCircle/InDisk/InAnnulus generate uniform distributions. DirectionTo returns zero-vector when source equals target.",
                    Code =
@"Vector2 moved  = pos.Add(x: 1f);             // only X
Vector2 scaled = vel.Multiply(x: 2f, y: 0.5f);
Vector2 dir    = origin.DirectionTo(target);
bool    close  = pos.InRangeOf(target, 3f);

Vector2 onRing = origin.RandomPointInAnnulus(1f, 3f);
Vector2 perp   = dir.Perpendicular();
Vector2 rot    = dir.Rotate(45f);",
                    Tags = new[] { "Vector2", "Math", "Spatial" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "With(float? x, float? y)",
                            Summary = "Returns a new Vector2 with the specified components replaced; omit a param to keep that axis unchanged.",
                            Code = @"Vector2 flat = velocity.With(y: 0f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Add / Subtract / Multiply / Divide(float? x, float? y)",
                            Summary = "Fluent per-axis arithmetic. Divide ignores null/zero divisors (leaves axis unchanged).",
                            Code =
@"Vector2 a = pos.Add(x: 1f);
Vector2 b = vel.Multiply(x: 2f, y: 0.5f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "InRangeOf(Vector2 target, float range)",
                            Summary = "Returns true when distance to target <= range. Negative range always returns false.",
                            Code = @"if (pos.InRangeOf(enemy.position, detectionRadius)) Alert();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithMagnitude(float magnitude)",
                            Summary = "Returns a vector in the same direction with the given magnitude. Returns zero if original is zero.",
                            Code = @"Vector2 capped = velocity.WithMagnitude(maxSpeed);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DirectionTo(Vector2 to)",
                            Summary = "Returns the normalized direction toward the target. Returns zero if source equals target.",
                            Code = @"Vector2 dir = transform.position.DirectionTo(target.position);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DistanceTo(Vector2 to)",
                            Summary = "Returns the distance to the target vector.",
                            Code = @"float dist = pos.DistanceTo(target);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Perpendicular() / PerpendicularClockwise()",
                            Summary = "Returns a vector perpendicular to this one. Perpendicular is counter-clockwise; PerpendicularClockwise is clockwise.",
                            Code = @"Vector2 normal = moveDir.Perpendicular();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Rotate(float degrees)",
                            Summary = "Rotates the vector by the given angle in degrees.",
                            Code = @"Vector2 rotated = forward.Rotate(45f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "MaxComponent() / MinComponent()",
                            Summary = "Returns the largest or smallest component of the vector.",
                            Code = @"float biggest = size.MaxComponent();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideCircle(Vector2 center, float radius)",
                            Summary = "Returns true when this point is inside the circle. Delegates to InRangeOf.",
                            Code = @"if (point.IsInsideCircle(origin, radius)) Collect();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideRect(Vector2 center, Vector2 size)",
                            Summary = "Returns true when this point is inside the axis-aligned rectangle.",
                            Code = @"if (point.IsInsideRect(roomCenter, roomSize)) Enter();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointOnCircle(float radius)",
                            Summary = "Returns a random point on the circumference of a circle of the given radius around this origin.",
                            Code = @"Vector2 spawnPos = center.RandomPointOnCircle(spawnRadius);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInDisk(float radius)",
                            Summary = "Returns a random point inside a disk of the given radius around this origin.",
                            Code = @"Vector2 scatter = origin.RandomPointInDisk(blastRadius);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInAnnulus(float minRadius, float maxRadius)",
                            Summary = "Returns a random point in an annulus (ring). Throws if minRadius < 0 or maxRadius < minRadius.",
                            Code = @"Vector2 pos = center.RandomPointInAnnulus(minSpawn, maxSpawn);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Vector3Extensions",
                    Namespace = "TRnK.Extensions",
                    Summary = "Fluent Vector3 arithmetic (With/Add/Subtract/Multiply/Divide), rotation helpers, spatial queries, random point generation on configurable planes.",
                    Description = "All arithmetic methods accept nullable float components. RotateX/Y/Z use Quaternion.AngleAxis. RandomPointOnCircle/InDisk/InAnnulus accept a Plane2D parameter (XY/XZ/YZ) defaulting to XZ (ground plane). IsInsideColliderBounds uses Bounds.Contains for AABB approximation.",
                    Code =
@"Vector3 flat   = velocity.With(y: 0f);
Vector3 moved  = pos.Add(x: 1f, z: -1f);
Vector3 spun   = forward.RotateY(90f);
bool    close  = pos.InRangeOf(target, 5f);

// Random spawn on ground plane
Vector3 spawn = origin.RandomPointInAnnulus(2f, 8f, Plane2D.XZ);",
                    Tags = new[] { "Vector3", "Math", "Spatial" },
                    Category = DocCategory.Extensions,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "With(float? x, float? y, float? z)",
                            Summary = "Returns a new Vector3 with specified components replaced; omit a param to keep that axis unchanged.",
                            Code = @"Vector3 flat = velocity.With(y: 0f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Add / Subtract / Multiply / Divide(float? x, float? y, float? z)",
                            Summary = "Fluent per-axis arithmetic. Divide ignores null/zero divisors.",
                            Code =
@"Vector3 shifted = pos.Add(y: 1f);
Vector3 scaled  = vel.Multiply(x: 2f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RotateX(float degrees) / RotateY(float degrees) / RotateZ(float degrees)",
                            Summary = "Rotates the vector around the given axis by the specified angle in degrees using Quaternion.AngleAxis.",
                            Code = @"Vector3 rotated = forward.RotateY(90f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "InRangeOf(Vector3 target, float range)",
                            Summary = "Returns true when distance to target <= range. Negative range always returns false.",
                            Code = @"if (pos.InRangeOf(enemy.position, attackRange)) Attack();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithMagnitude(float magnitude)",
                            Summary = "Returns a vector in the same direction with the given magnitude. Returns zero if original is zero.",
                            Code = @"Vector3 capped = velocity.WithMagnitude(maxSpeed);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DirectionTo(Vector3 to)",
                            Summary = "Returns the normalized direction toward the target. Returns zero if source equals target.",
                            Code = @"Vector3 dir = transform.position.DirectionTo(target.position);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "DistanceTo(Vector3 to)",
                            Summary = "Returns the distance to the target vector.",
                            Code = @"float dist = pos.DistanceTo(enemy.position);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "MaxComponent() / MinComponent()",
                            Summary = "Returns the largest or smallest of the X, Y, Z components.",
                            Code = @"float biggest = bounds.size.MaxComponent();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideSphere(Vector3 center, float radius)",
                            Summary = "Returns true when this point is inside the sphere. Delegates to InRangeOf.",
                            Code = @"if (point.IsInsideSphere(blastOrigin, blastRadius)) ApplyDamage();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideBox(Vector3 center, Vector3 size)",
                            Summary = "Returns true when this point is inside the axis-aligned box.",
                            Code = @"if (point.IsInsideBox(roomCenter, roomSize)) Trigger();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInsideColliderBounds(Collider collider)",
                            Summary = "Returns true when this point is inside the collider's AABB (Bounds.Contains — approximation, not exact shape).",
                            Code = @"if (point.IsInsideColliderBounds(zoneTrigger)) EnterZone();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointOnCircle(float radius, Plane2D plane = XZ)",
                            Summary = "Returns a random point on the circumference of a circle around this origin. Plane2D selects XY/XZ/YZ.",
                            Code = @"Vector3 pos = origin.RandomPointOnCircle(5f, Plane2D.XZ);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInDisk(float radius, Plane2D plane = XZ)",
                            Summary = "Returns a uniformly random point inside a disk of the given radius around this origin.",
                            Code = @"Vector3 scatter = origin.RandomPointInDisk(blastRadius);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "RandomPointInAnnulus(float minRadius, float maxRadius, Plane2D plane = XZ)",
                            Summary = "Returns a uniformly random point in an annulus (ring). Throws if minRadius < 0 or maxRadius < minRadius.",
                            Code = @"Vector3 spawn = center.RandomPointInAnnulus(minDist, maxDist);" },
                    }
                },

                // ── Utilities ───────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "MouseUtils",
                    Namespace = "TRnK.Utilities",
                    Summary = "Mouse world-position helpers for 2D and 3D, raycast shortcuts, game-window boundary check, and pointer-over-UI queries.",
                    Description = "GetMousePosition2D works for orthographic cameras. GetMousePosition3D takes a distance parameter. GetMousePosition3DFromRaycast hits actual geometry using an int layerMask. GetMouseRay returns the screen-to-world ray. All position/ray methods accept an optional Camera (defaults to Camera.main). IsPointerOverUI prevents world-space input from firing through UI.",
                    Code =
@"bool inWindow = Utils.IsMouseInGameWindow();
Vector2 pos2D = Utils.GetMousePosition2D();            // Camera.main
Vector2 pos2D = Utils.GetMousePosition2D(cam);         // explicit camera
Vector3 pos3D = Utils.GetMousePosition3D(10f);
Vector3 hit3D = Utils.GetMousePosition3DFromRaycast(
    LayerMask.GetMask(""Ground""));
Ray ray = Utils.GetMouseRay();

if (Utils.IsPointerOverUI()) return;",
                    Tags = new[] { "Mouse", "Input", "Raycast" },
                    Category = DocCategory.Utilities,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsMouseInGameWindow()",
                            Summary = "Returns true when the mouse cursor is within the game window bounds.",
                            Code = @"if (!Utils.IsMouseInGameWindow()) return;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetMousePosition2D(Camera camera = null)",
                            Summary = "Returns the mouse position in 2D world space. Defaults to Camera.main when camera is null.",
                            Code =
@"Vector2 mouseWorld = Utils.GetMousePosition2D();
aimTarget.position = mouseWorld;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetMousePosition3D(float distance, Camera camera = null)",
                            Summary = "Returns the mouse position on a plane at the given distance from the camera. Defaults to Camera.main.",
                            Code =
@"Vector3 pos = Utils.GetMousePosition3D(10f);
targetMarker.position = pos;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetMousePosition3DFromRaycast(int layerMask, Camera camera = null)",
                            Summary = "Raycasts from the mouse and returns the hit point on geometry matching the int layer mask.",
                            Code =
@"Vector3 ground = Utils.GetMousePosition3DFromRaycast(
    LayerMask.GetMask(""Ground""));
character.MoveTo(ground);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetMouseRay(Camera camera = null)",
                            Summary = "Returns the Ray from the camera through the current mouse position. Defaults to Camera.main.",
                            Code =
@"Ray ray = Utils.GetMouseRay();
if (Physics.Raycast(ray, out RaycastHit hit))
    Debug.Log(hit.collider.name);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsPointerOverUI()",
                            Summary = "Returns true if the pointer is currently over any Unity UI element.",
                            Code =
@"if (Utils.IsPointerOverUI()) return; // block world input while hovering UI" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsPointerOverUI(LayerMask layer)",
                            Summary = "Returns true if the pointer is over a UI element on the given layer.",
                            Code =
@"LayerMask hud = LayerMask.GetMask(""HUD"");
if (Utils.IsPointerOverUI(hud)) return;" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetEventSystemRaycastResults()",
                            Summary = "Returns all EventSystem raycast results at the current mouse/touch position.",
                            Code =
@"var hits = Utils.GetEventSystemRaycastResults();
foreach (var hit in hits)
    Debug.Log(hit.gameObject.name);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "EnumUtils",
                    Namespace = "TRnK.Utilities",
                    Summary = "GetRandomEnum, AllEnum, CountEnum, ForEnum — generic enum helpers without boilerplate.",
                    Description = "GetRandomEnum can exclude specific values. AllEnum returns all values as an array with optional exclusions. ForEnum is a foreach substitute that takes an action.",
                    Code =
@"MyEnum rand  = Utils.GetRandomEnum<MyEnum>();
MyEnum randX = Utils.GetRandomEnum(MyEnum.None, MyEnum.Default);

int      count = Utils.CountEnum<MyEnum>();
MyEnum[] all   = Utils.AllEnum<MyEnum>();
MyEnum[] some  = Utils.AllEnum(MyEnum.None);

Utils.ForEnum<MyEnum>(v => Debug.Log(v));",
                    Tags = new[] { "Enum", "Random", "Iteration" },
                    Category = DocCategory.Utilities,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetRandomEnum<T>()",
                            Summary = "Returns a random value of the enum type T.",
                            Code = @"WeaponType wt = Utils.GetRandomEnum<WeaponType>();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetRandomEnum<T>(params T[] exclude)",
                            Summary = "Returns a random enum value, excluding the specified values.",
                            Code = @"Element elem = Utils.GetRandomEnum(Element.None, Element.Null);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "AllEnum<T>(params T[] exclude)",
                            Summary = "Returns all enum values as a T[] array, optionally excluding specified values.",
                            Code =
@"GameState[] active = Utils.AllEnum(GameState.None, GameState.Error);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "CountEnum<T>()",
                            Summary = "Returns the total number of values defined in the enum.",
                            Code = @"int count = Utils.CountEnum<Direction>(); // 4" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "ForEnum<T>(Action<T>)",
                            Summary = "Iterates over all enum values and invokes the action for each.",
                            Code =
@"Utils.ForEnum<Direction>(dir =>
{
    var btn = Instantiate(directionButton);
    btn.Setup(dir);
});" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "TransformUtils / TimeUtils",
                    Namespace = "TRnK.Utilities",
                    Summary = "GetAngleFromVector, GetRandomRotation, and cached WaitForSeconds allocation helpers.",
                    Description = "GetRandomRotation accepts an Axis enum and optional per-axis Vector2 range. TimeUtils.GetWaitForSeconds caches WaitForSeconds instances to avoid per-frame GC allocation in coroutines.",
                    Code =
@"float angle    = Utils.GetAngleFromVector(direction);
Quaternion rY  = Utils.GetRandomRotation(Axis.Y);
Quaternion rXY = Utils.GetRandomRotation(
    Axis.XY,
    new Vector2(0f, 90f),
    new Vector2(-45f, 45f));

// Cached waits — avoids GC alloc
yield return Utils.GetWaitForSeconds(1.5f);
yield return Utils.GetWaitForSecondsRealtime(2f);",
                    Tags = new[] { "Transform", "Rotation", "Coroutine" },
                    Category = DocCategory.Utilities,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetAngleFromVector(Vector2 direction)",
                            Summary = "Returns the angle in degrees of a direction vector relative to Vector2.right.",
                            Code =
@"float angle = Utils.GetAngleFromVector(moveDir);
transform.rotation = Quaternion.Euler(0, 0, angle);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetRandomRotation(Axis axis, Vector2? xRange = null, Vector2? yRange = null, Vector2? zRange = null)",
                            Summary = "Returns a random rotation on the specified axes. Axis is a flags enum (X, Y, Z, XY, XZ, YZ, XYZ). Omitted range parameters default to 0–360.",
                            Code =
@"transform.rotation = Utils.GetRandomRotation(Axis.Y);             // random Y, 0-360
transform.rotation = Utils.GetRandomRotation(Axis.XYZ);            // fully random
transform.rotation = Utils.GetRandomRotation(
    Axis.XY,
    xRange: new Vector2(-30f, 30f),
    yRange: new Vector2(-30f, 30f));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetWaitForSeconds(float duration)",
                            Summary = "Returns a cached WaitForSeconds to avoid GC allocation inside coroutines.",
                            Code =
@"IEnumerator Reload()
{
    yield return Utils.GetWaitForSeconds(reloadTime);
    isReloaded = true;
}" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetWaitForSecondsRealtime(float duration)",
                            Summary = "Returns a cached WaitForSecondsRealtime (unscaled time) to avoid GC allocation.",
                            Code = @"yield return Utils.GetWaitForSecondsRealtime(2f);" },
                    }
                },
                // ── Editor Tools ─────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "Timer Tracker Window",
                    Namespace = "Window > TRnK Framework > Timer Tracker",
                    Summary = "Live inspector for all active Countdowns and Stopwatches. Progress bars, memory stats, pagination.",
                    Description = "Two tabs: Countdowns and Stopwatches. Each row shows source GameObject, component name, elapsed/remaining time, and a smooth progress bar. Stats bar displays IsAlive count, Capacity, Free Slots, and Slot Memory via Unsafe.SizeOf. Paginated at 20 items. Clears on exiting Play mode.",
                    Code =
@"// Open: Window > TRnK Framework > Timer Tracker

// Timers appear automatically when created via Countdown/Stopwatch:
var countdown = Countdown.Create(this, 10f).OnComplete(() => { });
countdown.Start();  // now visible in Timer Tracker",
                    Tags = new[] { "Editor", "Debug", "Timer" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "Scene Switcher (Toolbar)",
                    Namespace = "Edit > Project Settings > TRnK.Toolkit",
                    Summary = "Toolbar dropdown listing all Build Settings scenes. Right-click to set a Startup Scene for Play mode.",
                    Description = "Lists scenes by name; duplicates disambiguated with their paths. Right-click any entry to mark it as the Startup Scene — Play mode auto-loads it then restores the original on exit. Setting: Activate Loaded Additive On Select reactivates an already-loaded additive scene instead of reopening it.",
                    Code =
@"// Settings: Edit > Project Settings > TRnK.Toolkit
// Startup Scene Path
//   — auto-loaded when entering Play mode
// Activate Loaded Additive On Select
//   — reactivate an existing additive scene instead of reloading it",
                    Tags = new[] { "Editor", "Scene", "Toolbar" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "Time Scale Tool (Toolbar)",
                    Namespace = "Edit > Project Settings > TRnK.Toolkit",
                    Summary = "Toolbar slider (0–10) for adjusting Time.timeScale in real time. Auto-restores to 1.0 on exiting Play mode.",
                    Description = "Bidirectionally synced with Project Settings > Time Manager. Reset button snaps to 1.0. Upper bound configurable via Time Scale Max in TRnK.Toolkit settings (default 10).",
                    Code =
@"// No code needed. Slider appears in the Unity toolbar during Play mode.
// Settings: Edit > Project Settings > TRnK.Toolkit
//   Time Scale Max — upper bound of the slider (default 10)",
                    Tags = new[] { "Editor", "TimeScale", "Toolbar" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "Clear PlayerPrefs (Toolbar)",
                    Namespace = "Edit > Project Settings > TRnK.Toolkit",
                    Summary = "One-click toolbar button to clear all PlayerPrefs with a confirmation dialog.",
                    Description = "Shows a confirmation dialog before deletion. In Play mode: exits, clears PlayerPrefs, then optionally re-enters Play mode (controlled by Auto Re-enter Play After Clear setting).",
                    Code =
@"// No code needed. Toolbar button with confirmation dialog.
// Settings: Edit > Project Settings > TRnK.Toolkit
//   Auto Re-enter Play After Clear — default true",
                    Tags = new[] { "Editor", "PlayerPrefs", "Toolbar" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "Game Screenshot Tool",
                    Namespace = "Tools > TRnK Framework > Screenshot",
                    Summary = "In-editor screenshot capture. GameView or SpecificCamera mode. Supersize multiplier up to 4×.",
                    Description = "Capture modes: GameView (resolution × supersize) or SpecificCamera (offscreen — no Screen Space Overlay UI). Supersize 2× on 1080p → 4K output. Shortcut: Ctrl+Shift+K. Output: {SceneName}_{Width}x{Height}_{Timestamp}.png. Requires Play mode.",
                    Code =
@"// Menu: Tools > TRnK Framework > Screenshot > Open Settings
// Shortcut: Ctrl+Shift+K (quick capture)
// Supersize table (1080p GameView):
//   1x = 1920x1080
//   2x = 3840x2160  (4K)
//   3x = 5760x3240
//   4x = 7680x4320  (8K)",
                    Tags = new[] { "Editor", "Screenshot", "Capture" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "Setup Window",
                    Namespace = "Window > TRnK Framework > Setup",
                    Summary = "One-time project setup wizard: create standard folder structure and install pre-defined packages.",
                    Description = "Tabs: Folders (creates project folder tree from SetupFoldersSettings ScriptableObject) and Packages (installs a pre-defined list from SetupPackagesSettings). Both settings are serialized ScriptableObjects customizable per team.",
                    Code =
@"// Menu: Window > TRnK Framework > Setup
// Tabs:
//   Folders  — creates project folder structure
//   Packages — installs Unity packages from a pre-defined list
// Both tabs are driven by ScriptableObject settings assets.",
                    Tags = new[] { "Editor", "Setup", "Wizard" },
                    Category = DocCategory.EditorTools
                },
                new TRnKDocEntry
                {
                    Title = "TRnK.Toolkit Project Settings",
                    Namespace = "Edit > Project Settings > TRnK.Toolkit",
                    Summary = "Central settings for all TRnK.Toolkit editor tools. Auto-created at Assets/Plugins/TRnK/Toolkit/Editor/TRnKSettings.asset.",
                    Description = "Available settings: Startup Scene Path, Activate Loaded Additive On Select, Time Scale Max (default 10), Hide Toolbar, Auto Re-enter Play After Clear (default true). Persisted as a ScriptableObject asset.",
                    Code =
@"// Edit > Project Settings > TRnK.Toolkit
// Asset: Assets/Plugins/TRnK/Toolkit/Editor/TRnKSettings.asset
// Startup Scene Path         — auto-load on entering Play mode
// Time Scale Max             — toolbar slider upper bound (default 10)
// Hide Toolbar               — hide all TRnK.Toolkit toolbar elements
// Auto Re-enter After Clear  — re-enter Play after clearing prefs",
                    Tags = new[] { "Editor", "Settings", "Configuration" },
                    Category = DocCategory.EditorTools
                },

                // ── TRnK.Signal ────────────────────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "ISignal",
                    Namespace = "TRnK.Signal",
                    Summary = "Marker interface all signal structs must implement. Enforces struct constraint — null payload is impossible by design.",
                    Description = "Signals must be structs (readonly struct recommended for immutability). The subscribe and emit generics enforce `where T : struct, ISignal`, so signals are always stack-allocated value types.",
                    Code =
@"using TRnK.Signal;

public readonly struct PlayerDied : ISignal { }

public readonly struct PlayerHealthChanged : ISignal
{
    public readonly int NewHealth;
    public readonly int MaxHealth;

    public PlayerHealthChanged(int newHealth, int maxHealth)
    {
        NewHealth = newHealth;
        MaxHealth = maxHealth;
    }
}",
                    Tags = new[] { "Signal", "Struct", "Interface" },
                    Category = DocCategory.TRnKSignal
                },
                new TRnKDocEntry
                {
                    Title = "[OnSignal] + SignalHub.Bind",
                    Namespace = "TRnK.Signal",
                    Summary = "Attribute-based subscription. Decorate handler methods with [OnSignal] and call SignalHub.Bind(this) once — no manual wiring.",
                    Description = "TRnK.Signal discovers all [OnSignal]-decorated methods via reflection at bind time only (zero reflection on emit). Always pair Bind in OnEnable with Unbind in OnDisable. Forgetting Unbind leaks the delegate — the Memory Leaks tab in Signal Tracker will surface it.",
                    Code =
@"using TRnK.Signal;

public class UIHealthBar : MonoBehaviour
{
    private void OnEnable()  => SignalHub.Bind(this);
    private void OnDisable() => SignalHub.Unbind(this);

    [OnSignal]
    private void OnHealthChanged(PlayerHealthChanged s)
    {
        healthBar.fillAmount = (float)s.NewHealth / s.MaxHealth;
    }

    // Priority — higher runs first (default 0)
    [OnSignal(priority: 10)]
    private void OnHealthChangedEarly(PlayerHealthChanged s) { }
}",
                    Tags = new[] { "Subscribe", "Attribute", "Bind" },
                    Category = DocCategory.TRnKSignal,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalHub.Bind(MonoBehaviour owner)",
                            Summary = "Scans owner for all [OnSignal]-decorated methods and registers them. Call in OnEnable.",
                            Code =
@"private void OnEnable()  => SignalHub.Bind(this);
private void OnDisable() => SignalHub.Unbind(this);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalHub.Unbind(MonoBehaviour owner)",
                            Summary = "Unregisters all [OnSignal] handlers for this owner. Call in OnDisable to prevent memory leaks.",
                            Code = @"private void OnDisable() => SignalHub.Unbind(this);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalHub.IsBound(MonoBehaviour owner)",
                            Summary = "Returns true if the target currently has active [OnSignal] bindings registered via SignalHub.Bind.",
                            Code =
@"if (!SignalHub.IsBound(this))
    SignalHub.Bind(this);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "[OnSignal]",
                            Summary = "Attribute that marks a method as a signal handler. The parameter type determines which signal it receives.",
                            Code =
@"[OnSignal]
private void OnPlayerDied(PlayerDied s)
{
    ShowGameOverScreen();
}

// With priority — higher values run first (default 0)
[OnSignal(priority: 10)]
private void HandleEarlyResponse(PlayerDied s) { }" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "SignalBus.Emit / this.Emit",
                    Namespace = "TRnK.Signal",
                    Summary = "Emit a signal to all subscribers. Use this.Emit() from a MonoBehaviour to record the emitter in the Signal Tracker.",
                    Description = "Both overloads are equivalent in dispatch behavior. `this.Emit()` attaches the emitting MonoBehaviour as context for the Signal Tracker log. `SignalBus.Emit()` is usable from any non-MonoBehaviour context.",
                    Code =
@"using TRnK.Signal;

// From a MonoBehaviour — emitter recorded in Signal Tracker
this.Emit(new PlayerHealthChanged(health, maxHealth));

// From anywhere — no emitter context
SignalBus.Emit(new PlayerHealthChanged(health, maxHealth));",
                    Tags = new[] { "Emit", "Dispatch" },
                    Category = DocCategory.TRnKSignal,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "this.Emit<T>(T signal)",
                            Summary = "Emits a signal from a MonoBehaviour. The emitter is recorded in Signal Tracker.",
                            Code =
@"// From a MonoBehaviour — emitter visible in Signal Tracker
this.Emit(new PlayerHealthChanged(health, maxHealth));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalBus.Emit<T>(T signal)",
                            Summary = "Emits a signal from any context. No emitter is recorded.",
                            Code =
@"// From anywhere — no MonoBehaviour context
SignalBus.Emit(new PlayerHealthChanged(health, maxHealth));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "this.Emit<T>(T signal, ISignalFilter[] filters)",
                            Summary = "Emits with filters. Only subscribers whose owner passes all filters receive the signal.",
                            Code =
@"private ISignalFilter[] _filters;
private void Awake() => _filters = new ISignalFilter[] { new WithTag(""Player"") };

private void Update()
{
    if (detected)
        this.Emit(new EnemyDetected(target), _filters);
}" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Listen<T> / SignalReceiver",
                    Namespace = "TRnK.Signal",
                    Summary = "Manual subscription returning a SignalReceiver. Call Dispose() to unsubscribe. Use when you need conditional or lifetime-limited subscriptions.",
                    Description = "Listen<T>() returns a SignalReceiver. Call Dispose() to unsubscribe at any time — it is idempotent. SignalReceiver.IsActive is true until Dispose() is called. Abandoned receivers are auto-cleaned when the owner MonoBehaviour is destroyed, but explicit disposal is cleaner.",
                    Code =
@"using TRnK.Signal;

public class TemporaryListener : MonoBehaviour
{
    private SignalReceiver _receiver;

    private void OnEnable()
    {
        // Default priority (0)
        _receiver = this.Listen<GameStarted>(OnGameStarted);
        // With priority — higher runs first
        _receiver = this.Listen<GameStarted>(OnGameStarted, priority: 10);
    }

    private void OnDisable()
    {
        _receiver.Dispose();
    }

    private void OnGameStarted(GameStarted s) { }
}

// From anywhere
var rx = SignalBus.Listen<PlayerDied>(owner, OnPlayerDied);
var rxPri = SignalBus.Listen<PlayerDied>(owner, OnPlayerDied, priority: 5);
rx.Dispose(); // unsubscribe",
                    Tags = new[] { "Subscribe", "Listen", "Receiver", "Dispose" },
                    Category = DocCategory.TRnKSignal,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "this.Listen<T>(Action<T> callback)",
                            Summary = "Subscribes to signal T from a MonoBehaviour. Returns a SignalReceiver.",
                            Code =
@"private SignalReceiver _rx;

private void OnEnable()  => _rx = this.Listen<GameStarted>(OnGameStarted);
private void OnDisable() => _rx.Dispose();

private void OnGameStarted(GameStarted s) { }" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "this.Listen<T>(Action<T> callback, int priority)",
                            Summary = "Same as Listen<T> but sets dispatch priority. Higher values are invoked first. Default is 0.",
                            Code =
@"// Runs before default-priority subscribers
_rx = this.Listen<PlayerDied>(OnPlayerDied, priority: 10);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalBus.Listen<T>(MonoBehaviour owner, Action<T> callback)",
                            Summary = "Subscribes from any context. Owner is used for tracker display and leak detection.",
                            Code =
@"var rx = SignalBus.Listen<PlayerDied>(this, OnPlayerDied);
rx.Dispose(); // unsubscribe later" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalBus.Listen<T>(MonoBehaviour owner, Action<T> callback, int priority)",
                            Summary = "Same as SignalBus.Listen<T> but sets dispatch priority.",
                            Code =
@"var rx = SignalBus.Listen<PlayerDied>(this, OnPlayerDied, priority: 5);" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "receiver.IsActive",
                            Summary = "True while the receiver is live. Becomes false after Dispose() is called.",
                            Code =
@"if (_rx.IsActive)
    Debug.Log(""Still listening"");" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "receiver.SignalType",
                            Summary = "The System.Type of the signal this receiver is subscribed to.",
                            Code =
@"Debug.Log($""Subscribed to: {_rx.SignalType.Name}"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "receiver.Dispose()",
                            Summary = "Unsubscribes and marks the receiver inactive. Safe to call multiple times.",
                            Code =
@"_rx.Dispose(); // idempotent — safe to call again" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SignalBus.GetSubscriberCount<T>()",
                            Summary = "Returns the number of active subscribers for signal type T.",
                            Code =
@"int count = SignalBus.GetSubscriberCount<PlayerDied>();
Debug.Log($""{count} listeners registered for PlayerDied"");" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "ISignalFilter + Filtered Emit",
                    Namespace = "TRnK.Signal",
                    Summary = "Emitter-side filters restrict delivery to subscribers whose MonoBehaviour owner passes all provided filters.",
                    Description = "Three built-in filters: HasComponent<T>, InLayer, WithTag. Custom filters implement ISignalFilter.Evaluate(MonoBehaviour owner). For signals emitted every frame, pre-allocate the filter array in Awake to avoid params allocation.",
                    Code =
@"using TRnK.Signal;

// Fluent one-off
new EnemySpotted(target)
    .ConfigureFilters()
    .Require(new HasComponent<Rigidbody>())
    .Require(new WithTag(""Player""))
    .Emit();

// Inline (allocates params array)
this.Emit(new EnemySpotted(target), new InLayer(LayerMask.GetMask(""Enemy"")));

// Pre-allocated (no allocation on hot path)
private ISignalFilter[] _filters;
private void Awake() => _filters = new ISignalFilter[] { new WithTag(""Player"") };
private void Update()
{
    if (detected)
        SignalBus.Emit(new EnemyDetected(target), _filters);
}

// Custom filter
public sealed class TeamFilter : ISignalFilter
{
    private readonly int _teamId;
    public TeamFilter(int teamId) => _teamId = teamId;
    public bool Evaluate(MonoBehaviour owner)
        => owner.TryGetComponent<TeamMember>(out var m) && m.TeamId == _teamId;
}",
                    Tags = new[] { "Filter", "ISignalFilter", "HasComponent", "InLayer", "WithTag" },
                    Category = DocCategory.TRnKSignal,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "signal.ConfigureFilters()",
                            Summary = "Starts a fluent filter chain on the signal. Returns a FilteredEmitBuilder.",
                            Code =
@"new EnemySpotted(target)
    .ConfigureFilters()
    .Require(new HasComponent<Rigidbody>())
    .Emit();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "builder.Require(ISignalFilter filter)",
                            Summary = "Adds a filter to the builder. All filters must pass for a subscriber to receive the signal.",
                            Code =
@"signal.ConfigureFilters()
    .Require(new WithTag(""Player""))
    .Require(new InLayer(LayerMask.GetMask(""Characters"")))
    .Emit();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "builder.Emit()",
                            Summary = "Dispatches the signal with all configured filters applied.",
                            Code =
@"new DamageDealt(damage)
    .ConfigureFilters()
    .Require(new HasComponent<IDamageable>())
    .Emit();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "HasComponent<T>()",
                            Summary = "Built-in filter: subscriber's owner MonoBehaviour must have component T.",
                            Code =
@"this.Emit(new Signal(), new HasComponent<Shield>());" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "InLayer(LayerMask mask)",
                            Summary = "Built-in filter: subscriber's owner must be in the given LayerMask.",
                            Code =
@"this.Emit(new Signal(), new InLayer(LayerMask.GetMask(""Enemy"")));" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "WithTag(string tag)",
                            Summary = "Built-in filter: subscriber's owner must have the matching tag.",
                            Code =
@"this.Emit(new Signal(), new WithTag(""Player""));" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "Signal Tracker",
                    Namespace = "Window > TRnK Framework > Signal Tracker",
                    Summary = "Editor window with live subscription monitor, emit log, and memory leak detector.",
                    Description = "Three tabs:\n• Subscription Monitor — live subscriber table per signal type (GameObject, component, method, priority). Searchable.\n• Signal Log — emit history with emitter context, timestamp, payload fields, and applied filters.\n• Memory Leaks — MonoBehaviours that called SignalHub.Bind but were destroyed without Unbind. Cleared automatically on exiting Play Mode.",
                    Tags = new[] { "Editor", "Debug", "Tracker", "Memory" },
                    Category = DocCategory.TRnKSignal
                },

                // ── TRnK.Flow ──────────────────────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "StateBehaviour",
                    Namespace = "TRnK.Flow",
                    Summary = "MonoBehaviour-based FSM controller. Derive from it, create states, then declare transitions in Awake.",
                    Description = "StateBehaviour is the 'brain' — it owns transition predicates and state instances. Use GetTimeInCurrentState() for time-based transitions. API: IsInState<T>(), TryGetCurrentState<T>(out T), GetTimeInCurrentState().",
                    Code =
@"using TRnK.Flow;

public class EnemyController : StateBehaviour
{
    private EnemyIdleState _idle;
    private EnemyPatrolState _patrol;

    private void Awake()
    {
        _idle   = new EnemyIdleState(this);
        _patrol = new EnemyPatrolState(this);

        this.StartWith(_idle)
            .At(_idle,   _patrol, () => GetTimeInCurrentState() >= 2f)
            .At(_patrol, _idle,   () => GetTimeInCurrentState() >= 5f)
            .Any(_patrol, () => IsAlerted()); // any-state transition
    }
}",
                    Tags = new[] { "FSM", "StateMachine", "Controller" },
                    Category = DocCategory.TRnKFlow,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartWith(IState state)",
                            Summary = "Sets the initial state and activates it. Call once in Awake.",
                            Code =
@"private void Awake()
{
    _idle = new IdleState(this);
    this.StartWith(_idle);
}" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "At(IState from, IState to, Func<bool> predicate)",
                            Summary = "Registers a transition from one state to another when predicate returns true.",
                            Code =
@"this.StartWith(_idle)
    .At(_idle, _patrol, () => GetTimeInCurrentState() >= 2f)
    .At(_patrol, _idle, () => !enemyVisible);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Any(IState to, Func<bool> predicate)",
                            Summary = "Registers a global transition that fires from any state when predicate is true.",
                            Code =
@".Any(_dead, () => health <= 0); // triggers from idle, patrol, or any other state" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "IsInState<T>()",
                            Summary = "Returns true if the machine is currently in state T.",
                            Code =
@"if (IsInState<EnemyIdleState>())
    Debug.Log(""Enemy is idle"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "TryGetCurrentState<T>(out T state)",
                            Summary = "Tries to cast the current state to T. Returns false if current state is a different type.",
                            Code =
@"if (TryGetCurrentState<PatrolState>(out var patrol))
    patrol.SetWaypoint(nextPoint);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "GetTimeInCurrentState()",
                            Summary = "Returns elapsed seconds since the last state transition. Useful for timed transitions.",
                            Code =
@".At(_idle, _patrol, () => GetTimeInCurrentState() >= 3f)" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "BaseState<TContext>",
                    Namespace = "TRnK.Flow",
                    Summary = "Convenience base class for states. Provides protected _context, _gameObject, and _transform.",
                    Description = "Implement IState directly for pure C# states, or inherit BaseState<T> when you need convenient access to the controller and its GameObject. State logic only — no transition predicates here.",
                    Code =
@"using TRnK.Flow;

public sealed class EnemyIdleState : BaseState<EnemyController>
{
    public EnemyIdleState(EnemyController context) : base(context) { }

    public override void OnEnter() { /* start idle anim */ }

    public override void OnTick(float deltaTime)
    {
        // State behavior only — transitions live in the controller
    }

    public override void OnExit() { /* cleanup */ }
}",
                    Tags = new[] { "FSM", "State", "BaseState" },
                    Category = DocCategory.TRnKFlow,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "_context",
                            Summary = "Reference to the owning TContext controller. Available inside any state method.",
                            Code =
@"public override void OnTick(float deltaTime)
{
    _context.Animator.SetFloat(""Speed"", _context.Speed);
}" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "_gameObject / _transform",
                            Summary = "Shortcuts to _context.gameObject and _context.transform.",
                            Code =
@"public override void OnEnter()
{
    _transform.position = spawnPoint;
}" },
                        new DocMember { Kind = DocMemberKind.Callback, Signature = "OnEnter()",
                            Summary = "Called when this state becomes active. Set up animations, flags, subscriptions here.",
                            Code =
@"public override void OnEnter()
{
    _context.Animator.SetTrigger(""Idle"");
}" },
                        new DocMember { Kind = DocMemberKind.Callback, Signature = "OnTick(float deltaTime)",
                            Summary = "Called every frame while this state is active. State behavior only — no transition logic here.",
                            Code =
@"public override void OnTick(float deltaTime)
{
    _context.MoveTowardsTarget(deltaTime);
}" },
                        new DocMember { Kind = DocMemberKind.Callback, Signature = "OnExit()",
                            Summary = "Called when this state is deactivated. Clean up anything started in OnEnter.",
                            Code =
@"public override void OnExit()
{
    _context.Animator.ResetTrigger(""Idle"");
}" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "StateMachine (pure C#)",
                    Namespace = "TRnK.Flow",
                    Summary = "Standalone state machine, no MonoBehaviour required. Tick it manually in Update.",
                    Description = "Use StateMachine directly when you want FSM logic inside a plain C# class. Remember to call Tick(deltaTime) each frame. API: StartWith, At, Any, SetState, CurrentState, TimeInState, Is<T>(), Get<T>().",
                    Code =
@"using TRnK.Flow;

public class EnemyBrain : MonoBehaviour
{
    private StateMachine _sm;

    private void Awake()
    {
        _sm = new StateMachine();
        var idle   = new IdleState();
        var patrol = new PatrolState();

        _sm.StartWith(idle)
            .At(idle,   patrol, () => _sm.TimeInState >= 1f)
            .At(patrol, idle,   () => _sm.TimeInState >= 3f);
    }

    private void Update() => _sm?.Tick(Time.deltaTime);
}",
                    Tags = new[] { "FSM", "StateMachine", "PureCSharp" },
                    Category = DocCategory.TRnKFlow,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "CurrentState",
                            Summary = "The currently active IState instance.",
                            Code =
@"Debug.Log(_sm.CurrentState?.GetType().Name);" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "TimeInState",
                            Summary = "Elapsed seconds since the last state transition.",
                            Code =
@".At(idle, patrol, () => _sm.TimeInState >= 2f)" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "StartWith(IState state)",
                            Summary = "Sets the initial state. Call once before the first Tick.",
                            Code =
@"_sm = new StateMachine();
_sm.StartWith(idle);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "At(IState from, IState to, Func<bool> predicate)",
                            Summary = "Registers a transition from one state to another.",
                            Code =
@"_sm.At(idle, patrol, () => _sm.TimeInState >= 1f)
   .At(patrol, idle, () => _sm.TimeInState >= 3f);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Any(IState to, Func<bool> predicate)",
                            Summary = "Registers a global transition that fires from any state.",
                            Code =
@"_sm.Any(dead, () => hp <= 0);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "SetState(IState state)",
                            Summary = "Forces an immediate transition to the given state, bypassing predicates.",
                            Code =
@"_sm.SetState(stunned);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Tick(float deltaTime)",
                            Summary = "Evaluates all transitions and ticks the current state. Call every frame.",
                            Code =
@"private void Update() => _sm?.Tick(Time.deltaTime);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Is<T>()",
                            Summary = "Returns true if the current state is of type T.",
                            Code =
@"if (_sm.Is<PatrolState>())
    DrawDebugPath();" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "Get<T>()",
                            Summary = "Returns the current state cast to T, or null if the type doesn't match.",
                            Code =
@"var patrol = _sm.Get<PatrolState>();
patrol?.SetNextWaypoint(wp);" },
                    }
                },

                // ── TRnK.Serializer ────────────────────────────────────────────
                new TRnKDocEntry
                {
                    Title = "NSR.Save / NSR.Load",
                    Namespace = "TRnK.Serializer",
                    Summary = "Core save/load API. Writes to the configured storage backend (PlayerPrefs or JSON file). Load returns defaultValue when the key is missing.",
                    Description = "All public API lives on the static NSR class. Unity value types (Vector2/3, Quaternion, Color, Rect, Bounds, Transform snapshot, etc.) are serialized automatically via Newtonsoft.Json converters.",
                    Code =
@"using TRnK.Serializer;

// Save
NSR.Save(""playerName"", ""Neko"");
NSR.Save(""score"", 9001);
NSR.Save(""position"", transform.position);

// Load — second arg is the default if key is missing
string name = NSR.Load<string>(""playerName"", ""Unknown"");
int    score = NSR.Load<int>(""score"", 0);
Vector3 pos  = NSR.Load<Vector3>(""position"");

// Key checks & deletion
if (NSR.Exists(""highScore""))
    NSR.Delete(""highScore"");",
                    Tags = new[] { "Save", "Load", "Persistence" },
                    Category = DocCategory.TRnKSerializer,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Save<T>(string key, T value)",
                            Summary = "Saves value under key to the configured storage backend.",
                            Code =
@"NSR.Save(""playerName"", ""Neko"");
NSR.Save(""score"", 9001);
NSR.Save(""position"", transform.position);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Load<T>(string key, T defaultValue = default)",
                            Summary = "Loads value by key. Returns defaultValue when the key is missing.",
                            Code =
@"string name  = NSR.Load<string>(""playerName"", ""Unknown"");
int    score  = NSR.Load<int>(""score"", 0);
Vector3 pos   = NSR.Load<Vector3>(""position"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Exists(string key)",
                            Summary = "Returns true if the key has a saved value in the current storage backend.",
                            Code =
@"if (NSR.Exists(""highScore""))
    Debug.Log(""Previous high score found"");" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Delete(string key)",
                            Summary = "Removes the saved entry for key from the storage backend.",
                            Code =
@"NSR.Delete(""tempData""); // clean up after level complete" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "NSR.SaveAsync / NSR.LoadAsync",
                    Namespace = "TRnK.Serializer",
                    Summary = "Async variants of Save and Load. Returns Task / Task<T> — await in async methods.",
                    Description = "Async variants are useful when saving large or complex data structures to JSON files to avoid blocking the main thread. Both methods mirror the synchronous API exactly.",
                    Code =
@"using TRnK.Serializer;

await NSR.SaveAsync(""highScore"", 9999);
int hs = await NSR.LoadAsync<int>(""highScore"", 0);",
                    Tags = new[] { "Async", "Save", "Load" },
                    Category = DocCategory.TRnKSerializer,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.SaveAsync<T>(string key, T value)",
                            Summary = "Async save. Returns Task. Use to avoid blocking the main thread on large payloads.",
                            Code =
@"private async void SaveProgress()
{
    await NSR.SaveAsync(""progress"", progressData);
    Debug.Log(""Saved!"");
}" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.LoadAsync<T>(string key, T defaultValue = default)",
                            Summary = "Async load. Returns Task<T>. Mirrors NSR.Load<T> exactly.",
                            Code =
@"private async void LoadProgress()
{
    var data = await NSR.LoadAsync<ProgressData>(""progress"", new ProgressData());
}" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "NSR.Pack / NSR.Unpack",
                    Namespace = "TRnK.Serializer",
                    Summary = "Bundle multiple saved keys into a portable string for cloud sync or profile transfer. Unpack restores them.",
                    Description = "Pack serializes the specified keys into a single string. Unpack writes them back to the storage backend. Pass overwriteExisting: false to preserve existing values and only write missing keys.",
                    Code =
@"using TRnK.Serializer;

// Bundle keys into a portable snapshot string
string snapshot = NSR.Pack(""playerName"", ""score"", ""position"");

// Restore — overwrites existing by default
NSR.Unpack(snapshot);

// Restore — skip keys that already exist
NSR.Unpack(snapshot, overwriteExisting: false);",
                    Tags = new[] { "Migration", "Pack", "Unpack", "CloudSync" },
                    Category = DocCategory.TRnKSerializer,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Pack(params string[] keys)",
                            Summary = "Bundles specified saved keys into a portable snapshot string for cloud sync or transfer.",
                            Code =
@"string snapshot = NSR.Pack(""playerName"", ""score"", ""position"");
PlayerPrefs.SetString(""cloudBackup"", snapshot); // or send to server" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Unpack(string snapshot)",
                            Summary = "Restores all keys from a snapshot string. Overwrites existing values by default.",
                            Code =
@"string snapshot = PlayerPrefs.GetString(""cloudBackup"");
NSR.Unpack(snapshot);" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Unpack(string snapshot, bool overwriteExisting)",
                            Summary = "Restores keys from snapshot. Pass false to skip keys that already exist locally.",
                            Code =
@"// Only write keys that don't exist yet (first-time restore)
NSR.Unpack(snapshot, overwriteExisting: false);" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "NSR.Serialize / NSR.Deserialize",
                    Namespace = "TRnK.Serializer",
                    Summary = "Direct JSON serialization helpers — useful for networking, clipboard, or manual file handling.",
                    Description = "These methods bypass the storage backend entirely and work with raw JSON strings. PrettyPrintJson in SerializerSettings controls output formatting.",
                    Code =
@"using TRnK.Serializer;

string json   = NSR.Serialize(myData);
MyData data   = NSR.Deserialize<MyData>(json);

// Last save timestamps
DateTime utc   = NSR.LastSaveTimeUtc;
DateTime local = NSR.LastSaveTimeLocal;",
                    Tags = new[] { "JSON", "Serialize", "Deserialize" },
                    Category = DocCategory.TRnKSerializer,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Serialize<T>(T obj)",
                            Summary = "Serializes an object to a JSON string. Bypasses storage backend.",
                            Code =
@"string json = NSR.Serialize(myData);
SendToServer(json); // networking, clipboard, manual file" },
                        new DocMember { Kind = DocMemberKind.Method, Signature = "NSR.Deserialize<T>(string json)",
                            Summary = "Deserializes a JSON string to T. Bypasses storage backend.",
                            Code =
@"MyData data = NSR.Deserialize<MyData>(receivedJson);" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "NSR.LastSaveTimeUtc",
                            Summary = "UTC timestamp of the most recent Save or SaveAsync call.",
                            Code =
@"Debug.Log($""Last saved: {NSR.LastSaveTimeUtc}"");" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "NSR.LastSaveTimeLocal",
                            Summary = "Local time of the most recent Save or SaveAsync call.",
                            Code =
@"saveLabel.text = $""Saved at {NSR.LastSaveTimeLocal:HH:mm}"";
" },
                    }
                },
                new TRnKDocEntry
                {
                    Title = "SerializerSettings",
                    Namespace = "TRnK.Serializer",
                    Summary = "ScriptableObject config asset. Create once at Assets/Resources/SerializerSettings. Defaults apply if missing.",
                    Description = "Properties:\n• StorageOption — PlayerPrefs (default) or JsonFile\n• SaveDirectory — folder under Application.persistentDataPath (default: \"SaveData\")\n• UseEncryption — encrypt strings before writing (default: false)\n• EncryptionKey — key used when encryption is on (default: \"DefaultEncryptionKey\")\n• PrettyPrintJson — indented vs compact JSON (default: true)\n\nCreate via: Assets → Create → TRnK Framework → Serialize → Serializer Settings.",
                    Tags = new[] { "Settings", "Config", "Encryption", "Storage" },
                    Category = DocCategory.TRnKSerializer,
                    Members = new[]
                    {
                        new DocMember { Kind = DocMemberKind.Property, Signature = "StorageOption",
                            Summary = "PlayerPrefs (default) or JsonFile. Switch to JsonFile for larger data.",
                            Code =
@"// Set via Inspector on the SerializerSettings asset.
// PlayerPrefs — fast, platform-native key/value store
// JsonFile    — writes to Application.persistentDataPath/SaveDirectory" },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "SaveDirectory",
                            Summary = "Subfolder under Application.persistentDataPath for JsonFile storage. Default: \"SaveData\".",
                            Code =
@"// e.g. Application.persistentDataPath + ""/MyGame/""
// Set SaveDirectory = ""MyGame"" in the SerializerSettings asset." },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "UseEncryption",
                            Summary = "Whether to encrypt saved data. Default: false.",
                            Code =
@"// Enable in SerializerSettings asset to encrypt all Save/Load operations.
// Pair with a strong EncryptionKey." },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "EncryptionKey",
                            Summary = "Key used when UseEncryption is true. Default: \"DefaultEncryptionKey\" — change before shipping.",
                            Code =
@"// IMPORTANT: Change from the default before shipping.
// EncryptionKey is not secret from players with file access." },
                        new DocMember { Kind = DocMemberKind.Property, Signature = "PrettyPrintJson",
                            Summary = "Indented vs compact JSON output. Default: true. Disable for smaller file sizes.",
                            Code =
@"// PrettyPrintJson = true  → human-readable, easier to debug
// PrettyPrintJson = false → compact, smaller file size on JsonFile backend" },
                    }
                }
            };
        }
    }
}
#endif
