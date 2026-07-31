# 11 — Adversarial review: does the causality hold?

Companion/refutation pass over `01-win32.md` … `08-avalonia.md`. Those notes establish *what the OS
can state* and *on which clock*. This one asks the only question that decides whether any of it is
usable: **is the value in hand, on the right thread, at the moment `FrameStarting` fires — and is the
value the record actually needs?**

Everything in §1–§3 is read from source in this worktree, file:line. Reasoned consequences are
labelled **[reasoned]**; nothing here was measured at runtime (no Skia host was executed).

---

## 0. Verdict, up front

1. **The ordering argument does not fail first at the platform boundary. It fails inside Uno, in
   shared code, on every platform at once.** `Compositor.RenderRootVisual` is reachable from **two**
   entry points, and only one of them is downstream of a platform frame callback. The other —
   `CompositionTarget.OnRenderFrameOpportunity` — records *ahead of* the frame callback it is
   accounting against. Any "publish latest platform stamp / read it at record time" scheme therefore
   assigns some records the stamp of frame callback *N* and others the stamp of *N−1*, producing an
   intermittent **0-period / 2-period** step pattern. That is strictly worse than the sub-period
   wobble the estimator already removes.

2. **The platform where the ordering argument fails outright is macOS.** There is no
   timestamp-bearing frame callback anywhere in the macOS host, and the frame pump is
   `needsDisplay` → AppKit display cycle, which has *no* ordering relationship to any display link
   you might add as an observer. `03-apple.md §4.4` concedes the latched value "can be one callback
   stale"; the real bound is **not one callback — it is unbounded**, because the two cadences are
   independent. macOS needs the pump restructured, not a timestamp bolted on.

3. **The Android note's stated ordering claim is false as written.** `02-android.md §5.1` asserts "a
   value captured in `DoFrame` at `V_k` is available to record `R_{k+1}`, which is the very next
   record. No skipped frame, no extra vsync of latency." The record is *enqueued* there; it is not
   *run* there. `NativeDispatcher.TryGetRenderAction` gates the render action behind a counted number
   of Normal-priority items (`NativeDispatcher.cs:206-234`), so the record can start one, two or ten
   dispatcher items later — and the render thread does not wait for it. Separately, the pacer's
   vsync↔wait pairing is destroyed permanently by a single `MaxWait` timeout (§4.2).

4. **The `06-winui.md §8.1` recommendation — pull `DCompositionGetFrameId` on the UI thread at
   `Compositor.skia.cs:312` — is self-defeating.** `COMPOSITION_FRAME_ID_CREATED` names *the frame
   DWM has most recently created*, which advances on DWM's clock. Reading it at record time makes the
   answer a function of *when the record started* — the exact jittery quantity being eliminated. A
   record that starts 0.5 ms either side of a compositor frame boundary gets `targetTime` values a
   whole period apart. This is aliasing, and it converts millisecond wobble into whole-frame jumps.

5. **What survives all of it:** do not ask the platform for *this record's timestamp*. Ask it for the
   **grid** — phase anchor and period — and keep a per-record counter to place records on that grid.
   That is WPF's shape (`06-winui.md §0.4`) and GTK's (`05-x11.md §4.3`), it is what the current
   estimator already is structurally, and it is immune to every ordering failure below because a grid
   anchor may be arbitrarily stale without being wrong. §7.

---

## 1. The pipeline, as actually written

### 1.1 One raise site, two record entry points

`FrameStarting` is raised in exactly one place:

```csharp
// src/Uno.UI.Composition/Composition/Compositor.skia.cs:308-316
if (FrameStarting is { } frameStarting)
{
    var frameTimestamp = GetFrameTimestamp(TimestampInTicks);
    CurrentFrameTimestampInTicks = frameTimestamp;
    try { frameStarting(frameTimestamp); }
```

`Compositor.RenderRootVisual` (`Compositor.skia.cs:301`) has exactly one caller in the frame path:
`SkiaRenderHelper.RecordPictureAndReturnPath` → `rootVisual.Compositor.RenderRootVisual(canvas, rootVisual, damage)`
(`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:44`), called from `CompositionTarget.Render()`
(`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:119`).

(`RenderTargetBitmap.skia.cs:146`, `AlphaMaskSurface.skia.cs:52`, `CompositionVisualSurface.skia.cs:21`
and `RedirectVisual.skia.cs:16` call the *`Visual`*-level `RenderRootVisual`, which does not raise
`FrameStarting`. Good — off-screen captures do not tick the drivers.)

`CompositionTarget.Render()` itself has **two** callers:

| # | Caller | Thread | Triggered by |
|---|---|---|---|
| **A** | `EnqueueRenderCallback` — `CompositionTarget.RenderScheduling.skia.cs:152` | UI | dispatcher render action, enqueued from `OnNativePlatformFrameRequested` (`:172`), which the *platform frame callback* calls |
| **B** | `OnRenderFrameOpportunity` — `CompositionTarget.RenderScheduling.skia.cs:205` | UI | `CoreServices.OnTick` (`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:124`) and `Win32WindowWrapper.SynchronousRenderAndDraw` (`src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs:421`) |

**Path B is not downstream of any platform frame callback.** This is the load-bearing fact of this
document and every per-platform section below inherits it.

### 1.2 The frame callback presents the *previous* record

`OnNativePlatformFrameRequested` (`RenderScheduling.skia.cs:166-176`) does two things in this order:

```csharp
if (Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, false))
{
    NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback);   // :172 — schedules the NEXT record
}
return Draw(canvas, resizeFunc);                                       // :175 — presents a PREVIOUS record
```

So a platform frame callback at vsync *V* **presents** a picture recorded earlier and **schedules**
the record that will be presented at some later vsync. There is a structural one-frame pipeline. That
part is fine and is what makes a category-(b) predicted-present value meaningful at all.

### 1.3 The record does not run when it is enqueued

`NativeDispatcher.EnqueueRender` (`src/Uno.UI.Dispatching/Native/NativeDispatcher.cs:237-263`) stores
the handler in `_compositionTargets[target].renderAction` and wakes the native loop. The loop drains
via `DispatchItems` → `TryGetRenderAction` (`:206-234`):

```csharp
if (details.renderAction is not null)
{
    if (details.normalItemsToProcessBeforeNextRenderAction == 0)          // :214
    {
        _compositionTargets[compositionTarget] =
            (renderAction: null,
             normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);  // :216
```

The render action is only released once the number of Normal-priority items that were queued **at the
time the previous render action ran** have been processed. The record is therefore *not* "the next
thing the UI thread does after the frame callback" on any platform. It is separated from the callback
by an unbounded amount of application work.

### 1.4 Record : present is explicitly not 1:1 — the codebase already says so

`SkiaRenderHelper.FpsHelper` maintains two distinct counters for exactly these two failures:

* `OnFrameRecorded` (`SkiaRenderHelper.skia.cs:268-284`) — "If the previous generation was never
  consumed by `Draw` before this new recording starts, that previous CPU work is wasted — count it as
  *drawn-but-not-presented*." (records > presents)
* `OnFramePresentRequested` (`:292-324`) — "If no new frame has been recorded since the previous
  `Draw`, the native VSync fired but the UI thread didn't produce anything new — we'll re-blit the
  same picture. Count that as a *dropped* frame." (presents > records)

**Consequence [reasoned]:** at record time, the record does not and cannot know which vsync it will be
shown at. Therefore *no* per-record assignment of a specific vsync timestamp — measured, predicted, or
otherwise — can be correct in general. Only a **grid** can be.

---

## 2. The universal refutation: `OnRenderFrameOpportunity`

### 2.1 What it does

```csharp
// CompositionTarget.RenderScheduling.skia.cs:178-208
internal void OnRenderFrameOpportunity()
{
    // If we get an opportunity to get call Render earlier than EnqueuePaintCallback, then we do that
    // but skip the Render call in the next EnqueuePaintCallback so that overall we're still keeping
    // the rate of Render calls the same.
    ...
    if (RenderRequested && !_renderedAheadOfTime) { RenderRequested = false; _renderedAheadOfTime = true; shouldRender = true; }
    if (shouldRender) { Render(); }        // :205 — FrameStarting fires here
}
```

and the matching skip in `EnqueueRenderCallback` (`:131-144`): when `_renderedAheadOfTime`, the frame
callback's render action **does not record**.

So the design deliberately keeps *record count* equal to *frame-callback count*, but it **moves one
record to before its frame callback instead of after it.**

### 2.2 Why it fires during a fling

`RenderRequested` is set every frame while a driver is subscribed: `RenderRootVisual` ends with

```csharp
// Compositor.skia.cs:373-376
if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
{
    rootVisual.CompositionTarget?.RequestNewFrame();
}
```

and `CoreServices.OnTick` (`CoreServices.cs:77-127`) is a **Normal**-priority dispatcher item
(`:73`) that ends by calling `OnRenderFrameOpportunity` (`:124`). It is armed by
`CoreServices.RequestAdditionalFrame()`, whose callers include:

* `EventManager.cs:34` — the **effective-viewport changed queue**. This fires continuously while a
  virtualized list scrolls.
* `EventManager.cs:69` — `RequestRaiseLoadedEventOnNextTick` (element realization during scroll).
* `XamlRoot.crossruntime.cs:18, 26` — `InvalidateMeasure` / `InvalidateArrange` on the root.

**During an inertial scroll over a virtualized list, path B is not an edge case. It is the common
case.** [reasoned — from the call graph; not instrumented.]

### 2.3 The failure, concretely

Let `V_k` be the platform frame callback / vsync stamp published for callback *k*, and suppose the
compositor reads "latest published value" (or receives it captured at `EnqueueRender` time) at record
time.

```
steady state, path A only
  V_k published  →  record R_k stamped V_k  →  present at V_{k+1}          Δstamp = 1 period ✓

one path-B record inserted
  V_{k-1} published
  … CoreServices.OnTick runs before the frame callback for k …
  record R_k stamped V_{k-1}                                              Δstamp = 0 periods ✗
  V_k published → EnqueueRenderCallback SKIPS (RenderScheduling.skia.cs:131-144)
  V_{k+1} published → record R_{k+1} stamped V_{k+1}                      Δstamp = 2 periods ✗
```

Two consecutive records, presented one refresh apart, receive timestamps two periods apart — and the
pair before them receives timestamps zero periods apart. For a fling whose position is
`x(t) = f(t − _flingStartTimestamp)` (`ScrollContentPresenter.Managed.cs:628`) that renders as **one
frozen frame followed by a double-length step**. At the measured launch velocity (~2650 dip/s, 120 Hz)
that is a ~22 dip discontinuity — an order of magnitude worse than the 4 ms / ~10 dip wobble this
whole workstream exists to remove.

The current estimator does not have this failure mode: `GetFrameTimestamp` advances `_frameClock` by
exactly one median period **per call** (`Compositor.skia.cs:273`) regardless of who called it, and
only slips in whole periods when the raw error exceeds a full period (`:276-281`). Records are counted,
not timed. **That property is the estimator's real virtue and it must be preserved by any replacement.**

### 2.4 What a correct fix must do

Any platform-timestamp scheme must therefore either

* **(i)** carry the stamp as *data* attached to the specific record request — and give path B a
  *derived* stamp (`last published + 1 period`, because a path-B record is claiming the *next* frame
  callback's slot), or
* **(ii)** suppress path B while `HasFrameStartingSubscribers` is true (`Compositor.skia.cs:211`
  already exposes the flag) — at the cost of the latency path B exists to buy, or
* **(iii)** stop assigning per-record timestamps at all and use a counted grid (§7).

`01-win32.md §7b` gets halfway there — it correctly rejects "read a shared latest field at record
time" and proposes capture-at-enqueue — but it does not consider path B, so its captured value still
lands on the wrong record whenever `OnRenderFrameOpportunity` wins the race.

---

## 3. Per-platform ordering verdict

Legend for **(a)**: is a timestamp for the frame *about to be recorded* obtainable at `FrameStarting`
time? **(b)**: is "previous vsync + one period" a correct and safe prediction?

| Host | Frame callback carries a time? | Thread | (a) usable at `FrameStarting`? | (b) prev+period safe? |
|---|---|---|---|---|
| Win32 software | no (`DwmFlush` return only) | render | via publish — **after** the flush of the *previous* iteration | yes, with a staleness guard |
| Win32 Vulkan | same | render | same | same |
| **Win32 OpenGL (default path)** | **no pacer at all** | render | **no** — `_pacer == null` when following refresh (`Rendering.OpenGl.cs:146-148`) | n/a |
| Android Vulkan | yes, `frameTimeNanos`, discarded (`ChoreographerFramePacer.cs:99`) | private Looper | only via publish, and **racy** (§4.1) | yes *if* the pairing survives (§4.2 — it does not) |
| **Android GL** | **no Choreographer anywhere** (`UnoSKCanvasView.cs:52,61-64,160`) | GLSurfaceView GLThread | **no** | n/a |
| iOS / tvOS / Catalyst | yes, `_link.Timestamp` / `.TargetTimestamp` live in the callback, discarded (`UnoSKMetalView.cs:37`) | dedicated runloop thread | **yes**, structurally the cleanest | yes — `TargetTimestamp` *is* the prediction |
| **macOS** | **nothing exists** (`UNOMetalViewDelegate.m:35`) | AppKit main | **no** | **no** (§5) |
| X11 / XWayland | **no vsync signal at all** — `System.Timers.Timer` (`FramePacer.cs:19,31`) | thread-pool timer → render thread | no | n/a |
| Linux framebuffer (DRM) | yes, hardware vblank `(sequence, tv_sec, tv_usec)`, all discarded (`DRMRenderer.cs:377-393`) | `PageFlipLoop` poll thread | **yes** — arrives immediately before `Render()` (`:388`) | yes, and `sequence` makes drops *exact* |
| WASM | yes, rAF `DOMHighResTimeStamp`, discarded (`BrowserRenderer.ts:47-51`) | the only thread | **yes**, structurally guaranteed | yes |
| Headless / Tizen | none | — | no | n/a |

---

## 4. Android — the note's ordering claim, tested

### 4.1 The claim vs. the loop

`02-android.md §5.1` reproduces the loop correctly:

```csharp
// src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:143-159
while (_surfaceReady && !_disposed)
{
    _renderEvent.Wait(TimeSpan.FromMilliseconds(100));   // :146
    _renderEvent.Reset();                                // :147
    if (!_surfaceReady || _disposed || !_renderRequested) continue;
    _renderRequested = false;
    RenderFrame();                                       // :153 → EnqueueRender + Draw + vkQueuePresent
    _pacer.WaitForNextFrame();                           // :158 → arms Choreographer, blocks until V_k
}
```

and then concludes "the record for iteration `k+1` is enqueued immediately after the wait for `V_k`
returns … a value captured in `DoFrame` at `V_k` is available to record `R_{k+1}`, which is the very
next record."

**The enqueue is adjacent to the wake. The record is not.** By §1.3 the record for iteration *k+1* is
released by `TryGetRenderAction` only after the counted Normal items drain. Meanwhile the render
thread does not block on it — it presents whatever `_lastRenderedFrame` holds and proceeds. So the
main thread may still be executing (or not yet have started) record *k+1* when `V_{k+1}` fires and
overwrites the published value. **A "latest published value" read inside `FrameStarting` on Android
can legitimately return `V_{k-1}`, `V_k` or `V_{k+1}` for the same record**, depending on main-thread
load — and main-thread load is precisely what varies frame to frame. That is not a smaller version of
the current jitter; it is the same jitter quantized to whole periods.

Capture-at-enqueue (`01-win32.md §7b`'s shape) fixes this specific race. It does not fix §2, and it
does not fix §4.2.

### 4.2 The pacer desynchronises permanently after one timeout — a real bug

```csharp
// src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:30, 66-76
private static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(100);
...
public void WaitForNextFrame()
{
    if (_disposed || !_ready.Wait(MaxWait) || _handler is null) return;
    _handler.Post(() => _choreographer?.PostFrameCallback(_callback!));   // :74
    _vsync.WaitOne(MaxWait);                                             // :75
}
```

`_vsync` is an `AutoResetEvent(false)` (`:32`) and `RemoveFrameCallback` is called only from `Dispose`
(`:89`).

**[reasoned]** If `WaitOne` times out (app backgrounded, surface gone, compositor stalled — exactly
the cases `MaxWait` exists for), the posted frame callback is still registered. When it eventually
fires, `_vsync.Set()` **latches**. The next `WaitForNextFrame` posts a *second* callback and then
returns immediately by consuming the latched set. From that point on there is permanently one extra
outstanding callback: every wait is satisfied by the *previous* frame's vsync, and the loop's phase
relative to the display is shifted by a full period with no way to recover. Two consequences:

* pacing quality silently degrades and nothing reports it;
* any "the *k*-th vsync stamp belongs to the *k*-th iteration" bookkeeping is wrong from then on, in a
  way no staleness guard detects — the value is *fresh*, just attributed to the wrong frame.

Fix regardless of the clock work: `RemoveFrameCallback` on the timeout path, and/or replace the
`AutoResetEvent` with a monotonically-increasing frame counter the waiter compares against
(`Interlocked.Read` of a `long` sequence + `SpinWait`/`Monitor`), so a missed vsync is *observable*
rather than silently re-attributed.

### 4.3 (c) dropped frame / rate change

* **Dropped frame.** `frameTimeNanos` is monotonic and skips; a `sequence`-free consumer sees a
  2-period delta and must slip by whole periods, which the current estimator already does
  (`Compositor.skia.cs:276-281`). Preserved.
* **Rate change (120→60).** `frameTimeNanos` deltas track it after the fact — one wrong frame.
  `ExpectedPresentationTimeNanos` (API 33+) tracks it immediately; `02-android.md §5.3` is right that
  this is the one place the predicted value genuinely beats the raw one. Note the estimator's
  `MedianFrameDelta()` over a 32-sample window (`Compositor.skia.cs:292-299`) needs ~17 frames to
  cross over on a rate change — a real, if brief, defect the platform period would fix.
* **Timeout with no vsync.** Published value freezes. A driver reading a frozen stamp **freezes the
  fling**; `ScrollDecaySimulation.Tick` tolerates it (`elapsed <= 0 → return true`,
  `ScrollDecaySimulation.cs:61-64`) but `OnFlingFrame` does not — it recomputes an absolute position
  from `_flingStartTimestamp` (`ScrollContentPresenter.Managed.cs:628`) and will simply re-emit the
  same offset. Any platform-stamp path needs an explicit freshness bound.

### 4.4 (d) cross-thread publication

`Compositor` is the shared instance (`Compositor.GetSharedCompositor()`), and the writer is a
different thread on every host except WASM. Requirements:

* **Torn reads.** A `long` timestamp is *not* guaranteed atomic on 32-bit runtimes, and Uno ships
  32-bit targets (android-arm, wasm32). The codebase already knows this —
  `SkiaRenderHelper.skia.cs:170-171`: *"TimeSpan ticks (100ns units); accessed across threads via
  `Interlocked` to avoid torn reads on 32-bit."* Use `Interlocked.Read`/`Exchange`, **not**
  `Volatile.Read/Write` on a `long`. `02-android.md §5.1`'s "a plain `Volatile.Write` … /
  `Volatile.Read`" is wrong on 32-bit ARM.
* **Multi-value consistency.** Timestamp + period + sequence must be observed as a *set*. Three
  independent `Interlocked` ops are not a transaction. Either pack into one 64-bit word, publish an
  immutable record via a single reference write (reference stores are atomic and
  `Volatile.Write<object>` gives the release fence), or use a seqlock.
* **Ordering.** Release on publish / acquire on read is required so the consumer cannot observe a new
  sequence with an old timestamp. `Interlocked.Exchange` is a full fence; a lone `Volatile.Write` on
  the second field of a pair is not sufficient.
* **Multi-window / multi-display.** `FrameStarting` and the estimator live on the **shared**
  compositor, but every frame source is per-window. Two windows on 60 Hz and 120 Hz displays publish
  into the same field and alternate. `03-apple.md §4.3` flags this as "not a regression"; that is only
  true for the *estimator*, whose whole-period slip absorbs it. A stamped-value scheme makes it a
  hard, per-frame alternation. **If platform stamps land, the state must move to `CompositionTarget`.**

---

## 5. macOS — where the argument fails outright

Verified chain:

```
MacOSWindowHost.cs:288   InvalidateRender() => NativeUno.uno_window_invalidate(handle)
UNOWindow.m:389-395      uno_window_invalidate → renderingView.needsDisplay = true
UNOWindow.m:324          v.enableSetNeedsDisplay = YES   (on UNOMetalFlippedView : MTKView)
UNOMetalViewDelegate.m:35-48  - (void)drawInMTKView:(MTKView *)view      ← no timestamp parameter
MacOSWindowHost.cs:107   OnNativePlatformFrameRequested(...)
```

`drawInMTKView:` carries **nothing**. There is no `CADisplayLink`, no `CVDisplayLink`, no timestamp of
any kind in the macOS host. `03-apple.md §1.3` states this correctly.

Where §4.4 of that note goes wrong is the mitigation. It proposes attaching a display link as an
**observer** while keeping `needsDisplay` scheduling, and reasons that "`drawRect`-driven
`drawInMTKView:` still lands near vsync — but it means the latched value can be one callback stale."

**That bound is not justified.** An observer display link and the AppKit display cycle are two
independent producers with no handshake. The latched value at record time is "whichever callback most
recently ran", and the record runs on the UI thread through the same gated dispatcher queue as
everywhere else (§1.3), plus path B (§2). The stale-ness is bounded only by main-thread latency, and
the *aliasing* — which callback you happen to catch — is bounded by nothing at all. Feeding that
directly into `x(t)` reproduces §2.3's 0-period/2-period pattern with a higher duty cycle.

Additionally, `MTKView.enableSetNeedsDisplay = YES` with `paused` never explicitly set means the
interaction between the internal display link and `needsDisplay` scheduling is **UNVERIFIED** here
(Apple's documented pairing is `enableSetNeedsDisplay = YES` *with* `paused = YES`; nothing in
`UNOWindow.m` or `UNOMetalViewDelegate.m` sets `paused`). Whether macOS frames are currently
vsync-phase-locked *at all* is therefore an open question, and it should be answered before any clock
work — it may be the larger smoothness defect on that host.

**Verdict:** macOS cannot be fixed by publishing a timestamp. It needs the pump restructured to
link-driven (iOS shape), or it stays on the estimator. Do not ship an observer-link stamp there.

---

## 6. Win32 — the two proposals, both re-examined

### 6.1 `01-win32.md`: publish `qpcVBlank` from the render thread

The loop (`Win32WindowWrapper.RenderThread.cs:52-90`):

```
56  _frameSignal.WaitOne()
65  StartPaint()
68  _drawFrame()      → OnNativePlatformFrameRequested → EnqueueRender(:172) ; Draw(:175)
73  CopyPixels(w,h)   → BitBlt/present, then _pacer.WaitForNextFrame() → DwmFlush (Vulkan: Rendering.Vulkan.cs:131)
86  EndPaint()
```

**(a)** The `DwmFlush` at step 73 happens *after* the `EnqueueRender` at step 68. So the freshest
vblank available at the top of iteration *k* is the one sampled at the end of iteration *k−1*. That is
fine — per `01-win32.md §4`, `qpcVBlank` sampled right after `DwmFlush` is already ~0.93 periods in
the future, i.e. it names the vblank that *begins* iteration *k*. **The value is causally available
before the record it should govern. §7b's capture-at-enqueue is sound.** ✔

**(b)** "previous vsync + one period" is not needed here — the sampled value is already forward-looking.
Adding lead on top would be a latency change; `01-win32.md §7c` is right to defer it, and right about
why (the drag path latches finger geometry and reads no clock, so leading only inertia puts a step at
the handoff).

**(c)** Dropped frame: `cRefresh` not advancing is the exact detector, already proposed. Rate change:
`qpcRefreshPeriod` is re-read per frame, strictly better than `MedianFrameDelta()`.

**What §7b misses:** path B (§2). And a second signal source — `Win32WindowWrapper.cs:414-432`
`SynchronousRenderAndDraw` calls `OnRenderFrameOpportunity()` (`:421`) and *then*
`_renderThread?.SignalNewFrame()` (`:424`) on resize/move/show. That is path B firing by construction,
plus an extra render-thread iteration that presents without a matching record.

**The renderer coverage gap is larger than §7d admits.** `GlRenderer` on the *default* path has no
pacer at all:

```csharp
// src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32WindowWrapper.Rendering.OpenGl.cs:146-148
var pacer = followRefreshRate ? null
    : new Win32RenderPacer(FeatureConfiguration.CompositionTarget.FrameRate, followRefreshRate: false);
```

`followRefreshRate` is `SetFrameRateAsScreenRefreshRate` (`:118`), whose default is the follow-refresh
path — so the shipped OpenGL renderer never calls `DwmFlush` and has no sample point. Renderer
selection is Vulkan → OpenGL → Software (`Win32WindowWrapper.cs:109-115`), so **OpenGL is the fallback
that most machines without Vulkan land on.** The win32 proposal covers software and Vulkan only.

### 6.2 `06-winui.md §8.1`: pull DComp stats on the UI thread — reject

> "On the UI thread at `Compositor.skia.cs:312`, before raising `FrameStarting`:
> `DCompositionGetFrameId(COMPOSITION_FRAME_ID_CREATED, out id)` → `DCompositionGetStatistics(...)`"

This trades a cross-thread hazard for an **aliasing** hazard, and aliasing is worse.

`COMPOSITION_FRAME_ID_CREATED` returns the id of the most recently *created* compositor frame. That
counter advances on DWM's clock, asynchronously to the record. Reading it at record time means:

* the record instant's millisecond wobble — the defect — determines *which* frame id is returned;
* records straddling a compositor frame boundary alternate between `targetTime` values one full period
  apart;
* §2.3's 0/2 pattern, again, now driven directly by the quantity we set out to eliminate.

The note's own §5.1 data shows the mechanism: "when nothing is being composited, the frame id does not
advance and `targetTime` goes stale (rows 1–3 repeat frame 2356559 with `target-wake` marching
backwards by exactly one period each wake)" — i.e. the pull is only meaningful *when synchronised
with the compositor clock*, which is exactly what `DCompositionWaitForCompositorClock` does and what
the record does not do. The measurement in §5.1 was taken **inside a
`DCompositionWaitForCompositorClock` loop**; the recommendation in §8.1 discards that
synchronisation and keeps the numbers.

Its §5.2 also records "`targetDelta == 0.0000` rows are two distinct frame ids sharing one
`targetTime`" — a repeated timestamp. `OnFlingFrame` (`ScrollContentPresenter.Managed.cs:619-646`)
recomputes an absolute position, so a repeat renders as a frozen frame. Must be guarded.

**If DComp is used at all, it must be sampled from a thread woken by
`DCompositionWaitForCompositorClock` and published, exactly like `qpcVBlank` — or used only as a grid
anchor (§7), where staleness is harmless.**

### 6.3 A contradiction between the two notes, resolved

* `01-win32.md §8` measured `DwmGetCompositionTimingInfo(NULL, …)` **succeeding**: `hwnd=NULL:
  cbSize=292 -> S_OK`, with a full plausible field dump.
* `06-winui.md §5.3` measured the same call **failing** with `0x88980090` "from a console process on
  this build", and concluded "do not build on it".

Same machine, same build, opposite results. The resolution is in the Windows SDK:

```
C:\Program Files (x86)\Windows Kits\10\Include\10.0.26100.0\shared\winerror.h:61059-61065
// MessageId: MILERR_MISMATCHED_SIZE
#define MILERR_MISMATCHED_SIZE           _HRESULT_TYPEDEF_(0x88980090L)
```

`0x88980090` **is** `MILERR_MISMATCHED_SIZE` — the `cbSize` failure `01-win32.md §7a` explicitly warns
about ("If CsWin32 emits a naturally-aligned 320-byte struct, the call fails with
`MILERR_MISMATCHED_SIZE` and the whole feature silently degrades"). `06-winui.md §5.3`'s probe hit its
own struct-packing bug and attributed it to the API. **§5.3's conclusion should be withdrawn**; the
`Pack = 1` / `cbSize = 292` requirement is real and is the single highest-value line in either note.

---

## 7. What survives: anchor the grid, count the records

Every failure above is a failure of **assignment** — attaching a specific platform instant to a
specific record. None of them is a failure of the platform *data*. So do not assign.

**Design.** Keep `GetFrameTimestamp`'s structure exactly as it is — an internal clock advanced by one
period per call (`Compositor.skia.cs:273`), with whole-period slip on large error (`:276-281`) and
gentle phase pull on small error (`:286`). Replace only its two *estimated* inputs with *stated* ones:

| Today | Replace with |
|---|---|
| `MedianFrameDelta()` over 32 record deltas (`:292-299`) | the platform's stated period: `qpcRefreshPeriod`, `COMPOSITION_FRAME_STATS.framePeriod`, `targetTimestamp − timestamp` (CADisplayLink), `frameTimeNanos` deltas / `expectedPresentationTimeNanos`, DRM mode line (`DRMRenderer.CalculateRefreshRate`, `:264`), XRandR mode line (`X11DisplayInformationExtension.cs:492-500`) |
| phase pulled toward `Stopwatch.GetTimestamp()` at record time (`:274`) | phase pulled toward the most recent **published platform vsync**, with hysteresis |

Why this is immune to §2–§6:

* **Staleness is harmless.** A grid anchor two frames old still defines the same grid. The phase pull
  is a slow filter, not a per-frame assignment.
* **Aliasing is harmless.** An anchor that occasionally lands one period off moves the phase estimate
  by `period/16`, not by a period.
* **Path B is harmless.** The grid advances once per record regardless of who called `Render()`.
* **Dropped frames still work** through the existing whole-period slip, and the DRM `sequence` /
  DWM `cRefresh` / Choreographer frame counters make the slip *exact* instead of inferred.
* **Rate changes are immediate** instead of taking ~17 frames of median window.
* **Cross-thread publication becomes trivial**: a single `Interlocked.Exchange` of a `long`, no
  transaction, no pairing invariant, no per-window state required for correctness (only for quality).

This is WPF's `_estimatedNextPresentationTime` shape (`06-winui.md §0.4`: "a phase-locked grid derived
from real vsync data, i.e. exactly Uno's estimator but with a real anchor") and GTK's
(`05-x11.md §4.3`, which notes GTK *has* a real presentation time on X11 **and still smooths it**).
Both mature stacks that had the real number chose not to use it raw. That is the strongest available
evidence that raw assignment is the wrong shape.

**Ordering of work, revised:**

0. Fix `ChoreographerFramePacer`'s timeout desync (§4.2) — independent bug, ship first.
1. Feed the estimator the **stated period** everywhere it is already known (Win32 `qpcRefreshPeriod`,
   X11 XRandR, DRM mode line, iOS `targetTimestamp − timestamp`). No ordering hazard, no new API on
   three of four. This alone removes the 32-sample warm-up and the rate-change lag.
2. Feed the estimator a **phase anchor** on the hosts where one is already in hand and free: DRM
   (`OnPageFlip`, `DRMRenderer.cs:388` — a genuine hardware vblank sitting in an unused parameter),
   WASM (rAF argument, `BrowserRenderer.ts:48`), iOS (`_link.Timestamp`, `UnoSKMetalView.cs:37`).
   Anchor only — do not assign.
3. Only then consider per-record assignment, and only on hosts where §2 has been closed and the
   ordering is structural (WASM; iOS; Win32 software/Vulkan with capture-at-enqueue).
4. macOS and Win32-OpenGL: estimator, until their pumps are restructured.

**One thing to fix before any phase change ships.** Two drivers anchor on a *raw* clock and then
receive *grid* values, which is fine today (the grid stays within a period of raw) and breaks the
moment the grid gains a forward phase offset:

* `ScrollContentPresenter.Managed.cs:669` — `var now = compositor.TimestampInTicks;` seeds
  `ScrollDecaySimulation.Start`, whose `Tick` then receives the grid value (`:707-708`).
* `GestureRecognizer.Manipulation.InertiaProcessor.cs:355` — `_startTimestamp = compositor.TimestampInTicks;`
  and the handler subtracts it from the grid value (`:356`).

The fling path already does this correctly by anchoring on the *first frame's* timestamp
(`ScrollContentPresenter.Managed.cs:597, 621-625`). The other two should follow. With a
predicted-present grid (~2 periods ahead of the record instant) the current form injects a ~17 ms
head-start into the first frame of every wheel decay and every inertia manipulation.

---

## 8. Epoch, restated concretely

Only the parts load-bearing for §7. `Compositor.TimestampInTicks` is
`(long)(Stopwatch.GetTimestamp() * s_tickFrequency)` with
`s_tickFrequency = TimeSpan.TicksPerSecond / Stopwatch.Frequency`
(`src/Uno.UI.Composition/Composition/Compositor.cs:32-38`).

| Platform | Platform stamp | .NET `Stopwatch.GetTimestamp()` | Relationship | Source |
|---|---|---|---|---|
| Windows | `qpcVBlank`, `COMPOSITION_FRAME_STATS.targetTime` — QPC | `QueryPerformanceCounter` | **identical counter**; QPF = 10 MHz here so `s_tickFrequency == 1.0` exactly | measured in `01-win32.md §2` (2000 interleaved samples, 0-tick out-of-bracket skew); mechanism inferred, **runtime source not read** |
| Android | `frameTimeNanos` — `System.nanoTime()` → `CLOCK_MONOTONIC` | `CLOCK_MONOTONIC` via minipal | same epoch; `ticks = nanos / 100` | `02-android.md §3.1-3.4` (read `dotnet/runtime` `src/native/minipal/time.c`) — **not re-verified here, no local clone** |
| Apple | `CACurrentMediaTime()` / `CADisplayLink.timestamp` — `mach_absolute_time` | `CLOCK_UPTIME_RAW` = `mach_absolute_time` | same epoch, seconds ↔ ticks | `03-apple.md §3.1-3.4` — **not re-verified here** |
| Linux | DRM `tv_sec`/`tv_usec`, X11 UST — `CLOCK_MONOTONIC` | `CLOCK_MONOTONIC` | same epoch; `ticks = µs * 10` | `05-x11.md §3.1` — **not re-verified here** |
| WASM | rAF `DOMHighResTimeStamp` — `performance.now()` origin | browser-wasm `Stopwatch` | `04-wasm.md §3` — **not re-verified here** |

**For the §7 design the epoch question is much weaker than it is for assignment.** A grid anchor only
needs to be on *a* uniform clock whose rate matches `Stopwatch`; a constant offset is absorbed by the
phase filter within a few frames. Epoch precision matters only if per-record assignment is adopted
(step 3).

---

## 9. Explicitly UNVERIFIED

* **No runtime measurement was taken for this note.** Every ordering consequence in §2.3, §4.1, §4.2
  and §5 is derived from source, not observed. The single highest-value experiment is to log
  `(CurrentFrameTimestampInTicks, path A|B, present index)` per frame during a fling over a
  virtualized list and count how often path B wins. If path B is rare in practice, §2 downgrades from
  "universal refutation" to "must-guard edge case".
* Whether `MTKView.paused` is effectively `YES` on macOS given `enableSetNeedsDisplay = YES`
  (`UNOWindow.m:324`), and therefore whether macOS frames are vsync-phase-locked today at all.
* Whether `Choreographer.PostFrameCallback` on the pacer's private Looper
  (`ChoreographerFramePacer.cs:53`) is delivered with the same `frameTimeNanos` the main Looper would
  receive (expected yes — one `DisplayEventReceiver` per thread, same SurfaceFlinger vsync — but not
  verified).
* The steady-state behaviour after the §4.2 desync (whether the loop free-runs or merely shifts phase)
  is reasoned, not measured.
* `dotnet/runtime` was **not** read for this note (no local clone at `D:\Work`). All epoch claims in
  §8 are inherited from `02-android.md` / `03-apple.md` / `05-x11.md` / `04-wasm.md`, which do cite
  runtime source, or from `01-win32.md`'s measurement.
* Tizen (`src/Uno.UI.Runtime.Skia.Tizen`) was not examined.
* `X11EGLRenderer` / `X11VulkanRenderer` / `X11OpenGLRenderer` were not read individually; the X11
  ordering verdict rests on `X11XamlRootHost.Rendering.cs:34-59` and `FramePacer.cs`, which pace all
  of them.

## 10. Sources read in this worktree

`Compositor.skia.cs` · `Compositor.cs` · `CompositionTarget.Rendering.skia.cs` ·
`CompositionTarget.RenderScheduling.skia.cs` · `SkiaRenderHelper.skia.cs` · `NativeDispatcher.cs` ·
`CoreServices.cs` · `EventManager.cs` · `XamlRoot.crossruntime.cs` ·
`ScrollContentPresenter.Managed.cs` · `ScrollDecaySimulation.cs` ·
`GestureRecognizer.Manipulation.InertiaProcessor.cs` · `Win32WindowWrapper.RenderThread.cs` ·
`Win32WindowWrapper.Rendering.cs` · `Win32WindowWrapper.Rendering.Vulkan.cs` ·
`Win32WindowWrapper.Rendering.OpenGl.cs` · `Win32RenderPacer.cs` · `Win32WindowWrapper.cs` ·
`FramePacer.cs` · `ChoreographerFramePacer.cs` · `UnoSKVulkanView.cs` · `UnoSKCanvasView.cs` ·
`UnoSKMetalView.cs` · `RootViewController.cs` · `MacOSWindowHost.cs` · `UNOWindow.m` ·
`UNOMetalViewDelegate.m` · `UNOSoftView.m` · `X11XamlRootHost.Rendering.cs` · `DRMRenderer.cs` ·
`FrameBufferRenderer.cs` · `SoftwareRenderer.cs` · `BrowserRenderer.ts` · `BrowserRenderer.cs` ·
`HeadlessWindowWrapper.cs`.

External: `C:\Program Files (x86)\Windows Kits\10\Include\10.0.26100.0\shared\winerror.h:61059-61065`.
