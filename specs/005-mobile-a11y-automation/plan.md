# Implementation Plan: Mobile Accessibility and Automation

**Branch**: `005-mobile-a11y-automation` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/005-mobile-a11y-automation/spec.md`  
**Grounding**: [research.md](./research.md), based on independent Android, iOS,
WinUI/shared-contract, and mobile-test-infrastructure audits

## Summary

Add first-class Skia mobile accessibility backends for Android and iOS. Both backends use
the existing WinUI `AutomationPeer` graph and `SkiaAccessibilityBase` routing as the source
of truth, expose stable per-window native trees to TalkBack/VoiceOver and mobile automation
clients, route native actions to live provider patterns, and translate peer property/event
changes into targeted native invalidation and announcements.

Android keeps and refactors the existing `ExploreByTouchHelper` virtual-node bridge; iOS
adds a managed `UIAccessibilityElement` tree hosted by the existing Skia/Metal view. Native
node properties remain pull-based, avoiding a duplicate cached semantic model. Legacy
native Android/UIKit renderers are compile/no-regression targets only.

## Technical Context

**Language/Version**: C# on .NET 9.0/10.0; .NET Android and .NET iOS platform bindings  
**Primary Dependencies**: Uno.UI `AutomationPeer` and provider interfaces;
`Uno.UI.Runtime.Skia` (`AccessibilityRouter`, `SkiaAccessibilityBase`,
`IAccessibilityOwner`); AndroidX `ExploreByTouchHelper` /
`AccessibilityNodeInfoCompat`; Android accessibility events/actions; UIKit
`UIAccessibilityElement`, `UIAccessibility`, and `UIView.AccessibilityElements`  
**Storage**: In-memory per-window native-node registries with weak owner references; no persistence  
**Testing**: MSTest in `Uno.UI.RuntimeTests`; existing Skia Android/iOS device/simulator
runtime-test stages; existing `SamplesApp.UITests`; manual TalkBack, VoiceOver,
Accessibility Inspector, UIAutomator/XCUITest/Appium compatibility smoke  
**Target Platform**: Skia-on-Android and Skia-on-iOS. Existing repository-supported OS
versions; CI reference environments are Android API 34 and iOS 17.5 simulator  
**Project Type**: Cross-platform framework runtime extension; no new product or test project  
**Performance Goals**: No full accessibility-tree invalidation per render frame; native
property updates complete within the 16 ms p95 target on the 500-node fixture; only
realized/accessibility-required nodes for a 1,000-item virtualized collection; removed
nodes return registries to baseline  
**Constraints**: No new public API; preserve WinUI peer/tree/provider semantics; native
callbacks may arrive asynchronously and must marshal to the UI thread; stable node
identity; secure-text redaction; no strong-reference leaks; no native-renderer regression;
UIA-only concepts require explicit mobile fallbacks  
**Scale/Scope**: 39 core automation properties, 34 `PatternInterface` values, pattern
state groups, five relation groups, and 30 `AutomationEvents` values, plus
`RaiseNotificationEvent`; standard peer-backed control matrix across two mobile backends

### Resolved decisions

- **Scope**: Skia Android + Skia iOS; legacy native Android/UIKit are no-regression only.
- **Architecture**: Direct `SkiaAccessibilityBase` subclasses; no new mobile base class.
- **State model**: Pull live peer properties on native query; use push events only as
  invalidation/announcement hints. No cached semantic snapshot.
- **Tree**: Resolved automation-peer Control/Content tree with Raw-node pruning and
  descendant promotion; never keyboard tab-order enumeration.
- **Peer traversal**: `FrameworkElementAutomationPeer` children already compile in
  `Uno.UI.Skia`; mobile adapters consume that existing peer order unchanged.
- **Android**: Refactor the existing `UnoExploreByTouchHelper`; stable weak virtual-ID maps,
  bounds in screen, full provider actions, targeted invalidation.
- **iOS**: Pure managed `UIAccessibilityElement` objects in the existing Metal view's
  `AccessibilityElements`; use `AccessibilityFrameInContainerSpace`.
- **Availability**: Native trees remain queryable when TalkBack/VoiceOver is off so
  UIAutomator/XCUITest can inspect them; service state gates only unsolicited events.
- **Automation identity**: Android resource/unique ID and iOS
  `AccessibilityIdentifier`; never use AutomationId as the spoken name.
- **Testing**: Reuse existing RuntimeTests and SamplesApp.UITests; no new Appium project.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | PASS | No public API changes. Tree membership, EventsSource, provider behavior, errors, focus, and events come from the WinUI-aligned peer contract. |
| II. Cross-Platform Parity | PASS | Adds the missing Skia Android/iOS backends and isolates native projections in platform projects. Legacy renderers retain compilation/current behavior. |
| III. Test-First Quality Gates | PASS (enforced) | Every mapping/action/event slice includes real Android node or iOS element tests plus shared contract coverage; mobile Skia stages run on PRs. |
| IV. Performance and Resource Discipline | PASS | Removes Android per-render root invalidation, uses pull queries and targeted invalidation, weak registries, virtualization, and explicit leak/perf tests. |
| V. Generated Code Boundaries | PASS | No `Generated/` files are modified. |
| VI. Backward Compatibility | PASS | Internal additive backends and bug fixes; no public/binary breaking change. Behavior changes correct previously absent or invalid mobile accessibility. |
| VII. WinUI Implementation Alignment | APPLIES / PASS | User supplied the `microsoft-ui-xaml` source. Research mapped AutomationPeer lifecycle, EventsSource, properties, patterns, actions, focus, structure, and events before design. |

**Gate Status before research**: PASS.  
**Gate Status after design**: PASS. No constitution violation requires complexity tracking.

## Project Structure

### Documentation (this feature)

```text
specs/005-mobile-a11y-automation/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    ├── mobile-adapter-contract.md
    └── capability-matrix.md
```

### Source code (repository root)

```text
src/Uno.UI/Accessibility/
└── AccessibilityPeerHelper.cs
    # Small internal tree-inclusion/provider-action helper; live peers only, no snapshot (NEW)

src/Uno.UI.Runtime.Skia/Accessibility/
├── AccessibilityRouter.cs
└── SkiaAccessibilityBase.cs

src/Uno.UI.Runtime.Skia.Android/
├── Accessibility/
│   ├── AndroidSkiaAccessibility.cs
│   │   # Per-XamlRoot SkiaAccessibilityBase adapter (NEW)
│   ├── UnoExploreByTouchHelper.cs
│   │   # Peer-tree enumeration, native node projection, actions, weak IDs (MODIFY)
│   └── AccessibilityNodeInfoCompatJni.cs
│       # Mixed toggle/version-safe native checked state as needed (MODIFY)
├── Hosting/
│   ├── AndroidHost.cs
│   │   # Initialize AccessibilityRouter before Application.Start (MODIFY)
│   └── AndroidSkiaXamlRootHost.cs
│       # IAccessibilityOwner, adapter lifecycle/activation (MODIFY)
├── Rendering/
│   ├── IUnoSkiaRenderView.cs
│   ├── UnoSKCanvasView.cs
│   └── UnoSKVulkanView.cs
│       # Keep view-created helper; remove per-frame root invalidation and preserve
│       # explicit surface/orientation invalidation after relayout (MODIFY)
└── ApplicationActivity.cs
    # Window/activity activation and disposal routing if host lifecycle requires it (MODIFY)

src/Uno.UI.Runtime.Skia.AppleUIKit/
├── Accessibility/
│   ├── AppleUIKitAccessibility.cs
│   │   # Per-XamlRoot SkiaAccessibilityBase adapter and element registry (NEW)
│   └── UnoUIAccessibilityElement.cs
│       # Stable managed virtual element and provider action overrides (NEW)
├── Hosting/
│   └── AppleUIKitHost.cs
│       # Initialize AccessibilityRouter (MODIFY)
└── UI/Xaml/Window/
    ├── RootViewController.cs
    │   # IAccessibilityOwner and Metal-view accessibility container (MODIFY)
    └── AppleUIKitWindowWrapper.cs
        # NativeWindowWrapper adapter build/activate/show/dispose lifecycle (MODIFY)

src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/
├── Given_MobileAccessibilityTree.cs
├── Given_MobileAccessibilityActions.cs
├── Given_MobileAccessibilityEvents.cs
├── Given_MobileAccessibilityLifecycle.cs
├── Given_SkiaAndroidAccessibilityNode.cs
└── Given_SkiaIOSAccessibilityElement.cs
    # New native-observable runtime coverage; existing Given_Accessible* files also extended

src/SamplesApp/SamplesApp.Samples/Windows_UI.Xaml_Automation/
├── AccessibilityScreenReaderPage.xaml
└── AutomationProperties_*.xaml
    # Reuse/extend existing parity fixture; no separate sample app (MODIFY as needed)

src/SamplesApp/SamplesApp.UITests/Windows_UI_Xaml_Automation/
└── MobileAccessibility_Tests.cs
    # Android/iOS identifier and representative action smoke (NEW)
```

**Structure Decision**: Reuse the existing shared Skia router/base and existing mobile
runtime projects. Add one adapter per platform and one narrow internal helper for shared
tree/action semantics. Do not add a mobile semantic cache, native iOS framework, Appium
project, or new CI stage.

## Implementation design

### 1. Shared peer-tree and action helper

`FrameworkElementAutomationPeer.GetAutomationPeerChildren` already runs in the
`Uno.UI.Skia` assembly used by both mobile Skia runtimes. The adapters must consume the
returned peer order rather than adding platform compile guards or walking keyboard tab stops.
That order already incorporates `AreAutomationPeerChildrenReversed()` where required.

Extract only the shared logic needed by both adapters:

- Control/Content/Raw inclusion and descendant promotion;
- EventsSource resolution;
- native-action operation -> live provider execution;
- disabled/read-only/unavailable result translation.

The helper must not cache property values or retain peers.

### 2. Android adapter

`AndroidSkiaAccessibility` connects the per-XamlRoot host to
`SkiaAccessibilityBase`. The render view continues to create and install its existing
`UnoExploreByTouchHelper`; after XamlRoot creation, the adapter configures/wraps that helper
with the window owner, root resolver, node registry, and event/action routing. The
`IUnoSkiaRenderView.ExploreByTouchHelper` property remains the handoff surface.

Core changes:

- initialize/activate/dispose through `AccessibilityRouter`;
- replace tab-stop enumeration and `Window.Current` root lookup with peer-tree/XamlRoot routing;
- allocate monotonic virtual IDs with weak reverse mappings and generation checks;
- preassign IDs before relation resolution;
- populate names, roles, IDs, bounds, state, text, range, collection, hierarchy,
  relations, live settings, and actions from live peers;
- use `SetBoundsInScreen`;
- dispatch click/toggle/select/expand/range/value/scroll/realize/window/text/custom actions;
- override property routing for generic `InvalidateVirtualView`;
- if an update arrives before a virtual ID exists, record the handle as dirty so first
  registration/query pulls final state instead of silently dropping the update;
- map native focus, property, text, selection, structure, window, and announcement events;
- remove accessibility root invalidation from normal render invalidation;
- after Vulkan/GL surface size or orientation changes, explicitly invalidate the
  accessibility root once after layout so TalkBack re-queries all bounds.

### 3. iOS adapter

`AppleUIKitAccessibility` maintains stable managed virtual elements in the existing
Skia/Metal view container.

Core changes:

- register per-XamlRoot ownership/lifecycle through `RootViewController` and window wrapper;
- build the promoted peer tree into a stable `AccessibilityElements` collection;
- use weak adapter/node references and remove elements on lifecycle changes;
- pull label, hint, value, traits, ID, custom content, relations, and actions from live peers;
- use `AccessibilityFrameInContainerSpace`;
- implement activate/increment/decrement/scroll/escape and localized custom actions;
- post targeted layout/screen/announcement notifications;
- preserve modal scope and focus restoration;
- keep the tree queryable for XCUITest when VoiceOver is disabled.

### 4. Complete capability coverage

`contracts/capability-matrix.md` is normative. Implementation must close it category by
category:

- 39 core properties;
- all 34 provider patterns and associated state groups;
- relations and annotations;
- all 30 automation events plus notification events;
- explicit native limitations for UIA-only concepts.

Every row receives:

1. an initial-state native assertion;
2. a live-update/action assertion where dynamic;
3. a documented fallback when no native equivalent exists;
4. secure-text and AutomationId/name separation checks where applicable.

### 5. Test architecture

- Shared tests prove peer tree, Raw pruning, EventsSource, provider errors, actions, and
  event routing independently of platform projection.
- Android tests obtain the actual `AccessibilityNodeInfoCompat` from the native provider.
- iOS tests obtain the actual stable `UIAccessibilityElement` from the adapter/container.
- Lifecycle tests verify weak maps, generation rejection, no stale relations, focus
  recovery, window isolation, and disposal.
- Existing SamplesApp.UITests proves native automation lookup/actions.
- TalkBack/VoiceOver matrices validate announcement quality that cannot be asserted solely
  through node metadata.

## Phased delivery (suggested PR sequence)

Each phase is independently testable and keeps both mobile targets progressing toward parity.

### Phase A - Shared tree and native test harness (P1)

- Add shared tree/action helper and tests for Control/Content/Raw, EventsSource, custom
  peers, item peers, peer-returned reading order (including reversed children), and provider errors.
- Add narrow internal native-node test accessors.
- Add failing Android/iOS root-tree tests.

**Exit**: both platforms can prove the same promoted peer tree contract; iOS test fails
because no native elements exist, Android tests expose current tab-order gaps.

### Phase B - Platform hosts, identity, names, roles, and bounds (P1)

- Wire both hosts into `AccessibilityRouter`/`IAccessibilityOwner`.
- Add Android adapter and refactor virtual-ID/tree enumeration.
- Add iOS adapter and managed element container.
- Implement stable identity, AutomationId separation, name/help, control type, enabled,
  focusability, off-screen, password, heading/landmark/dialog basics, and correct bounds.
- Remove Android per-render accessibility-root invalidation.

**Exit**: TalkBack, VoiceOver, UIAutomator, XCUITest, and native tests can discover the
standard fixture with correct identity, reading order, names, roles, and geometry.

### Phase C - P1 actions, focus, and modal behavior

- Invoke, Toggle, SelectionItem, ExpandCollapse, Value, RangeValue, Scroll, ScrollItem,
  Realize, and window/dismiss actions.
- Native <-> XAML focus synchronization with re-entry guards.
- Modal subtree filtering, open/close notifications, and focus restoration.
- Disabled/read-only/unavailable action behavior.

**Exit**: primary user tasks are operable through both screen readers and native actions.

### Phase D - Live properties, structure, and events

- Generic property fallthrough invalidation after base routing.
- Complete producer audit for dynamic properties (including the deferred XP-02 gaps).
- Targeted value/state/text/selection/bounds updates.
- Child add/remove/reorder/virtualization events.
- Live regions, notifications, window/menu/layout/text events, coalescing, and throttling.

**Exit**: mounted nodes stay correct without leaving/re-entering focus or rebuilding the
full tree.

### Phase E - Rich controls and relationships

- Collection/grid/table/tree/tab/menu/radio metadata and hierarchy.
- MultipleView state and `ChangeView` actions.
- Range/progress/orientation semantics.
- Editable/read-only/multiline/password/rich text and supported selection behavior.
- LabeledBy, DescribedBy, ControlledPeers, FlowsTo/From, annotations, culture, required,
  validation, level/position/size, localized control/landmark types.
- Custom actions/content for representable advanced patterns.

**Exit**: every capability-matrix row has a native mapping/fallback and automated coverage.

### Phase F - Automation, performance, and release validation

- SamplesApp.UITests identity/action smoke on both platforms.
- 500-node incremental-update profiling and 1,000-item virtualization tests.
- Leak/stale-ID/window-disposal/thread-affinity tests.
- TalkBack and VoiceOver manual matrices on emulator/simulator plus representative devices.
- Appium compatibility smoke against the same native fixture without adding a project dependency.

**Exit**: all success criteria and release evidence are complete; no ignored P1 mobile
accessibility tests.

## Phase 0: Research

Complete: [research.md](./research.md).

All initial unknowns were resolved:

- active target/rendering scope;
- direct-base vs snapshot architecture;
- Android virtual-tree and iOS managed-element designs;
- native identity, geometry, focus, event, and action translations;
- exact mobile test/CI paths;
- unsupported UIA-only fallback policy.

## Phase 1: Design and Contracts

- Runtime state model: [data-model.md](./data-model.md)
- Adapter/lifecycle/action contract:
  [contracts/mobile-adapter-contract.md](./contracts/mobile-adapter-contract.md)
- Complete property/pattern/event mapping:
  [contracts/capability-matrix.md](./contracts/capability-matrix.md)
- Build/test/manual validation workflow: [quickstart.md](./quickstart.md)
- Agent context: `CLAUDE.md` points to this plan between the SPECKIT markers.

Post-design constitution re-check: **PASS**. The design remains internal, WinUI-grounded,
Skia-first, test-first, event-driven, and resource-safe.

## Implementation status

Implemented source coverage:

- shared peer-tree promotion, provider actions, property/event routing, focus recovery, and
  announcement coalescing;
- Android virtual nodes with stable weak IDs, native range/collection/text/relation metadata,
  custom fixed actions, normalized automation identity, locale spans, incremental invalidation,
  and realized-only virtualization;
- iOS stable `UIAccessibilityElement` instances with per-XamlRoot weak dispatch, live values,
  AX custom content, localized custom actions, native focus/modal handling, and incremental
  container diffs;
- platform-neutral runtime contracts for native snapshots, actions, events, focus, rich
  semantics, typed internal/unsupported fallbacks, lifecycle, performance, and complete
  capability-matrix integrity;
- SamplesApp and Uno.UITest fixtures that keep spoken names separate from machine identifiers
  and never seed secure-text values.

Intentional fallbacks:

- ownerless virtualized item peers are omitted until a real container is realized;
- parameterized move/resize/rotate/absolute-zoom operations are automation-hook operations, not
  parameterless screen-reader actions;
- UIA-only object models, synchronized input, rich text-range identity, drag/drop provider
  objects, spreadsheet/style providers, and unsupported navigation providers remain available
  through typed diagnostics rather than dead native actions;
- Android secondary windows are not supported by the current host. Root-keyed hooks are tested
  for the primary root and iOS uses a weak per-root adapter registry.

Local validation completed on Windows:

- generic Skia runtime-test application builds for `net10.0`;
- Android runtime project builds for `net10.0-android`;
- AppleUIKit runtime project compiles for `net9.0-ios18.0` / `iossimulator-x64` using the local
  iOS reference pack;
- capability-matrix tests pass on Skia Desktop;
- native Android/iOS execution and manual TalkBack/VoiceOver evidence remain CI/device work.

## Complexity Tracking

No constitution violations.

The two platform adapters are required by fundamentally different native APIs
(`ExploreByTouchHelper` vs `UIAccessibilityElement`). Shared behavior remains in existing
peer/router/base code and the narrow live-peer helper; platform files contain only native
projection and lifecycle logic.
