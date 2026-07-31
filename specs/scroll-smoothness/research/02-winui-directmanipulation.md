# WinUI legacy `ScrollViewer` + DirectManipulation (DManip) integration — firsthand source audit

**Scope**: the DManip integration layer in `dxaml` (legacy XAML core), i.e. what makes the *classic*
`Microsoft.UI.Xaml.Controls.ScrollViewer` scroll smoothly. This is NOT `ScrollPresenter`/`InteractionTracker`
(that is `controls/dev/ScrollPresenter`, a different mechanism).

**Sources read** (all paths absolute, all line numbers verified against the working tree at
`D:/Work/microsoft-ui-xaml2`):

| File | Role |
|---|---|
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.cpp` (5281 lines) | PAL implementation, owns the DM manager/compositor/update-manager |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/plat/win/browserdesktop/DirectManipulationService.h` (715 lines) | class decl, all constants |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/plat/win/browserdesktop/DirectManipulationViewportEventHandler.cpp/.h` | DM → XAML UI-thread callbacks |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/plat/win/browserdesktop/DirectManipulationFrameInfoProvider.cpp/.h` | `IDirectManipulationFrameInfoProvider` |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/compositor/CompositorDirectManipulationViewport.cpp/.h` | legacy compositor-thread viewport wrapper |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/input/DirectManipulationServiceSharedState.cpp` + `core/inc/DirectManipulationServiceSharedState.h` | process/thread-shared `IDirectManipulationCompositor` |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/pal/inc/PalDirectManipulationService.h`, `PalDirectManipulationCompositorService.h` | PAL contracts |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/inc/DirectManipulationContainerHandler.h`, `inc/XcpDirectManipulationViewportEventHandler.h` | core↔framework contracts |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/dxaml/lib/DirectManipulationTypes.h`, `pal/inc/paltypes.h` | enums |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/input/InputServices.cpp` (~13800 lines) | the input manager / per-tick DM driver |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/hw/DManipData.cpp`, `core/hw/ManipulationTransform.cpp` | shared-transform → Composition expression plumbing |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/components/comptree/HWCompNodeWinRT.cpp` | comp-node consumption of the shared transform |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/dxaml/lib/ScrollViewer_Partial.cpp/.h`, `ScrollContentPresenter_Partial.cpp` | framework side |
| `D:/Work/microsoft-ui-xaml2/dxaml/xcp/core/dll/xcpcore.cpp` | frame loop (`NWDrawMainTree` / `NWDrawTree`) |

**Not in this repo** (so implementation details are `UNVERIFIED` here):
`DirectManipulationHelper.h/.cpp` — `find . -iname "DirectManipulationHelper*"` under
`D:/Work/microsoft-ui-xaml2` returns **nothing**; only `#include "DirectManipulationHelper.h"` references exist
(`DirectManipulationService.cpp:14`, `core/input/DirectManipulationServiceSharedState.cpp:8`). It wraps
`Microsoft.DirectManipulation.dll` (lifted DManip). Likewise `Microsoft.DirectManipulation.h` and the DManip
engine itself (deceleration model, inertia integrator, chaining arbitration) are **binary/out-of-repo**.

---

## 0. TL;DR — the five things that actually produce smoothness

1. **The manipulation is not computed on the XAML UI thread at all.** XAML hands touch contacts to DManip
   (`SetContact`) and puts the viewport in `DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC`
   (`DirectManipulationService.cpp:4307`, `:4310`), so DManip consumes the raw pointer stream on its own
   delegate thread and produces the transform there.
2. **The transform reaches the screen without ever passing through the UI thread.** DManip creates a
   *shared* DComp transform object per content; XAML wraps it in a `CompositionPropertySet` and drives the
   comp-node's `TransformMatrix` with a **Composition ExpressionAnimation**
   (`core/hw/DManipData.cpp:152-182`, `core/hw/ManipulationTransform.cpp:83-106`,
   `components/comptree/HWCompNodeWinRT.cpp:2340-2384`). One-time setup; after that the compositor evaluates
   `transform * manipTransform.Matrix` every compositor frame with zero XAML involvement.
3. **UI-thread deltas are decoupled and dirty the tree as `DirtyFlags::Independent`**, which explicitly does
   *not* schedule a render walk (`core/input/InputServices.cpp:8724-8730`,
   `components/elements/UIElementRenderWalk.cpp:118-127, 236-260`).
4. **Layout is frozen during a manipulation.** `ScrollContentPresenter::ArrangeOverride` uses the
   *pre-manipulation* offsets while `ScrollViewer::IsInManipulation()`
   (`dxaml/lib/ScrollContentPresenter_Partial.cpp:2255-2261`), so re-arranges during a scroll are no-ops
   visually — content motion is 100% the DManip transform.
5. **Mouse wheel is a "pure inertia" DManip manipulation** — the `WM_POINTERWHEEL` message is forwarded to
   DManip which animates the detent as an inertia curve on the compositor
   (`dxaml/lib/ScrollViewer_Partial.cpp:2803`, `:9756-9803`; `DirectManipulationService.cpp:583-592`).

---

## 1. Which thread runs the manipulation? How does the transform reach the compositor?

### 1.1 The DManip objects and where they live

`CDirectManipulationService` (one instance per `IDirectManipulationContainer`, i.e. effectively per
`ScrollViewer`) owns four DManip COM objects
(`DirectManipulationService.h:562-576`):

```cpp
// DM manager owned by this service
IDirectManipulationManager3* m_pDMManager;
// Update manager used to retrieve latest content transforms
IDirectManipulationUpdateManager* m_pDMUpdateManager;
// DM frame information implementation used to provide timing information
IDirectManipulationFrameInfoProvider* m_pDMFrameInfoProvider;
// DM Compositor, responsible for managing DManip's DComp resources and sending updates to DComp
IDirectManipulationCompositor* m_pDMCompositor;
```

Creation, `DirectManipulationService.cpp:180-206`:

```cpp
if (!m_pDMManager)
{
    wrl::ComPtr<IDirectManipulationManager3> spDMManager = CDirectManipulationService::CreateDirectManipulationManager();
    m_pDMManager = spDMManager.Detach();
    m_islandInputSite = pIslandInputSite;

    IFC_RETURN(m_sharedState->GetSharedDCompManipulationCompositor(&m_pDMCompositor));
    ASSERT(m_pDMCompositor);

    if (!fIsForCrossSlideViewports)
    {
        IFC_RETURN(EnsureFrameInfoProvider());          // -> m_pDMFrameInfoProvider
        ctl::ComPtr<IDirectManipulationUpdateManager> spDMUpdateManager;
        IFC_RETURN(m_pDMManager->GetUpdateManager(IID_PPV_ARGS(&spDMUpdateManager)));
        m_pDMUpdateManager = spDMUpdateManager.Detach();
        IFC_RETURN(m_pDMCompositor->SetUpdateManager(m_pDMUpdateManager));   // <-- the key wiring
    }
    m_pDMHelper.Initialize(m_pDMCompositor, m_pDMManager);
}
```

`m_pDMManager` is created from `Microsoft.DirectManipulation.dll` (lifted DManip), loaded lazily:
`DirectManipulationService.cpp:5260-5275`.

`IDirectManipulationCompositor::SetUpdateManager(m_pDMUpdateManager)` (line 202) is the plumbing that lets
the **DComp compositor**, not XAML, tick DManip. Every DManip content added to that compositor gets a
transform that DComp updates itself.

### 1.2 The DManip compositor is *shared per UI thread*

`DirectManipulationServiceSharedState` (`core/input/DirectManipulationServiceSharedState.cpp:20-35`) creates
exactly **one** `IDirectManipulationCompositor` per UI thread and ref-counts it across all
`CDirectManipulationService` instances:

```cpp
HRESULT DirectManipulationServiceSharedState::GetSharedDCompManipulationCompositor(IDirectManipulationCompositor **ppResult)
{
    if (!m_compositor)
    {
        HMODULE hmodDManip = LoadLibraryExWAbs(L"Microsoft.DirectManipulation.dll", nullptr, LOAD_WITH_ALTERED_SEARCH_PATH);
        wrl::ComPtr<IClassFactory> directManipulationFactory;
        IFC_RETURN(DirectManipulationHelper::GetDCompManipulationCompositorFactory(hmodDManip, &directManipulationFactory));
        IFC_RETURN(directManipulationFactory->CreateInstance(nullptr, IID_PPV_ARGS(m_compositor.ReleaseAndGetAddressOf())))
    }
    m_compositorUseCount++;
    m_compositor.CopyTo(ppResult);
    return S_OK;
}
```

Header comment (`core/inc/DirectManipulationServiceSharedState.h:8`):
> `// Represents the state shared between CDirectManipulationService instances within the same UI thread`

Practical effect: N `ScrollViewer`s scrolling at once do **not** each spin up compositor/update-manager state.

### 1.3 Input mode: DManip consumes the pointer stream itself ("delegate thread")

`CDirectManipulationService::CreateViewport`, `DirectManipulationService.cpp:4297-4320`:

```cpp
HWND inputHwnd = CInputServices::GetUnderlyingInputHwndFromIslandInputSite(m_islandInputSite.Get());
IFC(static_cast<IDirectManipulationManager*>(m_pDMManager)->CreateViewport(m_pDMFrameInfoProvider, inputHwnd, IID_PPV_ARGS(dmViewport.ReleaseAndGetAddressOf())));

// Use the delegate thread mechanism
IFC(dmViewport->SetInputMode(DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC));

// Viewports that use the compositor must be in automatic input mode.
IFC(dmViewport->SetUpdateMode(DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC));

// Disable DManip pixel snapping as we will perform all pixel snapping ourselves.
IFC(dmViewport->SetViewportOptions(DIRECTMANIPULATION_VIEWPORT_OPTIONS_DISABLEPIXELSNAPPING));

IFC(CreateViewportEventHandler());
IFC(dmViewport->AddEventHandler(inputHwnd, m_pUIThreadViewportEventHandler, &viewportEventHandlerCookie));
```

Three smoothness-critical facts here:

* `DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC` for **input**: once `SetContact` is called for a pointer id,
  DManip's own thread ("delegate thread") receives the raw pointer packets. XAML's UI thread never touches a
  touch-move during a pan.
* `DIRECTMANIPULATION_INPUT_MODE_AUTOMATIC` for **update**: DManip advances the manipulation itself. The
  comment on line 4309 (`// Viewports that use the compositor must be in automatic input mode.`) confirms
  the compositor-driven path requires it.
* `DISABLEPIXELSNAPPING` — XAML does its own snapping, avoiding double-quantization jitter.

Contacts are declared to DManip via `SetContact`, using a `PointerPoint` (not just a pointer id) on the
modern path (`DirectManipulationService.cpp:656-661`):

```cpp
{
    // http://osgvsowi/14575768 - Suspend until we have a touch driver reset solution
    SuspendFailFastOnStowedException suspender;
    auto pointerPoint = GetPointerPointFromPointerId(pointerId);
    IFC(m_pDMHelper->SetContact(pDMViewport, pointerPoint.Get()));
}
```

`GetPointerPointFromPointerId` uses `Microsoft.UI.Input.PointerPoint.GetCurrentPoint(pointerId)`
(`DirectManipulationService.cpp:5247-5258`).

### 1.4 The transform path to the screen: DManip *shared transform* → DComp expression

This is the single most important mechanism, and it has **no UI-thread round trip**.

**Step A** — `CDirectManipulationService::EnsureSharedContentTransform`
(`DirectManipulationService.cpp:3045-3117`) asks DManip's DComp compositor to create a shared transform for a
DManip content, via `CreateSharedContentTransformForContent`
(`DirectManipulationService.cpp:3236-3266`):

```cpp
GetDirectManipulationHelper()->CreateSharedContentTransformForContent(compositor, spDMContent, spSharedTransform.ReleaseAndGetAddressOf());
```

Two variants (`:3074-3088`):
* **Case 2 (normal)** — one *primary* shared transform.
* **Case 1 (overpan mode `None`)** — a *primary* + *secondary* pair (the "overpan reflex" pair). See §6.

Release is symmetric via `m_pDMCompositor->RemoveContent(spDMContent)` (`:3291`).

**Step B** — the comp node consumes it. `HWCompTreeNodeWinRT::…` in
`components/comptree/HWCompNodeWinRT.cpp:2340-2384`:

```cpp
if (hasIndependentTransformManipulation)
{
    IPALDirectManipulationService* dmService = m_spDManipData->GetDMService();
    IObject* manipulationContent = m_spDManipData->GetManipulationContent();
    ...
    xref_ptr<IUnknown> sharedDManipPrimaryTransform;
    xref_ptr<IUnknown> sharedDManipSecondaryTransform;
    IFCFAILFAST(dmService->EnsureSharedContentTransform(
        dcompTreeHost->GetCompositor(), manipulationContent, contentType,
        sharedDManipPrimaryTransform.ReleaseAndGetAddressOf(),
        sharedDManipSecondaryTransform.ReleaseAndGetAddressOf()));

    IFCFAILFAST(m_spDManipData->SetSharedContentTransforms(sharedDManipPrimaryTransform, sharedDManipSecondaryTransform, dcompTreeHost->GetCompositor()));
    IFCFAILFAST(dmanipData->EnsureOverallContentPropertySet(dcompTreeHost->GetCompositor()));
}
dmanipPS = dmanipData->GetOverallContentPropertySet();
```

`dmanipPS` is then handed to `GetLocalTransformHelper(...)` as `pDManipTransform`
(`HWCompNodeWinRT.cpp:2400-2415`) and folded into a `WinRTLocalExpressionBuilder` expression
(`builder.EnsureLocalExpression()`, line 2417). Note the guard at line 2331-2337:

```cpp
bool requiresTransformExpression =
    hasIndependentTransformManipulation     // DManip-driven animation
    || hasIndependentTransformAnimation
    || !redirectionIsTranslationOnly;
```

i.e. presence of a DManip manipulation is exactly what promotes the visual to the *expression* path instead
of a static `TransformMatrix`.

**Step C** — the expression. `DManipDataWinRT::EnsureOverallContentPropertySet`
(`core/hw/DManipData.cpp:152-182`):

```cpp
IFC_RETURN(pCompositor->CreatePropertySet(m_spOverallContentPropertySet.ReleaseAndGetAddressOf()));
IFC_RETURN(m_spOverallContentPropertySet->InsertMatrix4x4(HStringReference(L"Matrix").Get(), { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 }));

// Xaml-computed offset (constant in expression)
wfn::Matrix4x4 prependTransform = { 1,0,0,0, 0,1,0,0, 0,0,1,0, m_contentOffsetX, m_contentOffsetY, 0, 1 };

if (m_spSharedSecondaryContentTransformCO)
{
    // "targetPS.Matrix = ContentOffsetTransform * DManipSecondaryTransform.Matrix * DManipPrimaryTransform.Matrix"
    IFC_RETURN(::ConnectComplexAnimationWithPrependTransform(spOverallContentPropertySetCO.get(), m_spSharedPrimaryContentTransformCO.get(), m_spSharedSecondaryContentTransformCO.get(), prependTransform, L"Matrix"));
}
else
{
    // "targetPS.Matrix = ContentOffsetTransform * DManipTransform.Matrix"
    IFC_RETURN(::ConnectAnimationWithPrependTransform(spOverallContentPropertySetCO.get(), m_spSharedPrimaryContentTransformCO.get(), prependTransform, L"Matrix"));
}
```

And the expression construction itself (`core/hw/ManipulationTransform.cpp:83-106`):

```cpp
_Check_return_ HRESULT ConnectAnimationWithPrependTransform(...)
{
    const wchar_t *sourceKey = L"manipTransform";
    const wchar_t *transformKey = L"transform";
    std::wstring expression;                       // "transform*manipTransform.Matrix"
    expression.append(transformKey); expression.append(L"*");
    expression.append(sourceKey);    expression.append(L"."); expression.append(propertyName);

    IFC_RETURN(CreateExpressionAnimation(manipulationPropertySetCO, expression.c_str(), &compositionAnimation));
    IFC_RETURN(compositionAnimation->SetReferenceParameter(HStringReference(sourceKey).Get(), sourceCO));
    IFC_RETURN(compositionAnimation->SetMatrix4x4Parameter(HStringReference(transformKey).Get(), prependTransform));
    IFC_RETURN(manipulationPropertySetCO->StartAnimation(HStringReference(propertyName).Get(), compositionAnimation.Get()));
}
```

The complex form is `"transform*manipTransformSecondary.Matrix*manipTransformPrimary.Matrix"`
(`ManipulationTransform.cpp:109-138`).

**Consequence**: after this one-time setup, the per-frame data flow is
`DManip delegate thread → DComp shared transform → ExpressionAnimation → Visual.TransformMatrix`.
The XAML UI thread is not in the loop. If the UI thread stalls for 500 ms, the finger still drags content at
compositor frame rate.

### 1.5 The *legacy* compositor-thread polling path (still in the tree, effectively dead)

`IPALDirectManipulationCompositorService`
(`pal/inc/PalDirectManipulationCompositorService.h:15-30`) exposes:

```cpp
virtual HRESULT GetCompositorContentTransform(IObject* pCompositorContent, XDMContentType contentType, bool& fIsInertial,
                                              XFLOAT& translationX, XFLOAT& translationY,
                                              XFLOAT& uncompressedZoomFactor, XFLOAT& zoomFactorX, XFLOAT& zoomFactorY) = 0;
// Even if no compositor node exists for a DM content, DM needs to be ticked in inertia mode.
virtual HRESULT UpdateCompositorContentTransform(IObject* pCompositorContent, XUINT32 deltaCompositionTime) = 0;
virtual HRESULT GetCompositorViewportKey(IObject* pCompositorViewport, XHANDLE* pKey) = 0;
virtual HRESULT GetCompositorViewportStatus(IObject* pCompositorViewport, XDMViewportStatus* pStatus) = 0;
```

Consumed by `CCompositorDirectManipulationViewport`
(`core/compositor/CompositorDirectManipulationViewport.h:5-8`):
> `// Each DirectManipulation viewport is associated with a CCompositorDirectManipulationViewport instance by CCoreServices and handed off to the compositor thread for handling DM notifications.`

Its per-frame tick (`core/compositor/CompositorDirectManipulationViewport.cpp:50-65`):

```cpp
void CCompositorDirectManipulationViewport::UpdateTransform()
{
    // TODO - Jupiter (Windows) bug 847117. Replace 16 with the actually milliseconds until the transform is shown on screen
    IGNOREHR(pCompositorService->UpdateCompositorContentTransform(pCompositorContent, 16 /*deltaCompositionTime*/));
}
```

**Note the hardcoded 16 ms** — a fixed one-frame-at-60 Hz latency estimate, never replaced.

`CDirectManipulationService::UpdateCompositorContentTransform`
(`DirectManipulationService.cpp:3834-3879`) stashes that value and ticks DManip:

```cpp
// This time lapse will be used in the CDirectManipulationCompositor::GetNextFrameInfo call caused by this Update call.
m_deltaCompositionTime = deltaCompositionTime;
IFC(m_pDMUpdateManager->Update(m_pDMFrameInfoProvider));
```

**Status of this path today**: `CCoreServices::GetDirectManipulationChanges`
(`core/dll/xcpcore.cpp:1412-1462`) still builds the `xvector<CCompositorDirectManipulationViewport*>` in
`NWDrawTree` (`core/dll/xcpcore.cpp:6275-6350`), but a grep for `directManipulationViewportsChanged` in that
file returns only the declaration (`:6276`) and the out-param at the call site (`:6349`) —
**the result is never consumed**. Also `DirectManipulationService.cpp:3586`:

```cpp
//TODO: Remove the fIsInertial out parameter once DManip-on-DComp is always turned on. It won't be consumed anymore.
```

and `DirectManipulationService.h:706`:
```cpp
// DManipOnDComp_Staging:  This lock can be removed when DManip-on-DComp is finished
```

Conclusion: in the current lifted build, per-frame transform delivery is entirely DComp-side; the
compositor-thread poll is legacy scaffolding. `GetCompositorContentTransform` survives only as a
**UI-thread read** helper, called from `CInputServices::GetDirectManipulationCompositorTransform`
(`core/input/InputServices.cpp:13181-13199`).

---

## 2. What is the frame-info provider for? vsync/compositor-clock alignment

`CDirectManipulationFrameInfoProvider` implements `IDirectManipulationFrameInfoProvider`
(`plat/win/browserdesktop/DirectManipulationFrameInfoProvider.h:10-38`). Header abstract
(`DirectManipulationFrameInfoProvider.cpp:4-6`):

> `// CDirectManipulationFrameInfoProvider class used for listening to DirectManipulation feedback on the compositor thread.`

Its one method (`DirectManipulationFrameInfoProvider.cpp:48-71`):

```cpp
IFACEMETHODIMP CDirectManipulationFrameInfoProvider::GetNextFrameInfo(
    _Out_ XUINT64* pTime, _Out_ XUINT64* pProcessTime, _Out_ XUINT64* pCompositionTime)
{
    *pTime = 0;
    *pProcessTime = 0;
    *pCompositionTime = m_pDMService->GetDeltaCompositionTime();
}
```

`GetDeltaCompositionTime()` returns `m_deltaCompositionTime`
(`DirectManipulationService.h:283-286`, field at `:614`), documented as:

> `// Lapse of time in milliseconds between the time the compositor calls UpdateCompositorContentTransform and the time the resulting transform is shown on screen`

**How XAML actually supplies it today**: `EnsureFrameInfoProvider`
(`DirectManipulationService.cpp:4215-4231`) does **not** instantiate `CDirectManipulationFrameInfoProvider`
at all any more — it QIs the DComp DManip compositor:

```cpp
if (!m_pDMFrameInfoProvider)
{
    ASSERT(m_pDMCompositor);
    IFC(m_pDMCompositor->QueryInterface(IID_PPV_ARGS(&m_pDMFrameInfoProvider)));
}
```

(The `CComObject<CDirectManipulationFrameInfoProvider>* pFrameInfoProvider` local at line 4220 is allocated
nowhere and only `ReleaseInterface`'d at 4229 — dead code.)

So: **the frame-info provider handed to `CreateViewport` (line 4298) is the DComp compositor itself.** That is
the vsync alignment: DManip asks DComp "when will the next frame be presented?", DComp answers with its own
composition clock, and DManip evaluates the manipulation/inertia curve *at the presentation timestamp*, not
at the time it happened to be ticked. This is the classic anti-jitter measure — sample the animation at
`t_present`, not `t_now`.

Cross-slide viewports pass `NULL` for the frame-info provider
(`CreateCrossSlideViewport`, `DirectManipulationService.cpp:4358`) — they never animate.

**UNVERIFIED**: the exact units/semantics of `pTime`/`pProcessTime` (XAML returns 0 for both) and how DManip
consumes `pCompositionTime` are inside `Microsoft.DirectManipulation.dll`.

---

## 3. Inertia parameters and the deceleration model

### 3.1 XAML sets **no** `IDirectManipulationInertiaBehavior`

A repo-wide grep confirms this:

```
cd D:/Work/microsoft-ui-xaml2 && grep -rn "IDirectManipulationInertiaBehavior\|InertiaBehavior" --include=*.cpp --include=*.h .
→ (no hits)
```

The only `DesiredDeceleration` / `DesiredDisplacement` hits in the tree are the **XAML `ManipulationDelta`
public API** metadata (`components/metadata/Indexes.g.h:1296-1301`,
`components/metadata/StaticMetadata.g.cpp:24591-24633`), which belong to
`Microsoft.UI.Xaml.Input.Inertia*Behavior` — the *routed manipulation events* API, not DManip.

Behaviors XAML *does* create on DManip viewports:
* `CLSID_Microsoft_AutoScrollBehavior` (`DirectManipulationService.cpp:3455`) — constant-velocity autoscroll.
* `CLSID_Microsoft_ParametricMotionBehavior` (`DirectManipulationService.cpp:5156`) — overpan reflex content.
* `IDirectManipulationDragDropBehavior` (`AttachDragDropBehavior`, `:3519-3546`).

### 3.2 What XAML *does* control: the configuration flags

Inertia is enabled/disabled purely by configuration bits.
`XDMConfigurations` → `DIRECTMANIPULATION_CONFIGURATION` mapping
(`DirectManipulationService.cpp:4614-4645`):

```cpp
if ((configuration & XcpDMConfigurationPanInertia)  != 0) dmConfig |= DIRECTMANIPULATION_CONFIGURATION_TRANSLATION_INERTIA;
if ((configuration & XcpDMConfigurationZoomInertia) != 0) dmConfig |= DIRECTMANIPULATION_CONFIGURATION_SCALING_INERTIA;
```

Enum values (`dxaml/lib/DirectManipulationTypes.h:33-44`, mirrored in `pal/inc/paltypes.h:441+`):

```cpp
DMConfigurationNone        = 0x00,
DMConfigurationInteraction = 0x01,
DMConfigurationPanX        = 0x02,
DMConfigurationPanY        = 0x04,
DMConfigurationZoom        = 0x10,
DMConfigurationPanInertia  = 0x20,
DMConfigurationZoomInertia = 0x80,
DMConfigurationRailsX      = 0x100,
DMConfigurationRailsY      = 0x200
```

`ScrollViewer::GetNonTouchManipulationConfiguration`
(`dxaml/lib/ScrollViewer_Partial.cpp:8422-8472`) builds the **keyboard/mouse-wheel** config:

```cpp
if (horizontalScrollMode != ScrollMode_Disabled) nonTouchConfiguration += DMConfigurationPanX;
if (verticalScrollMode   != ScrollMode_Disabled) nonTouchConfiguration += DMConfigurationPanY;
if (isScrollInertiaEnabled && nonTouchConfiguration != DMConfigurationNone) nonTouchConfiguration += DMConfigurationPanInertia;
if (zoomMode != ZoomMode_Disabled) { nonTouchConfiguration += DMConfigurationZoom;
                                     if (isZoomInertiaEnabled) nonTouchConfiguration += DMConfigurationZoomInertia; }
```

i.e. `ScrollViewer.IsScrollInertiaEnabled` / `IsZoomInertiaEnabled` are the *only* app-facing knob, and they
are boolean.

The bring-into-viewport configuration is a fixed constant (`ScrollViewer_Partial.cpp:7927`):

```cpp
DMConfigurations bringIntoViewportConfiguration = (DMConfigurations)(DMConfigurationPanX + DMConfigurationPanY + DMConfigurationZoom + DMConfigurationPanInertia + DMConfigurationZoomInertia);
```

There is also a rail/inertia interaction: touch config drops `DMConfigurationPanInertia` in one case
(`ScrollViewer_Partial.cpp:8248-8251`).

### 3.3 Deceleration model

**UNVERIFIED / not in this repo.** The integrator lives in `Microsoft.DirectManipulation.dll`. What *is*
observable from XAML source:

* XAML can read the *predicted end* of the inertia animation at any time:
  `GetContentInertiaEndTransform` (`DirectManipulationService.cpp:1812-1880`) calls
  `IDirectManipulationPrimaryContent::GetInertiaEndTransform` — so DManip's inertia is a **closed-form,
  pre-solved curve with a known terminus**, not a step-integrated spring. That is the strongest evidence in
  this tree about the model: it is analytically solvable at t=0.
* Guard for a known DManip race (`:1851-1858`):
  ```cpp
  hr = pDMContent->GetInertiaEndTransform(reinterpret_cast<XFLOAT*>(&matrix), 6);
  if (hr == E_FAIL)
  {
      // Because of an unavoidable race condition given DManip's APIs, DManip occasionally returns E_FAIL
      // even though the retrieved status is DIRECTMANIPULATION_INERTIA. Details in Win Blue bug 38233.
      hr = S_FALSE;
  }
  ```
* XAML consumes it as `ScrollViewerViewChangingEventArgs.FinalView` (`ScrollViewer_Partial.cpp:13756-13790`,
  `m_inertiaEndHorizontalOffset` / `m_inertiaEndVerticalOffset` / `m_inertiaEndZoomFactor`), and the
  virtualizing panel uses it for predictive realization.

### 3.4 The only numeric constants XAML sets — the **overpan** curves

`DirectManipulationService.cpp:35-42` (declared `DirectManipulationService.h:672-700`):

```cpp
const float CDirectManipulationService::s_maxOverpanDistance             = 200.0f;
const float CDirectManipulationService::s_scaleOverpanValue              = 0.91f;
const float CDirectManipulationService::s_minOverpanDistance             = 1.0f;
const float CDirectManipulationService::s_centerPointScaleFactor         = 1.94f;
const float CDirectManipulationService::s_curveSuppressionValueForZoom   = 1.0f;
const float CDirectManipulationService::s_curveSuppressionValueForTranslate = 0.0f;
const float CDirectManipulationService::s_linearCurvePassThroughSlope    = 1.0f;
float       CDirectManipulationService::s_range[]                        = { 0, FLT_MAX };
```

with the documenting comments at `DirectManipulationService.h:672-700`:

> `// Defines the range of interpolation of scale as a function of translation:`
> `//  As translation varies from 0px to g_fMaxOverpanDistance, the scale varies from 1 to g_fScaleOverpanValue.`
> `//  Larger values of g_fMaxOverpanDistance would result in higher-resistance scale overpan curves.`
> …
> `// This scale factor is multiplied by the device height to determine the center-point offset.`
> `// Larger values of center-point offset result in lighter resistance in the scale curve`
> …
> `// Define the valid offset range for the right/bottom curves. Clamping them at 0 prevents negative values and prevents these curves from overlapping with the left/top curves (curve overlap can cause discrete jumps e.g. when zooming in!).`

`s_centerPointScaleFactor` is applied against the *physical display height* divided by rasterization scale
(`RefreshOverpanCurves`, `DirectManipulationService.cpp:5080-5087`):

```cpp
dm.dmSize = sizeof(dm);
if (::EnumDisplaySettings(nullptr, ENUM_CURRENT_SETTINGS, &dm))
{
    XFLOAT physicalDeviceHeight = static_cast<XFLOAT>(dm.dmPelsHeight);
    XFLOAT logicalDeviceHeight  = physicalDeviceHeight / pReflexes->m_zoomScale;
    centerpointOffset = logicalDeviceHeight * s_centerPointScaleFactor;
}
ASSERT(centerpointOffset > 0.0f);
```

**Important**: these constants are only used when `XDMOverpanMode != Default` (i.e. overpan *suppression*,
`DMOverpanModeNone = 0x04`, `DirectManipulationTypes.h:78-82`). The default bounce is DManip's own and is
**not parameterized by XAML** (UNVERIFIED beyond that).

---

## 4. How the UI thread learns about the offsets — cadence, and why there is no layout pass per delta

### 4.1 The two notification channels

**(a) Status changes — push, async, off-tick.**
`CDirectManipulationViewportEventHandler` (`ATL::CComObjectRootEx<ATL::CComMultiThreadModel>`,
`DirectManipulationViewportEventHandler.h:17-20`) implements `IDirectManipulationViewportEventHandler`,
`IDirectManipulationInteractionEventHandler`, `IDirectManipulationDragDropEventHandler`. Registered per
viewport with the input HWND (`DirectManipulationService.cpp:4319`), so callbacks arrive **on the UI thread**
via the HWND's message pump.

Only `OnViewportStatusChanged` and `OnInteraction` are consumed
(`DirectManipulationViewportEventHandler.cpp:95-119`, `:165-188`). The per-content and per-viewport
*transform* notifications are **explicitly declined**:

```cpp
IFACEMETHODIMP CDirectManipulationViewportEventHandler::OnViewportUpdated(IDirectManipulationViewport* pDMViewport)
{ RRETURN(S_FALSE); }                                                    // line 130-135

IFACEMETHODIMP CDirectManipulationViewportEventHandler::OnContentUpdated(IDirectManipulationViewport* pDMViewport, IDirectManipulationContent* pDMContent)
{ RRETURN(S_FALSE); }                                                    // line 146-152
```

**This is the crux**: XAML does *not* take a per-delta callback. It would be a UI-thread interrupt per
manipulation frame. Instead:

* status changes are *queued* on `CDMViewport` (`CInputServices::ProcessDirectManipulationViewportStatusUpdate`,
  `core/input/InputServices.cpp:7415`, whose header comment at `:7409-7411` reads
  `// Called as soon as the status of the provided viewport changed. This method is called outside of the tick, but the processing of that change will happen at the next UI tick.`)
* the offsets are **polled once per UI tick**.

**(b) Offsets — pull, once per UI tick.**

`CCoreServices::NWDrawMainTree` (`core/dll/xcpcore.cpp:6212-6230`):

```cpp
// Update input manager state.
if (m_inputServices) { IFC(m_inputServices->ProcessUIThreadTick()); }

IFC(NWDrawTree(GetHWWalk(), pIRenderTarget, m_pMainVisualTree, forceRedraw, pFrameDrawn));

if (m_inputServices) { m_inputServices->OnPostUIThreadTick(); }
```

`CInputServices::ProcessUIThreadTick` (`core/input/InputServices.cpp:9098-9122`):

```cpp
IFC_RETURN(gps->IsDirectManipulationSupported(isDirectManipulationSupported));
if (isDirectManipulationSupported)
{
    IFC_RETURN(InitializeDirectManipulationContainers());
    IFC_RETURN(ProcessDirectManipulationViewportChanges());
    IFC_RETURN(RefreshDirectManipulationHandlerWantsNotifications());
}
```

`ProcessDirectManipulationViewportChanges()` (`:7092-7110`, header comment `:7086-7088`
`// Called at each UI tick to handle any potential viewport status or transform updates.`) iterates all
viewports and, per viewport (`:7123-7345`), drains the queued status list and calls
`ProcessDirectManipulationViewportValuesUpdate` when the viewport is `Running`/`Inertia`/`Suspended`/
`AutoRunning` (`:7268-7283`).

`ProcessDirectManipulationViewportValuesUpdate` (`:8604-8790`) does the **single** transform read per tick:

```cpp
IFC(pDirectManipulationService->GetPrimaryContentTransform(
    pViewport, newTranslationX, newTranslationY, newUncompressedZoomFactor, newZoomFactorX, newZoomFactorY));
```

which lands in `CDirectManipulationService::GetDMTransform`
(`DirectManipulationService.cpp:3639-3693`):

```cpp
IFC_RETURN(pDMContent->GetContentTransform(reinterpret_cast<XFLOAT*>(&matrix), 6));
ASSERT(matrix[0] == matrix[3]);
uncompressedZoomFactor = matrix[0];
...
if (!fUsingCustomOverpanReflexes)
{
    IFC_RETURN(pDMContent->GetOutputTransform(reinterpret_cast<XFLOAT*>(&matrix), 6));
}
zoomFactorX = matrix[0]; zoomFactorY = matrix[3];
translationX = matrix[4]; translationY = matrix[5];
```

Two DManip transforms exist and they mean different things:
* `GetContentTransform` — the *uncompressed* manipulation state (used for the "true" zoom factor).
* `GetOutputTransform` — the *rendered* transform including overpan compression.

Secondary contents (sticky headers, clips) are polled in the same tick via
`ProcessDirectManipulationSecondaryContentsUpdate` (`InputServices.cpp:8800-8930`) →
`GetSecondaryContentTransform`.

### 4.2 Re-arming the frame loop

At the end of `ProcessDirectManipulationViewportChanges(pViewport)`
(`InputServices.cpp:7317-7323`):

```cpp
// If the viewport is active, request another tick to continue updating the UI thread tree
// in response to the ongoing DM changes.
IFC_RETURN(pViewport->GetCurrentStatus(currentStatus));
if (IsViewportActive(currentStatus)) { IFC_RETURN(RequestAdditionalFrame()); }
```

with (`core/inc/InputServices.h:1078-1081`):

```cpp
static bool IsViewportActive(XDMViewportStatus status)
{ return status == XcpDMViewportRunning || status == XcpDMViewportInertia || status == XcpDMViewportSuspended || status == XcpDMViewportAutoRunning; }
```

and `RequestAdditionalFrame` (`InputServices.cpp:7065-7080`):

```cpp
ITickableFrameScheduler *pFrameScheduler = pBH->GetFrameScheduler();
if (pFrameScheduler) { IFC_RETURN(pFrameScheduler->RequestAdditionalFrame(0 /*immediate*/, RequestFrameReason::InputManager)); }
```

So the UI-thread cadence during a manipulation is **one tick per frame, self-sustaining**, and every tick does
exactly one `GetOutputTransform` per DManip content — not one per input packet.

Statuses (`pal/inc/paltypes.h:428-438`):
```cpp
XcpDMViewportBuilding=0, XcpDMViewportEnabled=1, XcpDMViewportDisabled=2, XcpDMViewportRunning=3,
XcpDMViewportInertia=4, XcpDMViewportReady=5, XcpDMViewportSuspended=6, XcpDMViewportAutoRunning=7
```

### 4.3 Why the UI-thread work does **not** cost a render walk

`ProcessDirectManipulationViewportValuesUpdate`, `InputServices.cpp:8720-8736`:

```cpp
if (fNotifyManipulationDelta || fIsLastDelta)
{
    // Note that this invalidation is marked as independent. This prevents an unnecessary render walk
    // from occurring if the only change this frame was a DM value update, and no dependent changes
    // occurred in response. If a dependent change occurs (e.g. new virtualized items added, scroll indicator moved)
    // then those will also mark an element dirty and that will cause a render walk to occur.
    CUIElement::NWSetTransformDirty(
        pManipulatedElement,
        DirtyFlags::Render | DirtyFlags::Bounds | DirtyFlags::Independent
        );

    IFC(ProcessDirectManipulationSecondaryContentsUpdate(pViewport, pDirectManipulationService));
}
```

`DirtyFlags::Independent` short-circuits every `NWSet*Dirty` handler
(`components/elements/UIElementRenderWalk.cpp`):

```cpp
// :118    void CUIElement::NWSetOpacityDirty(...)   { if (!flags_enum::is_set(flags, DirtyFlags::Independent)) { ... } }
// :236    void CUIElement::NWSetContentDirty(...)   { if (!flags_enum::is_set(flags, DirtyFlags::Independent)) { ... }
// :255-260                                            else if (flags_enum::is_set(flags, DirtyFlags::Bounds) != 0) {
//                                                        // Independent changes can only dirty bounds.
//                                                        ASSERT(flags == (DirtyFlags::Independent | DirtyFlags::Bounds));
//                                                        pUIE->NWSetDirtyFlagsAndPropagate(flags, FALSE); } }
// :281-307 void CUIElement::NWSetSubgraphDirty(...)  same shape
```

So an independent transform change propagates **bounds only** — enough to keep hit-testing/bounds correct,
not enough to force a re-render of subtree content.

### 4.4 Why there is **no layout pass per delta** — the frozen arrange

`ScrollViewer::HandleManipulationDelta` (`dxaml/lib/ScrollViewer_Partial.cpp:13432-13840`) *does* push
offsets down: `ScrollByPixelDelta` (`:6126-6280`) → `ScrollToHorizontalOffsetInternal` →
`ScrollContentPresenter::SetHorizontalOffsetPrivate`
(`dxaml/lib/ScrollContentPresenter_Partial.cpp:824-882`), which calls `InvalidateArrange()` at line 871.

But that arrange is *neutralized during a manipulation*.
`ScrollContentPresenter::ArrangeOverride` (`ScrollContentPresenter_Partial.cpp:2094-2517`), lines 2254-2273:

```cpp
if (spScrollViewer && spScrollViewer.Cast<ScrollViewer>()->IsInManipulation())
{
    offsetX = -(spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationOffsetX());
    offsetY = -(spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationOffsetY());
    zoomFactor = spScrollViewer.Cast<ScrollViewer>()->GetPreDirectManipulationZoomFactor();
}
else
{
    if (spScrollViewer && spScrollViewer.Cast<ScrollViewer>()->IsInDirectManipulationCompletion())
    { IFC(spScrollViewer.Cast<ScrollViewer>()->PostDirectManipulationLayoutRefreshed()); }
    offsetX = -static_cast<FLOAT>(pScrollData->m_ComputedOffset.X);
    offsetY = -static_cast<FLOAT>(pScrollData->m_ComputedOffset.Y);
    zoomFactor = currentZoomFactor;
}
```

`GetPreDirectManipulationOffsetX/Y/ZoomFactor` are frozen snapshots taken at manipulation start
(`dxaml/lib/ScrollViewer_Partial.h:1220-1233`, backed by `m_preDirectManipulationOffsetX/Y`,
`m_preDirectManipulationZoomFactor`).

Moreover, in the current source these locals are **never consumed further down `ArrangeOverride`** — the
child is arranged at the header origin only (`ScrollContentPresenter_Partial.cpp:2295-2305`):

```cpp
childRect.X = static_cast<FLOAT>(DoubleUtil::Max(topLeftHeaderDesiredSize.Width,  leftHeaderDesiredSize.Width));
childRect.Y = static_cast<FLOAT>(DoubleUtil::Max(topLeftHeaderDesiredSize.Height, topHeaderDesiredSize.Height));
childRect.Width  = static_cast<FLOAT>(DoubleUtil::Max(desiredSize.Width,  finalSize.Width));
childRect.Height = static_cast<FLOAT>(DoubleUtil::Max(desiredSize.Height, finalSize.Height));
IFC(spChild->Arrange(childRect));
```

(verified by scanning NR 2094..2900 for any other use of `offsetX`/`offsetY`/`zoomFactor` — only the
declarations and the two assignments above appear.)

**Therefore: the legacy `ScrollContentPresenter` never translates its child by the scroll offset. 100 % of
visible scroll motion comes from the DManip shared transform on the comp node.** The `IScrollInfo` offsets are
bookkeeping for hit-testing, scrollbars, `BringIntoView`, and virtualization.

The one place a *synchronous* layout is forced during a delta is when a virtualizing panel
(`IManipulationDataProvider`) is present (`ScrollViewer_Partial.cpp:13683`):

```cpp
IFC(m_trElementScrollContentPresenter.Cast<ScrollContentPresenter>()->UpdateLayout());
// Now that the synchronous layout update completed, unexpected offset changes must be propagated to the manipulation handler again.
m_isOffsetChangeIgnored = FALSE;
...
IFC(m_trElementScrollContentPresenter.Cast<ScrollContentPresenter>()->AreScrollOffsetsInSync(areScrollOffsetsInSync));
if (!areScrollOffsetsInSync) { IFC(SynchronizeScrollOffsets()); }
```

— i.e. item realization is the *only* per-delta layout cost, and it is opt-in via the virtualizing panel.

### 4.5 Notification batching / event coalescing

`HandleManipulationDelta` brackets itself with (`ScrollViewer_Partial.cpp:13478-13481`):

```cpp
// Batch up any potential ViewChanging/ViewChanged events during HandleManipulationDelta into a single notification
DelayViewChanging();
DelayViewChanged();
```
…and flushes once at the end (`:13830-13832`):
```cpp
hr = FlushViewChanging(hr);
RRETURN(FlushViewChanged(hr));
```

Status change bursts are also smoothed. `ProcessDirectManipulationViewportChanges`
(`InputServices.cpp:7126-7175`) has an explicit anti-flicker delay:

```cpp
static const XUINT32 StatusChangesForIntermediaryStatus = 2;
...
// Workaround for bug 689141. Occasionally DM sends an extraneous Ready status in between two active statuses.
if (!pViewport->GetHasDelayedStatusChangeProcessing() &&
    !IsViewportActive(oldStatus) && fHasActiveStatus && currentStatus == XcpDMViewportReady &&
    pViewport->GetIgnoredRunningStatuses() == 0)
{
    // Delaying processing to see if the viewport is going back to an active status shortly.
    pViewport->SetHasDelayedStatusChangeProcessing(TRUE);
    IFC_RETURN(RequestAdditionalFrame());
    return S_OK;
}
```

**Design lesson**: a spurious `Ready` between two active statuses would otherwise fire
`ManipulationCompleted` → snap-back → `ManipulationStarted`, which the user reads as a stutter. WinUI
deliberately defers one frame to see if it was transient.

### 4.6 Bail-out: inertia without a comp node

`OnPostUIThreadTick` → `StopInertialViewportsWithoutCompositorPeer`
(`InputServices.cpp:7348-7400`), run **after** `NWDrawTree` so composition peers are up to date:

```cpp
if (currentStatus == XcpDMViewportInertia && pViewport->GetManipulatedElementNoRef()->GetCompositionPeer() == nullptr)
{
    // The viewport is in the Inertia phase and it does not have a composition peer.
    // Immediately jump to the end-of-inertia transform and complete the manipulation since there
    // are no shared transforms for this viewport.
    IFC_RETURN(StopInertialViewport(pViewport, false /*restrictToKnownInertiaEnd*/, nullptr));
}
```

i.e. rather than let inertia animate invisibly (or force UI-thread-driven motion), WinUI **teleports to the
end state**.

---

## 5. Mouse wheel injection and smoothing

### 5.1 Route

`ScrollViewer::OnPointerWheelChanged` (`dxaml/lib/ScrollViewer_Partial.cpp:2720-2843`). When the
`ScrollContentPresenter` is the `IScrollInfo` implementer (the default, non-virtualized case):

```cpp
if (isScrollContentPresenterScrollClient)
{
    // Give DirectManipulation an opportunity to handle the mouse wheel message
    IFC(ProcessPureInertiaInputMessage(messageZoomDirection, &handled));
    IFC(pArgs->put_Handled(handled));
}
else
{
    // Let the IScrollInfo implementation handle the wheel delta
    ... spScrollInfo->MouseWheelDown(-mouseWheelDelta) ... // classic per-detent jump
}
```

`ProcessPureInertiaInputMessage` (`:9756-9803`) — note the naming, "**pure inertia**":

```cpp
// Called when this DM container wants the DM handler to process the current pure inertia input message,
// by forwarding it to DirectManipulation.
...
// Pass the event to DM, except if all these hold:
// - it's a zoom event, AND - we have zoom enabled, AND - we have zoom chaining enabled, AND
// - won't result in a zoom change ...
// This allows us to implement zoom chaining via our regular routed events. DM doesn't provide
// for chaining of inertia-only manipulations (as in, anything not related to a touch pointer).
if (!stopProcessing) { IFC(ProcessInputMessage(false /*ignoreFlowDirection*/, isHandled)); }
```

→ `IDirectManipulationContainerHandler::ProcessInputMessage`
(`core/inc/DirectManipulationContainerHandler.h:122-125`) → `CInputServices` →
`IPALDirectManipulationService::ProcessInput`.

### 5.2 The forwarding itself

`CDirectManipulationService::ProcessInput` (`DirectManipulationService.cpp:481-608`). The message is
**reconstructed** as a real Win32 `MSG` and pushed into DManip:

```cpp
// A new MSG is reconstructed from the PAL MsgPacket and provided to DM.
msg.hwnd    = CInputServices::GetUnderlyingInputHwndFromIslandInputSite(m_islandInputSite.Get());
msg.message = GetWindowsMessageFromMessageMap(msgID, fIsSecondaryMessage, fIsKeyboardInput);
msg.wParam  = GetWindowsMessageWParam(msgID, wParam, fInvertForRightToLeft && fIsForHorizontalPan);
msg.lParam  = pMsgPack->m_lParam;
msg.time    = ::GetMessageTime();
msg.pt.x = 0; msg.pt.y = 0;

IFC(GetDMViewportFromHandle(pViewport, &pDMViewport));

// Note that these pseudo pointer ids must still use the pointer id version of SetContact ...
IFC(pDMViewport->SetContact(fIsKeyboardInput ? DIRECTMANIPULATION_KEYBOARDFOCUS : DIRECTMANIPULATION_MOUSEFOCUS /*pointerId*/));

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
fHandled = !!handled;
IFC(pDMViewport->ReleaseContact(fIsKeyboardInput ? DIRECTMANIPULATION_KEYBOARDFOCUS : DIRECTMANIPULATION_MOUSEFOCUS /*pointerId*/));
```

Message mapping (`:4691-4709`):

```cpp
case XCP_POINTERWHEELCHANGED: return fIsSecondaryMessage ? WM_POINTERHWHEEL : WM_POINTERWHEEL;
case XCP_KEYDOWN:             fIsKeyboardInput = TRUE; return fIsSecondaryMessage ? WM_SYSKEYDOWN : WM_KEYDOWN;
```

Pseudo-contacts `DIRECTMANIPULATION_MOUSEFOCUS` / `DIRECTMANIPULATION_KEYBOARDFOCUS` are set **and released
around a single message** — that is what makes DManip treat it as a "pure inertia" manipulation with no
Running phase: it goes straight from `Ready` → `Inertia`.

Keyboard scrolling rides the same path. Direction filtering
(`:4719-4766`): `VK_LEFT/VK_RIGHT` → horizontal-only; `VK_UP/VK_DOWN` → vertical-only;
`VK_PRIOR/VK_NEXT/VK_HOME/VK_END` → axis chosen by config + Ctrl (`:549-565`):

```cpp
else if ((activatedConfiguration & (XcpDMConfigurationPanX | XcpDMConfigurationPanY)) == (XcpDMConfigurationPanX | XcpDMConfigurationPanY))
{
    XUINT32 modifierKeys = 0;
    IFC(gps->GetKeyboardModifiersState(&modifierKeys));
    fIsForHorizontalPan = modifierKeys & KEY_MODIFIER_CTRL ? TRUE : FALSE;
}
```

RTL inversion is a wParam remap (`:4776-4804`): `VK_LEFT↔VK_RIGHT`, `VK_PRIOR↔VK_NEXT`, `VK_HOME↔VK_END`.

### 5.3 Does DManip animate the wheel detent? Yes.

Direct evidence in XAML source:
* `ScrollViewer::ProcessPureInertiaInputMessage` doc comment (`:9744-9753`) calls it an
  "**inertia-only manipulation**" and notes "DM doesn't provide for chaining of inertia-only manipulations".
* `CInputServices` marks the viewport for a hit-test replay after the wheel scroll finishes
  (`InputServices.cpp:6908-6916`):
  ```cpp
  // In the case of a mouse scroll, we want to replay the most recent pointer update after the DM inertia is done.
  // The pointer could be over a new element after the content is done scrolling.
  if (currentMsgForDirectManipulationProcessing->m_msgID == XCP_POINTERWHEELCHANGED)
  {
      pViewport->SetRequestReplayPointerUpdateWhenInertiaCompletes(TRUE);
  }
  ```
* `CDMViewport` field comment (`core/inc/DMViewport.h:1388-1392`):
  > `// When a mouse wheel scroll completes, we want to replay the most recent pointer update (mouse move) message to hit test again. Mouse wheel scroll is handled by DM through inertia, so replay the pointer update when we transition out of the inertia state and back into ready.`
  > `// Flick is also handled with inertia, and in that case we don't want to replay the pointer update ...`

So each wheel detent starts (or *extends*, when detents arrive in quick succession — DManip accumulates)
a compositor-side inertia animation. The wheel is smooth for exactly the same reason a flick is: the animation
runs on the compositor with vsync-aligned sampling, and the UI thread only observes it once per tick.

Off-path: when there *is* a custom `IScrollInfo` (e.g. a virtualizing panel implementing it),
`MouseWheelUp/Down/Left/Right` is used — the classic *unanimated* per-detent jump
(`ScrollViewer_Partial.cpp:2812-2836`). That is the historical source of "some WinUI lists scroll smoothly by
wheel, some jump".

---

## 6. Overpan / bounce, and chaining to a parent viewport

### 6.1 Default overpan is DManip's, and it is compositor-side

For `XcpDMOverpanModeDefault` XAML installs nothing — DManip's built-in overpan compression produces the
rubber-band, and it shows up in `GetOutputTransform` (vs the uncompressed `GetContentTransform`).
`GetDMTransform` (`DirectManipulationService.cpp:3658-3690`) reads *both* precisely to distinguish
"where the content logically is" from "where it is drawn while bouncing".

### 6.2 Overpan **suppression** (`DMOverpanModeNone = 0x04`) — parametric reflexes

`ApplyOverpanModes` (`DirectManipulationService.cpp:4914-5048`) creates, per viewport, a
`ViewportOverpanReflexes` (`DirectManipulationService.h:635-670`) holding **six** DManip contents:

```cpp
IFC(CreateParametricBehavior(spManager2.Get(), spDMViewport2.Get(), pReflexes));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spContentPrimaryReflex));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spContentSecondaryReflex));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spLeftHeaderPrimaryReflex));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spLeftHeaderSecondaryReflex));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spTopHeaderPrimaryReflex));
IFC(CreateParametricReflex(spManager2.Get(), spDMViewport2.Get(), &pReflexes->m_spTopHeaderSecondaryReflex));
```

Each reflex is a `CLSID_Microsoft_ParametricMotionBehavior` content added to the viewport
(`:5146-5163`):

```cpp
IFC(pManager2->CreateContent(NULL /*pFrameInfoProvider*/, CLSID_Microsoft_ParametricMotionBehavior, IID_PPV_ARGS(&spReflex)));
IFC(pDMViewport2->AddContent(spReflex.Get()));
```

Roles (`GetDMTransformFromOverpanReflexes`, `:3799-3802`):

```cpp
// Combine the reflex matrices to get the final transform.
// The primary reflex implements the scaling effect and centerpoint correction effect.
// The secondary reflex implements the overpan suppression effect.
combinedD2DMatrix = secondaryReflexD2DMatrix * primaryReflexD2DMatrix;
```

and the compositor sees the same composition — the "complex" expression form in
`DManipData.cpp:172` / `ManipulationTransform.cpp:109-138`:
`"transform * manipTransformSecondary.Matrix * manipTransformPrimary.Matrix"`.

**Smoothness implication**: overpan suppression stays *independent* — it is expressed as extra DManip
contents that the compositor multiplies in, not as a UI-thread correction.

Curve refresh (`RefreshOverpanCurves`, `:5050-5116`) is invoked whenever viewport/content bounds change
(`SetContentBounds` line 2182: `IFCFAILFAST(RefreshOverpanCurves(pViewport));`) and when overpan modes change.

Two hard-won constraints are documented:

* **Don't attach a viewport behavior mid-manipulation** (`:4964-4971`):
  ```cpp
  // We can't update the viewport behavior while in manipulation since this may hit a DM deadlock (WPB 275883).
  // ... it seems okay to wait until the zoom manipulation completes and a new manip is started ...
  if (fIsStartingNewManipulation && pReflexes->m_fIsBehaviorRefreshNeeded) { ... }
  ```
* **Curve updates must be atomic w.r.t. the reading thread** — `XcpAutoCriticalSection m_overpanReflexesLock`
  (`DirectManipulationService.h:702-707`), taken in `ApplyOverpanModes` (`:4976`, `:4995`),
  `CleanupOverpanReflexData` (`:5172`) and, importantly, on the *read* side in `GetDMTransform`
  (`:3669-3679`) with a double-check:
  ```cpp
  XcpAutoLock lock(m_overpanReflexesLock);
  // After acquiring the lock, check a second time to make sure the overpan reflex data is still available.
  if (m_mapViewportOverpanReflexes.ContainsKey(static_cast<XHANDLE>(pViewport))) { ... }
  ```
  Comment at `DirectManipulationService.h:468-471`:
  > `// NOTE: ApplyCurves() is called individually for each curve, so lock any call to this method if the curve updates need to be processed atomically (i.e. when first applied).`
  Non-atomic curve updates → visible discrete jumps. Also `DirectManipulationService.h:697-699`:
  > `// Clamping them at 0 prevents negative values and prevents these curves from overlapping with the left/top curves (curve overlap can cause discrete jumps e.g. when zooming in!).`

### 6.3 Chaining

**Declaration**: `SetViewportChaining` → `IDirectManipulationViewport::SetChaining(dmMotionTypes)`
(`DirectManipulationService.cpp:1611-1638`). Motion types map 1:1
(`DirectManipulationService.h:388-391`; `DirectManipulationTypes.h:48-56`):

```cpp
DMMotionTypeNone=0x00, DMMotionTypePanX=0x01, DMMotionTypePanY=0x02,
DMMotionTypeZoom=0x04, DMMotionTypeCenterX=0x10, DMMotionTypeCenterY=0x20
```

**Contact fan-out is what makes chaining work.** `CInputServices::InitializeDirectManipulationForPointerId`
(`InputServices.cpp:4919-5013`) walks the whole ancestor chain and calls `SetDirectManipulationContact` on
**every** DM container it finds:

```cpp
do
{
    pParentDO = pParentDO->GetParentInternal();
    if (pParentDO)
    {
        pElement = do_pointer_cast<CUIElement>(pParentDO);
        if (pElement)
        {
            if (fUseDM)
            {
                if (pElement->GetIsDirectManipulationContainer())
                {
                    IFC(SetDirectManipulationContact(pointerId, fIsForDMHitTest, pPointedElement, pChildElement, pElement, &fContactFailure));
                    ExitOnSetContactFailure(fContactFailure);
                    *pContactSuccess = true;
                }
                fUseDM = (SystemManipulationModes(pElement->GetManipulationMode()) != DirectUI::ManipulationModes::None);
                if (!fUseDM && fIsForDMHitTest) { break; }
            }
            if (!fIsForDMHitTest) { IFC(SetDirectManipulationCrossSlideContainer(pointerId, pElement, &fContactFailure)); ... }
            pChildElement = pElement;
        }
    }
} while (pParentDO);
```

Because the *same* pointer id is registered on the child and all ancestor viewports, **DManip itself**
arbitrates which viewport consumes the motion and when to hand over. XAML never re-routes a delta between
viewports. That is why chaining is seamless — the handover is inside DManip, on its own thread, mid-gesture,
with no UI-thread involvement and no re-hit-test.

**XAML's own bookkeeping around chaining** — `CInputServices::HasChainingChildViewport`
(`InputServices.cpp:4141-4383`), documented at `:4128-4139`:

> `// Chaining only kicks in when`
> `//   - both viewports have the touch configuration activated.`
> `//   - the child viewport is in Running status.`

Its six checks: (1) child active/`Running`; (2) child has touch config activated; (3) child has non-`None`
chained motion types; (4) child's chaining ∩ child's touch configuration ≠ ∅; (5) child's chaining ∩
**parent's** touch configuration ≠ ∅; (6) child is an actual descendant of the parent's manipulated element.
`FilterChainedMotions` (`:4104-4125`) does the intersections.

This is used to **delay the parent's `ManipulationCompleted`** while a chaining child is still live
(`:7976-8010`, `:8331`) — otherwise the parent would fire completion → snap-back mid-gesture. See also
`CompleteParentViewports` (`:4389+`).

Chaining independence in the transform domain is automatic: each viewport has its own DManip content and its
own shared transform / comp node, so a chained parent scroll and a child scroll are two independent
expression animations on two visuals.

---

## 7. `DM_POINTERHITTEST`, "sticky" contact handoff, and latency

### 7.1 What `DM_POINTERHITTEST` is for

Windows sends `DM_POINTERHITTEST` when a contact lands over a window that has active DManip viewports, **before**
the normal `WM_POINTERDOWN` routing settles. The app must synchronously decide which viewport(s) get the
contact; if it does, DManip takes the pointer stream immediately and the pan starts on the very first move —
no waiting for XAML hit-testing, gesture recognition, or a manipulation threshold on the UI thread.

**Routing** (message must reach XAML even when the XAML `Window` has no content):

* `CJupiterWindow::ShouldForwardToControl` includes it (`dxaml/lib/JupiterWindow.cpp:328`).
* `CoreWindowSubclassProc` forces `fDefaultToControl = true` (`JupiterWindow.cpp:429-431`):
  ```cpp
  case DM_POINTERHITTEST:
      fDefaultToControl = true;
      break;
  ```
  (contrast with `default:` at `:434-437`, which drops messages when `Window.Content` is unset).
* `CJupiterControl::HandleWindowMessage` (`dxaml/lib/JupiterControl.cpp:297-301`) and
  `HandleGenericMessage`/wndproc (`JupiterControl.cpp:966`) both route it to `HandlePointerMessage`.
* Island path: `CJupiterWindow::OnIslandDirectManipulationHitTest`
  (`JupiterWindow.cpp:1189-1205`) → `m_pControl->HandlePointerMessage(DM_POINTERHITTEST, pointerId, 0, contentRoot, false, pointerPoint)`;
  `XamlIslandRoot.cpp:506` injects it (`InjectPointerMessage(DM_POINTERHITTEST, e)`).
* PAL mapping: `XCP_DMPOINTERHITTEST = 42` (`pal/inc/paltypes.h:594`);
  `host/win/browserdesktop/PlatWinBrowserHost.cpp:47-48` translates `DM_POINTERHITTEST → XCP_DMPOINTERHITTEST`.

### 7.2 The handler — hit-test + `SetContact`, synchronously

`CInputServices::ProcessDirectManipulationPointerHitTest`
(`InputServices.cpp:1143-1208`), doc comment `:1137-1141`:

> `// Processes DM_POINTERHITTEST message. Does a hit-test and calls DManip's SetContact on hit-tested viewports.`

```cpp
// Do the hit-test
IFC(contentRoot->GetInputManager().GetPointerInputProcessor().HitTestHelper(pMsg->m_pointerInfo.m_pointerLocation, contentRoot->GetXamlIslandRootNoRef(), &pDOContact));
...
if (!pDOContact)
{
    // No hit-testable element was found on the walk up. Set the current contact to the visual root.
    pDOContact = contentRoot->GetVisualTreeNoRef()->GetPublicRootVisual();
    ...
}
pContactElement = do_pointer_cast<CUIElement>(pDOContact);
if (pContactElement)
{
    bool unused;
    IFC(InitializeDirectManipulationForPointerId(pMsg->m_pointerInfo.m_pointerId, TRUE /*fIsForDMHitTest*/, pContactElement, &unused));
}
*pfHandled = TRUE;
```

`fIsForDMHitTest = TRUE` changes the ancestor walk: cross-slide containers are skipped, and the walk **stops**
at the first ancestor with `ManipulationMode == None` (`InputServices.cpp:4976-4982`). That keeps this
latency-critical path short.

### 7.3 The "sticky" bit: `m_fHasDMHitTestContactId`

`CDMViewport` bit field (`core/inc/DMViewport.h:1378-1383`):

> `// Set to True when a SetContact call was made for this viewport due to a DM_POINTERHITTEST message.`
> `// A subsequent viewport interaction type of XcpDMViewportInteractionEnd then means that all contact points were released.`

`CInputServices::ProcessDirectManipulationViewportInteractionTypeUpdate`
(`InputServices.cpp:7760-7790`):

```cpp
if (pViewport->GetHasDMHitTestContactId())
{
    if (newInteractionType == XcpDMViewportInteractionManipulation)
    {
        // This notification marks the recognition of a DManip manipulation for a DM_POINTERHITTEST-initiated contact.
        // The transition to a inactive status can be used to mark the end of the manipulation rather than the
        // XcpDMViewportInteractionEnd interaction type notification.
        pViewport->SetHasDMHitTestContactId(FALSE);
    }
    else if (newInteractionType == XcpDMViewportInteractionEnd)
    {
        // This notification marks the end of an interaction where at least one contact Id was registered due to a DM_POINTERHITTEST message.
        pViewport->SetHasDMHitTestContactId(FALSE);
        IFC_RETURN(UnregisterContactIds(pViewport, FALSE /*fReleaseAllContactsAndDisableViewport*/));
    }
}
```

Because `DM_POINTERHITTEST` registers the contact *before* the normal `WM_POINTERDOWN`/`WM_POINTERUP` pair is
guaranteed to arrive at XAML, XAML cannot rely on pointer-up to clean up; it uses the DManip **interaction
type** notification instead. Get this wrong and the viewport is left in `ManipulationStarting` forever — see
the recovery path at `InputServices.cpp:7326-7338`:

```cpp
// A WM_POINTERDOWN was received while a viewport was in inertia phase. No transition to an active status occurred
// for pViewport and no WM_POINTERUP is received. Thus pViewport would remain in the ManipulationStarting state.
IFC_RETURN(UnregisterContactIds(pViewport, FALSE));
```

### 7.4 Contact-failure fast path

`SetContact` errors are treated as *expected* and unwound (`DirectManipulationService.cpp:663-690`):
`ERROR_OBJECT_ALREADY_EXISTS`, `ERROR_OBJECT_NO_LONGER_EXISTS` are recognized; the whole ancestor walk aborts
via the `ExitOnSetContactFailure(fContactFailure)` macro and cross-slide viewports are discarded
(`InputServices.cpp:5000-5008`). A `SuspendFailFastOnStowedException suspender` wraps the actual call
(`:657-660`, comment: `// http://osgvsowi/14575768 - Suspend until we have a touch driver reset solution`).

### 7.5 The other latency knob: `GetPointerPointFromPointerId`

`SetContact` and the wheel path both re-resolve a live `Microsoft.UI.Input.PointerPoint`
(`DirectManipulationService.cpp:5247-5258`, `m_pointerPointStatics->GetCurrentPoint(pointerId, ...)`).
This gives DManip the *current* pointer sample rather than a stale coordinate captured by XAML earlier in the
message pump — one less frame of positional lag at manipulation start.

---

## 8. Secondary content, sticky headers and clips (bonus, relevant to "everything must stay independent")

`XDMContentType` (`DirectManipulationTypes.h:66-75` / `paltypes.h`):

```cpp
DMContentTypePrimary=0, DMContentTypeTopLeftHeader=1, DMContentTypeTopHeader=2,
DMContentTypeLeftHeader=3, DMContentTypeCustom=4, DMContentTypeDescendant=5
```

Sticky headers and parallax are **not** UI-thread corrections. `AddSecondaryContent` takes
`CParametricCurveDefinition*` arrays (`PalDirectManipulationService.h:47`,
`DirectManipulationService.cpp:1275-1375`), pushed to DManip via
`m_pDMHelper->SetupSecondaryContent(...)` (`:4541-4575`) with cubic segments
(`beginOffset, constantCoefficient, linearCoefficient, quadraticCoefficient, cubicCoefficient`, `:4565-4571`).
Clip content works the same (`AddSecondaryClipContent`, `:1443-1543`).

Each secondary content also gets its own shared transform and its own comp-node expression — so a sticky
header stays pinned *on the compositor*, not by the UI thread.

Curve changes are made race-free by `RemoveSharedContentTransformMapping`
(`DirectManipulationService.cpp:3182-3207`), commented:

> `// Clear out the mapping we have for this content from content -> shared parts map. This function helps carry out synchronizing a change to a sticky header curve.`

and by a deferred release queue (`DMDeferredRelease`, `ProcessDeferredReleaseQueue`,
`InputServices.cpp:13141-13148`):

```cpp
// If we have items for this viewport in the deferred release queue, we need to process them immediately.
// Not doing so would leave live DManip transforms in the tree and cause a visible flash.
```

Also, before an animated `BringIntoView`, `PrepareCompositionNodesForBringIntoView`
(`InputServices.cpp:13108-13140`) resets manipulation data on primary **and** secondary/clip contents:

> `// We don't want secondary/clip content to be updated immediately either as this operation would likely happen on a different frame and appear to glitch`

---

## 9. Rounding / snapping discipline (jitter avoidance)

DManip works in integer client pixels. XAML's conversion is deliberately asymmetric
(`DirectManipulationService.cpp:4814-4890` for the helpers; usage at `:2029-2055` and `:2166-2169`):

```cpp
// SetViewportBounds — "To accommodate snap points, always round down the viewport width, instead of picking the closest integer."
viewportBounds.right  = viewportBounds.left + GetRoundedDownLong(clientBounds.Width);
viewportBounds.bottom = viewportBounds.top  + GetRoundedDownLong(clientBounds.Height);
// ...with a floor:
// "Avoid a nil viewport width when the actual width is between 0 and 1 pixel. Declare a 1 pixel width instead."

// SetContentBounds — "To accommodate snap points, always round up the content sizes, instead of picking the closest integers."
contentBounds.right  = contentBounds.left + GetRoundedUpLong(bounds.Width);
contentBounds.bottom = contentBounds.top  + GetRoundedUpLong(bounds.Height);
```

Viewport rounds **down**, content rounds **up** ⇒ the scrollable range is never accidentally shortened by
rounding, so a snap point at the extent end is always reachable. Combined with
`DIRECTMANIPULATION_VIEWPORT_OPTIONS_DISABLEPIXELSNAPPING` (`:4313`) this means the *animation* is
sub-pixel-continuous while only the *bounds* are quantized.

Snap points API: `SetPrimaryContentSnapPoints` (regular: offset+interval, `:2445-2488`; irregular: array,
`:2489-2537`), `SetPrimaryContentSnapPointsType(fAreSnapPointsOptional, fAreSnapPointsSingle)` (`:2539-2594`),
`SetPrimaryContentSnapPointsCoordinate` (`:2596-2640`). Coordinates
(`DirectManipulationTypes.h:59-64`): `Boundary=0x00, Origin=0x01, Mirrored=0x10`.

---

## 10. Cross-cutting: the two "workaround" caches that reveal DManip's failure modes

1. **Duplicate inertial transform.** `CCompositorDirectManipulationViewport`
   (`core/compositor/CompositorDirectManipulationViewport.h:139-144`):
   ```cpp
   // Fields used to cache the lastest inertial DManip transform. This is done as a workaround for a DManip bug
   // where the retrieved transform is a duplicate because a viewport characteristic has changed.
   bool m_fIsTransformSet; XFLOAT m_translationX; XFLOAT m_translationY; XFLOAT m_zoomFactor;
   ```
   with `IsThisTransformSet(...)` (`:57-66`) as an exact-equality dedupe.

2. **Viewport identity across recreation.** `GetCompositorViewportKey` uses the raw
   `IDirectManipulationViewport*` as the key (`DirectManipulationService.cpp:3889-3906`) so the compositor can
   detect "two compositor viewports point to the same underlying DM viewport"
   (`CompositorDirectManipulationViewport.h:83-86`).

---

## 11. Distilled model — "the DManip contract" in six bullets

1. One `IDirectManipulationManager` per `ScrollViewer`; one `IDirectManipulationCompositor` +
   `IDirectManipulationUpdateManager` **shared per UI thread**.
2. Input mode and update mode are both `AUTOMATIC`; XAML declares contacts and then gets out of the way.
3. The frame-info provider is the **compositor itself** — the manipulation is sampled at the next
   presentation timestamp.
4. The transform is a **shared compositor object**; XAML binds it once with an ExpressionAnimation and never
   writes a per-frame value.
5. The UI thread **polls** at most once per tick, applies `DirtyFlags::Independent`, and freezes arrange for
   the duration of the manipulation.
6. Everything that could break independence — overpan curves, sticky-header curves, transform release —
   is guarded by a lock, a deferred-release queue, or a "wait for the next manipulation" rule, because a
   non-atomic update is visible as a jump.

---

## 12. Explicit UNVERIFIED list

* `DirectManipulationHelper` implementation (`SetContact(viewport, pointerPoint)`,
  `ProcessInputWithPointerPoint`, `CreateSharedContentTransformForContent`, `ApplyPrimaryReflexCurves`,
  `ApplySecondaryReflexCurves`, `CreateParametricBehavior`, `SetPrimaryContentTransform`,
  `CreateSharedInteractionForViewport`) — header/source absent from `D:/Work/microsoft-ui-xaml2`.
* DManip's **deceleration model** (exponential vs constant-deceleration vs piecewise) and its default
  deceleration constants — inside `Microsoft.DirectManipulation.dll`. Only inference available:
  `GetInertiaEndTransform` exists, so the curve is analytically solvable at inertia start.
* DManip's default **overpan/bounce** curve (used when `XDMOverpanModeDefault`).
* DManip's internal **chaining arbitration** policy and its handover thresholds.
* Whether `IDirectManipulationCompositor`'s `GetNextFrameInfo` uses `pTime`/`pProcessTime` (XAML returns 0).
* The DComp-side evaluation cadence of the shared transform (assumed: every composition frame).
* The exact semantics of `DIRECTMANIPULATION_MOUSEFOCUS` / `DIRECTMANIPULATION_KEYBOARDFOCUS` pseudo-contact ids.

---

## 13. Applicability to Uno Skia — concrete mapping

Uno's managed `ScrollContentPresenter` already has the right *shape* for point (4) above:
`D:/Work/uno-worktrees/scrollsmooth/src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:412-470`
moves content by writing `visual.AnchorPoint` / `visual.Scale` on the content's `Visual` rather than by
arranging it at `-offset`:

```csharp
var target = new Vector2((float)(-horizontalOffset + centeringOffsetX), (float)(-verticalOffset + centeringOffsetY));
var targetScale = new Vector3(zoom, zoom, 1);
var visual = view.Visual;
...
if (options is { DisableAnimation: true } or { IsTouch: true })
{
    visual.StopAnimation(nameof(Visual.AnchorPoint));
    visual.StopAnimation(nameof(Visual.Scale));
    visual.AnchorPoint = target;
    visual.Scale = targetScale;
    Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
}
```

The structural gaps versus WinUI-on-DManip:

* **Inertia is integrated on the UI thread.**
  `src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:27-75` runs an
  `IInertiaProcessorTimer` and pushes deltas into `Set(...)`, i.e. the WinUI *anti*-pattern — motion is
  produced by the same thread that runs layout and app code. WinUI's equivalent runs off-thread and is
  sampled at present-time.
* **No presentation-time sampling.** There is no analogue of
  `IDirectManipulationFrameInfoProvider::GetNextFrameInfo` — nothing tells the inertia integrator "the frame
  you are computing will be shown at T".
* **No "independent dirty" concept in the scroll path.** WinUI's
  `DirtyFlags::Render | Bounds | Independent` is exactly the "update bounds, do not schedule a render walk"
  signal.
* **No frozen-arrange rule.** WinUI's `IsInManipulation()` → pre-manipulation offsets is the single cheapest
  way to guarantee that a mid-scroll `InvalidateArrange` cannot cause a visual jump.
* **The wheel-detent animation** in Uno is a Composition keyframe on the UI thread; WinUI hands the detent to
  DManip which produces a real inertia curve on the compositor and chains successive detents.

(Uno-side citations above are from the working tree at
`D:/Work/uno-worktrees/scrollsmooth`, branch `dev/mazi/smooth-scroll`.)
