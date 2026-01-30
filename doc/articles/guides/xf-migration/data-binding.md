---
uid: Uno.XamarinFormsMigration.DataBinding
---

# Migrating Data Binding from Xamarin.Forms to Uno Platform

In this guide, we will explore how to migrate data binding techniques from Xamarin.Forms to Uno Platform. Data binding is the glue that holds your UI and business logic together, and understanding the differences between the two frameworks will help you successfully migrate your existing investment.

## What is Data Binding

When Windows Presentation Foundation (WPF) was introduced, it offered a new approach to create user interfaces using XAML markup. This allowed for a cleaner separation of UI design from app logic.

The Model-View-ViewModel (MVVM) paradigm was created to provide structure for this separation. The user interface uses data binding to connect controls to public properties and commands exposed by your view model. The view model has no knowledge of how the UI appears or works, just what functionality to expose and the ability to update it as required.

All subsequent XAML-based frameworks have largely supported the same functionality, making it generally easy to move view models and other app logic between frameworks. However, there are important considerations for making this work in the most efficient way.

## Dependency Properties

Many UI controls have a key property that describes their main purpose – a **TextBlock**, for example, has a **Text** property containing a string, and a **CheckBox** has a boolean value describing whether it is selected. However, data binding in XAML is not limited to these key properties – you can data bind any dependency property, changing not just the main data but also colors, fonts, sizing, etc.

Dependency properties are accessed through the **GetValue** and **SetValue** methods on **DependencyObject**. Most implementations add strongly-typed properties to the class to make it easier to work with these properties via code.

On top of these properties, there is a related concept – the attached property. This allows one class to define a property that can be applied to another type. You'll likely have encountered these when positioning controls in a grid using the **Grid.Row** and **Grid.Column** attached properties.

## Binding Markup

The key to data binding any property in XAML is a special markup extension called `Binding`. Markup extensions allow you to pass things other than explicit values to a property. Alongside its use for data binding, you've probably seen `StaticResource` used to reference a resource with a unique key defined in XAML.

## Binding Context vs. Data Context

Dependency objects in Xamarin.Forms all have a **BindingContext** property – this is the object that provides the values for data binding. Minimally, it is an instance of a class that provides some public properties containing the values to display in the view. Usually, it will also implement **INotifyPropertyChanged** – this interface defines a standard event that notifies the view when property values change. When you use an MVVM framework, it will include a base class such as **ObservableObject** that contains the plumbing for this, so you don't have to implement boilerplate code.

WinUI follows the WPF and UWP convention of using **DataContext** instead of **BindingContext**, but it functions in the same way.

## Binding Modes

Bindings support multiple modes that determine how the binding behaves when values are changed:

- **OneWay** (default in traditional `Binding`): UI controls update when a data-bound value in the view model changes
- **OneTime**: The value is set once with no change handling
- **TwoWay**: Changes in the UI are sent back to the view model – commonly used for data entry controls

## Value Converters

Converters are classes used to convert a value into an appropriate format for the UI control. They implement the **IValueConverter** interface, which defines two methods to convert to and from another type. There is no strong typing used, so you must ensure you use a converter in the right place to return the expected type for the bound control. If you are using the converter for a one-way binding, you don't need to write code for the `ConvertBack` method.

Common scenarios include:
- Converting ranges of values into colors
- Converting Enum values into formatted strings

When migrating from Xamarin.Forms to Uno Platform, you'll likely need to make changes to converters. For example, if your converter returns a `Xamarin.Forms.Color`, it will need to be reworked to return a `Microsoft.UI.Color` (or `Windows.UI.Color`) instead.

## String Formatting

Xamarin.Forms, like WPF, supports the **StringFormat** property on bindings. This allows you to specify how to format the data-bound value. For example, if you have a numerical value you want to display as a percentage, you can apply this in the string format rather than using converters or exposing a string property in the view model with the value already formatted.

On UWP and WinUI, there isn't an equivalent to `StringFormat`, so you must either:
- Use a converter to return the formatted string
- Expose a view model property with the string already formatted
- Use `x:Bind`, which supports inline functions (discussed below)

## Commands

Commands are actions that have a method to perform a function and optionally a method to indicate whether they can be executed. For example, some actions only work in a particular state. When a command reports that it cannot execute, the bound control will be disabled.

The `ICommand` interface includes an event that is fired when the state may have changed, and the function will be reevaluated. Because of its roots in WPF, the interface is defined in the `System.Windows.Input` namespace but doesn't have dependencies on other WPF APIs. This makes it easy to migrate code using commands with no code changes.

## Compiled Bindings

Normal bindings are resolved at runtime. This means they are not as performant as compiled code, and there is no validation at build time, which can lead to unexpected errors.

Xamarin.Forms can use compiled bindings that provide extra performance benefits and binding errors at build time. There are two prerequisites:
1. Enable compiled XAML at the assembly level (enabled by default in new project templates)
2. Indicate the data type of the bound object in XAML using the `x:DataType` attribute

WinUI doesn't have an equivalent option for `Binding` markup but instead introduces the `x:Bind` markup extension, which adds functionality and compiles the binding into code.

## x:Bind

`x:Bind` was introduced in UWP to support compiled binding and add more powerful options. However, you cannot simply replace every `Binding` with `x:Bind`, as they behave differently.

### Key Differences

**Binding Context**: `x:Bind` doesn't use the `DataContext` to define the target but instead uses the page or control itself. While this is fine for code-behind in the view, we rarely want to do that. Instead, we need to provide a strongly-typed property for the view model in the code-behind:

```csharp
namespace DataBindingCodeBehind
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.ViewModel = new MyViewModel();
        }

        public MyViewModel ViewModel { get; set; }
    }
}
```

Previously, you might have bound a control to a property on your view model like this:

```xml
<TextBlock Text="{Binding Title}"/>
```

With `x:Bind`, you would use:

```xml
<TextBlock Text="{x:Bind ViewModel.Title}"/>
```

The dotted notation traverses the object hierarchy, so the following is valid:

```xml
<TextBlock Text="{x:Bind ViewModel.DateField.Month}"/>
```

**Default Binding Mode**: Because `OneWay` and `TwoWay` binding modes require generating more code for change handling, the default binding mode with `x:Bind` is `OneTime`. If you find that your UI is not updating when values change, check that you've explicitly set the mode:

```xml
<TextBlock Text="{x:Bind ViewModel.DateField.Month, Mode=OneWay}"/>
```

### String Formatting with x:Bind

Because `x:Bind` markup can contain functions, you can include string formatting logic in your markup without changes to your view model.

Xamarin.Forms example:

```xml
<TextBlock Text="{Binding Angle, StringFormat='The angle is {0:F0} degrees'}"/>
```

Equivalent using `x:Bind` and formatting with a function:

```xml
<TextBlock Text="{x:Bind sys:String.Format('The angle is {0:F0} degrees', ViewModel.Angle)}"/>
```

To use methods from the `String` type, define the `System` namespace with the `sys` prefix in the page XAML:

```xml
xmlns:sys="using:System"
```

Alternatively, if you don't need additional text and can rely on the `ToString()` functionality:

```xml
<TextBlock Text="{x:Bind ViewModel.Value.ToString('F0')}"/>
```

When you need to include quotes inside the markup, use single quotes instead of double quotes, as double quotes are used for the containing XAML attribute.

### Binding to Methods

`x:Bind` can be used to bind commands to controls with a `Command` property. However, the `x:Bind` syntax can also contain methods, allowing you to bind control events directly to methods in code without using commands. There's flexibility here – methods don't have to exactly match the event signature. The method can have no parameters or contain the same number of parameters as the event signature if they are types to which the arguments can be cast.

## Summary

You can generally migrate code from Xamarin.Forms to Uno Platform without significant changes to your logic in view models:

- Where you have used converters, you may need to make changes to UI-specific types
- To take advantage of compiled bindings, move to the `x:Bind` markup
- You may need to add a strongly-typed property to your page to hold your view model
- You can use `x:Bind` to remove converters, perform string formatting, and take advantage of compiled code and build-time binding errors

## Next Steps

- Continue with [Migrating Effects](xref:Uno.XamarinFormsMigration.Effects)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)
