# Modern WinUI `ScrollView` / `ScrollPresenter` — How Smooth Scrolling Is Actually Achieved

Research note. All claims below are grounded in real source read from
`D:/Work/microsoft-ui-xaml2` (WinUI 3 / microsoft-ui-xaml repo, `controls/dev/ScrollPresenter/`,
`controls/dev/ScrollView/`, `controls/dev/AnnotatedScrollBar/`) and, where the Uno comparison is
made, `D:/Work/uno-worktrees/scrollsmooth/src`.

Citations are `path:line`. Anything I could not verify in source is explicitly labelled
**UNVERIFIED**.

---

## 0. Executive summary — the smoothness thesis in one paragraph

Modern WinUI scrolling is smooth because **the UI thread is not in the loop for the visual
transform**. A single `InteractionTracker` (a Composition object living on the compositor /
animation thread) owns the authoritative `Position` and `Scale`. Two `ExpressionAnimation`s bind
`Content.Translation` and `Content.Scale` to `it.Position` / `it.Scale`, and those are evaluated by
the compositor on *its* clock. Input (touch, precision-touchpad, mouse wheel) is *redirected* into
the tracker via `VisualInteractionSource.TryRedirectForManipulation` /
`ManipulationRedirectionMode = CapableTouchpadAndPointerWheel`, so a pan or wheel spin never round
trips through XAML. Inertia, boundaries, snap points, and rails are all expressed *declaratively* as
Composition objects (`InteractionTrackerInertiaRestingValue` + `ExpressionAnimation` conditions,
`CompositionConditionalValue` modifiers) evaluated on the compositor thread. The UI thread only
receives *notifications* (`ValuesChanged`, state-entered callbacks) to update `HorizontalOffset`,
scrollbars, automation, and to run virtualization/anchoring — and if the UI thread stalls, the
content keeps moving smoothly anyway. The single deliberate exception is a 4-tick stop/restart of
the transform animations after idle, purely to force content re-rasterization at the new zoom
factor.

**What breaks it (per this codebase's own design):** doing input math on the UI thread; a
per-frame UI-thread callback driving the transform; recreating/restarting expression animations
while moving; letting extent/viewport changes race the tracker (WinUI explicitly delays operations
3 UI ticks for this); and per-tick reallocation of inertia modifiers.

---

## 1. Which Composition objects carry the scroll

### 1.1 Object graph

| Role | Object | Created at |
|---|---|---|
| Authoritative scroll state | `winrt::InteractionTracker` (`m_interactionTracker`) | `ScrollPresenter.cpp:1873` |
| Owner callback sink | `InteractionTrackerOwner` (`IInteractionTrackerOwner`) | `ScrollPresenter.cpp:1870` |
| Touch / touchpad / wheel input source | `VisualInteractionSource` on the **ScrollPresenter's own visual** (`m_scrollPresenterVisualInteractionSource`) | `ScrollPresenter.cpp:1886-1888` |
| Optional scroll-controller input source | `VisualInteractionSource` on the controller's `PanningElementAncestor` visual | `ScrollPresenter.cpp:1903-1909` |
| Public read-only view state | `CompositionPropertySet m_expressionAnimationSources` | `ScrollPresenter.cpp:1831-1838` |
| Content transform | 2 × `ExpressionAnimation` (`m_translationExpressionAnimation`, `m_zoomFactorExpressionAnimation`) | `ScrollPresenter.cpp:2019, 2024` |
| Boundaries | 2 × `ExpressionAnimation` (`m_minPositionExpressionAnimation`, `m_maxPositionExpressionAnimation`) | `ScrollPresenter.cpp:2000, 2004` |

The tracker is created *with an owner* so the state callbacks come back:

```cpp
// ScrollPresenter.cpp:1870-1873
m_interactionTrackerOwner = winrt::make_self<InteractionTrackerOwner>(*this).try_as<winrt::IInteractionTrackerOwner>();
const winrt::Compositor compositor = winrt::ElementCompositionPreview::GetElementVisual(*this).Compositor();
m_interactionTracker = winrt::InteractionTracker::CreateWithOwner(compositor, m_interactionTrackerOwner);
```

The `VisualInteractionSource` is created on the **ScrollPresenter's** visual, not the content's:

```cpp
// ScrollPresenter.cpp:1885-1888
const winrt::Visual scrollPresenterVisual = winrt::ElementCompositionPreview::GetElementVisual(*this);
winrt::VisualInteractionSource scrollPresenterVisualInteractionSource = winrt::VisualInteractionSource::Create(scrollPresenterVisual);
m_interactionTracker.InteractionSources().Add(scrollPresenterVisualInteractionSource);
m_scrollPresenterVisualInteractionSource = scrollPresenterVisualInteractionSource;
```

Note also: the ScrollPresenter constructor forces a transparent background specifically so
hit-testing succeeds and a manipulation can start over empty area:

```cpp
// ScrollPresenter.cpp:55-57
// Set the default Transparent background so that hit-testing allows to start a touch manipulation
// outside the boundaries of the Content, when it's smaller than the ScrollPresenter.
Background(winrt::SolidColorBrush(winrt::Colors::Transparent()));
```

### 1.2 The transform expression animations — exact strings

Built in `SetupTransformExpressionAnimations` (`ScrollPresenter.cpp:3383-3424`).

**LTR (the common case), non-Image content:**

```
Vector3(-it.Position.X + (it.Scale - 1.0f) * adjustment.X, -it.Position.Y + (it.Scale - 1.0f) * adjustment.Y, 0.0f)
```
(`ScrollPresenter.cpp:3412`)

**RTL, non-Image content:**

```
Vector3(it.Position.X + (it.Scale - 1.0f) * adjustment.X, -it.Position.Y + (it.Scale - 1.0f) * adjustment.Y, 0.0f)
```
(`ScrollPresenter.cpp:3407`)

**RTL, `Image` content** (extra `contentSizeX` term):

```
Vector3(it.Position.X + (it.Scale - 1.0f) * (adjustment.X + contentSizeX), -it.Position.Y + (it.Scale - 1.0f) * adjustment.Y, 0.0f)
```
(`ScrollPresenter.cpp:3403`)

**Zoom:**

```
Vector3(it.Scale, it.Scale, 1.0f)
```
(`ScrollPresenter.cpp:3420`)

Reference parameters and the `adjustment` (arrange-vs-render size delta) are pushed once:

```cpp
// ScrollPresenter.cpp:3417-3421
m_translationExpressionAnimation.SetReferenceParameter(L"it", m_interactionTracker);
m_translationExpressionAnimation.SetVector2Parameter(L"adjustment", arrangeRenderSizesDelta);
m_zoomFactorExpressionAnimation.Expression(L"Vector3(it.Scale, it.Scale, 1.0f)");
m_zoomFactorExpressionAnimation.SetReferenceParameter(L"it", m_interactionTracker);
```

### 1.3 Which Visual property is targeted

`GetVisualTargetedPropertyName` (`ScrollPresenter.cpp:3741-3751`) returns:

* Scroll → `s_translationPropertyName` = **`"Translation"`** (`ScrollPresenter.h:928`)
* Zoom → `s_scalePropertyName` = **`"Scale"`** (`ScrollPresenter.h:929`)

And it is started **on the `UIElement` (content) directly**, not on a raw Visual:

```cpp
// ScrollPresenter.cpp:3434-3441
m_translationExpressionAnimation.Target(scrollPropertyName);
m_zoomFactorExpressionAnimation.Target(zoomFactorPropertyName);
content.StartAnimation(m_translationExpressionAnimation);
...
content.StartAnimation(m_zoomFactorExpressionAnimation);
```

Legacy RS1 fallback names still exist in the header but are unused by the current code path
(`ScrollPresenter.h:923-926`): `TransformMatrix._41/._42/._11/._22`.

**Consequence for smoothness:** because the expression is bound to `Translation` (a
composition-native, independently animatable property) and the target is the content element,
nothing about the scroll transform requires a XAML layout pass, a `RenderTransform`, or a
`UIElement` property write per frame.

### 1.4 Boundary expression animations

`SetupPositionBoundariesExpressionAnimations` (`ScrollPresenter.cpp:3350-3381`) binds
`InteractionTracker.MinPosition` / `MaxPosition` to expressions so the clamp itself lives on the
compositor and follows zoom + content alignment **without UI-thread involvement**. Reference params
are `it` (the tracker) and `scrollPresenterVisual`.

Representative LTR strings (`ScrollPresenter.cpp:3100-3247`):

* `GetMinPositionXExpression`, no FrameworkElement content → `"contentLayoutOffsetX"` (`:3142`)
* Center/Stretch H-alignment → `"Min(0.0f, (contentSizeX * it.Scale - scrollPresenterVisual.Size.X) / 2.0f) + contentLayoutOffsetX"` (`:3134`)
* `GetMaxPositionXExpression`, default → `"Max(0.0f, contentSizeX * it.Scale - scrollPresenterVisual.Size.X) + contentLayoutOffsetX"` (`:3221`)
* Center/Stretch H-alignment → `"(contentSizeX * it.Scale - scrollPresenterVisual.Size.X) >= 0 ? … + contentLayoutOffsetX : … / 2.0f + contentLayoutOffsetX"` (`:3213`)
* Y-axis mirrors these (`:3145-3168`, `:3224-3247`)
* Composed as `Vector3(<x>, <y>, 0.0f)` (`:3097`, `:3176`)

### 1.5 The `ExpressionAnimationSources` property set (public read-only mirror)

`EnsureExpressionAnimationSources` (`ScrollPresenter.cpp:1823-1861`) creates a
`CompositionPropertySet` with `Extent`, `Viewport`, `Offset`, `Position`, `MinPosition`,
`MaxPosition`, `ZoomFactor` (names at `ScrollPresenter.h:41-47`), and animates four of them off the
tracker:

```cpp
// ScrollPresenter.cpp:1846-1856
m_positionSourceExpressionAnimation    = compositor.CreateExpressionAnimation(L"Vector2(it.Position.X, it.Position.Y)");
m_minPositionSourceExpressionAnimation = compositor.CreateExpressionAnimation(L"Vector2(it.MinPosition.X, it.MinPosition.Y)");
m_maxPositionSourceExpressionAnimation = compositor.CreateExpressionAnimation(L"Vector2(it.MaxPosition.X, it.MaxPosition.Y)");
m_zoomFactorSourceExpressionAnimation  = compositor.CreateExpressionAnimation(L"it.Scale");
```

This is the sanctioned way for app code (parallax, sticky headers, `ParallaxView`) to react to
scroll **without any per-frame UI-thread callback**. `Extent`/`Viewport`/`Offset` are pushed from
the UI thread when layout changes (`UpdateExpressionAnimationSources`, `ScrollPresenter.cpp:5373`).

### 1.6 Per-scroll-controller property set (drives scrollbar thumbs on the compositor)

`EnsureScrollControllerExpressionAnimationSources` (`ScrollPresenter.cpp:1933-1988`) creates a
per-dimension property set with `MinOffset`, `MaxOffset`, `Offset`, `Multiplier`
(`ScrollPresenter.h:932-935`) and animates:

```cpp
// ScrollPresenter.cpp:1973-1986
m_horizontalScrollControllerOffsetExpressionAnimation    = "it.Position.X - it.MinPosition.X"
m_horizontalScrollControllerMaxOffsetExpressionAnimation = "it.MaxPosition.X - it.MinPosition.X"
m_verticalScrollControllerOffsetExpressionAnimation      = "it.Position.Y - it.MinPosition.Y"
m_verticalScrollControllerMaxOffsetExpressionAnimation   = "it.MaxPosition.Y - it.MinPosition.Y"
```

`AnnotatedScrollBar` consumes it to drive its thumb entirely on the compositor
(`AnnotatedScrollBarPanningInfo.cpp:202-210`):

```
min(sources.MaxOffset,max(sources.MinOffset,sources.Offset))/(-sources.Multiplier)
```
started on `Translation.Y` of the thumb visual (`AnnotatedScrollBarPanningInfo.cpp:188`).

> Note: the *plain* `ScrollView` scrollbars do **not** use this path —
> `ScrollBarController::PanningInfo()` returns `nullptr` (`ScrollBarController.cpp:42-45`), so the
> classic `ScrollBar` thumb is positioned on the UI thread via `ScrollBar.Value`. See §7.

---

## 2. Thread affinity — who does what per frame

### 2.1 Compositor / animation thread (every frame)

Evaluates:
1. `InteractionTracker` state machine + inertia integration (Windows-internal; **UNVERIFIED** in
   this repo — the Composition implementation is not part of microsoft-ui-xaml sources).
2. `m_translationExpressionAnimation` → `Content.Translation`
3. `m_zoomFactorExpressionAnimation` → `Content.Scale`
4. `m_minPositionExpressionAnimation` / `m_maxPositionExpressionAnimation` → tracker bounds
5. The four `ExpressionAnimationSources` animations
6. The scroll-controller `Offset`/`MaxOffset` animations
7. All `InteractionTrackerInertiaRestingValue` condition + resting-value expressions (snap points)
8. All `CompositionConditionalValue` `DeltaPosition*`/`CenterPoint*` modifiers

### 2.2 UI thread during a **touch pan**

Per gesture start only:
* `ScrollPresenter::OnPointerPressed` (`ScrollPresenter.cpp:4553-4648`) walks ancestors checking
  `ManipulationModes::System` (`:4612-4617`) then calls
  `m_scrollPresenterVisualInteractionSource.TryRedirectForManipulation(args.GetCurrentPoint(nullptr))`
  (`:4636`). **This is the only per-pointer-press UI-thread work.** After redirection, subsequent
  pointer input is consumed by the compositor input path; XAML sees nothing.

Per *notification* (not per frame — these are tracker-driven callbacks that may coalesce):
* `ScrollPresenter::ValuesChanged` (`:1172-1239`): recomputes `m_zoomFactor`, calls
  `ComputeMinMaxPositions`, `UpdateOffset` ×2, then `OnViewChanged` if anything moved.
* `OnViewChanged` (`:5677-5695`) → `UpdateScrollControllerValues` (writes `ScrollBar.Value` etc.),
  `UpdateScrollAutomationPatternProperties` (raises UIA property-change events),
  `RaiseViewChanged` (public `ViewChanged` event → this is where `ItemsRepeater` virtualization and
  anchoring get scheduled).
* State transitions: `InteractingStateEntered` (`:1120`), `InertiaStateEntered` (`:1031`),
  `IdleStateEntered` (`:995`), `CustomAnimationStateEntered` (`:987`).

**Crucially: if the UI thread is blocked, none of the above runs, and the content still scrolls
smoothly** — offsets/scrollbars/virtualization just lag. That is the whole architectural payoff.

### 2.3 UI thread during **mouse wheel**

Nothing. There is **no** `PointerWheelChanged` handler in either `ScrollPresenter.cpp` or
`ScrollView.cpp` (grep across both files returns zero handler registrations; the only wheel-related
code is `#ifdef IsMouseWheelScrollDisabled` / `IsMouseWheelZoomDisabled` config, which are *not*
defined by default). Instead:

```cpp
// ScrollPresenter.cpp:2791-2804
void ScrollPresenter::SetupVisualInteractionSourceRedirectionMode(const winrt::VisualInteractionSource& visualInteractionSource)
{
    winrt::VisualInteractionSourceRedirectionMode redirectionMode = winrt::VisualInteractionSourceRedirectionMode::CapableTouchpadOnly;
    if (!IsInputKindIgnored(winrt::ScrollingInputKinds::MouseWheel))
    {
        redirectionMode = winrt::VisualInteractionSourceRedirectionMode::CapableTouchpadAndPointerWheel;
    }
    visualInteractionSource.ManipulationRedirectionMode(redirectionMode);
}
```

See §5 for the full discussion.

### 2.4 UI thread during a **programmatic animation** (`ScrollTo`/`ScrollBy`)

* `CompositionTarget.Rendering` is hooked (`HookCompositionTargetRendering`,
  `ScrollPresenter.cpp:7266-7275`) **only while operations are queued**, and unhooked as soon as the
  queue drains (`OnCompositionTargetRendering` → `UnhookCompositionTargetRendering`, `:4312-4315`).
* Each tick, `OnCompositionTargetRendering` (`:4213-4316`) walks `m_interactionTrackerAsyncOperations`,
  decrements tick countdowns, and eventually calls `ProcessDequeuedViewChange`, which issues a single
  `TryUpdatePositionWithAnimation(...)` and returns. **The animation itself then runs on the
  compositor**; the UI thread stops ticking (for animated ops,
  `unhookCompositionTargetRendering` stays `true`, `:4276-4279`).

So even a programmatic animated scroll costs the UI thread ~3 ticks of bookkeeping and then zero.

### 2.5 The one deliberate UI-thread per-frame cost: rasterization restart

```cpp
// ScrollPresenter.cpp:3497-3519  (StopTranslationAndZoomFactorExpressionAnimations)
if (m_zoomFactorExpressionAnimation && m_animationRestartZoomFactor != m_zoomFactor)
{
    if (m_translationAndZoomFactorAnimationsRestartTicksCountdown == 0)
    {
        // Stop Translation and Scale animations to trigger rasterization of Content, to avoid fuzzy text rendering for instance.
        StopTransformExpressionAnimations(content);
        // Trigger ScrollPresenter::OnCompositionTargetRendering calls in order to re-establish the Translation and Scale animations
        // after the Content rasterization was triggered within a few ticks.
        HookCompositionTargetRendering();
    }
    m_animationRestartZoomFactor = m_zoomFactor;
    m_translationAndZoomFactorAnimationsRestartTicksCountdown = s_translationAndZoomFactorAnimationsRestartTicks;
}
```

* Triggered from `IdleStateEntered` (`:1028`) — i.e. **after** motion stops.
* Guarded by `m_animationRestartZoomFactor != m_zoomFactor` — a pure scroll (no zoom change) does
  **not** pay this cost at all.
* Restart happens `s_translationAndZoomFactorAnimationsRestartTicks == 4` ticks later
  (`ScrollPresenter.h:79`) via `StartTranslationAndZoomFactorExpressionAnimations`
  (`ScrollPresenter.cpp:3463-3495`).

---

## 3. Default animations for `ScrollTo` / `ScrollBy` / `AddScrollVelocity`

### 3.1 `ScrollTo` / `ScrollBy` (the animated path)

`ScrollTo`/`ScrollBy` → `ChangeOffsetsPrivate` (`:484`, `:510`) → queued
`TryUpdatePositionWithAnimation` op → `ProcessOffsetsChange` (`:6451-6471`) →
`GetPositionAnimation` (`:3249-3311`).

```cpp
// ScrollPresenter.cpp:3257-3283
int64_t minDuration  = s_offsetsChangeMinMs;      // 50
int64_t maxDuration  = s_offsetsChangeMaxMs;      // 1000
int64_t unitDuration = s_offsetsChangeMsPerUnit;  // 5
const int64_t distance = static_cast<int64_t>(sqrt(pow(dx,2.0) + pow(dy,2.0)));
winrt::Vector3KeyFrameAnimation positionAnimation = compositor.CreateVector3KeyFrameAnimation();
...
positionAnimation.InsertKeyFrame(1.0f, winrt::float3(endPosition, 0.0f));
positionAnimation.Duration(winrt::TimeSpan::duration(std::clamp(distance * unitDuration, minDuration, maxDuration) * 10000));
```

Constants (`ScrollPresenter.h:68-70`):

| Constant | Value |
|---|---|
| `s_offsetsChangeMsPerUnit` | **5** ms per pixel of euclidean distance |
| `s_offsetsChangeMinMs` | **50** ms |
| `s_offsetsChangeMaxMs` | **1000** ms |

So duration = `clamp(round(hypot(Δx,Δy)) * 5ms, 50ms, 1000ms)`. Saturates at 1000 ms once the
distance ≥ 200 px. (`* 10000` converts ms → 100 ns `TimeSpan` ticks.)

**Easing: there is none supplied.**
`InsertKeyFrame(1.0f, endPosition)` is called with **no `CompositionEasingFunction` overload**
(`:3282`). There is **no `CubicBezierEasingFunction`, no `CreateCubicBezierEasingFunction`, and no
`EasingFunction` anywhere** in `controls/dev/ScrollPresenter/` or `controls/dev/ScrollView/`
production code — the only hits are in the test-UI samples
(`ScrollPresenter/TestUI/ScrollPresenterDynamicPage.xaml.cs:1677`,
`ScrollPresenter/TestUI/CompositionScrollController.cs:777`), which build *custom* animations to
demonstrate the `ScrollAnimationStarting` override.

Therefore the actual curve is **whatever the Compositor's default `KeyFrameAnimation` easing is**.
The Composition implementation is not in this repo → **UNVERIFIED** what those control points are.
The documented behaviour (not verifiable here) is a default cubic-bezier; do not treat any specific
control points as source-confirmed.

**The extension point is `ScrollAnimationStarting`:**

```cpp
// ScrollPresenter.cpp:3310  (non-scroll-controller case)
return RaiseScrollAnimationStarting(positionAnimation, startPosition, endPosition, offsetsChangeCorrelationId);
```
`RaiseScrollAnimationStarting` (`:7620-7650`) hands the app the stock `Vector3KeyFrameAnimation`
plus `StartPosition`/`EndPosition` and **uses whatever animation the handler leaves in
`args.Animation`** (`:7644`). `ScrollView` re-raises it verbatim (`ScrollView.cpp:952-962`).
Args shape: `ScrollingScrollAnimationStartingEventArgs.h:24-34`.

### 3.2 `ZoomTo` / `ZoomBy`

`GetZoomFactorAnimation` (`ScrollPresenter.cpp:3313-3343`), `ScalarKeyFrameAnimation`, single
keyframe at 1.0, no easing, duration
`clamp(|Δzoom| * s_zoomFactorChangeMsPerUnit, s_zoomFactorChangeMinMs, s_zoomFactorChangeMaxMs)`:

| Constant | Value | Cite |
|---|---|---|
| `s_zoomFactorChangeMsPerUnit` | **250** | `ScrollPresenter.h:73` |
| `s_zoomFactorChangeMinMs` | **50** | `ScrollPresenter.h:74` |
| `s_zoomFactorChangeMaxMs` | **1000** | `ScrollPresenter.h:75` |

Note `distance` is `int64_t(abs(zoomFactor - m_zoomFactor))` (`:3321`) — an integer truncation, so a
0.2 zoom delta yields distance 0 and therefore the **50 ms minimum**.

### 3.3 `AddScrollVelocity`

There is **no keyframe animation at all**. `AddScrollVelocity` (`:523-537`) →
`ChangeOffsetsWithAdditionalVelocityPrivate` (`:5939-6004`) → `ProcessOffsetsChange(…,
OffsetsChangeWithAdditionalVelocity)` (`:6476-6547`) → a single

```cpp
// ScrollPresenter.cpp:6542-6544
m_latestInteractionTrackerRequest = m_interactionTracker.TryUpdatePositionWithAdditionalVelocity(
    winrt::float3(offsetsVelocity, 0.0f));
```

The *shape* of the motion is then purely the tracker's inertia model (§4). This is what
`ScrollView` uses for keyboard/gamepad scrolling and what `ScrollBarController` uses for
`SmallIncrement`/`LargeIncrement` when animations are enabled.

### 3.4 Animation mode resolution

`GetComputedAnimationMode` (`:3700-3723`): `ScrollingAnimationMode::Auto` (the default,
`ScrollingScrollOptions.h:21`) resolves to `Enabled`/`Disabled` from
`SharedHelpers::IsAnimationsEnabled()` — i.e. the OS "Show animations in Windows" setting. With
animations off, everything degrades to non-animated `TryUpdatePosition` / `TryUpdatePositionBy`
(`:6393`, `:6426`) — instant, never janky-looking-but-slow.

---

## 4. Inertia: `TryUpdatePositionWithAdditionalVelocity`, decay rates, resting values

### 4.1 Decay rate

`InteractionTracker.PositionInertiaDecayRate` is a `float3` in [0,1]. ScrollPresenter's stated
default and the value it substitutes for "unspecified" is:

```cpp
// ScrollPresenter.cpp:29-31
// Default inertia decay rate used when a IScrollController makes a request for
// an offset change with additional velocity.
const float c_scrollPresenterDefaultInertiaDecayRate = 0.95f;
```

Applied / restored:

```cpp
// ScrollPresenter.cpp:6519-6531
if (inertiaDecayRate)
{
    const float horizontalInertiaDecayRate = std::clamp(inertiaDecayRate.Value().x, 0.0f, 1.0f);
    const float verticalInertiaDecayRate   = std::clamp(inertiaDecayRate.Value().y, 0.0f, 1.0f);
    m_interactionTracker.PositionInertiaDecayRate(winrt::float3(horizontalInertiaDecayRate, verticalInertiaDecayRate, 0.0f));
}
else
{
    // Restore the default 0.95 position inertia decay rate since it may have been overridden by a prior offset change with additional velocity.
    ResetOffsetsInertiaDecayRate();
}
```

`ResetOffsetsInertiaDecayRate` sets it to **`nullptr`** (i.e. back to system default), not to
0.95f literally (`:6773-6786`, key line `:6785`). Same for zoom:
`ResetZoomFactorInertiaDecayRate` → `ScaleInertiaDecayRate(nullptr)` (`:6789-6806`, `:6806`),
described in comments as the **0.985** default (`:1130`, `:6712`).

| Axis | Documented default decay rate | Cite |
|---|---|---|
| Position (X/Y) | **0.95** | `ScrollPresenter.cpp:31`, `:1127`, `:6529` |
| Scale | **0.985** | `ScrollPresenter.cpp:1130`, `:6712` |

Restoration also happens on `InteractingStateEntered` (`:1125-1132`) — i.e. a new finger-down after
a controller-initiated velocity scroll resets the feel back to the touch default. This is a real
smoothness/consistency guard: a keyboard scroll with decay 0.9995 must not make the *next touch
fling* feel like ice.

### 4.2 The minimum-velocity floor (30.0f)

InteractionTracker ignores velocities at or below 30 px/s. ScrollPresenter compensates when the
request comes from an `IScrollController` (which is deliberately InteractionTracker-agnostic):

```cpp
// ScrollPresenter.cpp:6488-6516
if (operationTrigger == InteractionTrackerAsyncOperationTrigger::HorizontalScrollControllerRequest ||
    operationTrigger == InteractionTrackerAsyncOperationTrigger::VerticalScrollControllerRequest)
{
    // Requests coming from an IScrollController implementation do not include the 'minimum inertia velocity' value of 30.0f, because that
    // concept is InteractionTracker-specific (the IScrollController interface is meant to be InteractionTracker-agnostic).
    if (m_state != winrt::ScrollingInteractionState::Inertia)
    {
        // When there is no current inertia, include that minimum velocity automatically. So the IScrollController-provided velocity is always
        // proportional to the resulting offset change.
        static constexpr float s_minimumVelocity{ 30.0f };
        // ± s_minimumVelocity applied per axis, sign-matched
    }
}
```

Note the guard `m_state != Inertia`: **during an existing fling, the floor is *not* re-added**, so
repeated velocity requests accumulate linearly instead of over-boosting. That is exactly the
"compose multiple rapid ticks into one continuing animation" property.

### 4.3 End-of-inertia snapshot

`InertiaStateEntered` (`:1031-1118`) records the tracker's predicted resting view:

```cpp
// ScrollPresenter.cpp:1076-1093
m_endOfInertiaPosition   = modifiedRestingPosition ? {mod.x, mod.y} : {naturalRestingPosition.x, naturalRestingPosition.y};
m_endOfInertiaZoomFactor = modifiedRestingScale ? modifiedRestingScale.Value() : naturalRestingScale;
```

Available via `ComputeEndOfInertiaPosition` (`:1355`) / `ComputeEndOfInertiaZoomFactor` (`:1342`).
The comment at `:1073-1074` says it "may be needed for custom pointer wheel processing" — this is
the hook a host would use to implement wheel-chaining without fighting the running inertia.

### 4.4 "Anticipated view" — the non-animated coalescing model

For **non-animated** relative scrolls, ScrollPresenter maintains
`m_anticipatedZoomedHorizontalOffset` / `…Vertical` / `m_anticipatedZoomFactor` so that N requests
issued before the tracker has caught up sum correctly instead of each being computed from a stale
current offset:

```cpp
// ScrollPresenter.cpp:6343-6353
case ScrollPresenterViewKind::RelativeToCurrentView:
{
    anticipatedZoomedHorizontalOffset = AnticipatedZoomedHorizontalOffset();
    anticipatedZoomedVerticalOffset   = AnticipatedZoomedVerticalOffset();
    if (snapPointsMode == Default || animationMode == Enabled)
    {
        // The new requested deltas are added to the prior deltas that have not been processed yet.
        zoomedHorizontalOffset += anticipatedZoomedHorizontalOffset;
        zoomedVerticalOffset   += anticipatedZoomedVerticalOffset;
    }
}
```
and clamped to `[0, AnticipatedScrollable*]` (`:6399-6411`, `:6430-6436`). Reset on animated /
velocity ops (`ResetAnticipatedView`, `:6765-6770`, called from `:6469`, `:6546`, `:6735`).

**Why this matters for smoothness:** without it, rapid repeated `ScrollBy` calls (wheel emulation,
key repeat) either drop deltas or snap back — the classic "fighting the user" bug.

---

## 5. Mouse wheel

### 5.1 There is no UI-thread wheel handling in the modern stack

Verified by exhaustive grep: `ScrollView.cpp` / `ScrollView.h` contain **zero** occurrences of
`MouseWheel`, `mouseWheel`, or `PointerWheel`. `ScrollPresenter.cpp/.h` contain only
`#ifdef`-gated configuration helpers behind `IsMouseWheelScrollDisabled` /
`IsMouseWheelZoomDisabled` (`ScrollPresenter.cpp:2755-2789`, `:2929-2943`, `ScrollPresenter.h:387-394`,
`:539-543`) — these symbols are **not defined**, so those functions are not even compiled.

The entire wheel path is:

```cpp
// ScrollPresenter.cpp:2796-2803
winrt::VisualInteractionSourceRedirectionMode redirectionMode = winrt::VisualInteractionSourceRedirectionMode::CapableTouchpadOnly;
if (!IsInputKindIgnored(winrt::ScrollingInputKinds::MouseWheel))
{
    redirectionMode = winrt::VisualInteractionSourceRedirectionMode::CapableTouchpadAndPointerWheel;
}
visualInteractionSource.ManipulationRedirectionMode(redirectionMode);
```

Called from `UpdateManipulationRedirectionMode` (`:5654-5660`), which is called on VIS creation
(`:1889`) and whenever `IgnoredInputKinds` changes.

**Consequence:** wheel → OS/DWM input → `VisualInteractionSource` → `InteractionTracker` inertia.
UI thread contributes nothing. Multiple rapid wheel ticks are composed by the *tracker's* inertia
accumulator, not by any XAML-level batching. This is why WinUI wheel scrolling has no key-repeat-style
"restart" stutter.

### 5.2 The `s_mouseWheelDeltaForVelocityUnit` / `s_mouseWheelInertiaDecayRate` constants asked about

**These do not exist in the modern WinUI ScrollPresenter/ScrollView production sources.**
Exhaustive grep across `controls/` and `dxaml/xcp/`:

* `mouseWheelDeltaForVelocityUnit` appears **only as a local test constant**:
  `ScrollPresenter/InteractionTests/ScrollPresenterTestsWithInputHelper.cs:32` →
  `const int mouseWheelDeltaForVelocityUnit = 120;`
  and `WebView2/InteractionTests/WebView2Tests.cs:677`.
* `s_mouseWheelInertiaDecayRate` — **no hits anywhere**. **UNVERIFIED / does not exist here.**
* The only shipping wheel-delta constant is the **legacy** `ScrollViewer`:
  `dxaml/xcp/dxaml/lib/ScrollViewer_Partial.h:30` → `#define ScrollViewerDefaultMouseWheelDelta (120)`,
  consumed at `ScrollViewer_Partial.cpp:2754`, `ScrollContentPresenter_Partial.cpp:647-775`,
  `CarouselPanel_Interfaces_Partial.cpp:145-217`, `OrientedVirtualizingPanel_Partial.cpp:227-344`.

So: if you have seen those names, they are either from the legacy DManip-based `ScrollViewer`, from
a WinUI *version* not present in this checkout, or from a port (Uno itself defines
`ScrollViewerDefaultMouseWheelDelta = 120` at
`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.mux.cs:18`, with the
`max(48, 0.15 * viewport) / 120` line formula at `:25-27`).

### 5.3 Wheel-relevant leftovers that *do* exist

`c_scrollPresenterLineDelta = 16.0` (`ScrollPresenter.cpp:26-27`) — "Number of pixels scrolled when
the automation peer requests a line-type change." Used by `LineUp/LineDown/LineLeft/LineRight`
(`:102-127`), i.e. the **UIA** line unit, not the wheel.

`RelativeToEndOfInertiaView` view-kind and the `m_endOfInertiaPosition` bookkeeping (§4.3) are
explicitly annotated as being for "custom pointer wheel processing" (`ScrollPresenter.cpp:1073-1074`)
— evidence that a UI-thread wheel path was *considered* and deliberately left out of the default.

---

## 6. Snap points as `InteractionTrackerInertiaModifier`s

### 6.1 Structure

`SetupSnapPoints<T>` (`ScrollPresenter.cpp:2029-2143`) builds an
`IVector<InteractionTrackerInertiaModifier>` of `InteractionTrackerInertiaRestingValue` objects and
hands the whole vector to the tracker:

```cpp
// ScrollPresenter.cpp:2123-2142
case ScrollPresenterDimension::HorizontalScroll: m_interactionTracker.ConfigurePositionXInertiaModifiers(modifiers); break;
case ScrollPresenterDimension::VerticalScroll:   m_interactionTracker.ConfigurePositionYInertiaModifiers(modifiers); break;
case ScrollPresenterDimension::ZoomFactor:       m_interactionTracker.ConfigureScaleInertiaModifiers(modifiers);   break;
```

Each modifier is `{ Condition: ExpressionAnimation, RestingValue: ExpressionAnimation }`:

```cpp
// ScrollPresenter.cpp:7224-7240 (GetInertiaRestingValue)
const bool isInertiaFromImpulse = IsInertiaFromImpulse();
const winrt::InteractionTrackerInertiaRestingValue modifier = winrt::InteractionTrackerInertiaRestingValue::Create(compositor);
const winrt::ExpressionAnimation conditionExpressionAnimation    = snapPointWrapper->CreateConditionalExpression(m_interactionTracker, target, scale, isInertiaFromImpulse);
const winrt::ExpressionAnimation restingPointExpressionAnimation = snapPointWrapper->CreateRestingPointExpression(m_interactionTracker, target, scale, isInertiaFromImpulse);
modifier.Condition(conditionExpressionAnimation);
modifier.RestingValue(restingPointExpressionAnimation);
```

Targets/scales (`ScrollPresenter.cpp:2081-2092`, names at `ScrollPresenter.h:938-941`):

| Dimension | `target` | `scale` |
|---|---|---|
| HorizontalScroll | `NaturalRestingPosition.x` | `this.Target.Scale` |
| VerticalScroll | `NaturalRestingPosition.y` | `this.Target.Scale` |
| ZoomFactor | `NaturalRestingScale` | `"1.0"` |

**Empty-set workaround** (older OS versions reject an empty modifier collection) —
a single always-false modifier is inserted (`ScrollPresenter.cpp:2098-2108`):

```cpp
winrt::ExpressionAnimation conditionExpressionAnimation    = compositor.CreateExpressionAnimation(L"false");
winrt::ExpressionAnimation restingPointExpressionAnimation = compositor.CreateExpressionAnimation(L"this.Target." + target);
```

### 6.2 Exact expression strings

Single `ScrollSnapPoint` (`SnapPoint.cpp:193-259`):

* Resting point: `"%1!s!*%2!s!"` → `snapPointValue*this.Target.Scale` (`SnapPoint.cpp:201`)
* Condition: (`SnapPoint.cpp:239-246`)
  ```
  this.Target.IsInertiaFromImpulse ? (this.Target.NaturalRestingPosition.y >= (minImpAppValue*this.Target.Scale) && this.Target.NaturalRestingPosition.y <= (maxImpAppValue*this.Target.Scale))
                                   : (this.Target.NaturalRestingPosition.y >= (minAppValue*this.Target.Scale)    && this.Target.NaturalRestingPosition.y <= (maxAppValue*this.Target.Scale))
  ```
  (format string literally `L"%1!s!?(%2!s!>=%5!s!&&%2!s!<=%6!s!):(%2!s!>=%3!s!&&%2!s!<= %4!s!)"`)

`RepeatedScrollSnapPoint` — one modifier covers an infinite series via `Floor`/`Ceil` arithmetic
(`SnapPoint.cpp:689` resting, `:746` condition). Condition abbreviated:

```
((!T.IsInertiaFromImpulse && T.NaturalRestingPosition.y/T.Scale>=S && T.NaturalRestingPosition.y/T.Scale<=E)
 || (T.IsInertiaFromImpulse && …>=iS && …<=iE))
&& (((Floor((T.…/T.Scale-P)/V)*V)+P+aR >= T.…/T.Scale)
    || (((Ceil((T.…/T.Scale-P)/V)*V)+P-aR <= T.…/T.Scale) && ((Ceil(…)*V)+P <= (IsInertiaFromImpulse?iE:E))))
```

Parameter aliases (`SnapPoint.h:120-133`):

| Alias | Meaning |
|---|---|
| `snapPointValue` | single snap point value |
| `minAppValue` / `maxAppValue` | actual applicable zone |
| `minImpAppValue` / `maxImpAppValue` | impulse applicable zone |
| `V` | interval |
| `S` / `E` | start / end |
| `P` | first (offset) |
| `aR` | applicable range |
| `iS` / `iE` | impulse start / end |
| `M` | impulse ignored value |
| `T` | the InteractionTracker reference |

`s_equalityEpsilon = 0.00001` (`SnapPoint.h:117`).

### 6.3 "Impulse" snapping and the ignored value

`IsInertiaFromImpulse` (`ScrollPresenter.cpp:7242-7254`) reads `InteractionTracker.IsInertiaFromImpulse`
(RS5+) with a managed fallback field. An *impulse* (a discrete wheel/keyboard/scrollbar push, as
opposed to a finger fling) must not snap back to the point you are already sitting on — so the
current value is registered as "ignored":

* `UpdateSnapPointsIgnoredValue` (`:2186-2238`, `:2242-2286`) sets/clears the ignored value and, on
  change, **rebuilds all modifiers for the dimension** via
  `GetUpdatedExpressionAnimationsForImpulse()` (`:2214-2236`).
* Called on `IdleStateEntered` for all three dimensions (`:1023-1025`) and when snap points change
  while idle (`:2041-2062`).

### 6.4 Applicable-zone computation

`UpdateSnapPointsRanges` (`:2150-2183`) walks the sorted set and calls
`DetermineActualApplicableZone(previous, next, forImpulseOnly)` so mandatory points expand to the
midpoint between neighbours and optional points shrink — documented at `:2145-2149`.

**Smoothness note:** every one of these is a *compositor-side* declaration. The UI thread only pays
when the snap-point set, ignored value, or viewport changes — never during the fling.

---

## 7. How ScrollBar dragging feeds the tracker

There are **two distinct mechanisms**.

### 7.1 Classic `ScrollBar` (what `ScrollView` uses) — no VisualInteractionSource

`ScrollView` instantiates `ScrollBarController` per axis (`ScrollView.cpp:393-439`) and assigns it
via `scrollPresenter.HorizontalScrollController(...)` / `VerticalScrollController(...)`
(`ScrollView.cpp:1611`, `:1635`).

`ScrollBarController::PanningInfo()` returns **`nullptr`** (`ScrollBarController.cpp:42-45`), so
`ScrollPresenter::HorizontalScrollController(value)` sets
`m_horizontalScrollControllerPanningInfo` to null (`ScrollPresenter.cpp:303`) and
`SetupScrollControllerVisualInterationSource` takes the "no longer uses a Visual" branch
(`ScrollPresenter.cpp:2420-2465`) — **no `VisualInteractionSource` is created for a plain ScrollBar**.

Instead, `ScrollBar.Scroll` is handled on the UI thread (`ScrollBarController::OnScroll`,
`ScrollBarController.cpp:381-498`):

| `ScrollEventType` | Request raised | ScrollPresenter path |
|---|---|---|
| `ThumbTrack`, `ThumbPosition` | `RaiseScrollToRequested(args.NewValue())` (`:443`) | `OnScrollControllerScrollToRequested` → `ChangeOffsetsPrivate(Absolute, AnimationMode::Disabled, SnapPointsMode::Ignore)` → `TryUpdatePosition` |
| `Small*`/`Large*`, animations **on** | `RaiseAddScrollVelocityRequested(offsetChange)` (`:480`) | `OnScrollControllerAddScrollVelocityRequested` → `TryUpdatePositionWithAdditionalVelocity` |
| `Small*`/`Large*`, animations **off** | `RaiseScrollByRequested(offsetChange)` (`:484`) | `TryUpdatePositionBy` |

Options for thumb drag are explicitly non-animated + snap-free:

```cpp
// ScrollBarController.cpp:510-512
auto options = winrt::make_self<ScrollingScrollOptions>(
    winrt::ScrollingAnimationMode::Disabled,
    winrt::ScrollingSnapPointsMode::Ignore);
```

That is the right call for thumb dragging: the thumb *is* the position, so any easing would lag the
cursor.

**Velocity translation for button/track clicks** (`ScrollBarController.h:86-99`):

```cpp
static constexpr double s_defaultViewportToSmallChangeRatio{ 8.0 };            // SmallChange = viewport/8
static constexpr float  s_inertiaDecayRate = 0.9995f;                          // near-frictionless => nearly linear travel
static constexpr double s_velocityNeededPerPixel{ 7.600855902349023 };         // velocity per px at that decay
static constexpr double s_minMaxEpsilon{ 0.001 };                              // extra push to actually reach Min/Max
```
consumed at `ScrollBarController.cpp:127` (`SmallChange`), `:467-476` (epsilon), `:594-599`
(`offsetChange * s_velocityNeededPerPixel`, boxed `s_inertiaDecayRate`).

**Feedback-loop avoidance (a real jank source, handled explicitly):**

```cpp
// ScrollBarController.cpp:129-135
// The ScrollBar Value is only updated when there is no operation in progress otherwise the Scroll
// event handler, ScrollBarScroll, may initiate a new request impeding the on-going operation.
if (m_operationsCount == 0 || m_scrollBar.Value() < minOffset || m_scrollBar.Value() > maxOffset)
{
    m_scrollBar.Value(offset);
    m_lastScrollBarValue = offset;
}
```
`m_operationsCount` is incremented per *new* correlation id and decremented in
`NotifyRequestedScrollCompleted` (`:154-172`). Coalesced requests (same correlation id returned)
do **not** increment (`:530-533`, `:572-575`, `:613-616`).

`GetScrollAnimation` returns `nullptr` = "use the consumer's default animation"
(`ScrollBarController.cpp:142-152`), which ScrollPresenter interprets at `:3307` as "keep the stock
`positionAnimation`".

### 7.2 `AnnotatedScrollBar` (and any custom controller) — real VisualInteractionSource

When `IScrollController::PanningInfo()` returns non-null and exposes a `PanningElementAncestor`,
ScrollPresenter creates a **second** `VisualInteractionSource` on that visual
(`EnsureScrollControllerVisualInteractionSource`, `ScrollPresenter.cpp:1894-1931`):

```cpp
// ScrollPresenter.cpp:1903-1909
winrt::VisualInteractionSource scrollControllerVisualInteractionSource = winrt::VisualInteractionSource::Create(panningElementAncestorVisual);
scrollControllerVisualInteractionSource.ManipulationRedirectionMode(winrt::VisualInteractionSourceRedirectionMode::CapableTouchpadOnly);
scrollControllerVisualInteractionSource.PositionXChainingMode(winrt::InteractionChainingMode::Never);
scrollControllerVisualInteractionSource.PositionYChainingMode(winrt::InteractionChainingMode::Never);
scrollControllerVisualInteractionSource.ScaleChainingMode(winrt::InteractionChainingMode::Never);
scrollControllerVisualInteractionSource.ScaleSourceMode(winrt::InteractionSourceMode::Disabled);
m_interactionTracker.InteractionSources().Add(scrollControllerVisualInteractionSource);
```

and configures it `EnabledWithoutInertia` on the pan axis
(`ScrollPresenter.cpp:2506-2529`) — dragging a scrollbar thumb must **not** fling.

`PanRequested` → `TryRedirectForManipulation` on that VIS
(`OnScrollControllerPanningInfoPanRequested`, `ScrollPresenter.cpp:4652-4699`, key line `:4680`),
wrapped in a try/catch that swallows `E_ACCESSDENIED` for InteractionTracker bug 17434718
(`:4682-4696`).

**The clever part — thumb-to-content ratio done in the compositor.**
`SetupScrollControllerVisualInterationSourcePositionModifiers`
(`ScrollPresenter.cpp:2551-2678`) installs **four** `CompositionConditionalValue` modifiers on the
VIS's `DeltaPosition{X,Y}` so a 1 px thumb move becomes `Multiplier` px of content move, *clamped*
to the tracker's Min/Max:

```
conditions[0] = "scvis.DeltaPosition.Y < 0.0f && sceas.Multiplier < 0.0f"
conditions[1] = "scvis.DeltaPosition.Y < 0.0f && sceas.Multiplier >= 0.0f"
conditions[2] = "scvis.DeltaPosition.Y >= 0.0f && sceas.Multiplier < 0.0f"
conditions[3] = "true"                                            // case #4
values(clampToMin) = "min(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.Y - it.MinPosition.Y)"
values(clampToMax) = "max(sceas.Multiplier * scvis.DeltaPosition.Y, it.Position.Y - it.MaxPosition.Y)"
```
(`ScrollPresenter.cpp:2638-2641`, `:2668-2675`; horizontal variants at `:2596-2620`)

Cross-axis suppression: when the controller's visual moves on one axis but drives the other, a
`{ Condition:"true", Value:"0" }` modifier zeroes the orthogonal delta
(`ScrollPresenter.cpp:2624-2633`, `:2655-2664`).

**Smoothness verdict:** an `AnnotatedScrollBar` thumb drag never touches the UI thread after the
initial `TryRedirectForManipulation` — the ratio, clamping and rail are all compositor expressions.
A plain `ScrollBar` thumb drag *does* go through the UI thread once per `Scroll` event, but issues a
non-animated `TryUpdatePosition`, so the transform still applies on the compositor.

---

## 8. Complete constant inventory (scroll feel)

### 8.1 `ScrollPresenter`

| Constant | Value | File:line | Effect |
|---|---|---|---|
| `c_scrollPresenterLineDelta` | `16.0` | `ScrollPresenter.cpp:27` | px per UIA "line" (`LineUp/Down/Left/Right`) |
| `c_scrollPresenterDefaultInertiaDecayRate` | `0.95f` | `ScrollPresenter.cpp:31` | default position inertia decay substituted for scroll-controller requests |
| `ScrollPresenter::s_noOpCorrelationId` | `-1` | `ScrollPresenter.cpp:33` | sentinel |
| `s_minimumVelocity` (local) | `30.0f` | `ScrollPresenter.cpp:6497` | InteractionTracker's velocity floor, re-added for controller requests when not already in Inertia |
| `c_scrollableEpsilon` (local) | `0.0001` | `ScrollPresenter.cpp:5590` | below this, scrollable size reported as 0 to controllers |
| `s_offsetsChangeMsPerUnit` | `5` | `ScrollPresenter.h:68` | ms per px of scroll animation |
| `s_offsetsChangeMinMs` | `50` | `ScrollPresenter.h:69` | floor |
| `s_offsetsChangeMaxMs` | `1000` | `ScrollPresenter.h:70` | ceiling |
| `s_zoomFactorChangeMsPerUnit` | `250` | `ScrollPresenter.h:73` | ms per zoom unit |
| `s_zoomFactorChangeMinMs` | `50` | `ScrollPresenter.h:74` | floor |
| `s_zoomFactorChangeMaxMs` | `1000` | `ScrollPresenter.h:75` | ceiling |
| `s_translationAndZoomFactorAnimationsRestartTicks` | `4` | `ScrollPresenter.h:79` | UI ticks between stopping and restarting the transform animations after idle/zoom-complete (rasterization trigger) |
| `s_defaultMinZoomFactor` / `s_defaultMaxZoomFactor` | `0.1` / `10.0` | `ScrollPresenter.h:63-64` | zoom bounds |
| `s_defaultAnchorRatio` | `0.0` | `ScrollPresenter.h:65` | anchoring |
| `s_defaultAnchorAtExtent` | `true` | `ScrollPresenter.h:62` | anchoring |
| `s_defaultHorizontal/VerticalScrollRailMode` | `Enabled` | `ScrollPresenter.h:52-53` | rails on by default (kills diagonal drift) |
| `s_defaultHorizontal/VerticalScrollChainMode` | `Auto` | `ScrollPresenter.h:50-51` | nested-scroller chaining |
| `s_defaultZoomChainMode` | `Auto` | `ScrollPresenter.h:58` | |
| `s_defaultZoomMode` | `Disabled` | `ScrollPresenter.h:59` | |
| `s_defaultIgnoredInputKinds` | `None` | `ScrollPresenter.h:60` | all input kinds active |
| `s_defaultContentOrientation` | `Both` | `ScrollPresenter.h:61` | |

Property-set / expression parameter names: `ScrollPresenter.h:41-47` (`Extent`, `Viewport`,
`Offset`, `Position`, `MinPosition`, `MaxPosition`, `ZoomFactor`), `:923-929` (`TransformMatrix._41`
… `Translation`, `Scale`), `:932-935` (`MinOffset`, `MaxOffset`, `Offset`, `Multiplier`),
`:938-941` (`NaturalRestingPosition.x/.y`, `NaturalRestingScale`, `this.Target.Scale`).

### 8.2 `InteractionTrackerAsyncOperation`

| Constant | Value | File:line | Effect |
|---|---|---|---|
| `c_maxNonAnimatedOperationTicks` | `10` | `InteractionTrackerAsyncOperation.h:35` | workaround for tracker bug 12465209 (silent `TryUpdatePosition` to current position) |
| `c_queuedOperationTicks` | `3` | `InteractionTrackerAsyncOperation.h:39` | UI ticks a queued op waits so pending size changes reach the compositor first |

### 8.3 `ScrollView`

| Constant | Value | File:line | Effect |
|---|---|---|---|
| `smallScrollProportion` | `0.15` | `ScrollView.cpp:1992` | arrow-key scroll = 15% of viewport |
| `scrollAmountProportion` (XY-focus) | `1.0` page / `0.5` non-page | `ScrollView.cpp:2013` | gamepad page vs directional |
| `numPagesLookAhead` | `2` | `ScrollView.cpp:2228` | focus-navigation search |
| `offsetEpsilon` | `0.001` | `ScrollView.cpp:2264`, `:2515`, `:2563` | ensures extremes are actually reached |
| `inertiaDecayRate` (key scroll) | `float2(0.9995f, 0.9995f)` | `ScrollView.cpp:2393` | near-frictionless → distance ≈ velocity/`s_velocityNeededPerPixel` |
| `minVelocity` | `30.0` | `ScrollView.cpp:2396` | tracker floor |
| `s_velocityNeededPerPixel` | `7.600855902349023` | `ScrollView.cpp:2399` | velocity per px at decay 0.9995 |
| `s_noIndicatorCountdown` | `2000 * 10000` (= 2 s) | `ScrollView.h:377` | scrollbar-indicator hide delay |
| `s_defaultMinZoomFactor`/`Max` | `0.1` / `10.0` | `ScrollView.h:43-44` | mirrors presenter |
| `s_defaultAnchorAtExtent`/`s_defaultAnchorRatio` | `true` / `0.0` | `ScrollView.h:45-46` | |

### 8.4 `ScrollBarController`

| Constant | Value | File:line |
|---|---|---|
| `s_defaultViewportToSmallChangeRatio` | `8.0` | `ScrollBarController.h:90` |
| `s_inertiaDecayRate` | `0.9995f` | `ScrollBarController.h:93` |
| `s_velocityNeededPerPixel` | `7.600855902349023` | `ScrollBarController.h:96` |
| `s_minMaxEpsilon` | `0.001` | `ScrollBarController.h:99` |

### 8.5 `SnapPoint`

| Constant | Value | File:line |
|---|---|---|
| `s_equalityEpsilon` | `0.00001` | `SnapPoint.h:117` |
| expression aliases | see §6.2 | `SnapPoint.h:120-133` |

### 8.6 `ScrollingScrollOptions`

| Constant | Value | File:line |
|---|---|---|
| `s_defaultAnimationMode` | `ScrollingAnimationMode::Auto` | `ScrollingScrollOptions.h:21` |
| `s_defaultSnapPointsMode` | `ScrollingSnapPointsMode::Default` | `ScrollingScrollOptions.h:22` |

---

## 9. The `velocity = baseline + offset * 7.6008…` composition trick (key repeat / scrollbar repeat)

This is the modern stack's answer to "many rapid discrete scroll requests must feel like one
continuous glide", and it is worth reproducing verbatim:

```cpp
// ScrollView.cpp:2391-2435  (DoScroll)
if (SharedHelpers::IsAnimationsEnabled())
{
    static const winrt::float2 inertiaDecayRate(0.9995f, 0.9995f);
    static const double minVelocity = 30.0;                      // A velocity <= this has no effect.
    static constexpr double s_velocityNeededPerPixel{ 7.600855902349023 };

    const auto scrollDir = offset > 0 ? 1 : -1;
    double baselineVelocity = minVelocity * scrollDir;

    // If there is already a scroll animation running for a previous key press, we want to take that into account
    // for calculating the baseline velocity.
    const auto previousScrollViewChangeCorrelationId = isVertical ? m_verticalAddScrollVelocityOffsetChangeCorrelationId
                                                                  : m_horizontalAddScrollVelocityOffsetChangeCorrelationId;
    if (previousScrollViewChangeCorrelationId != s_noOpCorrelationId)
    {
        const auto directionOfPreviousScrollOperation = isVertical ? m_verticalAddScrollVelocityDirection
                                                                   : m_horizontalAddScrollVelocityDirection;
        if (directionOfPreviousScrollOperation == 1)       { baselineVelocity -= minVelocity; }
        else if (directionOfPreviousScrollOperation == -1) { baselineVelocity += minVelocity; }
    }

    const auto velocity = static_cast<float>(baselineVelocity + (offset * s_velocityNeededPerPixel));
    ...AddScrollVelocity(offsetsVelocity, inertiaDecayRate);
}
```

Reading of the mechanics:

* **Decay 0.9995 is deliberately near-1** so that with the tracker's exponential model the travelled
  distance is essentially linear in velocity, making `velocity = px * 7.6008…` an accurate
  "scroll exactly N px" instruction.
* **The 30 px/s floor is added once**, not per repeat: if a fling in the same direction is already
  running, the baseline is *removed* again (`baselineVelocity -= minVelocity`) so the two
  contributions sum to exactly the requested pixel distance instead of over-shooting by 30/7.6 ≈ 4 px
  per repeat.
* If direction reverses, the baseline is *added twice* in the opposite sense
  (`baselineVelocity += minVelocity` while `scrollDir == -1` gives `-30 + 30 = 0`… then the offset
  term dominates) — a reversal cancels the previous impulse rather than fighting it.
* The complementary guard lives in `ProcessOffsetsChange` (`ScrollPresenter.cpp:6493`): the presenter
  only re-adds `s_minimumVelocity` **when `m_state != Inertia`**.
* Correlation ids are cleared when the *other* axis scrolls (`ScrollView.cpp:2442`, `:2449`) and
  when the operation completes (`ScrollView.cpp:1029-1036`).

Result: holding ↓ produces a single accelerating glide, not a staircase. This is the single most
transplantable idea in the whole file.

---

## 10. Operation queuing / coalescing (the anti-jank scheduler)

`m_interactionTrackerAsyncOperations` is a list of `InteractionTrackerAsyncOperation`
(`ScrollPresenter.h`, list mutated at `:5917`, `:5996`). Each op carries:

* `m_preProcessingTicksCountdown` initialised to `c_queuedOperationTicks == 3`
  (`InteractionTrackerAsyncOperation.h:253`)
* `m_postProcessingTicksCountdown` for the non-animated completion workaround (`:248`)

Key behaviours:

1. **Delay by 3 ticks before handing to the tracker** so pending size changes have propagated
   (`InteractionTrackerAsyncOperation.h:37-39`).
2. **`SetMaxTicksCountdown()`** bumps queued ops back to 3 whenever extent or viewport changes:
   ```cpp
   // ScrollPresenter.cpp:5441-5445
   if (extentChanged || viewportChanged)
   {
       MaximizeInteractionTrackerOperationsTicksCountdown();
       UpdateScrollAutomationPatternProperties();
   }
   ```
   (`MaximizeInteractionTrackerOperationsTicksCountdown` at `:6969-6992`,
   `SetMaxTicksCountdown` at `InteractionTrackerAsyncOperation.h:140-151`.)
   **This is the explicit fix for "scroll target computed against a stale extent".**
3. **User-triggered ops jump the queue**: `SetTicksCountdown(max(1, GetInteractionTrackerOperationsTicksCountdown()))`
   (`ScrollPresenter.cpp:5909-5915`, `:5988-5994`) — "processed as quickly as possible ... while
   preserving ordering".
4. **Coalescing**: `GetInteractionTrackerOperationFromKinds` /
   `GetInteractionTrackerOperationWithAdditionalVelocity` (`:7075`, `:7183`) find an existing
   same-tick, same-kind, same-options op and *mutate* it rather than enqueuing a second one —
   `OnScrollControllerScrollToRequested` (`:4785-4804`),
   `OnScrollControllerScrollByRequested` (`:4844-4863`),
   `OnScrollControllerAddScrollVelocityRequested` (`:4929-5033`). The same correlation id is
   returned, which is precisely what `ScrollBarController` uses to avoid double-counting operations
   (`ScrollBarController.cpp:520-533`).
5. **`GetRequiredOperation()` dependency chain** so a queued op waits for a prior non-animated op to
   land (`OnCompositionTargetRendering`, `:4240-4261`).

---

## 11. What the design says *breaks* smoothness (explicit evidence)

| Hazard | Evidence in source | Mitigation used |
|---|---|---|
| Stale extent/viewport when an op is handed to the tracker | `ScrollPresenter.cpp:5441-5445`, `InteractionTrackerAsyncOperation.h:137-151` | re-arm 3-tick countdown |
| Scrollbar value write → new Scroll event → new request → fight | `ScrollBarController.cpp:129-135` | suppress `Value` writes while `m_operationsCount > 0` |
| Repeated impulses over-shooting by the min-velocity floor | `ScrollView.cpp:2406-2420`, `ScrollPresenter.cpp:6493` | remove baseline when already in inertia |
| Impulse snapping back to the point you're standing on | `ScrollPresenter.cpp:2242-2286`, `:1023-1025` | ignored-value + `IsInertiaFromImpulse` branch in every condition expression |
| Relative scrolls computed against not-yet-applied deltas | `ScrollPresenter.cpp:6343-6353`, `:5510-5541` | "anticipated view" accumulation |
| Blurry text while zoomed (rasterization at stale scale) | `ScrollPresenter.cpp:3497-3519` | stop/restart transform animations 4 ticks after idle — *only when zoom changed* |
| A stale controller decay rate poisoning the next touch fling | `ScrollPresenter.cpp:1125-1132` | reset both decay rates on `InteractingStateEntered` |
| Diagonal drift during a vertical pan | `ScrollPresenter.h:52-53` (`RailMode::Enabled` default), `ScrollPresenter.cpp:2680-2696` | `IsPositionXRailsEnabled` / `IsPositionYRailsEnabled` |
| Scrollbar drag flinging | `ScrollPresenter.cpp:2506, 2511, 2522, 2527` | `InteractionSourceMode::EnabledWithoutInertia` |
| Orthogonal finger movement leaking through a scrollbar drag | `ScrollPresenter.cpp:2624-2633`, `:2655-2664` | `{true → 0}` conditional value on the other axis |
| Rendering-hook left running forever | `ScrollPresenter.cpp:4312-4315`, `:7277-7282` | unhook the moment the operation queue drains |

---

## 12. Uno-relevant comparison (read from the working repo)

Uno has a fairly faithful C# port of `ScrollPresenter` (`src/Uno.UI/UI/Xaml/Controls/ScrollPresenter/`)
including the same constants (`ScrollPresenter.h.cs:44-46` → 5 / 50 / 1000;
`ScrollPresenter.cs:46` → `c_scrollPresenterDefaultInertiaDecayRate = 0.95f`) and the same
expression strings (`ScrollPresenter.cs:1701-1711`). The delta is **below** ScrollPresenter, in
Composition:

1. **Inertia runs on a `System.Threading.Timer` at a fixed 17 ms, not on the render clock.**
   `InteractionTrackerActiveInputInertiaHandler.cs:24` → `IntervalInMilliseconds = 17`, started at
   `:48` with `new Timer(OnTick, null, 0, IntervalInMilliseconds)`. Same in
   `InteractionTrackerPointerWheelInertiaHandler.cs:15,55`. A free-running 58.8 Hz timer beating
   against a 60 / 120 / 144 Hz vsync produces periodic duplicate-or-skipped frames — visible
   micro-stutter independent of CPU load.

2. **Every tracker position update is marshalled to the UI thread.**
   `InteractionTracker.cs:62-74`:
   ```csharp
   internal void SetPosition(Vector3 newPosition, int requestId)
   {
       if (_position != newPosition)
       {
           _position = newPosition;
           var scale = _scale;
           NativeDispatcher.Main.Enqueue(() =>
           {
               Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, scale, requestId));
               OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
           });
       }
   }
   ```
   So the transform update is *behind* the UI-thread queue. If the UI thread is busy (layout,
   virtualization, GC), the content stops moving. In WinUI it would not.

3. **`ExpressionAnimation` is push-based, not compositor-tracked.**
   `src/Uno.UI.Composition/Composition/ExpressionAnimation.cs:22-31`:
   ```csharp
   // ExpressionAnimation is re-evaluated on property changes, not on every render frame by the compositor.
   internal override bool IsTrackedByCompositor => false;
   private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
   { if (_parsedExpression is not null) { RaiseAnimationFrame(); } }
   ```
   Combined with (2), the whole `Position → Translation` chain executes on the UI thread, once per
   enqueued update, with a string-parsed AST evaluation (`ExpressionAnimationParser`) per evaluation.

4. **Wheel handling is UI-thread and quantised.** `InteractionTracker.cs:137-145`:
   ```csharp
   internal void ReceivePointerWheel(int mouseWheelTicks, bool isHorizontal)
   {
       // ... For now, we just use 16*3=48.
       var delta = mouseWheelTicks * 48;
       _state.ReceivePointerWheel(-delta, isHorizontal);
   }
   ```
   and `InteractionTrackerPointerWheelInertiaHandler.cs:36` uses **constant velocity for 0.25 s**
   (`_calculatedFinalPosition = Position + InitialVelocity * 0.25f`, terminated at `:68` when
   `elapsed >= 250`). Constant velocity with a hard stop is the single most "unsmooth" possible
   wheel curve — no ease-out at all.

5. **Uno's inertia math otherwise mirrors the tracker model** correctly: decay `1 - decayRate`
   (`InteractionTrackerActiveInputInertiaHandler.AxisHelper.cs:39`, defaulting to `new(0.95f)`),
   minimum velocity `30.0f` (`:65`), `t_min = (ln(30) - ln(v0)) / ln(decay)` (`:82`),
   `Δx = ((decay^t - 1) * v0) / ln(decay)` (`:112-113`), and a critically-damped overpan settle with
   `wn = 5.8335 / settlingTime` (`:128-130`). Good fidelity — the problem is *where* it runs and
   *what clock* drives it.

---

## 13. Answers, condensed

1. **Objects/expressions** — §1. `InteractionTracker` (`:1873`) + `VisualInteractionSource` on the
   presenter's visual (`:1886`) + `ExpressionAnimation` on `UIElement.Translation` and
   `UIElement.Scale` (`:3434-3441`). Strings at `:3403/:3407/:3412/:3420`; boundaries at
   `:3100-3247`; public mirror at `:1846-1856`.
2. **Thread** — §2. Compositor evaluates the transform. UI thread does one
   `TryRedirectForManipulation` per pointer-press, then only notification bookkeeping
   (`ValuesChanged` → scrollbars/UIA/`ViewChanged`). Wheel: zero UI-thread work. Programmatic:
   ~3 `CompositionTarget.Rendering` ticks then unhook.
3. **Default animations** — §3. `Vector3KeyFrameAnimation`, one keyframe at progress 1.0, **no
   easing function supplied**; duration `clamp(dist * 5ms, 50ms, 1000ms)`. Zoom:
   `clamp(|Δ| * 250ms, 50ms, 1000ms)`. `AddScrollVelocity` uses no keyframe animation at all.
   The specific default cubic-bezier control points are **UNVERIFIED** (Composition is out of repo).
4. **Inertia** — §4. `TryUpdatePositionWithAdditionalVelocity(float3)` (`:6542`);
   `PositionInertiaDecayRate` clamped to [0,1] (`:6521-6525`) or reset to `nullptr` (`:6785`);
   documented defaults 0.95 (position) / 0.985 (scale); floor 30 px/s (`:6497`); resting values
   modified by `InteractionTrackerInertiaRestingValue` modifiers (§6).
5. **Mouse wheel** — §5. **No UI-thread handler.**
   `ManipulationRedirectionMode = CapableTouchpadAndPointerWheel` (`:2800`); rapid ticks composed by
   the tracker's own inertia accumulation. `s_mouseWheelDeltaForVelocityUnit` exists only as a test
   constant (`InteractionTests/…:32`, value 120); `s_mouseWheelInertiaDecayRate` **does not exist**.
   Legacy `ScrollViewer` has `ScrollViewerDefaultMouseWheelDelta (120)`
   (`dxaml/xcp/dxaml/lib/ScrollViewer_Partial.h:30`).
6. **Snap points** — §6. `InteractionTrackerInertiaRestingValue` with `Condition` +
   `RestingValue` expression animations over `NaturalRestingPosition.x/.y` /
   `NaturalRestingScale`, installed via
   `ConfigurePositionX/YInertiaModifiers` / `ConfigureScaleInertiaModifiers` (`:2132-2138`).
   Impulse branch keyed on `IsInertiaFromImpulse` plus a per-set "ignored value".
7. **ScrollBar → tracker** — §7. Plain `ScrollBar`: no VIS; UI-thread `Scroll` →
   `ScrollToRequested` (Absolute, animation Disabled, snap Ignore) → `TryUpdatePosition`; buttons →
   `AddScrollVelocityRequested` with decay 0.9995 and 7.6008…/px. `AnnotatedScrollBar`: a real
   second `VisualInteractionSource` with four `CompositionConditionalValue` `DeltaPosition`
   modifiers implementing the multiplier + clamp entirely in the compositor.
8. **Constants** — §8.

---

## 14. Explicit UNVERIFIED list

* Default `CompositionEasingFunction` used by `KeyFrameAnimation.InsertKeyFrame` when none is
  supplied (control points, or whether it is linear). Composition source is not in
  `D:/Work/microsoft-ui-xaml2`.
* The internal `InteractionTracker` inertia integration (exact per-frame formula, frame clock,
  chaining/overpan behaviour). Only ScrollPresenter's *configuration* of it is verifiable here.
* `s_mouseWheelInertiaDecayRate` — no such symbol exists anywhere in this checkout.
* Actual measured frame rates / latency numbers — no telemetry or benchmark data in these sources.
