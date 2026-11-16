---
uid: Uno.XamarinFormsMigration.NativeControls
---

# Migrating Custom Renderers and Native Controls

This guide explains how to migrate Xamarin.Forms custom renderers and native library bindings to Uno Platform. While the approaches differ, Uno Platform provides powerful alternatives for integrating native controls and platform-specific code.

## Understanding Custom Renderers in Xamarin.Forms

In Xamarin.Forms, custom renderers allowed you to:

- Create platform-specific implementations of custom controls
- Override the default rendering of built-in controls
- Access native platform APIs and controls

Each platform had its own renderer implementation:

```csharp
// Xamarin.Forms - iOS Renderer
[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
public class CustomEntryRenderer : EntryRenderer
{
    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
    {
        base.OnElementChanged(e);
        if (Control != null)
        {
            Control.BorderStyle = UITextBorderStyle.None;
        }
    }
}
```

## Uno Platform Alternatives

Uno Platform doesn't use the renderer pattern. Instead, you have several approaches:

### 1. Control Templates (Recommended for Visual Changes)

For visual customizations, use control templates. This is the most common migration path for renderers that only changed appearance.

**Xamarin.Forms Renderer:**

```csharp
// Removed underline on Android Entry
protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
{
    base.OnElementChanged(e);
    if (Control != null)
    {
        Control.Background = null;
    }
}
```

**Uno Platform Equivalent:**

```xml
<Style x:Key="NoUnderlineTextBox" TargetType="TextBox">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TextBox">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="4">
                    <ContentControl x:Name="ContentElement" />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

See the [Effects Migration Guide](xref:Uno.XamarinFormsMigration.Effects) for more details on control templates.

### 2. Platform-Specific Code with Conditional Compilation

For platform-specific behavior, use conditional compilation. Uno Platform draws its controls itself on every platform, so a `TextBox` has no `UITextField` behind it to reach into: express the customization with the control's own properties, and keep native APIs for non-UI platform features.

**Xamarin.Forms Renderer:**

```csharp
// iOS Renderer
protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
{
    base.OnElementChanged(e);
    if (Control != null)
    {
        Control.BorderStyle = UITextBorderStyle.RoundedRect;
        Control.Layer.CornerRadius = 10;
    }
}
```

**Uno Platform Equivalent:**

```csharp
public partial class CustomTextBox : TextBox
{
    public CustomTextBox()
    {
#if __IOS__
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(10);
#endif
    }
}
```

### 3. Attached Properties for Reusable Behaviors

For behaviors that can be applied to any control, use attached properties.

**Xamarin.Forms Effect:**

```csharp
public class ShadowEffect : PlatformEffect
{
    protected override void OnAttached()
    {
        // Add shadow to control
    }
}
```

**Uno Platform Attached Property:**

```csharp
public static class ShadowExtensions
{
    public static readonly DependencyProperty EnableShadowProperty =
        DependencyProperty.RegisterAttached(
            "EnableShadow",
            typeof(bool),
            typeof(ShadowExtensions),
            new PropertyMetadata(false, OnEnableShadowChanged));

    public static bool GetEnableShadow(DependencyObject obj) 
        => (bool)obj.GetValue(EnableShadowProperty);

    public static void SetEnableShadow(DependencyObject obj, bool value) 
        => obj.SetValue(EnableShadowProperty, value);

    private static void OnEnableShadowChanged(DependencyObject d, 
        DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && (bool)e.NewValue)
        {
            element.Shadow = new ThemeShadow();
            element.Translation = new System.Numerics.Vector3(0, 0, 32);
        }
    }
}
```

Usage:

```xml
<Border local:ShadowExtensions.EnableShadow="True">
    <TextBlock Text="With Shadow" />
</Border>
```

## Accessing Native Controls

Uno Platform renders its controls with Skia on every platform, including Android and iOS. A `TextBox`, a `Button` or any other built-in control is not backed by a native view, so there is no `EditText` or `UITextField` to retrieve from its template. A renderer that customized the native control of a built-in control becomes a property, style or template change.

### Platform-Specific Properties

Most native-only settings that renderers used to apply have a WinUI or Uno Platform equivalent. For example, the hint color and the keyboard's return key of an Android `EditText`:

```csharp
public partial class CustomEntry : TextBox
{
    public CustomEntry()
    {
        PlaceholderForeground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        Uno.UI.Xaml.Controls.TextBoxExtensions.SetInputReturnType(this, Uno.UI.Xaml.Controls.InputReturnType.Done);
    }
}
```

### Getting the Native Control

When you need a real native widget, create it yourself and host it (see [Wrapping Native Controls](#wrapping-native-controls)). Keep a reference to the instance you created to customize it; Uno Platform doesn't wrap or replace it:

```csharp
#if __ANDROID__
var editText = new Android.Widget.EditText(Uno.UI.ContextHelper.Current);
editText.SetHintTextColor(Android.Graphics.Color.Gray);
editText.ImeOptions = Android.Views.InputMethods.ImeAction.Done;

nativeHost.Content = editText; // nativeHost is a ContentControl
#endif
```

## Native Library Bindings

### Xamarin Bindings to Uno Platform

If you have Xamarin bindings for native libraries, you'll need to adapt them for Uno Platform.

#### Android Libraries

**Xamarin.Android Binding:**

```xml
<!-- Metadata.xml -->
<metadata>
  <attr path="/api/package[@name='com.example.library']" name="managedName">ExampleLibrary</attr>
</metadata>
```

**For Uno Platform:**

The same Xamarin.Android binding can often be used directly. Reference the binding library in your Android head project:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
    <ProjectReference Include="..\MyAndroidBinding\MyAndroidBinding.csproj" />
</ItemGroup>
```

#### iOS Libraries

**Xamarin.iOS Binding:**

```csharp
[BaseType(typeof(NSObject))]
interface CustomSDK
{
    [Export("initialize")]
    void Initialize();
}
```

**For Uno Platform:**

Similar to Android, iOS bindings can be referenced in your iOS head project. Create the binding using the standard Xamarin.iOS binding process.

### Wrapping Native Controls

To host a native control, set it as the `Content` of a `ContentControl` (or `ContentPresenter`). Uno Platform overlays the native view on the Skia-rendered UI and keeps it in sync with the control's layout, clipping, z-order and opacity. A native view can only be hosted as content: it can't be a `Panel` child.

To create a cross-platform control that wraps native implementations:

```csharp
public partial class CustomRatingControl : ContentControl
{
    public CustomRatingControl()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        CreateNativeControl();
    }

    partial void CreateNativeControl();
}

// Android implementation
#if __ANDROID__
partial class CustomRatingControl
{
    partial void CreateNativeControl()
    {
        var ratingBar = new Android.Widget.RatingBar(Uno.UI.ContextHelper.Current);
        ratingBar.NumStars = 5;

        Content = ratingBar;
    }
}
#endif

// iOS implementation
#if __IOS__
partial class CustomRatingControl
{
    partial void CreateNativeControl()
    {
        var ratingView = new CustomRatingView();

        Content = ratingView;
    }
}
#endif
```

Don't set a `ContentTemplate` or `ContentTemplateSelector` on a control that hosts a native view. For the details of native hosting on each platform, including measuring and the alignment defaults, see [Embedding Native Elements in Skia Apps](xref:Uno.Skia.Embedding.Native).

### Using Native Controls in XAML

A native control can also be declared in XAML, as the content of a `ContentControl`. Use a [platform-specific XAML prefix](xref:Uno.Development.PlatformSpecificXaml) that maps the native CLR namespace, so the markup is only compiled for that platform:

```xml
<Page xmlns:android="http://uno.ui/android#using:Android.Widget"
      xmlns:ios="http://uno.ui/ios#using:UIKit"
      mc:Ignorable="d android ios">

    <StackPanel>
        <android:ContentControl>
            <android:RatingBar />
        </android:ContentControl>
        <ios:ContentControl>
            <ios:UISwitch />
        </ios:ContentControl>
    </StackPanel>
</Page>
```

## Migrating Common Renderer Scenarios

### Scenario 1: Removing Platform-Specific UI Elements

**Xamarin.Forms:**

```csharp
// Android: Remove default underline
Control.Background = null;

// iOS: Remove border
Control.BorderStyle = UITextBorderStyle.None;
```

**Uno Platform:**

Use a custom control template that doesn't include those elements, or set the control's own properties (for example `BorderThickness` or `Background`), conditionally per platform if needed.

### Scenario 2: Customizing Touch/Click Behavior

**Xamarin.Forms:**

```csharp
Control.Touch += OnTouch;
```

**Uno Platform:**

```csharp
element.PointerPressed += OnPointerPressed;
element.PointerReleased += OnPointerReleased;
element.PointerMoved += OnPointerMoved;
```

A native view that you host yourself (see [Wrapping Native Controls](#wrapping-native-controls)) keeps receiving its native touch events, so a `Touch` handler on it continues to work.

### Scenario 3: Custom Drawing

For custom drawing, see the [Custom-Drawn Controls Migration Guide](xref:Uno.XamarinFormsMigration.CustomDrawnControls) which covers SkiaSharp integration.

### Scenario 4: Platform-Specific Events

**Xamarin.Forms:**

```csharp
protected override void OnElementPropertyChanged(object sender, 
    PropertyChangedEventArgs e)
{
    base.OnElementPropertyChanged(sender, e);
    
    if (e.PropertyName == Entry.TextProperty.PropertyName)
    {
        // Handle text change
    }
}
```

**Uno Platform:**

```csharp
public partial class CustomEntry : TextBox
{
    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        // Handle text change
    }
}
```

## Dependency Service Migration

Xamarin.Forms `DependencyService` should be migrated to proper dependency injection.

### Xamarin.Forms Pattern

```csharp
// Interface
public interface IDeviceService
{
    string GetDeviceId();
}

// Android implementation
[assembly: Dependency(typeof(DeviceService))]
public class DeviceService : IDeviceService
{
    public string GetDeviceId() => Android.Provider.Settings.Secure.GetString(
        Application.Context.ContentResolver, 
        Android.Provider.Settings.Secure.AndroidId);
}

// Usage
var deviceService = DependencyService.Get<IDeviceService>();
```

### Uno Platform Pattern

```csharp
// Interface (shared)
public interface IDeviceService
{
    string GetDeviceId();
}

// Platform-specific implementation
#if __ANDROID__
public class DeviceService : IDeviceService
{
    public string GetDeviceId() => Android.Provider.Settings.Secure.GetString(
        Android.App.Application.Context.ContentResolver,
        Android.Provider.Settings.Secure.AndroidId);
}
#elif __IOS__
public class DeviceService : IDeviceService
{
    public string GetDeviceId() => 
        UIKit.UIDevice.CurrentDevice.IdentifierForVendor.AsString();
}
#else
public class DeviceService : IDeviceService
{
    public string GetDeviceId() => "unknown";
}
#endif

// Registration in App.xaml.cs or Startup.cs
services.AddSingleton<IDeviceService, DeviceService>();

// Usage via constructor injection
public class MyViewModel
{
    private readonly IDeviceService _deviceService;
    
    public MyViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }
}
```

## ApiExtensibility Pattern

For more complex scenarios, use Uno's `ApiExtensibility` pattern:

```csharp
// In shared code
public partial class PlatformHelper
{
    public static string GetPlatformInfo() => GetPlatformInfoImpl();
    
    static partial string GetPlatformInfoImpl();
}

// In Android-specific file
#if __ANDROID__
partial class PlatformHelper
{
    static partial string GetPlatformInfoImpl()
    {
        return $"Android {Android.OS.Build.VERSION.Release}";
    }
}
#endif

// In iOS-specific file
#if __IOS__
partial class PlatformHelper
{
    static partial string GetPlatformInfoImpl()
    {
        return $"iOS {UIKit.UIDevice.CurrentDevice.SystemVersion}";
    }
}
#endif
```

## Migration Checklist

- [ ] Identify all custom renderers in your Xamarin.Forms app
- [ ] Categorize renderers by type (visual, behavioral, native API access)
- [ ] Migrate visual renderers to control templates
- [ ] Migrate behavioral renderers to attached properties
- [ ] Migrate native API access to platform-specific code with conditional compilation
- [ ] Replace `DependencyService` with proper dependency injection
- [ ] Update native library bindings to work with Uno Platform
- [ ] Test on all target platforms
- [ ] Verify platform-specific functionality works correctly

## Best Practices

1. **Prefer Control Templates**: For visual-only customizations, always use control templates first
2. **Use Conditional Compilation Sparingly**: Keep platform-specific code minimal and isolated
3. **Leverage Uno's Platform Abstractions**: Use existing Uno Platform features before creating custom platform code
4. **Test Thoroughly**: Platform-specific code can behave differently - test on all platforms
5. **Document Platform Differences**: Note any behavior differences between platforms

## Summary

Migrating custom renderers to Uno Platform:

- **Visual changes**: Use control templates
- **Behavioral changes**: Use attached properties or custom controls
- **Native API access**: Use conditional compilation with platform-specific code
- **Native controls**: Hosted as the content of a `ContentControl`, from code or XAML, optionally wrapped in a custom control
- **Dependency injection**: Replace `DependencyService` with proper DI container

## Next Steps

- [Migrating Effects](xref:Uno.XamarinFormsMigration.Effects)
- [Migrating Custom Controls](xref:Uno.XamarinFormsMigration.CustomControls)
- [Migrating Custom-Drawn Controls](xref:Uno.XamarinFormsMigration.CustomDrawnControls)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## See Also

- [Native Views in Uno Platform](xref:Uno.Development.NativeViews)
- [Platform-Specific C#](xref:Uno.Development.PlatformSpecificCSharp)
- [Platform-Specific XAML](xref:Uno.Development.PlatformSpecificXaml)
