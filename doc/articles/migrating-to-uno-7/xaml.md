---
uid: Uno.Development.MigratingToUno7.Xaml
---

# XAML and default styles

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** your XAML uses `clr-namespace:`, `XamlDefault*` or `NativeDefault*` styles, hand-merged Fluent dictionaries, relative URIs or `x:Name` on custom `DependencyObject` types, or doesn't merge `XamlControlsResources`.

**Skip it if** your XAML is plain WinUI markup with no relative URIs, and your app merges `XamlControlsResources`. Conditional prefixes such as `skia:` are covered in [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes).

## Markup that no longer builds

### The WPF-style `clr-namespace:` xmlns form is no longer accepted

WinUI only supports
`using:`; Uno used to also accept `clr-namespace:MyApp.Controls;assembly=MyLib` and silently
strip the prefix and the `;assembly=` token. Replace each declaration with the `using:` form —
the assembly is inferred, so the `;assembly=` part is simply dropped:

```diff
- xmlns:local="clr-namespace:MyApp.Controls;assembly=MyLib"
+ xmlns:local="using:MyApp.Controls"
```

This is enforced at **build time** (the `UXAML0006` diagnostic) and at **run time** by
`XamlReader.Load` and Hot Reload, which throw a `XamlParseException`. The declaration itself is
rejected whether or not the prefix is used, so unused `clr-namespace:` declarations must also be
removed — the only exemption is a prefix listed in `mc:Ignorable` on the root element.

See also: [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes).

### `legacy:ListView` and `legacy:GridView` no longer compile

The XAML generator used to resolve any
type it could not find in `using:Uno.UI.Controls.Legacy` to the `Microsoft.UI.Xaml.Controls` type of the
same name, a leftover of the native legacy lists removed in 7.0, so this markup already produced the
regular controls. Use `ListView` and `GridView` from the default namespace. Types that do exist in that
namespace are unaffected.

See also: [`Uno.UI.Controls.Legacy.ProgressRing`](xref:Uno.Development.MigratingToUno7.RemovedApis#unouicontrolslegacyprogressring), [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes).

### Binding an attached property whose owner type cannot be found is a build error

For
`u:ListViewBaseCommand.Command="{Binding OpenItem}"` where `ListViewBaseCommand` does not
resolve, the XAML generator used to bind `Command` by name on the `ListView` itself, so the
markup silently hit an unrelated property of that name, or nothing at all. It now reports
`UXAML0001`, as it already did for a literal value. Reference the assembly that declares the
owner type, fix the `using:` namespace, or remove the attribute.

See also: [`ListViewBase.ItemClickCommand`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand).

## Relative URIs

### A relative URI on a `Uri`-typed property now compiles to `ms-resource:///Files/…`

A relative URI on a `Uri`-typed property now compiles to `ms-resource:///Files/…`, the MRT
local-resource form WinUI produces. Previously Uno emitted the relative string verbatim. This
affects custom `Uri` properties, `HyperlinkButton.NavigateUri`, `Hyperlink.NavigateUri`,
`BitmapIcon.UriSource`, `BitmapIconSource.UriSource`, `WebView2.Source`,
`MediaPlayerElement.Source`, `LottieVisualSource.UriSource`, and the `Uri` passed to
`RandomAccessStreamReference.CreateFromUri`:

```diff
  <BitmapIcon UriSource="Assets/icon.png" />
- // compiled value: Assets/icon.png
+ // compiled value: ms-resource:///Files/Assets/icon.png
```

The rewrite is a prefix concat that drops a leading `/` and ignores the folder of the XAML file
containing it, exactly as WinUI does. **Images declared in application XAML keep loading the same
asset** — Uno resolves `ms-resource:///Files/X` as `ms-appx:///X`. To keep a value verbatim, write
it as an explicit `ms-appx:///…` URI in XAML.

Only the four image sinks Uno already resolved as assets carry that mapping. The properties above
that never resolved a relative URI stay inert with the new value, exactly as they are on WinUI —
a hand-written `ms-resource:///Files/…` is treated the same as the compiler-generated form, since
that *is* the MRT local-file namespace and Uno ships no MRT to resolve it any other way.

Two changes go beyond the value you read back:

- **A relative value is now absolute at the point of use.** `WebView2.Source` with a relative value
  used to throw `ArgumentException` while the page initialized; it now navigates a `ms-resource:` URI
  no web view can service, so the failure is silent instead of loud. A `NavigateUri` written without
  a scheme (`www.example.com`) likewise becomes `ms-resource:///Files/www.example.com` and reaches
  the launcher as an unregistered scheme, and a relative `Control.DefaultStyleResourceUri` no longer
  throws while resolving. Give these properties absolute URIs — `https://…`, `ms-appx:///…`.
- **In a library, relative URIs resolve differently per property type.** On an `ImageSource`-typed
  property the value now carries the library's assembly prefix (`ms-appx:///MyLib/Assets/x.png`), so
  a library's own assets resolve — but a library asset that previously resolved from the *consuming
  app's* root no longer does. On a `Uri`-typed property the `ms-resource` form cannot express that
  prefix, so it resolves against the app root; a library shipping assets for `BitmapIcon` must use
  an explicit `ms-appx:///MyLib/…` URI.

Properties typed `ImageSource` are unaffected in shape but now resolve consistently: `Image.Source`,
`ImageBrush.ImageSource`, and custom `ImageSource` properties all resolve a relative URI against
the base URI. `ImageBrush.ImageSource` and custom `ImageSource` properties previously kept the
relative string. `ResourceDictionary.Source` is unchanged.

Two divergences from WinUI remain here, both deliberate:

- **The base URI is the assembly root, not the XAML file's folder.** `Assets/logo.png` written in
  `Views/MainPage.xaml` resolves as `ms-appx:///Assets/logo.png` on Uno and
  `ms-appx:///Views/Assets/logo.png` on WinUI. This predates the rewrite and is unchanged by it.
- **Svg assets keep the `ms-appx:///` form.** Measured on WinAppSDK 1.7, WinUI compiles every
  relative svg URI to `ms-resource:///Files/…` — including one written on `Image.Source` rather
  than on `SvgImageSource.UriSource`. Uno keeps `ms-appx:///`, because that form is resolved as an
  asset path and so carries the assembly prefix a library's own svg assets need; the `ms-resource`
  form resolves only against the application root.

See also: [`bundle://` image scheme removed](xref:Uno.Development.MigratingToUno7.Packages#bundle-image-scheme-removed-iostvos), [Android drawables](xref:Uno.Development.MigratingToUno7.Behavior#android-drawables).

## Fluent styles and resources

### The `XamlDefault*` styles are removed

They were Uno-only keys that never existed in WinUI,
so `{StaticResource XamlDefaultButton}` and friends no longer resolve. Removed keys:
`XamlDefaultButton`, `XamlDefaultRepeatButton`, `XamlDefaultToggleButton`, `XamlDefaultRadioButton`,
`XamlDefaultCheckBox`, `XamlDefaultComboBox`, `XamlDefaultSlider`, `XamlDefaultToggleSwitch`,
`XamlDefaultFrame`, `XamlDefaultPivot`, `XamlDefaultPasswordBox`, `XamlDefaultTextBox`,
`XamlDefaultProgressBar`, `XamlDefaultScrollBar`, `XamlDefaultCommandBar`, `XamlDefaultMenuBar`,
`XamlDefaultAppBarButton`, `XamlDefaultAppBarToggleButton`, `XamlDefaultAppBarSeparator` and
`XamlDefaultAppBarElementContainer`, along with the related Uno-only keys `XamlAppBarButton`,
`XamlAppBarToggleButton`, `XamlAppBarSeparator`, `XamlCommandBar`, `XamlDefaultCommandBar_EllipsisButton`,
`XamlNoAnimationScrollBar`, the `XamlDefaultScrollBar_*` templates and `DefaultFlyoutPresenter`. Also gone
are the unused `NativeScrollViewerStyle`, `ListViewBaseScrollViewerStyle` and `DefaultSplitViewTemplate`
resources and the Uno-only `ContentLinkBackgroundColor` and `ContentLinkForegroundColor` colors.

As on WinUI, `DefaultSplitViewStyle`, `SplitViewPaneRootCornerRadius`,
`ContentDialogLightDismissOverlayBackground` and the `ProgressBar*ThemeBrush`,
`ProgressBarIndicatorPauseOpacity` and `ProgressBarThemeMinHeight` resources now come only from
`XamlControlsResources`.

If you only used one to get the default look of a control, remove the `Style` attribute and the
implicit style applies. If you derive from it (`BasedOn`) and merge `XamlControlsResources`, use the
WinUI equivalent instead, for example `DefaultButtonStyle`. `Frame` and `AppBarElementContainer` have no
keyed WinUI equivalent: drop the `BasedOn`, or copy the style.

```diff
- <Style x:Key="MyButton" TargetType="Button" BasedOn="{StaticResource XamlDefaultButton}">
+ <Style x:Key="MyButton" TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
```

See also: [Native default styles](xref:Uno.Development.MigratingToUno7.RemovedApis#native-default-styles).

### Styles that WinUI ships in `XamlControlsResources` now come only from there

Uno.UI no longer
carries its own, often older, copies of them, nor of the Fluent acrylic brushes such as
`AcrylicBackgroundFillColorDefaultBrush`. An app that does not merge `XamlControlsResources` gets
no default template for `BreadcrumbBar`, `ColorPicker`, `DropDownButton`, `InfoBadge`, `InfoBar`,
`ItemContainer`, `ItemsView`, `MenuBar`, `NumberBox`, `PersonPicture`, `PipsPager`,
`ProgressRing`, `RadioButtons`, `RatingControl`, `RefreshContainer`, `RefreshVisualizer`, `ScrollView`,
`SelectorBar`, `SplitButton`, `SwipeControl`, `TreeView` and `TwoPaneView`. Controls that WinUI also styles in its `generic.xaml`, such as
`ScrollViewer`, `ProgressBar`, `AppBarButton` and `MenuFlyout`, fall back to those styles, and
`DefaultScrollViewerStyle` is now defined by `XamlControlsResources`, as on WinUI.

### `XamlControlsResources` dictionaries are re-synced with WinUI 2.5.1

Most templates and
resource values now match WinUI exactly, so some controls look slightly different. Examples: the
`CheckBox` glyph animation, `MenuFlyout` and `SplitMenuFlyoutItem`, and HighContrast colors.
WinUI's newer resources, such as `SubtleButtonStyle` and the `ListBox` theme resources, are now
available. The Reveal resources come from a single `RevealBrush_themeresources.xaml`, as on WinUI 3,
instead of per-OS-version dictionaries.
`MediaTransportControls` no longer has a `CompactOverlayButton`, matching WinUI 3's template, so
`IsCompactOverlayButtonVisible` has no visible effect.

### The Fluent theme-resources dictionary is now `themeresources.xaml`

The Fluent theme-resources dictionary is now `themeresources.xaml`, matching WinUI. Its
`ms-appx:` URI changes accordingly, so a dictionary that merged the old path by hand no longer
resolves. Merging `XamlControlsResources` — the supported way to load the Fluent styles — is
unaffected, since it resolves the URI internally:

```diff
- <ResourceDictionary Source="ms-appx:///Microsoft.UI.Xaml/Themes/themeresources_v2.xaml" />
+ <ResourceDictionary Source="ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml" />
```

The `_v2` suffix was an Uno-only artifact of the Fluent V1/V2 split; with V1 removed there is one
theme-resources dictionary, under the name WinUI itself uses.

See also: [`Uno.UI.FluentTheme.v2` merged](xref:Uno.Development.MigratingToUno7.Packages#unouifluentthemev2-assembly-merged-into-unouifluenttheme), [Fluent Design resource-version types](xref:Uno.Development.MigratingToUno7.RemovedApis#fluent-design-resource-version-types).

### `ContentDialog`'s light-dismiss overlay

`ContentDialog`'s light-dismiss overlay now uses `SystemControlPageBackgroundMediumAltMediumBrush`,
matching WinUI, so overriding `ContentDialogLightDismissOverlayBackground` no longer changes it.

## Names

### `x:Name` on a custom `DependencyObject` no longer sets its own `Name` property

As on WinUI,
`x:Name` on any non-`FrameworkElement` `DependencyObject` is stored in the framework's name property
(`FrameworkElement.NameProperty`, read back with `GetValue(FrameworkElement.NameProperty)`), even when
the type declares its own CLR `Name` property or a `"Name"` dependency property. Uno used to write
`x:Name` into that property, so code that read it now gets its default value. Use a plain `Name="..."`
attribute to set the type's own property, or read `GetValue(FrameworkElement.NameProperty)`.

### `VisualState.Name` and `VisualStateGroup.Name` default to `""`, not `null`

This matches WinUI.
Code that checks an unnamed state or group with `Name == null` or `Name ?? ...` should use
`string.IsNullOrEmpty(Name)`. As on WinUI, `VisualStateManager.GoToState(control, "", ...)` returns
`false` and never activates an unnamed state.

## Related pages

- [Libraries and conditional code](xref:Uno.Development.MigratingToUno7.Libraries) for conditional prefixes such as `skia:`.
- [Removed public APIs](xref:Uno.Development.MigratingToUno7.RemovedApis) for removed types used from XAML.
- [Packages and Uno Features](xref:Uno.Development.MigratingToUno7.Packages) for the Fluent theme assemblies.
- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.
