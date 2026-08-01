# 12 — The load hypothesis: prosecuted, then refuted (as a cause), then rehabilitated (as a modulator)

**Brief:** argue, as hard as the evidence permits, that "the Android UI thread simply cannot record
120 times a second on a virtualized ListView of text, and every other mechanism is noise."

**Verdict up front:**

> Load is **real, measurable, and necessary** — but it is **not the differentiator**, and the
> hypothesis as stated is **dead**. A drag and a fling execute *byte-for-byte the same record*, on the
> same device, at the same rate. Any explanation whose only variable is "how much work the record
> does" predicts drag and fling drop identically. They do not. The hypothesis cannot survive that.
>
> What survives is narrower and, I think, correct: **load is the gate, not the trigger.** The fling
> issues a redundant render-thread wake-up from *inside* the record — at record-end minus epsilon —
> which the drag structurally does not. That wake-up is harmless while the record finishes inside its
> vsync slot, and produces a stale present when it doesn't. Load decides which of those two you get.
> That is why the same code is clean on Win32 (fast record), clean on the RedirectVisual page (tiny
> record), and dirty on an Android ListView fling (record straddling the 8.33 ms budget).

Everything below is anchored to `file:line` in this worktree. Claims I could not confirm from source
are marked **UNVERIFIED**.

---

## 1. First, fix the metrology — three of the numbers do not mean what they look like

Before costing anything, three corrections, because they change the arithmetic.

### 1.1 "FPS" counts *presents*, including the stale ones

`SkiaRenderHelper.skia.cs:243-260` — `EndFrame()` increments `_framesRenderedInLastSecond`, and
`BeginFrame()/EndFrame()` bracket only the blit (`CompositionTarget.Rendering.skia.cs:294-299`),
which runs on every `Draw` that had a frame slot to present, fresh or not.

So the overlay's `FPS` is **presents per second**, and `dropped` is the subset of those presents that
re-blitted the previous picture. The number of *unique* frames the user saw is `FPS − dropped`.

> "120 FPS, dropped 20" is not a 120 Hz experience with a small blemish. It is **~100 unique
> frames per second displayed on a 120 Hz panel**, i.e. a repeating 1,1,1,1,1,2-vsync cadence.
> That is exactly the artefact `SurfaceFrameRate.cs:12-17` already documents for the 90-on-120 case
> ("*a repeating one, one, two vsync cadence, which reads as judder in anything animating*").
> The felt symptom and the counter agree; §7 returns to this.

### 1.2 On Android, `Draw` is **not** driven by vsync

The brief describes `dropped` as "the native vsync fired and `Draw` ran". On Win32 that is roughly
true. On Android it is not. `UnoSKVulkanView.RenderLoop` (`UnoSKVulkanView.cs:137-171`):

```csharp
while (_surfaceReady && !_disposed)
{
    _renderEvent.Wait(TimeSpan.FromMilliseconds(100));
    _renderEvent.Reset();

    if (!_surfaceReady || _disposed || !_renderRequested)
        continue;                      // <-- no Draw, no drop counted

    _renderRequested = false;
    RenderFrame();                     // <-- Draw + present
    _pacer.WaitForNextFrame();         // <-- vsync, AFTER the present
}
```

The loop is **demand-driven**. It parks on `_renderEvent` and only draws when somebody called
`IXamlRootHost.InvalidateRender()` (`UnoSKVulkanView.cs:60-65`, which sets `_renderRequested` and
signals the event). The Choreographer pacer (`ChoreographerFramePacer.cs:80-102`) runs *after* the
present, to stop the MAILBOX swapchain free-running.

This has a consequence that is fatal to the naive load story and that must be stated plainly:

> **A slow UI thread, on its own, cannot produce a single `dropped` on Android.** If the record
> overruns, the render thread wakes at its vsync, finds `_renderRequested == false`, hits `continue`,
> and parks again. No `Draw`, no `OnFramePresentRequested`, no drop. It presents late — lowering
> FPS and adding jitter — but it never presents *stale*.
>
> To get a `dropped`, something must call `InvalidateRender()` **without a new picture behind it**.
> `dropped` is a count of **surplus wake-ups**, not a count of missed deadlines.

That is the single most important structural fact in this document.

### 1.3 The `dropped` counter can over-count by a race

`CompositionTarget.Rendering.skia.cs:135-157`:

```csharp
lock (_frameGate)
{
    ...
    _lastRenderedFrame = (framePicture, path, damageSnapshot);   // :147  fresh picture published
    ...
}                                                                 // :155  gate released
_fpsHelper.OnFrameRecorded();                                     // :157  generation bumped
```

`Draw` takes `_frameGate`, reads `_lastRenderedFrame`, and calls `OnFramePresentRequested()` inside
the same lock (`:235-241`). If the render thread acquires the gate in the window between `:155` and
`:157`, it gets the **fresh** picture but sees the **un-incremented** generation, and
`OnFramePresentRequested` (`SkiaRenderHelper.skia.cs:309-313`) scores a drop for a frame that was
actually presented fresh.

The window is a few hundred nanoseconds, but the render thread is on another core and is *actively
contending for that lock* at exactly that moment during a fling (see §5.3). This is worth fixing
regardless of the outcome of this investigation — move `OnFrameRecorded()` inside the `_frameGate`
block — because it means part of the reported `20+` may be pure instrumentation error.
**Classification: metrology defect, not the root cause.** **UNVERIFIED** what share of the 20 it
accounts for.

---

## 2. The load estimate: what the UI thread actually does per fling frame

Now the substantive part of the brief. What is the real per-frame UI-thread cost for a virtualized
text ListView on this device?

Assumptions (**UNVERIFIED** — I cannot see the product owner's actual sample page): Fold 7 inner
display, ~800×750 dp logical, 48 dp text rows ⇒ ~15 rows visible; ListView cache is
`CacheLength = 1.0 × ExtendedViewportScaling 0.5` = half a viewport per side
(`FeatureConfiguration.cs:323`, `VirtualizingPanelLayout.managed.cs:155`) ⇒ **~28–30 realized
containers**. A plain `TextBlock`-in-`ListViewItem` costs ~5 visuals per item (ListViewItem
`BorderVisual` → ContentPresenter `BorderVisual` → template root → TextBlock `ContainerVisual` +
`TextVisual`); a richer template 12–20. So the paint walk visits roughly **150–500 visuals**.

### 2.1 Text is *not* re-rendered on the UI thread. This part of the hypothesis is simply false.

`Visual.skia.cs:471-511` `PaintStep`:

```csharp
var contentChanged = (visual._flags & VisualFlags.PaintDirty) != 0;
if (contentChanged) { ...record visual.Paint(...) into visual._picture... }
visual.ContributeDamageOnPaint(contentChanged, session.Damage, clip);
UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, visual._picture, null, IntPtr.Zero);
```

A scroll sets `PaintDirty` on exactly one visual — the scroll port's content visual, via
`Compositor.InvalidateRenderPartial` (`Compositor.skia.cs:378-383`) which calls
`SetMatrixDirty()` + `InvalidatePaint()` on *that* visual only. `SetMatrixDirty` recurses to children
(`ContainerVisual.skia.cs:212-227`); `InvalidatePaint` does **not**. So every item keeps its
`_picture`.

Consequently, per frame, per `TextBlock`: **one `sk_canvas_draw_picture` op recorded into a parent
recorder.** `TextBlock.Draw` → `ParsedText.Draw` (`TextBlock.skia.cs:280-308`) does **not** run.
No shaping, no glyph run construction, no rasterization on the UI thread.

Rasterization of the text happens on the **render thread**, in `Draw` →
`SkiaRenderHelper.RenderPicture` → `canvas.DrawPicture` + `canvas.Flush()`
(`SkiaRenderHelper.skia.cs:55-71`), reached from `UnoSKVulkanView.RenderFrame`
(`UnoSKVulkanView.cs:202-230`) on the `UnoVulkanRenderThread`. It is GPU-bound work on a different
thread, and it is **identical for drag and fling**.

> "Text rendering is what blows the budget" is refuted by inspection. The UI thread records a picture
> reference; it does not draw text.

### 2.2 Layout is ~free in steady state, and expensive only in proportion to velocity

From the prior audit (`research/13-uno-virtualization-cost.md` §4.1, itself code-anchored):

> **Steady-state ListView scroll frame = 0 full measure passes, 0 full arrange passes, 0 element
> measures, 20 short-circuited `Arrange` calls, plus one full walk of the recycle cache.**

`container.Arrange(bounds)` short-circuits on an unchanged rect
(`UIElement.Layout.crossruntime.cs:362-366`), and during a pure scroll the item rects in panel space
do not change — the offset lives on the SCP content's `Visual.AnchorPoint`
(`ScrollContentPresenter.Managed.cs:521-528`), not in layout.

The genuinely expensive layout work is **line crossing** (research §4.2): a `Leave` subtree walk, an
`Enter` subtree walk enumerating every DP on every node (`DependencyObjectStore.PropertySystem.mux.cs:57-64`),
a full template `Measure`, VSM transitions, and two extra root `UpdateLayout()` passes on the next
tick. Multi-millisecond, easily.

**And its rate is proportional to scroll velocity.** This matters enormously:

> The reported symptom is that the fling gets **worse as it slows down**. Line-crossing load goes to
> **zero** as the fling decays. Under a load-only hypothesis, the tail of a fling is the *cheapest*
> part of the whole gesture and should be the cleanest. It is reported as the worst.
> **This is a direct empirical contradiction of load-as-cause**, independent of any drag comparison.

(Caveat, stated fairly: a constant drop *rate* is perceptually worse at low velocity, because the eye
tracks individual items instead of a blur. So "worse when slower" is consistent with a
velocity-independent drop rate. It is not consistent with a velocity-*proportional* one. Either way
it does not support load.)

### 2.3 What the record walk actually costs, per visual, per frame

This is where the real per-frame UI-thread cost lives. Enumerating `Visual.Render`
(`Visual.skia.cs:375-592`), per visited visual, per frame, during a scroll:

| # | Work | Site | Native Skia calls |
|---|---|---|---|
| 1 | `_pathPool.Allocate()` ×2 (`ownClip`, `childClip`) | `:407-410` | 0 (pooled) |
| 2 | `clipInRoot.Transform(Identity, ownClip)` | `:421` | 1 path copy |
| 3 | `GetPrePaintingClipping(preClip)` → `GetArrangeClipPathInElementCoordinateSpace` | `ContainerVisual.skia.cs:167-210`, `:121-143` | 1 `SKPath` ctor+dtor + 1 transform, when a `LayoutClip` exists |
| 4 | `canvas.ClipPath(preClip)`, `preClip.Transform(toRoot)`, `ownClip.Op(preClip, Intersect, ownClip)` | `:423-427` | 1 clip op + 1 transform + **1 path boolean** |
| 5 | `ownClip.Transform(Identity, childClip)` | `:429` | 1 path copy |
| 6 | post-paint clip: alloc + 2 transforms + `Op(Intersect)` | `:430-437` | **1 path boolean** when present |
| 7 | `ContributeDamageOnPaint` → `TryGetPaintDamageRegion` | `Visual.Damage.skia.cs:27-185` | ≥1 path copy (`:88`), see below |
| 8 | `sk_canvas_draw_picture` of the cached `_picture` | `Visual.skia.cs:505` | 1 |
| 9 | `ApplyPostPaintingClipping` | `:525`, `BorderVisual.skia.cs:182-194` | 1 clip op |

Item 7 has two very different cost classes, and the split is decided by
`Visual.Damage.skia.cs:100`:

```csharp
var canUseBounds = ShadowState is null ? CanPaint() && PaintsWithinOwnSize : true;
```

* **Bounds branch** (`canUseBounds == true`): `BorderVisual` (`BorderVisual.skia.cs:377-383`),
  `SpriteVisual` (`SpriteVisual.skia.cs:19-22`), `TextVisual` (`TextVisual.skia.cs:40-42`). Cheap:
  one `MapRect`, an inflate, an intersect (`:119-149`).
* **Exact-path branch**: everything whose `PaintsWithinOwnSize` is false — notably **every
  `ShapeVisual`** (`ShapeVisual.skia.cs:65` overrides `CanPaint` but not `PaintsWithinOwnSize`).
  These pay, *per visual per frame while scrolling*: a path copy, a matrix transform, then
  `OutsetForAntialiasing` (`Visual.Damage.skia.cs:190-201`) which is a **stroke-to-fill
  `SKPaint.GetFillPath` plus an `SKPathOp.Union`**, then a second `Op(clipPath, Intersect)`.
  That is 2 path booleans plus a stroke-to-fill, per shape, per frame.
* **Non-painting containers**: `canUseBounds` false, `_ownContentPath` null ⇒ they still pay the
  `clip.Transform(Identity, clipPath)` copy at `:88` before bailing out at `:123`.

The comment at `Visual.Damage.skia.cs:44-48` ("*Scrolling makes this the common case for the whole
subtree, so take bounds*") describes an optimisation that **`ShapeVisual` does not qualify for**.
Any Path/Ellipse/Rectangle/icon glyph in the item template is on the expensive branch every frame.
This is a genuine, citable, avoidable load defect. It is also **identical for drag and fling.**

**Cost estimate (ESTIMATE, not measured):** on a Snapdragon-class ARM64 core with SkiaSharp
P/Invoke, ~50–100 ns per transition, ~0.2–0.5 µs per small path copy, ~1–4 µs per `SKPath.Op` even
on rectangles (Skia's path ops go through the full `SkOpBuilder` pipeline), ~5–20 µs for a
stroke-to-fill on a non-trivial path. That gives:

| Node class | Est. per visual per frame |
|---|---|
| Non-painting `ContainerVisual` | ~1–2 µs |
| `BorderVisual` with layout clip + corner clip | ~3–6 µs |
| `TextVisual` | ~2–4 µs |
| `ShapeVisual` (exact-damage branch) | ~10–30 µs |

**300 visuals of the first three classes ⇒ ~0.6–1.8 ms.** To blow an 8.33 ms budget on the paint
walk alone you would need ~2,500 plain visuals, or ~300 `ShapeVisual`s. A plain text ListView has
neither.

### 2.4 The picture-collapsing optimization is structurally disabled during any scroll

`Visual.skia.cs:39-45, 389-399, 531-591`. `RenderChildrenStep` collapses a subtree into a cached
`_childrenPicture` only when `_framesSinceSubtreeNotChanged >= 50` **and** the subtree has ≥ 100
visuals. But `SetMatrixDirty` → `InvalidateParentChildrenPicture(false)` (`Visual.skia.cs:140-146,
245-258`) walks the ancestor chain setting `ChildrenSKPictureInvalid`, and `Render` resets
`_framesSinceSubtreeNotChanged = 0` for every visual carrying that flag (`:395-398`).

During a scroll, every interior node under the scroll port is matrix-dirty every frame, so the
counter never leaves 0 and the optimization **can never engage**. That is correct behaviour (the
content genuinely moves) but it means there is no caching backstop: the full walk runs every frame,
by construction. Same for drag and fling.

### 2.5 Everything else on the UI thread's frame

* `UpdateVisibilities` walks the *entire* recycle pool (up to `CacheLimit` = 1024) writing the
  `Visibility` DP on detached containers, per `ViewChanged` (research §7.8,
  `VirtualizingPanelGenerator.managed.cs:269-280`). DP writes in Uno are not free.
* `CoreServices.OnTick` (`CoreServices.cs:77-127`) runs `root.UpdateLayout()` and, on Skia,
  `OnRenderFrameOpportunity()` at `:124`. This is the Normal-priority item that
  `EnqueueForEffectiveViewportChanged` → `RequestAdditionalFrame` (`EventManager.cs:29-35`,
  `CoreServices.cs:67-75`) schedules.
* Per-frame allocations (research §5.1): the `RemoveAll` closure in `EventManager.cs:31`,
  `EffectiveViewportChangedEventArgs` per propagated node, `ScrollViewerViewChangedEventArgs`,
  a `Dispatcher.RunAsync` closure + `UIAsyncOperation` per deferred `ViewChanged`
  (`ScrollViewer.cs:1301-1316`). Steady Gen0 pressure ⇒ periodic sub-millisecond GC pauses.
  **These land in both drag and fling.**

### 2.6 Load verdict, on the numbers

A realistic per-frame UI-thread cost for this content on this device:

| Item | Estimate |
|---|---|
| Record walk (~300 visuals) | 0.6–1.8 ms |
| `UpdateVisibilities` pool walk | 0.2–1.0 ms |
| EVP propagation + `UpdateLayout` (nothing dirty) | 0.1–0.4 ms |
| Dispatcher hops / sync-barrier exposure | 0.5–3 ms (§6, **UNVERIFIED**) |
| Line crossing, when it happens | +2–6 ms, ~1 frame in `itemHeight/step` |
| Gen0 GC, when it happens | +0.3–1 ms |

**Conclusion: a typical frame is comfortably inside 8.33 ms; the tail is not.** The distribution
straddles the budget, with a minority of frames overrunning. If ~17 % of frames overrun, you get
100 unique frames on 120 presents — which is precisely the reported `FPS 120 / dropped 20`.

That is a *quantitatively plausible* load story. It is also, on its own, **insufficient**, for the
reason in §1.2: overrunning does not by itself produce a stale present. And it is **not
discriminating**, for the reason in §3.

---

## 3. The killer: drag and fling execute the identical record

Both paths converge on the same method. `ScrollContentPresenter.Managed.cs`:

* Drag: pointer handler → `Set(...)` with `options.IsTouch: true`.
* Fling: `OnFlingFrame` (`:617-644`) → `Set(horizontalOffset: h, verticalOffset: v,
  options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: running))` at `:643`.

From `Set` onward the code is the same object graph:
`Set` → `Update` (`:471-582`) → the immediate branch at `:521-528` (`DisableAnimation || IsTouch`,
true for both) → `visual.AnchorPoint = target` → `Updated` (`:434-469`) → `OnPresenterScrolled` +
`InvalidateViewport`.

The fling driver's own work per frame is two decay-curve evaluations and two `Math.Clamp`
(`:631-632`). Microseconds.

Therefore:

> **The record's cost is a function of the offset delta and the tree, not of who wrote the offset.**
> Drag and fling do the same `SetMatrixDirty` fan-out, the same paint walk, the same damage
> accumulation, the same `UpdateVisibilities`, the same EVP propagation, the same allocations.
> A drag at 3000 px/s crosses *more* item lines per frame than the fling that follows it, so if
> anything the drag is the **more** loaded of the two.

A hypothesis whose sole variable is per-frame load must predict drag ≈ fling. Observation says
drag ≈ 0 and fling ≈ 20. **The hypothesis is falsified as a cause.**

---

## 4. Three-way prediction tables

Per the brief, every hypothesis states what it predicts for all three cases. ✓ = matches
observation, ✗ = contradicts.

### H1 — Pure load ("the UI thread can't record at 120 Hz")

| Case | Prediction | Observed | |
|---|---|---|---|
| Drag | Same record ⇒ same overruns ⇒ same drops (arguably more: higher velocity ⇒ more line crossings) | ~0 dropped | ✗ |
| Fling | Overruns ⇒ drops; **worst at the start** (fastest, most line crossings), improving as it decays | 20+, **worst as it slows** | ✗ |
| RedirectVisual | Trivial tree (2 TextBlocks, 1 Image, 1 AVP, 2 redirects — `RedirectVisualTests.xaml:14-67`) ⇒ record ≪ budget ⇒ 0 drops | 0 dropped | ✓ |

**1 of 3. Dead.** It also cannot produce a drop at all on Android's demand-driven loop (§1.2).

### H2 — Pure surplus-invalidate (load irrelevant)

*Mechanism:* the fling's `RequestNewFrame()` fires from inside the record, waking the render thread
for a present with nothing new behind it.

| Case | Prediction | Observed | |
|---|---|---|---|
| Drag | No `FrameStarting` subscriber ⇒ `Compositor.skia.cs:372-375` does not fire ⇒ no mid-record wake ⇒ 0 drops | ~0 | ✓ |
| Fling | Mid-record wake every frame ⇒ drops ≈ record rate (~100/s, not 20/s) | 20+ | ✗ (magnitude) |
| RedirectVisual | Lottie animation ⇒ `_runningAnimations.Count > 0` ⇒ **same** mid-record wake ⇒ should drop too | **0 dropped** | ✗ |

**1 of 3.** Also dead on its own — and RedirectVisual is what kills it, which is exactly why the
brief insisted on the three-way table.

### H3 — Surplus invalidate **gated by** record duration (synthesis)

*Mechanism:* the fling issues a redundant render-thread wake at *record-end − ε*. That wake is
absorbed if the render thread is still inside its post-present `_pacer.WaitForNextFrame()`; it
produces a stale `Draw` if the render thread has already passed its vsync and is parked on
`_renderEvent`. Which of the two happens is decided by whether the record finishes before or after
the next vsync boundary — i.e. by load.

| Case | Prediction | Observed | |
|---|---|---|---|
| Drag | Surplus wake comes from the **input handler at the start** of the frame's UI work, deep inside the render thread's pacer wait ⇒ collapses with the end-of-record invalidate ⇒ **0 drops regardless of load** | ~0 | ✓ |
| Fling | Surplus wake at record-end − ε. Drops on the fraction of frames whose record ends past the vsync ⇒ ~17 % of frames ⇒ **20 per 120** | 20+ | ✓ |
| RedirectVisual | Same mid-record wake, but a ~sub-ms record on a near-empty page always ends far inside the pacer wait ⇒ **0 drops** | 0 | ✓ |
| Win32, same fling | Same structure, but a desktop core records the tree in ~1–2 ms ⇒ always inside the slot ⇒ clean | 121 cb/s, 0 % dup | ✓ |

**4 of 4.** This is the only hypothesis on the table that survives all four data points, and it makes
load *necessary* — which is the honest rehabilitation of the brief's thesis.

---

## 5. The mechanism in code, precisely

### 5.1 The fling's surplus wake exists; the drag's does not

`Compositor.skia.cs:372-375`, at the end of `RenderRootVisual` — i.e. **inside**
`SkiaRenderHelper.RecordPictureAndReturnPath` (`SkiaRenderHelper.skia.cs:44`), which is called from
`CompositionTarget.Render()` at `CompositionTarget.Rendering.skia.cs:119-124`:

```csharp
if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
{
    rootVisual.CompositionTarget?.RequestNewFrame();
}
```

`FrameStarting` has subscribers only for: the SCP touch fling
(`ScrollContentPresenter.Managed.cs:601`), the wheel decay (`:675`), and the gesture recognizer's
inertia processor (`GestureRecognizer.Manipulation.InertiaProcessor.cs:357`, whose `Start()` runs on
pointer-up). **During a drag, `FrameStarting` is null and `_runningAnimations` is empty**, so this
line does not fire.

`ICompositionTarget.RequestNewFrame` (`CompositionTarget.RenderScheduling.skia.cs:86-118`) calls
`host.InvalidateRender()` on the `RenderRequested` false→true transition — and when `Render()` was
entered from `EnqueueRenderCallback` (`:145-153`), `RenderRequested` was just set false, so the
transition fires and the render thread is signalled.

Timing of that signal: it happens after the paint walk (`Compositor.skia.cs:349-352`) but **before**
`EndRecording` (`SkiaRenderHelper.skia.cs:50`), before `_lastRenderedFrame` is published
(`CompositionTarget.Rendering.skia.cs:147`) and before `OnFrameRecorded()` (`:157`).

The drag's equivalent signal comes from `Compositor.InvalidateRenderPartial`
(`Compositor.skia.cs:378-383`) when the pointer handler writes `visual.AnchorPoint` — i.e. at the
*start* of the frame's UI-thread work, one full record ahead of the danger zone. Plus the
unconditional `InvalidateRender()` at `CompositionTarget.Rendering.skia.cs:169-172`, which is *after*
`OnFrameRecorded()`.

> **This is the structural asymmetry.** Not "who does more work" — *when the redundant signal is
> emitted relative to the picture being published.*

### 5.2 Why load gates it

Render-thread cycle: `Draw` → present → `_pacer.WaitForNextFrame()` (blocks to the next vsync) →
park on `_renderEvent`. So there is a window, from the vsync boundary until the next signal, in
which the render thread is **awake and hungry**. Anything that signals during that window draws
immediately.

* Record finishes early (RedirectVisual, Win32): record-end − ε lands inside the pacer wait. The two
  signals collapse into one wake (`ManualResetEventSlim` + the `_renderRequested` flag). One `Draw`,
  fresh. **No drop.**
* Record finishes late (heavy Android fling frame): record-end − ε lands after the vsync, while the
  render thread is parked and hungry. It wakes, draws the *previous* picture — or, via the §1.3 race,
  the new picture with a stale generation. **Drop.**

Both outcomes are consistent with `FPS ≈ 120` (the panel rate is saturated by presents) while
unique content lands at ~100/s.

### 5.3 Two secondary defects found while tracing this

Neither is the root cause; both are worth filing.

1. **Lost-wakeup window in the Android render loop.** `UnoSKVulkanView.cs:150-155`: `_renderEvent` is
   `Reset()` at `:150`, `_renderRequested` is checked at `:152` and cleared at `:155`. An
   `InvalidateRender()` landing between `:152` and `:155` sets `_renderRequested = true` and signals,
   then `:155` clears the flag. The event is set, so the next iteration wakes immediately, resets,
   finds `_renderRequested == false`, and `continue`s — parking for up to 100 ms with a real request
   dropped on the floor. Rare, but it is a hard stall, not a jitter.
   **Classification: root-cause-class correctness defect (in a different failure mode).**
2. **`OnFrameRecorded()` outside `_frameGate`** — §1.3.

### 5.4 Where the "Normal-priority dispatcher" hypothesis actually sits

The leading hypothesis in the brief (`Set` → `InvalidateViewport` → `RequestAdditionalFrame`
enqueues a Normal item; `NativeDispatcher.TryGetRenderAction` withholds the render action until
`normalItemsToProcessBeforeNextRenderAction` drains — `NativeDispatcher.cs:206-234`) is a *load*
mechanism in disguise: it adds dispatcher latency to the record's effective duration. It therefore
**feeds H3** rather than competing with it — it is one of the terms that pushes record-end past the
vsync.

But note the interaction that complicates it, and that agent 01's thread should settle: when the
Normal item drains first, `CoreServices.OnTick` calls `OnRenderFrameOpportunity()`
(`CoreServices.cs:124`), which enters `Render()` with `_renderedAheadOfTime = true`
(`CompositionTarget.RenderScheduling.skia.cs:178-208`). In *that* path the mid-record
`RequestNewFrame` takes the `else if (_renderedAheadOfTime)` branch at `:98-101` and **does not call
`InvalidateRender()`** — the surplus wake is suppressed. So H3's trigger only fires on frames where
`Render()` was entered from `EnqueueRenderCallback`, not from `OnRenderFrameOpportunity`.

That is a *second* gate on the mechanism, and it is a plausible reason the observed rate is 20/120
rather than 100/120. **UNVERIFIED** — proving the ratio needs the §6 instrumentation.

---

## 6. Falsifiable experiments, cheapest first

### E1 — `Compositor.SkipVisualTreePainting` (decisive, one line, already in the codebase)

`Compositor.skia.cs:40, 349-352`. Setting it true skips **only** the paint walk; `FrameStarting`
still ticks, `RequestNewFrame` at `:372-375` still fires, the loop stays live.

Run the fling with it set. Then:

* **Drops → 0**: the surplus wake is harmless when the record is fast ⇒ **load-gated (H3
  confirmed)**, and RedirectVisual is explained by the same fact.
* **Drops stay ~20**: the surplus wake alone does it ⇒ **load is irrelevant (H2), the brief's thesis
  is fully dead**, and the fix is purely in the scheduling.

This is the single most decisive measurement available and it costs one `internal` accessor.

### E2 — Read the `unpresented` counter during the fling (zero cost)

The overlay already shows it (`SkiaRenderHelper.skia.cs:268-284, 382`). `unpresented` counts records
that were superseded before ever reaching the screen.

* `unpresented ≈ 0, dropped ≈ 20` ⇒ the UI thread is **under**-producing relative to presents ⇒
  surplus wakes (H2/H3).
* `unpresented > 0` ⇒ the UI thread is **over**-producing ⇒ a different failure entirely, and both
  H2 and H3 need revision.

Ask the product owner for this number before anything else. It is already on screen.

### E3 — `PRINT_FRAME_TIMES`, drag vs fling (direct load measurement)

`Compositor.skia.cs:1, 23-25, 344-356` — uncomment `#define PRINT_FRAME_TIMES` and compare the
per-frame paint-walk distribution for a drag and the fling that follows it, same list, same device.

* **Distributions identical** (they should be — §3) ⇒ load is a constant across the two, so it cannot
  be the differentiator. Confirms §3 empirically.
* **Fling materially worse** ⇒ my §3 argument is wrong and I have missed a work item that only the
  fling performs. That would resurrect H1 and I would want to know immediately.

Also read off what fraction of frames exceed 8.33 ms. H3 predicts that fraction ≈ dropped/FPS ≈ 17 %.
**That is a quantitative prediction H3 can fail.**

### E4 — Log the wake source (falsifies H3 directly)

Add a counter at `Compositor.skia.cs:374` and at `CompositionTarget.Rendering.skia.cs:171`, plus one
in `UnoSKVulkanView.RenderLoop` for "woke and drew". H3 predicts, per fling second: ~100
end-of-record invalidates, ~100 mid-record `RequestNewFrame` calls of which ~20 actually reach
`InvalidateRender` (the rest suppressed by `_renderedAheadOfTime` — §5.4), and ~120 draws.
Any other distribution refutes H3.

### E5 — Reproduce on Win32 by making the record slow

H3 says Win32 is clean only because the record fits. Force the fling harness
(`Given_ScrollSmoothness.cs:35-106`) onto a heavy tree — 30+ realized items with `ShapeVisual`s in
the template, which takes the expensive damage branch (§2.3) — or busy-wait ~10 ms inside a
`FrameStarting` handler, and check whether duplicate offsets appear on Win32 too.

* **Duplicates appear** ⇒ H3 confirmed, the bug is not Android-specific, and it is reproducible in CI.
* **Win32 stays clean** ⇒ the differentiator is in the Android host loop itself (`UnoSKVulkanView` /
  `ChoreographerFramePacer`), not in the shared scheduling, and H3 needs an Android-specific term.

E5 is the one that would let this become a **regression test** rather than a field observation.

### E6 — Android main-looper sync barriers (**UNVERIFIED**, cheap to check with `systrace`)

`NativeDispatcher.EnqueueNative` on Android is `_handler.Post(_implementor)`
(`NativeDispatcher.Android.cs:40-43`) onto the **main** `Looper`, which Uno shares with Android's own
input delivery and `ViewRootImpl` traversals. `ViewRootImpl.scheduleTraversals()` posts a *sync
barrier* that blocks ordinary `Handler.post` messages until the traversal runs. Note that
`UnoSKVulkanView.InvalidateRender` calls `ExploreByTouchHelper.InvalidateRoot()` at
`UnoSKVulkanView.cs:62` — **from the render thread** — which is an accessibility invalidation on a
`View`. If that schedules a traversal, every Uno dispatcher item behind the barrier is delayed by up
to a vsync.

This is a pure-load term (it inflates effective record duration) and would be the same for drag and
fling, so it does not differentiate — but it could be a large constant, and if it is, removing it
moves the whole distribution below the budget and makes H3's gate stop firing. **Worth a `systrace`
capture.**

---

## 7. Should we present at a stable divisor of the panel rate?

Assessed on its merits, independent of which mechanism wins. **Short answer: yes, ship it as a
user-selectable mode and as the automatic fallback under sustained overrun — but do not ship it as
the fix, because it hides the defect rather than removing it, and if H3 is right it may not even
hide it.**

### 7.1 The argument for is strong and this repo has already made it once

`SurfaceFrameRate.cs:12-17` is the precedent, in this codebase, with measurements:

> *"Android assigns the surface a frame-rate category — 90Hz on a 120Hz panel — and a rate that does
> not divide the panel's cannot be presented evenly: 120/90 leaves a repeating one, one, two vsync
> cadence, which reads as judder in anything animating. Measured at 64% single and 35% double
> intervals before this call."*

That is *exactly* the artefact under investigation, one level down the stack. 100 unique frames on a
120 Hz panel is 120/100 — a repeating 1,1,1,1,2 cadence. **A rock-steady 60 genuinely does beat an
uneven 100**, because the eye integrates *interval variance*, not frame count. Below ~72 Hz the eye
starts to resolve individual steps, but a uniform 60 Hz step is what every Android app shipped for a
decade and it reads as smooth; a jittering 100 Hz does not.

Second-order benefit that matters on a phone: halving the present rate roughly halves render-thread
GPU work and gives the UI thread a 16.67 ms budget it comfortably meets, which collapses the tail of
the record-duration distribution and drops power draw and thermal throttling — the latter being a
plausible reason a fling degrades *over time* on a folding phone. **UNVERIFIED.**

### 7.2 What it would take

Four pieces, in dependency order:

1. **Pace the present at N vsyncs.** `ChoreographerFramePacer.WaitForNextFrame()`
   (`ChoreographerFramePacer.cs:80-102`) already counts vsyncs via `_frameCount`; waiting for
   `seen + N` instead of `seen + 1` is a handful of lines. Call site `UnoSKVulkanView.cs:161`.
2. **Tell SurfaceFlinger.** Extend `SurfaceFrameRate` (`SurfaceFrameRate.cs:20-67`) to request the
   *target* rate with `FRAME_RATE_COMPATIBILITY_FIXED_SOURCE` rather than
   `SurfaceFrameRateCompatibility.Default` at the highest mode (`:47, :51`). On a panel that can drop
   to 60 Hz this makes every present a real refresh instead of a two-vsync hold — strictly better
   for latency and power. On a panel pinned at 120 Hz it at least stops the compositor guessing.
3. **Make the record cadence match.** The present pacer alone does not slow the *record*: the loop is
   driven by the invalidate ping-pong (§1.2, `CompositionTarget.Rendering.skia.cs:169-172`), so the
   UI thread would keep producing ~100 pictures/s and 40 of them would be thrown away — visible as
   the `unpresented` counter climbing, and a straight waste of battery. The record must be gated to
   the same divisor. The clean place is the render thread: only re-arm
   `_shouldEnqueueRenderOnNextNativePlatformFrameRequested`
   (`CompositionTarget.RenderScheduling.skia.cs:170-173`) on presenting vsyncs.
4. **Give the frame drivers the new period.** This part is **already built**:
   `Compositor.GetFrameTimestamp` (`Compositor.skia.cs:244-289`) recovers the presentation period
   from the median record interval and snaps drivers onto that grid, and `FrameIntervalInTicks`
   (`:220-222`) feeds the fling's launch back-dating (`ScrollContentPresenter.Managed.cs:623`). A
   *stable* 16.67 ms period is strictly easier for that median to track than a bimodal 8.33/16.67 —
   so this change makes the existing clock machinery work better, not worse.

Plus: expose it. `FeatureConfiguration.CompositionTarget.FrameRate` and
`SetFrameRateAsScreenRefreshRate` already exist (`FeatureConfiguration.cs:118, 125`) but are
documented "used by desktop skia renderers" and are **not consulted by
`UnoSKVulkanView.RenderLoop`**. Honouring them on Android is the natural surface.

### 7.3 The honest case against

* **It is a symptom fix.** Under H3 the drop is a redundant wake landing in a hungry window. At 60 Hz
  the window is *longer* (the render thread parks for 2 vsyncs' worth of hungry time after its pacer
  wait, if implemented as "wait 2 vsyncs then park"), so the surplus wake could still land in it —
  and the drop rate might not fall at all, it would just become a 60-vs-58 cadence instead of a
  120-vs-100 one. **This is a real risk and E1 tests it directly.** Implement §7.2 step 1 as "park,
  then only accept a signal after the Nth vsync" to close the window, not just lengthen the wait.
* **It costs latency.** 8.33 ms of extra worst-case input-to-photon on a touch drag, on a device
  whose selling point is the fast panel. Users who bought 120 Hz will notice, and the drag path —
  which is currently *clean* — would get worse. Any implementation must therefore be
  **phase-scoped**, not global.
* **It gives up.** Win32 does 121 records/s of this same content. The Android record is not
  fundamentally 10 ms of work; §2 says a typical frame is 1–3 ms and the tail is what hurts. Fixing
  the tail (the `ShapeVisual` damage branch §2.3, `UpdateVisibilities` §2.5, the dispatcher
  round-trips §5.4, possibly the sync barrier §6) is a better use of the effort and keeps 120 Hz.

### 7.4 Recommendation

1. **Do not ship the divisor as the fix for this bug.** Run E1 and E2 first; they cost almost nothing
   and they decide whether the divisor would even help.
2. **Do implement it**, as `FeatureConfiguration.CompositionTarget` support on Android (§7.2 steps
   1–3), because it is the correct behaviour for low-end devices, for battery-saver mode, and as an
   automatic fallback when the record duration's 90th percentile exceeds the vsync period for a
   sustained window. Make it adaptive and hysteretic, not static.
3. **Regardless of mechanism, fix the two defects in §5.3 and the `ShapeVisual` damage branch in
   §2.3.** They are independently wrong.

---

## 8. Summary

| Claim | Status |
|---|---|
| Text is re-rendered on the UI thread per frame | **False.** `Visual.skia.cs:487-506` — cached `_picture`, one `drawPicture` op. Rasterization is on the render thread. |
| Layout runs per scroll frame | **False in steady state.** 0 measure/arrange passes; 20 short-circuited `Arrange` (research §4.1). Expensive only on line crossings, whose rate is velocity-proportional. |
| The record walk is expensive | **True but insufficient.** ~0.6–1.8 ms for ~300 visuals (est.); `ShapeVisual`s cost 5–10× more via the exact-damage branch (`Visual.Damage.skia.cs:100-117`). |
| Picture collapsing helps during scroll | **False.** Structurally disabled — `_framesSinceSubtreeNotChanged` resets every frame (`Visual.skia.cs:389-398`). |
| A slow UI thread produces `dropped` on Android | **False.** The loop `continue`s without drawing (`UnoSKVulkanView.cs:152-153`). `dropped` counts surplus wakes. |
| Drag and fling do the same record work | **True.** Both funnel through `ScrollContentPresenter.Managed.cs:Set` with `IsTouch: true`. |
| Therefore load explains the drag/fling split | **False. The load hypothesis is dead as a cause.** |
| Load gates a structural asymmetry | **Best surviving explanation (H3).** 4/4 on the prediction table. Not yet verified — E1 decides it. |
| A stable divisor beats an uneven 100 | **True perceptually**, worth building, **but may not fix this bug** and costs latency on the currently-clean drag path. |

**The one number to get next:** the `unpresented` counter during the fling (E2 — already on screen),
followed by E1 (`SkipVisualTreePainting`).
