# Implementation Plan: Multi-Window Accessibility for Skia Desktop Hosts

**Branch**: `001-multi-window-a11y` | **Date**: 2026-04-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-multi-window-a11y/spec.md`

## Summary

PR #22858-era accessibility support (merged via PR #23005) shipped as a per-process singleton on both Skia desktop hosts. Secondary top-level windows therefore either expose the primary window's UIA tree (Win32) or have accessibility explicitly disabled to avoid native segfaults (macOS). This plan refactors the Skia desktop accessibility subsystem so every top-level `Window` owns its own accessibility instance, tree, and lifecycle, while a thin process-wide router handles the single-slot framework registration points and dispatches each callback to the owning window.

The design chosen during prior grill-me resolution:

- **Per-window instances** of `Win32Accessibility` / `MacOSAccessibility`, owned by the window wrapper.
- **Process-wide `AccessibilityRouter`** that owns the framework single-slot registrations and dispatches via `peer → owner → XamlRoot → XamlRootMap.GetHostForRoot → IAccessibilityOwner.Accessibility`.
- **Sticky active-window tracking** inside the router for source-less announcements.
- **Per-window disposal** (Win32: `UiaDisconnectProvider` per cached provider via `ConditionalWeakTable` enumeration; macOS: explicit `uno_accessibility_destroy_context(window)` before handle invalidation).
- **macOS native redesign**: replace process-global `g_elements` / `g_rootElement` / `g_focusedElement` with a `UNOAccessibilityContext` attached to each `NSWindow` via `objc_setAssociatedObject`.
- **Two-PR rollout**: PR 1 introduces the router, `IAccessibilityOwner`, and Win32 per-window behavior; PR 2 redesigns the macOS native context and enables multi-window on macOS. No feature flag; the singleton path is removed entirely in PR 1.

## Technical Context

**Language/Version**: C# 13 targeting .NET 9 / .NET 10 for `Uno.UI.Runtime.Skia.*` assemblies; Objective-C (macOS native) built in `UnoNativeMac.xcodeproj`.
**Primary Dependencies**: Uno Platform framework (`Uno.UI.Runtime.Skia`, `Uno.UI.Runtime.Skia.Win32`, `Uno.UI.Runtime.Skia.MacOS`); Windows UI Automation (`UiaRaise*`, `UiaDisconnectProvider`, `UiaReturnRawElementProvider`); AppKit `NSAccessibility` protocol with Objective-C runtime `objc_setAssociatedObject`.
**Storage**: N/A (process-resident state only: per-window provider caches, per-window native element dictionaries).
**Testing**: Runtime tests under `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/` executed against Skia Desktop via the `/runtime-tests` skill. Manual validation with Narrator / VoiceOver captured in quickstart.md.
**Target Platform**: Windows 10+ Win32 Skia Desktop; macOS 11+ Skia Desktop. WebAssembly and mobile out of scope.
**Project Type**: Single cross-platform framework repository. Changes localized to Skia desktop runtime assemblies.
**Performance Goals**: No regression in per-frame or per-mutation hot paths. Per-callback router lookup cost ≤ one dictionary indirection. Provider-cache enumeration only on window disposal, not on steady state.
**Constraints**: Must not crash under rapid window create/destroy (documented existing segfault scenario on macOS). Must not share debouncer or dedup state across windows. Must preserve existing single-window runtime test suite pass rate at 100%.
**Scale/Scope**: Typical applications: 1–5 simultaneously open top-level windows. Trees per window: up to several hundred focusable peers. Stress scenario: 100 sequential create/destroy cycles of a secondary window.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against the seven principles in `.specify/memory/constitution.md`.

| Principle | Check | Verdict |
|-----------|-------|---------|
| I. WinUI API Fidelity | No new public API surfaces are introduced. `AutomationPeer.AutomationPeerListener`, `AccessibilityAnnouncer`, `UIElementAccessibilityHelper`, `VisualAccessibilityHelper` are internal framework hooks and remain with single-slot semantics. `IAccessibilityOwner` is `internal` to `Uno.UI.Runtime.Skia`. | ✅ Pass |
| II. Cross-Platform Parity | Skia Desktop Win32 and macOS are both covered. Behavior remains identical on WebAssembly (separate implementation, out of scope). Mobile has no accessibility layer from this subsystem and is unaffected. Temporary interim state (Win32 multi-window, macOS primary-only) is explicitly documented and closed by PR 2. | ✅ Pass with documented interim |
| III. Test-First Quality Gates | New runtime test `Given_MultiWindowAccessibility` is authored before per-window refactor lands. Existing 9 accessibility test files must continue to pass without modification. Manual checklist in quickstart.md complements OS-integration validation. | ✅ Pass |
| IV. Performance and Resource Discipline | Router adds one `XamlRootMap` lookup per accessibility callback; provider cache remains `ConditionalWeakTable` (weak keying, no added allocations in steady state). Per-window debouncer timers: one `Timer` per window per severity instead of two global timers — proportional to window count, not hot-path churn. Provider enumeration only on dispose. | ✅ Pass |
| V. Generated Code Boundaries | No changes to `Generated/` folders. | ✅ Pass |
| VI. Backward Compatibility | No public API changes. The removal of the singleton path affects `internal` implementation only. Single-window runtime behavior is preserved. No `feat!` required. | ✅ Pass |
| VII. WinUI Implementation Alignment | WinUI C++ references consulted for per-island UIA tree semantics (`CUIAWrapper`, `CJupiterControl::WM_GETOBJECT` handling). Findings documented in `research.md`. | ✅ Pass |

**Gate result**: Proceed to Phase 0 research. No violations; Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/001-multi-window-a11y/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (entity/interface/state model)
├── quickstart.md        # Phase 1 output (manual validation checklist)
├── contracts/           # Phase 1 output
│   ├── IAccessibilityOwner.cs                # Managed: wrapper → accessibility surface
│   ├── AccessibilityRouter.cs                # Managed: static router surface
│   ├── SkiaAccessibilityBase.per-window.md   # Changes to the base class
│   └── UNOAccessibility.context.h            # Native: per-window context C surface
└── checklists/
    └── requirements.md  # Created by /speckit.specify
```

### Source Code (repository root)

```text
src/
├── Uno.UI.Runtime.Skia/
│   └── Accessibility/
│       ├── SkiaAccessibilityBase.cs          # (exists) trim singleton state, keep per-instance
│       ├── AccessibilityRouter.cs            # (new) static router; owns framework single-slots
│       └── IAccessibilityOwner.cs            # (new) internal interface
│
├── Uno.UI.Runtime.Skia.Win32/
│   ├── Accessibility/
│   │   ├── Win32Accessibility.cs             # (exists) drop singleton; per-window instance
│   │   ├── Win32RawElementProvider.cs        # (exists) unchanged (already per-HWND via ctor arg)
│   │   ├── Win32AccessibilityPatterns.cs     # (exists) unchanged
│   │   └── Win32UIAutomationInterop.cs       # (exists) unchanged
│   └── UI/Xaml/Window/
│       └── Win32WindowWrapper.cs             # (exists) implement IAccessibilityOwner;
│                                             #          hook WM_ACTIVATE; own dispose order
│
├── Uno.UI.Runtime.Skia.MacOS/                # Changes delivered in PR 2
│   ├── Accessibility/
│   │   └── MacOSAccessibility.cs             # (exists) per-window instance; remove singleton
│   ├── Native/
│   │   └── NativeUno.cs                      # (exists) add context-bound native APIs
│   ├── UI/Xaml/Window/
│   │   └── MacOSWindowNative.cs              # (exists) drop primary-only guard; per-window init
│   └── UnoNativeMac/UnoNativeMac/
│       ├── UNOAccessibility.h                # (exists) add context surface
│       └── UNOAccessibility.m                # (exists) replace globals with per-window context
│
└── Uno.UI.RuntimeTests/
    └── Tests/Windows_UI_Xaml_Automation/
        └── Given_MultiWindowAccessibility.cs # (new) PR 1; Skia Desktop only initially
```

**Structure Decision**: Follow existing layout. Accessibility lives in `Accessibility/` sub-folders of each runtime project. Router + interface land in the shared `Uno.UI.Runtime.Skia` assembly so both desktop hosts and the runtime tests consume the same surface. macOS native work stays inside the existing `UnoNativeMac` Xcode project.

## Rollout Strategy

**PR 1 — Router + Win32 per-window (this feature's first delivery)**

Scope:

- `AccessibilityRouter` (static, internal).
- `IAccessibilityOwner` interface (internal to `Uno.UI.Runtime.Skia`).
- `SkiaAccessibilityBase` updated so per-instance state (debouncer timers, focus tracker, announcement dedup) is truly per-instance. Removal of the `RegisterCallbacks()` pattern from subclasses; the router is the one registrant.
- `Win32Accessibility` refactored: drop `static _instance`, constructor takes `HWND` + root `UIElement` + router reference, disposal path added.
- `Win32WindowWrapper` implements `IAccessibilityOwner`; owns the per-window instance lifecycle and hooks `WM_ACTIVATE` + `WM_DESTROY`.
- Runtime test `Given_MultiWindowAccessibility` covers assertions a–d from SC-007 (Skia Desktop only; macOS uses `PlatformCondition` to skip).
- macOS keeps the existing primary-window-only guard and the singleton `MacOSAccessibility` (compatibility bridge to the new router: the macOS side registers itself with the router like any other instance, but only for the primary window).
- Documentation: PR description captures the macOS interim limitation.

**PR 2 — macOS per-window native context**

Scope:

- `UNOAccessibilityContext` Objective-C class encapsulating per-window elements/root/focused.
- `objc_setAssociatedObject` attaches context to each `NSWindow`.
- All `uno_accessibility_*` C entry points take `NSWindow*` and resolve to the context.
- Explicit `uno_accessibility_destroy_context(window)` called from managed disposal before NSWindow ref is dropped.
- `MacOSAccessibility` refactored: drop singleton; constructor takes `NSWindow` handle; disposal tears down native context and managed timers.
- `MacOSWindowNative.cs:54` primary-window guard removed.
- Stress test manually executed per quickstart.md.
- Runtime test file promoted from `PlatformCondition(Skia Desktop Windows)` to full Skia Desktop coverage.

**Interim contract between PR 1 and PR 2**: After PR 1 lands, macOS retains its current behavior (primary-window-only accessibility). This is an accepted, documented limitation covered by FR-024. PR 2 closes it.

## Complexity Tracking

*No constitution violations; section omitted.*
