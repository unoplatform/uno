# Research: WASM Accessibility — Advanced Features

**Date**: 2026-02-11 | **Branch**: `002-wasm-a11y-advanced`

## R1: WinUI AutomationPeer.RaiseAutomationEvent Dispatch Chain

**Decision**: Add an `IUnoAccessibility.OnAutomationEvent(peer, eventId)` extensibility callback in the shared `AutomationPeer.RaiseAutomationEvent`, mirroring WinUI's `CCoreServices → CUIAWindow` delegation pattern.

**Rationale**: WinUI uses a three-layer delegation: `CAutomationPeer::RaiseAutomationEvent` → `CCoreServices::UIARaiseAutomationEvent` → `CUIAWindow::UIARaiseAutomationEvent`. The Uno equivalent should use the existing `ApiExtensibility` / `IUnoAccessibility` interface pattern already established in 001-wasm-accessibility.

**Alternatives considered**:
- Subclass AutomationPeer per-platform (rejected: too invasive, all platforms would need custom peers)
- Event-based dispatch via static event (rejected: no way to check listener existence without coupling)

**Key WinUI sources** (D:\Work\microsoft-ui-xaml2):
- `src/dxaml/xcp/core/core/elements/AutomationPeer.cpp:1528-1534` — `RaiseAutomationEvent` with `ListenerExists()` guard
- `src/dxaml/xcp/core/core/elements/AutomationPeer.cpp:1517-1526` — `ListenerExists` checks `UIAClientsAreListening`
- `src/dxaml/xcp/core/dll/xcpcore.cpp:5588-5597` — `CCoreServices::UIARaiseAutomationEvent` delegates to `CUIAWindow`
- `src/dxaml/xcp/win/shared/UIAWindow.cpp:1664-1688` — `CUIAWindow::UIARaiseAutomationEvent` creates provider, calls `UiaRaiseAutomationEvent`
- `src/dxaml/xcp/win/shared/UIAWindow.cpp:1336-1339` — Listener counting: `m_cAdviseEventLiveRegionChanged++`

**WinUI LiveRegionChanged call chain**:
```
AutomationPeer.RaiseAutomationEvent(LiveRegionChanged)
  → ListenerExists(AELiveRegionChanged)
    → CCoreServices::UIAClientsAreListening → CUIAWindow::UIAClientsAreListening
      → checks m_cAdviseEventLiveRegionChanged counter
  → CCoreServices::UIARaiseAutomationEvent(pAP, AELiveRegionChanged)
    → CUIAWindow::UIARaiseAutomationEvent
      → CreateProviderForAP(pAP) → CUIAWrapper
      → ConvertEnumToId(AELiveRegionChanged) → LiveRegionChanged_Event EVENTID
      → UiaRaiseAutomationEvent(provider, LiveRegionChanged_Event)
```

**GetLiveSetting**: `CAutomationPeer::GetLiveSetting` reads `APLiveSettingProperty` from managed code → returns `LiveSetting` enum (Off=0, Polite=1, Assertive=2). UIA consumers call `GetPropertyValue(Name_Property)` to get the announced text.

---

## R2: Uno FocusManager Events for Bidirectional Sync

**Decision**: Subscribe to `FocusManager.GotFocus` static event for XAML→browser direction. The existing `FocusNative()` call in `FocusManager.skia.cs` already handles this partially. For browser→XAML, the existing `OnFocus` JSExport callback in `WebAssemblyAccessibility.cs` provides the reverse direction.

**Rationale**: The infrastructure for bidirectional focus sync is already partially implemented in the 001 spec. The remaining work is:
1. Implementing roving tabindex management (tabindex="0" on focused, "-1" on others)
2. Adding debounce to prevent rapid focus storms
3. Adding the is-syncing guard to prevent infinite loops

**Alternatives considered**:
- Per-control GotFocus routed events (rejected: requires attaching to every element, doesn't scale)
- Synchronous GettingFocus/LosingFocus (rejected: async GotFocus is sufficient and less disruptive)

**Key Uno sources**:
- `src/Uno.UI/UI/Xaml/Input/FocusManager.cs:29-47` — Static `GotFocus`/`LostFocus` events (async via CoreDispatcher)
- `src/Uno.UI/UI/Xaml/Input/FocusManager.mux.cs:1679-1949` — `UpdateFocus()` central method: sets `_focusedElement`, calls `FocusNative()`, fires async events
- `src/Uno.UI/UI/Xaml/Input/FocusManager.skia.cs` — `FocusNative()` already calls `NativeMethods.FocusSemanticElement(control.Visual.Handle)` when `OperatingSystem.IsBrowser()`
- `src/Uno.UI/UI/Xaml/Input/FocusManagerGotFocusEventArgs.cs` — Contains `NewFocusedElement` and `CorrelationId` (GUID)

**Event firing order**: GettingFocus (sync) → LosingFocus (sync) → `_focusedElement` updated → `FocusNative()` called → LostFocus (async) → GotFocus (async)

**Existing XAML→Browser sync**: `FocusManager.skia.cs` calls `Accessibility.focusSemanticElement(handle)` in TypeScript
**Existing Browser→XAML sync**: `WebAssemblyAccessibility.OnFocus(IntPtr handle)` JSExport calls `Control.Focus(FocusState.Keyboard)`

---

## R3: Virtualization Infrastructure for Accessibility

**Decision**: Use `ItemsRepeater.ElementPrepared`/`ElementClearing` events to create/remove semantic DOM elements. For `ListViewBase` (ListView/GridView), use the same VirtualizationInfo approach since ListViewBase internally uses similar virtualizing panel infrastructure.

**Rationale**: `RepeaterAutomationPeer.GetChildrenCore()` already filters unrealized peers, proving the pattern works. The accessibility layer should hook into the same lifecycle events to keep the semantic DOM in sync.

**Alternatives considered**:
- Polling via timer to check realized elements (rejected: unnecessary overhead, events are available)
- Hooking EffectiveViewportChanged directly (rejected: too low-level, ElementPrepared/Clearing is the right abstraction)

**Key Uno sources**:
- `src/Uno.UI/UI/Xaml/Controls/Repeater/ItemsRepeater.cs:544` — `ElementPrepared` event (UIElement + index)
- `src/Uno.UI/UI/Xaml/Controls/Repeater/ItemsRepeater.cs:563` — `ElementClearing` event (UIElement)
- `src/Uno.UI/UI/Xaml/Controls/Repeater/VirtualizationInfo.cs` — `IsRealized`, `Index`, `IsPinned`, `ElementOwner` enum
- `src/Uno.UI/UI/Xaml/Controls/Repeater/ViewManager.cs:909-966` — `UpdateFocusedElement()` pins/unpins focused items
- `src/Uno.UI/UI/Xaml/Controls/Repeater/RepeaterAutomationPeer.cs` — `GetChildrenCore()` filters by `VirtualizationInfo.IsRealized`, sorts by index
- `src/Uno.UI/UI/Xaml/Controls/Repeater/ViewportManagerWithPlatformFeatures.cs` — Subscribes to `EffectiveViewportChanged`, tracks `m_visibleWindow` and realization window (visible + cache buffer)
- `src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs` — `PropagateEffectiveViewportChange()` fires viewport events

**Element lifecycle**:
- `ElementFactory` → `Layout` (realized, `ElementPrepared` fires) → `ElementFactory` (unrealized, `ElementClearing` fires)
- `Layout` → `PinnedPool` (cleared but focused, stays realized) → `ElementFactory` (when unpinned)
- `ViewManager.UpdateFocusedElement()` auto-pins focused elements

**ListViewBase**: Uses `VirtualizingPanel` internally. `ListViewBaseAutomationPeer` is currently a NotImplemented stub — may need similar filtering to `RepeaterAutomationPeer`.

---

## R4: ContentDialog Modal Lifecycle for Focus Trapping

**Decision**: Hook into `ContentDialog.Opened`/`Closed`/`Closing` events to manage focus trap boundaries. Use the `PopupRoot` to detect active modals. Build DOM-side focus trapping with `aria-hidden` and Tab cycling.

**Rationale**: ContentDialog uses `Popup` internally, and Popup lifecycle is well-defined. The accessibility layer can detect modal state by checking if a ContentDialog-owned Popup is open and not light-dismiss-enabled.

**Alternatives considered**:
- Hooking Popup.IsOpen changes globally (rejected: too broad, includes non-modal popups like tooltips)
- Using FocusManager's existing focus scope (rejected: no existing modal focus trapping exists)

**Key Uno sources**:
- `src/Uno.UI/UI/Xaml/Controls/ContentDialog/ContentDialog.cs` — `ShowAsync()` opens via `Popup.IsOpen = true`
- `src/Uno.UI/UI/Xaml/Controls/ContentDialog/ContentDialog.cs` — `Opened`, `Closed`, `Closing` events
- `src/Uno.UI/UI/Xaml/Controls/Primitives/Popup.cs` — `IsOpen`, `IsLightDismissEnabled` properties
- `src/Uno.UI/UI/Xaml/Controls/Primitives/PopupRoot.cs` — Manages open popups in visual tree
- `src/Uno.UI/UI/Xaml/VisualTree.cs` — `PopupRoot` property, `GetPopupRootForElement()` helper
- `src/Uno.UI/UI/Xaml/VisualTree.cs` — `GetFocusManagerForElement()` helper

**ContentDialog lifecycle**:
1. `ShowAsync()` → opens Popup → `Opened` event fires
2. Dialog is active → focus managed internally by ContentDialog
3. User closes → `Closing` event (cancellable) → `Closed` event → Popup closes

**No existing focus trapping**: Neither ContentDialog nor Flyout have accessibility focus trapping for screen readers. The DOM-side trap (aria-hidden on background, Tab wrapping) must be built entirely.

**Flyout modal detection**: `Flyout.IsLightDismissEnabled = false` indicates modal-like behavior. However, standard Flyouts are not truly modal in WinUI (no focus trap). The spec should only trap focus for ContentDialog and explicitly modal overlays.

---

## R5: Existing 001-wasm-accessibility Infrastructure

**Decision**: Build on the existing architecture without restructuring. Extend `WebAssemblyAccessibility` with new capabilities, add new C# and TypeScript modules for distinct concerns.

**Key existing infrastructure**:
- `WebAssemblyAccessibility.cs` — Main coordinator, `IUnoAccessibility` + `IAutomationPeerListener`, 100ms debounce timer, JSImport/JSExport interop
- `Accessibility.ts` — Enable accessibility button, semantic container, `focusSemanticElement()`, `updateAriaChecked()`, announcements
- `SemanticElements.ts` — Type-specific element factories, `applyCommonStyles()`, `getSemanticsRoot()`
- `AriaMapper.cs` — `GetSemanticElementType()`, `GetAriaAttributes()`, `GetPatternCapabilities()`
- `SemanticElementFactory.cs` — Dispatches to NativeMethods per control type
- `AccessibilityDebugger.cs` — Debug overlay with logging
