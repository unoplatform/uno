# PR 2 — Multi-Window Accessibility: macOS per-window native context

## Summary

Completes the multi-window accessibility feature on Skia Desktop macOS by
replacing the process-global `g_elements` / `g_rootElement` /
`g_focusedElement` / `g_window` statics in `UNOAccessibility.m` with a
`UNOAccessibilityContext` Objective-C class attached to each `NSWindow`
via `objc_setAssociatedObject`. `MacOSAccessibility` is now per-window;
the primary-window-only guard in `MacOSWindowNative.cs` is removed.

Feature spec: `specs/004-multi-window-a11y/spec.md`
Rollout plan: `specs/004-multi-window-a11y/plan.md` (PR 2 slice)
Depends on: PR 1 (`AccessibilityRouter`, `IAccessibilityOwner`,
`SkiaAccessibilityBase` per-window plumbing).

## What changed

* **`UNOAccessibility.h` / `UNOAccessibility.m`** — new
  `UNOAccessibilityContext @interface` encapsulates per-window state
  (elements dict, root element, focused element, weak NSWindow back-ref).
  `uno_accessibility_init_context(NSWindow*)` attaches the context via
  `objc_setAssociatedObject(OBJC_ASSOCIATION_RETAIN)`.
  `uno_accessibility_destroy_context(NSWindow*)` is called explicitly
  from managed disposal BEFORE the NSWindow handle is cleared, so
  pending VoiceOver queries observe a detached element rather than
  following a dangling pointer. Window-scoped entry points
  (`add_element`, `remove_element`, `post_layout_changed`,
  `post_children_changed`, `announce`, `get_root_children`,
  `get_focused_element`) take `NSWindow*`. Per-element setters keep
  their single-handle signatures and resolve the owning context via the
  element's `unoContext` back-pointer (handles are process-unique
  GCHandle intptrs, so they never collide across contexts).

* **`MacOSAccessibility.cs`** — removes the singleton; the constructor
  takes the `NSWindow` handle, calls `uno_accessibility_init_context`,
  and stores per-instance focus / modal / tree-initialized state.
  `DisposeCore` calls `uno_accessibility_destroy_context` before the
  native window handle is zeroed (research Decision 6 ordering).

* **`MacOSWindowHost.cs`** — constructs `MacOSAccessibility` per window
  (replacing the PR 1 primary-only compatibility bridge), subscribes to
  the WinUI `Activated` event as the macOS analog of `WM_ACTIVATE` to
  drive the router's sticky active-owner reference (never cleared on
  `Deactivated`), queues the initial tree build once `RootElement` is
  available, and disposes the instance on window teardown.

* **`MacOSWindowNative.cs`** — drops the `if (MacSkiaHost.Current.InitialWindow == this)`
  guard (line 54). Every open window now gets its own accessibility
  tree. `Destroyed()` disposes the host's accessibility instance BEFORE
  clearing the native handle.

* **`NativeUno.cs`** — P/Invoke declarations updated to match the new
  signatures (`IntPtr window` on window-scoped entry points;
  `init_context`/`destroy_context` added).

* **Runtime test** —
  `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs`
  lifts its `PlatformCondition` from `SkiaWin32` only to
  `SkiaWin32 | SkiaMacOS`. Adds two additional `[TestMethod]`s covering
  FR-009 (per-instance debouncer state on identical text) and FR-010
  (per-window focus tracker isolation).

## Test plan

* [ ] `cd src && dotnet build Uno.UI-Skia-only.slnf --no-restore` on macOS
* [ ] Rebuild `libUnoNativeMac.dylib` via the `UnoNativeMac.xcodeproj`
      so the new `UNOAccessibilityContext` class ships alongside the
      managed assemblies.
* [ ] `/runtime-tests Given_MultiWindowAccessibility` — Skia Desktop
      macOS AND Windows (confirm no regression on the PR 1 platform).
* [ ] `/runtime-tests Windows_UI_Xaml_Automation` — full single-window
      accessibility regression suite (SC-006), Skia Desktop macOS.
* [ ] VoiceOver manual validation against the
      `specs/004-multi-window-a11y/quickstart.md` "macOS / VoiceOver
      checklist", including the **CRITICAL** 100-iteration rapid
      create/destroy stress block (SC-004).
* [ ] Resident-memory spot check after the stress loop: no linear
      growth relative to baseline.

## Closes the PR 1 interim

The documented FR-024 interim ("macOS primary-window only") is removed.
The feature now delivers full per-window accessibility on both Skia
Desktop hosts.
