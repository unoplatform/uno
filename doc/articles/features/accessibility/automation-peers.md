---
uid: Uno.Features.Accessibility.AutomationPeers
---

# Custom automation peers

> [!TIP]
> This article covers Uno-specific information for automation peers. For a full description of the peer model and how to extend it, see [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers).

Every XAML control exposes its accessibility information through an **automation peer** — a companion object that describes the control's role, name, states, and supported interaction patterns to assistive technologies. Uno Platform implements the same `AutomationPeer` model as WinUI, including over 50 built-in peers for standard controls.

## Built-in peers

When you use standard XAML controls, the correct automation peer is created automatically. For example:

| Control | Automation Peer | ARIA Role (WASM) |
|---------|----------------|------------------|
| `Button` | `ButtonAutomationPeer` | `button` |
| `CheckBox` | `CheckBoxAutomationPeer` | `checkbox` |
| `RadioButton` | `RadioButtonAutomationPeer` | `radio` |
| `Slider` | `SliderAutomationPeer` | `slider` |
| `TextBox` | `TextBoxAutomationPeer` | `textbox` |
| `PasswordBox` | `PasswordBoxAutomationPeer` | `textbox` (password) |
| `ComboBox` | `ComboBoxAutomationPeer` | `combobox` |
| `ToggleSwitch` | `ToggleSwitchAutomationPeer` | `button` (with toggle state) |
| `ListView` | `ListViewAutomationPeer` | `listbox` |
| `ListViewItem` | `ListViewItemAutomationPeer` | `option` |
| `HyperlinkButton` | `HyperlinkButtonAutomationPeer` | `link` |
| `Image` | `ImageAutomationPeer` | `img` |
| `ProgressBar` | `ProgressBarAutomationPeer` | `progressbar` |
| `TextBlock` | `TextBlockAutomationPeer` | (none — text role) |
| `ScrollViewer` | `ScrollViewerAutomationPeer` | (none — pane) |

On WASM, the ARIA role is derived from the `AutomationControlType` reported by the peer. The full mapping is defined in the `AriaMapper` class.

## Creating a custom peer

If you create a custom control, you need to provide an automation peer so assistive technologies can interact with it. Override `OnCreateAutomationPeer()` in your control:

```csharp
public class StarRating : Control
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new StarRatingAutomationPeer(this);
}
```

Then create the peer class by deriving from `FrameworkElementAutomationPeer` (or a more specific peer base class):

```csharp
public class StarRatingAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    public StarRatingAutomationPeer(StarRating owner) : base(owner) { }

    private StarRating OwnerStarRating => (StarRating)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Slider;

    protected override string GetClassNameCore()
        => nameof(StarRating);

    protected override string GetNameCore()
    {
        // Prefer explicitly set AutomationProperties.Name
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name))
            return name;

        return $"{OwnerStarRating.Value} of {OwnerStarRating.Maximum} stars";
    }

    // IRangeValueProvider implementation
    public double Value => OwnerStarRating.Value;
    public double Minimum => 0;
    public double Maximum => OwnerStarRating.Maximum;
    public double SmallChange => 1;
    public double LargeChange => 1;
    public bool IsReadOnly => !OwnerStarRating.IsEnabled;

    public void SetValue(double value)
    {
        OwnerStarRating.Value = value;
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue)
            return this;

        return base.GetPatternCore(patternInterface);
    }
}
```

## Automation patterns

Automation patterns define the interaction capabilities of a control. The peer reports which patterns it supports, and assistive technologies use them to interact with the control.

| Pattern | Interface | Used For |
|---------|-----------|----------|
| Invoke | `IInvokeProvider` | Single-action controls (buttons, links) |
| Toggle | `IToggleProvider` | Two-state controls (checkboxes, toggle buttons) |
| RangeValue | `IRangeValueProvider` | Controls with a numeric range (sliders, spinners) |
| Value | `IValueProvider` | Controls with a text value (text boxes) |
| ExpandCollapse | `IExpandCollapseProvider` | Expandable controls (combo boxes, tree items) |
| Selection | `ISelectionProvider` | Containers that manage selected items (lists) |
| SelectionItem | `ISelectionItemProvider` | Individual selectable items in a container |
| Scroll | `IScrollProvider` | Scrollable content areas |
| ScrollItem | `IScrollItemProvider` | Items that can be scrolled into view |

All of these patterns are routed through the Skia accessibility layer to the platform's native accessibility API.

## Announcing state changes

When a control's state changes (e.g., a checkbox is toggled), the automation peer must raise the appropriate event so the screen reader announces the change. Use `RaisePropertyChangedEvent`:

```csharp
// In the control's state change handler:
peer.RaisePropertyChangedEvent(
    TogglePatternIdentifiers.ToggleStateProperty,
    oldValue,
    newValue);
```

Built-in controls handle this automatically. You only need to raise events in custom peer implementations.

## Key implementation notes

### Skia targets

On all Skia targets (Win32, macOS, WASM, Android), the accessibility tree is built from automation peers by `SkiaAccessibilityBase`. This shared layer:

- Queries each peer for its name, role, states, and patterns
- Routes property changes and focus events to the platform-specific implementation
- Prunes structural elements (like `Grid`, `Border`, `ContentPresenter`) that have no accessible information, to keep the tree compact

### Platform-specific behavior

- **Win32**: Peers are exposed as UIAutomation provider nodes. Narrator and other UIAutomation clients query the tree directly.
- **macOS**: Each peer becomes an `NSAccessibilityElement` that VoiceOver can discover.
- **WASM**: Each peer produces a hidden DOM element with appropriate ARIA attributes (`role`, `aria-label`, `aria-checked`, etc.).

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
- [Role override](role-override.md)
- [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
