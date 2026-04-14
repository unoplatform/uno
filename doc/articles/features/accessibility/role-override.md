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

When set, this value takes precedence over the role that would normally be derived from the element's type or its `AutomationPeer`.

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

| Platform | Behavior |
|----------|----------|
| Web (WASM) | Applied directly as the HTML `role` attribute on the semantic DOM element |
| Windows (Win32) | Accepted but has no effect — UIAutomation exposes control type, not ARIA role strings |
| macOS | Read at query time from the accessibility tree; mapped to the appropriate `NSAccessibility` role |
| Android (WIP) | Routed through the Skia accessibility layer |
| iOS (WIP) | Routed through the Skia accessibility layer |

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
- [Controls accessibility reference](controls-reference.md)
