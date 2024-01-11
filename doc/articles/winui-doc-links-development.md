---
uid: Uno.Development.WinUIDevelopmentDoc
---

# WinUI documentation - Development

Uno Platform's API is derived from the WinUI application framework. Microsoft provides [extensive documentation for WinUI](https://docs.microsoft.com/en-us/windows/uwp/). Rather than duplicate all of it, here we list selected entries that are especially applicable to Uno Platform developers.

The resources below explain many aspects of the API shared by WinUI and Uno Platform in more detail, including layouting, styling and templating, data binding, and more.

## Layouting

* [Basic layout concepts](https://docs.microsoft.com/en-us/windows/uwp/design/layout/layouts-with-xaml)
* [Alignment, margin, padding](https://docs.microsoft.com/en-us/windows/uwp/design/layout/alignment-margin-padding)
* [Panels](https://docs.microsoft.com/en-us/windows/uwp/design/layout/layout-panels)
* [Custom panels](https://docs.microsoft.com/en-us/windows/uwp/design/layout/custom-panels-overview)
* [Transforms](https://docs.microsoft.com/en-us/windows/uwp/design/layout/transforms)

## Drawing

* [Images and image brushes](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/images-imagebrushes)
* [Shapes](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/shapes)
* [Path geometry syntax](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/move-draw-commands-syntax)
* [Brushes](https://docs.microsoft.com/en-us/windows/uwp/design/style/brushes) _(note: see [Brush types supported on non-Windows platforms](features/shapes-and-brushes.md#implemented-brushes--properties))_

## Styling and templating

* [Styling](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/xaml-styles)
* [Control templates](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/control-templates)
* [XAML resources and ResourceDictionary](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/resourcedictionary-and-xaml-resource-references)
* [Theme resources](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/xaml-theme-resources)

## Controls

* [Control basics](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/controls-and-events-intro)
* [Navigation view](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/navigationview)
* [Tab view](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/tab-view)

### Lists

* [Item templating](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/item-containers-templates)
* [Data template selection](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/data-template-selector)
* [Selection modes](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/selection-modes)
* [Filtering lists](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/listview-filtering)

## Animations

* [Storyboarded animations](https://docs.microsoft.com/en-us/windows/uwp/design/motion/storyboarded-animations)

## Events and pointer input

* [Pointer input](https://docs.microsoft.com/en-us/windows/uwp/design/input/handle-pointer-input)
* [Events and routed events](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/events-and-routed-events-overview)

## Data binding and dependency properties

* [Data binding overview](https://docs.microsoft.com/en-us/windows/uwp/data-binding/data-binding-quickstart)
* [Data binding in depth](https://docs.microsoft.com/en-us/windows/uwp/data-binding/data-binding-in-depth)
* [x:Bind to functions](https://docs.microsoft.com/en-us/windows/uwp/data-binding/function-bindings)
* [Dependency properties overview](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/dependency-properties-overview)
* [Custom dependency properties](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/custom-dependency-properties)
* [Attached properties overview](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/attached-properties-overview)
* [Custom attached properties](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/custom-attached-properties)
* [MVVM architecture](https://docs.microsoft.com/en-us/windows/uwp/data-binding/data-binding-and-mvvm)

## XAML

* [XAML overview](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/xaml-overview)
* [XAML syntax and terminology](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/xaml-syntax-guide)
* [Property path syntax rules](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/property-path-syntax)
* [XAML namespaces](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/xaml-namespaces-and-namespace-mapping)

### XAML attributes

* [x:Class attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-class-attribute)
* [x:DefaultBindMode attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-defaultbindmode-attribute)
* [x:FieldModifier attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-fieldmodifier-attribute)
* [x:Key attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-key-attribute)
* [x:Load attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-load-attribute)
* [x:Name attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-name-attribute)
* [x:Phase attribute](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-phase-attribute)
* [x:Uid directive](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-uid-directive)

### Markup extensions

* [x:Bind markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension)
* [`{Binding}` markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/binding-markup-extension)
* [CustomResource markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/customresource-markup-extension)
* [RelativeSource markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/relativesource-markup-extension)
* [StaticResource markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/staticresource-markup-extension)
* [TemplateBinding markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/templatebinding-markup-extension)
* [ThemeResource markup extension](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/themeresource-markup-extension)

## Other

* [Concept map - UWP, Android, iOS](https://docs.microsoft.com/en-us/windows/uwp/porting/android-ios-uwp-map)
