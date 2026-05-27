---
uid: Uno.Contributing.ThemeResource
---

# Theme resource internals for contributors

This article looks at the way theme resources are handled under the hood, including the way that resource assignations are resolved, and the way that assigned resources are updated, for example when the active theme changes in the running application (from light to dark or vice versa). It's aimed at Uno Platform contributors looking to better understand the internals of the feature.

From hereon, an understanding of the contract and consumer-visible behavior of [XAML resources](https://learn.microsoft.com/windows/apps/design/style/xaml-resource-dictionary) and [ThemeResource markup extensions](https://learn.microsoft.com/windows/apps/design/style/xaml-theme-resources) is assumed. Review those links if you need a refresher on how the feature is expected to behave.

## Resource resolution

When a theme resource or static resource assignation is resolved, the framework looks through all ResourceDictionaries that are in scope from the point of assignation, in a defined order, for a resource with a matching key.

### Priority of ResourceDictionaries

Scope and priority are determined by the following rules:

- if the `{ThemeResource}` assignation is itself inside a dictionary, other than inside a template, then the containing dictionary is checked first.
- if the assignation is on a `frameworkElement` or the logical child of a `frameworkElement`, and the `frameworkElement` is in the visual tree, then the `Resources` of the `frameworkElement` and its visual ancestors are checked in order, outwards from the `frameworkElement`. This typically occurs when the element is first loaded into the visual tree, or when the active theme changes (see below).
- the `Application.Resources` dictionary is checked next.
- if the XAML containing the assignation comes from an external assembly (ie, a class library), then next the dictionaries within that assembly are checked. (Note that Uno's behavior departs somewhat from WinUI here, because only the `Themes/Generic.xaml` dictionary from that assembly should be checked - see [issue #4424](https://github.com/unoplatform/uno/issues/4424).)
- finally, 'system level' resources are checked. This simply means the resources that are defined within the Uno.UI assembly.

### Searching within a ResourceDictionary

Resolving a resource within a `ResourceDictionary` is a somewhat complex operation in its own right, because a `ResourceDictionary` may contain nested dictionaries within its `MergedDictionaries` and `ThemeDictionaries` collection properties, those dictionaries may in turn contain dictionaries, and so on recursively. This can be thought of as a 'resource tree' that is traversed in order to find a match for a given resource key.

The matching resource is determined according to the following priority:

- the key-value pairs directly added to the `ResourceDictionary` are considered first.
- ResourceDictionaries in the `MergedDictionaries` collection are searched, in reverse order, in other words the last dictionary added has first priority.
- The `ResourceDictionary` in `ThemeDictionaries` whose key matches the active theme will be searched. This will either be the dictionary whose key exactly matches the active theme (eg "Dark" key when dark theme is active), or the "Default"-keyed dictionary if it exists and no exact match is found. Note that if an exact-match dictionary is found, the "Default" dictionary will be ignored, even if the resource key is not found within the exact-match dictionary.
- finally, if the search was initiated from consumer code, eg via indexing or the public `TryGetValue()` method, then system level resources will be checked, as defined above.

Searching for a resource is a very frequently performed operation and a bottleneck during page loading, and as such is heavily optimized within `ResourceDictionary`, via caching, dedicated internal resource keys, and other mechanisms. It may also be optimized 'outside' `ResourceDictionary` by using a tool to create a single flat merged dictionary out of multiple top-level ControlName.xaml files, which is faster to search (`O(1)`) than many nested MergedDictionaries.

## Resource updates

The resource update machinery can be broadly split into two parts: registering resource assignations for updates; and propagating updates to assigned values when the active theme changes.

### Registering assignations for updates

ThemeResource assignments can only be created in XAML - WinUI provides no way to create them in code. Thus, in Uno, all assignations come ultimately from either [XamlFileGenerator](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs) or XamlReader/[XamlObjectBuilder](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Markup/Reader/XamlObjectBuilder.cs).

There are 3 cases for ThemeResource assignations that are handled differently:

1. Assigning an ordinary dependency property to a ThemeResource reference.
2. Assigning an ordinary non-dependency property to a ThemeResource reference.
3. Assigning the Value property of a [Setter](https://learn.microsoft.com/uwp/api/windows.ui.xaml.setter) to a ThemeResource reference.

In case 1., we create a `ResourceBinding`, an internal Uno-only type that inherits from `BindingBase`, that binds the property to the ThemeResource, allowing its value to be subsequently reevaluated when the owner enters the live visual tree and also when the active theme changes. This is done through the `ResourceResolver` static class. As much as possible, XAML-generated code should use helper intermediaries that will not change when the underlying implementation evolves. XAML-generated code compiled with older versions of Uno may run against newer Uno versions, in the case of 3rd-party libraries, and we don't want to introduce breaking changes that could be avoided.

In case 2. we can't create a binding because we don't have a dependency property to bind to. The best we can do in this case is to do a one-time resolution of the value.

In case 3., the details of the ThemeResource reference are captured on the `Setter` (via the `ApplyThemeResourceUpdateValues()` extension method). When the setter is later applied, either within a `Style` or within a `VisualState`, then the ThemeResource assignation will be applied according to either case 1. or case 2. depending on whether the target property is a DependencyProperty or not.

In cases 1. and 3., at the time of registration we capture the 'context'. This is an instance of `XamlParseContext`, which captures the assembly in which the ThemeResource reference is declared, and is used to determine the scope which should be used when resolving it.

### Propagating updates on theme change

A theme change update is propagated whenever the active theme changes. This may be because a change in the system theme was detected (eg the user enabled dark mode in their system settings, or perhaps it changed automatically based on the time of day), or because the `RequestedTheme` property on the root element of the visual tree was programmatically changed (eg to support an in-app light/dark mode toggle).

In either case, the `Application.OnResourcesChanged()` method will be called. It will:

1. Update all items in the tree of ResourceDictionaries rooted by `Application.Resources`.
2. Update all items in system resources, ie ResourceDictionaries defined within the Uno.UI assembly.
3. Update all items within the active visual tree.

Cases 1. and 2. are functionally identical, operating on different root dictionaries.

Note that changing the active theme is something that we expect to happen relatively rarely during any given app session, and usually due to explicit user input. The relevant code nonetheless sits alongside and interacts with code paths that are very heavily used during any app session: loading controls, applying bindings, resolving resources etc, which all have an important impact on perceived performance. As much as possible, the theme change machinery has been and should be architected to place its 'performance burden' upon the update itself, rather than the code supporting the update which is called frequently during ordinary activities of the app.

#### Updating resource bindings

Resource bindings are managed by `DependencyObjectStore`. It checks if any properties of its parent instance are resource-bound, by checking if it has a non-empty resource bindings list.

If so, for each binding, it tries to re-resolve the correct theme value from the ResourceDictionaries currently in its scope. If it can, it will then set the value, with the correct precedence as captured when the binding was created.

The `DependencyObjectStore` will also propagate the resource binding update to any 'logical children' that are also DependencyObjects, but not FrameworkElements. (Child FrameworkElements will be updated during the traversal of the visual tree.) For performance reasons, the `DependencyObjectStore` does not explicitly maintain a list of logical children. Instead, the `InnerUpdateChildResourceBindings()` method looks at all current property values and pushes the update to those that are either DependencyObjects or collections of DependencyObjects. In case this is inadequate, it also tries to cast to the `IAdditionalChildrenProvider` internal interface, which allows a type to explicitly specify its logical child dependency objects.

#### Update other FrameworkElement properties

The `FrameworkElement.UpdateThemeBindings()` method is virtual. In addition to updating resource bindings, some controls override it to perform specific updates required when the theme changes. For example, `TextBlock` and `Control` set the default-precedence value of Foreground to the appropriate one for the current theme.

#### Updating 'theme-change-aware' entities

Some non-FrameworkElement types may also need to react to the theme change. This is supported by the `IThemeChangeAware` internal interface, whose `OnThemeChanged()` method is invoked after resource bindings are applied. For example, `Storyboard` implements this interface in order to re-apply long-running animations with the correct updated values.

## Resolving against the owner's inherited theme (outside the theme walk)

Each `ResourceDictionary` exposes one sub-dictionary per theme (Light / Dark / HighContrast / Default), and `dict.TryGetValue(key)` selects the sub-dictionary based on `Themes.Active` — the current top of the global `_requestedThemeForSubTree` stack. During a theme walk (`FrameworkElement.NotifyThemeChangedCore`), the element being processed pushes its own theme onto that stack before the descendant traversal runs, so descendant lookups happen against the right sub-dictionary.

Theme-resource lookups also happen **outside** a theme walk, however:

- **Parse-time** `ApplyResource` (initial value captured when XAML is first realized) — `Themes.Active` reflects the application/OS theme at that point, not the owner's inherited theme.
- **`ThemeResourceReference.RefreshValue`** (deferred refresh from item-container generation, re-templating, animation key-frame application, or any code path that re-applies a pinned binding after load).
- **`DependencyObjectStore.InnerUpdateResourceBindingsUnsafe`** (the `_resourceBindings` fallback path that complements `_themeResources` for `ThemeResource`-bound DPs, runs during Phase 2 of `UpdateAllThemeReferences`).

When the application's theme differs from the owner's inherited element-level theme (e.g. `RequestedTheme="Light"` on a subtree inside an application that follows an OS=Dark setting), these out-of-walk lookups would otherwise select the wrong Light/Dark sub-dictionary. The visible symptom is a Light-pinned subtree resolving Dark values for its theme-bound properties after a deferred refresh.

To preserve the invariant that a `ThemeResource` always resolves against the element's inherited `ActualTheme`, the three lookup sites above call `ThemeResourceReference.PushOwnerThemeIfDifferent(owner)` before the dictionary lookup and pop in a `finally` block. The helper:

1. Walks the owner chain — visual parent → logical parent → templated parent — to find the nearest `UIElement` ancestor with a stored Light/Dark theme. The walk is depth-bounded as a guard against pathological/cyclic trees.
2. Falls back to a hop to `AssociatedObject` (resolved via reflection, see below) when the chain ends at a non-`UIElement` `DependencyObject` such as a Microsoft.Xaml.Behaviors-style behavior.
3. Compares the resolved theme to the current top of the stack and pushes only when they differ; the caller pops in `finally`.

### Behavior-style hosts (non-UIElement DependencyObjects with ThemeResource-bound properties)

A Microsoft.Xaml.Behaviors `Behavior<T>` is a `DependencyObject` that attaches to a `UIElement` via its `AssociatedObject` property. Behaviors can declare `DependencyProperty`s of their own and bind them to `{ThemeResource}` values in XAML, but they do not participate in the visual tree directly: their visual-tree parent is the `AssociatedObject`. The owner chain walk handles this case by reflecting for an `AssociatedObject` property on the behavior (using `BindingFlags.DeclaredOnly` and walking the type hierarchy, so the lookup is unambiguous when `Behavior<T>` shadows the non-generic base `Behavior.AssociatedObject`) and continuing the walk from there. Uno.UI does not take a hard dependency on `Microsoft.Xaml.Behaviors`; the reflection step is the integration point.

### Theme-walk-time guard (no inversion during NotifyThemeChangedCore)

`FrameworkElement.NotifyThemeChangedCore` is structured as:

1. (step 2 in the implementation) **push** the NEW theme to the global stack.
2. (step 4) call `UpdateThemeBindings` to re-resolve resource-bound properties.
3. (step 5) `SetTheme(theme)` — persist the NEW theme onto the element so that descendants and event handlers observe it via `ActualTheme`.
4. (step 6) propagate to children, then pop the stack in `finally`.

This ordering means that during step 4, the element's stored `_theme` is still the **OLD** value (because `SetTheme` runs at step 5). If `PushOwnerThemeIfDifferent` were called from a lookup inside `UpdateThemeBindings` without a guard, it would read the chain's OLD `_theme`, see that it differs from the just-pushed NEW theme on the stack, and push the OLD theme back on top — overwriting the cascade's correct push and inverting the theme by exactly one toggle step.

The guard avoids that: `PushOwnerThemeIfDifferent` is a no-op while any `UIElement` in the owner chain has `IsProcessingThemeWalk == true`. The cascade has already pushed the right theme, the lookup uses that, and the OLD `_theme` is correctly ignored. Outside a theme walk (parse-time, deferred refresh, post-walk re-resolution), the owner chain's `_theme` is consistent with the inherited theme and the push fires normally.

## Hot reload updates

The ResourceBinding machinery is reused for supporting XAML Hot Reload on standalone ResourceDictionaries. When an application is run in debug with XAML Hot Reload enabled, ResourceBindings are created for all resources assignations, including StaticResource assignations. When a standalone ResourceDictionary XAML file is edited, the file is re-parsed and then an update is triggered in much the same way as if the active theme had changed, so that any modified resources will be reapplied.

To support these different use cases whilst preserving correct semantics, resource bindings have a `ResourceUpdateReason` flag associated them. This ensures that static resources are not updated when they shouldn't be while hot reload is operating.
