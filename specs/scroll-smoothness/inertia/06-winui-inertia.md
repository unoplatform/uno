# How WinUI keeps INERTIA smooth — and the one property Uno is missing

**Sources read firsthand:** `D:/Work/microsoft-ui-xaml2` (`dxaml/xcp` legacy DirectManipulation path,
`controls/dev/ScrollPresenter` modern InteractionTracker path) and the working tree
`D:/Work/uno-worktrees/scrollsmooth`.

**Builds on, does not repeat:** `research/01-winui-dxaml-scrollviewer.md` (§1.2, §5.2, §5.3),
`research/02-winui-directmanipulation.md` (§1.4, §2, §3, §4), `research/03-winui-scrollpresenter.md`
(§1, §2, §4), `research/04-winui-input-frame-pipeline.md` (§1, §3, §4). Where those notes already
established a mechanism it is cited by section rather than re-derived.

---

## 0. The answer in one paragraph

WinUI has **no drag-vs-inertia smoothness asymmetry to explain**, because in both of its stacks drag
and inertia are *the same object* evaluated on the *same non-UI clock*: DManip's shared content
transform, or `InteractionTracker.Position`. The UI thread is a **listener** in both phases, never a
driver. The single design property that Uno lacks is not "inertia runs on another thread" — it is
that **the inertia curve's time argument is the frame's presentation time, supplied by the
compositor's own clock, and is therefore uniformly spaced and independent of when any app code
happened to run**. Uno's fling instead samples `Stopwatch.GetTimestamp()` at the moment the UI thread
enters the picture-record pass, and that moment is scheduled by a dispatcher whose ordering policy is
*data-dependent* (`NativeDispatcher.TryGetRenderAction`). Drag is immune because a drag offset is a
function of **finger position**, not of time; inertia is a function of **time alone**, so every
millisecond of record-time jitter becomes a position error of `v · Δjitter` — 8 px at 2000 px/s and
4 ms. That is the asymmetry, and it is fixable without moving scrolling to a compositor thread.

---

## 1. Q1 — During inertia (finger lifted), what runs per frame, on which thread, against which clock?

### 1.1 Legacy stack (`ScrollViewer` + DirectManipulation) — what runs per frame

Three things run per frame, and only one of them touches pixels.

**(a) Inside `Microsoft.DirectManipulation.dll` / DComp — the only thing that moves pixels.**
The DManip content owns a *shared content transform* created against the DComp DManip compositor
(`dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.cpp:3045-3117`,
`:3236-3266`). The XAML comp node folds that shared transform into an `ExpressionAnimation`
(`dxaml/xcp/core/hw/DManipData.cpp:152-182`, expression built in
`dxaml/xcp/core/hw/ManipulationTransform.cpp:83-106` as `"transform*manipTransform.Matrix"`), and
that expression is what the visual's matrix is bound to
(`dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp:2340-2417`). Per research/02 §1.4: once this
one-time setup is done, the per-frame data flow is `DManip → DComp shared transform →
ExpressionAnimation → Visual`. **The XAML UI thread is not in that loop in either phase.**

**(b) On the XAML UI thread — a bookkeeping poll, once per tick, that does not move pixels.**
`CInputServices::ProcessUIThreadTick` (`dxaml/xcp/core/input/InputServices.cpp:9098-9122`) →
`ProcessDirectManipulationViewportChanges` (`:7092-7110`) →
`ProcessDirectManipulationViewportValuesUpdate` (`:8604-8790`) does exactly **one**
`GetPrimaryContentTransform` read per viewport per tick and pushes the resulting offsets into
`ScrollViewer::HandleManipulationDelta`. It is explicitly marked as an *independent* invalidation so
it cannot force a render walk (`InputServices.cpp:8720-8736`, `DirtyFlags::Independent`). Its
purpose is virtualization, `ViewChanging`, scroll-indicator state — the data model, not the picture.
Per-content and per-viewport transform *callbacks* are deliberately declined
(`DirectManipulationViewportEventHandler.cpp:130-135`, `:146-152` both `return S_FALSE`) precisely so
that DManip never interrupts the UI thread per manipulation frame (research/02 §4.1).

**(c) A self-sustaining tick request.** `InputServices.cpp:7317-7323` re-arms the UI-thread frame
loop while `IsViewportActive(status)` — and `XcpDMViewportInertia` is one of the four active statuses
(`dxaml/xcp/core/inc/InputServices.h:1078-1081`). This is a *request for a bookkeeping tick*, not the
thing that advances the animation.

**Clock:** the manipulation/inertia curve is advanced by DManip against the frame-info provider it was
given at viewport creation. See §2.

### 1.2 Modern stack (`ScrollView` / `ScrollPresenter` + `InteractionTracker`)

**Per frame on the compositor thread:** `InteractionTracker` advances `Position` under its natural-motion
inertia (decay rate set *once*, not per frame — `PositionInertiaDecayRate` at
`controls/dev/ScrollPresenter/ScrollPresenter.cpp:6524`, default
`c_scrollPresenterDefaultInertiaDecayRate = 0.95f` at `:31`), and an `ExpressionAnimation` bound to
the content's `Translation` re-evaluates:

```cpp
// controls/dev/ScrollPresenter/ScrollPresenter.cpp:3412  (LTR case)
translationExpression = L"Vector3(-it.Position.X + (it.Scale - 1.0f) * adjustment.X, -it.Position.Y + (it.Scale - 1.0f) * adjustment.Y, 0.0f)";
// :3415  m_translationExpressionAnimation.SetReferenceParameter(L"it", m_interactionTracker);
// :3432-3438  target = s_translationPropertyName ; content.StartAnimation(m_translationExpressionAnimation);
```

**Per frame on the UI thread:** `InteractionTrackerOwner::ValuesChanged`
(`controls/dev/ScrollPresenter/InteractionTrackerOwner.cpp:24-39`) → `ScrollPresenter::ValuesChanged`
(`ScrollPresenter.cpp:1172-1239`). Read that method carefully: it sets `m_zoomFactor`, calls
`UpdateOffset(...)` for each dimension, and raises `OnViewChanged`. **It never writes a transform.**
It is a notification that the model has moved, delivered after the fact.

**Once, at inertia entry:** `ScrollPresenter::InertiaStateEntered` (`ScrollPresenter.cpp:1031-1116`)
records `m_endOfInertiaPosition` / `m_endOfInertiaZoomFactor` from
`args.NaturalRestingPosition()` / `args.ModifiedRestingPosition()`. The legacy stack has the exact
analogue: at the status transition into inertia, `InputServices.cpp:8223-8248` calls
`GetDirectManipulationContentInertiaEndTransform` once and hands the resting offsets to
`NotifyManipulationProgress`, which `ScrollViewer` stashes in `m_inertiaEndHorizontalOffset` /
`m_inertiaEndVerticalOffset` (`dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp:13744-13800`).

**This is a structural point worth naming:** in WinUI, the resting position of a fling is a *known
constant from the first inertia frame*. Nothing in the inertia phase is decided incrementally. Snap
points are pushed to the compositor as `InteractionTrackerInertiaModifier`s ahead of time
(`ScrollPresenter.cpp:2068-2120`), not evaluated per frame on the UI thread.

**The one deliberate UI-thread per-frame cost:** `IdleStateEntered` (`ScrollPresenter.cpp:1025-1029`)
calls `StopTranslationAndZoomFactorExpressionAnimations()` "to trigger rasterization of Content &
avoid fuzzy text rendering". That is an *end-of-inertia* cost, not a per-inertia-frame cost.

---

## 2. Q2 — Is the inertia curve evaluated at a presentation timestamp?

**Yes, and this is the load-bearing mechanism.**

Every DManip viewport is created with an `IDirectManipulationFrameInfoProvider`:

```cpp
// dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.cpp:4298
IFC(static_cast<IDirectManipulationManager*>(m_pDMManager)->CreateViewport(
        m_pDMFrameInfoProvider, inputHwnd, IID_PPV_ARGS(dmViewport.ReleaseAndGetAddressOf())));
```

and `EnsureFrameInfoProvider` (`DirectManipulationService.cpp:4214-4230`) does not build XAML's own
provider — it QIs **the DComp DManip compositor itself**:

```cpp
if (!m_pDMFrameInfoProvider)
{
    ASSERT(m_pDMCompositor);
    IFC(m_pDMCompositor->QueryInterface(IID_PPV_ARGS(&m_pDMFrameInfoProvider)));
}
```

The interface's single method is `GetNextFrameInfo(pTime, pProcessTime, pCompositionTime)`. XAML's
own (now-dead) implementation shows exactly what the third parameter means
(`DirectManipulationFrameInfoProvider.cpp:48-71` returning
`m_pDMService->GetDeltaCompositionTime()`), and the field's documentation
(`DirectManipulationService.h:283-286`, field at `:614`) is unambiguous:

> `// Lapse of time in milliseconds between the time the compositor calls UpdateCompositorContentTransform and the time the resulting transform is shown on screen`

The legacy compositor-thread poll that fed it hardcoded the estimate, with a TODO that was never
closed:

```cpp
// dxaml/xcp/core/compositor/CompositorDirectManipulationViewport.cpp:50-65
// TODO - Jupiter (Windows) bug 847117. Replace 16 with the actually milliseconds until the transform is shown on screen
IGNOREHR(pCompositorService->UpdateCompositorContentTransform(pCompositorContent, 16 /*deltaCompositionTime*/));
```

So the design contract is explicit: **the inertia curve is sampled at `t_present`, not `t_now`**, and
in the shipping configuration the presentation estimate comes from DComp's own composition clock
rather than from anything the app can perturb. Cross-slide viewports — which never animate — are the
only ones created with a `NULL` frame-info provider (`DirectManipulationService.cpp:4358`), which is
a nice negative confirmation that the provider exists *for the animation*.

For `InteractionTracker`, the equivalent claim is structurally forced rather than directly readable:
`Position` is advanced by the Windows.UI.Composition animation engine on the compositor thread against
the composition clock, and the visual property is bound by an `ExpressionAnimation` evaluated in the
same pass. **UNVERIFIED:** the exact presentation-time offset WUC applies is inside
`Windows.UI.Composition` and is not in this repo.

---

## 3. Q3 — Is the inertia transform applied by the compositor with no per-frame UI-thread involvement?

**Yes, in both stacks, and it is provable by what happens when the UI thread is absent.**

Legacy: `HWCompNodeWinRT.cpp:2331-2337` shows that the presence of a DManip manipulation is exactly
what *promotes* the visual off the static `TransformMatrix` path onto the expression path:

```cpp
bool requiresTransformExpression =
    hasIndependentTransformManipulation     // DManip-driven animation
    || hasIndependentTransformAnimation
    || !redirectionIsTranslationOnly;
```

The clinching evidence is the bail-out XAML has to implement for the case where the compositor
*cannot* carry the inertia — because then, and only then, the motion cannot be shown at all:

```cpp
// dxaml/xcp/core/input/InputServices.cpp:7373-7399  StopInertialViewportWithoutCompositorPeer
if (currentStatus == XcpDMViewportInertia && pViewport->GetManipulatedElementNoRef()->GetCompositionPeer() == nullptr)
{
    // The viewport is in the Inertia phase and it does not have a composition peer.
    // Immediately jump to the end-of-inertia transform and complete the manipulation since there
    // are no shared transforms for this viewport.
    IFC_RETURN(StopInertialViewport(pViewport, false /*restrictToKnownInertiaEnd*/, nullptr));
}
```

WinUI's own answer to "what if there is no compositor to run the inertia?" is *teleport to the end
and stop* — there is no UI-thread fallback that ticks the curve. That is how firmly the inertia lives
on the compositor side.

Modern: same conclusion by construction (§1.2) — `ValuesChanged` is a read-only notification;
`content.StartAnimation(m_translationExpressionAnimation)` at `ScrollPresenter.cpp:3437` is what
moves pixels.

---

## 4. The ONE place WinUI does distinguish inertia from drag — and what it is telling us

Across the whole pipeline there is exactly one behavioural branch keyed on "is this inertia or a pan",
and it is not about threads:

```cpp
// dxaml/xcp/core/core/elements/uielement.cpp:11865-11896  CUIElement::ShouldDisablePixelSnapping()
if (IsTransformOrOffsetAffectingPropertyIndependentlyAnimating())
{
    if (IsManipulatedIndependently())
    {
        IFCFAILFAST(GetContext()->GetInputServices()->GetDirectManipulationViewportStatus(this, &status));
        if (status == XcpDMViewportInertia)
        {
            // Element has inertia, so disable pixel snapping to prevent jittering.
            // In other states, like panning, enable pixel snapping, so content can be clearly rendered.
            disablePixelSnapping = true;
        }
    }
    else { disablePixelSnapping = true; }   // XAML-animated transform
}
```

consumed at `components/comptree/HWCompNodeWinRT.cpp:2722-2741`
(`visual4->put_IsPixelSnappingEnabled(hasIndependentTransformManipulation && !disablePixelSnapping)`).

**Read the comment as a statement about human perception, because that is what it is:** WinUI is
willing to quantize position to whole pixels while the finger is down (crisp text, and the offset is
finger-locked so quantization is invisible), and refuses to do so during inertia (a quantized
decelerating curve reads as stutter). That is Microsoft shipping the exact claim this investigation
needs: **during inertia the eye is measuring the *second derivative of position over time*, and during
a drag it is not.** A drag has a physical reference (the finger) that the content is compared against;
inertia has no reference, so the only thing the eye can grade is the evenness of the increments. Any
perturbation of the increments — quantization, timing jitter, latency variance — is visible in inertia
and invisible in drag.

This is the general principle behind the asymmetry. §5 identifies the *specific* perturbation in Uno.

---

## 5. Why Uno has an asymmetry WinUI structurally cannot have

### 5.1 The two paths, side by side, in Uno's own code

| | Drag | Inertia (fling) |
|---|---|---|
| Trigger | pointer event → `IDirectManipulationHandler.OnUpdated` (`ScrollContentPresenter.Managed.cs:863-870`) | `Compositor.FrameStarting` → `OnFlingFrame` (`ScrollContentPresenter.Managed.cs:615-635`) |
| Offset formula | `HorizontalOffset + deltaX`, delta from `unhandledDelta.Translation` (`:802-804`) | `_flingH.GetPosition(elapsed)` (`:624`) |
| Depends on a clock? | **No** | **Yes — entirely** |
| Runs relative to record pass | **before** it | **inside** it |

Uno's fling clock:

```csharp
// ScrollContentPresenter.Managed.cs:593
_flingStartTimestamp = compositor.TimestampInTicks;
// ScrollContentPresenter.Managed.cs:617
var elapsed = (timestampInTicks - _flingStartTimestamp) / (double)TimeSpan.TicksPerSecond;
```

and the timestamp handed to it:

```csharp
// Uno.UI.Composition/Composition/Compositor.skia.cs:226-231
if (FrameStarting is { } frameStarting)
{
    // One timestamp for the whole frame: TimestampInTicks re-reads the clock on every access, so
    // sampling per driver would give drivers in the same frame different times.
    var frameTimestamp = TimestampInTicks;
    CurrentFrameTimestampInTicks = frameTimestamp;
// Uno.UI.Composition/Composition/Compositor.cs:38
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));
```

`Stopwatch.GetTimestamp()` at the instant the UI thread entered `RenderRootVisual`. Not a vsync
timestamp, not a predicted present time, not even a smoothed frame counter. **`t_record`.**

### 5.2 Why `t_record` is not merely late — it is *unevenly* late

If `t_record` were `t_present − k` for a constant `k`, nothing would be wrong: the whole motion would
be shifted by `k` and no human would ever know. The defect is that `k` varies, and Uno's scheduler
makes it vary in a *data-dependent* way. Three compounding mechanisms, all in Uno code:

**(a) The record can be pulled forward off the paced schedule.**
`CompositionTarget.OnRenderFrameOpportunity` (`CompositionTarget.RenderScheduling.skia.cs:178-208`)
calls `Render()` **early**, ahead of the enqueued render callback, and sets `_renderedAheadOfTime`
so the next scheduled tick skips. The frame *count* stays right; the frame *timestamps* do not. Its
caller is `CoreServices.OnTick()` (`Uno.UI/UI/Xaml/Internal/CoreServices.cs:124`), which is enqueued
at `NativeDispatcherPriority.Normal` by `CoreServices.RequestAdditionalFrame()` (`:67-75`).

**(b) During a fling, the fling itself schedules those early records.**
The loop is closed inside Uno:

```
OnFlingFrame            (ScrollContentPresenter.Managed.cs:615)   ← inside the record pass
  → Set                 (:634)
  → Update → Updated    (:436)
  → InvalidateViewport  (:469)
  → EventManager.EnqueueForEffectiveViewportChanged (Uno.UI/UI/Xaml/Internal/EventManager.cs:29-35)
  → CoreServices.RequestAdditionalFrame            (EventManager.cs:34)
  → NativeDispatcher.Main.Enqueue(OnTick, Normal)  (CoreServices.cs:73)
  → CoreServices.OnTick → UpdateLayout → OnRenderFrameOpportunity → Render()   ← the *next* record,
                                                                                 at a dispatcher-chosen time
```

**(c) The dispatcher explicitly defers the render behind a variable number of Normal items.**
This is the sharpest edge:

```csharp
// Uno.UI.Dispatching/Native/NativeDispatcher.cs:206-232  TryGetRenderAction
if (details.normalItemsToProcessBeforeNextRenderAction == 0)
{
    _compositionTargets[compositionTarget] =
        (renderAction: null,
         normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
    ...
    return details.renderAction;
}
```

When a render runs, the *next* render is gated behind **however many Normal-priority items happened to
be queued at that instant**, decremented one per Normal item in `DispatchItems`
(`NativeDispatcher.cs:155-165`). During a fling the Normal queue depth is exactly what virtualization,
`EffectiveViewportChanged` handlers, bindings and container materialization make it — it changes frame
to frame. So the gap between successive `FrameStarting` timestamps is a function of *app workload*,
not of the display.

**(d) The presents, meanwhile, are paced.** On Android the render thread does
`RenderFrame(); _pacer.WaitForNextFrame();` (`Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:153-158`)
and the pacer blocks on Choreographer. On Win32 it blocks on `DwmFlush`
(`Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:62`). **This is the worst combination:
sample times jittery, display times uniform.** Content position advances by unequal increments on a
perfectly regular display cadence — the textbook signature of micro-stutter at a solid 60/120 fps,
which is exactly what "smooth frame rate but doesn't feel smooth" means.

### 5.3 Why drag does not care

Every one of (a)–(d) applies to drag too. Drag is unaffected because a drag frame renders
`HorizontalOffset + deltaX` — a value that was already written by the pointer handler *before* the
record began, and that is a function of the finger, not of `t_record`. Recording that value early or
late does not change it. And even if the finger sample it came from is slightly stale, the eye grades
drag by *content-vs-finger agreement*, which is preserved. Inertia has no such anchor: its value is
recomputed from the clock inside the record, so `Δposition_displayed = v · Δt_record`, and
`Δt_record ≠ refresh period`.

Magnitude: at a typical post-flick velocity of 2000 px/s, ±2 ms of record-time jitter is ±4 px of
position error per frame, and — because an early record is necessarily followed by a late one — the
error alternates sign. A ±4 px alternating perturbation on a ~33 px/frame advance is a ~12% velocity
ripple at half the frame rate. That is comfortably visible.

### 5.4 What Uno throws away that it already has

```csharp
// Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:97-100
private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
{
    public void DoFrame(long frameTimeNanos) => onFrame();
}
```

`frameTimeNanos` **is** the vsync timestamp — Android's exact equivalent of what DComp hands DManip
through `GetNextFrameInfo`. Uno discards it. On Win32, `DwmGetCompositionTimingInfo` exposes
`qpcVBlank` and `qpcRefreshPeriod`; Uno calls only `DwmFlush` and reads no timing.

---

## 6. Q4 — The smallest change that buys the same property

Ranked by (effect / risk). **Nothing here moves scrolling to a compositor thread.** The goal is
narrow: make the fling's *time argument* uniform and presentation-anchored, leaving everything else
alone. Drag is untouched by all of these, by construction — drag never reads the clock.

### Fix 1 (minimum viable, platform-free, ~15 lines) — snap the frame clock to a vsync grid

Change `Compositor.RenderRootVisual` (`Uno.UI.Composition/Composition/Compositor.skia.cs:226-231`) so
that `CurrentFrameTimestampInTicks` is not `Stopwatch.GetTimestamp()` but a *monotone, quantized
frame clock*:

```
raw      = TimestampInTicks
period   = refresh period in ticks (from FeatureConfiguration / display refresh)
steps    = clamp(round((raw - previous) / period), 1, MaxCatchUpFrames)
frameTs  = previous + steps * period
if (raw - frameTs > ResyncThreshold) frameTs = raw   // hard resync after a stall
previous = frameTs
```

This is the cheapest possible emulation of "a compositor clock": presents are already on the vsync
grid, so putting the *sample* times on the same grid removes the entire error term without needing a
platform timestamp. It affects only clock-driven frame drivers (fling, wheel decay) — everything else
in the pipeline ignores `CurrentFrameTimestampInTicks`. It is unit-testable with a synthetic sequence
of jittery raw timestamps.

**Note:** the anti-jitter must live in the *compositor's* frame clock, not inside
`ScrollFlingSimulation`, so the wheel decay (which has the identical exposure) is fixed by the same
change and any future frame driver inherits it.

### Fix 2 (the true WinUI equivalent, still small) — feed the real vsync timestamp and add one period

Uno's `FrameStarting` already exists as the correct injection point; only the *value* is wrong.

* **Android**: capture `frameTimeNanos` in `ChoreographerFramePacer.FrameCallback.DoFrame`
  (`ChoreographerFramePacer.cs:99`), publish it, and have the compositor use
  `lastVsync + presentLatency` as the frame timestamp. Because the loop is
  `RenderFrame(); WaitForNextFrame();` (`UnoSKVulkanView.cs:153-158`), the frame being recorded is
  presented at the *next* vsync at the earliest, so `presentLatency = 1 × refreshPeriod` is the
  correct first approximation.
* **Win32**: `DwmGetCompositionTimingInfo` → `qpcVBlank` + `qpcRefreshPeriod`, same arithmetic, next
  to the existing `DwmFlush` call (`Win32RenderPacer.cs:62`).
* WinUI's own scaffolding used a **hardcoded 16 ms** for this
  (`CompositorDirectManipulationViewport.cpp:50-65`) and shipped like that for years — so a constant
  one-frame offset is an acceptable v1; the *uniformity* is what matters, not the exactness of the
  latency estimate.

Fix 2 subsumes Fix 1. Fix 1 is worth doing first because it needs no platform work and isolates the
hypothesis.

### Fix 3 (removes the jitter *source* rather than filtering it) — do not let the fling drive its own record schedule

Keep the fling's re-arm on the paced path only. Concretely: while a `FrameStarting` driver is active,
suppress the `OnRenderFrameOpportunity` early-record path
(`CompositionTarget.RenderScheduling.skia.cs:178-208`), so records happen only on the vsync-driven
`EnqueueRenderCallback` schedule. WinUI's equivalent is that DManip's `RequestAdditionalFrame`
(`InputServices.cpp:7317-7323`) requests a *bookkeeping* tick and can never pull the animation's
sample time forward, because the animation is not sampled on that tick at all.

This is riskier than 1 and 2 (it touches the render state machine and the ahead-of-time optimisation
exists for a reason), so it should follow, not lead.

### Fix 4 (cheap, independent, directly WinUI-grounded) — do not quantize position during inertia

Adopt `CUIElement::ShouldDisablePixelSnapping()`'s policy (§4) as a stated invariant: **no rounding
of the scroll offset or of `Visual.AnchorPoint` while an inertia driver is running.** The 2 px drag
quantization is already gone, but the invariant should be written down and asserted, because §4 shows
Microsoft found the same perceptual asymmetry from the other direction: whatever you may quantize
during a pan, you may not quantize during a fling.

### Explicitly NOT recommended

Re-fitting the launch velocity, retuning the deceleration constants, or adding smoothing to
`ScrollFlingSimulation`. The curve is already the right curve; the complaint is about the *sampling of
time*, and smoothing the curve would only trade stutter for sponginess.

---

## 7. Smallest proof that discriminates this hypothesis

One instrumented fling, no code changes to the fling itself:

1. In `Compositor.RenderRootVisual` (`Compositor.skia.cs:230`), record `frameTimestamp` into a ring
   buffer along with a flag for which entry point called `Render()` — `EnqueueRenderCallback`
   (`CompositionTarget.RenderScheduling.skia.cs:152`) vs `OnRenderFrameOpportunity` (`:205`).
2. Perform (a) a slow steady drag and (b) a flick, and dump the histogram of
   `Δ = frameTimestamp[n] − frameTimestamp[n-1]` for each.
3. **Prediction if this hypothesis is right:** during inertia, `Δ` has a clearly bimodal or
   high-variance distribution around the refresh period (σ of several ms), with the short intervals
   correlating with the `OnRenderFrameOpportunity` flag; presents (already paced) stay uniform.
   Compute `v · σ(Δ)` and check it lands in the several-px range.
4. **Refutation:** if `σ(Δ)` during inertia is well under ~0.5 ms and the entry point is uniformly
   `EnqueueRenderCallback`, this hypothesis is wrong and the remaining suspects are present-latency
   variance downstream of the record (Fix 2 would still help) or something in the paint walk.

A second, even cheaper discriminator: apply **Fix 1 only** and re-run the product owner's flick. Fix 1
changes nothing except the fling/wheel time argument, so a subjective improvement isolates the
mechanism to record-time jitter with no other variable moved.

---

## 8. Condensed answers

1. **Per frame during inertia, WinUI runs:** the manipulation transform advance inside
   DManip/DComp (legacy) or `InteractionTracker.Position` + its bound `ExpressionAnimation` (modern),
   **on the compositor thread, against the composition clock**; plus one cheap UI-thread bookkeeping
   poll (`ProcessDirectManipulationViewportValuesUpdate`, `InputServices.cpp:8604-8790`) or one
   UI-thread notification (`ScrollPresenter::ValuesChanged`, `ScrollPresenter.cpp:1172-1239`) that
   updates the *model* and never the picture.
2. **Yes.** The viewport is created with a frame-info provider that is the DComp compositor
   (`DirectManipulationService.cpp:4214-4230`, `:4298`), whose `GetNextFrameInfo` supplies "the lapse
   of time … until the resulting transform is shown on screen" (`DirectManipulationService.h:283-286`).
   The curve is evaluated at `t_present`.
3. **Yes.** No per-frame UI-thread involvement in either stack; the negative proof is
   `StopInertialViewportWithoutCompositorPeer` (`InputServices.cpp:7373-7399`), which *cancels the
   inertia outright* when no compositor peer exists rather than falling back to a UI-thread tick.
4. **Smallest adoptable change:** make `Compositor.CurrentFrameTimestampInTicks` a uniform,
   presentation-anchored frame clock instead of `Stopwatch.GetTimestamp()` at record entry —
   quantized to the vsync grid (Fix 1), then sourced from Choreographer `frameTimeNanos` /
   `DwmGetCompositionTimingInfo` plus one refresh period (Fix 2). Roughly 15 lines in
   `Compositor.skia.cs:226-231`, no threading change, no change to drag.

---

## 9. UNVERIFIED

* The internals of `Microsoft.DirectManipulation.dll` — how `pCompositionTime` from
  `GetNextFrameInfo` is consumed, and the exact inertia curve DManip integrates. Only the XAML-side
  contract and the documented semantics of the parameter are readable here.
* The exact presentation-time offset the Windows.UI.Composition animation engine applies when
  evaluating `InteractionTracker` inertia; `InteractionTracker` is not in this repo.
* No measurement was taken in this pass. The magnitude estimate in §5.3 (±2 ms → ±4 px at 2000 px/s)
  is arithmetic on plausible numbers, not observed data — §7 step 3 is the measurement that would
  replace it.
* The claimed frequency of `OnRenderFrameOpportunity`-driven records *during a fling specifically* is
  inferred from the call graph in §5.2(b), not observed. §7 step 1-2 settles it.
* Whether the WASM Skia host exhibits the same record-time jitter — only the Android and Win32 hosts
  were read.
