# Research: Mobile Accessibility and Automation

**Date**: 2026-07-10  
**Branch**: `005-mobile-a11y-automation`  
**Baseline**: `dev/doti/a11y-parity-remediation-impl` at `a496392e28`  
**Reference**: WinUI `microsoft-ui-xaml` automation peer implementation

> Evidence level: code review by inspection. The Android, iOS, shared WinUI contract,
> and mobile test infrastructure were audited independently. No implementation from this
> feature exists yet, so all platform behavior described as a gap still requires
> fails-before/passes-after runtime validation.

## 1. Current-state verdict

| Surface | Current state | Principal gap |
|---------|---------------|---------------|
| Shared Skia accessibility | `AccessibilityRouter` and `SkiaAccessibilityBase` already route common peer properties, events, focus, structure, scrolling, modal state, and announcements | Mobile hosts do not participate; the typed update switch covers only the common high-frequency subset |
| Skia Android | `UnoExploreByTouchHelper` exposes a shallow virtual tree and click activation | It is standalone, walks keyboard tab stops instead of the automation tree, receives no peer events, lacks most actions/metadata, invalidates the whole tree per render, and leaks virtual-node mappings |
| Skia iOS | Metal canvas only | No `UIAccessibilityElement` tree, no VoiceOver nodes, no identifiers, no actions, and no XCUITest/Appium visibility |
| Legacy native Android/UIKit | Partial platform-native behavior | Maintenance-only for this feature; shared changes must not regress it |
| Test infrastructure | Skia Android and Skia iOS runtime-test stages already run on every PR | No platform-native accessibility-node assertions; no TalkBack/VoiceOver automation matrix |

Key source locations:

- `src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs`
- `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`
- `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- `src/Uno.UI.Runtime.Skia.Android/Hosting/AndroidSkiaXamlRootHost.cs`
- `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/RootViewController.cs`
- `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/AppleUIKitWindowWrapper.cs`
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/`

## 2. Scope

**Decision**: Deliver full behavior for Skia-on-Android and Skia-on-iOS. Keep legacy
native Android/UIKit compiling and behavior-safe, but do not make legacy parity a release
gate. Exclude tvOS and Mac Catalyst behavior except for safe shared compilation.

**Rationale**: Win32, WASM, and macOS are Skia accessibility backends, and repository
policy makes Skia the active implementation surface. Expanding the feature into both
legacy renderers would create four independent implementations and conflict with the
maintenance-only boundary.

**Alternatives considered**:

- Implement both Skia and legacy mobile paths fully: rejected as duplicate architecture.
- Fix only Android because a partial bridge exists: rejected because iOS is completely opaque.
- Include tvOS focus-engine work: rejected as a separate platform behavior track.

## 3. Core architecture

**Decision**: Add `AndroidSkiaAccessibility` and `AppleUIKitAccessibility` as direct
`SkiaAccessibilityBase` subclasses. Keep native node properties pull-based: Android and
iOS query the live resolved `AutomationPeer` when the operating system requests a node.
Use the base class's typed updates as targeted invalidation hints, and override
`NotifyPropertyChangedEventCore` to invalidate the native node after any routed property
change, including properties not handled by the base switch.

**Rationale**:

- TalkBack calls `OnPopulateNodeForVirtualView`; VoiceOver queries properties on
  `UIAccessibilityElement`. Both APIs are naturally pull-based.
- Win32, the most complete backend, also pulls property values from the peer on demand.
- `SkiaAccessibilityBase` already owns EventsSource-aware routing, focus tracking,
  modal lifecycle, announcement debounce, scroll subscriptions, and structure callbacks.
- Generic native-node invalidation covers the long tail of automation properties without
  adding dozens of abstract `Update*` methods.

**Alternatives considered**:

- Cached immutable semantic snapshots: rejected because they duplicate native pull models,
  become stale, allocate per node, and still need live peers for actions.
- A second `MobileAccessibilityBase`: rejected because `SkiaAccessibilityBase` already
  supplies the common lifecycle and event contract.
- Two standalone adapters: rejected because they would duplicate EventsSource resolution,
  announcement throttling, focus behavior, and provider error handling.

Small shared helpers may be extracted for resolved-peer lookup, tree inclusion, and provider
action execution when both adapters would otherwise duplicate identical WinUI semantics.
They must remain internal and must not introduce another cached object model.

## 4. Automation tree and reading order

**Decision**: Build the mobile tree from the resolved automation-peer graph and WinUI
Control/Content view rules, not from keyboard tab order. `AccessibilityView.Raw` nodes are
pruned while eligible descendants are promoted. `EventsSource` is resolved before node
state or events are exposed. Assign all node identities before resolving cross-node
relationships.

`FrameworkElementAutomationPeer.GetAutomationPeerChildren` already runs in the
`Uno.UI.Skia` assembly consumed by both mobile Skia runtimes. The implementation therefore
must reuse the existing peer output rather than add runtime-project symbols to shared code.

**Rationale**: Android's current `FocusManager.GetNextTabStop` enumeration omits headings,
images, static text, non-tab-stop containers, and peer-only children. WinUI clients consume
the automation tree, not the keyboard navigation graph.

**Alternatives considered**:

- Retain tab-stop enumeration and add special cases: rejected because every new semantic
  non-control would require another hack.
- Walk the raw visual tree only: rejected because custom peers, item peers, EventsSource,
  and peer-provided children would be lost.

**Ordering**: Preserve the order returned by `peer.GetChildren()` as the default reading
order. The existing peer walk already honors `AreAutomationPeerChildrenReversed()`. Apply
platform traversal metadata only when WinUI relationships or an explicit automation order require it.

## 5. Node identity and lifecycle

**Decision**:

- Android uses monotonically allocated per-window virtual `int` IDs with
  `ConditionalWeakTable<DependencyObject, IdHolder>` for owner-to-ID lookup and
  `Dictionary<int, WeakReference<DependencyObject>>` for ID-to-owner routing.
- iOS keeps one stable managed `UnoUIAccessibilityElement` instance per realized node in
  the per-window adapter. The element stores a node ID and weak adapter reference, not a
  strong `AutomationPeer` or owner reference.
- Removed/recycled nodes are unregistered immediately; native callbacks against stale IDs
  return unavailable/false and never route to a reused peer.

**Rationale**: Android's current strong ID dictionary has an explicit leak TODO. Stable
object identity is also required for VoiceOver focus and UI automation.

**Alternatives considered**:

- Use `Visual.Handle` directly as Android's virtual ID: rejected because `nint` does not
  safely fit the Android `int` contract and stale handles may be reused.
- Recreate iOS elements on every query: rejected because VoiceOver and XCUITest need stable
  element identity and focus.
- Strong node-to-peer references: rejected because they retain unloaded pages and recycled items.

## 6. Android projection

**Decision**: Keep `UnoExploreByTouchHelper` created and installed by the render view, then
configure it from `AndroidSkiaAccessibility` once the XamlRoot owner exists. The adapter
coordinates the helper, root resolver, node registry, event routing, and provider actions.
`AndroidSkiaXamlRootHost` implements `IAccessibilityOwner`; `AndroidHost` initializes the
router; activity/window activation selects the active owner.

The helper will:

- Enumerate the resolved peer tree and perform accessible hit testing.
- Populate `AccessibilityNodeInfoCompat` from the live peer.
- Use `SetBoundsInScreen` with the render view's screen offset.
- Map `AutomationControlType` plus provider patterns to Android class names, role/state
  descriptions, actions, range info, collection info, item info, text metadata, heading,
  selected/checkable/checked state, live region, modal pane, and visibility.
- Dispatch Android standard/custom actions to the matching live provider.
- Use `InvalidateVirtualView` for property/bounds updates and `InvalidateRoot` only for
  structural changes.
- Preserve one explicit root invalidation after surface/orientation relayout so every
  native bounds rectangle is re-queried.
- Send native focus, content-change, selection, text, click, and announcement events.

**Rationale**: `ExploreByTouchHelper` is Android's established virtual-descendant API and
already supplies hover, keyboard, focus, and node-provider plumbing. Replacing it would add
risk without improving native compatibility.

**Alternatives considered**:

- Replace `ExploreByTouchHelper` with a custom `AccessibilityNodeProvider`: rejected because
  it would reimplement stable AndroidX behavior already in use.
- Keep whole-root invalidation on each render: rejected due to continuous work and stale
  semantic/event behavior.
- Convey exact heading level through tooltip text: rejected because tooltip is the wrong
  semantic. Expose `Heading=true`; preserve the numeric level in the capability/debug
  channel and use a localized role/state description only if platform guidance supports it.

**Availability rule**: The node provider remains available whenever Uno accessibility is
configured and the host is attached. Do not gate tree creation on TalkBack or touch
exploration being active, because UIAutomator and other services query the same tree.
`AccessibilityManager` state may suppress unsolicited announcements/events, not node
materialization.

## 7. iOS projection

**Decision**: Implement a pure managed `UIAccessibilityElement` tree in C#; do not add an
Objective-C framework. `RootViewController` implements `IAccessibilityOwner` and owns an
`AppleUIKitAccessibility` instance. The existing Skia/Metal view is the accessibility
container through its `AccessibilityElements` property.

Each `UnoUIAccessibilityElement` will:

- Pull label, hint, value, identifier, traits, custom content, and available actions from
  the live resolved peer.
- Use `AccessibilityFrameInContainerSpace` in logical UIKit points.
- Override activation, increment, decrement, scroll/escape behavior where UIKit exposes a
  standard method.
- Expose additional provider operations as localized `UIAccessibilityCustomAction` entries.
- Route every action through the adapter to the UI thread and the current live peer.

The adapter posts layout, screen, and announcement notifications, updates the element array
incrementally, tracks modal scope, and preserves stable element objects across property changes.

**Rationale**: .NET iOS fully binds `UIAccessibilityElement`; the AppleUIKit Skia project
uses managed UIKit bindings throughout and has no native framework build. Container-space
frames avoid manual scale, safe-area, rotation, and split-screen conversions.

**Alternatives considered**:

- Mirror the macOS Objective-C bridge: rejected as unnecessary build and lifetime complexity.
- Subclass the Metal view only to implement container protocol methods: rejected because
  `UIView.AccessibilityElements` already supplies the required managed container surface.
- Use screen-space frames with manual scale multiplication: rejected in favor of UIKit's
  container-space conversion.

**Availability rule**: Do not gate the tree on `UIAccessibility.IsVoiceOverRunning`;
XCUITest and Appium need the same elements when VoiceOver is off.

## 8. Roles, names, values, and relationships

**Decision**: Treat WinUI peer methods and provider patterns as the source. Do not use ARIA
role strings as the mobile contract.

- Android maps control type plus patterns to class name, role/state description, and node flags.
- iOS maps control type plus patterns to traits, container type, value, and custom actions.
- Accessible name and AutomationId remain separate on both platforms.
- Android exposes AutomationId as a normalized `ViewIdResourceName` and, where available,
  a stable unique ID; iOS uses `AccessibilityIdentifier`.
- Android uses direct labeled-by/traversal APIs where available.
- iOS resolves the accessible name/hint from peer relationships and records other
  representable metadata as custom content or traversal order.
- Unsupported UIA-only relationships remain in the internal peer contract and are marked
  explicitly in the capability matrix rather than silently claimed as native support.

**Rationale**: `AriaMapper` is web-oriented. Native platforms infer behavior from their own
class/trait/action models, and using AutomationId as spoken text would repeat an existing
legacy Android defect.

**Alternatives considered**:

- Reuse `AriaMapper.GetAriaRole` directly: rejected because ARIA names do not map 1:1 to
  Android classes or UIKit traits.
- Append developer IDs or relationship metadata to spoken labels: rejected because it
  degrades the user-facing name.

## 9. Provider actions and error behavior

**Decision**: Normalize native actions to internal operations, then execute the live
provider on the Uno UI thread. Reuse `AutomationPeer.InvokeAutomationPeer()` for its
supported activation path and a shared internal provider-action helper for Expand/Collapse,
SetValue, range adjustment, scrolling, selection, realization, window, text, and other
patterns.

Disabled/read-only failures preserve the WinUI provider exception semantics internally and
translate to native action failure (`false`/no completion event) without crashing the
accessibility service. State-changing actions invalidate the node and emit the appropriate
native event.

**Alternatives considered**:

- Treat every native action as click: rejected because it drops arguments, direction, range,
  text, expand/collapse, and scroll semantics.
- Copy UIA HRESULTs into mobile APIs: rejected because they are COM-specific.

## 10. Property changes and automation events

**Decision**:

1. Pull complete state whenever the native platform queries a node.
2. Let `SkiaAccessibilityBase` route its known high-frequency properties.
3. In each mobile adapter, override `NotifyPropertyChangedEventCore`, call `base`, then
   invalidate the resolved node for every property so unhandled properties are re-queried.
4. Map each `AutomationEvents` value to the closest native event or a documented no-native-event fallback.
5. Preserve producer correctness: every dynamic property in the capability matrix must
   identify the existing raise source or add a WinUI-aligned property/peer invalidation producer.

**Rationale**: The base's typed switch intentionally is not an exhaustive property store.
Generic invalidation fits mobile pull APIs and avoids dozens of platform update methods.
The deferred `004-a11y-parity-remediation` XP-02 property-producer gaps therefore become
an explicit dependency of complete mobile live behavior.

**Event policy**:

- Focus -> native accessibility focus event/notification.
- Structure -> subtree/root content change (Android) or layout change (iOS).
- Notification/live region -> debounced native announcement.
- Text/edit -> native text/value/selection change.
- Selection/invoke/toggle -> state invalidation plus native action event.
- Window/menu/layout -> native window or screen/layout change.
- UIA-only synchronized-input/object-model details -> no fabricated event; retain diagnostics
  and the capability-matrix rationale.

## 11. Focus, popups, and windows

**Decision**: Keep XAML keyboard focus and native accessibility focus as related but
distinct state. Native focus requests move XAML focus only for elements whose WinUI peer
is keyboard-focusable and whose action requires it. Programmatic XAML focus can request
native focus without re-entrant loops.

Use the existing base modal lifecycle to exclude background nodes. Opening a modal posts
the platform's window/screen-change signal and focuses a valid peer; closing restores the
previous valid native focus target. Disabling an element updates state/actions but does not
force XAML focus away, matching WinUI; removal, hiding, recycling, or window deactivation
does recover native accessibility focus.

Each XamlRoot/window owns an independent adapter and node registry. Never resolve roots via
`Window.Current`.

## 12. Performance, threading, and security

**Decision**:

- Build/query nodes lazily and update incrementally; no full-tree rebuild per render frame.
- Marshal all peer access and native-node mutation to the UI/main thread.
- Coalesce redundant node invalidations and announcements within one dispatcher cycle.
- Keep only realized or accessibility-required virtualized nodes.
- Prune all registries on removal and dispose all window hooks/listeners.
- Never expose password values in node text, identifiers, diagnostics, events, or test dumps.

**Rationale**: Android currently invalidates the entire tree per render and retains every
virtual node. Both are direct mobile performance and memory risks.

**Alternatives considered**:

- Poll the visual tree for changes: rejected in favor of existing structure/property hooks.
- Cache all peer properties: rejected due to stale values and allocation pressure.

## 13. Testing and CI

**Decision**: Reuse existing projects and dependencies; do not create a new Appium project.

Test layers:

1. Shared peer/tree/action contract tests in
   `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/`.
2. Skia Android tests query the real `AccessibilityNodeInfoCompat` returned by the existing
   node provider using a small internal test accessor for stable virtual IDs.
3. Skia iOS tests query the real managed `UIAccessibilityElement` objects owned by the
   adapter/container using internal test accessors, not reflection.
4. Existing `SamplesApp.UITests` verifies AutomationId lookup and representative actions
   through the same native trees used by UI automation clients.
5. Manual TalkBack and VoiceOver matrices verify announcement wording, rotor/navigation,
   focus, actions, secure text, and dynamic updates.
6. Dedicated lifecycle/performance tests cover virtual-ID cleanup, stale callbacks,
   1,000-item virtualization, 500-node incremental updates, and thread affinity.

Existing PR stages:

- `runtime_tests_skia_android` ->
  `build/test-scripts/android-run-skia-runtime-tests.sh` (Android API 34, five groups).
- `runtime_tests_skia_ios` ->
  `build/test-scripts/ios-uitest-run.sh` with `UITEST_VARIANT=skia` (iOS 17.5 simulator,
  four groups).

**Alternatives considered**:

- New Appium test project and CI stage: rejected because existing runtime tests and
  `SamplesApp.UITests` can prove the native tree and automation identity without new packages.
- Peer-only iOS tests: rejected because they would compile while the Metal canvas remained opaque.
- Reflection-heavy access to adapters: rejected; add narrow internal test surfaces.

## 14. WinUI alignment boundary

**Decision**: Preserve WinUI behavior for peer creation, Control/Content/Raw views,
EventsSource, provider selection, disabled-action errors, automatic property tracking,
focus, structure, and notification semantics. Translate only the final UIA transport into
Android and UIKit concepts.

Relevant WinUI sources:

- `src/dxaml/xcp/core/inc/AutomationPeer.h`
- `src/dxaml/xcp/core/core/elements/AutomationPeer.cpp`
- `src/dxaml/xcp/core/core/elements/FrameworkElementAutomationPeer.cpp`
- `src/dxaml/xcp/core/native/text/Controls/TextBoxAutomationPeer.cpp`
- UIA bridge sources under `src/dxaml/xcp/win/`

UIA COM providers, HRESULT transport, runtime IDs, and UIA-only events are reference
semantics, not mobile implementation APIs.

## 15. Resolved unknowns

| Unknown | Resolution |
|---------|------------|
| Mobile target scope | Skia Android + Skia iOS; legacy renderers no-regression only |
| Shared architecture | Direct `SkiaAccessibilityBase` subclasses; pull complete peer state, push invalidation hints |
| Android virtual tree | Keep/refactor `ExploreByTouchHelper`; replace tab-order enumeration with peer tree |
| iOS native layer | Pure managed `UIAccessibilityElement` objects in the existing Metal view container |
| iOS coordinates | `AccessibilityFrameInContainerSpace` |
| Tree availability | Query-driven; not gated on TalkBack/VoiceOver active state |
| Identity | Per-window monotonic Android IDs + weak reverse map; stable managed iOS element objects |
| Role source | `AutomationControlType` + provider patterns, not ARIA strings |
| Live updates | Base typed routing plus generic node invalidation and producer audit |
| AutomationId | Android native test ID/resource identity; iOS `AccessibilityIdentifier`; never spoken name |
| Test infrastructure | Existing mobile runtime-test stages and `SamplesApp.UITests`; no new Appium project |
| Remaining unsupported concepts | Explicit capability-matrix fallback, never silent or falsely claimed |
