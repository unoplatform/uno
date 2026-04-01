---
uid: Uno.Features.Accessibility.AutomationPeers
---

# Automation peers

> [!TIP]
> This article covers Uno-specific details about how automation peers work on Skia targets. For a full description of the peer model and how to create custom peers, see [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers).

Every XAML control exposes its accessibility information through an **automation peer**. Uno implements the same `AutomationPeer` model as WinUI — when you use standard controls, the correct peer is created automatically.

## Built-in peers and ARIA role mapping

On WASM, the ARIA role is derived from the `AutomationControlType` reported by the peer. The full mapping is defined in the `AriaMapper` class.

| Control | Automation Peer | ARIA Role (WASM) |
|---------|----------------|------------------|
| `Button` | `ButtonAutomationPeer` | `button` |
| `CheckBox` | `CheckBoxAutomationPeer` | `checkbox` |
| `RadioButton` | `RadioButtonAutomationPeer` | `radio` |
| `Slider` | `SliderAutomationPeer` | `slider` |
| `TextBox` | `TextBoxAutomationPeer` | `textbox` |
| `PasswordBox` | `PasswordBoxAutomationPeer` | `textbox` (password) |
| `ComboBox` | `ComboBoxAutomationPeer` | `combobox` |
| `ToggleSwitch` | `ToggleSwitchAutomationPeer` | `button` (with `aria-pressed`) |
| `ToggleButton` | `ToggleButtonAutomationPeer` | `button` (with `aria-pressed`) |
| `ListView` | `ListViewAutomationPeer` | `listbox` |
| `ListViewItem` | `ListViewItemAutomationPeer` | `option` |
| `HyperlinkButton` | `HyperlinkButtonAutomationPeer` | `link` |
| `Image` | `ImageAutomationPeer` | `img` |
| `ProgressBar` | `ProgressBarAutomationPeer` | `progressbar` |
| `TextBlock` | `TextBlockAutomationPeer` | (none — text role) |

## Supported automation patterns

Automation patterns define the interaction capabilities of a control. All patterns below are routed through the Skia accessibility layer to each platform's native API.

| Pattern | Interface | Used For |
|---------|-----------|----------|
| Invoke | `IInvokeProvider` | Single-action controls (buttons, links) |
| Toggle | `IToggleProvider` | Two-state controls (checkboxes, toggle buttons) |
| RangeValue | `IRangeValueProvider` | Controls with a numeric range (sliders) |
| Value | `IValueProvider` | Controls with a text value (text boxes) |
| ExpandCollapse | `IExpandCollapseProvider` | Expandable controls (combo boxes, tree items) |
| Selection | `ISelectionProvider` | Containers that manage selected items (lists) |
| SelectionItem | `ISelectionItemProvider` | Individual selectable items |
| Scroll | `IScrollProvider` | Scrollable content areas |
| ScrollItem | `IScrollItemProvider` | Items that can be scrolled into view |
| Grid | `IGridProvider` | Grid layouts |
| GridItem | `IGridItemProvider` | Items within a grid |
| Table | `ITableProvider` | Table structures |

## Skia accessibility architecture

On all Skia targets (Win32, macOS, WASM, Android), the accessibility tree is built from automation peers by `SkiaAccessibilityBase`. This shared layer:

- Queries each peer for its name, role, states, and patterns
- Routes property changes and focus events to the platform-specific implementation
- Prunes structural elements (like `Grid`, `Border`, `ContentPresenter`) that have no accessible information, to keep the tree compact

### Platform-specific behavior

- **Win32** — Peers are exposed as UIAutomation provider nodes. Narrator and other UIAutomation clients query the tree directly.
- **macOS** — Each peer becomes an `NSAccessibilityElement` that VoiceOver can discover.
- **WASM** — Each peer produces a hidden DOM element with appropriate ARIA attributes (`role`, `aria-label`, `aria-checked`, etc.).

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
- [Role override](role-override.md)
- [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
