# Contract: SkiaAccessibilityBase per-window refactor

**Feature**: 001-multi-window-a11y
**Phase**: 1 — Design contracts
**Shipped location**: `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`

Describes the delta to the existing `SkiaAccessibilityBase` class so that it is usable as a per-window instance rather than a singleton. No public API change; all members remain `internal`.

## Removed

- `protected void RegisterCallbacks()` — the base class no longer writes to
  `AccessibilityAnnouncer.AccessibilityImpl`,
  `UIElementAccessibilityHelper.ExternalOnChildAdded / ExternalOnChildRemoved`,
  `VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged`, or
  `AutomationPeer.AutomationPeerListener`. The router owns all framework
  single-slot registrations. Subclass constructors no longer call this method.

## Added

### `protected bool IsDisposed { get; }`

Set by `Dispose()` to short-circuit pending dispatcher callbacks. Also used by the router's `ListenerExistsHelper` to report that a disposed instance is no longer a listener.

### `public virtual void Dispose()`

Ordered teardown:

1. Set `IsDisposed = true` so any pending `DispatcherQueue.TryEnqueue` callback short-circuits.
2. Dispose debouncer timers (`_politeDebounceTimer`, `_assertiveDebounceTimer`).
3. Invoke subclass `DisposeCore()` hook for platform-specific teardown (Win32: walk `_providers`, `UiaDisconnectProvider` each; macOS: `uno_accessibility_destroy_context`).
4. Untrack any focused element (unsubscribe `IsEnabledChanged`, `Unloaded`).

### `protected abstract void DisposeCore()`

Subclass hook for platform-specific disposal. Called once, before the shared timer/focus teardown. Must be idempotent.

### `internal void RouteChildAdded(UIElement parent, UIElement child, int? index)`
### `internal void RouteChildRemoved(UIElement parent, UIElement child)`
### `internal void RouteVisualOffsetOrSizeChanged(Microsoft.UI.Composition.Visual visual)`

Router entry points that wrap the existing `OnChildAddedCore` / `OnChildRemovedCore` / `OnSizeOrOffsetChangedCore` guarded paths. Semantics unchanged from today except that the callback reaches this specific per-window instance rather than the singleton.

## Unchanged

- All abstract "Platform state / Tree management / Property updates / Focus & modal / Announcements" members retain their existing signatures.
- `NotifyPropertyChangedEvent`, `NotifyAutomationEvent`, `NotifyNotificationEvent` shape unchanged; the guard `if (!IsAccessibilityEnabled)` continues to protect pre-initialization calls.
- Focus tracking (`_trackedFocusedElement`, `TrackFocusedElement`, `UntrackFocusedElement`, `OnTrackedElementIsEnabledChanged`, `OnTrackedElementUnloaded`, `RecoverFocus`) is unchanged in logic; its state is now naturally per-window because the instance is per-window.
- Announcement debouncing / throttling / dedup (`AnnouncePolite`, `AnnounceAssertive`, `FlushPoliteAnnouncement`, `FlushAssertiveAnnouncement`, `ResetAnnouncementTracking`) is unchanged in logic; its state is now per-window.

## Invariants

- After construction and before `Initialize*` (subclass-specific), `IsAccessibilityEnabled` returns `false`. All `Notify*` calls short-circuit.
- After `Dispose()`, `IsAccessibilityEnabled` returns `false` permanently, `IsDisposed` is true, all timers are disposed, and focus tracking is cleared.
- `Dispose()` is idempotent.
