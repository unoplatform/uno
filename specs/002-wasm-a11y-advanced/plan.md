# Implementation Plan: WASM Accessibility — Advanced Features

**Branch**: `002-wasm-a11y-advanced` | **Date**: 2026-02-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-wasm-a11y-advanced/spec.md`

## Summary

Extends the base WASM accessibility layer (001-wasm-accessibility) with virtualized list/grid support, live region event wiring through core Uno.UI AutomationPeer changes, bidirectional focus synchronization, modal focus trapping, focus recovery, performance validation, and WCAG 2.1 AA compliance verification. Implementation spans both the WebAssembly Browser runtime (TypeScript + C# interop) and the shared Uno.UI project (AutomationPeer dispatch chain).

## Technical Context

**Language/Version**: C# (.NET 10.0) + TypeScript (ES2020+)
**Primary Dependencies**: Uno.UI (core framework), Uno.UI.Runtime.Skia.WebAssembly.Browser (WASM runtime), System.Runtime.InteropServices.JavaScript (JSImport/JSExport)
**Storage**: N/A (DOM manipulation only)
**Testing**: MSTest via Uno.UI.RuntimeTests (headless Skia execution), axe-core (browser-based WCAG scanning), manual screen reader testing (NVDA, VoiceOver)
**Target Platform**: WebAssembly (Skia-based rendering with semantic DOM overlay)
**Project Type**: Framework library (contributions to existing Uno Platform monorepo)
**Performance Goals**: <2ms/frame overhead for 500 elements, >30fps during virtualized list scrolling, <16ms total DOM update for 100 simultaneous property changes
**Constraints**: On-demand activation (semantic DOM created only after screen reader activates via "Enable accessibility" button), two-tier rate limiting for live regions (100ms debounce + 500ms/200ms sustained throttle)
**Scale/Scope**: Support 1,000+ virtualized items, 500+ simultaneous semantic elements, 11 control types

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | PASS | RaiseAutomationEvent is a WinUI API; implementation aligns with WinUI C++ dispatch pattern |
| II. Cross-Platform Parity | PASS | Core Uno.UI changes (RaiseAutomationEvent dispatch) must not break other platforms; WASM-specific code isolated in WebAssembly.Browser project |
| III. Test-First Quality Gates | PASS | Runtime tests for all user stories; axe-core for WCAG |
| IV. Performance Discipline | PASS | Explicit perf targets (2ms/frame, 30fps minimum); batching via requestAnimationFrame |
| V. Generated Code Boundaries | PASS | No generated files modified |
| VI. Backward Compatibility | PASS | No breaking API changes; RaiseAutomationEvent dispatch extension is additive |
| VII. WinUI Implementation Alignment | PASS | Clarification requires consulting WinUI C++ sources (D:\Work\microsoft-ui-xaml2) before modifying shared dispatch chain |

**Gate result**: ALL PASS — proceed to Phase 0.

### Post-Design Re-evaluation (after Phase 1)

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| I. WinUI API Fidelity | PASS | Research R1 confirmed WinUI C++ dispatch chain. Contracts align with WinUI's UIA provider model. LiveSetting enum values (Off=0, Polite=1, Assertive=2) match WinUI. |
| II. Cross-Platform Parity | PASS | All new classes isolated in WebAssembly.Browser project. Core AutomationPeer change is additive (calls IUnoAccessibility.OnAutomationEvent, no-op when no provider registered). |
| III. Test-First Quality Gates | PASS | 6 test files planned covering all 7 user stories. Runtime tests use headless Skia execution. |
| IV. Performance Discipline | PASS | requestAnimationFrame batching, two-tier rate limiting, viewport-only semantic elements — all aligned with perf targets. |
| V. Generated Code Boundaries | PASS | No generated files touched. AutomationPeer.cs is non-generated. |
| VI. Backward Compatibility | PASS | All changes additive. IUnoAccessibility interface extension does not break existing consumers. |
| VII. WinUI Implementation Alignment | PASS | R1 extensively analyzed WinUI C++ sources (AutomationPeer.cpp, xcpcore.cpp, UIAWindow.cpp). Dispatch mirrors WinUI's ListenerExists → RaiseAutomationEvent pattern. |

**Post-design gate result**: ALL PASS — proceed to Phase 2 (task generation).

## Project Structure

### Documentation (this feature)

```text
specs/002-wasm-a11y-advanced/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: WinUI source analysis, Uno internals research
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Developer integration guide
├── contracts/           # Phase 1: TypeScript API contracts
│   ├── virtualization.ts
│   ├── live-regions.ts
│   ├── focus-sync.ts
│   └── modal-focus.ts
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/Uno.UI/
├── UI/Xaml/Automation/Peers/
│   └── AutomationPeer.cs              # Core: RaiseAutomationEvent dispatch modification
│
src/Uno.UI.Runtime.Skia.WebAssembly.Browser/
├── Accessibility/
│   ├── WebAssemblyAccessibility.cs     # Main coordinator (extend with focus sync, modal, virtualization)
│   ├── AriaMapper.cs                   # ARIA attribute mapping (extend for live regions)
│   ├── SemanticElementFactory.cs       # Element creation (extend for virtualized items)
│   ├── AccessibilityDebugger.cs        # Debug overlay (extend for perf metrics)
│   ├── FocusSynchronizer.cs            # NEW: Bidirectional focus bridge
│   ├── ModalFocusScope.cs              # NEW: Focus trap manager
│   ├── LiveRegionManager.cs            # NEW: Two-tier rate-limited announcements
│   └── VirtualizedSemanticRegion.cs    # NEW: Viewport-aware semantic element lifecycle
├── ts/Runtime/
│   ├── Accessibility.ts                # Main TS class (extend for focus sync, modal trap)
│   ├── SemanticElements.ts             # Element factories (extend for virtualized lifecycle)
│   ├── FocusTrap.ts                    # NEW: Modal focus cycling in DOM
│   └── LiveRegion.ts                   # NEW: Two-tier rate limiter for announcements
│
src/Uno.UI.RuntimeTests/
└── Tests/Windows_UI_Xaml_Automation/
    ├── Given_VirtualizedListAccessibility.cs   # NEW: US1 tests
    ├── Given_LiveRegionEvents.cs               # NEW: US2 tests
    ├── Given_FocusSynchronization.cs           # NEW: US3 tests
    ├── Given_ModalFocusTrap.cs                 # NEW: US4 tests
    ├── Given_FocusRecovery.cs                  # NEW: US5 tests
    └── Given_AccessibilityPerformance.cs       # NEW: US6 tests
```

**Structure Decision**: This feature extends the existing Uno Platform monorepo structure. New C# classes are added to `Accessibility/` in the WebAssembly Browser project. New TypeScript modules are added to `ts/Runtime/`. Core Uno.UI changes are minimal and surgical (AutomationPeer dispatch). All test files go in the existing RuntimeTests location.

## Complexity Tracking

No constitution violations to justify.
