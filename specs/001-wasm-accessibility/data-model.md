# Data Model: WebAssembly Skia Accessibility Enhancement

**Date**: 2026-02-11
**Branch**: `001-wasm-accessibility`

## Overview

This document defines the core entities and their relationships for the WebAssembly accessibility layer. The data model bridges Uno's automation peer system with the browser's accessibility tree via semantic DOM elements.

---

## Core Entities

### 1. SemanticElement

Represents a DOM element in the accessibility layer that corresponds to a Uno UIElement.

**Attributes**:

| Attribute | Type | Description |
|-----------|------|-------------|
| Handle | `IntPtr` | Unique identifier linking to Uno UIElement's Visual handle |
| ElementType | `SemanticElementType` | Type of HTML element to create (Button, Slider, TextBox, etc.) |
| ParentHandle | `IntPtr` | Handle of parent semantic element |
| AriaAttributes | `AriaAttributes` | Collection of ARIA attributes for this element |
| Position | `(float X, float Y)` | Absolute position relative to semantics root |
| Size | `(float Width, float Height)` | Element dimensions |
| IsFocusable | `bool` | Whether element can receive keyboard focus |
| IsVisible | `bool` | Whether element is in EffectiveViewport |

**Lifecycle**:
- Created when UIElement enters visual tree (and is visible in viewport for virtualized lists)
- Updated on property changes via IAutomationPeerListener
- Removed when UIElement leaves visual tree (or scrolls out of viewport)

---

### 2. SemanticElementType (Enum)

Determines which HTML element is created and what event handlers are attached.

```
Generic      → <div> with ARIA role
Button       → <button>
Checkbox     → <input type="checkbox">
RadioButton  → <input type="radio">
Slider       → <input type="range">
TextBox      → <input type="text">
TextArea     → <textarea>
Password     → <input type="password">
ComboBox     → <div role="combobox">
ListBox      → <div role="listbox">
ListItem     → <div role="option">
Link         → <a>
```

---

### 3. AriaAttributes

Collection of ARIA attributes for a semantic element.

**Attributes**:

| Attribute | Type | ARIA Mapping | Source |
|-----------|------|--------------|--------|
| Role | `string?` | `role` | AutomationControlType |
| Label | `string?` | `aria-label` | AutomationPeer.GetName() |
| Checked | `string?` | `aria-checked` | IToggleProvider.ToggleState |
| Expanded | `bool?` | `aria-expanded` | IExpandCollapseProvider.ExpandCollapseState |
| Selected | `bool?` | `aria-selected` | ISelectionItemProvider.IsSelected |
| Disabled | `bool` | `aria-disabled` | !AutomationPeer.IsEnabled() |
| Required | `bool` | `aria-required` | AutomationProperties.IsRequiredForForm |
| ValueNow | `double?` | `aria-valuenow` | IRangeValueProvider.Value |
| ValueMin | `double?` | `aria-valuemin` | IRangeValueProvider.Minimum |
| ValueMax | `double?` | `aria-valuemax` | IRangeValueProvider.Maximum |
| PositionInSet | `int?` | `aria-posinset` | AutomationPeer.GetPositionInSet() |
| SizeOfSet | `int?` | `aria-setsize` | AutomationPeer.GetSizeOfSet() |
| Level | `int?` | `aria-level` | AutomationPeer.GetHeadingLevel() |
| MultiSelectable | `bool?` | `aria-multiselectable` | ISelectionProvider.CanSelectMultiple |
| HasPopup | `string?` | `aria-haspopup` | Derived from control type |
| Controls | `string?` | `aria-controls` | Related element ID |
| DescribedBy | `string?` | `aria-describedby` | AutomationProperties.DescribedBy |
| LabelledBy | `string?` | `aria-labelledby` | AutomationProperties.LabeledBy |

---

### 4. PatternCapabilities

Describes which automation patterns a UIElement supports, determined at element creation.

**Attributes**:

| Attribute | Type | Description |
|-----------|------|-------------|
| CanInvoke | `bool` | Has IInvokeProvider |
| CanToggle | `bool` | Has IToggleProvider |
| CanRangeValue | `bool` | Has IRangeValueProvider |
| CanValue | `bool` | Has IValueProvider |
| CanExpandCollapse | `bool` | Has IExpandCollapseProvider |
| CanSelect | `bool` | Has ISelectionItemProvider |
| CanScroll | `bool` | Has IScrollProvider |

---

### 5. AccessibilityState

Global state for the accessibility system.

**Attributes**:

| Attribute | Type | Description |
|-----------|------|-------------|
| IsEnabled | `bool` | Whether accessibility is active |
| DebugModeEnabled | `bool` | Whether visual debug outlines are shown |
| UpdateTimer | `Timer?` | Debounce timer for DOM updates |
| PendingUpdates | `Queue<UpdateAction>` | Queued updates during debounce |
| ElementMap | `Dictionary<IntPtr, SemanticElement>` | Handle-to-element lookup |
| FocusedHandle | `IntPtr?` | Currently focused element handle |

---

## Relationships

```
┌─────────────────────────────────────────────────────────────────────┐
│                          UIElement                                   │
│  - Visual.Handle (IntPtr)                                           │
│  - GetOrCreateAutomationPeer() → AutomationPeer                     │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ 1:1
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        AutomationPeer                                │
│  - GetPattern(PatternInterface) → IProvider                         │
│  - GetName(), GetAutomationControlType()                            │
│  - IsEnabled(), GetPositionInSet(), etc.                            │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ extracts
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         AriaMapper                                   │
│  - GetAriaAttributes(peer) → AriaAttributes                         │
│  - GetSemanticElementType(peer) → SemanticElementType               │
│  - GetPatternCapabilities(peer) → PatternCapabilities               │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ creates
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       SemanticElement                                │
│  - Handle, ElementType, AriaAttributes                              │
│  - Position, Size, IsFocusable, IsVisible                           │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ JS Interop
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      DOM Element (Browser)                           │
│  - <button>, <input>, <div role="...">                              │
│  - Event listeners → JSExport callbacks                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## State Transitions

### SemanticElement Visibility

```
                    ┌─────────────┐
                    │   Created   │
                    └──────┬──────┘
                           │ EnterViewport
                           ▼
                    ┌─────────────┐
            ┌───────│   Visible   │───────┐
            │       └─────────────┘       │
  ExitViewport│                           │PropertyChange
            │       ┌─────────────┐       │
            └──────▶│   Hidden    │◀──────┘
                    └──────┬──────┘
                           │ Removed
                           ▼
                    ┌─────────────┐
                    │  Disposed   │
                    └─────────────┘
```

### Update Debouncing

```
PropertyChange → Queue Update → Start Timer (100ms)
                     │
                     ▼
            ┌─────────────────┐
            │ More changes?   │──Yes──▶ Reset Timer
            └────────┬────────┘
                     │ No (timer fires)
                     ▼
            ┌─────────────────┐
            │ Flush all       │
            │ queued updates  │
            └─────────────────┘
```

---

## Validation Rules

1. **Handle Uniqueness**: Each SemanticElement must have a unique Handle
2. **Parent Existence**: ParentHandle must reference an existing element (except root)
3. **Position Non-Negative**: X and Y must be >= 0
4. **Size Positive**: Width and Height must be > 0
5. **Value Range**: For sliders, ValueNow must be between ValueMin and ValueMax
6. **PositionInSet Range**: If set, must be >= 1 and <= SizeOfSet
