# Research: WebAssembly Skia Accessibility Enhancement

**Date**: 2026-02-11
**Branch**: `001-wasm-accessibility`

## 1. Existing Implementation Analysis

### Current WebAssemblyAccessibility.cs Capabilities

**Location**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs`

**What Works**:
- Implements `IUnoAccessibility` and `IAutomationPeerListener`
- Creates parallel semantic DOM tree with divs mapped by `IntPtr` handles
- Basic ARIA: `aria-label`, `aria-checked`, `role`, `aria-live` regions
- Focus management with `tabindex`
- Scroll handling via wheel events
- Toggle state sync (CheckBox, RadioButton) via `TogglePatternIdentifiers.ToggleStateProperty`
- Name/label updates via `AutomationElementIdentifiers.NameProperty`
- Scroll offset sync via `ScrollPatternIdentifiers.HorizontalScrollPercentProperty`

**Current Gaps**:
- No slider/range values (`aria-valuenow`, `aria-valuemin`, `aria-valuemax`)
- No hidden inputs for sliders/text (keyboard interaction impossible)
- Missing `aria-expanded`, `aria-selected`, `aria-disabled`
- No invoke/click handling for buttons via assistive technology
- Text input not accessible (no text sync)
- Hardcoded scroll behavior (`horizontallyScrollable = true; verticallyScrollable = true`)

### TypeScript Implementation (Accessibility.ts)

**Location**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts`

**Current Structure**:
- Creates `#uno-semantics-root` container with `filter: opacity(0%)`
- Creates live regions (`aria-live="polite"` and `aria-live="assertive"`)
- Creates enable-accessibility button at `(-1px, -1px)`
- Element creation uses `<div>` elements with unique IDs: `uno-semantics-{handle}`
- Position/size via absolute positioning
- Scroll events forwarded to managed code

**Decision**: Extend existing implementation rather than rewrite. Add element type factories for native inputs.

**Rationale**: The foundation is solid. The gaps are in element types (div vs button/input) and event routing.

**Alternatives Considered**:
- Complete rewrite: Rejected - too much working code to discard
- Third-party accessibility library: Rejected - adds dependency, may conflict with Uno patterns

---

## 2. Automation Peer Pattern Coverage

### Pattern-to-ARIA Mapping

| Automation Pattern | Interface | ARIA Attributes | HTML Element |
|-------------------|-----------|-----------------|--------------|
| Invoke | `IInvokeProvider` | `role="button"` | `<button>` |
| Toggle | `IToggleProvider` | `aria-checked` | `<input type="checkbox">` or `<input type="radio">` |
| RangeValue | `IRangeValueProvider` | `aria-valuenow`, `aria-valuemin`, `aria-valuemax` | `<input type="range">` |
| Value | `IValueProvider` | `role="textbox"` | `<input type="text">` or `<textarea>` |
| ExpandCollapse | `IExpandCollapseProvider` | `aria-expanded`, `aria-haspopup` | `<div>` with attributes |
| Selection | `ISelectionProvider` | `aria-multiselectable`, `role="listbox"` | `<div role="listbox">` |
| SelectionItem | `ISelectionItemProvider` | `aria-selected`, `aria-posinset`, `aria-setsize` | `<div role="option">` |
| Scroll | `IScrollProvider` | `aria-controls` | Container with `overflow: scroll` |

### Control Type Mapping (AutomationControlType → ARIA role)

```csharp
AutomationControlType.Button → "button"
AutomationControlType.CheckBox → "checkbox"
AutomationControlType.RadioButton → "radio"
AutomationControlType.Slider → "slider"
AutomationControlType.Edit → "textbox"
AutomationControlType.ComboBox → "combobox"
AutomationControlType.List → "listbox"
AutomationControlType.ListItem → "option"
AutomationControlType.Menu → "menu"
AutomationControlType.MenuItem → "menuitem"
AutomationControlType.Tab → "tablist"
AutomationControlType.TabItem → "tab"
AutomationControlType.Tree → "tree"
AutomationControlType.TreeItem → "treeitem"
AutomationControlType.ProgressBar → "progressbar"
AutomationControlType.ScrollBar → "scrollbar"
AutomationControlType.Text → "label"
AutomationControlType.Hyperlink → "link"
AutomationControlType.Image → "img"
AutomationControlType.Group → "group"
AutomationControlType.Header → "heading"
```

**Decision**: Use native HTML elements where possible (`<button>`, `<input>`), fall back to ARIA roles on `<div>` for complex widgets.

**Rationale**: Native elements provide built-in keyboard support and better screen reader recognition.

---

## 3. TypeScript/JSInterop Patterns

### Existing JSImport Methods (NativeMethods)

```csharp
AddRootElementToSemanticsRoot(handle, width, height, x, y, isFocusable)
AddSemanticElement(parentHandle, handle, index, width, height, x, y, role, automationId, isFocusable, ariaChecked, isVisible, horizontallyScrollable, verticallyScrollable, temporary)
RemoveSemanticElement(parentHandle, childHandle)
UpdateAriaLabel(handle, automationId)
UpdateAriaChecked(handle, ariaChecked)
UpdateNativeScrollOffsets(handle, horizontalOffset, verticalOffset)
UpdateSemanticElementPositioning(handle, width, height, x, y)
UpdateIsFocusable(handle, isFocusable)
HideSemanticElement(handle)
AnnouncePolite(text)
AnnounceAssertive(text)
```

### New JSImport Methods Required

```csharp
// Element type-specific creation
CreateButtonElement(handle, x, y, width, height, label, disabled)
CreateSliderElement(handle, x, y, width, height, value, min, max, step, orientation)
CreateTextBoxElement(handle, x, y, width, height, value, multiline, password, readonly)
CreateCheckboxElement(handle, x, y, width, height, checked, label)
CreateComboBoxElement(handle, x, y, width, height, expanded, selectedValue)
CreateListBoxElement(handle, x, y, width, height, multiselect)
CreateListItemElement(handle, x, y, width, height, selected, positionInSet, sizeOfSet)

// State updates
UpdateSliderValue(handle, value, min, max)
UpdateTextBoxValue(handle, value, selectionStart, selectionEnd)
UpdateExpandCollapseState(handle, expanded)
UpdateSelectionState(handle, selected)
UpdateDisabledState(handle, disabled)

// Debug mode
EnableDebugMode(enabled)

// Focus
FocusSemanticElement(handle)
```

### New JSExport Callbacks Required

```csharp
[JSExport] void OnInvoke(IntPtr handle)
[JSExport] void OnToggle(IntPtr handle)
[JSExport] void OnRangeValueChange(IntPtr handle, double value)
[JSExport] void OnTextInput(IntPtr handle, string value, int selectionStart, int selectionEnd)
[JSExport] void OnExpandCollapse(IntPtr handle)
[JSExport] void OnSelection(IntPtr handle)
[JSExport] void OnFocus(IntPtr handle)
[JSExport] void OnBlur(IntPtr handle)
```

**Decision**: Add new JSImport methods for type-specific element creation; keep generic `AddSemanticElement` for fallback.

**Rationale**: Type-specific methods allow TypeScript to create appropriate native HTML elements with proper event handlers.

---

## 4. Screen Reader Compatibility

### NVDA (Windows - Primary)

- Excellent support for native HTML inputs
- Announces `aria-valuenow` changes on sliders
- Respects `aria-live` regions reliably
- Tab navigation works with `tabindex="0"`

### VoiceOver (macOS/Safari)

- Quirks with `aria-live` requiring non-breaking space workaround
- Modal dialogs require aria-live elements inside modal
- Better support for native `<input>` than divs with roles

### JAWS (Windows)

- Similar to NVDA
- More strict about ARIA attribute correctness
- Prefers native HTML elements

### TalkBack (Android via browser)

- WebAssembly support varies by browser
- Touch gesture handling may conflict with canvas interactions

**Decision**: Target NVDA as primary, VoiceOver as secondary. Use native HTML elements for maximum compatibility.

**Rationale**: NVDA is most common on Windows; VoiceOver covers macOS. Native elements work better across all readers.

---

## 5. EffectiveViewport Integration

### How EffectiveViewport Works

`EffectiveViewport` is exposed on `FrameworkElement` and reports the visible portion of an element accounting for clipping by parent scroll viewers.

```csharp
element.EffectiveViewportChanged += (s, e) => {
    var viewport = e.EffectiveViewport;
    // viewport.X, Y, Width, Height define visible area
};
```

### Integration Strategy for Virtualized Lists

1. **On list item added**: Check if item's bounds intersect with parent's EffectiveViewport
2. **On viewport change**:
   - Create semantic elements for newly visible items
   - Remove semantic elements for items scrolled out of view
3. **Buffer zone**: Consider small buffer (e.g., 1-2 items beyond viewport) to prevent flicker

### Implementation Approach

```csharp
// In WebAssemblyAccessibility
private void OnEffectiveViewportChanged(FrameworkElement element, EffectiveViewportChangedEventArgs e)
{
    if (!IsAccessibilityEnabled) return;

    if (element is ItemsControl itemsControl)
    {
        var viewport = e.EffectiveViewport;
        foreach (var item in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromItem(item);
            if (container is UIElement uiElement)
            {
                var bounds = uiElement.TransformToVisual(itemsControl).TransformBounds(
                    new Rect(0, 0, uiElement.ActualSize.X, uiElement.ActualSize.Y));

                bool isVisible = bounds.IntersectsWith(viewport);
                UpdateSemanticElementVisibility(uiElement, isVisible);
            }
        }
    }
}
```

**Decision**: Use EffectiveViewport for virtualization. Create/remove semantic elements based on visibility.

**Rationale**: Aligns with clarification decision. Leverages existing Uno infrastructure.

---

## 6. Flutter Reference Implementation

### Key Patterns from Flutter Web Semantics

1. **Parallel DOM Tree**: `<flt-semantics>` elements overlay canvas with `filter: opacity(0%)`
2. **Hidden Inputs**: Uses `<input type="range">` for sliders with surrogate values
3. **Role-Based Behaviors**: Composable behaviors (Focusable, Tappable, Checkable) attached to roles
4. **Gesture Mode Coordination**: Switches between browser gestures and framework pointer events
5. **Click Debouncing**: Prevents double-handling of clicks by framework and AT
6. **Label Strategies**: Three approaches (aria-label, DOM text, sized spans)

### Applicable Patterns for Uno

| Flutter Pattern | Uno Equivalent |
|----------------|----------------|
| `<flt-semantics>` | `#uno-semantics-root` children |
| Hidden `<input type="range">` | Adopt for Slider |
| SemanticRole behaviors | AriaMapper + SemanticElementFactory |
| Gesture mode flag | Consider for touch vs keyboard detection |
| Debug outline mode | Adopt for FR-034 |

**Decision**: Adopt Flutter's hidden input pattern for interactive controls and debug outline mode.

**Rationale**: Proven approach with good screen reader compatibility.

---

## Summary of Decisions

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Overall approach | Extend existing implementation | Foundation solid; gaps addressable |
| Element types | Native HTML where possible | Built-in keyboard support |
| Pattern mapping | Type-specific element factories | Proper semantics per control type |
| Screen reader target | NVDA primary, VoiceOver secondary | Coverage of main platforms |
| Virtualization | EffectiveViewport-based | Aligns with Uno infrastructure |
| Debug mode | Visible outline overlay | Easier debugging of a11y tree |
| Event routing | JSExport callbacks | Bidirectional sync pattern |
