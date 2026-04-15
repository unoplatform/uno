# Phase 1 Data Model: Multi-Window Accessibility

**Feature**: 001-multi-window-a11y
**Date**: 2026-04-14

This feature does not introduce persistent data entities. It restructures in-process state previously held as singletons into per-window scope, coordinated by a process-wide router. The "data model" is therefore a state-and-lifecycle model for those runtime entities.

---

## Entities

### 1. `AccessibilityRouter` (process-wide)

**Purpose**: Own the framework's single-slot accessibility registration points and dispatch each incoming callback to the correct per-window accessibility instance. Track the currently active window for source-less announcements.

**State**:

| Field | Type | Lifetime | Notes |
|-------|------|----------|-------|
| `_activeOwner` | `IAccessibilityOwner?` | Process | Sticky across deactivation; updated on platform activation signals. |
| (static slot registrations) | — | Process | `AutomationPeer.AutomationPeerListener`, `AccessibilityAnnouncer.AccessibilityImpl`, `UIElementAccessibilityHelper.ExternalOn*`, `VisualAccessibilityHelper.ExternalOn*` all point to router static methods. |

**Lifecycle**:

- Static — initialized on first access (e.g., at host startup via `Win32Host` / `MacSkiaHost` static ctor).
- Never torn down during process lifetime.

**Operations**:

- `Resolve(AutomationPeer peer) → SkiaAccessibilityBase?` — peer → owner → XamlRoot → host → instance.
- `Resolve(UIElement element) → SkiaAccessibilityBase?` — same, starting from element.
- `SetActive(IAccessibilityOwner owner)` — called by wrappers on activation signal.
- `NotifyDisposed(IAccessibilityOwner owner)` — called by wrappers during disposal; if owner was active, picks any live fallback.
- `TryGetActive(out SkiaAccessibilityBase instance) → bool` — used by source-less announcement path.

**Invariants**:

- At most one `_activeOwner` at any time.
- All live `IAccessibilityOwner` instances are reachable via `XamlRootMap` (no parallel registry).
- After `NotifyDisposed` of the active owner, either another live owner becomes active or `_activeOwner` is null.

---

### 2. `IAccessibilityOwner` (interface)

**Purpose**: A minimal capability interface exposed by the per-host window wrapper so the router can reach its accessibility instance. Implemented by `Win32WindowWrapper` and (in PR 2) `MacOSWindowHost`.

**Shape**:

```csharp
internal interface IAccessibilityOwner
{
    SkiaAccessibilityBase? Accessibility { get; }
}
```

**Invariants**:

- `Accessibility` is non-null after the wrapper's accessibility-initialization step has run.
- `Accessibility` becomes null (or the instance becomes `IsAccessibilityEnabled == false` and `IsDisposed == true`) when the wrapper begins its window-destroy sequence.

---

### 3. `SkiaAccessibilityBase` (per-window abstract base)

**Purpose**: Shared per-window accessibility state and routing for property changes, automation events, and announcements.

**State (per instance, per window)**:

| Field | Type | Notes |
|-------|------|-------|
| `_trackedFocusedElement` | `UIElement?` | Focus-recovery tracking for this window's tree. |
| `_politeDebounceTimer` | `Timer?` | Polite-announcement debouncer. |
| `_assertiveDebounceTimer` | `Timer?` | Assertive-announcement debouncer. |
| `_pendingPoliteContent` | `string?` | Pending polite announcement text. |
| `_pendingAssertiveContent` | `string?` | Pending assertive announcement text. |
| `_lastAnnouncedPoliteContent` | `string?` | Dedup state for polite announcements. |
| `_lastAnnouncedAssertiveContent` | `string?` | Dedup state for assertive announcements. |
| `_politeThrottleTimestamp` | `long` | Throttle timestamp. |
| `_assertiveThrottleTimestamp` | `long` | Throttle timestamp. |
| `_disposed` | `bool` | Guard for pending dispatcher callbacks. |

**Lifecycle**:

- Created by the window wrapper after the `HWND` / `NSWindow` is available and the `Window.RootElement` is set.
- Registered in the router indirectly (via the wrapper's `IAccessibilityOwner` + `XamlRootMap`).
- Disposed before the native window handle is destroyed. Disposal:
  1. Sets `_disposed = true` (so coalesced dispatcher callbacks no-op).
  2. Disposes debouncer timers.
  3. Invokes subclass-specific teardown (Win32: `UiaDisconnectProvider` over the cache; macOS: `uno_accessibility_destroy_context`).

**Removed state (was singleton-scoped, now eliminated from the base class)**:

- The previous `RegisterCallbacks()` method and its writes to `AccessibilityAnnouncer.AccessibilityImpl`, `UIElementAccessibilityHelper.ExternalOn*`, `VisualAccessibilityHelper.ExternalOn*`, and `AutomationPeer.AutomationPeerListener` are moved to the router. The base class no longer touches those static slots.

---

### 4. `Win32Accessibility` (per-window)

**Purpose**: Per-window UIA provider tree manager.

**State (per instance)**:

| Field | Type | Notes |
|-------|------|-------|
| `_hwnd` | `HWND` | Owning window handle. Immutable after construction. |
| `_rootProvider` | `Win32RawElementProvider` | Root UIA provider for this window. |
| `_providers` | `ConditionalWeakTable<UIElement, Win32RawElementProvider>` | Lazily populated provider cache. |
| `_pendingStructureChanges` | `HashSet<Win32RawElementProvider>` | Coalesced structure-change batch for this window. |
| `_dispatcherQueue` | `DispatcherQueue` | Per-window dispatcher for batching. |

**Removed state**: `static _instance` (singleton), `static _currentHwnd`. No process-global state remains.

**Lifecycle**:

- Constructed by `Win32WindowWrapper` with `(hwnd, rootElement, dispatcherQueue)`.
- Disposed from `Win32WindowWrapper.OnWmDestroy` *before* `XamlRootMap.Unregister` and *before* `HWND` release.

**Invariants**:

- `_hwnd` is unique per instance; two windows never share it.
- Providers in `_providers` are all constructed with the same `_hwnd`. No cross-window provider aliasing.
- On `Dispose`: every provider in `_providers` receives `UiaDisconnectProvider` exactly once.

---

### 5. `MacOSAccessibility` (per-window; PR 2)

**Purpose**: Per-window macOS accessibility tree manager.

**State (per instance)**:

| Field | Type | Notes |
|-------|------|-------|
| `_windowHandle` | `nint` | Owning NSWindow pointer. Immutable after construction. |
| `_accessibilityTreeInitialized` | `bool` | Set once the tree has been built for the window. |
| `_isCreatingAOM` | `bool` | Guard during AOM creation. |
| `_activeModalHandle` | `nint` | Per-window modal tracking. |
| `_modalTriggerHandle` | `nint` | Per-window modal trigger. |

**Removed state**: `static _instance`, the `MacOSWindowNative.NativeWindowReady` subscription on a singleton (replaced by direct per-window construction).

**Lifecycle**:

- Constructed by `MacOSWindowHost` after the native window is ready and the root element is set.
- Calls `uno_accessibility_init_context(_windowHandle)` in its constructor.
- Disposed from `MacOSWindowNative.Destroyed()` before `Handle = 0`; disposal calls `uno_accessibility_destroy_context(_windowHandle)`.

---

### 6. `UNOAccessibilityContext` (Objective-C, per-NSWindow)

**Purpose**: Replace process-global `g_elements`, `g_rootElement`, `g_focusedElement` in `UNOAccessibility.m` with per-window state.

**Shape (conceptual)**:

```objc
@interface UNOAccessibilityContext : NSObject
@property (weak) NSWindow *window;
@property (strong) NSMutableDictionary<NSNumber*, UNOAccessibilityElement*> *elements;
@property (strong, nullable) UNOAccessibilityElement *rootElement;
@property (strong, nullable) UNOAccessibilityElement *focusedElement;
@end
```

**Lifecycle**:

- Created by `uno_accessibility_init_context(NSWindow*)` and attached to the NSWindow via `objc_setAssociatedObject(window, key, context, OBJC_ASSOCIATION_RETAIN)`.
- Lifetime follows the NSWindow (associated-object semantics).
- Explicitly purged by `uno_accessibility_destroy_context(NSWindow*)` during managed-initiated window teardown: clears element dict, nils rootElement/focusedElement, calls `objc_setAssociatedObject(window, key, nil, OBJC_ASSOCIATION_RETAIN)`.

**Lookup helper**:

```objc
static UNOAccessibilityContext* _Nullable uno_a11y_context_for_window(NSWindow *window);
```

All `uno_accessibility_*` C entry points that currently use `g_elements` / `g_rootElement` are rewritten to:

1. Accept an `NSWindow*` (or resolve it from the element handle via back-pointer in `UNOAccessibilityElement`).
2. Resolve the context via `uno_a11y_context_for_window`.
3. Operate on the context's own element dict.

**Invariants**:

- Element handles (process-unique GCHandle intptrs) are unique *across all contexts*, but element dicts are per-window. An element created for window A never appears in window B's context.
- Two `UNOAccessibilityContext` instances never share an element pointer.
- After `uno_accessibility_destroy_context`, no element previously registered in that context is reachable via any native lookup.

---

## State Transitions

### Per-window instance lifecycle

```
  [Wrapper ctor runs]
         │
         ▼
  AccessibilityInstance created (IsAccessibilityEnabled=false)
         │
         ▼
  Wrapper sets RootElement (via Window.Content assignment or Window.Activated)
         │
         ▼
  Initialize(hwnd/window, rootElement) → IsAccessibilityEnabled=true
         │
         ▼
  Steady state: callbacks routed via router; tree queries served per-window
         │
         ▼
  Wrapper receives WM_DESTROY / NSWindow close
         │
         ▼
  Dispose():
    _disposed = true
    dispose debouncer timers
    (Win32) walk _providers → UiaDisconnectProvider each
    (macOS) uno_accessibility_destroy_context(window)
         │
         ▼
  Router.NotifyDisposed(this) → if active, fallback to any live owner
         │
         ▼
  Wrapper unregisters from XamlRootMap; releases HWND/NSWindow
```

### Active-window tracking

```
  WM_ACTIVATE(WA_ACTIVE|WA_CLICKACTIVE) / NSWindowDidBecomeMainNotification
                                    │
                                    ▼
                     Router._activeOwner = this owner
                                    │
                                    ▼
  WM_ACTIVATE(WA_INACTIVE) / NSWindowDidResignMainNotification
                                    │
                                    ▼
                       (no-op; stickiness preserved)
                                    │
                                    ▼
                       Owner.Dispose() triggers NotifyDisposed
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                                 ▼
      owner == _activeOwner?                        no-op
                    │
                    ▼
      Pick any live IAccessibilityOwner as
      fallback; if none, _activeOwner = null
```

### Source-less announcement routing

```
  AccessibilityAnnouncer.AccessibilityImpl.AnnouncePolite(text)
                                    │
                                    ▼
                 Router.TryGetActive(out instance)
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                                 ▼
                  true                              false
                    │                                 │
                    ▼                                 ▼
    instance.AnnouncePolite(text)       Drop with trace log (FR-008)
```

---

## Data Integrity Rules

- **No process-global accessibility state** is retained across windows (enforces FR-019).
- **No sharing of debouncer/dedup state** across windows (enforces FR-009).
- **`_hwnd` / `_windowHandle` is immutable** for the life of the instance.
- **Provider/element dictionaries are disjoint** across windows (enforces FR-003).
- **All disposal paths idempotent** — calling `Dispose()` twice is a no-op.

---

## No persistent storage

All state is process-resident and short-lived. No migration paths, no schema versioning. The refactor is a runtime-only change.
