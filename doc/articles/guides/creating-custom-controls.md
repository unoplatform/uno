---
uid: Uno.Tutorials.CreatingCustomControls  
---  

# Creating Custom Controls

Custom controls in the Uno Platform enable developers to create reusable UI components that work seamlessly across iOS, Android, WebAssembly, Linux, macOS, and Windows. Uno Platform supports the same approach as WinUI/WinAppSDK, with a few specific considerations for cross-platform compatibility.

> [!TIP]
> Use **[Hot Design®](xref:Uno.HotDesign.Overview)** to visually prototype your custom controls and **[Hot Design® Agent](xref:Uno.HotDesign.Agent)** for AI-assisted development of control templates and visual states.

> [!NOTE]  
> For a more detailed explanation of creating custom controls, including dependency properties and control templates, refer to the official WinUI documentation: [Build XAML controls with C# | Microsoft Learn](https://learn.microsoft.com/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3).

If you're not familiar with concepts such as **dependency properties**, **control templates**, and **visual states**, it might be useful to explore those topics before diving into custom controls. These are fundamental concepts for creating reusable, maintainable UI components:

- **Dependency Properties**: These are properties that are tracked by a dedicated property system to allow binding, styling, animations, and tracking value changes to work. For more information, see [Dependency properties overview | Microsoft Learn](https://learn.microsoft.com/windows/uwp/xaml-platform/dependency-properties-overview).
- **Control Templates**: These define the visual structure and behavior of a control, allowing you to separate design from logic. Check out [Control templates | Microsoft Learn](https://learn.microsoft.com/windows/apps/design/style/xaml-control-templates) for further reading.
- **Visual States and Visual State Manager**: Visual states help you define different appearances for a control depending on its state (e.g., pressed, focused). Learn more here: [VisualStateManager Class | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.windows.visualstatemanager).

## Ensuring Cross-Platform Compatibility

To ensure your custom control functions properly across all platforms, make sure that the `Themes/Generic.xaml` file is included in your Uno project and correctly referenced for each platform. On non-Windows platforms, this file might not be automatically included, and you may need to adjust your project settings or add custom build steps to ensure it is properly referenced.

In WinUI apps, `Themes/Generic.xaml` is the standard location for default styles. It should be supported across all platforms and automatically referenced when Uno searches for implicit or default styles in any `TemplatedControl` defined in `Generic.xaml`. Additionally, it's common to use a `MergedDictionary` to reference resources from other directories within `Generic.xaml`. Currently, styles defined in `Themes/Generic.xaml` are not found automatically across platforms in Uno.

To resolve this, you can define styles in `App.xaml` and use a `MergedDictionary` to pull in the resources, as shown in the following example:

```xml
<Application xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <ResourceDictionary Source="ms-appx:///Themes/Generic.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## Handling Platform-Specific Adjustments

In some cases, custom control behavior might need adjustments based on the platform. Uno Platform provides the flexibility to handle platform-specific differences using conditional code in both XAML and C#. For XAML, platform-specific XML namespaces can be utilized, while `#if` preprocessor directives are available in C#. For more details, refer to the documentation on [Platform-Specific C#](xref:Uno.Development.PlatformSpecificCSharp) and [XAML Conditions](xref:Uno.Development.PlatformSpecificXaml).
