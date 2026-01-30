---
uid: Uno.XamarinFormsMigration.Effects
---

# Migrating Effects from Xamarin.Forms to Uno Platform

This guide explores how to migrate Effects from Xamarin.Forms to Uno Platform. Effects are a valuable tool for making smaller customizations to native controls. While Uno Platform doesn't have a direct equivalent to Effects, it offers alternative approaches to achieve similar functionality.

## What are Effects in Xamarin.Forms?

An Effect can be applied to a control in XAML to add specific look-and-feel or behavior changes to the control. Usually, this requires platform-specific implementations that change the native controls. Xamarin.Forms provides a method of registering the platform implementation with a unique name, so you apply the effect once, and Xamarin.Forms applies the correct native effect automatically. If there is no corresponding effect on the current platform, nothing happens – you don't have to worry about errors where functionality isn't available.

A common Effect across Xamarin.Forms projects is one to remove the default underline on the Entry control on Android. It's such a common scenario that it was added to the Xamarin Community Toolkit.

### How Effects Work in Xamarin.Forms

1. A class is created in shared code that inherits from `RoutingEffect`. This is what is inserted into the XAML.
2. An assembly-wide `ResolutionGroupName` attribute defines a unique identifier for all custom effects (often the same name as the namespace).
3. In each native platform project, a class is created that inherits from `PlatformEffect`.
4. Customization is performed within two required methods: `OnAttached` and `OnDetached`.
5. The assembly has a matching `ResolutionGroupName` attribute.
6. An `ExportEffect` attribute registers the specific platform effect with Xamarin.Forms.

## Uno Platform Alternatives to Effects

Uno Platform is based on the WinUI API and doesn't have an equivalent concept to Effects. However, it offers alternative approaches:

### 1. Control Templates

A `ControlTemplate` allows you to replace the visual tree for a specific control type and can include other controls. To create a custom `TextBox` with a new type of border but without an underline, you can create a template and apply it to `TextBox` controls using a `Style`.

Any `TextBox` with this `Style` specified will use the template to define its appearance, and you can create any number of different templates for use in different parts of your app.

#### Example: Rounded TextBox Style

```xml
<Style x:Key="RoundedTextBox" TargetType="TextBox">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="TextBox">
                <Border CornerRadius="6" 
                        BorderBrush="{TemplateBinding Foreground}" 
                        Background="Wheat" 
                        BorderThickness="2">
                    <ContentControl x:Name="ContentElement" />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### Key Points About Control Templates

- Both the `Style` and the `ControlTemplate` must set the `TargetType` property to specify the type of control they apply to
- Your style can contain any other setters alongside the template
- The template contains the XAML controls that will make up the control, typically with a `Border` or `Grid` or similar container control

### The Content Element

The inner `ContentControl` represents the actual native control part. Uno has helper methods to create a `ContentControl` that hosts the native control and manages layout. In this case, it displays the text and text-editing functionality, while the `Border` in the template defines the appearance.

There is no `TextBox` in this template – the `ContentControl` named `ContentElement` is where Uno will place the native text editing control.

### Customizing with Template Binding

You can use `TemplateBinding` so that properties take their values from the control as defined in your page. This means values are passed through, and data binding is fully supported. As far as the consuming page is concerned, it's just another `TextBox`.

For example, binding the `BorderBrush` of the `Border` to the `Foreground` property of the `TextBox`:

```xml
BorderBrush="{TemplateBinding Foreground}"
```

You can use template binding for different properties as long as they are the same type. For example, you can bind the `BorderBrush` of the `Border` to the `Foreground` property of the `TextBox` because they are both `Brush` types.

### Using the Control Template

You can define styles either locally at a page (or control) level or in the shared `App.xaml` to make them available throughout the app.

## 2. Platform-Specific Customization with Conditional Compilation

For functionality that cannot be achieved with `ControlTemplate` alone, you can use conditional compilation to add platform-specific code. This is similar to how Effects work in Xamarin.Forms but requires a different approach in Uno Platform.

### Accessing Native Controls

To understand this approach, you need to know how to access the native control. The default `ControlTemplate` for `TextBox` contains a `ContentControl` named `ContentElement`. Given an instance of the control, you can call `GetTemplateChild` with this unique name to retrieve the `ContentControl` element. This control contains the native control in its `Content` property.

However, you must be careful when supporting multiple platforms because on iOS this will be a `UITextView`, while on Android it will be an `EditText`. Use conditional compilation to add platform-specific code:

```csharp
#if ANDROID
    // Android-specific code here
#endif
```

Any `using` statements for platform-specific namespaces must also be wrapped in `#if` directives.

### Example: Android EditText Error Property

This example shows how to implement the native Android `EditText` Error property, which displays an error overlay below the control for data validation.

```csharp
public static class TextBoxErrorHelper
{
    public static readonly DependencyProperty ErrorTextProperty =
        DependencyProperty.RegisterAttached(
            "ErrorText",
            typeof(string),
            typeof(TextBoxErrorHelper),
            new PropertyMetadata(null, OnErrorTextChanged));

    public static string GetErrorText(DependencyObject obj)
    {
        return (string)obj.GetValue(ErrorTextProperty);
    }

    public static void SetErrorText(DependencyObject obj, string value)
    {
        obj.SetValue(ErrorTextProperty, value);
    }

    public static void OnErrorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TextBox t = (TextBox)d;
#if ANDROID
        var contentElement = (ContentControl)t.GetTemplateChild("ContentElement");
        if (contentElement != null)
        {
            var editText = (Android.Widget.EditText)contentElement.Content;
            editText.Error = (string)e.NewValue;
        }
#endif
    }
}
```

The code:
1. Gets the `ContentControl` defined in the template
2. Accesses the inner native `EditText` control
3. Sets its `Error` property, which defines the message to display

Setting the `Error` property to null removes the error warning and associated message.

### Using the Attached Property

Once defined, add the attached property to any `TextBox` control:

```xml
<TextBox x:Name="ErrorTextBox" 
         FontSize="18" 
         Text="Text with error" 
         local:TextBoxErrorHelper.ErrorText="{x:Bind ErrorText, Mode=OneWay}"/>
```

Note: When using `x:Bind` to set the value, specify `Mode=OneWay` to pass through changes because `x:Bind` defaults to `OneTime` binding.

## Comparison: Effects vs. Uno Platform Approaches

| Aspect | Xamarin.Forms Effects | Uno Platform Control Templates | Uno Platform Conditional Compilation |
|--------|----------------------|-------------------------------|-------------------------------------|
| **Scope** | Platform-specific native changes | Visual tree replacement | Platform-specific native changes |
| **Registration** | Assembly attributes + Export | XAML styles | Attached properties or extension methods |
| **Platform Safety** | Automatic – missing implementations are ignored | Cross-platform XAML | Requires #if directives |
| **Complexity** | Medium – requires platform projects | Low – XAML only | Medium – requires native API knowledge |
| **Use Case** | Small native customizations | Visual appearance changes | Advanced native control features |

## Summary

While Uno Platform doesn't have a direct equivalent to Xamarin.Forms Effects, you can achieve similar results through:

1. **Control Templates**: For visual customizations and templating the control appearance
2. **Conditional Compilation with Attached Properties**: For accessing platform-specific native control features

The choice depends on your requirements:
- Use control templates when you need to change the visual appearance
- Use conditional compilation when you need to access platform-specific native control methods and properties that aren't exposed by the standard WinUI controls

## Next Steps

- Continue with [Migrating Animations](xref:Uno.XamarinFormsMigration.Animations)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## Sample Code

The complete sample code demonstrating these techniques is available in the [Uno.Samples repository](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MigratingEffects).
