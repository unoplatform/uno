# 01 — Dispatcher render-action gating: exact accounting, and whether it explains the drops

Scope: Skia-on-Android (Vulkan, `UnoSKVulkanView` + `ChoreographerFramePacer`), 120 Hz.
Question posed: is `NativeDispatcher.TryGetRenderAction` / `normalItemsToProcessBeforeNextRenderAction`
the cause of the 20+ "dropped" presents per second during a touch fling?

**Verdict up front: NO — not as the primary mechanism.** The gate is bounded to
`N ∈ {0,1}` in every one of the three cases, and when it does fire it costs one *looper message*, not
one vsync. It has a real but secondary role, and that role is the opposite of the stated hypothesis
(see §5). A different mechanism in the same "one item per looper message" family explains all three
observations exactly; it is in §6.

---

## 1. Which dispatcher is actually running on Skia-Android

This matters, and it is easy to get wrong.

- `src/Uno.UI.Runtime.Skia.Android/Uno.UI.Runtime.Skia.Android.csproj:88` references
  **`Uno.UI.Dispatching.netcoremobile.csproj`**, whose TFMs are `net9.0-android;net10.0-android`
  (`src/Uno.UI.Dispatching/Uno.UI.Dispatching.netcoremobile.csproj:4`). The `UnoRuntimeIdentifier=Skia`
  fallback there (line 14) applies **only** when the TFM has no platform identifier.
- ⇒ Skia-on-Android compiles **`NativeDispatcher.Android.cs`**, *not* `NativeDispatcher.skia.cs`.
  Confirmation by elimination: nothing in the repo ever assigns
  `NativeDispatcher.DispatchOverride` / `HasThreadAccessOverride` for Android (only Win32, X11, macOS,
  FrameBuffer, Headless, unit tests do), and `NativeDispatcher.skia.cs:17,24` `Debug.Assert`s they are set.

Consequence — `src/Uno.UI.Dispatching/Native/NativeDispatcher.Android.cs:39-42`:

```csharp
partial void EnqueueNative(NativeDispatcherPriority priority)
{
    _handler.Post(_implementor);      // Looper.MainLooper
}
```

**The priority argument is ignored.** Every dispatcher wake-up — High, Normal, Low, Idle, and the
render action — is the same `Handler.Post` appended to the tail of the Android main `MessageQueue`.
Priority exists only *inside* `DispatchItems`, as the order in which the four managed queues are
scanned. This is the same shape as Win32 (`Win32EventLoop.Schedule` → `PostMessage` +
`Queue<Action>`, `src/Uno.UI.Runtime.Skia.Win32/Native/Win32EventLoop.cs:93-105`), so it is not by
itself an Android/Win32 divergence.

## 2. Exact accounting of `normalItemsToProcessBeforeNextRenderAction`

State: `Dictionary<object, (Action? renderAction, int normalItemsToProcessBeforeNextRenderAction)> _compositionTargets`
(`NativeDispatcher.cs:36`). Call the counter **N**. One entry per `CompositionTarget`; a single-window
app has exactly one.

### Seeded — `NativeDispatcher.cs:214-227`, inside `TryGetRenderAction`

```csharp
if (details.normalItemsToProcessBeforeNextRenderAction == 0)
{
    _compositionTargets[compositionTarget] =
        (renderAction: null,
         normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
    ...
    return details.renderAction;
}
```

N is (re)seeded **at the instant a render action is taken**, from the Normal queue depth **at that
instant**. Nothing else writes N upward — in particular `EnqueueRender` does **not**
(`NativeDispatcher.cs:237-263`: it only sets `renderAction`, `details with { renderAction = handler }`).

### Decremented — `NativeDispatcher.cs:156-165`, inside `DispatchItems`

```csharp
if (@this._currentPriority == NativeDispatcherPriority.Normal)
{
    foreach (var (compositionTarget, details) in @this._compositionTargets)
        if (details.normalItemsToProcessBeforeNextRenderAction > 0)
            _compositionTargets[ct] = details with { … = details.… - 1 };
}
```

Decremented **only** when a Normal item is actually *dequeued*, by exactly 1, and for **all**
composition targets at once. High / Low / Idle dequeues do not decrement it. Clamped at 0.

### Withheld — `NativeDispatcher.cs:206-234`

`TryGetRenderAction` scans `_compositionTargets`; an entry with `renderAction != null` but `N > 0` is
**skipped** (the loop continues to the next target; it does not early-return). If no target is
eligible, `DispatchItems` falls through to the priority scan and dequeues **one** item, High→Normal→Low→Idle.

### The property that decides the whole question

`DispatchItems` runs **exactly one work item per native message**, and re-posts itself whenever work
remains:

- `NativeDispatcher.cs:151-154` (queue dequeue) and `:220-223` (render-action take):
  `if (Interlocked.Decrement(ref _globalCount) > 0) EnqueueNative(...)`.
- `EnqueueCore` / `EnqueueRender` post only on the `_globalCount` 0→1 transition
  (`:250`, `:487`).

⇒ **A withheld render action is delayed by one `Handler.Post` round-trip per unit of N, not by one
vsync per unit of N.** The main looper turns those messages back-to-back, in microseconds, unless the
intervening item is itself expensive or the looper has unrelated work interleaved.

### Withholding table

| N at take-time | Effect |
|---|---|
| 0 | render action taken immediately; N re-seeded from current Normal depth |
| 1 | render action skipped once; one Normal item dequeued (N→0); render action taken on the *next* looper message |
| k | k extra looper messages |

Additional (unexercised here) hazards worth recording:
- N is **global across composition targets**, so a second window's Normal traffic gates the first
  window's render action.
- `RemoveCompositionTargets` (`:44-87`) drops entries without draining N — irrelevant to scrolling.

## 3. Every Normal-priority enqueue per frame, in the three cases

Exhaustive grep of `NativeDispatcherPriority.Normal` / default-priority `Enqueue` in
`src/Uno.UI` + `src/Uno.UI.Composition`:

| Site | Priority | Per-frame during scroll? |
|---|---|---|
| `CoreServices.cs:73` — `Enqueue(static () => OnTick(), Normal)` | **Normal** | **yes — the only one** |
| `Popup.WithPopupRoot.cs:86,94` | Normal | no |
| `CustomEventManager.cs:48` | Normal | **dead on Skia** — file is `#if !UNO_HAS_ENHANCED_LIFECYCLE`; Skia defines it (`src/Uno.CrossTargetting.targets:78`) |
| `CompositionTarget.Rendering.skia.cs:448` — `Enqueue(RaiseRendering, High)` | High | only when `_isRenderingActive` (a `CompositionTarget.Rendering` subscriber exists) |
| `SkiaCompositionSurface.skia.cs:62` | High | image decode only |
| `ScrollContentPresenter.Managed.cs:444` — `DispatcherQueue.TryEnqueue` | Normal | **no** — `Updated()` only takes that branch when `!HasThreadAccess`; drag and fling both run on the UI thread |

And `CoreServices.RequestAdditionalFrame` is **single-flight** (`CoreServices.cs:67-75`):

```csharp
if (GetXamlRoot() is { Bounds: { Width: not 0, Height: not 0 } } &&
    Interlocked.CompareExchange(ref _isAdditionalFrameRequested, 1, 0) == 0)
        NativeDispatcher.Main.Enqueue(static () => OnTick(), NativeDispatcherPriority.Normal);
```
reset to 0 at `OnTick` entry (`:79`). **Every** frame-request source funnels through it and therefore
coalesces into at most one queued item:
- `EventManager.EnqueueForEffectiveViewportChanged` → `:34`
- `EventManager.RequestRaiseLoadedEventOnNextTick` → `:69`
- `XamlRoot.InvalidateMeasure` / `InvalidateArrange` → `XamlRoot.crossruntime.cs:18,26`

### Per-case count

**Drag.** Pointer handler (main looper, not a dispatcher item) → `ScrollContentPresenter.Set`
(`.Managed.cs:311`) → `Update` → `Visual.AnchorPoint` write → `Compositor.InvalidateRenderPartial`
→ `RequestNewFrame`; and `Updated` → `InvalidateViewport`
(`FrameworkElement.EffectiveViewport.cs:256`) → `PropagateEffectiveViewportChange` → (if
`viewportUpdated`, `:378`) `EnqueueForEffectiveViewportChanged` → `RequestAdditionalFrame`.
→ **exactly 1 Normal item (`OnTick`) per frame.**

**Fling.** `Compositor.FrameStarting` → `OnFlingFrame` (`ScrollContentPresenter.Managed.cs:628`) →
the identical `Set` path. → **exactly 1 Normal item (`OnTick`) per frame** — the same one, from the
same call site.

**RedirectVisual (Lottie / `AnimatedVisualPlayer`).** The page's only motion is a Composition
`KeyFrameAnimation` ticked by `Compositor.RenderRootVisual`'s `_runningAnimations` loop
(`Compositor.skia.cs:325-341`). It writes Composition properties only: no layout invalidation, no
viewport change, no `Loaded`. → **0 Normal items per frame**, hence N is permanently 0, hence the
render action is *never* withheld. (Marked **UNVERIFIED** by measurement — see experiment E1; it is a
code-path argument, and it is the single cheapest thing to check on device.)

## 4. Is an item enqueued INSIDE the record accounted differently?

**No — the accounting is identical.** The dictionary stores only a count; nothing records *where* an
item came from. The only difference is *when* it lands relative to the seeding:

| enqueued… | counted in the N seeded at this take? | gates… |
|---|---|---|
| **before** the render action is taken | yes (it is in `_queues[Normal].Count` at `:216`) | this take → withheld now |
| **inside** the record (during the render action) | no — seeding already happened | the *next* take, and only if it survives that long |

And it does **not** survive that long in steady state. Trace, fling, Android:

1. Render thread's `Draw` at vsync *k* → `OnNativePlatformFrameRequested` calls
   `EnqueueRender` (`CompositionTarget.RenderScheduling.skia.cs:172`) → `Handler.Post`.
2. UI: `DispatchItems` → `TryGetRenderAction` takes it, seeds N from the current Normal depth.
3. The render action records; `FrameStarting` fires inside `RenderRootVisual`
   (`Compositor.skia.cs:307-320`); the fling writes the offset; `RequestAdditionalFrame` enqueues
   `OnTick` → `_globalCount` 0→1 → `Handler.Post`.
4. Render action returns. The looper immediately runs the posted `DispatchItems`. `renderAction` is
   null (the render thread is parked in `ChoreographerFramePacer.WaitForNextFrame`, ~8.3 ms away), so
   `OnTick` is dequeued → **N is already back to 0**.
5. The render thread posts the *next* render action ~8.3 ms later, into an empty Normal queue.

⇒ **In steady state the fling seeds N = 0 and the gate never fires.** It fires only when the UI
thread is still busy when `EnqueueRender` arrives — i.e. under overload, as a *symptom*, not a cause.

## 5. Verdict on the leading hypothesis, with the three-way table

> *"A fling enqueues Normal-priority dispatcher work from inside the record, so the next record is
> delayed past its vsync → stale present → dropped."*

Three independent reasons it cannot carry the observation:

1. **Magnitude.** N ≤ 1 in all three cases (§3), so the gate can withhold at most one item.
2. **Units.** A withheld item costs one looper message, not one vsync (§2). To lose a vsync, the
   single intervening `OnTick` would have to consume the whole remaining frame budget — and if it
   did, that is `UpdateLayout` cost, a different hypothesis.
3. **Direction.** An item enqueued inside the record is *drained before* the next take (§4), so the
   fling's own `OnTick` is precisely the one item that does **not** gate anything.

| Hypothesis | drag | inertia | RedirectVisual | Consistent with all three? |
|---|---|---|---|---|
| **H1** — Normal-priority gating delays the record past its vsync | 1 Normal item/frame, same as fling ⇒ predicts drops | drops | 0 Normal items ⇒ no drops | **NO** — fails drag (identical `OnTick` traffic, ~0 drops observed) |
| **H2** — items enqueued inside the record are mis-accounted vs. before it | no drops | drops | no drops | **NO** — accounting is provably identical (§4); the asymmetry the hypothesis needs does not exist in the code |
| **H4** — per-frame `UpdateLayout` (virtualization) exceeds the budget | same ListView, same realization work ⇒ predicts drops | drops | no layout ⇒ no drops | **NO** — fails drag |
| **H3** — ahead-of-time record re-arms the presenter without recording (§6) | **no drops** | **drops** | **no drops** | **YES** |

### The gate's real, secondary role

When the gate *does* fire (N=1, under load), it does something worse than delay: it reorders
`OnTick` ahead of the render action, `OnTick` then renders **ahead of time**
(`CoreServices.cs:124` → `CompositionTarget.OnRenderFrameOpportunity`), and the render action that
follows therefore takes the `_renderedAheadOfTime` branch and **produces no picture at all**. During
a fling that branch also re-arms the presenter (§6). So the gate converts a recording render-callback
into a re-arm-only one — it is an *amplifier* of H3 under load, which is consistent with "worse the
slower / heavier it gets", but it is not the generator.

Note also that the gate is redundant with `OnRenderFrameOpportunity`: both exist to get Normal work
in before the record, and they interact badly.

## 6. What does explain all three — H3

`CompositionTarget.RenderScheduling.skia.cs:120-157`:

```csharp
private void EnqueueRenderCallback()
{
    ...
    if (_renderedAheadOfTime)
    {
        _renderedAheadOfTime = false;
        if (_renderRequestedAfterAheadOfTimePaint)
        {
            _renderRequestedAfterAheadOfTimePaint = false;
            ((ICompositionTarget)this).RequestNewFrame();   // → host.InvalidateRender()  ← ARMS THE PRESENTER
        }                                                   //   …and records NOTHING
    }
    else if (RenderRequested) { RenderRequested = false; Render(); }
}
```

On Android the presenter is a **separate thread** (`UnoSKVulkanView.RenderLoop`, `:137-171`) that
`Draw`s once per vsync whenever `_renderRequested` is set (`InvalidateRender`, `:60-65`), and
`FpsHelper.OnFramePresentRequested` (`SkiaRenderHelper.skia.cs:292-313`) counts a **drop** for exactly
"a `Draw` with no intervening `Render`". So the highlighted branch — *arm the presenter, produce no
picture* — is a drop generator by construction. The next record cannot happen until the render thread
`Draw`s (stale) and posts another `EnqueueRender`: **one full vsync of dead time.**

The branch requires `RequestNewFrame()` to be called while `_renderedAheadOfTime == true`. Where does
that come from?

`Compositor.skia.cs:372`:
```csharp
if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
    rootVisual.CompositionTarget?.RequestNewFrame();
```
— executed at the tail of **every record**.

| | `OnTick` runs? (→ ahead-of-time record possible) | `FrameStarting is not null` at `Compositor.skia.cs:372`? | ⇒ `_renderRequestedAfterAheadOfTimePaint` set? | predicted drops |
|---|---|---|---|---|
| **Drag** | yes (viewport invalidation ⇒ `OnTick`; `OnRenderFrameOpportunity` finds `RenderRequested` set by the pointer handler) | **no** — no fling/wheel subscriber, no running animations | **no** ⇒ the render callback is a pure no-op, it does **not** call `InvalidateRender` | **~0** ✔ matches |
| **Inertia** | yes (same viewport invalidation, driven from `OnFlingFrame`) | **yes** — `OnFlingFrame` is subscribed (`ScrollContentPresenter.Managed.cs:601`) | **yes, on every ahead-of-time record** | **drops, one per engagement** ✔ matches |
| **RedirectVisual** | **no** — Lottie writes Composition properties only; `RequestAdditionalFrame` is never called, so `OnTick` never runs and `_renderedAheadOfTime` is never true | yes (`_runningAnimations.Count > 0`) — but it takes the healthy branch, since `_renderedAheadOfTime == false && RenderRequested == false` inside `Render()` | **no** | **0, locked to 120 Hz** ✔ matches |

The discriminator is `FrameStarting is not null`, which is true **exactly** during a fling / wheel
decay and false during a drag. That is the only per-frame signal in the pipeline that differs between
observation 1 and observation 2.

Consistent-but-not-derived: "worse the slower the fling gets". As velocity decays, `viewportUpdated`
(`FrameworkElement.EffectiveViewport.cs:369`) stops firing on some frames, so `OnTick` stops being
enqueued on some frames, so path A / path B alternate irregularly rather than steadily — judder rather
than a clean 2:1 cadence. Not proven here.

### About the Win32 "0% duplicates" datum — it may not be comparable

`Given_ScrollSmoothness` measures via `CompositionTarget.Rendering`. Subscribing sets
`_isRenderingActive = true` (`CompositionTarget.Rendering.skia.cs:84-98`), which:
- makes `Render()` call `RequestNewFrame()` unconditionally at `:164-167`, and
- posts a **High**-priority `RaiseRendering` per record batch at `:448`.

Both change the exact state machine under test. `ScrollContentPresenter` does the same whenever
`ScrollDiagnostics.IsEnabled` (`.Managed.cs:171`). So *the harness perturbs the thing it measures*,
and "121 callbacks/s, 0% duplicate offsets" counts **records**, not stale presents — it does not
falsify H3. This should be re-run without a `Rendering` subscriber, counting `FpsHelper` drops.

## 7. Experiments (cheapest first)

- **E1 (device, 5 min, no code change beyond logging).** Count, per second, the three exits of
  `EnqueueRenderCallback` — (a) `_renderedAheadOfTime && _renderRequestedAfterAheadOfTimePaint`
  [re-arm, no record], (b) `_renderedAheadOfTime` only [pure no-op], (c) `RenderRequested` → `Render()`.
  Also count `CoreServices.OnTick` invocations.
  *H3 predicts:* drops/s ≈ bucket (a)/s during the fling; bucket (a) ≈ 0 during the drag; `OnTick`/s ≈ 0
  on the RedirectVisual page.
  *H1 predicts nothing about these* — it needs E2 instead.
- **E2 (device, direct test of the assigned hypothesis).** Log the seeded N at every take in
  `TryGetRenderAction` (`NativeDispatcher.cs:216`) and count how often the render action is withheld.
  *This analysis predicts:* seeded N = 0 on the overwhelming majority of frames in all three cases, and
  withhold-rate ≪ 20 % during the fling. If instead the withhold-rate is ≥ 20 % and tracks the drop
  count, H1 survives and this document is wrong.
- **E3 (one-line falsification of H3).** In `EnqueueRenderCallback`, replace the
  `RequestNewFrame()` in the `_renderRequestedAfterAheadOfTimePaint` branch with a direct `Render()`
  (record instead of re-arm). If fling drops fall to ~0 with drag and RedirectVisual unchanged, H3 is
  confirmed and the fix is local.
- **E4 (Win32, falsifies the "Win32 is clean" premise).** Re-run the fling on Win32 with **no**
  `CompositionTarget.Rendering` subscriber and `ScrollDiagnostics` off, counting `FpsHelper` drops
  instead of Rendering callbacks. *H3 predicts Win32 also drops* (same state machine, same
  render-thread split via `Win32WindowWrapper`'s render thread). If Win32 is still clean under this
  measurement, something Android-specific remains and H3 is incomplete.

## 8. Verified / unverified ledger

Verified by reading code at the cited lines: §1 (which dispatcher partial compiles on Skia-Android),
§2 (seed/decrement/withhold accounting), §3 (the exhaustive Normal-enqueue list and the single-flight
guard), §4 (identical accounting for inside-vs-before-the-record), §6's control flow.

**UNVERIFIED** (needs E1/E2): that `OnTick` really never runs on the RedirectVisual page; the actual
seeded-N distribution; the actual rate of the ahead-of-time re-arm branch; the claim that Android
delivers drag `MotionEvent`s vsync-aligned at the head of the frame (asserted nowhere in this repo).
