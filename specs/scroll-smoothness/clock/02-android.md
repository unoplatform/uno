# Frame clock on Android (Skia-on-Android)

Scope: can Uno obtain the *real* frame timestamp on Android — the vsync the frame is aligned to, or
the predicted presentation time of the frame being recorded — instead of the reconstructed grid in
`Compositor.GetFrameTimestamp`?

Answer up front: **yes, on two levels, and Uno already receives the cheaper one and throws it away in
two places.** The load-bearing epoch question resolves cleanly in our favour: `frameTimeNanos` and
.NET `Stopwatch.GetTimestamp()` are **the same clock, `CLOCK_MONOTONIC`**, verified from both source
trees *and* from the shipped `libSystem.Native.so` disassembly. No reconciliation, no offset
calibration, no drift model. A prior analysis claiming they mismatch is **refuted** — see §3.

---

## 1. What the Choreographer callback already receives and discards

There are exactly **two** `Choreographer.IFrameCallback` implementations in the tree. Both take
`frameTimeNanos` and drop it on the floor.

### 1.1 The new render pacer (private Looper thread, Vulkan path only)

`src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:97-100`

```csharp
private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
{
    public void DoFrame(long frameTimeNanos) => onFrame();
}
```

`onFrame` is `() => _vsync.Set()` (line 54). The parameter is never read. The callback is armed
one-shot per wait (`_handler.Post(() => _choreographer?.PostFrameCallback(_callback!))`, line 74) and
the render thread blocks on `_vsync.WaitOne(MaxWait)` (line 75).

This pacer is instantiated **only** by the Vulkan view:
`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:34` — `private readonly
ChoreographerFramePacer _pacer = new();`, waited at line 158 after `RenderFrame()`.

### 1.2 The dispatcher's animation-priority queue (main Looper — the record thread)

`src/Uno.UI.Dispatching/Native/NativeDispatcher.Android.cs:157-170`

```csharp
internal sealed class FrameCallbackImplementor : Java.Lang.Object, Choreographer.IFrameCallback
{
    private readonly Action _action;
    public FrameCallbackImplementor(Action action) { _action = action; }
    public void DoFrame(long frameTimeNanos) { _action(); }
}
```

Same story: parameter ignored. A `Choreographer` instance already lives on the **main Looper**
(`NativeDispatcher.Android.cs:36`, `_choreographer = Choreographer.Instance;`) and is posted to from
`RunAnimation` (line 56) and `QueueOperations` (line 122).

This matters more than it looks: **the main Looper is where the record happens**, and Uno already
holds a live main-thread `Choreographer`. Getting a vsync timestamp onto the record thread requires
*no new thread, no new Looper, no JNI plumbing that isn't already there*.

### 1.3 What is NOT Choreographer-driven today

The record is **not** posted through the Choreographer. It goes through a plain `Handler`:

- `CompositionTarget.RenderScheduling.skia.cs:172` — `NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback);`
- → `NativeDispatcher.cs:261` — `EnqueueNative(NativeDispatcherPriority.High);`
- → `NativeDispatcher.Android.cs:40-43` — `_handler.Post(_implementor);` where `_handler = new Handler(Looper.MainLooper)` (line 33).

`Handler.post` runs the message whenever the main `MessageQueue` gets to it. That is precisely the
unpaced-record wobble the estimator in `Compositor.GetFrameTimestamp` was built to hide
(`src/Uno.UI.Composition/Composition/Compositor.skia.cs:244-290`).

### 1.4 The OpenGL path has no pacer and no Choreographer at all

`UnoSKCanvasView` (`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKCanvasView.cs:25`) is a
`GLSurfaceView` with `RenderMode = Rendermode.WhenDirty` (line 53) and `RequestRender()` (line 65).
There is no `ChoreographerFramePacer` in that file; pacing is whatever back-pressure
`eglSwapBuffers` applies. Vulkan is the default
(`FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = true`, `src/Uno.UI/FeatureConfiguration.cs:672`;
selection at `src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs:297-320`, falling back to GL when
`PackageManager.FeatureVulkanHardwareLevel` is missing or construction throws).

**Consequence:** any pacer-hosted timestamp source covers the Vulkan path only. A main-thread
Choreographer source (§6, Option A) covers **both** paths, because it does not live in the pacer.

---

## 2. The three kinds of timestamp Android can give us

| Kind | Android API | Value | Usable to drive a curve? |
|---|---|---|---|
| **(a) vsync / frame-start** | `Choreographer.FrameCallback.doFrame(long frameTimeNanos)` — API 16+ (`postFrameCallback`) | The vsync the frame is aligned to, already snapped to the grid by the framework (§2.1) | **Yes** |
| **(b) predicted presentation** | `Choreographer.postVsyncCallback(VsyncCallback)` → `FrameData.PreferredFrameTimeline.ExpectedPresentationTimeNanos` — **API 33+** | When SurfaceFlinger expects this frame on screen | **Yes — this is the genuine predicted present time** |
| **(c) measured presentation** | `VK_GOOGLE_display_timing` (`vkGetPastPresentationTimingGOOGLE`), `FrameMetrics`, `SurfaceControl.TransactionCommittedListener` | When a *past* frame actually landed | **No** — after the fact |

### 2.1 `frameTimeNanos` is already a snapped grid, not a raw wake time

AOSP `frameworks/base/core/java/android/view/Choreographer.java`
(`android15-release`, `doFrame(long frameTimeNanos, int frame, DisplayEventReceiver.VsyncEventData)`,
~lines 1091-1123):

```java
long intendedFrameTimeNanos = frameTimeNanos;
startNanos = System.nanoTime();
final long jitterNanos = startNanos - frameTimeNanos;
if (jitterNanos >= frameIntervalNanos) {
    frameTimeNanos = startNanos;
    ...
    long lastFrameOffset = jitterNanos % frameIntervalNanos;
    frameTimeNanos = frameTimeNanos - lastFrameOffset;
    final long skippedFrames = jitterNanos / frameIntervalNanos;
```

This is structurally the same algorithm as `Compositor.GetFrameTimestamp` — advance on a grid, slip by
whole periods when a frame is missed — except the framework knows `frameIntervalNanos` **exactly**
(it comes from the display's real vsync period, not a median of 32 noisy samples) and the base value
is a genuine hardware vsync from `DisplayEventReceiver`, not a re-read of the clock at an arbitrary
point in a dispatcher tick.

`FrameCallback.doFrame` javadoc (~lines 1045-1053):

> "The time in nanoseconds when the frame started being rendered, in the `System.nanoTime()`
> timebase. Divide this value by `1000000` to convert it to the `SystemClock.uptimeMillis()` time
> base."

**So (a) alone already removes the entire class of error the estimator exists to hide.** The
estimator is an approximation of a number the OS hands us for free.

### 2.2 (b) — the predicted present time, API 33+

Verified in the **binding surface**, not just docs. Reading `[SupportedOSPlatform]` off
`Mono.Android.dll` from `Microsoft.Android.Ref.36/36.1.53/ref/net10.0` via `MetadataLoadContext`:

```
== Android.Views.Choreographer
   Method PostVsyncCallback:   SupportedOSPlatformAttribute("android33.0")
   Method RemoveVsyncCallback: SupportedOSPlatformAttribute("android33.0")
== Android.Views.Choreographer+FrameData
   [type]                       SupportedOSPlatformAttribute("android33.0")
   Property FrameTimeNanos:     SupportedOSPlatformAttribute("android33.0")
   Property PreferredFrameTimeline: SupportedOSPlatformAttribute("android33.0")
   Method GetFrameTimelines:    SupportedOSPlatformAttribute("android33.0")
== Android.Views.Choreographer+FrameTimeline
   Property DeadlineNanos:                  SupportedOSPlatformAttribute("android33.0")
   Property ExpectedPresentationTimeNanos:  SupportedOSPlatformAttribute("android33.0")
   Property VsyncId:                        SupportedOSPlatformAttribute("android33.0")
== Android.Views.Choreographer+IVsyncCallback
   Method OnVsync: SupportedOSPlatformAttribute("android33.0")
```

Managed API shape (all bound, no JNI needed):

```csharp
choreographer.PostVsyncCallback(vsyncCallback);   // IVsyncCallback
void OnVsync(Choreographer.FrameData data);
//   data.FrameTimeNanos                                  -> (a), System.nanoTime timebase
//   data.PreferredFrameTimeline.ExpectedPresentationTimeNanos -> (b)
//   data.PreferredFrameTimeline.DeadlineNanos            -> when we must be done by
//   data.PreferredFrameTimeline.VsyncId                  -> SF correlation id
//   data.GetFrameTimelines()                             -> the alternatives (latency vs. safety)
```

AOSP javadoc for `FrameTimeline.getExpectedPresentationTimeNanos()` (~line 1241):

> "The time in `System.nanoTime()` timebase which this frame is expected to be presented."

`getVsyncId()`:

> "The id that corresponds to this frame timeline, used to correlate a frame produced by HWUI with
> the timeline data stored in Surface Flinger."

**All FrameData/FrameTimeline accessors are callback-scoped** — AOSP guards them with
`if (!mInCallback) throw new IllegalStateException(...)`. The values must be **copied to primitives
inside `OnVsync`**; you cannot stash the `FrameData` object and read it later. This is a real
correctness constraint on any implementation.

Non-`postVsyncCallback` alternative on the same API level: `Choreographer.getFrameTime()` /
`getLastFrameTimeNanos()` are **not** in the public binding — grepping every public
`Android.Views.Choreographer.*` member in `Mono.Android.xml` yields only `PostFrameCallback`,
`PostFrameCallbackDelayed`, `PostVsyncCallback`, `RemoveFrameCallback`, `RemoveVsyncCallback`,
`Instance`. They are `@UnsupportedAppUsage` in AOSP. **The callback is the only door.**

### 2.3 NDK equivalents (for completeness — not needed)

From `developer.android.com/ndk/reference/group/choreographer`:

| Function | API |
|---|---|
| `AChoreographer_postVsyncCallback` | 33 |
| `AChoreographerFrameCallbackData_getFrameTimeNanos` | 33 |
| `AChoreographerFrameCallbackData_getPreferredFrameTimelineIndex` | 33 |
| `AChoreographerFrameCallbackData_getFrameTimelineExpectedPresentationTimeNanos` | 33 |
| `AChoreographerFrameCallbackData_getFrameTimelineVsyncId` | 33 |
| `AChoreographer_postFrameCallback64` | 29 |
| `AChoreographer_postFrameCallback` | 24, **deprecated in 29** |

`AChoreographer_postExtendedFrameCallback` is **not** in the current NDK reference index. It existed
transiently during the API 33 development cycle and was renamed to `AChoreographer_postVsyncCallback`
before release. **Do not target it** — mark the name as historical.
(*The rename history itself is* **UNVERIFIED** *— what is verified is that the current NDK reference
does not list it and lists `AChoreographer_postVsyncCallback` at API 33.*)

There is no reason to go to the NDK: the managed bindings exist and Uno already lives in Java-land on
this thread.

---

## 3. THE LOAD-BEARING QUESTION: is `frameTimeNanos` on the same epoch as `Stopwatch.GetTimestamp()`?

**Yes. Both are `clock_gettime(CLOCK_MONOTONIC)` in nanoseconds. Verified end-to-end.**

### 3.1 Android side — `System.nanoTime()` → `CLOCK_MONOTONIC`

AOSP `libcore/ojluni/src/main/native/System.c` (main branch):

```c
static jlong System_nanoTime() {
  struct timespec now;
  clock_gettime(CLOCK_MONOTONIC, &now);
  return now.tv_sec * 1000000000LL + now.tv_nsec;
}
```

Corroborated by ART's own `NanoTime()`, AOSP `art/libartbase/base/time_utils.cc` (main branch):

```cpp
timespec now;
clock_gettime(CLOCK_MONOTONIC, &now);
return static_cast<uint64_t>(now.tv_sec) * UINT64_C(1000000000) + now.tv_nsec;
```

And `Choreographer.FrameCallback.doFrame` is documented as being in exactly that timebase (§2.1).
The vsync timestamps that feed it come from `DisplayEventReceiver`/SurfaceFlinger, which publishes on
`CLOCK_MONOTONIC`.

### 3.2 .NET side — `Stopwatch.GetTimestamp()` → `CLOCK_MONOTONIC`

Source chain, `dotnet/runtime` tag **`v10.0.0`**:

1. `src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs`
   ```csharp
   public static long GetTimestamp() => Interop.Sys.GetTimestamp();

   private static long GetFrequency()
   {
       const long SecondsToNanoSeconds = 1000000000;
       return SecondsToNanoSeconds;
   }
   ```
   → **`Stopwatch.Frequency == 1_000_000_000` on Android. The unit is the nanosecond.**

2. `src/native/libs/System.Native/pal_time.c:99-102`
   ```c
   int64_t SystemNative_GetTimestamp(void)
   {
       return minipal_hires_ticks();
   }
   ```

3. `src/native/minipal/time.c` (non-Windows branch)
   ```c
   int64_t minipal_hires_tick_frequency(void) { return tccSecondsToNanoSeconds; }  // 10^9

   int64_t minipal_hires_ticks(void)
   {
   #if HAVE_CLOCK_GETTIME_NSEC_NP                       // Darwin only
       return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
   #elif HAVE_CLOCK_MONOTONIC
       struct timespec ts;
       int result = clock_gettime(CLOCK_MONOTONIC, &ts);
       ...
       return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToNanoSeconds)) + (int64_t)(ts.tv_nsec);
   #else
   #  error ...
   #endif
   }
   ```
   `clock_gettime_nsec_np` is an Apple (`_np`) API; bionic does not provide it, so Android takes the
   `CLOCK_MONOTONIC` branch. **`CLOCK_MONOTONIC_RAW` and `CLOCK_BOOTTIME` do not appear anywhere in
   this path.**

### 3.3 Binary confirmation on the *shipped* Android runtime

Source reading is not enough for a claim this load-bearing, so this was checked against the actual
artifact that runs on device:
`C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64\10.0.8\runtimes\android-arm64\native\`.

- `System.Private.CoreLib.dll` contains the metadata strings `SystemNative_GetTimestamp` and
  `libSystem.Native` — i.e. the managed `Stopwatch` really does P/Invoke into System.Native on this
  runtime flavour.
- `libSystem.Native.so` exports `SystemNative_GetTimestamp` (`.dynsym`, vaddr `0x12764`) and imports
  `clock_gettime` (`.rela.plt` entry 107 → GOT `0x1ca40`).
- Disassembling `SystemNative_GetTimestamp`: a single `b` (tail call), `0x14001138` → target
  `0x16C44` (the inlined-away-but-not-really `minipal_hires_ticks`). At `0x16C44`:

  ```
  0x16c5c: 52800020   movz w0, #1          ; clock id -> CLOCK_MONOTONIC (== 1 on Linux/bionic UAPI)
  0x16c68: 940004ae   bl   0x17f20         ; PLT slot for clock_gettime
                                            ; (.plt 0x17850 + 32-byte AArch64 header + 107*16 = 0x17f20)
  0x16c80: 5299400a   movz w10, #0xCA00
  0x16c84: 72a7734a   movk w10, #0x3B9A, lsl #16    ; w10 = 0x3B9ACA00 = 1_000_000_000
  0x16c88: 9b0a2500   madd x0, x8, x10, x9          ; tv_sec * 1e9 + tv_nsec
  ```

  **`clock_gettime(CLOCK_MONOTONIC, …)`, result in nanoseconds. Exactly what the source says.**

*(The CoreCLR-on-Android and NativeAOT-on-Android flavours were not disassembled — those packs do
not carry their own `libSystem.Native` locally. They build from the same `src/native/minipal/time.c`
and the same `Stopwatch.Unix.cs`, so the conclusion carries; flag as* **UNVERIFIED at the binary
level for CoreCLR/NativeAOT-on-Android** *if that distinction ever matters.)*

### 3.4 The conversion is exact and trivial

`src/Uno.UI.Composition/Composition/Compositor.cs:33,38`

```csharp
private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));
```

On Android `Stopwatch.Frequency == 1e9`, so `s_tickFrequency == 1e7 / 1e9 == 0.01` and

```
TimestampInTicks == nanoseconds / 100      (exactly — TimeSpan ticks are 100 ns)
```

Therefore a Choreographer timestamp converts to the compositor's tick domain with:

```csharp
long ticks = frameTimeNanos / 100;   // same epoch, same origin, no offset, no calibration
```

**This is a division, not a reconciliation.** Any implementation that computes an offset between
`frameTimeNanos` and `Stopwatch.GetTimestamp()` is compensating for a difference that does not exist,
and will bake in whatever noise the sampling pair happened to have.

### 3.5 Refuting the prior "they mismatch" claim

The claim is false as stated. The plausible origins of the confusion, and why each is not a mismatch:

- **`CLOCK_BOOTTIME` vs `CLOCK_MONOTONIC` (deep suspend).** `CLOCK_MONOTONIC` does not advance while
  the device is in suspend; `CLOCK_BOOTTIME` does. **Both** sides here use `CLOCK_MONOTONIC`, so they
  freeze and resume *together*. It is not a divergence between the two clocks — it is a shared
  property. (It does mean a wall-clock gap can appear across a suspend; the estimator's
  whole-period-slip path already covers gaps, and any replacement needs the same staleness guard.)
- **Apple confusion.** On Darwin, `minipal_hires_ticks` uses `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`
  — a genuinely different clock from `mach_absolute_time`-derived host timebases and from
  `CLOCK_MONOTONIC`. That is an **iOS/macOS** concern (see the Apple note in this series), not
  Android. Carrying it over to Android is the likely source of the claim.
- **`Stopwatch.Frequency` assumed to be 10 MHz.** It is `1e9` on Unix, not `1e7`. Code that assumes
  `Stopwatch.GetTimestamp()` is already in `TimeSpan` ticks *will* be off by 100×. That is a units
  bug, not an epoch mismatch — and `Compositor.TimestampInTicks` already handles it correctly
  (`Compositor.cs:33-38`, with the comment "s_tickFrequency is likely 1 on Windows, but not on Linux").

---

## 4. minSdk — what can Uno actually call?

- **.NET for Android default:** `AndroidMinimumSupportedApiLevel = 21`
  (`Microsoft.Android.Sdk.Windows/36.1.53/targets/Microsoft.Android.Sdk.SupportedPlatforms.targets:13`),
  flowed into `SupportedOSPlatformVersion` when unset
  (`Microsoft.Android.Sdk.DefaultProperties.targets:37-38`).
- **Uno's own Android Skia runtime project sets no `SupportedOSPlatformVersion`**
  (`src/Uno.UI.Runtime.Skia.Android/Uno.UI.Runtime.Skia.Android.csproj` — `TargetFrameworks` only), so
  it compiles at the SDK default of **21**.
- **SamplesApp (Skia) manifest:** `minSdkVersion="24" targetSdkVersion="36"`
  (`src/SamplesApp/SamplesApp.Skia.netcoremobile/Android/AndroidManifest.xml`).

So:

| Source | Needs | Guard |
|---|---|---|
| (a) `PostFrameCallback` / `doFrame(frameTimeNanos)` | API 16 | **none — always available**, and already wired |
| (b) `PostVsyncCallback` / `ExpectedPresentationTimeNanos` | **API 33** | `OperatingSystem.IsAndroidVersionAtLeast(33)` (or `Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu`, matching the existing `Build.VERSION.SdkInt` style at `UnoSKVulkanView.cs:49,278`) |

Building against API 33+ members from a library whose `SupportedOSPlatformVersion` is 21 produces
CA1416 unless guarded — the guard is mandatory, not optional. Uno's precedent for exactly this is
`LayoutProvider.cs:78` (`osVersion >= BuildVersionCodes.R`).

**(a) is free and covers 100% of devices. (b) is a refinement for API 33+.** Ship (a) first.

---

## 5. The threading problem, stated precisely

The pacer's callback "fires on a private Looper thread after present" — true, and it needs unpacking,
because the answer to "can it reach the *next* record" is **yes, with essentially zero added
latency**, but only because of where in the loop the wake lands.

### 5.1 The actual per-iteration ordering (Vulkan path)

`UnoSKVulkanView.RenderLoop`, `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:143-159`:

```
iteration k, on "UnoVulkanRenderThread":
  146  _renderEvent.Wait(100ms)
  153  RenderFrame()
         -> VulkanContext.RenderFrame(cb)
         -> cb: CompositionTarget.OnNativePlatformFrameRequested(canvas, resize)   [line 212]
              -> RenderScheduling.skia.cs:172  NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)
                                               // Handler.post to the MAIN looper -> record R_k
              -> RenderScheduling.skia.cs:175  Draw(canvas, resize)
                                               // replays _lastRenderedFrame == most recent completed record
         -> vkQueuePresentKHR                  (MAILBOX, returns immediately)
  158  _pacer.WaitForNextFrame()               // arms Choreographer, blocks until V_k
```

`Draw` only *replays* — the record is done on the main thread by
`CompositionTarget.Rendering.skia.cs:110-198` (`Render()` → `SkiaRenderHelper.RecordPictureAndReturnPath`
→ `Compositor.RenderRootVisual` → **`FrameStarting`** at `Compositor.skia.cs:308-316`).

So the record is one present behind the replay, and — crucially — **the record for iteration `k+1` is
enqueued immediately after the wait for `V_k` returns.** The wake and the next record enqueue are
adjacent in the loop:

```
V_k fires on "UnoVsyncPacer"  →  _vsync.Set()  →  render thread resumes at loop top
                              →  RenderFrame() →  EnqueueRender(R_{k+1})  →  main thread records R_{k+1}
```

**Therefore: a value captured in `DoFrame` at `V_k` is available to record `R_{k+1}`, which is the very
next record.** No skipped frame, no extra vsync of latency. The handoff is a plain
`Volatile.Write` on the pacer thread / `Volatile.Read` on the main thread.

### 5.2 Where in the pipeline the value must land

`Compositor.RenderRootVisual` currently does
(`src/Uno.UI.Composition/Composition/Compositor.skia.cs:308-316`):

```csharp
var frameTimestamp = GetFrameTimestamp(TimestampInTicks);
CurrentFrameTimestampInTicks = frameTimestamp;
frameStarting(frameTimestamp);
```

The replacement is to prefer a platform-supplied timestamp over the reconstruction. `Uno.UI.Composition`
is the generic Skia assembly and cannot reference `Android.Views`, but
`src/Uno.UI.Composition/AssemblyInfo.cs:19` already grants
`[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]`, and `UnoSKVulkanView.cs:57` already
reaches into `Compositor.GetSharedCompositor().IsSoftwareRenderer`. So an internal
`Compositor.SetPlatformFrameTimestamp(long ticks, long periodTicks)` (or similar) pushed from the
Android runtime project needs **no new extensibility mechanism**.

Semantics to preserve regardless of source: one timestamp for the whole frame
(`Compositor.skia.cs:310-311`), and `FrameIntervalInTicks` (`Compositor.skia.cs:220-222`) should
switch from the median-of-32 estimate to the real period, which both (a) and (b) hand us directly
(consecutive `frameTimeNanos` deltas, or `ExpectedPresentationTimeNanos - FrameTimeNanos`).

### 5.3 What "predicted present" means for *our* frame, and why the offset barely matters

`ExpectedPresentationTimeNanos` reported at `V_k` describes the frame a normal HWUI client would
submit in response to `V_k` — i.e. roughly `V_k + 1` period. Uno's record `R_{k+1}` is replayed and
presented in iteration `k+2`, so its true screen time is roughly `V_k + 2` periods.

For a fling this is a **constant offset**, and a constant offset in `x(t)` is invisible: what the eye
sees is the *spacing* between consecutive samples, and that is exactly one refresh period either way.
The offset only matters if you want the curve's absolute time to line up with finger position at
handoff from drag to inertia. If that alignment is wanted, add a documented constant
(`+2 * period`) — do **not** try to measure it, because measuring it re-introduces the jitter we are
eliminating.

Where `ExpectedPresentationTimeNanos` genuinely beats raw `frameTimeNanos` is **variable refresh
rate**: on an ARR/LTPO panel the period changes, and `expectedPresentationTimeNanos` reflects
SurfaceFlinger's actual schedule while a `frameTimeNanos` delta is only correct after the fact.

---

## 6. Implementation options, ranked

### Option A — main-thread Choreographer, `frameTimeNanos` only *(recommended first step)*

Post a `Choreographer.IFrameCallback` on the **main** Looper (`Choreographer.Instance` is already
constructed there, `NativeDispatcher.Android.cs:36`), keep it armed while
`Compositor.IsAnimating` (`Compositor.skia.cs:43`) is true, and store the latest `frameTimeNanos / 100`
into the compositor. `RenderRootVisual` uses it when fresh.

- Works on **API 21+**, i.e. everywhere, no guard.
- Works on **both** the Vulkan and the OpenGL render views, because it does not live in the pacer.
- Gives (a): a framework-snapped vsync grid — strictly better than the median-of-32 reconstruction.
- Costs: one `Choreographer` callback per frame while animating (Uno already does exactly this for
  `RunAnimation`), plus a staleness guard so an idle gap falls back rather than replaying an old grid.
- Risk: the callback runs at `CALLBACK_ANIMATION` on the main Looper; the record runs from a
  `Handler.post` at `CALLBACK_TRAVERSAL`-ish timing. Ordering between the two within a frame is
  **UNVERIFIED** and must be measured — if the record can beat the callback, the record consumes the
  *previous* frame's `frameTimeNanos`, which is still on the grid but one period behind (constant
  offset, harmless) as long as it does not sometimes-yes-sometimes-no. **This is the one thing that
  must be validated on device.**

### Option B — pacer-thread `PostVsyncCallback`, API 33+, layered on A

In `ChoreographerFramePacer`, replace `PostFrameCallback` with `PostVsyncCallback` when
`OperatingSystem.IsAndroidVersionAtLeast(33)`, and in `OnVsync(FrameData data)` copy out
`data.FrameTimeNanos`, `data.PreferredFrameTimeline.ExpectedPresentationTimeNanos` and
`.DeadlineNanos` **to `long` locals before returning** (§2.2 — the accessors throw outside the
callback), then publish them. Per §5.1 the value lands on the immediately-following record.

- Gives (b): a true predicted presentation time, correct under variable refresh rate.
- Vulkan path only; falls back to Option A elsewhere.
- Bonus: `DeadlineNanos` gives a real budget signal, and `VsyncId` allows correlating with
  SurfaceFlinger traces during investigation.

### Option C — drive the record *from* the vsync callback

Stop posting the record via `Handler.post` (`NativeDispatcher.Android.cs:42` for
`NativeDispatcherPriority.High`) and instead let the vsync callback both carry the timestamp and kick
`EnqueueRenderCallback`. This removes the wobble at its source instead of measuring around it.

- Architecturally the right answer; matches what mature stacks do (the record *is* the frame callback).
- Much larger blast radius: `EnqueueNative` is shared by all dispatcher priorities and by non-Skia
  Android, and `MaxRenderSpan`/`_animationQueue` semantics would need rethinking. Not a first step.

### What not to do

- Do **not** compute a `frameTimeNanos` ↔ `Stopwatch` offset (§3.4).
- Do **not** use `SystemClock.uptimeMillis()` as the bridge — it is millisecond-resolution, which is
  the same order as the error being removed.
- Do **not** reach for (c). `VK_GOOGLE_display_timing`'s `vkGetPastPresentationTimingGOOGLE` reports
  *past* presents; it can validate a fix but cannot drive a curve. It is also unreachable today: the
  device is created with a hard-coded extension list —
  `src/Uno.UI/Vulkan/Interop/VulkanDevice.Create.skia.cs:72-78` builds a merged list and then
  immediately overwrites it with `enabledExtensions = new[] { VK_KHR_swapchain };`, so **no optional
  device extension is ever enabled**. (Noted as an observation; out of scope here.)

---

## 7. Adjacent APIs asked about

- **`Display.getRefreshRate()`** — API 1, bound as `Android.Views.Display.RefreshRate`. Not used
  anywhere in the tree (`grep RefreshRate` over `src/Uno.UI.Runtime.Skia.Android/` and
  `src/Uno.UWP/Graphics/Display/DisplayInformation.Android.cs`: no hits). It is a *nominal* rate and
  is famously unreliable on multi-mode panels; `Display.Mode.RefreshRate` (API 23) is better, and
  `Display.getSuggestedFrameRate` is API 36. **None of these are needed** — consecutive
  `frameTimeNanos` deltas give the real period, and (b) gives it authoritatively.
- **`Surface.setFrameRate`** — bound at `android30.0` (3-arg) and `android31.0` (with
  `changeFrameRateStrategy`), plus `Surface.ClearFrameRate` at `android34.0`. This is an *output*: it
  tells the platform what cadence we intend, letting SurfaceFlinger pick a matching display mode. It
  does not report time. Worth a separate look for battery/ARR behaviour, but it is not a clock source.
  Uno has a `FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate` flag
  (`src/Uno.UI/FeatureConfiguration.cs:125`) but it is consumed only by Win32
  (`src/Uno.UI.Runtime.Skia.Win32/Graphics/Display/Win32WindowWrapper.DisplayInformation.cs:53`).
- **`VK_GOOGLE_display_timing`** — category (c) plus a *future* hint (`VkPresentTimeGOOGLE` lets you
  request a desired present time). Blocked today by the extension list above; and the swapchain is
  created `VK_PRESENT_MODE_MAILBOX_KHR` when available
  (`src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:103-109`), which is exactly why the pacer had to
  exist. Not a path to a record-time clock.

---

## 8. Bottom line

1. **Uno already receives a real vsync timestamp twice per frame and discards it twice** —
   `ChoreographerFramePacer.cs:99` and `NativeDispatcher.Android.cs:166-169`.
2. **`frameTimeNanos` and `Stopwatch.GetTimestamp()` are the same clock** (`CLOCK_MONOTONIC`,
   nanoseconds), proven from AOSP source, dotnet/runtime `v10.0.0` source, *and* the shipped
   `libSystem.Native.so` disassembly. Conversion is `frameTimeNanos / 100` into the compositor's tick
   domain. The prior mismatch claim is refuted.
3. **`frameTimeNanos` is already grid-snapped by the framework** using the display's true period —
   it is a strictly better version of what `Compositor.GetFrameTimestamp` reconstructs.
4. **A genuine predicted presentation time exists** (`FrameData.PreferredFrameTimeline.
   ExpectedPresentationTimeNanos`) from **API 33**, fully bound in `Mono.Android`, callback-scoped.
5. **The pacer's post-present wake is not a blocker** — it lands immediately before the next record
   enqueue, so the value reaches the next record with no added latency.
6. **Smallest correct first step: Option A** — a main-Looper Choreographer frame callback feeding the
   compositor, API 21+, both render paths. Then layer Option B for API 33+ Vulkan.

### Verification status

| Claim | Status |
|---|---|
| Uno's two `DoFrame` sites discard `frameTimeNanos` | **Verified** (source read) |
| Record is posted via `Handler.post`, not Choreographer | **Verified** (source read) |
| `System.nanoTime()` = `clock_gettime(CLOCK_MONOTONIC)` | **Verified** (AOSP libcore + ART source) |
| `Stopwatch.GetTimestamp()` on Android = `clock_gettime(CLOCK_MONOTONIC)`, ns | **Verified** (runtime source + arm64 disassembly of shipped `libSystem.Native.so`) |
| `Stopwatch.Frequency == 1e9` on Android | **Verified** (`Stopwatch.Unix.cs`, `minipal_hires_tick_frequency`) |
| Same epoch, no offset needed | **Verified** (follows from the two above) |
| `PostVsyncCallback` / `ExpectedPresentationTimeNanos` = API 33 | **Verified** (`[SupportedOSPlatform("android33.0")]` read from `Mono.Android.dll`) |
| Choreographer re-snaps `frameTimeNanos` to the vsync grid | **Verified** (AOSP `Choreographer.java`, `android15-release`; exact line numbers approximate) |
| `getLastFrameTimeNanos` not publicly bound | **Verified** (full member enumeration of `Mono.Android.xml`) |
| Uno Android SDK default minSdk = 21; SamplesApp = 24 | **Verified** (SDK targets file + manifest) |
| Vsync wake at `V_k` reaches record `R_{k+1}` | **Verified by code reading** of the loop ordering; **not** verified at runtime |
| Ordering of a main-thread `CALLBACK_ANIMATION` callback vs. the `Handler.post` record within one frame | **UNVERIFIED — must be measured on device** |
| CoreCLR-on-Android / NativeAOT-on-Android use the same `CLOCK_MONOTONIC` path | **UNVERIFIED at binary level**; source-identical |
| `AChoreographer_postExtendedFrameCallback` was renamed to `AChoreographer_postVsyncCallback` | **UNVERIFIED** (absent from current NDK reference) |

---

### Sources

- Uno (this worktree, `dev/mazi/smooth-scroll`) — file:line inline above.
- AOSP `frameworks/base` `core/java/android/view/Choreographer.java`, `android15-release`.
- AOSP `libcore` `ojluni/src/main/native/System.c`, `main`.
- AOSP `art` `libartbase/base/time_utils.cc`, `main`.
- dotnet/runtime **v10.0.0**: `src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs`,
  `src/native/libs/System.Native/pal_time.c`, `src/native/minipal/time.c`.
- Shipped artifact: `Microsoft.NETCore.App.Runtime.Mono.android-arm64/10.0.8/runtimes/android-arm64/native/{System.Private.CoreLib.dll, libSystem.Native.so}`.
- Bindings: `Microsoft.Android.Ref.36/36.1.53/ref/net10.0/{Mono.Android.dll, Mono.Android.xml}`.
- SDK: `Microsoft.Android.Sdk.Windows/36.1.53/targets/{Microsoft.Android.Sdk.SupportedPlatforms.targets, Microsoft.Android.Sdk.DefaultProperties.targets}`.
- NDK reference: `developer.android.com/ndk/reference/group/choreographer`.
