# 11 — Win32 as a control: per-hypothesis predictions, the divergence list, and whether the datum is admissible

Scope: take the Win32 result ("121 `CompositionTarget.Rendering` callbacks/s, 0 % duplicate offsets
across fast/medium/slow") and use it as a cross-check on every hypothesis in `01`–`06`. Two questions,
both asked explicitly:

1. For each surviving hypothesis — does it predict Win32 would **also** drop? If yes, is there a
   Win32-specific reason it does not manifest, *in source*?
2. Is the Win32 measurement even comparable? What would have to change to make it a fair control, and
   should the number be trusted at all?

Everything below is **code review by inspection** of the worktree at `dev/mazi/smooth-scroll`. Nothing
was compiled or executed. Unverified claims are marked **UNVERIFIED**.

---

## 0. Verdict up front

**A.** Every surviving hypothesis lives *entirely* in code that Win32 and Skia-on-Android compile
**from the same source into the same generic `netX` assembly** (§1). None of them contains a platform
term. Therefore, at face value, **every one of them predicts Win32 behaves identically to Android** —
and the Win32 datum, if trustworthy, would refute all of them at once. Since that cannot be right,
either the datum is inadmissible (§5 — it largely is) or the hypotheses are all missing the same
term. Both are true, and the missing term is the same in every case: **UI-thread latency**.

**B.** The Win32 ∖ Android divergence list (§2) is short, and on two of its entries it points the
**wrong way**. Win32 enqueues a **Normal-priority dispatcher item on every single present** that
Android does not (`Win32WindowWrapper.Rendering.cs:38-43`), and its render loop has **no
`_renderRequested` re-check**, so it Draws on every wake where Android can `continue`
(`Win32WindowWrapper.RenderThread.cs:52-77` vs `UnoSKVulkanView.cs:146-162`). Under the brief's
leading hypothesis ("Normal-priority work gates the render action past its vsync") Win32 should be
**strictly worse than Android**. It is not. **That refutation does not depend on trusting the
harness's numbers at all** — it is a structural argument, and it is the strongest thing in this
document.

**C.** The `02`/`04` claim that `EnqueueRenderCallback` **State A** *deterministically* burns a present
cycle is **wrong**, and Win32 is what exposes it. Traced properly (§4), the steady state is
`ahead-of-time record → present → State A → ahead-of-time record → …`: **one record per present, zero
drops**, which is precisely the documented design intent (`RenderScheduling.skia.cs:180-182`). State A
costs a present *only when the ahead-of-time record fails to land inside the period*. That is a
**latency** claim, not a state-machine claim. H-A is not self-sufficient; it is an *amplifier*.

**D.** The Win32 measurement is **not admissible as a control for "dropped"** (§5): the drop counter
is compiled-in but switched off (`SkiaRenderHelper.skia.cs:215`), the harness measures records not
presents, it perturbs the exact flag under investigation, its "drag" is a synchronous managed call
that never touches the OS input pipeline (`InputInjector.cs:112-140`), and the headline
"0 % duplicates" **cannot be the number the committed test prints** (§5, M6). Its one defensible use
is as a weak bound on record rate — and even that is diluted ~50 % by a post-fling idle tail.

---

## 1. The null hypothesis: what Win32 and Skia-Android share

This is the load-bearing framing, so it is worth establishing precisely.

`src/Uno.CrossTargetting.targets:77-82` defines, for **every** `UnoRuntimeIdentifier=Skia` build:

```
__SKIA__ ; UNO_HAS_ENHANCED_LIFECYCLE ; UNO_HAS_MANAGED_POINTERS ;
UNO_HAS_MANAGED_SCROLL_PRESENTER ; HAS_COMPOSITION_API ; …
```

Skia-on-Android and Win32 both consume the **generic `netX` `Uno.UI`** (per AGENTS.md's
`RuntimeAssetsSelectorTask` note). So the following are not merely "similar" — they are the same IL:

| Component | File | Divergence between Win32 and Skia-Android |
|---|---|---|
| Render scheduling state machine | `CompositionTarget.RenderScheduling.skia.cs` (whole file) | **none** |
| Record + publish + present accounting | `CompositionTarget.Rendering.skia.cs:110-198, 221-330` | **none** |
| `FrameStarting`, animation tick, end-of-record re-arm | `Compositor.skia.cs:300-376` | **none** |
| The ahead-of-time door | `CoreServices.cs:123-125` — guarded by `#if __SKIA__`, i.e. **on for both** | **none** |
| Single-flight frame request | `CoreServices.cs:67-75, 79` | **none** |
| Dispatcher queues, priorities, `TryGetRenderAction`, N-seeding | `NativeDispatcher.cs:128-234` | **none** (same source file; only the `EnqueueNative` partial differs) |
| Scroll driver: `StartFling`/`OnFlingFrame`/`Set`/`Updated` | `ScrollContentPresenter.Managed.cs:586-644` | **none** |
| Deferred SV update → `ViewChanged` | `ScrollViewer.cs:1234-1357` | **none** |
| Drop counter | `SkiaRenderHelper.skia.cs:268-324` | **none** |

**Consequence.** A hypothesis whose mechanism is stated purely in terms of these files makes an
identical prediction for Win32 and for Skia-Android. If Win32 is genuinely clean and Android is
genuinely not, the difference **must** be expressible using one of the seven divergences in §2 — or
the hypothesis is incomplete.

---

## 2. The complete Win32 ∖ Android divergence list, source-verified

| # | Divergence | Win32 | Skia-Android | Direction |
|---|---|---|---|---|
| **D1** | **Dispatcher backend** | `NativeDispatcher.skia.cs:22-27` → `DispatchOverride` = `Win32EventLoop.Schedule` (`Win32Host.cs:154`) → `PostMessage` to a **message-only HWND** (`Win32EventLoop.cs:51-63, 96-108`) on a thread pumped by `while(true) RunOnce()` (`Win32Host.cs:174-182`). **Nothing else posts to it.** | `NativeDispatcher.Android.cs:40-43` → `_handler.Post(_implementor)` on the **Android main `Looper`**, shared with ViewRootImpl, the input pipeline, binder, and every platform `Handler`. Priority argument **ignored**. | **Android worse** (unbounded external contention). The only divergence that can plausibly produce a multi-millisecond `L`. **UNVERIFIED** on device. |
| **D2** | **Input arrival ordering** | `Win32EventLoop.RunOnce` peeks `PM_REMOVE \| PM_QS_INPUT` **first** (`Win32EventLoop.cs:122-123`) — input outranks posted dispatcher work. | Strict FIFO on the Looper; input arrives through the platform input receiver. | Affects the **drag** arm only. Irrelevant during a fling (no input). |
| **D3** | **Render-loop wake predicate** | `_frameSignal.WaitOne()` then **unconditionally** `_drawFrame()` (`Win32WindowWrapper.RenderThread.cs:56-77`). No flag re-check. | `_renderEvent.Wait(100 ms); _renderEvent.Reset(); if (!_renderRequested) continue;` (`UnoSKVulkanView.cs:149-155`). | **Win32 worse or equal.** Win32 has strictly fewer opportunities to suppress a stale Draw. |
| **D4** | **Per-present Normal-priority dispatcher item** | `onClipPathUpdated` → `NativeDispatcher.Main.Enqueue(…, NativeDispatcherPriority.Normal)` on **every present**, from the render thread, **unconditionally** (subscriber-independent) — `Win32WindowWrapper.Rendering.cs:38-43`, called at `RenderThread.cs:72`. | The clip path is applied **inline on the render thread**: `ApplicationActivity.NativeLayerHost!.Path = nativeClipPath` (`UnoSKVulkanView.cs:220`). No dispatcher item. | **Win32 worse** for any Normal-gating hypothesis: Win32's `normalItemsToProcessBeforeNextRenderAction` is re-seeded ≥ 1 on essentially every handover (`NativeDispatcher.cs:216`); Android's is not. |
| **D5** | **Per-present render-thread cost** | Marshals an `SKPath` reference to the UI thread (D4). | `ClippedRelativeLayout.Path` setter runs `value.ToSvgPathData()` — an **SKPath → string serialization** plus a string compare — **every present** (`ApplicationActivity.cs:502-512`). | **Android worse** — inflates the present duration `D`, shrinking the UI thread's `T − D` budget. Pure waste; the compare could be done on path identity. |
| **D6** | **Per-invalidate UI-thread cost** | `IXamlRootHost.InvalidateRender() => _renderThread?.SignalNewFrame()` — two interlocked ops (`Win32WindowWrapper.Rendering.cs:24`, `RenderThread.cs:41-45`). | `InvalidateRender()` calls **`ExploreByTouchHelper.InvalidateRoot()` — a JNI hop into AndroidX — before** setting the flag (`UnoSKVulkanView.cs:60-65`). This runs on the **UI thread**, twice per fling frame (once from `RequestNewFrame:110`, once from `Rendering.skia.cs:171`). | **Android worse**, and it sits *inside* the critical window. |
| **D7** | **Pacer** | `Win32RenderPacer.WaitForNextFrame()` → `PInvoke.DwmFlush()`, an **in-thread block** (`Win32RenderPacer.cs:59-82`). | `ChoreographerFramePacer.WaitForNextFrame()` does a **`Handler.Post` to a third Looper thread per wait**, which then registers `PostFrameCallback` (`ChoreographerFramePacer.cs:88`). Late registration ⇒ a ~2-period sleep; racy `seen` read at `:92-93`. | **Android worse** — injects phase jitter into where the present lands relative to the record. |

Two facts that are **not** divergences, contrary to what `05` §1 implies:

- **Both are MAILBOX-style non-blocking present + explicit post-present pacer.** Win32 defaults to
  Vulkan (`FeatureConfiguration.cs:660`, `UseVulkanOnWin32 = true`) and `VulkanRenderer.CopyPixels`
  does `BlitAndPresent()` then `_pacer.WaitForNextFrame()` (`Win32WindowWrapper.Rendering.Vulkan.cs:123-132`) —
  structurally identical to `UnoSKVulkanView.cs:156-161`. The claim that Win32 "blocks for vsync
  *inside* the draw pass" is true only in the trivial sense that `CopyPixels` is called from the loop
  body; the ordering (present → pace → return to wait) is the same on both.
- **Both post the render action *before* the present** (`RenderScheduling.skia.cs:170-175`), from the
  render thread, on both platforms.

> **The divergence list has exactly one entry (D1) that can lengthen the UI thread's latency by
> milliseconds, and three (D5, D6, D7) that add fixed per-frame cost on Android. D3 and D4 push the
> other way.** Any hypothesis that needs Android to be worse must borrow from {D1, D5, D6, D7}.

---

## 3. Per-hypothesis Win32 prediction

Format: mechanism → where it lives → Win32 prediction → escape hatch (if the prediction is "drops") →
what would falsify it.

### H-N — "Normal-priority work gates the render action past its vsync" (the brief's leading hypothesis)

- **Lives in:** `NativeDispatcher.cs:206-234` — shared source, identical algorithm.
- **Win32 prediction: DROPS, and MORE than Android.** D4 puts a Normal item in the queue on **every
  present**, unconditionally, from the render thread — before the UI thread has even woken. So the
  N-seed at `NativeDispatcher.cs:216` is ≥ 1 on essentially every handover on Win32, where on Android
  it is 0 in steady state (`01` §4 establishes that the fling's own `OnTick` is drained before the
  next take).
- **Escape hatch:** none in source. D1 makes the *cost* of each withheld item smaller on Win32
  (a `PostMessage` round trip on an uncontended pump vs a `Handler.post` on a shared Looper), but
  that is a magnitude argument, not a mechanism argument — and it concedes the point that the
  mechanism is latency, not gating.
- **Status: DEAD.** It already failed the drag arm (`01` §5, `02` §5, `03` §3). Win32 kills it a
  second, independent way: **the platform that pays more of the alleged poison is the clean one.**

### H-A — "`EnqueueRenderCallback` State A (`_ahead && _rRAAOTP`) burns a present cycle" (`02` §2, `04` §5.2)

- **Lives in:** `RenderScheduling.skia.cs:131-139` × `Compositor.skia.cs:372-375` × `CoreServices.cs:124`.
  All shared, all `#if __SKIA__` or unguarded.
- **Preconditions:** P1 = `OnRenderFrameOpportunity` runs (needs a Normal `OnTick`, i.e.
  `RequestAdditionalFrame`); P2 = `FrameStarting is not null` (true for the whole fling).
- **Do the preconditions hold on Win32 with the harness?** **Yes, both.**
  - P2: identical code.
  - P1: `ComputedVerticalScrollBarVisibility` defaults to `Visible` (`ScrollViewer.cs:512-517`), so the
    vertical `ScrollBar` is realized; `ScrollViewer.Update` writes `VerticalOffset`
    (`ScrollViewer.cs:1326`) → template-bound `ScrollBar.Value` → `UpdateTrackLayout` → layout-affecting
    DP writes → `InvalidateParentMeasureDirtyPath` walks to the root → `XamlRoot.InvalidateMeasure()`
    (`UIElement.Layout.crossruntime.cs:74-77`) → `CoreServices.RequestAdditionalFrame()`
    (`XamlRoot.crossruntime.cs:14-20`) → a Normal `OnTick`. Same on both platforms.
    Additionally the harness's own `Rendering` subscription forces `_rRAAOTP` on **every**
    ahead-of-time record via `Rendering.skia.cs:164-167`, i.e. it *manufactures* P2 independently.
- **Win32 prediction: DROPS.** And at the rate `02` §3 claims (1 in 3 presents), the Win32 record rate
  would be ~⅔ of the present rate.
- **Escape hatch:** none — **because the premise is wrong.** See §4: State A is benign in steady
  state on *both* platforms. H-A as stated over-predicts everywhere, including Android.
- **Status: WRONG AS STATED.** Survives only in the composed form of §4.

### H-C / H-W — "the speculative `InvalidateRender` from `RequestNewFrame:110` outruns its picture" (`02` §5 H-C, `06` §0)

- **Lives in:** `RenderScheduling.skia.cs:106-113`, `Compositor.skia.cs:374`, `Rendering.skia.cs:147/157/171`.
  All shared.
- **Win32 prediction: DROPS, at the same rate.** The window
  `[in-record invalidate → picture published]` opens identically. Both render threads are parked in a
  pacer for the remainder of the period, so the window is normally absorbed identically.
- **Escape hatch:** D7 only. Android's pacer wake phase is set by a `Handler.Post` + Choreographer
  registration per wait; Win32's by `DwmFlush`. If Android's wake lands *early* relative to the record
  (or two periods late, per `05` §6 R1/R2), the window is more likely to catch a Draw. This is a real
  escape hatch but it is **UNVERIFIED** and it degenerates to "phase jitter", i.e. H-Φ again.
- **Status: NOT REFUTED, NOT TESTED.** The Win32 datum cannot see presents at all (§5 M1/M2), so it
  says nothing here. This is the hypothesis for which the Win32 result is *least* informative.

### H-Φ / H-L4 / H-L5 — the phase-and-budget family (`03` §3, `05` §4)

- **Statement:** `dropped++` on frame *k* ⟺ `L_k + R_k > T − D_k` (`05` §4), where `L` is
  present→record-start latency and `R` the record cost. Discrimination is in `L` (and in whether the
  workload self-sustains the present loop at all).
- **Lives in:** partly in shared code (`R`), partly in the platform (`L` ← D1, `D` ← D5/D7).
- **Win32 prediction: CLEAN**, and it says *why*: `L` is a `PostMessage` on an uncontended
  message-only pump (D1), `D` is a `DwmFlush` with no serialization (D5, D7), `R` is a 200-child
  `StackPanel` on a desktop CPU. Every term is smaller, and one (D1) is smaller by an unbounded
  factor.
- **Escape hatch:** not needed — this family predicts the Win32 result rather than being embarrassed
  by it. **This is the only family for which the Win32 datum is confirmatory.**
- **Status: SURVIVES.** With the caveat that "Win32 has margin" is a *weak* confirmation: it is
  compatible with almost any cost-driven story. The sharp version is E4 in §8.

### H-P — `ChoreographerFramePacer` R1/R2 phase noise (`05` §6)

- **Lives in:** `ChoreographerFramePacer.cs:80-102` — **Android only** (D7).
- **Win32 prediction: CLEAN by construction.** `DwmFlush` has no registration hop.
- **Status: SURVIVES as a contributor.** It is one of only two hypotheses whose mechanism is
  genuinely absent from Win32, so the Win32 datum is *consistent* with it — but consistency with a
  hypothesis that predicts "Win32 is clean" for a platform that is clean is nearly free evidence.
  Falsifiable only on device (E5 in `05`).

### H-D — the `_renderEvent.Reset()` / `_renderRequested` race (`02` §5 H-D)

- **Lives in:** `UnoSKVulkanView.cs:149-155` — **Android only** (D3).
- **Win32 prediction: CLEAN by construction** (no re-check, no Reset).
- **Status:** already rejected in `02` for being workload-independent; `05` §5 H6 argues the flag
  covers the lost `Set`. I agree with `05`: the flag is written before the `Set`
  (`UnoSKVulkanView.cs:63-64`) and read after the `Reset` (`:150-152`), so the worst case is one
  wasted iteration. **Not a drop source.**

### Summary table

| Hypothesis | Mechanism lives in | Win32 prediction | Win32-specific escape hatch | Status after this note |
|---|---|---|---|---|
| **H-N** (brief's leading) | shared | **drops, worse than Android** (D4) | none | **DEAD** (second, independent kill) |
| **H-A** (State A burns a present) | shared | **drops** | none — premise is wrong (§4) | **WRONG AS STATED** |
| **H-C / H-W** (invalidate outruns picture) | shared | **drops** | D7 only, unverified | **UNTESTED** — the Win32 metric is blind to it |
| **H-Φ / H-L4 / H-L5** (latency × cost) | shared `R`, platform `L`,`D` | **clean** | n/a — it predicts the datum | **SURVIVES** |
| **H-P** (pacer registration) | Android-only (D7) | **clean** | n/a | **SURVIVES as contributor** |
| **H-D** (loop reset race) | Android-only (D3) | **clean** | n/a | **not a drop source** |

---

## 4. What Win32 forces: State A is benign in steady state

`02` §3 and `04` §5.2 assert a cadence of "2 records per 3 presents", i.e. State A costs a present
every time it is entered. Traced against the actual code, that is not what happens.

Steady state during a fling, either platform. Enter the period with
`_ahead = true, _rRAAOTP = true, RenderRequested = false`:

1. **V_k** — render thread: `Draw` presents the ahead-of-time record. Generation is fresh ⇒
   `OnFramePresentRequested` takes the non-drop branch (`SkiaRenderHelper.skia.cs:309-323`).
   `OnNativePlatformFrameRequested` posts the render action first (`RenderScheduling.skia.cs:170-173`).
   Render thread then parks in the pacer.
2. **UI** — render action → `EnqueueRenderCallback` → **State A** (`:131-139`): clears both flags,
   `RequestNewFrame()` → `RenderRequested = true` + a speculative `host.InvalidateRender()`
   (`:106-113`). **Absorbed** — the render thread is parked. *No record.*
3. **UI** — the Normal queue drains: `ScrollViewer.Update` (`ScrollViewer.cs:1308-1316`) → `VerticalOffset`
   → `ScrollBar.UpdateTrackLayout` → root `InvalidateMeasure` → `RequestAdditionalFrame` →
   `OnTick`.
4. **UI** — `OnTick` → `root.UpdateLayout()` (`CoreServices.cs:115`) → `OnRenderFrameOpportunity`
   (`:124`). Tree is clean, so `CanRecordPicture` passes (`SkiaRenderHelper.skia.cs:33-34`);
   `RenderRequested && !_ahead` ⇒ `_ahead = true`, `RenderRequested = false`, **`Render()`**.
   Inside: `FrameStarting` advances the fling, `Compositor.skia.cs:374` sets `_rRAAOTP = true`,
   `Rendering.skia.cs:147` publishes, `:157` bumps the generation, `:171` issues a **backed**
   `InvalidateRender` (also absorbed).
5. **V_{k+1}** — present the record from step 4. Fresh ⇒ **no drop**. State is identical to step 0.

**One record per present. Zero drops.** That is exactly the bargain the comment at
`RenderScheduling.skia.cs:180-182` describes, and it works.

The `OnTick` in step 3-4 essentially always wins its race: it is enqueued ~one full period before the
*next* render action is posted (which happens at V_{k+1}, step 1). It loses only if the UI thread
is still busy when V_{k+1} arrives.

### So when *does* a drop happen?

When steps 3-4 do not complete inside the period. Then at V_{k+1} the `Draw` finds
`current == lastPresented` ⇒ **`dropped++`** (`SkiaRenderHelper.skia.cs:309-313`). Recovery is
immediate on the next turn — and note that the N-gate (`NativeDispatcher.cs:214`) *helps*: with
`N ≥ 1` the withheld render action lets the late `OnTick` run first, so the ahead-of-time record
still happens. `05` §5 already observed that the gate helps the drag; it helps here too.

**Therefore: `dropped` is a count of UI-thread period overruns.** State A is not the generator; it is
the *bookkeeping* that makes an overrun cost a whole present instead of a partial one. And the
"one overrun costs two periods" amplification of `05` §3 (G3+G4) is real and is what turns a modest
overrun rate into visible judder.

### Why that reconciles all four observations

The discriminator is not *which* mechanism runs — it is **whether the workload self-sustains the
present loop**, which decides whether an overrun is *visible to the counter at all*:

| | self-sustains the loop? | source | overrun rate | predicted `dropped` | observed |
|---|---|---|---|---|---|
| **Finger drag** (Android) | **NO** — `FrameStarting` null, `_runningAnimations` empty ⇒ `Compositor.skia.cs:372` does not fire; the only `RequestNewFrame` comes from the pointer handler's `AnchorPoint` write | `SCP.Managed.cs:521-527` | whatever it is | **structurally 0** — an overrun produces *no `InvalidateRender`*, hence *no `Draw`*, hence nothing to count | ~0 ✔ (and **uninformative**) |
| **Touch inertia** (Android) | **YES** — `FrameStarting is not null` ⇒ `Compositor.skia.cs:372-375` re-arms every record | `SCP.Managed.cs:601` | high: `R` = realized `ListView` record + paint; `L` = shared main Looper (D1) + JNI per invalidate (D6); `D` inflated by SVG serialization (D5); pacer jitter (D7) | **> 0** | 20+ ✔ |
| **RedirectVisual** (Android) | **YES** — the Skottie self-invalidate inside `Paint` calls `RequestNewFrame` every record (`04` §2.2) | `LottieVisualSource.Skottie.cs:346-351` → `Compositor.skia.cs:378-383` | ≈ 0: two ≤200×200 uncached subtrees, **zero Normal items**, no layout | **0** | 0 ✔ |
| **Touch inertia** (Win32, harness) | **YES** (both `FrameStarting` **and** `_isRenderingActive`) | `Rendering.skia.cs:164-167` | ≈ 0: `L` = uncontended `PostMessage` (D1); `R` = 200-`TextBlock` `StackPanel`; `D` = `DwmFlush` | **0** | 0 ✔ (to the extent it was measured) |

The drag row is the one to internalise: **its zero is guaranteed by construction and carries no
information.** `06` §5 says this and it is correct. Any hypothesis that "explains" the drag's zero by
appealing to scheduling is explaining an artefact. The real comparison in the observation set is
**fling vs RedirectVisual**, and that comparison is about cost and latency, not about a state machine.

**Label:** this section is a *refinement* of `03` H-L4 and `05` H3, not a new hypothesis. It adds:
(a) the proof that State A is benign in steady state, and (b) the reason the drag arm is inadmissible
as evidence for or against anything.

---

## 5. Is the Win32 measurement admissible?

Seven defects. M1-M4 are fatal; M5-M7 are severe.

**M1 — The drop counter is not running.** `FpsHelper` short-circuits every hook on
`Application.Current?.DebugSettings?.EnableFrameRateCounter ?? false` (`SkiaRenderHelper.skia.cs:215`;
guards at `:270, :294`). `Given_ScrollSmoothness` never sets it. **The Win32 run produced no drop
count at all** — the quantity under investigation was not measured. This alone means the Win32 result
cannot refute any hypothesis stated in terms of `dropped`.

**M2 — It counts records, not presents, and coalesced ones at that.**
`CompositionTarget.Rendering` is raised from a **High**-priority dispatcher item scheduled once per
*batch* of recorded pictures, guarded by `_renderingRaiseScheduled`
(`Rendering.skia.cs:436-450`). So callbacks ≤ records, and a duplicated *present* is invisible: a
1-record-per-cycle stream reads identically whether it was presented once or twice. `02` §7 and
`06` §7 both say this; I confirm it at the cited lines.

**M3 — The sampled quantity is on a *different*, independently coalesced schedule than the sampler.**
The harness reads `sut.VerticalOffset` (`Given_ScrollSmoothness.cs:57`). `ScrollViewer.VerticalOffset`
is written **only** in `Update()` (`ScrollViewer.cs:1326`), which during a fling runs from a deferred
**Normal**-priority item coalesced by `_hasPendingUpdate` (`RequestUpdate`, `:1301-1316`; taken
because `UpdatesMode` defaults to `AsynchronousIdle`). So the metric is *Normal-priority DP,
sampled from a High-priority callback* — two independently coalescing dispatcher items compared
against each other. It is not the value that went into the picture. The value that went into the
picture is `-contentElt.Visual.AnchorPoint.Y`, which is what `ScrollDiagnostics` correctly records
(`SCP.Managed.cs:186-188`). **The harness measures the wrong number.**

**M4 — The harness perturbs the exact flag under investigation.** `Rendering.add` sets
`_isRenderingActive = true` and kicks every target (`Rendering.skia.cs:84-98`), which makes
`Render()` call `RequestNewFrame()` at the end of **every** record (`:164-167`). On an ahead-of-time
record that sets `_renderRequestedAfterAheadOfTimePaint` unconditionally — i.e. it forces the
precondition of State A for *every* workload, including a drag. It also injects a High-priority
`RaiseRendering` item per batch (`:448`) and keeps the render loop free-running forever after the
fling ends. **The probe manufactures the condition it is probing.**

**M5 — The "drag" arm does not exist, and what looks like one is not a drag.**
`InputInjector.InjectTouchInput` dispatches **synchronously on the calling thread**
(`src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs:112-140` — `DispatchPointerUpdated` in a
plain `foreach`). It never enters the OS input queue, so it has none of the input-phase delivery
semantics that H-Φ's drag arm depends on. Worse, the caller is a `Task.Delay(stepMs)` continuation on
the UI thread (`Given_ScrollSmoothness.cs:71-74`), i.e. **a Normal-priority dispatcher item driven by
a threadpool timer at 8 ms / 16 ms**, unsynchronised to vsync — against a real finger sampled at
120 Hz+ and delivered by the platform. And the test reports no drag-phase metric anyway: its
`frameTicks` column comes from `Compositor.CurrentFrameTimestampInTicks`, which is **only assigned
inside `if (FrameStarting is { } …)`** (`Compositor.skia.cs:307-312`) — null during a drag. **The
column is meaningless in phase 1.**

**M6 — The headline number cannot be what the committed test prints.** After `StopFling`
(`SCP.Managed.cs:605-615`) `FrameStarting` has no subscriber and `sut.VerticalOffset` is constant —
but `_isRenderingActive` keeps the loop running at the present rate (M4). The test then idles for
2.5 s (`Given_ScrollSmoothness.cs:79-83`). So `unchangedOffsetPairs` (`:102`) accumulates roughly one
duplicate per present for the whole idle tail — on the order of 10², not 0. **A printed 0 % is
impossible.** The reported figure must come from an offline slice of the CSV (the file does carry a
`phase` column, `:93-98`), but that slice has not been published. **Provenance unverified — the
number as quoted is not reproducible from the test as committed.**

**M7 — The aggregate is diluted by the idle tail, and the workload is not the workload.**
For the "medium" row the capture is ~160 ms of injected drag + ~1 s of decay + ~1.5 s of idle. So
roughly half the samples come from a phase with no motion and no Normal-priority traffic — exactly
the phase that is trivially clean. "121 callbacks/s" is therefore an average dominated by the easy
half. Separately, the content is a **200-child `StackPanel`** (`:37-41`), not a virtualized
`ListView`: no `ViewChanged` subscriber, no `VirtualizingPanelLayout.OnScrollChanged`, no container
realization, no `EventManager.RequestRaiseLoadedEventOnNextTick` on line-boundary crossing. Per
`03` §5.1 C5/C8, that removes most of the fling's per-frame UI-thread tax — i.e. removes the term the
surviving hypothesis says is causal.

### What, if anything, the Win32 number *can* support

One weak bound: **callbacks ≤ records**, so ≥121 records/s were produced *on average over the whole
capture*. That is enough to say Win32's record rate was not collapsing to ⅔ of the present rate,
which is a real (if soft) constraint on H-A as originally stated. It is **not** enough to attribute
that rate to the inertia phase specifically (M7), and it says nothing about presents (M2).

---

## 6. What the harness would have to become to be a fair control

Concretely, in `Given_ScrollSmoothness.cs`:

1. **Measure presents, not records.** Set `Application.Current.DebugSettings.EnableFrameRateCounter = true`
   for the duration (M1) and read `dropped`/`unpresented`/`fps` out of `FpsHelper` — which currently
   has no accessor; expose one (`internal` is fine) or add two `Interlocked` counters next to
   `SkiaRenderHelper.skia.cs:311` and `:283`. Report `records/s`, `presents/s`, `drops/s` separately.
2. **Stop probing with `CompositionTarget.Rendering`.** Drive the capture from `FrameRendered`
   (`Rendering.skia.cs:80`) or from an internal callback in `Render()` — anything that does **not**
   set `_isRenderingActive` (M4). If `Rendering` must be used, report the run twice, with and without
   the subscriber, and show they agree.
3. **Sample the value that was rendered**, i.e. `-((UIElement)sut.Content).Visual.AnchorPoint.Y`, not
   the deferred `VerticalOffset` DP (M3). `ScrollDiagnostics` already does exactly this
   (`SCP.Managed.cs:186-188`) — use it, and make sure `ScrollDiagnostics.IsEnabled` state is recorded
   in the output so the confound of `SCP.Managed.cs:164-172` is auditable.
4. **Slice by phase in the assertion, not offline.** Compute the duplicate rate over
   `phase == 2 && offset is still changing`, and print the phase-2 window length. Print
   `presents/s`, `records/s` and `drops/s` per phase (M6, M7).
5. **Use the real workload.** A `ListView` over a virtualizing `ItemsStackPanel` with a non-trivial
   `DataTemplate`, sized so containers actually recycle during the fling — otherwise C5/C8 of
   `04` §5.1 are absent and the control is easier than the field case in exactly the dimension under
   test (M7).
6. **Add a real drag arm** that is comparable. `InjectTouchInput` cannot be made vsync-aligned
   (M5), so either (a) accept that the drag arm is not portable and drop it, or (b) drive the drag
   from `Compositor.FrameStarting` so at least its *phase* is well defined, and label it "synthetic
   drag" everywhere.
7. **Record the environment.** Which `IRenderer` was selected (`Win32WindowWrapper.cs:108-115` —
   Vulkan / GL / software have three different pacing behaviours), whether `DwmFlush` degraded
   (`Win32RenderPacer.cs:67-74`), and the actual refresh rate. None of this is currently captured,
   and the pacing model changes completely between them.
8. **Add a RedirectVisual-equivalent control row** to the same harness, so all three cases are
   measured by one instrument on one platform. Today the three observations come from three different
   instruments.

Additionally, to make Win32 a *mechanistically* fair control rather than merely a faster one,
neutralise the divergences that make Android harder: run Android with D5 removed (cache the SVG
string against `SKPath` identity, `ApplicationActivity.cs:507`) and D6 removed (hoist
`ExploreByTouchHelper.InvalidateRoot()` out of `InvalidateRender`, `UnoSKVulkanView.cs:62`). If the
Android fling goes clean, the answer was cost, and no scheduling change is needed.

---

## 7. Should the Win32 result be trusted at all?

Graded:

| Claim | Trust |
|---|---|
| "Win32 does not drop presented frames during a fling" | **No.** Presents were never measured (M1, M2). |
| "Win32's *record* rate keeps up with the display during a fling" | **Weakly yes**, as a whole-capture average (≥121 records/s), diluted ~50 % by the idle tail (M7). |
| "0 % duplicate offsets" | **No.** Not the number the committed test prints (M6); provenance unpublished; and the sampled quantity is the wrong one (M3). |
| "Win32 is a valid control for the Android fling" | **No.** Different workload (StackPanel vs virtualized ListView), different input model (synchronous injection vs OS touch), different instrument, and the probe perturbs the state machine (M4). |
| "Win32 is structurally different from Android in a way that matters" | **Yes — but the differences are D1/D5/D6/D7 (cost and latency), and D3/D4 point the other way.** This is a source claim, independent of the measurement, and it is the part of the Win32 comparison that should be trusted. |

**The one conclusion the Win32 comparison genuinely earns** is B in §0, and it needs no numbers:
Win32 pays a Normal-priority dispatcher item on **every present** (D4) that Android does not, and has
**no** stale-Draw suppression (D3). If Normal-priority gating were the generator, Win32 would be the
sick platform. It is not — by the product owner's own general experience, and by the harness's record
rate. **H-N is dead on structure.**

---

## 8. Experiments

Ordered by cost. E1 and E2 need no device.

**E1 — Republish the Win32 number honestly (harness only, ~1 h).** Apply §6 items 1-4 and re-run the
three rows. This is the minimum required before any Win32 datum is cited again. Predictions:
- **H-Φ/H-L4:** drops ≈ 0 on Win32 even with the real `ListView` workload (§6 item 5), because `L`
  is small.
- **H-C/H-W:** drops **> 0** on Win32 as soon as the counter is switched on, since the
  `[invalidate → publish]` window is identical. **This is the sharpest fork available without a
  device**, and it separates the two remaining families.

**E2 — Inflate `L` on Win32 until it breaks (harness, ~30 min after E1).** Register a
`Compositor.FrameStarting` handler *after* the scroll driver that spins for a configurable number of
milliseconds, and sweep it from 0 to ~1.5 × the period.
- **H-Φ predicts** a sharp knee once the injected cost crosses `T − D − L`, with the drop rate going
  from ~0 to substantial over a narrow band, **and** the RedirectVisual-equivalent row staying clean
  until the injected cost alone exceeds the period.
- **H-A-as-stated predicts** drops at a fixed ~⅓ rate *independent of the injected cost*, because it
  claims the state machine burns the cycle regardless. **These predictions are qualitatively
  different and cannot both be right.**

**E3 — Falsify §4's steady-state trace on device (Android, logging only, ~10 min).** Trace-log
`Microsoft.UI.Xaml.Media.CompositionTarget` and count, per second during a fling, (a) State A
(`RenderScheduling.skia.cs:137`), (b) State B (`:142`), (c) State C (`:151`), and separately count
`OnRenderFrameOpportunity` records (`:204`). §4 predicts **(a) ≈ (c-via-`OnRenderFrameOpportunity`) ≈
the present rate**, i.e. State A is entered on nearly every frame *and* is nearly always followed by
an ahead-of-time record inside the same period — with `dropped` far smaller than (a). If instead
`dropped ≈ (a)`, §4 is wrong and `02` §3 was right.

**E4 — Remove D5 and D6, measure, before touching any scheduling code (Android, ~1 h).**
Cache the SVG string against `SKPath` identity (`ApplicationActivity.cs:507`) and hoist
`ExploreByTouchHelper.InvalidateRoot()` out of the per-frame path (`UnoSKVulkanView.cs:62`). Both are
pure waste on the critical path and neither changes semantics. If `dropped` falls materially, the
answer was cost, the Win32 "control" was never needed, and no state-machine change should ship.

**E5 — Measure `L` directly on both platforms (the decisive one).** `05` E2: histogram
`EnqueueRender` post → `EnqueueRenderCallback`/`OnTick` entry, on Android during a fling and on Win32
during the harness fling. **H-Φ predicts a fat tail past ~2 ms on Android and none on Win32.** If the
two histograms are the same shape, the whole latency family is dead and H-C/H-W owns the result.

---

## 9. Ledger

| Claim | Status |
|---|---|
| Win32 and Skia-Android compile the entire scheduling/scroll/compositor path from identical source with identical symbols | **Verified** — `Uno.CrossTargetting.targets:77-82`; file list in §1 |
| Win32 enqueues a Normal-priority dispatcher item on every present; Android does not | **Verified** — `Win32WindowWrapper.Rendering.cs:38-43` + `RenderThread.cs:72` vs `UnoSKVulkanView.cs:220` |
| The Win32 render loop draws on any wake, with no `_renderRequested` re-check | **Verified** — `Win32WindowWrapper.RenderThread.cs:52-77` |
| Both platforms are non-blocking present + post-present pacer (Vulkan default on Win32) | **Verified** — `FeatureConfiguration.cs:660`; `Win32WindowWrapper.Rendering.Vulkan.cs:123-132`; `UnoSKVulkanView.cs:156-161` |
| Android pays a JNI `InvalidateRoot()` per invalidate on the UI thread, and an SKPath→SVG serialization per present on the render thread | **Verified** — `UnoSKVulkanView.cs:60-65`; `ApplicationActivity.cs:502-512` |
| Win32's dispatcher is an uncontended message-only pump; Android's is the shared main Looper | **Verified** — `Win32EventLoop.cs:51-63, 96-108`, `Win32Host.cs:154, 174-182`; `NativeDispatcher.Android.cs:40-43` |
| State A is benign in steady state (one record per present) | **Verified by inspection** — trace in §4 against `RenderScheduling.skia.cs:131-152, 178-208`, `CoreServices.cs:67-127`, `ScrollViewer.cs:1301-1357` |
| The drag's `dropped == 0` is structural and carries no information | **Verified by inspection** — `Compositor.skia.cs:372`; `SCP.Managed.cs:521-527`; agrees with `06` §5 |
| `FpsHelper` is entirely disabled unless `EnableFrameRateCounter` is set | **Verified** — `SkiaRenderHelper.skia.cs:215, 270, 294` |
| `CompositionTarget.Rendering` subscription sets `_isRenderingActive`, forcing `_rRAAOTP` on every ahead-of-time record and keeping the loop free-running | **Verified** — `Rendering.skia.cs:84-98, 164-167` |
| The harness samples a Normal-priority deferred DP from a High-priority coalesced callback | **Verified** — `Given_ScrollSmoothness.cs:57`; `ScrollViewer.cs:1301-1326`; `Rendering.skia.cs:436-450` |
| `InjectTouchInput` dispatches synchronously on the caller's thread | **Verified** — `src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs:112-140` |
| The committed harness cannot print 0 duplicate offset pairs | **Verified by inspection** — `Given_ScrollSmoothness.cs:79-83, 102` × `Rendering.skia.cs:164-167` |
| That Android main-Looper latency actually exceeds a period on ~17 % of fling frames | **UNVERIFIED** — this is the load-bearing claim of the surviving family; E5 is the test |
| That `ChoreographerFramePacer` late registration actually occurs on device | **UNVERIFIED** — `05` E5 |
| Which `IRenderer` the Win32 run actually used, and whether `DwmFlush` degraded | **UNVERIFIED** — not recorded by the harness |
| Provenance of the quoted "121 callbacks/s, 0 % duplicates" | **UNVERIFIED** — not reproducible from the committed test |
| Nothing in this note was compiled or executed | **True.** Evidence class: code review only. |
