# Data Model: WASM Accessibility — Advanced Features

**Branch**: `002-wasm-a11y-advanced` | **Date**: 2026-02-11

## Entities

### VirtualizedSemanticRegion

Tracks the accessibility state of a virtualized container (ItemsRepeater, ListView, GridView).

| Field | Type | Description |
|-------|------|-------------|
| ContainerHandle | IntPtr | Handle to the virtualizing container's Visual |
| ContainerPeer | AutomationPeer | Automation peer of the container (RepeaterAutomationPeer or ListViewBaseAutomationPeer) |
| TotalItemCount | int | Total items in the data source |
| RealizedHandles | Dictionary<int, IntPtr> | Map of data index → semantic element handle for realized items |
| ViewportBounds | Rect | Current visible viewport rectangle |
| IsFocusPinned | bool | Whether a focused item is preventing recycling |
| PinnedIndex | int? | Data index of the pinned (focused) item, if any |

**State transitions**:
- `Inactive` → `Active` (container registered with accessibility layer)
- `Active` → `Updating` (ElementPrepared/Clearing events processing)
- `Updating` → `Active` (DOM mutations flushed via requestAnimationFrame)
- `Active` → `Inactive` (container removed from tree or accessibility disabled)

**Validation rules**:
- `RealizedHandles.Count` MUST match the number of semantic DOM elements for this container
- `TotalItemCount` MUST be updated when `ItemsSourceView.Count` changes
- `PinnedIndex` MUST be cleared when focus leaves the container

---

### LiveRegionManager

Coordinates live region announcements with two-tier rate limiting.

| Field | Type | Description |
|-------|------|-------------|
| PoliteRegionElement | DOM Element | Reference to the aria-live="polite" div |
| AssertiveRegionElement | DOM Element | Reference to the aria-live="assertive" div |
| PendingPoliteContent | string? | Buffered polite announcement content |
| PendingAssertiveContent | string? | Buffered assertive announcement content |
| PoliteDebounceTimer | Timer | 100ms debounce for polite bursts |
| AssertiveDebounceTimer | Timer | 100ms debounce for assertive bursts |
| PoliteThrottleTimestamp | long | Last polite announcement time (for 500ms sustained throttle) |
| AssertiveThrottleTimestamp | long | Last assertive announcement time (for 200ms sustained throttle) |

**State transitions**:
- `Idle` → `Debouncing` (first event received, 100ms timer started)
- `Debouncing` → `Debouncing` (new event resets timer, replaces pending content)
- `Debouncing` → `Announcing` (timer expires, check throttle)
- `Announcing` → `Throttled` (content pushed to DOM, throttle timestamp set)
- `Throttled` → `Idle` (throttle window expires: 500ms polite, 200ms assertive)

**Validation rules**:
- PoliteRegionElement and AssertiveRegionElement MUST exist in DOM after accessibility activation
- Only one pending content per urgency level (latest wins)
- Throttle check: `now - lastTimestamp >= throttleInterval` before announcing

---

### FocusSynchronizer

Bidirectional bridge between XAML FocusManager and browser document.activeElement.

| Field | Type | Description |
|-------|------|-------------|
| CurrentFocusedHandle | IntPtr? | Handle of the currently-focused semantic element |
| PreviousFocusedHandle | IntPtr? | Handle of the previously-focused semantic element |
| IsSyncing | bool | Guard flag to prevent infinite XAML↔browser focus loops |
| CorrelationId | Guid | Tracks the current focus change operation |
| PendingFocusHandle | IntPtr? | Handle queued for next animation frame (debounce) |
| RafId | int | requestAnimationFrame ID for pending focus (0 = none) |

**State transitions**:
- `Idle` → `SyncingToDOM` (FocusManager.GotFocus received, IsSyncing=true)
- `SyncingToDOM` → `Idle` (DOM element.focus() called, IsSyncing=false)
- `Idle` → `SyncingToXAML` (semantic element focus event received, IsSyncing=true)
- `SyncingToXAML` → `Idle` (Control.Focus() called, IsSyncing=false)

**Validation rules**:
- IsSyncing MUST be checked before both directions to prevent infinite loops
- PendingFocusHandle replaces previous pending (only latest focus target applies)
- When syncing to DOM: set tabindex="0" on new, tabindex="-1" on previous

---

### ModalFocusScope

Represents a focus trap boundary for a modal dialog.

| Field | Type | Description |
|-------|------|-------------|
| ModalHandle | IntPtr | Handle to the modal container element |
| TriggerHandle | IntPtr | Handle of the element that opened the modal (for focus restore) |
| FocusableChildren | List<IntPtr> | Ordered list of focusable semantic element handles within the modal |
| ParentScope | ModalFocusScope? | Reference to outer scope if nested modals |
| HiddenElements | List<IntPtr> | Handles of background elements that were set to aria-hidden="true" |

**State transitions**:
- `Inactive` → `Active` (ContentDialog.Opened fires, background hidden, focus trapped)
- `Active` → `Active` (nested modal opens, becomes inner scope)
- `Active` → `Restoring` (dialog closes, aria-hidden removed, focus returning to trigger)
- `Restoring` → `Inactive` (trigger element focused, scope disposed)

**Validation rules**:
- Only one ModalFocusScope is the "active" trap at a time (innermost in nested case)
- HiddenElements MUST be restored to their original state when scope deactivates
- FocusableChildren MUST be refreshed when modal content changes (dynamic buttons)
- Tab wrapping: from last → first, Shift+Tab from first → last

---

## Relationships

```
WebAssemblyAccessibility (coordinator)
├── VirtualizedSemanticRegion[] (one per virtualizing container)
├── LiveRegionManager (singleton)
├── FocusSynchronizer (singleton)
└── ModalFocusScope? (active trap, nullable, linked-list for nesting)
```

- `WebAssemblyAccessibility` owns all entities and coordinates their lifecycle
- `VirtualizedSemanticRegion` subscribes to `ItemsRepeater.ElementPrepared`/`ElementClearing` or equivalent ListViewBase events
- `LiveRegionManager` receives events from `IUnoAccessibility.OnAutomationEvent(peer, LiveRegionChanged)`
- `FocusSynchronizer` subscribes to `FocusManager.GotFocus` and semantic element focus events
- `ModalFocusScope` subscribes to `ContentDialog.Opened`/`Closed` and manages DOM aria-hidden state
