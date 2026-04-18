---
uid: Uno.Features.Accessibility.RoleOverride
---

# AutomationPropertiesExtensions.Role

This Uno-specific attached property lets you explicitly override the accessibility role of any XAML element. It is useful for declaring landmarks, custom regions, or forcing a specific ARIA role when the default mapping from the control's `AutomationPeer` is not sufficient.

> [!NOTE]
> This is an Uno Platform extension with no WinUI equivalent. It is defined in the `Uno.UI.Toolkit` namespace.

## How to use

Add the `Uno.UI.Toolkit` namespace to your XAML, then set the `AutomationPropertiesExtensions.Role` attached property on any element:

```xml
<Page xmlns:uut="using:Uno.UI.Toolkit">

    <!-- Declare a navigation landmark -->
    <StackPanel uut:AutomationPropertiesExtensions.Role="navigation">
        <Button Content="Home" />
        <Button Content="Settings" />
    </StackPanel>

    <!-- Force a specific role on a control -->
    <Button uut:AutomationPropertiesExtensions.Role="tab"
            Content="Overview" />
</Page>
```

On elements that do **not** have an `AutomationPeer` (such as `Border` or `StackPanel`), this override takes precedence over any default role. On elements that **do** have a peer, the behavior is platform-dependent — see the table below.

## Clearing the override

Setting the value to an empty string or `null` removes the override, reverting to the default role:

```xml
<!-- Remove a previously set role override -->
<Button uut:AutomationPropertiesExtensions.Role="" Content="Regular button" />
```

Or in code-behind:

```csharp
AutomationPropertiesExtensions.SetRole(myButton, null);
```

## Supported role strings

Any standard [WAI-ARIA role](https://www.w3.org/TR/wai-aria-1.2/#role_definitions) string can be used.

## Platform behavior

| Platform | Rendering | Behavior |
|----------|-----------|----------|
| Web (WASM) | Skia | For elements without a peer, applied as the `role` attribute on the semantic DOM element. For elements with a peer, the peer's control type determines the role; the override is used as a fallback. Also makes the element focusable in the accessibility tree. |
| Web (WASM) | Native | Applied directly as the HTML `role` attribute on the real DOM element via `FindHtmlRole()` — takes precedence over the peer-derived role. |
| Windows (Win32) | Skia | Makes the element focusable in the accessibility tree, but the role string itself has no effect — UIAutomation exposes control type, not ARIA role strings. |
| macOS | Skia | Makes the element focusable in the accessibility tree, but the role string is not currently forwarded to VoiceOver. The native role comes from the peer's control type. |
| Android (WIP) | Skia | Routed through the Skia accessibility layer. |
| iOS (WIP) | Skia | Routed through the Skia accessibility layer. |
| Android / iOS | Native | `AutomationPropertiesExtensions.Role` is not consulted by the native rendering accessibility layer. |

## When to use vs. AutomationProperties.LandmarkType

For standard landmark regions, prefer `AutomationProperties.LandmarkType` — it is the WinUI-standard API and works consistently across all platforms:

```xml
<!-- Preferred for standard landmarks -->
<StackPanel AutomationProperties.LandmarkType="Navigation" />

<!-- Use Role override for non-standard or specific ARIA roles -->
<StackPanel uut:AutomationPropertiesExtensions.Role="complementary" />
```

Use `AutomationPropertiesExtensions.Role` when:

- You need an ARIA role that has no `AutomationProperties` equivalent (e.g., `complementary`, `banner`, `alert`)
- You need to override the default role mapping for a specific control instance
- You are building a custom widget that maps to a specific ARIA pattern

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
- [Custom automation peers](automation-peers.md)
