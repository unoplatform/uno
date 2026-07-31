# Frame clock prior art: WinUI 3 / Windows.UI.Composition / DirectComposition

**Source roots**

| Alias | Path |
|---|---|
| `dxaml` | `D:/Work/microsoft-ui-xaml2/` (WinUI 3 / lifted XAML, public sources) |
| `sdk` | `C:/Program Files (x86)/Windows Kits/10/Include/10.0.26100.0/` |
| `wpf` | `D:/Work/wpf/src/Microsoft.DotNet.Wpf/src/` |
| `uno` | `D:/Work/uno-worktrees/scrollsmooth/` |

Runtime evidence was produced on this machine (Windows 11 build 10.0.29595, 120 Hz primary display,
QPF = 10 MHz) with two probes in the session scratchpad: `winuiclock.cs`, `winuiclock2.cs`.
Everything not read out of source or measured is marked **UNVERIFIED**.

---

## 0. Answer, up front

1. **Yes — Windows states the frame grid exactly, and it is on the same clock and epoch as
   `Stopwatch.GetTimestamp()`.** `DCompositionGetFrameId` + `DCompositionGetStatistics` return
   `COMPOSITION_FRAME_STATS { startTime, targetTime, framePeriod }` in QPC units.
   `targetTime` is quantity **(b)**, a *predicted* presentation target, quantized to the vsync grid.
   Runtime-verified below: consecutive `targetTime` values differ by exact integer multiples of the
   refresh period to within ±20 µs, while a UI-thread clock read over the same frames wobbled by
   milliseconds.

2. **But WinUI does not use it.** A repo-wide grep of `dxaml` for `DCompositionGetFrameId`,
   `DCompositionGetStatistics`, `COMPOSITION_FRAME_STATS` and `nextEstimatedFrameTime` returns
   **zero hits**. XAML's UI-thread animation clock is a plain QPC read (`CWinClock::GetAbsoluteTimeInSeconds`,
   `dxaml/xcp/plat/win/browserdesktop/xcpwin.cpp:1835-1845`) snapped on the scheduling thread
   **before** the vblank wait (`dxaml/xcp/core/compositor/compositorscheduler.cpp:317` vs `:345`).
   **It is not vsync-quantized.** WinUI's dependent (UI-thread) animations have the same class of
   phase jitter Uno's do.

3. **So the reason WinUI scrolling does not exhibit our defect is not a better UI-thread clock — it is
   that the scroll curve is never evaluated on the UI thread at all.** Pan/inertia is a
   DirectManipulation (dxaml `ScrollViewer`) or InteractionTracker (`ScrollPresenter`) output living
   inside the system/lifted compositor, bound into the visual tree by an ExpressionAnimation. The
   compositor re-evaluates it once per composition frame on its own uniform clock, with zero UI-thread
   involvement. WinUI sidesteps the problem by moving the curve, not by fixing the clock.

4. **The closest Microsoft implementation of the thing Uno is trying to build is WPF, not WinUI.**
   WPF's `MediaContext` computes an explicit `_estimatedNextPresentationTime` — snapped to a vsync
   grid anchored on the compositor-reported present time, with a half-frame hysteresis so it does not
   re-derive a new estimate every frame — and hands *that* to the timing tree and to
   `CompositionTarget.Rendering`. That is a phase-locked grid derived from real vsync data, i.e. exactly
   Uno's estimator but with a real anchor.
   `wpf/PresentationCore/System/Windows/Media/MediaContext.cs:1080-1181`, `:1814-1819`.

---

## 1. The three quantities, and what Windows offers for each

| | Quantity | Windows API | Verified? |
|---|---|---|---|
| (a) | vsync / frame-start time | `COMPOSITION_FRAME_STATS.startTime`; `DWM_TIMING_INFO.qpcVBlank`; return instant of `DCompositionWaitForCompositorClock` / `DwmFlush` | yes (DComp), see §5 |
| (b) | **predicted presentation time of the frame being built** | **`COMPOSITION_FRAME_STATS.targetTime`** (+ `framePeriod` to step forward); legacy: `DCOMPOSITION_FRAME_STATISTICS.nextEstimatedFrameTime` from `IDCompositionDevice::GetFrameStatistics` | yes (DComp), see §5 |
| (c) | measured presentation time, after the fact | `DCompositionGetTargetStatistics` → `COMPOSITION_TARGET_STATS.presentTime`; `DWM_TIMING_INFO.qpcFrameDisplayed`; WPF's UCE `NotifyPresent` | not probed — **UNVERIFIED** |

Declarations: `sdk/shared/dcomptypes.h:78-85` (`DCOMPOSITION_FRAME_STATISTICS`),
`:100-114` (`COMPOSITION_FRAME_ID_TYPE`, `COMPOSITION_FRAME_STATS`), `:151-159`
(`COMPOSITION_TARGET_STATS`); `sdk/um/dcomp.h:175-231`
(`DCompositionGetFrameId`, `DCompositionGetStatistics`, `DCompositionGetTargetStatistics`,
`DCompositionBoostCompositorClock`, `DCompositionWaitForCompositorClock`).

```c
// sdk/shared/dcomptypes.h:109-114
typedef struct tagCOMPOSITION_FRAME_STATS
{
    UINT64 startTime;
    UINT64 targetTime;
    UINT64 framePeriod;
} COMPOSITION_FRAME_STATS;
```

Note `DCompositionGetFrameId` takes a `COMPOSITION_FRAME_ID_TYPE`:
`CREATED = 0`, `CONFIRMED = 1`, `COMPLETED = 2` (`sdk/shared/dcomptypes.h:100-105`). `CREATED` is the
frame the compositor has most recently begun — that is the one whose `targetTime` is the live
prediction. The header comment on `DCompositionGetFrameId` ("Returns the frameId of the most recently
started composition frame", `sdk/um/dcomp.h:167-177`) confirms this reading; the precise semantics of
`startTime` vs `targetTime` are not documented in the header and my characterisation below is
inferred from measurement, so treat the *naming* as **UNVERIFIED** even though the *numbers* are
measured.

---

## 2. WinUI's UI-thread frame clock — read from source

### 2.1 The clock object

`RefreshAlignedClock` (`dxaml/xcp/core/compositor/RefreshAlignedClock.{h,cpp}`) is the single time
source for everything on the XAML UI thread. Its header states the design intent
(`RefreshAlignedClock.h:6-35`):

> A clock that is shared between the scheduling thread and the UI thread. This clock is backed by
> QueryPerformanceCounter, but rather than having every "what time is it?" question go to QPC, it
> caches the current time and only updates it from the scheduling thread. Whenever the UI thread asks
> for the current time, it gets a consistent answer. […] otherwise timers and animations may not all
> be in sync.

* `Tick()` — scheduling thread only; `m_lastReportedTime = GetNextTickTimeInSeconds()`
  (`RefreshAlignedClock.cpp:71-86`).
* `GetNextTickTimeInSeconds()` — a live QPC read via `IPALClock`, monotonic-clamped
  (`RefreshAlignedClock.cpp:93-118`).
* `GetLastTickTimeInSeconds()` — the cached value every UI-thread consumer sees
  (`RefreshAlignedClock.cpp:124-130`).

The backing clock is raw QPC with an arbitrary process-start epoch:

```cpp
// dxaml/xcp/plat/win/browserdesktop/xcpwin.cpp:1820-1821 (CWinClock ctor)
QueryPerformanceCounter(&m_lTimeStart);
QueryPerformanceFrequency(&m_lFreq);

// dxaml/xcp/plat/win/browserdesktop/xcpwin.cpp:1835-1845
XDOUBLE CWinClock::GetAbsoluteTimeInSeconds()
{
    QueryPerformanceCounter(&lTimeEnd);
    rTime = XDOUBLE(lTimeEnd.QuadPart - m_lTimeStart.QuadPart) / XDOUBLE(m_lFreq.QuadPart);
    return rTime;
}
```

So: **there is no vsync, present-target or DWM quantity anywhere in the value.** It is
"QPC at the instant the scheduling thread got here", with the *only* smoothing being that all
UI-thread consumers within a frame share one snapshot.

The "one timestamp per frame" property is real and worth keeping in mind — it is the same property
Uno gets from `Compositor.skia.cs:310-313`. It removes intra-frame skew between drivers. It does
nothing about inter-frame phase jitter.

### 2.2 Where in the frame the snap happens — the decisive ordering

`CompositorScheduler::RenderThreadFrame()` (`dxaml/xcp/core/compositor/compositorscheduler.cpp:303-436`),
one iteration of the scheduling thread's `while (!m_fShutdownThread) { RenderThreadFrame(); }` loop
(`:274-277`):

```cpp
// compositorscheduler.cpp:311-352 (abridged)
auto lock = m_pDrawListsLock.lock();

// Move the clock forward.
const XDOUBLE frameTickTime = m_pClock->Tick();                      // :317   <-- QPC snapped HERE

const XUINT32 uiThreadRequest =
    m_pUIThreadSchedulerNoRef->GetScheduledIntervalInMilliseconds(frameTickTime);   // :330

if (uiThreadRequest > 0) { timeToNextWorkInMilliseconds = uiThreadRequest; }
else
{
    IFC(m_scheduler->OnImmediateUIThreadFrame());                    // :345   <-- vblank wait HERE
    IFC(m_pUIThreadSchedulerNoRef->QueueTick());                     // :348
}
```

`Tick()` is at `:317`; the vblank wait is at `:345`. **The clock is snapped before the wait.**

`Scheduler::OnImmediateUIThreadFrame()` (`dxaml/xcp/components/graphics/Scheduler.cpp:24-56`) is the
wait, and it is *conditional*:

```cpp
// Scheduler.cpp:19-22
bool Scheduler::ShouldWaitForVBlank(float currentTimeInMilliseconds, float previousFrameTimeInMilliseconds, float refreshIntervalInMilliseconds)
{
    return (currentTimeInMilliseconds - previousFrameTimeInMilliseconds) < refreshIntervalInMilliseconds;
}

// Scheduler.cpp:43-53
float currentTimeInMilliseconds = m_clock->GetAbsoluteTimeInMilliseconds();
float refreshIntervalInMilliseconds = m_refreshRateInfo->GetRefreshIntervalInMilliseconds();
if (Scheduler::ShouldWaitForVBlank(currentTimeInMilliseconds, m_previousFrameTimeInMilliseconds, refreshIntervalInMilliseconds))
{
    IFC_RETURN(m_refreshRateInfo->WaitForRefreshInterval());
    m_previousFrameTimeInMilliseconds = m_clock->GetAbsoluteTimeInMilliseconds();
}
else
{
    m_previousFrameTimeInMilliseconds = currentTimeInMilliseconds;
}
```

and its comment (`Scheduler.cpp:26-42`) explains that the wait is deliberately *skipped* whenever a
whole refresh interval has already elapsed, because `WaitForCompositorClock` "prevents the display
from going into a low-power state".

`RefreshRateInfo::WaitForRefreshInterval()` (`dxaml/xcp/core/compositor/RefreshRateInfo.cpp:142-190`)
is `DCompositionWaitForCompositorClock(0, dummyHandles, 80 /*timeout ms*/)`, dynamically resolved from
`dcomp.dll` (`:78-90`, `:109-112`), with `Sleep(refreshInterval)` as fallback (`:185`).
The refresh interval itself comes from `IDCompositionDevice::GetFrameStatistics` →
`currentCompositionRate`, pushed UI-thread → `RefreshRateInfo` on every commit
(`dxaml/xcp/components/comptree/DCompTreeHost.cpp:1105-1130`). **Note what is used and what is thrown
away there: XAML reads `DCOMPOSITION_FRAME_STATISTICS` and consumes only `currentCompositionRate`.
`lastFrameTime` and `nextEstimatedFrameTime` — quantities (a) and (b) — are in the same struct and are
discarded.**

### 2.3 Consequence

In the steady animating state the scheduling thread's loop is:

```
Tick()  →  wait for compositor clock  →  QueueTick()  →  sleep on m_pCompositorWait
                                                             ↑
                              UI thread ticks, and its EndTick() wakes the scheduling thread
                              (UIThreadScheduler.cpp:230-232)
```

so the `Tick()` at the top of iteration *N+1* is snapped at the moment the UI thread *finished* frame
*N* — i.e. it carries frame *N*'s measure/arrange cost, exactly the way Uno's record-time read does.
The subsequent vblank wait re-aligns *when the tick is queued*, but the timestamp already handed to
the timing tree was taken before it.

There is even a `TODO` acknowledging that this is the wrong quantity
(`dxaml/xcp/core/compositor/UIThreadScheduler.cpp:178-179`):

```cpp
// TODO: TICK: Do we really want the UI thread to tick to whenever the last compositor tick was?
// TODO: TICK: It might be better to move non-animated values forward by a number of frames in anticipation of when they'll hit the screen.
```

**Verdict: WinUI's UI-thread clock is NOT a uniform frame-target time.** Any WinUI animation that is
evaluated on the UI thread (a `Storyboard` that failed to convert to a Composition animation, a
`DispatcherTimer`-driven curve, a `CompositionTarget.Rendering` handler) is exposed to the same phase
jitter. This is a genuine finding and it means "WinUI does it this way" is *not* an argument for
keeping Uno's clock as a plain record-time read.

---

## 3. `CompositionTarget.Rendering` / `RenderingEventArgs.RenderingTime`

### 3.1 WinUI 3: it is the tick time, i.e. quantity (a)-ish, definitely not (b)

The whole implementation:

```cpp
// dxaml/xcp/core/dll/xcpcore.cpp:6485-6492 (inside CCoreServices::NWDrawTree)
if (m_fWantsRenderingEvent && m_pTimeManager != NULL)
{
    IFC(CallPerFrameCallback(m_pTimeManager->GetLastTickTime()));
    IFC(pLayoutManager->UpdateLayout(uLayoutWidth, uLayoutHeight));
}

// dxaml/xcp/core/dll/xcpcore.cpp:4481-4496
_Check_return_ HRESULT CCoreServices::CallPerFrameCallback(XFLOAT time)
{
    ...
    ts.Duration = static_cast<INT64>(time * 1000 * 10000);   // seconds → 100ns ticks
    IFCFAILFAST(pArgs->put_RenderingTime(ts));
```

and `CTimeManager::GetLastTickTime()` is just the cached clock minus the time-manager epoch:

```cpp
// dxaml/xcp/core/animation/timemgr.cpp:257-261
if (!newTimelinesOnly && !m_lockTimeToZero)
{
    m_rLastTickTime = m_pIClock->GetLastTickTimeInSeconds() - m_rTimeStarted;
}
```

`m_rTimeStarted` is itself a `GetLastTickTimeInSeconds()` snapshot taken the first time the time
manager runs (`dxaml/xcp/components/animation/TimeManager.cpp:178`).

So in WinUI 3, **`RenderingTime` = (QPC at the scheduling thread's clock snap for this frame) − (QPC at
first tick)**. It is a *frame-start-ish* time, on an app-relative epoch, and it is **not** a predicted
presentation time. The header comment on `GetLastTickTime` even warns
(`dxaml/xcp/core/animation/timemgr.cpp:199-207`): "This time is _not safe_ to use any time on the UI
thread […] only reliable inside frame generation after the TimeManager has been ticked."

`CompositionTarget.Rendered` is the companion, and it carries only `FrameDuration` — the *measured*
cost of the UI-thread frame, computed as `now − rFrameStartTime`
(`dxaml/xcp/core/dll/xcpcore.cpp:6785-6792`). That is neither (a), (b) nor (c) — it is a duration, not
a presentation time.

### 3.2 WPF: the same-named property means something different — quantity (b)

```csharp
// wpf/PresentationCore/System/Windows/Media/MediaContext.cs:1814-1819
// The RenderingEventArgs class stores the next estimated presentation time.
// Since the TimeManager has just ticked, LastTickTime is exactly this time.
Rendering?.Invoke(this.Dispatcher, new RenderingEventArgs(_timeManager.LastTickTime));
```

and the estimate it refers to (`MediaContext.cs:1151-1181`):

```csharp
nextVsyncTicks       = countsTicks + TicksUntilNextVsync(countsTicks);
nextPresentationTicks = nextVsyncTicks + (vsyncAdvance * RefreshPeriod);

// If we had previously estimated the next presentation time and that estimate still seems
// reasonable then use the previous estimate rather than the newly computed value. This is a good
// performance win because it means we will tick animations and thus run layout to the same value as
// last time [...] we will consider the previous estimate "reasonable" if it falls within 1/2 frame.
if ((nextPresentationTicks - _estimatedNextPresentationTime.Ticks) * _animationRenderRate > TimeSpan.FromMilliseconds(500).Ticks)
{
    _estimatedNextPresentationTime = TimeSpan.FromTicks(nextPresentationTicks);
}
```

The grid is anchored on `_lastPresentationTime`, a QPC value the compositor (UCE) reports back after a
present — quantity (c) used as a phase anchor (`MediaContext.cs:745-746`, `:790-791`,
`:921` `TicksSinceLastPresent`, `:932` `TicksSinceLastVsync`, `:941-943` `TicksUntilNextVsync`,
`:905-909` `RefreshPeriod`). Its own vsync source is `DwmGetCompositionTimingInfo` →
`info.qpcVBlank`, taken on the render thread right after `DwmpFlush`:

```cpp
// wpf/WpfGfx/core/uce/rendertargetmanager.cpp:1196-1215
hr = m_pfnDwmGetCompositionTimingInfo(NULL, &info);
...
*puiRefreshRate       = info.rateCompose.uiNumerator / info.rateCompose.uiDenominator;
*pqpcPresentationTime = info.qpcVBlank;
```

**This is the architecture Uno's new estimator is reinventing** — grid + period + half-frame hysteresis
so the value does not twitch — except WPF's grid has a real anchor and a real period instead of a
median of record deltas. Worth noting for us: WPF's hysteresis is *stickiness of the whole estimate*
(reuse the previous value if within half a frame), not a proportional pull (`error/16`); it
deliberately produces byte-identical tick times across frames when the phase has not moved, which is
even stronger than what Uno currently does.

---

## 4. Where WinUI actually evaluates inertia (and why the UI clock does not matter there)

### 4.1 dxaml `ScrollViewer` → DirectManipulation

Pan and inertia are owned by DirectManipulation. XAML publishes a WUC `ExpressionAnimation` bound to a
DManip-owned shared transform, so once committed, every pan/inertia frame is produced by the
compositor with no UI-thread involvement (`dxaml/xcp/core/hw/DManipData.cpp:152-183`,
`dxaml/xcp/core/hw/ManipulationTransform.cpp:82-137`,
`dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp:2306-2390`).

XAML's *only* timing input to DManip is a constant:

```cpp
// dxaml/xcp/core/compositor/CompositorDirectManipulationViewport.cpp:57-63
// TODO - Jupiter (Windows) bug 847117. Replace 16 with the actually milliseconds until the transform is shown on screen
IGNOREHR(pCompositorService->UpdateCompositorContentTransform(
    pCompositorContent,
    16 /*deltaCompositionTime*/
    ));
```

surfaced to DManip through the frame-info provider — note the doc comment, which is precisely the
"predicted presentation time" concept:

```cpp
// dxaml/xcp/plat/win/browserdesktop/DirectManipulationFrameInfoProvider.cpp:41-71
//  Synopsis:
//    Called when DM needs to be given the time the next frame is going to
//    be shown on the screen.
IFACEMETHODIMP CDirectManipulationFrameInfoProvider::GetNextFrameInfo(
    _Out_ XUINT64* pTime, _Out_ XUINT64* pProcessTime, _Out_ XUINT64* pCompositionTime)
{
    *pTime = 0;
    *pProcessTime = 0;
    *pCompositionTime = m_pDMService->GetDeltaCompositionTime();   // == 16 ms
```

XAML returns `0` for the absolute time and process time — i.e. it declines to state a frame time and
lets DManip use its own clock — and supplies only a fixed 16 ms "it will be on screen this long from
now" latency, with a live TODO admitting the constant is wrong. **What DManip does with those values,
and what clock it evaluates the inertia curve against, is inside `directmanipulation.dll` and is not in
these sources — UNVERIFIED.**

(`CCompositorDirectManipulationViewport::UpdateTransform()` has no callers in this tree; the path is
vestigial in the lifted architecture.)

### 4.2 WinUI 3 `ScrollPresenter` → InteractionTracker

`controls/dev/ScrollPresenter/` contains only *clients* of `InteractionTracker`
(`InteractionTrackerOwner.cpp/.h`, `InteractionTrackerAsyncOperation.cpp/.h`, `ScrollPresenter.cpp`).
`find . -iname "*InteractionTracker*"` in `dxaml` returns only those files plus
`SwipeControl/SwipeControlInteractionTrackerOwner.*`. **There is no InteractionTracker implementation
in the public WinUI sources**; it lives in the lifted compositor (`Microsoft.UI.Composition`, shipped as
a binary in the Windows App SDK, backed by dcomp/dwmcore).

Consequently:

* The clock that `InteractionTracker` inertia and WUC `KeyFrameAnimation`s are evaluated against is
  **not in any source available here — UNVERIFIED.**
* What *is* verified is the boundary: XAML never passes a timestamp into it. XAML creates the
  animation, calls `Start`, and thereafter only *seeks* it to a XAML-relative offset when the app
  seeks/pauses/resumes a Storyboard (`dxaml/xcp/components/animation/DoubleAnimation.cpp:727-751`,
  `dxaml/xcp/components/animation/Timeline.cpp:247-278`,
  `dxaml/xcp/components/animation/TimeManager.cpp:300-331`). Begin times are expressed as
  `IKeyFrameAnimation::put_DelayTime` relative offsets
  (`dxaml/xcp/components/animation/DCompAnimationConversionContext.cpp:321-330`), never as absolute
  times on XAML's clock.
* The one hint in the tree that XAML *knows* the compositor evaluates on its own clock and that the
  handoff instant matters is `dxaml/xcp/core/dll/xcpcore.cpp:7191-7192`: "This should happen as late as
  possible before submitting the frame, so we can account for as much of the UI thread frame cost as we
  can when adjusting animation start times."

**Answer to the decisive question:** WinUI does not sample inertia at a uniform frame-target time *on
the UI thread* — it does not sample inertia on the UI thread at all. The compositor samples it, once
per composition frame. Whether the compositor uses `targetTime`, `startTime`, or something else is
**UNVERIFIED** (closed dcomp/dwmcore). What *is* measured (§5) is that the compositor's own frame grid
is uniform to ~±20 µs, so whichever of those it uses, the curve is sampled on a clean grid.

---

## 5. What the OS will actually tell us — runtime-verified

Probe: `winuiclock.cs`, `winuiclock2.cs` (scratchpad), plain `net10.0` console, this machine.

### 5.1 `DCompositionGetFrameId` + `DCompositionGetStatistics` work from any process/thread

`dcomp.dll` exports both; no device, no window, no COM apartment needed. Direct `DllImport` succeeded
from a console app.

Woken by `DCompositionWaitForCompositorClock(0, NULL, 200)` in a loop, then reading the `CREATED`
frame's stats (all columns in ms, relative to the `Stopwatch.GetTimestamp()` taken at the wake):

```
wait#  wake_delta_ms  frameId  start-wake_ms  target-wake_ms  period_ms
   0  r=0x0       0.000     2356559      -2.708       0.048    8.3334
   1  r=0x0       8.372     2356559     -11.080      -8.324    8.3334
   4  r=0x0       8.362     2356560      -5.621       0.004    8.3333
   7  r=0x0       8.289     2356561      -1.403       0.042    8.3333
   8  r=0x0       8.440     2356562      -8.347      -0.066    8.3333
   9  r=0x0       8.235     2356563      -8.260       0.032    8.3333
  11  r=0x0       8.331     2356564      -4.805       0.028    8.3333
```

* `framePeriod` = 83 333–83 334 QPC counts = **8.3333 ms = 120 Hz**, matching this display.
* Each compositor-clock wake lands within **±0.07 ms** of the newly-created frame's `targetTime`.
* `startTime` precedes `targetTime` by a *variable* 1.4–8.3 ms — i.e. `startTime` is when the compositor
  began the frame (jittery) and `targetTime` is the grid value (clean). **Use `targetTime`.**
* When nothing is being composited, the frame id does not advance and `targetTime` goes stale (rows 1–3
  repeat frame 2356559 with `target-wake` marching backwards by exactly one period each wake). This
  matters: the API is only live while frames are actually being produced. During a scroll we are
  presenting every frame, so it is live; but the consumer must handle a stale id.

### 5.2 `targetTime` is an exact grid; a UI-thread clock read is not

`winuiclock2.cs` waits on the compositor clock, then burns a *deliberately variable* 0–3 ms of
"record cost" before sampling `Stopwatch.GetTimestamp()` — simulating exactly what Uno's record does —
and records both series for each distinct frame id (141 frames):

```
i   targetDelta_ms   uiSampleDelta_ms   (target-uiSample)_ms
  1        25.0135            17.6339               7.0949
  2         8.3479             9.0062               6.4366
  3        16.6673            17.3781               5.7258
  4         8.3334             8.7743               5.2849
  9        41.6469            35.2749               6.3612
 13        33.3215            26.4781              -2.0138
 19        58.3485            26.6214              -2.2712
 ...
target delta:    mean=23.7497 ms  stddev=14.1538 ms  min=0.0000 max=75.0110
uiSample delta:  mean=23.7097 ms  stddev=10.4790 ms  min=5.4031 max=49.6136
```

The stddevs are not the point (the console app composites irregularly, so frames are multiple periods
apart). **The point is quantization.** Every non-zero `targetDelta` is an integer multiple of the
8.3334 ms period:

| targetDelta (ms) | n × 8.3334 | residual |
|---|---|---|
| 8.3334 | 1× = 8.3334 | 0.0000 |
| 16.6673 | 2× = 16.6668 | +0.0005 |
| 25.0135 | 3× = 25.0002 | +0.0133 |
| 33.3222 | 4× = 33.3336 | −0.0114 |
| 41.6469 | 5× = 41.6670 | −0.0201 |
| 58.3485 | 7× = 58.3338 | +0.0147 |
| 75.0110 | 9× = 75.0006 | +0.0104 |

**Residuals ≤ 20 µs.** The `uiSampleDelta` column over the same frames has no such structure — it is
the raw record instant, and it wanders by milliseconds. That is the defect, measured, and the fix,
measured, side by side.

(`targetDelta == 0.0000` rows are two distinct frame ids sharing one `targetTime`; a consumer must
therefore key on `targetTime`, not on frame id, and must tolerate a repeat.)

### 5.3 `DwmGetCompositionTimingInfo` — do not build on it

`DwmGetCompositionTimingInfo(NULL, &ti)` returned **`0x88980090`** (facility 2200 =
`FACILITY_WINCODEC_DWRITE_DWM` per `sdk/shared/winerror.h:194`; code `0x0090` is not defined in
`winerror.h`) from a console process on this build, with all output fields zero. Whether it succeeds
from a process that owns a real composed HWND on this build is **UNVERIFIED** — but WPF's own code
already treats failure as routine and falls back
(`wpf/WpfGfx/core/uce/rendertargetmanager.cpp:1204-1210`), and the DComp path above worked
unconditionally. Prefer DComp.

---

## 6. The epoch question — verified, not assumed

**On Windows, `Stopwatch.GetTimestamp()` *is* `QueryPerformanceCounter`, same value, same epoch.**
Measured by bracketing (`winuiclock.cs`):

```
QueryPerformanceFrequency = 10000000
Stopwatch.Frequency       = 10000000
Stopwatch.IsHighResolution= True

qpc=430898260946  sw=430898260976  qpc=430898260976   sw-a=30  b-sw=0  bracketed=True
qpc=430898260995  sw=430898260995  qpc=430898260995   sw-a=0   b-sw=0  bracketed=True
qpc=430898261002  sw=430898261002  qpc=430898261002   sw-a=0   b-sw=0  bracketed=True
```

Three interleaved reads, `Stopwatch` bracketed by QPC every time with 0–30 counts (0–3 µs) of
difference — i.e. the same counter read at slightly different instants, not two clocks.

`COMPOSITION_FRAME_STATS` values sit on that same timeline: in §5.1, `targetTime − Stopwatch.GetTimestamp()`
at the wake is ≈ 0 ms, and in §5.3's DWM row the nonsense `43 089 839 ms` offset is simply
`qpcVBlank == 0` from the failed call — the DComp numbers, by contrast, are immediately meaningful
against `Stopwatch`. **No offset is needed on Windows.**

For Uno specifically: `Compositor.TimestampInTicks` is
`(long)(Stopwatch.GetTimestamp() * s_tickFrequency)` where
`s_tickFrequency = TimeSpan.TicksPerSecond / Stopwatch.Frequency`
(`uno/src/Uno.UI.Composition/Composition/Compositor.cs:33-38`). On this machine
`Stopwatch.Frequency == 10 000 000 == TimeSpan.TicksPerSecond`, so `s_tickFrequency == 1.0` and
`TimestampInTicks` is **literally the raw QPC count**. A `COMPOSITION_FRAME_STATS.targetTime` can be
substituted for it with zero conversion.

**Caveat, must be handled:** `Stopwatch.Frequency` is documented (and coded) to be QPF on Windows, but
QPF is not guaranteed to be 10 MHz on every machine — on hardware where it differs, `s_tickFrequency != 1`
and the DComp value must be scaled the same way: `(long)(targetTime * s_tickFrequency)`. Do not assume
1.0. (I read this from Uno's own source comment at `Compositor.cs:32-37`, which already flags exactly
this; I did not read the .NET runtime source — the runtime's Windows `Stopwatch` implementation being a
straight `QueryPerformanceCounter`/`QueryPerformanceFrequency` pair is **inferred from the measurement
above plus that comment**, not read, so call the *mechanism* UNVERIFIED even though the *equality* is
measured.)

---

## 7. What Uno already receives and throws away

**Win32 — the vsync instant is in hand and discarded.**
`Win32RenderPacer.WaitForNextFrame()` calls `PInvoke.DwmFlush()`
(`uno/src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:61`), which blocks until the DWM
composition frame boundary. The moment it returns is a vsync-aligned instant on the render thread —
quantity (a), free — and the code does nothing with it but check the HRESULT (`:62-81`). Callers
likewise just loop: `uno/src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32WindowWrapper.Rendering.Software.cs:102`,
`.../Win32WindowWrapper.Rendering.Vulkan.cs:27`, `.../Win32WindowWrapper.Rendering.OpenGl.cs:148`.

**Meanwhile the drivers are fed a UI-thread clock read**, at
`uno/src/Uno.UI.Composition/Composition/Compositor.skia.cs:312`
(`GetFrameTimestamp(TimestampInTicks)` inside `RenderRootVisual`), reconstructed by the estimator at
`Compositor.skia.cs:244-299`.

So on Win32 today: a clean vsync edge exists on the render thread and is dropped; a dirty instant is
sampled on the UI thread and phase-locked back into (approximately) that same grid. The DComp API
replaces the reconstruction with the stated value and does not even require the render thread to hand
anything across — it can be read directly on the UI thread at `Compositor.skia.cs:312`.

`uno/src/Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt:174-180` already lists `DwmFlush` and
friends for CsWin32, so adding `DCompositionGetFrameId` / `DCompositionGetStatistics` /
`DCompositionWaitForCompositorClock` is a three-line change *if* the Win32 metadata exposes them
(**UNVERIFIED** — the metadata package is not restored in this worktree). If it does not, a hand-written
`[DllImport("dcomp.dll")]` works: that is exactly what both probes used.

---

## 8. Recommendation for Uno (Win32/Skia)

1. On the UI thread at `Compositor.skia.cs:312`, before raising `FrameStarting`:
   `DCompositionGetFrameId(COMPOSITION_FRAME_ID_CREATED, out id)` →
   `DCompositionGetStatistics(id, out stats, 0, null, null)`.
2. Frame timestamp = `stats.targetTime` scaled by `s_tickFrequency` (see §6). Frame period =
   `stats.framePeriod` — this also gives `FrameIntervalInTicks`
   (`Compositor.skia.cs:220-222`) a stated value instead of a median of record deltas, and it tracks
   VRR/refresh changes for free.
3. **Keep the estimator as the fallback**, and gate on the two failure modes actually observed:
   the call failing (any HRESULT ≠ 0 → fall back), and the frame id / `targetTime` not advancing
   (§5.1 rows 1–3 — the compositor is idle; advance the previous value by `framePeriod` or fall back).
   Feeding a stale `targetTime` straight into `x(t)` would freeze the fling.
4. Because `targetTime` is a *presentation target* and the current clock read is a *record* time, the
   value will jump forward by roughly one to two frame periods on switchover. That is a one-time phase
   step, not an error — but it must not land mid-fling (it would show as a single position jump). Take
   the offset at driver-start, or accept it only while `FrameStarting` has no subscribers.
5. **Do not copy WinUI here.** §2 shows WinUI's UI-thread clock is the same jittery quantity Uno has;
   copying it buys nothing. Copy WPF's shape (§3.2) — grid + hysteresis — but with DComp's stated
   `targetTime` where WPF had to derive one from `qpcVBlank` + present callbacks.

---

## 9. Explicitly UNVERIFIED

* The clock `InteractionTracker` inertia and WUC `KeyFrameAnimation`s are evaluated against. Not in
  `dxaml`; lives in the closed lifted compositor / dcomp / dwmcore. Only the *boundary* (XAML supplies
  no absolute time) is verified.
* Whether DirectManipulation uses the 16 ms `compositionTime` as a lead, and what clock it evaluates
  inertia against. Inside `directmanipulation.dll`.
* Documented semantics of `COMPOSITION_FRAME_STATS.startTime` vs `targetTime`. The SDK header has no
  field comments; my reading ("compositor began the frame" vs "vsync grid target") is inferred from the
  measurements in §5.1/§5.2, which are themselves solid.
* Whether `DwmGetCompositionTimingInfo` succeeds from a process owning a composed HWND on this
  Windows build. It failed with `0x88980090` from a console process.
* Whether the CsWin32 Win32 metadata Uno consumes exposes the three `DComposition*` entry points.
* Quantity (c) (`DCompositionGetTargetStatistics` → `presentTime`) was not probed.
* The .NET runtime's Windows `Stopwatch` implementation was not read; the QPC equality in §6 is
  measured, the mechanism is inferred.
* Non-Windows targets are out of scope for this note (Android's `Choreographer` frame-time story is
  covered elsewhere in this folder's series).
