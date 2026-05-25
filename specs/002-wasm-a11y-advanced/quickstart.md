# Quickstart: WASM Accessibility — Advanced Features

**Branch**: `002-wasm-a11y-advanced` | **Depends on**: `001-wasm-accessibility`

## Overview

This spec extends the base WASM accessibility layer with five major capabilities:

1. **Virtualized list/grid accessibility** — Semantic elements for realized items only
2. **Live region events** — RaiseAutomationEvent(LiveRegionChanged) wired to aria-live
3. **Focus synchronization** — Bidirectional XAML ↔ browser focus sync
4. **Modal focus trapping** — ContentDialog focus trap with aria-hidden
5. **Focus recovery** — Graceful handling of disabled/removed focused elements

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Uno.UI (shared, cross-platform)                        │
│  ┌─────────────────────────────────────────────────┐    │
│  │ AutomationPeer.RaiseAutomationEvent             │    │
│  │   → IUnoAccessibility.OnAutomationEvent (NEW)   │    │
│  └─────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────┐    │
│  │ FocusManager.GotFocus / LostFocus               │    │
│  │   → FocusNative() [already calls JS on WASM]    │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│  Uno.UI.Runtime.Skia.WebAssembly.Browser                │
│  ┌─────────────────────┐  ┌──────────────────────────┐  │
│  │ WebAssemblyAccessi- │  │ VirtualizedSemantic-     │  │
│  │ bility.cs           │  │ Region.cs                │  │
│  │ (coordinator)       │  │ (realize/unrealize)      │  │
│  ├─────────────────────┤  ├──────────────────────────┤  │
│  │ LiveRegionManager   │  │ FocusSynchronizer.cs     │  │
│  │ .cs (two-tier       │  │ (bidirectional bridge)   │  │
│  │ rate limiter)       │  ├──────────────────────────┤  │
│  ├─────────────────────┤  │ ModalFocusScope.cs       │  │
│  │                     │  │ (focus trap manager)     │  │
│  └─────────────────────┘  └──────────────────────────┘  │
│              │ JSImport/JSExport │                       │
└──────────────┼──────────────────┼───────────────────────┘
               ▼                  ▼
┌─────────────────────────────────────────────────────────┐
│  TypeScript (Browser DOM)                               │
│  ┌─────────────────────┐  ┌──────────────────────────┐  │
│  │ Accessibility.ts    │  │ SemanticElements.ts      │  │
│  │ (extended)          │  │ (extended)               │  │
│  ├─────────────────────┤  ├──────────────────────────┤  │
│  │ LiveRegion.ts (NEW) │  │ FocusTrap.ts (NEW)       │  │
│  └─────────────────────┘  └──────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────┐
│  Semantic DOM Overlay (transparent, over Skia canvas)    │
│  ┌──────────┐ ┌──────────────┐ ┌──────────────────────┐ │
│  │ listbox  │ │ aria-live    │ │ role="dialog"         │ │
│  │ [option] │ │ [polite]     │ │ [trapped focus]       │ │
│  │ [option] │ │ [assertive]  │ │   [button] [button]   │ │
│  └──────────┘ └──────────────┘ └──────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Data Flow: Virtualized List

```
1. ItemsRepeater realizes item at index 42
   → ElementPrepared(element, index=42) event fires

2. VirtualizedSemanticRegion receives event
   → Gets AutomationPeer for element
   → Calls AriaMapper.GetAriaAttributes(peer)
   → Calls NativeMethods.AddVirtualizedItem(
       containerHandle, itemHandle, index=42,
       totalCount=1000, x, y, w, h, "option", "Item 42")

3. TypeScript creates <div role="option" aria-posinset="43" aria-setsize="1000">
   → Appended inside container's listbox element
   → Batched via requestAnimationFrame

4. User scrolls, item 42 scrolls out of view
   → ElementClearing(element) event fires
   → NativeMethods.RemoveVirtualizedItem(itemHandle)
   → TypeScript removes the DOM element
```

## Data Flow: Live Region Event

```
1. Developer calls: peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)

2. AutomationPeer.RaiseAutomationEvent (shared Uno.UI)
   → NEW: calls IUnoAccessibility.OnAutomationEvent(peer, LiveRegionChanged)

3. WebAssemblyAccessibility.OnAutomationEvent
   → Gets peer.GetLiveSetting() → Polite (1)
   → Gets peer.GetName() → "Order submitted successfully"
   → Passes to LiveRegionManager

4. LiveRegionManager applies two-tier rate limiting:
   a. 100ms debounce: if another event arrives within 100ms, replaces content
   b. 500ms sustained throttle: if last polite was <500ms ago, queues

5. When rate limit allows:
   → NativeMethods.UpdateLiveRegionContent(handle, "Order submitted successfully", 1)
   → TypeScript updates aria-live="polite" div textContent
   → Screen reader announces when current speech ends
```

## Data Flow: Focus Sync

```
XAML → Browser direction:
1. User presses Tab → FocusManager moves focus to Button2
2. FocusManager.UpdateFocus() sets _focusedElement = Button2
3. FocusNative() calls Accessibility.focusSemanticElement(button2.Handle)
4. TypeScript: element.focus(), sets tabindex="0", previous gets tabindex="-1"

Browser → XAML direction:
1. Screen reader user navigates to Button3's semantic element
2. TypeScript: element focus event fires → calls managed OnFocus(handle)
3. C# OnFocus: checks IsSyncing guard → sets IsSyncing=true
4. Finds control from handle → calls control.Focus(FocusState.Keyboard)
5. FocusManager fires GotFocus → FocusNative() skipped (IsSyncing=true)
6. IsSyncing=false
```

## Data Flow: Modal Focus Trap

```
1. ContentDialog.ShowAsync() → Opened event fires

2. ModalFocusScope created:
   → Saves trigger element handle
   → Enumerates focusable children in dialog
   → Calls NativeMethods.ActivateFocusTrap(modalHandle, triggerHandle, focusables)

3. TypeScript:
   → Sets aria-hidden="true" on all elements outside dialog
   → Saves original tabindex values
   → Sets tabindex="-1" on all background focusable elements
   → Adds keydown listener for Tab wrapping

4. User presses Tab on last button in dialog:
   → FocusTrap.handleTrapTab(handle, shiftKey=false) → wraps to first button

5. Dialog closes → Closed event fires:
   → ModalFocusScope.Deactivate()
   → NativeMethods.DeactivateFocusTrap(modalHandle)
   → TypeScript restores aria-hidden, tabindex values
   → Focus returned to trigger element
```

## Key Integration Points

| Component | Hook | Purpose |
|-----------|------|---------|
| AutomationPeer (Uno.UI) | `RaiseAutomationEvent` | Add `IUnoAccessibility.OnAutomationEvent` callback |
| FocusManager (Uno.UI) | `FocusNative()` in skia.cs | Already calls JS — extend with roving tabindex |
| ItemsRepeater | `ElementPrepared`/`ElementClearing` events | Create/remove semantic elements |
| ListViewBase | VirtualizingPanel lifecycle | Same pattern as ItemsRepeater |
| ContentDialog | `Opened`/`Closed` events | Activate/deactivate focus trap |
| WebAssemblyAccessibility | Coordinator | Routes events to managers |

## Build & Test

```bash
# Build (from src/)
dotnet build Uno.UI-Wasm-only.slnf --no-restore

# Run runtime tests headlessly
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml

# Run axe-core (after deploying WASM app)
# npx axe-core-cli http://localhost:5000 --tags wcag2aa
```
