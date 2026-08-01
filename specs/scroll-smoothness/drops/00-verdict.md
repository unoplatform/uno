# 00 — Verdict: why a fling drops frames, a drag does not, and RedirectVisual is perfect

Decision document. Inputs: `drops/01`–`06` (six independent angles) and `drops/10`–`12` (three
refutation passes). Every load-bearing claim below was **re-verified against source in this worktree**
(`dev/mazi/smooth-scroll`) while writing this document; citations are `file:line` at that commit.
Evidence class is **code review by inspection** — nothing here was compiled or executed. Claims that
require a device or a run are marked **UNVERIFIED** and are the subject of §5.

---

## 1. The answer

**On Skia-on-Android the render loop is demand-driven, not vsync-driven** — it draws only when
somebody called `InvalidateRender()` (`UnoSKVulkanView.cs:146-162`, `continue` at `:152-153`), and it
paces to vsync *after* the present (`:161`). So a slow UI thread cannot, by itself, produce a single
`dropped`: an overrun just means no `Draw` happens. **`dropped` counts an `InvalidateRender()` issued
with no picture behind it** that was still outstanding when the render thread woke. There is exactly
one place in the pipeline that issues such an invalidate as a matter of routine, and it is reachable
**only during a fling**: `EnqueueRenderCallback`'s *State A*
(`CompositionTarget.RenderScheduling.skia.cs:134-139`), which clears both ahead-of-time flags, calls
`RequestNewFrame()` → `host.InvalidateRender()` (`:110`), and **returns without recording**. Entering
State A requires `_renderRequestedAfterAheadOfTimePaint`, which requires a `RequestNewFrame` raised
while `_renderedAheadOfTime` is already true — i.e. **from inside the ahead-of-time record itself**
(the flag is set at `:195`, *before* `Render()` at `:205`). A fling supplies that with certainty,
twice over, because `OnFlingFrame` runs from `Compositor.FrameStarting` **inside** `RenderRootVisual`
(`Compositor.skia.cs:307-324`; subscribed at `ScrollContentPresenter.Managed.cs:601`): its
`visual.AnchorPoint` write (`SCP.Managed.cs:525`) goes through `InvalidateRenderPartial`
(`Compositor.skia.cs:378-383`) → `RequestNewFrame`, and `Compositor.skia.cs:372-375` re-arms
unconditionally at the end of *every* record while `FrameStarting is not null`. **A fling therefore
runs the pipeline in "promise mode": each vsync's present is armed before the picture for it
exists**, and the promise is kept only if a Normal-priority `CoreServices.OnTick` completes an
ahead-of-time `Render()` before the next vsync (`CoreServices.cs:73` → `:115` → `:124`). Whenever the
UI thread overruns the 8.33 ms period — Android's *shared* main Looper (`NativeDispatcher.Android.cs`),
two Normal-priority hops per scroll frame, plus the paint walk — the promise is broken and the render
thread presents the previous picture: `dropped++` (`SkiaRenderHelper.skia.cs:309-313`). A **drag**
writes the same offset through the same `Set` with byte-identical options (`SCP.Managed.cs:866`/`:877`
vs `:643`) but **from the pointer handler, before the record**, and nothing re-arms from inside the
record (`Compositor.skia.cs:372`'s three terms are all false), so the render action takes the *silent*
**State B** (`:140-143`) — no unbacked invalidate is ever issued, and an overrun degrades to "no Draw"
instead of "stale Draw". **RedirectVisual** never calls `CoreServices.RequestAdditionalFrame`, so
`OnRenderFrameOpportunity` never runs, `_renderedAheadOfTime` is permanently false, States A/B are
unreachable, and every frame is **State C** (`:145-153`): one record per present, forever.

### No single mechanism does it. Two are needed, and both are necessary.

| | Factor | What it explains | Share |
|---|---|---|---|
| **F1** | **The unbacked invalidate** — State A (`RenderScheduling.skia.cs:134-139`), armed only when a frame driver writes from *inside* the record | **The entire 0-vs-20-vs-0 split.** Which workload is *capable* of reporting a drop at all | **~100 % of the drag/fling asymmetry and of the RedirectVisual zero** |
| **F2** | **UI-thread period overrun** — main-Looper latency + 2 Normal hops/frame + paint walk vs 8.33 ms | **The rate** (~17 % ⇒ ~20 of ~120), and why Win32 and RedirectVisual stay clean while an Android `ListView` fling does not | **~100 % of the magnitude**, 0 % of the split |

**F1 alone** predicts ~100 drops/s during a fling (every State A would drop) — 5× too many.
**F2 alone** predicts the drag drops identically, because drag and fling execute the *same record*
(§3, H-Load). Only the conjunction fits.

### The caveat that must not be lost

Removing F1 takes the counter to ~0 but does **not** by itself remove the felt stutter. The fling's
position is a function of `timestampInTicks` (`SCP.Managed.cs:617-643`), so a skipped record costs a
**doubled step** in the motion curve, not merely a repeated pixel. The counter and the feel share a
cause (F2) but are not the same defect. **Ship both halves** (§4 Fix B *and* Fix C/E).

---

## 2. The three-way table for the winning explanation

Preconditions, restated so each row can be checked mechanically:

- **P1** — `_renderedAheadOfTime` can be set. Requires `OnRenderFrameOpportunity`
  (`RenderScheduling.skia.cs:178-208`), whose only Skia caller is `CoreServices.cs:124`, at the tail
  of the Normal-priority `OnTick` enqueued by `CoreServices.RequestAdditionalFrame` (`:67-75`).
- **P2** — a `RequestNewFrame` lands **while `_renderedAheadOfTime == true`**, i.e. from inside the
  ahead-of-time record (`:195` sets the flag before `:205` calls `Render()`).
- **P1 ∧ P2 ⇒ State A ⇒ unbacked `InvalidateRender` ⇒ a drop whenever F2 fires.**

| | **Finger drag** | **Touch inertia (fling)** | **RedirectVisual page** |
|---|---|---|---|
| **P1 — Normal `OnTick` ⇒ `_renderedAheadOfTime`** | **YES.** `Set` → `Updated` (`SCP.Managed.cs:434-468`) → `OnPresenterScrolled` → `ScrollViewer.RequestUpdate` (Normal, `ScrollViewer.cs:1301-1316`) → `Update` writes `VerticalOffset` (`:1325`) → template-bound `ScrollBar.Value` (`ScrollViewer.xaml:274`) → `OnValueChanged` → `UpdateTrackLayout` (`ScrollBar.mux.cs:729-733`) writes `Height`/`Margin` (`:1030,:1055`) → root `InvalidateMeasure` → `XamlRoot.InvalidateMeasure` (`XamlRoot.crossruntime.cs:14-19`) → `RequestAdditionalFrame` | **YES.** Identical chain — the fling calls the *same* `Set` (`:643` vs `:866`/`:877`, same `DisableAnimation:true, IsTouch:true, IsIntermediate:true`) | **NO.** Nothing on the page invalidates measure/arrange or enqueues an effective-viewport change. `RequestAdditionalFrame`'s complete caller set is `EventManager.cs:34`, `EventManager.cs:69`, `XamlRoot.crossruntime.cs:18,26` — none is reached. `OnTick` never runs |
| **P2 — a `RequestNewFrame` from inside the ahead-of-time record** | **NO — structurally.** The offset is written from the pointer handler *before* the record. Inside the record `Compositor.skia.cs:372` is false on all three terms: no `FrameStarting` subscriber, `_runningAnimations` empty (the `IsTouch` branch *stops* animations at `SCP.Managed.cs:521-526`), no transitions | **YES — with certainty, twice.** `FrameStarting` is subscribed for the whole fling (`SCP.Managed.cs:601`) and raised at the **top** of `RenderRootVisual` (`Compositor.skia.cs:307-315`), so `OnFlingFrame`'s `AnchorPoint` write (`SCP.Managed.cs:525`) → `InvalidateRenderPartial` (`Compositor.skia.cs:382`) lands inside; and `Compositor.skia.cs:372-375` re-arms unconditionally at record end because `FrameStarting is not null` — *even on a frame where the offset did not move* | **Irrelevant (P1 already false).** Note for the record: the page's Lottie is **not** a Composition animation — `LottieVisualSourceBase.TryCreateAnimatedVisual` throws (`LottieVisualSourceBase.cs:74-79`), `AnimatedVisualPlayer` catches and sets `m_useWinUIFlow = false` (`AnimatedVisualPlayer.mux.cs:299-317`) — so `_runningAnimations` is empty and `Compositor.skia.cs:372` does not fire here either. Its frames come from a Skottie self-invalidate inside its own paint (`LottieVisualSource.Skottie.cs:347-349`) |
| **Branch the render action takes** | **State B** — `RenderScheduling.skia.cs:140-143`: clears `_renderedAheadOfTime`, **no invalidate**, no record | **State A** — `:134-139`: clears both flags, `RequestNewFrame()` → **unbacked `host.InvalidateRender()`** (`:110`), returns **without recording** | **State C** — `:145-153`: `RenderRequested = false; Render();` — records every time |
| **Can an overrun be seen by the counter?** | **No.** Nothing armed the host, so at the vsync `_renderRequested == false` → `continue` (`UnoSKVulkanView.cs:152-153`) → no `Draw`, no drop. The frame is merely *late* | **Yes.** The host was armed by State A with no picture behind it → the `Draw` re-blits → `current == lastPresented` → `dropped++` (`SkiaRenderHelper.skia.cs:309-313`) | **No, and it never overruns anyway.** Zero Normal items ⇒ `normalItemsToProcessBeforeNextRenderAction` re-seeds to 0 (`NativeDispatcher.cs:216`) and the render action is taken on the first pump; the record is a `StackPanel`, 2 `Image`/`Canvas` pairs and one 200×200 Lottie (`RedirectVisualTests.xaml:14-67`) |
| **Predicted** | **~0 dropped** (not *exactly* 0 — see below) | **> 0, one per broken promise**; ~17 % overrun ⇒ ~20 of ~120 | **0 dropped at the full panel rate** |
| **Observed** | ~0 ✔ | 20+ ✔ | 0 @ 120 ✔ |

**Why the drag is "~0" and not "exactly 0", and why that matters.** The drag has exactly one route
into State A: a `MotionEvent` landing in the window between the ahead-of-time `Render()`
(`:205`) and the render action running — one dispatcher hop wide. That is the correct prediction for
an *approximate* zero, and it also refutes `06`'s claim that the drag's zero is structurally
guaranteed and therefore carries no information: the drag's pointer handler *does* raise a speculative
`InvalidateRender` (`RenderScheduling.skia.cs:93-97,110`) with no picture behind it, so a drag frame
that overruns after its handler ran **would** be counted. It isn't. **The observation carries real
information.**

**Why FPS reads 100+ in every arm.** `fps` counts `Draw`s, including the stale ones
(`SkiaRenderHelper.skia.cs:243-260`, incremented at `:259` on the render thread). So
*unique frames/s = fps − dropped*: fling ≈ 100, drag ≈ 100+, RedirectVisual = 120. "120 FPS with 20
dropped" is a 1,1,1,1,1,2-vsync cadence on a 120 Hz panel — exactly the artefact
`SurfaceFrameRate.cs:12-17` documents one layer down.

---

## 3. Refuted

Ranked by how much confidence was invested in them. Every kill is a source citation, not an argument
from plausibility.

| # | Hypothesis | Verdict | The citation that kills it |
|---|---|---|---|
| **H-N** | **The brief's leading hypothesis** — a fling enqueues Normal-priority work from *inside* the record, so `NativeDispatcher.TryGetRenderAction` withholds the render action and the next record misses its vsync | **DEAD, three independent ways** | **(a) Symmetric:** the drag enqueues the *identical* Normal item — `SCP.Managed.cs:866`/`:877` reach the same `Set` as the fling's `:643`, same `Updated` → `OnPresenterScrolled` → `ScrollViewer.cs:1241` chain. **(b) Wrong physics:** on Android a record delayed past its vsync produces **no `Draw` at all** (`UnoSKVulkanView.cs:152-153`), hence no drop. The counter is physically incapable of registering "too slow". **(c) Causality inverted:** the gate is an *ordering* mechanism, not a delay — withholding the render action is precisely what lets `OnTick → OnRenderFrameOpportunity → Render()` run *early*, which is the door into the ahead-of-time path. It is a **precondition of the real mechanism**, not a competitor. Also: `_compositionTargets[…]` stores only a count seeded from `_queues[Normal].Count` at take time (`NativeDispatcher.cs:216`) — there is **no provenance**, so "enqueued inside the record" is not a distinction the dispatcher can make |
| **H-Load** | Per-frame record / layout / virtualization cost starves the frame | **DEAD as a cause; ALIVE as F2** | Drag and fling execute a byte-identical record from `Set` onward (`SCP.Managed.cs:521-527`), and the fling driver's own work is two decay evaluations (`:631-632`). A drag at finger speed crosses *more* item lines per frame than the fling that follows it. Any hypothesis whose only variable is load predicts drag ≈ fling. Second kill, on sign: line-crossing cost is velocity-**proportional**, so under load-as-cause the tail of a fling is the cheapest part and should be the cleanest; it is reported as the worst |
| **H-EVP** | Effective-viewport propagation enqueues a Normal item per scroll frame | **DEAD** | `PropagateEffectiveViewportChange` early-returns unless `IsEffectiveViewportEnabled` (`FrameworkElement.EffectiveViewport.cs:84`, `:349-353`), and no `ListViewBase`/`ScrollViewer`/`ScrollContentPresenter`/`ItemsStackPanel` subscribes `EffectiveViewportChanged` — the live subscribers are `ItemsRepeater`, `CalendarPanel`, `TeachingTip`, `SystemFocusVisual`. **For a plain `ListView` this leg never executes.** (P1's real supplier is the ScrollBar layout route — §2 row 1.) And it is on the drag path too |
| **H-A′** | "State A **deterministically** burns a present cycle; steady state is 2 records per 3 presents" (`02` §3, `04` §5.2, echoed by `10` §4.1) | **WRONG AS STATED** | Traced against source, the steady state is `ahead-of-time record → present (fresh) → State A → ahead-of-time record → …`: **one record per present, zero drops** — exactly the bargain the comment at `RenderScheduling.skia.cs:180-182` describes, and it works. State A costs a present *only when the ahead-of-time record does not land inside the period.* `11` §4 is right and its two siblings are wrong. This correction is load-bearing: it is why the "record instead of re-request" one-liner (below) is a **regression**, not a fix |
| **H-Window** | Drops = a vsync landing in `[in-record invalidate → picture published]`; RedirectVisual is clean because its window is sub-millisecond (`06`) | **DEAD** | Its RedirectVisual arm rests on `_runningAnimations.Count > 0`, which is **false** on that page — `LottieVisualSourceBase.cs:74-79` throws, `AnimatedVisualPlayer.mux.cs:312-317` falls back to the legacy `SKCanvasElement` flow, so `Compositor.RegisterAnimation` is never called. Corrected, it collapses into a pure cost story and over-predicts the drag, whose record is the same `ListView`. (`03` §0 carries the same error.) The *window* is real and is the proximate mechanism of every drop; it is not the trigger |
| **H-Structural-zero** | "The drag's zero is structural, so the drag/fling comparison carries no information" (`06` §5, endorsed by `11` §4) | **DEAD** | The drag's pointer handler writes `AnchorPoint` → `RequestNewFrame` → `!_ahead && !RenderRequested` → `RenderRequested = true` **and a speculative `host.InvalidateRender()`** (`RenderScheduling.skia.cs:93-97,106-113`) with no picture behind it. A drag frame that overran after that point **would** be counted. It isn't. The comparison is admissible |
| **H-Pacer** | `ChoreographerFramePacer` phase noise / late registration | **NOT THE CAUSE** | `WaitForNextFrame` runs identically on every loop iteration in all three cases (`UnoSKVulkanView.cs:161`), so it cannot produce a split. Residual real defect, filed separately: `_handler.Post` at `ChoreographerFramePacer.cs:88` registers the frame callback on *another thread's* Looper after the present, so a present finishing just before a vsync can miss it and sleep ~2 periods; and `var seen = _frameCount` at `:93` is read after the post, so a `DoFrame` arriving in between costs an extra period. **Latency contributor to F2, not a drop source** |
| **H-Loop-race** | The `_renderEvent.Reset()` / `_renderRequested` race in the Android loop | **NOT THE CAUSE** — but the window is real and both siblings located it wrong | `02` puts it between `Reset()` (`:150`) and the check (`:152`) — harmless, the flag survives. `05` declares the whole thing safe. The real window is between the check (`:152-153`) and `_renderRequested = false` (`:155`): an `InvalidateRender` landing there sets the flag, `:155` clears it, and the surviving `_renderEvent.Set()` is eaten by the next iteration's `Reset()`. Because `RequestNewFrame` only invalidates on the `false → true` transition of `RenderRequested` (`:93-97`), that lost request can park the loop for up to the 100 ms `Wait` timeout. **Workload-independent, so not this bug — but a latent hard stall.** See Fix D |
| **H-Android-cost** | Android-only per-frame waste (JNI + SVG serialization) is the cause | **NOT THE CAUSE** (hits all three) — but it is **real F2 fuel** | `UnoSKVulkanView.cs:62` calls `ExploreByTouchHelper.InvalidateRoot()` — a JNI hop into AndroidX — on **every** `InvalidateRender`, i.e. twice per fling frame, **on the UI thread, inside the critical window**. `ApplicationActivity.cs:502-512` runs `SKPath.ToSvgPathData()` (a full path→string serialization) on **every present**, from `UnoSKVulkanView.cs:220`. Both are pure waste. See Fix E |
| **H-Sync-barrier** | Android `ViewRootImpl` sync barrier blocks `Handler.post` | **REFUTED ON SIGN, and UNVERIFIED** | Traversals are scheduled by touch, so it should make the **drag** worse and the fling better. Observation is the reverse |
| **H-Instrument** | The 20 is instrumentation error | **NOT the whole story, but partly true and must be ruled out first** | `Rendering.skia.cs:147` publishes the picture *inside* `lock (_frameGate)`; `:157` bumps the generation *outside* it. A `Draw` acquiring the gate between `:155` and `:157` takes the **fresh** picture but reads the **stale** generation and scores a drop that did not happen (`SkiaRenderHelper.skia.cs:309-313`). One-sided over-count, and the render thread is actively contending for that lock during a fling. **Fix A, do it before trusting any number** |

**The Win32 datum is not admissible as a control** and must stop being cited as one. Four fatal
defects in `Given_ScrollSmoothness.cs`: (1) `FpsHelper` short-circuits on
`DebugSettings.EnableFrameRateCounter` (`SkiaRenderHelper.skia.cs:215`), which the test never sets —
**`dropped` was never measured**; (2) `CompositionTarget.Rendering` counts *records*, coalesced per
batch, so a duplicated *present* is inexpressible; (3) it samples `sut.VerticalOffset` (`:57`), a DP
written only from the deferred Normal-priority `ScrollViewer.Update` (`ScrollViewer.cs:1325`), not the
`AnchorPoint` that went into the picture; (4) **subscribing `Rendering` sets `_isRenderingActive`**
(`Rendering.skia.cs:84-98`), which makes `Render()` call `RequestNewFrame()` at the end of *every*
record (`:164-167`) — i.e. **the probe manufactures `_renderRequestedAfterAheadOfTimePaint` for every
workload, including the drag.** It also keeps the loop free-running through the 2.5 s post-fling idle
tail (`Given_ScrollSmoothness.cs:79-83`), which makes a printed "0 % duplicate offsets" arithmetically
impossible from the committed test. **What Win32 *does* earn, on structure alone and needing no
numbers:** Win32 enqueues a Normal-priority dispatcher item on **every present**
(`Win32WindowWrapper.Rendering.cs:38-43` from `RenderThread.cs:72`) that Android does not (Android
applies the clip path inline, `UnoSKVulkanView.cs:220`), and its render loop has **no**
`_renderRequested` re-check. Under H-N the platform paying more of the alleged poison would be the
sick one. It isn't. That is H-N's third, independent kill.

---

## 4. The fix

Ordered. Each is labelled `root-cause fix` or `hardening` per the debugging protocol.

### Fix A — `root-cause fix` (of the *instrument*) · 10 min · risk: none

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs`

Move `_fpsHelper.OnFrameRecorded()` from `:157` **into** the `lock (_frameGate)` block, immediately
after `_lastRenderedFrame = (framePicture, path, damageSnapshot);` (`:147`). Today a `Draw` that
acquires the gate between `:155` and `:157` reads the fresh picture with the stale generation and
counts a drop that did not happen. **Do this first** — an unknown share of the reported 20 may be this.

### Fix B — `root-cause fix` · ~5 lines · 1 h + a test · risk: low

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:134-139`

**State A must not invalidate**, because nothing new has been recorded. Replace the
`RequestNewFrame()` call with a direct latch:

```csharp
if (_renderRequestedAfterAheadOfTimePaint)
{
    _renderRequestedAfterAheadOfTimePaint = false;
    // The ahead-of-time record already published and armed the present (Rendering.skia.cs:171).
    // Arming again here would promise a picture this tick will not produce.
    RenderRequested = true;
}
```

Trace check (this is the part `02`/`04`/`10` got wrong, so it matters):

- **Healthy period** — unchanged. `Rendering.skia.cs:171` already armed the host after publishing, so
  the present at the next vsync still happens and is fresh. One record per present.
- **Overrun period** — the host is **not** armed, so at the vsync `_renderRequested == false` →
  `continue` (`UnoSKVulkanView.cs:152-153`) → **no stale present, no drop, no wasted GPU frame**. When
  the late `OnTick` publishes, `:171` wakes the render thread *immediately* rather than at the next
  vsync — this is a **latency improvement**, not just a counter change.
- **No wedge.** `RenderRequested = true` with `_renderedAheadOfTime == false` is consumed either by the
  next `OnRenderFrameOpportunity` (ahead-of-time record) or, if no `OnTick` comes, by the next render
  action taking **State C** (`:145-153`), which the next `Draw` posts because
  `_shouldEnqueueRenderOnNextNativePlatformFrameRequested` was set at `:125`.

**Do NOT instead call `Render()` in State A** (proposed as "the one-line fix" by `01` E2, `02` E2 and
`10` E2). Because `Compositor.skia.cs:372-375` sets `_renderRequestedAfterAheadOfTimePaint` on
**every** fling record, State A is entered on **every** render action, so recording there would
produce **two records per present** — it violates the stated invariant at `:180-182`, doubles UI-thread
cost, and drives `unpresented` up. It is a regression.

**Expected effect:** fling `dropped` → ~0; `fps` during a fling drops from ~120 to the honest
~100 (fps now equals unique frames, because there are no stale `Draw`s). **The stutter will still be
there** — that is F2, and Fix C/E is what removes it.

### Fix C — `root-cause fix` (the real one) · 2–3 days · risk: medium

**Drive the frame drivers before layout, not inside the record.**

**Where the hook goes.** `src/Uno.UI/UI/Xaml/Internal/CoreServices.cs`, in `OnTick` (`:77-127`), in the
per-window loop, **immediately before `root.UpdateLayout()` at `:115`**:

```csharp
foreach (var window in ApplicationHelper.WindowsInternal)
{
    if (window.RootElement is not { } root) continue;

    // Drivers write their frame's values here — before layout, before the record —
    // so their writes are ordinary pre-frame invalidations, not mid-record ones.
    (root.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget)?.RaiseFrameStarting();

    root.UpdateLayout();
    // ... Loaded ...
#if __SKIA__
    (root.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget)?.OnRenderFrameOpportunity();
#endif
}
```

and delete the `FrameStarting` raise from `Compositor.RenderRootVisual`
(`Compositor.skia.cs:307-324`) together with the `|| FrameStarting is not null` term at `:372`.

**Why this is the root cause.** The current design conflates two different meanings of
`RequestNewFrame`: *"content changed since the last record"* and *"keep the loop alive"*. A driver that
writes from inside the record produces the second but is read as the first, which is precisely what
sets `_renderRequestedAfterAheadOfTimePaint` spuriously on every fling frame. Move the write before the
record and it is correctly classified: `_renderedAheadOfTime` is still false (it is set at `:195`), so
the write takes the `!_ahead && !RenderRequested` branch, `OnRenderFrameOpportunity` immediately
consumes it, and the render action takes **State B**. **The fling becomes structurally identical to the
drag, which is the case we know is clean.**

**Second, independent win (this is F2, not F1):** the layout consequences of the fling's own offset
write — `ScrollBar.UpdateTrackLayout` → root `InvalidateMeasure` (`ScrollBar.mux.cs:1030,1055`) — are
cleaned by the **same** `UpdateLayout()` in the **same** tick instead of dirtying the tree for the next
one. That removes a whole Normal-priority main-Looper hop from the per-frame critical path.

**What breaks, and what must follow:**

1. **The frame clock.** `CurrentFrameTimestampInTicks` is stamped at `Compositor.skia.cs:311-312`
   inside `RenderRootVisual`, and `GetFrameTimestamp` (`:244-289`) recovers the presentation grid from
   the median **record** interval. Ticks and records stay 1:1 after the move, so this is a relocation
   — but `_lastRawFrameTimestamp` must now be fed from the tick, not the record. If one is fed from the
   tick and the other from the record they drift, and the drivers get exactly the v·Δt position error
   the doc comment at `:227-243` was written to prevent.
2. **`Compositor.FrameStarting` should move to `CompositionTarget`.** It is on the shared `Compositor`
   today (`:209`), but `OnTick` iterates *windows*; raising a shared event once per window would tick
   every driver N times on a multi-window app. `CompositionTarget` is already where
   `RequestNewFrame` / `_renderedAheadOfTime` / the render action live, and both
   `OnRenderFrameOpportunity` and the idle predicate need a *per-target* answer to "does this target
   have a driver". **Yes — move it, and move it as part of this change, not after.** Consumers to
   update: `SCP.Managed.cs:601` (fling), `:675` (wheel decay),
   `GestureRecognizer.Manipulation.InertiaProcessor.cs`, and `Compositor.HasFrameStartingSubscribers`
   (`:211`).
3. **`IsAnimating` / `WaitForIdle`.** `Compositor.IsAnimating => _runningAnimations.Count > 0 ||
   FrameStarting is not null` (`Compositor.skia.cs:43`) is consumed by `UITestHelper.WaitForIdle`
   (`UITestHelper.cs:113`) and stays semantically correct after the move — a subscribed driver is still
   motion. But once liveness comes from `CoreServices.RequestAdditionalFrame` rather than from
   `Compositor.skia.cs:372`, the idle predicate must **also** wait on a pending additional frame
   (`CoreServices._isAdditionalFrameRequested`, `:70`), otherwise a test can observe an idle compositor
   with the fling's next tick still queued and go on to assert against a stale offset. If
   `FrameStarting` moves to `CompositionTarget`, `IsAnimating` must aggregate over targets.
4. **Driver liveness.** `Compositor.skia.cs:372-375` currently keeps the fling alive. After the move
   the driver must call `CoreServices.RequestAdditionalFrame()` each tick. That is safe: `OnTick` resets
   `_isAdditionalFrameRequested` at `:79`, *before* the body, so a re-request from inside the tick is
   honoured. Keep the `_runningAnimations`/`transitions` terms at `:372` — Composition animations still
   tick inside the record (`:326-342`) and that is correct for them.
5. **The `FrameStarting` doc comment** (`Compositor.skia.cs:200-208`) states the current design as an
   invariant ("the only pre-record per-frame hook… `CompositionTarget.Rendering` is raised after the
   record"). Rewrite it; the whole point of the change is that the hook is no longer *in* the record.
6. **`ScrollDiagnostics`** deliberately uses `CompositionTarget.Rendering`, not `FrameStarting`, so it
   observes the value that went into the frame (`SCP.Managed.cs:164-172`). Unaffected — and the comment
   there explaining *why* becomes the model for the new hook's doc.

### Fix D — `hardening` · half a day · risk: low-medium

`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs`

Gate the render loop on a **published-frame generation** rather than the bare `volatile bool
_renderRequested` (`:35`). Promote `FpsHelper`'s `_currentFrameGeneration` /
`_lastPresentedGeneration` (`SkiaRenderHelper.skia.cs:185-186`) onto `CompositionTarget` as
non-debug state, and have `RenderLoop` `continue` when nothing new has been published. This makes a
stale present **impossible regardless of who lies**, and it closes the lost-wakeup window between
`:152-153` and `:155` in the same edit. Defence in depth behind Fix B, and it fixes a genuine latent
stall.

### Fix E — `hardening` (but it is what actually attacks F2) · ~1 day · risk: low

1. `UnoSKVulkanView.cs:62` — hoist `ExploreByTouchHelper.InvalidateRoot()` out of `InvalidateRender`.
   It is a JNI hop into AndroidX on the **UI thread**, twice per fling frame, inside the critical
   window. Accessibility does not need re-invalidating twice per frame; coalesce it (e.g. on the
   record, or debounced).
2. `ApplicationActivity.cs:502-512` — the `NativeLayerHost.Path` setter runs
   `SKPath.ToSvgPathData()` on **every present** (from `UnoSKVulkanView.cs:220`) purely to compare
   against the previous value. Compare on `SKPath` reference identity first; `CompositionTarget` already
   returns the same instance when nothing changed (`Rendering.skia.cs:314-325`).
3. `Visual.Damage.skia.cs:100` — `canUseBounds` is false for every `ShapeVisual`
   (`ShapeVisual.skia.cs` overrides `CanPaint` but not `PaintsWithinOwnSize`), so any
   Path/Ellipse/Rectangle/icon in an item template pays a stroke-to-fill plus two path booleans **per
   visual per frame** while scrolling. Fix the override.

---

## 5. The proof

### 5.1 On Win32, via the existing harness — the discriminating experiment, no device needed

`src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_ScrollSmoothness.cs`

**Make the harness admissible first** (this is mandatory — see §3):

1. Set `Application.Current.DebugSettings.EnableFrameRateCounter = true` for the capture, and expose
   an `internal` accessor on `FpsHelper` for `fps` / `dropped` / `unpresented`. **Presents, not
   records.**
2. Stop probing with `CompositionTarget.Rendering` — drive the capture from the internal
   `CompositionTarget.FrameRendered` event (`Rendering.skia.cs:80`). Subscribing `Rendering` sets
   `_isRenderingActive` and manufactures the flag under test (`:164-167`).
3. Sample `-((UIElement)sut.Content).Visual.AnchorPoint.Y`, not the deferred `VerticalOffset` DP.
4. Assert per phase; drop the 2.5 s idle tail from the aggregate.
5. Add three counters: State A (`RenderScheduling.skia.cs:137`), State B (`:142`), State C (`:152`).

**Then run three rows, and inject a configurable spin (0 → 12 ms) into a `Compositor.FrameStarting`
handler registered *after* the scroll driver** to push the record past the period:

| Row | Injected spin | Predicted State A/s | Predicted `dropped`/s | Predicted `fps` |
|---|---|---|---|---|
| Fling | 0 ms | **≈ present rate (~120)** | **≈ 0** | ~121 |
| Fling | 6 ms | ≈ present rate | **large — tens/s** | ~121 (stale `Draw`s counted) |
| Fling | 6 ms, **after Fix B** | ≈ present rate | **≈ 0** | **drops to ~60–80** (honest) |
| Drag (`PointerMoved`-driven, no injection into the record) | 6 ms | **0** | **≈ 0** | falls, but no drops |
| Composition-animation page (no `RequestAdditionalFrame`) | 6 ms | **0** | **≈ 0** | falls, no drops |

**The discriminator is rows 2 vs 4**: the same injected cost, one arm drops and the other does not.
A pure-load hypothesis (H-Load) predicts **both** degrade identically. If both drop, this verdict is
wrong and F1 is not real. If neither drops, F2 is not real and the mechanism is purely structural
(then State A/s should equal `dropped`/s at 0 ms spin, which row 1 says it does not).

Secondary prediction that is cheap and sharp: **State A/s ≈ the present rate on Win32 during a
fling, with `dropped` ≈ 0.** That single number reconciles the Win32 result with this verdict — Win32
enters State A on essentially every frame and it is harmless, because Win32 never overruns.

### 5.2 On device, using only the FPS overlay's own counters

No code change beyond Fix A. Product owner reads three numbers per arm, from the overlay
(`SkiaRenderHelper.skia.cs:380-385` — `fps`, `dropped`, `unpresented`):

| Arm | `fps` | `dropped` | `unpresented` |
|---|---|---|---|
| Drag | 100+ | **~0** | **~0** |
| Fling — **before** any fix | 100+ | **20+** | **~0** |
| Fling — **after Fix A only** | 100+ | **≤ 20** — a fall here means part of the 20 was the instrument | ~0 |
| Fling — **after Fix B** | **~100** | **~0** | ~0 |
| Fling — **after Fix C + E** | **~118–120** | ~0 | ~0 |
| RedirectVisual | 120 | 0 | 0 |

**`unpresented ≈ 0` is a prediction this verdict can fail.** It says the UI thread is *under*-producing
relative to presents. If `unpresented > 0` during the fling, the UI thread is over-producing and both
F1 and F2 need revision. **Ask for this number first — it is already on screen.**

**One free decisive check before writing any code:** set `Compositor.SkipVisualTreePainting`
(`Compositor.skia.cs:40,349-352`) during the fling. It skips **only** the paint walk while
`FrameStarting` still ticks and `:372-375` still re-arms — i.e. it removes F2 and keeps F1. Prediction:
**`dropped` → ~0**. If `dropped` stays at 20, F2 is not the gate and the mechanism is F1 alone (in
which case Fix B alone is sufficient and Fix C is optional). If `dropped` → 0, both factors are
confirmed and the full ladder is warranted.

**The one thing this verdict does not derive: "worse the slower the fling gets." UNVERIFIED.** The
leading explanation is perceptual — a doubled step is a larger *relative* timing error at low velocity,
and the eye tracks individual items instead of a blur, so a constant drop rate reads as worsening. The
mechanical alternatives all come out with the *wrong* sign (line-crossing cost and damage area both
shrink as velocity decays). **Settle it by bucketing `dropped`/s against instantaneous velocity across
one fling.** A flat rate confirms the perceptual reading; a rising rate means there is a mechanism
nothing in `drops/` has found yet.

---

## 6. What NOT to do

1. **Do not "fix" State A by calling `Render()` there.** Proposed as the one-liner by `01` E2, `02` E2
   and `10` E2. Because `Compositor.skia.cs:372-375` sets the flag on **every** fling record, State A
   is entered on **every** render action; recording there doubles the record rate, violates the
   invariant at `RenderScheduling.skia.cs:180-182`, and inflates `unpresented`. See Fix B.

2. **Do not touch the Normal-priority dispatcher gate** (`NativeDispatcher.cs:206-234`). It is not the
   defect (§3, H-N) and it is not even a delay — it is the *ordering* mechanism that produces the
   early record, which is a latency feature. Removing it would make things worse on the drag path,
   which is currently clean.

3. **`Surface.setFrameRate` (`SurfaceFrameRate.cs`) is irrelevant to this defect. Bluntly: it has
   nothing to do with it.** It runs **once**, in `SurfaceChanged` (`UnoSKVulkanView.cs:104`), and asks
   the display for its highest mode. It cannot distinguish drag from fling from RedirectVisual —
   all three run on the same surface with the same requested rate. It fixed a genuine, *different*
   problem (Android assigning a 90 Hz frame-rate category on a 120 Hz panel, giving a hardware-level
   1,1,2 cadence — measured at 64 % single / 35 % double before the call, `SurfaceFrameRate.cs:12-17`).
   **Keep it. Do not credit it, do not blame it, and do not tune it for this bug.**

4. **The `ChoreographerFramePacer` frame-count fix is also not this defect.** Bluntly: replacing the
   event with a count (`ChoreographerFramePacer.cs:32-36`) fixed a real correctness bug — a stale
   signal from a timed-out wait satisfying every subsequent wait, leaving the render thread permanently
   unpaced. But `WaitForNextFrame` executes identically on every loop iteration in all three arms
   (`UnoSKVulkanView.cs:161`), so it cannot produce a 0/20/0 split. It contributes phase jitter to F2
   (the per-wait `_handler.Post` at `:88`, and the `seen` read after the post at `:93`) — file those,
   fix them on their own merits, do not present them as the answer here.

5. **Do not ship a stable frame-rate divisor (present at 60 on a 120 Hz panel) as the fix.** It is a
   defensible *feature* — an uneven 100 does read worse than a rock-steady 60, and
   `SurfaceFrameRate.cs:12-17` already makes that argument with measurements — but as a fix it (a)
   hides the defect, (b) costs 8.33 ms of worst-case input-to-photon on the drag path that is
   currently clean, and (c) may not even help: under F1 a longer pacer wait *lengthens* the window in
   which the render thread sits armed and hungry. Build it later, adaptive and hysteretic, scoped to
   sustained overrun — not as this bug's resolution.

6. **Do not cite the Win32 "121 callbacks/s, 0 % duplicate offsets" number again until §5.1 items 1–5
   are done.** `dropped` was never measured on Win32 (`SkiaRenderHelper.skia.cs:215`), the metric
   counts records not presents, the sampled DP is not the value that was rendered, and the probe sets
   `_isRenderingActive`, manufacturing the exact flag under test. The *structural* Win32/Android
   comparison in §3 (H-N's third kill) needs no numbers and should be trusted; the numbers should not.

7. **Do not measure anything with `ScrollDiagnostics.IsEnabled` on** (`SCP.Managed.cs:164-172`). Every
   loaded `ScrollContentPresenter` then subscribes `CompositionTarget.Rendering`, setting
   `_isRenderingActive`, which forces `_renderRequestedAfterAheadOfTimePaint` on every ahead-of-time
   record (`Rendering.skia.cs:164-167`) — importing State A **into the drag** and destroying the
   control arm.

8. **Do not chase the record's raw cost first.** It is F2 fuel and Fix E is worth doing, but drag and
   fling record the same tree; no amount of making the record cheaper changes *which* arm can report a
   drop.

---

## 7. Evidence ledger

| Claim | Status |
|---|---|
| Android's `Draw` is invalidate-driven and vsync-**paced**; a record that misses its vsync produces no `Draw` and no drop | **Verified** — `UnoSKVulkanView.cs:146-162`, `:60-65` |
| `dropped++` ⟺ a `Draw` ran with no `Render()` since the previous `Draw` | **Verified** — `SkiaRenderHelper.skia.cs:268-284, 292-324`; `Rendering.skia.cs:147, 157, 240` |
| `fps` counts `Draw`s including stale ones ⇒ unique frames = fps − dropped | **Verified** — `SkiaRenderHelper.skia.cs:243-260` |
| State A invalidates without recording; State B is silent; State C records | **Verified** — `RenderScheduling.skia.cs:134-139` / `:140-143` / `:145-153`, `:106-113` |
| `_renderedAheadOfTime` is set **before** the ahead-of-time `Render()` runs, so the record's own writes set `_rRAAOTP` | **Verified** — `RenderScheduling.skia.cs:195, 205` |
| `Compositor.skia.cs:372-375` fires unconditionally during a fling, never during a drag | **Verified** — `Compositor.skia.cs:372`; `SCP.Managed.cs:601, 521-527` |
| The fling driver runs **inside** `RenderRootVisual`, before the paint walk | **Verified** — `Compositor.skia.cs:307-324` vs `:349-352` |
| Drag and fling call `Set` with identical options from different phases | **Verified** — `SCP.Managed.cs:643` vs `:866`/`:877` |
| P1's supplier for both scroll arms is the ScrollBar layout route, not effective viewport | **Verified by inspection** — `ScrollViewer.xaml:274`; `ScrollBar.mux.cs:729-733, 1030, 1055`; `XamlRoot.crossruntime.cs:14-19`; `CoreServices.cs:67-75` |
| The effective-viewport route is dead for a plain `ListView` | **Verified** — `FrameworkElement.EffectiveViewport.cs:84, 256-266, 349-353` + exhaustive subscriber grep |
| RedirectVisual has **no** Composition animation and **no** `RequestAdditionalFrame`; its frames come from a Skottie self-invalidate | **Verified** — `LottieVisualSourceBase.cs:74-79`; `AnimatedVisualPlayer.mux.cs:299-317`; `LottieVisualSource.Skottie.cs:347-349`; `RedirectVisualTests.xaml:14-67` |
| State A is **benign** in steady state (one record per present) — `02`/`04`/`10` are wrong on this | **Verified by inspection** — trace against `RenderScheduling.skia.cs:120-208`, `CoreServices.cs:67-127`, `Rendering.skia.cs:110-198` |
| `OnFrameRecorded()` sits outside `_frameGate` ⇒ one-sided drop over-count | **Verified** — `Rendering.skia.cs:135-157` vs `:233-241` |
| The `:152-153` → `:155` lost-wakeup window | **Verified by inspection**; magnitude **UNVERIFIED** |
| `FpsHelper` is entirely disabled unless `EnableFrameRateCounter` is set ⇒ Win32 never measured `dropped` | **Verified** — `SkiaRenderHelper.skia.cs:215, 270, 294`; `Given_ScrollSmoothness.cs:35-106` |
| Subscribing `CompositionTarget.Rendering` manufactures `_rRAAOTP` on every ahead-of-time record | **Verified** — `Rendering.skia.cs:84-98, 164-167` |
| Android pays a JNI `InvalidateRoot()` per invalidate on the UI thread and an `SKPath`→SVG serialization per present | **Verified** — `UnoSKVulkanView.cs:60-65, 220`; `ApplicationActivity.cs:502-512` |
| `IsAnimating` already covers frame drivers and feeds `WaitForIdle` | **Verified** — `Compositor.skia.cs:43`; `UITestHelper.cs:113` |
| **The ~17 % overrun rate (F2's magnitude) on device** | **UNVERIFIED** — §5.2 |
| **"Worse the slower the fling gets"** | **UNVERIFIED, and not derived by anything in `drops/`** — §5.2 |
| Nothing in this document was compiled or executed | **True.** Evidence class: code review only |
