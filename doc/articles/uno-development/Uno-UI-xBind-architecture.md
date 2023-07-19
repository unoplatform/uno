---
uid: Uno.Contributing.xBind
---

# x:Bind in Uno Platform

## Overview of the x:Bind feature in WinUI

> Note that this section is based on observations of the behavior of `x:Bind`

`x:Bind` markup extensions have originally been developed by Microsoft to provide enhanced Data Binding performance, where no reflection is needed to do so. Code is generated along with XAML files that contain controls and DataTemplates, in `.g.cs` and `.g.i.cs` files.

Those bindings differ in multiple ways with standard `Binding` markup:

- The default binding mode is `OneTime` (`OneWay` for `Binding`)
- For top level controls the data context is the control itself
- For `DataTemplate`, the data context is the `DataContext` of the root element
- They can reference static fields and properties
- They can accept member and static functions with multiple parameters
- Function parameters can be string or number literals, as well as `x:True`, `x:False` and `x:Null`
- Function parameters can be the update source of the whole function binding

The creation of that generated code is performed in two passes:

- The first pass where x:Bind markup is removed from an intermediate file, creating unique "connections"
- The second pass creates a set of classes and interfaces within the top level class, or a hidden class for Data Templates.

The generated code is accessible through a private member called `Bindings`, which offers the following interface:

```csharp
private interface IXBindUserControl_Bindings
{
    void Initialize();
    void Update();
    void StopTracking();
}
```

The most commonly used member is `Update` which allows to refresh a `OneTime` binding to a newer value.

## Implementation in Uno Platform

Uno is interpreting the XAML in a very different way, when compared to WinUI. Uno generates code at compile for most of the document, not just for the bindings, where WinUI generates code for `x:Bind` and XBF for the rest of the XAML.

In order to obtain a sufficiently close implementation of `x:Bind` the first iteration in Uno was only relying on the `Binding` engine to perform bindings. The only difference was in the default DataContext used by the resulting `Binding` object (`this` for `x:Bind`, the `DataContext` property for `DataTemplate`).

However, in order to implement the functions portions of `x:Bind`, more code needed to be generated. To the ability to observe changes on multiple source paths, in both `INotifyPropertyChanged` types, as well as `DependencyProperty` instances, runtime reflection or `TypeMetadataProvider` based path observation is used.

When encountering an `x:Bind` markup, the Uno generator parses the expression to generate a C# compatible expression, then extracts the "update sources". At this point, code is generated to invoke Uno specific APIs `Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider` that add more information to the `Binding` class with properties paths, the binding source, and the function to execute when the target property needs to be updated.

For a typical OneTime `x:Bind` implementation:

```xml
<TextBlock Text="{x:Bind TypeProperty.Value, FallbackValue=42}" />
```

The code is similar to this:

```csharp
c18.SetBinding(
    global::Windows.UI.Xaml.Controls.TextBlock.TextProperty, 
    new Windows.UI.Xaml.Data.Binding{ 
        Mode = global::Windows.UI.Xaml.Data.BindingMode.OneWay,
    }
    .Apply(___b => 
        global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(
            ___b, // The binding to update
            this, // The DataContext
            ___ctx => Add(InstanceDP, MyxBindClassInstance.MyIntProperty), // the code to execute
            new [] {"InstanceDP", "MyxBindClassInstance.MyIntProperty"} // The properties to observe
        )
    )
);
```

At this point of the implementation, the executed expression is not yet decomposed to provide finer support for `FallbackValue`, where any member of an observed path may be null, and generate a NullReferenceException. A more advanced implementation will do so and stop the evaluation when a path member is null. In any case, the fallback value is applied if an exception occurs when executing the binding function.

The binding engine can still detect an invalid path, and stop the execution when the path has more than two indirections.
