# 10 — Three-way refutation: every hypothesis in `drops/`, scored against drag / inertia / RedirectVisual

Scope: read `drops/01`–`06`, extract every hypothesis they state, and independently re-derive each
one's prediction for the three observations **from source**, not from the sibling notes. Kill
anything that cannot produce a 0-vs-20-vs-0 split.

Everything below is **code review by inspection** at `dev/mazi/smooth-scroll`. Nothing was compiled
or executed. Claims that depend on device behaviour are marked **UNVERIFIED**.

**Bottom line: exactly one hypothesis survives.** It is the `EnqueueRenderCallback` *State A* branch
(`CompositionTarget.RenderScheduling.skia.cs:134-139`), reached only when two independent
preconditions hold at once. It is the same mechanism `01` §6, `02` §2 and `04` §5.2 converged on
from three different directions; this note verifies every link in its chain and kills the other
fourteen. Six sibling claims are outright wrong and are corrected in §5 — including two that were
being used to *support* the surviving hypothesis and one that was being used to argue the
observation carries no information.

---

## 1. The four facts every prediction has to be scored against

These are the pipeline invariants. Each is verified at the cited line; everything in §3 is scored
against them.

### F-1 — On Skia-on-Android a `Draw` happens **iff** somebody called `InvalidateRender()`

`UnoSKVulkanView.RenderLoop` (`src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:146-162`):

```csharp
_renderEvent.Wait(TimeSpan.FromMilliseconds(100));   // :149
_renderEvent.Reset();                                // :150
if (!_surfaceReady || _disposed || !_renderRequested)
    continue;                                        // :152-153
_renderRequested = false;                            // :155
RenderFrame();                                       // :156  → OnNativePlatformFrameRequested → Draw
_pacer.WaitForNextFrame();                           // :161  → blocks to the next Choreographer vsync
```

`_renderRequested` / `_renderEvent` are written **only** by `InvalidateRender`
(`UnoSKVulkanView.cs:60-65`). The pacer is a rate limiter *after* the draw, not the trigger.

> **Consequence that kills three hypotheses outright: a record that misses its vsync produces
> _no `Draw` at all_, therefore _no drop_.** The counter is physically incapable of registering "the
> UI thread was too slow". Any hypothesis whose mechanism is "the record was delayed" predicts
> **fewer** Draws, not more drops.

### F-2 — `dropped` counts an `InvalidateRender` that outran its picture

`SkiaRenderHelper.skia.cs:292-324`: `OnFramePresentRequested` is called once per `Draw`, at the top,
inside `_frameGate` (`CompositionTarget.Rendering.skia.cs:233-241`). It increments `_droppedThisSecond`
when `_currentFrameGeneration == _lastPresentedGeneration` (`:309-313`), and the generation is bumped
only by `OnFrameRecorded` (`:283`), called from `Render()` (`Rendering.skia.cs:157`).

> **dropped++ ⟺ a `Draw` ran with no `Render()` since the previous `Draw`.**
> Combined with F-1: **dropped++ ⟺ an `InvalidateRender` was issued that was not backed by a
> freshly published picture, and the render thread acted on it.**

There are exactly two `InvalidateRender` call sites on the scroll path, and only one is honest:

| site | backed by a picture? |
|---|---|
| `Rendering.skia.cs:169-172`, at the end of `Render()` | **yes** — published at `:147`, generation bumped at `:157` |
| `RenderScheduling.skia.cs:106-113`, inside `RequestNewFrame` | **no** — speculative, "a record is owed" |

### F-3 — A speculative invalidate is harmless *unless* the next `Render()` is more than a vsync away

The render thread spends nearly the whole period parked in `_pacer.WaitForNextFrame()`
(`UnoSKVulkanView.cs:161`), so a speculative invalidate raised during that window is absorbed into
`_renderRequested` and coalesces with the backed one that follows. The **only** way it becomes a
counted drop is if no `Render()` publishes before the render thread acts on it.

### F-4 — `fps` counts `Draw`s, including dropped ones

`EndFrame` increments `_framesRenderedInLastSecond` on the render thread for every `Draw` that
reaches the paint block (`SkiaRenderHelper.skia.cs:243-260`), and a dropped `Draw` still has a
non-null `_lastRenderedFrame` (put back by `ReturnFrame`, `Rendering.skia.cs:412-434`).
So **fresh frames/s = fps − dropped**: fling ≈ 100+ − 20; drag ≈ 100+ − 0; RedirectVisual = 120.

---

## 2. The state machine, stated exactly (this is where the answer lives)

`EnqueueRenderCallback` (`RenderScheduling.skia.cs:120-157`) — the render action, run on the UI thread:

| state | condition | records? | calls `InvalidateRender`? |
|---|---|---|---|
| **A** | `_renderedAheadOfTime && _renderRequestedAfterAheadOfTimePaint` (`:134-139`) | **NO** | **YES** — `RequestNewFrame()` at `:138` → `:110` |
| **B** | `_renderedAheadOfTime && !_rRAAOTP` (`:140-143`) | **NO** | **no** |
| **C** | `!_ahead && RenderRequested` (`:145-153`) | **YES** | yes, backed, at `Rendering.skia.cs:171` |
| D | neither | no | no |

> **The entire 0-vs-20 split is the difference between A and B — one branch of one `if`.**
> Both skip the record. Only A raises an unbacked `InvalidateRender`, and by F-2 + F-3 that is a
> guaranteed drop: the picture cannot exist before the *next* render action, i.e. at least one full
> present cycle later.

Reaching A needs two independent preconditions:

- **P1 — `_renderedAheadOfTime` can be set.** Only `OnRenderFrameOpportunity`
  (`RenderScheduling.skia.cs:192-197`) sets it, and its only Android caller is `CoreServices.cs:124`,
  at the tail of `OnTick` — a Normal-priority item enqueued only by `CoreServices.RequestAdditionalFrame`
  (`CoreServices.cs:67-75`). Exhaustive caller list of `RequestAdditionalFrame` (grep over `src`):
  `EventManager.cs:34` (effective viewport), `EventManager.cs:69` (`Loaded` on next tick),
  `XamlRoot.crossruntime.cs:18,26` (layout invalidation), `CustomEventManager.cs:60`.
- **P2 — a `RequestNewFrame` lands while `_ahead == true`**, i.e. between `OnRenderFrameOpportunity`
  setting the flag at `:195` and the render action running. Note the flag is set *before* `Render()`
  is called at `:205`, so **anything the ahead-of-time record itself does counts**.

---

## 3. The master table

Every hypothesis stated anywhere in `drops/01`–`06`, with my own prediction for each case and the
line that decides it. "Predicts" = what the hypothesis implies, not what was observed.

| # | Hypothesis (source note) | Drag | Inertia | RedirectVisual | Verdict | Deciding file:line |
|---|---|---|---|---|---|---|
| **H1** | **Brief's leading hypothesis** — a fling enqueues Normal-priority work from inside the record, so the next record is delayed past its vsync (01 H1, 02 H-B, 05 H2) | **drops** — the drag reaches `Set(…, IsTouch:true, IsIntermediate:true)` from `SCP.Managed.cs:873-877`, byte-identical to the fling's `:643`, and therefore enqueues the *same* Normal item via `ScrollViewer.cs:1241` | drops | 0 (no Normal items) | **REFUTED** — over-predicts drag, and refuted a second time by F-1: a delayed record makes **no `Draw`**, hence **no drop** | `ScrollContentPresenter.Managed.cs:873-877` vs `:643`; `ScrollViewer.cs:1239-1243`; `UnoSKVulkanView.cs:152-153` |
| **H2** | Items enqueued *inside* the record are accounted differently from those enqueued before it (01 H2) | 0 | drops | 0 | **REFUTED — premise false.** `TryGetRenderAction` seeds the counter from `_queues[Normal].Count` at take time and stores nothing about provenance | `NativeDispatcher.cs:216` |
| **H3** | **`EnqueueRenderCallback` State A burns a present cycle** (01 H3, 02 H-A, 04 H-F2/A) | **≈0** — P1 yes, **P2 structurally no**: `Compositor.skia.cs:372` needs `_runningAnimations>0 \|\| transitions \|\| FrameStarting != null`, all false during a drag; the render action takes silent **State B** | **drops, one per ahead-of-time cycle** — P1 yes, **P2 with certainty**: `Compositor.skia.cs:372-375` fires unconditionally while `FrameStarting is not null`, *inside* the ahead-of-time record, while `_ahead` is already true | **0** — **P1 no**: the page never calls `RequestAdditionalFrame`, so `_ahead` is permanently false and A/B are unreachable; every record goes through State C | **SURVIVES** | `RenderScheduling.skia.cs:134-139` (A) vs `:140-143` (B); `:192-197,205`; `Compositor.skia.cs:372-375`; `SCP.Managed.cs:601`; `CoreServices.cs:124` |
| **H4** | Per-frame layout / virtualization / record cost starves the frame (01 H4, 03 H-L2, 04 H-record-cost) | **drops** — identical work through the identical `Set` call | drops | 0 | **REFUTED** — over-predicts drag. Wrong sign too: realization is line-boundary-triggered and scales *with* velocity, while the symptom worsens as the fling slows | `SCP.Managed.cs:873-877` vs `:643`; `VirtualizingPanelLayout.managed.cs:296-318` |
| **H5** | Effective-viewport propagation enqueues a Normal item per scroll frame (03 H-L1) | drops | drops | 0 | **REFUTED twice.** (a) `PropagateEffectiveViewportChange` early-returns because `IsEffectiveViewportEnabled` is false — the only four `EffectiveViewportChanged` subscribers in `src/Uno.UI` are `ItemsRepeater`, `CalendarPanel`, `TeachingTip`, `SystemFocusVisual`; `ListViewBase`/`ItemsStackPanel`/`ScrollViewer`/`SCP` never subscribe. (b) Even if live, it is on the drag path too | `FrameworkElement.EffectiveViewport.cs:84`, `:349-353` |
| **H6** | ScrollBar `UpdateTrackLayout` dirties layout to the root every scroll update (03 H-L3) | drops | drops | 0 | **REFUTED as the discriminator** — symmetric. **Reclassified: this is the supplier of P1** for H3. `Value` is template-bound to `VerticalOffset`, `OnValueChanged` → `UpdateTrackLayout` writes `Height`/`Margin`, which reaches the root and calls `RequestAdditionalFrame` | `ScrollViewer.xaml:274`; `ScrollBar.mux.cs:729-733,1041,1055-1056`; `UIElement.Layout.crossruntime.cs:68-77`; `XamlRoot.crossruntime.cs:14-19` |
| **H7** | The speculative `InvalidateRender` at `RequestNewFrame:110` wakes the render thread before the picture exists (02 H-C) | drops (the pointer handler raises it too) | drops | drops (the Lottie raises it from inside the record) | **INSUFFICIENT ALONE** — over-predicts both controls. It is the **proximate mechanism every drop goes through** (F-2), but by F-3 it only becomes a drop when the gap to the next `Render()` exceeds a vsync. **Keep as the mechanism; H3 is the trigger** | `RenderScheduling.skia.cs:93-97,106-113`; `Compositor.skia.cs:378-383` |
| **H8** | Lost wake-up in the Android render loop (02 H-D, rejected by 05 H6) | occasional | occasional | occasional | **REFUTED as the explanation** — workload-independent, cannot make a 0/20/0 split, and would surface as `unpresented` or a stall, not `dropped`. **But both sibling notes got the window wrong** — see §5.4 | `UnoSKVulkanView.cs:152-155` |
| **H9** | `_isRenderingActive` / `ScrollDiagnostics` (02 H-E, 04 F4) | drops **if enabled** | drops | 0 | **NOT IN PLAY** (`EnableDiagnostics` defaults false) but a **real measurement hazard**: with any `CompositionTarget.Rendering` subscriber, `Render():164-167` sets `_rRAAOTP` on every ahead-of-time record, which imports State A **into the drag** | `FeatureConfiguration.cs:502`; `SCP.Managed.cs:164-172`; `Rendering.skia.cs:164-167` |
| **H10** | Two-factor: self-sustaining frame request × per-frame tax (03 H-L4) | 0 "by construction" | drops | 0 | **SUPERSEDED, and its Factor A is half-wrong**: `Compositor.skia.cs:372` does **not** fire on the RedirectVisual page (§5.1). Its "the drag's 0 is structural" claim is also refuted (§5.2). What remains is H3 with Factor B recast as "the supplier of P1" | `AnimatedVisualPlayer.mux.cs:312-317`; `LottieVisualSourceBase.cs:76-79` |
| **H11** | Phase only: the drag enqueues its Normal items early, the fling from inside the record (03 H-L5) | 0 | drops | 0 | **INSUFFICIENT** — fits the shape but has no mechanism that turns "enqueued late" into a *counted* drop; F-1 says a late record produces no `Draw`. Subsumed by H3, which supplies the missing unbacked invalidate | `UnoSKVulkanView.cs:152-153` |
| **H12** | Budget inequality `L + R > T − D`, with the drag's record phase anchored to the OS input clock (05 H3) | ≈0 **only via an unverified premise** | drops | 0 | **NOT DECIDED / demote to amplifier.** Its discriminating claim is that the drag's record is anchored by input delivery — but the drag's record is produced by the *same* `OnTick → OnRenderFrameOpportunity` Normal item as the fling's, posted the same way on the same looper. The asserted phase difference does not exist in the code | `CoreServices.cs:73,115,123-125` — identical for both scroll cases |
| **H13** | `ChoreographerFramePacer` late registration / phase noise (05 H4, R1-R2) | neutral | worsens | neutral | **REFUTED as cause** — `WaitForNextFrame` runs identically on every loop iteration for all three cases, so it cannot make a split. **The R2 race is real**: `seen` is read *after* the post, so a `DoFrame` landing between the post and the `lock` costs a full extra period | `ChoreographerFramePacer.cs:88-93` |
| **H14** | Android-specific per-present cost (`NativeLayerHost.Path` SVG serialisation; `ExploreByTouchHelper.InvalidateRoot()` JNI per invalidate) (05 H5) | drops | drops | drops | **REFUTED as cause** — hits all three. Real waste worth removing: it inflates `D` and shrinks everyone's budget, and the JNI call runs on the **UI thread**, twice per fling frame | `UnoSKVulkanView.cs:62,220` |
| **H15** | Android `ViewRootImpl` sync barrier blocks `Handler.post` messages (04) | should be **worse** (traversals are scheduled by touch) | should be **better** (no touch) | 0 | **REFUTED on sign**, and **UNVERIFIED** | `NativeDispatcher.Android.cs:40-43` |
| **H16** | Window-width: drops = a vsync landing in `[in-record invalidate → publish]`; RedirectVisual's window is too narrow (06 §0) | **drops** — same record cost as the fling, so the same window width | drops | 0 | **REFUTED.** Its RedirectVisual arm rests on `_runningAnimations.Count > 0`, which is **false** (§5.1). Corrected, it becomes a purely quantitative cost story and therefore over-predicts the drag, whose record is the same ListView | `AnimatedVisualPlayer.mux.cs:312-317`; `SCP.Managed.cs:873-877` vs `:643` |

### Scoring rule applied

A hypothesis was killed if it predicts drops for the drag (it must not — the drag runs the *same*
`Set` → `Updated` → `OnPresenterScrolled` → virtualization → ScrollBar chain, from the same
`options` record) or predicts drops for RedirectVisual, **or** if its mechanism cannot reach the
counter at all under F-1/F-2. "Could contribute" was not accepted: H7, H13 and H14 are all real and
all rejected as *the* explanation because none of them can produce a 0-vs-20 split.

---

## 4. The surviving mechanism, traced per case

### 4.1 Fling — State A engages deterministically on every ahead-of-time cycle

1. `StartFling` subscribes `OnFlingFrame` to `Compositor.FrameStarting` for the whole fling
   (`SCP.Managed.cs:601`).
2. Some Normal-priority `OnTick` is pending (P1 supplier: the ScrollBar's `UpdateTrackLayout` per
   scroll update — `ScrollBar.mux.cs:1041,1055-1056` → root → `CoreServices.cs:73`; plus container
   realization → `EventManager.cs:69`).
3. `OnTick` → `root.UpdateLayout()` (`CoreServices.cs:115`, tree now clean so
   `CanRecordPicture` passes at `RenderScheduling.skia.cs:185`) → `OnRenderFrameOpportunity`
   → `RenderRequested==true && !_ahead` → **`_renderedAheadOfTime = true`** (`:195`) → `Render()` (`:205`).
4. Inside that record: `Compositor.RenderRootVisual` raises `FrameStarting` **before** the paint walk
   (`Compositor.skia.cs:307-315`) → `OnFlingFrame` writes the offset → `visual.AnchorPoint = target`
   (`SCP.Managed.cs:525`) → `InvalidateRenderPartial` → `RequestNewFrame` → `_ahead` is true →
   **`_rRAAOTP = true`**. And even on a frame where the offset did not move,
   `Compositor.skia.cs:372-375` re-requests **unconditionally** because `FrameStarting is not null`.
   **P2 is satisfied twice over, with certainty.**
5. The record publishes at `Rendering.skia.cs:147` and fires the backed invalidate at `:171` — this
   cycle is fine.
6. The render action (posted by the previous `Draw` at `RenderScheduling.skia.cs:170-173`) then runs
   → **State A** → clears both flags → `RequestNewFrame()` → `RenderRequested=true` +
   **unbacked `InvalidateRender`** → **returns without recording**.
7. The render thread acts on that invalidate and presents the same picture → `current == lastPresented`
   → **dropped++** (`SkiaRenderHelper.skia.cs:309-313`). The next picture cannot exist until the
   following render action: **one full present cycle of dead time.**

Note what the design intended. The comment at `RenderScheduling.skia.cs:180-182` says the skip keeps
the overall `Render` rate the same. That bargain is sound in State B (nothing changed since the early
record). In State A the content *did* change, and the code takes the strictly worst option available:
it **skips and waits**, when calling `Render()` inline would cost nothing.

### 4.2 Drag — State B, silent

Identical up to step 3 (`_ahead = true`, early record, backed invalidate at `:171`). Then:
`Compositor.skia.cs:372` is false on all three terms — `FrameStarting` has no subscriber, and the
`IsTouch` branch of `Update` *stops* animations before writing `AnchorPoint`
(`SCP.Managed.cs:521-526`). Nothing re-requests. The render action takes **State B**
(`RenderScheduling.skia.cs:140-143`): it clears `_ahead` and **does not invalidate**. No extra `Draw`,
no drop. Steady state is one record per present, `dropped == 0`.

The drag's only route into State A is a `MotionEvent` landing in the window between the ahead-of-time
`Render()` (`:205`) and the render action — one dispatcher hop wide. That is consistent with "~0",
not with "exactly 0", and it is why the observation is *approximately* zero.

### 4.3 RedirectVisual — the ahead-of-time path is unreachable

The page's only motion is the squirrel Lottie, and it is **not** a Composition animation:
`LottieVisualSourceBase.TryCreateAnimatedVisual` throws (`LottieVisualSourceBase.cs:76-79`),
`AnimatedVisualPlayer` catches and falls back to the legacy flow
(`AnimatedVisualPlayer.mux.cs:299-317`), so `Compositor.RegisterAnimation` is never called and
`_runningAnimations` stays empty. The driver is a self-invalidating paint:
`LottieVisualSource.Skottie.cs:346-351` calls `_skCanvasElement.Invalidate()` from inside `Render`
→ `SKCanvasElement.cs:56` → `SKCanvasVisual.skia.cs:24` → `Compositor.InvalidateRender` →
`InvalidateRenderPartial` (`Compositor.skia.cs:378-383`) → `RequestNewFrame`.

So RedirectVisual **does** request its next frame from inside the record, exactly like a fling —
which is why "requests a frame from inside the record" is **not** the discriminator. What it never
does is touch layout: `SKCanvasElement.Invalidate` goes to the *visual*, not to measure/arrange, so
`RequestAdditionalFrame` is never called, `OnTick` never runs, `OnRenderFrameOpportunity` is never
invoked, `_renderedAheadOfTime` is permanently false, and States A and B are **unreachable**. Every
record is State C: one record per `Draw`, 120 FPS, zero drops.

Secondary (not needed for the verdict): with no Normal items,
`normalItemsToProcessBeforeNextRenderAction` re-seeds to 0 on every handover
(`NativeDispatcher.cs:216`) and the render action is never withheld (`:214`).

---

## 5. Corrections to the sibling notes

### 5.1 `_runningAnimations` is empty on the RedirectVisual page — `06` §0 is wrong, `04` §2.1 is right

`06`'s three-way table asserts "Lottie/`AnimatedVisualPlayer` keeps `_runningAnimations.Count > 0` ⇒
the same `:374` fires every frame". **Refuted**: the WinUI flow is never selected
(`AnimatedVisualPlayer.mux.cs:302-317` + `LottieVisualSourceBase.cs:76-79`), so
`Compositor.skia.cs:372` does **not** fire on that page and `Compositor.IsAnimating` is false
(`Compositor.skia.cs:43`). `03` §0's "an animation is running (RedirectVisual)" is wrong for the same
reason. This invalidates `06`'s entire discriminator (H16) and half of `03`'s Factor A (H10).

### 5.2 The drag's zero is **not** structurally guaranteed — `06` §5 is wrong

`06` claims a drag "can have an arbitrarily expensive record, miss any number of vsyncs, and still
report `dropped == 0`", and concludes the two arms of the comparison are not comparable. **Refuted**:
the drag's pointer handler writes `AnchorPoint` → `RequestNewFrame` → `!_ahead && !RenderRequested`
→ `RenderRequested = true` **and a speculative `host.InvalidateRender()`**
(`RenderScheduling.skia.cs:93-97,106-113`), with no picture behind it. If the drag's record then
misses the vsync, the resulting `Draw` is stale and **is** counted. The drag's ~0 therefore does mean
"the drag produces one record per present" — **the observation carries real information.**

### 5.3 The stale `Draw` paints nothing, but the user still sees a repeat — `06` §0.2 needs qualifying

`06` is right that the repeat `Draw` paints nothing: the presented frame's damage `SKPath` was
`Reset()` by the previous `Draw` (`Rendering.skia.cs:309`), so the picture replay is fully clipped
out at `:291`. But Skia renders into a **persistent intermediate image** that is blitted to the
swapchain on every present (`src/Uno.UI/Vulkan/VulkanContext.skia.cs:30,177-215,291`), so the pixels
presented are byte-identical to the previous frame. The `FpsHelper` doc comment
(`SkiaRenderHelper.skia.cs:286-291`) is therefore correct about the *effect*, if not the mechanism.

### 5.4 The render-loop race window — `02` H-D and `05` H6 are both wrong, in opposite directions

`02` puts the race between `Reset()` (`:150`) and the `_renderRequested` check (`:152`); that window
is harmless — the flag survives and the loop draws. `05` declares the whole thing safe. The real
window is between the check (`:152-153`) and `_renderRequested = false` (`:155`): an `InvalidateRender`
landing there sets the flag, `:155` immediately clears it, and the surviving `_renderEvent.Set()` is
consumed by the next iteration's `Reset()` — that request is **lost**. Because `RequestNewFrame` only
raises `InvalidateRender` on the `false → true` transition of `RenderRequested`
(`RenderScheduling.skia.cs:93-97`), a lost request during a fling can wedge the loop until the 100 ms
`Wait` timeout or the next unrelated invalidate. A few instructions wide, workload-independent, and
not the observation — but a real latent defect.

### 5.5 The `Win32` datum cannot refute H3, and the harness perturbs what it measures

`Given_ScrollSmoothness.cs` counts `CompositionTarget.Rendering` callbacks and duplicate offsets
(`:55-61,101-105`) — i.e. **records, not presents**. A duplicated present is invisible to it (F-2).
It also subscribes `Rendering`, setting `_isRenderingActive`, which makes `Render():164-167` call
`RequestNewFrame()` at the end of *every* record — importing State A into workloads that would not
otherwise reach it (H9). So "121 callbacks/s, 0% duplicate offsets" is neither confirmation nor
refutation of H3. See §6 for the version that would be.

### 5.6 The dispatcher gate helps the drag; the brief has its causality backwards

`TryGetRenderAction` withholding the render action behind Normal items (`NativeDispatcher.cs:214-216`)
is what lets `OnTick → OnRenderFrameOpportunity → Render()` run **early** at all. It is not a delay
mechanism; it is an **ordering** mechanism. Also verified: on Skia-on-Android the priority argument is
ignored — `EnqueueNative` is a bare `_handler.Post` to the shared main Looper
(`NativeDispatcher.Android.cs:40-43`; Skia-Android references
`Uno.UI.Dispatching.netcoremobile.csproj`, `Uno.UI.Runtime.Skia.Android.csproj:88`) — so all four
priorities are the same native message and `DispatchItems` runs exactly one item per message
(`NativeDispatcher.cs:151-154,220-223`). The gate costs **looper messages, not vsyncs**.

---

## 6. What H3 does **not** explain, stated plainly

1. **The engagement rate.** H3 predicts one drop per ahead-of-time cycle. Observed ~20 drops per ~120
   presents implies the ahead-of-time path engages on ~1 cycle in 6. Whether `OnTick` fires every
   scroll frame (ScrollBar route) or only on line-boundary crossings (realization route), and who wins
   the `TryGetRenderAction` race, is a timing question I cannot settle by inspection. **UNVERIFIED.**
2. **Why Win32 is clean — and this is the sharpest challenge to H3.** `CoreServices.cs:124` is
   `#if __SKIA__`, so Win32 has the same ahead-of-time path, and the Win32 harness's `Rendering`
   subscription should make State A *more* likely, not less. 121 records/s on a ~121 Hz display means
   records ≈ presents, i.e. **State A was not engaging on Win32**. The only structural difference I
   can point to is ordering: Win32's dispatcher is a dedicated, Uno-only message pump
   (`Win32EventLoop.cs`), so the Normal queue drains promptly and `TryGetRenderAction` hands the render
   action over with `N == 0` — the render action wins the race and `OnRenderFrameOpportunity` finds
   nothing to do. On Android every item is a `Handler.Post` onto the shared main Looper, interleaved
   with input, binder and platform traffic, so the ordering is a genuine race. **This is a plausible
   and testable reconciliation, and it is UNVERIFIED.** If E1 shows State A firing on Win32 too, H3 is
   incomplete.
3. **"Worse the slower the fling gets."** H3 has a plausible right-signed mechanism — as velocity
   decays the record and paint get cheaper (damage shrinks), the UI thread gains slack, `OnTick` more
   reliably beats the render action, so the ahead-of-time path engages *more* often — plus the
   perceptual term (a doubled step is a larger fraction of a smaller step). **Neither is verified.**
   This is the one part of the observation that nothing in `drops/` currently derives.

---

## 7. Experiments, ordered by how much they can kill

**E1 — decisive, and runnable on Win32 with no device.** Count the three exits of
`EnqueueRenderCallback` per second — A (`:137`, whose trace message is unique:
*"rendered ahead of time and got a new frame request since…"*), B (`:142`), C (`:152`) — plus
`FpsHelper`'s `dropped`, for three workloads: the existing fling, a `PointerMoved`-driven drag
equivalent, and a composition-only page. Run **without** any `CompositionTarget.Rendering` subscriber
(§5.5) and with `EnableDiagnostics` off.
*H3 predicts:* A ≈ 0 / A > 0 / A = 0, and `dropped` tracking A one-for-one.
*If A is ~0 during the fling, H3 is dead* and H7's cost story is next.
*If A > 0 on Win32 while duplicates stay at 0%, §6.2's reconciliation is wrong.*

**E2 — one line, and plausibly the fix.** In State A, record instead of rescheduling:

```csharp
if (_renderRequestedAfterAheadOfTimePaint)
{
    _renderRequestedAfterAheadOfTimePaint = false;
    Render();                       // instead of ((ICompositionTarget)this).RequestNewFrame();
}
```

(`RenderRequested` is already false in this state and the compositor re-arms it, so the invariants at
`RenderScheduling.skia.cs:210-218` hold.)
*H3 predicts:* fling `dropped` → ~0 and the stutter goes, with **no change** to drag or RedirectVisual.

**E3 — constructive, separates P1 from P2 with no framework change.** Two sample variants of the
RedirectVisual page:
(a) add a per-frame `XamlRoot.InvalidateArrange()` — imports **P1** only. *H3 predicts still 0 drops*
(State B, harmless). *H1/H16 predict drops appear.*
(b) additionally subscribe a no-op handler to `Compositor.FrameStarting` — imports **P2**.
*H3 predicts drops appear only when both are present.* This is the whole content of the conjunction
claim, tested in two builds.

**E4 — bisect.** Comment out `CoreServices.cs:124`. *H3 predicts fling drops → 0, drag and
RedirectVisual unchanged.* Not shippable (the early record exists for input latency), but a clean cut.

**E5 — free, do it regardless.** Move `_fpsHelper.OnFrameRecorded()` (`Rendering.skia.cs:157`) inside
the `lock (_frameGate)` block, right after `:147`. Today a `Draw` that acquires `_frameGate` between
`:155` and `:157` takes the **new** picture while reading the **old** generation and counts a drop
that did not happen — a one-sided over-count in the instrument. Rule it out before trusting any
number above.

**Confounds to eliminate first, in this order:** confirm `EnableDiagnostics` is false
(`FeatureConfiguration.cs:502`); confirm nothing in the SamplesApp shell subscribes
`CompositionTarget.Rendering`; confirm the squirrel Lottie is actually animating on the RedirectVisual
page (if it is static the control is an idle page and observation 3 carries no information at all).

---

## 8. Evidence ledger

| Claim | Status |
|---|---|
| `Draw` is invalidate-driven and vsync-paced on Android; a late record makes no `Draw` | **Verified** — `UnoSKVulkanView.cs:146-162,60-65` |
| `dropped` = an `InvalidateRender` not backed by a fresh picture | **Verified** — `SkiaRenderHelper.skia.cs:292-324`; `Rendering.skia.cs:147,157,240` |
| `fps` counts `Draw`s including dropped ones | **Verified** — `SkiaRenderHelper.skia.cs:243-260` |
| State A invalidates without recording; State B is silent | **Verified** — `RenderScheduling.skia.cs:134-139` vs `:140-143` |
| `_renderedAheadOfTime` is set *before* the ahead-of-time `Render()` runs | **Verified** — `RenderScheduling.skia.cs:195,205` |
| `Compositor.skia.cs:372-375` fires unconditionally during a fling and never during a drag | **Verified** — `Compositor.skia.cs:372`; `SCP.Managed.cs:601,521-526` |
| Drag and fling call `Set` with identical options from different phases | **Verified** — `SCP.Managed.cs:643` vs `:873-877` |
| The effective-viewport route is dead for a plain `ListView` | **Verified** — `FrameworkElement.EffectiveViewport.cs:84,349-353`; exhaustive subscriber grep |
| The ScrollBar route reaches `RequestAdditionalFrame` per scroll update | **Verified by inspection** — `ScrollViewer.xaml:274`; `ScrollBar.mux.cs:729-733,1041`; `ScrollBar.xaml:604`; `UIElement.Layout.crossruntime.cs:68-77` |
| RedirectVisual has no Composition animation and no `RequestAdditionalFrame` | **Verified** — `AnimatedVisualPlayer.mux.cs:302-317`; `LottieVisualSourceBase.cs:76-79`; `LottieVisualSource.Skottie.cs:346-351`; `SKCanvasVisual.skia.cs:24` |
| Skia-on-Android compiles `NativeDispatcher.Android.cs`; priority is ignored natively | **Verified** — `Uno.UI.Runtime.Skia.Android.csproj:88`; `NativeDispatcher.Android.cs:40-43` |
| `EnableDiagnostics` / `_isRenderingActive` are off by default | **Verified** — `FeatureConfiguration.cs:502`; `Rendering.skia.cs:84-108` |
| The `:152→:155` lost-wake-up window | **Verified by inspection**, magnitude **UNVERIFIED** |
| The `ChoreographerFramePacer` `seen` race | **Verified by inspection** — `:88-93`, magnitude **UNVERIFIED** |
| Ahead-of-time **engagement rate**, the Win32 discrepancy, the velocity dependence | **UNVERIFIED** — E1 |
| Nothing here was compiled or executed | **True.** Evidence class: code review only |
