# Legacy WinUI / dxaml `ScrollViewer` — how it achieves smooth scrolling

**Source tree studied:** `D:/Work/microsoft-ui-xaml2` (WinAppSDK / microsoft-ui-xaml snapshot; legacy core under `dxaml/xcp/`).
All paths below are **relative to `D:/Work/microsoft-ui-xaml2/`** unless stated otherwise.
All line numbers were read from the actual files in this snapshot.

> **Headline finding, stated once up front:** the legacy `ScrollViewer` does *not* implement smooth scrolling.
> It **delegates the entire per-frame scroll motion to the OS DirectManipulation (DManip) component**, which runs
> off the XAML UI thread and drives a **shared DComp transform** that the XAML compositor tree references through
> a WinRT **ExpressionAnimation**. XAML's UI thread is a *bookkeeper*: it pushes configuration and extents into
> DManip, and it *reads back* DManip's transform once per UI tick to update `HorizontalOffset`/`VerticalOffset`,
> ScrollBar values, virtualization and events. If the UI thread stalls, the content keeps moving at full
> refresh rate anyway. Everything else in this note is detail on that one architectural decision.

---

## 0. Map of the players

| Layer | File | Role |
|---|---|---|
| Core element | `dxaml/xcp/core/core/elements/ScrollViewer.cpp` (105 lines), `dxaml/xcp/core/inc/ScrollViewer.h` (47 lines) | Almost empty. Only owns the `ManipulationTransform` `CompositionPropertySet` exposed to apps. |
| Framework control | `dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp` (**16 821 lines**), `.h` (2 325 lines) | All the logic: input routing, DManip container implementation, offsets, snap points, anchoring, `ChangeView`. |
| DManip orchestration | `dxaml/xcp/core/input/InputServices.cpp` (~13 000 lines) | `CInputServices` is the single UI-thread broker between XAML and DManip. |
| DManip viewport state | `dxaml/xcp/core/inc/DMViewport.h` (1 613 lines) | `CDMViewport` — per-ScrollViewer DManip state machine mirror. |
| PAL → DManip | `dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.cpp` | Actual `IDirectManipulationManager` / `IDirectManipulationViewport` calls. |
| Compositor glue | `dxaml/xcp/core/hw/DManipData.{h,cpp}`, `dxaml/xcp/core/hw/ManipulationTransform.cpp`, `dxaml/xcp/components/transforms/WinRTLocalExpressionBuilder.cpp`, `dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp` | Wires DManip's shared transform into the WUC visual tree via ExpressionAnimations. |

The `CScrollViewer` core class is deliberately trivial — its only real job is
`EnsureManipulationTransformPropertySet` / `UpdateManipulationTransformPropertySet`
(`dxaml/xcp/core/core/elements/ScrollViewer.cpp:27-104`), i.e. exposing the DManip transform as a
`CompositionPropertySet` so apps can build their own expression animations off the scroll position
(sticky headers, parallax) **without** touching the UI thread.

---

## 1. Dependent (UI-thread) vs independent (DManip/compositor) manipulation

### 1.1 Which offsets are authoritative, and when

There are **three** offset representations, and which one is authoritative depends on the viewport status:

| Representation | Owner | Where |
|---|---|---|
| DManip "primary content transform" (translationX/Y, zoomFactorX/Y) | **DManip**, updated on its own thread | read via `IPALDirectManipulationService::GetPrimaryContentTransform` |
| `CDMViewport` cached current/initial transformation values | UI thread mirror, refreshed once per UI tick | `CDMViewport::SetCurrentTransformationValues` (`dxaml/xcp/core/input/InputServices.cpp:8714`) |
| `ScrollViewer::m_xOffset/m_yOffset`, `m_xPixelOffset/m_yPixelOffset`, and the public `HorizontalOffset`/`VerticalOffset` DPs | UI thread, derived *from* DManip | `ScrollViewer::HandleManipulationDelta` (`dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp:13432`) |

**During an active manipulation (`XcpDMViewportRunning`, `XcpDMViewportInertia`, `XcpDMViewportSuspended`,
`XcpDMViewportAutoRunning`) DManip's transform is authoritative.** XAML *follows*:

```cpp
// dxaml/xcp/core/inc/InputServices.h:1078-1081
static bool IsViewportActive(XDMViewportStatus status)
{
    return status == XcpDMViewportRunning || status == XcpDMViewportInertia
        || status == XcpDMViewportSuspended || status == XcpDMViewportAutoRunning;
}
```

Statuses (`dxaml/xcp/pal/inc/paltypes.h:428-438`):
`XcpDMViewportBuilding=0, Enabled=1, Disabled=2, Running=3, Inertia=4, Ready=5, Suspended=6, AutoRunning=7`.

**When no manipulation is active, XAML is authoritative and *pushes* into DManip** —
`CInputServices::UpdateManipulationPrimaryContentTransform`
(`dxaml/xcp/core/input/InputServices.cpp:11855`). Note the asymmetry at line 12102–12105: while active,
XAML must not call `SyncContentTransform`; it can only move the *content rect* (`SetContentBounds`):

```cpp
// InputServices.cpp:12102-12103
// IDirectManipulationContentBehavior::SyncContentTransform can not be called on an active viewport.
// IDirectManipulationContent::SetContentRect must be called instead.
```

and when *not* active it must round-trip through `BringIntoViewport` instead:

```cpp
// InputServices.cpp:12136-12146
// Let the DM container make a BringIntoViewport call to synchronize DManip and XAML.
IFC(pDirectManipulationContainer->NotifyBringIntoViewportNeeded(pManipulatedElement));
```

### 1.2 The independent (compositor) path — how pixels actually move

DManip hands XAML an opaque legacy DComp transform ("shared primary content transform"). XAML never
writes to it and never reads it per frame for rendering. Instead:

1. **`DManipDataWinRT` builds an "overall content" `CompositionPropertySet`** combining DManip's matrix with
   XAML's own content offset, using an ExpressionAnimation:

```cpp
// dxaml/xcp/core/hw/DManipData.cpp:150-176  (EnsureOverallContentPropertySet)
IFC_RETURN(m_spOverallContentPropertySet->InsertMatrix4x4(HStringReference(L"Matrix").Get(), {identity}));
wfn::Matrix4x4 prependTransform = { 1,0,0,0, 0,1,0,0, 0,0,1,0, m_contentOffsetX, m_contentOffsetY, 0, 1 };
// "targetPS.Matrix = ContentOffsetTransform * DManipTransform.Matrix"
IFC_RETURN(::ConnectAnimationWithPrependTransform(spOverallContentPropertySetCO.get(),
             m_spSharedPrimaryContentTransformCO.get(), prependTransform, L"Matrix"));
```

   (with a 3-way variant when there is a secondary/header transform, `DManipData.cpp:166-170`).

2. **`WinRTLocalExpressionBuilder::ApplyDManipSharedTransform` copies that matrix into the element's
   `LOCAL` property set** via a second ExpressionAnimation:

```cpp
// dxaml/xcp/components/transforms/WinRTLocalExpressionBuilder.cpp:210-242
// ExpressionHelper::sc_Expression_DMTransform == L"PS.Matrix"
IFCFAILFAST(m_winrtContext.GetCompositorNoRef()->CreateExpressionAnimationWithExpression(
    HStringReference(ExpressionHelper::sc_Expression_DMTransform).Get(), &expressionAnimation));
expressionAnimationAsCA->SetReferenceParameter(HStringReference(sc_paramName_PropertySet).Get(), dmanipTransformAsCO.Get());
IFCFAILFAST(propertySetAsCO->StartAnimation(
    HStringReference(ExpressionHelper::sc_propertyName_LocalDManipTransform).Get(), expressionAnimationAsCA.Get()));
m_transformFlags |= TransformFlag_DManip;
```

3. **The element's final `Visual.TransformMatrix` is one composed expression** built from the flags that are
   actually in use — the DManip term is:

```cpp
// dxaml/xcp/components/graphics/ExpressionHelper.cpp:164
const wchar_t* const ExpressionHelper::sc_Expression_LocalTransform_DManip =
    L"Matrix4x4.CreateFromTranslation(V.Offset) * LOCAL.DManip * Matrix4x4.CreateFromTranslation(-V.Offset)";
```

   combined in `WinRTLocalExpressionBuilder::GetExpressionString()` (`.cpp:300-330`) as
   `Projection * Transform3D * Render * TTRender * FlowDirection * DManip * Redir`, joined by `" * "`.

4. **Clips move independently too** — a 4x4→3x2 conversion expression so the viewport clip tracks the
   manipulation without a UI-thread frame:

```cpp
// dxaml/xcp/components/graphics/ExpressionHelper.cpp:196
const wchar_t* const ExpressionHelper::sc_Expression_DManipClipTransform =
  L"Matrix3x2(Primary.Matrix._11, Primary.Matrix._12, Primary.Matrix._21, Primary.Matrix._22, Primary.Matrix._41, Primary.Matrix._42)";
```
   used at `dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp:2222`.

**Net effect:** once the expression graph is built (at manipulation start), *zero* UI-thread work is needed to
move the content. The compositor evaluates `LOCAL.DManip` each compositor frame from a property set that
DManip writes on its own thread.

### 1.3 The dependent (UI-thread) path

Per UI tick, `CInputServices::ProcessUIThreadTick` (`dxaml/xcp/core/input/InputServices.cpp:9098-9124`) runs:

```cpp
IFC_RETURN(InitializeDirectManipulationContainers());
IFC_RETURN(ProcessDirectManipulationViewportChanges());
IFC_RETURN(RefreshDirectManipulationHandlerWantsNotifications());
```

`ProcessDirectManipulationViewportValuesUpdate` (`InputServices.cpp:8604`) reads the transform once and
notifies XAML:

```cpp
// InputServices.cpp:8666-8673
IFC(pDirectManipulationService->GetPrimaryContentTransform(
        pViewport, newTranslationX, newTranslationY, newUncompressedZoomFactor, newZoomFactorX, newZoomFactorY));
...
// InputServices.cpp:8722-8731   *** the key comment ***
// Note that this invalidation is marked as independent. This prevents an unnecessary render walk
// from occurring if the only change this frame was a DM value update, and no dependent changes
// occurred in response. If a dependent change occurs (e.g. new virtualized items added, scroll indicator moved)
// then those will also mark an element dirty and that will cause a render walk to occur.
CUIElement::NWSetTransformDirty(pManipulatedElement,
    DirtyFlags::Render | DirtyFlags::Bounds | DirtyFlags::Independent);
```

`DirtyFlags::Independent` (`dxaml/xcp/core/inc/DirtyFlags.h:13`, value `0x04`) is the throttle. Every
`NWSet*Dirty` handler short-circuits when it's set:

```cpp
// dxaml/xcp/core/core/elements/uielement.cpp:8643-8660  (CUIElement::NWSetTransformDirty)
if (!flags_enum::is_set(flags, DirtyFlags::Independent)) {
    pUIE->NWSetDirtyFlagsAndPropagate(flags | DirtyFlags::Render | DirtyFlags::Bounds, pUIE->m_fNWTransformDirty);
    pUIE->m_fNWTransformDirty = TRUE;
} else {
    // Independent changes only dirty bounds.
    pUIE->NWSetDirtyFlagsAndPropagate(DirtyFlags::Independent | DirtyFlags::Bounds, FALSE);
}
```

Same pattern in `NWSetOpacityDirty`, `NWSetContentDirty`, `NWSetSubgraphDirty`
(`dxaml/xcp/components/elements/UIElementRenderWalk.cpp:118, 236, 281`) and in the brush/transform
`RENDERCHANGEDPFN`s (`SolidColorBrush.cpp:65`, `Transform.cpp:211`, `TransformGroup.cpp:28`,
`Projection.cpp:40`, `Transform3D.cpp:34`). So **a pure scroll frame dirties bounds only — no render walk,
no re-rasterization.**

---

## 2. Mouse wheel → scroll, traced end to end

### 2.1 `ScrollViewer::OnPointerWheelChanged`

`dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp:2720`.

* Reads `MouseWheelDelta` (`:2754`) and `Control` modifier (`:2745-2747`) →
  `ZoomDirection_In/Out` when Ctrl is down (`:2756-2765`).
* Gated by `ArePointerWheelEventsIgnored` (`:2773-2774`, DP accessors at `:647/:654`) and
  `m_ignoreSemanticZoomNavigationInput`.
* **The branch that matters** (`:2795-2836`):

```cpp
IFC(IsScrollContentPresenterScrollClient(isScrollContentPresenterScrollClient));
if (isScrollContentPresenterScrollClient)
{
    // Give DirectManipulation an opportunity to handle the mouse wheel message
    IFC(ProcessPureInertiaInputMessage(messageZoomDirection, &handled));
    IFC(pArgs->put_Handled(handled));
}
else
{
    // Let the IScrollInfo implementation handle the wheel delta
    ... spScrollInfo->MouseWheelDown(-mouseWheelDelta) / MouseWheelUp(...) / MouseWheelLeft/Right ...
}
```

So there are **two completely different wheel paths**:

* **`ScrollContentPresenter` is the `IScrollInfo`** (the normal case, non-virtualized or
  `ItemsStackPanel`-style pixel scrolling) → **DManip path**, animated, off UI thread.
* **A custom/logical-scrolling `IScrollInfo`** (e.g. `OrientedVirtualizingPanel`, `CarouselPanel`,
  `TextBoxView`) → **`IScrollInfo::MouseWheel*` path**, an *immediate, unanimated* offset jump on the UI
  thread. This is the legacy WPF-shaped path and is *not* smooth by construction.

### 2.2 The DManip wheel path

```
ScrollViewer::OnPointerWheelChanged                     ScrollViewer_Partial.cpp:2803
  → ScrollViewer::ProcessPureInertiaInputMessage        ScrollViewer_Partial.cpp:9756
      (zoom-chaining early-out at :9770-9797)
  → ScrollViewer::ProcessInputMessage                   ScrollViewer_Partial.cpp:9016
  → CoreImports::ManipulationHandler_ProcessInputMessage  core/dll/CoreImports.cpp:3534
  → CUIDMContainerHandler::ProcessInputMessage          core/optional/elements/touch/UIDMContainerHandler.cpp:454
  → CInputServices::ProcessInputMessageWithDirectManipulation  core/input/InputServices.cpp:6711
  → CDirectManipulationService::ProcessInput            plat/win/browserdesktop/DirectManipulationService.cpp:482
  → IDirectManipulationManager::ProcessInput(&msg,...)  DirectManipulationService.cpp:587/591
```

The message being forwarded is the **real Win32 message**, stashed on the input manager just before the
routed event is raised so the DM container can pick it up synchronously:

```cpp
// dxaml/xcp/components/ContentRoot/PointerInputProcessor.cpp:657-663
// Used by the ProcessInputMessageWithDirectManipulation method potentially invoked by the DM container,
// synchronously during the RaiseRoutedEvent call.
m_pCurrentMsgForDirectManipulationProcessing = pMsg;
auto guard = wil::scope_exit([&] { m_pCurrentMsgForDirectManipulationProcessing = nullptr; });
m_inputManager.m_coreServices.GetEventManager()->RaiseRoutedEvent(
    EventHandle(KnownEventIndex::UIElement_PointerWheelChanged), ..., TRUE /*fRaiseSync*/, TRUE /*fInputEvent*/);
```

`CDirectManipulationService::ProcessInput` rebuilds a `MSG` and hands it to DManip:

```cpp
// DirectManipulationService.cpp:563-591
msg.hwnd    = CInputServices::GetUnderlyingInputHwndFromIslandInputSite(m_islandInputSite.Get());
msg.message = GetWindowsMessageFromMessageMap(msgID, fIsSecondaryMessage, fIsKeyboardInput); // WM_POINTERWHEEL / WM_POINTERHWHEEL
msg.wParam  = GetWindowsMessageWParam(msgID, wParam, fInvertForRightToLeft && fIsForHorizontalPan);
msg.lParam  = pMsgPack->m_lParam;
msg.time    = ::GetMessageTime();
msg.pt.x = 0; msg.pt.y = 0;

IFC(pDMViewport->SetContact(fIsKeyboardInput ? DIRECTMANIPULATION_KEYBOARDFOCUS : DIRECTMANIPULATION_MOUSEFOCUS));
if (msgID == XCP_POINTERWHEELCHANGED) {
    XUINT32 pointerId = GET_POINTERID_WPARAM(msg.wParam);
    auto pointerPoint = GetPointerPointFromPointerId(pointerId);
    IFC(m_pDMHelper->ProcessInputWithPointerPoint(&msg, pointerPoint.Get(), &handled));
} else {
    IFC(m_pDMManager->ProcessInput(&msg, &handled));
}
...
IFC(pDMViewport->ReleaseContact(...));   // DirectManipulationService.cpp:599
```

Message mapping (`DirectManipulationService.cpp:4692-4709`):

```cpp
case XCP_POINTERWHEELCHANGED: return fIsSecondaryMessage ? WM_POINTERHWHEEL : WM_POINTERWHEEL;
case XCP_KEYDOWN:             fIsKeyboardInput = TRUE;
                              return fIsSecondaryMessage ? WM_SYSKEYDOWN : WM_KEYDOWN;
```

Pseudo-contact IDs are `DIRECTMANIPULATION_MOUSEFOCUS` / `DIRECTMANIPULATION_KEYBOARDFOCUS` — a
`SetContact`/`ProcessInput`/`ReleaseContact` triple per wheel notch, on the UI thread, but the *resulting
motion* is a DManip **pure-inertia** manipulation that runs entirely on DManip's thread and lands the
viewport in `XcpDMViewportInertia`.

### 2.3 Answering the specific sub-questions

* **Does it go through `DM_POINTERHITTEST`?** No — `DM_POINTERHITTEST` is the *touch/pen* entry point
  (`dxaml/xcp/core/input/InputServices.cpp:1144` `ProcessDirectManipulationPointerHitTest`, dispatched from
  `dxaml/xcp/dxaml/lib/JupiterControl.cpp:297` and `:965`). The wheel uses `ProcessInput` with a
  `MOUSEFOCUS` pseudo-contact.
* **Does it go through `ZoomToRect` / `SetContentOffsets`?** No. Wheel is `ProcessInput` only.
  `ZoomToRect` is for `ChangeView`/`BringIntoView` (§3); `SetContentBounds` is only for keeping the content
  rect in sync.
* **Is it animated?** Yes — but **the animation lives inside `directmanipulation.dll`**, not in XAML.
  XAML supplies **no** curve, no duration, no deceleration. The only inertia knob XAML exposes to DManip is
  the *presence* of the `DMConfigurationPanInertia` flag (see §7.1).
* **Curve / duration constants:** **Not present in this source tree — UNVERIFIED / not-applicable.**
  Searching the whole tree for inertia tuning turns up nothing: the DM service header has no
  `SetDeceleration`, no inertia-parameter API (`DirectManipulationService.h` has only
  `GetContentInertiaEndTransform:136` and `GetCompositorContentTransform:229`). The wheel-scroll feel
  (lines per detent, inertia decay, "smooth scrolling" OS setting) is entirely OS-side and per-device.
* **`ScrollViewerScrollingAndZoomingConstantVelocity`:** **does not exist anywhere in this tree.** The only
  "constant velocity" concept is `DMManipulationState::ConstantVelocityScrollStarted/Stopped`
  (`dxaml/xcp/dxaml/lib/DirectManipulationTypes.h:17-18` and `dxaml/xcp/core/inc/DirectManipulationContainer.h:22-23`),
  covered in §4. Mark any reference to that identifier as unverified.
* **`SCROLL_VIEWER_DEFAULT_*`:** no such prefix in this tree. The real constants are
  `ScrollViewerDefaultMouseWheelDelta` etc. — see §8.

### 2.4 The non-DManip wheel path (the "unsmooth" fallback), with the real WHEEL_DELTA math

`dxaml/xcp/dxaml/lib/ScrollViewer_Partial.h:27-37`:

```cpp
// Default physical amount to scroll with Up/Down/Left/Right key
#define ScrollViewerLineDelta (16.0f)

// This value comes from WHEEL_DELTA defined in WinUser.h. It represents the universal default mouse wheel delta.
#define ScrollViewerDefaultMouseWheelDelta (120)

// These macros compute how many integral pixels need to be scrolled based on the viewport size and mouse wheel delta.
// - First the maximum between 48 and 15% of the viewport size is picked.
// - Then that number is multiplied by (mouse wheel delta/120), 120 being the universal default value.
// - Finally if the resulting number is larger than the viewport size, then that viewport size is picked instead.
#define GetVerticalScrollWheelDelta(size, delta)   (DoubleUtil::Min(DoubleUtil::Floor(size.Height), DoubleUtil::Round(delta * DoubleUtil::Max(48.0, DoubleUtil::Round(size.Height * 0.15, 0)) / 120.0, 0)))
#define GetHorizontalScrollWheelDelta(size, delta) (DoubleUtil::Min(DoubleUtil::Floor(size.Width),  DoubleUtil::Round(delta * DoubleUtil::Max(48.0, DoubleUtil::Round(size.Width  * 0.15, 0)) / 120.0, 0)))
```

Consumed by `ScrollContentPresenter::MouseWheelUp/Down/Left/Right`
(`dxaml/xcp/dxaml/lib/ScrollContentPresenter_Partial.cpp:651-740`), which call
`SetVerticalOffset(offset ± GetVerticalScrollWheelDelta(size, delta))` — a hard jump, `InvalidateArrange`,
one layout pass. **Note the `size` passed is `DesiredSize`, not viewport size** (`:672 get_DesiredSize(&size)`).

**Critically: `ScrollContentPresenter::MouseWheelUp/Down` are only reached when the SCP is NOT the scroll
client** (`ScrollViewer_Partial.cpp:2795-2836`). When SCP *is* the scroll client — the common case in real
apps — the wheel never touches this formula at all; DManip does the whole thing. So the `15% / 48px / 120`
formula is a *fallback* in WinUI, not the primary wheel feel.

---

## 3. Keyboard, `ScrollToVerticalOffset`, `ChangeView`, `BringIntoView`

### 3.1 Keyboard

`ScrollViewer::OnKeyDown` (`ScrollViewer_Partial.cpp:2360`) does **not** compute an offset. After gamepad
handling and chaining checks it calls the *same* DManip pipeline as the wheel:

```cpp
// ScrollViewer_Partial.cpp:2444-2447
messageZoomDirection = GetKeyboardMessageZoomAction(keyModifiers, key);
if (!m_ignoreSemanticZoomNavigationInput || messageZoomDirection == ZoomDirection_None) {
    // Let the InputManager forward this keystroke to DirectManipulation for potential processing.
    IFC(ProcessPureInertiaInputMessage(messageZoomDirection, &handled));
```

The keystroke is stashed for DM by the keyboard processor only for a whitelisted key set:

```cpp
// dxaml/xcp/components/input/lib/KeyboardUtility.cpp:201-219
bool InputUtility::Keyboard::ShouldForwardToDirectManipulation(remappedVirtualKey, virtualKey)
{
    return  remappedVirtualKey == VirtualKey_Escape || Up || Down || Left || Right
         || XboxUtility::IsGamepadNavigationDirection(virtualKey)
         || PageUp || PageDown || Home || End || Add || Subtract
         || virtualKey == VK_OEM_PLUS || virtualKey == VK_OEM_MINUS;
}
```
stashed at `dxaml/xcp/components/ContentRoot/KeyboardInputProcessor.cpp:489-498`.

DManip then decides horizontal vs vertical vs page from the vkey, with XAML pre-filtering by active
configuration:

```cpp
// DirectManipulationService.cpp:4720-4765
IsWindowsMessageForHorizontalPan → XCP_KEYDOWN && (VK_LEFT || VK_RIGHT)
IsWindowsMessageForVerticalPan   → XCP_KEYDOWN && (VK_DOWN || VK_UP)
IsWindowsMessageForPan           → XCP_KEYDOWN && (VK_PRIOR || VK_NEXT || VK_HOME || VK_END)
```
plus RTL mirroring of the vkey (`GetWindowsMessageWParam`, `DirectManipulationService.cpp:4777-4800`:
Left↔Right, PageUp↔PageDown, Home↔End).

**So keyboard scroll in legacy WinUI is animated by DManip, with an OS-owned curve, exactly like the wheel.**

The *unanimated* keyboard path exists too, via `ScrollViewer::ScrollInDirection(key, animate)`
(`ScrollViewer_Partial.cpp:1709`):

```cpp
// ScrollViewer_Partial.cpp:1713-1728
if (animate) {
    // Let DManip animate the scroll within a ListViewBase header or footer.
    IFC(ProcessInputMessage(key == VirtualKey_PageUp || PageDown || Home || End /*ignoreFlowDirection*/, isHandled));
} else {
    ... LineUp() / LineDown() / LineLeft() / LineRight() / PageUp() / PageDown() / PageHome() / PageEnd() ...
}
```

`LineUp()`/`LineDown()` → `HandleVerticalScroll(ScrollEventType_SmallDecrement/Increment)`
(`ScrollViewer_Partial.cpp:3104`) → `IScrollInfo::LineUp()` →
`ScrollContentPresenter::LineUpImpl` → `SetVerticalOffset(offset - ScrollViewerLineDelta)`
(`ScrollContentPresenter_Partial.cpp:442`), i.e. **16 px per arrow key**, instant.

### 3.2 `ScrollToVerticalOffset` / ScrollBar thumb — never animated

`ScrollToVerticalOffsetInternal` (`ScrollViewer_Partial.cpp:1832`) →
`HandleVerticalScroll(ScrollEventType_ThumbPosition, offset)` (`:3104`) →
clamp → `spScrollInfo->SetVerticalOffset(newOffset)` (`:3196`).
`ScrollContentPresenter::SetVerticalOffsetPrivate` (`ScrollContentPresenter_Partial.cpp:902-959`) does:

```cpp
if (!DoubleUtil::AreClose(currentY, scrollY)) {
    IFC_RETURN(pScrollData->put_OffsetY(scrollY));
    IFC_RETURN(InvalidateArrange());     // :949
    m_scrollRequested = TRUE;
}
```
→ a layout pass, then `InvalidateScrollInfo` → `OnPrimaryContentTransformChanged` →
`NotifyBringIntoViewportNeeded` → a **non-animated** `BringIntoViewport` that calls
`SetPrimaryContentTransform` on DManip.

Thumb *drag* additionally uses `EnterIntermediateViewChangedMode()` on `ScrollEventType_ThumbTrack`
(`ScrollViewer_Partial.cpp:3138-3143`) and can defer the scroll entirely via
`IsDeferredScrollingEnabled` (`:3183-3193`) — deferred scrolling trades smoothness for CPU by only applying
the offset on `EndScroll`.

### 3.3 `ChangeView` / `ChangeViewWithOptionalAnimation`

`ChangeViewImpl` (`ScrollViewer_Partial.cpp:3316-3342`) calls `ChangeViewInternal` with:

```
forceChangeToCurrentView   = FALSE
adjustWithMandatorySnapPoints = TRUE
skipDuringTouchContact     = TRUE
skipAnimationWhileRunning  = TRUE
disableAnimation           = FALSE     // ← animated by default
applyAsManip               = TRUE
```

`ChangeViewWithOptionalAnimationImpl` (`:3355-3383`) is identical but takes `disableAnimation` from the caller.

`ChangeViewInternal` (`:3385`) then:

* forces `disableAnimation = TRUE` when DManip is partly off (`:3607-3611`),
* forces `disableAnimation = TRUE` when the OS "Play animations in Windows" setting is off (`:3613-3617`, via
  `IsAnimationEnabled()` at `:59-77`),
* forces `disableAnimation = TRUE` when there is an `IManipulationDataProvider` (logical-scrolling virtualizing
  panel) — **no animation is supported at all there** (`:3630-3643`),
* short-circuits if the target is within tolerance of the current/target view
  (`ScrollViewerScrollRoundingToleranceForBringIntoViewport = 0.001f`, `ScrollViewerZoomRoundingToleranceForBringIntoViewport = 0.00001f`;
  used at `:3871-3877`, `:3967-3969`, `:4011-4029`),
* and finally calls `BringIntoViewportInternal(bounds, ..., animate = !disableAnimation, ...)` (`:4175-4185`).

`BringIntoViewportInternal` (`:8745`) → `CoreImports::ManipulationHandler_BringIntoViewport` →
`CInputServices::BringIntoViewport` (`core/input/InputServices.cpp:12502`) → for the animated case:

```cpp
// InputServices.cpp:12875   (fAnimate == true)
IFC(pDirectManipulationService->BringIntoViewport(pViewport, bounds, fAnimate));
```
→ `CDirectManipulationService::BringIntoViewport` (`DirectManipulationService.cpp:1893`):

```cpp
// DirectManipulationService.cpp:1918
IFC(pDMViewport->ZoomToRect(bounds.X, bounds.Y, bounds.X + bounds.Width, bounds.Y + bounds.Height, fAnimate));
```

**`IDirectManipulationViewport::ZoomToRect(..., animate=TRUE)` *is* the WinUI "animated ChangeView".** The
easing and duration are DManip's; XAML never specifies them. Success is detected purely by DManip having
entered inertia:

```cpp
// InputServices.cpp:12878-12894
IFC(pDirectManipulationService->GetViewportStatus(pViewport, newStatus));
if (fAnimate) {
    if (newStatus == XcpDMViewportInertia) {
        fHandled = TRUE;
        pViewport->SetTargetTranslation(-bounds.X, -bounds.Y);
        if (fIsForMakeVisible) pViewport->SetIsProcessingMakeVisibleInertia(TRUE);
    }
    // Else DManip did not take any action because the target transform is too close to the current one.
}
```

For the **non-animated** case, `SetPrimaryContentTransform` is preferred over `ZoomToRect` to avoid rounding
error at large offsets:

```cpp
// InputServices.cpp:12866-12871
if (!fAnimate && fTransformIsValid) {
    // ... use the DManip SetContentTransformValues API instead of ZoomToRect to avoid rounding errors
    IFC(pDirectManipulationService->SetPrimaryContentTransform(pViewport,
        translateX - contentOffsetX * zoomFactor, translateY - contentOffsetY * zoomFactor, zoomFactor));
}
```

…and it is preceded by a **deliberate glitch-avoidance dance** that is directly relevant to "what breaks
smoothness" (`InputServices.cpp:12807-12841`, abridged):

```
// With DManip-on-DComp we must guard against a synchronization problem when performing the ZoomToRect().
// DManip would normally send the updated transform to DComp immediately which may be out of sync with
// operations XAML is trying to perform on the UI thread, resulting in a noticeable glitch.
// 1) virtualizing panels change their extent estimate ... adjustment transform on the UI thread to counteract the jump
// 2) layout change + DManip config change → DManip changes output transform before we even call ZoomToRect
// 3) V/SIS customers complete drawing + ChangeView together
// Fix: 1) detach the shared DManip transform from the DManip content (leave it on the visual), clear CompNode data
//      2) dirty the UIElement so next UI frame forces creation of a new DManip transform
//      3) call ZoomToRect (DManip updates its notion but it is not propagated to DComp)
//      4) next UI frame: create new shared transform, update it, attach, commit from the UI thread
// Note we don't do this synchronization when animating — the transform smoothly transitions, so no glitch possible.
IFC(PrepareCompositionNodesForBringIntoView(pViewport));
```

**That is the single clearest statement in the codebase of why WinUI prefers animated view changes: the
non-animated path requires an explicit 2-frame detach/reattach to avoid tearing between the DManip thread
and the UI thread.**

### 3.4 `BringIntoView` / `MakeVisible`

`ScrollViewer::OnBringIntoViewRequested` (`ScrollViewer_Partial.cpp:2992`) reads
`args->get_AnimationDesired(&useAnimation)` (`:3070`) and forwards to `MakeVisible(..., useAnimation, ...)`
(`:2864` signature, `:2934`/`:2977` call sites).

`ScrollContentPresenter::MakeVisibleImpl` (`ScrollContentPresenter_Partial.cpp:1078`) then branches at
`:1360`:

```cpp
if (spScrollViewer && useAnimation) {
    ...
    // disableAnimation is FALSE by default, which is what we want here.
    IFC_RETURN(spScrollViewer.Cast<ScrollViewer>()->ChangeViewInternal(
        hOffset, vOffset, NULL, NULL,
        FALSE /*forceChangeToCurrentView*/, TRUE /*adjustWithMandatorySnapPoints*/,
        TRUE  /*skipDuringTouchContact*/,  TRUE /*skipAnimationWhileRunning*/,
        FALSE /*disableAnimation*/,        TRUE /*applyAsManip*/,
        FALSE /*transformIsInertiaEnd*/,   TRUE /*isForMakeVisible*/, &handled));
} else {
    ... SetHorizontalOffsetPrivate / SetVerticalOffsetPrivate ...   // :1420-1470, instant
}
```

`ScrollContentPresenter::MakeVisibleImpl` (public 4-arg overload) hardcodes `FALSE /*useAnimation*/`
(`:1054-1057`).

The `isForMakeVisible` flag matters for smoothness: it sets `SetIsProcessingMakeVisibleInertia(TRUE)`, and a
subsequent user input will **cancel** that inertia rather than fight it:

```cpp
// InputServices.cpp:6781-6798 (in ProcessInputMessageWithDirectManipulation)
if (pViewport->GetIsProcessingMakeVisibleInertia()) {
    IFC(StopInertialViewport(pViewport, true /*restrictToKnownInertiaEnd*/, &inertiaStopped));
    if (!inertiaStopped && pViewport->GetIsProcessingMakeVisibleInertia()) {
        // Do not forward this message to DManip in this rare case. It is still performing the animation for a
        // MakeVisible call and inertia could not be stopped. This avoids the viewport landing on a random transform.
        goto Cleanup;
    }
}
```

---

## 4. The "constant velocity" path

This is **auto-scroll / edge-scroll during drag-and-drop**, not a general scroll mechanism.

* Public entry: `ScrollViewer::SetConstantVelocities(dx, dy)` (`ScrollViewer_Partial.cpp:8981`) →
  `CoreImports::ManipulationHandler_SetConstantVelocities` (`core/dll/CoreImports.cpp:3505`) →
  `CInputServices::SetConstantVelocities` (`core/input/InputServices.cpp:9179`).
* `ASSERT(!(panXVelocity != 0.0f && panYVelocity != 0.0f))` (`InputServices.cpp:9201`) — **one axis at a time only**.
* It refuses to start while a real manipulation is running (`InputServices.cpp:9228-9231`):
  `viewportStatus != Running && != Inertia && != Suspended`.
* It re-uses the **bring-into-viewport configuration** (`fActivateBringIntoViewConfiguration = TRUE`,
  `InputServices.cpp:9299`) and then activates a DManip auto-scroll behavior:

```cpp
// InputServices.cpp:9339-9352
if (panXVelocity != 0.0f)
    IFC(pDirectManipulationService->ActivateAutoScroll(pViewport, XcpDMMotionTypePanX, panXVelocity < 0.0f /*autoScrollForward*/));
else
    IFC(pDirectManipulationService->ActivateAutoScroll(pViewport, XcpDMMotionTypePanY, panYVelocity < 0.0f /*autoScrollForward*/));
```

* `CDirectManipulationService::ActivateAutoScroll` (`DirectManipulationService.cpp:3414`) lazily creates a
  `CLSID_Microsoft_AutoScrollBehavior` and configures direction only:

```cpp
// DirectManipulationService.cpp:3455-3463
IFC(spManager2->CreateBehavior(CLSID_Microsoft_AutoScrollBehavior, IID_PPV_ARGS(&spAutoScrollBehavior)));
IFC(spDMViewport2->AddBehavior(spAutoScrollBehavior.Get(), &m_autoScrollBehaviorCookie));
...
IFC(m_spAutoScrollBehavior->SetConfiguration(dmMotionType,
       autoScrollForward ? DIRECTMANIPULATION_AUTOSCROLL_CONFIGURATION_FORWARD
                         : DIRECTMANIPULATION_AUTOSCROLL_CONFIGURATION_REVERSE));
```

  **The magnitude of `panXVelocity`/`panYVelocity` is discarded** — DManip only gets a direction. XAML uses the
  magnitude only to decide "same direction → no-op" (`InputServices.cpp:9240-9250`).

* Viewport enters `XcpDMViewportAutoRunning`; UI-thread state becomes
  `ConstantVelocityScrollStarted (6)` / `ConstantVelocityScrollStopped (7)`
  (`InputServices.cpp:8414`, `ProcessConstantVelocityViewportStatusUpdate` at `:8393`).
  `ScrollViewer` mirrors it in `m_isInConstantVelocityPan` (`ScrollViewer_Partial.cpp:13018-13029`) and reports
  `IsInManipulation()` true (`ScrollViewer_Partial.h:1184`).

* The only real caller is `ListViewBase` reorder edge-scrolling
  (`dxaml/xcp/dxaml/lib/ListViewBase_Partial_Reorder.cpp:1796`), with a linear velocity ramp:

```cpp
// ListViewBase_Partial_Reorder.cpp:39-47
#define LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX 100
#define LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC 50
#define LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED (150.0  /* px/sec */)
#define LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED (1500.0 /* px/sec */)

// :1732-1746
return (XFLOAT)(MAX_SPEED - (distanceFromEdge / EDGE_WIDTH_PX) * (MAX_SPEED - MIN_SPEED));
```
with a 50 ms `DispatcherTimer` start delay and instant velocity updates thereafter
(`ListViewBase_Partial_Reorder.cpp:1754-1782`, timer at `:1804-1828`).

---

## 5. UI-thread work per scroll frame — and what is kept off it

### 5.1 What the UI thread does per frame while scrolling

`CInputServices::ProcessUIThreadTick` → `ProcessDirectManipulationViewportChanges(pViewport)`
(`InputServices.cpp:7123`), per viewport, per tick:

1. Drain the queued DManip status changes (`GetStatusesCount()`, loop at `:7186-7305`). There is a
   de-glitching workaround for a spurious `Ready` between two active statuses
   (`:7136-7174`, `StatusChangesForIntermediaryStatus = 2` at `:7124`).
2. `ProcessDirectManipulationViewportValuesUpdate` (`:8604`): one `GetPrimaryContentTransform` read,
   `SetCurrentTransformationValues`, `NWSetTransformDirty(..., Independent|Render|Bounds)`,
   `ProcessDirectManipulationSecondaryContentsUpdate` (headers), then
   `NotifyManipulationProgress(ManipulationDelta | ManipulationLastDelta, ...)`.
3. `ScrollViewer::NotifyManipulationProgress` (`ScrollViewer_Partial.cpp:12887`) → `HandleManipulationDelta`
   (`:13432`), which per frame:
   * `ManipulationHandler_GetPrimaryContentTransform` (`:13492`);
   * zoom-factor change detection with `ScrollViewerZoomRoundingTolerance` = `0.000001f` (`:13513-13515`);
   * `ComputeTranslationXCorrection` / `ComputeTranslationYCorrection` (`:13630-13649`);
   * `ScrollByPixelDelta(...)` for X and Y (`:13734-13742`) → `ScrollToHorizontal/VerticalOffsetInternal` →
     `IScrollInfo::SetHorizontal/VerticalOffset` (which for the SCP means **`InvalidateArrange`**, see §6);
   * inertia-end offsets bookkeeping (`:13748-13816`);
   * `DelayViewChanging()/DelayViewChanged()` at entry (`:13478-13480`) and `FlushViewChanging/FlushViewChanged`
     at exit (`:13836-13838`) so **at most one `ViewChanging` + one `ViewChanged` pair per frame**.
4. `InvalidateScrollInfo` → **`ScrollBar::put_Value(m_yOffset)`** (`ScrollViewer_Partial.cpp:4614`) and the
   horizontal equivalent — i.e. the ScrollBar thumb is a *dependent* update, one UI frame behind the content.
5. `RequestAdditionalFrame()` while active (`InputServices.cpp:7318-7322`) — keeps the XAML tick alive so the
   bookkeeping keeps up with DManip.
6. `RefreshDirectManipulationHandlerWantsNotifications` (`InputServices.cpp:6988`) — a **200-tick countdown**
   (`#define UITicksThresholdForNotifications 200`, `dxaml/xcp/core/inc/InputServices.h:30`) keeping the
   ScrollViewer "listening" for DManip-affecting property changes after an interaction, then switching it off.
7. `OnPostUIThreadTick` (`InputServices.cpp:9135`) → `StopInertialViewportsWithoutCompositorPeer()` (`:7353`) —
   if a viewport is in `Inertia` but its manipulated element lost its composition peer, inertia is stopped and
   the transform snapped to the end value (`:7377-7402`), because there is no shared transform to animate.

### 5.2 What is deliberately NOT on the UI thread

* **The scroll transform itself** — expression-driven from DManip's property set (§1.2).
* **The clip transform** — same (`sc_Expression_DManipClipTransform`).
* **Header (secondary content) transforms** — combined in the same expression
  (`ConnectComplexAnimationWithPrependTransform`, `dxaml/xcp/core/hw/ManipulationTransform.cpp:107-137`;
  `DManipData.cpp:166-170`).
* **The inertia simulation** — DManip's own thread, ticked via
  `CCompositorDirectManipulationViewport::UpdateTransform()`
  (`dxaml/xcp/core/compositor/CompositorDirectManipulationViewport.cpp:50-64`), which passes a *frame-latency
  hint* to DManip:

```cpp
// CompositorDirectManipulationViewport.cpp:58-63
// TODO - Jupiter (Windows) bug 847117. Replace 16 with the actually milliseconds until the transform is shown on screen
IGNOREHR(pCompositorService->UpdateCompositorContentTransform(pCompositorContent, 16 /*deltaCompositionTime*/));
```

  That 16 flows into `CDirectManipulationFrameInfoProvider::GetNextFrameInfo`
  (`plat/win/browserdesktop/DirectManipulationFrameInfoProvider.cpp:47-69`) as `pCompositionTime`, i.e.
  **DManip is told "this transform will be on screen in 16 ms" and extrapolates accordingly.** `pTime` and
  `pProcessTime` are returned as 0 (`:60-64`). This is a hard-coded 1-frame-at-60 Hz assumption.

* **Hit-test-to-manipulation handoff.** `DM_POINTERHITTEST` arrives *before* `WM_POINTERDOWN`; XAML hit-tests
  synchronously and calls `SetContact` so DManip owns the pointer stream from that moment:

```cpp
// InputServices.cpp:1165-1200  ProcessDirectManipulationPointerHitTest
IFC(contentRoot->GetInputManager().GetPointerInputProcessor().HitTestHelper(
        pMsg->m_pointerInfo.m_pointerLocation, contentRoot->GetXamlIslandRootNoRef(), &pDOContact));
if (!pDOContact) { pDOContact = contentRoot->GetVisualTreeNoRef()->GetPublicRootVisual(); ... }
IFC(InitializeDirectManipulationForPointerId(pMsg->m_pointerInfo.m_pointerId, TRUE /*fIsForDMHitTest*/, pContactElement, &unused));
```

  Dispatched at `dxaml/xcp/dxaml/lib/JupiterControl.cpp:297` / `:965`, listed as an input message at
  `dxaml/xcp/dxaml/lib/JupiterWindow.cpp:328`.
  **This one synchronous hit-test is the *entire* UI-thread cost of starting a touch pan.** After that the
  finger tracks at compositor rate regardless of UI-thread health.

* **Viewport input/update mode is fully automatic** so DManip never waits on XAML:

```cpp
// DirectManipulationService.cpp:4305-4313
// Use the delegate thread mechanism
IFC(dmViewport->SetInputMode(DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC));
// Viewports that use the compositor must be in automatic input mode.
IFC(dmViewport->SetUpdateMode(DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC));
// Disable DManip pixel snapping as we will perform all pixel snapping ourselves.
IFC(dmViewport->SetViewportOptions(DIRECTMANIPULATION_VIEWPORT_OPTIONS_DISABLEPIXELSNAPPING));
```

### 5.3 Pixel snapping policy — crisp while panning, smooth while inerting

```cpp
// dxaml/xcp/core/core/elements/uielement.cpp:11865-11896  CUIElement::ShouldDisablePixelSnapping()
if (IsTransformOrOffsetAffectingPropertyIndependentlyAnimating()) {
    if (IsManipulatedIndependently()) {              // DManip is animating the transform
        GetDirectManipulationViewportStatus(this, &status);
        if (status == XcpDMViewportInertia) {
            // Element has inertia, so disable pixel snapping to prevent jittering.
            // In other states, like panning, enable pixel snapping, so content can be clearly rendered.
            disablePixelSnapping = true;
        }
    } else {
        // XAML is animating the transform, so disable pixel snapping to prevent jittering.
        disablePixelSnapping = true;
    }
}
```
consumed in `HWCompTreeNode::…` (`dxaml/xcp/core/hw/hwcompnode.cpp:541-549`) and applied to the WUC visuals:

```cpp
// dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp:2722-2741  UpdatePrimaryVisualPixelSnapping
// We turn on pixel snapping for all manipulatable CompNodes to keep content crisp
// even when DManip computes sub-pixel offsets for the manipulation transform.
const bool isPixelSnappingEnabled = hasIndependentTransformManipulation && !disablePixelSnapping;
IFCFAILFAST(visual4->put_IsPixelSnappingEnabled(isPixelSnappingEnabled));
```

and a second, narrower rule for the **prepend** visual, explicitly to stop header/content "jiggle"
(`HWCompNodeWinRT.cpp:1227-1271`):

```cpp
// ...Parent and Child pixel snap at different times and Child appears to jiggle up and down.
// By turning on pixel snapping for the PrependVisual, we help guarantee the primary and secondary content
// stay aligned with each other and we avoid the jiggling.
// We scope this only to the primary manipulatable content of ItemsPresenters as this is the only known problematic scenario.
// To prevent jittering, don't pixel snap if a transform is being animated.
const bool isPixelSnappingEnabled = hasIndependentTransformManipulation && isPrimaryManipulatableContent
    && !disablePixelSnapping && m_pUIElementNoRef->OfTypeByIndex<KnownTypeIndex::ItemsPresenter>();
```

**This is a real, shipped smoothness trade-off: snap during finger-tracking (crisp text, and the offset is
finger-locked anyway), stop snapping during inertia (a snapped decelerating curve looks like stutter).**

---

## 6. Invalidation / layout on scroll: what a scroll does and does not trigger

### 6.1 During a manipulation: layout does NOT move the content

This is the crucial mechanism. `ScrollContentPresenter::ArrangeOverride`
(`ScrollContentPresenter_Partial.cpp:2094`) pins the child's arrange origin to the **pre-manipulation**
offset while a manipulation is running:

```cpp
// ScrollContentPresenter_Partial.cpp:2256-2271
if (spScrollViewer && spScrollViewer.Cast<ScrollViewer>()->IsInManipulation())
{
    offsetX = -(spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationOffsetX());
    offsetY = -(spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationOffsetY());
    zoomFactor = spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationZoomFactor();
}
else
{
    if (spScrollViewer && spScrollViewer.Cast<ScrollViewer>()->IsInDirectManipulationCompletion())
        IFC(spScrollViewer.Cast<ScrollViewer>()->PostDirectManipulationLayoutRefreshed());
    offsetX = -static_cast<FLOAT>(pScrollData->m_ComputedOffset.X);
    offsetY = -static_cast<FLOAT>(pScrollData->m_ComputedOffset.Y);
    zoomFactor = currentZoomFactor;
}
```

The pre-manipulation values are captured once in `HandleManipulationStarting`
(`ScrollViewer_Partial.cpp:~13370-13392`: `m_preDirectManipulationOffsetX/Y`, `m_preDirectManipulationZoomFactor`,
accessors at `ScrollViewer_Partial.h:1221+`).

> ⚠️ In *this* snapshot `offsetX`/`offsetY`/`zoomFactor` are computed at `:2258-2270` and then **not read again**
> inside `ArrangeOverride` (verified by grepping `offsetX\b` restricted to lines 2090–2600: only 2133/2258/2268).
> The consumer appears to have been refactored away or moved to the DManip content-rect path
> (`SetContentBounds`, `CInputServices::UpdateManipulationPrimaryContentTransform`,
> `InputServices.cpp:12104-12107`). **Flagging this as a partial UNVERIFIED**: the *intent* and the
> `IsInManipulation()` branch are unambiguous in source, but I could not locate the final consumer of the
> computed offsets in this tree.

### 6.2 What a scroll *does* invalidate

* **Non-DManip offset change** (`ScrollToVerticalOffset`, thumb, arrow key fallback, wheel-on-`IScrollInfo`):
  `ScrollContentPresenter::SetVerticalOffsetPrivate` → `InvalidateArrange()`
  (`ScrollContentPresenter_Partial.cpp:949`; horizontal at `:871`) → a full arrange pass of the SCP subtree.
* **DManip-driven offset change:** `HandleManipulationDelta` → `ScrollByPixelDelta` →
  `ScrollToVerticalOffsetInternal` → `SetVerticalOffset` → also `InvalidateArrange`. **But** the arrange then
  uses the pinned pre-manipulation offset (§6.1) and the DManip transform is what visibly moves — so the
  arrange exists to drive **virtualization** and `ScrollData`/extent bookkeeping, not to move pixels.
* **Only bounds, never render, from the DM transform itself** — `DirtyFlags::Independent` (§1.3).
* **A forced synchronous `UpdateLayout()` on the last delta** when there is an uncommitted zoom change:

```cpp
// ScrollViewer_Partial.cpp:13664-13707
// A layout is forced only when there is an uncommitted zoom factor change and the ScrollContentPresenter is the IScrollInfo implementer
if (isLastDelta && m_trElementScrollContentPresenter && (m_contentWidthRequested != -1 || m_contentHeightRequested != -1)) {
    ...
    m_isOffsetChangeIgnored = TRUE;
    IFC(m_trElementScrollContentPresenter.Cast<ScrollContentPresenter>()->UpdateLayout());
    m_isOffsetChangeIgnored = FALSE;
    ...
}
```
  and similarly in `GetManipulationPrimaryContent` (`ScrollViewer_Partial.cpp:11325`) and
  `ChangeViewInternal` (`:4329`, `:4354`). These synchronous `UpdateLayout()` calls are the known
  UI-thread hazards; note they are all scoped to *start* / *end* / *zoom* rather than every frame.

* **Anchoring** runs inside `ScrollViewer::ArrangeOverride` (`ScrollViewer_Partial.cpp:2120-2260`):
  `IsAnchoring()` → `EnsureAnchorElementSelection(preArrangeViewport)` (`:2154`) →
  `ComputeViewportToElementAnchorPointsDistance` pre-arrange → base `ArrangeOverride` →
  same computation post-arrange → `m_pendingViewportShiftX/Y`. Edge-anchoring tolerance
  `const double c_edgeDetectionTolerance = 0.1;` (`ScrollViewer_Partial.cpp:16127`, used at `:16221`, `:16228`,
  `:16250`). Anchoring corrections are *offset* corrections applied through the normal
  `ChangeView`/`BringIntoViewport` machinery — i.e. they can fight a running manipulation if unguarded.

* **`RequestReplayPreviousPointerUpdate()`** is called on content-offset change so hover state stays correct
  without any pointer moving (`dxaml/xcp/core/core/elements/ScrollViewer.cpp:69`).

---

## 7. Snap points, rails, overpan/bounce — expressed to DManip so they stay independent

The design rule is: **anything that must be applied per-frame during inertia is expressed as DManip state,
never as UI-thread logic.**

### 7.1 Configurations (rails + inertia)

`DMConfigurations` (`dxaml/xcp/dxaml/lib/DirectManipulationTypes.h:34-43`), kept in sync with
`XDMConfigurations` in `paltypes.h`:

```
DMConfigurationNone        = 0x00
DMConfigurationInteraction = 0x01
DMConfigurationPanX        = 0x02
DMConfigurationPanY        = 0x04
DMConfigurationZoom        = 0x10
DMConfigurationPanInertia  = 0x20
DMConfigurationZoomInertia = 0x80
DMConfigurationRailsX      = 0x100
DMConfigurationRailsY      = 0x200
```

Built in `ScrollViewer::GetManipulationConfigurations` (touch) — rails and inertia are pure flags:

```cpp
// ScrollViewer_Partial.cpp:8145-8157
panXConfiguration = DMConfigurationPanX;
IFC(GetEffectiveIsHorizontalRailEnabled(canUseCachedProperties, isRailEnabled));
if (isRailEnabled) panXConfiguration = (DMConfigurations)(panXConfiguration + DMConfigurationRailsX);
IFC(GetEffectiveIsScrollInertiaEnabled(canUseCachedProperties, isScrollInertiaEnabled));
if (isScrollInertiaEnabled) panXConfiguration = (DMConfigurations)(panXConfiguration + DMConfigurationPanInertia);
```
(vertical equivalent at `:8228-8240`, with a de-dup that removes `PanInertia` before re-adding it at `:8248-8251`).

Non-touch (wheel/keyboard) configuration (`ScrollViewer::GetNonTouchManipulationConfiguration`,
`ScrollViewer_Partial.cpp:8422-8470`) has **no rails** — only `PanX | PanY | PanInertia | Zoom | ZoomInertia`.

Bring-into-viewport configuration is a fixed constant (`ScrollViewer_Partial.cpp:7927`):

```cpp
DMConfigurations bringIntoViewportConfiguration =
    (DMConfigurations)(DMConfigurationPanX + DMConfigurationPanY + DMConfigurationZoom
                     + DMConfigurationPanInertia + DMConfigurationZoomInertia);
```

`ScrollMode.Auto` becomes an *optional* configuration set (multiple configurations added to the viewport so
DManip can pick per-gesture): `cConfigurations = 1 * (isPanXOptional ? 2 : 1) * (isPanYOptional ? 2 : 1)`
(`ScrollViewer_Partial.cpp:8290`).

### 7.2 Snap points

Pushed wholesale to DManip; **DManip enforces them during inertia**, XAML does not.

* `ScrollViewer::NotifySnapPointsChanged` → `CInputServices::NotifySnapPointsChanged`
  (`InputServices.cpp:11862` region → `UpdateManipulationSnapPoints` at `:12196`).
* Regular snap points → `IDirectManipulationPrimaryContent::SetSnapInterval(motion, interval, offset)`
  (`DirectManipulationService.cpp:2470`).
* Irregular snap points → `SetSnapPoints(motion, pSnapPoints, cSnapPoints)`
  (`DirectManipulationService.cpp:2522`).
* Type (mandatory vs optional, single vs multiple) →
  `CDirectManipulationService::SetPrimaryContentSnapPointsType` (`DirectManipulationService.cpp:2543+`).
* Coordinate system chosen by motion type:
  `XDMSnapCoordinate snapCoordinate = (motionType == XcpDMMotionTypeZoom) ? XcpDMSnapCoordinateOrigin : XcpDMSnapCoordinateBoundary;`
  (`InputServices.cpp:12216`); enum at `DirectManipulationTypes.h:57-62`
  (`Boundary=0x00, Origin=0x01, Mirrored=0x10`).
* Nudge tolerance for a snap point landing on `extent - viewport`:
  `#define ScrollViewerSnapPointLocationTolerance (0.0001f)` (`ScrollViewer_Partial.h:72`).
* For **programmatic** view changes, XAML pre-adjusts the target to a mandatory snap point on the UI thread
  (`AdjustViewWithMandatorySnapPoints`, `ScrollViewer_Partial.cpp:4291+`), because `ZoomToRect`'s target must
  already be snap-consistent.

### 7.3 Overpan / bounce

Two regimes:

* **Default** (`XcpDMOverpanModeDefault`, `DirectManipulationTypes.h:76-80`): DManip's built-in overpan/bounce.
  Nothing in XAML.
* **`DMOverpanModeNone` (0x04)**: XAML installs *parametric motion behaviors and reflexes* so overpan can be
  suppressed/customized **still entirely on DManip's thread**:

```cpp
// DirectManipulationService.cpp:5010-5032
pReflexes = new ViewportOverpanReflexes();
IFC(CreateParametricBehavior(spManager2.Get(), spDMViewport2.Get(), pReflexes));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spContentPrimaryReflex));
IFC(CreateParametricReflex(..., &pReflexes->m_spContentSecondaryReflex));
IFC(CreateParametricReflex(..., &pReflexes->m_spLeftHeaderPrimaryReflex));
... m_spLeftHeaderSecondaryReflex, m_spTopHeaderPrimaryReflex, m_spTopHeaderSecondaryReflex ...
```

  Reflexes are `CLSID_Microsoft_ParametricMotionBehavior` contents (`DirectManipulationService.cpp:5169-5186`)
  whose curves are refreshed only on manipulation start:

```cpp
// DirectManipulationService.cpp:5051-5115  RefreshOverpanCurves
if (::EnumDisplaySettings(nullptr, ENUM_CURRENT_SETTINGS, &dm)) {
    XFLOAT physicalDeviceHeight = static_cast<XFLOAT>(dm.dmPelsHeight);
    XFLOAT logicalDeviceHeight  = physicalDeviceHeight / pReflexes->m_zoomScale;
    centerpointOffset = logicalDeviceHeight * s_centerPointScaleFactor;    // 1.94f
}
IFC(m_pDMHelper->ApplyPrimaryReflexCurves(..., s_curveSuppressionValueForZoom /*1.0f*/,
                                               s_curveSuppressionValueForTranslate /*0.0f*/));
IFC(m_pDMHelper->ApplySecondaryReflexCurves(..., s_linearCurvePassThroughSlope /*1.0f*/,
                                                 s_curveSuppressionValueForTranslate /*0.0f*/, s_range /*{0, FLT_MAX}*/));
```

  Guarded by `XcpAutoLock lock(m_overpanReflexesLock)` (`:4975`, `:4993`) precisely because
  **the compositor thread reads `m_mapViewportOverpanReflexes` while only the UI thread writes it**
  (comment at `DirectManipulationService.cpp:4998-5000`).
  Behavior changes are deferred to the *start of a new manipulation* to avoid a documented DManip deadlock:

```cpp
// DirectManipulationService.cpp:4964-4971
// We can't update the viewport behavior while in manipulation since this may hit a DM deadlock (WPB 275883).
```

**Caveat:** `s_maxOverpanDistance = 200.0f`, `s_scaleOverpanValue = 0.91f`, `s_minOverpanDistance = 1.0f`
(`DirectManipulationService.cpp:35-37`) are **declared but have no consumer anywhere in this tree** (grep for
each name yields only the definition and the `.h` declaration). Treat them as dead/legacy.

### 7.4 Chaining

`SetChaining(dmMotionTypes)` (`DirectManipulationService.cpp:1634`, and the Auto-mode variant at `:985`) —
DManip chains between nested viewports on its own. Zoom chaining is the exception and is done on the UI
thread through routed events because "DManip doesn't provide for chaining of inertia-only manipulations"
(`ScrollViewer_Partial.cpp:9765-9770`).

---

## 8. Every constant / magic number relevant to scroll feel

### 8.1 `dxaml/xcp/dxaml/lib/ScrollViewer_Partial.h`

| Constant | Value | Line | Meaning |
|---|---|---|---|
| `ScrollViewerLineDelta` | `16.0f` | 27 | px per arrow-key line scroll (non-DManip path) |
| `ScrollViewerDefaultMouseWheelDelta` | `120` | 30 | `WHEEL_DELTA` |
| `GetVerticalScrollWheelDelta(size, delta)` | `min(floor(size.Height), round(delta * max(48.0, round(size.Height*0.15)) / 120.0))` | 36 | wheel px, `IScrollInfo` fallback path only |
| `GetHorizontalScrollWheelDelta(size, delta)` | same with `Width` | 37 | ditto |
| `ScrollViewerMinimumZoomFactor` | `0.1f` | 42 | floor for Min/Max/current zoom |
| `ScrollViewerScrollRoundingTolerance` | `0.05f` | 46 | px tolerance for non-DM-driven scrolls |
| `ScrollViewerScrollRoundingToleranceForProvider` | `1.0f` | 51 | px tolerance when `IManipulationDataProvider` present |
| `ScrollViewerScrollRoundingToleranceForBringIntoViewport` | `0.001f` | 56 | min delta to bother calling `BringIntoViewport` |
| `ScrollViewerZoomExtentRoundingTolerance` | `0.001f` | 60 | |
| `ScrollViewerZoomRoundingTolerance` | `0.000001f` | 64 | zoom-change detection in DM delta handling |
| `ScrollViewerZoomRoundingToleranceForBringIntoViewport` | `0.00001f` | 68 | |
| `ScrollViewerSnapPointLocationTolerance` | `0.0001f` | 72 | snap-point nudge near `extent - viewport` |
| `ScrollViewerMinHeightToReflowAroundOcclusions` | `32.0f` | 77 | min viewport height when reflowing around IHM |

### 8.2 `dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp`

| Constant | Value | Line |
|---|---|---|
| `SCROLLVIEWER_KEYCODE_EQUALS` | `187` | 55 |
| `c_edgeDetectionTolerance` (anchoring) | `0.1` | 16127 |
| bring-into-viewport configuration | `PanX|PanY|Zoom|PanInertia|ZoomInertia` | 7927 |

### 8.3 `dxaml/xcp/core/inc/InputServices.h`

| Constant | Value | Line |
|---|---|---|
| `UITicksThresholdForNotifications` | `200` | 30 |

### 8.4 `dxaml/xcp/core/input/InputServices.cpp`

| Constant | Value | Line |
|---|---|---|
| `StatusChangesForIntermediaryStatus` | `2` | 7124 |

### 8.5 `dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.cpp`

| Constant | Value | Line | Notes |
|---|---|---|---|
| `s_maxOverpanDistance` | `200.0f` | 35 | **no consumer in tree** |
| `s_scaleOverpanValue` | `0.91f` | 36 | **no consumer in tree** |
| `s_minOverpanDistance` | `1.0f` | 37 | **no consumer in tree** |
| `s_centerPointScaleFactor` | `1.94f` | 38 | overpan reflex centerpoint = logicalDeviceHeight × 1.94 |
| `s_curveSuppressionValueForZoom` | `1.0f` | 39 | |
| `s_curveSuppressionValueForTranslate` | `0.0f` | 40 | |
| `s_linearCurvePassThroughSlope` | `1.0f` | 41 | secondary reflex curve slope |
| `s_range` | `{ 0, FLT_MAX }` | 42 | reflex curve domain |

### 8.6 `dxaml/xcp/core/compositor/CompositorDirectManipulationViewport.cpp`

| Constant | Value | Line | Notes |
|---|---|---|---|
| `deltaCompositionTime` | `16` (ms) | 63 | hard-coded frame-latency hint fed to DManip's `GetNextFrameInfo`; the source comment flags it as a TODO (bug 847117) |

### 8.7 `dxaml/xcp/dxaml/lib/ListViewBase_Partial_Reorder.cpp` (edge/auto-scroll)

| Constant | Value | Line |
|---|---|---|
| `LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX` | `100` | 39 |
| `LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC` | `50` | 40 |
| `LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED` | `150.0` px/s | 46 |
| `LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED` | `1500.0` px/s | 47 |

### 8.8 `controls/dev/CommonStyles/ScrollViewer_themeresources.xaml` (indicator feel)

| Resource | Value | Line |
|---|---|---|
| `ScrollViewerSeparatorContractBeginTime` | `00:00:02.00` | 17 |
| `ScrollViewerSeparatorContractDelay` | `00:00:02` | 18 |
| TouchIndicator → NoIndicator hide delay | `0:0:0.5` | 99 (and 110) |

### 8.9 Contrast: modern MUX `ScrollPresenter` (`controls/dev/ScrollPresenter/ScrollPresenter.h`)

These are the *replacement* architecture's constants and are worth having side-by-side, because unlike legacy
they are **XAML-owned** durations:

| Constant | Value | Line |
|---|---|---|
| `s_offsetsChangeMsPerUnit` | `5` | 68 |
| `s_offsetsChangeMinMs` | `50` | 69 |
| `s_offsetsChangeMaxMs` | `1000` | 70 |
| `s_zoomFactorChangeMsPerUnit` | `250` | 73 |
| `s_zoomFactorChangeMinMs` | `50` | 74 |
| `s_zoomFactorChangeMaxMs` | `1000` | 75 |
| `s_translationAndZoomFactorAnimationsRestartTicks` | `4` | 78 |
| `s_defaultMinZoomFactor` / `s_defaultMaxZoomFactor` | `0.1` / `10.0` | 63-64 |

`ScrollPresenter` duration formula: `clamp(minDuration, unitDuration * distance, maxDuration)` around
`ScrollPresenter.cpp:3257-3259` and `:3320`.

---

## 9. State machines worth naming

**`DMManipulationState`** (`dxaml/xcp/dxaml/lib/DirectManipulationTypes.h:12-19`, mirrored in
`dxaml/xcp/core/inc/DirectManipulationContainer.h:15-23`):

```
ManipulationStarting = 1, ManipulationStarted = 2, ManipulationDelta = 3,
ManipulationLastDelta = 4, ManipulationCompleted = 5,
ConstantVelocityScrollStarted = 6, ConstantVelocityScrollStopped = 7
```

**`XDMViewportStatus`** (`dxaml/xcp/pal/inc/paltypes.h:428-438`) — see §1.1.

**`DMMotionTypes`** (`DirectManipulationTypes.h:47-54`): `PanX=0x01, PanY=0x02, Zoom=0x04, CenterX=0x10, CenterY=0x20`.

**`DMAlignment`** (`DirectManipulationTypes.h:22-29`): `None=0x00, Near=0x01, Center=0x02, Far=0x04, UnlockCenter=0x08`.

**`DMContentType`** (`DirectManipulationTypes.h:65-72`): `Primary=0, TopLeftHeader=1, TopHeader=2, LeftHeader=3, Custom=4, Descendant=5`.

Note the two-phase status processing per tick (`InputServices.cpp:7186-7305`): each queued status transition is
run through `ProcessDirectManipulationViewportStatusUpdate` *pre*-values-change and *post*-values-change, with
`RaiseQueuedDirectManipulationStateChanges` in between, so `DirectManipulationStarted` always precedes
`ViewChanging`/`ViewChanged`, and `DirectManipulationCompleted` always follows them
(`:7259-7265`, `:7290-7300`).

---

## 10. What breaks smoothness in this design (as evidenced by the code's own workarounds)

1. **Any UI-thread work that must complete before DManip can move.** Mitigated by making DManip fully
   `AUTOMATIC` input/update mode and by the ExpressionAnimation graph (§1.2, §5.2). The *only* mandatory
   synchronous UI-thread work is the `DM_POINTERHITTEST` hit test.
2. **Non-animated `ZoomToRect` / config changes tearing against a UI-thread frame** → the explicit
   detach/dirty/ZoomToRect/reattach protocol in `PrepareCompositionNodesForBringIntoView`
   (`InputServices.cpp:12807-12841`).
3. **Virtualizing panels changing their extent estimate mid-scroll** → offset compensation and
   `SetContentBounds`; and the forced `UpdateLayout()` on last delta (`ScrollViewer_Partial.cpp:13683`).
4. **Pixel snapping during inertia** → explicitly disabled (`uielement.cpp:11881-11886`).
5. **Prepend/primary visuals snapping at different phases** → prepend snapping forced on for
   `ItemsPresenter` (`HWCompNodeWinRT.cpp:1244-1266`, "Child appears to jiggle up and down").
6. **Spurious DManip `Ready` between two active statuses** → one-tick delay workaround
   (`InputServices.cpp:7136-7174`, "Workaround for bug 689141").
7. **Losing the composition peer while inerting** → inertia forcibly stopped and snapped
   (`InputServices.cpp:7373-7402`).
8. **`ViewChanging`/`ViewChanged` storms** → `DelayViewChanging`/`FlushViewChanging` bracketing every delta
   (`ScrollViewer_Partial.cpp:13478-13480`, `:13836-13838`) plus
   `Enter/LeaveIntermediateViewChangedMode` for thumb drags.
9. **Programmatic view change fighting user input** → `skipDuringTouchContact` (contact-count check at
   `InputServices.cpp:12633-12645`), `skipAnimationWhileRunning` (`:12613-12626`), and
   `StopInertialViewport(restrictToKnownInertiaEnd=true)` for MakeVisible inertia (`:6781-6798`).
10. **`IManipulationDataProvider` (logical scrolling)** is a hard smoothness cliff: animations disabled
    (`ScrollViewer_Partial.cpp:3630-3643`), `BringIntoViewport` disallowed (`:3638-3641`), tolerance widened
    from `0.05f` to `1.0f` (`ScrollViewerScrollRoundingToleranceForProvider`).

---

## 11. Applicability to Uno Skia (grounded in the working repo)

Uno's `ScrollViewer`/`ScrollContentPresenter` mirror the *legacy* control's **API and its `IScrollInfo`
fallback path**, which is precisely the branch WinUI does **not** use for smooth scrolling.

`D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.mux.cs:18-27`
ports the constants faithfully:

```csharp
internal const int ScrollViewerDefaultMouseWheelDelta = 120;
private static double GetVerticalScrollWheelDelta(Size size, double delta)
    => Math.Min(Math.Floor(size.Height), Math.Round(delta * Math.Max(48.0, Math.Round(size.Height * 0.15, 0)) / ScrollViewerDefaultMouseWheelDelta, 0));
```

and `…/ScrollContentPresenter.cs:245-352` (`PointerWheelScroll`) is a direct port of the **fallback** path —
with Uno-specific additions (trackpad detection on iOS/macOS at `:310-334`, `ClearOffsetIntents()` at `:251`,
and a `Set(..., disableAnimation: false)` composition animation on other platforms).
`…/ScrollContentPresenter.Managed.cs:175` even says so:

```csharp
// Note: the way WinUI does scrolling is very different, and doesn't use PointerWheelChanged changes, etc.
```

Concretely transferable ideas, in descending value:

1. **Independent-dirty flag.** Uno has no equivalent of `DirtyFlags::Independent`. A scroll frame in Uno that
   only changes a transform should mark bounds dirty and skip the render walk / repaint of unchanged content.
2. **Compositor-side scroll transform driven by an expression**, so the visual offset advances without a
   managed layout/arrange pass. Uno already animates offsets on the composition side in some paths
   (`Set(..., disableAnimation: false)`), but the *authoritative* offset is still managed state.
3. **Pin arrange to the pre-manipulation offset during an active scroll** (`ArrangeOverride`'s
   `IsInManipulation()` branch), so the arrange pass exists only for virtualization/extents, never to move
   pixels.
4. **Batch `ViewChanging`/`ViewChanged` to at most one pair per frame** (`DelayViewChanging`/`Flush…`).
5. **Pixel-snap while finger-tracking, un-snap during inertia/animation.** Cheap, high perceptual payoff, and
   directly ported from `CUIElement::ShouldDisablePixelSnapping`.
6. **Feed a frame-latency estimate into the physics.** WinUI hard-codes 16 ms; Uno can do better with the
   actual Skia present interval.
7. **Never let a programmatic/anchoring correction run against live user input** — WinUI's
   `skipDuringTouchContact` + contact count is the exact guard. (Matches the recorded "no user fighting"
   requirement.)
8. **Do not copy the `15% / 48px / 120` wheel formula as the primary wheel feel** — in WinUI it is the
   *fallback* branch. If Uno wants WinUI-like wheel feel it needs an inertia/animation model, not a per-notch
   jump; the modern `ScrollPresenter` numbers (§8.9) are the better parity target
   (`clamp(50ms, 5ms × px, 1000ms)`).

---

## 12. Explicit UNVERIFIED list

* **`ScrollViewerScrollingAndZoomingConstantVelocity`** — no such identifier exists anywhere in
  `D:/Work/microsoft-ui-xaml2`. UNVERIFIED / likely a misremembered name.
* **`SCROLL_VIEWER_DEFAULT_*`** — no such macro prefix exists in this tree. UNVERIFIED.
* **DManip's inertia curve, deceleration constant, wheel lines-per-detent, and `ZoomToRect` animation
  duration/easing** — not present in any XAML source; owned by `directmanipulation.dll` / OS settings.
  UNVERIFIED from this tree by construction.
* **Consumer of `offsetX`/`offsetY`/`zoomFactor` computed in `ScrollContentPresenter::ArrangeOverride`
  (`ScrollContentPresenter_Partial.cpp:2258-2270`)** — computed but no read found within the method in this
  snapshot. The `IsInManipulation()` branch and its intent are verified; the application point is not.
* **`s_maxOverpanDistance` / `s_scaleOverpanValue` / `s_minOverpanDistance`** — defined
  (`DirectManipulationService.cpp:35-37`) but with no call sites found. Presumed dead.
* **Compositor-thread ownership of `CCompositorDirectManipulationViewport`** — the class documents itself as
  "handed off to the compositor thread" (`CompositorDirectManipulationViewport.h:5-8`) and
  `CCoreServices::GetDirectManipulationChanges` (`xcpcore.cpp:1413`) is called from `NWDrawTree`
  (`xcpcore.cpp:6346-6349`) with the list released at `:6927`. I did **not** trace where `UpdateTransform()` is
  invoked in the current (WinAppSDK, WUC-based) compositor; in this snapshot no caller of
  `CCompositorDirectManipulationViewport::UpdateTransform()` was found outside its own definition. The
  16 ms latency hint may therefore be vestigial in this build. UNVERIFIED.
