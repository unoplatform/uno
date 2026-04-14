# Phase 0 Research: Multi-Window Accessibility

**Feature**: 001-multi-window-a11y
**Date**: 2026-04-14

This document captures the design decisions resolved during prior grill-me interview and the supporting research needed to green-light Phase 1.

---

## Decision 1: Topology — per-window instances vs. singleton-with-per-window-dicts

**Decision**: Per-window instances of the host-specific accessibility class (`Win32Accessibility`, `MacOSAccessibility`). One instance per open top-level `Window`, owned by the platform wrapper (`Win32WindowWrapper`, `MacOSWindowHost`).

**Rationale**:

- `SkiaAccessibilityBase` already carries per-window-shaped state: `_politeDebounceTimer`, `_assertiveDebounceTimer`, `_lastAnnouncedPoliteContent`, `_lastAnnouncedAssertiveContent`, `_trackedFocusedElement`. As a singleton, that state silently interferes across windows (a window B "Saved" announcement can be suppressed by window A's earlier identical one via the `_lastAnnouncedPoliteContent` dedup).
- Per-window lifetime aligns with `HWND` / `NSWindow` lifetime. Disposal is naturally scoped.
- Matches WinUI 3's per-island UIA tree model where each XAML island owns its own `CUIAWrapper` tree root.

**Alternatives considered**:

- *Singleton with per-HWND dictionaries*: `_hwnd → (rootProvider, providers, pendingStructureChanges, …)` keyed inside the singleton. Rejected because the base-class state (timers, dedup) would still need per-window splitting, and window close requires walking the same per-window bucket — the dictionary indirection buys nothing over an instance.
- *Feature flag* gating per-window vs singleton: rejected. Accessibility feature flags go untested in practice; the singleton path would rot.

---

## Decision 2: Router topology — how callbacks reach the right per-window instance

**Decision**: A process-wide static `AccessibilityRouter` (internal, in `Uno.UI.Runtime.Skia`) owns the framework's single-slot registration points and dispatches each incoming callback to the correct per-window instance. Resolution uses the existing `XamlRootMap`:

```
peer → TryGetPeerOwner(peer, out UIElement owner)
     → owner.XamlRoot
     → host = XamlRootMap.GetHostForRoot(xamlRoot)
     → ((IAccessibilityOwner)host).Accessibility
```

Active-window tracking for source-less announcements is a small `_activeOwner` field inside the router, updated by wrappers on `WM_ACTIVATE` / `NSWindowDidBecomeMainNotification`.

**Rationale**:

- `XamlRootMap` is already the framework's source-of-truth for `XamlRoot → host`. Used today by Win32 display-info plumbing (`Win32Host.cs:72`). No parallel registry to maintain.
- `IAccessibilityOwner` on the wrapper keeps each host's wrapper as the authoritative lifecycle owner of its accessibility state.
- The active-window tracker limits router ambiguity to one narrow case: announcements arriving via `AccessibilityAnnouncer.AccessibilityImpl` with no element context. All other paths disambiguate from the peer's owner.

**Alternatives considered**:

- *Dedicated `AccessibilityRouter` dictionary*: rejected — duplicates `XamlRootMap` bookkeeping.
- *Static fan-out on `SkiaAccessibilityBase` itself*: rejected — couples dispatch logic to a base class already doing per-instance work; the router is deliberately kept free of per-instance behavior.
- *Promoting an accessibility capability to `IXamlRootHost`*: rejected for now — more invasive, and only two hosts implement it. `IAccessibilityOwner` carries smaller blast radius; may be promoted later if WebAssembly or another host ever needs it.

---

## Decision 3: Active-window tracking — does tree building depend on activation?

**Decision**: No. Every open window keeps its accessibility tree live and navigable regardless of activation state. Active-window tracking is used *only* for source-less announcement fallback.

**Rationale**:

- Narrator, NVDA, JAWS, and VoiceOver all routinely query inactive windows (review cursor, object navigation, rotor, item chooser). Automated testing tools (Appium, FlaUI, macOS AXTest) do the same.
- UIA explicitly enumerates top-level HWNDs regardless of Z-order.
- Confining the tree to the active window would break all of the above.

**Policy for source-less announcements**:

- `SetActive` is called on `WM_ACTIVATE` (`WA_ACTIVE`, `WA_CLICKACTIVE`) and on `NSWindowDidBecomeMainNotification`.
- `_activeOwner` is *sticky* across deactivation — not cleared on `WA_INACTIVE` / `DidResignMain`. This preserves the "last active" target so announcements that arrive while the whole app is inactive still route correctly when the user returns.
- On `NotifyDisposed`, if the disposed instance was `_activeOwner`, the router picks any other live instance as a best-effort fallback (documented FR-008 behavior).
- If `_activeOwner` is null (first-startup race or all windows disposed), source-less announcements are dropped with a trace log (FR-008, FR-017).

**Rationale (sticky policy)**: Clearing on deactivation creates a gap where the app is active-less and an announcement has no target. The "last active" fallback is the natural user expectation and matches how OS screen readers themselves scope their attention.

---

## Decision 4: Focus tracking scope

**Decision**: Per-instance tracking. `_trackedFocusedElement` lives on each per-window `SkiaAccessibilityBase` instance. No cross-window coordination.

**Rationale**:

- `FocusManager.GetFocusedElement(xamlRoot)` is already per-root; `SkiaAccessibilityBase.IsElementCurrentlyFocused` reads per-root state.
- `RecoverFocus` walks the visual tree rooted in the same window — cross-window recovery doesn't make sense.
- The OS has one keyboard focus at a time, but logically only one window holds it; the other window's `_trackedFocusedElement` is the "last known" within that window, which the WinUI per-island model expects.

**Alternatives considered**:

- *Deactivate A's tracker when B activates*: rejected — unnecessary coordination for no benefit; each instance's `IsElementCurrentlyFocused` check already handles this correctly.

---

## Decision 5: Win32 disposal order and provider cleanup

**Decision**:

- `Win32Accessibility._providers` stays `ConditionalWeakTable<UIElement, Win32RawElementProvider>`.
- On `Dispose()`, enumerate the table directly via the .NET 9+ `IEnumerable<KeyValuePair<TKey, TValue>>` implementation, call `UiaDisconnectProvider(provider)` for each entry, then clear the table.
- Dispose per-window timers (`_politeDebounceTimer`, `_assertiveDebounceTimer`).
- Set `_disposed = true` so pending `DispatcherQueue.TryEnqueue` callbacks for structure-change coalescing no-op.
- `Win32WindowWrapper.OnWmDestroy` calls `_accessibility?.Dispose()` *before* unregistering from `XamlRootMap` and before releasing the `HWND`. Order matters: UIA clients must see a well-formed disconnect rather than a dangling HWND.

**Rationale**:

- Enumerating `ConditionalWeakTable` directly avoids the parallel `HashSet<Win32RawElementProvider>` bookkeeping overhead.
- Disposing *before* `HWND` release ensures `UiaDisconnectProvider` reaches provider objects whose COM callouts may still be serialized on the window's message thread.
- `UiaDisconnectAllProviders` is explicitly rejected because it is process-wide — it would disconnect providers belonging to other windows.

**Alternatives considered**:

- *Parallel `HashSet<Win32RawElementProvider>`*: earlier grill-me proposal; rejected once the .NET 9+ `ConditionalWeakTable` enumeration was confirmed available.
- *Strong dictionary*: rejected — blocks element GC.

---

## Decision 6: macOS native redesign — per-window context

**Decision**: Replace process-global `g_elements`, `g_rootElement`, `g_focusedElement` in `UNOAccessibility.m` with a `UNOAccessibilityContext` Objective-C class attached to each `NSWindow` via `objc_setAssociatedObject`. Every `uno_accessibility_*` native entry point takes `NSWindow*` and resolves to the context.

Lifecycle:

- `uno_accessibility_init_context(NSWindow*)` creates the context and attaches it via `objc_setAssociatedObject` with `OBJC_ASSOCIATION_RETAIN`.
- Context auto-releases when the `NSWindow` deallocs.
- Managed side calls `uno_accessibility_destroy_context(NSWindow*)` explicitly from `MacOSAccessibility.Dispose()` *before* `MacOSWindowNative.Handle = 0`. This tears down the context eagerly so pending VoiceOver queries receive a disconnected response rather than a stale pointer.

**Rationale**:

- Managed side already passes the `NSWindow` handle into most native entry points, so adding the parameter where missing is mechanical.
- Associated objects give us automatic NSWindow-lifetime binding without a separate "destroy context" requirement from the managed side — explicit teardown becomes an optimization for deterministic disconnect, not a correctness requirement.
- Element handles are process-unique `GCHandle` intptrs, so per-window element dictionaries do not require handle namespacing.
- The current guard in `MacOSWindowNative.cs:54` (`if (MacSkiaHost.Current.InitialWindow == this)`) is a workaround for the `[g_elements removeAllObjects]` reset that happens inside `uno_accessibility_init(window)`. With per-window contexts, initialization no longer trampolines across windows, and the guard is removed.

**Alternatives considered**:

- *Explicit context handle passed around*: rejected — doubles the managed-side bookkeeping; we'd track both the window handle and the context pointer.
- *Keep global dict, key by window*: rejected — leaks on window close unless we explicitly purge. Associated-object path gets cleanup for free.
- *Root element `unoParent = g_window` fallback*: replaced by context's associated NSWindow reference (`context.window`).

---

## Decision 7: Rollout strategy

**Decision**: Two PRs.

- **PR 1**: Router + `IAccessibilityOwner` + `Win32Accessibility` per-window + `SkiaAccessibilityBase` per-instance plumbing + `Given_MultiWindowAccessibility` runtime test (Skia Desktop Windows only via `PlatformCondition`). macOS keeps current primary-window-only behavior as documented interim limitation (FR-024).
- **PR 2**: macOS native context refactor + `MacOSAccessibility` per-window + removal of `MacOSWindowNative.cs:54` guard + manual stress test + runtime test promoted to full Skia Desktop coverage.

**No feature flag.** The singleton path is removed entirely in PR 1.

**Rationale**:

- The Win32 refactor is managed-side C# only — mostly mechanical. Low risk. Ships with runtime tests.
- The macOS refactor touches Objective-C runtime tricks and has the documented segfault risk. It deserves its own focused PR with narrow blast radius and manual VoiceOver validation.
- Avoid the feature-flag trap: teams disable them and forget, leading to silent accessibility regressions on assistive-tech users.

**Alternatives considered**:

- *Single big PR*: rejected — harder to review, harder to bisect.
- *macOS first*: rejected — riskier work should not block the lower-risk, managed-side improvement.

---

## Decision 8: Validation strategy

**Decision**: Hybrid approach combining automated and manual validation.

- **Automated runtime tests** (Skia Desktop):
  - `Given_MultiWindowAccessibility` — creates a secondary `Window`, asserts both have non-null `IAccessibilityOwner.Accessibility` with `IsAccessibilityEnabled == true`, asserts disjoint provider/element sets, asserts router resolves correctly, asserts disposal cleans up.
  - Existing accessibility runtime test suite (`Given_AccessibleButton`, `Given_AccessibleCheckBox`, `Given_AccessibleSlider`, `Given_AccessibleTextBox`, `Given_AccessibilityAnnouncements`, `Given_AccessibilityFocus`, `Given_AccessibleComboBox`, `Given_AccessibleListView`) must pass unmodified.
- **Manual checklist** in `quickstart.md`:
  - Narrator (Win32): tree nav in both windows, announcement routing, focus changes, close primary first, close secondary first.
  - VoiceOver (macOS): same checklist + rapid create/destroy stress test.

**Rationale**:

- UIA-client-based automated assertions (spinning up a UIA client inside the test to walk trees) is feasible but is scope creep for this refactor — and would not catch OS-level Narrator/VoiceOver-specific behavior.
- Manual checklists capture OS integration that cannot be automated in a runtime test harness.

**Alternatives considered**:

- *UIA-client automated walks*: rejected as scope creep.
- *Manual-only*: rejected — leaves the routing logic uncovered by regression protection.

---

## Decision 9: WinUI C++ alignment

**Decision**: Consulted `D:\Work\microsoft-ui-xaml2\src\` per Principle VII. Key reference points:

- `CUIAWrapper` in WinUI is instantiated per-element but lives under a per-island UIA tree root. Each XAML island handles `WM_GETOBJECT` on its own HWND and returns its own root. This matches the per-window design in this feature.
- WinUI calls `UiaDisconnectProvider(this)` from `CUIAWrapper::Invalidate()` when the automation peer is destroyed — the same pattern Uno already uses in `Win32Accessibility.CleanupProviders` (line 281), and which we preserve on per-window disposal.
- WinUI's announcement path (`CAutomationPeer::RaiseNotificationEvent`) always carries an element context. Uno's `AccessibilityAnnouncer.AccessibilityImpl` is the element-less fallback for framework-level announcements; the active-window router policy handles this gap.

No deviations from WinUI behavior are introduced; this feature brings Uno closer to the WinUI per-island accessibility model than the existing singleton.

---

## Open questions resolved

All `NEEDS CLARIFICATION` markers in the original spec were resolved during grill-me. No residual research items remain for Phase 1.
