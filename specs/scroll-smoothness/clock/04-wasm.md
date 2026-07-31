# Frame clock on WebAssembly (Skia-on-WASM, browser)

Scope: can Uno obtain a **real** per-frame timestamp on the browser instead of the phase-locked
estimator in `Compositor.GetFrameTimestamp`, and what do mature stacks feed their scroll curves on
the web?

**Headline: yes, and it costs about ten lines.** The browser already hands Uno an exact, uniform,
vsync-aligned frame timestamp on every frame — as the single argument of the
`requestAnimationFrame` callback — and Uno throws it away in the arrow function at
`src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/BrowserRenderer.ts:48`. It is on
`performance.now()`'s epoch, which on browser-wasm is *the same clock* `Stopwatch.GetTimestamp()`
reads. This is the strongest of the four targets: no platform API to negotiate, no prediction to
reconstruct, and it is exactly what Flutter Web feeds its scroll simulation.

---

## 1. How frames are driven today (verified, Uno source)

The whole WASM frame pump is these five hops:

| # | Where | What |
|---|-------|------|
| 1 | `Rendering/BrowserRenderer.cs:47-57` `InvalidateRender()` | sets `_pendingInvalidate`, calls JS `invalidate` |
| 2 | `ts/Runtime/BrowserRenderer.ts:47-51` `static invalidate()` | `window.requestAnimationFrame(() => { instance.requestRender(); })` |
| 3 | `ts/Runtime/BrowserRenderer.ts:11` `requestRender` | `skiaSharpExports.…BrowserRenderer.RenderFrame(this.managedHandle)` |
| 4 | `Rendering/BrowserRenderer.cs:59-63` `[JSExport] RenderFrame` | → instance `RenderFrame()` (`:65`) → `compositionTarget.OnNativePlatformFrameRequested(...)` (`:98`) |
| 5 | `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:166-176` | `NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)` (`:172`), then `Draw(...)` (`:175`) |

`Draw` **presents** the picture recorded on a previous pass. The **record** does not happen inside
the rAF callback at all — `EnqueueRender` only queues `EnqueueRenderCallback`
(`CompositionTarget.RenderScheduling.skia.cs:120-157`), which calls `Render()`
(`CompositionTarget.Rendering.skia.cs:110`), which is what runs
`SkiaRenderHelper.RecordPictureAndReturnPath` (`:119`) and therefore
`Compositor.RenderRootVisual` → `FrameStarting` (`Compositor.skia.cs:308-316`).

### The record runs in a `postMessage` macrotask

Which dispatcher does Skia-on-WASM use? Two-layer runtime selection ships the **webassembly** build
of `Uno.UI.Dispatching.dll` even when the UI layer is Skia —
`RuntimeAssetsSelectorTask.cs:261-262` lists `uno.ui.dispatching` as a "WinRT assembly", and
`GetWinRTAssembly` takes it from `uno-runtime/<tfm>/webassembly` (`:270`). So the compiled
dispatcher is `NativeDispatcher.wasm.cs`, whose `EnqueueNative` (`:52-80`) calls
`NativeMethods.WakeUp()` (`:84-85`) → `globalThis.Uno.UI.Dispatching.NativeDispatcher.WakeUp` →
`src/Uno.UWP/ts/Windows/Dispatching/NativeDispatcher.ts:18-31`:

```ts
(<any>window).setImmediate(() => { NativeDispatcher._dispatcherCallback(); });
```

and `setImmediate` is the YuzuJS polyfill vendored at `src/Uno.UWP/WasmScripts/setImmediate.js`
(header `:1-5`), which in browsers installs the **`postMessage`** implementation (`:105-120`).

So the pipeline is:

```
rAF(T_n) ──► Draw()  [presents picture recorded after rAF(T_{n-1})]
        └──► postMessage task ──► Render() ──► FrameStarting(GetFrameTimestamp(Stopwatch.GetTimestamp()))
                                              ▲ arbitrary macrotask time, NOT a frame boundary
             Render() ends with host.InvalidateRender()  (Rendering.skia.cs:171) ──► rAF(T_{n+1})
```

**This is the jitter, located.** The clock the drivers read is sampled inside a `postMessage`
macrotask that is scheduled after the rAF callback returns and competes with every other queued
task (input dispatch, GC, image decode callbacks, interop). The record instant is a *consequence*
of task-queue scheduling; the frame boundary `T_n` is a *fact* the browser handed us one hop
earlier and we dropped.

---

## 2. What Uno already receives and discards

`ts/Runtime/BrowserRenderer.ts:47-51`:

```ts
static invalidate(instance: BrowserRenderer) {
    window.requestAnimationFrame(() => {     // ← callback argument (DOMHighResTimeStamp) discarded
        instance.requestRender();
    });
}
```

The arrow function declares **no parameter**, so the frame timestamp the browser passes is dropped
on the floor. The interop signature downstream has no slot for it either —
`Rendering/BrowserRenderer.cs:59-63`:

```csharp
[JSExport]
internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance)
```

(There are four other `requestAnimationFrame` call sites in the project — `Accessibility.ts:296`,
`SemanticElements.ts:1435` — but they are DOM-batching helpers, not the frame pump. The only frame
driver is `BrowserRenderer.ts:48`.)

### What that argument actually is

Category **(a) vsync/frame-start time**, not a prediction and not a measurement.

- Per the HTML Standard's "update the rendering" step, one `frameTimestamp` is computed for the
  rendering update and every `requestAnimationFrame` callback of that update is invoked with it.
  MDN states it plainly: *"When multiple callbacks queued by `requestAnimationFrame()` begin to fire
  in a single frame, each receives the same timestamp"* and *"For `Window` objects … it is equal to
  `document.timeline.currentTime`"*
  ([MDN, Window.requestAnimationFrame](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame)).
  It is therefore **already a uniform per-frame clock** — the exact quantity
  `Compositor.GetFrameTimestamp` is reconstructing.
- In Chromium it originates from the compositor's `BeginFrameArgs.frame_time`, documented in
  `components/viz/common/frame_sinks/begin_frame_args.h` as *"The time at which the frame started.
  Used, for example, by animations to decide to slow down or skip ahead."*
  `third_party/blink/renderer/platform/widget/widget_base.cc` `WidgetBase::BeginMainFrame(const
  viz::BeginFrameArgs& args)` forwards the whole `args` to `client_->BeginMainFrame(args)`, and
  `third_party/blink/renderer/core/page/page_animator.cc` `PageAnimator::ServiceScriptedAnimations`
  converts it per-document (`document->Timeline().CalculateZeroTime()` +
  `time_clamper.ClampTimeResolution(...)`) and stores it via
  `controller->SetCurrentFrameTimeMs(window->document()->Timeline().CurrentTimeMilliseconds())`
  before `ExecuteFrameCallbacks()`. (Chromium `main`, read July 2026.)
- Chromium's *predicted present* time exists in the same struct — `deadline`, and the
  `present_delta` described in the `PossibleDeadline` comments as "the expected user-visible
  presentation time if work finishes on schedule" — but it is **not exposed to JavaScript**. So
  category (b) is unavailable on the web platform for general content; see §4 for the one exception.

MDN's own prose ("indicating the end time of the previous frame's rendering") is a loose gloss; the
Chromium code above is the authority and it is the frame's begin/vsync time.

---

## 3. EPOCH: `performance.now()` vs .NET `Stopwatch.GetTimestamp()` on browser-wasm

Verified end-to-end through runtime source:

| Hop | Source | Result |
|-----|--------|--------|
| `Stopwatch.GetTimestamp()` | `System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs` (dotnet/runtime `main`) — `public static long GetTimestamp() => Interop.Sys.GetTimestamp();`, `GetFrequency() => 1_000_000_000` | ns, `Frequency` = 1e9 |
| `SystemNative_GetTimestamp` | `src/native/libs/System.Native/pal_time.c` — `return minipal_hires_ticks();` | |
| `minipal_hires_ticks` | `src/native/minipal/time.c`, non-Windows branch — `clock_gettime(CLOCK_MONOTONIC, &ts)` → ns | no wasm special case |
| `clock_gettime` under emscripten | `system/lib/libc/musl/src/time/clock_gettime.c` — the `__EMSCRIPTEN__` branch calls `__wasi_clock_time_get()` | |
| `__wasi_clock_time_get` | emscripten `src/lib/libwasi.js` `clock_time_get` — `now = _emscripten_get_now(); var nsec = Math.round(now * 1000 * 1000);` | ms → ns |
| `emscripten_get_now` | emscripten `src/lib/libcore.js` — **`() => performance.now()`**; with pthreads (and no audio worklet) it becomes **`() => performance.timeOrigin + performance.now()`** | |

And Uno's own conversion, `src/Uno.UI.Composition/Composition/Compositor.cs:33,38`:

```csharp
private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency; // 1e7/1e9 = 0.01
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));          // ns/100 = 100ns ticks
```

**Conclusion.** In the default **single-threaded** browser-wasm runtime pack,
`Compositor.TimestampInTicks` *is* `performance.now()` expressed in 100 ns ticks — literally the
same JS call the rAF timestamp comes from, same `performance.timeOrigin` epoch. Converting a rAF
timestamp is then a bare multiply:

```csharp
var frameTicks = (long)(rafTimestampMs * TimeSpan.TicksPerMillisecond); // 10_000 ticks per ms
```

**With `WasmEnableThreads`** (the `Microsoft.NETCore.App.Runtime.Mono.multithread.browser-wasm`
pack; Uno detects the feature at `NativeDispatcher.wasm.cs:39-41` via
`UNO_BOOTSTRAP_MONO_RUNTIME_FEATURES`) the emscripten variant adds `performance.timeOrigin`, so
`Stopwatch` runs ~1.7e12 ms ahead of the rAF value — a **fixed** offset, but non-zero.

**So do not hardcode either.** Sample the offset once, in the same JS turn, and add it:

```csharp
// once at startup, from JS: performance.now() captured, then immediately:
_epochOffsetTicks = compositor.TimestampInTicks - (long)(perfNowMs * TimeSpan.TicksPerMillisecond);
// per frame:
var frameTicks = (long)(rafMs * TimeSpan.TicksPerMillisecond) + _epochOffsetTicks;
```

This is 0 in the single-threaded build and `performance.timeOrigin` in the threaded one, and it
costs one JS call at startup. **UNVERIFIED:** I did not read the emscripten link flags of the
shipped .NET browser-wasm runtime packs, only emscripten's own conditional. The offset sample makes
that irrelevant — and doubles as the runtime assertion that the two clocks agree (log it; if it is
neither ~0 nor ~`performance.timeOrigin`, something changed upstream).

Precision note: `performance.now()` (and therefore both sides) is privacy-coarsened —
`ClampTimeResolution` in Blink's `page_animator.cc`, and MDN documents rAF's argument as having
"a minimal precision of 1 millisecond". Even a 1 ms quantum is a ~1.3 dip error at 2650 dip/s
versus the ~4 ms (~10 dip) task-scheduling wobble it replaces, and it is quantization of an exact
grid rather than accumulating phase noise. Cross-origin-isolated pages get finer resolution.

---

## 4. Is there anything better?

| Candidate | Category | Verdict |
|-----------|----------|---------|
| `requestAnimationFrame(t)` | **(a)** frame-start/vsync | **Use this.** Already delivered, spec-guaranteed uniform per frame, correct epoch. |
| `HTMLVideoElement.requestVideoFrameCallback` → `metadata.expectedDisplayTime` | **(b)** predicted present — genuinely | Requires a playing `<video>`; fires on *video* frame cadence, not page frames; not present on all engines. Not viable as the app frame clock. `presentationTime` in the same metadata is (c). |
| Frame Timing API (`PerformanceFrameTiming`) | — | Never shipped. The 2016 W3C Working Draft was superseded; **UNVERIFIED** beyond spec-status reading, but no engine exposes the interface. |
| Long Animation Frames (`PerformanceObserver`, `long-animation-frame`, Chrome 123+) | **(c)** measured after the fact | Diagnostics only (`renderStart`, `styleAndLayoutStart`, `blockingDuration`). Cannot drive a curve. Useful later for *measuring* the record cost, not for timing it. |
| `document.timeline.currentTime` | (a) **only inside the rendering lifecycle** | **Trap — do not use as a substitute here.** MDN says it equals the rAF timestamp, which is true *during* the frame update. But Blink's `core/animation/animation_clock.cc` `AnimationClock::CurrentTime()` self-advances when read outside the lifecycle: it returns the cached `time_` only while `can_dynamically_update_time_` is false, otherwise it extrapolates with `const base::TimeDelta frame_shift = (current_time - time_) % kApproximateFrameTime;` — a hardcoded 1/60 s guess. Uno's record runs in a `postMessage` macrotask, i.e. *outside* the lifecycle, so reading it there would return a 60 Hz-extrapolated value on a 120 Hz display. Capture the rAF argument instead. |
| `navigator.scheduling`, `scheduler.postTask`, `WebXR XRFrame` predicted display time | — | Not applicable to non-immersive page content. |

### What the mature stack does

**Flutter Web** feeds the rAF timestamp directly into its frame pipeline —
`engine/src/flutter/lib/web_ui/lib/src/engine/frame_service.dart` (flutter/flutter `master`):

```dart
domWindow.requestAnimationFrame((JSNumber highResTime) {
  final int highResTimeMicroseconds = (1000 * highResTime).toInt();
  EnginePlatformDispatcher.instance.invokeOnBeginFrame(
    Duration(microseconds: highResTimeMicroseconds),
  );
```

`invokeOnBeginFrame` is what drives `SchedulerBinding`'s frame timestamp, which is what
`Ticker`/`ScrollActivity` evaluate the scroll `Simulation` against (see
`specs/scroll-smoothness/research/07-flutter-scroll-physics.md`). So Flutter's web scroll curve is
sampled on *exactly* the clock Uno currently discards. No estimator, no smoothing, no prediction.

Chrome's own scrolling never reaches script — it is compositor-side — so there is no "what does the
browser do" answer beyond "it uses `BeginFrameArgs.frame_time`", which is the same value.

---

## 5. The Android-Chrome 60 Hz / 120 Hz symptom

Reported: a WASM page in Android Chrome runs at 60 Hz untouched and 120 Hz while the finger is down.

A rAF-sourced clock handles this **automatically and for free**. rAF fires once per real page frame
at whatever rate the compositor is running; the timestamps simply arrive ~8.33 ms apart instead of
~16.67 ms. There is nothing to detect, nothing to re-learn — the value is the truth, not an
estimate of it.

The current estimator handles it *badly*, and does so precisely at the worst moment. `GetFrameTimestamp`
(`Compositor.skia.cs:244-290`) estimates the period as the median of the last `FrameClockWindow = 32`
raw deltas (`:224`, `:292-299`). On a 60→120 Hz flip:

- the median stays at ~16.67 ms until **17 of the last 32** samples are the new 8.33 ms — i.e. ~17
  frames, ~140 ms, of running the grid at half the true rate;
- during that window `_frameClock += period` (`:273`) advances twice as far as real time each frame,
  so `error` grows until `|error| >= period` and the whole-period slip branch (`:276-281`) yanks the
  grid back — a discrete jump in a position curve, i.e. a visible hitch;
- the flips coincide with **touch-down and touch-up**, so the 60→120 mis-estimate lands on the drag
  and the 120→60 mis-estimate lands on the **first ~140 ms of the fling** — the exact interval where
  velocity is highest and 2.65 dip/ms hurts most.

`FrameIntervalInTicks` (`Compositor.skia.cs:220-222`, used by drivers wanting a nominal step) is
wrong for the same window, and falls back to a hardcoded 1/60 s before 8 samples exist.

This alone justifies the change on WASM independently of the jitter argument.

---

## 6. Concrete change

Three edits plus one compositor seam. All plumbing already exists; nothing new is negotiated with
the platform.

**1. `ts/Runtime/BrowserRenderer.ts`** — accept and forward the argument (`:5`, `:11`, `:47-51`):

```ts
private readonly requestRender: (frameTimestamp: number) => void;
…
this.requestRender = ts => skiaSharpExports.Uno.UI.Runtime.Skia.BrowserRenderer.RenderFrame(this.managedHandle, ts);
…
static invalidate(instance: BrowserRenderer) {
    window.requestAnimationFrame(ts => instance.requestRender(ts));
}
```

**2. `Rendering/BrowserRenderer.cs:59-63`** — widen the `[JSExport]`:

```csharp
[JSExport]
internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance, double frameTimestampMs)
```

and in the instance `RenderFrame` (`:65`), *before* `OnNativePlatformFrameRequested` (`:98`), hand
the converted value to the compositor. Note the early-return path at `:71-79` re-arms rAF without a
record — do not stash a timestamp on that path.

**3. `Compositor.skia.cs`** — a platform-supplied frame timestamp seam:

```csharp
private long? _platformFrameTimestamp;   // set by the host at frame start, consumed by the next record
internal void SetPlatformFrameTimestamp(long ticks) => _platformFrameTimestamp = ticks;
```

and in `RenderRootVisual` (`:308-316`) prefer it over the estimator:

```csharp
var frameTimestamp = _platformFrameTimestamp is { } platform
    ? platform
    : GetFrameTimestamp(TimestampInTicks);
```

Keep `GetFrameTimestamp` as the fallback for hosts that have no platform clock; on WASM it becomes
dead weight in the steady state, which is the point.

### One design decision to make deliberately: which frame is `T_n`?

The picture recorded in the `postMessage` task after rAF(`T_n`) is drawn into the canvas during
rAF(`T_{n+1}`) and is on screen one compositor step after that. So evaluating the curve at `T_n` is
*consistent* (uniform grid, jitter gone) but is the frame-start of the frame *before* the one that
shows it. Adding whole periods, `T_n + k·period`, is now a clean, exact latency knob — `k` frames of
extrapolation, with `period` measured from consecutive real rAF timestamps rather than estimated.
That is a separate decision from this change; do the exact-clock swap first, measure, then tune `k`.
Do **not** fold a fractional or adaptive lead into the clock — that reintroduces the estimator.

### Validation (cannot be run from this environment)

The epoch claim is source-verified but should be asserted at runtime once:

1. In the same JS turn, capture `performance.now()` and call a `[JSExport]` that reads
   `Compositor.TimestampInTicks`; log
   `ticks/TimeSpan.TicksPerMillisecond - perfNowMs`. Expect ~0 (single-threaded) or
   ~`performance.timeOrigin` (threads).
2. Log 200 consecutive rAF deltas during a fling on Android Chrome across a touch-down/touch-up
   boundary; expect a clean 16.67 → 8.33 → 16.67 step with no re-convergence tail (contrast with the
   estimator's ~17-frame tail).
3. `specs/scroll-smoothness/` already has the fling-smoothness harness; re-run it with the platform
   clock enabled and disabled.

**Effort: small.** Two interop signatures, one compositor field, one startup offset sample.
**Risk: low** — the fallback path is the code that exists today.

---

## 7. Aside (out of scope, noted while reading)

`Devices/Input/BrowserPointerInputSource.cs:315-316`:

```csharp
private ulong ToTimestamp(double timestamp)
    => _bootTime + (ulong)(timestamp * 1000);
```

`_bootTime` is set from JS as `Date.now() - performance.now()` — **milliseconds** since the Unix
epoch (`ts/Runtime/BrowserPointerInputSource.ts:62`) — and is added to `evt.timeStamp * 1000`, which
is **microseconds**. The scales disagree by 1000×. It is a constant, so pointer-timestamp *deltas*
(and therefore drag velocity) are unaffected; only the absolute epoch of `PointerPoint.Timestamp` is
wrong. It would matter if anything ever compared a pointer timestamp to a frame timestamp — which
this work is edging toward. Flagging, not fixing.

Also worth knowing when that comparison gets built: Chromium dispatches coalesced pointer moves
rAF-aligned using the same value — `widget_base.cc`,
`widget_input_handler_manager_->input_event_queue()->DispatchRafAlignedInput(args.frame_time)` —
so on Chrome the input batch and the rAF timestamp are already on the same frame boundary.

---

## Sources

Uno (this worktree, branch `dev/mazi/smooth-scroll`) — cited inline by `file:line`.

External, read July 2026:
- [MDN — `Window.requestAnimationFrame()`](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame)
- [MDN — `HTMLVideoElement.requestVideoFrameCallback()`](https://developer.mozilla.org/en-US/docs/Web/API/HTMLVideoElement/requestVideoFrameCallback)
- [HTML Standard — animation frame callbacks](https://html.spec.whatwg.org/multipage/imagebitmap-and-animations.html)
- dotnet/runtime `main`: `Stopwatch.Unix.cs`, `pal_time.c`, `minipal/time.c`
- emscripten-core/emscripten `main`: [`src/lib/libcore.js`](https://github.com/emscripten-core/emscripten), [`src/lib/libwasi.js`](https://github.com/emscripten-core/emscripten), [`system/lib/libc/musl/src/time/clock_gettime.c`](https://github.com/emscripten-core/emscripten/blob/main/system/lib/libc/musl/src/time/clock_gettime.c)
- chromium/chromium `main`: `components/viz/common/frame_sinks/begin_frame_args.h`, `third_party/blink/renderer/platform/widget/widget_base.cc`, `third_party/blink/renderer/core/page/page_animator.cc`, `third_party/blink/renderer/core/animation/animation_clock.cc`
- flutter/flutter `master`: [`lib/web_ui/lib/src/engine/frame_service.dart`](https://github.com/flutter/flutter)
- [W3C Frame Timing (2016 WD)](https://www.w3.org/TR/frame-timing/) · [Long Animation Frames API](https://w3c.github.io/long-animation-frames/)
