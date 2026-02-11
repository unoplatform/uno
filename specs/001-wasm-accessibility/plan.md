# Implementation Plan: WebAssembly Skia Accessibility Enhancement

**Branch**: `001-wasm-accessibility` | **Date**: 2026-02-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-wasm-accessibility/spec.md`

## Summary

Enhance Uno Platform's WebAssembly accessibility by mapping automation peer patterns to ARIA attributes and enabling user interaction through hidden HTML input elements. This follows Flutter's proven approach of creating a parallel semantic DOM tree that overlays the Skia canvas, using native HTML inputs for interactive controls to get built-in keyboard support and screen reader compatibility.

## Technical Context

**Language/Version**: C# (.NET 9.0/10.0), TypeScript
**Primary Dependencies**:
- Uno.UI (automation peers, UIElement)
- Uno.UI.Runtime.Skia.WebAssembly.Browser (accessibility layer)
- System.Runtime.InteropServices.JavaScript (JSImport/JSExport)

**Storage**: N/A (runtime accessibility layer, no persistence)
**Testing**: Uno.UI.RuntimeTests (headless Skia), manual screen reader testing (NVDA, VoiceOver)
**Target Platform**: WebAssembly (browser)
**Project Type**: Framework library extension
**Performance Goals**:
- Debounce semantic DOM updates by 100ms
- No measurable frame rate impact from accessibility tree maintenance
- Virtualized lists: semantic elements only for visible items

**Constraints**:
- Must integrate with existing IAutomationPeerListener infrastructure
- Must use EffectiveViewport for virtualization
- TypeScript compiled to embedded JS resource
- Cannot modify automation peer interfaces (cross-platform)

**Scale/Scope**:
- 34 functional requirements across 10 pattern categories
- 8 control patterns (Invoke, Toggle, RangeValue, Value, ExpandCollapse, Selection, Scroll, Live)
- ~20 control types to support

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. WinUI API Fidelity | PASS | Leverages existing WinUI automation peer API; no new public APIs |
| II. Cross-Platform Parity | PASS | WebAssembly-specific using `.wasm.cs` suffix; no impact on other platforms |
| III. Test-First Quality Gates | PASS | Runtime tests required for each pattern; manual screen reader testing |
| IV. Performance Discipline | PASS | 100ms debounce, EffectiveViewport virtualization specified |
| V. Generated Code Boundaries | PASS | No generated code modifications needed |
| VI. Backward Compatibility | PASS | Additive changes only; existing apps unaffected |
| VII. WinUI Implementation Alignment | N/A | No WinUI equivalent for web accessibility (platform-specific) |

**Gate Status**: PASS - No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-wasm-accessibility/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (TypeScript interfaces)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/Uno.UI.Runtime.Skia.WebAssembly.Browser/
├── Accessibility/
│   ├── WebAssemblyAccessibility.cs      # Main implementation (MODIFY)
│   ├── AriaMapper.cs                    # Pattern-to-ARIA mapping (NEW)
│   ├── SemanticElementFactory.cs        # Element creation strategies (NEW)
│   └── AccessibilityDebugger.cs         # Visual debug mode (NEW)
└── ts/Runtime/
    ├── Accessibility.ts                 # TypeScript implementation (MODIFY)
    └── SemanticElements.ts              # Element factories (NEW)

src/Uno.UI/
└── UI/Xaml/Automation/
    ├── Peers/
    │   └── AutomationPeer.cs            # IAutomationPeerListener usage (REFERENCE)
    └── AutomationProperties.cs          # FindHtmlRole extension (MODIFY)

src/Uno.UI.RuntimeTests/
└── Tests/Windows_UI_Xaml_Automation/
    ├── Given_AccessibleButton.cs        # Button/Invoke tests (NEW)
    ├── Given_AccessibleSlider.cs        # RangeValue tests (NEW)
    ├── Given_AccessibleCheckBox.cs      # Toggle tests (NEW)
    ├── Given_AccessibleTextBox.cs       # Value tests (NEW)
    ├── Given_AccessibleComboBox.cs      # ExpandCollapse tests (NEW)
    ├── Given_AccessibleListView.cs      # Selection tests (NEW)
    └── Given_AccessibilityFocus.cs      # Focus management tests (NEW)
```

**Structure Decision**: Extends existing Uno.UI.Runtime.Skia.WebAssembly.Browser project with new accessibility classes. No new projects needed. TypeScript files compile to embedded resource.

## Complexity Tracking

> No violations - table not required.

---

## Phase 0: Research

See [research.md](./research.md) for detailed findings.

### Research Tasks

1. **Existing Implementation Analysis**: Document current WebAssemblyAccessibility.cs capabilities and gaps
2. **Automation Peer Pattern Coverage**: Map all pattern providers to ARIA attributes
3. **TypeScript/JSInterop Patterns**: Best practices for bidirectional event routing
4. **Screen Reader Compatibility**: NVDA/VoiceOver/JAWS behavior differences
5. **EffectiveViewport Integration**: How to detect visible items for virtualization

---

## Phase 1: Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Uno Application                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │   Button    │    │   Slider    │    │  TextBox    │   ...   │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘         │
│         │                  │                  │                 │
│  ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐         │
│  │ ButtonAuto- │    │ SliderAuto- │    │ TextBoxAuto-│         │
│  │ mationPeer  │    │ mationPeer  │    │ mationPeer  │         │
│  │ (IInvoke)   │    │ (IRangeVal) │    │ (IValue)    │         │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘         │
└─────────┼──────────────────┼──────────────────┼─────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              WebAssemblyAccessibility (C#)                       │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │
│  │  AriaMapper    │  │ SemanticElement│  │ Accessibility  │    │
│  │  (pattern →   │  │ Factory        │  │ Debugger       │    │
│  │   ARIA)       │  │                │  │                │    │
│  └───────┬────────┘  └───────┬────────┘  └────────────────┘    │
│          │                   │                                  │
│          ▼                   ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              NativeMethods (JSImport/JSExport)          │   │
│  └──────────────────────────┬──────────────────────────────┘   │
└─────────────────────────────┼───────────────────────────────────┘
                              │ JS Interop
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Accessibility.ts (TypeScript)                 │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │
│  │ Element Factory│  │ Event Bridge   │  │ Debug Renderer │    │
│  │ (button, input │  │ (click →      │  │ (outline mode) │    │
│  │  range, etc)   │  │  OnInvoke)    │  │                │    │
│  └───────┬────────┘  └───────┬────────┘  └────────────────┘    │
│          │                   │                                  │
│          ▼                   ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                 Semantic DOM Tree                        │   │
│  │  #uno-semantics-root (filter: opacity(0%))              │   │
│  │    └─ <button> ← role, aria-label, tabindex, click      │   │
│  │    └─ <input type="range"> ← aria-valuenow/min/max      │   │
│  │    └─ <input type="text"> ← value, input event          │   │
│  │    └─ <div role="listbox"> ← aria-multiselectable       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **Hidden HTML Inputs**: Use native `<input type="range">`, `<input type="text">`, `<button>` for interactive controls rather than divs with ARIA roles. This provides built-in keyboard support and better screen reader compatibility.

2. **Transparent Overlay**: Semantic DOM tree uses `filter: opacity(0%)` to be invisible but focusable. Debug mode toggles visibility.

3. **Debounced Updates**: Visual tree changes trigger 100ms debounce timer before updating semantic DOM.

4. **EffectiveViewport Virtualization**: Subscribe to EffectiveViewport changes for virtualized lists; only create semantic elements for visible items.

5. **Bidirectional Sync**:
   - Uno → DOM: Property changes via IAutomationPeerListener trigger NativeMethods calls
   - DOM → Uno: Event handlers call JSExport methods that invoke automation peer actions

### Data Model

See [data-model.md](./data-model.md) for entity definitions.

### API Contracts

See [contracts/](./contracts/) for TypeScript interfaces.

### Quickstart

See [quickstart.md](./quickstart.md) for developer guide.
