# WinUI / dxaml: input → screen pipeline (non-ScrollViewer parts)

**Source root for every citation below:** `D:/Work/microsoft-ui-xaml2/` — paths are written relative to that root, e.g. `dxaml/xcp/core/compositor/compositorscheduler.cpp:303`.

Everything in this note was read out of the actual source tree. Where a claim could not be verified from source, it is explicitly marked **UNVERIFIED**.

---

## 0. Executive summary — the five things that make WinUI scrolling smooth

1. **A dedicated high-priority scheduling thread, separate from the UI thread**, owns the frame clock. It is the *only* thing that decides when the UI thread is allowed to produce a frame, and it throttles that decision to the display's actual refresh interval using `DCompositionWaitForCompositorClock`. `dxaml/xcp/core/compositor/compositorscheduler.cpp:260-436`, `dxaml/xcp/components/graphics/Scheduler.cpp:24-56`, `dxaml/xcp/core/compositor/RefreshRateInfo.cpp:92-190`.
2. **The XAML "render thread" does not render.** It ticks a clock, decides whether to wake the UI thread, and sleeps. All actual composition/animation evaluation happens out of process in the system compositor (DComp/IXP). `dxaml/xcp/core/compositor/compositorscheduler.cpp:303-436` contains no draw call of any kind.
3. **Panning is expressed as a WUC ExpressionAnimation bound to a DirectManipulation-owned shared transform.** Once the expression is committed, every pan/inertia frame is produced by the compositor with *zero* UI-thread involvement. `dxaml/xcp/core/hw/DManipData.cpp:152-183`, `dxaml/xcp/core/hw/ManipulationTransform.cpp:82-137`, `dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp:2306-2390`.
4. **Ticking is interleaved with input at equal priority**, via a `DispatcherQueueTimer` with `Interval = 0`, dispatching exactly **one** queued XAML message per callback and then re-arming. This prevents both input starvation and tick starvation. `dxaml/xcp/win/shared/xcpwindow.cpp:312-320` and `:1213-1262`.
5. **Everything in a tick is guarded by dirty flags and early-outs**: `CLayoutManager::UpdateLayout` returns immediately if nothing requires layout (`dxaml/xcp/core/layout/LayoutManager.cpp:239-242`), the render walk is skipped entirely unless `NWNeedsRendering()` (`dxaml/xcp/core/dll/xcpcore.cpp:6647-6657`), and the DComp `Commit()` is skipped unless something actually changed (`dxaml/xcp/core/dll/xcpcore.cpp:6736-6742`).

---

## 1. The frame loop

### 1.1 Two threads, two schedulers

| Object | Thread | File |
|---|---|---|
| `CompositorScheduler` | its own thread ("render thread"/scheduling thread) | `dxaml/xcp/core/compositor/compositorscheduler.{h,cpp}` |
| `Scheduler` | called *on* the compositor thread | `dxaml/xcp/components/graphics/Scheduler.{h,cpp}` |
| `RefreshAlignedClock` | shared, written by compositor thread, read by both | `dxaml/xcp/core/compositor/RefreshAlignedClock.{h,cpp}` |
| `RefreshRateInfo` | shared, written by UI thread, read by compositor thread | `dxaml/xcp/core/compositor/RefreshRateInfo.{h,cpp}` |
| `UIThreadScheduler` | state machine shared by both threads | `dxaml/xcp/core/compositor/UIThreadScheduler.cpp`, `dxaml/xcp/core/inc/UIThreadScheduler.h` |
| `CXcpDispatcher` | UI thread message pump adapter | `dxaml/xcp/win/shared/xcpwindow.cpp` |

The compositor thread is created in `CompositorScheduler::Startup()` (`compositorscheduler.cpp:118-142`) and immediately raised to real-time priority:

```cpp
// compositorscheduler.cpp:260-266
XINT32 CompositorScheduler::RenderThreadMain()
{
    // Run the render thread at high priority
    IGNOREHR(gps->ThreadSetPriority(NULL, PAL_THREAD_PRIORITY_REAL_TIME));
```

The loop is a bare `while (!m_fShutdownThread) { RenderThreadFrame(); }` (`compositorscheduler.cpp:274-277`).

### 1.2 What drives ticks

`CompositorScheduler::RenderThreadFrame()` (`compositorscheduler.cpp:303-436`) is the heart:

1. Take `m_pDrawListsLock` (`:312`).
2. **Advance the shared clock**: `const XDOUBLE frameTickTime = m_pClock->Tick();` (`:317`). Comment at `:319-322` explains ordering: the clock must be advanced *before* queuing the UI tick "so that the UI thread sees up-to-date values. In virtualization scenarios, if the UI thread sees an 'old' value, then it may choose to sleep until the next vsync rather than generating new content."
3. Ask the `UIThreadScheduler` how long until the UI thread wants a frame: `GetScheduledIntervalInMilliseconds(frameTickTime)` (`:330`).
   * `> 0` → just set the sleep timeout (`:337-341`). No UI tick.
   * `== 0` → **throttle to vblank** via `m_scheduler->OnImmediateUIThreadFrame()` (`:345`), then `m_pUIThreadSchedulerNoRef->QueueTick()` (`:348`).
4. Sleep: `gps->WaitForObjects(1, &m_pCompositorWait, TRUE, timeToNextWorkInMilliseconds)` (`:418-423`). Timeout `XINFINITE` means "no scheduled work; only wake on `WakeCompositionThread()`", and that state is published via `m_waitingForWork` so the UI thread can query idleness (`:410-429`, `IsIdle()` at `:450-458`).

So the driving clock is: **vsync (via the DComp compositor clock) gates how *fast*; the UI thread's own frame requests gate *whether***. There is no free-running timer.

### 1.3 The vsync wait — exact mechanism and constants

`Scheduler::OnImmediateUIThreadFrame()` (`components/graphics/Scheduler.cpp:24-56`) contains the frame-pacing policy, and the comment is worth quoting in full because it is the whole battery-vs-smoothness tradeoff:

```cpp
// Scheduler.cpp:26-42
// We need a frame immediately. We'll also throttle the UI thread here. Generally, there's no reason
// to tick or render faster than the screen can display the results. The correct way to do this is
// to throttle to the refresh rate, but that comes at a performance cost. Calling WaitForVBlank (in
// our case, via WaitForCompositorClock) prevents the display from going into a low-power state, so
// it's even more expensive than just keeping the CPU busy. We try to not call WaitForVBlank if we
// don't have to.
//
// We'll compare QPC times first. If it's been more than a whole vblank since the previous frame,
// let the UI thread go now. Otherwise we'll take the battery life hit and go through WaitForVBlank.
```

The predicate is one line (`Scheduler.cpp:19-22`):

```cpp
bool Scheduler::ShouldWaitForVBlank(float currentTimeInMilliseconds, float previousFrameTimeInMilliseconds, float refreshIntervalInMilliseconds)
{
    return (currentTimeInMilliseconds - previousFrameTimeInMilliseconds) < refreshIntervalInMilliseconds;
}
```

`RefreshRateInfo::WaitForRefreshInterval()` (`RefreshRateInfo.cpp:142-190`) calls, in order:

* `DCompositionWaitForCompositorClock(0 /*count*/, dummyHandles, 80 /*timeout ms*/)`, dynamically resolved from `dcomp.dll` (`RefreshRateInfo.cpp:78-90`, `:109-112`). Constant: `static constexpr DWORD c_vBlankWaitTimeoutMs = 80;` (`:69`), documented as "default 80ms timeout is used internally by `DxgkWaitForVerticalBlankEvent2`".
  * Comment at `:106-108`: "We want to wait for the compositor clock instead of `WaitForVBlank` because it also responds to framerate boosting." — i.e. this is the API that makes XAML follow variable/boosted refresh rates.
  * `STATUS_GRAPHICS_PRESENT_OCCLUDED` (0xC01E0006) → treated as "display powered off", fall back (`:114-120`).
* Fallback: `Sleep(static_cast<uint32_t>(refreshIntervalInMilliseconds));` (`RefreshRateInfo.cpp:185`).

The refresh interval itself is **not** guessed; it comes from DComp frame statistics, pushed from UI thread → `RefreshRateInfo` after every commit (`components/comptree/DCompTreeHost.cpp:1105-1130`):

```cpp
DCOMPOSITION_FRAME_STATISTICS frameStatistics;
HRESULT hr = m_dcompDevice->GetFrameStatistics(&frameStatistics);
...
float refreshIntervalInMilliseconds =
    1000.0f * (float)frameStatistics.currentCompositionRate.Denominator / (float)frameStatistics.currentCompositionRate.Numerator;
m_refreshRateInfo->SetRefreshIntervalInMilliseconds(refreshIntervalInMilliseconds);
```

Default before the first commit: `const XFLOAT DefaultRefreshIntervalInMilliseconds = 1000.0f / 60.0f;` (`dxaml/xcp/pal/inc/palgfx.h:105`).

`UpdateRefreshRate()` is called from `DCompTreeHost::CommitMainDevice()` (`DCompTreeHost.cpp:1078-1092`), i.e. once per submitted frame.

### 1.4 The shared clock (`RefreshAlignedClock`)

Design intent is documented at `core/compositor/RefreshAlignedClock.h:8-35`:

> "A clock that is shared between the scheduling thread and the UI thread. This clock is backed by QueryPerformanceCounter, but rather than having every 'what time is it?' question go to QPC, it caches the current time and only updates it from the scheduling thread. Whenever the UI thread asks for the current time, it gets a consistent answer… otherwise timers and animations may not all be in sync."

* `Tick()` (`RefreshAlignedClock.cpp:74-88`) — compositor thread only; snaps the new time.
* `GetLastTickTimeInSeconds()` (`:128-133`) — what the UI thread uses for the whole frame.
* `GetNextTickTimeInSeconds()` (`:97-119`) — real QPC, **monotonic-clamped**: "Make sure time never flows backwards" (`:103-116`).

This is a smoothness-critical property: every animation, timer and `CompositionTarget.Rendering` handler in a frame sees exactly one timestamp.

### 1.5 The `UIThreadScheduler` state machine

Three states (`core/inc/UIThreadScheduler.h:87-92`):

```
UITSS_Waiting     // scheduling work; waiting for the render thread to queue a tick
UITSS_TickQueued  // a tick has been enqueued to the dispatcher
UITSS_Ticking     // UI thread is in the tick; new requests target the *next* tick
```

`RequestAdditionalFrame(nextTickIntervalInMilliseconds, reason)` (`UIThreadScheduler.cpp:81-167`):

* Requests are collapsed by **minimum**: `if (nextTickIntervalInMilliseconds < m_nextTickIntervalInMilliseconds) { m_nextTickIntervalInMilliseconds = …; }` (`:136-138`). N requests in a frame ⇒ 1 frame.
* If a tick is already queued (`UITSS_TickQueued`), the request is dropped entirely (`:134`).
* Only if the scheduler was idle (`UITSS_Waiting`) does it wake the compositor thread (`:146-149`). Requests made *during* a tick are deliberately deferred to `EndTick` "to prevent waking up the render thread until all requests from the current frame are combined" (`:141-145`).
* Requests made **outside** a tick are flagged high-priority. The comment (`:118-127`) is the clearest statement of XAML's input-vs-render priority policy:

> "Tick requests OUTSIDE of tick processing will be treated as high-priority. This includes changing layout, rendering, or animation state directly from input events… A high priority tick, once posted, will be processed before any additional input messages. Tick requests coming from within the tick itself are treated as low-priority… A low priority tick will be deferred until all input is processed."

  (Note: `IsHighPriority()` is only *consumed* in two places today — ETW tracing at `win/shared/xcpwindow.cpp:1133` and a virtualization heuristic at `dxaml/lib/ModernCollectionBasePanel_WindowManagement_Partial.cpp:2982`. The actual queue ordering is done by CoreMessaging, not by XAML. **Partly UNVERIFIED** — the CoreMessaging side is not in this tree.)

`BeginTick()` (`:175-196`) snaps `m_lastTickTimeInSeconds = m_pIClock->GetLastTickTimeInSeconds()` and resets the interval to `XINFINITE`. `EndTick()` (`:204-240`) flips back to `UITSS_Waiting` and, if anything asked for another frame, wakes the compositor thread so throttling is applied ("Wake the scheduling thread and have it queue a tick. This ensures we get throttled properly to the frame rate", `:230-232`).

There is a `TODO` at `UIThreadScheduler.cpp:178-179` that is directly relevant to latency:

```cpp
// TODO: TICK: Do we really want the UI thread to tick to whenever the last compositor tick was?
// TODO: TICK: It might be better to move non-animated values forward by a number of frames in anticipation of when they'll hit the screen.
```

i.e. **WinUI does not do any latency-compensating time extrapolation on the UI thread.**

### 1.6 `RequestFrameReason` — the full taxonomy

`core/inc/UIThreadScheduler.h:15-49` enumerates every reason a frame can be requested. This doubles as an exhaustive list of "things that can cost you a frame":

```
ThemeChange 0x1, AnimationTick 0x2, StoryboardTick 0x4, TimerTick 0x8, VSMAnimation 0x10,
RootVisualDirty 0x20, ImageDirty 0x40, VSISUpdate 0x80, RTBRender 0x100, MediaQueue 0x200,
InputManager 0x400, EventManager 0x800, Paint 0x1000, WindowSize 0x2000, Download 0x4000,
PerFrameCallback 0x8000, SurfaceContentsLost 0x10000, SettingsChanged 0x20000, DeviceLost 0x40000,
RequestCommit 0x80000, AfterResume 0x100000, EnableRender 0x200000, LayoutCompletedNeeded 0x400000,
UnlockTime 0x800000, BuildTreeServiceWork 0x1000000, PhasedWork 0x2000000, ComponentHost 0x4000000,
ConnectedAnimation 0x8000000, EnableTicks 0x10000000, SwapChainPanelHitTest 0x20000000,
ReplayPointerUpdate 0x40000000, LoadedImageSurface 0x80000000
```

They are accumulated as a bitmask per frame and traced (`UIThreadScheduler.cpp:153-163`), which is exactly the diagnostic you want for "why did we tick".

### 1.7 Getting the tick onto the UI thread

`UIThreadScheduler::QueueTick()` (`:276-312`) → `m_pDispatcher->QueueTick()` → `CXcpDispatcher::QueueTick()` (`win/shared/xcpwindow.cpp:1171-1175`) → `QueueDeferredInvoke(WM_INTERNAL_TICK, 0, 0)`.

`CXcpDispatcher::QueueDeferredInvoke` (`xcpwindow.cpp:1181-1207`) pushes onto XAML's own lock-protected linked list (`CDeferredInvoke::Enqueue`, `xcpwindow.cpp:150-195`) and, **only if the list was empty**, starts a `DispatcherQueueTimer`:

```cpp
// xcpwindow.cpp:1188-1206
// Xaml depends on callbacks made from CoreMessaging. We kick off this process by starting
// a dispatcher queue timer, which will cause a callback to eventually come in on the UI thread. This timer is at
// the same priority as input messages, so ticking will be interleaved with input.
if (isFirstItem)
{
    IFCFAILFAST(m_dispatcherQueueTimer->Start());
}
```

Timer configuration (`xcpwindow.cpp:312-320`):

```cpp
// In order to not have input and rendering starve each other, this timer must be at the same priority as input,
// which DispatcherQueueTimer is.
IFCFAILFAST(m_dispatcherQueue->CreateTimer(m_dispatcherQueueTimer.GetAddressOf()));
IFCFAILFAST(m_dispatcherQueueTimer->put_IsRepeating(false));
IFCFAILFAST(m_dispatcherQueueTimer->put_Interval(wf::TimeSpan { 0 }));
```

`MessageTimerCallback()` (`xcpwindow.cpp:1213-1262`) dispatches **exactly one** queued message and, if more remain, re-arms the timer:

```cpp
// xcpwindow.cpp:1246-1257
if (hasMoreWork)
{
    // There are more queued messages to dispatch. Request another callback via the timer. This will let CoreMessaging
    // finish the currently queued input messages before calling us back again. This interleaves ticking with input and
    // prevents starvation.
    IFCFAILFAST(m_dispatcherQueueTimer->Start());
}
```

**This is the single most important anti-jank/anti-starvation construct in the pipeline** and it has no equivalent in a naive "post a message and loop" design.

`WM_INTERNAL_TICK` is dispatched through `ProcessMessage` → `OnReentrancyProtectedWindowMessage` (`xcpwindow.cpp:584-606`, `:643-694`), which fail-fasts on re-entrancy (a pumped message loop inside layout would corrupt state).

### 1.8 Ordering inside a tick

`CXcpDispatcher::Tick()` (`xcpwindow.cpp:1095-1141`) → `CXcpBrowserHost::OnTick()` (`host/win/browserdesktop/WinBrowserHost.cpp:318-356`):

```cpp
IFC(m_pUIThreadScheduler->BeginTick());
IFC(m_pNWRenderTarget->Draw(m_pcs, forceRedraw, &frameDrawn));   // → CCoreServices::NWDrawMainTree
IFC(m_pUIThreadScheduler->EndTick());
```

`CWindowRenderTarget::Draw` (`core/compositor/windowrendertarget.cpp:99-146`) checks the compositor thread's saved HRESULT for device loss, recovers, then calls `NWDrawMainTree`.

`CCoreServices::NWDrawMainTree` (`core/dll/xcpcore.cpp:6158-6245`):

| Order | Work | Line |
|---|---|---|
| 1 | snapshot `m_qpcDrawMainTreeStart` (QPC) | `6172-6178` |
| 2 | ensure DComp device / island targets | `6183-6209` |
| 3 | **`m_inputServices->ProcessUIThreadTick()`** — DManip viewport state pull | `6212-6216` |
| 4 | `NWDrawTree(...)` — the real frame | `6219-6225` |
| 5 | `m_inputServices->OnPostUIThreadTick()` | `6227-6230` |

`CCoreServices::NWDrawTree` (`core/dll/xcpcore.cpp:6261-6920`) — canonical intra-frame order:

| # | Step | Line(s) | Notes |
|---|---|---|---|
| 1 | `TraceFrameBegin`, ETW `CoreServices_Frame` start | `6304-6312` | |
| 2 | `BudgetService_StoreFrameTime(TRUE)` | `6315` | starts the per-frame budget clock |
| 3 | re-entrancy fail-fast arm (`m_pDrawReentrancyCheck = NULL`) | `6322-6323` | |
| 4 | snapshot `rFrameStartTime` from `IPALClock` | `6332` | |
| 5 | **`Tick(TRUE /*tickForDrawing*/…)`** — TimeManager, event manager, media queue | `6343` | animations evaluated *before* layout |
| 6 | **`GetDirectManipulationChanges(...)`** | `6347-6350` | any DM viewport change forces a render walk |
| 7 | `GetWindowSize` / recursive invalidate on zoom or locale change | `6367-6404` | `RecursiveInvalidateMeasure()` — whole-tree remeasure |
| 8 | **`pLayoutManager->UpdateLayout(w,h)`** (measure+arrange) | `6425` | inside `BeginCachingThemeResources()` scope |
| 9 | VSIS lost-resource handling, `RaiseQueuedSurfaceContentsLostEvent` | `6428-6443` | |
| 10 | XamlRoot changed events | `6448-6466` | |
| 11 | `RaiseLoadedEvent()` **+ UpdateLayout again** | `6476-6481` | |
| 12 | **`CallPerFrameCallback(...)` = `CompositionTarget.Rendering`** **+ UpdateLayout again** | `6485-6492` | |
| 13 | `PhasedWorkDistributor_PerformWork` (incremental container build) **+ UpdateLayout again** | `6494-6511` | requests another frame if work remains |
| 14 | RTB metrics **+ UpdateLayout again** | `6513-6522` | |
| 15 | `UpdateFocusRect`, `RealizeRegisteredLayoutTransitions`, `UpdateImplicitShowHideAnimations` **+ UpdateLayout again** | `6524-6536` | |
| 16 | `TransitionLayout()`, `IncrementLayoutCounter()` | `6539-6543` | |
| 17 | `TimeManager->Tick(newTimelinesOnly=TRUE)` — animations started *by* layout | `6551-6572` | |
| 18 | `UpdateDirtyState()`, `SetLayoutCleanSignaledStatus` | `6592-6600` | |
| 19 | `WaitForD3DDependentResourceCreation()` | `6603-6607` | |
| 20 | `pTimeMgrNoRef->UpdateIATargets()` — clone independent animations onto comp nodes | `6617-6620` | |
| 21 | compute `needsRenderWalk` | `6651-6657` | |
| 22 | **`RenderWalk(...)`** | `6682-6688` | |
| 23 | RTB render, `FlushPendingImageUpdates`, `SubmitTextureUpdates` | `6710-6726` | |
| 24 | compute `shouldSubmitFrame` | `6736-6742` | |
| 25 | **`SubmitPrimitiveCompositionCommands(...)`** → DComp commit | `6768-6772` | |
| 26 | `CompositionTarget.Rendered` event with measured frame duration | `6779-6810` | |
| 27 | `FlushImageDecodeRequests`, `ProcessTrackedImages` | `6846-6853` | |
| 28 | `BudgetService_StoreFrameTime(FALSE)` | `6856` | |
| 29 | pointer-update replay bookkeeping | `6873-6890` | see §2.5 |

**Up to six `UpdateLayout` calls per frame.** Each is cheap when clean (see §3.1), but each app callback (Loaded, Rendering, phased work) can re-dirty layout and force a real pass. This is the classic WinUI jank source.

The critical comment at `6584-6586`:

```cpp
// NOTE: No user code callbacks should be made beyond this point.
```

Everything from step 18 onwards is app-code-free, so the back half of the frame has bounded cost.

### 1.9 The commit

`CCoreServices::SubmitPrimitiveCompositionCommands` (`core/dll/xcpcore.cpp:7154-7300`):

1. `dcompTreeHost->PreCommitMainDevice()` → `m_pCompositionHelper->Flush()` — flushes D2D work and gutters (`DCompTreeHost.cpp:1057-1066`).
2. **Take the draw-lists lock** shared with the compositor thread (`xcpcore.cpp:7186`). The comment (`:7176-7183`) states why:

> "This is required to snap up-to-date property values committed on the UI thread, and to prevent the independent thread from committing newer values until the UI thread commits. Failure to do either of these tasks makes it possible for the UI thread to commit stale values for independently-animating properties, **causing them to glitch by jumping backwards to a value from an earlier time**."

3. Inside the lock: `ConnectedAnimation::PreCommit`, `SubmitRenderCommandsToCompositor(...)` (`:7199`), shadow scene update, RTB pre-commit, `UpdateTargetDOs` (attach/start WUC animations whose targets are collapsed).
   Note at `:7191-7192`: "This should happen as late as possible before submitting the frame, so we can account for as much of the UI thread frame cost as we can when adjusting animation start times."
4. `pTimeManager->ResetIndependentTimelinesChanged()` (`:7236`).
5. **Release the lock, then `dcompTreeHost->CommitMainDevice()`** (`:7244`). Explicit reasoning at `:7238-7243`:

> "Commit the main device outside the lock… the main device Commit() of the content changes can take a long time (> 1 vBlank) to return. If the UI thread holds the lock this entire time, it prevents the render thread from ticking any active animations or manipulations. **It is more preferable to allow for de-synchronization at this point than to glitch the render thread for the duration.**"

`SubmitRenderCommandsToCompositor` itself (`xcpcore.cpp:7347-7379`) is small: `pRootCompNode->UpdateTreeRoot(dcompTreeHost, true /*useDCompAnimations*/)`, `dcompTreeHost->SetRoot(...)`, and the same for each island root.

---

## 2. Pointer input

### 2.1 Which thread reads input

**The UI thread.** Lifted WinUI 3 subscribes to `Microsoft.UI.Input.InputPointerSource` WinRT events on the content island; there is no separate XAML input thread.

`InputSiteAdapter::InitInputObjects` (`dxaml/lib/InputSiteAdapter.cpp:100-173`) acquires `IInputPointerSource` via `InputPointerSourceStatics::GetForIsland` (`:154-162`). `SubscribeToInputPointerSourceEvents()` (`:332` ff.) adds `add_PointerMoved` (`:366`), `add_PointerPressed` (`:376`), `add_PointerWheelChanged` (`:396`), etc., using agile callbacks that run on the island's thread — the XAML UI thread.

Delivery chain for a pointer move:

```
InputSiteAdapter::OnPointerMoved                        (InputSiteAdapter.cpp:689)
  → OnPointerMessage(WM_POINTERUPDATE, e)               (:737-746)
    → CJupiterWindow::OnCoreWindowPointerMessage        (dxaml/lib/JupiterWindow.cpp:1926-1959)
      → CJupiterControl::HandlePointerMessage           (dxaml/lib/JupiterControl.cpp:554-589)
        → CXcpBrowserHost::HandleInputMessage           (host/win/browserdesktop/WinBrowserHost.cpp:984-1082)
          → CXcpBrowserHost::HandlePointerMessage       (host/win/browserdesktop/PlatWinBrowserHost.cpp:16-94)
            → CInputServices::ProcessInput              (core/input/InputServices.cpp:820-906)
              → PointerInputProcessor::ProcessPointerInput
                                                        (components/ContentRoot/PointerInputProcessor.cpp:171-…)
```

Note that XAML still speaks in Win32 message IDs internally (`WM_POINTERUPDATE`, `WM_POINTERWHEEL`) even though the transport is WinRT — `PlatWinBrowserHost.cpp:22-55` maps them to `XCP_*` message IDs.

Each hop allocates: `std::make_shared<InputMessage>()` per message (`WinBrowserHost.cpp:999`) and `new CPointerEventArgs(...)` per message (`PointerInputProcessor.cpp:365`).

### 2.2 Coalescing

XAML does **not** coalesce. The OS/`InputPointerSource` does: one `PointerMoved` event per frame-ish, with the skipped samples retrievable as *intermediate points*.

XAML consumes the intermediate points in exactly one place — feeding the gesture recognizer:

```cpp
// components/gestures/export/ElementGestureTracker.cpp:506-522
else if (msg.m_msgID == XCP_POINTERUPDATE)
{
    wrl::ComPtr<wfc::IVector<ixp::PointerPoint*>> pointerPoints;
    if (msg.m_pPointerEventArgsNoRef)
    {
        IFC_RETURN(msg.m_pPointerEventArgsNoRef->GetIntermediatePoints(&pointerPoints));
    }
    ...
    IFC_RETURN_ALLOW(m_gestureRecognizerAdapter.m_gestureRecognizer->ProcessMoveEvents(pointerPoints.Get()), INPUT_E_OUT_OF_ORDER);
}
```

So: **velocity for gestures/manipulations is computed from the full, un-decimated sample stream**, not from the one coalesced point. Down/up use the single current point (`ProcessDownEvent` at `:501`, `ProcessUpEvent` at `:505`).

Public API mirror: `PointerRoutedEventArgs::GetIntermediatePointsImpl` (`dxaml/lib/PointerRoutedEventArgs_Partial.cpp:45-90`) forwards to `IPointerEventArgs::GetIntermediateTransformedPoints`. Notable smoothness detail — for *replayed* (synthetic) pointer updates the list is truncated to a single point:

```cpp
// PointerRoutedEventArgs_Partial.cpp:66-80
// We are in a generated event.  That means we are replaying a previous
// event so we can respond to scene changes.  For replays, we only want
// our intermediate points to contain the most current point.
```

### 2.3 Prediction / resampling

A repo-wide case-insensitive grep for `predict` finds **no pointer prediction machinery** in `dxaml/xcp` (only `IsTextPredictionEnabled` and unrelated comments). There is no resampling / `GetPointerFrameInfoHistory` usage either (only one `POINTER_INFO*` local at `ElementGestureTracker.cpp:456`, which is allocated-and-freed but never populated in the read path).

The **only** thing resembling prediction is a fixed 16 ms latency constant handed to DirectManipulation:

```cpp
// core/compositor/CompositorDirectManipulationViewport.cpp:57-63
// TODO - Jupiter (Windows) bug 847117. Replace 16 with the actually milliseconds until the transform is shown on screen
IGNOREHR(pCompositorService->UpdateCompositorContentTransform(
    pCompositorContent,
    16 /*deltaCompositionTime*/
    ));
```

which lands in `CDirectManipulationService::UpdateCompositorContentTransform` (`plat/win/browserdesktop/DirectManipulationService.cpp:3834-3878`), stored as `m_deltaCompositionTime` and returned to DManip via `IDirectManipulationFrameInfoProvider::GetNextFrameInfo`:

```cpp
// plat/win/browserdesktop/DirectManipulationFrameInfoProvider.cpp:48-71
IFACEMETHODIMP CDirectManipulationFrameInfoProvider::GetNextFrameInfo(
    _Out_ XUINT64* pTime, _Out_ XUINT64* pProcessTime, _Out_ XUINT64* pCompositionTime)
{
    *pTime = 0;
    *pProcessTime = 0;
    *pCompositionTime = m_pDMService->GetDeltaCompositionTime();
```

This tells DManip "your output will be on screen 16 ms from now", so DManip evaluates inertia one frame ahead. **Any real prediction/extrapolation of the pointer stream happens inside DirectManipulation / the system compositor, which is outside this source tree — UNVERIFIED here.**

Note also `CCompositorDirectManipulationViewport::UpdateTransform()` has **no callers** in this tree (grep for `UpdateTransform()` finds only its definition and unrelated `DragDropVisual`), so this path appears vestigial in the lifted architecture.

### 2.4 Per-message cost on the UI thread

`PointerInputProcessor::ProcessPointerInput` (`components/ContentRoot/PointerInputProcessor.cpp:171-…`) per pointer message:

* re-entrancy detection with telemetry (`:188-203`) — a nested message marks the outer one `m_supersededByLaterMessage`.
* `SetLastInputDeviceType` with a "did the pointer actually move" filter to avoid spurious UIA focus-state churn (`:219-247`):

```cpp
bool keepUIAFocusState = pointerState && pMsg->m_msgID == XCP_POINTERUPDATE &&
                         pointerState->GetPointerInputType() == pMsg->m_pointerInfo.m_pointerInputType &&
                         pointerState->GetLastPosition().x == pMsg->m_pointerInfo.m_pointerLocation.x &&
                         pointerState->GetLastPosition().y == pMsg->m_pointerInfo.m_pointerLocation.y;
```

* **a full hit test on every pointer message**: `HitTestWithLightDismissAwareness(...)` (`:287-295`).
* allocation of `CPointerEventArgs` (`:365`) and synchronous routed-event raising into app code.

`CInputServices::RequestAdditionalFrame()` (`core/input/InputServices.cpp:7065-7080`) requests an immediate frame with `RequestFrameReason::InputManager` — so input that changes state costs a frame, but pure hover motion does not by itself.

### 2.5 Pointer replay (hover correctness after a scroll)

After the content moves under a stationary mouse, WinUI *re-injects* the last pointer update so hover/`PointerEntered` are correct:

* `InputSiteAdapter::UpdateLastPointerPointForReplay` (`InputSiteAdapter.cpp:755-790`) caches the last mouse `WM_POINTERUPDATE/DOWN/UP` (mouse only; touch/pen clear the cache).
* `InputSiteAdapter::ReplayPointerUpdate()` (`:846-869`) re-delivers it as `WM_POINTERUPDATE` with `isReplayedMessage = true`.
* Scheduling and throttling live in `NWDrawTree` (`core/dll/xcpcore.cpp:6873-6890`) with a hard rate limit:

```cpp
static const UINT64 MIN_POINTER_REPLAY_PERIOD_IN_MS = 500;   // core/dll/xcpcore.cpp:111
```

If a replay was requested less than 500 ms ago, XAML instead schedules a frame for the remainder (`RequestFrameReason::ReplayPointerUpdate`, `:6887`). `m_replayPointerUpdateAfterTick` is set whenever a render walk happened (`:6664`) or explicitly by `CScrollViewer` (`core/core/elements/ScrollViewer.cpp:70`).

DManip wheel scrolls arm a replay when inertia ends:

```cpp
// core/input/InputServices.cpp:6910-6916
if (currentMsgForDirectManipulationProcessing->m_msgID == XCP_POINTERWHEELCHANGED)
{
    pViewport->SetRequestReplayPointerUpdateWhenInertiaCompletes(TRUE);
}
```

---

## 3. How WinUI avoids a layout pass per scroll delta

### 3.1 Layout early-out

`CLayoutManager::UpdateLayout(w,h)` (`core/layout/LayoutManager.cpp:225-...`) bails in ~10 instructions when nothing is dirty:

```cpp
// LayoutManager.cpp:239-242
XUINT32 fPluginSizeChanged = controlWidth != m_previousPluginWidth || controlHeight != m_previousPluginHeight;
if (!fPluginSizeChanged && !pRoot->GetRequiresLayout()
    && !pRoot->GetIsViewportDirtyOrOnViewportDirtyPath())
    RRETURN(S_OK);
```

The loop then runs at most one of {measure, arrange, effective-viewport walk, event raising} per iteration, bounded by `MaxLayoutIterations = 250` (`core/inc/LayoutManager.h:151`), with stack-trace capture for the last `WarningLayoutIterations = 8` (`core/inc/LayoutManager.h:154`) to diagnose layout cycles.

`EffectiveViewportChanged` is a *separate pass* (`LayoutManager.cpp:340-386`) driven by its own dirty bit — so viewport reporting during scroll does not force measure/arrange.

### 3.2 The real answer: the scroll offset never reaches layout

The manipulation transform is owned by DirectManipulation and consumed by the compositor through a WUC `ExpressionAnimation`. XAML publishes it once, then stops participating.

`DManipDataWinRT::EnsureOverallContentPropertySet` (`core/hw/DManipData.cpp:152-183`):

```cpp
IFC_RETURN(pCompositor->CreatePropertySet(m_spOverallContentPropertySet.ReleaseAndGetAddressOf()));
IFC_RETURN(m_spOverallContentPropertySet->InsertMatrix4x4(HStringReference(L"Matrix").Get(), {identity}));

// Update Xaml-computed offset (constant in expression)
wfn::Matrix4x4 prependTransform = { 1,0,0,0, 0,1,0,0, 0,0,1,0, m_contentOffsetX, m_contentOffsetY, 0, 1 };
...
// "targetPS.Matrix = ContentOffsetTransform * DManipTransform.Matrix"
IFC_RETURN(::ConnectAnimationWithPrependTransform(spOverallContentPropertySetCO.get(),
    m_spSharedPrimaryContentTransformCO.get(), prependTransform, L"Matrix"));
```

and the expression is literally string-built in `core/hw/ManipulationTransform.cpp:82-105`:

```cpp
// ConnectAnimationWithPrependTransform
const wchar_t *sourceKey = L"manipTransform";
const wchar_t *transformKey = L"transform";
expression = transformKey + L"*" + sourceKey + L"." + propertyName;   // "transform*manipTransform.Matrix"
compositionAnimation->SetReferenceParameter(L"manipTransform", sourceCO);
compositionAnimation->SetMatrix4x4Parameter(L"transform", prependTransform);
manipulationPropertySetCO->StartAnimation(propertyName, compositionAnimation.Get());
```

Two-transform variant (overpan suppression) at `ManipulationTransform.cpp:107-137`:
`"transform * manipTransformSecondary.Matrix * manipTransformPrimary.Matrix"`.

The comp node wires that property set into the visual's `TransformMatrix` expression:

```cpp
// components/comptree/HWCompNodeWinRT.cpp:2333-2338
bool requiresTransformExpression =
    hasIndependentTransformManipulation     // DManip-driven animation
    || hasIndependentTransformAnimation     // Keyframe-driven animation of RenderTransform/Transform3D/Projection
    || !redirectionIsTranslationOnly;
```

…then `EnsureSharedContentTransform` / `SetSharedContentTransforms` / `EnsureOverallContentPropertySet` (`HWCompNodeWinRT.cpp:2365-2383`) and `WinRTLocalExpressionBuilder` (`:2385-2415`).

The DManip transform objects themselves are opaque `ICompositionObject`s obtained from DManip (`core/hw/DManipData.h:112-119`):

> "the objects obtained from DManip via `CreateManipulationTransform` are of type CompositionObject, and conceptually represent a PropertySet, but appear to be opaque… They are meant to be used as a reference parameter."

**Consequence:** during a pan/inertia, no XAML code runs per frame. The compositor evaluates `transform * manipTransform.Matrix` at its own rate.

### 3.3 What XAML *does* run during a manipulation

Per UI tick (only while a viewport is active):

* `CInputServices::ProcessUIThreadTick()` (`core/input/InputServices.cpp:9098-9124`): `InitializeDirectManipulationContainers()`, `ProcessDirectManipulationViewportChanges()`, `RefreshDirectManipulationHandlerWantsNotifications()`, `RaiseManipulationInertiaProcessingEvent()`, `ProcessDeferredReleaseQueue()`.
* `ProcessDirectManipulationViewportChanges(viewport)` (`:7122-7325`) drains a *queue of status changes* accumulated since the last tick (`GetStatusesCount()`, `GetStatus(i, …)`) — i.e. DManip status notifications are **batched per frame**, not processed per notification.
  * `static const XUINT32 StatusChangesForIntermediaryStatus = 2;` (`:7126`) plus a delay heuristic (`:7150-7172`) to swallow DManip's spurious `Running→Ready→Running` sequences — an explicit *anti-flicker* workaround.
  * If the viewport is still active it requests another frame (`:7311-7315`).
* `CInputServices::OnPostUIThreadTick()` (`:9134-9149`) — stops inertial viewports whose manipulated element lost its comp node, clears per-tick focus caches.

DManip status callbacks arrive on the UI thread via `CDirectManipulationViewportEventHandler::OnViewportStatusChanged` (`plat/win/browserdesktop/DirectManipulationViewportEventHandler.cpp:95-118`); `OnViewportUpdated` and `OnContentUpdated` deliberately **return `S_FALSE` and do nothing** (`:130-152`) — XAML does not want a UI-thread callback per manipulation frame.

### 3.4 Dirty-flag discipline: "independent" changes don't dirty rendering

`DirtyFlags::Independent` marks a property change that the compositor will handle by itself. The render-walk dirty setters short-circuit on it:

```cpp
// components/elements/UIElementRenderWalk.cpp:113-127 (NWSetOpacityDirty)
if (!flags_enum::is_set(flags, DirtyFlags::Independent))
{
    pUIE->NWSetDirtyFlagsAndPropagate(flags | DirtyFlags::Render, pUIE->m_fNWOpacityDirty);
    pUIE->m_fNWOpacityDirty = TRUE;
}
```

```cpp
// components/elements/UIElementRenderWalk.cpp:249-263 (NWSetContentDirty)
else if (flags_enum::is_set(flags, DirtyFlags::Bounds) != 0)
{
    // Independent changes can only dirty bounds.
    ASSERT(flags == (DirtyFlags::Independent | DirtyFlags::Bounds));
    pUIE->NWSetDirtyFlagsAndPropagate(flags, FALSE);
}
```

Same for `NWSetSubgraphDirty` (`:270-311`). WUC `PropertySet` listeners feed changes back as `DirtyFlags::Independent | DirtyFlags::Bounds` only (`components/elements/PropertySetListener.cpp:163`, `:202`, `:250`) — i.e. hit-testing bounds get updated, rendering does not get invalidated.

### 3.5 Render-walk gating

```cpp
// core/dll/xcpcore.cpp:6651-6657
const bool needsRenderWalk =
    (!IsInBackgroundTask() &&
    pVisualRoot != nullptr &&
        (  forceRedraw
        || pVisualRoot->NWNeedsRendering()
        || m_renderStateChanged));
```

and commit gating:

```cpp
// core/dll/xcpcore.cpp:6736-6742
const bool shouldSubmitFrame = needsRenderWalk
    || (pTimeMgrNoRef && pTimeMgrNoRef->HaveIndependentTimelinesChanged())
    || hasRenderTargetBitmaps
    || m_debugSettingsChanged
    || m_fLayoutCompletedNeeded
    || m_commitRequested;
```

A tick that is only servicing a DManip status change therefore does **no** render walk and **no** DComp commit.

---

## 4. What is committed to DComp per frame; what runs on the compositor

### 4.1 Committed per frame (UI thread)

Under the draw lock (`xcpcore.cpp:7186-7234`):

* the WUC visual tree delta — `HWCompTreeNodeWinRT::UpdateTreeRoot(dcompTreeHost, /*useDCompAnimations*/ true)` (`xcpcore.cpp:7355`), which writes visual `Offset`, `Size`, `TransformMatrix` (or the expression), `Clip`, `Opacity`, brushes, etc.
* new/changed **WUC ExpressionAnimations** for independently-animating transforms and for DManip (`HWCompNodeWinRT.cpp:2306-2416`).
* new/changed **WUC KeyFrameAnimations** converted from XAML Storyboards (`CTimeManager::Tick` → `MakeCompositionAnimationsWithProperties`, `core/animation/timemgr.cpp:318-330`; attach via `UpdateIATargets`, `xcpcore.cpp:6619`).
* ThemeShadow scene updates (`xcpcore.cpp:7205-7209`), RTB pre-commit (`:7213`).
* texture uploads are submitted *outside* the lock beforehand: `pHWWalk->GetTextureManager()->SubmitTextureUpdates()` (`xcpcore.cpp:6726`) — "This can occur outside the lock, since the independent thread never accesses textures or the graphics device."

Then, outside the lock: `dcompTreeHost->CommitMainDevice()` → `m_spMainDevice->Commit()` (`DCompTreeHost.cpp:1078-1092`).

Redundant writes are filtered: `IsWUCTransformMatrixDifferentFromPrevious` (`HWCompNodeWinRT.cpp:2288-2296`), `IsWUCClipTransformMatrixDifferentFromPrevious` (`:2298-2304`), and the DComp inset-clip comparison just above. The comp node keeps `m_previousWUCTransformMatrix` and sets `M11 = NaN` to force the next comparison to fail when switching to the expression path (`:2417`).

### 4.2 Runs entirely on the compositor (no XAML thread involvement)

* Evaluation of every WUC `ExpressionAnimation` and `KeyFrameAnimation` attached to the tree.
* DirectManipulation pan/zoom/inertia integration — the `manipTransform.Matrix` referenced by the expression.
* The actual per-vsync composition and present.

Confirmation that the XAML "render thread" does no drawing: `RenderThreadFrame` (`compositorscheduler.cpp:303-436`) contains only the clock tick, the UI-thread scheduling decision, the `CNotifyRenderStateChangedCommand` execution, and the wait. `CompositorScheduler` holds no device, no surface, no draw list traversal.

### 4.3 Which XAML animations qualify as "independent"

`CAnimation::ShouldTickOnUIThread` (`core/animation/animation.cpp:1326-1477`) decides. Notable points:

* Non-continuous animations are allowed to be "dependent" without smoothness loss (`IsAllowedDependentAnimation`, `:1344-1347`): "If the animation is not continuous, it's okay to treat it as independent since there's no 'smoothness' sacrificed by running it on the UI thread."
* If conversion to a Composition animation failed (`m_conversionResult != CompositionAnimationConversionResult::Success`), the animation degrades to a UI-thread-ticked *dependent* animation and the target loses its Composition expression — "This means Xaml is free to re-rasterize every frame of a scale animation again." (`:1367-1376`).
* Dependent animations are skipped entirely unless opted in (`s_allowDependentAnimations && m_enableDependentAnimation`, `:1432-1436`), with a debug warning: `"WARNING: Animation of \"%s\" on \"%s\" is not independent and will be skipped"` (`:1452-1461`).
* Approved independent targets are an explicit allow-list per animation type, e.g. `CDoubleAnimation::FindIndependentAnimationTargetsRecursive` (`core/animation/DoubleAnimation.cpp:104-...`): `Canvas.Left/Top` (only under a `Canvas`), `UIElement.Opacity`, and all the `RotateTransform` / `ScaleTransform` / `SkewTransform` / `TranslateTransform` / `CompositeTransform` scalars.
* Conversion constraints live in `CompositionAnimationConversionContext` (`components/animation/inc/DCompAnimationConversionContext.h:34-135`), with hard limits `CompositionMaximumTimeLimit = 24*24*60*60` seconds and `CompositionMinimumDuration = 0.001f` (`:131-135`), and a taxonomy of failure reasons (`:15-30`) — `CannotHaveInfiniteDuration`, `CannotNestAutoReverse`, `CannotHaveFractionalRepeat`, etc.

---

## 5. Mouse wheel cadence and normalization

### 5.1 Message types

Lifted XAML receives wheel input as `InputPointerSource.PointerWheelChanged`, converted to `WM_POINTERWHEEL` (`InputSiteAdapter.cpp:707-711`). The Win32 `WM_POINTERHWHEEL` variant exists in the switch (`dxaml/lib/JupiterControl.cpp:284-290`) and both map to one XAML message with a direction flag:

```cpp
// host/win/browserdesktop/PlatWinBrowserHost.cpp:39-43
case WM_POINTERWHEEL:
case WM_POINTERHWHEEL:
    pMsg->m_msgID = XCP_POINTERWHEELCHANGED;
    pMsg->m_bIsSecondaryMessage = (uMsg == WM_POINTERHWHEEL);
    break;
```

There is an acknowledged gap for islands: `core/core/elements/XamlIslandRoot.cpp:479-481` — "ISLANDTODO: CoreInput doesn't distinguish between WM_POINTERWHEEL and WM_POINTERHWHEEL."

Classic `WM_MOUSEWHEEL` does **not** appear anywhere in the XAML input path (grep finds it only as the WinUser constant in unrelated contexts). WinUI consumes the pointer-message form exclusively, which is what makes high-resolution precision-touchpad deltas available (deltas that are not multiples of 120).

### 5.2 Cadence

Cadence is whatever the device/OS produces — XAML applies no accumulation, no per-frame batching, and no threshold. Each `PointerWheelChanged` event is processed synchronously in the routed-event tree, on the UI thread, exactly like a pointer move. **UNVERIFIED:** the actual driver/OS cadence (per-notch bursts vs per-sample streaming for precision touchpads) is not observable from this tree.

### 5.3 Normalization — two distinct paths

**(a) DManip path (default, when `ScrollContentPresenter` is the scroll client).**

`ScrollViewer::OnPointerWheelChanged` (`dxaml/lib/ScrollViewer_Partial.cpp:2720-2844`) reads `MouseWheelDelta` (`:2754`) only to decide *zoom vs scroll* (Ctrl held → `ZoomDirection_In/Out`, `:2756-2766`), then hands the **original message** to DirectManipulation:

```cpp
// ScrollViewer_Partial.cpp:2800-2805
if (isScrollContentPresenterScrollClient)
{
    // Give DirectManipulation an opportunity to handle the mouse wheel message
    IFC(ProcessPureInertiaInputMessage(messageZoomDirection, &handled));
    IFC(pArgs->put_Handled(handled));
}
```

→ `ScrollViewer::ProcessInputMessage` (`:9016-9042`) → `CoreImports::ManipulationHandler_ProcessInputMessage` → `CInputServices::ProcessInputMessageWithDirectManipulation` (`core/input/InputServices.cpp:6710-6919`).

There, XAML **reconstructs a real Win32 `MSG`** and feeds it to DManip:

```cpp
// plat/win/browserdesktop/DirectManipulationService.cpp:566-591
msg.hwnd = CInputServices::GetUnderlyingInputHwndFromIslandInputSite(m_islandInputSite.Get());
msg.message = GetWindowsMessageFromMessageMap(msgID, fIsSecondaryMessage, fIsKeyboardInput);  // WM_POINTERWHEEL / WM_POINTERHWHEEL
msg.wParam  = GetWindowsMessageWParam(msgID, wParam, fInvertForRightToLeft && fIsForHorizontalPan);
msg.lParam  = pMsgPack->m_lParam;
msg.time    = ::GetMessageTime();
msg.pt.x = 0; msg.pt.y = 0;
...
IFC(pDMViewport->SetContact(fIsKeyboardInput ? DIRECTMANIPULATION_KEYBOARDFOCUS : DIRECTMANIPULATION_MOUSEFOCUS));
if (msgID == XCP_POINTERWHEELCHANGED)
{
    XUINT32 pointerId = GET_POINTERID_WPARAM(msg.wParam);
    auto pointerPoint = GetPointerPointFromPointerId(pointerId);
    IFC(m_pDMHelper->ProcessInputWithPointerPoint(&msg, pointerPoint.Get(), &handled));
}
else
{
    IFC(m_pDMManager->ProcessInput(&msg, &handled));
}
...
IFC(pDMViewport->ReleaseContact(... DIRECTMANIPULATION_MOUSEFOCUS));
```

**So the entire wheel→pixels→easing→inertia curve for the common case is DirectManipulation's, not XAML's.** XAML contributes only: pseudo-contact `DIRECTMANIPULATION_MOUSEFOCUS`, RTL inversion, and the config/rails. Keyboard scrolling goes through the identical path with `DIRECTMANIPULATION_KEYBOARDFOCUS` and `WM_KEYDOWN` (`GetWindowsMessageFromMessageMap`, `DirectManipulationService.cpp:4691-4709`).

The same file also encodes the "which axis does this key pan" policy: `IsWindowsMessageForHorizontalPan` (VK_LEFT/RIGHT, `:4718-4726`), `IsWindowsMessageForVerticalPan` (VK_UP/DOWN, `:4736-4744`), `IsWindowsMessageForPan` (PRIOR/NEXT/HOME/END, `:4755-4764`), plus a Ctrl-key disambiguation when both axes are pannable (`:557-559`).

**(b) `IScrollInfo` path (custom scroll clients — `ItemsPresenter`, `CarouselPanel`, `OrientedVirtualizingPanel`, `TextBoxView`).**

Here XAML does the normalization itself, and the constants matter:

```cpp
// dxaml/lib/ScrollViewer_Partial.h:26-37
#define ScrollViewerLineDelta (16.0f)                  // Up/Down/Left/Right key step, in px

// This value comes from WHEEL_DELTA defined in WinUser.h. It represents the universal default mouse wheel delta.
#define ScrollViewerDefaultMouseWheelDelta (120)

// These macros compute how many integral pixels need to be scrolled based on the viewport size and mouse wheel delta.
// - First the maximum between 48 and 15% of the viewport size is picked.
// - Then that number is multiplied by (mouse wheel delta/120), 120 being the universal default value.
// - Finally if the resulting number is larger than the viewport size, then that viewport size is picked instead.
#define GetVerticalScrollWheelDelta(size, delta)   (DoubleUtil::Min(DoubleUtil::Floor(size.Height), DoubleUtil::Round(delta * DoubleUtil::Max(48.0, DoubleUtil::Round(size.Height * 0.15, 0)) / 120.0, 0)))
#define GetHorizontalScrollWheelDelta(size, delta) (DoubleUtil::Min(DoubleUtil::Floor(size.Width),  DoubleUtil::Round(delta * DoubleUtil::Max(48.0, DoubleUtil::Round(size.Width  * 0.15, 0)) / 120.0, 0)))
```

So: **pixels = clamp(round(delta × max(48, round(0.15 × viewportExtent)) / 120), 0, viewportExtent)**, rounded to integral pixels.

Dispatch (`ScrollViewer_Partial.cpp:2806-2836`) uses `IsHorizontalMouseWheel` and the delta sign to pick `MouseWheelUp/Down/Left/Right(abs(delta))`. Because `delta` is passed through un-quantized, a high-resolution touchpad delta of e.g. 17 yields a proportionally small pixel step — the normalization is linear and continuous, not notch-quantized. Note the `DoubleUtil::Round(..., 0)` at the end **does** quantize the result to whole pixels, which is a smoothness ceiling for this path.

Other consumers of the same macros: `ItemsPresenter_IScrollInfo.cpp:347-500`, `CarouselPanel_Interfaces_Partial.cpp:143-230`, `core/native/text/Controls/TextBoxView.cpp:1126-1185`, `MediaTransportControls_partial.cpp:6130` (`sliderValue + VolumeSliderWheelScrollStep * (mouseWheelDelta / WHEEL_DELTA)` — the only place the raw 120 quantization is used).

Special case worth noting: if the wheel hit-test finds nothing (Windows "scroll inactive windows" off), the message is re-routed to the focused element:

```cpp
// components/ContentRoot/PointerInputProcessor.cpp:300-311
if (pMsg->m_msgID == XCP_POINTERWHEELCHANGED)
{
    spDOContact = static_cast<CDependencyObject*>(contentRoot->GetFocusManagerNoRef()->GetFocusedElementNoRef());
}
```

---

## 6. Explicit anti-jank machinery (inventory)

| Mechanism | Where | What it prevents |
|---|---|---|
| **Compositor-clock frame throttling** | `Scheduler.cpp:24-56`, `RefreshRateInfo.cpp:142-190` | UI thread producing frames faster than the display; also picks up refresh-rate boosting |
| **QPC skip-the-vblank-wait optimization** | `Scheduler.cpp:45-53` | needless `WaitForVBlank` (which keeps the display out of low-power state) when the previous frame was already >1 refresh ago |
| **Frame-request coalescing (min-interval)** | `UIThreadScheduler.cpp:130-151` | N state changes ⇒ 1 frame |
| **Deferred wake during tick** | `UIThreadScheduler.cpp:141-149`, `:216-233` | waking the compositor thread mid-frame for every request |
| **High vs low priority tick classification** | `UIThreadScheduler.cpp:102-128` | animation ticks starving input; input-driven state changes being delayed behind input backlog |
| **One-message-per-callback dispatch loop** | `xcpwindow.cpp:1213-1262` | tick↔input starvation in either direction |
| **`DispatcherQueueTimer` at input priority, Interval=0** | `xcpwindow.cpp:312-320` | XAML work jumping ahead of / behind input |
| **Re-entrancy fail-fast + `PauseNewDispatch`** | `xcpwindow.cpp:643-680`, `:1315-1400`; `xcpcore.cpp:6322-6323` | app code pumping messages mid-layout ⇒ corrupt/torn frames |
| **Independent animations (WUC KeyFrameAnimations)** | `animation.cpp:1326-1477`, `timemgr.cpp:318-330` | UI-thread-rate animation |
| **Independent manipulations (WUC ExpressionAnimation over DManip transform)** | `DManipData.cpp:152-183`, `ManipulationTransform.cpp:82-137` | UI-thread-rate scrolling |
| **`DirtyFlags::Independent`** | `UIElementRenderWalk.cpp:113-311` | compositor-owned property changes triggering a render walk |
| **Render-walk / commit gating** | `xcpcore.cpp:6651-6657`, `:6736-6742` | pointless walks and commits |
| **Draw-lists lock held across tree submit, released before `Commit()`** | `xcpcore.cpp:7186-7244` | (a) committing stale independent values ⇒ visible backward jump; (b) blocking the compositor for the duration of a long `Commit()` |
| **Skip-frame-on-render-error** | `xcpcore.cpp:7065-7095` (islands) and `:7116-7143` (main tree); flag set/cleared at `:6749` / `:6816` | one-frame content flicker when an element fails to produce content — rationale comment at `xcpcore.cpp:7066-7079` |
| **Per-frame work budget (`BudgetManager`)** | `BudgetManager_Partial.cpp:25-42`, `:44-71`; `BUDGET_MANAGER_DEFAULT_LIMIT 40` at `BudgetManager_Partial.h:47` | long incremental work (container prepare, phase-based item rendering) blowing the frame |
| **Phased work distributor** | `xcpcore.cpp:6494-6511`; `FxCallbacks.cpp:975-976` | virtualization work being done all at once; requests another tick if work remains |
| **Cache-inflation only when idle** | `ModernCollectionBasePanel_WindowManagement_Partial.cpp:2957-2983`; `PerformCacheInflationWhenTimeAvailable = 40` (`ModernCollectionBasePanel_Partial.cpp:32`), gated on `!pFrameScheduler->IsHighPriority()` | speculative virtualization work during active interaction |
| **Deferred/rate-limited pointer replay** | `xcpcore.cpp:6873-6890`; `MIN_POINTER_REPLAY_PERIOD_IN_MS = 500` (`xcpcore.cpp:111`) | hover-recompute storms while content moves |
| **DManip status-change batching + "Inactive/Active/Ready" delay heuristic** | `InputServices.cpp:7126-7176` | flicker from DManip's spurious intermediate `Ready` status |
| **`SkipFrames(n)` / `m_framesToSkip`** | `corep.h:898`, `xcpcore.cpp:6622-6632` | test/serialization hook that defers rendering N ticks |
| **Frame-rate counter & ETW (`TraceFrameBegin/End`, `CoreServices_Frame`, `Scheduling_*`, `RefreshAlignedClock_Tick`)** | `xcpcore.cpp:6304-6312`, `:6900-6912`; `compositorscheduler.cpp:332-335`, `:400-435` | (diagnostics) |
| **Monotonic clock clamp** | `RefreshAlignedClock.cpp:103-116` | animations jumping backwards on QPC anomalies |

Also worth listing as *hazards*, not mechanisms:

* Up to six `UpdateLayout()` calls per frame, each re-runnable by app callbacks (`xcpcore.cpp:6425-6537`).
* Full hit test per pointer message (`PointerInputProcessor.cpp:287-295`).
* Whole-tree `RecursiveInvalidateMeasure()` on any zoom-scale or locale change (`xcpcore.cpp:6379-6392`).
* `WaitForD3DDependentResourceCreation()` on the UI thread inside the tick (`xcpcore.cpp:6605`).

---

## 7. Concrete answers to the six questions

**1. What drives ticks; ordering within a tick.**
A dedicated real-time-priority scheduling thread (`CompositorScheduler`, `compositorscheduler.cpp:260-289`) runs a loop that (a) advances a shared QPC-backed clock, (b) asks the `UIThreadScheduler` whether the UI thread wants a frame *now* or *in N ms*, (c) if now, blocks on `DCompositionWaitForCompositorClock` (unless a full refresh interval has already elapsed), then posts one tick to the UI thread, (d) sleeps until the next scheduled time or an explicit wake. Within the UI tick, the order is: DManip state pull → animations → DM viewport changes → layout → app callbacks (Loaded / `CompositionTarget.Rendering` / phased work), each followed by another layout → newly-started animations → clone independent animations to comp nodes → render walk → texture submit → build WUC tree under lock → `DCompositionDevice::Commit()` outside the lock → `CompositionTarget.Rendered`.

**2. Pointer input: coalescing / intermediate points / prediction / thread.**
Read on the **UI thread** via `Microsoft.UI.Input.InputPointerSource` WinRT events (`InputSiteAdapter.cpp:332-420`). XAML does not coalesce; the OS does. Intermediate points **are** retrieved and fed wholesale to the gesture recognizer so manipulation velocity uses every sample (`ElementGestureTracker.cpp:506-522`). **No prediction and no resampling exists in XAML.** The only latency-compensation artifact is the hard-coded 16 ms `deltaCompositionTime` handed to DManip via `IDirectManipulationFrameInfoProvider::GetNextFrameInfo` (`CompositorDirectManipulationViewport.cpp:57-63`, `DirectManipulationFrameInfoProvider.cpp:48-71`), and even that call site appears unreferenced in the lifted tree.

**3. Avoiding layout per scroll delta.**
Three layers: (i) the scroll offset is never a XAML property during a manipulation — it lives in a DManip-owned transform consumed by a compositor `ExpressionAnimation` (`DManipData.cpp:152-183`); (ii) `DirtyFlags::Independent` prevents compositor-owned property changes from dirtying render (`UIElementRenderWalk.cpp:113-311`); (iii) `CLayoutManager::UpdateLayout` early-outs in a few instructions when no dirty bit is set (`LayoutManager.cpp:239-242`), and `EffectiveViewportChanged` is a separate, separately-dirtied pass.

**4. Committed per frame vs compositor-only.**
Committed: WUC visual property deltas, new/changed expression + keyframe animations, shadow scenes, texture uploads (submitted before the lock) — one `IDCompositionDevice::Commit()` per frame, and only when `shouldSubmitFrame` (`xcpcore.cpp:6736-6742`). Compositor-only: evaluation of all WUC animations/expressions, DManip pan/zoom/inertia integration, and the present. The XAML "render thread" itself draws nothing.

**5. Wheel cadence and normalization.**
XAML consumes only the pointer form (`WM_POINTERWHEEL` / `WM_POINTERHWHEEL` → `XCP_POINTERWHEELCHANGED`, `PlatWinBrowserHost.cpp:39-43`); classic `WM_MOUSEWHEEL` is absent from the path. For the default `ScrollContentPresenter` case, the raw message is reconstructed as a Win32 `MSG` and forwarded verbatim to DirectManipulation with a `DIRECTMANIPULATION_MOUSEFOCUS` pseudo-contact (`DirectManipulationService.cpp:566-604`) — DManip owns the scroll curve. For `IScrollInfo` clients, XAML normalizes with `max(48, round(0.15 × viewportExtent)) × delta / 120`, clamped to the viewport and rounded to whole pixels (`ScrollViewer_Partial.h:36-37`).

**6. Anti-jank machinery.**
See the table in §6. The four with the highest leverage: compositor-clock frame pacing, the one-message-per-callback dispatcher interleave, independent animations/manipulations on the compositor, and the draw-lock discipline around `Commit()`.

---

## 8. Notable comments worth quoting verbatim (design rationale)

* On why the clock is advanced before the UI tick — `compositorscheduler.cpp:319-322`.
* On the WaitForVBlank battery tradeoff — `Scheduler.cpp:26-42`.
* On why the tick timer must be at input priority — `xcpwindow.cpp:312-314` and `:1188-1191`.
* On tick priority vs input — `UIThreadScheduler.cpp:118-127`.
* On the draw lock and independent-value glitching — `xcpcore.cpp:7176-7183`.
* On committing outside the lock — `xcpcore.cpp:7238-7243`.
* On skipping frame submission after a render error to avoid flicker — `xcpcore.cpp:7066-7079` (repeated verbatim at `:7119-7132`).
* On "no user code callbacks beyond this point" — `xcpcore.cpp:6584-6586`.
* On the (unimplemented) idea of extrapolating non-animated values forward to hide latency — `UIThreadScheduler.cpp:178-179`.

---

## 9. Gaps / UNVERIFIED

* The actual queueing semantics of CoreMessaging (`DispatcherQueue` / `DispatcherQueueTimer`) — whether the timer really lands at the same priority as pointer input — is asserted by comments in `xcpwindow.cpp:312-314` but its implementation is not in this tree.
* DirectManipulation's internal inertia curve, per-notch wheel-to-pixel mapping, touchpad-precision handling, and any pointer prediction it performs are all outside `dxaml/xcp`.
* The IXP/lifted compositor's own scheduling (when it samples the DManip transform relative to vsync) is outside this tree.
* `CCompositorDirectManipulationViewport::UpdateTransform()` and `IPALDirectManipulationCompositorService::UpdateCompositorContentTransform` have no live callers here; whether the 16 ms `deltaCompositionTime` is still on any active path is unverified.
* `UIThreadScheduler::IsHighPriority()` is consumed only for tracing and one virtualization heuristic in this tree; the claimed "processed before any additional input messages" behavior is not implemented by XAML code visible here.
