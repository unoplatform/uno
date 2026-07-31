# Avalonia rendering / compositor architecture — what makes frames smooth

Research note for the Uno scroll-smoothness effort.

**Source tree read:** `D:/Work/Avalonia`, commit `e81f3f7ff7802e8dd4dcd52137358bb08952ecc0`
(2026-04-23, "Changes in CommandBar icon foreground inheritance (#21251)").
All paths below are relative to that root unless prefixed. Line numbers are from that commit.

Everything below was read from source. Anything I could not verify is explicitly marked
**UNVERIFIED**.

---

## 0. TL;DR of the architecture

```
                    UI THREAD                                       RENDER THREAD
                    =========                                       =============

 input / property change
        │
        ▼
 Visual.InvalidateVisual()  ──► CompositingRenderer.AddDirty(visual)
        │                              │
        │                              ▼
        │                     Compositor.RequestCompositionUpdate(_update)
        │                              │
        ▼                              ▼
 MediaContext.ScheduleRender()  ── DispatcherOperation @ DispatcherPriority.Render
        │
        ▼
 MediaContext.RenderCore()
   1. _clock.Pulse(now)                   (UI-thread animations, transitions)
   2. FireInvokeOnRenderCallbacks()       (LayoutManager: measure + arrange)
   3. CommitCompositorsWithThrottling()
        │                                        ┌──────────────────────────────┐
        ▼                                        │  IRenderTimer (vsync source) │
 Compositor.Commit()                             │  DXGI WaitForVBlank /        │
   - runs _invokeBeforeCommit queue              │  DwmFlush /                  │
     (CompositingRenderer.Update →               │  DComp WaitForCommitComplete/│
      per-dirty-Visual re-record of DrawList)    │  CVDisplayLink /             │
   - serializes changed CompositionObjects       │  AChoreographer /            │
     into a BatchStream (structs + objects)      │  CADisplayLink /             │
   - ServerCompositor.EnqueueBatch(batch)  ──────┼─► rAF (browser)              │
        │                                        └──────────────┬───────────────┘
        │  (UI thread now BLOCKS scheduling                     │ tick
        │   further commits until batch.Processed)              ▼
        │                                             DefaultRenderLoop.TimerTick
        │                                                       │
        │                                                       ▼
        │                                             ServerCompositor.RenderCore()
        │                                               ApplyPendingBatches()   ← deserialize
        │                                               NotifyBatchesProcessed()───┐
        │                                               Animations.Process()       │
        │                                               VisualOwnPropertiesUpdate  │
        │                                               AdornerUpdatePass          │
        │                                               foreach target:            │
        │                                                 t.Update()  ← dirty rects│
        │                                                 t.Render()  ← rasterize  │
        │                                               VisualReadbackUpdatePass   │
        ▼                                                                          │
 CompositionBatchFinished  ◄──── Dispatcher.Post(Send priority) ◄───────────────────┘
        │
        ▼
 ScheduleRender(false)  → next frame
```

The single most important structural fact: **the UI thread is paced by the render thread**, not by a
timer. `MediaContext` refuses to produce a new frame while a batch is in flight
(`MediaContext.Compositor.cs:64-82`), and the render thread only consumes batches at vsync. So the
UI thread naturally emits exactly one layout+commit per display refresh, and no more.

---

## 1. Is there a separate render/composition thread? What runs where?

**Yes**, on every desktop/mobile backend. It is opt-out (`ShouldRenderOnUIThread`) rather than opt-in.

### The thread boundary is `ServerCompositor : IRenderLoopTask`

`src/Avalonia.Base/Rendering/Composition/Server/ServerCompositor.cs:21`

```csharp
internal partial class ServerCompositor : IRenderLoopTask
```

It is registered into the render loop in its constructor (`ServerCompositor.cs:63`):

```csharp
_renderLoop.Add(this);
```

and `IRenderLoopTask.Render()` (`ServerCompositor.cs:185`) is what the render timer thread calls.

### Runs on the **UI thread**

Everything with a client-side `Compositor` / `CompositionObject` / `Visual` identity:

| Work | Location |
|---|---|
| Animation clock pulse for *UI-thread* animations & transitions | `MediaContext.cs:136` `_clock.Pulse(now)` |
| Measure + arrange (LayoutManager) | `MediaContext.cs:142` `FireInvokeOnRenderCallbacks()`; `LayoutManager.cs:353` `MediaContext.Instance.BeginInvokeOnRender(_invokeOnRender)` |
| Re-recording draw lists for dirty visuals | `CompositingRenderer.cs:150-169` — `visual.Render(_recorder); comp.DrawList = _recorder.GetRenderResults();` |
| Pushing `Visual` → `CompositionVisual` property values | `Visual.Composition.cs:129-170` `SynchronizeCompositionProperties()` |
| Serializing the batch | `Compositor.cs:123-197` `CommitCore()` |
| Hit-testing (against *readback* snapshots) | `CompositionTarget.cs:30-49` `TryHitTest` |

### Runs on the **render thread**

`ServerCompositor.RenderCore()` (`ServerCompositor.cs:251-290`):

```csharp
private bool RenderCore(bool catchExceptions)
{
    UpdateServerTime();                                            // :253
    var compositorGlobalPassesElapsed = ExecuteGlobalPasses();     // :255
    ...
        RenderInterface.EnsureValidBackendContext();               // :261
        ExecuteServerJobs(_receivedJobQueue);                      // :262
        foreach (var t in _activeTargets)
        {
            t.Update(compositorGlobalPassesElapsed);               // :266  transforms + dirty rects
            t.Render();                                            // :267  rasterize + present
        }
        VisualReadbackUpdatePass();                                // :270
        ExecuteServerJobs(_receivedPostTargetJobQueue);            // :272
```

and `ExecuteGlobalPasses()` (`ServerCompositor.cs:232-249`):

```csharp
ApplyPendingBatches();          // deserialize the UI thread's batch stream
NotifyBatchesProcessed();       // → completes CompositionBatch.Processed
Animations.Process();           // SERVER-SIDE animation evaluation
ApplyEnqueuedRenderResourceChangesPass();
VisualOwnPropertiesUpdatePass();  // recompute per-visual transform / clip / bounds
AdornerUpdatePass();
```

So the render thread owns: batch deserialization, **animation evaluation**, transform recomputation,
bounding-box/dirty-rect computation, tree walk, rasterization, present, and hit-test readback
publication.

### Thread-safety mechanism

Everything the render thread touches is guarded by one lock and an owner-thread check
(`ServerCompositor.cs:209-230, 313-318`):

```csharp
private bool RenderReentrancySafe(bool catchExceptions)
{
    lock (_lock)
    {
        try { _safeThread = Thread.CurrentThread; return RenderCore(catchExceptions); }
        finally { NotifyBatchesRendered(); }
    }
    ...
}
public bool CheckAccess() => _safeThread == Thread.CurrentThread;
```

If the *UI thread* has to run the compositor synchronously (resize, teardown, or
`ShouldRenderOnUIThread`), it takes the same lock and additionally suppresses dispatcher pumping
(`ServerCompositor.cs:188-207`):

```csharp
if (Dispatcher.UIThread.CheckAccess())
{
    if (_uiThreadIsInsideRender) throw new InvalidOperationException("Reentrancy is not supported");
    _uiThreadIsInsideRender = true;
    try { using (Dispatcher.UIThread.DisableProcessing()) return RenderReentrancySafe(catchExceptions); }
    finally { _uiThreadIsInsideRender = false; }
}
```

### Cross-thread reads: MVCC readback

Hit-testing and `TransformToVisual`-style queries must read transforms that the render thread is
concurrently mutating. Avalonia uses a **two-slot MVCC** scheme, documented in-place at
`ServerCompositionVisual.Readback.cs:10-33`:

- Two `ReadbackData` slots per visual (`:45-47`), each carrying `Matrix`, `Revision`, `TargetId`,
  `Visible`, `TransformedSubtreeBounds`.
- Writer (`UpdateReadback`, `:82-103`) picks the slot the reader is not allowed to see;
  `Interlocked.Exchange(ref slot.Revision, writerRevision)` (`:98`) prevents `ulong` tearing.
- Reader (`GetReadback`, `:60-80`) picks the newest slot with `Revision <= readerRevision`.
- Revisions advance under `ReadbackIndices` (`ReadbackIndices.cs:19-35`), where `BeginWrite`
  takes a `Monitor` and `NextRead` publishes `LastCompletedWrite`.
- The write pass is deliberately queue-driven and short — comment at `ServerCompositor.Passes.cs:42-43`:
  *"visual.HitTest is waiting for this lock to be released, so we need to be quick / this is why we have a queue in the first place"*.

**Why this matters for scroll smoothness:** the UI thread never has to stop the compositor to
hit-test. There is no "sync to get the current transform" stall in the input path.

---

## 2. What drives the frame clock, per platform

Abstraction: `IRenderTimer` (`src/Avalonia.Base/Rendering/IRenderTimer.cs`) → `IRenderLoop`
(`RenderLoop.cs:19` `RenderLoop.FromTimer(timer)` → `DefaultRenderLoop`).

| Platform | Timer class | Actual clock source | Thread | Cite |
|---|---|---|---|---|
| Win32 (DXGI/ANGLE composition) | `DxgiConnection` | `IDXGIOutput::WaitForVBlank()`, falling back to `DwmFlush()` | dedicated STA thread `"DxgiRenderTimerLoop"` | `src/Windows/Avalonia.Win32/DirectX/DxgiConnection.cs:104, 120, 233-238` |
| Win32 (WinUI composition mode) | `WinUiCompositorConnection` | `ICompositor5::RequestCommitAsync()` completion callback (DWM commit cadence) | dedicated STA thread `"DwmRenderTimerLoop"` with a message pump | `WinRT/Composition/WinUiCompositorConnection.cs:96, 112-145, 175-207` |
| Win32 (DirectComposition mode) | `DirectCompositionConnection` | `IDCompositionDevice::WaitForCommitCompletion()` | dedicated STA thread `"DwmRenderTimerLoop"` | `DComposition/DirectCompositionConnection.cs:86, 104-106` |
| Win32 fallback / redirection surface | `DefaultRenderTimer(60)` or `UiThreadRenderTimer(60)` | plain `System.Threading.Timer` @ 16.67 ms | threadpool (or UI thread) | `Win32Platform.cs:90`, `DefaultRenderTimer.cs:68-73` |
| X11 / Linux | `SleepLoopRenderTimer(60)` or `UiThreadRenderTimer(60)` | **no vsync at all** — a sleep loop targeting 60 Hz | dedicated background thread | `src/Avalonia.X11/X11Platform.cs:76-78, 90`; `SleepLoopRenderTimer.cs:52-68` |
| macOS | `ThreadProxyRenderTimer(AvaloniaNativeRenderTimer(...))` | `CVDisplayLink` | display-link callback thread, proxied onto `"RenderTimerLoop"` | `src/Avalonia.Native/AvaloniaNativePlatform.cs:125`; `AvaloniaNativeRenderTimer.cs:20-36`; `native/Avalonia.Native/src/OSX/PlatformRenderTimer.mm:24-30, 39-49, 73-79` |
| Android | `ChoreographerTimer` | `AChoreographer_postFrameCallback64` (API 29+), `AChoreographer_postFrameCallback` below | `"Choreographer Thread"` (Looper) posts to `"Render Thread"` | `src/Android/Avalonia.Android/ChoreographerTimer.cs:25-38, 90-144`; `AndroidPlatform.cs:93` |
| iOS | `DisplayLinkTimer` | `CADisplayLink` on its own `NSRunLoop` thread | dedicated thread | `src/iOS/Avalonia.iOS/DisplayLinkTimer.cs:16-27, 40-43`; `Platform.cs:96` |
| Browser (WASM) | `BrowserRenderTimer` | `self.requestAnimationFrame` | main thread, or a **web worker** when threading is on | `src/Browser/Avalonia.Browser/Rendering/BrowserRenderTimer.cs:33-49`; `webapp/modules/avalonia/timer.ts:4-9`; `Rendering/RenderWorker.cs:26-40` |

### Notable per-platform details

**Windows DXGI:** it deliberately picks the output with the *highest* refresh rate in a multi-monitor
setup (`DxgiConnection.cs:126-186`, `GetBestOutputToVWaitOn`, using `EnumDisplaySettings` +
`dmDisplayFrequency`). If `WaitForVBlank` throws it disposes the output and rediscovers
(`:100-113`); if no output at all, it degrades to `DwmFlush()` (`:120`).

**Windows WinUI composition:** the tick *is* the DWM commit completion. A watchdog SetTimer at 1000 ms
force-completes a stuck `RequestCommitAsync` (`WinUiCompositorConnection.cs:148-171`) — a workaround
for D3D device-loss hangs. The comment at `:150-154` names Parallels Desktop pause/resume.

**Android:** two threads. `Loop()` (`ChoreographerTimer.cs:69-74`) does `Looper.Prepare()` +
`AChoreographer_getInstance()` and just pumps. `DoFrameCallback` (`:106-115`) immediately re-posts the
next frame callback and then `_event.Set()`s the render thread. So Choreographer scheduling is
decoupled from render work — a slow frame doesn't miss the next Choreographer callback.
Both threads run at `ThreadPriority.AboveNormal` (`:30, 35`).

**iOS:** the `CADisplayLink` is added to a *dedicated thread's* run loop (`DisplayLinkTimer.cs:19-24`),
not the main run loop, so `NSRunLoop` tracking modes on the UI thread can't starve it. Note
`// TODO: start/stop on RenderLoop request` at `:33` — the iOS link is never paused for idle
(only on app background, `:25-26`).

**X11 is the weak spot.** `SleepLoopRenderTimer` sleeps to hit 60 Hz with no display sync:

```csharp
// SleepLoopRenderTimer.cs:60-66
var now = _st.Elapsed;
var timeTillNextTick = lastTick + _timeBetweenTicks - now;
if (timeTillNextTick.TotalMilliseconds > 1)
    _wakeEvent.WaitOne(timeTillNextTick);
lastTick = now = _st.Elapsed;
_tick?.Invoke(now);
```

I found **no** X Present-extension / GLX_OML_sync / `glXSwapInterval` usage in
`src/Avalonia.X11` (grep for `SwapInterval|glXSwapInterval` returns only `glXSwapBuffers`
at `Glx/Glx.cs:76-77`). So Linux frame pacing relies on the driver's implicit throttle inside
`SwapBuffers`, plus a free-running 60 Hz sleep loop. **This is exactly the "phase drift against
vsync" failure mode**: 16.667 ms wall-clock ticks beating against a 16.667 ms display cadence produce
periodic missed/doubled frames.

### `ThreadProxyRenderTimer` — an important pattern

`src/Avalonia.Base/Rendering/ThreadProxyRenderTimer.cs:20-84`

macOS wraps `CVDisplayLink` in a proxy that does *not* run render work on the display-link callback
thread. The callback only does `_autoResetEvent.Set()` (`:75`); a dedicated thread named
`"RenderTimerLoop"` with a **1 MiB** stack (`:20, 25`) does the actual `_tick?.Invoke(_stopwatch.Elapsed)`
(`:78-84`).

Rationale (inferable, not stated in code): CVDisplayLink callbacks are real-time-ish and must return
fast; blocking them stalls the display server. **UNVERIFIED** whether that was the stated motivation.

---

## 3. Batching and commit — and the latency budget

### Marking dirty

Client-side setters are generated from `src/Avalonia.Base/composition-schema.xml` by
`src/tools/DevGenerators/CompositionGenerator/`. The generated setter body
(`Generator.cs:374-405`) is:

```csharp
// Update the backing value
_offset = value;

// Register object for serialization in the next batch
_changedFieldsOfCompositionVisual |= CompositionVisualChangedFields.Offset;
RegisterForSerialization();

// Reset previous animation if any
PendingAnimations.Remove(s_IdOfOffsetProperty);
_changedFieldsOfCompositionVisual &= ~CompositionVisualChangedFields.OffsetAnimated;
// Check for implicit animations
if(ImplicitAnimations != null && ImplicitAnimations.TryGetValue("Offset", out var animation) == true)
{ ... }
```

`RegisterForSerialization` (`CompositionObject.cs:162-171`) is idempotent per-object per-batch via
`_registeredForSerialization`, and forwards to `Compositor.RegisterForSerialization`
(`Compositor.cs:221-227`) which pushes into a queue + hashset and calls `RequestCommitAsync()`.

The dirty mask is a `[Flags]` enum sized to the property count — `byte`/`ushort`/`uint`/`ulong`
(`Generator.cs:370`). So "which of my 20 properties changed" costs one machine word.

### Serialization format

`Compositor.CommitCore()` (`Compositor.cs:123-197`) writes into a `BatchStreamWriter`. The stream is
**two parallel streams** (`Transport/BatchStream.cs:11-21`):

```csharp
/// - objects: CLR reference types that are references to either server-side or common objects
/// - structs: blittable types like int, Matrix, Color
/// Each "stream" consists of memory segments that are pooled
internal class BatchStreamData
{
    public Queue<BatchStreamSegment<object?[]>> Objects { get; } = new();
    public Queue<BatchStreamSegment<IntPtr>> Structs { get; } = new();
}
```

Struct writes are `Unsafe.WriteUnaligned` into pooled native memory
(`BatchStream.cs:99-114`), with an ARM32 workaround for
[dotnet/runtime#80068](https://github.com/dotnet/runtime/issues/80068) (`:30-31, 106-111`).
Object writes go into pooled `object?[]` (`:116-120`). Both pools are per-`Compositor`
(`Compositor.cs:72-73`) and the `BatchStreamData` container itself is pooled in a static
`ConcurrentBag` (`Transport/Batch.cs:15, 24-26, 59`).

**Net effect: a steady-state scroll frame allocates ~nothing in the transport layer.**

### The commit sequence

```csharp
// Compositor.cs:130-132 — run deferred "update before commit" callbacks (this is where
// CompositingRenderer.Update re-records dirty visuals' draw lists)
(_invokeBeforeCommitRead, _invokeBeforeCommitWrite) = (_invokeBeforeCommitWrite, _invokeBeforeCommitRead);
while (_invokeBeforeCommitRead.Count > 0)
    _invokeBeforeCommitRead.Dequeue()();

// :134-148 — serialize each registered object once
using (var writer = new BatchStreamWriter(_nextCommit.Changes, _batchMemoryPool, _batchObjectPool))
{
    while(_objectSerializationQueue.TryDequeue(out var obj))
    {
        var serverObject = obj.TryGetServer(this);
        if (serverObject != null) { writer.WriteObject(serverObject); obj.SerializeChanges(this, writer); }
    }
    ...
    // :151-158 — deferred disposals ride the same batch
    // :161-176 — arbitrary render-thread jobs (pre-target and post-target markers)
}

_nextCommit.CommittedAt = Server.Clock.Elapsed;   // :179
_server.EnqueueBatch(_nextCommit);                // :180
```

`EnqueueBatch` (`ServerCompositor.cs:66-71`) enqueues under `lock (_batches)` and then
`_renderLoop.Wakeup()` — i.e. it re-arms the timer if the loop had gone idle.

Deserialization on the render thread (`ServerCompositor.ApplyPendingBatches`, `:77-134`) drains the
whole queue, dispatching on sentinel objects (`RenderThreadJobsStartMarker`,
`RenderThreadPostTargetJobsStartMarker`, `RenderThreadDisposeStartMarker` — `:39-43`), otherwise
`((SimpleServerObject)readObject).DeserializeChanges(stream, batch)` (`:112-113`).

There is an opt-in `DEBUG_COMPOSITOR_SERIALIZATION` mode writing magic-guid end markers per object
(`Compositor.cs:143-146`, `ServerCompositor.cs:114-121`) — a nice way to catch write/read schema drift.

### Double throttle (this is the frame pacing)

**Throttle A — `Compositor` level** (`Compositor.cs:90-107`):

```csharp
public CompositionBatch RequestCompositionBatchCommitAsync()
{
    Dispatcher.VerifyAccess();
    if (_nextCommit == null)
    {
        _nextCommit = new ();
        var pending = _pendingBatch;
        if (pending != null)
            pending.Processed.ContinueWith(
                _ => Dispatcher.Post(_triggerCommitRequested, DispatcherPriority.Send),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        else
            _triggerCommitRequested();
    }
    return _nextCommit;
}
```

**Throttle B — `MediaContext` level** (`MediaContext.Compositor.cs:64-82`):

```csharp
private bool CommitCompositorsWithThrottling()
{
    Dispatcher.UIThread.VerifyAccess();
    if (_pendingCompositionBatches.Count > 0)
        return true;                       // previous commit isn't handled yet — skip this frame
    if (_requestedCommits.Count == 0)
        return false;
    foreach (var c in _requestedCommits.ToArray()) CommitCompositor(c);
    return true;
}
```

and the completion callback (`MediaContext.Compositor.cs:41-56`):

```csharp
private void CompositionBatchFinished(Compositor compositor, CompositionBatch batch)
{
    if (_pendingCompositionBatches.TryGetValue(compositor, out var waitingForBatch) && waitingForBatch == batch)
        _pendingCompositionBatches.Remove(compositor);
    if (_pendingCompositionBatches.Count == 0)
    {
        _animationsAreWaitingForComposition = false;
        if (_requestedCommits.Count != 0 || _clock.HasSubscriptions)
            ScheduleRender(false);
    }
}
```

**Exactly one batch is in flight at a time, per compositor.** The UI thread cannot run ahead of the
render thread. That is the pacing mechanism — no explicit frame budget, no dropped-frame counter,
just backpressure.

Note `_animationsAreWaitingForComposition` (`MediaContext.cs:18, 135, 155`): while a batch is
outstanding the **animation clock is not pulsed**. That prevents animations from advancing time for
frames that will never be shown, i.e. it keeps animation progress tied to *presented* frames rather
than to wall clock.

### Latency: UI-thread change → pixels

Best case, steady state, vsync-driven backend:

| Step | Cost |
|---|---|
| property setter → `RegisterForSerialization` → `ScheduleRender` | ~0 (same call stack) |
| `DispatcherOperation` @ `DispatcherPriority.Render` dequeued | next dispatcher drain, sub-ms if the UI thread is free |
| layout + draw-list re-record + serialize + `EnqueueBatch` | one UI frame's work |
| `_renderLoop.Wakeup()` → next timer tick | **0 … 1 vsync** (the batch waits for the next `WaitForVBlank`/`Choreographer`/`CVDisplayLink` callback) |
| deserialize + transform pass + dirty rects + rasterize + present | one render frame's work, then the swapchain's own present latency |

So minimum ≈ **1 vsync + present latency**; typical ≈ 1–2 vsync. Because of the single-batch
backpressure, the pipeline never gets deeper than that: there is no queue of stale frames to drain.

`DispatcherPriority.Render` is **above** `Input` (`Threading/DispatcherPriority.cs:32, 80, 86, 92, 97`):

```
DataBind > AsyncRenderTargetResize > BeforeRender > Render > AfterRender
        > UiThreadRender > Loaded > Default(Send=…) > Input > Background
```

So render preempts input. Avalonia guards against input starvation with an explicit marker
(`MediaContext.cs:88-104`):

```csharp
// Sometimes our animation, layout and render passes might be taking more than a frame to complete
// which can cause a "freeze"-like state when UI is being updated, but input is never being processed
// So here we inject an operation with Input priority to check if Input wasn't being processed
// for a long time. If that's the case the next rendering operation will be scheduled to happen after all pending input
var priority = DispatcherPriority.Render;
if (_inputMarkerOp == null)
{
    _inputMarkerOp = _dispatcher.InvokeAsync(_inputMarkerHandler, DispatcherPriority.Input);
    _inputMarkerAddedAt = _time.Elapsed;
}
else if (!now && (_time.Elapsed - _inputMarkerAddedAt).TotalSeconds > MaxSecondsWithoutInput)
{
    priority = DispatcherPriority.Input;
}
```

`MaxSecondsWithoutInput` defaults to **1 second** (`Threading/DispatcherOptions.cs:20`), with a doc
comment noting it "may need to be lowered on resource-constrained platforms where input events are
processed on the same thread as rendering" (`DispatcherOptions.cs:15-19`). This is a WPF-derived
pattern (WPF's `_inputMarkerOperation`).

---

## 4. Can a `CompositionVisual.Offset` be animated server-side, with no UI-thread involvement?

**Yes. Fully.** This is the strongest architectural difference vs. Uno's current Skia compositor.

### `Offset` is a declared animatable property

`src/Avalonia.Base/composition-schema.xml:24`

```xml
<Property Name="Offset" Type="Vector3D" Animated="true"/>
```

(also `Translation` :25, `Scale` :31, `RotationAngle` :29, `Orientation` :30, `Opacity` :20,
`Size` :26, `AnchorPoint` :27, `CenterPoint` :28, `TransformMatrix` :32, `Visible` :19,
`ClipToBounds` :23.)

For each `Animated="true"` property the generator emits an extra `…Animated` flag bit and a
serialization branch that ships an **`IAnimationInstance` object** instead of a value
(`Generator.cs:487-505`):

```csharp
if((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.OffsetAnimated) == …OffsetAnimated)
    writer.WriteObject(PendingAnimations.GetAndRemove(s_IdOfOffsetProperty));
else if((_changedFieldsOfCompositionVisual & CompositionVisualChangedFields.Offset) == …Offset)
    writer.Write(_offset);
```

and the deserializer (`Generator.cs:524-541`):

```csharp
if((changed & CompositionVisualChangedFields.OffsetAnimated) == …OffsetAnimated)
    SetAnimatedValue(s_IdOfOffsetProperty, ref _offset, committedAt, reader.ReadObject<IAnimationInstance>());
else if((changed & CompositionVisualChangedFields.Offset) == …Offset)
    Offset = reader.Read<Vector3D>();
```

Note `committedAt` — the animation's t=0 is the **UI-thread commit timestamp**
(`Compositor.cs:179` `_nextCommit.CommittedAt = Server.Clock.Elapsed;`, plumbed through
`DeserializeChangesCore(reader, committedAt)` and into `animation.Initialize(committedAt, …)` at
`ServerObjectAnimations.cs:113`). That means an animation started on a laggy UI frame still plays at
the right phase.

### Server-side evaluation loop

`ServerCompositorAnimations.Process()` (`ServerCompositorAnimations.cs:18-36`), called from
`ExecuteGlobalPasses` every render tick:

```csharp
foreach (var animation in _clockItems)  _clockItemsToUpdate.Add(animation);
foreach (var animation in _clockItemsToUpdate) animation.OnTick();   // marks dirty
_clockItemsToUpdate.Clear();

while (_dirtyAnimatedObjectQueue.Count > 0)
{
    var animation = _dirtyAnimatedObjectQueue.Dequeue();
    _dirtyAnimatedObjects.Remove(animation);
    animation.EvaluateAnimations();
}
```

`ServerObjectAnimations.ServerObjectAnimationInstance<T>.UpdateTargetProperty()`
(`ServerObjectAnimations.cs:77-86`) writes straight into the server-side backing field:

```csharp
_property.SetField(Owner._owner, GetVariant().CastOrDefault<T>());
Owner._owner.NotifyAnimatedValueChanged(_property);
Owner.OnSetDirectValue(_property);
```

and `ServerCompositionVisual.NotifyAnimatedValueChanged` (`…Visual.DirtyInputs.cs:89-118`) maps
`s_IdOfOffsetProperty` → `TriggerCombinedTransformDirty()` (`:105-107`), which enqueues the visual
for `RecomputeOwnProperties` and readback update.

`RecomputeOwnProperties` (`…Visual.ComputedProperties.cs:148-156`):

```csharp
if (_combinedTransformDirty)
{
    _ownTransform = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix, Scale,
        RotationAngle, Orientation, Offset + Translation);
    setDirtyForRender = setDirtyBounds = true;
    AttHelper_CombinedTransformChanged();
}
```

**No UI-thread round trip anywhere in that path.**

The render loop keeps ticking while animations exist (`ServerCompositor.cs:279-281`):

```csharp
// Request a tick if we have active animations or if there are recent batches
if (Animations.NeedNextTick || _ticksSinceLastCommit < CommitGraceTicks)
    return true;
```

with `NeedNextTick => _clockItems.Count > 0` (`ServerCompositorAnimations.cs:38`).

### Two animation kinds, both server-side

**`KeyFrameAnimationInstance<T>`** (`Animations/KeyFrameAnimationInstance.cs`) — full WinUI-shaped
keyframe model evaluated at `Compositor.ServerNow`: delay behavior (`:85-91`), iteration count &
`AlternateReverse` direction (`:93-107`), per-keyframe easing (`:143`), interpolation (`:147-151`),
self-removal from the clock when finished (`:72-79`).

**`ExpressionAnimationInstance`** (`Animations/ExpressionAnimationInstance.cs:19-31`) — a parsed
expression re-evaluated per tick with `StartingValue` / `FinalValue` / `CurrentValue` in scope. The
expression language has a real parser, tokenizer, and a `[StructLayout(LayoutKind.Explicit)]`
variant type (`Expressions/ExpressionParser.cs`, `TokenParser.cs`, `ExpressionVariant.cs:32-45`,
`BuiltInExpressionFfi.cs`).

**Dependency tracking**: `AnimationInstanceBase.Initialize` (`:26-51`) resolves the string references
collected from the expression into `(ServerObject, CompositionProperty)` pairs and subscribes
(`Activate()`, `:62-67`). When a tracked property changes, `ServerObjectSubscriptionStore.Invalidate()`
(`ServerObjectAnimations.cs:26-34`) invalidates every dependent animation instance, which re-enqueues
itself into the dirty queue (`AnimationInstanceBase.Invalidate`, `:76-82`). Results are cached per
tick via `IsDirty` (`ServerObjectAnimations.cs:51-62`), with an explicit
"set `IsDirty = false` *before* evaluating to prevent stack overflows due to potential cyclic
references" (`:58-59`).

### Implicit animations

`ImplicitAnimationCollection` (`Animations/ImplicitAnimationCollection.cs`) is a
`Dictionary<string, ICompositionAnimationBase>` on `CompositionObject.ImplicitAnimations`
(`CompositionObject.cs:22`). The generated setter (`Generator.cs:386-403`) checks it on *every*
assignment and, if a matching key exists, converts the direct write into an animation whose
`this.FinalValue` is the value being assigned.

Real use in Avalonia's own controls — pull-to-refresh animates the **scroll content's Offset**
server-side (`src/Avalonia.Controls/PullToRefresh/ScrollViewerIRefreshInfoProviderAdapter.cs:173-188`):

```csharp
var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
offsetAnimation.Target = "Offset";
offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);

var animation = compositor.CreateImplicitAnimationCollection();
animation["Offset"] = offsetAnimation;
scollContentComposition.ImplicitAnimations = animation;
```

So: "UI thread writes a target offset once; the compositor thread interpolates toward it every vsync"
is a supported, in-use pattern.

### But: normal `ScrollViewer` scrolling does **not** use it

`ScrollContentPresenter.ArrangeWithAnchoring` arranges the child at `-Offset`
(`src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs:462, 495, 500`):

```csharp
ArrangeOverrideImpl(size, -Offset);
```

which sets `Layoutable.Bounds` (`Layout/Layoutable.cs:751`), and `BoundsProperty` is registered with
`AffectsRender` (`Visual.cs:140-149`):

```csharp
AffectsRender<Visual>(
    BoundsProperty,
    ClipProperty,
    ClipToBoundsProperty,
    IsVisibleProperty, ...);
```

→ `InvalidateVisual()` → `Renderer.AddDirty(this)` (`Visual.cs:418-421`) → `CompositingRenderer.UpdateCore`
→ `SynchronizeCompositionProperties()` → `comp.Offset = new (Bounds.Left, Bounds.Top, 0)`
(`Visual.Composition.cs:137`).

So an Avalonia scroll is: **arrange one element per frame, re-record one element's draw list, ship one
`Offset` (12 bytes + a flags word), re-rasterize dirty rects.** Descendants keep their local `Bounds`
and are never invalidated — their retained `CompositionRenderData` draw lists survive the scroll
untouched (`CompositingRenderer.cs:150-169` iterates only `_dirty`).

There is an acknowledged inefficiency at `Visual.Composition.cs:135`:

```csharp
// TODO: Introduce a dirty mask like WPF has, so we don't overwrite properties every time
```

— every dirty visual re-writes *all* of its composition properties, so one changed `Bounds` produces
a batch entry with ~10 changed-field bits set. Cheap, but not free.

### Inertia is *not* compositor-side

`ScrollGestureRecognizer` runs fling inertia on the **UI thread**, one step per animation frame
(`src/Avalonia.Base/Input/GestureRecognizers/ScrollGestureRecognizer.cs:205-262`):

```csharp
MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);          // :211, :260
...
private void OnAnimationRequested(TimeSpan _)
{
    Dispatcher.UIThread.InvokeAsync(() =>                                   // :220
    {
        ...
        var speed = inertia * Math.Pow(InertialResistance, (_lastTime - _inertiaStartTime).TotalSeconds);  // :232
        var distance = speed * elapsedSinceLastTick.TotalSeconds;
        Target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, distance));
        ...
    }, DispatcherPriority.Input);                                           // :261
}
```

Constants: `InertialResistance = 0.15` (`:14`) — an **exponential decay of 0.15× per second** —
and `InertialScrollSpeedEnd = 5` px/s termination threshold (`:13`).

`MediaContext.RequestAnimationFrame` (`MediaContext.Clock.cs:44-48`) enqueues into
`_queuedAnimationFrames` and calls `ScheduleRender(false)`; the queue is swapped before draining
(`:56-59`) so a callback re-registering itself doesn't spin within the same pulse.

Important: the inertia step is dispatched at **`DispatcherPriority.Input`** (`:261`), deliberately
below the render pass, with the comment *"This is done asynchronously so we have run the events with
Input priority"* (`:217-218`).

**Velocity estimation is a direct port of Flutter's** — header comment at
`Input/GestureRecognizers/VelocityTracker.cs:1-3`:

```csharp
// Code in this file is derived from
// https://github.com/flutter/flutter/blob/master/packages/flutter/lib/src/gestures/velocity_tracker.dart
```

Constants (`:63-66`): `AssumePointerMoveStoppedMilliseconds = 40`, `HistorySize = 20`,
`HorizonMilliseconds = 100`, `MinSampleSize = 3`; degree-2 weighted least-squares fit via
`LeastSquaresSolver.Solve(2, …)` (`:154-157`), with `stackalloc` buffers (`:104-107`) so the hot path
doesn't allocate. There is even a TODO to add Flutter's iOS/macOS fling-specific trackers (`:9`).

**No input resampling or vsync-phase alignment of pointer events exists.** Coalesced/intermediate
points are surfaced (`Input/Raw/RawPointerEventArgs.cs:125` `Lazy<IReadOnlyList<RawPointerPoint>?> IntermediatePoints`,
`Browser/BrowserInputHandler.cs:76-84` uses `GetCoalescedEvents`) but only for velocity/ink fidelity;
they are not resampled to the frame clock. Grep for `resampl` across `src/` returns only OpenGL
constant names.

---

## 5. Dirty rects and partial repaint during scroll

This is a **port of WPF's dirty-region machinery**, and it's the most reusable part of the design.

### Tracker selection

`ServerCompositionTarget` ctor (`Server/ServerCompositionTarget.cs:54-66`):

```csharp
if (platformRender?.SupportsRegions == true && compositor.Options.UseRegionDirtyRectClipping != false)
{
    var maxRects = compositor.Options.MaxDirtyRects ?? 8;
    DirtyRects = maxRects <= 0
        ? new RegionDirtyRectTracker(platformRender)
        : new MultiDirtyRectTracker(platformRender, maxRects,
            // WPF uses 50K, but that merges stuff rather aggressively
            compositor.Options.DirtyRectMergeEagerness ?? 1000);
}
DirtyRects ??= new SingleDirtyRectTracker();
```

Constants: **8 dirty rects max**, **merge-overhead budget 1000** (WPF uses 50 000 — noted in the
comment as too aggressive). Tunable via `CompositionOptions` (`CompositionOptions.cs:11-33`).

`MultiDirtyRectTracker.CDirtyRegion.cs:9-12` is explicit:

```csharp
/// <summary>
/// This is a port of CDirtyRegion2 from WPF
/// </summary>
class CDirtyRegion2(int MaxDirtyRegionCount)
```

It maintains an `MaxDirtyRegionCount × MaxDirtyRegionCount` **overhead matrix** (`:17`) of the wasted
area that would result from unioning each pair of rects, and merges the cheapest pair when a 9th rect
arrives (`ComputeUnion` `:53-70`, `UpdateOverhead` `:99-110`).

### Dirty-region collection = the `Update` pass

`ServerCompositionTarget.Update()` (`:102-129`) walks the whole visual tree with a
`ServerTreeWalker<UpdateContext>`:

```csharp
var transform = Matrix.CreateScale(Scaling, Scaling);
Root.UpdateRoot(collector, transform, new LtrbRect(0, 0, PixelSize.Width, PixelSize.Height));
```

`ServerCompositionVisual.Update.cs` implements a near-verbatim WPF algorithm. The cheatsheet WPF table
is even reproduced in comments at `ServerCompositionVisual.ComputedProperties.cs:61-89`.

Key mechanics:

- **Old + new bounds are both added.** `PreSubgraph` (`Update.cs:74-90`) adds the *pre-update*
  `_transformedSubTreeBounds`; `PostSubgraph` (`:163-167`) adds the *post-update* one:

```csharp
// PreSubgraph
if (node._isDirtyForRender || node is { _isDirtyForRenderInSubgraph: true, HasEffect: true })
{
    if (node._needsBoundingBoxUpdate && !AreDirtyRegionsDisabled())
        AddToDirtyRegion(node._transformedSubTreeBounds);   // OLD bounds
    _dirtyRegionDisableCount++;
}
...
// PostSubgraph
if(node._isDirtyForRender || node is { _isDirtyForRenderInSubgraph: true, Effect: not null })
{
    _dirtyRegionDisableCount--;
    AddToDirtyRegion(node._transformedSubTreeBounds);       // NEW bounds
}
```

  For a scroll this means old-viewport ∪ new-viewport = the whole viewport. That is correct and
  unavoidable for a translation of visible content.

- **Descendant suppression.** Once an ancestor's bbox has been collected, `_dirtyRegionDisableCount`
  suppresses descendants (`AddToDirtyRegion` early-outs at `:204`), so a scroll produces **one** rect,
  not one per child. Comment at `Update.cs:87-88`: *"If we added a node in the parent chain to the
  bbox we don't need to add anything below this node to the dirty region."*

- **Subtree pruning.** `PreSubgraph` sets `visitChildren = node._isDirtyForRenderInSubgraph || node._needsBoundingBoxUpdate`
  (`Update.cs:64`). Clean subtrees are never walked at all. The flags are pushed up the parent chain
  by `PropagateFlags` (`ComputedProperties.cs:90-111`), which short-circuits as soon as an ancestor
  already carries the flag:

```csharp
while (parent != null &&
       ((needsBoundingBoxUpdate && !parent._needsBoundingBoxUpdate) ||
        (setIsDirtyForRenderInSubgraph && !parent._isDirtyForRenderInSubgraph)))
{ ... parent = parent.Parent; }
```

- **Bounds are recomputed bottom-up only where needed** (`Update.cs:108-114, 121-138`), unioning
  child `_transformedSubTreeBounds` into the parent's `_subTreeBounds`.

- **Reparenting is handled** — `OnParentChanging` adds the outgoing child's bounds to the old parent
  as an "extra dirty rect" (`DirtyInputs.cs:150-155`).

### Partial repaint = the `Render` pass

`ServerCompositionTarget.Render()` (`:131-256`):

```csharp
if (DirtyRects.IsEmpty && !_redrawRequested && !_updateRequested)   // :173  — nothing at all: bail
    return;
_redrawRequested |= !DirtyRects.IsEmpty;
if (!_redrawRequested) return;
...
var needLayer = _overlays.RequireLayer
                || !(_renderTarget.Properties.RetainsPreviousFrameContents
                     && _renderTarget.Properties.IsSuitableForDirectRendering);      // :187-190
...
if (_fullRedrawRequested || (!needLayer && !properties.PreviousFrameIsRetained))     // :211
{ _fullRedrawRequested = false; fullRedraw = true; }
if (fullRedraw) { DirtyRects.Initialize(renderBounds); DirtyRects.AddRect(renderBounds); }
if (!DirtyRects.IsEmpty)
{
    DirtyRects.FinalizeFrame(renderBounds);                                          // :226
    ... RenderRootToContextWithClip(renderTargetContext, Root);
}
```

**Partial repaint is only possible when the platform render target retains the previous frame's
contents** (`RetainsPreviousFrameContents` + `IsSuitableForDirectRendering`). Otherwise Avalonia
draws into an offscreen layer and blits it (`:227-242`), which preserves correctness on
swapchains that don't preserve backbuffers.

`MultiDirtyRectTracker.FinalizeFrame` (`:28-45`) inflates each rect by **1 px** for anti-aliasing
before pushing them as a platform region:

```csharp
var inflated = rect.Inflate(new(1)).IntersectOrEmpty(bounds);
_inflatedRects.Add(inflated);
_clipRegion.AddRect(LtrbPixelRect.FromRectUnscaled(inflated));
combined = LtrbRect.FullUnion(combined, inflated);
```

`BeginDraw` pushes the **multi-rect region** as a clip (`:47-51` → `ctx.PushClip(_clipRegion)`), so
Skia's own scissor/clip culling applies.

### Per-visual culling during the render walk

`ServerCompositionVisual.Render.cs:59-120` — `HandlePreGraphTransformClipOpacity` rejects a subtree if:

```csharp
if (!visual.Visible || visual._transformedSubTreeBounds == null) return false;      // :61
var effectiveOpacity = visual.Opacity * _opacity;
if (effectiveOpacity <= 0.003) return false;                                        // :64  (~1/255)
...
var worldBounds = visual._transformedSubTreeBounds.Value.TransformToAABB(_walkContext.Transform);
if (!effectiveClip.Intersects(worldBounds)
    || _dirtyRects?.Intersects(worldBounds) == false)                               // :85-87
    return false;
```

`_dirtyRects.Intersects` walks the ≤8 inflated rects (`MultiDirtyRectTracker.cs:55-64`). So during a
scroll of a long list, off-viewport realized items are rejected by bounds before any drawing.

Also, the render context intersects the incoming clip with the combined dirty rect up front
(`Render.cs:35-41`):

```csharp
if (dirtyRects != null)
{
    var dirtyClip = dirtyRects.CombinedRect;
    if (dirtyRects is SingleDirtyRectTracker) dirtyRects = null;
    clip = clip.IntersectOrEmpty(dirtyClip);
}
```

### Retained draw lists

Leaf content is `CompositionDrawListVisual.DrawList` — a `CompositionRenderData` recorded on the UI
thread and referenced by the server (`CompositionDrawListVisual.cs:23-35`,
`Server/ServerCompositionDrawListVisual.cs:31-41`). `RenderCore` is just
`_renderCommands?.Render(context.Canvas)` (`ServerCompositionDrawListVisual.cs:44-47`). A pure offset
change never touches `DrawList`.

### Optional bitmap cache

`CacheMode` → `ServerCompositionVisualCache` (`Server/ServerCompositionVisual/ServerCompositionVisualCache.cs`)
renders a subtree into an offscreen bitmap in **local space** (`_drawAtOffset` at `:135`,
transform applied at `:200`) with its own nested dirty-rect collector
(`Update.cs:30-55` `PushCacheIfNeeded`/`PopCacheIfNeeded`). This is opt-in per element
(`Visual.Composition.cs:147-149`), not automatic.

### Debug overlays

`RendererDebugOverlays` (`Rendering/RendererDebugOverlays.cs:9-35`): `Fps`, `DirtyRects`,
`LayoutTimeGraph`, `RenderTimeGraph`. `CompositionTargetOverlays` (`Server/CompositionTargetOverlays.cs`)
maintains four `FrameTimeGraph`s — Layout, Render, `GUpdate` (global compositor passes),
`TUpdate` (per-target update) — each created with a **16.67 ms reference line**
(`CompositionTargetOverlays.cs:66` `new FrameTimeGraph(360, new Size(360.0, 64.0), 1000.0 / 60.0, title, …)`).
`MultiDirtyRectTracker.Visualize` (`:75-81`) paints each frame's region in a random colour.

---

## 6. Explicit frame pacing / catch-up / dropped frames

Avalonia does **not** implement frame-budget accounting, timestamp extrapolation, or catch-up. Its
pacing is entirely emergent from backpressure. The concrete mechanisms:

### (a) Overlapping ticks are dropped, never queued

`DefaultRenderLoop.TimerTick` (`RenderLoop.cs:121-179`):

```csharp
private void TimerTick(TimeSpan time)
{
    if (Interlocked.CompareExchange(ref _inTick, 1, 0) == 0)
    {
        ...
    }   // no else — a tick arriving while one is in progress is silently discarded
}
```

If a frame takes >1 vsync, the intervening vsync tick is simply lost. No accumulation, no burst
catch-up. **This is the right default for smoothness** — catch-up bursts are what produce visible
"jump then stall".

### (b) The render loop sleeps when there is nothing to do

`IRenderLoopTask.Render()` returns a `bool wantsNextTick`. `DefaultRenderLoop`
(`RenderLoop.cs:143-169`):

```csharp
var wantsNextTick = false;
for (int i = 0; i < _itemsCopy.Count; i++)
    wantsNextTick |= _itemsCopy[i].Render();
if (!wantsNextTick)
{
    lock (_timerLock)
    {
        if (!_running) { }
        else if (_wakeupPending) { _wakeupPending = false; }
        else { _running = false; _timer.Tick = null; }   // stop the platform timer entirely
    }
}
```

`ServerCompositor.RenderCore` decides (`ServerCompositor.cs:279-289`):

```csharp
// Request a tick if we have active animations or if there are recent batches
if (Animations.NeedNextTick || _ticksSinceLastCommit < CommitGraceTicks)
    return true;
// Request a tick if we had unready targets in the last tick, to check if they are ready next time
foreach (var target in _activeTargets)
    if (target.IsWaitingForReadyRenderTarget)
        return true;
// Otherwise there is no need to waste CPU cycles, tell the timer to pause
return false;
```

`CommitGraceTicks = 10` (`ServerCompositor.cs:49`); `_ticksSinceLastCommit` resets whenever a batch
arrives (`:130-133`). **Ten grace ticks (~166 ms at 60 Hz)** of hysteresis before the loop parks —
so a scroll never pays the wake-up cost mid-gesture.

Wake-up is `_renderLoop.Wakeup()` from `EnqueueBatch` (`ServerCompositor.cs:70`), which re-arms
under `_timerLock` with a `_wakeupPending` flag so a wake racing with a tick is not lost
(`RenderLoop.cs:105-119, 127-135`).

### (c) UI-thread animation fallback timer

When the render loop is *not* the thing driving animations (i.e. a UI-thread animation exists but no
composition batch is outstanding), `MediaContext` uses a `DispatcherTimer` at
`DispatcherPriority.Render` with a **hardcoded 16 ms** interval (`MediaContext.cs:30-36`):

```csharp
private readonly DispatcherTimer _animationsTimer = new(DispatcherPriority.Render)
{
    // Since this timer is used to drive animations that didn't contribute to the previous frame at all
    // We can safely use 16ms interval until we fix our animation system to actually report the next expected
    // frame
    Interval = TimeSpan.FromMilliseconds(16)
};
```

Started only when composition is *not* going to call back (`MediaContext.cs:156-157`):

```csharp
if (!_animationsAreWaitingForComposition && _clock.HasSubscriptions)
    _animationsTimer.Start();
```

This is an acknowledged approximation ("until we fix our animation system to actually report the next
expected frame").

### (d) `UiThreadRenderTimer` self-corrects its interval

For the render-on-UI-thread mode (`UiThreadRenderTimer.cs:42-61`):

```csharp
private void OnTick(object? sender, EventArgs e)
{
    var tickedAt = _parent._clock.Elapsed;
    var nextTickAt = tickedAt + Interval;
    try { _tick(tickedAt); }
    finally
    {
        var afterTick = _parent._clock.Elapsed;
        var interval = nextTickAt - afterTick;
        if (interval < s_minInterval)
            // We are way overdue, but shouldn't cause starvation in other areas
            interval = s_minInterval;
        _timer.Interval = interval;
    }
}
private static readonly TimeSpan s_minInterval = TimeSpan.FromMilliseconds(1);
```

A deadline-based reschedule with a 1 ms floor — no burst catch-up, but no cumulative drift either.

### (e) Layout-storm guards

`MediaContext.RenderCore` re-runs the pre-render callback loop at most **10 times** for animations
started during layout (`MediaContext.cs:140`), and `FireInvokeOnRenderCallbacks` throws
`"Infinite layout loop detected"` after **153** iterations (`MediaContext.cs:196-198`) — the same
magic number WPF uses. These bound the worst-case UI frame time rather than letting a pathological
layout wedge the frame.

### (f) What is missing

- No frame timestamp / expected-present-time is propagated to animations; animations sample
  `Compositor.ServerNow` = `Stopwatch.Elapsed` at the *start* of `RenderCore`
  (`ServerCompositor.cs:29-30, 73, 253`). No vsync phase or predicted present time.
- No dropped-frame counter or telemetry outside the debug overlays.
- No input resampling to the frame clock.
- X11 has no vsync source at all (§2).

---

## 7. Concrete lessons for Uno's Skia compositor

Grounding for the Uno side comes from `specs/scroll-smoothness/research/14-orchestrator-firsthand.md`
in this worktree, and from spot checks:
`src/Uno.UI.Composition/Composition/Compositor.skia.cs:199-263`,
`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:120-176`.

Uno's current shape: a **record/replay** split where the UI thread walks the tree, ticks animations
inline, and records an `SKPicture`; the platform thread replays it. Avalonia's shape: a
**client/server** split where the UI thread ships a *property diff* and the render thread owns
transforms, animations, culling and rasterization.

### 7.1 Adopt single-batch backpressure as the frame pacer — **highest value, lowest risk**

Avalonia's `MediaContext.CommitCompositorsWithThrottling` (`MediaContext.Compositor.cs:64-82`) plus
`Compositor.RequestCompositionBatchCommitAsync`'s pending-batch chaining (`Compositor.cs:96-103`)
means the UI thread produces exactly one frame per consumed frame. Uno's `RequestNewFrame` →
`host.InvalidateRender()` → `OnNativePlatformFrameRequested` → `EnqueueRender` path
(`CompositionTarget.RenderScheduling.skia.cs:106-118, 166-176`) has already grown ad-hoc state
(`_renderedAheadOfTime`, `_renderRequestedAfterAheadOfTimePaint`, `_shouldEnqueueRenderOnNextNativePlatformFrameRequested`).
Replacing that with one invariant — *"never start recording frame N+1 until frame N has been
consumed by the platform"* — collapses that state machine and gives deterministic pacing.

Corollary worth copying verbatim: **do not advance the animation clock for frames that will not be
presented** (`MediaContext.cs:135` `if (!_animationsAreWaitingForComposition) _clock.Pulse(now);`).

### 7.2 Move the transform/offset property to a server-side animatable slot

Uno today animates scroll via `Visual.AnchorPoint` keyframes evaluated on the UI thread inside the
picture record (`Compositor.skia.cs:206-208`). Avalonia's `Offset` is `Animated="true"` in the schema
(`composition-schema.xml:24`) and its interpolation happens in `ServerCompositorAnimations.Process()`
on the render thread (`ServerCompositorAnimations.cs:18-36`).

Even without a full second thread, the *structural* win is separable: make offset a value the
**frame-consumption side** interpolates, so a slow UI frame does not stall the scroll. Avalonia's
`committedAt` timestamp (`Compositor.cs:179` → `ServerObjectAnimations.cs:113`) is the piece that
makes animations phase-correct even when the producing frame was late — copy that.

### 7.3 Port WPF's `CDirtyRegion2` (Avalonia already did the work)

`MultiDirtyRectTracker.CDirtyRegion.cs` is a self-contained, dependency-free 348-line port with an
overhead matrix and a tunable merge budget (default max 8 rects, overhead 1000 —
`ServerCompositionTarget.cs:58-63`). Uno's damage is currently a single `SKPath`
(`Compositor.RenderRootVisual(canvas, rootVisual, SKPath? damage)`), which for scroll is fine but for
mixed scroll+cursor+focus-visual frames over-invalidates. The multi-rect region also feeds Skia's
clip directly (`MultiDirtyRectTracker.cs:47-51`), so the culling is free at raster time.

Pair it with Avalonia's per-visual dirty-rect cull in the walk
(`ServerCompositionVisual.Render.cs:85-87`) so off-viewport realized list items are rejected before
`RenderCore`.

### 7.4 Replace the O(subtree) `SetMatrixDirty` walk with WPF-style flag propagation

Uno's `ContainerVisual.SetMatrixDirty` recurses into every child per offset change. Avalonia's
`PropagateFlags` (`ServerCompositionVisual.ComputedProperties.cs:90-111`) walks **up** the parent
chain and **stops as soon as an ancestor already has the flag**; the descendant walk then happens
once per frame in `Update`, pruned by `visitChildren = node._isDirtyForRenderInSubgraph || node._needsBoundingBoxUpdate`
(`Update.cs:64`). Cost goes from O(subtree) per delta to O(depth) per delta + O(dirty subtree) per
frame.

The dirty *inputs* are also classified rather than blanket-invalidated
(`ServerCompositionVisual.DirtyInputs.cs:10-62`): `CombinedTransformFieldsMask`,
`ClipSizeDirtyMask`, `OwnBoundsUpdateFieldsMask`, `ReadbackDirtyMask` — so an `Offset` change marks
transform + readback dirty and **nothing else**. That directly answers the two TODOs in
`Compositor.skia.cs:260-261` ("only invalidate matrix when specific properties are changed" /
"only repaint when dependent properties are changed").

### 7.5 Adopt the two-slot MVCC readback for hit-testing

`ServerCompositionVisual.Readback.cs` + `ReadbackIndices.cs`. Small (140 lines total), well
commented, and removes any need for the input thread to synchronize with the render thread when the
compositor eventually goes async. Worth adding *before* the threading split, so hit-testing is
already reading a snapshot rather than live state.

### 7.6 Copy the input-starvation marker

`MediaContext.cs:88-104` + `DispatcherOptions.InputStarvationTimeout` (default 1 s,
`DispatcherOptions.cs:20`). Uno runs its render enqueue on `NativeDispatcher`; if render work is
prioritized over input, a heavy scroll frame can starve the very pointer events that drive it.
Avalonia's marker-operation trick detects this with one extra dispatcher op per frame and demotes
render priority until input drains. Cheap insurance.

### 7.7 Copy the idle-parking hysteresis, not just the idle-parking

Uno's `RequestNewFrame` self-sustaining loop (`Compositor.skia.cs:250-255`) runs while animations
exist. Avalonia adds `CommitGraceTicks = 10` (`ServerCompositor.cs:49, 280`) so the loop keeps
spinning for ~166 ms after the last change. During a wheel-scroll burst with gaps between detents,
this avoids repeated timer teardown/restart — which on Choreographer/CVDisplayLink costs a full frame
of latency each time.

### 7.8 Adopt Avalonia's velocity tracker wholesale

`Input/GestureRecognizers/VelocityTracker.cs` is already a Flutter port in C#, MIT-compatible, with
`stackalloc` hot path, 20-sample history, 100 ms horizon, degree-2 weighted least squares
(`:63-66, 104-107, 154-157`). This is a strictly better replacement for Uno's first-vs-last two-point
estimate in `GestureRecognizer.Manipulation.cs:462-467`, and it requires no architectural change.

### 7.9 Fix the frame source only where it is actually broken

Avalonia's per-platform table (§2) shows that a correct vsync source is table stakes but not
sufficient — X11 (`SleepLoopRenderTimer(60)`) is the one backend with no display sync, and it is the
one where Avalonia scrolling is reported worst. **UNVERIFIED** as a causal claim (I read only the
code, not user reports). For Uno the analogous risk identified in the firsthand note is Android's
`_handler.Post` scheduling *phase*, not the source. Avalonia's `ChoreographerTimer` re-posts the next
frame callback **before** signalling the render thread (`ChoreographerTimer.cs:106-115`), which keeps
the callback cadence independent of render duration — worth mirroring.

### 7.10 Instrument like Avalonia does

Four separate `FrameTimeGraph`s against a 16.67 ms reference (`CompositionTargetOverlays.cs:66`) —
Layout / Render / global-compositor-update / per-target-update — plus a random-colour dirty-rect
visualizer (`MultiDirtyRectTracker.cs:75-81`). Uno already has `SkiaRenderHelper.FpsHelper` with
dropped/unpresented frame counters; adding the *phase-separated* time graphs is what turns "it feels
janky" into "the layout pass is the 22 ms".

### Things **not** to copy

- The `// TODO: Introduce a dirty mask like WPF has, so we don't overwrite properties every time`
  at `Visual.Composition.cs:135` — Avalonia re-writes every composition property of every dirty
  visual each frame. Uno should do the property-level diff from the start.
- `SleepLoopRenderTimer` as any kind of default.
- Driving fling inertia from the UI thread through `Dispatcher.InvokeAsync`
  (`ScrollGestureRecognizer.cs:220, 261`) — this is Avalonia's weakest scroll path and is exactly
  what Uno should *not* replicate; the whole point of the server-side animation infrastructure
  (§4) is that inertia could live there.
- The 16 ms hardcoded animation fallback timer (`MediaContext.cs:30-36`), by Avalonia's own comment.

---

## Appendix A — file inventory (lines read)

```
src/Avalonia.Base/Rendering/
  RenderLoop.cs                      182
  IRenderTimer.cs                     29
  DefaultRenderTimer.cs               80
  UiThreadRenderTimer.cs              72
  SleepLoopRenderTimer.cs             70
  ThreadProxyRenderTimer.cs           85
  SwapchainBase.cs                    89
  RendererDebugOverlays.cs            35
  Composition/
    Compositor.cs                    345
    Compositor.Factories.cs           43
    CompositionObject.cs             184
    CompositionTarget.cs             100
    CompositionOptions.cs             33
    CompositingRenderer.cs           272
    CompositionDrawListVisual.cs      68
    CompositionPropertySet.cs        148
    CompositionCustomVisualHandler.cs 112
    ElementCompositionPreview.cs      35
    Visual.cs                        106
    Animations/{AnimationInstanceBase,CompositionAnimation,ExpressionAnimation,
                ExpressionAnimationInstance,ImplicitAnimationCollection,
                KeyFrameAnimationInstance,PropertySetSnapshot}.cs
    Server/
      ServerCompositor.cs            320
      ServerCompositor.Passes.cs      73
      ServerCompositionTarget.cs     314
      ServerCompositorAnimations.cs   45
      ServerObjectAnimations.cs      174
      ServerCustomCompositionVisual.cs 93
      ReadbackIndices.cs              37
      FpsCounter.cs                   68
      CompositionTargetOverlays.cs   185
      DrawingContextProxy.cs         334 (+ .PendingCommands.cs 168)
      DirtyRects/{IDirtyRectTracker,MultiDirtyRectTracker,
                  MultiDirtyRectTracker.CDirtyRegion,SingleDirtyRectTracker}.cs
      ServerCompositionVisual/{,.ComputedProperties,.DirtyInputs,.Readback,.Render,.Update}.cs
    Transport/{Batch,BatchStream}.cs
src/Avalonia.Base/Media/{MediaContext,MediaContext.Compositor,MediaContext.Clock}.cs
src/Avalonia.Base/Visual.cs, Visual.Composition.cs, Layout/Layoutable.cs, Layout/LayoutManager.cs
src/Avalonia.Base/Threading/{DispatcherPriority,DispatcherOptions}.cs
src/Avalonia.Base/Input/GestureRecognizers/{ScrollGestureRecognizer,VelocityTracker}.cs
src/Avalonia.Base/composition-schema.xml
src/tools/DevGenerators/CompositionGenerator/{Generator,Config,CompositionRoslynGenerator}.cs
src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs
src/Avalonia.Controls/PullToRefresh/{RefreshVisualizer,ScrollViewerIRefreshInfoProviderAdapter}.cs
src/Windows/Avalonia.Win32/{Win32Platform.cs, DirectX/DxgiConnection.cs,
                            DComposition/DirectCompositionConnection.cs,
                            WinRT/Composition/WinUiCompositorConnection.cs}
src/Avalonia.X11/X11Platform.cs
src/Avalonia.Native/{AvaloniaNativePlatform.cs, AvaloniaNativeRenderTimer.cs}
native/Avalonia.Native/src/OSX/PlatformRenderTimer.mm
src/Android/Avalonia.Android/{AndroidPlatform.cs, ChoreographerTimer.cs}
src/iOS/Avalonia.iOS/{Platform.cs, DisplayLinkTimer.cs}
src/Browser/Avalonia.Browser/Rendering/{BrowserRenderTimer,BrowserSharedRenderLoop,RenderWorker}.cs
src/Browser/Avalonia.Browser/webapp/modules/avalonia/timer.ts
```

## Appendix B — magic numbers, one table

| Constant | Value | Where |
|---|---|---|
| `CommitGraceTicks` | 10 ticks | `ServerCompositor.cs:49` |
| Default `MaxDirtyRects` | 8 | `ServerCompositionTarget.cs:58` |
| Default `DirtyRectMergeEagerness` | 1000 (WPF: 50 000) | `ServerCompositionTarget.cs:63` |
| Dirty-rect AA inflation | 1 px | `MultiDirtyRectTracker.cs:38` |
| Opacity cull threshold | 0.003 | `ServerCompositionVisual.Render.cs:64` |
| `InputStarvationTimeout` | 1 s | `DispatcherOptions.cs:20` |
| Animation fallback timer | 16 ms | `MediaContext.cs:35` |
| Layout re-run cap (animations) | 10 | `MediaContext.cs:140` |
| Layout-storm abort | 153 callbacks | `MediaContext.cs:197` |
| `UiThreadRenderTimer` min interval | 1 ms | `UiThreadRenderTimer.cs:61` |
| Fallback render timer FPS | 60 | `Win32Platform.cs:90`, `X11Platform.cs:77-78` |
| `ThreadProxyRenderTimer` stack | 1 MiB | `ThreadProxyRenderTimer.cs:20` |
| WinUI commit watchdog | 1 s | `WinUiCompositorConnection.cs:141, 156` |
| `InertialResistance` (decay/s) | 0.15 | `ScrollGestureRecognizer.cs:14` |
| `InertialScrollSpeedEnd` | 5 px/s | `ScrollGestureRecognizer.cs:13` |
| VelocityTracker history / horizon | 20 samples / 100 ms | `VelocityTracker.cs:64-65` |
| VelocityTracker fit degree / min samples | 2 / 3 | `VelocityTracker.cs:66, 154` |
| `AssumePointerMoveStoppedMilliseconds` | 40 ms | `VelocityTracker.cs:63` |
| Frame-time-graph reference line | 1000/60 ms | `CompositionTargetOverlays.cs:66` |
| Android Choreographer/Render thread priority | `AboveNormal` | `ChoreographerTimer.cs:30, 35` |
