# 04 — The RedirectVisual control case, and the exhaustive fling∖RedirectVisual difference list

Scope: establish *exactly* what the RedirectVisual sample does per frame, from source, then derive
the complete set of things a fling frame does that a RedirectVisual frame does not. Everything is
cited `file:line` from the worktree at `dev/mazi/smooth-scroll`. Anything not established from code
is marked **UNVERIFIED**.

---

## 0. Verdict up front

1. **The sample is not driven by a Composition animation.** There is no `KeyFrameAnimation`, no
   `ExpressionAnimation`, no timer, no `CompositionTarget.Rendering` subscriber. It is driven by a
   **self-invalidating paint**: the Skottie Lottie renderer calls `Invalidate()` on its own visual
   from *inside its own `Paint`*, and its time source is a `Stopwatch`, not the compositor frame
   clock. `Compositor._runningAnimations` is **empty** and `Compositor.FrameStarting` is **null** for
   the whole page. This corrects `02-scheduling.md` §4 ("a `RedirectVisual` + composition
   animations") and `03-layout.md` §0 ("an animation is running (RedirectVisual)").

2. That correction **strengthens** the leading scheduling hypothesis rather than weakening it. The
   sample *does* issue a `RequestNewFrame()` from inside the record (via
   `Compositor.InvalidateRenderPartial`, `Compositor.skia.cs:378-383`) — exactly the shape a fling
   has. So "requests a frame from inside the record" is **not** the discriminator; it is shared by
   the fling and the control. The only thing the control lacks is the *other* precondition:
   it never enqueues a Normal-priority dispatcher item, therefore `CoreServices.OnTick` never runs,
   therefore `CompositionTarget.OnRenderFrameOpportunity` never runs, therefore
   `_renderedAheadOfTime` is permanently `false`.

3. **The difference list has exactly two entries that survive the three-way filter** (present in the
   fling, absent from *both* the drag and RedirectVisual). Everything else in the list is either
   shared with the drag (so it cannot explain observation 1) or shared with RedirectVisual (so it
   cannot explain observation 3). They are §5.2 **F1** and **F2**, and they are two faces of the
   same line of code.

---

## 1. The sample: what it is

| File | Content |
|---|---|
| `src/SamplesApp/SamplesApp.Samples/Windows_UI_Composition/RedirectVisualTests.xaml` | 2 source/target pairs |
| `src/SamplesApp/SamplesApp.Samples/Windows_UI_Composition/RedirectVisualTests.xaml.cs` | `[Sample("Microsoft.UI.Composition", Name = "RedirectVisual", IsManualTest = true, IgnoreInSnapshotTests = true)]`, `:22-26` |
| `src/Uno.UI.Composition/Composition/RedirectVisual.skia.cs` | the whole Skia implementation, 25 lines |

Tree (`RedirectVisualTests.xaml`):

- Pair 1 — a static `Image` (`x:Name="img"`, `:40-45`, `ms-appx:///Assets/test_image_125_125.png`)
  redirected into an empty `Canvas` (`x:Name="canvas"`, `:46-49`).
- Pair 2 — an `AnimatedVisualPlayer` (`x:Name="player"`, `:55-61`, **`AutoPlay="True"`**) whose
  content is `<lottie:LottieVisualSource UriSource="ms-appx:///Assets/Animations/squirrel.json" />`
  (`:60`), redirected into a second empty `Canvas` (`x:Name="canvas2"`, `:62-65`).

Wiring, `RedirectVisualTests.xaml.cs:35-47` — done **once**, in `Loaded`:

```csharp
var redirectVisual  = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(img));
ElementCompositionPreview.SetElementChildVisual(canvas, redirectVisual);
redirectVisual.Size = new(100, 100);                                     // :38-41
var redirectVisual2 = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(player));
ElementCompositionPreview.SetElementChildVisual(canvas2, redirectVisual2);
redirectVisual2.Size = new(200, 200);                                    // :43-46
```

No code runs on this page after `Loaded` returns. **The only moving thing on the page is the
squirrel Lottie.**

---

## 2. What actually animates, and through which mechanism

### 2.1 It is *not* the WinUI composition-animation path

`AnimatedVisualPlayer` has two flows. The WinUI flow drives a `ScalarKeyFrameAnimation` on a
`CompositionPropertySet` (`AnimatedVisualPlayer.mux.cs:765-803`). It is selected only if the source
returns an `IAnimatedVisual`:

```csharp
try { animatedVisual = source.TryCreateAnimatedVisual(m_rootVisual!.Compositor, out diagnostics); }
catch { animatedVisual = null; }                                  // AnimatedVisualPlayer.mux.cs:299-307
…
if (animatedVisual is null) { m_useWinUIFlow = false; return; }   // AnimatedVisualPlayer.mux.cs:312-318
```

`LottieVisualSource`'s implementation is a stub that **throws**:

```csharp
[NotImplemented]
public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
    => throw new NotImplementedException();      // LottieVisualSourceBase.cs:74-79
```

So the player takes the legacy path (`AnimatedVisualPlayer.legacy.cs:1-10` states this explicitly and
names this exact case: *"CommunityToolkit.WinUI.Lottie's LottieVisualSource on Skia, which uses a
custom SKCanvasElement"*).

**Consequence: `Compositor.RegisterAnimation` (`Compositor.skia.cs:45-94`) is never called for this
page. `_runningAnimations` and `_runningTargets` (`Compositor.skia.cs:20-21`) stay empty.**

### 2.2 It is a self-invalidating paint on a `Stopwatch` clock

`HAS_SKOTTIE` is defined for the Skia flavour (`src/AddIns/Uno.UI.Lottie/Uno.UI.Lottie.Skia.csproj:18`),
so `LottieVisualSource.Skottie.cs` is the live implementation. On `__SKIA__` it builds an
`SKCanvasElement` subclass as the render surface:

```csharp
_skCanvasElement = new LottieSKCanvasElement(this);   // LottieVisualSource.Skottie.cs:222-224
…
protected override void RenderOverride(SKCanvas canvas, Size area) => owner.OnRenderOverride(canvas, area);
                                                      // LottieVisualSource.Skottie.cs:509-514
```

`Play` starts a `Stopwatch` and kicks **one** paint; there is no timer on Skia:

```csharp
#if __SKIA__
    // Kick the first paint. Subsequent paints schedule themselves from Render().
    Invalidate();
#else
    _timer = DispatcherQueue…CreateTimer(); _timer.Tick += … ; _timer.Start();
#endif
    _stopwatch.Restart();                             // LottieVisualSource.Skottie.cs:398-408
```

and `Render` — which runs **inside the paint walk** — re-arms itself at its own tail:

```csharp
var frameTime = GetFrameTime();                       // :318  → _stopwatch.Elapsed, :355-382
animation.SeekFrameTime(frameTime, _invalidationController);   // :326
animation.Render(canvas, …);                          // :337
…
#if __SKIA__
    if (_stopwatch.IsRunning) { _skCanvasElement?.Invalidate(); }   // :346-351  ← THE FRAME DRIVER
#endif
```

The chain from there:

```
SKCanvasElement.Invalidate()                      AddIns/Uno.WinUI.Graphics2DSK/SKCanvasElement.cs:56
 └ SKCanvasVisual.Invalidate()                    Uno.UI/Graphics/SKCanvasVisual.skia.cs:24
    └ Compositor.InvalidateRender(visual)         Uno.UI.Composition/Composition/Compositor.cs:251
       └ InvalidateRenderPartial(visual)          Uno.UI.Composition/Composition/Compositor.skia.cs:378-383
          ├ visual.SetMatrixDirty()
          ├ visual.InvalidatePaint()
          └ visual.CompositionTarget?.RequestNewFrame()   ← the next frame is requested HERE
```

> **The RedirectVisual page requests its next frame from *inside* the record**, just like a fling
> does. Its time source, however, is `_stopwatch` (`LottieVisualSource.Skottie.cs:78, 362`), read
> during paint — not `Compositor.CurrentFrameTimestampInTicks`.

### 2.3 Why the Lottie is painted twice per frame

`RedirectVisual.skia.cs` in full:

```csharp
internal override SKPath? Paint(in PaintingSession session)
{
    base.Paint(in session);
    if (Source is not null && session.Canvas is { } canvas) { Source.RenderRootVisual(canvas, null); }
    return null;                                              // :10-20
}
internal override bool CanPaint() => Source?.CanPaint() ?? false;   // :22
internal override bool RequiresRepaintOnEveryFrame => true;         // :23
```

Two effects, both per frame:

1. `RequiresRepaintOnEveryFrame => true` selects the uncached branch of `PaintStep`
   (`Visual.skia.cs:478-484`): the visual's picture cache is bypassed, its parent's children-picture
   is invalidated (`InvalidateParentChildrenPicture(includeSelf: false)`), and it contributes damage
   with `contentChanged: true` — i.e. its full bounds are damaged **every frame, unconditionally**.
2. `Paint` re-walks the *source* subtree (`Source.RenderRootVisual(canvas, null)`), so the
   `AnimatedVisualPlayer` subtree — including the `SKCanvasVisual` — is painted a **second** time in
   the same record. `LottieVisualSourceBase.Render` therefore runs twice per frame, and calls
   `Invalidate()` twice (idempotent: `RequestNewFrame` collapses, `RenderScheduling.skia.cs:93-101`).

---

## 3. Anatomy of one RedirectVisual frame, end to end

Android/Vulkan present loop is *invalidate-driven, vsync-paced* (`UnoSKVulkanView.cs:146-162`;
`ChoreographerFramePacer.cs:80-102`) — a `Draw` happens iff someone called `InvalidateRender()`.

| # | Thread | Step | Citation |
|---|---|---|---|
| 1 | render | `_renderEvent` set, `_renderRequested` true → `RenderFrame()` → `OnNativePlatformFrameRequested` | `UnoSKVulkanView.cs:149-156, 215` |
| 2 | render | `NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)`, then `Draw(...)` | `RenderScheduling.skia.cs:170-175` |
| 3 | render | `Draw` borrows `_lastRenderedFrame`, `_fpsHelper.OnFramePresentRequested()`, clips to damage, blits | `Rendering.skia.cs:233-241, 285-299` |
| 4 | render | `_pacer.WaitForNextFrame()` — blocks to the next vsync | `UnoSKVulkanView.cs:158-161` |
| 5 | UI | `DispatchItems` → `TryGetRenderAction`. **Normal queue depth is 0**, so `normalItemsToProcessBeforeNextRenderAction` is 0 and the render action is taken on the *first* pump | `NativeDispatcher.cs:128-130, 206-234` |
| 6 | UI | `EnqueueRenderCallback`: `_renderedAheadOfTime == false`, `RenderRequested == true` → **State C** → `Render()` | `RenderScheduling.skia.cs:145-153` |
| 7 | UI | `Render` → `RecordPictureAndReturnPath` → `Compositor.RenderRootVisual` | `Rendering.skia.cs:119-124`; `SkiaRenderHelper.skia.cs:36-53` |
| 7a | UI | `FrameStarting` is **null** → no driver callout, `CurrentFrameTimestampInTicks` not even updated | `Compositor.skia.cs:307-324` |
| 7b | UI | `_runningAnimations` is **empty** → the `RaiseAnimationFrame` loop body never executes | `Compositor.skia.cs:326-342` |
| 7c | UI | paint walk; RedirectVisual repaints uncached and re-walks its source; Skottie seeks by `_stopwatch` and renders | `Visual.skia.cs:478-484`; `RedirectVisual.skia.cs:14-17`; `LottieVisualSource.Skottie.cs:302-353` |
| 7d | UI | Lottie tail: `_skCanvasElement?.Invalidate()` → `RequestNewFrame()`. `_ahead == false`, `RenderRequested == false` → **clean branch**: `RenderRequested = true` **+ speculative `host.InvalidateRender()`** | `LottieVisualSource.Skottie.cs:346-351`; `RenderScheduling.skia.cs:93-97, 106-113` |
| 7e | UI | `_backgroundTransitions` empty **and** `_runningAnimations.Count == 0` **and** `FrameStarting is null` ⇒ the end-of-record re-arm at `Compositor.skia.cs:372-375` **does not fire** | `Compositor.skia.cs:358-375` |
| 8 | UI | `_lastRenderedFrame` published; `OnFrameRecorded()` bumps the generation | `Rendering.skia.cs:147, 157` |
| 9 | UI | `_isRenderingActive` false (no `CompositionTarget.Rendering` subscriber) → no extra `RequestNewFrame`, no High-priority `RaiseRendering` item | `Rendering.skia.cs:164-167, 445-449` |
| 10 | UI | `host.InvalidateRender()` — **backed** by the picture published at step 8 | `Rendering.skia.cs:169-172` |
| 11 | render | pacer releases at the next vsync → step 1. `OnFramePresentRequested` sees `current > lastPresented` ⇒ **not a drop** | `SkiaRenderHelper.skia.cs:299-323` |

Steady state: **exactly one `Render()` per `Draw`, one dispatcher pump per frame, zero Normal items,
zero layout.** That is the control.

---

## 4. What a RedirectVisual frame provably does *not* do

| Not done | Why, from source |
|---|---|
| No measure/arrange, ever | Nothing calls `InvalidateMeasure`/`InvalidateArrange` after `Loaded`. `SKCanvasElement.Invalidate` goes to the *visual*, not to layout (`SKCanvasElement.cs:56` → `SKCanvasVisual.skia.cs:24`). |
| No `CoreServices.RequestAdditionalFrame()` | Its only callers are `XamlRoot.InvalidateMeasure`/`InvalidateArrange` (`XamlRoot.crossruntime.cs:18,26`), `EventManager.EnqueueForEffectiveViewportChanged` (`EventManager.cs:34`), `EventManager.RequestRaiseLoadedEventOnNextTick` (`EventManager.cs:69`). None fire per frame here. |
| ⇒ No `CoreServices.OnTick` | It is the Normal item enqueued by `RequestAdditionalFrame` (`CoreServices.cs:67-75`). |
| ⇒ **No `CompositionTarget.OnRenderFrameOpportunity`** | Its only Android caller is `CoreServices.cs:124`. ⇒ `_renderedAheadOfTime` is permanently `false` ⇒ `EnqueueRenderCallback` States A and B (`RenderScheduling.skia.cs:131-144`) are **unreachable**. |
| No Normal-priority dispatcher items at all | ⇒ `normalItemsToProcessBeforeNextRenderAction` is re-seeded to 0 on every handover (`NativeDispatcher.cs:216`) ⇒ the render action is never withheld (`NativeDispatcher.cs:214`). |
| No effective-viewport propagation | Nothing on the page subscribes `EffectiveViewportChanged`; `IsEffectiveViewportEnabled` is false so `PropagateEffectiveViewportChange` early-returns (`FrameworkElement.EffectiveViewport.cs:84, 349-353`). |
| No visual-tree mutation | ⇒ `ContainerVisual.skia.cs:46`'s `RequestNewFrame` on children-collection change never fires; no `Loaded` events per frame. |
| No `_runningAnimations` traffic | §2.1. |
| No `Compositor.IsAnimating` | Both terms false (`Compositor.skia.cs:43`). |
| No pointer input | The page is idle under the finger. |

### Caveats on the control case (**UNVERIFIED** on device)

- The whole frame chain depends on the squirrel Lottie actually loading. If Skottie failed to parse
  the asset, `_stopwatch.IsRunning` is false, `Invalidate()` is never re-armed, and the page would go
  fully static — `FpsHelper` would show *Idle*, not 120 FPS. **The reported "120 FPS, no drops"
  therefore implies the Lottie is running**, but that inference should be confirmed by eye.
- If the SamplesApp shell has a `CompositionTarget.Rendering` subscriber (or a diagnostics overlay
  that adds one), `_isRenderingActive` flips and `Render:164-167` calls `RequestNewFrame()` at the
  end of every record, plus a High-priority `RaiseRendering` item per frame
  (`Rendering.skia.cs:445-449`). High priority does **not** feed
  `normalItemsToProcessBeforeNextRenderAction`, so the control's key property survives, but the flag
  traffic changes. Not verified for the product owner's build.
- `ScrollDiagnostics.IsEnabled` adds a `CompositionTarget.Rendering` subscriber *on every loaded
  `ScrollContentPresenter`* (`ScrollContentPresenter.Managed.cs:164-172`). If it were on during the
  fling measurement but not on the RedirectVisual page, that alone is an asymmetry in
  `_isRenderingActive` — see D-R3 below. **Must be checked before trusting the comparison.**

---

## 5. THE DIFFERENCE LIST

Everything a fling frame does that a RedirectVisual frame does not. Split by whether the drag does it
too, because an item the drag shares cannot explain observation 1 (drag ≈ 0 drops).

### 5.1 Bucket C — in the fling, **also in the drag**, not in RedirectVisual
*(≈ the whole scroll machine. None of these can be the discriminator on its own.)*

| # | What | Citation |
|---|---|---|
| C1 | `Set(...)` runs: clamping against `ScrollableWidth/Height`, `_touchInertia`/`StopWheelDecay` bookkeeping | `ScrollContentPresenter.Managed.cs:311-429` |
| C2 | `Update(...)` writes `visual.AnchorPoint` (and `Scale`) → `Visual.OnPropertyChangedCore` → `Compositor.InvalidateRender` → `RequestNewFrame` | `SCP.Managed.cs:485-527`; `Visual.cs:192-194` |
| C3 | `Updated` → `UpdateOffsets` → `Scroller.OnPresenterScrolled(...)` | `SCP.Managed.cs:434-468` |
| C4 | `ScrollViewer.OnPresenterScrolled` → `RequestUpdate()` → **one Normal-priority dispatcher item per frame** (`Dispatcher.RunAsync(Normal)`, coalesced by `_hasPendingUpdate`) | `ScrollViewer.cs:1239-1243, 1301-1316` |
| C5 | The deferred `ScrollViewer.Update(isIntermediate:true)` writes the `HorizontalOffset`/`VerticalOffset` DPs → `ViewChanged` → `VirtualizingPanelLayout.OnScrollChanged` (fill/unfill/realize/arrange loop) | `ScrollViewer.cs:1318-1356`; see `03-layout.md` §1.4 |
| C6 | `ScrollBar` `Value` template-binding → `UpdateTrackLayout` | see `03-layout.md` §1.5 |
| C7 | `ScrollOffsets = new Point(...)` then `InvalidateViewport()` → `PropagateEffectiveViewportChange` | `SCP.Managed.cs:466-467`; `FrameworkElement.EffectiveViewport.cs:256-266` |
| C8 | Container realization on line-boundary crossing → `OnChildAdded` → `EventManager.RequestRaiseLoadedEventOnNextTick()` → **`CoreServices.RequestAdditionalFrame()` → a Normal `OnTick` → `root.UpdateLayout()` → `OnRenderFrameOpportunity()`** | `UIElement.crossruntime.cs:108-114`; `EventManager.cs:66-70`; `CoreServices.cs:67-75, 108-124` |
| C9 | Damage covers the whole scroll viewport each frame instead of two ≤200×200 boxes | `Rendering.skia.cs:139-147, 285-299` |
| C10 | Paint walk covers the realized ListView subtree (with per-visual picture caching) rather than two tiny uncached subtrees | `Visual.skia.cs:471-511` |
| C11 | `normalItemsToProcessBeforeNextRenderAction` is non-zero, so the render action is withheld behind ≥1 Normal item on each handover | `NativeDispatcher.cs:206-234` |
| C12 | The record is frequently performed by `OnRenderFrameOpportunity` (tail of `OnTick`) instead of by the render action, setting `_renderedAheadOfTime = true` | `CoreServices.cs:124`; `RenderScheduling.skia.cs:178-208` |

> **C7/C8 note.** `03-layout.md` §1.2 establishes that for a *plain* `ListView`, `InvalidateViewport`
> early-returns (`IsEffectiveViewportEnabled == false`) so C7 does **not** reach
> `RequestAdditionalFrame`. C8 then becomes the only route to `OnTick` during a fling, and it fires
> only on line-boundary crossings — i.e. **`_renderedAheadOfTime` is entered intermittently, at a rate
> that grows with fling velocity.** This matters: `02-scheduling.md` assumed C7 supplied a per-frame
> `OnTick`. Whichever of C7/C8 is live on the product owner's page, both are **shared with the drag**,
> so the bucket assignment is unaffected — but the *rate* is, and it is the rate that sets the drop
> count. Resolving which one fires is worth one line of on-device logging (E-3).

### 5.2 Bucket F — in the fling, in **neither** the drag **nor** RedirectVisual
*(the complete candidate set for the defect)*

| # | What | Citation |
|---|---|---|
| **F1** | **`Compositor.FrameStarting` has a subscriber for the whole fling.** The offset driver runs *inside* the record, before the paint walk, against `CurrentFrameTimestampInTicks`. A drag writes the offset from the pointer handler before the record; RedirectVisual has no `FrameStarting` subscriber at all. | `SCP.Managed.cs:586-615` (`StartFling`/`StopFling`), `:617-644` (`OnFlingFrame`); `Compositor.skia.cs:209, 307-324` |
| **F2** | **`Compositor.RenderRootVisual` re-arms a frame at the end of *every* record**, because `FrameStarting is not null`. This fires unconditionally, even on a frame where the offset did not move. A drag never satisfies the condition (no `FrameStarting`, `_runningAnimations` empty, `IsTouch` branch *stops* animations at `SCP.Managed.cs:521-523`); RedirectVisual never satisfies it either (§3 step 7e). | `Compositor.skia.cs:372-375` |
| F3 | `Compositor.IsAnimating` is true, and `CurrentFrameTimestampInTicks` / `GetFrameTimestamp` / the median frame-clock machinery run each frame | `Compositor.skia.cs:43, 244-298, 311-312` |
| F4 | `ScrollDiagnostics.CurrentPhase == PhaseInertia` and, *if* `ScrollDiagnostics.IsEnabled`, one `ScrollDiagnostics.Record` per frame from a `CompositionTarget.Rendering` handler — which also sets `_isRenderingActive` | `SCP.Managed.cs:594, 164-190` |

F3 is bookkeeping with no scheduling effect. F4 is a measurement confound, not a mechanism (but see
§4 caveat 3 and E-4). **F1 and F2 are the same subscription seen from two ends, and they are the only
mechanistic candidates.**

Crucially, F2 is what makes `_renderRequestedAfterAheadOfTimePaint` set *with certainty* whenever
`_renderedAheadOfTime` is true — which, combined with C12, is precisely the `EnqueueRenderCallback`
**State A** path (`RenderScheduling.skia.cs:131-139`) analysed in `02-scheduling.md` §2:

```
_ahead && _rRAAOTP  →  clear both, RequestNewFrame() → speculative host.InvalidateRender()
                    →  RETURN WITHOUT RENDERING
```

i.e. an `InvalidateRender` with no picture behind it, one full present cycle before the picture can
exist. **Fling: guaranteed on every ahead-of-time record. Drag: never, because F2 is absent —
`EnqueueRenderCallback` takes harmless State B instead. RedirectVisual: unreachable, because C12 is
absent.**

### 5.3 Bucket R — in **RedirectVisual**, not in either scroll case
*(confounds; things that make the control "easier" for reasons unrelated to F1/F2)*

| # | What | Citation |
|---|---|---|
| R1 | Zero Normal-priority dispatcher items ⇒ the render action is taken on the first pump, every frame | `NativeDispatcher.cs:214-216` |
| R2 | The record cost is tiny and constant: two ≤200×200 uncached subtrees, damage ≤ the two redirect boxes | `RedirectVisual.skia.cs:23`; `Visual.skia.cs:478-484` |
| R3 | No `CompositionTarget.Rendering` subscriber ⇒ `_isRenderingActive == false` ⇒ `Render:164-167` does **not** call `RequestNewFrame` | `Rendering.skia.cs:84-108, 164-167` |
| R4 | The animation clock is a `Stopwatch` read during paint, so a late frame silently *skips* Lottie time rather than producing a duplicate position. A duplicated present is far less visible than in a scroll. | `LottieVisualSource.Skottie.cs:78, 355-382` |

R4 is a **perceptual** confound worth stating plainly: even if the RedirectVisual page *did* drop a
present, a Lottie squirrel repeating one frame is nearly invisible, whereas a scroll repeating one
step is exactly the artefact being hunted. The reported "no drops" is a counter reading, not a
judgement — but it is the counter that matters here, so this does not weaken the control.

---

## 6. Three-way prediction table per candidate

| Hypothesis | Drag | Inertia | RedirectVisual | Fits all three? |
|---|---|---|---|---|
| **H-F2/A — F2 sets `_rRAAOTP` on every ahead-of-time record; `EnqueueRenderCallback` State A then burns a present cycle** (`Compositor.skia.cs:372-375` × `RenderScheduling.skia.cs:131-139`) | F2 absent ⇒ State B, harmless ⇒ **0** | F2 + C12 both present ⇒ **drops, ~1 per ahead-of-time record** | C12 absent (no `OnTick`) ⇒ State A unreachable ⇒ **0** | **YES** |
| H-C11 — Normal items withhold the render action past its vsync (the brief's leading hypothesis) | C4/C8 identical to the fling ⇒ **drops** ✘ | drops ✔ | no Normal items ⇒ 0 ✔ | **NO** — over-predicts drag |
| H-record-cost — the fling's record/paint/layout is simply heavier | C1-C12 identical for a drag ⇒ **drops** ✘ | drops ✔ | trivial record ⇒ 0 ✔ | **NO** — over-predicts drag |
| H-damage — bigger damage region per frame | identical for a drag ⇒ **drops** ✘ | drops ✔ | 0 ✔ | **NO** |
| H-in-record-request — "the next frame is requested from inside the record" | pointer writes before the record ⇒ 0 ✔ | drops ✔ | **RedirectVisual also requests from inside the record** (§2.2) ⇒ should drop ✘ | **NO** — over-predicts RedirectVisual. This is the sharpest thing this document falsifies. |
| H-animation-driven — "a Composition animation never touches layout, so it records every vsync" | n/a | n/a | **there is no Composition animation on this page** (§2.1) | **Premise false** |
| H-Android-sync-barrier — `Handler.post` (`NativeDispatcher.Android.cs:39-43`) messages blocked behind a `ViewRootImpl` traversal barrier | traversals are scheduled *by touch* ⇒ drag should be **worse** ✘ | fling has no touch ⇒ should be **better** ✘ | 0 ✔ | **NO** — sign is inverted. **UNVERIFIED** but dismissible on sign alone. |

---

## 7. Corrections this document makes to the sibling notes

| Note | Claim | Correction |
|---|---|---|
| `02-scheduling.md` §4, RedirectVisual row | "A `RedirectVisual` + composition animations…" and "P3 alone does not separate fling from RedirectVisual — `Compositor.skia.cs:372` fires for a running composition animation too" | There is **no** composition animation. `Compositor.skia.cs:372` does **not** fire on this page. The in-record `RequestNewFrame` comes from `InvalidateRenderPartial` via the Lottie's self-invalidate instead. The conclusion (P1 is the discriminator) is unchanged and in fact cleaner: P3 is satisfied by all three of drag-adjacent, fling and RedirectVisual paths in different ways, so P1 carries the whole weight. |
| `03-layout.md` §0 | "an animation is running (RedirectVisual)" | Same correction. `Compositor.IsAnimating` is **false** for the RedirectVisual page. |
| Task brief | "A Composition animation (RedirectVisual) never touches layout or the viewport queue, so it records every vsync." | The *conclusion* is right; the *premise* is wrong. It is not a Composition animation. The reason it records every vsync is R1 + the absence of C12, not the animation subsystem. |

---

## 8. Cheapest decisive experiments

**E-1 (control-case sanity, 30 s, no code).** On the RedirectVisual page, confirm the squirrel is
actually animating while the counter reads 120 FPS. If it is static, the "control" is an idle page
and observation 3 carries no information at all. *This is the single cheapest thing to do and it
gates everything else in this document.*

**E-2 (make the control produce Normal items — the direct test of R1/C12).** Add a sibling sample:
the same two `RedirectVisual` pairs, plus a `DispatcherQueueTimer` (or a `CompositionTarget.Rendering`
handler) that calls `XamlRoot.InvalidateArrange()` once per frame — i.e. import C12 *without*
importing F1/F2. **H-F2/A predicts: still 0 drops** (State A needs both `_ahead` *and* `_rRAAOTP`; F2
is still absent, so `EnqueueRenderCallback` takes State B). **H-C11 predicts drops appear.** This
separates the two surviving-ish hypotheses with one sample file and no framework change.

**E-3 (which route reaches `OnTick` during a fling — C7 or C8).** One-line trace log in
`CoreServices.RequestAdditionalFrame` (`CoreServices.cs:67`) printing the caller. `02-scheduling.md`
assumes the per-frame effective-viewport route (C7); `03-layout.md` §1.2 shows it is dead for a plain
`ListView`, leaving the realization/`Loaded` route (C8), which fires only on line-boundary crossings
and therefore **more often at higher velocity** — the opposite of "worse the slower". Whichever it is
changes the predicted drop-rate-vs-velocity curve, which is the one part of the observation nothing
currently explains.

**E-4 (kill the F4 confound before comparing).** Confirm `ScrollDiagnostics.IsEnabled` is false in the
product owner's build (`SCP.Managed.cs:164-172`). If it is true, every loaded
`ScrollContentPresenter` subscribes `CompositionTarget.Rendering`, `_isRenderingActive` is true for
both scroll cases and false for RedirectVisual, and `Render:164-167` injects an extra
`RequestNewFrame` per record into exactly the flag under investigation. The comparison would be
invalid.

**E-5 (import F1/F2 into the control — the constructive test).** Take the RedirectVisual sample and
subscribe a no-op handler to `Compositor.FrameStarting` for the lifetime of the page (internal API;
a throwaway build). This imports F1/F2 *without* importing any scroll work. **H-F2/A predicts: still
0 drops**, because C12 is still absent. Then add E-2's per-frame `InvalidateArrange()` on top:
**H-F2/A predicts drops appear only when both are present.** Two builds, and it settles the
conjunction claim that is the whole content of §5.2.

---

## 9. Status

| Claim | Status |
|---|---|
| The RedirectVisual sample contains no Composition animation; `_runningAnimations` is empty | **Verified by inspection** — `LottieVisualSourceBase.cs:74-79`, `AnimatedVisualPlayer.mux.cs:299-318`, `AnimatedVisualPlayer.legacy.cs:1-10` |
| The frame driver is the Skottie self-invalidate inside `Paint`, on a `Stopwatch` clock | **Verified by inspection** — `LottieVisualSource.Skottie.cs:346-351, 355-382`; `SKCanvasVisual.skia.cs:24` |
| The page requests its next frame from *inside* the record | **Verified by inspection** — `Compositor.skia.cs:378-383` |
| `Compositor.skia.cs:372-375` does **not** fire on this page | **Verified by inspection** — all three terms false |
| The page enqueues zero Normal dispatcher items, hence never reaches `OnRenderFrameOpportunity` | **Verified by inspection** — exhaustive caller list in §4 |
| `RedirectVisual` repaints uncached every frame and paints its source subtree twice | **Verified by inspection** — `RedirectVisual.skia.cs:10-23`, `Visual.skia.cs:478-484` |
| Bucket F contains exactly F1-F4, and only F1/F2 are mechanistic | **Verified by inspection**, subject to the §4 caveats |
| The Lottie is actually running on the product owner's device | **UNVERIFIED** — E-1 |
| `ScrollDiagnostics.IsEnabled` state in the measured build | **UNVERIFIED** — E-4 |
| Which of C7/C8 supplies `OnTick` during the measured fling | **UNVERIFIED** — E-3; the two give opposite velocity dependences |
| No runtime validation was performed | **True.** Everything here is code review. |
