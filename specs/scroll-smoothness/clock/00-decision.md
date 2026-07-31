# Decision — the frame clock

Consolidates `01-win32.md` … `08-avalonia.md` (platform surveys) and `10-refute-epoch.md`,
`11-refute-ordering.md`, `12-refute-sufficiency.md` (adversarial passes). Every Uno claim below was
re-read from source in this worktree; external claims carry the note that verified them. Evidence
label for this document: **code review (inspection) only** — no compile, no runtime, no device. The
runtime numbers quoted are from the probes recorded in `01-win32.md §8`, `06-winui.md §5`,
`10-refute-epoch.md §3`.

---

## 1. Can we get the exact timestamp, or is it impossible?

**Yes on six of nine host/renderer combinations, and on five of those the value is already being
delivered into Uno and thrown away on a specific line. The epoch question — the thing that was
supposed to make this expensive — is dead: `Stopwatch.GetTimestamp()` and the platform frame clock
are literally the same counter on Windows (QPC), Android and Linux (`CLOCK_MONOTONIC`), Apple
(`mach_absolute_time`) and the shipping single-threaded WASM pack (`performance.now()`), so every
conversion is a unit scale with a zero offset. The honest "no" cases are macOS (no timestamp-bearing
callback exists anywhere in the host, and bolting on an observer display link aliases without bound),
Win32-OpenGL on the default follow-refresh path (`_pacer == null`, so there is no sample point at
all), Android-GL (`UnoSKCanvasView` has no Choreographer), and X11 (a real `ust` is obtainable, but
the X11 host is not vsync-paced in the first place — its record cadence is a free-running
`System.Timers.Timer`, so a better clock on a worse pump buys almost nothing). Those four stay on the
estimator, permanently and without apology.**

| Target | Available? | API / where | Category | Epoch vs `Stopwatch.GetTimestamp()` | Effort |
|---|---|---|---|---|---|
| **Win32** software + Vulkan | **Yes** | `DwmGetCompositionTimingInfo(HWND.Null, …)` → `qpcVBlank`, `qpcRefreshPeriod`, `cRefresh`, sampled immediately after the `PInvoke.DwmFlush()` at `Win32RenderPacer.cs:61` | **(a)** vsync — and it lands ~0.93 periods *ahead*, so it doubles as **(b)** with no extrapolation | **Identity, zero offset.** `Stopwatch.Windows.cs:19-28` P/Invokes `QueryPerformanceCounter` directly. Measured: `qpcVBlank − Stopwatch` = 7.93–8.07 ms on an 8.3325 ms grid, 8 consecutive frames | **S** |
| **Win32** OpenGL, follow-refresh (the default on non-Vulkan machines) | **No** | `_pacer == null` at `Win32WindowWrapper.Rendering.OpenGl.cs:146-148`; blocks inside `wglSwapIntervalEXT(1)`, never calls `DwmFlush` | — | — | — |
| **Android** Vulkan (default) | **Yes, in hand** | `DoFrame(long frameTimeNanos)` at `ChoreographerFramePacer.cs:99` — `=> onFrame();`, argument dropped. API 33+ upgrade: `PostVsyncCallback` → `FrameData.PreferredFrameTimeline.ExpectedPresentationTimeNanos` | **(a)**, framework-snapped to the true vsync grid / **(b)** on 33+ | **Identity**, `ns / 100`. Both `CLOCK_MONOTONIC` (AOSP `System.c`; shipped `libSystem.Native.so` has no `clock_gettime_nsec_np`) | **S** |
| **Android** GL fallback | **No** | no Choreographer anywhere in `UnoSKCanvasView`. A main-Looper callback would cover both paths (`NativeDispatcher.Android.cs:36` already holds one) | (a) | identity | M |
| **iOS / tvOS / Mac Catalyst** | **Yes, in hand** | `_link.Timestamp` / `_link.TargetTimestamp` on the `CADisplayLink` field created at `UnoSKMetalView.cs:37` — `CADisplayLink.Create(() => this.Draw())` discards the link | **(b) genuine predicted present**, plus the exact per-frame period as `Target − Timestamp` | **Identity**, `s × 1e7`. `CLOCK_UPTIME_RAW` = `mach_absolute_time` (Apple Libc `gen/clock_gettime.c:121-135`); Chromium `ca_display_link_mac.mm:59-80` builds `TimeTicks() + Seconds(targetTimestamp)` with no offset term | **S** |
| **macOS** | **No** | `UNOMetalViewDelegate.m:35` — `drawInMTKView:(MTKView *)view` carries nothing. No `CADisplayLink`, no `CVDisplayLink`, and `Uno.UI.Runtime.Skia.MacOS` is not even in `Uno.UI.Composition/AssemblyInfo.cs`'s `InternalsVisibleTo` list | — | (would be identity) | — |
| **WASM** | **Yes, in hand** | the `requestAnimationFrame` callback argument, discarded by the zero-parameter arrow at `BrowserRenderer.ts:47-51` | **(a)** — Chromium `viz::BeginFrameArgs::frame_time` | **Identity, offset exactly 0** on the shipping pack: `dotnet.native.js` contains verbatim `_emscripten_get_now=()=>performance.now()`. With `WasmEnableThreads` it is a fixed `performance.timeOrigin` offset | **S** |
| **Linux DRM/KMS framebuffer** | **Yes, in hand** | `OnPageFlip(int fd, uint sequence, uint tv_sec, uint tv_usec, …)` at `DRMRenderer.cs:378` — hardware vblank time **and** the vblank counter, all three parameters unused | **(a)** hardware scanout + exact drop detection via `sequence` | **Identity**, `µs × 10`. `DRM_CAP_TIMESTAMP_MONOTONIC` is always 1 since kernel 4.15 | **S** |
| **Linux X11 / XWayland** | Technically yes, practically **no** | `xcb_present_select_input` + `xcb_present_notify_msc(divisor=1)` → `PresentCompleteNotify{ust,msc}` | (a) — but under XWayland (the GNOME/KDE default) `ust` is `GetTimeInMicros()` at `wl_surface.frame` dispatch, i.e. a software repaint time, not scanout | Identity `µs × 10` **only if the server is local and monotonic**. Three foreign-epoch modes, two of them **drifting**: the `X_GETTIMEOFDAY` → `CLOCK_REALTIME` fallback, remote X (another machine's oscillator), time namespaces. Needs a magnitude discriminator | M — **and the clock is not the X11 problem** |
| **Headless / Tizen** | No | — | — | — | — |

**The period is separately and freely available on more hosts than the phase is**, and it is the
cheaper half of the win: Win32 `qpcRefreshPeriod` (or `DEVMODEW.dmDisplayFrequency`, already read at
`Win32WindowWrapper.DisplayInformation.cs`), X11's exact XRandR mode-line rate (already computed at
`X11DisplayInformationExtension.cs:458-500` and delivered only to `FramePacer`), the DRM mode line
(`DRMRenderer.CalculateRefreshRate:264`), Android `Display.RefreshRate`, iOS
`TargetTimestamp − Timestamp`. Today the compositor ignores all of them and re-derives the number as
the median of 32 jittery record deltas (`Compositor.skia.cs:220-222, 292-299`).

Rejected outright, with reasons: `IDXGIOutput::WaitForVBlank` (returns no timestamp — it is
Avalonia's approach and is equivalent to the flush-return instant, 9× worse than `qpcVBlank`);
`IDXGISwapChain::GetFrameStatistics` (category (c), and there is no DXGI swap chain anywhere in
`Uno.UI.Runtime.Skia.Win32`); `DCompositionGetFrameId` pulled on the UI thread (aliasing — §4);
`VK_GOOGLE_display_timing` / `VK_EXT_present_timing` (Mesa 26.1/26.2, and Uno's device creation
overwrites the extension list at `VulkanDevice.Create.skia.cs:72-78`); `_NET_WM_FRAME_TIMINGS` (needs
a cooperating compositor and a sync-counter handshake); `CAMetalDisplayLink` (restructures drawable
acquisition for the same phase `CADisplayLink` already gives).

---

## 2. What WinUI, Flutter and Avalonia actually do

**WinUI does not use the exact number Windows offers it.** `DCompositionGetStatistics` returns
`COMPOSITION_FRAME_STATS.targetTime`, measured on this machine as an exact multiple of the refresh
period to within ±20 µs — and a repo-wide grep of `dxaml` for `DCompositionGetFrameId`,
`COMPOSITION_FRAME_STATS` and `nextEstimatedFrameTime` returns zero hits. XAML's UI-thread clock is a
raw QPC snap taken on the scheduling thread *before* the vblank wait
(`compositorscheduler.cpp:317` vs `:345`), carrying the previous frame's measure/arrange cost exactly
the way Uno's record-time read does — there is even a live `TODO` admitting it
(`UIThreadScheduler.cpp:178-179`). WinUI escapes our defect by never evaluating the inertia curve on
the UI thread: pan and fling are DirectManipulation / InteractionTracker outputs bound in by an
ExpressionAnimation and re-evaluated inside the compositor. **WinUI is therefore evidence about
architecture, not about clocks — the only Microsoft stack that solves the clock problem is WPF, and
WPF uses `qpcVBlank` as the *anchor of a phase-locked grid* (`_estimatedNextPresentationTime`,
`MediaContext.cs:1151-1181`), never as the timestamp itself.**

**Flutter feeds every animation the predicted presentation time, and freely fabricates it where the
OS will not say.** `Animator::BeginFrame` ends with
`delegate_.OnAnimatorBeginFrame(frame_target_time, …)` (`animator.cc:113-118`), and the comment at
`platform_configuration.cc:469` is explicit: *"frameTime is not a delta; its the timestamp of the
presentation."* On iOS and macOS that value is genuinely `CADisplayLink.targetTimestamp` /
`CVTimeStamp inOutputTime`; on Android it is a real vsync plus a derived period; **on Windows it is
`SnapToNextTick(now, start_time_, interval)` where `start_time_` is a never-assigned zero
(`flutter_windows_engine.h:502`) — a grid anchored to nothing, with `qpcVBlank` sitting unread in the
same `DWM_TIMING_INFO` struct it queries for the refresh rate — and on Linux/GTK it is a hardcoded
60 Hz grid.** Flutter also never assumes epochs match: it does a paired read of both clocks at the
callback and applies the delta (`vsync_waiter_ios.mm:117-118`, `vsync_waiter_android.cc:89-90`).
**Flutter's lesson is that what the curve needs is a uniform grid rather than a clock read, and that
synthesizing that grid on Windows and Linux is normal engineering — Uno's estimator is already better
than Flutter's shipped Windows embedder, which can produce a duplicate or a double tick from phase
noise where Uno's counted advance cannot.**

**Avalonia has our defect in a worse form.** Its platform layer successfully captures a real vsync
time on Android (`ChoreographerTimer.cs:106-113`) and Browser (`BrowserRenderTimer.cs:43-48`), and
could trivially on macOS and iOS — then `DefaultRenderLoop.TimerTick(TimeSpan time)` discards the
parameter (`RenderLoop.cs:121-146`) because `IRenderLoopTask.Render()` has no slot for it, and four
mutually unsynchronised `Stopwatch`es drive one scrolling frame. Its own touch fling is the worst
case in any stack surveyed: `OnAnimationRequested(TimeSpan _)` names the frame time `_`
(`ScrollGestureRecognizer.cs:216`), re-reads a private `Stopwatch` inside a
`Dispatcher.UIThread.InvokeAsync(…, DispatcherPriority.Input)` continuation — an entire input-queue
drain away from the frame boundary — and then integrates `distance = speed * measuredDt` on top of an
analytic `0.15^t` decay, so it is jitter-sensitive on both the absolute-time and the delta axis where
Uno's analytic `x(t)` is sensitive on only one. **Avalonia is not a model to copy; it is a free
existence proof that the real vsync timestamp reaches managed .NET code on Android and Browser with
no exotic plumbing, and a warning that the value dies at whichever interface forgets to carry it.**

---

## 3. The design

### 3.1 The principle: anchor the grid, count the records — never assign

The single most important finding across the three refutation passes is that **no per-record
assignment of a platform timestamp can be correct in Uno, on any platform, for reasons entirely
internal to Uno.** `CompositionTarget.Render()` — the only path that raises `FrameStarting` — has two
callers, and only one is downstream of a platform frame callback:

* **Path A** — `EnqueueRenderCallback` (`CompositionTarget.RenderScheduling.skia.cs:152`), enqueued
  from `OnNativePlatformFrameRequested` (`:172`).
* **Path B** — `OnRenderFrameOpportunity` (`:205`), called from `CoreServices.OnTick`
  (`CoreServices.cs:124`) and `Win32WindowWrapper.cs:421`. It deliberately records *ahead of* the
  frame callback it accounts against and then skips that callback's record (`:131-144`). It is armed
  by the effective-viewport queue (`EventManager.cs:34`), i.e. it is the **common** case while
  flinging a virtualized list.

A published-latest-stamp scheme therefore gives some records stamp *N* and others *N−1*, producing an
intermittent 0-period / 2-period step — about 22 dip of discontinuity at the measured 2650 dip/s, an
order of magnitude worse than the ~2.4 dip wobble the work exists to remove. The record also cannot
know which vsync it will be shown at: `SkiaRenderHelper.FpsHelper` maintains separate counters for
records-without-presents and presents-without-records precisely because the ratio is not 1:1
(`SkiaRenderHelper.skia.cs:268-324`).

So: **ask the platform for the grid (phase anchor + period), not for this record's timestamp.** A
grid anchor may be arbitrarily stale without being wrong. That is WPF's shape, it is GTK4's shape
(GTK *has* a real presentation time on X11 and still runs a quadratic phase-lock over it,
`gdkframeclockidle.c`), and it is structurally what `GetFrameTimestamp` already is.

### 3.2 The seam — one type, one interface method, no per-platform abstraction

**Add** `src/Uno.UI.Composition/Composition/FrameClock.skia.cs` — `internal sealed class FrameClock`,
holding the estimator state that today lives on `Compositor` plus the platform reference:

```csharp
internal sealed class FrameClock
{
    // Any thread, any time. Both values in the Compositor tick domain (100 ns); 0 = unknown.
    internal void ReportCadence(long vsyncTimestampInTicks, long periodInTicks);

    // UI thread, once per record.
    internal long NextTimestamp(long rawTimestampInTicks);

    internal long IntervalInTicks { get; }
}
```

`ReportCadence` publishes an immutable `(vsync, period, reportedAt)` triple through a single
`Interlocked.Exchange<T>` of a reference. That is deliberate: `Volatile.Read/Write` on a `long` is
**not** atomic on the 32-bit runtimes Uno ships (android-arm, wasm32) — the codebase already knows
this (`SkiaRenderHelper.skia.cs:170-171`) — and the three fields must be observed as a set.

**Change** `src/Uno.UI.Composition/Composition/ICompositionTarget.cs`, inside the existing `#if
__SKIA__` region:

```csharp
FrameClock FrameClock { get; }
void ReportFrameCadence(long vsyncTimestampInTicks, long periodInTicks);
```

`ICompositionTarget` is already `internal` in `Uno.UI.Composition` and already visible to every Skia
host through `AssemblyInfo.cs`. Every host already holds the `CompositionTarget` at its frame
callback — that is the object `OnNativePlatformFrameRequested` is an instance method on. **No new
extensibility mechanism, no `ApiExtensibility`, no per-platform clock interface.**

**Change** `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs` — own one
`FrameClock`, implement the two members.

**Change** `src/Uno.UI.Composition/Composition/Compositor.skia.cs` — delete `_frameDeltas`,
`_frameDeltaIndex`, `_frameDeltaCount`, `_lastRawFrameTimestamp`, `_frameClock`, `GetFrameTimestamp`,
`MedianFrameDelta` (`:224-299`); `RenderRootVisual` resolves the clock from the target it is already
touching at `:375`:

```csharp
var clock = rootVisual.CompositionTarget?.FrameClock;
var frameTimestamp = clock?.NextTimestamp(TimestampInTicks) ?? TimestampInTicks;
```

and `FrameIntervalInTicks` (`:220-222`) delegates to `clock.IntervalInTicks`.

### 3.3 How the platform source and the estimator compose

They are **the same loop with a better reference signal**, not two implementations. One code path
runs on every host; the fallback is not a second, untested branch.

```
period    = platformPeriod, if fresh and sane;  else MedianFrameDelta()

reference = platformVsync projected onto the grid nearest `raw`:
                platformVsync + round((raw − platformVsync) / period) * period
            if fresh and sane;  else raw            // ← today's behaviour, unchanged

frames    = max(1, round((reference − _frameClock) / period))
_frameClock += frames * period                       // counted advance, monotone by construction
_frameClock += (reference − _frameClock) / 16        // sub-period phase pull, unchanged
```

Four properties fall out, and each one is why this shape and not another:

1. **Staleness is harmless.** A vsync anchor two frames old defines exactly the same grid. This is
   what makes the whole cross-thread problem evaporate: no capture-at-enqueue, no pairing invariant,
   no per-frame handshake, no ordering requirement between the platform callback and the record.
2. **Aliasing is harmless.** The `round(…) * period` projection normalizes whole-period offsets away
   before the anchor is used, so catching "the wrong callback" costs nothing. Only the sub-period
   phase — the quantity we actually want — survives projection.
3. **Path B is harmless.** The grid advances once per record regardless of who called `Render()`.
4. **The fallback is free.** With no platform source, `reference = raw` and `period = median` — the
   shipped estimator, bit for bit. macOS, Win32-OpenGL, Android-GL, X11, Headless and Tizen ride the
   identical code.

Two more consequences worth stating explicitly:

* **`frames = max(1, round(…))` replaces the `if (|error| >= period)` branch at
  `Compositor.skia.cs:276`**, which is GTK4's formulation (`gdkframeclockidle.c`, *"we avoid minor
  jitter in the frame times making the animation speed uneven, but still animate evenly in case of
  whole frame skips"*). This removes the discontinuity at the one-period boundary, removes the
  backward-slip bug (§5.2), and — critically — makes a *stated panel period* correct even when the
  app produces records at half the panel rate: an app rendering 60/s on a 120 Hz panel gets
  `round(2) = 2` every frame with no lag, where the current hard branch sits exactly on its threshold
  and would flip sides on jitter.
* **One sanity gate, written once, serving three purposes** (note 10 §6.2/§6.3): accept a cadence
  report only if `|vsync − raw| < 1 second` (the epoch discriminator that kills X11's `CLOCK_REALTIME`
  fallback, remote X and time namespaces, and that catches any future runtime regression on *any*
  platform), `raw − vsync < 8 × period` (staleness), and `period ∈ [2.5 ms, 100 ms]` (Chromium's
  bounds). Log once on rejection, fall back. Compare with a tolerance, never equality — the
  `(long)(ns × 0.01)` double conversion diverges from integer `/ 100` by one tick above 104 days of
  uptime.

### 3.4 Host publishers — one call each

| Host | Where to publish from | What to publish |
|---|---|---|
| Win32 software/Vulkan | after `PInvoke.DwmFlush()`, `Win32RenderPacer.cs:61` | `qpcVBlank × s_tickFrequency`, `qpcRefreshPeriod × s_tickFrequency`, gated on `cRefresh` advancing. Add `DwmGetCompositionTimingInfo` + `DWM_TIMING_INFO` to `Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt` (`DwmFlush` is already at `:180`). **`[StructLayout(Pack = 1)]`, `cbSize == 292`, `hwnd == NULL`** — unit-test the size; a 320-byte natural layout fails with `MILERR_MISMATCHED_SIZE` and silently degrades forever |
| WASM | `BrowserRenderer.ts:47-51` → `Rendering/BrowserRenderer.cs:59-63` | widen the `[JSExport]` with `double frameTimestampMs`; `ticks = ms × 10_000`. Do **not** publish on the re-arm early-return path at `:71-79` |
| Apple UIKit | `UnoSKMetalView.cs:37` | `_link.Timestamp × 1e7`, period `(TargetTimestamp − Timestamp) × 1e7` |
| Android Vulkan | `ChoreographerFramePacer.cs:99` | `frameTimeNanos / 100`; API 33+ may add `ExpectedPresentationTimeNanos` — copy to `long` *inside* `OnVsync`, the AOSP accessors throw outside the callback |
| Linux framebuffer | `DRMRenderer.cs:378` | `(tv_sec × 1e6 + tv_usec) × 10`, period from `CalculateRefreshRate:264`, `sequence` as an exact drop counter |
| X11 | `X11DisplayInformationExtension.cs:220` | **period only** (XRandR mode line). No phase — see §4 |

`Uno.UI.Runtime.Skia.MacOS` is not in `Uno.UI.Composition/AssemblyInfo.cs`'s `InternalsVisibleTo`
list, which is a convenient reminder that macOS is out of scope here.

### 3.5 What must move with the clock

`Compositor` is a process-wide singleton (`Compositor.cs:17`, `:40`) and `FrameStarting` is declared
on it (`Compositor.skia.cs:209`), but records are per-`CompositionTarget` and `CoreServices.OnTick`
records **every window in one dispatcher tick** (`CoreServices.cs:108-124`). Moving the clock state
to `CompositionTarget` fixes the ring poisoning (§5.1) but on its own creates a new problem: a driver
subscribed to the shared compositor would then be ticked once per window, against a different
window's grid. **`FrameStarting` must move to `CompositionTarget` alongside the clock**, with drivers
subscribing through their own visual — the pattern `Compositor.RequestFrame(Visual)`
(`Compositor.skia.cs:217`) already uses. The two current subscribers are
`ScrollContentPresenter.Managed.cs:603/677` (has `Visual` in hand) and
`CompositionInertiaProcessorTimer` (`InertiaProcessor.cs:354`, uses `GetSharedCompositor()` and needs
the element's visual threaded in — a small plumb). Take the `event Action<long>` →
`EventHandler<T>` fix that `AGENTS.md` requires in the same edit.

---

## 4. Ordered workplan

Ranked by value per unit effort. Items 1–3 are worth doing whether or not a single platform
timestamp is ever plumbed.

| # | Item | Kind | Effort | Platforms | Risk |
|---|---|---|---|---|---|
| **1** | **Fix the two HIGH bugs in `GetFrameTimestamp` and adopt the GTK formulation** — `frames = max(1, round(error/period))` replacing the `\|error\| >= period` branch (kills the backward slip and the boundary discontinuity in one change), reject idle gaps at the ring boundary, reset on `FrameStarting` unsubscribe. §5.1–§5.4 | **root-cause fix** | S | all Skia | **Low.** As shipped the estimator can snap a fling back to its launch offset and can silently degrade to the raw clock — both worse than the defect it removes. Nothing else ships until this does |
| **2** | **Move the frame clock — and `FrameStarting` — from `Compositor` to `CompositionTarget`** (§3.5), and stop drivers latching raw `TimestampInTicks` (`ScrollContentPresenter.Managed.cs:669`, `InertiaProcessor.cs:355`) | **root-cause fix** | M | all Skia; bites multi-window desktop and XAML islands | Medium — touches the driver subscription shape. Mandatory before any phase anchor lands, because a forward-phased grid turns the raw/grid mixing into a negative first `elapsed` and an unspecified `(ulong)` conversion |
| **3** | **Build the seam and feed it the *stated period* only** — `FrameClock` + `ICompositionTarget.ReportFrameCadence`, wired to numbers the tree already computes: X11 XRandR (`X11DisplayInformationExtension.cs:458-500`), DRM mode line (`DRMRenderer.cs:264`), Win32 `DEVMODEW.dmDisplayFrequency`, Android `Display.RefreshRate`, iOS `Target − Timestamp` | **root-cause fix** | S–M | all | Low. Removes the 8-frame warm-up (which lands on the *first fling of the process*, back-dating its launch by 16.7 ms instead of 8.3 and jumping the first frame two steps) and the ~17-frame median lag on a refresh change. This is the estimator's genuinely bad case and it is the cheap half of the whole effort |
| **4** | **Phase anchor: WASM** — `BrowserRenderer.ts:48` + the `[JSExport]` signature | **root-cause fix** | S | WASM | Low. Highest value-per-effort of any phase work: two signatures, offset provably 0, and it fixes the Android-Chrome 60↔120 Hz switch *for free* — the switch coincides with touch-down/touch-up, so today the mis-estimate lands on the first ~140 ms of every fling. It also removes the median being reconstructed from `performance.now()` samples quantized to 100 µs |
| **5** | **Phase anchor: Win32** — `DwmGetCompositionTimingInfo` after the existing `DwmFlush` | **root-cause fix** | S | Win32 software + Vulkan | Low, and it is the one target measurable on the dev machine today (0.90 ms rms → 0.018 ms rms, 50×). `cRefresh` is the freshness guard. Do not add forward lead in this change |
| **6** | **Fix `ChoreographerFramePacer`'s timeout desync** — `RemoveFrameCallback` on the `MaxWait` path, or replace the `AutoResetEvent` with a monotone frame counter | **root-cause fix** | S | Android Vulkan | Low. **Independent of the clock work.** One timeout latches a set and leaves a permanently extra outstanding callback; every subsequent wait is then satisfied by the *previous* frame's vsync, silently and unrecoverably |
| **7** | **Phase anchor: Android** — `frameTimeNanos` at `ChoreographerFramePacer.cs:99`; optionally `ExpectedPresentationTimeNanos` on API 33+ | **root-cause fix** | S–M | Android Vulkan only | Medium — needs #6 first, and covers only the Vulkan path. Justified almost entirely by adaptive refresh: on an LTPO panel the rate can change *because* you started scrolling, which is the estimator's worst case |
| **8** | **Phase anchor: iOS/tvOS/Catalyst** — latch `_link.Timestamp` at `UnoSKMetalView.cs:37` | **root-cause fix** | S | Apple UIKit | Low code risk, but unprovable without a device. The only target where a *genuine* predicted-present time exists, and ProMotion makes `Target − Timestamp` structurally better than any median |
| **9** | **Phase anchor: Linux framebuffer** — the three unused `OnPageFlip` parameters | hardening | S | Linux FB | Low, free, and it is a real hardware vblank with an exact `sequence`. Ranked last only because almost nobody flings on a framebuffer host; it is however the cheapest place to *validate the whole idea* on Linux |

### Explicitly NOT worth doing

* **macOS observer display link.** An observer link and AppKit's `needsDisplay` cycle are independent
  producers with no handshake, so the aliasing is unbounded, not "one callback stale" as
  `03-apple.md §4.4` supposed. macOS needs its pump restructured to link-driven (iOS shape) or it
  stays on the estimator. Do not ship a stamp there. Separately, whether macOS frames are
  vsync-phase-locked *at all* today is an open question — `UNOWindow.m:324` sets
  `enableSetNeedsDisplay = YES` without ever setting `paused`. **Answer that before spending
  anything on macOS smoothness; it may be a larger defect than the clock.**
* **X11 Present / `glXGetSyncValuesOML` plumbing.** The X11 host is paced by a
  `System.Timers.Timer` (`X11XamlRootHost.Rendering.cs:15-59`, `FramePacer.cs`) with no vsync signal
  anywhere. The timer and the display are independent oscillators, so the app beats against the panel
  regardless of how clean the curve evaluation is. Fix the *pacing* (a Present-event or
  `glXWaitForMscOML` wait replacing the timer) or fix nothing. A better clock on a free-running pump
  is polishing the wrong term.
* **`DCompositionGetFrameId` / `DCompositionGetStatistics` pulled on the UI thread** as
  `06-winui.md §8.1` proposes. `COMPOSITION_FRAME_ID_CREATED` advances on DWM's clock, so reading it
  at record time makes the answer a function of *when the record started* — pure aliasing of the exact
  defect being removed. Its own §5.1 measurements were taken inside a
  `DCompositionWaitForCompositorClock` loop that §8.1 discards. (`06-winui.md §5.3`'s conclusion that
  `DwmGetCompositionTimingInfo` "cannot be depended on" should also be withdrawn: `0x88980090` is
  `MILERR_MISMATCHED_SIZE`, i.e. that probe's own `cbSize` bug.)
* **Forward lead / evaluating at a predicted present.** A *constant* offset in `x(t)` is invisible —
  the fling anchors `_flingStartTimestamp` on the first frame's own timestamp
  (`ScrollContentPresenter.Managed.cs:621-625`), so it cancels exactly. Lead is a *latency* change,
  and leading only the inertial curve while the drag path latches finger geometry and reads no clock
  puts a step at the finger-lift handoff — precisely the user-fighting artifact this workstream has
  ruled out. Separate experiment, separate capture, after the grid lands.
* **Per-record timestamp assignment on any host**, including capture-at-enqueue. §3.1.
* **Computing a `frameTimeNanos` ↔ `Stopwatch` offset on Android/Apple/Win32/WASM-1T.** It is zero.
  Measuring it bakes in the sampling pair's noise. Assert it; do not correct with it.
* Chasing category (c) anywhere — `vkGetPastPresentationTimingGOOGLE`, `FrameMetrics`,
  `DXGI_FRAME_STATISTICS`, `IMTLDrawable.PresentedTime`, `wp_presentation_feedback`,
  long-animation-frame `PerformanceObserver`. Useful to *validate* a prediction, structurally unable
  to drive a curve. (`IMTLDrawable.PresentedTime` is the right tool for the §4 lead experiment, later.)

### The honest bottom line on value

Deriving the shipped estimator's transfer function (`Compositor.skia.cs:273-287`) gives a first-order
PLL with α = 1/16, i.e. a fixed **15.7× attenuation** of step jitter. Applied to the measured
σ = 0.90 ms baseline at 2650 dip/s on a 120 Hz panel:

| Frame-clock source | position ripple (rms) | fraction of one 22.1 dip step |
|---|---|---|
| raw `TimestampInTicks` (before the estimator) | 2.39 dip | 10.8 % |
| **phase-locked estimator (shipped)** | **0.15 dip** | **0.68 %** |
| `qpcVBlank` / `targetTime` | 0.05 dip | 0.21 % |

**The estimator already removes ~94 % of the ripple; the platform anchor's marginal gain is ~0.10 dip
rms — below one physical pixel at every density Uno ships.** Meanwhile the residual that *neither*
addresses — record→present latency quantizing to 1 or 2 refresh intervals
(`CompositionTarget.Rendering.skia.cs:135-155` writes the slot, an unrelated render thread picks it
up and presents) — is up to a **full 22 dip step**, 150× larger, and has never been measured. That is
why items 1–3 come before items 4–9, and why §6's metric B is a gate, not a formality: if B is
already at drag's level, **close the clock track and go instrument the present side instead.**

---

## 5. Bugs in the newly-added `GetFrameTimestamp` that must be fixed regardless

There are five. Two are HIGH.

**5.1 HIGH — one frame clock serving N record loops.** `src/Uno.UI.Composition/Composition/Compositor.skia.cs:227-231`
(the ring and `_frameClock` are fields on the process-wide singleton, `Compositor.cs:17`, `:40`) vs
`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:108-124`, which loops every window and calls
`OnRenderFrameOpportunity` on each. Two animating windows push two near-zero deltas per tick into the
same ring; at 3+ content roots the near-zero deltas outnumber the real ones, `sorted[count/2]` returns
a near-zero period, and with `period ≈ ε` the slip branch computes `Round(error/ε)·ε ≈ error` — **the
estimator silently degrades to the raw clock it was written to replace**, with no exception and no
log. Fix per §3.5 (move the state to `CompositionTarget`, and move `FrameStarting` with it, or the
per-target grids alias into the shared driver).

**5.2 HIGH — the grid is not monotonic.** `Compositor.skia.cs:280`. Net advance is
`period · (1 + Round(error/period, AwayFromZero))`, which is **negative** whenever
`error ≤ −1.5·period`, i.e. whenever a record lands more than half a period before the previous
frame's grid value — reachable via 5.1 and via any burst that wakes the record loop twice inside one
refresh interval. Downstream: `ScrollFlingSimulation.cs:95-96` clamps `t ≤ 0` to `u = 1` and returns
`_start`, so **a backward slip in the first frames of a fling snaps the content back to its launch
offset** — the exact user-fighting artifact the project has ruled out. Fixed by the `max(1, round(…))`
form in §3.3; make monotonicity an asserted post-condition rather than an emergent property of the
arithmetic.

**5.3 MEDIUM — two drivers latch the raw clock and then subtract grid stamps.**
`src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:355-356` —
`_startTimestamp = compositor.TimestampInTicks;` then
`timestamp => onTick(TimeSpan.FromTicks(timestamp - _startTimestamp))`. The grid sits at the *mean*
record offset, up to ~1 ms either side of raw, so the first `elapsed` can be **negative**; `Process`
then evaluates `_t0 + (ulong)elapsed.TotalMicroseconds` (`:221`), and converting a negative `double`
to `ulong` unchecked is *unspecified* in C#. `ScrollContentPresenter.Managed.cs:669-674`
(`AddWheelImpulse`) has the same shape, saved only by the `elapsed <= 0` guard at
`ScrollDecaySimulation.cs:61-64`. `StartFling` gets it right (`ScrollContentPresenter.Managed.cs:597`,
`:621-625`) — anchor on the first *frame* stamp, and make the other two follow. **This becomes
severe, not latent, the moment a phase anchor gives the grid a forward offset.**

**5.4 MEDIUM-LOW — idle gaps are recorded as frame deltas.** `Compositor.skia.cs:252-260` runs only
under `if (FrameStarting is { } …)` (`:308`), and the ring persists across the unsubscribed gap, so
the first record of every new fling pushes an arbitrary idle interval into `_frameDeltas`. The grid
itself recovers in one frame; the ring does not self-clean, and the same median feeds
`FrameIntervalInTicks` (`:220-222`) which back-dates the fling launch
(`ScrollContentPresenter.Managed.cs:625`) — so a poisoned median becomes a *position* error. Reject
deltas beyond a few multiples of the current median; zero `_lastRawFrameTimestamp` on unsubscribe.

**5.5 MEDIUM (becomes HIGH the moment a stated period lands) — the one-period branch is a
discontinuity sitting exactly on its own threshold.** `Compositor.skia.cs:276`,
`if (Math.Abs(error) >= period)`. In steady state at the app's own cadence `error ≈ 0` and this never
fires. But feed it a *stated panel period* while the app produces records at half that rate and
`error ≈ period` exactly — jitter then flips the branch roughly every other frame, alternating a
1.06-period gentle advance with a 2-period slip. GTK4 avoids this by rounding unconditionally and
correcting afterwards; adopt that (§3.3). Not identified in any of the eight surveys; it is the
reason the period cannot simply be swapped in under the current branch structure.

**Clean, and re-verified here:** the `% 32` ring index cannot overflow (`:256`, operand always in
`[0,31]`); `error / 16` truncates toward zero for both signs so the deadband is symmetric at ±1.5 µs
and cannot ratchet; the first-frame path (`:246-250`) is correct (though `0` as an unset sentinel for
a `Stopwatch`-derived value is undocumented); `MedianFrameDelta` itself is right (`:292-299` — the
ring fills `0..count-1` in order during warm-up, `stackalloc[32]` always fits); subscribe/unsubscribe
is leak-free (`ScrollContentPresenter.Managed.cs:616`, `:699`, `InertiaProcessor.cs:362-366`); and
there is **no data race** — the `Compositor` overload of `RenderRootVisual` (`:301`) has exactly one
caller chain, `SkiaRenderHelper.skia.cs:44` → `CompositionTarget.Rendering.skia.cs:119`, guarded by
`NativeDispatcher.CheckThreadAccess()` at `:114`; the other four call sites bind the `Visual` overload
and never touch the frame clock. 5.1 is not concurrency — it is one clock serving N record loops on
one thread.

**Rider (not a bug, but the seam work touches the line):** `Compositor.skia.cs:209` declares
`internal event Action<long>? FrameStarting;`, which `AGENTS.md` prohibits outright in favour of
`EventHandler<TEventArgs>`. Fix it while moving the event to `CompositionTarget` (§3.5).

---

## 6. How to prove it on a device

Everything below reuses the existing capture with **no new instrumentation**. `ScrollDiagnostics`
already emits, per sample, `<F|I> <phase> <wallUs> <frameUs> <src> <value>`
(`ScrollDiagnostics.cs:148-154`), where `frameUs = CurrentFrameTimestampInTicks / 10`
(`:98`, fed from `ScrollContentPresenter.Managed.cs:189`) and `value = −AnchorPoint.Y`. Phases:
`1 = drag`, `2 = inertia`, `3 = wheel` (`:71-74`). Enable with
`FeatureConfiguration.ScrollViewer.EnableDiagnostics`.

**The capture already contains both clocks.** `wallUs` is sampled inside `Record()`, which runs
inside `Set(…)` inside `OnFlingFrame` inside `FrameStarting` — microseconds from the record instant.
`frameUs` is the grid. So `σ(Δ wallUs)` is the raw record-phase noise and `σ(Δ frameUs)` is what the
drivers actually saw, from one file, today.

**Capture 1 — the estimator alone, before any platform work.** One slow steady drag → flick → let it
settle, on the target device. Compute per phase, using the metric definitions in
`specs/scroll-smoothness/inertia/00-verdict.md §4`:

| Metric | Definition | What it decides |
|---|---|---|
| **A** | `σ(Δ frameUs)`, and now also `σ(Δ wallUs)` | Sanity check on the estimator itself: the derived transfer function predicts `σ(Δ frameUs) ≈ σ(Δ wallUs)/16`. **If that ratio is not ~15×, the estimator is not behaving as modelled — stop and find out why before any platform plumbing.** |
| **B** | `σ(Δ value) / mean(Δ value)`, phase 2 vs phase 1 at matched mean speed | **The gate.** B at drag's level ⇒ the clock work is done; the remaining 0.10 dip is not worth five platforms of plumbing — **close the clock track.** B unmoved ⇒ the residual is downstream of the record and a platform *clock* would not have helped either; the next instrument is a present-side timestamp on the render thread, not a vsync clock on the UI thread |
| **C** | `corr(Δ value, Δ frameUs)`, per phase | The direct signature of `Δx = v·Δt`. Should be ≈ 0 in phase 2 with the estimator in place, ≈ +1 without |

Additionally, run an autocorrelation over `Δ wallUs` in phase 2. Structure **slower than ~1 s** is the
single spectrum a first-order PLL at α = 1/16 cannot suppress, and it is the only measurement that
justifies the platform source on its own merits. Realization bursts at one per 5–10 frames sit an
order of magnitude above the loop's ~1.2 Hz corner and are attenuated normally.

**Capture 2 — with the phase anchor, same device, same gesture.** Expect `σ(Δ frameUs)` to fall from
~57 µs to ~18 µs on Win32 and B to be *unchanged*. That is the success criterion for items 4–9 and
also the reason they rank below 1–3: they improve a term that capture 1 will likely show is already
invisible.

**Order of devices, and why:**

1. **Win32 desktop first.** Exact timebase (QPC ≡ `Stopwatch`, `s_tickFrequency == 1.0` on a 10 MHz
   QPF machine), a `qpcVBlank` ground truth available in the same process, and **wheel decay is
   measurable with no finger at all** (`ScrollDiagnostics.PhaseWheel`), removing the single-threaded
   input confounder from the comparison. It is the machine the probes already ran on.
2. **Android Vulkan.** The only place adaptive refresh can change the panel rate mid-fling, which is
   the estimator's worst case (§4 item 3/7). Capture across a touch-down/touch-up boundary.
3. **WASM in Android Chrome.** Log 200 consecutive `Δ frameUs` across a touch-down/touch-up boundary
   and look for the 16.67 → 8.33 → 16.67 step. With the estimator you should see a ~17-frame
   re-convergence tail on each transition; with the rAF anchor there should be none.

**Two free separating experiments, no code:**

* Run the identical flick on a tall non-virtualized `StackPanel` and on a `ListView`. Smooth on the
  StackPanel and rough on the `ListView` ⇒ layout cost feeding the record instant and the
  realize-after-record inversion are material, and neither is a clock problem.
* Log `(path A | path B)` alongside each frame sample for one session — one field, one `bool`. **If
  path B never wins during a fling, §3.1's ordering refutation downgrades from "universal" to "guard
  the edge case", and item 2's urgency drops with it.** This is the single highest-value unmeasured
  fact in the whole investigation.

**Instrumentation caveat that must appear in any report using these numbers.** Enabling diagnostics
subscribes `CompositionTarget.Rendering` (`ScrollContentPresenter.Managed.cs:171`), which sets
`_isRenderingActive` and makes every `Render` re-request a frame and enqueue an allocating
`RaiseRendering` at High priority (`CompositionTarget.Rendering.skia.cs:90-96`, `:164-167`,
`:444-448`). Absolute microseconds are inflated in **both** phases. A, B and C are within-phase ratios
and a cross-phase comparison, so the conclusions survive; the absolute figures do not.

---

## 7. One-line summary

Get the real timestamp where it is already being handed to us and thrown away — WASM's rAF argument,
Win32's `qpcVBlank`, Android's `frameTimeNanos`, iOS's `CADisplayLink`, DRM's page-flip time — but
feed it into the existing phase-locked loop **as the grid's anchor and period, never as the frame's
timestamp**, because Uno's own record scheduling makes per-record assignment incorrect on every
platform at once; fix the two HIGH bugs in the estimator first, because as shipped it can snap a fling
back to its launch offset; and gate the whole phase-anchor track on metric B, because the estimator
already removes 94 % of the error and the residual it leaves is 150× smaller than the one nobody has
measured yet.
