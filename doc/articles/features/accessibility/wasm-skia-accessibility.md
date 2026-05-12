---
uid: Uno.Features.Accessibility.WasmSkiaAccessibility
---

# WASM accessibility with Skia rendering

This guide explains how accessibility works in Uno Platform WebAssembly (WASM) applications that use the Skia rendering engine, and what you — as an app developer — need to know to ship an accessible web app.

> [!TIP]
> If you are already familiar with WinUI accessibility concepts (`AutomationProperties`, `AutomationPeer`), most of what you know applies directly. This guide highlights the WASM-specific behavior and activation flow.

## How Skia WASM accessibility works

When Uno renders your app with Skia on WASM, all visual content is drawn onto a single HTML `<canvas>` element. By itself, a canvas is opaque to assistive technologies — screen readers cannot see the buttons, text, or controls inside it.

To solve this, Uno automatically builds a **hidden semantic DOM** alongside the canvas. This DOM mirrors the accessibility-relevant parts of your visual tree: for every `Button`, `TextBox`, `Slider`, or other control, a corresponding hidden HTML element is created with the appropriate [ARIA](https://www.w3.org/WAI/standards-guidelines/aria/) role and attributes. Screen readers interact with this semantic DOM, not the canvas.

```
┌─────────────────────────────────┐
│  Browser Window                 │
│                                 │
│  ┌───────────────────────────┐  │
│  │  <canvas>                 │  │
│  │   (Skia visual output)    │  │
│  └───────────────────────────┘  │
│                                 │
│  ┌───────────────────────────┐  │
│  │  #uno-semantics-root      │  │
│  │   (hidden semantic DOM)   │  │
│  │   ├─ <button>             │  │
│  │   ├─ <input type="text">  │  │
│  │   ├─ <h1>                 │  │
│  │   └─ ...                  │  │
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

> [!NOTE]
> The semantic DOM is visually hidden (off-screen) but remains in the document's accessibility tree. Screen readers navigate the semantic elements, and keyboard focus is synchronized between the semantic DOM and the Skia visual tree.

## Activation flow

To avoid unnecessary performance overhead, the accessibility layer is **not active by default**. It activates through the following flow:

1. The user presses the **Tab** key.
2. An **"Enable accessibility"** button appears on screen.
3. The user activates the button (click or `Space`).
4. The full semantic tree is created and screen reader navigation becomes available.

This design ensures that apps without accessibility needs do not pay the cost of maintaining the semantic DOM.

### Enabling accessibility on startup

If your app must be accessible immediately — for example, a kiosk application or a compliance requirement — skip the activation button by setting this property **before the host is built** (typically in `App.cs` or `App.xaml.cs` constructor, before `MainWindow` is created):

```csharp
Uno.UI.FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true;
```

This creates the semantic tree as soon as the app loads, without waiting for user interaction. The property is read once during the accessibility subsystem initialization, so setting it after the window is built has no effect for the current session.

### Programmatic activation

You can also activate the accessibility layer from JavaScript in the browser console, which is useful during development:

```js
document.getElementById('uno-enable-accessibility').click();
```

## What you need to do

If you are already using `AutomationProperties` in your XAML, most of the work is done. The framework maps those properties to ARIA attributes automatically.

### 1. Set accessible names on interactive controls

Every interactive control should have a name that the screen reader announces.

```xml
<Button Content="Save"
        AutomationProperties.Name="Save settings" />

<TextBox AutomationProperties.Name="Email address"
         AutomationProperties.HelpText="Enter your email to receive notifications" />

<Image Source="logo.png"
       AutomationProperties.Name="Company logo" />
```

> [!TIP]
> If the `Button.Content` is already a descriptive string, you do not need to set `AutomationProperties.Name` separately — the content text is used automatically. Set it explicitly when the content is an icon, image, or a complex template.

### 2. Mark headings for page structure

Screen reader users rely on headings to navigate. Use `AutomationProperties.HeadingLevel` to define the page outline:

```xml
<TextBlock Text="Account Settings"
           AutomationProperties.HeadingLevel="Level1" />

<TextBlock Text="Profile"
           AutomationProperties.HeadingLevel="Level2" />
```

On WASM, heading levels are rendered as `<h1>` through `<h6>` elements in the semantic DOM, which screen readers expose in their heading navigation (Rotor on VoiceOver, Headings list on NVDA).

### 3. Define landmarks for major regions

Landmarks help screen reader users jump between major sections of the UI:

```xml
<StackPanel AutomationProperties.LandmarkType="Navigation">
    <!-- Navigation links -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Main">
    <!-- Primary content -->
</StackPanel>
```

Supported values: `Main`, `Navigation`, `Search`, `Form`, `Custom`. On WASM, these map to ARIA landmark roles (`role="main"`, `role="navigation"`, etc.).

### 4. Announce dynamic content changes

When content updates without a page navigation — such as a status message, a toast, or a counter — use `AutomationProperties.LiveSetting` so screen readers announce the change:

```xml
<TextBlock x:Name="StatusMessage"
           AutomationProperties.LiveSetting="Polite" />
```

- `Polite` — the announcement waits until the screen reader finishes its current output.
- `Assertive` — the announcement interrupts immediately. Use sparingly.

To trigger an announcement, update the `Text` of an element that has `LiveSetting` set. The framework handles the ARIA live-region notification automatically.

### 5. Override the ARIA role when needed

In rare cases, the default ARIA role assigned by the automation peer may not match your control's purpose. Use the Uno-specific `Role` attached property to override it:

```xml
xmlns:utu="using:Uno.UI.Toolkit"

<Border utu:AutomationPropertiesExtensions.Role="tablist">
    <!-- Custom tab strip -->
</Border>
```

See [Role override](xref:Uno.Features.Accessibility.RoleOverride) for the full list of supported roles and per-platform behavior.

## Supported controls

All standard Uno controls have built-in automation peers that produce the correct ARIA role and attributes on WASM. Key mappings include:

| Control | ARIA Role | Notes |
|---------|-----------|-------|
| `Button` | `button` | Invoke pattern supported |
| `CheckBox` | `checkbox` | Toggle state via `aria-checked` |
| `RadioButton` | `radio` | Selection within a group |
| `Slider` | `slider` | Range value via `aria-valuenow` / `aria-valuemin` / `aria-valuemax` |
| `TextBox` | `textbox` | Rendered as `<input type="text">` |
| `PasswordBox` | `textbox` (password) | Rendered as `<input type="password">` |
| `ComboBox` | `combobox` | Expand/collapse pattern |
| `ToggleSwitch` | `switch` | Toggle state via `aria-checked` |
| `ListView` | `listbox` | Selection pattern |
| `ListViewItem` | `option` | Individual selectable items |
| `HyperlinkButton` | `link` | Rendered as `<a>` element |
| `ProgressBar` | `progressbar` | `aria-valuenow` for determinate progress |

For the full mapping table, see [Automation peers](xref:Uno.Features.Accessibility.AutomationPeers).

## Configuration reference

| Setting | Default | Description |
|---------|---------|-------------|
| `FeatureConfiguration.AutomationPeer.AutoEnableAccessibility` | `false` | When `true`, creates the semantic tree on startup without the activation button. |
| `FeatureConfiguration.AutomationPeer.UseSimpleAccessibility` | `false` | **iOS/Android native only.** Not used on Skia WASM. |
| `IsUiAutomationMappingEnabled` (MSBuild property) | `false` | Enables `AutomationId` mapping to `xamlautomationid` attributes. Enable for UI testing. |

## Testing your app

### Recommended screen readers

| Browser | Screen Reader | Notes |
|---------|---------------|-------|
| Chrome | NVDA (Windows) | Best overall support for ARIA in Chromium-based browsers. |
| Firefox | NVDA (Windows) | Good alternative; Firefox has its own accessibility engine. |
| Safari | VoiceOver (macOS) | Best VoiceOver experience. |
| Chrome | VoiceOver (macOS) | Works, but Safari is recommended for VoiceOver. Enable Full Keyboard Access in System Settings → Keyboard. |

### Quick testing checklist

1. **Build and run** your app targeting WASM.
2. **Press `Tab`** to trigger the activation flow, then activate the "Enable accessibility" button.
3. **Turn on a screen reader** (NVDA, VoiceOver, or your preferred tool).
4. **Navigate by Tab** — verify each interactive control is reachable and announced correctly.
5. **Navigate by headings** — verify `H` key (NVDA) or Rotor (VoiceOver) lists your headings in the correct hierarchy.
6. **Navigate by landmarks** — verify `D` key (NVDA) or Rotor (VoiceOver) lists your regions.
7. **Interact with controls** — activate buttons, toggle checkboxes, move sliders, type in text boxes.
8. **Trigger a live region** — verify the screen reader announces the dynamic content change.

### Debugging the semantic DOM

Open your browser's DevTools and look for the `#uno-semantics-root` element in the DOM. It contains all the hidden semantic elements:

```
#uno-semantics-root
├── <button aria-label="Save settings">
├── <input type="text" aria-label="Email address" aria-description="Enter your email...">
├── <h1>Account Settings</h1>
├── <div role="navigation">
│   └── ...
└── ...
```

**Chrome:** DevTools → Elements → Accessibility pane (right sidebar)
**Firefox:** DevTools → Accessibility tab
**Safari:** Develop → Show Web Inspector → Elements → Node → Accessibility

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| Screen reader is silent | Accessibility layer not activated | Press `Tab`, then activate the "Enable accessibility" button. Or set `AutoEnableAccessibility = true`. |
| A control has no label | `AutomationProperties.Name` not set and content is not plain text | Set `AutomationProperties.Name` explicitly. Check `aria-label` in the semantic DOM. |
| Heading navigation skips a heading | `HeadingLevel` not set | Add `AutomationProperties.HeadingLevel` to the `TextBlock`. Verify `<h1>`–`<h6>` appears in `#uno-semantics-root`. |
| Landmark not listed | `LandmarkType` not set | Add `AutomationProperties.LandmarkType`. Verify the ARIA role in the semantic DOM. |
| Live region does not announce | `LiveSetting` not set or content not changing | Set `AutomationProperties.LiveSetting`. Verify `aria-live` attribute on the semantic element. |
| Checkbox/toggle state not announced | Expected for custom templates | Verify the control uses the correct `AutomationPeer` or set `Role` explicitly. |
| Focus does not move to the correct element | Structural elements absorbing focus | Set `AutomationProperties.AccessibilityView="Raw"` on decorative containers. |
| Performance degrades on large pages | Too many semantic elements | Use `AutomationProperties.AccessibilityView="Raw"` on decorative elements to prune them from the semantic tree. |

## Coming from the native WASM renderer

If you previously built Uno WASM apps with the **native rendering engine** (where each XAML element was a real HTML DOM element), here is what changed with the Skia renderer:

| Aspect | Native WASM renderer | Skia WASM renderer |
|--------|----------------------|---------------------|
| Rendering model | Each `UIElement` is a real HTML element (`<div>`, `<button>`, etc.) | All content drawn on a single `<canvas>` |
| Inherent accessibility | HTML elements had built-in accessibility semantics | Canvas is opaque; a separate semantic DOM overlay provides accessibility |
| `AutomationProperties` APIs | Same WinUI APIs | Same WinUI APIs — no change |
| Activation | Always active | User-triggered activation (Tab → "Enable accessibility") or `AutoEnableAccessibility = true` |
| Custom HTML attributes | Could set custom attributes on DOM elements | Not applicable — the semantic DOM elements are framework-managed |
| ARIA attributes | Set directly on the element's HTML | Set on hidden semantic DOM elements in `#uno-semantics-root` |
| Pruning | No pruning needed — all elements exist in DOM | Structural elements without accessible info are pruned from the semantic DOM automatically |

**What you need to do:**

- **Your XAML and `AutomationProperties` usage does not need to change.** The same APIs work on both renderers.
- **Test with a screen reader** after switching to Skia to verify the behavior matches your expectations.
- **Consider enabling `AutoEnableAccessibility`** if your users expect accessibility to be immediately available, since the Skia renderer requires an activation step that the native renderer did not.
- **Remove any native-WASM-specific workarounds** (custom HTML attribute setting, manual DOM accessibility) that are no longer applicable.

## See also

- [Accessibility overview](xref:Uno.Features.Accessibility)
- [AutomationProperties reference](xref:Uno.Features.Accessibility.AutomationProperties)
- [Custom automation peers](xref:Uno.Features.Accessibility.AutomationPeers)
- [Role override](xref:Uno.Features.Accessibility.RoleOverride)
- [Testing with screen readers](xref:Uno.Features.Accessibility.TestingWithScreenReaders)
- [Accessibility overview (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-overview)
