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

For accessing platform-specific APIs, use conditional compilation.

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
#if __IOS__
using UIKit;
#endif

public partial class CustomTextBox : TextBox
{
    public CustomTextBox()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
#if __IOS__
        if (this.GetTemplateChild("ContentElement") is ContentControl contentElement 
            && contentElement.Content is UITextField textField)
        {
            textField.BorderStyle = UITextBorderStyle.RoundedRect;
            textField.Layer.CornerRadius = 10;
        }
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

Uno Platform allows direct access to native controls on iOS, Android, and macOS.

### Getting the Native Control

```csharp
public static class NativeControlHelper
{
    public static void CustomizeNativeControl(UIElement element)
    {
#if __ANDROID__
        if (element.GetTemplateChild("ContentElement") is ContentControl contentElement)
        {
            var nativeView = contentElement.Content as Android.Views.View;
            // Customize Android native view
        }
#elif __IOS__
        if (element.GetTemplateChild("ContentElement") is ContentControl contentElement)
        {
            var nativeView = contentElement.Content as UIKit.UIView;
            // Customize iOS native view
        }
#endif
    }
}
```

### Platform-Specific Properties

Use platform-specific code to set properties that don't exist in WinUI:

```csharp
public partial class CustomEntry : TextBox
{
    public CustomEntry()
    {
        InitializeComponent();
        ApplyPlatformCustomizations();
    }

    partial void ApplyPlatformCustomizations();
}

// In a platform-specific file (e.g., CustomEntry.Android.cs)
#if __ANDROID__
partial class CustomEntry
{
    partial void ApplyPlatformCustomizations()
    {
        Loaded += (s, e) =>
        {
            if (GetTemplateChild("ContentElement") is ContentControl content
                && content.Content is Android.Widget.EditText editText)
            {
                editText.SetHintTextColor(Android.Graphics.Color.Gray);
                editText.ImeOptions = Android.Views.InputMethods.ImeAction.Done;
            }
        };
    }
}
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

### Using Native Controls in XAML

You can include native controls directly in your Uno Platform XAML:

**Android:**

```xml
<Page xmlns:android="http://uno.ui/android"
      xmlns:androidwidget="using:Android.Widget"
      mc:Ignorable="android">
    
    <StackPanel>
        <android:Grid>
            <androidwidget:RatingBar android:layout_width="wrap_content"
                                     android:layout_height="wrap_content" />
        </android:Grid>
    </StackPanel>
</Page>
```

**iOS:**

```xml
<Page xmlns:ios="http://uno.ui/ios"
      xmlns:uikit="using:UIKit"
      mc:Ignorable="ios">
    
    <StackPanel>
        <ios:Grid>
            <uikit:UISwitch />
        </ios:Grid>
    </StackPanel>
</Page>
```

### Wrapping Native Controls

To create a cross-platform control that wraps native implementations:

```csharp
public partial class CustomRatingControl : ContentControl
{
    public CustomRatingControl()
    {
        InitializeComponent();
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
        var ratingBar = new Android.Widget.RatingBar(Android.App.Application.Context);
        ratingBar.NumStars = 5;
        
        Content = VisualTreeHelper.AdaptNative(ratingBar);
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
        
        Content = VisualTreeHelper.AdaptNative(ratingView);
    }
}
#endif
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

Use a custom control template that doesn't include those elements, or set them via platform-specific code as shown above.

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

// Or platform-specific
#if __ANDROID__
nativeView.Touch += OnTouch;
#endif
```

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
- **Native controls**: Can be embedded directly in XAML or wrapped in custom controls
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
