# PR 1 — Multi-Window Accessibility: Router + Win32 per-window

## Summary

Introduces process-wide accessibility dispatch via `AccessibilityRouter` +
`IAccessibilityOwner` and delivers per-window accessibility on the Skia
Desktop Win32 host. The legacy `Win32Accessibility` singleton is removed;
each top-level `Window` now owns its own UIA provider tree, debouncer
state, focus tracker, and announcement dedup.

Feature spec: `specs/004-multi-window-a11y/spec.md`
Rollout plan: `specs/004-multi-window-a11y/plan.md` (PR 1 slice)

## What changed

* **`Uno.UI.Runtime.Skia` (shared)** — new `AccessibilityRouter` claims
  the framework's four single-slot registrations
  (`AutomationPeer.AutomationPeerListener`,
  `AccessibilityAnnouncer.AccessibilityImpl`,
  `UIElementAccessibilityHelper.ExternalOnChildAdded/Removed`,
  `VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged`) and
  dispatches callbacks via `peer → UIElement.XamlRoot → XamlRootMap →
  IAccessibilityOwner → SkiaAccessibilityBase`. `SkiaAccessibilityBase`
  gains `Dispose`/`DisposeCore`/`IsDisposed` + `Route*` entry points so
  per-window subclasses can tear down native resources with short-circuit
  guards on pending dispatcher callbacks. `XamlRootMap.Enumerate` is
  added for fallback active-owner resolution.

* **`Uno.UI.Runtime.Skia.Win32`** — `Win32Accessibility` inherits from
  `SkiaAccessibilityBase`, its constructor takes `(hwnd, rootElement,
  dispatcherQueue)`, and all cached provider state is per-instance.
  `DisposeCore` walks the `ConditionalWeakTable` and calls
  `UiaDisconnectProvider` per entry (no process-wide
  `UiaDisconnectAllProviders`). `Win32WindowWrapper` implements
  `IAccessibilityOwner`, hooks `WM_ACTIVATE`
  (`WA_ACTIVE`/`WA_CLICKACTIVE`) to update the router's sticky
  active-owner reference, and disposes at `WM_DESTROY` before
  `XamlRootMap.Unregister` / HWND release.

* **`Uno.UI.Runtime.Skia.MacOS` — interim bridge** — `MacOSAccessibility`
  keeps its singleton model for PR 1 but stops calling the removed
  `RegisterCallbacks`; `MacOSWindowHost` implements `IAccessibilityOwner`
  returning the singleton only for the primary/initial window. Secondary
  macOS windows continue to receive no accessibility tree — a documented
  interim limitation (FR-024) that is closed by PR 2 along with the
  `UNOAccessibilityContext` native redesign.

* **Runtime test** —
  `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs`
  covers SC-007 a/b/c/d and FR-006/FR-009 for Skia Desktop Windows:
  two distinct instances with disjoint state, peer/element resolution
  to the owning window's instance, source-bearing routing bypassing the
  active owner, disposal cleanup, and per-instance debouncer timer
  identities. Gated via
  `[PlatformCondition(Include, SkiaWin32)]` until PR 2 lifts it.

## Test plan

* [ ] `cd src && dotnet build Uno.UI-Skia-only.slnf --no-restore`
* [ ] `/runtime-tests Given_MultiWindowAccessibility` (Skia Desktop
      Windows)
* [ ] `/runtime-tests Windows_UI_Xaml_Automation` (single-window
      regression — SC-006)
* [ ] Narrator manual validation against the checklist in
      `specs/004-multi-window-a11y/quickstart.md` "Windows / Narrator
      checklist" — tree coverage, focus, announcements, disposal
      (including the 100-iteration secondary-window create/close loop).

## Interim limitation

macOS retains primary-window-only accessibility. This is tracked under
FR-024 in the spec and closed by the follow-up PR that redesigns the
macOS native accessibility context.
