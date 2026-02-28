---
uid: Uno.Development.WinUIDevelopmentDoc
---

# WinUI documentation - Development

Uno Platform's API is derived from the WinUI application framework. Microsoft provides [extensive documentation for WinUI](https://learn.microsoft.com/windows/apps/). Rather than duplicate all of it, here we list selected entries that are especially applicable to Uno Platform developers.

The resources below explain many aspects of the API shared by WinUI and Uno Platform in more detail, including layouting, styling and templating, data binding, and more.

## Layouting

* [Basic layout concepts](https://learn.microsoft.com/windows/apps/design/layout/layouts-with-xaml)
* [Alignment, margin, padding](https://learn.microsoft.com/windows/apps/design/layout/alignment-margin-padding)
* [Panels](https://learn.microsoft.com/windows/apps/design/layout/layout-panels)
* [Custom panels](https://learn.microsoft.com/windows/apps/design/layout/custom-panels-overview)
* [Transforms](https://learn.microsoft.com/windows/apps/design/layout/transforms)

## Drawing

* [Images and image brushes](https://learn.microsoft.com/windows/apps/design/controls/images-imagebrushes)
* [Shapes](https://learn.microsoft.com/windows/apps/design/controls/shapes)
* [Path geometry syntax](https://learn.microsoft.com/windows/apps/develop/platform/xaml/move-draw-commands-syntax)
* [Brushes](https://learn.microsoft.com/windows/apps/design/style/brushes) _(note: see [Brush types supported on non-Windows platforms](./features/shapes-and-brushes.md#implemented-brushes--properties))_

## Styling and templating

* [Styling](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-styles)
* [Control templates](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-control-templates)
* [Template Settings](https://learn.microsoft.com/windows/apps/develop/platform/xaml/template-settings-classes)
* [XAML resources and ResourceDictionary](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-resource-dictionary)
* [Theme resources](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-theme-resources)

## Controls

* [Control basics](https://learn.microsoft.com/windows/apps/design/controls/controls-and-events-intro)
* [Navigation view](https://learn.microsoft.com/windows/apps/design/controls/navigationview)
* [Tab view](https://learn.microsoft.com/windows/apps/design/controls/tab-view)
* [ItemsView](https://learn.microsoft.com/windows/apps/design/controls/itemsview)
* [ListView and GridView](https://learn.microsoft.com/windows/apps/design/controls/listview-and-gridview)
  * [Nested UI](https://learn.microsoft.com/windows/apps/design/controls/nested-ui)
* [FlipView](https://learn.microsoft.com/windows/apps/design/controls/flipview)
* [PipsPager](https://learn.microsoft.com/windows/apps/design/controls/pipspager)
* [TreeView](https://learn.microsoft.com/windows/apps/design/controls/tree-view)
* [ItemsRepeater](https://learn.microsoft.com/windows/apps/design/controls/items-repeater)

### Lists

* [Item templating](https://learn.microsoft.com/windows/apps/design/controls/item-containers-templates)
* [Data template selection](https://learn.microsoft.com/windows/apps/design/controls/data-template-selector)
* [Selection modes](https://learn.microsoft.com/windows/apps/design/controls/selection-modes)
* [Filtering lists](https://learn.microsoft.com/windows/apps/design/controls/listview-filtering)

## Animations

* [Storyboarded animations](https://learn.microsoft.com/windows/apps/design/motion/storyboarded-animations)

## Events and pointer input

* [Pointer input](https://learn.microsoft.com/windows/apps/design/input/handle-pointer-input)
* [Events and routed events](https://learn.microsoft.com/windows/apps/develop/platform/xaml/events-and-routed-events-overview)

## Data binding and dependency properties

* [Data binding overview](https://learn.microsoft.com/windows/apps/develop/data-binding/data-binding-overview)
* [Data binding in depth](https://learn.microsoft.com/windows/apps/develop/data-binding/data-binding-in-depth)
* [x:Bind to functions](https://learn.microsoft.com/windows/apps/develop/data-binding/function-bindings)
* [Dependency properties overview](https://learn.microsoft.com/windows/apps/develop/platform/xaml/dependency-properties-overview)
* [Custom dependency properties](https://learn.microsoft.com/windows/apps/develop/platform/xaml/custom-dependency-properties)
* [Attached properties overview](https://learn.microsoft.com/windows/apps/develop/platform/xaml/attached-properties-overview)
* [Custom attached properties](https://learn.microsoft.com/windows/apps/develop/platform/xaml/custom-attached-properties)
* [Data binding in MVVM architecture](https://learn.microsoft.com/windows/apps/develop/data-binding/data-binding-and-mvvm)

## XAML

* [XAML overview](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-overview)
* [XAML syntax and terminology](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-syntax-guide)
* [Property path syntax rules](https://learn.microsoft.com/windows/apps/develop/platform/xaml/property-path-syntax)
* [XAML namespaces](https://learn.microsoft.com/windows/apps/develop/platform/xaml/xaml-namespaces-and-namespace-mapping)

### XAML attributes

* [x:Class attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-class-attribute)
* [x:DefaultBindMode attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-defaultbindmode-attribute)
* [x:FieldModifier attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-fieldmodifier-attribute)
* [x:Key attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-key-attribute)
* [x:Load attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-load-attribute)
* [x:Name attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-name-attribute)
* [x:Phase attribute](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-phase-attribute)
* [x:Uid directive](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-uid-directive)

### Markup extensions

* [x:Bind markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/x-bind-markup-extension)
* [`{Binding}` markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/binding-markup-extension)
* [CustomResource markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/customresource-markup-extension)
* [RelativeSource markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/relativesource-markup-extension)
* [StaticResource markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/staticresource-markup-extension)
* [TemplateBinding markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/templatebinding-markup-extension)
* [ThemeResource markup extension](https://learn.microsoft.com/windows/apps/develop/platform/xaml/themeresource-markup-extension)
