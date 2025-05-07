---
uid: uno.development.using.markup
---

# UI Markup

Uno Platform provides two ways for defining your UI: WinUI XAML or [C# Markup](xref:Uno.Extensions.Markup.Overview).

You can choose either one for developing your application, based on your preferences.

> [!NOTE]
> At this time, [Hot Design](xref:Uno.HotDesign.Overview) only supports designing XAML markup files.

## XAML Markup

XAML is an XML based UI definition declarative language, and Uno Platform uses the WinUI 3 flavor.

Elements such as `Grid`, `StackPanel` and `TextBlock` can be used to defined your user interface.

For more information about XAML, see the [Microsoft documentation](https://learn.microsoft.com/en-us/windows/apps/design/layout/) on the topic.

## C# Markup

C# Markup is a declarative, fluent-style syntax for defining the layout of an application in C#.

With C# Markup, you can define both the layout and the logic of your application using the same language. C# Markup leverages the same underlying object model as XAML, meaning that it has all the same capabilities, such as data binding, converters, and access to resources. You can use all the built-in controls, any custom controls you create, and any 3rd party controls, all from C# Markup.

For more information, see our overview of [C# Markup](xref:Uno.Extensions.Markup.Overview).

## Related documentation

- [WinUI Development topics](xref:Uno.Development.WinUIDevelopmentDoc)
- [WinUI Design](xref:Uno.Development.WinUIDesignDoc)
