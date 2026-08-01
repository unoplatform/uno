# 02 — CompositionTarget render scheduling as the cause of fling "dropped" frames

Scope: `CompositionTarget.RenderScheduling.skia.cs` + `CompositionTarget.Rendering.skia.cs` state
machine, its interaction with `NativeDispatcher`, `CoreServices.RequestAdditionalFrame`, and the
Android (Vulkan/Choreographer) present loop.

Headline: **the `_renderedAheadOfTime` + `_renderRequestedAfterAheadOfTimePaint` reschedule branch
(`CompositionTarget.RenderScheduling.skia.cs:131-139`) burns an entire present cycle without
recording a picture, and a fling is the only one of the three cases that reaches it
deterministically.**

---

## 0. Two corrections to the framing before anything else

### 0.1 "dropped" is NOT `_lastRenderedFrame == null`

The task asks me to enumerate states where "a native vsync arrives and `Draw` finds
`_lastRenderedFrame` null". That is not what the counter measures, and the code says so explicitly.

`FpsHelper.OnFramePresentRequested` (`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:292-324`) is
generation-based:

```csharp
var current = Interlocked.Read(ref _currentFrameGeneration);
var lastPresented = Interlocked.Read(ref _lastPresentedGeneration);
if (current == 0) { return; }              // :304-307 startup guard
if (current == lastPresented) { Interlocked.Increment(ref _droppedThisSecond); return; }  // :309-313
```

and the comment at `SkiaRenderHelper.skia.cs:181-184` states the reason:

> Mismatches give us dropped-vs-unpresented accounting **without relying on `_lastRenderedFrame`,
> which is always re-populated by `CompositionTarget.ReturnFrame` after each `Draw`.**

`_currentFrameGeneration` is incremented only in `OnFrameRecorded` (`SkiaRenderHelper.skia.cs:283`),
called from `Render()` (`CompositionTarget.Rendering.skia.cs:157`). So:

> **dropped++ ⟺ a `Draw` ran with no `Render()` since the previous `Draw`.**

For completeness, the states where `Draw` genuinely finds `_lastRenderedFrame == null`
(`CompositionTarget.Rendering.skia.cs:233-246`) are:

| # | State | Reachable in our three cases? |
|---|---|---|
| N1 | Before the first `Render()` ever ran (startup, surface created before first record) | Yes, once. **Not counted** — `current == 0` guard, `SkiaRenderHelper.skia.cs:304-307`. |
| N2 | A second `Draw` re-enters while a first `Draw` is between the borrow (`:238`) and `ReturnFrame` (`:310`/`:258`) | **No.** Android has exactly one render thread (`UnoSKVulkanView.cs:84-89`), and it is the sole caller of `OnNativePlatformFrameRequested` (`UnoSKVulkanView.cs:215`). Same on Win32 (`Win32WindowWrapper.RenderThread.cs:52-90`). |
| N3 | `XamlRootMap.Unregistered` → `FailPendingRenderJobs`/window teardown | Not during scroll. |

So N1–N3 are **not** the explanation, and I am discarding that framing. The rest of this document
answers the real question: **which scheduling states let a `Draw` run with no intervening
`Render()`?**

### 0.2 On Android, `Draw` is *pull*-driven, not vsync-driven

The task describes "the native vsync fired and `Draw` ran". That is not how the Android Vulkan view
works (`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:137-171`):

```csharp
while (_surfaceReady && !_disposed)
{
    _renderEvent.Wait(TimeSpan.FromMilliseconds(100));
    _renderEvent.Reset();
    if (!_surfaceReady || _disposed || !_renderRequested) continue;
    _renderRequested = false;
    RenderFrame();                  // → OnNativePlatformFrameRequested → Draw
    _pacer.WaitForNextFrame();      // ChoreographerFramePacer, blocks to the next vsync
}
```

`_renderRequested`/`_renderEvent` are set **only** by `IXamlRootHost.InvalidateRender()`
(`UnoSKVulkanView.cs:60-65`). Choreographer is a *rate limiter after the fact*
(`ChoreographerFramePacer.cs:80-102`), not the trigger. An idle app does no work at all.

Therefore:

> **A `Draw` happens ⟺ somebody called `InvalidateRender()`.
> A drop is an `InvalidateRender()` that was not backed by a fresh `Render()`.**

This is the load-bearing reframing. It makes the whole problem enumerable, because there are only
four callers of `InvalidateRender`.

---

## 1. State machine map

### 1.1 Fields (`CompositionTarget.RenderScheduling.skia.cs:69-84`)

| Field | Meaning | Written by |
|---|---|---|
| `_renderRequested` (via `RenderRequested`) | "content changed; a record is owed" | `RequestNewFrame` :95, `EnqueueRenderCallback` :149, `OnRenderFrameOpportunity` :194 |
| `_renderedAheadOfTime` | "we already recorded this cycle's picture early, so the next scheduled render action must *skip*" | `OnRenderFrameOpportunity` :195 (set), `EnqueueRenderCallback` :133 (clear) |
| `_renderRequestedAfterAheadOfTimePaint` | "new content arrived *after* the early record — the early picture is already stale" | `RequestNewFrame` :100 (set), `EnqueueRenderCallback` :136 (clear) |
| `_shouldEnqueueRenderOnNextNativePlatformFrameRequested` | one-shot arming so exactly one render action is queued per present | `EnqueueRenderCallback` :125 (arm), `OnNativePlatformFrameRequested` :170 (consume) |

Invariants asserted at `:210-218`: `_rRAAOTP ⇒ _ahead`, and `_ahead ⇒ !RenderRequested`.

### 1.2 Transitions

**`RequestNewFrame()` — `:86-118`**

| Entry state | Effect | `InvalidateRender()`? |
|---|---|---|
| `!_ahead && !RenderRequested` | `RenderRequested = true` | **YES (:110)** — *speculative*: no picture exists yet |
| `!_ahead && RenderRequested` | nothing | no |
| `_ahead` (:98-101) | `_rRAAOTP = true` | **no** |

**`OnRenderFrameOpportunity()` — `:178-208`** (UI thread; *the only entry to the ahead-of-time path*)

- Gate 1: `SkiaRenderHelper.CanRecordPicture(root)` (:185) — layout must be clean.
- Gate 2: `RenderRequested && !_renderedAheadOfTime` (:192).
- Effect: `RenderRequested = false; _renderedAheadOfTime = true;` then `Render()` (:205).

**Callers of `OnRenderFrameOpportunity` — exhaustive (`grep`, whole `src`):**
1. `src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:124` — inside `OnTick`, under `#if __SKIA__`.
2. `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs:421` — `SynchronousRenderAndDraw`, Win32-only, resize/move/show.

**On Android there is exactly one caller: `CoreServices.OnTick`.** And `OnTick` runs only as the
Normal-priority item enqueued by `CoreServices.RequestAdditionalFrame` (`CoreServices.cs:67-75`),
which is called only from:
- `XamlRoot.InvalidateMeasure` / `InvalidateArrange` (`XamlRoot.crossruntime.cs:18,26`)
- `EventManager.EnqueueForEffectiveViewportChanged` (`EventManager.cs:34`)
- `EventManager.RequestRaiseLoadedEventOnNextTick` (`EventManager.cs:69`)
- `CustomEventManager` (`CustomEventManager.cs:60`)

> **Corollary A (the RedirectVisual discriminator): a workload that never invalidates layout and
> never enqueues an effective-viewport change can never reach `_renderedAheadOfTime` on Android.**

**`EnqueueRenderCallback()` — `:120-157`** (UI thread, run as the dispatcher's render action)

| Entry state | Effect | Records a picture? |
|---|---|---|
| A. `_ahead && _rRAAOTP` (:134-139) | clear both, call `RequestNewFrame()` → `RenderRequested=true` + `InvalidateRender()` | **NO** — log literally says *"Doing nothing this tick and rescheduling another tick"* |
| B. `_ahead && !_rRAAOTP` (:140-143) | clear `_ahead` | **NO** (correct: the early record *is* this cycle's picture) |
| C. `!_ahead && RenderRequested` (:145-153) | `RenderRequested=false`, `Render()` | **YES** |
| D. `!_ahead && !RenderRequested` | nothing | NO |

**`Render()` — `CompositionTarget.Rendering.skia.cs:110-198`**

```
:119-124  RecordPictureAndReturnPath  → SkiaRenderHelper.skia.cs:44 → Compositor.RenderRootVisual
:147      _lastRenderedFrame = (framePicture, path, damageSnapshot)
:157      _fpsHelper.OnFrameRecorded()          ← generation++
:164-167  if (_isRenderingActive) RequestNewFrame()
:169-172  XamlRootMap.GetHostForRoot(...)?.InvalidateRender()   ← UNCONDITIONAL, backed by a real picture
```

Inside the record, `Compositor.RenderRootVisual` (`Compositor.skia.cs:300-376`):
```
:307-324  FrameStarting(frameTimestamp)                       ← the fling driver ticks HERE
:326-342  RaiseAnimationFrame() for every running animation
:351      rootVisual.RenderRootVisual(canvas, ...)            ← paint walk
:372-375  if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
              rootVisual.CompositionTarget?.RequestNewFrame();   ← UNCONDITIONAL re-arm, INSIDE Render
```

**`OnNativePlatformFrameRequested()` — `:166-176`** (render thread)
```
if (Interlocked.Exchange(ref _shouldEnqueue..., false))  NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback);
return Draw(canvas, resizeFunc);
```
Note the ordering: the render action is queued **before** the present. Self-sustaining chain — each
present queues one render action, whose execution re-arms the flag (`:125`) for the next present.

**`NativeDispatcher.TryGetRenderAction()` — `NativeDispatcher.cs:206-234`**

The render action is withheld while `normalItemsToProcessBeforeNextRenderAction > 0` (:214), and on
every handover the counter is **re-seeded from the current Normal queue depth** (:216):
```csharp
_compositionTargets[compositionTarget] = (renderAction: null,
    normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
```
Decremented one per Normal item dequeued (`:156-165`).

---

## 2. The four callers of `InvalidateRender` — and which are "backed"

| Caller | Backed by a fresh picture at call time? |
|---|---|
| `CompositionTarget.Render` `:171` | **YES** — `_lastRenderedFrame` was set at `:147`, generation bumped at `:157` |
| `CompositionTarget.RequestNewFrame` `:110` | **NO** — speculative: "content changed, a record is owed" |
| `TryExecuteOnNextRenderAsync` `:348` | NO (RenderTargetBitmap etc.; not on the scroll path) |
| `UnoSKVulkanView.SurfaceChanged` `:107` | NO (startup/rotation) |

A speculative `InvalidateRender` is normally **harmless**, because the matching `Render()` lands one
dispatcher turn later (sub-millisecond on an idle UI thread) while the render thread is still parked
in `_pacer.WaitForNextFrame()` — the two invalidations coalesce into a single wake and a single
present of the fresh picture.

It becomes a **guaranteed drop** exactly when the gap between the speculative `InvalidateRender` and
the next `Render()` exceeds one vsync period. There is precisely one scheduling state that
guarantees that:

> **State A of `EnqueueRenderCallback` (`:131-139`): `_renderedAheadOfTime && _renderRequestedAfterAheadOfTimePaint`.**
> It issues a speculative `InvalidateRender` (via `RequestNewFrame` :110) and then **returns without
> rendering**. The picture cannot arrive before the *next* render action, i.e. at least one full
> present cycle later. The intervening present necessarily re-blits the previous picture → dropped++.

Note the design intent vs. the outcome. The comment at `:180-182` says: *"If we get an opportunity to
call Render earlier than EnqueuePaintCallback, then we do that but skip the Render call in the next
EnqueuePaintCallback so that overall we're still keeping the rate of Render calls the same."* That
bargain is only sound in State B (nothing changed since the early record). In State A the content
*did* change, and the code takes the strictly worst option available: it skips **and** waits.
Calling `Render()` inline instead would cost nothing and lose nothing.

---

## 3. Steady-state trace of a fling on Android

Preconditions established from source:

- `StartFling` subscribes `OnFlingFrame` to `Compositor.FrameStarting`
  (`ScrollContentPresenter.Managed.cs:601`) for the whole fling.
- `OnFlingFrame` (`:617-644`) calls `Set(..., new(DisableAnimation: true, IsTouch: true, ...))`.
- `Set` → `Update` (`:416-421`) → the `IsTouch` branch (`:521-528`) stops animations and assigns
  `visual.AnchorPoint = target` → `Compositor.InvalidateRender` → `InvalidateRenderPartial`
  (`Compositor.skia.cs:378-383`) → `target.RequestNewFrame()`.
- `Update` then calls `Updated(...)` (`:527`) → `UpdateOffsets` → `InvalidateViewport()` (`:467`) →
  `PropagateEffectiveViewportChange` → `EnqueueForEffectiveViewportChanged`
  (`FrameworkElement.EffectiveViewport.cs:384`, gated on `viewportUpdated` at `:379`) →
  `CoreServices.RequestAdditionalFrame()` → **one Normal-priority `OnTick` per fling frame**.

Let a "slot" be one Choreographer period (8.3 ms @120 Hz).

**Slot k — ahead-of-time record**
1. UI: `OnTick` (Normal) → `UpdateLayout()` (clean) → `OnRenderFrameOpportunity` → `RenderRequested==true`, `!_ahead` → `_ahead=true`, `RenderRequested=false`, `Render()`.
2. Inside `Render`: `FrameStarting` → `OnFlingFrame` → `Set` → `AnchorPoint` write → `RequestNewFrame` → `_ahead` is true → **`_rRAAOTP = true`** (no `InvalidateRender`).
3. Still inside `Render`: `Compositor.skia.cs:372-374` — `FrameStarting is not null` → `RequestNewFrame()` again → `_rRAAOTP` already true. *This line alone guarantees step 2's outcome even if the offset didn't move.*
4. `Updated` → `InvalidateViewport` → next `OnTick` enqueued (Normal).
5. `:147` slot filled, `:157` generation++, `:171` **backed `InvalidateRender`**.
6. Render thread: present → fresh generation → **no drop**; queues render action.

**Slot k+1 — the burned cycle**
7. UI: `TryGetRenderAction` is gated by `normalItemsToProcessBeforeNextRenderAction` (≥1, because the
   fling put an `OnTick` in the Normal queue and the counter was re-seeded at the previous handover,
   `NativeDispatcher.cs:216`). The `OnTick` runs first → `OnRenderFrameOpportunity` → `RenderRequested==false` → no-op.
8. UI: render action runs → `EnqueueRenderCallback` **State A** → clears both flags, `RequestNewFrame()`
   → `RenderRequested=true` + **speculative `InvalidateRender`**. **No `Render()`.**
9. Render thread: wakes, presents the *same* picture → `current == lastPresented` → **DROPPED++**.

**Slot k+2 — normal record**
10. UI: render action → State C → `Render()`. Fling ticks inside the record and re-arms
    `RenderRequested` (step 2/3 again, this time via the `!_ahead` branch → speculative invalidate,
    which coalesces with the backed one at `:171`). `OnTick` enqueued.
11. Render thread: present → fresh → no drop.
12. The next `OnTick` finds `RenderRequested==true` → ahead-of-time again → back to slot k.

**Cadence: 2 records per 3 presents ⇒ ~1 drop per 3 presents when the ahead-of-time path engages
every cycle.** The observed ~20 drops at ~120 presents/s is ≈1-in-6, i.e. the ahead-of-time path
engaging roughly half the time — consistent, since step 7's ordering is a race decided by
`TryGetRenderAction`'s gating counter and how deep the Normal queue happens to be.

---

## 4. Three-way prediction table — leading hypothesis

**H-A: State A of `EnqueueRenderCallback` (`_ahead && _rRAAOTP`) burns a present cycle. A fling
reaches it deterministically; a drag only by coincidence; RedirectVisual never.**

The two independent preconditions:

- **P1 — can `_renderedAheadOfTime` ever be set?** Requires `CoreServices.OnTick`, i.e. a
  `RequestAdditionalFrame` (layout invalidation or effective-viewport enqueue).
- **P3 — does a `RequestNewFrame` land between the ahead-of-time `Render()` and the render action?**

| | P1 — ahead-of-time reachable? | P3 — re-request inside that window? | Predicted drops |
|---|---|---|---|
| **Finger drag** | **Yes.** `Updated` → `InvalidateViewport` → `EnqueueForEffectiveViewportChanged` (`EventManager.cs:34`) fires per pointer move, exactly as in a fling. | **No, structurally.** `FrameStarting` has no subscriber during a drag and `_runningAnimations` is empty — the `IsTouch` branch (`ScrollContentPresenter.Managed.cs:521-527`) *stops* animations and assigns `AnchorPoint` directly. So `Compositor.skia.cs:372` does **not** re-request. The only source is the next `MotionEvent` landing in the sub-ms window between `Render()` (`:205`) and the render action. | **≈0** ✔ |
| **Touch inertia (fling)** | **Yes**, per frame (same path). | **Yes, with certainty.** `OnFlingFrame` runs *inside* the record via `FrameStarting` (`Compositor.skia.cs:307-324`), and `Compositor.skia.cs:372-374` re-requests unconditionally while `FrameStarting is not null`. Both fire while `_ahead == true`. Hit rate 100% on every ahead-of-time cycle. | **high (~1 per 3–6 presents)** ✔ |
| **RedirectVisual sample** | **No.** A `RedirectVisual` + composition animations never invalidate measure/arrange and never enqueue an effective-viewport change → `RequestAdditionalFrame` never fires → `OnTick` never runs → `OnRenderFrameOpportunity` (only Android caller: `CoreServices.cs:124`) is never invoked → `_renderedAheadOfTime` is **always false**. Every render goes through State C. Bonus: the Normal queue is empty, so `normalItemsToProcessBeforeNextRenderAction` is always 0 and the render action is never withheld (`NativeDispatcher.cs:214`). | n/a | **0, at full 120 Hz** ✔ |

**Consistent with all three.** Note that P3 alone does *not* separate fling from RedirectVisual —
`Compositor.skia.cs:372` fires for a running composition animation too (`_runningAnimations.Count > 0`).
P1 is what separates them. And P1 alone does not separate fling from drag. **Both preconditions are
required, and only the fling satisfies both.** That is what makes this explanation fit all three
observations rather than two.

### "worse the slower the fling gets"

Partially explained, and I flag the rest as **UNVERIFIED**:
- Perceptual: one stale present is a *doubled step*. At high velocity the doubled step is a small
  fraction of a large motion; at low velocity the eye is tracking a slow, regular cadence and a
  doubled step reads as a visible hitch.
- Mechanical (**UNVERIFIED**): as the fling decelerates, the damage region shrinks
  (`CompositionTarget.Rendering.skia.cs:139-147`, `Draw` clips to it at `:291`), so both the record
  and the paint get cheaper, the UI thread gains slack, and step 7's race is won by the Normal
  `OnTick` more often → the ahead-of-time path engages more often → higher drop rate.
- Counter-pressure: once the offset stops changing, `viewportUpdated` (`:379`) goes false, no more
  `OnTick`, and the ahead-of-time path stops engaging. So the drop *rate* should peak somewhere in
  the middle of the deceleration and fall at the very end. **This is a sharp, cheap prediction to
  test** (see E2).

---

## 5. Competing hypotheses, attacked

### H-B (the task's leading hypothesis): "the Normal-priority item delays the render action past its vsync"

`NativeDispatcher.TryGetRenderAction:214-216` does withhold the render action behind Normal items,
and the fling does enqueue one per frame.

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| Predicts | Normal item enqueued identically → **drops** ✘ | drops ✔ | no Normal items → no drops ✔ |

**Rejected as stated.** It predicts the drag drops too, and observation 1 says it doesn't. The
`ScrollContentPresenter.Managed.cs:467` → `EventManager.cs:34` → `CoreServices.cs:73` chain is
byte-for-byte identical for a drag and a fling; nothing about "from inside the record" changes what
`RequestAdditionalFrame` enqueues.

Quantitatively it is also weak: one extra Normal `OnTick` (whose body is a clean-tree `UpdateLayout`)
costs microseconds, not the ~8 ms needed to miss a vsync.

**But H-B is the right neighbourhood.** The gating is a real participant — it is precisely what makes
the `OnTick` run *before* the render action, which is how the ahead-of-time path gets entered at all
(step 7). H-A is the corrected form of H-B: the Normal item does not delay the render, it **hands the
render to `OnRenderFrameOpportunity` early**, and then the ahead-of-time bookkeeping throws away the
following cycle. Same ingredients, different mechanism, and this version survives the drag case.

### H-C: "speculative `InvalidateRender` from `RequestNewFrame:110` wakes the render thread before the picture exists"

Real, and it is the *proximate* cause of every drop. But on its own:

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| Predicts | pointer handler calls `RequestNewFrame` outside `Render` → speculative wake → **drops** ✘ | drops ✔ | `Compositor.skia.cs:374` fires from inside `Render`, then `:171` fires ~immediately → coalesce → 0 ✔ |

**Insufficient alone** — it over-predicts the drag. It only becomes a drop when the gap to the next
`Render()` exceeds a vsync, and State A is the only state that guarantees that. H-C is the mechanism;
H-A is the trigger. Keep both in the write-up: a fix could attack either end.

### H-D: lost wake-up in the Android render loop

`UnoSKVulkanView.cs:149-155`:
```csharp
_renderEvent.Wait(...);
_renderEvent.Reset();          // (a)
if (... || !_renderRequested) continue;
_renderRequested = false;      // (b)
```
An `InvalidateRender` landing between (a) and (b) sets `_renderRequested`, then (b) clears it; the
`_renderEvent.Set()` survives but the next iteration `continue`s and `Reset()`s it away. **This is a
real lost-frame bug**, independent of everything above.

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| Predicts | occasional | occasional | occasional |

**Rejected as the explanation** — it is workload-independent, so it cannot produce a 0 / 20+ / 0
split. It would also surface as **unpresented**, not **dropped** (`SkiaRenderHelper.skia.cs:268-284`).
Worth fixing separately; the observed unpresented count would confirm it.

### H-E: `_isRenderingActive`

`Render:164-167` calls `RequestNewFrame()` at the end of *every* render when anything is subscribed
to `CompositionTarget.Rendering` — which sets `_rRAAOTP` on every ahead-of-time render regardless of
workload. **Not the cause here** (the product owner's app has no `Rendering` subscriber), but it is a
serious confound for the harness — see §7.

---

## 6. Falsifiable experiments

**E1 — on-device, zero code, 60 seconds. The decisive one.**
Enable trace logging for `Microsoft.UI.Xaml.Media.CompositionTarget` and fling. The message at
`RenderScheduling.skia.cs:137` is unique to State A:
> `"rendered ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick"`

Count it per second during a drag vs a fling vs the RedirectVisual page.
**H-A predicts: ≈0 / ≈20–40 / exactly 0.** If the fling count is ~0, H-A is dead.
Also count `:142` ("no new frame was requested since", State B) — H-A predicts B dominates during a
drag and A dominates during a fling.

**E2 — one-line on-device fix, immediate visual verdict.**
In `EnqueueRenderCallback` State A (`:134-139`), replace the reschedule with an inline render:
```csharp
if (_renderRequestedAfterAheadOfTimePaint)
{
    _renderRequestedAfterAheadOfTimePaint = false;
    Render();     // instead of RequestNewFrame()
}
```
(`RenderRequested` is already false in this state and `Render` re-arms it via the compositor, so the
invariants at `:210-218` hold.)
**H-A predicts the fling drop counter collapses to ~0 and the fling stops feeling stuttery, with no
change to drag or RedirectVisual.** This is the cheapest test that is also plausibly the fix.

**E3 — kill the ahead-of-time path outright (bisect).**
Comment out `CoreServices.cs:124`. **H-A predicts fling drops → 0, drag and RedirectVisual
unchanged.** If drops persist, the cause is not the ahead-of-time path and H-C or something else owns
it. Not a shippable fix (it exists for input latency), but a clean bisect.

**E4 — Win32 falsification, done correctly (see §7 first).**
Add a `Given_ScrollSmoothness` case that reads `FpsHelper`'s dropped counter (or counts
`OnFramePresentRequested`-with-equal-generation) rather than `Rendering` callbacks, and that does
**not** subscribe `CompositionTarget.Rendering`. Then run the existing fast/medium/slow fling.
**H-A predicts Win32 shows the same drops**, because `Win32WindowWrapper.Rendering.cs:24`
(`InvalidateRender → SignalNewFrame`) is the same pull model and `CoreServices.cs:124` is `#if __SKIA__`.
If Win32 is genuinely clean under a present-counting metric, H-A is incomplete and the difference is
in the pacing (`CopyPixels` blocks for vsync *after* the draw on Win32 vs
`_pacer.WaitForNextFrame()` *after* a non-blocking MAILBOX present on Android).

---

## 7. The Win32 evidence does not falsify this — it measured a different quantity

Two problems with "121 Rendering callbacks/s, 0% duplicate offsets" as counter-evidence:

1. **It counts records, not presents.** `CompositionTarget.Rendering` is raised from
   `OnFramePictureRecorded` (`Rendering.skia.cs:445-449`), i.e. once per `Render()`. A drop is
   *presents without records*. A metric that samples once per record cannot express a duplicated
   present — a 1-record-per-cycle stream looks perfect whether it was presented once or twice. The
   Android metric and the Win32 metric are not comparable.

2. **Subscribing to `Rendering` perturbs the state machine under test.** `Rendering.add` sets
   `_isRenderingActive` (`Rendering.skia.cs:90-97`), which makes `Render:164-167` call
   `RequestNewFrame()` at the end of every render — i.e. it forces `_rRAAOTP = true` on every
   ahead-of-time render, for *every* workload including a drag. The harness is a Heisenberg probe on
   exactly the flag under investigation.

So the Win32 result is neither confirmation nor refutation. E4 is the corrected version.

(A secondary Win32/Android difference worth keeping in mind: Win32's `CopyPixels` blocks for vsync
*inside* the draw pass (`Win32WindowWrapper.RenderThread.cs:73`), whereas Android's MAILBOX present
returns immediately and pacing happens afterwards (`UnoSKVulkanView.cs:158-161`). The coalescing
window for a speculative `InvalidateRender` therefore sits at a different point in the cycle on the
two platforms, which could change how often H-C bites even if H-A were identical. **UNVERIFIED.**)

---

## 8. Summary

| Claim | Status |
|---|---|
| "dropped" counts presents with no intervening `Render()`, generation-based, not `_lastRenderedFrame`-null | **Verified** — `SkiaRenderHelper.skia.cs:292-324`, comment at `:181-184` |
| `Draw` finding `_lastRenderedFrame == null` is unreachable in steady state and uncounted at startup | **Verified** — single render thread; `current == 0` guard |
| On Android, `Draw` is triggered by `InvalidateRender`, not by vsync | **Verified** — `UnoSKVulkanView.cs:137-171`, `ChoreographerFramePacer.cs` |
| `EnqueueRenderCallback` State A returns without rendering, guaranteeing one stale present | **Verified by inspection** — `RenderScheduling.skia.cs:131-139` |
| `OnRenderFrameOpportunity` has exactly one Android caller, gated on `RequestAdditionalFrame` | **Verified** — grep over `src`; `CoreServices.cs:67-127` |
| A fling sets `_rRAAOTP` deterministically from inside the record | **Verified by inspection** — `Compositor.skia.cs:307-324` + `:372-375`, `ScrollContentPresenter.Managed.cs:601,617-644` |
| A drag has no in-record re-request source | **Verified by inspection** — `ScrollContentPresenter.Managed.cs:521-527` (IsTouch stops animations, no `FrameStarting` subscriber) |
| RedirectVisual never enters the ahead-of-time path | **Verified by inspection** — no layout/viewport invalidation ⇒ no `RequestAdditionalFrame` ⇒ no `OnTick` |
| The exact 2-records-per-3-presents cadence | **UNVERIFIED** — depends on the `TryGetRenderAction` gating race; needs E1 |
| "worse the slower" mechanism | **Partially UNVERIFIED** — needs E2's drop-rate-vs-velocity curve |
| No runtime validation was performed | **True.** Everything above is code review. E1/E2 are the on-device checks. |
