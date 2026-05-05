# Feature Specification: Soft Input Keyboard Handling for Skia Targets

**Created**: 2026-04-02
**Status**: Draft
**Issues**: [#22264](https://github.com/unoplatform/uno/issues/22264), [#5407](https://github.com/unoplatform/uno/issues/5407), uno-private#1154

## Problem Statement

When a soft keyboard appears on mobile devices running Uno Platform Skia apps, focused elements (TextBox, PasswordBox, etc.) are not properly scrolled into view, and content behind the keyboard becomes inaccessible. The severity varies by platform:

- **WASM Skia**: No keyboard detection at all. Browser `window.resize` events from keyboard appearance cause flyouts to reposition/dismiss, making text input in flyouts impossible. `InputPane.OccludedRect` is never set.
- **Android Skia / iOS Skia**: Keyboard detection works, but only the `ScrollContentPresenter.Pad()` mechanism is available. Pages without a ScrollViewer have no way to scroll content into view.
- **All Skia**: No `RootScrollViewer` exists. WinUI wraps all app content in a root-level ScrollViewer that enables whole-page scrolling when the keyboard appears. This is stubbed in Uno but never implemented.
- **All Skia**: `InputPane` is a process-wide singleton, incompatible with multi-window scenarios.

## Goals

1. **WASM keyboard detection**: Detect soft keyboard appearance using the `visualViewport` browser API, set `InputPane.OccludedRect`, and prevent keyboard-triggered viewport changes from affecting XAML layout or dismissing flyouts.
2. **RootScrollViewer**: Implement a custom `RootScrollViewer` class (derived from `ScrollViewer`, matching WinUI) that wraps app content and enables whole-page scrolling when the keyboard appears, even for pages without an inner ScrollViewer.
3. **Per-window InputPane**: Change `InputPane` from a singleton to per-XamlRoot instances, removing `Window.InitialWindow` usage, to support multi-window scenarios.
4. **Automatic scroll-to-focus**: When a text control receives focus while the keyboard is showing, automatically scroll it into the visible area above the keyboard.

## Non-Goals

- Native platform targets (Android native, iOS native, WASM native) - these already work via native mechanisms.
- Desktop Skia platforms without soft keyboards (Win32, X11, macOS) - no soft keyboard to handle.
- SIP transition animations (WinUI's `ApplyInputPaneTransition`) - deferred to follow-up.
- Caret-specific BringIntoView adjustments (WinUI's 75% threshold, 20px padding) - deferred to follow-up.
- BottomAppBar repositioning above keyboard - deferred to follow-up.

## WinUI Reference

The implementation follows WinUI's architecture as analyzed from the C++ sources at `D:\Work\microsoft-ui-xaml2\src\`:

- `RootScrollViewer_Partial.h/.cpp` - Custom ScrollViewer subclass with SIP state management
- `ScrollViewer_Partial.cpp` - `IsRootScrollViewer()` virtual gating behavior suppression
- `InputPaneHandler.cpp` - RSV height shrink/restore on SIP show/hide
- `BringIntoViewHandler.cpp` - Focused element scroll-into-view coordination
- `ContentManager.cpp` - RSV creation and visual tree placement
- `VisualTree.cpp` - `SetPublicRootVisual()` / `AddVisualRootToRootScrollViewer()`

Key WinUI behaviors replicated:
- RSV created with all scrolling disabled, no template, not focusable
- SIP showing: shrink RSV Height, enable scrolling, save offsets, BringIntoView
- SIP hiding: restore Height, disable scrolling, restore offsets
- RSV suppresses pointer/keyboard/focus events when SIP is not showing

Key WinUI behaviors deferred (documented as TODOs):
- `CBringIntoViewHandler` caret adjustments (75% CaretAlignmentThreshold, 20px ExtraPixelsForBringIntoView)
- `ApplyInputPaneTransition()` smooth animations
- `ApplicationBarService.OnBoundsChanged` and `FlyoutBase.NotifyInputPaneStateChange` notifications
- `InputPaneProcessor.NotifyFocusChanged()` text-editable detection (using `FocusManager.GotFocus` instead)

## Implementation

See [plan.md](./plan.md) for the detailed implementation plan with 5 phases:

1. WASM Skia Keyboard Detection (visualViewport API, IInputPaneExtension, flyout fix)
2. RootScrollViewer (custom class, visual tree insertion, XamlIslandRoot integration)
3. Make InputPane Per-Window (per-XamlRoot instances, GetForXamlRoot(), backward-compatible)
4. Connect InputPane to RootScrollViewer (RSV shrink/restore, Pad mechanism, focus change)
5. Platform-Specific Verification (Android Skia, iOS Skia, ContentDialog)

## Testing

### Manual Testing Matrix

| Scenario | WASM Skia | Android Skia | iOS Skia |
|----------|-----------|--------------|----------|
| TextBox at bottom of page (no ScrollViewer) | RSV scrolls into view | RSV scrolls into view | RSV scrolls into view |
| TextBox inside ScrollViewer | Pad + BringIntoView | Pad + BringIntoView | Pad + BringIntoView |
| TextBox in Flyout | Flyout stays, keyboard overlays | Flyout stays | Flyout stays |
| TextBox in ContentDialog | Dialog adjusts up | Dialog adjusts up | Dialog adjusts up |
| Focus change between TextBoxes (keyboard open) | Re-scrolls to new element | Re-scrolls | Re-scrolls |
| Real window resize (orientation/browser resize) | Layout updates normally | Layout updates normally | N/A |
| Keyboard dismiss | RSV restores, content returns | RSV restores | RSV restores |

### Runtime Tests
- BringIntoView with RootScrollViewer present
- InputPane.OccludedRect propagation
- ScrollContentPresenter.Pad still works with RSV
