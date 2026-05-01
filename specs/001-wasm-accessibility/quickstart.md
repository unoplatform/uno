# Quickstart: WebAssembly Skia Accessibility Enhancement

**Date**: 2026-02-11
**Branch**: `001-wasm-accessibility`

## Overview

This guide explains how to work with the enhanced WebAssembly accessibility layer. The feature enables screen readers and keyboard-only users to fully interact with Uno Platform applications running on Skia WebAssembly.

---

## For Application Developers

### Accessibility is Automatic

If your Uno application uses standard controls (Button, CheckBox, Slider, TextBox, ComboBox, ListView, etc.), accessibility support is automatic. The framework:

1. Creates semantic HTML elements overlaying the canvas
2. Maps control properties to ARIA attributes
3. Routes keyboard interactions to your controls

### Setting Accessible Names

Use `AutomationProperties.Name` to set the accessible name announced by screen readers:

```xml
<Button Content="Submit"
        AutomationProperties.Name="Submit form" />

<Slider Value="{x:Bind Volume}"
        AutomationProperties.Name="Volume control" />
```

### Testing with Screen Readers

1. **Enable accessibility**: Click the hidden button at top-left corner (keyboard-accessible) or tab into the app
2. **NVDA (Windows)**: Download from nvaccess.org, press NVDA+N to start
3. **VoiceOver (macOS)**: Press Cmd+F5 to toggle

### Debug Mode

To visualize the semantic element overlay:

```csharp
// In your app startup or debug menu
#if DEBUG
Uno.UI.Runtime.Skia.WebAssemblyAccessibility.Instance.EnableDebugMode(true);
#endif
```

This shows green outlines around all semantic elements, helpful for diagnosing accessibility tree issues.

---

## For Framework Contributors

### Project Structure

```
src/Uno.UI.Runtime.Skia.WebAssembly.Browser/
├── Accessibility/
│   ├── WebAssemblyAccessibility.cs   # Main coordinator
│   ├── AriaMapper.cs                  # Pattern → ARIA mapping
│   ├── SemanticElementFactory.cs      # Element type selection
│   └── AccessibilityDebugger.cs       # Debug overlay
└── ts/Runtime/
    ├── Accessibility.ts               # TypeScript implementation
    └── SemanticElements.ts            # Element factories
```

### Adding Support for a New Control

1. **Ensure automation peer exists** in `src/Uno.UI/UI/Xaml/Automation/Peers/`
2. **Add control type mapping** in `AriaMapper.cs`:

```csharp
case AutomationControlType.YourControl:
    return "appropriate-aria-role";
```

3. **Add element factory** if needed in `SemanticElements.ts`:

```typescript
createYourControlElement(params: YourControlParams): HTMLElement {
    const element = document.createElement('div');
    element.setAttribute('role', 'your-role');
    // ... set attributes and event handlers
    return element;
}
```

4. **Add pattern handler** if the control has special interaction patterns

### Pattern Implementation Checklist

For each automation pattern, implement:

| Pattern | C# | TypeScript |
|---------|-----|------------|
| IInvokeProvider | `OnInvoke` JSExport | Click/Enter/Space handler |
| IToggleProvider | `OnToggle` JSExport | Change event handler |
| IRangeValueProvider | `OnRangeValueChange` JSExport | Input event handler |
| IValueProvider | `OnTextInput` JSExport | Input/Composition handlers |
| IExpandCollapseProvider | `OnExpandCollapse` JSExport | Click/Enter handler |
| ISelectionItemProvider | `OnSelection` JSExport | Click/Enter/Space handler |

### Running Tests

```bash
# Build for WebAssembly
cd src
dotnet build Uno.UI-Wasm-only.slnf

# Run accessibility runtime tests
cd src/SamplesApp/SamplesApp.Skia.Generic
dotnet run --configuration Release -- --runtime-tests="*Accessible*"
```

### Manual Testing Protocol

1. **Keyboard Navigation**:
   - Tab through all controls
   - Verify focus order matches visual order
   - Test Enter/Space activation

2. **Screen Reader Verification**:
   - Control names announced correctly
   - States (checked, expanded, selected) announced
   - Value changes announced for sliders

3. **Debug Mode**:
   - Enable debug overlay
   - Verify all interactive controls have semantic elements
   - Check positioning matches visual controls

---

## Architecture Overview

```
User Input (Keyboard/Screen Reader)
           │
           ▼
    ┌──────────────┐
    │ Semantic DOM │ ← Transparent overlay
    │   <button>   │
    │   <input>    │
    └──────┬───────┘
           │ DOM Events
           ▼
    ┌──────────────┐
    │ Accessibility│ ← TypeScript
    │     .ts      │
    └──────┬───────┘
           │ JSExport
           ▼
    ┌──────────────┐
    │ WebAssembly- │ ← C#
    │ Accessibility│
    └──────┬───────┘
           │ Automation Peer
           ▼
    ┌──────────────┐
    │ Uno Control  │
    │ (Button, etc)│
    └──────────────┘
```

### Data Flow: User Activates Button

1. User presses Enter on focused `<button>` semantic element
2. TypeScript `click` event handler fires
3. Calls `managedOnInvoke(handle)` via JSExport
4. C# `OnInvoke` retrieves UIElement from handle
5. Gets automation peer: `element.GetOrCreateAutomationPeer()`
6. Gets pattern: `peer.GetPattern(PatternInterface.Invoke)`
7. Invokes: `invokeProvider.Invoke()`
8. Button's Click event fires in application code

### Data Flow: Slider Value Updates

1. Application sets `slider.Value = 50`
2. `RangeBaseAutomationPeer` raises property changed
3. `IAutomationPeerListener.NotifyPropertyChangedEvent` called
4. C# calls `NativeMethods.UpdateSliderValue(handle, 50, 0, 100)`
5. TypeScript updates `<input type="range">` value and aria attributes
6. Screen reader announces new value

---

## Troubleshooting

### Control Not Announced

1. Verify control has automation peer: `control.GetOrCreateAutomationPeer() != null`
2. Check `AutomationProperties.Name` is set
3. Enable debug mode to see if semantic element exists
4. Check browser DevTools accessibility inspector

### Keyboard Interaction Not Working

1. Verify semantic element has `tabindex="0"`
2. Check element type (should be `<button>`, `<input>`, not just `<div>`)
3. Verify event handlers are attached in TypeScript
4. Check browser console for JavaScript errors

### Focus Not Syncing

1. Check `IsFocusable` is true for the control
2. Verify focus event handlers call `managedOnFocus`
3. Check for competing focus management in application code

### Performance Issues

1. For large lists, verify virtualization is working (only visible items should have semantic elements)
2. Check debounce timer is preventing excessive DOM updates
3. Profile with browser DevTools Performance tab
